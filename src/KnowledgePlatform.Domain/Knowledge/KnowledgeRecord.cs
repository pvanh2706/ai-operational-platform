using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Knowledge;

/// <summary>
/// Tổ chức khẳng định gì, áp dụng cho tình huống nào.
///
/// T1: MỘT KnowledgeRecord = MỘT NGUYÊN NHÂN / CƠ CHẾ, kèm cách nhận ra nó.
///     Identity là NGUYÊN NHÂN — không phải một luật, không phải một assertion.
///     Lý do: nguyên nhân là thứ BỀN nhất (tín hiệu đổi khi log đổi format;
///     cách xử lý đổi theo version; "parser < 2.3 không hỗ trợ payload dạng X"
///     sống lâu). D5: đặt identity vào thứ bền nhất, không vào thứ dễ mục.
///
/// Hình dạng đầy đủ: docs/04_KNOWLEDGE_MODEL_V0.1.md §3C.5
/// </summary>
public sealed class KnowledgeRecord : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid TenantId { get; init; }

    /// <summary>
    /// Tên nguyên nhân — IDENTITY của record (T1). Ví dụ: "Parser dưới 2.3
    /// bỏ qua payload OTA dạng X".
    ///
    /// ⚠ Nội dung chi tiết KHÔNG nằm ở đây mà ở assertion
    /// <see cref="AssertionKind.CauseExists"/> — vì phát biểu "nguyên nhân này
    /// tồn tại" cũng cần origin/evidence/verification riêng như mọi phát biểu khác
    /// (§3.4 ví dụ A1: VERIFIED, git commit + source code).
    /// </summary>
    public required string CauseName { get; set; }

    /// <summary>T3 — đúng hai type cho MVP, mỗi type có ca thật.</summary>
    public required KnowledgeType Type { get; init; }

    /// <summary>Cụm assertion tạo nên record này. §3C.5.</summary>
    public List<Assertion> Assertions { get; init; } = [];

    // --- LIFECYCLE: mức RECORD (V2), chỉ BA giá trị được lưu (V3) ---

    /// <summary>
    /// V2: lifecycle gắn ở mức RECORD, không per assertion — vì S7 đã quyết
    /// duyệt nội dung và mở quyền xem là MỘT hành động, và §6.4 là ràng buộc
    /// cứng (bắt người duyệt bấm từng assertion là đúng cái đã làm field
    /// "Version đang sử dụng" trống 100/100).
    /// </summary>
    public StoredLifecycleState Lifecycle { get; private set; } = StoredLifecycleState.Draft;

    /// <summary>
    /// Lần duyệt gần nhất. S7: duyệt nội dung + mở quyền xem trong MỘT hành động,
    /// và log cả hai.
    /// </summary>
    public KnowledgeApproval? LastApproval { get; private set; }

    // --- VISIBILITY: mức RECORD (S7 / §1.10) ---

    /// <summary>
    /// Nhãn visibility hiện tại của record.
    ///
    /// ⚠ TẬP GIÁ TRỊ CHƯA ĐƯỢC KHÓA Ở BẤT KỲ QUYẾT ĐỊNH NÀO. Cố ý để dạng chuỗi
    /// đục thay vì tự phát minh một enum ở tầng code — đó chính là cách §6.9
    /// (vocabulary song song) tái phát. Khoá tập giá trị này là một quyết định
    /// domain, không phải quyết định implementation. Xem AR-a ở 07.
    ///
    /// Quy tắc S7 áp lên nó: mặc định = HẸP NHẤT trong các nguồn; mở rộng phải là
    /// hành vi tường minh của người thấy được TẤT CẢ nguồn; hệ thống KHÔNG BAO GIỜ tự mở.
    /// </summary>
    public string? VisibilityScope { get; private set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // =========================================================================
    //  STATE SUY RA — V3. KHÔNG lưu, KHÔNG thêm cột cờ.
    //  Nguyên tắc dùng lần thứ tư (L4 → AP3 → V3 → PR1):
    //  "Nếu một thông tin chỉ đúng khi suy từ thứ khác, thì nó là PHÉP CHIẾU,
    //   không phải dữ liệu độc lập."
    // =========================================================================

    /// <summary>
    /// NEEDS_REVIEW được KÍCH HOẠT, không phải ai đó tự chọn (V3).
    /// Một tri thức có assertion vừa bị bác bỏ sẽ KHÔNG nằm im ở Active
    /// cho tới khi có người để ý.
    ///
    /// ⚠ Slice này hiện thực 3 trong 5 trigger của V3. Hai trigger còn lại cần
    /// thứ chưa có trong slice:
    ///   · quan hệ CONTRADICTS tới record khác  → cần L4 (KnowledgeRelation)
    ///   · một nguồn chống lưng bị đổi/xoá      → cần theo dõi thay đổi nguồn
    /// Ghi rõ ở đây để không ai tưởng là đã đủ.
    /// </summary>
    public bool NeedsReview =>
        Lifecycle == StoredLifecycleState.Active &&
        Assertions.Any(a =>
            a.IsCurrent &&
            (a.Verification is VerificationLevel.Invalidated or VerificationLevel.Conflicting
             || (LastApproval is not null && a.CreatedAt > LastApproval.ApprovedAt)));

    /// <summary>
    /// SUPERSEDED suy ra từ quan hệ "A SUPERSEDES nó" (L4) — KHÔNG lưu.
    ///
    /// ⚠ Slice này CHƯA có KnowledgeRelation (L4 ngoài phạm vi Path A), nên giá
    /// trị này luôn false. Để lại như một điểm nối tường minh thay vì bỏ trống,
    /// và để cho thấy nó là phép chiếu chứ không phải cột dữ liệu.
    /// V3: SUPERSEDED thắng NEEDS_REVIEW khi cả hai cùng đúng.
    /// </summary>
    public bool IsSuperseded => false;

    /// <summary>
    /// Nhãn hiển thị cho người dùng — gộp state lưu và state suy ra.
    /// V3: NeedsReview KHÔNG rút tri thức khỏi retrieval, nó GẮN CỜ. Rút đi thì
    /// mất đúng giá trị sản phẩm đang bán (§6.3), và trái G4 — bày chỗ tranh chấp
    /// ra chính là điều người duyệt cần (cùng triết lý S8).
    /// </summary>
    public string DisplayState =>
        IsSuperseded ? "SUPERSEDED"
        : NeedsReview ? "NEEDS_REVIEW"
        : Lifecycle.ToString().ToUpperInvariant();

    // =========================================================================
    //  Chuyển trạng thái
    // =========================================================================

    /// <summary>
    /// Duyệt: nội dung + quyền xem trong MỘT hành động, log cả hai (S7).
    ///
    /// D4: người CÔNG NHẬN, không phải AI. Không có đường nào để AI gọi hàm này.
    /// S7: người duyệt phải là người thấy được TẤT CẢ nguồn — điều đó được kiểm
    /// ở tầng ứng dụng trước khi gọi vào đây (không phải ở DB, vì Q-D còn mở).
    /// </summary>
    public void Approve(string approvedByActor, string visibilityScope, string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedByActor);
        ArgumentException.ThrowIfNullOrWhiteSpace(visibilityScope);

        LastApproval = new KnowledgeApproval
        {
            ApprovedByActor = approvedByActor,
            ApprovedAt = DateTimeOffset.UtcNow,
            VisibilityScopeBefore = VisibilityScope,
            VisibilityScopeAfter = visibilityScope,
            Reason = reason,
        };

        VisibilityScope = visibilityScope;
        Lifecycle = StoredLifecycleState.Active;
    }

    /// <summary>
    /// Rút tri thức, không có bản thay thế.
    /// V4 ca (a): tri thức VẪN ĐÚNG nhưng không còn ai gặp (ví dụ không còn khách
    /// nào chạy bản dưới 2.3) → vào đây, và verification của các assertion
    /// KHÔNG đổi. Chính ca này là bằng chứng việc tách hai trục kiếm được chỗ đứng:
    /// một trục thì buộc gắn INVALIDATED cho một phát biểu vẫn đúng.
    /// </summary>
    public void Deprecate() => Lifecycle = StoredLifecycleState.Deprecated;
}

/// <summary>
/// Một lần duyệt. S7 đòi ba chỗ chứa, đây là chỗ thứ ba:
/// ai mở rộng quyền xem, khi nào, từ đâu tới đâu.
/// </summary>
public sealed class KnowledgeApproval
{
    public required string ApprovedByActor { get; init; }
    public required DateTimeOffset ApprovedAt { get; init; }

    /// <summary>NULL nếu đây là lần duyệt đầu (record còn ở Draft).</summary>
    public string? VisibilityScopeBefore { get; init; }

    public required string VisibilityScopeAfter { get; init; }

    public string? Reason { get; init; }
}
