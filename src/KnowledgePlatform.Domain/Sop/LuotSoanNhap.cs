using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Sop;

/// <summary>
/// Một lượt gọi model để soạn nháp — **lượt TỐN TIỀN**.
///
/// `E3-1` chốt: *chỉ người có quyền duyệt được bấm, có trần tháng*. Bảng này là chỗ đếm cho
/// cái trần đó, và đồng thời là biên lai: câu *"tháng này ai đã tiêu bao nhiêu lượt"* chỉ trả
/// lời được nếu có dòng.
///
/// ⚠ **Ghi CẢ lượt hỏng.** Gọi model mà đứt giữa chừng vẫn có thể đã tính tiền — không ghi
/// thì trần tháng đếm thiếu, và nó đếm thiếu đúng vào lúc có sự cố lặp đi lặp lại.
///
/// 🛑 <see cref="NguoiYeuCau"/> là một **LỜI KHAI**, không phải một phép xác thực. Sản phẩm
/// hôm nay biết *request này thuộc công ty nào* nhưng không biết *người nào* — xem `docs/18`
/// §D5. Ghi đúng bản chất ở đây để về sau không ai đọc nó thành một chuỗi uỷ quyền.
/// </summary>
public sealed class LuotSoanNhap : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public required string Nhom { get; init; }

    /// <summary>Ai khai là mình yêu cầu. LỜI KHAI — xem phần tóm tắt của lớp.</summary>
    public required string NguoiYeuCau { get; init; }

    public required int SoCase { get; init; }

    /// <summary>Số chỗ cổng che `AR-o` đã che trước khi gửi. 0 nghĩa là luật KHÔNG THẤY GÌ.</summary>
    public required int SoChoDaChe { get; init; }

    public bool ThanhCong { get; set; }

    public string? Loi { get; set; }

    /// <summary>
    /// `Q1-1` (2026-10-22): lượt này tính tiền ở nhà cung cấp nào, model nào. <c>NULL</c> = lượt trước `Q1-1` — khi đó chỉ
    /// có Anthropic + <c>claude-opus-5</c>.
    /// </summary>
    public string? NhaCungCap { get; set; }

    public string? Model { get; set; }

    public DateTimeOffset Luc { get; init; } = DateTimeOffset.UtcNow;
}
