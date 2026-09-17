using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KnowledgePlatform.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Kênh 2 — đường nạp tài liệu (`AR5`, bước 8).
///
/// 🛑 Test nặng nhất là <c>Nap_tai_lieu_KHONG_sinh_ra_tri_thuc</c>. `S6` chốt điều đó từ
/// Workstream 04, và nó là lý do Kênh 2 được hoãn suốt: build xong đường này thì kho tri thức
/// **vẫn là 0**. Nếu ngày nào đó nạp tài liệu tự đẻ ra `KnowledgeRecord`, test ấy phải đỏ —
/// vì lúc đó cả `D4` lẫn hai cổng của `/hoi` đều bị đi vòng qua.
/// </summary>
public sealed class Kenh2Tests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private readonly List<string> _thuMucTam = [];

    public void Dispose()
    {
        foreach (var d in _thuMucTam.Where(Directory.Exists)) Directory.Delete(d, recursive: true);
    }

    private ApiFactory App(string? tenantKey = null)
    {
        var kho = Path.Combine(Path.GetTempPath(), $"kp-kho-{Guid.CreateVersion7():N}");
        _thuMucTam.Add(kho);

        return ApiFactory.Dedicated(tenantKey ?? db.TenantAKey).With("KhoFile:ThuMucGoc", kho);
    }

    private static MultipartFormDataContent Goi(
        string ten, string noiDung, string? subTenantCode = "khong-xac-dinh", string? nguoiNap = "chị Lan")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(noiDung));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        form.Add(file, "file", ten);
        if (subTenantCode is not null) form.Add(new StringContent(subTenantCode), "subTenantCode");
        if (nguoiNap is not null) form.Add(new StringContent(nguoiNap), "nguoiNap");

        return form;
    }

    // =========================================================================

    [Fact]
    public async Task Nap_duoc_roi_tai_lai_dung_noi_dung()
    {
        using var app = App();
        var c = app.CreateClient();

        var res = await c.PostAsync("/documents", Goi("huong-dan.txt", "Bước 1: mở Kibana"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();
        var id = kq.GetProperty("id").GetGuid();
        Assert.False(kq.GetProperty("daCoTuTruoc").GetBoolean());

        var tai = await c.GetAsync($"/documents/{id}/tai");
        Assert.Equal(HttpStatusCode.OK, tai.StatusCode);
        Assert.Equal("Bước 1: mở Kibana", await tai.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// 🛑 **`S6`.** Nạp tài liệu mang VẬT CHỨA vào, không mang TRI THỨC vào. Test này giữ
    /// chính quyết định đó, không giữ một dòng code.
    /// </summary>
    [Fact]
    public async Task Nap_tai_lieu_KHONG_sinh_ra_tri_thuc()
    {
        using var app = App();
        var c = app.CreateClient();

        await using var ctx = db.OpenContext(db.TenantAId);
        var truoc = await ctx.KnowledgeRecords.CountAsync();

        (await c.PostAsync("/documents", Goi("quy-trinh.txt", "B1 · B2 · B3"))).EnsureSuccessStatusCode();

        Assert.Equal(truoc, await ctx.KnowledgeRecords.CountAsync());
    }

    /// <summary>
    /// Nạp lại cùng một file KHÔNG sinh bản thứ hai — job đồng bộ chạy lại là chuyện thường,
    /// cùng lý do `IM-20` ở đường tín hiệu. So bằng SHA-256 của NỘI DUNG, không bằng tên.
    /// </summary>
    [Fact]
    public async Task Nap_lai_cung_noi_dung_thi_KHONG_sinh_ban_thu_hai()
    {
        using var app = App();
        var c = app.CreateClient();

        var lan1 = await (await c.PostAsync("/documents", Goi("a.txt", "cùng một nội dung")))
            .Content.ReadFromJsonAsync<JsonElement>();

        // Tên KHÁC, nội dung GIỐNG -> vẫn là một.
        var lan2 = await (await c.PostAsync("/documents", Goi("b.txt", "cùng một nội dung")))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(lan2.GetProperty("daCoTuTruoc").GetBoolean());
        Assert.Equal(lan1.GetProperty("id").GetGuid(), lan2.GetProperty("trungVoi").GetGuid());
    }

    [Theory]
    [InlineData(null, "chị Lan", "subTenantCode")]
    [InlineData("khong-xac-dinh", null, "nguoiNap")]
    public async Task Thieu_truong_bat_buoc_thi_400(string? sub, string? nguoi, string? nhacTen)
    {
        using var app = App();

        var res = await app.CreateClient().PostAsync("/documents", Goi("x.txt", "nội dung", sub, nguoi));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains(nhacTen, await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task File_rong_thi_400()
    {
        using var app = App();

        var res = await app.CreateClient().PostAsync("/documents", Goi("rong.txt", ""));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Vuot_tran_kich_thuoc_thi_400()
    {
        using var app = App().With("KhoFile:TranMB", "1");

        var res = await app.CreateClient().PostAsync("/documents",
            Goi("to.txt", new string('x', 2 * 1024 * 1024)));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("vượt trần", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Khong_gui_form_thi_400_va_noi_ro_phai_gui_gi()
    {
        using var app = App();

        var res = await app.CreateClient().PostAsJsonAsync("/documents", new { ten = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("multipart/form-data", await res.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// 🛑 Khách A KHÔNG tải được tài liệu của khách B. RLS che phần siêu dữ liệu; phần NỘI
    /// DUNG nằm ở kho file và không có RLS nào — nên đây là chỗ duy nhất kiểm được rằng tầng
    /// kho tự đứng vững (thư mục theo tenant + mã kho do sản phẩm sinh).
    /// </summary>
    [Fact]
    public async Task Khach_A_khong_tai_duoc_tai_lieu_cua_khach_B()
    {
        // CÙNG một thư mục kho cho cả hai khách — ca xấu nhất, không phải ca dễ.
        var kho = Path.Combine(Path.GetTempPath(), $"kp-kho-chung-{Guid.CreateVersion7():N}");
        _thuMucTam.Add(kho);

        using var appA = ApiFactory.Dedicated(db.TenantAKey).With("KhoFile:ThuMucGoc", kho);
        using var appB = ApiFactory.Dedicated(db.TenantBKey).With("KhoFile:ThuMucGoc", kho);

        var cuaB = await (await appB.CreateClient().PostAsync("/documents",
                Goi("cua-b.txt", $"bí mật của khách B {Guid.CreateVersion7()}")))
            .Content.ReadFromJsonAsync<JsonElement>();

        var id = cuaB.GetProperty("id").GetGuid();

        var res = await appA.CreateClient().GetAsync($"/documents/{id}/tai");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Liet_ke_chi_thay_tai_lieu_cua_chinh_minh()
    {
        using var app = App();
        var c = app.CreateClient();

        var ten = $"rieng-{Guid.CreateVersion7():N}.txt";
        (await c.PostAsync("/documents", Goi(ten, $"nội dung {Guid.CreateVersion7()}")))
            .EnsureSuccessStatusCode();

        var ds = await c.GetFromJsonAsync<List<TomTatTaiLieu>>("/documents");

        Assert.NotNull(ds);
        Assert.Contains(ds!, d => d.TenKhaiBao == ten);
    }

    /// <summary>
    /// Mã kho do SẢN PHẨM sinh, không lấy từ tên người nạp khai — nên một tên file mang
    /// <c>../</c> không chạm được tới đường dẫn.
    /// </summary>
    [Fact]
    public async Task Ten_file_mang_duong_dan_KHONG_cham_toi_cho_ghi()
    {
        using var app = App();
        var c = app.CreateClient();

        var res = await c.PostAsync("/documents", Goi("../../../thoat-ra.txt", "thử đi ngang thư mục"));
        res.EnsureSuccessStatusCode();

        var id = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await using var ctx = db.OpenContext(db.TenantAId);
        var tl = await ctx.Set<Document>().SingleAsync(x => x.Id == id);

        // Mã kho là 32 ký tự hex do sản phẩm sinh — không dính gì tới tên khai báo.
        Assert.Equal(32, tl.MaKho.Length);
        Assert.DoesNotContain("..", tl.MaKho);
        Assert.All(tl.MaKho, ch => Assert.True(char.IsAsciiHexDigitLower(ch)));
    }
}

/// <summary>Bản đọc của <c>GET /documents</c> — giữ riêng để test không phụ thuộc kiểu nội bộ.</summary>
public sealed record TomTatTaiLieu(
    Guid Id, string TenKhaiBao, string LoaiNoiDung, long KichThuoc,
    string SubTenantCode, string NguoiNap, DateTimeOffset NapLuc);
