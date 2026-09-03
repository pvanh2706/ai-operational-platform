using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Evidence;

/// <summary>
/// Ta quan sát được gì. Canonical Case v0.2 §9.
///
/// K-B3: Evidence KHÔNG phải Knowledge — nó gắn với một thời điểm và một nguồn.
/// K-B9: Evidence được phép trỏ TRỰC TIẾP vào Knowledge, không qua Case —
///       vì một email của senior hay một tin Zalo không thuộc case nào, và với
///       thực tế 60% fragment rải rác thì đó không phải trường hợp hiếm.
/// v0.2 §9: một EvidenceItem có thể liên quan NHIỀU Case — không mặc định
///       1 Evidence = thuộc độc quyền 1 Case.
///
/// Phạm vi slice này (Path A): chỉ đủ để gom từ case cũ. SourceDocument đầy đủ
/// (PDF/Word, AR4) chưa nằm trong slice này.
/// </summary>
public sealed class EvidenceItem : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid TenantId { get; init; }

    /// <summary>Nội dung quan sát được, ở dạng đọc được.</summary>
    public required string Content { get; set; }

    /// <summary>
    /// Nguồn gốc ở hệ thống ngoài — ví dụ "jira:ES-1234#comment-7", "email:...".
    /// v0.2 R5: connector giữ nguyên văn nguồn, việc dịch nghĩa thuộc lớp canonical
    /// và phải dịch lại được nếu sau phát hiện sai.
    /// </summary>
    public required string SourceReference { get; set; }

    /// <summary>
    /// Case mà evidence này được quan sát trong đó, nếu có.
    /// NULL là hợp lệ và quan trọng — đó là K-B9 (email/ghi chú rời không thuộc case nào).
    ///
    /// ⚠ **ĐỘ LỆCH CÓ CHỦ ĐÍCH so với v0.2 §9 — đọc trước khi sửa.**
    /// §9 nói *"một EvidenceItem có thể liên quan NHIỀU Case"*, còn field này chỉ giữ
    /// được MỘT. Đây là rút gọn ĐÃ CÂN NHẮC (`AR-f`, chốt 2026-08-30), không phải quên:
    /// Path A chỉ cần "evidence của case này là những gì", chưa cần một evidence phục
    /// vụ hai case. §6.7 — vừa đủ để build.
    ///
    /// Làm đúng §9 sẽ là một bảng liên kết <c>case_evidence</c> và bỏ field này. Khi nào
    /// tới lúc đó: khi có ca thật cần một mẩu evidence chống lưng cho hai case khác nhau.
    /// Ghi ở đây để người đọc sau phân biệt được "đã cân nhắc rồi rút gọn" với "bỏ sót".
    /// </summary>
    public Guid? ObservedInCaseId { get; set; }

    /// <summary>
    /// Máy đọc được đến mức nào. v0.2 §9. Ảnh chụp màn hình chưa OCR = thấp,
    /// và đó chính là trạng thái KNOWLEDGE_EXISTS_NOT_RETRIEVABLE ở §6.3.
    /// </summary>
    public MachineReadability MachineReadability { get; set; } = MachineReadability.Unknown;

    /// <summary>Thời điểm quan sát được (ở nguồn), khác thời điểm nạp vào hệ thống.</summary>
    public DateTimeOffset? ObservedAt { get; set; }

    public DateTimeOffset IngestedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>v0.2 §9 metadata. Dùng lại cho ba trạng thái coverage ở §6.3.</summary>
public enum MachineReadability
{
    Unknown = 0,

    /// <summary>Ví dụ: ảnh chưa OCR, ghi âm cuộc gọi.</summary>
    Low = 1,

    Medium = 2,

    /// <summary>Ví dụ: kết quả query Kibana, response API (§8.1-KQ B1/B2).</summary>
    High = 3,
}
