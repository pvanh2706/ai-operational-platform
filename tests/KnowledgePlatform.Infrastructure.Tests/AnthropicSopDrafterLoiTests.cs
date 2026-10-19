using System.Net;
using System.Text;
using Anthropic;
using Anthropic.Exceptions;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Sop;

namespace KnowledgePlatform.Infrastructure.Tests;

/// <summary>
/// Anthropic từ chối vì CẤU HÌNH → lỗi có tên, không phải exception thô của SDK (2026-10-19, `IM-56`).
///
/// 🛑 Sinh ra từ lượt gọi model THẬT đầu tiên của dự án: khoá trong appsettings.Local.json bị trả
/// <c>401 invalid x-api-key</c>, và <c>POST /knowledge/soan-nhap</c> trả HTTP 500 kèm stack trace. Test đi qua SDK THẬT
/// với một HttpMessageHandler trả đúng thân lỗi Anthropic trả — không mạng, không tiền.
/// </summary>
public sealed class AnthropicSopDrafterLoiTests
{
    private const string KhoaGia = "khoa-gia-KHONG-DUOC-LOT-VAO-THONG-DIEP";

    private sealed class MayChuLoi(HttpStatusCode ma, string loai, string thongDiep) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(ma)
            {
                Content = new StringContent(
                    $$"""{"type":"error","error":{"type":"{{loai}}","message":"{{thongDiep}}"},"request_id":"req_gia"}""",
                    Encoding.UTF8, "application/json"),
            });
    }

    private static AnthropicSopDrafter Dung(HttpStatusCode ma, string loai, string thongDiep) =>
        new(new AnthropicClient { ApiKey = KhoaGia, MaxRetries = 0, HttpClient = new HttpClient(new MayChuLoi(ma, loai, thongDiep)) });

    private static SopDraftRequest YeuCau() => new()
    {
        GroupName = "Nhóm thử",
        Cases = [new CaseForDrafting("ES-1", "Không phát hành được", [new EvidenceForDrafting("ES-1#m1", "Báo E0042", DateTimeOffset.UtcNow)])],
    };

    [Fact]
    public async Task Khoa_bi_tu_choi_401_thanh_loi_co_ten_va_KHONG_in_khoa()
    {
        var drafter = Dung(HttpStatusCode.Unauthorized, "authentication_error", "invalid x-api-key");

        var e = await Assert.ThrowsAsync<ModelTuChoiException>(() => drafter.DraftAsync(YeuCau()));

        Assert.Equal(ModelTuChoiException.KhoaBiTuChoi, e.LyDo);
        Assert.Contains("invalid x-api-key", e.Message);      // nói ra Anthropic đã trả gì
        Assert.Contains("--xuat-soan-nhap", e.Message);       // và việc làm thay
        Assert.DoesNotContain(KhoaGia, e.Message);            // 🛑 nhưng KHÔNG bao giờ in khoá
    }

    [Fact]
    public async Task Khong_co_model_404_thanh_model_khong_ton_tai_kem_ten_model()
    {
        var drafter = Dung(HttpStatusCode.NotFound, "not_found_error", "model: khong-co");

        var e = await Assert.ThrowsAsync<ModelTuChoiException>(() => drafter.DraftAsync(YeuCau()));

        Assert.Equal(ModelTuChoiException.ModelKhongTonTai, e.LyDo);
        Assert.Contains(SopDrafterOptions.ModelMacDinh, e.Message);
    }

    [Fact]
    public async Task Het_credit_thanh_het_credit()
    {
        var drafter = Dung(HttpStatusCode.BadRequest, "invalid_request_error",
            "Your credit balance is too low to access the Anthropic API.");

        var e = await Assert.ThrowsAsync<ModelTuChoiException>(() => drafter.DraftAsync(YeuCau()));

        Assert.Equal(ModelTuChoiException.HetCredit, e.LyDo);
    }

    /// <summary>400 vì lý do KHÁC (request hỏng) không phải lỗi cấu hình — ném nguyên, đừng nói sai việc phải làm.</summary>
    [Fact]
    public async Task Loi_400_khac_KHONG_bi_doi_thanh_loi_cau_hinh()
    {
        var drafter = Dung(HttpStatusCode.BadRequest, "invalid_request_error", "max_tokens: too large");

        await Assert.ThrowsAsync<AnthropicBadRequestException>(() => drafter.DraftAsync(YeuCau()));
    }
}
