using System.Text.RegularExpressions;

namespace KnowledgePlatform.Domain.Redaction;

/// <summary>
/// Che dữ liệu của BÊN THỨ BA ở khâu NẠP — `AR-n`, chốt 2026-09-15 (phiếu `docs/14` §B2 = B2a-1).
///
/// ⚠ **ĐÂY LÀ RANH GIỚI THỨ BA, và đừng nhầm nó với hai cái kia.**
/// <list type="bullet">
///   <item>`AR-j` / <c>check_corpus.py</c> — che **BÍ MẬT** (credential) lúc nạp. Câu hỏi:
///     "cái này có phải mật khẩu không?"</item>
///   <item>`AR-o` / <see cref="EgressRedactor"/> — cổng lúc **GỬI RA** ngoài máy. Fail closed,
///     vì không có bước người xem lại và không có đường thu hồi.</item>
///   <item>**Ở đây** — dữ liệu không phải bí mật, không phải của ta, và **chủ của nó chưa
///     từng được hỏi**. `ES-346481#comment-802977` chứa payload hoá đơn với khối
///     <c>&lt;NMua&gt;</c>: tên một ĐƠN VỊ SỰ NGHIỆP NHÀ NƯỚC, mã số thuế, địa chỉ đầy đủ.
///     Đó là dữ liệu **khách CỦA khách sạn** — che credential xong thì vẫn còn công bố
///     một khoản chi lưu trú của một tổ chức có tên.</item>
/// </list>
/// `AR-l` chốt ranh giới HAI cấp (ezCloud → khách sạn); đây là **cấp thứ ba** nó chưa mô tả.
///
/// 🛑 **CHE THEO CẤU TRÚC, KHÔNG THEO TỪ KHOÁ — và đó là bài học đã trả giá.**
/// `AR-j` đo được rằng luật theo từ khoá cùng dòng chỉ bắt **11%**. Ở đây từ khoá còn tệ hơn:
/// chuỗi <c>MST</c> xuất hiện trần trong **10/345** mẩu fixture, và phần lớn là **MST của
/// chính khách sạn** — một định danh CÔNG KHAI mà <c>make_fixture.py</c> cố ý GIỮ LẠI
/// (<c>4500621073</c>, ghi rõ "định danh công khai"). Một luật bắt theo chữ "MST" sẽ che mất
/// đúng dữ liệu cần giữ. Luật ở đây chỉ nhìn **vị trí trong cây XML**: chỉ những giá trị nằm
/// TRONG khối <c>&lt;NMua&gt;</c>.
///
/// 🛑 **CHE ĐỊNH DANH, GIỮ SỐ TIỀN — và đây là quyết định có giá, không phải cẩu thả.**
/// Khối <c>&lt;NMua&gt;</c> là bên MUA; các dòng hàng (<c>&lt;DSHHDVu&gt;</c>) và tổng tiền
/// (<c>&lt;TgTTTBSo&gt;</c>) nằm NGOÀI nó và **không bị đụng**. Lý do: nhóm SOP dày nhất trong
/// 19 nhóm là *"Lệch cấu hình tiền/thuế"* (8 case, 63 mẩu), và nó chẩn đoán bằng cách **mở
/// cùng một hoá đơn ở ba nơi rồi so từng cột tiền**. Che số tiền là vứt đúng bằng chứng của
/// nhóm dày nhất — đây sẽ là lần thứ BA một "bộ lọc nghe rất hợp lý" vứt đúng phần có giá
/// trị, sau *"chỉ lấy case đã đóng"* và *"chỉ lấy phần khách đọc được"*.
/// → Bỏ định danh đi thì phần còn lại là một hoá đơn VÔ DANH: vẫn tra được mã lỗi, vẫn so
///   được cột tiền, mà không còn pháp nhân nào bị nêu tên.
///
/// ⚠ **GIỮ NGUYÊN THẺ, CHỈ THAY GIÁ TRỊ** (`G6`/`AP3`: giữ dấu). Người đọc phải phân biệt
/// được "đã che" với "không có gì" — và ở đây nó còn mang giá trị chẩn đoán thật: nhóm 2
/// (*NCC từ chối payload*) hỏng đúng vì **trường nào thiếu / trường nào rỗng**, nên việc
/// <c>&lt;SDThoai&gt;</c> rỗng hay <c>&lt;STKNHang /&gt;</c> tự đóng phải còn nhìn thấy được.
/// → Vì thế **thẻ rỗng thì KHÔNG bị che**: che một thẻ vốn đã rỗng là biến "không có" thành
///   "có nhưng bị giấu", tức tự tay tạo ra đúng sự nhầm lẫn mà luật này đang chống.
///
/// 🛑 **ĐỘ PHỦ: n = 1. ĐỌC TRƯỚC KHI TIN.** Trên 873 mẩu thật (345 fixture + 528 corpus 12
/// tháng), <c>&lt;NMua&gt;</c> xuất hiện ở **đúng MỘT mẩu**. Một mẫu là đủ để chứng minh loại
/// dữ liệu này TỒN TẠI (và `AR-n` được chốt trên chính nó), nhưng KHÔNG đủ để nói luật này
/// phủ được bao nhiêu. Hình dạng thứ hai sẽ tới, và nó sẽ không mang thẻ <c>NMua</c>.
/// → Nên đừng đọc "0 chỗ bị che" thành "mẩu này sạch". Nó chỉ nghĩa là luật không thấy gì.
/// </summary>
public static class ThirdPartyRedactor
{
    /// <summary>
    /// Dấu thay cho giá trị đã che. KHÁC dấu của <see cref="EgressRedactor"/> một cách có chủ
    /// đích: hai cổng che vì hai lý do khác nhau, và người đọc bản nháp cần phân biệt được
    /// "đây là bí mật kỹ thuật" với "đây là người ngoài không liên quan".
    /// </summary>
    public const string Marker = "[ĐÃ CHE: bên thứ ba]";

