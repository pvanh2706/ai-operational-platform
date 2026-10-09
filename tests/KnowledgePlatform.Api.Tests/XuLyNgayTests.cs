using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Api.Webhooks;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// P7 (2026-10-10) — *"cứ nhận dữ liệu về là cho vào luồng để xử lý luôn"*: mẩu vừa nhận làm case BƯỚC SANG
/// bước khác thì tự xếp một lượt trả kết quả về, mang tri thức của bước MỚI.
///
/// 🛑 Bốn chỗ cố ý KHÔNG xếp, và mỗi chỗ có test — vì "xếp thêm cho chắc" là cách khách nhận năm thông báo cho
/// một việc: không đổi bước · gửi lại cùng mẩu · case đã xong ở nguồn · đang có lượt chờ (lượt đó tự mang bước
/// mới nhất vì gói được dựng LÚC GỬI).
/// </summary>
public sealed class XuLyNgayTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private const string GocLink = "http://kp-noi-bo.thu-nghiem:5119";
    private const string DichThu = "http://ben-nhan.thu-nghiem/webhook";

    private readonly List<string> _fileTam = [];
    private readonly BenNhanGia _benNhan = new();

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private sealed record BoiCanh(ApiFactory App, HttpClient C, Guid TenantId, string KhoaCase);

    /// <summary>
    /// Khách riêng, đích webhook đã khai, case vào hàng đợi, tri thức + quy trình (K1, K2, XL — có dấu hiệu) đã
    /// duyệt. <paramref name="guiLuotDau"/>: gửi luôn lượt "case mới" — lượt đó dựng lượt chạy quy trình.
    /// </summary>
    private async Task<BoiCanh> MoiAsync(bool guiLuotDau = true)
    {
        var (key, tenantId) = await db.TaoTenantMoiAsync();
        var id = Guid.CreateVersion7().ToString("N");
        var nhom = $"nhom-xln-{id}";
        var maTran = $"XN-{id}";

        var bang = Path.Combine(Path.GetTempPath(), $"dt-xln-{id}.json");
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

        (await c.PutAsJsonAsync("/internal/webhook", new { url = DichThu })).EnsureSuccessStatusCode();

        var khoaCase = $"jira:{maTran}";
        (await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = khoaCase, subject = "Không phát hành được hoá đơn" } })).EnsureSuccessStatusCode();

        var nap = await (await c.PostAsJsonAsync("/knowledge/sop-draft", Nhap(nhom, maTran))).Content.ReadFromJsonAsync<JsonElement>();
        foreach (var r in nap.GetProperty("nguyenNhan").EnumerateArray())
        {
            (await c.PostAsJsonAsync($"/knowledge/{r.GetProperty("id").GetGuid()}/approve",
                new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true })).EnsureSuccessStatusCode();
        }

        var pid = nap.GetProperty("quyTrinh").GetProperty("id").GetGuid();
        (await c.PutAsJsonAsync($"/processes/{pid}/buoc", new
        {
            nguoiSua = "chị Lan",
            buoc = new object[]
            {
                new { ma = "K1", noiDung = "Xem nhật ký kết nối", dauHieu = new[] { "nhật ký kết nối" } },
                new { ma = "K2", noiDung = "Kiểm phân quyền", dauHieu = new[] { "phân quyền" } },
                new { ma = "XL", noiDung = "Xử lý" },
            },
        })).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync($"/processes/{pid}/approve",
            new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true })).EnsureSuccessStatusCode();

        var b = new BoiCanh(app, c, tenantId, khoaCase);
        if (guiLuotDau) await GuiMotVongAsync(b);
        return b;
    }

    private static SopDraft Nhap(string nhom, string ma) => new()
    {
        Nhom = nhom,
        SoCaseDungSau = 1,
        TrieuChungVao = [new SopSymptom("x", [ma])],
        BuocKiem =
        [
            new SopCheckStep { Ma = "K1", CauHoi = "Xem nhật ký kết nối", NoiXem = "Nhật ký", Nhanh = [new SopBranch("Có E0042", "S1", [ma], "evidence-noi-ro")] },
            new SopCheckStep { Ma = "K2", CauHoi = "Kiểm phân quyền", NoiXem = "Phân quyền", Nhanh = [new SopBranch("Thiếu quyền", "S2", [ma], "evidence-noi-ro")] },
        ],
        BuocSua = [new SopFixStep("S1", "Sửa cấu hình kết nối", [ma]), new SopFixStep("S2", "Cấp quyền ký hiệu", [ma])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(1, [ma]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    private async Task GuiMotVongAsync(BoiCanh b)
    {
        await using var scope = b.App.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(b.TenantId);
        await scope.ServiceProvider.GetRequiredService<WebhookSender>().GuiDenHanAsync();
    }

    private static Task NapMauAsync(BoiCanh b, string khoaMau, string noiDung) =>
        b.C.PostAsJsonAsync("/signals/case-evidence", new[]
        {
            new { caseSourceReference = b.KhoaCase, sourceReference = khoaMau, content = noiDung,
                  machineReadability = "High", subTenantCode = "khong-xac-dinh" },
        }).ContinueWith(t => t.Result.EnsureSuccessStatusCode());

    private async Task<List<WebhookDelivery>> HangDoiAsync(BoiCanh b)
    {
        await using var ctx = db.OpenContext(b.TenantId);
        return await ctx.Set<WebhookDelivery>().Where(d => d.CaseSourceReference == b.KhoaCase).ToListAsync();
    }

    // =========================================================================

    [Fact]
    public async Task Mau_moi_lam_DOI_BUOC_thi_xep_luot_gui_va_goi_mang_buoc_MOI()
    {
        var b = await MoiAsync();
        using var app = b.App;

        await NapMauAsync(b, $"{b.KhoaCase}#comment-1", "Da xem nhat ky ket noi, thay E0042");

        var hang = await HangDoiAsync(b);
        var moi = Assert.Single(hang, d => d.State == WebhookDeliveryState.Pending);
        Assert.Equal("buoc-hien-tai-doi", moi.SuKien);

        await GuiMotVongAsync(b);

        var goi = JsonSerializer.Deserialize<JsonElement>(_benNhan.DaNhan.Last().Body);
        Assert.Equal("buoc-hien-tai-doi", goi.GetProperty("suKien").GetString());
        Assert.Equal("K2", goi.GetProperty("quyTrinh").GetProperty("buocHienTai").GetProperty("ma").GetString());
    }

    [Fact]
    public async Task Mau_moi_KHONG_doi_buoc_thi_KHONG_xep()
    {
        var b = await MoiAsync();
        using var app = b.App;

        await NapMauAsync(b, $"{b.KhoaCase}#comment-1", "Khach goi lai hoi tien do");

        Assert.DoesNotContain(await HangDoiAsync(b), d => d.State == WebhookDeliveryState.Pending);
    }

    /// <summary>Bên gửi CÓ gửi lại — nạp lại cả lô không được đẻ thông báo thứ hai cho cùng một lần đổi bước.</summary>
    [Fact]
    public async Task Gui_lai_cung_mau_KHONG_xep_them()
    {
        var b = await MoiAsync();
        using var app = b.App;

        await NapMauAsync(b, $"{b.KhoaCase}#comment-1", "Da xem nhat ky ket noi");
        await GuiMotVongAsync(b);
        await NapMauAsync(b, $"{b.KhoaCase}#comment-1", "Da xem nhat ky ket noi");

        var hang = await HangDoiAsync(b);
        Assert.Equal(1, hang.Count(d => d.SuKien == "buoc-hien-tai-doi"));
        Assert.DoesNotContain(hang, d => d.State == WebhookDeliveryState.Pending);
    }

    [Fact]
    public async Task Loi_xac_nhan_buoc_CUNG_kich_hoat()
    {
        var b = await MoiAsync();
        using var app = b.App;

        (await b.C.PostAsJsonAsync("/quy-trinh/xac-nhan-buoc",
            new { khoaNguon = b.KhoaCase, maBuoc = "K1", nguoiXacNhan = "chị Lan", subTenantCode = "khong-xac-dinh" }))
            .EnsureSuccessStatusCode();

        Assert.Single(await HangDoiAsync(b), d => d.State == WebhookDeliveryState.Pending && d.SuKien == "buoc-hien-tai-doi");
    }

    /// <summary>Cùng tinh thần `F1-1`: "bước tiếp theo" cho một việc đã đóng ở nguồn là nhiễu.</summary>
    [Fact]
    public async Task Case_da_XONG_O_NGUON_thi_KHONG_xep()
    {
        var b = await MoiAsync();
        using var app = b.App;

        (await b.C.PostAsJsonAsync("/signals/case-status",
            new[] { new { sourceReference = b.KhoaCase, daXongONguon = true } })).EnsureSuccessStatusCode();
        await NapMauAsync(b, $"{b.KhoaCase}#comment-1", "Da xem nhat ky ket noi");

        Assert.DoesNotContain(await HangDoiAsync(b), d => d.State == WebhookDeliveryState.Pending);
    }

    /// <summary>Lượt đang chờ dựng gói LÚC GỬI, nên nó tự mang bước mới nhất — xếp thêm là gửi hai lần cùng một tin.</summary>
    [Fact]
    public async Task Dang_co_luot_CHO_thi_KHONG_xep_chong()
    {
        var b = await MoiAsync(guiLuotDau: false);
        using var app = b.App;

        // Lượt "case mới" còn chờ; dựng lượt chạy qua /hoi.
        (await b.C.PostAsJsonAsync("/hoi", new { maTicket = b.KhoaCase })).EnsureSuccessStatusCode();
        await NapMauAsync(b, $"{b.KhoaCase}#comment-1", "Da xem nhat ky ket noi");

        var hang = await HangDoiAsync(b);
        Assert.Single(hang);
        Assert.Null(hang[0].SuKien);
    }
}
