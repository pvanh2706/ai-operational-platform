namespace KnowledgePlatform.Domain.Knowledge;

// =============================================================================
//  TỪ VỰNG ĐÃ KHÓA — nguồn duy nhất: docs/04_KNOWLEDGE_MODEL_V0.1.md §3D.7
//
//  KHÔNG thêm, bớt, hay đổi tên giá trị nào ở đây mà không sửa §3D.7 trước.
//  §6.9 (vocabulary song song) đã tái phát BA lần trong workstream 04 —
//  file này tồn tại để nó không tái phát lần thứ tư ở tầng code.
//
//  Tài liệu viết SCREAMING_SNAKE (AI_INFERENCE); C# viết PascalCase
//  (AiInference). Đó là khác biệt cách viết, KHÔNG phải giá trị mới.
// =============================================================================

/// <summary>
/// Thông tin này được hình thành bằng cách nào. Canonical Case v0.2 §7.1 — 5 giá trị.
/// Gắn ở TỪNG ASSERTION, không phải ở mức record (AP3).
/// Giữ VĨNH VIỄN, không rewrite sau khi người verify (v0.2 §7.4, K-B5).
/// </summary>
public enum Origin
{
    /// <summary>Hệ thống trực tiếp quan sát được, TRONG MỘT PHẠM VI XÁC ĐỊNH (v0.2 §7.6).</summary>
    SystemFact = 1,

    /// <summary>
    /// Người dùng khẳng định. v0.2 §7.5: đây KHÔNG phải sự thật khách quan —
    /// fact là "user đã nói vậy", không phải "điều đó đúng".
    /// </summary>
    UserConfirmed = 2,

    /// <summary>AI suy luận. Path A sinh ra assertion ở giá trị này.</summary>
    AiInference = 3,

    /// <summary>Một con người có chuyên môn tự đưa ra nhận định (ví dụ: email của senior — §2.3).</summary>
    HumanAssessment = 4,

    /// <summary>Một bên ngoài khẳng định, ta chỉ nhập vào (ví dụ: "vendor OTA nói rằng...").</summary>
    ImportedSourceAssertion = 5,
}

/// <summary>
/// Nhận định này được xác minh tới mức nào. Canonical Case v0.2 §7.3.
/// Gắn ở TỪNG ASSERTION (T4), không phải một con số cho cả record.
///
/// ⚠ KHÔNG phải một đường thẳng (V1). Bốn giá trị đầu là THANG đơn điệu tăng;
/// Conflicting và Invalidated nằm NGOÀI thang. Dùng <see cref="VerificationLevelExtensions.IsOnLadder"/>.
/// </summary>
public enum VerificationLevel
{
    // --- THANG, đơn điệu tăng ---
    Speculative = 1,

    /// <summary>⚠ Chưa có ca thật nào dùng giá trị này (V1). Giữ vì là kernel dùng chung (S4).</summary>
    Plausible = 2,

    Supported = 3,
    Verified = 4,

    // --- NGOÀI THANG ---

    /// <summary>
    /// Bằng chứng chỉ HAI HƯỚNG. KHÔNG phải "hơi tin".
    /// S8 làm giá trị này BẮT BUỘC: bản nháp gom từ N case luôn có chỗ các case
    /// không đồng ý, và chính chỗ đó là chỗ người duyệt cần nhìn.
    /// </summary>
    Conflicting = 10,

    /// <summary>Từng tin, nay bị bác. KHÔNG phải "rất không tin". Xem V4 ca (b).</summary>
    Invalidated = 11,
}

public static class VerificationLevelExtensions
{
    /// <summary>
    /// Bốn mức đầu nằm trên thang và so sánh được với nhau. Conflicting/Invalidated thì không —
    /// xếp hạng chúng như một mức tin trung bình sẽ làm chỗ tranh chấp biến mất khỏi mắt
    /// người duyệt, đúng thứ V1 cảnh báo.
    /// </summary>
    public static bool IsOnLadder(this VerificationLevel level) =>
        level is VerificationLevel.Speculative
            or VerificationLevel.Plausible
            or VerificationLevel.Supported
            or VerificationLevel.Verified;
}

/// <summary>
/// Lifecycle state được LƯU. V3: chỉ có BA giá trị.
///
/// ⚠ NeedsReview và Superseded KHÔNG có ở đây — chúng được SUY RA
/// (xem <c>KnowledgeRecord.IsSuperseded</c> / <c>NeedsReview</c>).
/// Thêm chúng vào enum này là vi phạm V3 và tạo lại đúng bệnh §6.9.
/// </summary>
public enum StoredLifecycleState
{
    /// <summary>Chưa từng được duyệt. Path A sinh record ở đây.</summary>
    Draft = 1,

    /// <summary>Đã được duyệt — nội dung + quyền xem, MỘT hành động (S7).</summary>
    Active = 2,

    /// <summary>
    /// Người có quyền RÚT nó, không có bản thay thế.
    /// Khác Superseded (có bản thay thế cụ thể, và là state SUY RA).
    /// V4 ca (a): tri thức vẫn ĐÚNG nhưng không còn ai gặp → vào đây,
    /// và verification KHÔNG đổi.
    /// </summary>
    Deprecated = 3,
}

/// <summary>Knowledge types cho MVP. T3 — đúng HAI type, mỗi type có ca thật.</summary>
public enum KnowledgeType
{
    /// <summary>tín hiệu → nguyên nhân. Ca thật: OTA booking không về PMS.</summary>
    Diagnostic = 1,

