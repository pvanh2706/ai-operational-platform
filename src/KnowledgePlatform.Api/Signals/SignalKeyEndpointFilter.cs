using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Cái chốt tạm trên endpoint tín hiệu, trong lúc `AR-e` chưa được quyết.
///
/// **Nó không phải câu trả lời cho `AR-e`.** Một khoá dùng chung không phân biệt
/// được khách A với khách B ở chế độ shared, không thu hồi được theo từng khách,
/// và không chống được replay. Nó chỉ làm một việc: endpoint GHI không mở toang
/// ra internet trong lúc chờ quyết định thật.
///
/// Nếu không cấu hình khoá thì ứng dụng đã không khởi động được (`StartupChecks`),
/// trừ khi có người thừa nhận tường minh. Nên tới được đây có hai trạng thái, và
/// cả hai đều là trạng thái đã biết, không phải trạng thái bị bỏ sót.
/// </summary>
public sealed class SignalKeyEndpointFilter(IOptions<IngestOptions> options) : IEndpointFilter
{
    private readonly IngestOptions _options = options.Value;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = _options.SignalApiKey;

        // Không có khoá = đã được thừa nhận tường minh lúc khởi động. Đi tiếp.
        if (string.IsNullOrEmpty(expected)) return await next(context);

        var provided = context.HttpContext.Request.Headers[_options.ApiKeyHeader].ToString();

        if (!FixedTimeEquals(provided, expected))
        {
            // Không nói là thiếu khoá hay khoá sai — hai câu trả lời khác nhau là
            // một kênh dò khoá.
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Không được phép gửi tín hiệu",
                detail: $"Thiếu hoặc sai header {_options.ApiKeyHeader}.");
        }

        return await next(context);
    }

    /// <summary>
    /// So sánh theo thời gian hằng số. So bằng <c>==</c> thoát sớm ở ký tự đầu khác
    /// nhau, và thời gian đó đo được — đủ để dò khoá từng ký tự.
    /// </summary>
    private static bool FixedTimeEquals(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
}
