using System.ComponentModel.DataAnnotations;

namespace KnowledgePlatform.Api.Tenancy;

/// <summary>
/// `G13`: sản phẩm phải deploy được ở CẢ HAI chế độ trên cùng một codebase, và
/// tenant luôn đến từ **cấu hình hoặc ngữ cảnh request**, không bao giờ từ hằng
/// số toàn cục.
///
/// Chỗ này là nơi hai chế độ đó gặp nhau. Cùng schema, cùng policy RLS, cùng
/// interceptor — khác duy nhất một lớp cài đặt <c>ITenantContext</c>.
/// </summary>
public sealed class TenancyOptions
{
    public const string SectionName = "Tenancy";

    /// <summary>
    /// KHÔNG có giá trị mặc định, cố ý. Quên cấu hình là **không start được**,
    /// không phải "chạy theo một giả định nào đó". Cùng tinh thần với `IM-2`:
    /// vi phạm phải là hành động tường minh.
    /// </summary>
    public TenancyMode Mode { get; set; } = TenancyMode.Unspecified;

    /// <summary>
    /// Chỉ dùng ở chế độ <see cref="TenancyMode.DedicatedSingleTenant"/>: khoá
    /// ngoài của công ty khách hàng mà bản deploy này phục vụ, khớp
    /// <c>Tenant.ExternalKey</c>.
    ///
    /// ⚠ Đây là "tenant từ CẤU HÌNH" mà `G13` cho phép, KHÔNG phải "tenant từ
    /// hằng số toàn cục" mà `G13` cấm. Khác biệt: giá trị này đi qua
    /// <c>ITenantContext</c> như mọi giá trị khác, nên không có đường code nào
    /// biết mình đang ở chế độ nào. Đổi chế độ là đổi file cấu hình.
    /// </summary>
    public string? TenantExternalKey { get; set; }

    /// <summary>
    /// Chỉ dùng ở chế độ <see cref="TenancyMode.SharedMultiTenant"/>.
    ///
    /// ⚠ **CHƯA CÓ XÁC THỰC.** Ở chế độ shared, tenant đến từ một header của
    /// người gọi, và hiện chưa có gì kiểm người gọi có quyền dùng khoá đó hay
    /// không — `AR-e` còn OPEN. Nghĩa là bất kỳ ai gọi được API đều đọc được dữ
    /// liệu của bất kỳ khách hàng nào, chỉ cần biết khoá.
    ///
    /// Nên chế độ shared **từ chối khởi động** trừ khi cờ này được đặt tường minh
    /// bằng <c>true</c>. Không phải để bảo vệ hệ thống — cờ nào cũng bật được —
    /// mà để không ai deploy nó do SƠ SUẤT. Cùng lý do với `IM-5`: cái bẫy đáng
    /// sợ là cái không ai thấy.
    /// </summary>
    public bool AcknowledgeUnauthenticatedTenantHeader { get; set; }

    /// <summary>Header mang khoá ngoài của tenant ở chế độ shared.</summary>
    [Required]
    public string TenantKeyHeader { get; set; } = "X-Tenant-Key";
}

public enum TenancyMode
{
    /// <summary>Chưa cấu hình. Ứng dụng từ chối khởi động ở trạng thái này.</summary>
    Unspecified = 0,

    /// <summary>
    /// Một bản deploy cho một khách hàng. Database ấy TÌNH CỜ chỉ chứa một tenant
    /// — policy RLS chạy y nguyên, không có gì phải sửa (`06` §3, `G13`).
    /// Đây là chế độ của khách hàng #0 (`D3`).
    /// </summary>
    DedicatedSingleTenant = 1,

    /// <summary>
    /// Một bản deploy phục vụ nhiều khách hàng, tenant đến từ ngữ cảnh request.
    /// Chưa có xác thực — xem <see cref="TenancyOptions.AcknowledgeUnauthenticatedTenantHeader"/>.
    /// </summary>
    SharedMultiTenant = 2,
}
