using System.Data.Common;
using KnowledgePlatform.Domain.Tenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KnowledgePlatform.Infrastructure.Persistence;

/// <summary>
/// MẮT XÍCH nối C# với luật row-level security của PostgreSQL.
///
/// Không có nó thì hệ thống ở trạng thái sau: C# biết <c>TenantId</c>, Postgres
/// KHÔNG biết. Policy <c>tenant_isolation</c> đọc biến session
/// <c>app.current_tenant</c>; biến đó chưa ai đặt, nên mọi truy vấn trả về 0 dòng.
/// Đã kiểm trên DB thật: an toàn nhưng vô dụng.
///
/// Đặt ở TẦNG CONNECTION, không phải trong từng repository, là cố ý — cùng lý do
/// với `AR2`. Nếu việc đặt tenant nằm trong code truy vấn thì nó lại phụ thuộc
/// vào việc KHÔNG LẬP TRÌNH VIÊN NÀO QUÊN, tức là trái `G7`. Ở đây mọi connection
/// mở ra đều đi qua chỗ này, không có đường vòng.
///
/// <para><b>LUÔN GHI, KỂ CẢ KHI CHƯA XÁC ĐỊNH ĐƯỢC TENANT.</b> Đây là chi tiết
/// quan trọng nhất của class này. Connection lấy từ pool có thể còn giữ giá trị
/// của request TRƯỚC. Nếu ta chỉ ghi khi <see cref="ITenantContext.IsResolved"/>
/// đúng, thì một job hệ thống hoặc một request chưa phân giải được tenant sẽ
/// THỪA HƯỞNG tenant của người khác — rò rỉ dữ liệu, im lặng, không log. Nên khi
/// chưa xác định được, ta ghi CHUỖI RỖNG, và policy hiểu đó là "không có tenant"
/// (`IM-9` — chính vì thế policy phải có <c>nullif</c>).</para>
///
/// <para><b>Npgsql multiplexing phải TẮT</b> (mặc định là tắt). Multiplexing
/// trộn nhiều lệnh của nhiều nơi lên cùng một connection vật lý, nên biến session
/// không còn thuộc về ai — chỗ này sẽ đặt tenant cho lệnh của tenant khác. Nếu
/// một ngày nào đó bật nó để tăng thông lượng, cơ chế tenant phải đổi cách khác.</para>
/// </summary>
public sealed class TenantConnectionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    /// <summary>
    /// `false` ở tham số thứ ba của <c>set_config</c> = phạm vi SESSION, không
    /// phải transaction. Cần vậy vì EF mở connection trước rồi mới bắt đầu
    /// transaction, và một connection thường phục vụ nhiều transaction.
    /// </summary>
    private const string SetTenantSql = "SELECT set_config('app.current_tenant', @tenant, false)";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var cmd = CreateCommand(connection);
        cmd.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var cmd = CreateCommand(connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = SetTenantSql;

        var p = cmd.CreateParameter();
        p.ParameterName = "tenant";

        // Tham số hoá, không nội suy chuỗi. TenantId là Guid nên tự nó đã an toàn,
        // nhưng `set_config` nhận text và ai đó sẽ đổi kiểu của TenantId một ngày nào đó.
        p.Value = tenantContext.IsResolved
            ? tenantContext.TenantId.ToString()
            : string.Empty;

        cmd.Parameters.Add(p);
        return cmd;
    }
}