    private const RegexOptions Opts =
        RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.IgnoreCase;

    /// <summary>
    /// Khối bên MUA. <c>IgnoreCase</c> vì bản xuất của nhà cung cấp không đảm bảo hoa thường,
    /// và một khối lọt lưới vì viết <c>&lt;nmua&gt;</c> là một pháp nhân bị nêu tên.
    /// </summary>
    private static readonly Regex KhoiBenMua = new(@"<NMua\b[^>]*>(?<ruot>.*?)</NMua\s*>", Opts);

    /// <summary>
    /// Một thẻ con CÓ nội dung. <c>[^&lt;]</c> ở phần ruột là cố ý: nó chỉ khớp thẻ LÁ, nên
    /// một thẻ lồng (nếu nhà cung cấp đổi cấu trúc) sẽ KHÔNG bị nuốt cả cụm thành một dấu che.
    /// </summary>
    private static readonly Regex TheCoNoiDung = new(@"<(?<ten>[A-Za-z][\w.-]*)\b[^>/]*>(?<giatri>[^<]*)</\k<ten>\s*>", Opts);

    /// <summary>
    /// Rỗng theo nghĩa của corpus NÀY, không theo nghĩa của <c>string.IsNullOrWhiteSpace</c>.
    ///
    /// 🛑 **Bản đầu dùng thẳng <c>IsNullOrWhiteSpace</c> và nó SAI trên dữ liệu thật — lỗi chỉ
    /// lộ ra khi CHẠY, không lộ ra trong test.** Bản xuất Jira mang xuống dòng ở dạng **chuỗi
    /// thoát nguyên văn**: một thẻ rỗng trong corpus mang BỐN ký tự — gạch chéo, chữ r,
    /// gạch chéo, chữ n — chứ không phải hai ký tự xuống dòng thật.
    /// <c>IsNullOrWhiteSpace</c> thấy có gạch chéo và chữ nên nói "không rỗng",
    /// rồi một thẻ vốn trống bị dán dấu che — tức đúng sự nhầm lẫn mà guard này sinh ra để
    /// chặn, chỉ khác là nó tự gây ra.
    /// → Bài học, và nó là bài học của cả repo: test tự dựng vật liệu cho mình chỉ kiểm được
    ///   những hình dạng mà người viết test NGHĨ RA. Test cũ viết xuống dòng thật vì tay người
    ///   viết luôn gõ xuống dòng thật — y hệt ca `+07:00` của `IM-24`, nơi 103 test cũ không
    ///   thấy bug vì tay người viết test luôn viết UTC.
    /// </summary>
    private static bool LaRong(string giaTri) =>
        string.IsNullOrWhiteSpace(
            giaTri.Replace("\\r", string.Empty, StringComparison.Ordinal)
                  .Replace("\\n", string.Empty, StringComparison.Ordinal)
                  .Replace("\\t", string.Empty, StringComparison.Ordinal));

    /// <summary>
    /// Che mọi giá trị nằm trong khối <c>&lt;NMua&gt;</c>. Trả về văn bản đã che, số chỗ đã
    /// che, và chỗ nào là thẻ gì — để ĐO, không để log giá trị.
    /// </summary>
    public static RedactionResult Redact(string? content)
    {
        if (string.IsNullOrEmpty(content) || !content.Contains("NMua", StringComparison.OrdinalIgnoreCase))
        {
            return new RedactionResult(content ?? string.Empty, 0, new Dictionary<string, int>());
        }

        var shapes = new Dictionary<string, int>(StringComparer.Ordinal);

        var text = KhoiBenMua.Replace(content, khoi =>
        {
            var ruotDaChe = TheCoNoiDung.Replace(khoi.Groups["ruot"].Value, the =>
            {
                var giaTri = the.Groups["giatri"].Value;

                // ⚠ Thẻ RỖNG thì để yên. Che nó là biến "không có" thành "có nhưng bị giấu",
                // và chính sự rỗng đó mới là thứ nhóm 2 cần nhìn thấy để chẩn đoán.
                if (LaRong(giaTri)) return the.Value;

                // Đã che rồi thì không che chồng — nạp lại cùng một mẩu không được làm con
                // số phồng lên, và dấu che không được lồng nhau.
                if (giaTri.Contains(Marker, StringComparison.Ordinal)) return the.Value;

                var ten = the.Groups["ten"].Value;
                shapes[$"NMua/{ten}"] = shapes.GetValueOrDefault($"NMua/{ten}") + 1;

                return the.Value.Replace(giaTri, Marker, StringComparison.Ordinal);
            });

            return khoi.Value.Replace(khoi.Groups["ruot"].Value, ruotDaChe, StringComparison.Ordinal);
        });

        return new RedactionResult(text, shapes.Values.Sum(), shapes);
    }
}
