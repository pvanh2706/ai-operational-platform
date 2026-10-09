using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>
/// Định tuyến một ticket MỚI bằng model — P8, 2026-10-10, `N7-1` (PROPOSED). Cùng khuôn <see cref="AnthropicSopDrafter"/>:
/// mỏng và nhạt, chỉ gọi — đọc JSON — đếm token. Ngưỡng, taxonomy, trần, bộ nhớ đệm nằm ở nơi gọi.
///
/// ⚠ Model dùng lại đúng <see cref="SopDrafterOptions.ModelMacDinh"/> (`AR3`). Một model rẻ hơn cho việc phân loại là
/// hợp lý, nhưng đổi model là câu `AGENT.md` §10.1 — agent không tự chọn.
///
/// ⚠ CHƯA TỪNG CHẠY THẬT: tài khoản chưa có credit (`docs/18` §Trạng thái). Hình dạng request/response được kiểm bằng
/// HttpClient giả; con số 84,1% của `docs/13` KHÔNG áp cho prompt này — prompt này chưa được đo.
/// </summary>
public sealed class AnthropicDinhTuyen(AnthropicClient client, int maxTokens = 1024) : IGoiModelDinhTuyen
{
    public const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "nhom": { "type": "string" },
            "nguon": { "type": "string", "enum": ["noi-ro-trong-mo-ta", "suy-tu-trieu-chung", "khong-du-can-cu"] },
            "giaiThich": { "type": "string" },
            "doTinCay": { "type": "number" }
          },
          "required": ["nhom", "nguon", "giaiThich", "doTinCay"],
          "additionalProperties": false
        }
        """;

    private sealed record DauRa(string Nhom, string Nguon, string? GiaiThich, double DoTinCay);

    private static readonly JsonSerializerOptions Doc = new(JsonSerializerDefaults.Web);

    public async Task<KetQuaModelDinhTuyen> PhanLoaiAsync(DinhTuyenPrompt prompt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var schema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(SchemaJson)!;

        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = SopDrafterOptions.ModelMacDinh,
            MaxTokens = maxTokens,
            System = prompt.SystemText,
            Messages = [new() { Role = Role.User, Content = prompt.UserText }],
            OutputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = schema } },
        }, cancellationToken: ct);

        var chu = string.Concat(response.Content
            .Select(b => b.TryPickText(out TextBlock? t) ? t?.Text : null)
            .Where(t => !string.IsNullOrWhiteSpace(t)));

        if (string.IsNullOrWhiteSpace(chu))
        {
            throw new InvalidOperationException(
                $"Câu trả lời không có block chữ nào (stop_reason = {response.StopReason}).");
        }

        var ra = JsonSerializer.Deserialize<DauRa>(chu, Doc)
                 ?? throw new InvalidOperationException("Model trả về JSON rỗng.");

        return new KetQuaModelDinhTuyen(
            ra.Nhom.Trim(), ra.Nguon, ra.GiaiThich, ra.DoTinCay,
            response.Usage?.InputTokens is { } vao ? (int)vao : 0,
            response.Usage?.OutputTokens is { } r ? (int)r : 0);
    }
}
