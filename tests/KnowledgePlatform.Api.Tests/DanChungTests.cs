using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Ô `H` của sơ đồ (P3, 2026-10-10) qua HTTP thật: mã ticket trong bản nháp → case → MỌI mẩu chưa
/// rút lại → <c>assertion_evidence</c>.
///
/// 🛑 Trước P3 bảng đó có 0 dòng — không phải vì chưa ai nạp, mà vì đường nạp vứt mã ticket. Bốn
/// điều file này giữ, và cả bốn là lỗi im lặng nếu thiếu:
/// <list type="bullet">
///   <item>mã TRẦN (<c>DC-…</c>) phải khớp case mang tiền tố nguồn (<c>jira:DC-…</c>);</item>
///   <item>khớp HAI case thì báo "mơ hồ", KHÔNG tự chọn;</item>
///   <item>mẩu đã RÚT LẠI không chống lưng gì;</item>
///   <item>rút lại SAU lần duyệt thì record phải tự thành NEEDS_REVIEW.</item>
/// </list>
///
/// ⚠ Mỗi test một khách hàng RIÊNG: số liên kết là dữ liệu đếm được của cả tenant.
/// </summary>
public sealed class DanChungTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private static string Ma() => $"DC-{Guid.CreateVersion7():N}";

    private async Task<(ApiFactory App, HttpClient C, Guid TenantId)> MoiKhachAsync()
    {
        var (key, id) = await db.TaoTenantMoiAsync();
        var app = ApiFactory.Dedicated(key);
        return (app, app.CreateClient(), id);
    }

    private static async Task TaoCaseAsync(HttpClient c, string khoaNguon) =>
        (await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = khoaNguon, subject = "Không phát hành được hoá đơn" } }))
            .EnsureSuccessStatusCode();

    private static async Task NapMauAsync(HttpClient c, string khoaCase, params string[] noiDung)
    {
        var res = await c.PostAsJsonAsync("/signals/case-evidence", noiDung.Select((n, i) => new
        {
            caseSourceReference = khoaCase,
            sourceReference = $"{khoaCase}#comment-{Guid.CreateVersion7():N}-{i}",
            content = n,
            machineReadability = "High",
            subTenantCode = "khong-xac-dinh",
        }).ToArray());
        res.EnsureSuccessStatusCode();
    }

    /// <summary>Bản nháp một nhánh + một bước sửa, cả hai trích <paramref name="ma"/>.</summary>
    private static SopDraft Nhap(string nhom, string ma, string chungCu = "evidence-noi-ro") => new()
    {
        Nhom = nhom,
        SoCaseDungSau = 1,
        TrieuChungVao = [new SopSymptom("Không phát hành được hoá đơn", [ma])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1", CauHoi = "Ô ký hiệu có giá trị không?", NoiXem = "Màn phát hành",
                Nhanh = [new SopBranch("Ô trống", "S1", [ma], chungCu)],
            },
        ],
        BuocSua = [new SopFixStep("S1", "Chọn ký hiệu rồi phát hành lại", [ma])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(1, [ma]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    private static async Task<JsonElement> NapNhapAsync(HttpClient c, SopDraft nhap)
    {
        var res = await c.PostAsJsonAsync("/knowledge/sop-draft", nhap);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<List<(AssertionKind Kind, EvidenceRelation QuanHe)>> LienKetAsync(Guid tenantId, string nhom)
    {
        await using var ctx = db.OpenContext(tenantId);
        var nhan = SopDraftToKnowledge.NhanNhom(nhom);

        var recordIds = await ctx.KnowledgeRecords
            .Where(r => r.Assertions.Any(a => a.Kind == AssertionKind.Applicability && a.Content == nhan))
            .Select(r => r.Id)
            .ToListAsync();

        return (await ctx.Assertions
                .Where(a => recordIds.Contains(a.KnowledgeRecordId))
                .SelectMany(a => a.EvidenceLinks.Select(l => new { a.Kind, l.Relation }))
                .ToListAsync())
            .Select(x => (x.Kind, x.Relation))
            .ToList();
    }

    // =========================================================================

    [Fact]
    public async Task Ma_TRAN_trong_ban_nhap_noi_toi_MOI_mau_cua_case_co_tien_to()
    {
        var (app, c, tenantId) = await MoiKhachAsync();
        using var _ = app;
        var ma = Ma();
        var nhom = $"nhom-dc-{Guid.CreateVersion7():N}";

        await TaoCaseAsync(c, $"jira:{ma}");
        await NapMauAsync(c, $"jira:{ma}", "Da kiem o ky hieu: trong", "Chon ky hieu xong thi phat hanh duoc");

        var kq = await NapNhapAsync(c, Nhap(nhom, ma));

        var danChung = kq.GetProperty("danChung");
        Assert.True(danChung.GetProperty("soLienKetMoi").GetInt32() > 0);
        Assert.Empty(danChung.GetProperty("maKhongGiaiDuoc").EnumerateArray());

        var lk = await LienKetAsync(tenantId, nhom);

        // Mỗi phát biểu trích case nối tới ĐỦ 2 mẩu của case đó.
        Assert.Equal(2, lk.Count(x => x.Kind == AssertionKind.Recognition));
        Assert.Equal(2, lk.Count(x => x.Kind == AssertionKind.Handling));
        Assert.Equal(2, lk.Count(x => x.Kind == AssertionKind.CauseExists));
        Assert.DoesNotContain(lk, x => x.Kind == AssertionKind.Applicability);
        Assert.All(lk, x => Assert.Equal(EvidenceRelation.Support, x.QuanHe));
    }

    /// <summary><c>toi-suy-ra</c> là lời model tự khai rằng nó SUY RA — liên kết chỉ được là ngữ cảnh.</summary>
    [Fact]
    public async Task Nhanh_toi_suy_ra_thi_lien_ket_chi_la_ContextFor()
    {
        var (app, c, tenantId) = await MoiKhachAsync();
        using var _ = app;
        var ma = Ma();
        var nhom = $"nhom-dc-{Guid.CreateVersion7():N}";

        await TaoCaseAsync(c, $"jira:{ma}");
        await NapMauAsync(c, $"jira:{ma}", "Khach noi da lam lai van loi");

        await NapNhapAsync(c, Nhap(nhom, ma, chungCu: "toi-suy-ra"));

        var lk = await LienKetAsync(tenantId, nhom);
        Assert.All(lk.Where(x => x.Kind == AssertionKind.Recognition),
            x => Assert.Equal(EvidenceRelation.ContextFor, x.QuanHe));
        Assert.All(lk.Where(x => x.Kind == AssertionKind.Handling),
            x => Assert.Equal(EvidenceRelation.Support, x.QuanHe));
    }

    [Fact]
    public async Task Ma_khong_giai_duoc_thi_BAO_RA_va_VAN_nap()
    {
        var (app, c, _) = await MoiKhachAsync();
        using var __ = app;
        var ma = Ma();

        var kq = await NapNhapAsync(c, Nhap($"nhom-dc-{Guid.CreateVersion7():N}", ma));

        Assert.Equal(1, kq.GetProperty("daTao").GetInt32());
        Assert.Contains(ma, kq.GetProperty("danChung").GetProperty("maKhongGiaiDuoc")
            .EnumerateArray().Select(x => x.GetString()));
    }

    /// <summary>
    /// 🛑 Hai connector cùng đánh số kiểu <c>DC-…</c> là chuyện có thật. Tự chọn một trong hai là nối
    /// dẫn chứng của một ticket vào phát biểu nói về ticket KHÁC — đúng loại sai không ai nhìn ra.
    /// </summary>
    [Fact]
    public async Task Ma_khop_HAI_case_qua_duoi_thi_bao_MO_HO_va_KHONG_tu_chon()
    {
        var (app, c, tenantId) = await MoiKhachAsync();
        using var _ = app;
        var ma = Ma();
        var nhom = $"nhom-dc-{Guid.CreateVersion7():N}";

        await TaoCaseAsync(c, $"jira:{ma}");
        await TaoCaseAsync(c, $"crm:{ma}");
        await NapMauAsync(c, $"jira:{ma}", "mau cua jira");
        await NapMauAsync(c, $"crm:{ma}", "mau cua crm");

        var kq = await NapNhapAsync(c, Nhap(nhom, ma));

        Assert.Contains(ma, kq.GetProperty("danChung").GetProperty("maMoHo")
            .EnumerateArray().Select(x => x.GetString()));
        Assert.Empty(await LienKetAsync(tenantId, nhom));
    }

    [Fact]
    public async Task Mau_da_RUT_LAI_khong_duoc_noi()
    {
        var (app, c, tenantId) = await MoiKhachAsync();
        using var _ = app;
        var ma = Ma();
        var nhom = $"nhom-dc-{Guid.CreateVersion7():N}";
        var khoaCase = $"jira:{ma}";

        await TaoCaseAsync(c, khoaCase);
        var khoaMauRut = $"{khoaCase}#description";
        (await c.PostAsJsonAsync("/signals/case-evidence", new[]
        {
            new { caseSourceReference = khoaCase, sourceReference = khoaMauRut, content = "gui nham",
                  machineReadability = "High", subTenantCode = "khong-xac-dinh" },
        })).EnsureSuccessStatusCode();
        await NapMauAsync(c, khoaCase, "mau con dung duoc");

        (await c.PostAsJsonAsync("/evidence/rut-lai", new
        {
            khoaNguon = khoaMauRut, nguoiDanhDau = "chị Lan", lyDo = "Người gửi nói gửi nhầm",
        })).EnsureSuccessStatusCode();

        await NapNhapAsync(c, Nhap(nhom, ma));

        var lk = await LienKetAsync(tenantId, nhom);
        Assert.Equal(1, lk.Count(x => x.Kind == AssertionKind.Handling));
    }

    /// <summary>
    /// Case vào Kênh 1 SAU bản nháp là chuyện thường. Nạp lại bản nháp thì phải ĐIỀN BÙ vào record đã
    /// có — và nạp lần ba thì không điền thêm gì (đã có liên kết rồi).
    /// </summary>
    [Fact]
    public async Task Nap_lai_sau_khi_co_mau_thi_DIEN_BU_dung_mot_lan()
    {
        var (app, c, tenantId) = await MoiKhachAsync();
        using var _ = app;
        var ma = Ma();
        var nhom = $"nhom-dc-{Guid.CreateVersion7():N}";

        var lan1 = await NapNhapAsync(c, Nhap(nhom, ma));
        Assert.Equal(0, lan1.GetProperty("danChung").GetProperty("soLienKetMoi").GetInt32());

        await TaoCaseAsync(c, $"jira:{ma}");
        await NapMauAsync(c, $"jira:{ma}", "mau den sau");

        var lan2 = await NapNhapAsync(c, Nhap(nhom, ma));
        Assert.Equal(0, lan2.GetProperty("daTao").GetInt32());
        Assert.True(lan2.GetProperty("danChung").GetProperty("soLienKetDienBu").GetInt32() > 0);

        var lan3 = await NapNhapAsync(c, Nhap(nhom, ma));
        Assert.Equal(0, lan3.GetProperty("danChung").GetProperty("soLienKetDienBu").GetInt32());

        Assert.Equal(3, (await LienKetAsync(tenantId, nhom)).Count);   // CauseExists + Recognition + Handling
    }

    [Fact]
    public async Task Ban_nhap_goc_duoc_luu_DUNG_MOT_LAN()
    {
        var (app, c, _) = await MoiKhachAsync();
        using var __ = app;
        var nhap = Nhap($"nhom-dc-{Guid.CreateVersion7():N}", Ma());

        var lan1 = await NapNhapAsync(c, nhap);
        var lan2 = await NapNhapAsync(c, nhap);

        Assert.False(lan1.GetProperty("banNhapGoc").GetProperty("daCoTuTruoc").GetBoolean());
        Assert.True(lan2.GetProperty("banNhapGoc").GetProperty("daCoTuTruoc").GetBoolean());
        Assert.Equal(lan1.GetProperty("banNhapGoc").GetProperty("id").GetGuid(),
            lan2.GetProperty("banNhapGoc").GetProperty("id").GetGuid());
    }

    /// <summary>
    /// 🛑 V3 qua HTTP thật: mẩu chống lưng bị rút SAU lần duyệt → GET /knowledge/{id} báo NEEDS_REVIEW,
    /// và chỉ ra MẨU NÀO. Đường đọc mà quên nạp mẩu thì luật này im lặng trả false.
    /// </summary>
    [Fact]
    public async Task Rut_mau_SAU_khi_duyet_thi_doc_lai_bao_NEEDS_REVIEW_va_chi_ra_mau()
    {
        var (app, c, _) = await MoiKhachAsync();
        using var __ = app;
        var ma = Ma();
        var khoaCase = $"jira:{ma}";

        await TaoCaseAsync(c, khoaCase);
        var khoaMau = $"{khoaCase}#comment-1";
        (await c.PostAsJsonAsync("/signals/case-evidence", new[]
        {
            new { caseSourceReference = khoaCase, sourceReference = khoaMau, content = "Chon ky hieu xong",
                  machineReadability = "High", subTenantCode = "khong-xac-dinh" },
        })).EnsureSuccessStatusCode();

        var kq = await NapNhapAsync(c, Nhap($"nhom-dc-{Guid.CreateVersion7():N}", ma));
        var id = kq.GetProperty("nguyenNhan")[0].GetProperty("id").GetGuid();

        (await c.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true }))
            .EnsureSuccessStatusCode();

        (await c.PostAsJsonAsync("/evidence/rut-lai", new
        {
            khoaNguon = khoaMau, nguoiDanhDau = "chị Lan", lyDo = "Người gửi nói gửi nhầm",
        })).EnsureSuccessStatusCode();

        var doc = await (await c.GetAsync($"/knowledge/{id}")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("NEEDS_REVIEW", doc.GetProperty("trangThai").GetString());
        var mau = doc.GetProperty("assertion").EnumerateArray()
            .SelectMany(a => a.GetProperty("danChung").EnumerateArray())
            .ToList();
        Assert.NotEmpty(mau);
        Assert.All(mau, m => Assert.True(m.GetProperty("daRutLai").GetBoolean()));
        Assert.All(mau, m => Assert.Equal(khoaMau, m.GetProperty("khoaNguonMau").GetString()));
    }

    /// <summary>Bản B kế thừa dẫn chứng — qua đường SỬA thật, nơi handler phải nạp liên kết trước khi chép.</summary>
    [Fact]
    public async Task Sua_qua_HTTP_thi_ban_B_VAN_mang_dan_chung()
    {
        var (app, c, _) = await MoiKhachAsync();
        using var __ = app;
        var ma = Ma();

        await TaoCaseAsync(c, $"jira:{ma}");
        await NapMauAsync(c, $"jira:{ma}", "Chon ky hieu xong thi phat hanh duoc");

        var kq = await NapNhapAsync(c, Nhap($"nhom-dc-{Guid.CreateVersion7():N}", ma));
        var id = kq.GetProperty("nguyenNhan")[0].GetProperty("id").GetGuid();

        var doc = await (await c.GetAsync($"/knowledge/{id}")).Content.ReadFromJsonAsync<JsonElement>();
        var xuLy = doc.GetProperty("assertion").EnumerateArray()
            .First(a => a.GetProperty("kind").GetString() == "Handling");

        (await c.PostAsJsonAsync($"/knowledge/{id}/sua", new
        {
            nguoiSua = "chị Lan",
            sua = new[] { new { assertionId = xuLy.GetProperty("id").GetGuid(), noiDungMoi = "Chọn lại ký hiệu", mucKiemChung = "Verified" } },
        })).EnsureSuccessStatusCode();

        var sau = await (await c.GetAsync($"/knowledge/{id}")).Content.ReadFromJsonAsync<JsonElement>();
        var banB = sau.GetProperty("assertion").EnumerateArray()
            .Single(a => a.GetProperty("kind").GetString() == "Handling" && a.GetProperty("conHieuLuc").GetBoolean());

        var lk = Assert.Single(banB.GetProperty("danChung").EnumerateArray());
        Assert.Contains("kế thừa từ bản A", lk.GetProperty("ghiChu").GetString());
    }
}
