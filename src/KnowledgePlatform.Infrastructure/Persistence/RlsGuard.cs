using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KnowledgePlatform.Infrastructure.Persistence;

/// <summary>
/// Kiểm lúc KHỞI ĐỘNG rằng mọi bảng tenant-scoped đều đã bật row-level security
/// và có policy. Thiếu là ném ngay — không phải lỗ rò lúc chạy.
///
/// Vì sao cần thứ này: G7 nói tenant boundary là NỀN TẢNG. Nền tảng nghĩa là
/// không dựa vào việc không ai quên. Một entity mới cài ITenantScoped mà migration
/// quên bật RLS sẽ chạy hoàn toàn bình thường trong dev (global query filter che
/// mất), rồi rò rỉ dữ liệu giữa hai công ty khách hàng trên production ở đúng chỗ
/// nào đó dùng SQL thô.
///
/// Danh sách bảng cần kiểm được suy ra TỪ MODEL (AppDbContext.TenantScopedTables),
/// không phải danh sách viết tay — nên nó không thể lệch với model.
/// </summary>
public static class RlsGuard
{
    /// <summary>
    /// Gọi ở startup, sau khi migration đã chạy. Ném <see cref="InvalidOperationException"/>
    /// kèm tên bảng thiếu nếu có bảng nào chưa được bảo vệ.
    /// </summary>
    public static async Task VerifyAsync(AppDbContext db, CancellationToken ct = default)
    {
        var expected = db.TenantScopedTables;
        if (expected.Count == 0)
        {
            throw new InvalidOperationException(
                "Không tìm thấy bảng tenant-scoped nào. Hoặc model sai, hoặc ITenantScoped " +
                "đã bị bỏ khỏi các entity — cả hai đều nghĩa là ranh giới tenant không được thực thi.");
        }

        var schema = db.Model.GetDefaultSchema() ?? "public";

        var protectedTables = new HashSet<string>(StringComparer.Ordinal);
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        var opened = conn.State != System.Data.ConnectionState.Open;
        if (opened) await conn.OpenAsync(ct);

        try
        {
            // Vừa bật RLS (relrowsecurity) VÀ có ít nhất một policy — bật mà
            // không có policy thì Postgres chặn HẾT, nghĩa là cấu hình sai chứ
            // không phải an toàn.
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT c.relname
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = @schema
                  AND c.relkind = 'r'
                  AND c.relrowsecurity
                  AND EXISTS (SELECT 1 FROM pg_policy p WHERE p.polrelid = c.oid)
                """;
            var p = cmd.CreateParameter();
            p.ParameterName = "schema";
            p.Value = schema;
            cmd.Parameters.Add(p);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                protectedTables.Add(reader.GetString(0));
            }
        }
        finally
        {
            if (opened) await conn.CloseAsync();
        }

        var unprotected = expected.Where(t => !protectedTables.Contains(t)).ToList();
        if (unprotected.Count > 0)
        {
            throw new InvalidOperationException(
                $"Row-level security thiếu ở {unprotected.Count} bảng tenant-scoped: " +
                $"{string.Join(", ", unprotected)}. " +
                "Đây là vi phạm G7 (tenant boundary là nền tảng) — không được khởi động. " +
                "Thêm ENABLE ROW LEVEL SECURITY + policy cho các bảng trên trong migration.");
        }
    }
}
