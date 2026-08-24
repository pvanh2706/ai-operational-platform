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
    });

    /// <summary>Một bản deploy phục vụ nhiều khách hàng, tenant từ header.</summary>
    public static ApiFactory Shared(bool acknowledgeNoAuth = true) => new(new()
    {
        ["Tenancy:Mode"] = "SharedMultiTenant",
        ["Tenancy:AcknowledgeUnauthenticatedTenantHeader"] = acknowledgeNoAuth ? "true" : "false",
    });

    public static ApiFactory WithSettings(Dictionary<string, string?> settings) => new(settings);
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
