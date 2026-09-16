using System.Net.Http.Headers;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Dựng host thật của API trong bộ nhớ, trên PostgreSQL thật.
///
/// ⚠ Cố ý dùng <c>UseEnvironment("Production")</c>: môi trường Development sẽ nạp
/// <c>appsettings.Development.json</c> và mang theo cấu hình của máy dev vào test.
/// Test phải nói rõ nó đang kiểm cấu hình nào — không thừa hưởng.
///
/// ⚠ Database RIÊNG (<c>kp_api_test</c>), không dùng chung với bộ test
/// Infrastructure: bộ đó có một test TẮT RLS tạm thời để kiểm <c>RlsGuard</c> biết
/// ném. Dùng chung thì test đó làm test ở đây đỏ ngẫu nhiên.
/// </summary>
public sealed class ApiFactory(Dictionary<string, string?> settings, Action<IServiceCollection>? dichVu = null)
    : WebApplicationFactory<Program>
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("KP_API_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=kp_api_test;Username=kp_app;Password=123456";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);

        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings));
        if (dichVu is not null) builder.ConfigureServices(dichVu);
    }

    /// <summary>Bản deploy riêng cho một khách hàng — chế độ của khách hàng #0 (`D3`).</summary>
    public static ApiFactory Dedicated(string tenantExternalKey) => new(new()
    {
        ["Tenancy:Mode"] = "DedicatedSingleTenant",
        ["Tenancy:TenantExternalKey"] = tenantExternalKey,
        ["Auth:AcknowledgeUnauthenticatedApi"] = "true",
    });

    /// <summary>
    /// Một bản deploy phục vụ nhiều khách hàng. Tenant đến từ KHOÁ API (`AR-e`), nên
    /// mọi request ở chế độ này phải mang <c>Authorization: Bearer kp_...</c> —
    /// không còn header <c>X-Tenant-Key</c> để tự khai mình là ai.
    /// </summary>
    public static ApiFactory Shared() => new(new()
    {
        ["Tenancy:Mode"] = "SharedMultiTenant",
    });

    public static ApiFactory WithSettings(Dictionary<string, string?> settings) => new(settings);

    /// <summary>
    /// Cùng cấu hình, đổi/thêm một khoá. Trả về factory MỚI thay vì sửa tại chỗ —
    /// cấu hình đã đọc rồi thì đổi cũng không có tác dụng, nên sửa tại chỗ chỉ tạo
    /// ra một test trông như đang kiểm cái gì đó mà thật ra không.
    /// </summary>
    public ApiFactory With(string key, string? value) =>
        new(new Dictionary<string, string?>(settings) { [key] = value }, dichVu);

    /// <summary>Ghi đè dịch vụ trong DI — dùng để cắm một bên nhận webhook GIẢ.</summary>
    public ApiFactory WithServices(Action<IServiceCollection> ghiDe) =>
        new(new Dictionary<string, string?>(settings), ghiDe);

    /// <summary>
    /// Bỏ phần thừa nhận "chạy không khoá" — từ đây mọi endpoint đòi khoá API thật.
    /// Dùng cho test kiểm chính cái cổng đó; phần lớn test khác không cần.
    /// </summary>
    public ApiFactory RequireApiKey() => With("Auth:AcknowledgeUnauthenticatedApi", "false");
}

/// <summary>
/// Chuẩn bị database dùng chung cho cả class test: migrate một lần, và tạo sẵn
/// hai công ty khách hàng để kiểm cách ly.
/// </summary>
public sealed class ApiDatabaseFixture : IAsyncLifetime
{
    public string TenantAKey { get; } = $"tenant-a-{Guid.CreateVersion7()}";
    public string TenantBKey { get; } = $"tenant-b-{Guid.CreateVersion7()}";

    public Guid TenantAId { get; private set; }
    public Guid TenantBId { get; private set; }

    /// <summary>
    /// Khoá API thật của từng khách (`AR-e`), sinh đúng bằng đường mà vận hành sẽ dùng.
    ///
    /// ⚠ Cố ý KHÔNG nhét một chuỗi khoá viết tay vào đây. Bài học `IM-37`/`IM-38` lặp lại:
    /// vật liệu test phải đi qua chính đường sinh của sản phẩm, không lấy từ trí nhớ người
    /// viết test — nếu không thì hình dạng khoá đổi mà test vẫn xanh.
    /// </summary>
    public string TenantAApiKey { get; private set; } = string.Empty;

