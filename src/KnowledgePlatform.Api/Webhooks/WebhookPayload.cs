namespace KnowledgePlatform.Api.Webhooks;

/// <summary>
/// Hình dạng gói gửi về phần mềm của khách — **hợp đồng tích hợp**, không phải chi tiết nội bộ.
///
/// ⚠ Đổi hình dạng này là đổi thứ bên thứ ba đang phụ thuộc. Có <see cref="PhienBan"/> để
/// đổi được mà không làm chết bên đang cắm: thêm ô thì giữ nguyên số, bỏ/đổi nghĩa một ô thì
/// tăng số.
/// </summary>
/// <param name="PhienBan">Phiên bản hợp đồng. Bên nhận nên từ chối số mình chưa biết.</param>
/// <param name="MaGoi">
/// Khoá **idempotency** (`D1-1`). Gửi lại do retry mang ĐÚNG mã này — bên nhận thấy mã đã xử
/// lý thì bỏ qua, không sinh bản thứ hai.
/// </param>
/// <param name="BietTraLoi">
/// <c>false</c> là một câu trả lời HỢP LỆ (`D4-1` + `G3.4`), không phải lỗi. Đừng đọc nó
/// thành "hệ thống hỏng" — đọc thành "chưa có tri thức nào được duyệt cho ca này".
/// </param>
/// <param name="LyDoKhongBiet">
/// Cửa nào đã chặn: <c>dinh-tuyen-khong-biet</c> (không xác định được nhóm) hay
/// <c>nhom-chua-co-sop-duoc-duyet</c> (đúng nhóm nhưng nhóm chưa có SOP được người duyệt).
/// Hai thứ rất khác nhau về việc phải làm gì tiếp.
/// </param>
/// <param name="DaQuaCongChe">
/// Thêm 2026-10-10 (`N1-2`). <c>false</c> nghĩa là bản deploy đang bật cờ tắt-tạm cổng che:
/// tóm tắt và tiêu đề trong gói này là NGUYÊN VĂN, có thể chứa mật khẩu/token thật. Thêm ô nên
/// giữ <see cref="PhienBan"/> = "1" (`docs/19` §6).
/// </param>
public sealed record WebhookPayload(
    string PhienBan,
    Guid MaGoi,
    DateTimeOffset Luc,
    WebhookCase Case,
    bool BietTraLoi,
    string? Nhom,
    string NguonDinhTuyen,
    string? LyDoKhongBiet,
    IReadOnlyList<WebhookHuongDan> HuongDan,
    bool DaQuaCongChe)
{
    public const string PhienBanHienTai = "1";

    /// <summary>
    /// Thêm 2026-10-10 (P5): case đang ở BƯỚC nào của quy trình đã duyệt. Chỉ có mặt khi case khớp được một
    /// quy trình — thiếu ô này KHÔNG phải "quy trình rỗng". Thêm ô nên giữ phiên bản "1" (`docs/19` §6).
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public WebhookQuyTrinh? QuyTrinh { get; init; }

    /// <summary>
    /// Thêm 2026-10-10 (P6, ô `J`): mã của LƯỢT GỢI Ý này — gửi lại vào <c>POST /goi-y/{id}/phan-hoi</c> để khai
    /// "đã nhận / đã dùng". ⚠ KHÁC <see cref="MaGoi"/>: <c>maGoi</c> là khoá chống trùng của lượt GỬI (giữ nguyên
    /// qua các lần thử lại), còn mã này đổi theo từng lần thử — mỗi lần thử là một câu trả lời dựng lại.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Guid? MaLuotGoiY { get; init; }

    /// <summary>
    /// Thêm 2026-10-10 (P7): vì sao có gói này — <c>case-moi</c> (có việc mới ở nguồn) hay <c>buoc-hien-tai-doi</c>
    /// (dữ liệu vừa nhận làm case bước sang bước khác; <see cref="QuyTrinh"/> mang bước MỚI).
    /// </summary>
    public string SuKien { get; init; } = "case-moi";
}

/// <param name="TraTheo"><c>buoc</c> = hướng dẫn tra theo bước hiện tại · <c>nhom</c> = rơi về mức nhóm.</param>
public sealed record WebhookQuyTrinh(string Ten, int PhienBan, WebhookBuoc? BuocHienTai, bool DaXongHet, string TraTheo);

public sealed record WebhookBuoc(string Ma, string NoiDung, string? NoiXem);

public sealed record WebhookCase(string KhoaNguon, string TieuDe);

/// <summary>
/// Một hướng dẫn: **tóm tắt + link** — đúng `D3-1`.
///
/// ⚠ Cố ý KHÔNG mang toàn văn. Hai lý do, và lý do thứ hai mới là lý do thật:
/// <list type="number">
///   <item>gói gửi đi nằm lại trong hệ thống của bên nhận (comment Jira, log của họ) —
///     càng ít nội dung ra ngoài càng ít bề mặt;</item>
///   <item>bản đầy đủ đọc qua link thì mỗi lượt đọc đi qua cổng che và cổng trạng thái
///     **tại thời điểm đọc**. Nhét toàn văn vào gói là đóng băng một bản chưa chắc còn
///     đúng, ở một nơi không thu hồi được.</item>
/// </list>
/// </summary>
/// <param name="TrangThai">
/// <c>NEEDS_REVIEW</c> nghĩa là bản này đã có người sửa và chưa duyệt lại. Nó vẫn được gửi —
/// `V3` gắn cờ chứ không rút, vì người đang xử lý ticket cần bản tốt nhất hiện có KÈM cảnh báo.
/// </param>
/// <param name="Link">
/// Có hạn và thu hồi được (`D5-3`). <c>null</c> nghĩa là chưa phát được link — thường vì
/// thiếu <c>Link:PublicBaseUrl</c>; khi đó tóm tắt vẫn đi, chỉ mất đường đọc đầy đủ.
/// </param>
/// <param name="TicketDanChung">
/// Thêm 2026-10-10 (P5, ô `I`): ticket nào chống lưng hướng dẫn này. Chỉ khoá nguồn. Rỗng nghĩa là chưa
/// nối được dẫn chứng nào (bản ghi nạp trước P3, hoặc ticket trích không có trong kho).
/// </param>
public sealed record WebhookHuongDan(
    Guid Id,
    string TenNguyenNhan,
    string TrangThai,
    string TomTat,
    string? Link,
    DateTimeOffset? LinkHetHanLuc,
    IReadOnlyList<string> TicketDanChung);
