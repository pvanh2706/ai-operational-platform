using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
public sealed class ApiFactory(Dictionary<string, string?> settings) : WebApplicationFactory<Program>
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("KP_API_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=kp_api_test;Username=kp_app;Password=123456";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);

        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings));
    }

    /// <summary>Bản deploy riêng cho một khách hàng — chế độ của khách hàng #0 (`D3`).</summary>
    public static ApiFactory Dedicated(string tenantExternalKey) => new(new()
    {
        ["Tenancy:Mode"] = "DedicatedSingleTenant",
        ["Tenancy:TenantExternalKey"] = tenantExternalKey,
        ["Ingest:AcknowledgeUnauthenticatedSignalEndpoint"] = "true",
    });

    /// <summary>Một bản deploy phục vụ nhiều khách hàng, tenant từ header.</summary>
    public static ApiFactory Shared(bool acknowledgeNoAuth = true) => new(new()
    {
        ["Tenancy:Mode"] = "SharedMultiTenant",
        ["Tenancy:AcknowledgeUnauthenticatedTenantHeader"] = acknowledgeNoAuth ? "true" : "false",
        ["Ingest:AcknowledgeUnauthenticatedSignalEndpoint"] = "true",
    });

    public static ApiFactory WithSettings(Dictionary<string, string?> settings) => new(settings);

    /// <summary>
    /// Cùng cấu hình, đổi/thêm một khoá. Trả về factory MỚI thay vì sửa tại chỗ —
    /// cấu hình đã đọc rồi thì đổi cũng không có tác dụng, nên sửa tại chỗ chỉ tạo
    /// ra một test trông như đang kiểm cái gì đó mà thật ra không.
    /// </summary>
    public ApiFactory With(string key, string? value) =>
        new(new Dictionary<string, string?>(settings) { [key] = value });

    /// <summary>Đặt khoá cho endpoint tín hiệu, và bỏ luôn phần thừa nhận "chạy không khoá".</summary>
    public ApiFactory WithSignalApiKey(string apiKey) =>
        With("Ingest:SignalApiKey", apiKey)
            .With("Ingest:AcknowledgeUnauthenticatedSignalEndpoint", "false");
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

    /// <summary>Số case đã tạo cho từng khách — con số mà API phải thấy, và chỉ thấy đúng nó.</summary>
    public const int CasesForA = 2;
    public const int CasesForB = 5;

    public async Task InitializeAsync()
    {
        await using var db = NewContext(unresolvedTenant: true);
        await db.Database.MigrateAsync();

        TenantAId = await SeedTenantAsync(TenantAKey, CasesForA);
        TenantBId = await SeedTenantAsync(TenantBKey, CasesForB);
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
