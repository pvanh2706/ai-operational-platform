using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>
/// Cấu hình cho <see cref="AnthropicSopDrafter"/>. Cố ý KHÔNG có API key ở đây: SDK tự đọc
/// `ANTHROPIC_API_KEY` (hoặc profile của `ant auth login`). Một khoá đi qua tay mình là
/// một khoá có thể rơi vào log, vào appsettings, vào git.
/// </summary>
public sealed class SopDrafterOptions
{
    /// <summary>`AR3` chốt `claude-opus-5`. Đổi model là một quyết định, không phải tuỳ chọn vận hành.</summary>
    public string Model { get; init; } = "claude-opus-5";

    /// <summary>
    /// Bản nháp của một nhóm 10 case đo được khoảng 5-6 nghìn token đầu ra. Để 16 000 vì
    /// đụng trần là output bị cắt giữa câu và phải gọi lại — mà gọi lại thì mất tiền hai lần.
    /// </summary>
    public int MaxTokens { get; init; } = 16_000;
}

/// <summary>
/// Hiện thực <see cref="ISopDrafter"/> bằng SDK chính thức (`AR3`: SDK chính thức + interface mỏng).
///
/// Lớp này CỐ Ý mỏng và nhạt. Mọi thứ đáng tranh luận đã nằm ở chỗ khác:
/// <list type="bullet">
///   <item>luật che và chỗ nối → <see cref="SopPromptBuilder"/> ở tầng Domain, vì cổng che
///     nằm cạnh chỗ gọi mạng thì một đường gọi mới là đi vòng qua được nó (`IM-22`)</item>
///   <item>hình dạng đầu ra → <see cref="SopDraftSchema"/>, khớp với bộ eval đã có</item>
///   <item>bản nháp có ĐÚNG hay không → `nhom_sop.py --kiem-cay` và người duyệt</item>
/// </list>
/// Nó chỉ làm ba việc: gọi, đọc JSON, đếm token.
///
/// ⚠ CHƯA làm, và biết là chưa: Batches API. `S5` nói Path A không nhạy latency nên Batches
/// giảm được một nửa giá (cả 19 nhóm: ~$2,77 → ~$1,39). Nhưng lượt gọi ĐẦU TIÊN phải là
/// lượt đồng bộ, vì batch trả kết quả sau và mọi lỗi hình dạng sẽ hiện ra chậm hơn nhiều.
/// Đo xong hình dạng rồi mới chuyển sang batch.
/// </summary>
public sealed class AnthropicSopDrafter(AnthropicClient client, SopDrafterOptions? options = null)
    : ISopDrafter
{
    private readonly SopDrafterOptions _options = options ?? new SopDrafterOptions();

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    public async Task<SopDraftResult> DraftAsync(SopDraftRequest request, CancellationToken ct = default)
    {
        // Cổng che chạy Ở ĐÂY, trước khi có byte nào rời khỏi máy (`AR-o`). Nếu payload
        // không sạch được thì hàm này ném và KHÔNG gọi gì — fail closed.
        var prompt = SopPromptBuilder.Build(request);

        var schema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(SopDraftSchema.Json)
                     ?? throw new InvalidOperationException("SopDraftSchema.Json không đọc được.");

        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = prompt.SystemText,
            Messages = [new() { Role = Role.User, Content = prompt.UserText }],
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = schema },
            },
        }, cancellationToken: ct);

        var json = LayVanBan(response);
        var draft = JsonSerializer.Deserialize<SopDraft>(json, ReadOptions)
                    ?? throw new InvalidOperationException("Model trả về JSON rỗng.");

        return new SopDraftResult(
            draft,
            prompt.RedactedCount,
            response.Usage?.InputTokens is { } vao ? (int)vao : 0,
            response.Usage?.OutputTokens is { } ra ? (int)ra : 0);
    }

    /// <summary>
    /// Gom mọi block chữ trong câu trả lời.
    ///
    /// ⚠ KHÔNG lấy `content[0]` — câu trả lời có thể mở đầu bằng block `thinking` (Opus 5
    /// bật thinking theo mặc định), và lấy phần tử đầu là đọc đúng chỗ rỗng rồi báo là model
    /// trả về JSON hỏng. Đúng loại lỗi mà thông điệp lỗi chỉ về sai hướng.
    /// </summary>
    private static string LayVanBan(Message response)
    {
        var chu = new List<string>();
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out TextBlock? text) && !string.IsNullOrWhiteSpace(text?.Text))
            {
                chu.Add(text!.Text);
            }
        }

        if (chu.Count == 0)
        {
            throw new InvalidOperationException(
                $"Câu trả lời không có block chữ nào (stop_reason = {response.StopReason}). " +
                "Nếu stop_reason là refusal thì đây là câu bị từ chối, không phải lỗi hình dạng.");
        }

        return string.Concat(chu);
    }
}
