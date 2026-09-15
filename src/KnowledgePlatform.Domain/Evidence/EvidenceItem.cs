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

    /// <summary>
    /// Mã khách sạn mà mẩu này thuộc về — `AR-l`, chốt 2026-09-15: **bên gửi KHAI**.
    /// Ba trạng thái và ĐỪNG GỘP CHÚNG — xem <see cref="SubTenantCodes"/>.
    ///
    /// ⚠ **Kiểu là <c>string?</c> dù hợp đồng nạp BẮT BUỘC khai, và đó là chủ đích.** Ở dự án
    /// này chặn-lúc-biên-dịch được ưu tiên hơn chặn-lúc-chạy, nên <c>required</c> mới là phản
    /// xạ đúng. Nhưng <c>required</c> ở đây sẽ nói dối về 128 mẩu ĐÃ nằm trong <c>kp_dev</c>
    /// từ trước luật này: chúng không có giá trị nào, và cách duy nhất để chúng thoả
    /// <c>required</c> là bịa ra một giá trị. Cột NULL được là để <c>NULL</c> giữ đúng nghĩa
    /// "chưa ai từng được hỏi" — hàng thật, hàng cũ, và nhìn ra được ngay.
    /// → Tính BẮT BUỘC vì thế được thực thi ở CỬA VÀO (`ValidateEvidence` trả 400), không ở
    ///   kiểu dữ liệu. Ghi ra đây vì đó là một ngoại lệ có lý do, không phải một chỗ bỏ sót.
    /// </summary>
    public string? SubTenantCode { get; set; }

    // ─────────────────────────────────────────────────────────────────────────────
    //  AR-m — nội dung NGƯỜI GỬI ĐÃ RÚT LẠI. Chốt 2026-09-15 (phiếu `docs/14` §B1 = B1-1).
    //
    //  Ca thật: `ES-346661#description` chứa một đoạn giải thích nguyên nhân overbooking
    //  đầy đủ kèm hai mã booking OTA. MƯỜI SÁU PHÚT SAU, cùng người gửi viết "dạ anh/chị
    //  bỏ qua giúp em tin nhắn trên ạ, em gửi nhầm nội dung" — và tiêu đề case là "lỗi
    //  không sinh mã đặt phòng", không dính gì tới overbook.
    //
    //  ⚠ VÌ SAO HÔM NAY CHƯA HỎNG VÀ NGÀY MAI SẼ HỎNG: hôm nay đoạn sai và câu đính chính
    //    nằm CHUNG một mẩu 1 916 ký tự, nên model đọc mẩu đó thấy luôn câu đính chính. Đó
    //    là MAY, không phải thiết kế. Ngày nào pipeline cắt transcript thành nhiều mẩu —
    //    việc rất có thể làm, vì `AR-h` đo được mẩu dài đè bẹp xếp hạng 29:1 — thì đoạn đã
    //    bị rút vào kho KHÔNG MANG DẤU HIỆU GÌ.
    //  🛑 Nên thứ tự là bắt buộc: có chỗ đánh dấu RỒI mới được chốt cách cắt. Cắt trước là
    //    tạo ra đúng ca mà hai cột này sinh ra để chặn, và cắt rồi thì không ghép lại được.
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>NULL = chưa ai rút lại. Đặt được CHỈ qua <see cref="RutLai"/>.</summary>
    public DateTimeOffset? RetractedAt { get; private set; }

    /// <summary>Lý do người đánh dấu ghi ra. Bắt buộc khi rút — xem <see cref="RutLai"/>.</summary>
    public string? RetractedReason { get; private set; }

    /// <summary>
    /// AI đánh dấu. `B1-1` chốt **CHỈ NGƯỜI điền tay**, nên phải biết là người nào.
    ///
    /// ⚠ Trường này KHÔNG có trong đề xuất gốc của `AR-m` (chỉ nêu <c>RetractedAt</c> +
    /// <c>RetractedReason</c>), thêm vào vì đúng bài học của `M2` phần (c): một dấu vết
    /// không có tác giả thì không phân biệt được với một dấu vết do script đặt — mà "script
    /// không được đặt" chính là nội dung của quyết định này.
    /// </summary>
    public string? RetractedBy { get; private set; }

    public bool DaRutLai => RetractedAt is not null;

    /// <summary>
    /// Người đánh dấu một mẩu là ĐÃ BỊ NGƯỜI GỬI RÚT LẠI.
    ///
    /// 🛑 **KHÔNG có đường nào cho máy gọi hàm này.** `B1-1` chọn ít tín hiệu mà mỗi cái đều
    /// đúng, hơn nhiều tín hiệu mà không biết cái nào tin được: người gửi rút lại bằng CÂU
    /// CHỮ ("bỏ qua giúp em"), và suy ra từ câu chữ là ĐOÁN — trái `G6`/`AP3`. Vì thế
    /// <c>CaseEvidenceSignal</c> KHÔNG mang ba trường này, và đường nạp không
    /// có cách nào đặt chúng.
    ///
    /// ⚠ **Rút lại rồi thì KHÔNG rút lại lần hai được.** Cho ghi đè là để lần đánh dấu thứ
    /// hai âm thầm xoá mất tác giả và lý do của lần đầu — tức mất đúng thứ ba trường này
    /// sinh ra để giữ. Muốn sửa lý do thì đó là một quyết định khác, và chưa ai cần.
    /// </summary>
    public void RutLai(string nguoiDanhDau, string lyDo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nguoiDanhDau);
        ArgumentException.ThrowIfNullOrWhiteSpace(lyDo);

        if (DaRutLai)
        {
            throw new InvalidOperationException(
                $"Mẩu này đã được đánh dấu rút lại lúc {RetractedAt:O} bởi \"{RetractedBy}\". " +
                "Đánh dấu lần hai sẽ xoá mất tác giả và lý do của lần đầu.");
        }

        RetractedAt = DateTimeOffset.UtcNow;
        RetractedBy = nguoiDanhDau.Trim();
        RetractedReason = lyDo.Trim();
    }
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
