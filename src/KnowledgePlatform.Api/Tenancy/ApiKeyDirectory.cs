using KnowledgePlatform.Domain.Tenancy;
using Npgsql;

namespace KnowledgePlatform.Api.Tenancy;

/// <summary>Kết quả tra một khoá API. <c>null</c> ở chỗ gọi nghĩa là không nhận ra khoá.</summary>
/// <param name="ApiKeyId">Khoá chính của dòng — dùng để ghi nhật ký, KHÔNG phải bí mật.</param>
/// <param name="TenantId">Công ty khách hàng mà khoá này nói thay.</param>
/// <param name="ConHieuLuc">Sai nghĩa là đã thu hồi.</param>
public sealed record ApiKeyLookup(Guid ApiKeyId, Guid TenantId, bool ConHieuLuc);

/// <summary>
/// Đổi một chuỗi khoá API thành tenant — cửa DUY NHẤT đọc bảng <c>kp.api_key</c>.
///
/// ⚠ **Cố ý KHÔNG dùng <c>AppDbContext</c>**, y hệt <see cref="TenantDirectory"/> và vì
/// cùng một vòng tròn: <c>AppDbContext</c> cần <c>ITenantContext</c>, mà <c>ITenantContext</c>
/// thì đang đợi chính lượt tra này để biết mình là tenant nào. Vòng tròn đó không phải
/// vấn đề của DI — nó nói lên một điều thật: **việc xác thực nằm NGOÀI ranh giới tenant.**
///
/// Không cache, cố ý — cùng lập luận với <see cref="TenantDirectory"/>, và ở đây còn một
/// lý do nặng hơn: cache một khoá ĐÃ THU HỒI nghĩa là thu hồi không có tác dụng ngay, tức
/// phá đúng thứ mà `C1-1` chọn phương án này để có.
/// </summary>
public sealed class ApiKeyDirectory(string connectionString)
{
    /// <summary>
    /// Tra và kiểm một chuỗi khoá đầy đủ (<c>kp_&lt;keyId&gt;_&lt;secret&gt;</c>).
    ///
    /// ⚠ Trả <c>null</c> cho MỌI kiểu hỏng — sai hình dạng, không có dòng, sai bí mật.
    /// Phân biệt chúng ra ngoài là dựng sẵn một kênh dò khoá: *"khoá này có tồn tại không"*
    /// đã là thông tin về khách hàng khác.
    /// </summary>
    public async Task<ApiKeyLookup?> XacThucAsync(string? khoaDayDu, CancellationToken ct = default)
    {
        if (!ApiKeyMaterial.TryTach(khoaDayDu, out var keyId, out var secret)) return null;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """SELECT "Id", "TenantId", "SecretHash", "RevokedAt" FROM kp.api_key WHERE "KeyId" = @keyId""";
        cmd.Parameters.AddWithValue("keyId", keyId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var id = reader.GetGuid(0);
        var tenantId = reader.GetGuid(1);
        var hashLuu = reader.GetString(2);
        var conHieuLuc = await reader.IsDBNullAsync(3, ct);

        // So hash, KHÔNG so chuỗi khoá. Và so theo thời gian hằng số.
        return ApiKeyMaterial.HashKhop(ApiKeyMaterial.BamBiMat(secret), hashLuu)
            ? new ApiKeyLookup(id, tenantId, conHieuLuc)
            : null;
    }

    /// <summary>
    /// Sinh một khoá mới cho một công ty khách hàng và trả về chuỗi đầy đủ.
    ///
    /// ⚠ **Chuỗi trả về là lần DUY NHẤT nó tồn tại ở dạng đọc được.** Bảng chỉ giữ hash,
    /// nên mất là mất — phải sinh khoá khác. Đó là chủ đích, không phải thiếu tính năng.
    /// </summary>
    public async Task<string> TaoAsync(Guid tenantId, string label, CancellationToken ct = default)
    {
        var (keyId, secretHash, khoaDayDu) = ApiKeyMaterial.Sinh();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO kp.api_key ("Id", "TenantId", "KeyId", "SecretHash", "Label", "CreatedAt", "RevokedAt")
            VALUES (@id, @tenantId, @keyId, @hash, @label, @createdAt, NULL)
            """;
        cmd.Parameters.AddWithValue("id", Guid.CreateVersion7());
        cmd.Parameters.AddWithValue("tenantId", tenantId);
        cmd.Parameters.AddWithValue("keyId", keyId);
        cmd.Parameters.AddWithValue("hash", secretHash);
        cmd.Parameters.AddWithValue("label", label);
        cmd.Parameters.AddWithValue("createdAt", DateTimeOffset.UtcNow);

        await cmd.ExecuteNonQueryAsync(ct);
        return khoaDayDu;
    }

    /// <summary>Đếm khoá của một khách: (còn hiệu lực, tổng). Dùng cho lệnh soi cấu hình.</summary>
    public async Task<(int ConHieuLuc, int Tong)> DemAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT count(*) FILTER (WHERE \"RevokedAt\" IS NULL), count(*) " +
            "FROM kp.api_key WHERE \"TenantId\" = @tenantId";
        cmd.Parameters.AddWithValue("tenantId", tenantId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? (reader.GetInt32(0), reader.GetInt32(1)) : (0, 0);
    }

    /// <summary>Tên hiển thị + khoá ngoài của mọi khách. Dùng cho lệnh soi cấu hình.</summary>
    public async Task<IReadOnlyList<(Guid Id, string Ten, string ExternalKey)>> LietKeKhachAsync(
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT \"Id\", \"Name\", \"ExternalKey\" FROM kp.tenant ORDER BY \"CreatedAt\"";

        var ra = new List<(Guid, string, string)>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) ra.Add((reader.GetGuid(0), reader.GetString(1), reader.GetString(2)));

        return ra;
    }

    /// <summary>Thu hồi theo <c>KeyId</c> (phần công khai). Trả số dòng bị đổi.</summary>
    public async Task<int> ThuHoiAsync(string keyId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        // Đã thu hồi rồi thì KHÔNG ghi đè mốc cũ — mốc đầu tiên mới là mốc trả lời được
        // câu "từ lúc nào thì khoá này hết dùng được".
        cmd.CommandText =
            """UPDATE kp.api_key SET "RevokedAt" = @now WHERE "KeyId" = @keyId AND "RevokedAt" IS NULL""";
        cmd.Parameters.AddWithValue("keyId", keyId);
        cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);

        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
