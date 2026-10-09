using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// `N1-2` (`docs/18`, chốt 2026-10-10) — cờ TẮT TẠM mọi cổng che.
///
/// 🛑 Ba điều file này giữ, và cả ba là chỗ một lần "dọn cho gọn" sẽ phá:
/// <list type="number">
///   <item>chế độ shared TỪ CHỐI KHỞI ĐỘNG khi cờ bật — dữ liệu của nhiều công ty;</item>
///   <item>khâu NẠP là cửa MỘT CHIỀU, nên mỗi mẩu mang dấu đã-qua-cổng-hay-chưa;</item>
///   <item>cờ TẮT (mặc định) thì mọi thứ y như trước — các test che cũ phải xanh nguyên.</item>
/// </list>
/// Đường ra (webhook) có test riêng trong <see cref="WebhookGuiVeTests"/>, cạnh test che cũ.
///
/// ⚠ MỌI GIÁ TRỊ TRONG FILE NÀY LÀ GIẢ.
/// </summary>
public sealed class CongCheTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private const string CoTatTam = "CongChe:TatTamVaChapNhanRuiRo";

    private const string KhoiBenMua = """
        <NMua>
          <Ten>Cong ty TNHH Gia Lap Khong Co That</Ten>
          <MST>0100000009</MST>
        </NMua>
        Ma loi: E0042
        """;

    [Fact]
    public void Shared_bat_co_thi_TU_CHOI_KHOI_DONG()
    {
        using var factory = ApiFactory.Shared().With(CoTatTam, "true");

        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("CongChe:TatTamVaChapNhanRuiRo", ex.Message);
        Assert.Contains("MỘT CHIỀU", ex.Message);
    }

    [Fact]
    public void Dedicated_bat_co_thi_VAN_khoi_dong()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey).With(CoTatTam, "true");

        using var _ = factory.CreateClient();
    }

    private static async Task<string> NapMotMauAsync(HttpClient c)
    {
        var khoaCase = $"jira:CC-{Guid.CreateVersion7():N}";
        (await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = khoaCase, subject = "NCC tu choi payload" } }))
            .EnsureSuccessStatusCode();

        var khoaMau = khoaCase + "#comment-1";
        (await c.PostAsJsonAsync("/signals/case-evidence", new[]
        {
            new
            {
                caseSourceReference = khoaCase,
                sourceReference = khoaMau,
                content = KhoiBenMua,
                machineReadability = "High",
                subTenantCode = "khong-xac-dinh",
            },
        })).EnsureSuccessStatusCode();

        return khoaMau;
    }

    /// <summary>Mặc định: đi qua cổng, và mẩu mang dấu <c>true</c>.</summary>
    [Fact]
    public async Task Co_tat_thi_khau_nap_VAN_che_va_mau_mang_dau_true()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        var khoaMau = await NapMotMauAsync(app.CreateClient());

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.EvidenceItems.SingleAsync(e => e.SourceReference == khoaMau);

        Assert.DoesNotContain("Gia Lap Khong Co That", luu.Content);
        Assert.True(luu.DaCheBenThuBa);
    }

    /// <summary>
    /// 🛑 Cửa MỘT CHIỀU: cờ bật thì mẩu vào kho nguyên văn, và dấu <c>false</c> là thứ DUY NHẤT
    /// còn nói được chuyện đó về sau — vì tắt cờ đi cũng không che lại được.
    /// </summary>
    [Fact]
    public async Task Co_bat_thi_mau_vao_kho_NGUYEN_VAN_va_mang_dau_false()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey).With(CoTatTam, "true");
        var khoaMau = await NapMotMauAsync(app.CreateClient());

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.EvidenceItems.SingleAsync(e => e.SourceReference == khoaMau);

        Assert.Contains("Gia Lap Khong Co That", luu.Content);
        Assert.False(luu.DaCheBenThuBa);
    }
}
