namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Tín hiệu loại **THỨ HAI** của Kênh 1: *"việc ở nguồn vừa đổi trạng thái"* (`F1-1`).
///
/// `06` §1 liệt kê ba loại tín hiệu thật: *issue mới · người dùng đổi trạng thái · người dùng
/// hỏi về tài liệu*. Loại thứ nhất đã có từ 2026-08-25; đây là loại thứ hai. Loại thứ ba
/// KHÔNG cần endpoint riêng — `F2-1` chốt dùng luôn <c>POST /hoi</c>.
///
/// 🛑 **Và nó CỐ Ý không làm gì ngoài cập nhật Case** — `F1-1`, chọn trên một SỐ ĐO chứ không
/// theo trực giác. Trực giác nói: *case đóng rồi thì gom tri thức từ nó.* Đo trên corpus 12
/// tháng thì ngược lại:
/// <code>
///   corpus 12 tháng   100% case đã đóng   ->  1,6 mẩu dùng được / case
///   corpus 4 ngày      22% case đã đóng   ->  2,5 mẩu dùng được / case
/// </code>
/// Case đã đóng **mỏng hơn** case còn mở, vì bước kết luận thường xảy ra ở nơi khác (phiên
/// remote, cuộc gọi) và chỉ để lại trong ticket một dòng *"Done nhé"*. Kích hoạt gom lúc đóng
/// case là build đúng cái bộ lọc mà dự án đã đo được là **vứt đi phần có giá trị** — lần thứ
/// ba cùng một hình dạng, sau *"chỉ lấy case đã đóng"* và *"chỉ lấy phần khách đọc được"*.
/// </summary>
/// <param name="SourceReference">Khoá của bản ghi ở hệ thống nguồn. Case phải đã tồn tại.</param>
/// <param name="DaXongONguon">
/// Trạng thái ở nguồn, dạng nhị phân.
///
/// ⚠ **Bắt buộc, và cố ý KHÔNG suy từ việc có hay không có <see cref="SourceResolvedAt"/>.**
/// Cùng luật với <c>subTenantCode</c> của `AR-l`: *"chưa gửi"* và *"đã mở lại"* đều cho ra một
/// ô trống, mà hai điều đó khác hẳn nhau và về sau không dựng lại được.
/// </param>
/// <param name="SourceResolvedAt">
/// Lúc việc được xử lý xong ở nguồn. Bỏ trống khi <see cref="DaXongONguon"/> là <c>true</c>
/// thì lấy thời điểm nhận tín hiệu — kèm chấp nhận rằng mốc ấy KHÔNG phải mốc thật ở nguồn.
/// </param>
public sealed record CaseStatusSignal(
    string SourceReference,
    bool? DaXongONguon,
    DateTimeOffset? SourceResolvedAt);

/// <summary>Chuyện gì đã xảy ra với một tín hiệu đổi trạng thái.</summary>
public static class CaseStatusOutcome
{
    public const string DaCapNhat = "da-cap-nhat";

    /// <summary>Trạng thái gửi tới TRÙNG với trạng thái đang có. Không phải lỗi.</summary>
    public const string KhongDoi = "khong-doi";

    /// <summary>
    /// Chưa từng có tín hiệu tạo cho khoá này.
    ///
    /// ⚠ CỐ Ý không tự tạo Case ở đây: một case dựng từ tín hiệu đổi trạng thái sẽ không có
    /// <c>Subject</c> — tức một dòng rỗng nghĩa nằm trong kho gom. Nói ra để bên gửi biết
    /// mình gửi thiếu, thay vì im lặng đẻ ra rác.
    /// </summary>
    public const string KhongThayCase = "khong-thay-case";
}

/// <param name="TrangThai">Một trong ba giá trị của <see cref="CaseStatusOutcome"/>.</param>
public sealed record CaseStatusResult(string SourceReference, Guid? CaseId, string TrangThai);

/// <param name="Received">Tổng số tín hiệu nhận được — để bên gửi đối chiếu, không phải đoán.</param>
/// <param name="DaCapNhat">Số case thật sự đổi.</param>
/// <param name="KhongThayCase">Số khoá nguồn chưa từng được tạo. Khác 0 là bên gửi gửi thiếu.</param>
public sealed record CaseStatusBatchResult(
    int Received,
    int DaCapNhat,
    int KhongThayCase,
    IReadOnlyList<CaseStatusResult> Results);
