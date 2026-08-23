using KnowledgePlatform.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KnowledgePlatform.Infrastructure.Persistence;

/// <summary>
/// Chỉ dùng lúc THIẾT KẾ (sinh migration), không dùng lúc chạy.
///
/// <see cref="AppDbContext"/> cần <see cref="ITenantContext"/> vì G13 — tenant
/// đến từ ngữ cảnh, không từ hằng số. Lúc sinh migration thì không có request
/// nào, nên ở đây dùng một cài đặt CHƯA XÁC ĐỊNH: nó ném nếu bị đọc.
///
/// Đó là điều đúng: nếu việc sinh migration vô tình cần tới TenantId thì có
/// nghĩa model đang lẫn dữ liệu vào schema, và ta muốn biết ngay.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            // Chuỗi kết nối giả — migration chỉ cần biết provider, không cần DB thật.
            .UseNpgsql("Host=localhost;Database=kp_design_time;Username=design;Password=design")
            .Options;

        return new AppDbContext(options, new UnresolvedTenantContext());
    }

    private sealed class UnresolvedTenantContext : ITenantContext
    {
        public bool IsResolved => false;

        public Guid TenantId => throw new InvalidOperationException(
            "TenantId bị đọc lúc thiết kế. Schema không được phụ thuộc vào một tenant cụ thể — " +
            "nếu bạn thấy lỗi này, có chỗ nào đang trộn dữ liệu vào định nghĩa model.");
    }
}
