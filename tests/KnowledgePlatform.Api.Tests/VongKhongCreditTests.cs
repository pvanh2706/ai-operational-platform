using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Domain.Sop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// **VÒNG KHÔNG-CREDIT cho định tuyến**: xuất ticket → một ngữ cảnh AI khác đọc → nạp về.
///
/// 🛑 Ba test nặng nhất:
/// <list type="bullet">
///   <item><c>Nhom_BIA_RA_bi_tu_choi</c> — `cham_routing.py` gọi đây là bẫy thứ ba, cùng loại
///   lỗi mà `G6`/`AP3` chặn ở bản nháp SOP. Một nhãn nhóm bịa sẽ được `/hoi` trả về cho
///   người làm thật, và không có gì báo.</item>
///   <item><c>Nguon_tra_ve_NOI_RO_ket_qua_den_tu_dau</c> — trộn kết quả người nạp với kết quả
///   đã đo mà không phân biệt được là cách nhanh nhất để về sau không ai chấm lại được gì.</item>
///   <item><c>Ket_qua_nap_cua_khach_A_khong_anh_huong_khach_B</c>.</item>
/// </list>
/// </summary>
public sealed class VongKhongCreditTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private readonly List<string> _fileTam = [];

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private static readonly HashSet<string> TenNhomHopLe =
        new(StringComparer.Ordinal) { "Nhóm một", "Nhóm hai" };

    private string BangTraSanRong()
    {
        var duong = Path.Combine(Path.GetTempPath(), $"dt-tra-san-{Guid.CreateVersion7():N}.json");
        File.WriteAllText(duong, """{"ketQua":{}}""");
        _fileTam.Add(duong);
        return duong;
    }

    private ApiFactory App(string tenantKey) =>
        ApiFactory.Dedicated(tenantKey).With("DinhTuyen:DuongDanBangTraSan", BangTraSanRong());

    private static async Task<string> TaoCaseAsync(HttpClient c, string? tieuDe = null)
    {
        var khoa = $"jira:VKC-{Guid.CreateVersion7():N}";
        var res = await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = khoa, subject = tieuDe ?? "Không phát hành được hoá đơn" } });
        res.EnsureSuccessStatusCode();
        return khoa;
    }

    /// <summary>Chạy VongKhongCredit dưới tenant chỉ định — lệnh CLI làm y hệt.</summary>
    private static async Task<T> TrongScopeAsync<T>(ApiFactory app, Guid tenantId, Func<VongKhongCredit, Task<T>> viec)
    {
        await using var scope = app.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(tenantId);
        return await viec(scope.ServiceProvider.GetRequiredService<VongKhongCredit>());
    }

    private static KetQuaDinhTuyenNap Dong(string khoa, string nhom) =>
        new(khoa, nhom, "noi-ro-trong-mo-ta", "vì tiêu đề nói rõ");

    // =========================================================================

    [Fact]
    public async Task Nap_xong_thi_hoi_ra_dung_nhom_da_nap()
    {
        using var app = App(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = await TaoCaseAsync(c);

        var kq = await TrongScopeAsync(app, db.TenantAId, v =>
            v.NapAsync([Dong(khoa, "Nhóm một")], TenNhomHopLe, "test"));

        Assert.Equal(1, kq.DaNap);
        Assert.Empty(kq.BoQua);

        var hoi = await (await c.PostAsJsonAsync("/hoi", new { maTicket = khoa, tieuDe = "x", moTa = (string?)null }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Nhóm một", hoi.GetProperty("nhom").GetString());
    }

    /// <summary>
    /// 🛑 Nguồn phải NÓI RÕ kết quả đến từ đâu. Bảng `dinh-tuyen-150-case.json` là một PHÉP
    /// ĐO, kho nạp tay là thao tác của người — trộn hai thứ vào một câu trả lời không phân
    /// biệt được là đúng chỗ mà `R-K4` đã phải hạ cấp một kết luận vì nó viết ở sai mức.
    /// </summary>
    [Fact]
    public async Task Nguon_tra_ve_NOI_RO_ket_qua_den_tu_dau()
    {
        using var app = App(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = await TaoCaseAsync(c);

        await TrongScopeAsync(app, db.TenantAId, v =>
            v.NapAsync([Dong(khoa, "Nhóm hai")], TenNhomHopLe, "test"));

        var daNap = await (await c.PostAsJsonAsync("/hoi", new { maTicket = khoa, tieuDe = "x", moTa = (string?)null }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith(DinhTuyenKho.TienToNguoiNap, daNap.GetProperty("nguonDinhTuyen").GetString());

        var chuaNap = await (await c.PostAsJsonAsync("/hoi",
                new { maTicket = "jira:CHUA-BAO-GIO", tieuDe = "x", moTa = (string?)null }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith(DinhTuyenKho.TienToTraSan, chuaNap.GetProperty("nguonDinhTuyen").GetString());
        Assert.Equal("dinh-tuyen-khong-biet", chuaNap.GetProperty("lyDoKhongBiet").GetString());
    }

    /// <summary>
    /// 🛑 Tên nhóm bịa ra bị TỪ CHỐI, và KHÔNG được "sửa cho giống". Sửa hộ là biến một lỗi
    /// nhìn thấy được thành một lỗi im lặng.
    /// </summary>
    [Fact]
    public async Task Nhom_BIA_RA_bi_tu_choi()
    {
        using var app = App(db.TenantAKey);
        var khoa = await TaoCaseAsync(app.CreateClient());

        var kq = await TrongScopeAsync(app, db.TenantAId, v =>
            v.NapAsync([Dong(khoa, "Nhóm mà tôi tự nghĩ ra")], TenNhomHopLe, "test"));

        Assert.Equal(0, kq.DaNap);
        Assert.Single(kq.BoQua);
        Assert.Contains("KHÔNG có trong taxonomy", kq.BoQua[0]);

        await using var ctx = db.OpenContext(db.TenantAId);
        Assert.False(await ctx.Set<DinhTuyenDaNap>().AnyAsync(x => x.KhoaNguon == khoa));
    }

    [Fact]
    public async Task Khoa_nguon_khong_co_case_nao_thi_bi_tu_choi()
    {
        using var app = App(db.TenantAKey);

        var kq = await TrongScopeAsync(app, db.TenantAId, v =>
            v.NapAsync([Dong("jira:KHONG-CO-THAT", "Nhóm một")], TenNhomHopLe, "test"));

        Assert.Equal(0, kq.DaNap);
        Assert.Contains("không có case nào", kq.BoQua[0]);
    }

    /// <summary>
    /// `KHONG-BIET` là một câu trả lời HỢP LỆ và được mong đợi — `docs/13` đo được 62/150
    /// ticket rơi vào đó. Từ chối nó là ép người nạp phải đoán bừa.
    /// </summary>
    [Fact]
    public async Task KHONG_BIET_nap_duoc_va_tra_ve_khong_biet()
    {
        using var app = App(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = await TaoCaseAsync(c);

        var kq = await TrongScopeAsync(app, db.TenantAId, v => v.NapAsync(
            [new KetQuaDinhTuyenNap(khoa, KetQuaDinhTuyen.KhongBiet, "khong-du-thong-tin", "chỉ có tiêu đề")],
            TenNhomHopLe, "test"));

        Assert.Equal(1, kq.DaNap);

        var hoi = await (await c.PostAsJsonAsync("/hoi", new { maTicket = khoa, tieuDe = "x", moTa = (string?)null }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(hoi.GetProperty("bietTraLoi").GetBoolean());
        Assert.StartsWith(DinhTuyenKho.TienToNguoiNap, hoi.GetProperty("nguonDinhTuyen").GetString());
    }

    /// <summary>
    /// Nạp lại thì ĐÈ — người nạp lại là người đang sửa một kết quả họ thấy sai. Khác hẳn
    /// `K-B3` (không ghi đè quá khứ của TRI THỨC đã duyệt): đây là bộ nhớ thao tác.
    /// </summary>
    [Fact]
    public async Task Nap_lai_thi_DE_len_ket_qua_cu()
    {
        using var app = App(db.TenantAKey);
        var khoa = await TaoCaseAsync(app.CreateClient());

        await TrongScopeAsync(app, db.TenantAId, v => v.NapAsync([Dong(khoa, "Nhóm một")], TenNhomHopLe, "lần 1"));
        var lai = await TrongScopeAsync(app, db.TenantAId, v =>
            v.NapAsync([Dong(khoa, "Nhóm hai")], TenNhomHopLe, "lần 2"));

        Assert.Equal(0, lai.DaNap);
        Assert.Equal(1, lai.DaDe);

        await using var ctx = db.OpenContext(db.TenantAId);
        var dong = await ctx.Set<DinhTuyenDaNap>().SingleAsync(x => x.KhoaNguon == khoa);
        Assert.Equal("Nhóm hai", dong.Nhom);
    }

    [Fact]
    public async Task Chi_xuat_case_CHUA_co_ket_qua()
    {
        using var app = App(db.TenantAKey);
        var c = app.CreateClient();
        var daNap = await TaoCaseAsync(c);
        var chuaNap = await TaoCaseAsync(c);

        await TrongScopeAsync(app, db.TenantAId, v => v.NapAsync([Dong(daNap, "Nhóm một")], TenNhomHopLe, "test"));

        var ra = await TrongScopeAsync(app, db.TenantAId, v => v.ChonTicketAsync(500));
        var khoa = ra.Select(x => x.KhoaNguon).ToList();

        Assert.DoesNotContain(daNap, khoa);
        Assert.Contains(chuaNap, khoa);
    }

    /// <summary>
    /// 🛑 Gói xuất ra RỜI KHỎI MÁY (người ta dán nó vào một cửa sổ chat), nên nó phải đi qua
    /// cổng che `AR-o` — cùng chốt mà `SoanNhapRunner --xuat-payload` đã có.
    /// </summary>
    [Fact]
    public async Task Noi_dung_xuat_ra_da_qua_cong_che()
    {
        using var app = App(db.TenantAKey);
        var c = app.CreateClient();

        // Vật liệu lấy từ luật đã đo, không từ trí nhớ — xem ghi chú cùng ý ở LinkXemTests.
        var khoa = await TaoCaseAsync(c, "anh cho em ultraview với, Mật khẩu: 13579 nhé ạ");

        var ra = await TrongScopeAsync(app, db.TenantAId, v => v.ChonTicketAsync(500));
        var cua = ra.Single(x => x.KhoaNguon == khoa);

        Assert.DoesNotContain("13579", cua.TieuDe);
        Assert.Contains("[ĐÃ CHE]", cua.TieuDe);
        Assert.Contains("ultraview", cua.TieuDe);   // che GIÁ TRỊ, không bỏ cả mẩu
    }

    [Fact]
    public async Task Ket_qua_nap_cua_khach_A_khong_anh_huong_khach_B()
    {
        using var appA = App(db.TenantAKey);
        using var appB = App(db.TenantBKey);

        var khoa = await TaoCaseAsync(appA.CreateClient());
        await TrongScopeAsync(appA, db.TenantAId, v => v.NapAsync([Dong(khoa, "Nhóm một")], TenNhomHopLe, "A"));

        // B hỏi đúng mã ticket đó. RLS chặn dòng của A -> B rơi về bảng tra sẵn (rỗng).
        var hoi = await (await appB.CreateClient().PostAsJsonAsync("/hoi",
                new { maTicket = khoa, tieuDe = "x", moTa = (string?)null }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(hoi.GetProperty("bietTraLoi").GetBoolean());
        Assert.StartsWith(DinhTuyenKho.TienToTraSan, hoi.GetProperty("nguonDinhTuyen").GetString());
    }
}
