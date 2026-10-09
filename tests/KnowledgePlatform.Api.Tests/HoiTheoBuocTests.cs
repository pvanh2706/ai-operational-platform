using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Api.Webhooks;
using KnowledgePlatform.Domain.Sop;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Ô `G` + `I` của sơ đồ (P5, 2026-10-10): tri thức tra theo BƯỚC HIỆN TẠI của case, kèm dẫn chứng trỏ về nguồn.
///
/// 🛑 Hình dạng thật của SOP (`docs/11`): mỗi bước kiểm dẫn tới một nhóm nguyên nhân KHÁC. Đang ở bước K1 mà
/// trả cả nguyên nhân của K2 là đưa người làm support tới đúng chỗ họ chưa kiểm. Test giữ ba điều:
/// <list type="bullet">
///   <item>câu trả lời ĐỔI THEO BƯỚC — xác nhận K1 thì sang tri thức của K2;</item>
///   <item>xong hết bước thì RƠI VỀ mức nhóm, không trả rỗng;</item>
///   <item>chưa có quy trình được duyệt thì VẪN trả theo nhóm như trước P5, và nói rõ vì sao.</item>
/// </list>
/// </summary>
public sealed class HoiTheoBuocTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private const string GocLink = "http://kp-noi-bo.thu-nghiem:5119";
    private const string DichThu = "http://ben-nhan.thu-nghiem/webhook";

    private readonly List<string> _fileTam = [];
    private readonly BenNhanGia _benNhan = new();

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private sealed record BoiCanh(ApiFactory App, HttpClient C, Guid TenantId, string Nhom, string MaTran, string KhoaCase);

    private async Task<BoiCanh> MoiAsync(bool coWebhook = false)
    {
        var (key, tenantId) = await db.TaoTenantMoiAsync();
        var id = Guid.CreateVersion7().ToString("N");
        var nhom = $"nhom-hb-{id}";
        var maTran = $"HB-{id}";

        var bang = Path.Combine(Path.GetTempPath(), $"dt-hb-{id}.json");
        File.WriteAllText(bang, JsonSerializer.Serialize(new
        {
            ketQua = new Dictionary<string, object> { [maTran] = new { nhom, nguon = "noi-ro-trong-mo-ta", vietTat = "t" } },
        }));
        _fileTam.Add(bang);

        var app = ApiFactory.Dedicated(key)
            .With("DinhTuyen:DuongDanBangTraSan", bang)
            .With("Link:PublicBaseUrl", GocLink)
            .With("Webhook:BatVongQuet", "false")
            .WithServices(s => s.AddHttpClient(WebhookSender.TenHttpClient).ConfigurePrimaryHttpMessageHandler(() => _benNhan));
        var c = app.CreateClient();

        if (coWebhook) (await c.PutAsJsonAsync("/internal/webhook", new { url = DichThu })).EnsureSuccessStatusCode();

        var khoaCase = $"jira:{maTran}";
        (await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = khoaCase, subject = "Không phát hành được hoá đơn" } })).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync("/signals/case-evidence", new[]
        {
            new { caseSourceReference = khoaCase, sourceReference = $"{khoaCase}#description",
                  content = "Khach bao loi khong phat hanh duoc", machineReadability = "High", subTenantCode = "khong-xac-dinh" },
        })).EnsureSuccessStatusCode();

        return new BoiCanh(app, c, tenantId, nhom, maTran, khoaCase);
    }

    /// <summary>Hai bước kiểm, mỗi bước dẫn tới MỘT nguyên nhân riêng; mọi chỗ trích đúng ticket của case.</summary>
    private static SopDraft Nhap(string nhom, string ma) => new()
    {
        Nhom = nhom,
        SoCaseDungSau = 1,
        TrieuChungVao = [new SopSymptom("Không phát hành được", [ma])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1", CauHoi = "Xem nhật ký kết nối", NoiXem = "Nhật ký",
                Nhanh = [new SopBranch("Có mã E0042", "S1", [ma], "evidence-noi-ro")],
            },
            new SopCheckStep
            {
                Ma = "K2", CauHoi = "Kiểm phân quyền ký hiệu", NoiXem = "Phân quyền",
                Nhanh = [new SopBranch("Thiếu quyền ký hiệu", "S2", [ma], "evidence-noi-ro")],
            },
        ],
        BuocSua = [new SopFixStep("S1", "Sửa cấu hình kết nối", [ma]), new SopFixStep("S2", "Cấp quyền ký hiệu", [ma])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(1, [ma]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res)
    {
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>Nạp bản nháp → duyệt cả hai nguyên nhân → (tuỳ) thêm dấu hiệu + duyệt quy trình.</summary>
    private static async Task DungTriThucAsync(BoiCanh b, bool duyetQuyTrinh)
    {
        var nap = await JsonAsync(await b.C.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom, b.MaTran)));

        foreach (var r in nap.GetProperty("nguyenNhan").EnumerateArray())
        {
            (await b.C.PostAsJsonAsync($"/knowledge/{r.GetProperty("id").GetGuid()}/approve",
                new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true })).EnsureSuccessStatusCode();
        }

        if (!duyetQuyTrinh) return;

        var pid = nap.GetProperty("quyTrinh").GetProperty("id").GetGuid();
        (await b.C.PostAsJsonAsync($"/processes/{pid}/approve",
            new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true })).EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> HoiAsync(BoiCanh b) =>
        await JsonAsync(await b.C.PostAsJsonAsync("/hoi", new { maTicket = b.MaTran }));

    private static IEnumerable<string> CachXuLy(JsonElement traLoi) =>
        traLoi.GetProperty("huongDan").EnumerateArray()
            .SelectMany(h => h.GetProperty("cachXuLy").EnumerateArray())
            .Select(p => p.GetProperty("noiDung").GetString()!);

    private static Task XacNhanAsync(BoiCanh b, string ma) =>
        b.C.PostAsJsonAsync("/quy-trinh/xac-nhan-buoc",
            new { khoaNguon = b.KhoaCase, maBuoc = ma, nguoiXacNhan = "chị Lan", subTenantCode = "khong-xac-dinh" })
            .ContinueWith(t => t.Result.EnsureSuccessStatusCode());

    // =========================================================================

    [Fact]
    public async Task Cau_tra_loi_DOI_THEO_BUOC_va_xong_het_thi_roi_ve_NHOM()
    {
        var b = await MoiAsync();
        using var app = b.App;
        await DungTriThucAsync(b, duyetQuyTrinh: true);

        // Đang ở K1 → CHỈ nguyên nhân K1 nhận ra được.
        var k1 = await HoiAsync(b);
        Assert.True(k1.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal("K1", k1.GetProperty("quyTrinh").GetProperty("buocHienTai").GetProperty("ma").GetString());
        Assert.Equal("buoc", k1.GetProperty("quyTrinh").GetProperty("traTheo").GetString());
        Assert.Equal(["Sửa cấu hình kết nối"], CachXuLy(k1));

        await XacNhanAsync(b, "K1");

        var k2 = await HoiAsync(b);
        Assert.Equal("K2", k2.GetProperty("quyTrinh").GetProperty("buocHienTai").GetProperty("ma").GetString());
        Assert.Equal(["Cấp quyền ký hiệu"], CachXuLy(k2));

        await XacNhanAsync(b, "K2");

        // Tới bước XL: chủ đề là cả nhóm → cả hai nguyên nhân.
        var xl = await HoiAsync(b);
        Assert.Equal("XL", xl.GetProperty("quyTrinh").GetProperty("buocHienTai").GetProperty("ma").GetString());
        Assert.Equal("nhom", xl.GetProperty("quyTrinh").GetProperty("traTheo").GetString());
        Assert.Equal(2, CachXuLy(xl).Count());
    }

    /// <summary>Ô `I`: mỗi phát biểu trỏ về mẩu nguồn VÀ ticket chứa nó — không kèm nội dung mẩu.</summary>
    [Fact]
    public async Task Moi_phat_bieu_kem_DAN_CHUNG_tro_ve_ticket()
    {
        var b = await MoiAsync();
        using var app = b.App;
        await DungTriThucAsync(b, duyetQuyTrinh: true);

        var traLoi = await HoiAsync(b);

        var dc = traLoi.GetProperty("huongDan")[0].GetProperty("cachXuLy")[0].GetProperty("danChung");
        var mot = Assert.Single(dc.EnumerateArray());
        Assert.Equal(b.KhoaCase, mot.GetProperty("khoaNguonCase").GetString());
        Assert.Equal($"{b.KhoaCase}#description", mot.GetProperty("khoaNguonMau").GetString());
        Assert.False(mot.TryGetProperty("noiDung", out _));
    }

    /// <summary>
    /// Chưa có quy trình được duyệt: hành vi TRƯỚC P5 phải nguyên vẹn (trả theo nhóm), và nói rõ vì sao không có
    /// quy trình — chứ không trả một ô <c>quyTrinh</c> rỗng trông như "quy trình không có bước nào".
    /// </summary>
    [Fact]
    public async Task Chua_duyet_quy_trinh_thi_VAN_tra_theo_nhom_va_noi_ly_do()
    {
        var b = await MoiAsync();
        using var app = b.App;
        await DungTriThucAsync(b, duyetQuyTrinh: false);

        var traLoi = await HoiAsync(b);

        Assert.True(traLoi.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal(2, CachXuLy(traLoi).Count());
        Assert.False(traLoi.TryGetProperty("quyTrinh", out _));
        Assert.Equal("nhom-chua-co-quy-trinh-duoc-duyet", traLoi.GetProperty("lyDoKhongCoQuyTrinh").GetString());
    }

    /// <summary>`AR-m`: mẩu người gửi đã rút không được trích làm dẫn chứng.</summary>
    [Fact]
    public async Task Mau_da_rut_lai_KHONG_duoc_trich()
    {
        var b = await MoiAsync();
        using var app = b.App;
        await DungTriThucAsync(b, duyetQuyTrinh: true);

        (await b.C.PostAsJsonAsync("/evidence/rut-lai", new
        {
            khoaNguon = $"{b.KhoaCase}#description", nguoiDanhDau = "chị Lan", lyDo = "Người gửi nói gửi nhầm",
        })).EnsureSuccessStatusCode();

        var traLoi = await HoiAsync(b);

        Assert.All(traLoi.GetProperty("huongDan").EnumerateArray()
                .SelectMany(h => h.GetProperty("cachXuLy").EnumerateArray()),
            p => Assert.Empty(p.GetProperty("danChung").EnumerateArray()));
    }

    /// <summary>Đường RA thứ ba mang cùng bước hiện tại + ticket dẫn chứng — vì nó đi qua CHÍNH lõi của /hoi.</summary>
    [Fact]
    public async Task Goi_webhook_mang_BUOC_HIEN_TAI_va_ticket_dan_chung()
    {
        var b = await MoiAsync(coWebhook: true);
        using var app = b.App;
        await DungTriThucAsync(b, duyetQuyTrinh: true);

        await using (var scope = b.App.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(b.TenantId);
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<WebhookSender>().GuiDenHanAsync());
        }

        var goi = JsonSerializer.Deserialize<JsonElement>(_benNhan.DaNhan.Single().Body);

        Assert.Equal("1", goi.GetProperty("phienBan").GetString());
        Assert.Equal("K1", goi.GetProperty("quyTrinh").GetProperty("buocHienTai").GetProperty("ma").GetString());
        var huongDan = Assert.Single(goi.GetProperty("huongDan").EnumerateArray());
        Assert.Equal([b.KhoaCase], huongDan.GetProperty("ticketDanChung").EnumerateArray().Select(x => x.GetString()));
    }
}