    public string TenantBApiKey { get; private set; } = string.Empty;

    /// <summary>Số case đã tạo cho từng khách — con số mà API phải thấy, và chỉ thấy đúng nó.</summary>
    public const int CasesForA = 2;
    public const int CasesForB = 5;

    public async Task InitializeAsync()
    {
        await using var db = NewContext(unresolvedTenant: true);
        await db.Database.MigrateAsync();

        TenantAId = await SeedTenantAsync(TenantAKey, CasesForA);
        TenantBId = await SeedTenantAsync(TenantBKey, CasesForB);

        var khoa = new Tenancy.ApiKeyDirectory(ApiFactory.ConnectionString);
        TenantAApiKey = await khoa.TaoAsync(TenantAId, "test — tenant A");
        TenantBApiKey = await khoa.TaoAsync(TenantBId, "test — tenant B");
    }

    /// <summary>Header xác thực cho một khoá — đúng hình dạng mà bên tích hợp phải gửi.</summary>
    public static AuthenticationHeaderValue Bearer(string apiKey) => new("Bearer", apiKey);

    /// <summary>
    /// Dựng một công ty khách hàng MỚI, dùng riêng cho một test.
    ///
    /// ⚠ Vì sao cần, và nó là bài học đã trả giá một lần ở <c>VongTriThucTests</c>: cả class
    /// chạy trên MỘT database, nên thứ một test ghi vào tenant dùng chung sẽ được test sau
    /// đọc phải. Với dữ liệu "có hay không có" — hàng đợi gửi, cấu hình đích webhook — thì
    /// điều đó không chỉ làm test đỏ, nó còn làm test XANH VÌ LÝ DO SAI.
    /// </summary>
    public async Task<(string Key, Guid Id)> TaoTenantMoiAsync()
    {
        var key = $"tenant-rieng-{Guid.CreateVersion7():N}";
        var id = await SeedTenantAsync(key, caseCount: 0);
        return (key, id);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Mở một <see cref="AppDbContext"/> trỏ vào chính database test, với tenant chỉ định.
    ///
    /// Dùng khi test cần ĐỌC LẠI thứ vừa ghi mà API chưa có endpoint đọc — ví dụ nhãn
    /// <c>MachineReadability</c> đã lưu là gì. Không có nó thì mấy hành vi "âm thầm"
    /// (tự về Unknown, âm thầm ghi đè nội dung) chỉ kiểm được gián tiếp qua số dòng,
    /// mà số dòng thì không phân biệt được "giữ nguyên" với "ghi đè".
    /// </summary>
    public AppDbContext OpenContext(Guid tenantId) => NewContext(tenantId: tenantId);

    private static AppDbContext NewContext(bool unresolvedTenant = false, Guid? tenantId = null)
    {
        ITenantContext tenant = unresolvedTenant
            ? new UnresolvedTenant()
            : new FixedTenant(tenantId!.Value);

        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ApiFactory.ConnectionString)
                .AddInterceptors(new TenantConnectionInterceptor(tenant))
                .Options,
            tenant);
    }

    private static async Task<Guid> SeedTenantAsync(string externalKey, int caseCount)
    {
        var tenantId = Guid.CreateVersion7();

        // Bảng tenant là danh bạ, không tenant-scoped → ghi được khi chưa có tenant.
        await using (var db = NewContext(unresolvedTenant: true))
        {
            db.Tenants.Add(new Tenant { Id = tenantId, Name = externalKey, ExternalKey = externalKey });
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext(tenantId: tenantId))
        {
            for (var i = 0; i < caseCount; i++)
            {
                db.Cases.Add(new Domain.Cases.CanonicalCase
                {
                    TenantId = tenantId,
                    Subject = $"Case {i} của {externalKey}",
                    SourceReference = $"test:{Guid.CreateVersion7()}",
                });
            }

            await db.SaveChangesAsync();
        }

        return tenantId;
    }

    private sealed class FixedTenant(Guid id) : ITenantContext
    {
        public Guid TenantId => id;
        public bool IsResolved => true;
    }

    private sealed class UnresolvedTenant : ITenantContext
    {
        public bool IsResolved => false;
        public Guid TenantId => throw new InvalidOperationException("Chưa xác định tenant.");
    }
}
