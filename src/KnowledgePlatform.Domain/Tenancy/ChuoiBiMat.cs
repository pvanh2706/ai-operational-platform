using System.Security.Cryptography;
using System.Text;

namespace KnowledgePlatform.Domain.Tenancy;

/// <summary>
/// Sinh, tách và băm một chuỗi bí mật có tiền tố. Dùng chung cho **mọi** loại khoá/token
/// của sản phẩm — khoá API (`AR-e`) và token xem tri thức (`D5-3`).
///
/// ⚠ **Dùng chung là CÓ CHỦ ĐÍCH, không phải tiện tay.** Hình dạng này có một cái bẫy hỏng
/// NGẪU NHIÊN: phần bí mật mã base64url, mà base64url dùng <c>-</c> và <c>_</c> thay
/// <c>+</c> và <c>/</c> — nên **khoảng một nửa** số chuỗi sinh ra có dấu gạch dưới BÊN
/// TRONG. Viết lại phép tách ở chỗ thứ hai là mời cái bẫy đó quay lại ở chỗ chưa ai đột
/// biến thử. `IM-39` đã đo: tách sai thì ~50% số khoá trượt xác thực, ngẫu nhiên.
///
/// Hình dạng: <c>&lt;tiền tố&gt;_&lt;id&gt;_&lt;bí mật&gt;</c>
/// <code>
///   tiền tố   nhìn một cái là biết chuỗi này thuộc loại nào, và để máy quét bí mật
///             nhận ra nó (scripts/quet_credential_repo.py)
///   id        12 ký tự hex, CÔNG KHAI, dùng để tra dòng theo index
///   bí mật    32 byte ngẫu nhiên, base64url, CHỈ hiện ra đúng một lần lúc sinh
/// </code>
/// </summary>
public static class ChuoiBiMat
{
    private const int SoByteBiMat = 32;   // 256 bit
    private const int SoByteId = 6;       // -> 12 ký tự hex

    /// <summary>Sinh một chuỗi mới. Trả về hai phần để LƯU, cộng chuỗi đầy đủ để ĐƯA ĐI.</summary>
    public static (string Id, string Hash, string ChuoiDayDu) Sinh(string tienTo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tienTo);

        var id = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(SoByteId));
        var biMat = Base64Url(RandomNumberGenerator.GetBytes(SoByteBiMat));

        return (id, Bam(biMat), $"{tienTo}_{id}_{biMat}");
    }

    /// <summary>
    /// Tách một chuỗi đầy đủ. Trả <c>false</c> cho MỌI kiểu hỏng — chỗ gọi phải trả lời y
    /// hệt như khi chuỗi SAI: hai câu trả lời khác nhau là một kênh dò.
    /// </summary>
    public static bool TryTach(string? raw, string tienTo, out string id, out string biMat)
    {
        id = string.Empty;
        biMat = string.Empty;

        if (string.IsNullOrWhiteSpace(raw)) return false;

        // Tách ĐÚNG 3 phần. Tách hết rồi lấy phần cuối là sai — nó cắt cụt đúng những chuỗi
        // có '_' bên trong phần bí mật. Xem phần tóm tắt của lớp này.
        var phan = raw.Split('_', 3);
        if (phan.Length != 3) return false;
        if (!string.Equals(phan[0], tienTo, StringComparison.Ordinal)) return false;
        if (phan[1].Length == 0 || phan[2].Length == 0) return false;

        id = phan[1];
        biMat = phan[2];
        return true;
    }

    public static string Bam(string biMat) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(biMat)));

    /// <summary>
    /// So hash theo thời gian hằng số. So bằng <c>==</c> thoát sớm ở ký tự đầu khác nhau,
    /// và thời gian đó đo được — đủ để dò từng ký tự.
    /// </summary>
    public static bool HashKhop(string hashA, string hashB) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hashA),
            Encoding.UTF8.GetBytes(hashB));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
