using KnowledgePlatform.Api.Tenancy;

namespace KnowledgePlatform.Api.Startup;

/// <summary>
/// Hai lệnh vận hành của `AR-e`, chạy bằng chính host rồi thoát — KHÔNG mở cổng nghe.
///
/// <code>
///   dotnet run --project src/KnowledgePlatform.Api -- --tao-khoa dev-tenant "connector Jira"
///   dotnet run --project src/KnowledgePlatform.Api -- --thu-hoi-khoa a1b2c3d4e5f6
/// </code>
///
/// Vì sao là lệnh CLI chứ không phải endpoint quản trị: một endpoint tạo khoá tự nó cần
/// được xác thực, và thứ xác thực nó lại là khoá — vòng tròn. Ai đó sẽ giải vòng tròn bằng
/// một "khoá quản trị" cấu hình sẵn, và thế là khoá dùng chung quay lại bằng cửa sau, đúng
/// thứ `AR-e` vừa gỡ đi. Lệnh CLI cắt vòng tròn bằng quyền chạy được trên máy chủ.
///
/// ⚠ Chạy TRƯỚC <see cref="StartupChecks"/>, cố ý: hai lệnh này phải dùng được cả khi cấu
/// hình tenancy chưa xong — nhất là lúc dựng khách hàng đầu tiên, khi chưa có khoá nào.
/// </summary>
public static class AdminCommands
{
    public static async Task<bool> TryRunAsync(string[] args, IServiceProvider services, CancellationToken ct = default)
    {
        if (args.Length == 0) return false;

        switch (args[0])
        {
            case "--tao-khoa":
                await TaoKhoaAsync(args, services, ct);
                return true;

            case "--thu-hoi-khoa":
                await ThuHoiAsync(args, services, ct);
                return true;

            default:
                return false;
        }
    }

    private static async Task TaoKhoaAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Dùng: --tao-khoa <tenant-external-key> <nhãn>");
            Environment.ExitCode = 2;
            return;
        }

        var externalKey = args[1];
        var label = args[2];

        var tenants = services.GetRequiredService<TenantDirectory>();
        var tenantId = await tenants.FindIdByExternalKeyAsync(externalKey, ct);

        if (tenantId is null)
        {
            Console.Error.WriteLine($"Không có tenant nào trong kp.tenant với ExternalKey = \"{externalKey}\".");
            Environment.ExitCode = 3;
            return;
        }

        var khoa = await services.GetRequiredService<ApiKeyDirectory>().TaoAsync(tenantId.Value, label, ct);

        Console.WriteLine($"Đã tạo khoá cho tenant \"{externalKey}\" — {label}");
        Console.WriteLine();
        Console.WriteLine($"    {khoa}");
        Console.WriteLine();
        Console.WriteLine("⚠ Đây là lần DUY NHẤT chuỗi này đọc được. Database chỉ giữ hash — mất thì sinh khoá khác.");
        Console.WriteLine("⚠ ĐỪNG dán nó vào file được git theo dõi. Xem scripts/quet_credential_repo.py.");
    }

    private static async Task ThuHoiAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Dùng: --thu-hoi-khoa <keyId>   (phần giữa của chuỗi khoá, sau \"kp_\")");
            Environment.ExitCode = 2;
            return;
        }

        var so = await services.GetRequiredService<ApiKeyDirectory>().ThuHoiAsync(args[1], ct);

        // 0 dòng có hai nghĩa — không có khoá đó, HOẶC đã thu hồi từ trước. Nói ra cả hai
        // thay vì báo "không tìm thấy": người đang thu hồi khẩn cấp cần biết nó đã tắt rồi.
        Console.WriteLine(so > 0
            ? $"Đã thu hồi khoá {args[1]}."
            : $"Không đổi gì: khoá {args[1]} không tồn tại, hoặc đã bị thu hồi trước đó.");
    }
}
