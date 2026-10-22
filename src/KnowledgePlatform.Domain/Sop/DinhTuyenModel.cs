using KnowledgePlatform.Domain.Redaction;
using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Sop;

/// <summary>Một nhóm của taxonomy, đưa cho model đọc — tên + ranh giới, KHÔNG phải đáp án.</summary>
public sealed record NhomTaxonomy(string Ten, string? MoTa);

/// <summary>Payload một lượt định tuyến, kèm SỐ CHỖ ĐÃ CHE (`AR-o`).</summary>
public sealed record DinhTuyenPrompt(string SystemText, string UserText, int RedactedCount);

/// <summary>
/// Dựng payload định tuyến một ticket MỚI cho model — P8, 2026-10-10.
///
/// 🛑 Hai luật từ `docs/13` §1, và cả hai nằm ở CHỮ KÝ của hàm chứ không ở lời dặn:
/// <list type="bullet">
///   <item>chỉ TIÊU ĐỀ + MÔ TẢ — thứ có mặt lúc ticket đến. Không comment, không transcript: transcript chứa sẵn
///     câu chẩn đoán, đưa vào là đưa đáp án vào đề;</item>
///   <item>KHÔNG có tham số mã ticket. `IDinhTuyenNhom` đã ghi: biết mã ticket là biết đáp án (bảng đo tra theo mã).
///     Không có tham số thì không có đường nào để lỡ tay đưa nó vào.</item>
/// </list>
/// ⚠ Cổng che chạy ở ĐÂY như <see cref="SopPromptBuilder"/> — một đường gọi mạng mới là một đường ra mới (`IM-25`).
/// Mặc định CÓ che; chỉ cờ `N1-2` tắt được, qua tham số tường minh.
/// </summary>
public static class DinhTuyenPromptBuilder
{
    public const string SystemText = """
        Bạn nhận MỘT ticket hỗ trợ vừa đến (tiêu đề + mô tả) và một TAXONOMY gồm các nhóm nguyên nhân.
        Việc của bạn: ticket này thuộc nhóm nào.

        LUẬT:
        1. `nhom` phải CHÉP NGUYÊN một giá trị `ten` trong taxonomy. Không viết tắt, không diễn đạt lại, không tự đặt
           nhóm mới. Sai một chữ là câu trả lời bị bỏ.
        2. Không đủ căn cứ thì trả đúng chuỗi "KHONG-BIET". Đó là một câu trả lời ĐÚNG và được mong đợi.
           ⚠ Đoán bừa nguy hiểm hơn nói không biết: hướng dẫn sai sẽ được đưa cho người đang xử lý sự cố thật.
        3. `nguon`: "noi-ro-trong-mo-ta" nếu ticket NÓI RÕ dấu hiệu của nhóm; "suy-tu-trieu-chung" nếu bạn suy ra từ
           triệu chứng; "khong-du-can-cu" khi trả KHONG-BIET.
        4. `giaiThich`: một câu, nêu CHỖ TRONG TICKET làm bạn kết luận — để người đọc phản bác được.
        5. `doTinCay`: số từ 0 tới 1, bạn tin câu trả lời của mình tới đâu. Hệ thống sẽ IM LẶNG nếu số này thấp —
           nên đừng thổi nó lên.

        Một số chỗ có đoạn `[ĐÃ CHE]` — đó là thông tin đăng nhập đã được che. Đừng cố suy ra giá trị.
        """;

    public static DinhTuyenPrompt Build(string? tieuDe, string? moTa, IReadOnlyList<NhomTaxonomy> nhom, bool quaCongChe = true)
    {
        ArgumentNullException.ThrowIfNull(nhom);
        if (nhom.Count == 0) throw new ArgumentException("Taxonomy rỗng — không có nhóm nào để chọn.", nameof(nhom));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("TAXONOMY:");
        foreach (var n in nhom)
        {
            sb.Append("- ").Append(n.Ten);
            if (!string.IsNullOrWhiteSpace(n.MoTa)) sb.Append(": ").Append(n.MoTa);
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("TICKET:");
        sb.Append("tieuDe: ").AppendLine(tieuDe ?? string.Empty);
        sb.Append("moTa: ").AppendLine(moTa ?? string.Empty);

        if (!quaCongChe)
        {
            return new DinhTuyenPrompt(SystemText, sb.ToString(), 0);
        }

        var daChe = EgressRedactor.Redact(sb.ToString());
        EgressRedactor.EnsureSafeToSend(daChe.Text);
        return new DinhTuyenPrompt(SystemText, daChe.Text, daChe.RedactedCount);
    }
}

/// <summary>Câu trả lời thô của model — CHƯA qua ngưỡng tin cậy, CHƯA đối chiếu taxonomy.</summary>
public sealed record KetQuaModelDinhTuyen(string Nhom, string Nguon, string? GiaiThich, double DoTinCay, int TokenVao, int TokenRa);

/// <summary>
/// Gọi model để phân loại MỘT ticket — interface MỎNG theo khuôn `AR3` (như <see cref="ISopDrafter"/>).
/// Mọi luật (ngưỡng, taxonomy, trần, cache) nằm ở nơi gọi; bản hiện thực chỉ gọi và đọc JSON.
/// </summary>
public interface IGoiModelDinhTuyen
{
    Task<KetQuaModelDinhTuyen> PhanLoaiAsync(DinhTuyenPrompt prompt, CancellationToken ct = default);
}

/// <summary>
/// Biên lai + bộ nhớ đệm của một lượt định tuyến bằng model (P8, `N7-1`).
///
/// 🛑 Ghi TRƯỚC khi gọi, cùng lý do <see cref="LuotSoanNhap"/>: lượt đứt giữa chừng vẫn có thể đã tính tiền, và trần
/// tháng phải đếm cả nó. Lượt thành công đồng thời là BỘ NHỚ ĐỆM — định tuyến chạy trên MỌI webhook và MỌI
/// <c>/hoi</c>, nên không có đệm thì một case hỏi mười lần là mười lượt tính tiền.
///
/// ⚠ Lưu câu trả lời THÔ của model (kể cả dưới ngưỡng): ngưỡng là cấu hình, đổi được; câu model đã nói thì không
/// dựng lại được mà không gọi lại.
/// </summary>
public sealed class LuotDinhTuyenModel : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    /// <summary>Khoá nguồn của case, nếu biết — khoá đệm chính.</summary>
    public string? KhoaNguon { get; init; }

    /// <summary>SHA-256 của tiêu đề + mô tả — khoá đệm khi không có case.</summary>
    public required string HashDauVao { get; init; }

    public int SoChoDaChe { get; init; }

    public string? Nhom { get; set; }

    public string? Nguon { get; set; }

    public string? GiaiThich { get; set; }

    public double? DoTinCay { get; set; }

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
