using Npgsql;

namespace KnowledgePlatform.Api.Tenancy;

/// <summary>
/// Đổi <c>Tenant.ExternalKey</c> (khoá do phần mềm của khách cung cấp, `06` §1)
/// thành <c>TenantId</c>.
///
/// ⚠ **Cố ý KHÔNG dùng <c>AppDbContext</c>.** Nếu dùng thì sẽ vòng tròn:
/// <c>AppDbContext</c> cần <c>ITenantContext</c>, mà <c>ITenantContext</c> lại
/// đang đi tìm chính tenant của mình. Vòng tròn đó không chỉ là vấn đề DI — nó
/// nói lên một điều thật: **việc tra tenant nằm NGOÀI ranh giới tenant.**
///
/// Đó cũng là lý do bảng <c>kp.tenant</c> là bảng duy nhất KHÔNG có RLS: nó là
/// danh bạ, không phải dữ liệu của một khách hàng nào.
///
/// Không cache, cố ý (§6.7 "vừa đủ để chạy"). Một truy vấn khoá chính mỗi tín
/// hiệu là rẻ, còn cache sai ở đúng chỗ này nghĩa là **phục vụ sai khách hàng** —
/// đắt hơn nhiều so với chỗ nó tiết kiệm. Thêm cache khi ĐO ĐƯỢC là cần.
/// </summary>
public sealed class TenantDirectory(string connectionString)
{
    public async Task<Guid?> FindIdByExternalKeyAsync(string externalKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalKey)) return null;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """SELECT "Id" FROM kp.tenant WHERE "ExternalKey" = @key""";
        cmd.Parameters.AddWithValue("key", externalKey);

        var result = await cmd.ExecuteScalarAsync(ct);
        return result is Guid id ? id : null;
    }

    /// <summary>
    /// Dựng một công ty khách hàng mới, trả về id.
    ///
    /// ⚠ Ghi thẳng bằng SQL chứ không qua <c>AppDbContext</c>, cùng lý do với phần đọc ở trên:
    /// lúc này chưa có tenant nào để mà đặt vào <c>ITenantContext</c>. Bảng <c>kp.tenant</c>
    /// không có RLS nên ghi được — và đó chính là điều làm nó thành DANH BẠ chứ không phải
    /// dữ liệu của một khách hàng nào.
    /// </summary>
    public async Task<Guid> TaoAsync(string externalKey, string ten, CancellationToken ct = default)
    {
        var id = Guid.CreateVersion7();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "INSERT INTO kp.tenant (\"Id\", \"Name\", \"ExternalKey\", \"CreatedAt\") " +
            "VALUES (@id, @name, @key, @createdAt)";
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("name", ten);
        cmd.Parameters.AddWithValue("key", externalKey);
        cmd.Parameters.AddWithValue("createdAt", DateTimeOffset.UtcNow);

        await cmd.ExecuteNonQueryAsync(ct);
        return id;
    }

    /// <summary>
    /// Liệt kê id của MỌI công ty khách hàng. Dùng cho tiến trình nền — nơi không có request
    /// nào để suy ra tenant, nên phải duyệt từng khách một.
    ///
    /// ⚠ Đây là đường DUY NHẤT hợp lệ để một tiến trình nền chạm tới nhiều tenant, và nó
    /// vẫn không vượt ranh giới: nó chỉ đọc DANH BẠ (bảng vốn không thuộc tenant nào), rồi
    /// mỗi khách được phục vụ trong một scope riêng với tenant đặt tường minh. Cách còn lại —
    /// miễn RLS cho bảng nghiệp vụ để đọc một phát — là mở một lỗ ở đúng chỗ `G7` gọi là nền tảng.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> LietKeIdAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT "Id" FROM kp.tenant ORDER BY "CreatedAt"
            """;

        var ids = new List<Guid>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) ids.Add(reader.GetGuid(0));

        return ids;
    }
}
