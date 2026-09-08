namespace KnowledgePlatform.Domain.Sop;

/// <summary>
/// Soạn một bản nháp SOP từ một nhóm case + evidence.
///
/// `AR3` chốt ở Workstream 06: **SDK chính thức + một interface MỎNG**. Mỏng nghĩa là
/// interface này KHÔNG biết gì về nhà cung cấp model, về prompt, về token, về batch. Nó
/// biết đúng một việc: đưa vào một nhóm, nhận về một bản nháp.
///
/// Vì sao mỏng lại quan trọng ở đúng chỗ này — `D5`: *"model mạnh lên thì phần mềm mạnh
/// lên"*. Một interface rộng (có `Temperature`, `MaxTokens`, `SystemPrompt` trong chữ ký)
/// là một interface đóng băng cách làm của hôm nay vào tầng domain; sang model sau thì
/// phải sửa cả tầng domain để đổi một tham số.
///
/// ⚠ Cái interface này KHÔNG hứa: rằng bản nháp đúng. Nó chỉ hứa trả về đúng HÌNH DẠNG.
/// Đúng hay không là việc của bộ eval (`nhom_sop.py --kiem-cay`) và của người duyệt —
/// và `M2` (đã sửa 2026-09-08) đòi thêm bằng chứng rằng lượt duyệt CÓ xảy ra.
/// </summary>
public interface ISopDrafter
{
    /// <summary>
    /// Soạn nháp. Ném <see cref="Redaction.EgressBlockedException"/> nếu cổng che không
    /// làm sạch được payload — `AR-o` fail closed, và điều đó xảy ra TRƯỚC khi có byte nào
    /// rời khỏi máy.
    /// </summary>
    Task<SopDraftResult> DraftAsync(SopDraftRequest request, CancellationToken ct = default);
}

/// <summary>
/// Bản nháp, kèm những gì cần để ĐO lượt gọi đó. Ba con số ở đây không phải để cho đẹp:
/// <list type="bullet">
///   <item><see cref="RedactedCount"/> — `AR-o` đòi ghi lại số chỗ đã che MỖI lần gọi,
///     vì luật che là cận trên đã đo, không phải một sự đảm bảo.</item>
///   <item><see cref="InputTokens"/> / <see cref="OutputTokens"/> — để chi phí là một con
///     số đo được chứ không phải một cảm giác. Đã ước trên corpus thật: một nhóm 10 case
///     ~$0,16, cả 19 nhóm ~$2,77 (giảm nửa qua Batches). Ước lượng phải được thay bằng số
///     đo ngay lần gọi đầu.</item>
/// </list>
/// </summary>
public sealed record SopDraftResult(
    SopDraft Draft,
    int RedactedCount,
    int InputTokens,
    int OutputTokens)
{
    /// <summary>Giá tạm tính theo bảng giá `claude-opus-5` ($5 / $25 một triệu token).</summary>
    public decimal EstimatedUsd => (InputTokens * 5m + OutputTokens * 25m) / 1_000_000m;
}
