using System.Text.RegularExpressions;

namespace KnowledgePlatform.Domain.Redaction;

/// <summary>
/// Một chỗ nghi là bí mật, kèm VỊ TRÍ trong văn bản gốc.
///
/// ⚠ Vì sao phải có vị trí, trong khi bản Python (`check_corpus.py`) chỉ trả về giá trị:
/// bản Python chỉ cần ĐẾM và NÓI RA để người đọc quyết. Bản này phải CHE ĐƯỢC, tức phải
/// biết che vào đâu. Đó là khác biệt giữa ranh giới NẠP (`AR-j`) và ranh giới GỬI RA
/// (`AR-o`) — xem `docs/07` §5.
/// </summary>
/// <param name="Shape">Hình dạng nào bắt được: "nhãn+số", "công cụ+số", "số trần", "key JSON".</param>
/// <param name="Value">Giá trị bị bắt. ⚠ ĐỪNG ghi ra log — nó là bí mật thật.</param>
/// <param name="Index">Vị trí bắt đầu trong văn bản GỐC.</param>
/// <param name="Length">Độ dài trong văn bản GỐC.</param>
public sealed record SecretHit(string Shape, string Value, int Index, int Length)
{
    public int End => Index + Length;
}

/// <summary>
/// Quét bí mật theo BỐN HÌNH DẠNG đã thấy trong dữ liệu vận hành thật, không theo giá trị.
///
/// Port từ <c>scripts/jira-export/check_corpus.py</c> · <c>quet_bi_mat</c>. Cố ý giữ y
/// nguyên bốn hình dạng và cả những chỗ vá của bản Python — mỗi chỗ vá là một credential
/// đã từng bị trượt trên corpus thật:
///
/// <list type="number">
///   <item>`ID của bạn: &lt;9 chữ số&gt;` / `Mật khẩu: &lt;5 số&gt;` — nhãn và giá trị CÙNG DÒNG</item>
///   <item>`Ultraview: &lt;nn nnn nnn&gt; // &lt;5 số&gt;` — NHIỀU giá trị trên cùng dòng</item>
///   <item>một dòng mà TOÀN BỘ nội dung là một dãy số — kèm số đứng sát ngay trước nó</item>
///   <item>`"Account": "&lt;tên tài khoản&gt;"` — key JSON, và key KHÔNG phải password</item>
/// </list>
///
/// ⚠ RECALL LÀ CẬN TRÊN, KHÔNG PHẢI ƯỚC LƯỢNG ĐÚNG — luật vẫn được soi ra TỪ chính corpus
/// dùng để đo nó. Nhưng chỗ đứng đã khác trước: hình dạng 3 nay thuần CẤU TRÚC, không đọc
/// chữ, nên không có danh sách từ khoá nào để phải vá.
/// Lịch sử con số, giữ lại vì nó giải thích vì sao đừng tin một phép đo recall duy nhất:
/// bản Python đo 13/13 trên corpus 32 case (2026-09-04) → cùng bộ luật đó đo lại trên 345
/// mẩu (2026-09-11) chỉ còn **12/29**. Không phải luật hỏng đi; là corpus lớn hơn lộ ra
/// những hình dạng corpus nhỏ chưa có. Hình dạng thứ sáu ("ulatrview") đã tới đúng như dự
/// báo cũ, và nó phá được bản cũ vì bản cũ phải đọc được chữ mới bắt được số.
/// → Vì thế <see cref="EgressRedactor"/> KHÔNG được coi luật này là đủ. Nó chỉ là một lớp.
/// </summary>
public static class SecretShapeScanner
{
    private const RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    // ⚠ Dấu tiếng Việt phải liệt kê ĐỦ: "khẩu" là kh-ẩ-u, không phải kh-â-u. Bản đầu của
    // luật Python thiếu chữ 'ẩ' và vì thế trượt đúng một credential dạng "Mật khẩu: <5 số>".
    private const string Nhan =
        @"(?:m[aậâạ][tj]?\s*kh[aâẩ]u|matkhau|pass(?:word)?|pw|pwd|ID(?:\s*c[uủ]a\s*b[aạ]n)?)";

    private const string So = @"\d{2,3}[ ]\d{3}[ ]\d{3}|\d{4,9}";

