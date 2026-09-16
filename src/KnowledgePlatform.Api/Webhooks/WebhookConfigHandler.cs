using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Domain.Webhooks;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Webhooks;

/// <summary>Khai báo đích gửi kết quả về của MỘT khách hàng.</summary>
public sealed record KhaiBaoWebhook(string? Url);

/// <param name="BiMatKy">
/// CHỈ có giá trị ngay sau khi tạo mới. Lần đọc sau trả <c>null</c> — cùng luật với khoá API.
/// </param>
public sealed record WebhookKetQua(string Url, bool Active, string? BiMatKy, DateTimeOffset CapNhatLuc);

/// <summary>
/// Đọc/ghi cấu hình webhook của khách hàng hiện tại.
///
/// ⚠ Bí mật ký **sinh ra ở phía sản phẩm, không nhận từ bên gửi**. Nhận từ ngoài nghĩa là
/// nhận luôn cả những bí mật yếu do người gõ tay — và chữ ký chỉ mạnh bằng bí mật yếu nhất
/// từng được dùng. Sinh ở đây thì mọi khách có cùng một mức entropy, không có ngoại lệ.
/// </summary>
public sealed class WebhookConfigHandler(AppDbContext db, ITenantContext tenant, TimeProvider clock)
{
    public async Task<WebhookKetQua?> DocAsync(CancellationToken ct = default)
    {
        var e = await db.Set<WebhookEndpoint>().FirstOrDefaultAsync(ct);

        // BiMatKy = null: đã tạo rồi thì không đọc lại được. Mất thì khai lại để sinh cái mới.
        return e is null ? null : new WebhookKetQua(e.Url, e.Active, null, e.UpdatedAt ?? e.CreatedAt);
    }

    /// <summary>Tạo mới hoặc đổi địa chỉ. Trả <c>null</c> nếu URL không dùng được.</summary>
    public async Task<WebhookKetQua?> KhaiBaoAsync(string url, CancellationToken ct = default)
    {
        if (!HopLe(url)) return null;

        var bayGio = clock.GetUtcNow();
        var e = await db.Set<WebhookEndpoint>().FirstOrDefaultAsync(ct);

        if (e is not null)
        {
            // Đổi địa chỉ KHÔNG đổi bí mật ký: bên nhận đang kiểm chữ ký bằng bí mật cũ, và
            // đổi cả hai cùng lúc là làm mọi gói đang bay trở thành gói không kiểm được.
            e.Url = url;
            e.Active = true;
            e.UpdatedAt = bayGio;
            await db.SaveChangesAsync(ct);

            return new WebhookKetQua(e.Url, e.Active, null, bayGio);
        }

        var (_, _, biMat) = ChuoiBiMat.Sinh("kpw");

        var moi = new WebhookEndpoint
        {
            TenantId = tenant.TenantId,
            Url = url,
            SigningSecret = biMat,
            CreatedAt = bayGio,
        };

        db.Set<WebhookEndpoint>().Add(moi);
        await db.SaveChangesAsync(ct);

        return new WebhookKetQua(moi.Url, moi.Active, biMat, bayGio);
    }

    /// <summary>
    /// Chỉ nhận <c>http</c>/<c>https</c> tuyệt đối.
    ///
    /// ⚠ Cố ý KHÔNG chặn địa chỉ nội bộ: `H1-5` + `H1b-1` chốt bản deploy chạy LOCAL trong
    /// LAN, nên đích gửi hợp lệ hoàn toàn có thể là một máy trong cùng mạng. Một luật chặn
    /// "IP nội bộ" nghe an toàn nhưng ở đây nó cấm đúng ca thật đang dùng.
    /// </summary>
    private static bool HopLe(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u)
        && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}
