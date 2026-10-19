using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Domain.Sop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Hai đường đọc của console nội bộ (`P2-1`, docs/18 nhóm P): <c>GET /internal/cases</c> và
/// <c>GET /internal/cases/{k}/dong-chay</c>.
///
/// 🛑 Test nặng nhất là <see cref="Xem_KHONG_goi_model_va_KHONG_ghi_bien_lai"/>: một màn hình mở ra mà tiêu tiền là
/// một màn hình người ta sẽ sợ mở — và <c>GET /cases/{k}/quy-trinh</c> có sẵn đang làm đúng chuyện đó khi bật model.
/// </summary>
public sealed class DongChayTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private readonly List<string> _fileTam = [];

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private string FileTam(object noiDung)
    {
        var duong = Path.Combine(Path.GetTempPath(), $"dc-{Guid.CreateVersion7():N}.json");
        File.WriteAllText(duong, JsonSerializer.Serialize(noiDung));
        _fileTam.Add(duong);
        return duong;
    }

    /// <summary>Khách riêng; bảng đo RỖNG nên mọi ticket là "ngoài bảng"; taxonomy riêng một nhóm.</summary>
    private async Task<(ApiFactory App, HttpClient C, Guid TenantId, string Nhom)> MoiAsync(
        ModelDinhTuyenGia? model = null, bool goiModel = false)
    {
        var (key, tenantId) = await db.TaoTenantMoiAsync();
        var nhom = $"nhom-dc-{Guid.CreateVersion7():N}";

        var app = ApiFactory.Dedicated(key)
            .With("DinhTuyen:DuongDanBangTraSan", FileTam(new { ketQua = new Dictionary<string, object>() }))
            .With("DinhTuyen:DuongDanTaxonomy", FileTam(new { nhom = new[] { new { ten = nhom, moTa = "x" } } }))
            .With("DinhTuyen:GoiModel", goiModel ? "true" : "false");
        if (model is not null) app = app.WithServices(s => s.AddSingleton(new ModelDinhTuyen(model)));

        return (app, app.CreateClient(), tenantId, nhom);
    }

    private static async Task<string> TaoCaseAsync(HttpClient c, string tienTo, string? moTa = null)
    {
        var khoa = $"{tienTo}{Guid.CreateVersion7():N}";
        (await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = khoa, subject = "Không phát hành được hoá đơn" } })).EnsureSuccessStatusCode();
        if (moTa is not null)
        {
            (await c.PostAsJsonAsync("/signals/case-evidence", new[]
            {
                new { caseSourceReference = khoa, sourceReference = khoa + "#m1", content = moTa, subTenantCode = "khong-xac-dinh" },
            })).EnsureSuccessStatusCode();
        }
        return khoa;
    }

    private static async Task<JsonElement> DongChayAsync(HttpClient c, string khoa)
    {
        var res = await c.GetAsync($"/internal/cases/{Uri.EscapeDataString(khoa)}/dong-chay");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    // =========================================================================

    [Fact]
    public async Task Danh_sach_loc_theo_tien_to_va_moi_nap_truoc()
    {
        var (app, c, _, _) = await MoiAsync();
        using var _ = app;
        var lo = $"dc-lo-{Guid.CreateVersion7():N}:";

        var dau = await TaoCaseAsync(c, lo);
        await TaoCaseAsync(c, lo);
        var cuoi = await TaoCaseAsync(c, lo);
        var ngoai = await TaoCaseAsync(c, "dc-ngoai:");

        var ds = await c.GetFromJsonAsync<JsonElement>($"/internal/cases?tienTo={Uri.EscapeDataString(lo)}");
        var khoa = ds.EnumerateArray().Select(x => x.GetProperty("khoaNguon").GetString()).ToList();

        Assert.Equal(3, khoa.Count);
        Assert.Equal(cuoi, khoa[0]);
        Assert.Equal(dau, khoa[^1]);
        Assert.DoesNotContain(ngoai, khoa);

        var hai = await c.GetFromJsonAsync<JsonElement>($"/internal/cases?tienTo={Uri.EscapeDataString(lo)}&soLuong=2");
        Assert.Equal(2, hai.GetArrayLength());
    }

    /// <summary>Đi một vòng thật — tín hiệu, evidence, người nạp định tuyến, /hoi — rồi đọc lại đủ từng phần.</summary>
    [Fact]
    public async Task Dong_chay_ke_du_evidence_dinh_tuyen_va_goi_y()
    {
        var (app, c, tenantId, nhom) = await MoiAsync();
        using var _ = app;
        var khoa = await TaoCaseAsync(c, "dc:", "Phát hành thì báo E0042");

        await using (var scope = app.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(tenantId);
            await scope.ServiceProvider.GetRequiredService<VongKhongCredit>().NapAsync(
                [new KetQuaDinhTuyenNap(khoa, nhom, "noi-ro-trong-mo-ta", "E0042")],
                new HashSet<string>(StringComparer.Ordinal) { nhom }, "chị Lan");
        }

        (await c.PostAsJsonAsync("/hoi", new { maTicket = khoa })).EnsureSuccessStatusCode();

        var dc = await DongChayAsync(c, khoa);

        Assert.Equal(1, dc.GetProperty("evidence").GetArrayLength());

        var dt = dc.GetProperty("dinhTuyen");
        Assert.Equal(nhom, dt.GetProperty("nhom").GetString());
        Assert.StartsWith(DinhTuyenKho.TienToNguoiNap, dt.GetProperty("nguon").GetString());
        Assert.Equal("chị Lan", dt.GetProperty("nguoiNap").GetProperty("nguoiNap").GetString());

        var goiY = dc.GetProperty("goiY");
        Assert.Equal(1, goiY.GetArrayLength());
        var nac = goiY[0].GetProperty("suKien").EnumerateArray().Select(s => s.GetProperty("nac").GetString()).ToList();
        Assert.Contains("RETRIEVED", nac);
        Assert.Contains("SHOWN", nac);

        // Không quy trình nào được duyệt cho nhóm → tiến độ nói rõ vì sao, và KHÔNG có lượt chạy nào được dựng ra.
        var qt = dc.GetProperty("quyTrinh");
        Assert.False(qt.GetProperty("tienDo").GetProperty("coQuyTrinh").GetBoolean());
        Assert.Equal(0, qt.GetProperty("luotChay").GetArrayLength());
    }

    /// <summary>
    /// 🛑 `P2-1`: đường XEM không gọi model và không ghi biên lai, kể cả khi <c>GoiModel</c> bật. Nó vẫn đọc kết quả
    /// model ĐÃ LƯU — nên sau một lượt /hoi, màn xem nói đúng câu /hoi đã nói, mà không gọi thêm lần nào.
    /// </summary>
    [Fact]
    public async Task Xem_KHONG_goi_model_va_KHONG_ghi_bien_lai()
    {
        string? nhomDung = null;
        var model = new ModelDinhTuyenGia(() => new KetQuaModelDinhTuyen(nhomDung!, "noi-ro-trong-mo-ta", "E0042", 0.95, 10, 5));
        var (app, c, tenantId, nhom) = await MoiAsync(model, goiModel: true);
        nhomDung = nhom;
        using var _ = app;
        var khoa = await TaoCaseAsync(c, "dc-model:", "Phát hành thì báo E0042");

        var truoc = await DongChayAsync(c, khoa);
        await c.GetFromJsonAsync<JsonElement>("/internal/cases?tienTo=dc-model:");

        Assert.Equal(0, model.SoLanGoi);
        Assert.Equal(DinhTuyenKho.ModelChuaGoi, truoc.GetProperty("dinhTuyen").GetProperty("nguon").GetString());
        await using (var ctx = db.OpenContext(tenantId))
        {
            Assert.False(await ctx.Set<LuotDinhTuyenModel>().AnyAsync());
        }

        (await c.PostAsJsonAsync("/hoi", new { maTicket = khoa })).EnsureSuccessStatusCode();
        Assert.Equal(1, model.SoLanGoi);

        var sau = await DongChayAsync(c, khoa);

        Assert.Equal(1, model.SoLanGoi);
        Assert.Equal(nhom, sau.GetProperty("dinhTuyen").GetProperty("nhom").GetString());
        Assert.StartsWith(DinhTuyenKho.TienToModel, sau.GetProperty("dinhTuyen").GetProperty("nguon").GetString());
        Assert.Equal(1, sau.GetProperty("dinhTuyen").GetProperty("bienLaiModel").GetArrayLength());
    }

    /// <summary>
    /// 🛑 `P5-1`: <c>GET /cases/{k}/quy-trinh</c> ghi "CHỈ ĐỌC" trong hợp đồng — trước 2026-10-19 nó gọi model khi bật
    /// <c>GoiModel</c>. Giờ nó đi đường xem như dòng chảy.
    /// </summary>
    [Fact]
    public async Task Duong_quy_trinh_CHI_DOC_cung_KHONG_goi_model()
    {
        var model = new ModelDinhTuyenGia(() => new KetQuaModelDinhTuyen("x", "noi-ro-trong-mo-ta", "x", 0.95, 1, 1));
        var (app, c, tenantId, _) = await MoiAsync(model, goiModel: true);
        using var _ = app;
        var khoa = await TaoCaseAsync(c, "dc-qt:", "Phát hành thì báo E0042");

        var kq = await c.GetFromJsonAsync<JsonElement>($"/cases/{Uri.EscapeDataString(khoa)}/quy-trinh");

        Assert.Equal(0, model.SoLanGoi);
        Assert.Equal("dinh-tuyen-khong-biet", kq.GetProperty("lyDoKhongCo").GetString());
        await using var ctx = db.OpenContext(tenantId);
        Assert.False(await ctx.Set<LuotDinhTuyenModel>().AnyAsync());
    }

    [Fact]
    public async Task Khach_B_khong_thay_case_cua_khach_A()
    {
        var (appA, cA, _, _) = await MoiAsync();
        var (appB, cB, _, _) = await MoiAsync();
        using var _a = appA;
        using var _b = appB;
        var tienTo = $"dc-ab-{Guid.CreateVersion7():N}:";
        var khoa = await TaoCaseAsync(cA, tienTo);

        var res = await cB.GetAsync($"/internal/cases/{Uri.EscapeDataString(khoa)}/dong-chay");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var ds = await cB.GetFromJsonAsync<JsonElement>($"/internal/cases?tienTo={Uri.EscapeDataString(tienTo)}");
        Assert.Equal(0, ds.GetArrayLength());
    }

    [Fact]
    public async Task Case_khong_ton_tai_thi_404()
    {
        var (app, c, _, _) = await MoiAsync();
        using var _ = app;

        var res = await c.GetAsync("/internal/cases/khong-bao-gio-co/dong-chay");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