    private static readonly Regex CongCu = new(@"ultra ?view|utlatra|teamview|anydesk", Opts);
    private static readonly Regex NhanVaSo = new(Nhan + @"\s*[:=]?\s*(" + So + ")", Opts);
    private static readonly Regex SoTran = new(@"^\s*(?:" + So + @")\s*$", Opts);
    private static readonly Regex LaySo = new(So, Opts);

    // `account`/`username` cũng vào đây: bản đầu chỉ bắt password nên để lọt cả hai tài
    // khoản VNPT. Một cặp đăng nhập thiếu nửa nào cũng vẫn là nửa bị rò.
    private static readonly Regex KeyJson = new(
        @"""?(a?c?pass\w*|password|matkhau|token|secret|api[_-]?key|account|user(?:name)?|login)""?\s*["":=]\s*""?([^""\s,}]{3,})",
        Opts);

    private static readonly Regex Url = new(@"https?://\S+", RegexOptions.CultureInvariant);

    // Dòng tiêu đề tin nhắn của bản xuất Jira: `*Tên_<Vai trò>_dd/MM/yyyy HH:mm:*`. Nó là
    // khung, không phải lời ai nói, nên khi đếm lùi tìm nửa kia của cặp đăng nhập thì nó
    // KHÔNG được tính. Bỏ qua chỗ này là một khối tiêu đề ăn hết ngân sách 3 dòng và luật
    // không bao giờ với tới dòng có ID.
    private static readonly Regex TieuDeTinNhan = new(@"^\s*\*.*_<[^>]*>_.*\*\s*$", Opts);

    /// <summary>
    /// Quét, trả về mọi chỗ nghi là bí mật kèm vị trí trong văn bản GỐC.
    /// Không sửa gì, không ném gì — chỉ nói ra. Quyết định làm gì là của
    /// <see cref="EgressRedactor"/>.
    /// </summary>
    public static IReadOnlyList<SecretHit> Scan(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return [];
        }

        // ⚠ BỎ URL TRƯỚC KHI QUÉT — một false positive đã đo thật trên corpus 12 tháng:
        // `...&table_id=23&id=2469&area=4` bị bắt vì nhãn `ID` khớp `id=`. Tham số truy vấn
        // trong URL không phải bí mật, và che nó đi thì mất đường dẫn mà người duyệt cần để
        // mở lại đúng màn hình đang lỗi.
        //
        // ⚠ Và thay bằng KHOẢNG TRẮNG CÙNG ĐỘ DÀI, không thay bằng "<URL>" như bản Python.
        // Lý do là thứ bản Python không cần: bản này phải che ĐÚNG VỊ TRÍ trong văn bản gốc,
        // nên mọi phép thay thế trước khi quét phải giữ nguyên offset. Đổi độ dài là làm
        // lệch mọi vị trí sau nó, và lệch âm thầm.
        var sach = Url.Replace(content, m => new string(' ', m.Length));

        var ra = new List<SecretHit>();
        foreach (var (dong, batDau) in TachDong(sach))
        {
            foreach (Match m in NhanVaSo.Matches(dong))
            {
                var g = m.Groups[1];
                ra.Add(new SecretHit("nhãn+số", g.Value, batDau + g.Index, g.Length));
            }

            var mc = CongCu.Match(dong);
            if (mc.Success)
            {
                foreach (Match s in LaySo.Matches(dong[(mc.Index + mc.Length)..]))
                {
                    ra.Add(new SecretHit("công cụ+số", s.Value,
                        batDau + mc.Index + mc.Length + s.Index, s.Length));
                }
            }
        }

        // Hình dạng 3 — VIẾT LẠI 2026-09-11 (`AR-o`), và nó KHÔNG còn dùng từ khoá nào.
        //
        // Bản cũ: từ một dòng có tên công cụ remote, nhìn xuôi 6 dòng tìm dãy số trần.
        // Đo trên 345 mẩu / 252 387 ký tự thì bản cũ bắt 12/29 chỗ. Ba kiểu sót, và cả ba
        // đều đánh vào cùng một điểm yếu — luật phải ĐỌC ĐƯỢC CHỮ mới bắt được số:
        //   · neo đúng chính tả nhưng ở xa (30 dòng, vì khối tự-trả-lời của Zalo chen vào)
        //   · neo là biến thể gõ sai chưa có trong danh sách ("ulatrview")
        //   · neo nằm ở MẨU KHÁC của cùng case, hoặc KHÔNG CÓ neo nào
        // Nới cửa sổ lên 40 dòng chỉ lên 18/29; đảo mặc định "che cả mẩu có nhắc công cụ"
        // cũng chỉ lên 20/29. Mọi luật dựa vào từ khoá đều thua, vì cùng một lý do.
        //
        // Thứ nhận ra chúng là HÌNH DẠNG TRÌNH BÀY, không phải chữ: **một dòng mà toàn bộ
        // nội dung chỉ là một dãy số thì luôn là credential** — 25/25 trên corpus đó. Số
        // của nghiệp vụ (số hoá đơn, mã đặt phòng, MST) luôn đi kèm chữ trong câu; chỉ ID
        // và mật khẩu mới được dán đứng một mình. Luật này bắt 29/29, che nhầm 0.
        //
        // ⚠ Và vì nó không có danh sách từ khoá nào, nó không có gì để phải vá khi biến thể
        //   gõ sai thứ bảy xuất hiện. Đó là lý do chính chọn nó, không phải con số 29.
        var dongs = TachDong(sach).ToList();
        for (var i = 0; i < dongs.Count; i++)
        {
            var (dong, batDau) = dongs[i];
            if (!SoTran.IsMatch(dong))
            {
                continue;
            }

            var soMatch = LaySo.Match(dong);
            if (soMatch.Success)
            {
                ra.Add(new SecretHit("số trần", soMatch.Value,
                    batDau + soMatch.Index, soMatch.Length));
            }

            // Nửa kia của cặp đăng nhập hay đứng NGAY TRƯỚC và hay nằm TRONG một câu:
            // "@Cs Ezcloud 10000007", "ultra : 100000008". Bắt được 4 ID thật, che nhầm 0.
            // Đếm lùi theo dòng CÓ NỘI DUNG — dòng trống và dòng tiêu đề tin nhắn không
            // tính, nếu không thì một khối tiêu đề là hết ngân sách 3 dòng.
            var daXet = 0;
            for (var k = i - 1; k >= 0 && daXet < 3; k--)
            {
                var (truoc, batDauTruoc) = dongs[k];
                if (string.IsNullOrWhiteSpace(truoc) || TieuDeTinNhan.IsMatch(truoc))
                {
                    continue;
                }

                daXet++;
                foreach (Match s in LaySo.Matches(truoc))
                {
                    // Dòng chỉ-có-số đã được chính vòng ngoài bắt rồi; đừng bắt hai lần.
                    if (!SoTran.IsMatch(truoc))
                    {
                        ra.Add(new SecretHit("số kề dòng số trần", s.Value,
                            batDauTruoc + s.Index, s.Length));
                    }
                }
            }
        }

        foreach (Match m in KeyJson.Matches(sach))
        {
            var g = m.Groups[2];
            ra.Add(new SecretHit("key JSON", g.Value, g.Index, g.Length));
        }

        // Gộp trùng: một giá trị có thể bị hai hình dạng bắt cùng lúc (ví dụ dãy số vừa
        // đứng sau nhãn vừa đứng trần ở dòng sau). Che hai lần thì lệch vị trí.
        return ra
            .GroupBy(h => (h.Index, h.Length))
            .Select(g => g.First())
            .OrderBy(h => h.Index)
            .ToList();
    }

    /// <summary>Tách thành dòng kèm offset của từng dòng trong văn bản gốc.</summary>
    private static IEnumerable<(string Dong, int BatDau)> TachDong(string s)
    {
        var batDau = 0;
        while (batDau <= s.Length)
        {
            var het = s.IndexOf('\n', batDau);
            if (het < 0)
            {
                yield return (s[batDau..], batDau);
                yield break;
            }

            var dai = het - batDau;
            if (dai > 0 && s[het - 1] == '\r')
            {
                dai--;
            }

            yield return (s.Substring(batDau, dai), batDau);
            batDau = het + 1;
        }
    }
}
