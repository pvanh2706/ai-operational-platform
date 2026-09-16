using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Webhooks;

/// <summary>
/// Nơi sản phẩm GỬI KẾT QUẢ VỀ cho phần mềm của khách — hiện thực `D1-1` (`docs/18`).
///
/// Trước đây sản phẩm tự mô tả là *"phần mềm của khách phát tín hiệu, sản phẩm thức tỉnh,
/// xử lý, và trả kết quả về"*, nhưng đường "trả về" **không tồn tại**: chỉ có <c>POST /hoi</c>
/// đồng bộ, tức bên kia phải tự hỏi mới có câu trả lời. Đây là đường đó.
///
/// ⚠ **Bảng này CÓ RLS** — và khác biệt với <c>api_key</c>/<c>knowledge_link_token</c> đáng
/// nói ra: hai bảng kia phải đọc được TRƯỚC khi biết tenant (chúng quyết định tenant). Bảng
/// này thì ngược lại — lúc đụng tới nó ta ĐÃ biết tenant rồi, nên không có lý do gì miễn trừ.
/// Mỗi lần thêm một bảng mới, câu hỏi phải là *"ai đọc nó, và lúc đó đã biết tenant chưa"*,
/// không phải *"bảng trước làm sao thì làm vậy"*.
/// </summary>
public sealed class WebhookEndpoint : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    /// <summary>Địa chỉ mà sản phẩm sẽ <c>POST</c> kết quả tới.</summary>
    public required string Url { get; set; }

    /// <summary>
    /// Bí mật dùng để KÝ gói gửi đi (HMAC-SHA256), để bên nhận biết gói thật sự đến từ đây.
    ///
    /// 🛑 **Lưu được ĐỌC LẠI, và đó là khác biệt có chủ đích với <c>ApiKey.SecretHash</c>** —
    /// chỗ này người review sẽ hỏi, nên trả lời sẵn: khoá API là thứ ta KIỂM (chỉ cần so
    /// hash), còn bí mật này là thứ ta KÝ BẰNG. Không có hàm băm một chiều nào ký được.
    ///
    /// ⚠ Hệ quả phải canh, và nó KHÔNG được quên khi bảng này lớn lên: một bản dump database
    /// làm lộ mọi bí mật ký. Nâng lên mã hoá-tại-chỗ khi có secret store thật (`AR-i` mới
    /// chốt nhánh biến môi trường, chưa có store).
    /// </summary>
    public required string SigningSecret { get; set; }

    /// <summary>Tắt tạm mà không mất cấu hình. Tắt thì KHÔNG xếp hàng gửi nữa.</summary>
    public bool Active { get; set; } = true;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>Một lượt gửi đang chờ hoặc đã xong.</summary>
public enum WebhookDeliveryState
{
    Pending = 0,
    Delivered = 1,

    /// <summary>Hết lượt thử. KHÔNG tự xoá — xem <see cref="WebhookDelivery"/>.</summary>
    Failed = 2,
}

/// <summary>
/// Một lượt gửi kết quả về, ở dạng HÀNG ĐỢI BỀN (outbox) chứ không phải gọi thẳng.
///
/// `D1-1` ghi rõ yêu cầu: *"phải có khoá idempotency + retry: bên nhận sập thì không được
/// mất kết quả, gửi lại không được sinh hai bản"*. Gọi thẳng trong lúc xử lý tín hiệu thì
/// hỏng cả hai vế — bên nhận sập là mất, và đường nạp bị kéo chậm theo bên thứ ba.
///
/// ⚠ **Dòng này KHÔNG chứa payload.** Cố ý: payload được dựng lại lúc GỬI, nên lượt thử lại
/// mang câu trả lời MỚI NHẤT. Kho tri thức đổi liên tục (có người duyệt, có người sửa), và
/// gửi lại một câu trả lời đông cứng từ ba ngày trước là gửi một thứ đã sai.
///
/// ⚠ **Thất bại KHÔNG xoá dòng.** `K-B3` (không ghi đè quá khứ) áp cả ở đây: câu hỏi *"vì
/// sao khách không nhận được gì hôm ấy"* chỉ trả lời được nếu dòng thất bại còn nằm đó kèm
/// <see cref="LastError"/>.
/// </summary>
public sealed class WebhookDelivery : ITenantScoped
{
    /// <summary>Số lượt thử tối đa trước khi bỏ cuộc.</summary>
    public const int SoLuotToiDa = 6;

    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public required Guid CaseId { get; init; }

    /// <summary>Khoá nguồn của case — thứ bên nhận dùng để biết gói này nói về ticket nào.</summary>
    public required string CaseSourceReference { get; init; }

    public required string Subject { get; init; }

    public WebhookDeliveryState State { get; set; } = WebhookDeliveryState.Pending;

    public int Attempts { get; set; }

    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Lỗi của lượt gần nhất. Giữ lại kể cả khi sau đó gửi thành công.</summary>
    public string? LastError { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DeliveredAt { get; set; }

    /// <summary>
    /// Ghi nhận một lượt thất bại và xếp lịch thử lại, giãn theo cấp số nhân.
    ///
    /// Giãn dần chứ không đều: bên nhận sập thường sập vài phút tới vài giờ, và đập cửa mỗi
    /// phút vừa vô ích vừa làm chính mình thành nguồn tải khi sự cố đang diễn ra.
    /// </summary>
    public void GhiThatBai(string loi, DateTimeOffset bayGio)
    {
        Attempts++;
        LastError = loi.Length > 1000 ? loi[..1000] : loi;

        if (Attempts >= SoLuotToiDa)
        {
            State = WebhookDeliveryState.Failed;
            return;
        }

        // 1 · 2 · 4 · 8 · 16 phút
        NextAttemptAt = bayGio + TimeSpan.FromMinutes(Math.Pow(2, Attempts - 1));
    }

    public void GhiThanhCong(DateTimeOffset bayGio)
    {
        Attempts++;
        State = WebhookDeliveryState.Delivered;
        DeliveredAt = bayGio;
    }
}
