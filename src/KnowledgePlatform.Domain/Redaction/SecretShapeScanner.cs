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
///   <item>`&lt;nnn nnn nnn&gt;` rồi `&lt;5 số&gt;` ở hai tin nhắn liền nhau — dãy số TRẦN</item>
///   <item>`"Account": "&lt;tên tài khoản&gt;"` — key JSON, và key KHÔNG phải password</item>
/// </list>
///
/// ⚠ RECALL LÀ CẬN TRÊN, KHÔNG PHẢI ƯỚC LƯỢNG ĐÚNG. Bản Python đo được 13/13 trên corpus
/// thật, nhưng luật được sửa THEO chính corpus dùng để đo nó — overfit theo định nghĩa.
/// Corpus 12 tháng đã lộ ra hình dạng thứ NĂM (JWT sau chữ "Token:") mà luật bắt được nhờ
/// `token` tình cờ có trong danh sách key JSON, không nhờ thiết kế. Hình dạng thứ sáu sẽ
/// tới, và nó sẽ không may như vậy.
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

        // Hình dạng 3 phải chạy trên TOÀN BỘ danh sách dòng, vì nó nhìn TỚI TRƯỚC vài dòng:
        // khách gõ ID ở một tin nhắn rồi mật khẩu ở tin nhắn kế tiếp, không từ khoá nào ở
        // dòng thứ hai. Đây là hình dạng làm luật "cùng dòng" chỉ bắt được 11%.
        var dongs = TachDong(sach).ToList();
        for (var i = 0; i < dongs.Count; i++)
        {
            var (dong, _) = dongs[i];
            if (!CongCu.IsMatch(dong) && !NhanVaSo.IsMatch(dong))
            {
                continue;
            }

            for (var k = i + 1; k < Math.Min(i + 7, dongs.Count); k++)
            {
                var (sau, batDauSau) = dongs[k];
                var mt = SoTran.Match(sau);
                if (!mt.Success)
                {
                    continue;
                }

                var soMatch = LaySo.Match(sau);
                if (soMatch.Success)
                {
                    ra.Add(new SecretHit("số trần", soMatch.Value,
                        batDauSau + soMatch.Index, soMatch.Length));
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
