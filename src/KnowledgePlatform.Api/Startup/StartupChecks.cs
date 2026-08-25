using KnowledgePlatform.Api.Signals;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Startup;

/// <summary>
/// Mọi thứ ở đây chạy TRƯỚC khi ứng dụng nhận request đầu tiên, và thất bại nào
/// cũng là **không khởi động được**.
///
/// Vì sao không dùng health check để báo: health check báo SAU khi đã start, tức
/// là có một khoảng thời gian ứng dụng đang chạy ở trạng thái sai. Với ranh giới
/// tenant (`G7` gọi là nền tảng) thì khoảng thời gian đó không được tồn tại.
///
/// Đây là `RlsGuard` mở rộng ra tầng cấu hình: cùng một nguyên tắc — **quên là
/// không start được, không phải rò rỉ lúc chạy**.
/// </summary>
public static class StartupChecks
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var config = sp.GetRequiredService<IConfiguration>();
        var tenancy = sp.GetRequiredService<IOptions<TenancyOptions>>().Value;

        RequireConnectionString(config);
        await ResolveTenancyAsync(sp, tenancy, ct);
        RequireSignalEndpointDecision(sp.GetRequiredService<IOptions<IngestOptions>>().Value);

        // Kiểm RLS trên database THẬT. Danh sách bảng suy ra từ model (`IM-7`),
        // nên thêm entity tenant-scoped mà migration quên bật RLS là ném ở đây.
        var db = sp.GetRequiredService<AppDbContext>();
        await RlsGuard.VerifyAsync(db, ct);
    }

    private static void RequireConnectionString(IConfiguration config)
    {
        if (!string.IsNullOrWhiteSpace(config.GetConnectionString("Default"))) return;

        throw new InvalidOperationException(
            """
            Thiếu chuỗi kết nối "ConnectionStrings:Default".

            Máy dev:   đã có sẵn trong appsettings.Development.json
            Deploy:    đặt bằng biến môi trường, KHÔNG commit mật khẩu
                       ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...

            ⚠ Đừng dùng role superuser. Superuser đi vòng qua row-level security,
              kể cả khi bảng có FORCE — xem scripts/dev-db-setup.sql.
            """);
    }

    /// <summary>
    /// Endpoint tín hiệu là endpoint GHI. Không xác thực nghĩa là bất kỳ ai cũng
    /// bơm được case giả vào dữ liệu khách hàng — làm sai kho tri thức và làm sai
    /// luôn `M2`. Nên phải có khoá, hoặc phải có người nói ra rằng mình biết.
    /// </summary>
    private static void RequireSignalEndpointDecision(IngestOptions ingest)
    {
        if (!string.IsNullOrWhiteSpace(ingest.SignalApiKey)) return;
        if (ingest.AcknowledgeUnauthenticatedSignalEndpoint) return;

        throw new InvalidOperationException(
            $"""
             Endpoint tín hiệu POST /signals/case-observed chưa có khoá.

             Đây là endpoint GHI: không có khoá thì bất kỳ ai gọi được cũng bơm được
             case giả vào dữ liệu của khách hàng. Nó không crash, không báo — nó chỉ
             làm sai kho tri thức và sai thước đo M2.

             Chọn một trong hai, tường minh:
                 Ingest:SignalApiKey = <chuỗi bí mật>
                 Ingest:AcknowledgeUnauthenticatedSignalEndpoint = true   (dev/test)

             ⚠ Khoá dùng chung KHÔNG phải câu trả lời cho AR-e: nó không phân biệt
               được khách A với khách B ở chế độ shared, không thu hồi theo từng
               khách, không chống replay. Nó chỉ là cái chốt trong lúc chờ.
             """);
    }

    private static async Task ResolveTenancyAsync(
        IServiceProvider sp, TenancyOptions tenancy, CancellationToken ct)
    {
        var holder = sp.GetRequiredService<DedicatedTenant>();

        switch (tenancy.Mode)
        {
            case TenancyMode.DedicatedSingleTenant:
                if (string.IsNullOrWhiteSpace(tenancy.TenantExternalKey))
                {
                    throw new InvalidOperationException(
                        "Chế độ Tenancy:Mode=DedicatedSingleTenant đòi Tenancy:TenantExternalKey — " +
                        "khoá ngoài của công ty khách hàng mà bản deploy này phục vụ.");
                }

                var directory = sp.GetRequiredService<TenantDirectory>();
                var id = await directory.FindIdByExternalKeyAsync(tenancy.TenantExternalKey, ct);

                // Khoá cấu hình sai mà vẫn start được là ca xấu nhất: ứng dụng chạy
                // bình thường, không thấy dữ liệu nào, và không ai biết vì sao.
                holder.Assign(id ?? throw new InvalidOperationException(
                    $"""
                     Không có tenant nào trong kp.tenant với ExternalKey = "{tenancy.TenantExternalKey}".

                     Bản deploy dedicated này sẽ không thấy dữ liệu nào — nên nó từ chối khởi động
                     thay vì chạy im lặng ở trạng thái sai.
                     """));
                break;

            case TenancyMode.SharedMultiTenant:
                if (!tenancy.AcknowledgeUnauthenticatedTenantHeader)
                {
                    throw new InvalidOperationException(
                        $"""
                         Chế độ Tenancy:Mode=SharedMultiTenant CHƯA CÓ XÁC THỰC (AR-e còn OPEN).

                         Tenant được lấy từ header "{tenancy.TenantKeyHeader}", và hiện chưa có gì
                         kiểm người gọi có quyền dùng khoá đó hay không. Bất kỳ ai gọi được API
                         đều đọc được dữ liệu của bất kỳ khách hàng nào, chỉ cần biết khoá.

                         Muốn chạy chế độ này (ví dụ để test, hoặc trong mạng nội bộ đã khoá) thì
                         phải nói ra tường minh:
                             Tenancy:AcknowledgeUnauthenticatedTenantHeader = true

                         Cờ này không bảo vệ gì cả — nó chỉ làm việc deploy một API chưa xác thực
                         trở thành QUYẾT ĐỊNH, không phải SƠ SUẤT.
                         """);
                }

                holder.Assign(null);
                break;

            case TenancyMode.Unspecified:
            default:
                throw new InvalidOperationException(
                    """
                    Thiếu cấu hình "Tenancy:Mode". Không có giá trị mặc định, cố ý (G13).

                    DedicatedSingleTenant   một bản deploy cho một khách hàng.
                                            Cần thêm Tenancy:TenantExternalKey.
                    SharedMultiTenant       một bản deploy phục vụ nhiều khách hàng,
                                            tenant đến từ header của request.

                    Cùng codebase, cùng schema, cùng policy RLS — khác một lớp cài đặt.
                    Đoán hộ chế độ deploy là đúng loại giả định mà G13 cấm.
                    """);
        }
    }
}
