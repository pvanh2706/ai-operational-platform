using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>Câu trả lời JSON của một lượt gọi, kèm số token để đo chi phí.</summary>
public sealed record KetQuaChatJson(string Json, int TokenVao, int TokenRa);

/// <summary>
/// Gọi OpenAI Chat Completions bằng <see cref="HttpClient"/> THUẦN — `Q1-1` (2026-10-22, `AGENT.md` §10.1).
///
/// 🛑 Vì sao không dùng gói NuGet <c>OpenAI</c>: chủ dự án chọn KHÔNG thêm dependency bên thứ ba. Đó cũng là lý do
/// `O3-2` bị bỏ ngày 2026-10-19. <c>System.Net.Http</c> + <c>System.Text.Json</c> là first-party .NET. Cái giá là lớp này
/// tự làm phần SDK làm hộ: dựng request, đọc lỗi, đếm token. Nó chỉ làm ĐÚNG phần dự án dùng — một lượt hỏi, đầu ra
/// ràng buộc bằng JSON Schema — không hơn.
///
/// Cùng khuôn `AR3` với <see cref="AnthropicSopDrafter"/>: mỏng và nhạt. Luật che, ngưỡng, trần, biên lai nằm ở nơi gọi.
///
/// ⚠ Model KHÔNG có mặc định (`Q2-1`): nó đến từ <c>OpenAI:Model</c>, và thiếu thì host không dựng lớp này. Không ai vô
///   tình tiêu tiền vào một model mình chưa chọn.
/// ⚠ CHƯA TỪNG CHẠY THẬT với OpenAI khi viết (2026-10-22): hình dạng request/response được kiểm bằng HttpClient giả theo
///   tài liệu API công khai. Lượt gọi thật đầu tiên là phép thử thật đầu tiên — bấm "Thử khoá" trước.
/// </summary>
public sealed partial class OpenAiChat
{
    public const string DiaChiMacDinh = "https://api.openai.com/v1/";

    /// <summary>Tên named HttpClient trong DI — timeout dài vì một bản nháp SOP sinh hàng nghìn token.</summary>
    public const string TenHttpClient = "openai";

    private readonly HttpClient _http;
    private readonly string _khoa;

    public OpenAiChat(HttpClient http, string khoa, string model)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrWhiteSpace(khoa);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        _http = http;
        _http.BaseAddress ??= new Uri(DiaChiMacDinh);
        _khoa = khoa;
        Model = model.Trim();
    }

    public string Model { get; }

    /// <summary>
    /// Một lượt hỏi, đầu ra là JSON đúng <paramref name="schemaJson"/> (<c>strict: true</c>).
    /// Ném <see cref="ModelTuChoiException"/> cho lỗi CẤU HÌNH (khoá, model, hạn mức); lỗi tạm thời ném nguyên.
    /// </summary>
    public async Task<KetQuaChatJson> GoiJsonAsync(
        string system, string user, string tenSchema, string schemaJson, int maxTokens, CancellationToken ct = default)
    {
        var than = new JsonObject
        {
            ["model"] = Model,

            // ⚠ `max_completion_tokens`, KHÔNG `max_tokens`: model suy luận của OpenAI từ chối tên cũ, và token suy luận
            //   cũng tính vào trần này. Đụng trần là JSON bị cắt giữa chừng — xem nhánh `length` bên dưới.
            ["max_completion_tokens"] = maxTokens,
            ["messages"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = system },
                new JsonObject { ["role"] = "user", ["content"] = user }),
            ["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject
                {
                    ["name"] = tenSchema,
                    ["strict"] = true,
                    ["schema"] = JsonNode.Parse(schemaJson),
                },
            },
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(than.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _khoa);

        using var res = await _http.SendAsync(req, ct);
        var chu = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw Loi(res.StatusCode, chu);

        using var doc = JsonDocument.Parse(chu);
        var choice = doc.RootElement.GetProperty("choices")[0];
        var msg = choice.GetProperty("message");

        if (msg.TryGetProperty("refusal", out var tuChoi) && tuChoi.ValueKind == JsonValueKind.String)
        {
            throw new InvalidOperationException($"OpenAI TỪ CHỐI trả lời (refusal): {tuChoi.GetString()}");
        }

        var lyDoDung = choice.TryGetProperty("finish_reason", out var f) ? f.GetString() : null;
        if (lyDoDung == "length")
        {
            throw new InvalidOperationException(
                $"Câu trả lời đụng trần max_completion_tokens = {maxTokens} nên JSON bị cắt giữa chừng. Lượt này vẫn tính tiền.");
        }

        var json = msg.TryGetProperty("content", out var c) ? c.GetString() : null;
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException($"Câu trả lời không có nội dung (finish_reason = {lyDoDung}).");
        }

        var vao = 0;
        var ra = 0;
        if (doc.RootElement.TryGetProperty("usage", out var u) && u.ValueKind == JsonValueKind.Object)
        {
            vao = u.TryGetProperty("prompt_tokens", out var p) ? p.GetInt32() : 0;
            ra = u.TryGetProperty("completion_tokens", out var r) ? r.GetInt32() : 0;
        }

        return new KetQuaChatJson(json, vao, ra);
    }

    /// <summary>
    /// <c>GET /v1/models/{model}</c> — MIỄN PHÍ, cùng vai <see cref="ThuKhoaModel"/> cho Anthropic. Trả <c>id</c> model khi
    /// khoá được nhận và model tồn tại; ném <see cref="ModelTuChoiException"/> khi bị từ chối vì cấu hình.
    /// </summary>
    public async Task<string> XemModelAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"models/{Uri.EscapeDataString(Model)}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _khoa);

        using var res = await _http.SendAsync(req, ct);
        var chu = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw Loi(res.StatusCode, chu);

        using var doc = JsonDocument.Parse(chu);
        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() ?? Model : Model;
    }

    private Exception Loi(HttpStatusCode ma, string than)
    {
        string? code = null;
        string? thongDiep = null;
        try
        {
            using var doc = JsonDocument.Parse(than);
            if (doc.RootElement.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.Object)
            {
                code = e.TryGetProperty("code", out var cd) && cd.ValueKind == JsonValueKind.String ? cd.GetString() : null;
                thongDiep = e.TryGetProperty("message", out var m) ? m.GetString() : null;
            }
        }
        catch (JsonException)
        {
            // Thân không phải JSON (proxy, trang lỗi HTML) — dùng nguyên văn đã cắt ngắn bên dưới.
        }

        var noi = Gon(thongDiep ?? (than.Length > 300 ? than[..300] + "…" : than));

        return (Exception?)ModelTuChoiException.TuOpenAi((int)ma, code, noi, Model)
               ?? new HttpRequestException($"OpenAI trả HTTP {(int)ma}{(code is null ? "" : $" ({code})")}: {noi}", null, ma);
    }

    /// <summary>
    /// 🛑 Thông điệp 401 của OpenAI CHÉP LẠI một phần khoá (<c>Incorrect API key provided: sk-proj-****abcd</c>). Thông
    /// điệp này đi vào biên lai, log và response 409 — nên mọi chuỗi trông như khoá bị gỡ ở ĐÂY, trước khi rời lớp này.
    /// </summary>
    internal static string Gon(string s) => HinhKhoa().Replace(s, "sk-…[đã gỡ]");

    [GeneratedRegex(@"sk-[A-Za-z0-9_\-\*]{2,}")]
    private static partial Regex HinhKhoa();
}
