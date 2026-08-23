using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Knowledge;

/// <summary>
/// Một phát biểu đơn lẻ bên trong một <see cref="KnowledgeRecord"/>.
///
/// ĐÂY LÀ CHỖ QUAN TRỌNG NHẤT CỦA MODEL. 04 §3.4 + §3C.5:
///   "KnowledgeRecord KHÔNG phải một khối văn bản. Nó là một CỤM ASSERTION
///    về cùng một subject, mỗi assertion mang evidence và verification riêng."
///
/// Bốn thứ gắn ở TỪNG assertion, không phải ở mức record:
///   T4   verification level
///   AP3  origin + actor
///   S8   evidence link
///
/// ⚠ 06 §10 mục 3: đây là chỗ DỄ SAI IM LẶNG NHẤT của cả dự án.
///   Gán sai <see cref="Origin"/> — ví dụ assertion do senior tự viết bị ghi
///   thành AiInference — là một LỖI PROVENANCE (vi phạm G6). Nó không làm
///   chương trình crash. Nó nằm im trong dữ liệu cho tới khi bộ eval phát hiện,
///   hoặc không ai phát hiện.
/// </summary>
public sealed class Assertion : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid TenantId { get; init; }

    public required Guid KnowledgeRecordId { get; init; }

    /// <summary>Assertion này nói về mặt nào của nguyên nhân.</summary>
    public required AssertionKind Kind { get; init; }

    /// <summary>
    /// Nội dung phát biểu. Với <see cref="AssertionKind.Applicability"/> thì đây
    /// chính là chỗ chứa "áp dụng cho bản dưới 2.3" — AP1 quyết định applicability
    /// là ASSERTION, KHÔNG phải field có cấu trúc (lý do: G12 + D5).
    /// </summary>
    public required string Content { get; set; }

    // --- PROVENANCE: gắn ở TỪNG assertion (AP3) ---

    /// <summary>
    /// Hình thành bằng cách nào. GIỮ VĨNH VIỄN — không rewrite sau khi người
    /// verify (v0.2 §7.4, K-B5). Một assertion do Path A gom vẫn mãi là
    /// AiInference, kể cả sau khi được duyệt; verification level mới là thứ đổi.
    /// </summary>
    public required Origin Origin { get; init; }

    /// <summary>
    /// Ai/cái gì đưa ra assertion này. Đây là "Authority" của S4 —
    /// V5 đã xác định nó là <c>Actor</c> của v0.2 §7, KHÔNG cần trục thứ ba.
    /// ⚠ KHÔNG mô hình hoá chức danh/mức chuyên môn ở đây (V5): quyền duyệt
    /// được quyết trên cơ sở khác — S7, người duyệt phải thấy được mọi nguồn.
    /// </summary>
    public string? ActorLabel { get; set; }

    /// <summary>Được xác minh tới mức nào. T4 — riêng từng assertion.</summary>
    public required VerificationLevel Verification { get; set; }

    /// <summary>Dẫn chứng chống lưng phát biểu NÀY (S8), với quan hệ riêng mỗi liên kết (L3).</summary>
    public List<AssertionEvidence> EvidenceLinks { get; init; } = [];

    // --- M2: giữ được cả bản AI và bản người sửa ---

    /// <summary>
    /// Nếu người duyệt sửa assertion này, bản gốc KHÔNG bị ghi đè — nó được
    /// đánh dấu là đã bị thay bởi assertion mới, và assertion mới mang
    /// <see cref="Origin"/> của người sửa.
    ///
    /// M2 cần <c>diff(bản nháp AI, bản đã duyệt)</c> vì nó vừa là thước đo
    /// tháng đầu vừa là NHÃN cho bộ eval (D6 flywheel). Ghi đè là phá cả hai.
    ///
    /// ⚠ Cố ý KHÔNG đặt tên "Supersedes" — "SUPERSEDES" là quan hệ
    /// Knowledge ↔ Knowledge của L4. Dùng lại từ đó ở đây sẽ tạo đúng bệnh
    /// §6.9 (vocabulary song song) mà workstream 04 đã mất ba lần để chữa.
    /// </summary>
    public Guid? ReplacedByAssertionId { get; set; }

    /// <summary>Assertion còn hiệu lực (chưa bị bản sửa nào thay).</summary>
    public bool IsCurrent => ReplacedByAssertionId is null;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Liên kết assertion ↔ evidence, CÓ THUỘC TÍNH TRÊN LIÊN KẾT.
///
/// L3 đòi "evidence riêng mỗi link" — đó là lý do đây là một entity chứ không
/// phải một quan hệ nhiều-nhiều trơn. Cũng chính là lý do AR1 chọn cơ sở dữ
/// liệu quan hệ: document store phải denormalize chỗ này.
/// </summary>
public sealed class AssertionEvidence : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid TenantId { get; init; }

    public required Guid AssertionId { get; init; }

    public required Guid EvidenceItemId { get; init; }

    /// <summary>Evidence chống lưng, phản bác, hay chỉ là ngữ cảnh (K-B9).</summary>
    public required EvidenceRelation Relation { get; init; }

    /// <summary>
    /// Ghi chú vì sao evidence này liên quan tới phát biểu này.
    /// Với bản nháp gom từ N case, đây là chỗ ghi "14/20 case làm bước này" —
    /// và S8 nói chính cái phân bố đó là thứ giá trị nhất, đừng ném đi.
    /// </summary>
    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
