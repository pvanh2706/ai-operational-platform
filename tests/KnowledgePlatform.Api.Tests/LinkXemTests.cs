using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Link xem tri thức (`D5-3`) — nửa "link" của `D3-1` (*tóm tắt + link*).
///
/// 🛑 Bốn test nặng nhất, đọc trước nếu chỉ đọc bốn cái:
/// <list type="bullet">
///   <item><c>Khong_phat_duoc_link_cho_ban_CHUA_DUYET</c> — nếu thiếu, link thành cửa sau đi
///   vòng qua đúng hai cổng mà <c>/hoi</c> đang giữ (`D4`).</item>
///   <item><c>Thieu_PublicBaseUrl_thi_TU_CHOI_chu_khong_doan</c> — đoán ở đây sinh ra link
///   <c>localhost</c>: tự bấm thì đúng, gửi cho người khác thì chết.</item>
///   <item><c>Ban_roi_khoi_Active_SAU_khi_phat_link_thi_khong_mo_duoc</c> — kiểm ở một chỗ
///   rồi tin mãi là cách một cổng đúng trở thành cổng hở.</item>
///   <item><c>Noi_dung_di_qua_cong_che_AR_o</c> — đường ra THỨ HAI cũng phải qua cổng che.</item>
/// </list>
/// </summary>
public sealed class LinkXemTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private const string GocThu = "http://kp-noi-bo.thu-nghiem:5119";

    private readonly List<string> _fileTam = [];

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private ApiFactory App(string tenantKey = "") =>
        ApiFactory.Dedicated(string.IsNullOrEmpty(tenantKey) ? db.TenantAKey : tenantKey)
            .With("Link:PublicBaseUrl", GocThu);

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
        BuocSua = [new SopFixStep("S1", "Chọn ký hiệu trên màn phát hành", ["ES-9001"])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(2, ["ES-9001", "ES-9002"]),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, [])),
    };

    private static string NhomMoi() => $"nhom-link-{Guid.CreateVersion7():N}";

    private static async Task<Guid> NapAsync(HttpClient client, string nhom)
    {
        var res = await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(nhom));
        res.EnsureSuccessStatusCode();
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();
        return kq.GetProperty("nguyenNhan").EnumerateArray().First().GetProperty("id").GetGuid();
    }

    private static async Task DuyetAsync(HttpClient client, Guid id)
    {
        var res = await client.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true });
        res.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> NapVaDuyetAsync(HttpClient client)
    {
        var id = await NapAsync(client, NhomMoi());
        await DuyetAsync(client, id);
        return id;
    }

    private static async Task<JsonElement> PhatLinkAsync(HttpClient client, Guid id, int? hanNgay = null)
    {
        var res = await client.PostAsJsonAsync($"/knowledge/{id}/link",
            new { nguoiPhat = "support", hanNgay });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    // =========================================================================
    //  PHÁT link
    // =========================================================================

    [Fact]
    public async Task Phat_duoc_link_cho_ban_da_duyet()
    {
        using var app = App();
        var client = app.CreateClient();

        var kq = await PhatLinkAsync(client, await NapVaDuyetAsync(client));

        var token = kq.GetProperty("token").GetString()!;
        var url = kq.GetProperty("url").GetString()!;

        Assert.StartsWith($"{KnowledgeLinkToken.TienTo}_", token);
        Assert.Equal($"{GocThu}/xem/{token}", url);

        // Hạn mặc định 14 ngày — phải là một mốc TRONG TƯƠNG LAI, không phải giá trị mặc
        // định của kiểu dữ liệu (một token hết hạn ngay lúc phát trông y hệt trong response).
        Assert.True(kq.GetProperty("hetHanLuc").GetDateTimeOffset() > DateTimeOffset.UtcNow.AddDays(13));
    }

    /// <summary>
    /// 🛑 Nếu thiếu cổng này, link thành CỬA SAU: bản `Draft` là đầu ra thô của model, chưa
    /// người nào nhìn qua. Trả nó cho người làm thật là đúng thứ `D4` sinh ra để chặn, và
    /// `/hoi` đã chặn — nhưng chặn ở một đường mà hở ở đường khác thì không chặn gì cả.
    /// </summary>
    [Fact]
    public async Task Khong_phat_duoc_link_cho_ban_CHUA_DUYET()
    {
        using var app = App();
        var client = app.CreateClient();

        var id = await NapAsync(client, NhomMoi());   // nạp xong là Draft, KHÔNG duyệt

        var res = await client.PostAsJsonAsync($"/knowledge/{id}/link", new { nguoiPhat = "support" });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("ĐÃ DUYỆT", await res.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// 🛑 Đoán địa chỉ gốc từ host của request sinh ra `http://localhost:5119/...` — chạy thử
    /// tại chỗ thấy đúng, gửi cho người khác thì chết. Chủ dự án chốt `H1-5` (local Windows)
    /// + `H1b-1` (LAN/VPN), nên link phải mang tên máy trong mạng, thứ request không nói ra.
    /// </summary>
    [Fact]
    public async Task Thieu_PublicBaseUrl_thi_TU_CHOI_chu_khong_doan()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);   // KHÔNG đặt Link:PublicBaseUrl
        var client = app.CreateClient();

        var id = await NapVaDuyetAsync(client);
        var res = await client.PostAsJsonAsync($"/knowledge/{id}/link", new { nguoiPhat = "support" });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Link:PublicBaseUrl", body);
        Assert.DoesNotContain("localhost", body);
    }

    [Fact]
    public async Task Thieu_nguoiPhat_thi_400()
    {
        using var app = App();
        var client = app.CreateClient();

        var id = await NapVaDuyetAsync(client);
        var res = await client.PostAsJsonAsync($"/knowledge/{id}/link", new { hanNgay = 7 });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Han_bi_ket_vao_tran_toi_da()
    {
        using var app = App().With("Link:HanToiDaNgay", "3");
        var client = app.CreateClient();

        var kq = await PhatLinkAsync(client, await NapVaDuyetAsync(client), hanNgay: 9999);

        Assert.True(kq.GetProperty("hetHanLuc").GetDateTimeOffset() < DateTimeOffset.UtcNow.AddDays(4));
    }

    /// <summary>
    /// Khoá API của khách B KHÔNG phát được link cho record của khách A. RLS lo phần này,
    /// nhưng câu trả lời phải là 404 — giống hệt ca "không có record nào như vậy".
    /// </summary>
    [Fact]
    public async Task Khach_khac_khong_phat_duoc_link_cho_record_cua_minh()
    {
        using var appA = App();
        using var appB = App(db.TenantBKey);

        var id = await NapVaDuyetAsync(appA.CreateClient());

        var res = await appB.CreateClient().PostAsJsonAsync(
            $"/knowledge/{id}/link", new { nguoiPhat = "support cua B" });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // =========================================================================
    //  XEM bằng link
    // =========================================================================

    [Fact]
    public async Task Mo_duoc_bang_link_va_KHONG_CAN_khoa_API()
    {
        using var app = App().RequireApiKey();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Authorization = ApiDatabaseFixture.Bearer(db.TenantAApiKey);

        var kq = await PhatLinkAsync(client, await NapVaDuyetAsync(client));
        var token = kq.GetProperty("token").GetString()!;

        // Client MỚI, không mang khoá API nào. Chính token là giấy thông hành.
        using var khach = app.CreateClient();
        var res = await khach.GetAsync($"/xem/{token}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("moDuoc").GetBoolean());
        Assert.Equal("noi-bo", body.GetProperty("phamViXem").GetString());
        Assert.NotEmpty(body.GetProperty("cachXuLy").EnumerateArray());
    }

    [Theory]
    [InlineData("kpl_khongcothat_khongcothat")]
    [InlineData("kp_nhamloaikhoa_nhamloaikhoa")]
    [InlineData("linh-tinh")]
    public async Task Token_sai_thi_404(string token)
    {
        using var app = App();

        var res = await app.CreateClient().GetAsync($"/xem/{token}");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    /// <summary>
    /// Hết hạn trả 404 **giống hệt** token không tồn tại. Phân biệt hai ca đó là nói cho
    /// người cầm link biết token ấy TỪNG có thật — đủ để dò xem một bản tri thức có tồn tại.
    /// </summary>
    [Fact]
    public async Task Token_HET_HAN_thi_404_giong_het_token_bia()
    {
        using var app = App();
        var client = app.CreateClient();
        var id = await NapVaDuyetAsync(client);

        // Phát thẳng qua directory với hạn ÂM: token hết hạn ngay khi sinh ra. Cách này
        // kiểm được ca hết hạn mà không phải chờ, và không phải tiêm đồng hồ giả.
        var directory = new LinkTokenDirectory(ApiFactory.ConnectionString);
        var (chuoi, _) = await directory.PhatAsync(db.TenantAId, id, "test", TimeSpan.FromSeconds(-1));

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/xem/{chuoi}")).StatusCode);
    }

    [Fact]
    public async Task Token_da_THU_HOI_thi_404()
    {
        using var app = App();
        var client = app.CreateClient();

        var id = await NapVaDuyetAsync(client);
        var token = (await PhatLinkAsync(client, id)).GetProperty("token").GetString()!;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/xem/{token}")).StatusCode);

        var directory = new LinkTokenDirectory(ApiFactory.ConnectionString);
        Assert.Equal(1, await directory.ThuHoiTheoRecordAsync(id));

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/xem/{token}")).StatusCode);
    }

    /// <summary>
    /// 🛑 **Test này bắt tôi sửa lại chính mình, ghi ra vì nó là một cái bẫy thật.** Bản đầu
    /// khẳng định: sửa xong thì link phải ĐÓNG. Nghe rất hợp lý — và SAI với quyết định đã
    /// có của dự án: `V3` gắn cờ `NEEDS_REVIEW` nhưng **KHÔNG rút khỏi `/hoi`**, vì người
    /// đang xử lý ticket cần bản tốt nhất hiện có kèm cảnh báo, chứ không cần một cánh cửa đóng.
    ///
    /// Đường link vì thế phải NHẤT QUÁN với `/hoi`, không được tự nghiêm hơn: hai đường đọc
    /// cùng một kho mà trả lời khác nhau thì người dùng học được rằng "thử đường kia xem" —
    /// và thói quen đó vô hiệu hoá cả hai cổng.
    ///
    /// Cái vẫn phải chặn là `Lifecycle` rời khỏi `Active` (Superseded/Retired), và đường đọc
    /// vẫn kiểm lại thay vì tin vào lượt kiểm lúc PHÁT.
    /// </summary>
    [Fact]
    public async Task Sua_xong_thi_link_VAN_mo_duoc_nhung_mang_co_NEEDS_REVIEW()
    {
        using var app = App();
        var client = app.CreateClient();

        var id = await NapVaDuyetAsync(client);
        var token = (await PhatLinkAsync(client, id)).GetProperty("token").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/xem/{token}")).StatusCode);

        // Người support sửa lại -> record tự thành NEEDS_REVIEW (V3).
        await using (var ctx = db.OpenContext(db.TenantAId))
        {
            // ⚠ `IsCurrent` là thuộc tính TÍNH TOÁN, không có cột nên EF không dịch được.
            // Lấy về rồi lọc trong bộ nhớ — vài dòng, không phải chỗ cần tối ưu.
            var assertionId = (await ctx.Assertions
                    .Where(a => a.KnowledgeRecordId == id && a.Kind == AssertionKind.Handling)
                    .ToListAsync())
                .First(a => a.IsCurrent).Id;

            var sua = await client.PostAsJsonAsync($"/knowledge/{id}/sua", new
            {
                nguoiSua = "support",
                sua = new[] { new { assertionId, noiDungMoi = "Bước sửa đã được viết lại", mucKiemChung = "Verified" } },
            });
            sua.EnsureSuccessStatusCode();
        }

        var res = await client.GetAsync($"/xem/{token}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("moDuoc").GetBoolean());

        // Cờ phải HIỆN RA, không im lặng: `S8`/`G4` — bày chỗ yếu ra chính là thứ người đọc cần.
        Assert.Equal("NEEDS_REVIEW", body.GetProperty("trangThai").GetString());

        // Và nội dung trả về là bản MỚI (bản B), không phải bản A còn sót.
        Assert.Contains(
            body.GetProperty("cachXuLy").EnumerateArray().Select(x => x.GetProperty("noiDung").GetString()!),
            x => x.Contains("viết lại"));
    }

    /// <summary>
    /// 🛑 Đường ra THỨ HAI cũng phải qua cổng che `AR-o`. Thêm một cửa ra mà quên cổng che
    /// là đúng hình dạng lỗi mà `IM-25` sinh ra để chặn — và cửa này nguy hơn payload gửi
    /// model ở một điểm: người cầm link chuyển tiếp nó được.
    /// </summary>
    [Fact]
    public async Task Noi_dung_di_qua_cong_che_AR_o()
    {
        using var app = App();
        var client = app.CreateClient();

        var id = await NapVaDuyetAsync(client);

        // ⚠ **Vật liệu lấy từ LUẬT ĐÃ ĐO, không từ trí nhớ người viết test.** Bản đầu của test
        // này bịa ra `pass 9h3kd0` và đỏ — không phải vì cổng che hỏng, mà vì luật `AR-j` chỉ
        // biết bí mật dạng SỐ (bốn hình dạng đo được trên corpus Jira thật). Đó đúng là bài học
        // `IM-37`/`IM-38` lặp lại ở ngay công cụ viết ra để chống nó. Chỗ hở ấy KHÔNG bị giấu
        // đi: nó được ghi vào README §Rủi ro (`AR-j`) như một xác nhận ĐỘC LẬP thứ hai.
        //
        // Test này kiểm đúng thứ nó kiểm được: cổng che ĐÃ ĐƯỢC CẮM vào đường ra mới.
        await using (var ctx = db.OpenContext(db.TenantAId))
        {
            // ⚠ `IsCurrent` là thuộc tính TÍNH TOÁN, không có cột nên EF không dịch được.
            // Lấy về rồi lọc trong bộ nhớ — vài dòng, không phải chỗ cần tối ưu.
            var assertionId = (await ctx.Assertions
                    .Where(a => a.KnowledgeRecordId == id && a.Kind == AssertionKind.Handling)
                    .ToListAsync())
                .First(a => a.IsCurrent).Id;

            var sua = await client.PostAsJsonAsync($"/knowledge/{id}/sua", new
            {
                nguoiSua = "support",
                sua = new[]
                {
                    new { assertionId, noiDungMoi = "anh cho em ultraview với, Mật khẩu: 13579 nhé ạ", mucKiemChung = "Verified" },
                },
            });
            sua.EnsureSuccessStatusCode();
        }

        await DuyetAsync(client, id);
        var token = (await PhatLinkAsync(client, id)).GetProperty("token").GetString()!;

        var body = await (await client.GetAsync($"/xem/{token}")).Content.ReadAsStringAsync();

        Assert.Contains("[ĐÃ CHE]", body);
        Assert.DoesNotContain("13579", body);

        // Phần văn xuôi còn nguyên — che GIÁ TRỊ, không bỏ cả mẩu (`AR-o`).
        Assert.Contains("ultraview", body);
    }
}
