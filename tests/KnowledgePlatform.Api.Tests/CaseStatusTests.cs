using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Signals;
using KnowledgePlatform.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Tín hiệu loại THỨ HAI của Kênh 1: việc ở nguồn đổi trạng thái (`F1-1`).
///
/// 🛑 Hai test nặng nhất:
/// <list type="bullet">
///   <item><c>Doi_trang_thai_KHONG_kich_hoat_gui_ket_qua_ve</c> — `F1-1` chọn nhánh này trên
///   một SỐ ĐO, và nó ngược với trực giác. Không có test này thì ai đó sẽ "sửa cho nhất
///   quán" với đường tín hiệu loại một.</item>
///   <item><c>Thieu_daXongONguon_thi_400</c> — bỏ trống làm *"chưa gửi"* và *"đã mở lại"*
///   trông giống hệt nhau, và về sau không dựng lại được.</item>
/// </list>
/// </summary>
public sealed class CaseStatusTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private const string DuongStatus = "/signals/case-status";
    private const string DuongObserved = "/signals/case-observed";

    private static string Moi() => $"jira:ST-{Guid.CreateVersion7():N}";

    private static async Task TaoCaseAsync(HttpClient c, string khoa)
    {
        var res = await c.PostAsJsonAsync(DuongObserved,
            new[] { new { sourceReference = khoa, subject = "Không phát hành được hoá đơn" } });
        res.EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> DoiAsync(
        HttpClient c, string khoa, bool? daXong, DateTimeOffset? luc = null)
    {
        var res = await c.PostAsJsonAsync(DuongStatus,
            new[] { new { sourceReference = khoa, daXongONguon = daXong, sourceResolvedAt = luc } });
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string TrangThai(JsonElement kq) =>
        kq.GetProperty("results").EnumerateArray().First().GetProperty("trangThai").GetString()!;

    // =========================================================================

    [Fact]
    public async Task Bao_da_xong_thi_ghi_moc_hoan_thanh()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = Moi();

        await TaoCaseAsync(c, khoa);

        var moc = new DateTimeOffset(2026, 9, 10, 8, 30, 0, TimeSpan.FromHours(7));
        var kq = await DoiAsync(c, khoa, daXong: true, luc: moc);

        Assert.Equal(CaseStatusOutcome.DaCapNhat, TrangThai(kq));
        Assert.Equal(1, kq.GetProperty("daCapNhat").GetInt32());

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.Cases.FirstAsync(x => x.SourceReference == khoa);

        // ⚠ So THỜI ĐIỂM, không so offset: cột timestamptz không lưu múi giờ, và mốc gửi vào
        //   mang +07:00 — đúng ca đã làm endpoint nạp trả 500 ở `IM-24`.
        Assert.Equal(moc.ToUniversalTime(), luu.SourceResolvedAt!.Value.ToUniversalTime());
    }

    [Fact]
    public async Task Bao_mo_lai_thi_XOA_moc_hoan_thanh()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = Moi();

        await TaoCaseAsync(c, khoa);
        await DoiAsync(c, khoa, daXong: true);

        Assert.Equal(CaseStatusOutcome.DaCapNhat, TrangThai(await DoiAsync(c, khoa, daXong: false)));

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.Cases.FirstAsync(x => x.SourceReference == khoa);
        Assert.Null(luu.SourceResolvedAt);
    }

    /// <summary>
    /// Gửi lại đúng trạng thái đang có KHÔNG phải lỗi — tín hiệu đến hai lần là chuyện
    /// thường của mọi hệ thống tích hợp (cùng lý do `IM-20`).
    /// </summary>
    [Fact]
    public async Task Gui_lai_dung_trang_thai_dang_co_thi_khong_doi()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = Moi();

        await TaoCaseAsync(c, khoa);
        await DoiAsync(c, khoa, daXong: true);

        var lai = await DoiAsync(c, khoa, daXong: true, luc: DateTimeOffset.UtcNow.AddMinutes(5));

        Assert.Equal(CaseStatusOutcome.KhongDoi, TrangThai(lai));
        Assert.Equal(0, lai.GetProperty("daCapNhat").GetInt32());
    }

    /// <summary>
    /// Case chưa từng được tạo thì NÓI RA, không tự tạo. Một case dựng từ tín hiệu này sẽ
    /// không có `Subject` — tức một dòng rỗng nghĩa nằm trong kho gom của Path A.
    /// </summary>
    [Fact]
    public async Task Case_chua_ton_tai_thi_noi_ra_chu_KHONG_tu_tao()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = Moi();

        var kq = await DoiAsync(c, khoa, daXong: true);

        Assert.Equal(CaseStatusOutcome.KhongThayCase, TrangThai(kq));
        Assert.Equal(1, kq.GetProperty("khongThayCase").GetInt32());

        await using var ctx = db.OpenContext(db.TenantAId);
        Assert.False(await ctx.Cases.AnyAsync(x => x.SourceReference == khoa));
    }

    [Fact]
    public async Task Thieu_daXongONguon_thi_400()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = Moi();
        await TaoCaseAsync(c, khoa);

        var res = await c.PostAsJsonAsync(DuongStatus, new[] { new { sourceReference = khoa } });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("daXongONguon", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Lo_rong_thi_400()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);

        var res = await app.CreateClient().PostAsJsonAsync(DuongStatus, Array.Empty<object>());

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>
    /// 🛑 **Test giữ chính QUYẾT ĐỊNH `F1-1`, không giữ một dòng code.**
    ///
    /// Trực giác nói: case đóng rồi thì gom tri thức từ nó. Đo trên corpus 12 tháng thì
    /// ngược lại — 100% case đã đóng mà chỉ **1,6 mẩu dùng được mỗi case**, mỏng hơn corpus
    /// 4 ngày (**2,5**) vốn chỉ 22% đã đóng. Bước kết luận thường xảy ra ở nơi khác (phiên
    /// remote, cuộc gọi) và chỉ để lại trong ticket một dòng *"Done nhé"*.
    ///
    /// Không có test này thì ai đó sẽ "sửa cho nhất quán" với đường tín hiệu loại một — và
    /// đó là lần thứ ba cùng một hình dạng lỗi, sau *"chỉ lấy case đã đóng"* và *"chỉ lấy
    /// phần khách đọc được"*.
    /// </summary>
    [Fact]
    public async Task Doi_trang_thai_KHONG_kich_hoat_gui_ket_qua_ve()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = Moi();

        // Có khai báo đích gửi — nên nếu đường này xếp hàng, hàng đợi sẽ có dòng.
        (await c.PutAsJsonAsync("/internal/webhook",
            new { url = "http://ben-nhan.thu-nghiem/f1" })).EnsureSuccessStatusCode();

        await TaoCaseAsync(c, khoa);

        await using var ctx = db.OpenContext(db.TenantAId);
        var truoc = await ctx.Set<WebhookDelivery>().CountAsync(d => d.CaseSourceReference == khoa);

        await DoiAsync(c, khoa, daXong: true);
        await DoiAsync(c, khoa, daXong: false);

        var sau = await ctx.Set<WebhookDelivery>().CountAsync(d => d.CaseSourceReference == khoa);

        Assert.Equal(truoc, sau);
    }

    /// <summary>Khoá của khách A không đổi được trạng thái case của khách B.</summary>
    [Fact]
    public async Task Khach_khac_khong_doi_duoc_trang_thai_case_cua_minh()
    {
        using var appA = ApiFactory.Dedicated(db.TenantAKey);
        using var appB = ApiFactory.Dedicated(db.TenantBKey);
        var khoa = Moi();

        await TaoCaseAsync(appA.CreateClient(), khoa);

        // B gửi đúng khoá nguồn đó. RLS làm nó không thấy case -> khong-thay-case, giống hệt
        // ca khoá chưa từng tồn tại. Phân biệt hai ca đó là dò được case của khách khác.
        var kq = await DoiAsync(appB.CreateClient(), khoa, daXong: true);

        Assert.Equal(CaseStatusOutcome.KhongThayCase, TrangThai(kq));

        await using var ctx = db.OpenContext(db.TenantAId);
        var cuaA = await ctx.Cases.FirstAsync(x => x.SourceReference == khoa);
        Assert.Null(cuaA.SourceResolvedAt);
    }
}
