using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Knowledge;

/// <summary>
/// Token mở một bản tri thức bằng LINK — hiện thực của `D5-3` (`docs/18`).
///
/// Vì sao cần: `D3-1` chốt rằng một lượt trả về mang **tóm tắt + link**. Một cái link thì
/// phải có chỗ để mở, và chỗ đó phải biết người mở là ai. Trước token này,
/// <c>GET /knowledge/{id}</c> chỉ đòi khoá API của HỆ THỐNG — tức là cái link gửi cho một
/// người sẽ hoặc không mở được, hoặc phải kèm khoá hệ thống (tệ hơn nhiều).
///
/// ⚠ **"MỘT LẦN" ở `D5-3` hiểu là MỘT LẦN PHÁT, không phải một lần MỞ** — đã ghi rõ ở
/// `docs/18` §D5 và chủ dự án bác được bất cứ lúc nào. Hai lý do đo được:
/// <list type="bullet">
///   <item>webhook có RETRY (`D1-1`) — gửi lại cùng một link, token tiêu rồi thì lần hai
///     vô nghĩa;</item>
///   <item>SOP là tri thức DÙNG CHUNG — hai người support cùng đọc một bản là chuyện
///     thường, không phải ngoại lệ. Một cơ chế mà người dùng thật thấy *"hỏng"* thì họ sẽ
///     đi đường vòng, và đường vòng nào cũng tệ hơn.</item>
/// </list>
/// Nên: mở được nhiều lần **trong hạn**, và thu hồi được.
///
/// ⚠ **Bảng này KHÔNG có RLS, cùng lý do với <c>kp.api_key</c> chứ KHÔNG phải lý do của
/// <c>kp.tenant</c>**: nó CÓ thuộc một tenant, nhưng chính nó là thứ quyết định tenant của
/// request — người bấm link không mang theo thứ gì khác. Bù lại, y hệt `AR-e`: không có
/// <c>DbSet</c>, và đọc nó đi qua đúng một cửa.
/// </summary>
public sealed class KnowledgeLinkToken
{
    /// <summary>Tiền tố riêng, để không lẫn với khoá API khi nhìn bằng mắt hay khi quét.</summary>
    public const string TienTo = "kpl";

    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid TenantId { get; init; }

    /// <summary>Bản tri thức mà link này mở. Một token mở đúng MỘT bản.</summary>
    public required Guid KnowledgeRecordId { get; init; }

    /// <summary>Phần công khai, dùng để tra dòng theo index.</summary>
    public required string TokenId { get; init; }

    /// <summary>SHA-256 của phần bí mật. Không lưu chuỗi trần — xem <see cref="ApiKey"/>.</summary>
    public required string SecretHash { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Ai phát link này. Để trả lời câu "link rò ra kia do ai phát", không để phân quyền.</summary>
    public required string CreatedBy { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Còn mở được không. Nhận <paramref name="bayGio"/> từ ngoài thay vì đọc đồng hồ ở
    /// đây — để test kiểm được ca hết hạn mà không phải chờ thật.
    /// </summary>
    public bool ConMoDuoc(DateTimeOffset bayGio) => RevokedAt is null && bayGio < ExpiresAt;
}
