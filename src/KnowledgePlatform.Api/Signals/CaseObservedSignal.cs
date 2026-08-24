namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Một tín hiệu: "ở hệ thống nguồn có một việc đang được xử lý".
///
/// `06` §1 liệt kê các tín hiệu thật: *issue mới · người dùng đổi trạng thái ·
/// người dùng hỏi về tài liệu*. Đây là loại **thứ nhất**, và cố ý chỉ có một loại:
/// thêm loại nào thì thêm khi có chỗ dùng, không phát minh trước một "envelope
/// tín hiệu tổng quát" (§6.7, `G11`).
///
/// ⚠ `G1`: Case KHÔNG phụ thuộc Jira. Nguồn đi vào <see cref="SourceReference"/>
/// dưới dạng chuỗi — <c>"jira:ES-1234"</c>, <c>"crm:deal/4471"</c>, <c>"zalo:..."</c>.
/// Không có field nào của DTO này biết Jira là gì, và đó là chủ đích.
/// </summary>
/// <param name="SourceReference">
/// Khoá của bản ghi ở hệ thống nguồn. Đây là thứ làm tín hiệu **lặp lại được mà
/// không sinh case trùng**: unique index <c>(TenantId, SourceReference)</c> đã có
/// từ migration đầu, nên gửi cùng một tín hiệu mười lần vẫn ra một case.
/// </param>
/// <param name="Subject">Việc gì cần xử lý, ở dạng chữ (v0.2 §6.6).</param>
/// <param name="SourceCreatedAt">Lúc bản ghi được tạo ở nguồn. Có thể thiếu.</param>
/// <param name="SourceResolvedAt">
/// Lúc việc được xử lý xong ở nguồn. <c>null</c> = chưa xong.
/// Path A cần field này để chọn "case CŨ" — case chưa xong thì chưa có gì để học.
/// </param>
public sealed record CaseObservedSignal(
    string SourceReference,
    string Subject,
    DateTimeOffset? SourceCreatedAt,
    DateTimeOffset? SourceResolvedAt);

/// <summary>Kết quả xử lý một tín hiệu.</summary>
/// <param name="Created">
/// <c>false</c> nghĩa là case đã tồn tại từ trước. Đây KHÔNG phải lỗi — tín hiệu
/// đến hai lần là chuyện bình thường của mọi hệ thống tích hợp, và trả lỗi cho nó
/// sẽ làm bên gửi phải tự nhớ mình đã gửi gì.
/// </param>
public sealed record CaseSignalResult(string SourceReference, Guid CaseId, bool Created);

/// <param name="Received">Tổng số tín hiệu nhận được — để bên gửi đối chiếu, không phải đoán.</param>
/// <param name="Created">Số case mới tạo.</param>
public sealed record CaseSignalBatchResult(
    int Received,
    int Created,
    IReadOnlyList<CaseSignalResult> Results);
