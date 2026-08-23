namespace KnowledgePlatform.Domain.Tenancy;

/// <summary>
/// Mọi thứ thuộc về một công ty khách hàng phải cài giao diện này.
///
/// G7 (tenant boundary là NỀN TẢNG) + AR2 (thực thi ở tầng DB bằng row-level
/// security) + G13 (tenant từ cấu hình/ngữ cảnh request, KHÔNG từ hằng số toàn cục).
///
/// Vì sao có giao diện này thay vì chỉ thêm một cột: nó làm việc "quên tenant"
/// trở thành thứ có thể kiểm được bằng máy. Xem <c>AppDbContext</c> — mọi entity
/// cài <see cref="ITenantScoped"/> đều bị bắt buộc có RLS policy; thiếu là lỗi
/// lúc khởi tạo, không phải lỗ rò lúc chạy.
/// </summary>
public interface ITenantScoped
{
    /// <summary>Công ty khách hàng sở hữu dòng dữ liệu này.</summary>
    Guid TenantId { get; }
}

/// <summary>
/// Một công ty khách hàng. Mô hình tích hợp (06 §1): phần mềm có sẵn của khách
/// phát tín hiệu, sản phẩm này thức tỉnh và xử lý — nên "tenant" là công ty gửi
/// tín hiệu, không phải một người dùng đăng nhập.
/// </summary>
public sealed class Tenant
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Tên hiển thị. Không dùng để phân quyền.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// Khoá ổn định do khách/host app cung cấp, dùng khi nhận tín hiệu.
    /// G13: tenant được xác định từ đây (ngữ cảnh request), không từ hằng số.
    /// </summary>
    public required string ExternalKey { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
