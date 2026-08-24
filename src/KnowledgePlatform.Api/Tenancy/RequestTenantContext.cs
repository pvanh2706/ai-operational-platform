using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Api.Tenancy;

/// <summary>
/// Cài đặt <see cref="ITenantContext"/> cho MỘT request. Đăng ký ở phạm vi
/// scoped, nên mỗi request có một thể hiện riêng.
///
/// Nó chỉ là chỗ GIỮ giá trị. Việc tra cứu (đọc header, hỏi danh bạ) do
/// <see cref="TenantResolutionMiddleware"/> làm — cố ý tách ra, vì tra cứu là
/// việc bất đồng bộ và có thể thất bại, còn <see cref="ITenantContext.TenantId"/>
/// là một property đồng bộ. Nhồi một truy vấn database vào getter của property
/// là cách tạo ra những chỗ chậm không ai tìm được.
///
/// Ở cả hai chế độ deploy của `G13`, phần còn lại của hệ thống chỉ thấy
/// <see cref="ITenantContext"/> — không đường code nào biết mình đang chạy chế độ nào.
/// </summary>
public sealed class RequestTenantContext : ITenantContext
{
    private Guid? _tenantId;

    public bool IsResolved => _tenantId.HasValue;

    public Guid TenantId => _tenantId ?? throw new InvalidOperationException(
        "Tenant chưa được xác định cho request này. Không truy vấn dữ liệu khách hàng " +
        "ở trạng thái này — xem TenantResolutionMiddleware.");

    /// <summary>
    /// Gọi MỘT lần cho mỗi request, bởi middleware. Gọi lần thứ hai là ném: đổi
    /// tenant giữa request là đúng loại lỗi mà `G7` gọi là rò rỉ im lặng.
    /// </summary>
    public void Resolve(Guid tenantId)
    {
        if (_tenantId.HasValue && _tenantId.Value != tenantId)
        {
            throw new InvalidOperationException(
                $"Tenant của request này đã được xác định là {_tenantId} và đang bị đổi " +
                $"thành {tenantId}. Một request phục vụ đúng một khách hàng.");
        }

        _tenantId = tenantId;
    }
}
