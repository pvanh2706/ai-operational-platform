using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Infrastructure.Tests;

/// <summary>
/// Các test trong project này chạy trên PostgreSQL THẬT, cố ý.
///
/// Row-level security là một tính năng của database. Test nó bằng in-memory
/// provider hay SQLite là test một thứ khác và tự cho mình cảm giác an toàn —
/// đúng loại thất bại im lặng mà `G7` đang cố chặn. `07 §2` đã ghi "apply
/// migration: CHƯA" trong nhiều buổi; những test này là chỗ đóng ghi chú đó lại.
/// </summary>
public sealed class TestDatabaseFixture : IAsyncLifetime
{
    /// <summary>
    /// `G13` áp cả cho cấu hình test: chuỗi kết nối đến từ MÔI TRƯỜNG, không
    /// phải hằng số trong code. Giá trị mặc định bên dưới chỉ để máy dev local
    /// chạy được ngay; deploy thật đặt <c>KP_TEST_DB</c>.
    /// </summary>
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("KP_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=kp_test;Username=kp_app;Password=123456";

    private const string SetupHelp = """
        Không kết nối / migrate được database test.

        Dựng một lần bằng superuser:
            psql -U postgres -h localhost -f scripts/dev-db-setup.sql

        Hoặc trỏ sang database khác:
            set KP_TEST_DB=Host=...;Database=...;Username=...;Password=...

        ⚠ KHÔNG trỏ vào role superuser. Superuser đi vòng qua row-level security
        nên mọi test cách ly tenant sẽ PASS GIẢ. Xem scripts/dev-db-setup.sql.
        """;

    public async Task InitializeAsync()
    {
        await using var db = NewContext(new UnresolvedTenantContext());
        try
        {
            await db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{SetupHelp}\n\nLỗi gốc: {ex.Message}", ex);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Dựng <see cref="AppDbContext"/> đúng như production sẽ dựng: cùng một
    /// <see cref="ITenantContext"/> vừa đi vào global query filter (tầng 2) vừa
    /// đi vào <see cref="TenantConnectionInterceptor"/> (tầng 1, RLS).
    ///
    /// Nếu test tự đặt <c>app.current_tenant</c> bằng tay thì nó kiểm SQL của
    /// migration mà KHÔNG kiểm mắt xích C#-Postgres — mà mắt xích đó chính là
    /// chỗ vừa được viết và chưa ai chạy thật.
    /// </summary>
    public AppDbContext NewContext(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .AddInterceptors(new TenantConnectionInterceptor(tenant))
                .Options,
            tenant);

    /// <summary>Đếm dòng bằng SQL THÔ, cố ý KHÔNG có điều kiện tenant nào.</summary>
    public static async Task<int> CountCasesWithoutTenantFilterAsync(AppDbContext db) =>
        await db.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM kp.canonical_case")
            .FirstAsync();
}

/// <summary>Tenant đã xác định — mô phỏng một request bình thường.</summary>
public sealed class FixedTenantContext(Guid tenantId) : ITenantContext
{
    public Guid TenantId => tenantId;
    public bool IsResolved => true;
}

/// <summary>
/// Chưa xác định được tenant — job hệ thống, health check, migration, hoặc một
/// request mà host app không gửi kèm tenant. Đọc <see cref="TenantId"/> là ném:
/// thà lỗi ồn còn hơn chạy một truy vấn không có ranh giới.
/// </summary>
public sealed class UnresolvedTenantContext : ITenantContext
{
    public bool IsResolved => false;

    public Guid TenantId => throw new InvalidOperationException(
        "Tenant chưa được xác định. Không truy vấn dữ liệu khách hàng ở trạng thái này.");
}
