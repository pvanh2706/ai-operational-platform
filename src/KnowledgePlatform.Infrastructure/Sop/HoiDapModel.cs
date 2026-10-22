using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>
/// Hỏi đáp bằng Anthropic — `Q3-1` (2026-10-22). Cùng khuôn <see cref="AnthropicDinhTuyen"/>: gọi, đọc JSON, đếm token.
/// Mã dẫn được đối chiếu ở nơi gọi (<see cref="HoiDapDoiChieu"/>), không ở đây.
/// </summary>
public sealed class AnthropicHoiDap(AnthropicClient client, int maxTokens = 4096) : IGoiModelHoiDap
{
    private static readonly JsonSerializerOptions Doc = new() { PropertyNameCaseInsensitive = true };

    public async Task<KetQuaModelHoiDap> HoiAsync(HoiDapPrompt prompt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var schema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(HoiDapPromptBuilder.SchemaJson)!;

        Message response;
        try
        {
            response = await client.Messages.Create(new MessageCreateParams
            {
                Model = SopDrafterOptions.ModelMacDinh,
                MaxTokens = maxTokens,
                System = prompt.SystemText,
                Messages = [new() { Role = Role.User, Content = prompt.UserText }],
                OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = schema } },
            }, cancellationToken: ct);
        }
        catch (Exception e) when (ModelTuChoiException.Tu(e, SopDrafterOptions.ModelMacDinh) is { } cauHinh)
        {
            throw cauHinh;
        }

        var chu = string.Concat(response.Content
            .Select(b => b.TryPickText(out TextBlock? t) ? t?.Text : null)
            .Where(t => !string.IsNullOrWhiteSpace(t)));

        if (string.IsNullOrWhiteSpace(chu))
        {
            throw new InvalidOperationException($"Câu trả lời không có block chữ nào (stop_reason = {response.StopReason}).");
        }

        var kq = JsonSerializer.Deserialize<KetQuaModelHoiDap>(chu, Doc)
                 ?? throw new InvalidOperationException("Model trả về JSON rỗng.");

        return kq with
        {
            TokenVao = response.Usage?.InputTokens is { } vao ? (int)vao : 0,
            TokenRa = response.Usage?.OutputTokens is { } ra ? (int)ra : 0,
        };
    }
}

/// <summary>Hỏi đáp bằng OpenAI (`Q1-1` + `Q3-1`) — cùng prompt, cùng schema (đã hợp strict).</summary>
public sealed class OpenAiHoiDap(OpenAiChat chat, int maxTokens = 8192) : IGoiModelHoiDap
{
    private static readonly JsonSerializerOptions Doc = new() { PropertyNameCaseInsensitive = true };

    public async Task<KetQuaModelHoiDap> HoiAsync(HoiDapPrompt prompt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var kq = await chat.GoiJsonAsync(prompt.SystemText, prompt.UserText, "hoi_dap", HoiDapPromptBuilder.SchemaJson,
            maxTokens, ct);

        var ra = JsonSerializer.Deserialize<KetQuaModelHoiDap>(kq.Json, Doc)
                 ?? throw new InvalidOperationException("Model trả về JSON rỗng.");

        return ra with { TokenVao = kq.TokenVao, TokenRa = kq.TokenRa };
    }
}
