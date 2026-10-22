using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Infrastructure.Sop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Hai nhà cung cấp model (`Q1-1`, `Q2-1`, `Q6-1` — 2026-10-22): host chọn đúng bản, nói đúng trạng thái, và KHÔNG BAO
/// GIỜ trả khoá ra ngoài.
///
/// ⚠ Không test nào ở đây gọi mạng: khoá là chuỗi giả, và mọi đường được kiểm đều dừng trước lượt gọi.
/// </summary>
public sealed class NhaCungCapTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private const string KhoaOpenAiGia = "sk-proj-KHOA-GIA-khong-duoc-lot-ra-9f8e";
    private const string KhoaAnthropicGia = "sk-ant-KHOA-GIA-khong-duoc-lot-ra-1a2b";

    [Fact]
    public async Task Trang_thai_AI_noi_co_khoa_thieu_model_va_KHONG_tra_khoa()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey)
            .With("OpenAI:ApiKey", KhoaOpenAiGia)
            .With("Anthropic:ApiKey", KhoaAnthropicGia);

        var res = await app.CreateClient().GetAsync("/internal/ai");
        var than = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.DoesNotContain(KhoaOpenAiGia, than);
        Assert.DoesNotContain(KhoaAnthropicGia, than);

        var kq = JsonDocument.Parse(than).RootElement;
        var openAi = kq.GetProperty("openAI");
        Assert.True(openAi.GetProperty("coKhoa").GetBoolean());
        Assert.False(openAi.GetProperty("dungDuoc").GetBoolean());
        Assert.Equal(ThuKhoaModel.ThieuModel, openAi.GetProperty("lyDo").GetString());
        Assert.True(kq.GetProperty("anthropic").GetProperty("dungDuoc").GetBoolean());
        Assert.Equal("anthropic", kq.GetProperty("dinhTuyen").GetProperty("nhaCungCap").GetString());
    }

    [Fact]
    public async Task Dinh_tuyen_chon_openai_thi_cam_ban_OpenAI_khi_du_khoa_va_model()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey)
            .With("OpenAI:ApiKey", KhoaOpenAiGia)
            .With("OpenAI:Model", "model-thu")
            .With("DinhTuyen:NhaCungCap", "openai");

        var model = app.Services.GetRequiredService<ModelDinhTuyen>();

        Assert.IsType<OpenAiDinhTuyen>(model.Model);
        Assert.Equal(("openai", "model-thu"), (model.NhaCungCap, model.TenModel));

        var kq = await app.CreateClient().GetFromJsonAsync<JsonElement>("/internal/ai");
        Assert.True(kq.GetProperty("dinhTuyen").GetProperty("dungDuoc").GetBoolean());
    }

    /// <summary>`Q2-1`: có khoá mà thiếu model thì định tuyến KHÔNG có bản gọi model — không có tên nào được đoán hộ.</summary>
    [Fact]
    public void Dinh_tuyen_openai_thieu_model_thi_khong_cam_ban_goi_model()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey)
            .With("OpenAI:ApiKey", KhoaOpenAiGia)
            .With("DinhTuyen:NhaCungCap", "openai");

        Assert.Null(app.Services.GetRequiredService<ModelDinhTuyen>().Model);
    }

    /// <summary>Sai chính tả tên nhà cung cấp → KHÔNG KHỞI ĐỘNG, thay vì âm thầm tiêu tiền ở Anthropic.</summary>
    [Fact]
    public void Ten_nha_cung_cap_sai_thi_host_tu_choi_khoi_dong()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:NhaCungCap", "open-ai");

        var e = Assert.ThrowsAny<Exception>(() => app.CreateClient());

        Assert.Contains("DinhTuyen:NhaCungCap", e.ToString());
    }

    [Fact]
    public async Task Thu_khoa_openai_khi_khong_co_khoa_tra_ngay_khong_goi_gi()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);

        var res = await app.CreateClient().PostAsync("/internal/thu-khoa-model?nhaCungCap=openai", null);
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("openai", kq.GetProperty("nhaCungCap").GetString());
        Assert.False(kq.GetProperty("coKhoa").GetBoolean());
        Assert.Equal(ThuKhoaModel.KhongCoKhoa, kq.GetProperty("lyDo").GetString());
    }

    [Fact]
    public async Task Thu_khoa_nha_cung_cap_la_thi_400()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);

        var res = await app.CreateClient().PostAsync("/internal/thu-khoa-model?nhaCungCap=gemini", null);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public void Gia_tri_MAU_cua_khoa_OpenAI_bi_coi_la_khong_co_khoa()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OpenAI:ApiKey"] = "sk-proj-DAN-KHOA-CUA-BAN-VAO-DAY" })
            .Build();

        Assert.Null(KnowledgePlatform.Api.Startup.KhoaOpenAI.Doc(cfg));
    }
}
