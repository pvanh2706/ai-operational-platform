namespace KnowledgePlatform.Domain.Evidence;

/// <summary>
/// Mã khách sạn của một mẩu evidence — `AR-l`, chốt 2026-09-15 (phiếu `docs/14` §C3 = C3-1):
/// **bên gửi KHAI, hệ thống KHÔNG suy.**
///
/// 🛑 **VÌ SAO KHÔNG SUY, và đây là chỗ đã đo chứ không phải cẩn thận thừa.** Nhánh trông
/// hợp lý nhất là đọc mã từ tiêu đề ticket. Đo trên 32 case thật: chỉ 8/32 có mã trong tiêu
/// đề (25%), và trong phần đó tên tiêu đề còn LỆCH với thân bài — `ES-346622` ghi "Mariha"
/// trong khi email là <c>mirahhotel.sales@</c>; `ES-346618/619` ghi "Mirah Hotel" trong khi
/// thân bài ghi "Thành Danh Hotel". Sub-tenant là ranh giới BẢO MẬT, nên suy sai không phải
/// một nhãn sai — nó là dữ liệu của khách sạn A nằm trong SOP của khách sạn B.
///
/// 🛑 **BA TRẠNG THÁI, VÀ ĐỪNG GỘP CHÚNG.** Đây là điểm quan trọng nhất của file này:
/// <list type="bullet">
///   <item><c>NULL</c> — mẩu được nạp TRƯỚC khi có luật này. Không ai từng được hỏi.</item>
///   <item><see cref="KhongXacDinh"/> — bên gửi ĐÃ được hỏi và trả lời "chưa biết".</item>
///   <item><see cref="KhongThuocKhachSan"/> — bên gửi khẳng định mẩu này KHÔNG thuộc khách
///         sạn nào: việc nội bộ ezCloud (`ES-346594`), ezGolf (`ES-346764`), hoặc một chuỗi
///         nhiều cơ sở (`ES-346615`) chưa tách được.</item>
/// </list>
/// Gộp <c>NULL</c> với <c>khong-xac-dinh</c> là xoá mất thông tin "ai chưa bao giờ được hỏi",
/// và đó đúng là thứ cần biết để trả lời câu "đã mở lại được chưa".
///
/// ⚠ **HAI GIÁ TRỊ DÀNH RIÊNG KHÔNG TÍNH VÀO PHÉP ĐẾM MỞ LẠI.** `AR-l` đặt điều kiện mở lại
/// là *"một nguồn cho ra ≥2 giá trị PHÂN BIỆT"*. Nếu sentinel được tính thì chỉ cần đổi từ
/// <c>khong-xac-dinh</c> sang <c>khong-thuoc-khach-san</c> là "đạt điều kiện" — trong khi
/// chẳng khách sạn nào được phân biệt với khách sạn nào. Đó chính là hình dạng lỗi mà
/// `Mã khách sạn = -1.0` (32/32 case) và `machineReadability = High` (128/128 mẩu) đã dạy
/// HAI LẦN trong một ngày: **đếm số giá trị PHÂN BIỆT, không đếm độ phủ.**
/// </summary>
public static class SubTenantCodes
{
    /// <summary>Bên gửi đã được hỏi và trả lời: chưa biết mẩu này thuộc khách sạn nào.</summary>
    public const string KhongXacDinh = "khong-xac-dinh";

    /// <summary>
    /// Bên gửi khẳng định mẩu này không thuộc khách sạn nào — việc nội bộ, hoặc một chuỗi
    /// nhiều cơ sở chưa tách được. KHÁC HẲN <see cref="KhongXacDinh"/>: đây là một khẳng
    /// định, kia là một chỗ trống đã được thừa nhận.
    /// </summary>
    public const string KhongThuocKhachSan = "khong-thuoc-khach-san";

    public const int MaxLength = 64;

    /// <summary>Để thông báo lỗi nói thẳng thay vì bắt bên gửi đoán.</summary>
    public static string GiaTriDanhRieng => $"{KhongXacDinh} · {KhongThuocKhachSan}";

    /// <summary>
    /// Giá trị dành riêng thì KHÔNG phân biệt được khách sạn nào với khách sạn nào, nên
    /// không được tính vào điều kiện mở lại của `AR-l`.
    /// </summary>
    public static bool LaGiaTriDanhRieng(string? value) =>
        string.Equals(value, KhongXacDinh, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, KhongThuocKhachSan, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Chuẩn hoá giá trị bên gửi khai. Trả <c>false</c> khi KHÔNG khai hoặc khai rỗng —
    /// và đó là 400, KHÔNG phải âm thầm về <see cref="KhongXacDinh"/>.
    ///
    /// 🛑 Vì sao bỏ trống KHÔNG được tự hiểu thành <c>khong-xac-dinh</c>: hai thứ đó khác
    /// nhau đúng ở chỗ quan trọng nhất — một bên là bên gửi đã cân nhắc, một bên là bên gửi
    /// chưa đọc tài liệu. Tự điền hộ là xoá mất khác biệt đó ngay tại cửa vào, và về sau
    /// không có cách nào dựng lại.
    /// </summary>
    public static bool TryChuanHoa(string? raw, out string value)
    {
        value = string.Empty;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var trimmed = raw.Trim();
        if (trimmed.Length > MaxLength) return false;

        // Giá trị dành riêng về đúng dạng kebab dù bên gửi viết hoa — hai dạng viết của
        // CÙNG một sentinel mà nằm thành hai dòng khác nhau là tự thổi phép đếm phân biệt.
        if (string.Equals(trimmed, KhongXacDinh, StringComparison.OrdinalIgnoreCase))
        {
            value = KhongXacDinh;
            return true;
        }

        if (string.Equals(trimmed, KhongThuocKhachSan, StringComparison.OrdinalIgnoreCase))
        {
            value = KhongThuocKhachSan;
            return true;
        }

        // Mã thật thì GIỮ NGUYÊN VĂN (v0.2 R5: connector giữ nguyên văn nguồn). Không hạ
        // chữ thường, vì mã của khách có thể phân biệt hoa thường mà ta không biết. Phép
        // ĐẾM phân biệt thì so không phân biệt hoa thường — xem SubTenantCoverage.
        value = trimmed;
        return true;
    }
}
