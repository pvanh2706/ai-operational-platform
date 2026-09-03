using KnowledgePlatform.Domain.Evidence;

namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Một mẩu quan sát được ở hệ thống nguồn: comment Jira, ghi chú xử lý, một email,
/// một tin Zalo. `AR-f` (chốt 2026-08-30) mở đường này.
///
/// ⚠ **Vì sao nó là endpoint riêng chứ không lồng vào tín hiệu case.**
/// `K-B9` đã chốt: evidence được phép trỏ THẲNG vào Knowledge, không qua Case — vì
/// email của senior hay tin nhắn Zalo không thuộc case nào, và với thực tế 60%
/// fragment rải rác thì đó không phải trường hợp hiếm. Nếu cửa duy nhất để evidence
/// vào hệ thống là bên trong một tín hiệu case, thì loại evidence đó **vĩnh viễn
/// không có đường vào** — không phải "chưa hỗ trợ", mà là không thể. Rồi cũng phải
/// mở cửa thứ hai, và lúc đó mới đúng cái bẫy `IM-12` cảnh báo: hai đường code cùng
/// tạo ra <c>evidence_item</c>, đường ít chạy hơn mục dần mà không ai biết.
///
/// Một cửa duy nhất cho evidence, và quan hệ với Case là **tuỳ chọn**.
/// </summary>
/// <param name="CaseSourceReference">
/// Khoá nguồn của Case mà quan sát này thuộc về — <c>"jira:ES-1234"</c>.
///
/// <c>null</c> LÀ HỢP LỆ và là chủ đích (`K-B9`): mẩu tri thức rời không thuộc case nào.
/// Nhưng nếu CÓ khai mà case không tồn tại thì **cả lô bị từ chối**: nhận rồi lặng lẽ
/// để link rỗng sẽ biến một lỗi thứ tự gọi thành dữ liệu sai không ai thấy.
///
/// ⚠ Bên gửi không cần "nhớ đã gửi case chưa": <c>/signals/case-observed</c> idempotent,
/// nên cứ gửi case trước mỗi lần, lần hai trả <c>created: 0</c> và vô hại.
/// </param>
/// <param name="SourceReference">
/// Khoá của chính mẩu quan sát ở nguồn — <c>"jira:ES-1234#comment-7"</c>.
/// Khoá chống trùng, unique <c>(TenantId, SourceReference)</c>.
/// </param>
/// <param name="Content">Nội dung quan sát được, ở dạng đọc được. v0.2 R5: giữ nguyên văn nguồn.</param>
/// <param name="ObservedAt">Thời điểm quan sát được Ở NGUỒN — khác <c>IngestedAt</c> (lúc ta biết).</param>
/// <param name="MachineReadability">
/// Máy đọc được đến mức nào: <c>Unknown</c> · <c>Low</c> · <c>Medium</c> · <c>High</c>.
///
/// **Bên gửi khai, hệ thống KHÔNG suy.** Connector biết nó đang đẩy text hay đường dẫn
/// ảnh; hệ thống nhìn vào một chuỗi thì không biết. Tự gán <c>High</c> cho mọi thứ là
/// text sẽ dán nhãn sai cho ảnh chưa OCR — đúng trạng thái
/// <c>KNOWLEDGE_EXISTS_NOT_RETRIEVABLE</c> ở §6.3 mà sản phẩm cần nhìn thấy.
///
/// Thiếu thì là <c>Unknown</c> — enum đã có sẵn giá trị đó, và "chưa ai nói" là một
/// câu trả lời thật. Sai chính tả thì **400**, không âm thầm về <c>Unknown</c>.
/// </param>
public sealed record CaseEvidenceSignal(
    string? CaseSourceReference,
    string SourceReference,
    string Content,
    DateTimeOffset? ObservedAt,
    string? MachineReadability);

/// <summary>Kết quả xử lý một mẩu evidence.</summary>
/// <param name="CaseId"><c>null</c> = evidence rời, không gắn case nào (`K-B9`).</param>
/// <param name="Created"><c>false</c> = đã có từ trước. KHÔNG phải lỗi — xem `IM-15`.</param>
public sealed record CaseEvidenceResult(
    string SourceReference,
    Guid EvidenceId,
    Guid? CaseId,
    bool Created);

/// <param name="Received">Tổng số nhận được — để bên gửi đối chiếu, không phải đoán.</param>
public sealed record CaseEvidenceBatchResult(
    int Received,
    int Created,
    IReadOnlyList<CaseEvidenceResult> Results);

/// <summary>
/// Đọc nhãn <see cref="MachineReadability"/> từ chuỗi. Một chỗ duy nhất biết cách
/// đọc, dùng bởi cả tầng kiểm tra lẫn tầng ghi — hai chỗ đọc khác nhau là cách
/// "hợp lệ lúc kiểm, khác nghĩa lúc ghi" ra đời.
/// </summary>
public static class EvidenceReadability
{
    /// <summary>Danh sách giá trị nhận được, để thông báo lỗi nói thẳng thay vì bắt đoán.</summary>
    public static string Allowed => string.Join(" · ", Enum.GetNames<MachineReadability>());

    public static bool TryParse(string? raw, out MachineReadability value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = MachineReadability.Unknown;
            return true;
        }

        // KHÔNG dùng Enum.TryParse: nó nhận cả chuỗi số ("1" → Low) và cả giá trị
        // ngoài enum ("7" → (MachineReadability)7, không ném). Khớp theo TÊN.
        foreach (var name in Enum.GetNames<MachineReadability>())
        {
            if (string.Equals(name, raw, StringComparison.OrdinalIgnoreCase))
            {
                value = Enum.Parse<MachineReadability>(name);
                return true;
            }
        }

        value = MachineReadability.Unknown;
        return false;
    }
}
