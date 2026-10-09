using KnowledgePlatform.Api.Startup;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Sop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Khoá Anthropic: hai TÊN, một chỗ đọc.
///
/// 🛑 Lỗi thật tìm ra 2026-10-10: host KIỂM <c>Anthropic:ApiKey</c> để chọn bản hiện thực, rồi dựng
/// client không truyền khoá — client tự đọc <c>ANTHROPIC_API_KEY</c>. Đặt tên này mà thiếu tên kia
/// thì hoặc gọi không khoá, hoặc báo "chưa có credit" khi khoá đang nằm sẵn. Không lượt nào crash.
///
/// ⚠ Không test nào ở đây GỌI model: chỉ kiểm bản hiện thực nào được cắm. Khoá dùng ở đây là chuỗi
/// giả — client không bao giờ được dùng.
/// </summary>
public sealed class KhoaAnthropicTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private const string KhoaGia = "khoa-gia-chi-de-chon-ban-hien-thuc";

    private static IConfiguration CauHinh(Dictionary<string, string?> giaTri) =>
        new ConfigurationBuilder().AddInMemoryCollection(giaTri).Build();

    [Fact]
    public void Doc_duoc_tu_TEN_CAU_HINH() =>
        Assert.Equal(KhoaGia, KhoaAnthropic.Doc(CauHinh(new() { ["Anthropic:ApiKey"] = KhoaGia })));

    [Fact]
    public void Doc_duoc_tu_TEN_MOI_TRUONG_cua_SDK() =>
        Assert.Equal(KhoaGia, KhoaAnthropic.Doc(CauHinh(new() { ["ANTHROPIC_API_KEY"] = KhoaGia })));

    [Fact]
    public void Ca_hai_trong_thi_null() =>
        Assert.Null(KhoaAnthropic.Doc(CauHinh(new()
        {
            ["Anthropic:ApiKey"] = "  ",
            ["ANTHROPIC_API_KEY"] = "",
        })));

    private ISopDrafter BanDuocCam(ApiFactory app)
    {
        using var scope = app.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ISopDrafter>();
    }

    /// <summary>Đúng ca lỗi cũ: chỉ có biến môi trường của SDK → host từng báo "chưa có credit".</summary>
    [Fact]
    public void Chi_co_ANTHROPIC_API_KEY_thi_VAN_cam_ban_goi_model()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("ANTHROPIC_API_KEY", KhoaGia);

        Assert.IsType<AnthropicSopDrafter>(BanDuocCam(app));
    }

    [Fact]
    public void Khong_co_khoa_nao_thi_cam_ban_bao_chua_co_credit()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);

        Assert.IsType<SopDrafterChuaCoCredit>(BanDuocCam(app));
    }
}