    /// <summary>
    /// điều kiện → hành động. Ca thật: CRM deal (§2.4).
    /// ⚠ Type DỄ LẪN VỚI PROCESS nhất — rule phân định là K-B6.
    /// </summary>
    ConditionalRecommendation = 2,
}

/// <summary>
/// Một assertion nói về mặt nào của nguyên nhân. Bốn thành phần ở 04 §3C.5.
/// Đây KHÔNG phải vocabulary khóa ở §3D.7 — nó là cách tổ chức nội dung,
/// suy ra từ T1/T2/AP1/AP4. Sửa được nếu có ca thật đòi.
/// </summary>
public enum AssertionKind
{
    /// <summary>"nguyên nhân này tồn tại" — identity của record (T1).</summary>
    CauseExists = 1,

    /// <summary>"nhận ra bằng log pattern ..." — AP4: một cách nhận ra = MỘT assertion.</summary>
    Recognition = 2,

    /// <summary>"áp dụng cho bản dưới 2.3" — AP1: applicability là assertion, không phải field có cấu trúc.</summary>
    Applicability = 3,

    /// <summary>"xử lý: nâng version" — T2: action ĐƠN LẺ nằm trong record.</summary>
    Handling = 4,
}

/// <summary>
/// Evidence quan hệ thế nào với một assertion. K-B9: Evidence được phép trỏ
/// TRỰC TIẾP vào Knowledge, không cần qua Case.
/// </summary>
public enum EvidenceRelation
{
    Support = 1,
    Refute = 2,
    ContextFor = 3,
}

/// <summary>
/// Ai được XEM một tri thức đã duyệt.
///
/// `S7` khoá QUY TẮC (mặc định hẹp nhất · mở rộng phải là hành vi tường minh · hệ thống
/// KHÔNG BAO GIỜ tự mở) nhưng không khoá TẬP GIÁ TRỊ. Tập giá trị chốt **2026-09-15** bởi
/// chủ dự án — `AR-a`, phiếu ở `docs/14` §A2.
///
/// 🛑 **THỨ TỰ CÓ NGHĨA, và đó là lý do chọn BA mức chứ không phải hai:** `S7` nói mặc định
/// là *"HẸP NHẤT trong các nguồn"*, mà "hẹp nhất" chỉ có nghĩa khi tập giá trị **có thứ tự**.
/// Số tăng dần = phạm vi RỘNG dần, nên so bằng `&lt;` là so độ hẹp.
///
/// ⚠ **TÊN C# KHÁC DẠNG LƯU.** Dạng lưu và dạng truyền là kebab (`noi-bo`) — đúng như quyết
/// định viết ra, và đúng như 7 dòng đã có trong `kp_dev` từ đợt build 2026-09-13. Nên
/// **KHÔNG** dùng `HasConversion&lt;string&gt;()`: nó lưu tên C# (`NoiBo`) và sẽ làm 7 dòng cũ
/// không đọc lại được mà không ai báo. Xem <see cref="VisibilityScopes"/>.
/// </summary>
public enum VisibilityScope
{
    /// <summary>HẸP NHẤT — mặc định của `S7` khi chưa ai mở rộng.</summary>
    RiengTu = 1,

    NoiBo = 2,

    /// <summary>RỘNG NHẤT.</summary>
    ToanCongTy = 3,
}

/// <summary>
/// Chuyển đổi giữa <see cref="VisibilityScope"/> và DẠNG LƯU / DẠNG TRUYỀN của nó.
///
/// ⚠ **Bảng ánh xạ TƯỜNG MINH, không sinh từ tên C#.** Sinh tự động (kiểu hạ chữ rồi chèn
/// gạch) thì một lần đổi tên enum sẽ âm thầm đổi dạng lưu, và dữ liệu cũ thành không đọc
/// được — mà không test nào đỏ, vì test cũng sinh chuỗi bằng chính hàm đó.
/// </summary>
public static class VisibilityScopes
{
    private static readonly (VisibilityScope Value, string Wire)[] Bang =
    [
        (VisibilityScope.RiengTu, "rieng-tu"),
        (VisibilityScope.NoiBo, "noi-bo"),
        (VisibilityScope.ToanCongTy, "toan-cong-ty"),
    ];

    /// <summary>Danh sách giá trị nhận được, để thông báo lỗi nói thẳng thay vì bắt đoán.</summary>
    public static string Allowed => string.Join(" · ", Bang.Select(x => x.Wire));

    public static string ToWire(this VisibilityScope value) =>
        Bang.First(x => x.Value == value).Wire;

    /// <summary>
    /// Khớp theo DẠNG LƯU. Hoa thường không quan trọng, nhưng dấu gạch thì có.
    ///
    /// KHÔNG dùng <c>Enum.TryParse</c> — cùng lý do đã ghi ở <c>EvidenceReadability</c>:
    /// nó nhận cả chuỗi số (<c>"1"</c>) lẫn giá trị ngoài enum (<c>"7"</c>) mà không ném.
    /// </summary>
    public static bool TryParse(string? raw, out VisibilityScope value)
    {
        var can = raw?.Trim();
        foreach (var (v, wire) in Bang)
        {
            if (string.Equals(wire, can, StringComparison.OrdinalIgnoreCase))
            {
                value = v;
                return true;
            }
        }

        value = default;
        return false;
    }
}
