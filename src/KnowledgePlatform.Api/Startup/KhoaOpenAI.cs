namespace KnowledgePlatform.Api.Startup;

/// <summary>
/// Đọc khoá + model OpenAI từ cấu hình — `Q1-1`/`Q2-1`/`Q7-1` (2026-10-22). Cùng khuôn <see cref="KhoaAnthropic"/>: MỘT chỗ
/// gom hai tên về, để không lặp lại lỗi "host kiểm một tên, client đọc một tên khác" của 2026-10-10.
///
/// ⚠ Khoá đến từ <see cref="IConfiguration"/>: biến môi trường, hoặc <c>appsettings.Local.json</c> ở máy dev (`O2-1`,
///   cùng cổng <c>git check-ignore</c>). Giá trị mẫu bị coi là KHÔNG có khoá.
/// 🛑 Model KHÔNG có mặc định (`Q2-1`): có khoá mà thiếu model thì OpenAI coi như CHƯA dùng được — không có tên nào được
///   đoán hộ. Đổi model là câu `AGENT.md` §10.1.
/// </summary>
public static class KhoaOpenAI
{
    /// <summary>Tên trong cấu hình .NET (biến môi trường <c>OpenAI__ApiKey</c>).</summary>
    public const string TenCauHinh = "OpenAI:ApiKey";

    /// <summary>Tên mà công cụ của OpenAI dùng.</summary>
    public const string TenMoiTruong = "OPENAI_API_KEY";

    /// <summary>Model — BẮT BUỘC, không mặc định (`Q2-1`).</summary>
    public const string TenCauHinhModel = "OpenAI:Model";

    /// <summary>Khoá, hoặc <c>null</c> nếu cả hai tên đều trống hoặc còn giá trị mẫu.</summary>
    public static string? Doc(IConfiguration cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        var khoa = cfg[TenCauHinh];
        if (string.IsNullOrWhiteSpace(khoa)) khoa = cfg[TenMoiTruong];

        if (string.IsNullOrWhiteSpace(khoa) || khoa.Contains(KhoaAnthropic.DauHieuGiaTriMau, StringComparison.Ordinal))
        {
            return null;
        }

        return khoa.Trim();
    }

    /// <summary>Tên model, hoặc <c>null</c> khi chưa đặt.</summary>
    public static string? Model(IConfiguration cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        var m = cfg[TenCauHinhModel];
        return string.IsNullOrWhiteSpace(m) ? null : m.Trim();
    }
}
