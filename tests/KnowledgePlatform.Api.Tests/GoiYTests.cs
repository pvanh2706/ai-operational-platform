using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Api.Webhooks;
using KnowledgePlatform.Domain.GoiY;
using KnowledgePlatform.Domain.Sop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Ô `J` của sơ đồ (P6, 2026-10-10): *"ghi lại đã gợi ý gì, người có dùng không"* — và vòng nét đứt `J → G`.
///
/// 🛑 Bốn điều file này giữ, và cả bốn là chỗ một lần "dọn cho gọn" sẽ làm số đo nói dối:
/// <list type="bullet">
///   <item>KHÔNG BIẾT vẫn là một lượt (RETRIEVED không đạt) — bỏ nó đi là phễu chỉ đếm những lần thành công;</item>
///   <item>SHOWN chỉ khi bên nhận trả 2xx — lượt gửi hỏng KHÔNG được đếm là đã giao;</item>
///   <item>ACCEPTED/USED chỉ đến từ lời khai, kèm tên người; ba nấc kia không khai tay được;</item>
///   <item>khai "nhóm đúng là X" thì lần hỏi SAU của chính case đó đi theo X — đó là vòng `J → G`.</item>
/// </list>
/// ⚠ Mỗi test một khách hàng riêng: số lượt là dữ liệu đếm được của cả tenant.
/// </summary>
public sealed class GoiYTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private const string GocLink = "http://kp-noi-bo.thu-nghiem:5119";
    private const string DichThu = "http://ben-nhan.thu-nghiem/webhook";

    private readonly List<string> _fileTam = [];
    private readonly BenNhanGia _benNhan = new();

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private sealed record BoiCanh(ApiFactory App, HttpClient C, Guid TenantId, string NhomA, string NhomB, string MaTran, string KhoaCase);

    private string FileTam(string ten, object noiDung)
    {
        var duong = Path.Combine(Path.GetTempPath(), ten);
        File.WriteAllText(duong, JsonSerializer.Serialize(noiDung));
        _fileTam.Add(duong);
        return duong;
    }

    /// <summary>Bảng đo đưa case vào NHÓM A; taxonomy riêng biết cả A lẫn B.</summary>
    private async Task<BoiCanh> MoiAsync(bool coWebhook = false)
    {
        var (key, tenantId) = await db.TaoTenantMoiAsync();
        var id = Guid.CreateVersion7().ToString("N");
        var nhomA = $"nhom-a-{id}";
        var nhomB = $"nhom-b-{id}";
        var maTran = $"GY-{id}";

        var bang = FileTam($"dt-gy-{id}.json", new
        {
            ketQua = new Dictionary<string, object> { [maTran] = new { nhom = nhomA, nguon = "noi-ro-trong-mo-ta", vietTat = "t" } },
        });
        var taxonomy = FileTam($"tx-gy-{id}.json", new
        {
            nhom = new[] { new { ten = nhomA, moTa = "a", caseKeys = Array.Empty<string>() }, new { ten = nhomB, moTa = "b", caseKeys = Array.Empty<string>() } },
        });

        var app = ApiFactory.Dedicated(key)
            .With("DinhTuyen:DuongDanBangTraSan", bang)
            .With("DinhTuyen:DuongDanTaxonomy", taxonomy)
            .With("Link:PublicBaseUrl", GocLink)
            .With("Webhook:BatVongQuet", "false")
            .WithServices(s => s.AddHttpClient(WebhookSender.TenHttpClient).ConfigurePrimaryHttpMessageHandler(() => _benNhan));
        var c = app.CreateClient();

        if (coWebhook) (await c.PutAsJsonAsync("/internal/webhook", new { url = DichThu })).EnsureSuccessStatusCode();

        var khoaCase = $"jira:{maTran}";
        (await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = khoaCase, subject = "Không phát hành được hoá đơn" } })).EnsureSuccessStatusCode();

        return new BoiCanh(app, c, tenantId, nhomA, nhomB, maTran, khoaCase);
    }

    private static SopDraft Nhap(string nhom, string viec) => new()
    {
        Nhom = nhom,
        SoCaseDungSau = 1,
        TrieuChungVao = [new SopSymptom("x", ["ES-1"])],
        BuocKiem = [new SopCheckStep { Ma = "K1", CauHoi = "Kiểm gì đó", NoiXem = "đâu đó", Nhanh = [new SopBranch("Thấy", "S1", ["ES-1"], "evidence-noi-ro")] }],
        BuocSua = [new SopFixStep("S1", viec, ["ES-1"])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(1, ["ES-1"]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res, HttpStatusCode mong = HttpStatusCode.OK)
    {
        Assert.Equal(mong, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>Nạp + duyệt tri thức của một nhóm (và tuỳ chọn duyệt quy trình của nó).</summary>
    private static async Task DungNhomAsync(BoiCanh b, string nhom, string viec, bool duyetQuyTrinh = false)
    {
        var nap = await JsonAsync(await b.C.PostAsJsonAsync("/knowledge/sop-draft", Nhap(nhom, viec)));
        foreach (var r in nap.GetProperty("nguyenNhan").EnumerateArray())
        {
            (await b.C.PostAsJsonAsync($"/knowledge/{r.GetProperty("id").GetGuid()}/approve",
                new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true })).EnsureSuccessStatusCode();
        }

        if (!duyetQuyTrinh) return;

        (await b.C.PostAsJsonAsync($"/processes/{nap.GetProperty("quyTrinh").GetProperty("id").GetGuid()}/approve",
            new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true })).EnsureSuccessStatusCode();
    }

    private async Task<LuotGoiY> LuotAsync(BoiCanh b, Guid id)
    {
        await using var ctx = db.OpenContext(b.TenantId);
        return await ctx.Set<LuotGoiY>().Include(x => x.SuKien).SingleAsync(x => x.Id == id);
    }

    private async Task GuiMotVongAsync(BoiCanh b)
    {
        await using var scope = b.App.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(b.TenantId);
        await scope.ServiceProvider.GetRequiredService<WebhookSender>().GuiDenHanAsync();
    }

    private static Task<HttpResponseMessage> PhanHoiAsync(BoiCanh b, Guid id, object body) =>
        b.C.PostAsJsonAsync($"/goi-y/{id}/phan-hoi", body);

    // =========================================================================

    [Fact]
    public async Task Hoi_ghi_MOT_luot_kem_anh_chup_va_hai_nac_dau()
    {
        var b = await MoiAsync();
        using var app = b.App;
        await DungNhomAsync(b, b.NhomA, "Sửa cấu hình A");

        var traLoi = await JsonAsync(await b.C.PostAsJsonAsync("/hoi", new { maTicket = b.MaTran }));
        var id = traLoi.GetProperty("maLuotGoiY").GetGuid();

        var luot = await LuotAsync(b, id);
        Assert.Equal("hoi", luot.KenhRa);
        Assert.Equal(b.KhoaCase, luot.KhoaNguonCase);
        Assert.Contains("Sửa cấu hình A", luot.NoiDung);   // ẢNH CHỤP thứ đã trả ra
        Assert.Equal([NacGoiY.Retrieved, NacGoiY.Shown], luot.SuKien.OrderBy(s => s.Nac).Select(s => s.Nac));
        Assert.All(luot.SuKien, s => Assert.True(s.DatDuoc));
    }

    /// <summary>KHÔNG BIẾT vẫn là một lượt — bỏ nó đi là phễu chỉ đếm những lần thành công.</summary>
    [Fact]
    public async Task Khong_biet_VAN_la_mot_luot_RETRIEVED_khong_dat()
    {
        var b = await MoiAsync();
        using var app = b.App;

        var traLoi = await JsonAsync(await b.C.PostAsJsonAsync("/hoi", new { maTicket = b.MaTran }));
        Assert.False(traLoi.GetProperty("bietTraLoi").GetBoolean());

        var luot = await LuotAsync(b, traLoi.GetProperty("maLuotGoiY").GetGuid());
        Assert.False(luot.SuKien.Single(s => s.Nac == NacGoiY.Retrieved).DatDuoc);
    }

    /// <summary>
    /// 🛑 SHOWN = bên nhận ĐÃ trả 2xx. Lượt hỏng chỉ có RETRIEVED — đếm nó là "đã giao" thì phễu nói dối đúng
    /// ở ngày bên nhận sập.
    /// </summary>
    [Fact]
    public async Task Webhook_hong_thi_CHI_RETRIEVED_giao_duoc_moi_co_SHOWN()
    {
        var b = await MoiAsync(coWebhook: true);
        using var app = b.App;
        await DungNhomAsync(b, b.NhomA, "Sửa cấu hình A");

        _benNhan.TraVe = HttpStatusCode.InternalServerError;
        await GuiMotVongAsync(b);

        await using (var ctx = db.OpenContext(b.TenantId))
        {
            var dsLuot = await ctx.Set<LuotGoiY>().Include(x => x.SuKien).Where(x => x.KenhRa == "webhook").ToListAsync();
            var hong = Assert.Single(dsLuot);
            Assert.DoesNotContain(hong.SuKien, s => s.Nac == NacGoiY.Shown);
        }

        // Lượt thử lại tới hạn sau 1 phút — đẩy mốc về quá khứ thay vì chờ.
        await using (var ctx = db.OpenContext(b.TenantId))
        {
            await ctx.Database.ExecuteSqlRawAsync("UPDATE kp.webhook_delivery SET \"NextAttemptAt\" = now() - interval '1 minute'");
        }

        _benNhan.TraVe = HttpStatusCode.OK;
        await GuiMotVongAsync(b);

        var goi = JsonSerializer.Deserialize<JsonElement>(_benNhan.DaNhan.Last().Body);
        var luot = await LuotAsync(b, goi.GetProperty("maLuotGoiY").GetGuid());
        Assert.Contains(luot.SuKien, s => s.Nac == NacGoiY.Shown && s.DatDuoc);
        Assert.NotEqual(goi.GetProperty("maGoi").GetGuid(), luot.Id);   // maGoi (chống trùng) ≠ mã lượt gợi ý
    }

    [Fact]
    public async Task Mo_link_trong_goi_webhook_ghi_OPENED_vao_DUNG_luot()
    {
        var b = await MoiAsync(coWebhook: true);
        using var app = b.App;
        await DungNhomAsync(b, b.NhomA, "Sửa cấu hình A");
        await GuiMotVongAsync(b);

        var goi = JsonSerializer.Deserialize<JsonElement>(_benNhan.DaNhan.Single().Body);
        var link = goi.GetProperty("huongDan")[0].GetProperty("link").GetString()!;

        (await b.App.CreateClient().GetAsync(new Uri(link).AbsolutePath)).EnsureSuccessStatusCode();

        var luot = await LuotAsync(b, goi.GetProperty("maLuotGoiY").GetGuid());
        Assert.Contains(luot.SuKien, s => s.Nac == NacGoiY.Opened && s.DatDuoc);
    }

    [Fact]
    public async Task Phan_hoi_USED_duoc_ghi_la_LOI_KHAI_kem_ten_nguoi()
    {
        var b = await MoiAsync();
        using var app = b.App;
        await DungNhomAsync(b, b.NhomA, "Sửa cấu hình A");
        var id = (await JsonAsync(await b.C.PostAsJsonAsync("/hoi", new { maTicket = b.MaTran }))).GetProperty("maLuotGoiY").GetGuid();

        var kq = await JsonAsync(await PhanHoiAsync(b, id, new { nac = "used", datDuoc = true, nguoiKhai = "chị Lan", ghiChu = "đúng bệnh" }));
        Assert.Equal("USED", kq.GetProperty("nac").GetString());

        var used = (await LuotAsync(b, id)).SuKien.Single(s => s.Nac == NacGoiY.Used);
        Assert.Equal("chị Lan", used.NguoiKhai);
        Assert.True(used.DatDuoc);
    }

    [Theory]
    [InlineData("shown", true, "chị Lan")]     // nấc hệ thống tự ghi — không khai tay được
    [InlineData("used", null, "chị Lan")]      // thiếu datDuoc: "không dùng" và "chưa nói" trông giống hệt nhau
    [InlineData("used", true, "")]             // lời khai không có tác giả
    public async Task Phan_hoi_khong_hop_le_thi_400(string nac, bool? datDuoc, string nguoiKhai)
    {
        var b = await MoiAsync();
        using var app = b.App;
        var id = (await JsonAsync(await b.C.PostAsJsonAsync("/hoi", new { maTicket = b.MaTran }))).GetProperty("maLuotGoiY").GetGuid();

        Assert.Equal(HttpStatusCode.BadRequest, (await PhanHoiAsync(b, id, new { nac, datDuoc, nguoiKhai })).StatusCode);
    }

    /// <summary>
    /// 🎯 Vòng `J → G`: định tuyến đưa case vào nhóm A, người dùng khai nhóm đúng là B → lần hỏi SAU của chính case
    /// đó đi theo B. Case đang chạy quy trình của nhóm A thì lượt chạy đó bị bỏ — bởi chính người khai.
    /// </summary>
    [Fact]
    public async Task Khai_nhomDung_thi_lan_hoi_SAU_di_nhom_moi_va_luot_chay_nhom_cu_bi_bo()
    {
        var b = await MoiAsync();
        using var app = b.App;
        await DungNhomAsync(b, b.NhomA, "Sửa cấu hình A", duyetQuyTrinh: true);
        await DungNhomAsync(b, b.NhomB, "Sửa cấu hình B");

        var lan1 = await JsonAsync(await b.C.PostAsJsonAsync("/hoi", new { maTicket = b.MaTran }));
        Assert.Equal(b.NhomA, lan1.GetProperty("nhom").GetString());
        Assert.True(lan1.TryGetProperty("quyTrinh", out _));        // đang chạy quy trình nhóm A

        var phanHoi = await JsonAsync(await PhanHoiAsync(b, lan1.GetProperty("maLuotGoiY").GetGuid(),
            new { nac = "accepted", datDuoc = false, nguoiKhai = "chị Lan", ghiChu = "bệnh này là của nhóm B", nhomDung = b.NhomB }));

        var sua = phanHoi.GetProperty("daSuaDinhTuyen");
        Assert.Equal(b.NhomB, sua.GetProperty("nhomMoi").GetString());
        Assert.NotEqual(JsonValueKind.Null, sua.GetProperty("daBoLuotChay").ValueKind);

        var lan2 = await JsonAsync(await b.C.PostAsJsonAsync("/hoi", new { maTicket = b.MaTran }));
        Assert.Equal(b.NhomB, lan2.GetProperty("nhom").GetString());
        Assert.Contains("Sửa cấu hình B", lan2.GetProperty("huongDan")[0].GetProperty("cachXuLy")[0].GetProperty("noiDung").GetString());
    }

    [Fact]
    public async Task Nhom_dung_KHONG_co_trong_taxonomy_thi_400_va_khong_sua_gi()
    {
        var b = await MoiAsync();
        using var app = b.App;
        await DungNhomAsync(b, b.NhomA, "Sửa cấu hình A");
        var id = (await JsonAsync(await b.C.PostAsJsonAsync("/hoi", new { maTicket = b.MaTran }))).GetProperty("maLuotGoiY").GetGuid();

        var res = await PhanHoiAsync(b, id, new { nac = "accepted", datDuoc = false, nguoiKhai = "x", nhomDung = "nhom-bia-ra" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        await using var ctx = db.OpenContext(b.TenantId);
        Assert.False(await ctx.Set<DinhTuyenDaNap>().AnyAsync());
    }

    [Fact]
    public async Task Thong_ke_dem_PHEU_theo_tung_nac()
    {
        var b = await MoiAsync();
        using var app = b.App;
        await DungNhomAsync(b, b.NhomA, "Sửa cấu hình A");

        var id = (await JsonAsync(await b.C.PostAsJsonAsync("/hoi", new { maTicket = b.MaTran }))).GetProperty("maLuotGoiY").GetGuid();
        await JsonAsync(await b.C.PostAsJsonAsync("/hoi", new { maTicket = "KHONG-CO-THAT" }));
        (await PhanHoiAsync(b, id, new { nac = "used", datDuoc = true, nguoiKhai = "x" })).EnsureSuccessStatusCode();

        var tk = await JsonAsync(await b.C.GetAsync("/internal/goi-y/thong-ke"));

        Assert.Equal(2, tk.GetProperty("soLuot").GetInt32());
        var theoNac = tk.GetProperty("theoNac").EnumerateArray().ToDictionary(x => x.GetProperty("nac").GetString()!);
        Assert.Equal(1, theoNac["RETRIEVED"].GetProperty("soLuotDat").GetInt32());
        Assert.Equal(1, theoNac["RETRIEVED"].GetProperty("soLuotKhongDat").GetInt32());
        Assert.Equal(2, theoNac["SHOWN"].GetProperty("soLuotDat").GetInt32());
        Assert.Equal(1, theoNac["USED"].GetProperty("soLuotDat").GetInt32());
    }
}
