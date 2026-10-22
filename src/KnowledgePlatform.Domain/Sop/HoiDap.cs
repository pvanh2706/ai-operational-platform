using System.Text.Json;
using System.Text.Json.Serialization;
using KnowledgePlatform.Domain.Redaction;
using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Sop;

/// <summary>Một mẩu nguồn đưa cho model đọc. <paramref name="Ma"/> là thứ model phải chép lại khi dẫn.</summary>
/// <param name="Ma"><c>N1</c>, <c>N2</c>… — ngắn, để model chép không sai.</param>
/// <param name="Nhan">Nhãn cho NGƯỜI đọc: khoá nguồn của evidence, hoặc "tài liệu X · đoạn 3".</param>
public sealed record NguonHoiDap(string Ma, string Nhan, string NoiDung);

/// <summary>Payload một lượt hỏi đáp, ĐÃ qua cổng che.</summary>
/// <param name="NguonDaChe">Đúng các mẩu đã gửi, ở dạng ĐÃ che — thứ trả cho người đọc để đối chiếu.</param>
/// <param name="BiCat">Nguồn dài quá trần nên chỉ gửi phần đầu — câu trả lời phải nói ra điều này.</param>
public sealed record HoiDapPrompt(
    string SystemText,
    string UserText,
    int RedactedCount,
    IReadOnlyList<NguonHoiDap> NguonDaChe,
    bool BiCat);

/// <summary>Một phát biểu model trả, kèm mã các mẩu nó dẫn.</summary>
public sealed record PhatBieuModel(
    [property: JsonPropertyName("noiDung")] string NoiDung,
    [property: JsonPropertyName("nguon")] IReadOnlyList<string> Nguon);

/// <summary>Câu trả lời THÔ của model — CHƯA đối chiếu mã nguồn.</summary>
public sealed record KetQuaModelHoiDap(
    [property: JsonPropertyName("bietTraLoi")] bool BietTraLoi,
    [property: JsonPropertyName("phatBieu")] IReadOnlyList<PhatBieuModel> PhatBieu,
    [property: JsonPropertyName("ghiChu")] string? GhiChu)
{
    [JsonIgnore] public int TokenVao { get; init; }

    [JsonIgnore] public int TokenRa { get; init; }
}

/// <summary>
/// Gọi model để trả lời MỘT câu hỏi về một case hoặc một tài liệu — `Q3-1` (2026-10-22). Interface MỎNG theo khuôn `AR3`:
/// mọi luật (nguồn nào, trần, biên lai, đối chiếu mã dẫn) nằm ở nơi gọi.
/// </summary>
public interface IGoiModelHoiDap
{
    Task<KetQuaModelHoiDap> HoiAsync(HoiDapPrompt prompt, CancellationToken ct = default);
}

