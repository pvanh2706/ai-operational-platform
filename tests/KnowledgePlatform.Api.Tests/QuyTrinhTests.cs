using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Ô `D` + `E` + `F` của sơ đồ (P4, 2026-10-10 — mở lại `I1`) qua HTTP thật.
///
/// 🛑 Năm điều file này giữ, và cả năm là chỗ một lần "dọn cho gọn" sẽ phá:
/// <list type="bullet">
///   <item>quy trình sinh từ bản nháp nằm ở DRAFT và KHÔNG khớp case nào cho tới khi có người duyệt (`D4`);</item>
///   <item>tiến độ là PHÉP CHIẾU — rút lại lời xác nhận thì bước lại thành chưa xong, không cờ nào phải dọn;</item>
///   <item>bản ACTIVE bất biến (`N5-1`); duyệt bản mới thì case đang chạy GIỮ bản cũ (`N6-1`);</item>
///   <item>bỏ lượt chạy thì hệ thống KHÔNG tự dựng lại;</item>
///   <item>chế độ shared từ chối duyệt (`AR-l`), và khách B không đọc được quy trình của khách A.</item>
/// </list>
/// ⚠ Mỗi test một khách hàng riêng: lượt chạy và quy trình ACTIVE là dữ liệu "có hay không" của cả tenant.
/// </summary>
public sealed class QuyTrinhTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private readonly List<string> _fileTam = [];

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private sealed record BoiCanh(ApiFactory App, HttpClient C, Guid TenantId, string Nhom, string KhoaCase);

    /// <summary>Khách riêng + nhóm riêng + một case <c>jira:QT-…</c>, bảng đo lưu mã TRẦN như bảng thật.</summary>
    private async Task<BoiCanh> MoiAsync()
    {
        var (key, tenantId) = await db.TaoTenantMoiAsync();
        var id = Guid.CreateVersion7().ToString("N");
        var nhom = $"nhom-qt-{id}";

        var bang = Path.Combine(Path.GetTempPath(), $"dt-qt-{id}.json");
        File.WriteAllText(bang, JsonSerializer.Serialize(new
        {
            ketQua = new Dictionary<string, object> { [$"QT-{id}"] = new { nhom, nguon = "noi-ro-trong-mo-ta", vietTat = "t" } },
        }));
        _fileTam.Add(bang);

        var app = ApiFactory.Dedicated(key).With("DinhTuyen:DuongDanBangTraSan", bang);
        var c = app.CreateClient();

        var khoaCase = $"jira:QT-{id}";
        (await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = khoaCase, subject = "Không phát hành được hoá đơn" } })).EnsureSuccessStatusCode();

        return new BoiCanh(app, c, tenantId, nhom, khoaCase);
    }

    private static SopDraft Nhap(string nhom) => new()
    {
        Nhom = nhom,
        SoCaseDungSau = 1,
        TrieuChungVao = [new SopSymptom("Không phát hành được hoá đơn", ["ES-1"])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1", CauHoi = "Xem nhật ký kết nối", NoiXem = "Nhật ký",
                Nhanh = [new SopBranch("Có mã E0042", "S1", ["ES-1"], "evidence-noi-ro")],
            },
        ],
        BuocSua = [new SopFixStep("S1", "Sửa cấu hình rồi phát hành lại", ["ES-1"])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(1, ["ES-1"]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage res, HttpStatusCode mong = HttpStatusCode.OK)
    {
        Assert.Equal(mong, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string Ma(string khoa) => Uri.EscapeDataString(khoa);

    private static async Task<Guid> NapNhapAsync(BoiCanh b) =>
        (await JsonAsync(await b.C.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom))))
        .GetProperty("quyTrinh").GetProperty("id").GetGuid();

    private static Task<HttpResponseMessage> ThemDauHieuAsync(BoiCanh b, Guid id) =>
        b.C.PutAsJsonAsync($"/processes/{id}/buoc", new
        {
            nguoiSua = "chị Lan",
            buoc = new object[]
            {
                new { ma = "K1", noiDung = "Xem nhật ký kết nối", dauHieu = new[] { "nhật ký kết nối" } },
                new { ma = "XL", noiDung = "Xử lý theo nguyên nhân" },
            },
        });

    private static Task<HttpResponseMessage> DuyetAsync(HttpClient c, Guid id) =>
        c.PostAsJsonAsync($"/processes/{id}/approve",
            new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true });

    private static Task<HttpResponseMessage> XacNhanAsync(BoiCanh b, string maBuoc, string? subTenant = "khong-xac-dinh") =>
        b.C.PostAsJsonAsync("/quy-trinh/xac-nhan-buoc",
            new { khoaNguon = b.KhoaCase, maBuoc, nguoiXacNhan = "chị Lan", subTenantCode = subTenant });

    private static async Task<JsonElement> TienDoAsync(BoiCanh b) =>
        await JsonAsync(await b.C.GetAsync($"/cases/{Ma(b.KhoaCase)}/quy-trinh"));

    private static async Task<Guid> SanSangAsync(BoiCanh b)
    {
        var id = await NapNhapAsync(b);
        (await ThemDauHieuAsync(b, id)).EnsureSuccessStatusCode();
        (await DuyetAsync(b.C, id)).EnsureSuccessStatusCode();
        return id;
    }

    // =========================================================================

    [Fact]
    public async Task Nap_ban_nhap_SOP_sinh_quy_trinh_DRAFT_DUNG_MOT_LAN()
    {
        var b = await MoiAsync();
        using var _ = b.App;

        var lan1 = await JsonAsync(await b.C.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom)));
        var lan2 = await JsonAsync(await b.C.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom)));

        var q1 = lan1.GetProperty("quyTrinh");
        Assert.Equal("DRAFT", q1.GetProperty("trangThai").GetString());
        Assert.Equal(2, q1.GetProperty("soBuoc").GetInt32());       // K1 + XL
        Assert.False(q1.GetProperty("daCoTuTruoc").GetBoolean());

        Assert.True(lan2.GetProperty("quyTrinh").GetProperty("daCoTuTruoc").GetBoolean());
        Assert.Equal(q1.GetProperty("id").GetGuid(), lan2.GetProperty("quyTrinh").GetProperty("id").GetGuid());
    }

    /// <summary>`D4`: quy trình máy sinh chưa ai duyệt thì KHÔNG được dẫn đường cho ai.</summary>
    [Fact]
    public async Task Quy_trinh_DRAFT_KHONG_khop_case_nao()
    {
        var b = await MoiAsync();
        using var _ = b.App;
        await NapNhapAsync(b);

        var td = await TienDoAsync(b);

        Assert.False(td.GetProperty("coQuyTrinh").GetBoolean());
        Assert.Equal("nhom-chua-co-quy-trinh-duoc-duyet", td.GetProperty("lyDoKhongCo").GetString());
    }

    [Fact]
    public async Task Duyet_roi_thi_case_khop_va_DOC_KHONG_tao_luot_chay()
    {
        var b = await MoiAsync();
        using var _ = b.App;
        await SanSangAsync(b);

        var td = await TienDoAsync(b);

        Assert.True(td.GetProperty("coQuyTrinh").GetBoolean());
        Assert.False(td.GetProperty("dangChay").GetBoolean());       // đường đọc không ghi gì
        Assert.Equal("K1", td.GetProperty("buocHienTai").GetProperty("ma").GetString());
    }

    [Fact]
    public async Task Mau_chua_dau_hieu_thi_buoc_XONG_kem_can_cu()
    {
        var b = await MoiAsync();
        using var _ = b.App;
        await SanSangAsync(b);

        (await b.C.PostAsJsonAsync("/signals/case-evidence", new[]
        {
            new { caseSourceReference = b.KhoaCase, sourceReference = $"{b.KhoaCase}#comment-1",
                  content = "Da xem nhat ky ket noi, thay E0042", machineReadability = "High", subTenantCode = "khong-xac-dinh" },
        })).EnsureSuccessStatusCode();

        var td = await TienDoAsync(b);

        var k1 = td.GetProperty("buoc").EnumerateArray().First(x => x.GetProperty("ma").GetString() == "K1");
        Assert.True(k1.GetProperty("daXong").GetBoolean());
        Assert.StartsWith("dau-hieu:", k1.GetProperty("canCu")[0].GetProperty("cach").GetString());
        Assert.Equal("XL", td.GetProperty("buocHienTai").GetProperty("ma").GetString());
    }

    /// <summary>
    /// 🛑 Lời xác nhận là EVIDENCE (N2-1) — kiểm thẳng trong database: Origin = UserConfirmed, trỏ đúng bước.
    /// Và rút lại nó thì bước lại thành chưa xong, KHÔNG có cờ nào phải dọn (PR1).
    /// </summary>
    [Fact]
    public async Task Xac_nhan_la_EVIDENCE_va_rut_lai_thi_buoc_CHUA_XONG_lai()
    {
        var b = await MoiAsync();
        using var _ = b.App;
        await SanSangAsync(b);

        var sau = await JsonAsync(await XacNhanAsync(b, "K1"));
        Assert.True(sau.GetProperty("dangChay").GetBoolean());
        Assert.Equal("XL", sau.GetProperty("buocHienTai").GetProperty("ma").GetString());

        string khoaMau;
        await using (var ctx = db.OpenContext(b.TenantId))
        {
            var mau = await ctx.EvidenceItems.SingleAsync(e => e.XacNhanProcessStepId != null && e.SourceReference.StartsWith("kp:xac-nhan:"));
            Assert.Equal(Origin.UserConfirmed, mau.Origin);
            khoaMau = mau.SourceReference;
        }

        (await b.C.PostAsJsonAsync("/evidence/rut-lai", new
        {
            khoaNguon = khoaMau, nguoiDanhDau = "chị Lan", lyDo = "Bấm nhầm bước",
        })).EnsureSuccessStatusCode();

        Assert.Equal("K1", (await TienDoAsync(b)).GetProperty("buocHienTai").GetProperty("ma").GetString());
    }

    [Fact]
    public async Task Sua_buoc_khi_da_ACTIVE_thi_409()
    {
        var b = await MoiAsync();
        using var _ = b.App;
        var id = await SanSangAsync(b);

        Assert.Equal(HttpStatusCode.Conflict, (await ThemDauHieuAsync(b, id)).StatusCode);
    }

    /// <summary>`N6-1`: duyệt bản mới thì bản cũ DEPRECATED, nhưng case đang chạy dở VẪN chạy bản cũ.</summary>
    [Fact]
    public async Task Duyet_phien_ban_moi_thi_ban_cu_DEPRECATED_va_case_dang_chay_GIU_ban_cu()
    {
        var b = await MoiAsync();
        using var _ = b.App;
        var v1 = await SanSangAsync(b);
        (await XacNhanAsync(b, "K1")).EnsureSuccessStatusCode();   // lượt chạy trên v1

        var v2 = (await JsonAsync(await b.C.PostAsJsonAsync("/processes", new
        {
            nhom = b.Nhom, ten = "Phiên bản 2", nguoiTao = "chị Lan",
            buoc = new object[] { new { ma = "B1", noiDung = "Bước mới" } },
        }))).GetProperty("id").GetGuid();

        var duyet = await JsonAsync(await DuyetAsync(b.C, v2));
        Assert.Contains(v1, duyet.GetProperty("daRutPhienBanCu").EnumerateArray().Select(x => x.GetGuid()));

        var cu = await JsonAsync(await b.C.GetAsync($"/processes/{v1}"));
        Assert.Equal("DEPRECATED", cu.GetProperty("trangThai").GetString());

        var td = await TienDoAsync(b);
        Assert.Equal(v1, td.GetProperty("quyTrinh").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Bo_luot_chay_thi_he_thong_KHONG_tu_dung_lai()
    {
        var b = await MoiAsync();
        using var _ = b.App;
        await SanSangAsync(b);
        (await XacNhanAsync(b, "K1")).EnsureSuccessStatusCode();

        (await b.C.PostAsJsonAsync("/quy-trinh/bo", new
        {
            khoaNguon = b.KhoaCase, nguoiBo = "chị Lan", lyDo = "Khớp nhầm quy trình",
        })).EnsureSuccessStatusCode();

        var td = await TienDoAsync(b);
        Assert.False(td.GetProperty("coQuyTrinh").GetBoolean());
        Assert.Equal("luot-chay-da-bi-bo", td.GetProperty("lyDoKhongCo").GetString());

        Assert.Equal(HttpStatusCode.Conflict, (await XacNhanAsync(b, "K1")).StatusCode);
    }

    [Fact]
    public async Task Xac_nhan_ma_buoc_khong_co_thi_400_va_LIET_KE_ma_dung()
    {
        var b = await MoiAsync();
        using var _ = b.App;
        await SanSangAsync(b);

        var res = await XacNhanAsync(b, "K9");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("K1, XL", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Xac_nhan_thieu_subTenantCode_thi_400()
    {
        var b = await MoiAsync();
        using var _ = b.App;
        await SanSangAsync(b);

        Assert.Equal(HttpStatusCode.BadRequest, (await XacNhanAsync(b, "K1", subTenant: null)).StatusCode);
    }

    [Fact]
    public async Task Che_do_shared_TU_CHOI_duyet_quy_trinh()
    {
        using var app = ApiFactory.Shared();
        var c = app.CreateClient();
        c.DefaultRequestHeaders.Authorization = ApiDatabaseFixture.Bearer(db.TenantAApiKey);

        var id = (await JsonAsync(await c.PostAsJsonAsync("/processes", new
        {
            nhom = $"nhom-shared-{Guid.CreateVersion7():N}", ten = "t", nguoiTao = "x",
            buoc = new object[] { new { ma = "B1", noiDung = "Bước" } },
        }))).GetProperty("id").GetGuid();

        var res = await DuyetAsync(c, id);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("AR-l", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Khach_B_KHONG_doc_duoc_quy_trinh_cua_khach_A()
    {
        using var app = ApiFactory.Shared();
        var a = app.CreateClient();
        a.DefaultRequestHeaders.Authorization = ApiDatabaseFixture.Bearer(db.TenantAApiKey);
        var bClient = app.CreateClient();
        bClient.DefaultRequestHeaders.Authorization = ApiDatabaseFixture.Bearer(db.TenantBApiKey);

        var id = (await JsonAsync(await a.PostAsJsonAsync("/processes", new
        {
            nhom = $"nhom-a-{Guid.CreateVersion7():N}", ten = "của A", nguoiTao = "x",
            buoc = new object[] { new { ma = "B1", noiDung = "Bước" } },
        }))).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync($"/processes/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bClient.GetAsync($"/processes/{id}")).StatusCode);
    }
}
