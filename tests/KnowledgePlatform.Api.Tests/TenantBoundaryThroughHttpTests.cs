using System.Net;
using System.Net.Http.Json;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Ranh giới tenant, đo qua HTTP thật.
///
/// Bộ test ở `KnowledgePlatform.Infrastructure.Tests` đã chứng minh policy RLS
/// chặn được khi gọi trực tiếp <c>AppDbContext</c>. Bộ này kiểm mảnh còn lại:
/// **một request HTTP thật có mang đúng tenant xuống tới database hay không.**
///
/// Đó là mảnh mà trước khi có project host không thể kiểm được, vì không có
/// "request" nào tồn tại — `ITenantContext` chỉ có hợp đồng, chưa có thân.
///
/// Con số cốt lõi là <c>rowsVisibleWithoutTenantFilter</c>: một câu SQL thô CỐ Ý
/// không có điều kiện tenant. Nếu nó trả đúng số dòng của khách hàng gọi request,
/// nghĩa là cả chuỗi request → ITenantContext → interceptor → policy đang sống.
/// </summary>
public sealed class TenantBoundaryThroughHttpTests(ApiDatabaseFixture db)
    : IClassFixture<ApiDatabaseFixture>
{
    private sealed record BoundaryResponse(string Mode, Guid TenantId, VisibleRows RowsVisibleWithoutTenantFilter);
    private sealed record VisibleRows(int Cases, int Knowledge);

    private const string BoundaryPath = "/internal/tenant-boundary";

    // =====================================================================
    //  Chế độ dedicated — tenant từ CẤU HÌNH (G13)
    // =====================================================================

    [Fact]
    public async Task Health_tra_loi_duoc_khi_request_chua_co_tenant()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Che_do_dedicated_lay_tenant_tu_cau_hinh_va_chi_thay_du_lieu_cua_tenant_do()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var body = await client.GetFromJsonAsync<BoundaryResponse>(BoundaryPath);

        Assert.NotNull(body);
        Assert.Equal("DedicatedSingleTenant", body.Mode);
        Assert.Equal(db.TenantAId, body.TenantId);
        Assert.Equal(ApiDatabaseFixture.CasesForA, body.RowsVisibleWithoutTenantFilter.Cases);
    }

    /// <summary>
    /// Bản deploy riêng cho khách A **không thấy** dữ liệu của khách B, dù hai khách
    /// đang nằm trong cùng một database. Đây chính là điều làm `G13` khả thi: bản
    /// dedicated là cùng code, cùng schema, database chỉ TÌNH CỜ chứa một khách (`06` §3).
    /// </summary>
    [Fact]
    public async Task Hai_ban_deploy_dedicated_khac_nhau_thay_hai_bo_du_lieu_khac_nhau()
    {
        using var forA = ApiFactory.Dedicated(db.TenantAKey);
        using var forB = ApiFactory.Dedicated(db.TenantBKey);

        var a = await forA.CreateClient().GetFromJsonAsync<BoundaryResponse>(BoundaryPath);
        var b = await forB.CreateClient().GetFromJsonAsync<BoundaryResponse>(BoundaryPath);

        Assert.Equal(ApiDatabaseFixture.CasesForA, a!.RowsVisibleWithoutTenantFilter.Cases);
        Assert.Equal(ApiDatabaseFixture.CasesForB, b!.RowsVisibleWithoutTenantFilter.Cases);
        Assert.NotEqual(a.TenantId, b.TenantId);
    }

    // =====================================================================
    //  Chế độ shared — tenant từ NGỮ CẢNH REQUEST (G13)
    // =====================================================================

    /// <summary>
    /// Test quan trọng nhất của file: **cùng một tiến trình, cùng một endpoint, hai
    /// khách hàng, hai kết quả.** Đây là lời hứa của `AR2` ở dạng đo được.
    /// </summary>
    [Fact]
    public async Task Che_do_shared_cung_mot_host_hai_khach_hang_thay_hai_bo_du_lieu_khac_nhau()
    {
        using var factory = ApiFactory.Shared();
        using var client = factory.CreateClient();

        var a = await GetBoundaryAsync(client, db.TenantAApiKey);
        var b = await GetBoundaryAsync(client, db.TenantBApiKey);

        Assert.Equal(db.TenantAId, a.TenantId);
        Assert.Equal(db.TenantBId, b.TenantId);
        Assert.Equal(ApiDatabaseFixture.CasesForA, a.RowsVisibleWithoutTenantFilter.Cases);
        Assert.Equal(ApiDatabaseFixture.CasesForB, b.RowsVisibleWithoutTenantFilter.Cases);
    }

    /// <summary>
    /// Gọi xen kẽ nhiều lượt. Nếu tenant bị giữ ở đâu đó sống lâu hơn một request —
    /// một biến static, một singleton, một connection trong pool chưa được ghi lại
    /// (`IM-10`) — thì lượt sau sẽ thấy dữ liệu của lượt trước. Kiểu lỗi này không
    /// hiện ra khi test tuần tự một khách hàng.
    /// </summary>
    [Fact]
    public async Task Che_do_shared_goi_xen_ke_hai_khach_hang_khong_lan_du_lieu_sang_nhau()
    {
        using var factory = ApiFactory.Shared();
        using var client = factory.CreateClient();

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(ApiDatabaseFixture.CasesForA,
                (await GetBoundaryAsync(client, db.TenantAApiKey)).RowsVisibleWithoutTenantFilter.Cases);
            Assert.Equal(ApiDatabaseFixture.CasesForB,
                (await GetBoundaryAsync(client, db.TenantBApiKey)).RowsVisibleWithoutTenantFilter.Cases);
        }
    }

    [Fact]
    public async Task Che_do_shared_thieu_khoa_thi_401_va_khong_tra_du_lieu()
    {
        using var factory = ApiFactory.Shared();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(BoundaryPath);

        // ⚠ Đổi từ 400 sang 401 khi `AR-e` thành code: trước đây thiếu header tenant là
        // "không biết bạn là ai" (400); giờ là "bạn chưa chứng minh được mình là ai" (401).
        // Khác nhau thật, không phải đổi số cho đẹp.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("rowsVisible", await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Khoá không tồn tại trả 401 **giống hệt** trường hợp thiếu khoá, cố ý.
    /// Phân biệt hai ca đó là nói cho người gọi biết khoá nào CÓ tồn tại — tức là
    /// tiết lộ thông tin về khách hàng khác.
    /// </summary>
    [Fact]
    public async Task Che_do_shared_khoa_khong_ton_tai_thi_401_giong_het_thieu_khoa()
    {
        using var factory = ApiFactory.Shared();
        using var client = factory.CreateClient();

        var response = await GetRawAsync(client, "kp_khongcothat_khongcothat");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =====================================================================
    //  Từ chối khởi động — cấu hình sai KHÔNG được chạy im lặng
    // =====================================================================

    [Fact]
    public void Tu_choi_khoi_dong_khi_thieu_Tenancy_Mode()
    {
        using var factory = ApiFactory.WithSettings([]);

        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("Tenancy:Mode", ex.Message);
        Assert.Contains("G13", ex.Message);
    }

    /// <summary>
    /// ⚠ **Thay cho test cũ**, và khác biệt đáng đọc: bản cũ kiểm rằng chế độ shared TỪ CHỐI
    /// KHỞI ĐỘNG khi chưa ai thừa nhận là "chưa có xác thực" — một cổng TRÁNH (`IM-13`).
    /// `AR-e` đã GIẢI thứ nó tránh, nên cổng đó bị gỡ và cờ cũ bị xoá.
    ///
    /// Cái thay nó KHÔNG phải cùng một cổng đổi tên: bỏ xác thực ở chế độ shared giờ không
    /// còn là "chấp nhận rủi ro" mà là **vô nghĩa** — không còn đường nào xác định tenant.
    /// Nên nó bị TỪ CHỐI, kể cả khi có người thừa nhận tường minh.
    /// </summary>
    [Fact]
    public void Che_do_shared_TU_CHOI_co_thua_nhan_chay_khong_khoa()
    {
        using var factory = ApiFactory.Shared().With("Auth:AcknowledgeUnauthenticatedApi", "true");

        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("AcknowledgeUnauthenticatedApi", ex.Message);
        Assert.Contains("SharedMultiTenant", ex.Message);
    }

    /// <summary>
    /// Khoá cấu hình sai mà vẫn start được là ca xấu nhất: ứng dụng chạy bình thường,
    /// không thấy dữ liệu nào, và không có gì chỉ ra vì sao.
    /// </summary>
    [Fact]
    public void Tu_choi_khoi_dong_khi_khoa_tenant_trong_cau_hinh_khong_ton_tai()
    {
        using var factory = ApiFactory.Dedicated("khoa-khong-ton-tai-trong-db");

        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("kp.tenant", ex.Message);
        Assert.Contains("khoa-khong-ton-tai-trong-db", ex.Message);
    }

    // =====================================================================
    //  Readiness không phải thứ trang trí
    // =====================================================================

    /// <summary>
    /// <c>/health/ready</c> phải chuyển sang 503 khi ranh giới tenant bị hỏng trên
    /// database ĐANG CHẠY. <c>StartupChecks</c> chỉ kiểm lúc start; nếu ai đó tắt RLS
    /// sau đó thì phải có chỗ phát hiện.
    /// </summary>
    [Fact]
    public async Task Ready_chuyen_sang_503_khi_RLS_bi_tat_tren_database_dang_chay()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.ExecuteSqlRawAsync("ALTER TABLE kp.assertion DISABLE ROW LEVEL SECURITY");
        try
        {
            var response = await client.GetAsync("/health/ready");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains("assertion", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE kp.assertion ENABLE ROW LEVEL SECURITY");
        }

        // Trả về nguyên trạng rồi kiểm lại — test này không được để lại DB hỏng.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    // =====================================================================

    private async Task<BoundaryResponse> GetBoundaryAsync(HttpClient client, string apiKey)
    {
        var response = await GetRawAsync(client, apiKey);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<BoundaryResponse>())!;
    }

    /// <summary>
    /// ⚠ Đổi 2026-09-16 cùng `AR-e`: tenant đến từ KHOÁ ĐÃ KIỂM, không từ header người gọi
    /// tự điền. Tham số vì thế là chuỗi khoá API, không phải <c>ExternalKey</c> của tenant.
    /// </summary>
    private static async Task<HttpResponseMessage> GetRawAsync(HttpClient client, string apiKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BoundaryPath);
        request.Headers.Authorization = ApiDatabaseFixture.Bearer(apiKey);

        return await client.SendAsync(request);
    }
}