/// <summary>
/// Dựng payload hỏi đáp — `Q3-1`.
///
/// 🛑 Đây là đường ĐẦU TIÊN cho model sinh chữ từ vật liệu CHƯA DUYỆT rồi đưa cho người đọc. Ba luật nằm ở đây và ở
/// <see cref="HoiDapDoiChieu"/>, không ở lời dặn trong prompt:
/// <list type="bullet">
///   <item>cổng che chạy trên CẢ câu hỏi lẫn nguồn, như mọi đường ra khác (`AR-o`, `IM-25`);</item>
///   <item>mỗi mẩu mang một mã ngắn, và phát biểu không dẫn được mã CÓ THẬT thì bị bỏ — không phải "đánh dấu";</item>
///   <item>nguồn dài quá trần thì cắt và NÓI RA — câu "nguồn không nói" phải phân biệt được với "phần đó không được gửi".</item>
/// </list>
/// </summary>
public static class HoiDapPromptBuilder
{
    public const string SchemaJson = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["bietTraLoi", "phatBieu", "ghiChu"],
          "properties": {
            "bietTraLoi": { "type": "boolean", "description": "false nếu các mẩu nguồn KHÔNG trả lời được câu hỏi." },
            "phatBieu": {
              "type": "array",
              "description": "Câu trả lời, chia thành từng phát biểu. Mỗi phát biểu dẫn ít nhất một mã mẩu nguồn.",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["noiDung", "nguon"],
                "properties": {
                  "noiDung": { "type": "string" },
                  "nguon": { "type": "array", "items": { "type": "string" }, "description": "Mã mẩu, chép nguyên văn, ví dụ N2." }
                }
              }
            },
            "ghiChu": { "type": "string", "description": "Nguồn thiếu gì, hoặc mâu thuẫn ở đâu. Chuỗi rỗng nếu không có." }
          }
        }
        """;

    public const string SystemText = """
        Bạn trả lời MỘT câu hỏi của người vận hành, CHỈ dựa trên các MẨU NGUỒN đưa kèm — evidence của một ticket hỗ trợ,
        hoặc các đoạn của một tài liệu.

        LUẬT:
        1. Mỗi phát biểu trong `phatBieu` phải dẫn ít nhất một mã mẩu trong `nguon` — chép NGUYÊN mã, ví dụ "N2". Phát
           biểu không dẫn được mã có thật sẽ bị BỎ trước khi tới người đọc.
        2. Không suy ra ngoài nguồn, không lấy hiểu biết chung lấp chỗ trống. Nguồn không nói thì trả bietTraLoi = false,
           phatBieu rỗng, và nói trong `ghiChu` nguồn thiếu gì. Đó là câu trả lời ĐÚNG và được mong đợi.
        3. Nguồn mâu thuẫn nhau thì nói rõ là mâu thuẫn và dẫn cả hai.
        4. Đoạn `[ĐÃ CHE]` là thông tin đăng nhập đã che. Đừng cố suy ra giá trị.
        5. Câu trả lời này CHƯA qua người duyệt. Viết ngắn, đúng sự kiện; không khuyên làm gì vượt quá điều nguồn nói.
        """;

    /// <param name="tranKyTu">Trần tổng số ký tự nguồn gửi đi; quá thì cắt từ cuối và đặt <see cref="HoiDapPrompt.BiCat"/>.</param>
    public static HoiDapPrompt Build(string cauHoi, string moTaNguon, IReadOnlyList<NguonHoiDap> nguon, int tranKyTu,
        bool quaCongChe = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cauHoi);
        ArgumentNullException.ThrowIfNull(nguon);
        if (nguon.Count == 0) throw new ArgumentException("Không có mẩu nguồn nào để hỏi.", nameof(nguon));

        var daChe = new List<NguonHoiDap>();
        var soCho = 0;
        var tong = 0;
        var biCat = false;

        foreach (var n in nguon)
        {
            var noiDung = n.NoiDung;
            if (tong + noiDung.Length > tranKyTu)
            {
                biCat = true;
                var conLai = tranKyTu - tong;
                if (conLai <= 0) break;
                noiDung = noiDung[..conLai];
            }

            tong += noiDung.Length;

            if (quaCongChe)
            {
                var r = EgressRedactor.Redact(noiDung);
                soCho += r.RedactedCount;
                noiDung = r.Text;
            }

            daChe.Add(n with { NoiDung = noiDung });
            if (biCat) break;
        }

        var cau = cauHoi.Trim();
        if (quaCongChe)
        {
            var r = EgressRedactor.Redact(cau);
            soCho += r.RedactedCount;
            cau = r.Text;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append("NGUỒN: ").AppendLine(moTaNguon);
        if (biCat) sb.AppendLine("⚠ Nguồn dài quá trần nên CHỈ phần đầu được gửi. Phần sau KHÔNG có ở đây.");
        sb.AppendLine();
        foreach (var n in daChe)
        {
            sb.Append('[').Append(n.Ma).Append("] ").AppendLine(n.Nhan);
            sb.AppendLine(n.NoiDung);
            sb.AppendLine();
        }

        sb.Append("CÂU HỎI: ").AppendLine(cau);

        var text = sb.ToString();
        if (quaCongChe) EgressRedactor.EnsureSafeToSend(text);

        return new HoiDapPrompt(SystemText, text, soCho, daChe, biCat);
    }
}

/// <summary>Một phát biểu ĐÃ đối chiếu: chỉ còn mã dẫn có thật.</summary>
public sealed record PhatBieuHoiDap(string NoiDung, IReadOnlyList<string> Nguon);

/// <summary>Kết quả đối chiếu câu trả lời của model (hoặc của AI qua file) với các mẩu đã gửi.</summary>
public sealed record DoiChieuHoiDap(bool BietTraLoi, IReadOnlyList<PhatBieuHoiDap> PhatBieu, int SoPhatBieuBiBo, string? GhiChu);

/// <summary>
/// Đối chiếu mã dẫn — chung cho chế độ API và chế độ File, để hai đường không có hai luật.
///
/// 🛑 Phát biểu không dẫn được mã CÓ THẬT bị BỎ, và số bị bỏ được trả ra. Mọi phát biểu đều bị bỏ → KHÔNG BIẾT, kể cả khi
/// model tự nói <c>bietTraLoi: true</c>: một câu trả lời không trỏ được về nguồn là một câu người đọc không phản bác được.
/// </summary>
public static class HoiDapDoiChieu
{
    public static DoiChieuHoiDap DoiChieu(KetQuaModelHoiDap kq, IReadOnlyCollection<string> maCoThat)
    {
        ArgumentNullException.ThrowIfNull(kq);

        var giu = new List<PhatBieuHoiDap>();
        var bo = 0;

        foreach (var p in kq.PhatBieu ?? [])
        {
            var ma = (p.Nguon ?? []).Select(x => x.Trim().Trim('[', ']')).Where(maCoThat.Contains).Distinct().ToList();
            if (ma.Count == 0 || string.IsNullOrWhiteSpace(p.NoiDung))
            {
                bo++;
                continue;
            }

            giu.Add(new PhatBieuHoiDap(p.NoiDung.Trim(), ma));
        }

        var ghiChu = string.IsNullOrWhiteSpace(kq.GhiChu) ? null : kq.GhiChu.Trim();
        return new DoiChieuHoiDap(kq.BietTraLoi && giu.Count > 0, kq.BietTraLoi ? giu : [], bo, ghiChu);
    }

    private static readonly JsonSerializerOptions Doc = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Đọc câu trả lời AI viết vào file (chế độ File). Gỡ hàng rào <c>```</c> — AI qua khung chat hay bọc JSON trong đó.
    /// <c>null</c> = không đọc được thành hình dạng của <see cref="HoiDapPromptBuilder.SchemaJson"/>.
    /// </summary>
    public static KetQuaModelHoiDap? DocTuFile(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        var s = json.Trim();
        if (s.StartsWith("```", StringComparison.Ordinal))
        {
            var dau = s.IndexOf('\n');
            var cuoi = s.LastIndexOf("```", StringComparison.Ordinal);
            if (dau > 0 && cuoi > dau) s = s[(dau + 1)..cuoi].Trim();
        }

        try
        {
            var kq = JsonSerializer.Deserialize<KetQuaModelHoiDap>(s, Doc);
            return kq is { PhatBieu: not null } ? kq : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

/// <summary>
/// Biên lai một lượt hỏi đáp bằng model — `Q3-1`. Ghi TRƯỚC khi gọi, cùng lý do <see cref="LuotSoanNhap"/>: lượt đứt giữa
/// chừng vẫn có thể đã tính tiền, và trần tháng phải đếm cả nó.
///
/// ⚠ KHÔNG lưu câu hỏi hay câu trả lời: biên lai là để ĐẾM tiền và lỗi, không phải để giữ nội dung — và `Q3-1` cấm ghi câu
///   trả lời thành tri thức. Câu hỏi có thể mang dữ liệu khách mà không ai định lưu.
/// </summary>
public sealed class LuotHoiDap : ITenantScoped
{
    public const string LoaiCase = "case";
    public const string LoaiTaiLieu = "tai-lieu";

    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    /// <summary><c>case</c> · <c>tai-lieu</c>.</summary>
    public required string LoaiNguon { get; init; }

    /// <summary>Khoá nguồn của case, hoặc id tài liệu.</summary>
    public required string MaNguon { get; init; }

    /// <summary>LỜI KHAI của người hỏi — cùng loại với <see cref="LuotSoanNhap.NguoiYeuCau"/>.</summary>
    public required string NguoiHoi { get; init; }

    public required int SoNguon { get; init; }

    public required int SoChoDaChe { get; init; }

    public required string NhaCungCap { get; init; }

    public string? Model { get; init; }

    public bool ThanhCong { get; set; }

    public string? Loi { get; set; }

    public DateTimeOffset Luc { get; init; } = DateTimeOffset.UtcNow;
}
