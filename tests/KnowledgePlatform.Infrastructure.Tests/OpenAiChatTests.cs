using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Sop;

namespace KnowledgePlatform.Infrastructure.Tests;

/// <summary>
/// OpenAI bằng HttpClient thuần (`Q1-1`, 2026-10-22) — kiểm HÌNH DẠNG request/response bằng HttpMessageHandler giả.
///
/// 🛑 Không có SDK nào đỡ ở giữa, nên mọi thứ SDK Anthropic làm hộ (dựng request, đọc lỗi, đếm token) là code của dự án
/// — và là code CHƯA TỪNG GẶP OpenAI thật khi viết. Test này giữ những gì kiểm được không cần mạng; "xanh" ở đây KHÔNG có
/// nghĩa là "OpenAI nhận request này". Bấm Thử khoá rồi gọi một lượt thật mới biết.
/// </summary>
public sealed class OpenAiChatTests
{
    private const string KhoaGia = "sk-proj-khoaGiaKHONGduocLOTra1234";

    private sealed class MayChu(HttpStatusCode ma, string than) : HttpMessageHandler
    {
        public List<string> DaGoi { get; } = [];

        public string? ThanRequest { get; private set; }

        public string? XacThuc { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            DaGoi.Add($"{request.Method} {request.RequestUri!.AbsolutePath}");
            XacThuc = request.Headers.Authorization?.ToString();
            ThanRequest = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(ma) { Content = new StringContent(than, Encoding.UTF8, "application/json") };
        }
    }

    private static (OpenAiChat Chat, MayChu May) Dung(HttpStatusCode ma, string than, string model = "model-thu")
    {
        var may = new MayChu(ma, than);
        return (new OpenAiChat(new HttpClient(may), KhoaGia, model), may);
    }

    private static string TraLoi(string noiDung, string lyDoDung = "stop", string? tuChoi = null) =>
        JsonSerializer.Serialize(new
        {
            id = "chatcmpl-gia",
            choices = new[]
            {
                new { index = 0, finish_reason = lyDoDung, message = new { role = "assistant", content = noiDung, refusal = tuChoi } },
            },
            usage = new { prompt_tokens = 120, completion_tokens = 30 },
        });

    private static string Loi(string code, string thongDiep) =>
        JsonSerializer.Serialize(new { error = new { message = thongDiep, type = "invalid_request_error", code } });

    private const string SchemaNho = """{"type":"object","properties":{"a":{"type":"string"}},"required":["a"],"additionalProperties":false}""";

    [Fact]
    public async Task Gui_dung_hinh_dang_chat_completions_va_dem_token()
    {
        var (chat, may) = Dung(HttpStatusCode.OK, TraLoi("""{"a":"x"}"""));

        var kq = await chat.GoiJsonAsync("hệ thống", "người dùng", "thu", SchemaNho, 500);

        Assert.Equal(["POST /v1/chat/completions"], may.DaGoi);
        Assert.Equal($"Bearer {KhoaGia}", may.XacThuc);
        Assert.Equal("""{"a":"x"}""", kq.Json);
        Assert.Equal((120, 30), (kq.TokenVao, kq.TokenRa));

        var than = JsonNode.Parse(may.ThanRequest!)!;
        Assert.Equal("model-thu", (string?)than["model"]);
        Assert.Equal(500, (int?)than["max_completion_tokens"]);
        Assert.Null(than["max_tokens"]);
        Assert.Equal("json_schema", (string?)than["response_format"]!["type"]);
        Assert.True((bool?)than["response_format"]!["json_schema"]!["strict"]);
        Assert.Equal("object", (string?)than["response_format"]!["json_schema"]!["schema"]!["type"]);
        Assert.Equal("system", (string?)than["messages"]![0]!["role"]);
        Assert.Equal("người dùng", (string?)than["messages"]![1]!["content"]);
    }

    [Fact]
    public async Task Khoa_bi_tu_choi_thanh_loi_co_ten_va_KHONG_lot_manh_khoa()
    {
        // Thông điệp 401 THẬT của OpenAI chép lại một phần khoá — lớp này phải gỡ nó trước khi đi vào biên lai/log/409.
        var (chat, _) = Dung(HttpStatusCode.Unauthorized,
            Loi("invalid_api_key", "Incorrect API key provided: sk-proj-****1234. You can find your API key at ..."));

        var e = await Assert.ThrowsAsync<ModelTuChoiException>(() => chat.GoiJsonAsync("s", "u", "thu", SchemaNho, 10));

        Assert.Equal(ModelTuChoiException.KhoaBiTuChoi, e.LyDo);
        Assert.DoesNotContain("sk-proj", e.Message);
        Assert.DoesNotContain(KhoaGia, e.Message);
    }

