using System.Text.Json;
using System.Text.Json.Nodes;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>
/// Hiện thực <see cref="ISopDrafter"/> bằng OpenAI (`Q1-1`). Cùng prompt, cùng cổng che với
/// <see cref="AnthropicSopDrafter"/>; schema DỰNG TỪ cùng bản gốc (<see cref="SopDraftSchemaOpenAi"/>) và đọc về cùng
/// <see cref="SopDraft"/>. Vì thế bộ eval và người duyệt không phải biết bản nháp đến từ nhà cung cấp nào để chấm nó.
///
/// ⚠ <see cref="SopDrafterOptions.Model"/> KHÔNG dùng ở đây: model là của <see cref="OpenAiChat"/> (<c>OpenAI:Model</c>).
/// ⚠ <see cref="SopDraftResult.EstimatedUsd"/> tính theo bảng giá Claude — với bản nháp này nó KHÔNG đúng. Số token thì
///   đúng; đọc chi phí từ token, đừng đọc từ con số USD.
/// </summary>
public sealed class OpenAiSopDrafter(OpenAiChat chat, SopDrafterOptions? options = null) : ISopDrafter
{
    private readonly SopDrafterOptions _options = options ?? new SopDrafterOptions();

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    public async Task<SopDraftResult> DraftAsync(SopDraftRequest request, CancellationToken ct = default)
    {
        // Cổng che TRƯỚC khi có byte nào rời máy (`AR-o`) — đúng chỗ bản Anthropic đặt nó.
        var prompt = SopPromptBuilder.Build(request, _options.QuaCongChe);

        var kq = await chat.GoiJsonAsync(prompt.SystemText, prompt.UserText, "sop_draft", SopDraftSchemaOpenAi.Json,
            _options.MaxTokens, ct);

        var draft = JsonSerializer.Deserialize<SopDraft>(SopDraftSchemaOpenAi.ChuyenVe(kq.Json), ReadOptions)
                    ?? throw new InvalidOperationException("Model trả về JSON rỗng.");

        return new SopDraftResult(draft, prompt.RedactedCount, kq.TokenVao, kq.TokenRa);
    }
}

/// <summary>
/// <see cref="SopDraftSchema"/> viết lại cho <c>strict: true</c> của OpenAI — DỰNG TỪ bản gốc lúc chạy, không chép tay.
///
/// 🛑 Sinh ra từ test <c>Schema_dung_duoc_voi_strict_cua_OpenAI</c> (2026-10-22), TRƯỚC lượt gọi thật nào: bản gốc có hai chỗ
/// Anthropic nhận mà strict của OpenAI từ chối CẢ REQUEST —
/// <list type="bullet">
///   <item><c>bangTraMaLoi</c>, <c>_haiCaseLech</c> nullable nhưng KHÔNG nằm trong <c>required</c> → ở đây thành required,
///     giữ kiểu có <c>null</c>: "bỏ trống" vẫn nói được, chỉ phải nói bằng <c>null</c>;</item>
///   <item><c>_haiCaseLech</c> là MAP (khoá = mã ticket, <c>additionalProperties</c> là một schema) — strict không nhận khoá
///     động → ở đây thành MẢNG <c>{khoa, giaTri}</c>, và <see cref="ChuyenVe"/> đổi về map TRƯỚC khi đọc.</item>
/// </list>
/// ⚠ Vì sao KHÔNG sửa bản gốc: bộ eval <c>nhom_sop.py --kiem-cay</c> và đường Anthropic đứng trên nó (xem
///   <see cref="SopDraftSchema"/>). Dựng lại lúc chạy thì sửa bản gốc là bản này đi theo, không có hai chỗ để lệch.
/// </summary>
public static class SopDraftSchemaOpenAi
{
    private static readonly (string Json, HashSet<string> TruongMap) Dung = DungTuGoc(SopDraftSchema.Json);

    public static string Json => Dung.Json;

    /// <summary>Tên các trường cấp cao nhất đã đổi map → mảng.</summary>
    public static IReadOnlySet<string> TruongMap => Dung.TruongMap;

    /// <summary>Đổi các trường mảng <c>{khoa, giaTri}</c> về map — hình dạng <see cref="SopDraft"/> đọc.</summary>
    public static string ChuyenVe(string json)
    {
        if (JsonNode.Parse(json) is not JsonObject goc) return json;

        foreach (var ten in Dung.TruongMap)
        {
            if (goc[ten] is not JsonArray mang) continue;

            var map = new JsonObject();
            foreach (var x in mang)
            {
                if (x?["khoa"]?.GetValue<string>() is { } k) map[k] = x["giaTri"]?.DeepClone();
            }

            goc[ten] = map;
        }

        return goc.ToJsonString();
    }

