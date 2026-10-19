using Anthropic;

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>Kết quả thử khoá — KHÔNG BAO GIỜ chứa khoá.</summary>
/// <param name="HopLe"><c>true</c> = khoá được nhận VÀ model tồn tại · <c>false</c> = bị từ chối · <c>null</c> = không thử được.</param>
/// <param name="LyDo"><c>khong-co-khoa</c> · <c>khoa-bi-tu-choi</c> · <c>model-khong-ton-tai</c> · <c>loi-khac</c> · <c>null</c> khi hợp lệ.</param>
/// <param name="TenModelHienThi">Tên Anthropic trả cho model — bằng chứng lượt thử đã thật sự tới nơi.</param>
public sealed record KetQuaThuKhoa(bool CoKhoa, bool? HopLe, string? LyDo, string Model, string? TenModelHienThi, string GiaiThich);

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
}