    [Fact]
    public async Task Model_khong_ton_tai_thanh_loi_co_ten()
    {
        var (chat, _) = Dung(HttpStatusCode.NotFound, Loi("model_not_found", "The model `model-thu` does not exist"));

        var e = await Assert.ThrowsAsync<ModelTuChoiException>(() => chat.GoiJsonAsync("s", "u", "thu", SchemaNho, 10));

        Assert.Equal(ModelTuChoiException.ModelKhongTonTai, e.LyDo);
    }

    [Fact]
    public async Task Het_han_muc_la_het_credit_nhung_429_gioi_han_toc_do_thi_KHONG()
    {
        var (hetTien, _) = Dung(HttpStatusCode.TooManyRequests, Loi("insufficient_quota", "You exceeded your current quota"));
        var e = await Assert.ThrowsAsync<ModelTuChoiException>(() => hetTien.GoiJsonAsync("s", "u", "thu", SchemaNho, 10));
        Assert.Equal(ModelTuChoiException.HetCredit, e.LyDo);

        // Giới hạn tốc độ là lỗi TẠM THỜI — thử lại là được, không phải sửa cấu hình.
        var (tocDo, _) = Dung(HttpStatusCode.TooManyRequests, Loi("rate_limit_exceeded", "Rate limit reached"));
        await Assert.ThrowsAsync<HttpRequestException>(() => tocDo.GoiJsonAsync("s", "u", "thu", SchemaNho, 10));
    }

    [Fact]
    public async Task Dung_tran_token_la_loi_noi_ro_chu_khong_doc_JSON_bi_cat()
    {
        var (chat, _) = Dung(HttpStatusCode.OK, TraLoi("""{"a":"x""", lyDoDung: "length"));

        var e = await Assert.ThrowsAsync<InvalidOperationException>(() => chat.GoiJsonAsync("s", "u", "thu", SchemaNho, 10));

        Assert.Contains("max_completion_tokens", e.Message);
    }

    [Fact]
    public async Task Model_tu_choi_tra_loi_la_loi_noi_ro()
    {
        var (chat, _) = Dung(HttpStatusCode.OK, TraLoi("", tuChoi: "I can't help with that."));

        var e = await Assert.ThrowsAsync<InvalidOperationException>(() => chat.GoiJsonAsync("s", "u", "thu", SchemaNho, 10));

        Assert.Contains("refusal", e.Message);
    }

    [Fact]
    public async Task Thu_khoa_goi_DUONG_MIEN_PHI_khong_goi_chat()
    {
        var (chat, may) = Dung(HttpStatusCode.OK, """{"id":"model-thu","object":"model","owned_by":"openai"}""");

        var kq = await ThuKhoaModel.ThuOpenAiAsync(chat, coKhoa: true);

        Assert.Equal(["GET /v1/models/model-thu"], may.DaGoi);
        Assert.True(kq.HopLe);
        Assert.Equal("openai", kq.NhaCungCap);
    }

    [Fact]
    public async Task Thu_khoa_co_khoa_ma_thieu_model_noi_ro_thieu_model()
    {
        var kq = await ThuKhoaModel.ThuOpenAiAsync(chat: null, coKhoa: true);

        Assert.Null(kq.HopLe);
        Assert.Equal(ThuKhoaModel.ThieuModel, kq.LyDo);
    }

    [Fact]
    public async Task Thu_khoa_bi_tu_choi_tra_HopLe_false()
    {
        var (chat, _) = Dung(HttpStatusCode.Unauthorized, Loi("invalid_api_key", "Incorrect API key provided: sk-proj-****1234."));

        var kq = await ThuKhoaModel.ThuOpenAiAsync(chat, coKhoa: true);

        Assert.False(kq.HopLe);
        Assert.Equal(ModelTuChoiException.KhoaBiTuChoi, kq.LyDo);
        Assert.DoesNotContain("sk-proj", kq.GiaiThich);
    }

    [Fact]
    public async Task Soan_nhap_qua_OpenAI_di_qua_cong_che_truoc_khi_gui()
    {
        var (chat, may) = Dung(HttpStatusCode.OK, TraLoi("{}"));
        var yeuCau = new SopDraftRequest
        {
            GroupName = "Nhóm thử",
            Cases = [new CaseForDrafting("ES-1", "Không vào được", [new EvidenceForDrafting("ES-1#m1", "Mật khẩu: 24680", DateTimeOffset.UtcNow)])],
        };

        // Bản nháp rỗng làm bước đọc JSON hỏng hay không không quan trọng ở đây — request đã đi rồi mới tới bước đó.
        try { await new OpenAiSopDrafter(chat).DraftAsync(yeuCau); } catch (Exception) { /* xem dòng trên */ }

        Assert.NotNull(may.ThanRequest);
        Assert.DoesNotContain("24680", may.ThanRequest);
    }

