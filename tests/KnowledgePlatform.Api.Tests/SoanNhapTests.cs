using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Sop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>Bản <see cref="ISopDrafter"/> giả — trả một bản nháp dựng sẵn, không tốn gì.</summary>
public sealed class SopDrafterGia : ISopDrafter
{
    public int SoLanGoi { get; private set; }

    public Func<SopDraftRequest, SopDraft>? Dung { get; set; }

    public Task<SopDraftResult> DraftAsync(SopDraftRequest request, CancellationToken ct = default)
    {
        SoLanGoi++;
        var draft = Dung?.Invoke(request) ?? SoanNhapTests.NhapToiThieu(request.GroupName);
        return Task.FromResult(new SopDraftResult(draft, 0, 0, 0));
    }
}

/// <summary>
/// Soạn nháp SOP **từ trong host** (bước 4) — và cổng trần tháng của `E3-1`.
///
/// 🛑 Hai test nặng nhất:
/// <list type="bullet">
///   <item><c>Chua_co_credit_thi_noi_ro_va_chi_duong_thay_the</c> — mặc định hôm nay. Hỏng
///   im lặng hoặc trả bản nháp rỗng ở đây là đưa một kết quả trông như thật vào kho.</item>
///   <item><c>Vuot_tran_thang_thi_TU_CHOI_truoc_khi_goi</c> — trần chỉ có nghĩa nếu nó chặn
///   TRƯỚC lượt gọi, không phải sau.</item>
/// </list>
/// </summary>
public sealed class SoanNhapTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private readonly List<string> _fileTam = [];
    private readonly SopDrafterGia _drafter = new();

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    internal static SopDraft NhapToiThieu(string nhom) => new()
    {
        Nhom = nhom,
        SoCaseDungSau = 1,
        TrieuChungVao = [new SopSymptom("Không phát hành được hoá đơn", ["ES-1"])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1", CauHoi = "Ô ký hiệu có giá trị không?", NoiXem = "Màn phát hành",
                Nhanh = [new SopBranch("Ô trống", "S1", ["ES-1"], "evidence-noi-ro")],
            },
        ],
        BuocSua = [new SopFixStep("S1", "Chọn ký hiệu rồi phát hành lại", ["ES-1"])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(1, ["ES-1"]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    private string BangTraSanRong()
    {
        var duong = Path.Combine(Path.GetTempPath(), $"sn-{Guid.CreateVersion7():N}.json");
        File.WriteAllText(duong, """{"ketQua":{}}""");
        _fileTam.Add(duong);
        return duong;
    }

    /// <summary>
    /// 🛑 **Mỗi test một khách hàng RIÊNG.** Biên lai `LuotSoanNhap` là dữ liệu "đếm được bao
    /// nhiêu" của CẢ TENANT, nên dùng chung thì trần tháng của test này bị test khác làm đầy —
    /// và kết quả đổi theo THỨ TỰ CHẠY. Cùng hình dạng lỗi đã trả giá ở `VongTriThucTests` và
    /// `WebhookGuiVeTests`; lần này nhận ra sớm hơn một nhịp.
    /// </summary>
    private async Task<(ApiFactory App, Guid TenantId)> MoiKhachAsync(bool coDrafter)
    {
        var (key, id) = await db.TaoTenantMoiAsync();

        var app = ApiFactory.Dedicated(key).With("DinhTuyen:DuongDanBangTraSan", BangTraSanRong());
        if (coDrafter) app = app.WithServices(s => s.AddScoped<ISopDrafter>(_ => _drafter));

        return (app, id);
    }

    private static async Task<string> TaoCaseAsync(HttpClient c)
    {
        var khoa = $"jira:SN-{Guid.CreateVersion7():N}";
        (await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = khoa, subject = "Không phát hành được hoá đơn" } }))
            .EnsureSuccessStatusCode();
        return khoa;
    }

    /// <summary>Dựng một nhóm có case: tạo case rồi nạp định tuyến cho nó.</summary>
    private static async Task<string> DungNhomAsync(ApiFactory app, Guid tenantId, HttpClient c)
    {
        var nhom = $"nhom-sn-{Guid.CreateVersion7():N}";
        var khoa = await TaoCaseAsync(c);

        await using var scope = app.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(tenantId);
        await scope.ServiceProvider.GetRequiredService<VongKhongCredit>().NapAsync(
            [new KetQuaDinhTuyenNap(khoa, nhom, "noi-ro-trong-mo-ta", "x")],
            new HashSet<string>(StringComparer.Ordinal) { nhom }, "test");

        return nhom;
    }

    // =========================================================================

    [Fact]
    public async Task Soan_xong_thi_ban_nhap_vao_kho_o_trang_thai_DRAFT()
    {
        var (app, tenantId) = await MoiKhachAsync(coDrafter: true);
        using var _ = app;
        var c = app.CreateClient();
        var nhom = await DungNhomAsync(app, tenantId, c);

        var res = await c.PostAsJsonAsync("/knowledge/soan-nhap",
            new { nhom, nguoiYeuCau = "chị Lan (support)" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(kq.GetProperty("thanh").GetBoolean());
        Assert.Equal(1, kq.GetProperty("soCase").GetInt32());
        Assert.True(kq.GetProperty("daNap").GetProperty("daTao").GetInt32() >= 1);
        Assert.Equal(1, _drafter.SoLanGoi);

        // Nạp KHÔNG phải duyệt (`D4`) — record phải ở DRAFT.
        await using var ctx = db.OpenContext(tenantId);
        var nhan = SopDraftToKnowledge.NhanNhom(nhom);
        var trangThai = await ctx.KnowledgeRecords
            .Where(r => r.Assertions.Any(a => a.Kind == AssertionKind.Applicability && a.Content == nhan))
            .Select(r => r.Lifecycle)
            .ToListAsync();

        Assert.NotEmpty(trangThai);
        Assert.All(trangThai, t => Assert.Equal(StoredLifecycleState.Draft, t));
    }

    /// <summary>
    /// 🛑 Mặc định HÔM NAY. Hỏng im lặng hoặc trả bản nháp rỗng ở đây là đưa một kết quả
    /// trông như thật vào kho — và nó sẽ nằm đó tới khi có người duyệt nhầm.
    /// </summary>
    [Fact]
    public async Task Chua_co_credit_thi_noi_ro_va_chi_duong_thay_the()
    {
        var (app, tenantId) = await MoiKhachAsync(coDrafter: false);
        using var _ = app;
        var c = app.CreateClient();
        var nhom = await DungNhomAsync(app, tenantId, c);

        var res = await c.PostAsJsonAsync("/knowledge/soan-nhap",
            new { nhom, nguoiYeuCau = "chị Lan" });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);

        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("chua-co-credit", body);
        Assert.Contains("--xuat-soan-nhap", body);   // chỉ ra ĐÚNG việc phải làm thay

        // Và KHÔNG có record nào lọt vào kho.
        await using var ctx = db.OpenContext(tenantId);
        var nhan = SopDraftToKnowledge.NhanNhom(nhom);
        Assert.False(await ctx.KnowledgeRecords
            .AnyAsync(r => r.Assertions.Any(a => a.Kind == AssertionKind.Applicability && a.Content == nhan)));
    }

    /// <summary>
    /// 🛑 Trần chỉ có nghĩa nếu nó chặn TRƯỚC lượt gọi. Chặn sau thì tiền đã tiêu rồi.
    /// </summary>
    [Fact]
    public async Task Vuot_tran_thang_thi_TU_CHOI_truoc_khi_goi()
    {
        var (goc, tenantId) = await MoiKhachAsync(coDrafter: true);
        using var app = goc.With("SoanNhap:TranThang", "1");
        goc.Dispose();
        var c = app.CreateClient();
        var nhom = await DungNhomAsync(app, tenantId, c);

        (await c.PostAsJsonAsync("/knowledge/soan-nhap", new { nhom, nguoiYeuCau = "lượt 1" }))
            .EnsureSuccessStatusCode();
        Assert.Equal(1, _drafter.SoLanGoi);

        var lai = await c.PostAsJsonAsync("/knowledge/soan-nhap", new { nhom, nguoiYeuCau = "lượt 2" });

        Assert.Equal(HttpStatusCode.Conflict, lai.StatusCode);
        Assert.Contains("vuot-tran-thang", await lai.Content.ReadAsStringAsync());

        // Chặn TRƯỚC khi gọi: số lần gọi KHÔNG tăng.
        Assert.Equal(1, _drafter.SoLanGoi);
    }

    [Fact]
    public async Task Nhom_chua_co_case_nao_thi_noi_ra_chu_khong_goi_model()
    {
        var (app, _) = await MoiKhachAsync(coDrafter: true);
        using var __ = app;

        var res = await app.CreateClient().PostAsJsonAsync("/knowledge/soan-nhap",
            new { nhom = "nhom-khong-ai-dinh-tuyen-vao", nguoiYeuCau = "chị Lan" });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("nhom-khong-co-case", await res.Content.ReadAsStringAsync());
        Assert.Equal(0, _drafter.SoLanGoi);
    }

    [Fact]
    public async Task Thieu_nguoiYeuCau_thi_400()
    {
        var (app, _) = await MoiKhachAsync(coDrafter: true);
        using var __ = app;

        var res = await app.CreateClient().PostAsJsonAsync("/knowledge/soan-nhap",
            new { nhom = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>
    /// Biên lai được ghi KỂ CẢ khi lượt hỏng — gọi model mà đứt giữa chừng vẫn có thể đã tính
    /// tiền, và một cái trần đếm thiếu sẽ đếm thiếu đúng lúc có sự cố lặp đi lặp lại.
    /// </summary>
    [Fact]
    public async Task Luot_HONG_van_duoc_ghi_bien_lai()
    {
        var (app, tenantId) = await MoiKhachAsync(coDrafter: false);
        using var _ = app;
        var c = app.CreateClient();
        var nhom = await DungNhomAsync(app, tenantId, c);

        await c.PostAsJsonAsync("/knowledge/soan-nhap", new { nhom, nguoiYeuCau = "chị Lan" });

        await using var ctx = db.OpenContext(tenantId);
        var luot = await ctx.Set<LuotSoanNhap>().SingleAsync(x => x.Nhom == nhom);

        Assert.False(luot.ThanhCong);
        Assert.Equal("chua-co-credit", luot.Loi);
        Assert.Equal("chị Lan", luot.NguoiYeuCau);
    }

    /// <summary>Mẩu người gửi đã RÚT LẠI không được đi vào bản nháp (`AR-m`).</summary>
    [Fact]
    public async Task Mau_da_rut_lai_KHONG_di_vao_ban_nhap()
    {
        var (app, tenantId) = await MoiKhachAsync(coDrafter: true);
        using var _ = app;
        var c = app.CreateClient();
        var nhom = $"nhom-rut-{Guid.CreateVersion7():N}";
        var khoa = await TaoCaseAsync(c);

        var maMau = $"jira:{Guid.CreateVersion7():N}#c1";
        (await c.PostAsJsonAsync("/signals/case-evidence", new[]
        {
            new
            {
                sourceReference = maMau, caseSourceReference = khoa,
                content = "Đoạn này người gửi đã rút lại", subTenantCode = "khong-xac-dinh",
            },
        })).EnsureSuccessStatusCode();

        (await c.PostAsJsonAsync("/evidence/rut-lai", new
        {
            khoaNguon = maMau, nguoiDanhDau = "chị Lan", lyDo = "gửi nhầm",
        })).EnsureSuccessStatusCode();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(tenantId);
            await scope.ServiceProvider.GetRequiredService<VongKhongCredit>().NapAsync(
                [new KetQuaDinhTuyenNap(khoa, nhom, "noi-ro-trong-mo-ta", "x")],
                new HashSet<string>(StringComparer.Ordinal) { nhom }, "test");
        }

        SopDraftRequest? thay = null;
        _drafter.Dung = r => { thay = r; return NhapToiThieu(r.GroupName); };

        (await c.PostAsJsonAsync("/knowledge/soan-nhap", new { nhom, nguoiYeuCau = "chị Lan" }))
            .EnsureSuccessStatusCode();

        Assert.NotNull(thay);
        Assert.DoesNotContain(thay!.Cases.SelectMany(x => x.Evidence),
            e => e.Content.Contains("đã rút lại"));
    }

    // ===================================================== O5-1 — case do MODEL định tuyến

    /// <summary>Taxonomy tạm chỉ có <paramref name="nhom"/> — cửa `G6` của tầng model đọc từ đây.</summary>
    private string TaxonomyTam(string nhom)
    {
        var duong = Path.Combine(Path.GetTempPath(), $"sn-tax-{Guid.CreateVersion7():N}.json");
        File.WriteAllText(duong, JsonSerializer.Serialize(new { nhom = new[] { new { ten = nhom, moTa = "x" } } }));
        _fileTam.Add(duong);
        return duong;
    }

    private static LuotDinhTuyenModel BienLai(Guid tenantId, string khoa, string nhom, double tin, DateTimeOffset luc) => new()
    {
        TenantId = tenantId, KhoaNguon = khoa, HashDauVao = Guid.CreateVersion7().ToString("N"),
        Nhom = nhom, Nguon = "suy-tu-trieu-chung", GiaiThich = "x", DoTinCay = tin, ThanhCong = true, Luc = luc,
    };

    /// <summary>
    /// 🛑 Đúng ca hai luồng KHÔNG nối nhau trước `O5-1`: chạy toàn bằng khoá API thì model xếp đúng nhóm,
    /// nhưng soạn nháp trả <c>nhom-khong-co-case</c> vì nó chỉ đọc kho NGƯỜI nạp.
    /// </summary>
    [Fact]
    public async Task Case_MODEL_dinh_tuyen_qua_nguong_cung_duoc_gom_va_dem_rieng()
    {
        var (goc, tenantId) = await MoiKhachAsync(coDrafter: true);
        var nhom = $"nhom-model-{Guid.CreateVersion7():N}";
        using var app = goc.With("DinhTuyen:DuongDanTaxonomy", TaxonomyTam(nhom));
        goc.Dispose();
        var c = app.CreateClient();
        var khoa = await TaoCaseAsync(c);

        await using (var ctx = db.OpenContext(tenantId))
        {
            ctx.Set<LuotDinhTuyenModel>().Add(BienLai(tenantId, khoa, nhom, 0.95, DateTimeOffset.UtcNow));
            await ctx.SaveChangesAsync();
        }

        var res = await c.PostAsJsonAsync("/knowledge/soan-nhap", new { nhom, nguoiYeuCau = "chị Lan" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, kq.GetProperty("soCase").GetInt32());
        Assert.Equal(1, kq.GetProperty("soCaseTuModel").GetInt32());
        Assert.Equal(1, _drafter.SoLanGoi);
    }

    /// <summary>
    /// Ba ca model KHÔNG được gom, mỗi ca vì đúng một cửa mà <c>DinhTuyenKho</c> cũng áp cho <c>/hoi</c>:
    /// dưới ngưỡng · người nạp đã nạp nhóm khác (người thắng model) · lần gọi MỚI NHẤT nói nhóm khác.
    /// </summary>
    [Fact]
    public async Task Model_duoi_nguong_bi_nguoi_de_hoac_da_doi_y_thi_KHONG_gom()
    {
        var (goc, tenantId) = await MoiKhachAsync(coDrafter: true);
        var nhom = $"nhom-model-{Guid.CreateVersion7():N}";
        const string nhomKhac = "nhom-khac";
        using var app = goc.With("DinhTuyen:DuongDanTaxonomy", TaxonomyTam(nhom));
        goc.Dispose();
        var c = app.CreateClient();

        var duoiNguong = await TaoCaseAsync(c);
        var biNguoiDe = await TaoCaseAsync(c);
        var doiY = await TaoCaseAsync(c);
        var bayGio = DateTimeOffset.UtcNow;

        await using (var ctx = db.OpenContext(tenantId))
        {
            ctx.Set<LuotDinhTuyenModel>().AddRange(
                BienLai(tenantId, duoiNguong, nhom, 0.5, bayGio),
                BienLai(tenantId, biNguoiDe, nhom, 0.99, bayGio),
                BienLai(tenantId, doiY, nhom, 0.99, bayGio.AddMinutes(-5)),
                BienLai(tenantId, doiY, nhomKhac, 0.99, bayGio));
            ctx.Set<DinhTuyenDaNap>().Add(new DinhTuyenDaNap
            {
                TenantId = tenantId, KhoaNguon = biNguoiDe, Nhom = nhomKhac, Nguon = "noi-ro-trong-mo-ta", NguoiNap = "test",
            });
            await ctx.SaveChangesAsync();
        }

        var res = await c.PostAsJsonAsync("/knowledge/soan-nhap", new { nhom, nguoiYeuCau = "chị Lan" });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("nhom-khong-co-case", await res.Content.ReadAsStringAsync());
        Assert.Equal(0, _drafter.SoLanGoi);
    }
}
