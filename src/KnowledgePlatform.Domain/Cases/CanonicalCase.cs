using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Cases;

/// <summary>
/// Việc gì đang được xử lý. Canonical Case Model v0.2.
///
/// ⚠ ĐÂY LÀ BẢN MỎNG, cố ý. Slice đầu của Workstream 07 là Path A
/// (gom N case cũ → nháp SOP), nên Case chỉ cần đủ để TÌM và GOM.
/// Chưa có: CaseEvent, OwnershipSegment, WaitingSegment, CaseProblem,
/// CaseClaim, Classification, CaseAction, CaseOutcome, CaseRelation.
/// Thêm khi tới lượt — 06 §10 và luật §6.7 ("vừa đủ để build").
///
/// G1: Case KHÔNG phụ thuộc Jira. Jira Issue chỉ là biểu diễn ở nguồn ngoài.
/// </summary>
public sealed class CanonicalCase : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid TenantId { get; init; }

    /// <summary>
    /// Việc gì cần xử lý, ở dạng chữ. v0.2 dùng khái niệm <c>Subject</c> —
    /// và §6.6 đã kiểm chứng: một deal CRM cũng có Subject mà không có CaseProblem.
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Tham chiếu tới bản ghi ở hệ thống nguồn — "jira:ES-1234", "crm:deal/4471".
    /// v0.2: SourceReference. G1: nguồn là connector, không phải ranh giới sản phẩm.
    /// </summary>
    public required string SourceReference { get; set; }

    public DateTimeOffset? SourceCreatedAt { get; set; }
    public DateTimeOffset? SourceResolvedAt { get; set; }

    public DateTimeOffset IngestedAt { get; init; } = DateTimeOffset.UtcNow;
}
