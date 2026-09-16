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
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    IOptions<TenancyOptions> options,
    IOptions<AuthOptions> authOptions)
{
    private readonly TenancyOptions _options = options.Value;
    private readonly AuthOptions _auth = authOptions.Value;

    public async Task InvokeAsync(
        HttpContext context,
        RequestTenantContext tenantContext,
        ApiKeyDirectory apiKeys,
        CallerIdentity caller,
        DedicatedTenant dedicated)
    {
        // Xác thực TRƯỚC, vì ở chế độ shared chính lượt này quyết định tenant.
        var khoa = await XacThucAsync(context, apiKeys);

        switch (_options.Mode)
        {
            case TenancyMode.DedicatedSingleTenant:
                // Tenant từ CẤU HÌNH — nhánh mà `G13` cho phép. Đã tra và xác nhận
                // tồn tại lúc khởi động, nên ở đây không có truy vấn nào.
                var cuaBanDeploy = dedicated.TenantId ?? throw new InvalidOperationException(
                    "Chế độ dedicated nhưng tenant chưa được tra lúc khởi động.");

                tenantContext.Resolve(cuaBanDeploy);

                // ⚠ Khoá HỢP LỆ của khách KHÁC vẫn là khoá không dùng được ở đây. Không có
                // dòng này thì một bản deploy dedicated của khách A chấp nhận khoá của khách
                // B — request sẽ chạy dưới tenant A, tức khoá của B ghi vào dữ liệu của A.
                // RLS không cứu được: nó chỉ biết tenant đã resolve, không biết ai đưa ra nó.
                if (khoa is { ConHieuLuc: true } && khoa.TenantId == cuaBanDeploy)
                {
                    caller.XacNhan(khoa.ApiKeyId);
                }
                break;

            case TenancyMode.SharedMultiTenant:
                // `AR-e` ĐÃ ĐÓNG Ở ĐÂY: tenant đến từ khoá đã kiểm, không từ một header mà
                // người gọi tự điền. Header `X-Tenant-Key` đã bị bỏ — giữ nó song song là
                // giữ nguyên lỗ hổng và thêm một đường để quên.
                if (khoa is { ConHieuLuc: true })
                {
                    tenantContext.Resolve(khoa.TenantId);
                    caller.XacNhan(khoa.ApiKeyId);
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

    /// <summary>
    /// Đọc header và tra khoá. Trả <c>null</c> cho mọi kiểu hỏng — middleware KHÔNG chặn,
    /// việc chặn là của <see cref="ApiKeyEndpointFilter"/>, cùng lý do đã ghi ở phần tóm tắt
    /// của lớp này: <c>/health</c> phải trả lời được khi chưa có gì cả.
    /// </summary>
    private async Task<ApiKeyLookup?> XacThucAsync(HttpContext context, ApiKeyDirectory apiKeys)
    {
        var raw = context.Request.Headers[_auth.HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var tienTo = _auth.Scheme + " ";
        if (!raw.StartsWith(tienTo, StringComparison.OrdinalIgnoreCase)) return null;

        return await apiKeys.XacThucAsync(raw[tienTo.Length..].Trim(), context.RequestAborted);
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
