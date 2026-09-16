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

    // ⚠ ĐÃ XOÁ 2026-09-16 cùng lúc `AR-e` thành code: `AcknowledgeUnauthenticatedTenantHeader`
    //   và `TenantKeyHeader`.
    //
    //   Ghi lại vì việc xoá một cái cờ an toàn là thứ phải giải thích được, không phải thứ
    //   lặng lẽ biến mất khỏi diff. Cờ đó tồn tại để việc deploy một API chưa xác thực trở
    //   thành QUYẾT ĐỊNH thay vì SƠ SUẤT (`IM-13`) — nó là TRÁNH, và tài liệu đã luôn gọi
    //   đúng tên nó như vậy.
    //
    //   Giờ thì thứ nó tránh đã được GIẢI: ở chế độ shared, tenant đến từ một khoá đã kiểm
    //   (`ApiKeyDirectory`), không còn từ header `X-Tenant-Key` mà người gọi tự điền. Không
    //   còn trạng thái "chạy mà chưa xác thực" để mà thừa nhận.
    //
    //   ⚠ Đây là lần ĐẦU TIÊN trong dự án một cổng TRÁNH được gỡ vì đã GIẢI. Ba cổng tránh
    //     khác vẫn còn nguyên và KHÔNG được gỡ theo: cổng 409 của `AR-l`, lời khai
    //     `xacNhanDuLieuThuongMai` của `AR-n` B2b, và luật che một-hình-dạng của `AR-n` B2a.
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
    /// Một bản deploy phục vụ nhiều khách hàng, tenant đến từ KHOÁ API đã kiểm
    /// (<c>AR-e</c>, <see cref="ApiKeyDirectory"/>) — không từ header người gọi tự điền.
    /// </summary>
    SharedMultiTenant = 2,
}
