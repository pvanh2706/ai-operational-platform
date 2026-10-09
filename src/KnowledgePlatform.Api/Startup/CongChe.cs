using KnowledgePlatform.Domain.Redaction;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Startup;

public sealed class CongCheOptions
{
    public const string SectionName = "CongChe";

    /// <summary>
    /// `N1-2` (`docs/18`, chốt 2026-10-10): TẮT TẠM mọi cổng che dữ liệu. Mặc định <c>false</c>
    /// — tức cổng VẪN CHẠY.
    ///
    /// 🛑 Tên dài cố ý: bật cờ này là một QUYẾT ĐỊNH, không phải một tuỳ chọn vận hành. Nó mở lại
    /// điều kiện chính chủ dự án đặt ngày 2026-09-08 (`07` §5 — chỉ gửi dữ liệu ticket cho model
    /// khi luật che chạy trước mỗi lần gọi), và corpus thật có credential ở 5,2% mẩu evidence.
    /// Bật lên thì các giá trị ấy đi thẳng tới Anthropic và vào hệ thống của khách.
    /// </summary>
    public bool TatTamVaChapNhanRuiRo { get; set; }
}

/// <summary>
/// MỘT cửa cho mọi lần che dữ liệu trong host — để cờ <see cref="CongCheOptions"/> chỉ cần đọc
/// ở một chỗ.
///
/// ⚠ Hai loại che, ở hai khâu KHÁC HẲN NHAU về chuyện quay lại được hay không:
/// <list type="bullet">
///   <item><b>Đường RA</b> (<see cref="CheDuongRa"/>) — payload gửi model, gói webhook, trang
///     <c>/xem</c>, file xuất cho ngữ cảnh AI. Che trên BẢN SAO lúc gửi, bản trong kho không đổi.
///     Tắt cờ đi là lượt sau lại được che: <b>cửa hai chiều</b>.</item>
///   <item><b>Khâu NẠP</b> (<see cref="CheBenThuBa"/>) — <see cref="ThirdPartyRedactor"/> che
///     trước khi nội dung chạm database, và KHÔNG giữ bản gốc. Mẩu vào lúc cờ bật thì nằm trong
///     kho KHÔNG CHE, tắt cờ đi cũng không che lại được: <b>cửa MỘT CHIỀU</b>. Vì thế mỗi mẩu mang
///     dấu <c>DaCheBenThuBa</c> để về sau còn biết mẩu nào vào lúc nào.</item>
/// </list>
///
/// ⚠ Lớp này nằm ở tầng Api chứ không ở Domain: Domain KHÔNG đọc cấu hình. Ở Domain, cờ đi vào
/// bằng một tham số tường minh (<c>SopPromptBuilder.Build(…, quaCongChe)</c>), mặc định an toàn.
/// </summary>
public sealed class CongChe(IOptions<CongCheOptions> options)
{
    private static readonly IReadOnlyDictionary<string, int> KhongCoHinhDang = new Dictionary<string, int>();

    /// <summary>Cờ đang bật — mọi cổng che bị bỏ qua.</summary>
    public bool DangTat => options.Value.TatTamVaChapNhanRuiRo;

    /// <summary>Dữ liệu ra khỏi host lúc này CÓ đi qua cổng che không. Đây là giá trị in vào <c>daQuaCongChe</c>.</summary>
    public bool DaQuaCongChe => !DangTat;

    /// <summary>
    /// Che ở đường RA (`AR-o`). Cờ bật thì trả nguyên văn, đếm 0 chỗ.
    /// ⚠ Cờ tắt thì vẫn có thể ném <see cref="EgressBlockedException"/> — fail closed như cũ.
    /// </summary>
    public RedactionResult CheDuongRa(string? noiDung) =>
        DangTat
            ? new RedactionResult(noiDung ?? string.Empty, 0, KhongCoHinhDang)
            : EgressRedactor.Redact(noiDung);

    /// <summary>Che dữ liệu BÊN THỨ BA ở khâu NẠP (`AR-n` B2a). Cờ bật thì nội dung vào kho nguyên văn.</summary>
    public RedactionResult CheBenThuBa(string? noiDung) =>
        DangTat
            ? new RedactionResult(noiDung ?? string.Empty, 0, KhongCoHinhDang)
            : ThirdPartyRedactor.Redact(noiDung);
}
