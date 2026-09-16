using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Tenancy;

/// <summary>
/// Cấu hình xác thực người gọi (`AR-e`).
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Cho phép chạy API **không** cần khoá. Dành cho máy dev và bộ test.
    ///
    /// ⚠ Cờ này **bị từ chối ở chế độ <c>SharedMultiTenant</c>** — xem <c>StartupChecks</c>.
    /// Ở chế độ shared, khoá API là thứ DUY NHẤT nói được request thuộc khách hàng nào, nên
    /// một cái cờ không thể thay nó: bật lên thì không còn tenant nào để phục vụ. Đây là khác
    /// biệt so với hai cờ thừa nhận cũ (`IM-13`), vốn chỉ làm sơ suất thành quyết định.
    /// </summary>
    public bool AcknowledgeUnauthenticatedApi { get; set; }

    /// <summary>Header mang khoá. Mặc định theo chuẩn HTTP, để bên tích hợp không phải học gì mới.</summary>
    public string HeaderName { get; set; } = "Authorization";

    public string Scheme { get; set; } = "Bearer";
}

/// <summary>
/// Người gọi của MỘT request — kết quả của lượt xác thực, giữ ở phạm vi scoped.
///
/// Tách khỏi <see cref="RequestTenantContext"/> vì hai thứ này trả lời hai câu khác nhau,
/// và gộp chúng chính là cái lỗi mà `AR-e` sinh ra để sửa:
/// <code>
///   RequestTenantContext  request này thuộc CÔNG TY nào
///   CallerIdentity        ai đang gọi, và đã chứng minh được chưa
/// </code>
/// Trước `AR-e`, hệ thống chỉ có vế trên và lấy nó từ một header người gọi tự điền.
/// </summary>
public sealed class CallerIdentity
{
    public Guid? ApiKeyId { get; private set; }

    public bool DaXacThuc => ApiKeyId.HasValue;

    internal void XacNhan(Guid apiKeyId)
    {
        if (ApiKeyId.HasValue && ApiKeyId.Value != apiKeyId)
        {
            throw new InvalidOperationException(
                "Người gọi của request này đã được xác định và đang bị đổi. Một request có đúng một người gọi.");
        }

        ApiKeyId = apiKeyId;
    }
}

/// <summary>
/// Đòi request phải mang một khoá API còn hiệu lực. Thay cho <c>SignalKeyEndpointFilter</c>.
///
/// Khác biệt so với cái cũ, và đây mới là chỗ `AR-e` đóng lại: khoá cũ trả lời *"có biết mật
/// khẩu chung không"*, khoá này trả lời *"là khách hàng NÀO"* — nên nó vừa xác thực vừa là
/// nguồn của tenant, và header <c>X-Tenant-Key</c> không còn lý do tồn tại.
/// </summary>
public sealed class ApiKeyEndpointFilter(CallerIdentity caller, IOptions<AuthOptions> options) : IEndpointFilter
{
    private readonly AuthOptions _options = options.Value;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (caller.DaXacThuc) return await next(context);

        // Đã được thừa nhận tường minh lúc khởi động (dev/test, và chỉ ở chế độ dedicated).
        if (_options.AcknowledgeUnauthenticatedApi) return await next(context);

        // Một câu trả lời cho MỌI kiểu hỏng: thiếu khoá, sai khoá, khoá đã thu hồi, khoá của
        // khách khác. Nói ra nó hỏng ở đâu là dựng sẵn một kênh dò.
        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Không được phép gọi endpoint này",
            detail: $"Thiếu hoặc không dùng được khoá API. Gửi kèm: {_options.HeaderName}: {_options.Scheme} kp_<id>_<secret>");
    }
}
