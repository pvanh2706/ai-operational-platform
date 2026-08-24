using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Tenancy;

/// <summary>
/// Tenant của bản deploy dedicated, tra MỘT LẦN lúc khởi động và đã đối chiếu với
/// bảng <c>kp.tenant</c> — khoá cấu hình sai thì ứng dụng không start được.
///
/// <c>TenantId == null</c> nghĩa là bản deploy này KHÔNG ở chế độ dedicated. Luôn
/// được đăng ký trong DI (kể cả khi null) để chỗ dùng không phải đoán xem có hay không.
///
/// ⚠ Đặt được đúng MỘT lần, bởi <c>StartupChecks</c>, trước khi có request đầu tiên.
/// Đây là chỗ duy nhất trong hệ thống có một giá trị tenant sống lâu hơn một
/// request — nên nó phải đóng lại được, và đặt lần thứ hai là ném.
/// </summary>
public sealed class DedicatedTenant
{
    private Guid? _tenantId;
    private bool _assigned;

    public Guid? TenantId => _tenantId;

    internal void Assign(Guid? tenantId)
    {
        if (_assigned)
        {
            throw new InvalidOperationException(
                "Tenant của bản deploy dedicated đã được đặt. Giá trị này chỉ đặt một lần lúc khởi động.");
        }

        _tenantId = tenantId;
        _assigned = true;
    }
}

/// <summary>
/// Xác định tenant cho mỗi request, theo chế độ deploy đã cấu hình (`G13`).
///
/// ⚠ Phải chạy TRƯỚC mọi thứ chạm vào <c>AppDbContext</c>. Nếu chạy sau, connection
/// đã mở với tenant rỗng và policy RLS sẽ từ chối mọi thứ — an toàn, nhưng biểu
/// hiện ra thành "không thấy dữ liệu" rất khó truy.
///
/// Middleware này KHÔNG chặn request thiếu tenant. Hai lý do:
///   · <c>/health</c> phải trả lời được khi chưa có tenant nào;
///   · nếu một endpoint quên kiểm tra, RLS vẫn chặn (thấy 0 dòng, `IM-6`) —
///     tức là quên ở đây không thành lỗ rò.
/// Endpoint nào cần tenant thì tự đòi, bằng <see cref="TenantEndpointFilter"/>.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next, IOptions<TenancyOptions> options)
{
    private readonly TenancyOptions _options = options.Value;

    public async Task InvokeAsync(
        HttpContext context,
        RequestTenantContext tenantContext,
        TenantDirectory directory,
        DedicatedTenant dedicated)
    {
        switch (_options.Mode)
        {
            case TenancyMode.DedicatedSingleTenant:
                // Tenant từ CẤU HÌNH — nhánh mà `G13` cho phép. Đã tra và xác nhận
                // tồn tại lúc khởi động, nên ở đây không có truy vấn nào.
                tenantContext.Resolve(dedicated.TenantId ?? throw new InvalidOperationException(
                    "Chế độ dedicated nhưng tenant chưa được tra lúc khởi động."));
                break;

            case TenancyMode.SharedMultiTenant:
                var key = context.Request.Headers[_options.TenantKeyHeader].ToString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    var id = await directory.FindIdByExternalKeyAsync(key, context.RequestAborted);

                    // Khoá lạ → KHÔNG resolve. Cố ý không trả 404 ở đây: "khoá này
                    // có tồn tại không" là thông tin về khách hàng khác, và endpoint
                    // sẽ trả 400 chung chung qua TenantEndpointFilter.
                    if (id.HasValue) tenantContext.Resolve(id.Value);
                }
                break;

            case TenancyMode.Unspecified:
            default:
                // Không tới được: StartupChecks đã từ chối khởi động. Để đây cho rõ
                // rằng trường hợp này là KHÔNG THỂ, không phải bị bỏ qua.
                throw new InvalidOperationException(
                    "Tenancy:Mode chưa được cấu hình. Ứng dụng đáng ra không khởi động được.");
        }

        await next(context);
    }
}

/// <summary>
/// Đòi request phải có tenant đã xác định. Gắn vào endpoint bằng
/// <c>.AddEndpointFilter&lt;TenantEndpointFilter&gt;()</c>.
///
/// Đây là tầng "báo lỗi cho tử tế", KHÔNG phải tầng bảo mật. Tầng bảo mật là RLS.
/// </summary>
public sealed class TenantEndpointFilter(RequestTenantContext tenantContext) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!tenantContext.IsResolved)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Không xác định được khách hàng của yêu cầu này",
                detail: "Thiếu hoặc không nhận ra khoá tenant. Xem cấu hình Tenancy của bản deploy này.");
        }

        return await next(context);
    }
}
