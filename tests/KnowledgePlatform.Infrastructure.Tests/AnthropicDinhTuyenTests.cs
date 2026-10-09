using System.Net;
using System.Text;
using System.Text.Json;
using Anthropic;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Sop;

namespace KnowledgePlatform.Infrastructure.Tests;

/// <summary>
/// Định tuyến bằng model (P8) — kiểm HÌNH DẠNG request/response bằng HttpClient giả.
///
/// 🛑 Đây KHÔNG phải phép đo chất lượng: chưa có credit, nên chưa một lượt gọi thật nào. Test này chỉ giữ hai thứ
/// kiểm được không cần mạng — request không mang mã ticket và có ràng buộc hình dạng đầu ra; response đọc đúng.
/// Đọc "xanh" ở đây thành "định tuyến dùng được" là đúng cái bẫy `README` §"Chạy thử" cảnh báo.
/// </summary>
public sealed class AnthropicDinhTuyenTests
{
    private sealed class MayChuGia(string cauTraLoiJson) : HttpMessageHandler
    {
        public string? ThanRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ThanRequest = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);

            var than = JsonSerializer.Serialize(new
            {
                id = "msg_gia",
                type = "message",
                role = "assistant",
                model = SopDrafterOptions.ModelMacDinh,
                content = new[] { new { type = "text", text = cauTraLoiJson } },
                stop_reason = "end_turn",
                stop_sequence = (string?)null,
                usage = new { input_tokens = 120, output_tokens = 30 },
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(than, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (AnthropicDinhTuyen Model, MayChuGia May) Dung(string cauTraLoiJson)
    {
        var may = new MayChuGia(cauTraLoiJson);
        var client = new AnthropicClient { ApiKey = "khoa-gia-chi-de-test", HttpClient = new HttpClient(may) };
        return (new AnthropicDinhTuyen(client), may);
    }

    private static DinhTuyenPrompt Prompt() =>
        DinhTuyenPromptBuilder.Build("Không phát hành được hoá đơn", "Báo lỗi E0042 khi bấm phát hành",
            [new NhomTaxonomy("Lệch cấu hình kết nối", "mã lỗi E00xx")]);

    [Fact]
    public async Task Doc_dung_cau_tra_loi_co_cau_truc_va_dem_token()
    {
        var (model, _) = Dung("""{"nhom":"Lệch cấu hình kết nối","nguon":"noi-ro-trong-mo-ta","giaiThich":"có E0042","doTinCay":0.93}""");

        var kq = await model.PhanLoaiAsync(Prompt());

        Assert.Equal("Lệch cấu hình kết nối", kq.Nhom);
        Assert.Equal("noi-ro-trong-mo-ta", kq.Nguon);
        Assert.Equal(0.93, kq.DoTinCay, 3);
        Assert.Equal(120, kq.TokenVao);
    }

    /// <summary>Request đi ra mang ràng buộc hình dạng (structured output), đúng model của `AR3`, và chỉ tiêu đề + mô tả.</summary>
    [Fact]
    public async Task Request_co_rang_buoc_hinh_dang_dung_model_va_chi_tieu_de_mo_ta()
    {
        var (model, may) = Dung("""{"nhom":"KHONG-BIET","nguon":"khong-du-can-cu","giaiThich":"thiếu","doTinCay":0.2}""");

        await model.PhanLoaiAsync(Prompt());

        Assert.NotNull(may.ThanRequest);
        Assert.Contains("output_config", may.ThanRequest);
        Assert.Contains(SopDrafterOptions.ModelMacDinh, may.ThanRequest);
        Assert.Contains("E0042", may.ThanRequest);
    }

    [Fact]
    public async Task KHONG_BIET_cua_model_di_nguyen_ve_noi_goi()
    {
        var (model, _) = Dung("""{"nhom":"KHONG-BIET","nguon":"khong-du-can-cu","giaiThich":"ticket không nói gì","doTinCay":0.1}""");

        var kq = await model.PhanLoaiAsync(Prompt());

        Assert.Equal(KetQuaDinhTuyen.KhongBiet, kq.Nhom);
    }
}
