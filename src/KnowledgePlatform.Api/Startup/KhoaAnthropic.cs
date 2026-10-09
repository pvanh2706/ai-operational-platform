namespace KnowledgePlatform.Api.Startup;

/// <summary>
/// Đọc khoá Anthropic từ cấu hình — MỘT chỗ cho mọi đường gọi model (soạn nháp, định tuyến).
///
/// 🛑 Vì sao có lớp này: tới 2026-10-10 host KIỂM <c>Anthropic:ApiKey</c> để chọn bản hiện
/// thực, rồi dựng <c>new AnthropicClient()</c> — mà client đó tự đọc biến môi trường
/// <c>ANTHROPIC_API_KEY</c>, KHÔNG đọc <c>Anthropic:ApiKey</c>. Hai tên, hai chỗ đọc:
/// <list type="bullet">
///   <item>chỉ đặt <c>Anthropic:ApiKey</c> → host tưởng có khoá, client gọi không khoá → lỗi
///     xác thực ở lượt gọi đầu, KHÔNG đi vào nhánh 409 "chưa có credit" đã thiết kế;</item>
///   <item>chỉ đặt <c>ANTHROPIC_API_KEY</c> → host tưởng KHÔNG có khoá → trả "chưa có credit"
///     trong khi khoá đang nằm sẵn trên máy.</item>
/// </list>
/// Cả hai đều không crash lúc khởi động. Đúng loại lỗi im lặng mà dự án dựng ra để chặn.
///
/// ⚠ Khoá vẫn KHÔNG đi qua appsettings hay log: nó đến từ <see cref="IConfiguration"/>, nơi biến
/// môi trường đã là một nguồn sẵn. Lớp này chỉ gom HAI tên về một chỗ.
/// </summary>
public static class KhoaAnthropic
{
    /// <summary>Tên trong cấu hình .NET (biến môi trường <c>Anthropic__ApiKey</c>).</summary>
    public const string TenCauHinh = "Anthropic:ApiKey";

    /// <summary>Tên mà SDK và công cụ <c>ant</c> dùng.</summary>
    public const string TenMoiTruong = "ANTHROPIC_API_KEY";

    /// <summary>Khoá, hoặc <c>null</c> nếu cả hai tên đều trống.</summary>
    public static string? Doc(IConfiguration cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        var khoa = cfg[TenCauHinh];
        if (string.IsNullOrWhiteSpace(khoa)) khoa = cfg[TenMoiTruong];

        return string.IsNullOrWhiteSpace(khoa) ? null : khoa.Trim();
    }
}