    [Fact]
    public async Task Dinh_tuyen_qua_OpenAI_doc_dung_cau_tra_loi()
    {
        var (chat, _) = Dung(HttpStatusCode.OK,
            TraLoi("""{"nhom":"Lệch cấu hình kết nối","nguon":"noi-ro-trong-mo-ta","giaiThich":"có E0042","doTinCay":0.93}"""));
        var prompt = DinhTuyenPromptBuilder.Build("Không phát hành được", "E0042", [new NhomTaxonomy("Lệch cấu hình kết nối", null)]);

        var kq = await new OpenAiDinhTuyen(chat).PhanLoaiAsync(prompt);

        Assert.Equal("Lệch cấu hình kết nối", kq.Nhom);
        Assert.Equal(0.93, kq.DoTinCay);
    }

    /// <summary>
    /// 🛑 <c>strict: true</c> của OpenAI TỪ CHỐI cả request nếu schema có một object thiếu <c>additionalProperties: false</c>
    /// hoặc có trường không nằm trong <c>required</c>. Anthropic dễ hơn, nên hai schema viết cho Anthropic có thể đúng ở đó
    /// mà hỏng ở đây — và hỏng ngay lượt gọi thật đầu tiên. Test này đo điều đó trước.
    /// </summary>
    /// <remarks>
    /// ⚠ Lần chạy đầu (2026-10-22) test này ĐỎ với <see cref="SopDraftSchema"/> gốc: hai trường nullable ngoài required và
    /// một map khoá động. Đó là lý do có <see cref="SopDraftSchemaOpenAi"/> — test giờ đo bản đó, bản thật sự được gửi.
    /// </remarks>
    [Theory]
    [InlineData(nameof(SopDraftSchemaOpenAi))]
    [InlineData(nameof(AnthropicDinhTuyen))]
    public void Schema_dung_duoc_voi_strict_cua_OpenAI(string ten)
    {
        var json = ten == nameof(SopDraftSchemaOpenAi) ? SopDraftSchemaOpenAi.Json : AnthropicDinhTuyen.SchemaJson;
        var loi = new List<string>();

        KiemStrict(JsonNode.Parse(json)!, "$", loi);

        Assert.Empty(loi);
    }

    [Fact]
    public void Map_khoa_dong_di_ra_thanh_mang_va_ve_lai_thanh_map()
    {
        Assert.Contains("_haiCaseLech", SopDraftSchemaOpenAi.TruongMap);

        var ve = SopDraftSchemaOpenAi.ChuyenVe("""{"nhom":"x","_haiCaseLech":[{"khoa":"ES-9","giaTri":"thuộc nhóm khác"}]}""");

        var doc = JsonNode.Parse(ve)!;
        Assert.Equal("thuộc nhóm khác", (string?)doc["_haiCaseLech"]!["ES-9"]);
        Assert.Equal("x", (string?)doc["nhom"]);
    }

    [Fact]
    public void Ban_goc_cua_Anthropic_KHONG_doi()
    {
        // Bộ eval đứng trên bản gốc — bản OpenAI dựng TỪ nó, không ghi ngược vào nó.
        Assert.DoesNotContain("giaTri", SopDraftSchema.Json);
        Assert.NotEqual(SopDraftSchema.Json, SopDraftSchemaOpenAi.Json);
    }

    private static void KiemStrict(JsonNode node, string duong, List<string> loi)
    {
        if (node is not JsonObject o) return;

        // `type` có thể là chuỗi hoặc mảng (`["object","null"]`).
        var laObject = o["type"] switch
        {
            JsonValue v => v.TryGetValue<string>(out var s) && s == "object",
            JsonArray a => a.Any(x => x is JsonValue v2 && v2.TryGetValue<string>(out var s2) && s2 == "object"),
            _ => false,
        };

        if (laObject)
        {
            if (o["additionalProperties"] is not JsonValue ap || ap.GetValue<bool>()) loi.Add($"{duong}: thiếu additionalProperties: false");

            var thuocTinh = (o["properties"] as JsonObject)?.Select(p => p.Key).ToHashSet() ?? [];
            var batBuoc = (o["required"] as JsonArray)?.Select(x => (string)x!).ToHashSet() ?? [];
            foreach (var t in thuocTinh.Except(batBuoc)) loi.Add($"{duong}.{t}: không nằm trong required");
        }

        foreach (var (k, v) in o)
        {
            if (v is JsonObject) KiemStrict(v, $"{duong}.{k}", loi);
            if (v is JsonArray a) foreach (var x in a) if (x is not null) KiemStrict(x, $"{duong}.{k}[]", loi);
        }
    }
}
