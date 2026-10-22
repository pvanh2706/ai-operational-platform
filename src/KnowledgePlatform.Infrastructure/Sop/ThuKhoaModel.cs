using Anthropic;

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>Kết quả thử khoá — KHÔNG BAO GIỜ chứa khoá.</summary>
/// <param name="HopLe"><c>true</c> = khoá được nhận VÀ model tồn tại · <c>false</c> = bị từ chối · <c>null</c> = không thử được.</param>
/// <param name="LyDo"><c>khong-co-khoa</c> · <c>khoa-bi-tu-choi</c> · <c>model-khong-ton-tai</c> · <c>loi-khac</c> · <c>null</c> khi hợp lệ.</param>
/// <param name="TenModelHienThi">Tên nhà cung cấp trả cho model — bằng chứng lượt thử đã thật sự tới nơi.</param>
/// <param name="NhaCungCap"><c>anthropic</c> · <c>openai</c> (`Q1-1`).</param>
public sealed record KetQuaThuKhoa(bool CoKhoa, bool? HopLe, string? LyDo, string Model, string? TenModelHienThi, string GiaiThich,
    string NhaCungCap = "anthropic");

/// <summary>
/// "Khoá này DÙNG ĐƯỢC không?" — trả lời TRƯỚC khi tiêu một lượt gọi (2026-10-19, `IM-57`).
///
/// 🛑 Vì sao có: lượt gọi THẬT đầu tiên (`IM-56`) tốn 11 lượt định tuyến hỏng mới biết khoá bị từ chối. Trang giả lập chỉ
/// biết khoá CÓ hay KHÔNG — hai câu khác nhau, và câu thứ hai mới là câu người bấm nút cần.
///
/// Gọi <c>GET /v1/models/{model}</c>: KHÔNG tính tiền, và một lượt trả lời được CẢ HAI điều — Anthropic nhận khoá, và
/// model mà code đang ghim (<see cref="SopDrafterOptions.ModelMacDinh"/>) có tồn tại với khoá đó.
/// ⚠ Nó KHÔNG nói được tài khoản còn credit hay không: đường miễn phí không chạm số dư. Hết credit chỉ lộ ra ở lượt
///   gọi tốn tiền (<see cref="ModelTuChoiException.HetCredit"/>). Câu trả lời nói thẳng điều đó, để "hợp lệ" không bị
///   đọc thành "gọi được".
/// </summary>
public static class ThuKhoaModel
{
    public const string KhongCoKhoa = "khong-co-khoa";
    public const string LoiKhac = "loi-khac";

    /// <summary>OpenAI có khoá nhưng <c>OpenAI:Model</c> trống (`Q2-1`) — không có gì để thử.</summary>
    public const string ThieuModel = "thieu-model";

    private const string KhongBietCredit = "Đường thử này MIỄN PHÍ nên KHÔNG kiểm được số dư credit — hết credit chỉ lộ ra ở lượt gọi tốn tiền đầu tiên.";

    /// <param name="client"><c>null</c> = host không có khoá — trả lời ngay, không gọi gì.</param>
    public static async Task<KetQuaThuKhoa> ThuAsync(AnthropicClient? client, string model, CancellationToken ct = default)
    {
        if (client is null)
        {
            return new(false, null, KhongCoKhoa, model, null,
                "Host KHÔNG có khoá (Anthropic:ApiKey / ANTHROPIC_API_KEY; máy dev: appsettings.Local.json). Không gọi gì.");
        }

        try
        {
            var m = await client.Models.Retrieve(model, cancellationToken: ct);
            return new(true, true, null, model, m.DisplayName,
                $"Anthropic NHẬN khoá và có model \"{model}\" ({m.DisplayName}). " + KhongBietCredit);
        }
        catch (Exception e) when (ModelTuChoiException.Tu(e, model) is { } cauHinh)
        {
            return new(true, false, cauHinh.LyDo, model, null, cauHinh.Message);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Mất mạng, 5xx, 429… — KHÔNG kết luận gì về khoá. Nói "không thử được", đừng nói "khoá sai".
            return new(true, null, LoiKhac, model, null,
                $"Không thử được — lỗi không phải của khoá ({e.GetType().Name}: {e.Message}). Thử lại sau.");
        }
    }

    /// <summary>
    /// Cùng câu hỏi cho OpenAI (`Q1-1`): <c>GET /v1/models/{model}</c>, miễn phí. Cùng ba kết cục — hợp lệ, bị từ chối,
    /// không thử được — để giao diện đọc một hình dạng.
    /// </summary>
    /// <param name="chat"><c>null</c> = thiếu khoá hoặc thiếu model; <paramref name="coKhoa"/> nói là thiếu cái nào.</param>
    public static async Task<KetQuaThuKhoa> ThuOpenAiAsync(OpenAiChat? chat, bool coKhoa, CancellationToken ct = default)
    {
        const string Ncc = "openai";

        if (chat is null)
        {
            return coKhoa
                ? new(true, null, ThieuModel, "", null,
                    "Có khoá OpenAI nhưng CHƯA đặt OpenAI:Model (Q2-1: không có mặc định, chủ dự án chọn). Không gọi gì.", Ncc)
                : new(false, null, KhongCoKhoa, "", null,
                    "Host KHÔNG có khoá OpenAI (OpenAI:ApiKey / OPENAI_API_KEY; máy dev: appsettings.Local.json). Không gọi gì.", Ncc);
        }

        try
        {
            var id = await chat.XemModelAsync(ct);
            return new(true, true, null, chat.Model, id,
                $"OpenAI NHẬN khoá và có model \"{chat.Model}\". " + KhongBietCredit, Ncc);
        }
        catch (ModelTuChoiException e)
        {
            return new(true, false, e.LyDo, chat.Model, null, e.Message, Ncc);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return new(true, null, LoiKhac, chat.Model, null,
                $"Không thử được — lỗi không phải của khoá ({e.GetType().Name}: {e.Message}). Thử lại sau.", Ncc);
        }
    }
}
