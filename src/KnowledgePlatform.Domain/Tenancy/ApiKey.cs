using System.Security.Cryptography;
using System.Text;

namespace KnowledgePlatform.Domain.Tenancy;

/// <summary>
/// Khoá API của MỘT công ty khách hàng — hiện thực của `AR-e` (`docs/14` §C1 = `C1-1`).
///
/// Vì sao nó thay được cái chốt cũ, và vì sao cái chốt cũ không bao giờ thay được nó:
/// khoá dùng chung (<c>Ingest:SignalApiKey</c>) trả lời câu *"người gọi có biết mật khẩu
/// không"*, còn ranh giới tenant hỏi câu *"người gọi LÀ AI"*. Hai câu khác nhau, và chừng
/// nào tenant còn đến từ một header mà người gọi tự điền (<c>X-Tenant-Key</c>) thì mọi lớp
/// xác thực thêm vào đều là trang trí.
///
/// ⚠ **Bảng này KHÔNG có RLS, và lý do KHÁC lý do của <c>kp.tenant</c>** — ghi rõ vì hai
/// chỗ trông giống nhau:
/// <list type="bullet">
///   <item><c>kp.tenant</c> miễn trừ vì nó **không thuộc tenant nào** (nó LÀ danh bạ).</item>
///   <item><c>kp.api_key</c> thì **CÓ** thuộc một tenant — nó miễn trừ vì phải đọc được
///         **TRƯỚC KHI** biết tenant của request. Chính dòng này quyết định tenant, nên
///         nó không thể nằm sau cánh cửa mà nó đang mở.</item>
/// </list>
/// Hệ quả phải canh: không endpoint nào được trả dòng của bảng này ra ngoài, và không có
/// <c>DbSet</c> nào cho nó trong <c>AppDbContext</c> — đọc nó đi qua đúng một cửa
/// (<c>ApiKeyDirectory</c>), giống <c>TenantDirectory</c> và vì cùng một lý do (`IM-14`).
/// </summary>
public sealed class ApiKey
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Công ty khách hàng mà khoá này nói thay.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>
    /// Phần CÔNG KHAI của khoá, dùng để tra dòng. Không bí mật — nó chỉ là số hiệu.
    ///
    /// Có nó thì việc kiểm khoá là một lần tra theo index, thay vì quét cả bảng rồi so
    /// hash từng dòng. Bảng có 5 dòng thì không khác gì; bảng có 5 000 dòng thì khác, và
    /// lúc đó sửa lại là đụng vào chính đường xác thực.
    /// </summary>
    public required string KeyId { get; init; }

    /// <summary>
    /// SHA-256 của phần bí mật, dạng hex thường.
    ///
    /// ⚠ **Không lưu khoá trần, kể cả "để tiện tra cứu".** Repo này đã một lần phải gỡ
    /// credential thật ra khỏi 5 file đang được git theo dõi (`0e602c3`) — tự đẻ ra một
    /// bảng chứa khoá trần là mời đúng sự cố đó quay lại, lần này ở chỗ không ai quét.
    ///
    /// ⚠ **SHA-256 là ĐỦ ở đây, và đây là chỗ người review sẽ hỏi:** bcrypt/argon2 sinh ra
    /// để chống dò cạn mật khẩu do NGƯỜI đặt (entropy thấp). Bí mật này là 32 byte ngẫu
    /// nhiên từ <see cref="RandomNumberGenerator"/> — không gian tìm kiếm 2^256, nên làm
    /// chậm hàm băm không mua thêm được gì. Đổi sang KDF khi nào khoá do người GÕ RA.
    /// </summary>
    public required string SecretHash { get; init; }

    /// <summary>
    /// Nhãn để người vận hành biết mình đang thu hồi cái gì — ví dụ
    /// <c>"connector Jira của ezCloud"</c>. Không dùng để phân quyền.
    /// </summary>
    public required string Label { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Thu hồi. <c>null</c> = còn hiệu lực.
    ///
    /// Cố ý là MỘT CỘT chứ không phải xoá dòng: `C1-1` đòi *"thu hồi được riêng"*, và một
    /// dòng đã xoá không trả lời được câu *"khoá bị lộ hôm ấy đã gọi những gì"*. Cùng tinh
    /// thần `K-B3` — không ghi đè quá khứ.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    public bool ConHieuLuc => RevokedAt is null;
}