    private static (string, HashSet<string>) DungTuGoc(string goc)
    {
        var schema = JsonNode.Parse(goc)!.AsObject();
        var truongMap = new HashSet<string>(StringComparer.Ordinal);

        if (schema["properties"] is JsonObject capMot)
        {
            foreach (var (ten, con) in capMot.ToList())
            {
                if (con is JsonObject o && o["additionalProperties"] is JsonObject giaTri)
                {
                    truongMap.Add(ten);
                    capMot[ten] = new JsonObject
                    {
                        ["type"] = CoNull(o) ? new JsonArray("array", "null") : "array",
                        ["description"] = $"{(string?)o["description"]} (Viết thành MẢNG {{khoa, giaTri}} — mỗi phần tử một khoá.)",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["additionalProperties"] = false,
                            ["required"] = new JsonArray("khoa", "giaTri"),
                            ["properties"] = new JsonObject
                            {
                                ["khoa"] = new JsonObject { ["type"] = "string" },
                                ["giaTri"] = giaTri.DeepClone(),
                            },
                        },
                    };
                }
            }
        }

        BatBuocHet(schema, "$");
        return (schema.ToJsonString(), truongMap);
    }

    /// <summary>Mọi object: required = mọi thuộc tính. Trường cũ KHÔNG bắt buộc phải cho phép null, không thì nghĩa đổi.</summary>
    private static void BatBuocHet(JsonNode node, string duong)
    {
        if (node is JsonArray a)
        {
            foreach (var x in a) if (x is not null) BatBuocHet(x, duong + "[]");
            return;
        }

        if (node is not JsonObject o) return;

        if (o["additionalProperties"] is JsonObject)
        {
            throw new InvalidOperationException(
                $"{duong}: map lồng bên trong — SopDraftSchemaOpenAi chỉ đổi được map ở cấp cao nhất. Sửa ở đây trước khi gọi OpenAI.");
        }

        if (o["properties"] is JsonObject thuocTinh)
        {
            var cu = (o["required"] as JsonArray)?.Select(x => (string)x!).ToHashSet() ?? [];
            foreach (var (ten, con) in thuocTinh)
            {
                if (!cu.Contains(ten) && con is JsonObject c && !CoNull(c))
                {
                    throw new InvalidOperationException(
                        $"{duong}.{ten}: không bắt buộc mà không cho null — strict của OpenAI buộc mọi trường phải có mặt.");
                }
            }

            o["required"] = new JsonArray([.. thuocTinh.Select(p => (JsonNode)JsonValue.Create(p.Key))]);
            o["additionalProperties"] = false;
        }

        foreach (var (k, v) in o.ToList()) if (v is not null) BatBuocHet(v, $"{duong}.{k}");
    }

    private static bool CoNull(JsonObject o) => o["type"] switch
    {
        JsonArray a => a.Any(x => x is JsonValue v && v.TryGetValue<string>(out var s) && s == "null"),
        _ => false,
    };
}

/// <summary>
/// Định tuyến một ticket MỚI bằng OpenAI (`Q1-1`) — cùng prompt và cùng schema với <see cref="AnthropicDinhTuyen"/>.
/// Ngưỡng, taxonomy, trần, bộ nhớ đệm vẫn ở <c>DinhTuyenKho</c>.
///
/// ⚠ Trần 4096 chứ không phải 1024 như bản Anthropic: model suy luận của OpenAI đếm token SUY LUẬN vào cùng trần, nên
///   1024 đủ cho bốn trường JSON mà vẫn có thể bị cắt trước khi viết được chữ nào.
/// </summary>
public sealed class OpenAiDinhTuyen(OpenAiChat chat, int maxTokens = 4096) : IGoiModelDinhTuyen
{
    private sealed record DauRa(string Nhom, string Nguon, string? GiaiThich, double DoTinCay);

    private static readonly JsonSerializerOptions Doc = new(JsonSerializerDefaults.Web);

    public async Task<KetQuaModelDinhTuyen> PhanLoaiAsync(DinhTuyenPrompt prompt, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var kq = await chat.GoiJsonAsync(prompt.SystemText, prompt.UserText, "dinh_tuyen", AnthropicDinhTuyen.SchemaJson,
            maxTokens, ct);

        var ra = JsonSerializer.Deserialize<DauRa>(kq.Json, Doc)
                 ?? throw new InvalidOperationException("Model trả về JSON rỗng.");

        return new KetQuaModelDinhTuyen(ra.Nhom.Trim(), ra.Nguon, ra.GiaiThich, ra.DoTinCay, kq.TokenVao, kq.TokenRa);
    }
}
