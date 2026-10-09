using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Api.Webhooks;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Bên nhận webhook GIẢ. Ghi lại đúng chuỗi byte đã tới, không phải một object đã dựng lại —
/// chữ ký chỉ kiểm được trên chuỗi thật.
/// </summary>
public sealed class BenNhanGia : HttpMessageHandler
{
    public List<(string Url, string Body, string? ChuKy, string? MaGoi)> DaNhan { get; } = [];

    /// <summary>Những gói đã tới một địa chỉ cụ thể — dùng để hỏi "khách A có nhận nhầm của B không".</summary>
    public IReadOnlyList<(string Url, string Body, string? ChuKy, string? MaGoi)> ToiDia(string url) =>
        [.. DaNhan.Where(x => x.Url == url)];

    public HttpStatusCode TraVe { get; set; } = HttpStatusCode.OK;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);

        DaNhan.Add((
            request.RequestUri!.ToString(),
            body,
            request.Headers.TryGetValues(WebhookSender.HeaderChuKy, out var s) ? s.FirstOrDefault() : null,
            request.Headers.TryGetValues(WebhookSender.HeaderMaGoi, out var m) ? m.FirstOrDefault() : null));

        return new HttpResponseMessage(TraVe);
    }
}

/// <summary>
/// Đường TRẢ KẾT QUẢ VỀ (`D1-1`) — đường ra thứ ba của sản phẩm.
///
/// 🛑 Bốn test nặng nhất:
/// <list type="bullet">
///   <item><c>Gui_lai_cung_mot_tin_hieu_KHONG_xep_them_luot</c> — tính idempotent mà `IM-20`
///   giữ ở đường VÀO phải không rò ra ở đường RA.</item>
///   <item><c>Chua_khai_bao_dich_thi_KHONG_xep_hang</c> — hàng đợi đầy những dòng không bao
///   giờ gửi được là một cách rò rỉ dung lượng rất im lặng.</item>
///   <item><c>Ben_nhan_loi_thi_thu_lai_chu_khong_mat</c> — nửa còn lại của `D1-1`.</item>
///   <item><c>Goi_gui_di_da_qua_cong_che_AR_o</c> — đường ra thứ ba cũng phải qua cổng che.</item>
/// </list>
/// </summary>
public sealed class WebhookGuiVeTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private const string GocLink = "http://kp-noi-bo.thu-nghiem:5119";
    private const string DichThu = "http://ben-nhan.thu-nghiem/webhook";

    private readonly List<string> _fileTam = [];

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private readonly BenNhanGia _benNhan = new();

    private sealed record BoiCanh(string Nhom, string Ticket, string Bang, string TenantKey, Guid TenantId);

    /// <summary>
    /// Một bối cảnh ĐỘC LẬP HOÀN TOÀN: khách hàng riêng, nhóm riêng, mã ticket riêng, bảng
    /// định tuyến riêng.
    ///
    /// 🛑 **Khách hàng riêng mới là phần quan trọng, và nó đã trả giá để học.** Bản đầu của
    /// file này dùng chung tenant A cho mọi test, và ba test đỏ vì đúng một lý do: cấu hình
    /// đích webhook và hàng đợi gửi là dữ liệu "CÓ hay KHÔNG CÓ" của cả tenant. Test
    /// *"chưa khai báo đích thì không xếp hàng"* đọc phải cái đích mà test trước vừa khai,
    /// rồi xanh/đỏ tuỳ THỨ TỰ CHẠY. Cùng hình dạng lỗi mà `VongTriThucTests` đã ghi.
    /// </summary>
    /// <param name="bangKhoaTran">
    /// Bảng đo lưu mã TRẦN (<c>WH-…</c>) trong khi case mang khoá nguồn (<c>jira:WH-…</c>) — đúng
    /// hình dạng của <c>dinh-tuyen-150-case.json</c> thật. Mặc định <c>false</c> giữ hành vi cũ
    /// của file này, và chính mặc định đó là thứ đã GIẤU lỗi lệch khoá tới 2026-10-10.
    /// </param>
    private async Task<BoiCanh> MoiBoiAsync(bool dinhTuyenBiet = true, bool bangKhoaTran = false)
    {
        var (tenantKey, tenantId) = await db.TaoTenantMoiAsync();
        var id = Guid.CreateVersion7().ToString("N");
        var nhom = $"nhom-wh-{id}";
        var ticket = $"jira:WH-{id}";

        var duong = Path.Combine(Path.GetTempPath(), $"dinh-tuyen-wh-{id}.json");
        File.WriteAllText(duong, JsonSerializer.Serialize(new
        {
            ketQua = new Dictionary<string, object>
            {
                [bangKhoaTran ? $"WH-{id}" : ticket] = new
                {
                    nhom = dinhTuyenBiet ? nhom : "KHONG-BIET",
                    nguon = "noi-ro-trong-mo-ta",
                    vietTat = "test",
                },
            },
        }));
        _fileTam.Add(duong);

        return new BoiCanh(nhom, ticket, duong, tenantKey, tenantId);
    }

    private ApiFactory App(BoiCanh b) =>
        new ApiFactory(new Dictionary<string, string?>
            {
                ["Tenancy:Mode"] = "DedicatedSingleTenant",
                ["Tenancy:TenantExternalKey"] = b.TenantKey,
                ["Auth:AcknowledgeUnauthenticatedApi"] = "true",
                ["Link:PublicBaseUrl"] = GocLink,
                ["DinhTuyen:DuongDanBangTraSan"] = b.Bang,

                // ⚠ TẮT vòng quét nền: nếu bật, nó nhặt mất chính dòng test đang chuẩn bị
                //   kiểm -> test đỏ NGẪU NHIÊN. Test gọi thẳng WebhookSender thay vì chờ.
                ["Webhook:BatVongQuet"] = "false",
            })
            .WithServices(s => s
                .AddHttpClient(WebhookSender.TenHttpClient)
                .ConfigurePrimaryHttpMessageHandler(() => _benNhan));

    private static SopDraft Nhap(string nhom) => new()
    {
        Nhom = nhom,
        SoCaseDungSau = 2,
        TrieuChungVao = [new SopSymptom("Không phát hành được hoá đơn", ["ES-9001"])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1",
                CauHoi = "Ô ký hiệu có giá trị không?",
                NoiXem = "Màn phát hành hoá đơn",
                Nhanh = [new SopBranch("Ô trống nhưng chọn được", "S1", ["ES-9001"], "evidence-noi-ro")],
            },
        ],
        BuocSua = [new SopFixStep("S1", "Chọn ký hiệu trên màn phát hành rồi phát hành lại", ["ES-9001"])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(2, ["ES-9001", "ES-9002"]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    private static async Task KhaiBaoDichAsync(HttpClient c)
    {
        var res = await c.PutAsJsonAsync("/internal/webhook", new { url = DichThu });
        res.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> TinHieuAsync(HttpClient c, string khoaNguon) =>
        await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = khoaNguon, subject = "Không phát hành được hoá đơn" } });

    private static async Task NapVaDuyetAsync(HttpClient c, string nhom)
    {
        var res = await c.PostAsJsonAsync("/knowledge/sop-draft", Nhap(nhom));
        res.EnsureSuccessStatusCode();
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        foreach (var r in kq.GetProperty("nguyenNhan").EnumerateArray())
        {
            var duyet = await c.PostAsJsonAsync($"/knowledge/{r.GetProperty("id").GetGuid()}/approve",
                new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true });
            duyet.EnsureSuccessStatusCode();
        }
    }

    /// <summary>Chạy một vòng gửi — thay cho việc chờ vòng quét nền.</summary>
    private async Task<int> GuiMotVongAsync(ApiFactory app, BoiCanh b)
    {
        await using var scope = app.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(b.TenantId);
        return await scope.ServiceProvider.GetRequiredService<WebhookSender>().GuiDenHanAsync();
    }

    private async Task<List<WebhookDelivery>> HangDoiAsync(BoiCanh b)
    {
        await using var ctx = db.OpenContext(b.TenantId);
        return await ctx.Set<WebhookDelivery>()
            .Where(d => d.CaseSourceReference == b.Ticket)
            .ToListAsync();
    }

    // =========================================================================
    //  Khai báo đích
    // =========================================================================

    [Fact]
    public async Task Bi_mat_ky_chi_hien_ra_DUNG_MOT_LAN()
    {
        var b = await MoiBoiAsync();
        using var app = App(b);
        var c = app.CreateClient();

        var tao = await (await c.PutAsJsonAsync("/internal/webhook", new { url = DichThu }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(string.IsNullOrWhiteSpace(tao.GetProperty("biMatKy").GetString()));

        var doc = await (await c.GetAsync("/internal/webhook")).Content.ReadFromJsonAsync<JsonElement>();

        // Đọc lại KHÔNG trả bí mật — cùng luật với khoá API. Mất thì khai lại để sinh cái mới.
        Assert.Equal(JsonValueKind.Null, doc.GetProperty("biMatKy").ValueKind);
        Assert.Equal(DichThu, doc.GetProperty("url").GetString());
    }

    [Theory]
    [InlineData("khong-phai-url")]
    [InlineData("ftp://ben-nhan/webhook")]
    [InlineData("")]
    public async Task Url_khong_dung_duoc_thi_400(string url)
    {
        var b = await MoiBoiAsync();
        using var app = App(b);

        var res = await app.CreateClient().PutAsJsonAsync("/internal/webhook", new { url });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // =========================================================================
    //  Xếp hàng
    // =========================================================================

    [Fact]
    public async Task Tin_hieu_moi_thi_xep_mot_luot_gui_ve()
    {
        var b = await MoiBoiAsync();
        using var app = App(b);
        var c = app.CreateClient();
        await KhaiBaoDichAsync(c);

        (await TinHieuAsync(c, b.Ticket)).EnsureSuccessStatusCode();

        var hang = await HangDoiAsync(b);
        Assert.Single(hang);
        Assert.Equal(WebhookDeliveryState.Pending, hang[0].State);
    }

    /// <summary>
    /// 🛑 Bên gửi thật CÓ gửi lại — webhook của Jira retry, job đồng bộ chạy lại. `IM-20` giữ
    /// tính idempotent ở đường VÀO; nếu đường RA không giữ thì mỗi lần gửi lại là một lần
    /// khách nhận thêm một thông báo cho cùng một ticket.
    /// </summary>
    [Fact]
    public async Task Gui_lai_cung_mot_tin_hieu_KHONG_xep_them_luot()
    {
        var b = await MoiBoiAsync();
        using var app = App(b);
        var c = app.CreateClient();
        await KhaiBaoDichAsync(c);

        (await TinHieuAsync(c, b.Ticket)).EnsureSuccessStatusCode();
        (await TinHieuAsync(c, b.Ticket)).EnsureSuccessStatusCode();
        (await TinHieuAsync(c, b.Ticket)).EnsureSuccessStatusCode();

        Assert.Single(await HangDoiAsync(b));
    }

    [Fact]
    public async Task Chua_khai_bao_dich_thi_KHONG_xep_hang()
    {
        var b = await MoiBoiAsync();
        using var app = App(b);

        // KHÔNG khai báo đích.
        (await TinHieuAsync(app.CreateClient(), b.Ticket)).EnsureSuccessStatusCode();

        Assert.Empty(await HangDoiAsync(b));
    }

    // =========================================================================
    //  Gửi
    // =========================================================================

    /// <summary>
    /// 🛑 Lỗi THẬT tìm ra 2026-10-09 khi đối chiếu sơ đồ với code: connector gửi case mang khoá
    /// nguồn <c>jira:ES-…</c>, bảng đo lưu <c>ES-…</c>, và webhook đưa nguyên khoá nguồn vào bộ
    /// định tuyến → mọi lượt trượt bảng, ra KHÔNG BIẾT. Không crash, không đỏ test nào, vì mọi test
    /// ở đây tự dựng bảng có sẵn tiền tố. Test này dựng bảng ĐÚNG hình dạng bảng thật.
    /// </summary>
    [Fact]
    public async Task Bang_do_luu_ma_TRAN_thi_webhook_VAN_dinh_tuyen_ra_nhom()
    {
        var b = await MoiBoiAsync(bangKhoaTran: true);
        using var app = App(b);
        var c = app.CreateClient();

        await KhaiBaoDichAsync(c);
        await NapVaDuyetAsync(c, b.Nhom);
        (await TinHieuAsync(c, b.Ticket)).EnsureSuccessStatusCode();

        Assert.Equal(1, await GuiMotVongAsync(app, b));

        var goi = JsonSerializer.Deserialize<JsonElement>(_benNhan.DaNhan.Single().Body);
        Assert.True(goi.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal(b.Nhom, goi.GetProperty("nhom").GetString());
    }

    [Fact]
    public async Task Goi_gui_di_mang_tom_tat_VA_link_va_chu_ky_dung()
    {
        var b = await MoiBoiAsync();
        using var app = App(b);
        var c = app.CreateClient();

        await KhaiBaoDichAsync(c);
        await NapVaDuyetAsync(c, b.Nhom);
        (await TinHieuAsync(c, b.Ticket)).EnsureSuccessStatusCode();

        Assert.Equal(1, await GuiMotVongAsync(app, b));

        var (_, body, chuKy, maGoi) = _benNhan.DaNhan.Single();
        var goi = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.Equal("1", goi.GetProperty("phienBan").GetString());
        Assert.True(goi.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal(b.Nhom, goi.GetProperty("nhom").GetString());

        var h = goi.GetProperty("huongDan").EnumerateArray().First();
        Assert.False(string.IsNullOrWhiteSpace(h.GetProperty("tomTat").GetString()));
        Assert.StartsWith($"{GocLink}/xem/kpl_", h.GetProperty("link").GetString());

        // Khoá idempotency (`D1-1`) phải trùng mã gói trong thân — bên nhận dùng cái nào cũng được.
        Assert.Equal(goi.GetProperty("maGoi").GetString(), maGoi);

        // Chữ ký tính trên ĐÚNG chuỗi byte đã tới.
        await using var ctx = db.OpenContext(b.TenantId);
        var biMat = (await ctx.Set<WebhookEndpoint>().FirstAsync()).SigningSecret;
        Assert.Equal("sha256=" + WebhookSender.Ky(biMat, body), chuKy);

        // Và hàng đợi đã đóng.
        Assert.Equal(WebhookDeliveryState.Delivered, (await HangDoiAsync(b)).Single().State);
    }

    /// <summary>
    /// `D4-1`: KHÔNG BIẾT vẫn GỬI, và nói thẳng lý do. Im lặng ở đây đọc ra thành "hệ thống
    /// chưa chạy" — khác hẳn với "đã chạy, và chưa có tri thức nào được duyệt cho ca này".
    /// </summary>
    [Fact]
    public async Task Khong_biet_van_GUI_va_noi_thang_ly_do()
    {
        var b = await MoiBoiAsync();
        using var app = App(b);
        var c = app.CreateClient();

        await KhaiBaoDichAsync(c);
        // CỐ Ý không nạp/duyệt gì: định tuyến ra đúng nhóm, nhưng nhóm chưa có SOP nào.
        (await TinHieuAsync(c, b.Ticket)).EnsureSuccessStatusCode();

        Assert.Equal(1, await GuiMotVongAsync(app, b));

        var goi = JsonSerializer.Deserialize<JsonElement>(_benNhan.DaNhan.Single().Body);

        Assert.False(goi.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal("nhom-chua-co-sop-duoc-duyet", goi.GetProperty("lyDoKhongBiet").GetString());
        Assert.Empty(goi.GetProperty("huongDan").EnumerateArray());
    }

    [Fact]
    public async Task Dinh_tuyen_khong_biet_thi_lyDo_la_cua_THU_NHAT()
    {
        var b = await MoiBoiAsync(dinhTuyenBiet: false);
        using var app = App(b);
        var c = app.CreateClient();

        await KhaiBaoDichAsync(c);
        (await TinHieuAsync(c, b.Ticket)).EnsureSuccessStatusCode();

        await GuiMotVongAsync(app, b);

        var goi = JsonSerializer.Deserialize<JsonElement>(_benNhan.DaNhan.Single().Body);
        Assert.Equal("dinh-tuyen-khong-biet", goi.GetProperty("lyDoKhongBiet").GetString());
    }

    /// <summary>
    /// 🛑 Nửa còn lại của `D1-1`: *"bên nhận sập thì không được mất kết quả"*. Dòng phải còn
    /// `Pending`, đếm lượt, và lùi lịch — chứ không biến mất.
    /// </summary>
    [Fact]
    public async Task Ben_nhan_loi_thi_thu_lai_chu_khong_mat()
    {
        var b = await MoiBoiAsync();
        using var app = App(b);
        var c = app.CreateClient();

        await KhaiBaoDichAsync(c);
        (await TinHieuAsync(c, b.Ticket)).EnsureSuccessStatusCode();

        _benNhan.TraVe = HttpStatusCode.InternalServerError;
        Assert.Equal(0, await GuiMotVongAsync(app, b));

        var luot = (await HangDoiAsync(b)).Single();
        Assert.Equal(WebhookDeliveryState.Pending, luot.State);
        Assert.Equal(1, luot.Attempts);
        Assert.Contains("500", luot.LastError);
        Assert.True(luot.NextAttemptAt > DateTimeOffset.UtcNow, "phải lùi lịch thử lại, không thử ngay");

        // Vòng sau chưa tới hạn -> KHÔNG gửi lại ngay. Đập cửa mỗi vòng là tự làm nguồn tải
        // khi bên kia đang sự cố.
        Assert.Equal(0, await GuiMotVongAsync(app, b));
        Assert.Single(_benNhan.DaNhan);
    }

    /// <summary>
    /// 🛑 Đường ra THỨ BA cũng phải qua cổng che `AR-o`. Gói này đi thẳng vào hệ thống của bên
    /// thứ ba (comment Jira, log của họ) và **không có đường thu hồi**.
    ///
    /// ⚠ Vật liệu lấy từ luật đã đo, không từ trí nhớ — xem ghi chú cùng ý ở `LinkXemTests`.
    /// </summary>
    [Fact]
    public async Task Goi_gui_di_da_qua_cong_che_AR_o()
    {
        var b = await MoiBoiAsync();
        using var app = App(b);
        var c = app.CreateClient();

        await KhaiBaoDichAsync(c);
        await NapVaDuyetAsync(c, b.Nhom);

        // Sửa bước xử lý thành một mẩu CÓ bí mật, rồi duyệt lại.
        await using (var ctx = db.OpenContext(b.TenantId))
        {
            var nhan = SopDraftToKnowledge.NhanNhom(b.Nhom);
            var rid = await ctx.KnowledgeRecords
                .Where(r => r.Assertions.Any(a => a.Kind == AssertionKind.Applicability && a.Content == nhan))
                .Select(r => r.Id)
                .FirstAsync();

            var aid = (await ctx.Assertions
                    .Where(a => a.KnowledgeRecordId == rid && a.Kind == AssertionKind.Handling)
                    .ToListAsync())
                .First(a => a.IsCurrent).Id;

            var sua = await c.PostAsJsonAsync($"/knowledge/{rid}/sua", new
            {
                nguoiSua = "support",
                sua = new[]
                {
                    new { assertionId = aid, noiDungMoi = "anh cho em ultraview với, Mật khẩu: 13579 nhé ạ", mucKiemChung = "Verified" },
                },
            });
            sua.EnsureSuccessStatusCode();
        }

        (await TinHieuAsync(c, b.Ticket)).EnsureSuccessStatusCode();
        await GuiMotVongAsync(app, b);

        var body = _benNhan.DaNhan.Single().Body;

        Assert.Contains("ultraview", body);
        Assert.DoesNotContain("13579", body);
    }
}
