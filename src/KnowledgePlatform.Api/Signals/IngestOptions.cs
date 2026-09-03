namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Cấu hình cho đường nhận tín hiệu (Kênh 1, `06` §1).
///
/// ⚠ Endpoint tín hiệu là endpoint **GHI**, và đó là khác biệt quan trọng so với
/// `/internal/tenant-boundary` (chỉ đọc). Một endpoint ghi không xác thực nghĩa là
/// bất kỳ ai cũng bơm được case giả vào dữ liệu của khách hàng — làm sai kho tri
/// thức, và làm sai luôn `M2` (thước đo chính của tháng đầu).
///
/// `AR-e` (xác thực người gọi) còn OPEN, nên đây **chưa phải** câu trả lời cho nó.
/// Đây là cái chốt nhỏ nhất có thể để endpoint không mở toang trong lúc chờ:
/// một khoá dùng chung, so sánh theo thời gian hằng số.
/// </summary>
public sealed class IngestOptions
{
    public const string SectionName = "Ingest";

    /// <summary>
    /// Khoá mà phần mềm của khách phải gửi kèm tín hiệu. Không có khoá và không
    /// thừa nhận tường minh (xem <see cref="AcknowledgeUnauthenticatedSignalEndpoint"/>)
    /// thì ứng dụng **không khởi động được**.
    /// </summary>
    public string? SignalApiKey { get; set; }

    /// <summary>
    /// Cho phép chạy endpoint tín hiệu KHÔNG có khoá. Dùng cho máy dev và test.
    ///
    /// Giống <c>Tenancy:AcknowledgeUnauthenticatedTenantHeader</c> (`IM-13`): cờ này
    /// không bảo vệ gì cả — nó chỉ làm việc mở một endpoint ghi không xác thực trở
    /// thành **quyết định** thay vì **sơ suất**.
    /// </summary>
    public bool AcknowledgeUnauthenticatedSignalEndpoint { get; set; }

    public string ApiKeyHeader { get; set; } = "X-Signal-Key";

    /// <summary>
    /// Trần số tín hiệu trong một request. Vượt là **từ chối cả request** kèm nói rõ
    /// trần là bao nhiêu — cố ý KHÔNG cắt bớt im lặng, vì cắt bớt im lặng đọc ra
    /// thành "đã nạp hết" trong khi không phải.
    /// </summary>
    public int MaxSignalsPerRequest { get; set; } = 500;

    /// <summary>
    /// Trần số mẩu evidence trong một request. Trần RIÊNG, không dùng chung với
    /// <see cref="MaxSignalsPerRequest"/>: một tín hiệu case là bốn trường ngắn, còn
    /// một mẩu evidence mang cả nội dung comment — cùng con số 500 nghĩa là hai kích
    /// thước body rất khác nhau. Gộp làm một là để con số đúng cho loại này thành
    /// con số sai cho loại kia.
    ///
    /// Vượt là **từ chối cả lô** kèm nói rõ trần, KHÔNG cắt bớt — cùng lý do `IM-16`.
    /// </summary>
    public int MaxEvidencePerRequest { get; set; } = 500;
}
