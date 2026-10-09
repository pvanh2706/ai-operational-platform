using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Domain.Sop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>Model định tuyến GIẢ — đếm số lần bị gọi, trả đúng câu được dặn.</summary>
public sealed class ModelDinhTuyenGia(Func<KetQuaModelDinhTuyen> traLoi) : IGoiModelDinhTuyen
{
    public int SoLanGoi { get; private set; }

    public string? PromptCuoi { get; private set; }

    public Task<KetQuaModelDinhTuyen> PhanLoaiAsync(DinhTuyenPrompt prompt, CancellationToken ct = default)
    {
        SoLanGoi++;
        PromptCuoi = prompt.UserText;
        return Task.FromResult(traLoi());
    }
}

/// <summary>
/// Định tuyến ticket MỚI bằng model (P8, `N7-1` PROPOSED) — các LUẬT bao quanh lượt gọi, kiểm bằng một model giả.
///
/// 🛑 Bảy điều file này giữ: mặc định KHÔNG gọi (cần cờ riêng) · dưới ngưỡng thì IM LẶNG · nhóm ngoài taxonomy bị bỏ
/// · bộ nhớ đệm (định tuyến chạy trên MỌI webhook) · trần tháng · lỗi model KHÔNG làm vỡ /hoi · và mã ticket KHÔNG
/// BAO GIỜ đi vào payload (biết mã là biết đáp án).
/// </summary>
public sealed class DinhTuyenModelTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private readonly List<string> _fileTam = [];

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private sealed record BoiCanh(ApiFactory App, HttpClient C, Guid TenantId, string Nhom);

    /// <summary>Tên nhóm của bối cảnh vừa dựng — model giả đọc nó LÚC ĐƯỢC GỌI (sau khi bối cảnh đã có).</summary>
    private string? _nhomHienTai;

    private string FileTam(string ten, object noiDung)
    {
        var duong = Path.Combine(Path.GetTempPath(), ten);
        File.WriteAllText(duong, JsonSerializer.Serialize(noiDung));
        _fileTam.Add(duong);
        return duong;
    }

    /// <summary>Khách riêng; bảng đo RỖNG (mọi ticket đều "ngoài bảng"); taxonomy riêng có đúng một nhóm đã duyệt SOP.</summary>
    private async Task<BoiCanh> MoiAsync(ModelDinhTuyenGia model, bool goiModel = true, int tranThang = 500)
    {
        var (key, tenantId) = await db.TaoTenantMoiAsync();
        var id = Guid.CreateVersion7().ToString("N");
        var nhom = $"nhom-dtm-{id}";

        var bang = FileTam($"dt-dtm-{id}.json", new { ketQua = new Dictionary<string, object>() });
        var taxonomy = FileTam($"tx-dtm-{id}.json",
            new { nhom = new[] { new { ten = nhom, moTa = "lệch cấu hình", caseKeys = Array.Empty<string>() } } });

        var app = ApiFactory.Dedicated(key)
            .With("DinhTuyen:DuongDanBangTraSan", bang)
            .With("DinhTuyen:DuongDanTaxonomy", taxonomy)
            .With("DinhTuyen:GoiModel", goiModel ? "true" : "false")
            .With("DinhTuyen:TranThang", tranThang.ToString())
            .WithServices(s => s.AddSingleton(new ModelDinhTuyen(model)));
        var c = app.CreateClient();

        var nap = await (await c.PostAsJsonAsync("/knowledge/sop-draft", SoanNhapTests.NhapToiThieu(nhom))).Content.ReadFromJsonAsync<JsonElement>();
        foreach (var r in nap.GetProperty("nguyenNhan").EnumerateArray())
        {
            (await c.PostAsJsonAsync($"/knowledge/{r.GetProperty("id").GetGuid()}/approve",
                new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true })).EnsureSuccessStatusCode();
        }

        return new BoiCanh(app, c, tenantId, nhom);
    }

    private static KetQuaModelDinhTuyen Tra(string nhom, double tin) => new(nhom, "noi-ro-trong-mo-ta", "có dấu hiệu", tin, 10, 5);

    private static async Task<JsonElement> HoiAsync(BoiCanh b, string tieuDe, string? maTicket = null) =>
        await (await b.C.PostAsJsonAsync("/hoi", new { maTicket, tieuDe, moTa = "Báo E0042 khi phát hành" }))
            .Content.ReadFromJsonAsync<JsonElement>();

    // =========================================================================

    /// <summary>`N7-1`: có khoá thôi CHƯA gọi — định tuyến chạy trên MỌI ticket, chi phí phải là một quyết định.</summary>
    [Fact]
    public async Task Mac_dinh_KHONG_goi_model_du_co_ban_hien_thuc()
    {
        var model = new ModelDinhTuyenGia(() => Tra("x", 1));
        var b = await MoiAsync(model, goiModel: false);
        using var app = b.App;

        var kq = await HoiAsync(b, "Không phát hành được hoá đơn");

        Assert.False(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal(0, model.SoLanGoi);
    }

    [Fact]
    public async Task Bat_co_va_model_chac_chan_thi_dinh_tuyen_RA_nhom_va_noi_nguon_la_model()
    {
        var model = new ModelDinhTuyenGia(() => Tra(_nhomHienTai!, 0.95));
        var b = await MoiAsync(model);
        _nhomHienTai = b.Nhom;
        using var app = b.App;

        var kq = await HoiAsync(b, "Không phát hành được hoá đơn");

        Assert.True(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal(b.Nhom, kq.GetProperty("nhom").GetString());
        Assert.StartsWith("model:", kq.GetProperty("nguonDinhTuyen").GetString());
        Assert.Contains("CHƯA ĐO", kq.GetProperty("nguonDinhTuyen").GetString());
    }

    /// <summary>`E1-1`: thà im lặng hơn thà đoán.</summary>
    [Fact]
    public async Task Duoi_nguong_thi_IM_LANG()
    {
        var model = new ModelDinhTuyenGia(() => Tra(_nhomHienTai!, 0.6));
        var b = await MoiAsync(model);
        _nhomHienTai = b.Nhom;
        using var app = b.App;

        var kq = await HoiAsync(b, "Không phát hành được hoá đơn");

        Assert.False(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Contains("duoi-nguong", kq.GetProperty("nguonDinhTuyen").GetString());
    }

    /// <summary>`G6`: nhóm model bịa ra không được "sửa cho giống".</summary>
    [Fact]
    public async Task Nhom_NGOAI_taxonomy_thi_bi_bo()
    {
        var model = new ModelDinhTuyenGia(() => Tra("nhom-model-tu-bia", 0.99));
        var b = await MoiAsync(model);
        using var app = b.App;

        var kq = await HoiAsync(b, "Không phát hành được hoá đơn");

        Assert.False(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Contains("nhom-ngoai-taxonomy", kq.GetProperty("nguonDinhTuyen").GetString());
    }

    /// <summary>Hỏi lại cùng nội dung → dùng kết quả đã lưu, KHÔNG tính tiền lần hai.</summary>
    [Fact]
    public async Task Hoi_lai_cung_noi_dung_thi_dung_BO_NHO_DEM()
    {
        var model = new ModelDinhTuyenGia(() => Tra(_nhomHienTai!, 0.95));
        var b = await MoiAsync(model);
        _nhomHienTai = b.Nhom;
        using var app = b.App;

        await HoiAsync(b, "Không phát hành được hoá đơn");
        var lan2 = await HoiAsync(b, "Không phát hành được hoá đơn");

        Assert.Equal(1, model.SoLanGoi);
        Assert.Contains("model-da-luu", lan2.GetProperty("nguonDinhTuyen").GetString());
    }

    [Fact]
    public async Task Cham_TRAN_THANG_thi_khong_goi_nua()
    {
        var model = new ModelDinhTuyenGia(() => Tra(_nhomHienTai!, 0.95));
        var b = await MoiAsync(model, tranThang: 1);
        _nhomHienTai = b.Nhom;
        using var app = b.App;

        await HoiAsync(b, "Ticket thứ nhất");
        var lan2 = await HoiAsync(b, "Ticket thứ hai, nội dung khác");

        Assert.Equal(1, model.SoLanGoi);
        Assert.Contains("vuot-tran-thang", lan2.GetProperty("nguonDinhTuyen").GetString());
    }

    /// <summary>Mất mạng / hết credit: /hoi VẪN trả 200 (KHÔNG BIẾT), và biên lai giữ lỗi — đã ghi TRƯỚC lúc gọi.</summary>
    [Fact]
    public async Task Model_loi_thi_KHONG_BIET_va_bien_lai_giu_loi()
    {
        var model = new ModelDinhTuyenGia(() => throw new HttpRequestException("credit balance too low"));
        var b = await MoiAsync(model);
        using var app = b.App;

        var res = await b.C.PostAsJsonAsync("/hoi", new { tieuDe = "Không phát hành được", moTa = "E0042" });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
        await using var ctx = db.OpenContext(b.TenantId);
        var bienLai = await ctx.Set<LuotDinhTuyenModel>().SingleAsync();
        Assert.False(bienLai.ThanhCong);
        Assert.Contains("credit balance too low", bienLai.Loi);
    }

    /// <summary>🛑 Biết mã ticket là biết đáp án (bảng đo tra theo mã) — mã KHÔNG được đi vào payload.</summary>
    [Fact]
    public async Task Ma_ticket_KHONG_BAO_GIO_vao_payload()
    {
        var model = new ModelDinhTuyenGia(() => Tra(_nhomHienTai!, 0.95));
        var b = await MoiAsync(model);
        _nhomHienTai = b.Nhom;
        using var app = b.App;

        await HoiAsync(b, "Không phát hành được hoá đơn", maTicket: "MA-BI-MAT-12345");

        Assert.DoesNotContain("MA-BI-MAT-12345", model.PromptCuoi);
    }
}
