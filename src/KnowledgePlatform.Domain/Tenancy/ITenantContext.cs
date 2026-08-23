namespace KnowledgePlatform.Domain.Tenancy;

/// <summary>
/// Tenant của yêu cầu đang xử lý.
///
/// G13 (AGENT.md §3.10): tenant LUÔN được xác định từ cấu hình hoặc ngữ cảnh
/// request, KHÔNG BAO GIỜ từ hằng số toàn cục.
///
/// Vì sao là interface được inject chứ không phải một static/singleton: đó chính
/// là ranh giới giữa hai chế độ deploy mà G13 đòi.
///   · shared multi-tenant     → cài đặt đọc tenant từ tín hiệu của host app
///   · dedicated single-tenant → cài đặt đọc tenant từ cấu hình, một giá trị
///
/// Cùng codebase, cùng schema, khác một lớp cài đặt. Nếu chỗ nào trong hệ thống
/// đọc tenant từ một biến tĩnh, chế độ thứ hai vẫn chạy nhưng chế độ thứ nhất
/// rò rỉ dữ liệu — và đó là lỗi không báo.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Tenant hiện tại. Ném nếu chưa được xác định — thà lỗi sớm và ồn còn hơn
    /// chạy một truy vấn không có ranh giới.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>Đã xác định được tenant chưa (dùng cho health check, migration, job hệ thống).</summary>
    bool IsResolved { get; }
}