/// <summary>
/// Sinh, tách và băm khoá API. Thuần tính toán, không chạm hạ tầng — nên bộ test của nó
/// chạy ở đâu cũng được (`IM-18`).
///
/// Hình dạng khoá: <c>kp_&lt;keyId&gt;_&lt;secret&gt;</c>
/// <code>
///   kp          tiền tố cố định, để nhìn một cái là biết chuỗi này là khoá của sản phẩm
///               nào — và để máy quét bí mật nhận ra nó (scripts/quet_credential_repo.py)
///   keyId       12 ký tự hex, CÔNG KHAI, dùng để tra dòng
///   secret      32 byte ngẫu nhiên, mã base64url, CHỈ hiện ra đúng một lần lúc sinh
/// </code>
/// </summary>
public static class ApiKeyMaterial
{
    public const string TienTo = "kp";

    /// <summary>Số byte ngẫu nhiên của phần bí mật. 32 byte = 256 bit.</summary>
    private const int SoByteBiMat = 32;

    /// <summary>Số byte của phần công khai. 6 byte = 12 ký tự hex.</summary>
    private const int SoByteKeyId = 6;

    /// <summary>
    /// Sinh một khoá mới. Trả về cả chuỗi đầy đủ (đưa cho khách, KHÔNG lưu) lẫn hai phần
    /// để lưu xuống bảng.
    /// </summary>
    public static (string KeyId, string SecretHash, string KhoaDayDu) Sinh()
    {
        var keyId = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(SoByteKeyId));
        var secret = Base64Url(RandomNumberGenerator.GetBytes(SoByteBiMat));

        return (keyId, BamBiMat(secret), $"{TienTo}_{keyId}_{secret}");
    }

    /// <summary>
    /// Tách một chuỗi khoá thành hai phần. Trả <c>false</c> nếu không đúng hình dạng —
    /// và chỗ gọi phải trả lời y hệt như khi khoá SAI, không được nói ra rằng nó sai ở
    /// bước tách: hai câu trả lời khác nhau là một kênh dò khoá.
    /// </summary>
    public static bool TryTach(string? raw, out string keyId, out string secret)
    {
        keyId = string.Empty;
        secret = string.Empty;

        if (string.IsNullOrWhiteSpace(raw)) return false;

        // Tách ĐÚNG 3 phần: phần bí mật là base64url nên tự nó CHỨA ĐƯỢC dấu gạch dưới.
        // Tách hết rồi lấy phần cuối là sai — nó cắt cụt đúng những khoá có '_' bên trong,
        // tức hỏng ngẫu nhiên với khoảng một nửa số khoá sinh ra.
        var phan = raw.Split('_', 3);
        if (phan.Length != 3) return false;
        if (!string.Equals(phan[0], TienTo, StringComparison.Ordinal)) return false;
        if (phan[1].Length == 0 || phan[2].Length == 0) return false;

        keyId = phan[1];
        secret = phan[2];
        return true;
    }

    public static string BamBiMat(string secret) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    /// <summary>
    /// So hash theo thời gian hằng số. So bằng <c>==</c> thoát sớm ở ký tự đầu khác nhau,
    /// và thời gian đó đo được — cùng lý do đã viết ở <c>SignalKeyEndpointFilter</c>.
    /// </summary>
    public static bool HashKhop(string hashA, string hashB) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hashA),
            Encoding.UTF8.GetBytes(hashB));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
