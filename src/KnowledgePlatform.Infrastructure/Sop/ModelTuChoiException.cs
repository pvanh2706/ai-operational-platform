using Anthropic.Exceptions;

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>
/// Model TỪ CHỐI vì CẤU HÌNH, không vì nội dung — khoá sai, model không tồn tại, hết credit. Chỗ gọi đổi thành một
/// câu trả lời đọc được (409 + <c>lyDo</c>), cùng khuôn <see cref="ChuaCoCreditException"/> (`IM-46`).
///
/// 🛑 Vì sao có lớp này (2026-10-19, bước 5 của nhóm P — lượt gọi model THẬT đầu tiên): khoá trong
/// <c>appsettings.Local.json</c> bị Anthropic trả <c>401 invalid x-api-key</c>. Định tuyến hỏng có kiểm soát (KHÔNG BIẾT
/// + biên lai), nhưng soạn nháp trả <b>HTTP 500 kèm nguyên stack trace</b>: người bấm nút thấy tên lớp exception của SDK
/// thay vì một câu "khoá bị từ chối, làm thế này". `IM-46` đã chốt "chưa có credit thì KHÔNG hỏng" — khoá bị từ chối
/// là cùng một loại chuyện, chỉ chưa ai gặp vì chưa ai gọi thật.
///
/// ⚠ Chỉ đổi lỗi CẤU HÌNH. Lỗi tạm thời (429, 5xx, mất mạng) vẫn ném nguyên — chúng cần thử lại, không cần sửa cấu
/// hình, và gộp chúng vào đây là nói sai việc phải làm.
/// </summary>
public sealed class ModelTuChoiException(string lyDo, string message, Exception inner) : Exception(message, inner)
{
    public const string KhoaBiTuChoi = "khoa-bi-tu-choi";
    public const string ModelKhongTonTai = "model-khong-ton-tai";
    public const string HetCredit = "het-credit";

    /// <summary><c>khoa-bi-tu-choi</c> · <c>model-khong-ton-tai</c> · <c>het-credit</c>.</summary>
    public string LyDo { get; } = lyDo;

    private const string VongThayThe =
        "Trong lúc chờ: vòng KHÔNG cần khoá — --xuat-soan-nhap <mã-khách> \"<tên nhóm>\" <file> → đưa cho Claude/Codex " +
        "→ POST /knowledge/sop-draft (hoặc chế độ File ở trang giả lập).";

    /// <summary>Lỗi SDK → lỗi cấu hình có tên, hoặc <c>null</c> nếu không phải lỗi cấu hình (để chỗ gọi ném nguyên).</summary>
    /// <remarks>⚠ Thông điệp của SDK chứa thân response của Anthropic (có request_id) — KHÔNG chứa khoá.</remarks>
    public static ModelTuChoiException? Tu(Exception e, string model) => e switch
    {
        AnthropicUnauthorizedException or AnthropicForbiddenException => new(KhoaBiTuChoi,
            "Anthropic TỪ CHỐI khoá (401/403). Khoá trong Anthropic:ApiKey / ANTHROPIC_API_KEY (máy dev: " +
            "appsettings.Local.json) không hợp lệ — đã thu hồi, bị vô hiệu, hoặc chép thiếu. Tạo khoá mới ở " +
            "console.anthropic.com, đặt lại, rồi KHỞI ĐỘNG LẠI app. Kiểm bằng --kiem-cau-hinh (không in khoá). " +
            VongThayThe + $"\n\nAnthropic trả: {e.Message}", e),

        AnthropicNotFoundException => new(ModelKhongTonTai,
            $"Anthropic không tìm thấy model \"{model}\" (404). Tên model là hằng số trong code (AR3); đổi model là câu " +
            "AGENT.md §10.1 — chủ dự án chọn, agent không tự đổi. " + VongThayThe + $"\n\nAnthropic trả: {e.Message}", e),

        AnthropicBadRequestException when e.Message.Contains("credit balance", StringComparison.OrdinalIgnoreCase) =>
            new(HetCredit,
                "Tài khoản Anthropic HẾT CREDIT (khoá đúng, nhưng không còn tiền để gọi). Nạp credit ở " +
                "console.anthropic.com. " + VongThayThe + $"\n\nAnthropic trả: {e.Message}", e),

        _ => null,
    };
}
