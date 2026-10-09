using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Tenancy;
using Npgsql;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>Kết quả tra một token link. <c>null</c> ở chỗ gọi nghĩa là không mở được.</summary>
/// <param name="LuotGoiYId">Lượt gợi ý đã phát link này (P6) — để mở link ghi được nấc OPENED.</param>
public sealed record LinkTokenLookup(Guid TenantId, Guid KnowledgeRecordId, Guid? LuotGoiYId = null);

/// <summary>
/// Cấu hình đường link công khai của bản deploy.
/// </summary>
public sealed class LinkXemOptions
{
    public const string SectionName = "Link";

    /// <summary>
    /// Địa chỉ gốc để dựng link, ví dụ <c>http://kp.noi-bo.ezcloud:5119</c>.
    ///
    /// 🛑 **PHẢI là cấu hình, KHÔNG được suy từ host của request** — và đây là chỗ rất dễ
    /// làm sai mà không ai thấy. Suy từ request thì lúc chạy trên chính máy đó nó sinh ra
    /// <c>http://localhost:5119/...</c>: **tự bấm thì đúng, gửi cho người khác thì chết**.
    /// Chủ dự án chốt `H1-5` (chạy local Windows trước) + `H1b-1` (máy nằm trong LAN/VPN),
    /// nên link phải mang đúng tên máy trong mạng — thứ mà request của chính mình không
    /// bao giờ nói ra.
    ///
    /// Thiếu giá trị này thì endpoint phát link **từ chối**, không đoán.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>Hạn mặc định của một link, tính bằng ngày.</summary>
    public int HanMacDinhNgay { get; set; } = 14;

    /// <summary>Trần hạn — không ai phát được link sống lâu hơn mức này.</summary>
    public int HanToiDaNgay { get; set; } = 90;
}

/// <summary>
/// Cửa DUY NHẤT đọc bảng <c>kp.knowledge_link_token</c> (`D5-3`).
///
/// ⚠ Cố ý KHÔNG dùng <c>AppDbContext</c>, y hệt <see cref="Tenancy.ApiKeyDirectory"/> và vì
/// cùng một vòng tròn: người bấm link không mang theo gì ngoài chính cái token, nên lượt tra
/// này phải chạy **trước khi** biết tenant — tức ngoài ranh giới tenant.
///
/// Không cache: cache một token ĐÃ THU HỒI hoặc ĐÃ HẾT HẠN nghĩa là hai cơ chế duy nhất của
/// `D5-3` mất tác dụng ngay ở chỗ chúng cần có tác dụng nhất.
/// </summary>
public sealed class LinkTokenDirectory(string connectionString)
{
    /// <summary>
    /// Tra và kiểm một chuỗi token. Trả <c>null</c> cho MỌI kiểu hỏng — sai hình dạng, không
    /// có dòng, sai bí mật, đã thu hồi, đã hết hạn.
    ///
    /// ⚠ Gộp hết làm một câu trả lời là CÓ CHỦ ĐÍCH: phân biệt *"hết hạn"* với *"không tồn
    /// tại"* cho người cầm link biết token đó TỪNG có thật — đủ để dò xem một bản tri thức
    /// có tồn tại hay không.
    /// </summary>
    public async Task<LinkTokenLookup?> MoAsync(string? chuoi, DateTimeOffset bayGio, CancellationToken ct = default)
    {
        if (!ChuoiBiMat.TryTach(chuoi, KnowledgeLinkToken.TienTo, out var tokenId, out var biMat)) return null;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT "TenantId", "KnowledgeRecordId", "SecretHash", "ExpiresAt", "RevokedAt", "LuotGoiYId"
            FROM kp.knowledge_link_token WHERE "TokenId" = @tokenId
            """;
        cmd.Parameters.AddWithValue("tokenId", tokenId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var tenantId = reader.GetGuid(0);
        var recordId = reader.GetGuid(1);
        var hashLuu = reader.GetString(2);
        var hetHan = reader.GetFieldValue<DateTimeOffset>(3);
        var thuHoi = await reader.IsDBNullAsync(4, ct) ? (DateTimeOffset?)null : reader.GetFieldValue<DateTimeOffset>(4);
        var luotGoiY = await reader.IsDBNullAsync(5, ct) ? (Guid?)null : reader.GetGuid(5);

        if (!ChuoiBiMat.HashKhop(ChuoiBiMat.Bam(biMat), hashLuu)) return null;

        var token = new KnowledgeLinkToken
        {
            TenantId = tenantId, KnowledgeRecordId = recordId, TokenId = tokenId,
            SecretHash = hashLuu, CreatedBy = string.Empty, ExpiresAt = hetHan, RevokedAt = thuHoi,
        };

        return token.ConMoDuoc(bayGio) ? new LinkTokenLookup(tenantId, recordId, luotGoiY) : null;
    }

    /// <summary>
    /// Phát một token mới. Chuỗi trả về là lần DUY NHẤT nó đọc được — bảng chỉ giữ hash.
    /// </summary>
    /// <param name="luotGoiYId">Lượt gợi ý phát link này (P6). <c>null</c> khi phát tay.</param>
    public async Task<(string Chuoi, DateTimeOffset HetHanLuc)> PhatAsync(
        Guid tenantId, Guid recordId, string nguoiPhat, TimeSpan han, CancellationToken ct = default,
        Guid? luotGoiYId = null)
    {
        var (tokenId, hash, chuoi) = ChuoiBiMat.Sinh(KnowledgeLinkToken.TienTo);
        var hetHan = DateTimeOffset.UtcNow + han;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO kp.knowledge_link_token
                ("Id", "TenantId", "KnowledgeRecordId", "TokenId", "SecretHash",
                 "CreatedAt", "CreatedBy", "ExpiresAt", "RevokedAt", "LuotGoiYId")
            VALUES (@id, @tenantId, @recordId, @tokenId, @hash, @createdAt, @createdBy, @expiresAt, NULL, @luotGoiY)
            """;
        cmd.Parameters.AddWithValue("id", Guid.CreateVersion7());
        cmd.Parameters.AddWithValue("tenantId", tenantId);
        cmd.Parameters.AddWithValue("recordId", recordId);
        cmd.Parameters.AddWithValue("tokenId", tokenId);
        cmd.Parameters.AddWithValue("hash", hash);
        cmd.Parameters.AddWithValue("createdAt", DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("createdBy", nguoiPhat);
        cmd.Parameters.AddWithValue("expiresAt", hetHan);
        cmd.Parameters.AddWithValue("luotGoiY", (object?)luotGoiYId ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
        return (chuoi, hetHan);
    }

    /// <summary>Thu hồi mọi token còn sống của một bản tri thức. Trả số dòng bị đổi.</summary>
    public async Task<int> ThuHoiTheoRecordAsync(Guid recordId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            UPDATE kp.knowledge_link_token SET "RevokedAt" = @now
            WHERE "KnowledgeRecordId" = @recordId AND "RevokedAt" IS NULL
            """;
        cmd.Parameters.AddWithValue("recordId", recordId);
        cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);

        return await cmd.ExecuteNonQueryAsync(ct);
    }
}
