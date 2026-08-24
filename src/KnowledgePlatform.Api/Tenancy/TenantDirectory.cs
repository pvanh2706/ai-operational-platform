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
}
