using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Process;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// Ô `E` + `F`: đang ở bước nào, SUY RA từ evidence (PR1, `N2-1`). Hàm thuần — không database.
///
/// 🛑 Đây là chỗ DUY NHẤT quyết định "bước này xong chưa". Sai ở đây thì không crash, không đỏ gì ở đâu khác:
/// người làm support chỉ nhận một gợi ý cho bước họ đã làm rồi, hoặc bỏ qua bước họ chưa làm.
/// </summary>
public sealed class TienDoQuyTrinhTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();

    private static ProcessDefinition QuyTrinh()
    {
        var d = new ProcessDefinition
        {
            TenantId = Tenant, Nhom = "nhom", Ten = "Quy trình thử", PhienBan = 1, Origin = Origin.HumanAssessment,
        };

        d.Buoc.Add(Buoc(d, 1, "K1", "Xem nhật ký kết nối", ["nhật ký kết nối"]));
        d.Buoc.Add(Buoc(d, 2, "K2", "Kiểm phân quyền ký hiệu", ["phân quyền"]));
        d.Buoc.Add(Buoc(d, 3, "XL", "Xử lý", []));
        return d;
    }

    private static ProcessStep Buoc(ProcessDefinition d, int thuTu, string ma, string noiDung, List<string> dauHieu) => new()
    {
        TenantId = Tenant, ProcessDefinitionId = d.Id, ThuTu = thuTu, Ma = ma, NoiDung = noiDung,
        DauHieu = dauHieu, ChuDe = SopDraftToKnowledge.NhanNhom("nhom"),
    };

    private static EvidenceItem Mau(string noiDung, Guid? xacNhan = null) => new()
    {
        TenantId = Tenant,
        Content = noiDung,
        SourceReference = $"test:{Guid.CreateVersion7():N}",
        XacNhanProcessStepId = xacNhan,
    };

    [Fact]
    public void Chua_co_evidence_thi_buoc_hien_tai_la_buoc_DAU()
    {
        var td = TienDoQuyTrinh.SuyRa(QuyTrinh(), []);

        Assert.Equal("K1", td.BuocHienTai!.Ma);
        Assert.All(td.Buoc, b => Assert.False(b.DaXong));
    }

    /// <summary>Ticket thật lẫn có dấu và không dấu — so nguyên văn là bỏ sót một nửa.</summary>
    [Fact]
    public void Dau_hieu_khop_KHONG_DAU_khong_hoa_thuong_va_kem_can_cu()
    {
        var mau = Mau("Da xem NHAT KY KET NOI, thay ma E0042");

        var td = TienDoQuyTrinh.SuyRa(QuyTrinh(), [mau]);

        var k1 = td.Buoc.Single(b => b.Ma == "K1");
        Assert.True(k1.DaXong);
        var canCu = Assert.Single(k1.CanCu);
        Assert.Equal(mau.Id, canCu.MauId);
        Assert.Equal("dau-hieu:nhật ký kết nối", canCu.Cach);
        Assert.Equal("K2", td.BuocHienTai!.Ma);
    }

    [Fact]
    public void Loi_xac_nhan_tro_dung_buoc_thi_buoc_do_xong()
    {
        var q = QuyTrinh();
        var k2 = q.Buoc.Single(b => b.Ma == "K2");

        var td = TienDoQuyTrinh.SuyRa(q, [Mau("Đã làm bước K2 — xác nhận bởi chị Lan.", xacNhan: k2.Id)]);

        Assert.True(td.Buoc.Single(b => b.Ma == "K2").DaXong);
        Assert.Equal("xac-nhan", td.Buoc.Single(b => b.Ma == "K2").CanCu.Single().Cach);
    }

    /// <summary>PR1: bước hiện tại là bước chưa xong ĐẦU TIÊN — kể cả khi bước sau đã xong trước.</summary>
    [Fact]
    public void Buoc_sau_xong_truoc_thi_buoc_hien_tai_VAN_la_buoc_dau_chua_xong()
    {
        var td = TienDoQuyTrinh.SuyRa(QuyTrinh(), [Mau("da kiem phan quyen roi")]);

        Assert.True(td.Buoc.Single(b => b.Ma == "K2").DaXong);
        Assert.Equal("K1", td.BuocHienTai!.Ma);
    }

    /// <summary>`AR-m`: một đoạn người gửi đã rút thì không làm xong bước nào.</summary>
    [Fact]
    public void Mau_da_RUT_LAI_khong_lam_xong_buoc_nao()
    {
        var mau = Mau("Da xem nhat ky ket noi");
        mau.RutLai("chị Lan", "gửi nhầm");

        var td = TienDoQuyTrinh.SuyRa(QuyTrinh(), [mau]);

        Assert.False(td.Buoc.Single(b => b.Ma == "K1").DaXong);
    }

    [Fact]
    public void Xong_het_thi_khong_con_buoc_hien_tai()
    {
        var q = QuyTrinh();
        var xl = q.Buoc.Single(b => b.Ma == "XL");

        var td = TienDoQuyTrinh.SuyRa(q,
            [Mau("nhat ky ket noi"), Mau("phan quyen ok"), Mau("xong", xacNhan: xl.Id)]);

        Assert.True(td.DaXongHet);
        Assert.Null(td.BuocHienTai);
    }

    /// <summary>Bước KHÔNG có dấu hiệu nào chỉ xong được bằng xác nhận — không phải xong với mọi mẩu.</summary>
    [Fact]
    public void Buoc_khong_dau_hieu_KHONG_tu_xong_voi_mau_bat_ky()
    {
        var td = TienDoQuyTrinh.SuyRa(QuyTrinh(), [Mau("bat cu noi dung gi")]);

        Assert.False(td.Buoc.Single(b => b.Ma == "XL").DaXong);
    }

    [Theory]
    [InlineData("Đã ĐỔI Mật Khẩu", "da doi mat khau")]
    [InlineData("Nhật ký kết nối", "nhat ky ket noi")]
    public void Chuan_hoa_bo_dau_ke_ca_chu_d(string vao, string ra) =>
        Assert.Equal(ra, TienDoQuyTrinh.ChuanHoa(vao));
}

/// <summary>Bản nháp SOP → quy trình DRAFT (S4 quy tắc 1), và nhãn chủ đề theo bước kiểm (L1).</summary>
public sealed class SopDraftToProcessTests
{
    private static SopDraft Nhap(bool coXacNhan) => new()
    {
        Nhom = "nhom-thu",
        SoCaseDungSau = 2,
        TrieuChungVao = [new SopSymptom("Không phát hành được", ["ES-1"])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1", CauHoi = "Xem nhật ký kết nối", NoiXem = "Nhật ký",
                Nhanh = [new SopBranch("Có mã E0042", "S1", ["ES-1"], "evidence-noi-ro")],
            },
            new SopCheckStep
            {
                Ma = "K2", CauHoi = "Kiểm phân quyền", NoiXem = "Phân quyền",
                Nhanh = [new SopBranch("Thiếu quyền", "S2", ["ES-2"], "toi-suy-ra")],
            },
        ],
        BuocSua = [new SopFixStep("S1", "Sửa cấu hình", ["ES-1"]), new SopFixStep("S2", "Cấp quyền", ["ES-2"])],
        BuocXacNhan = coXacNhan ? new SopConfirmStep("Phát hành lại một hoá đơn thử", ["ES-1"]) : null,
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(2, ["ES-1", "ES-2"]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    [Fact]
    public void Buoc_kiem_theo_thu_tu_roi_buoc_xu_ly_roi_buoc_xac_nhan()
    {
        var q = SopDraftToProcess.Dung(Nhap(coXacNhan: true), Guid.CreateVersion7(), null, phienBan: 1);

        Assert.Equal(["K1", "K2", "XL", "XN"], q.Buoc.OrderBy(b => b.ThuTu).Select(b => b.Ma));
        Assert.Equal(StoredLifecycleState.Draft, q.Lifecycle);
        Assert.Equal(Origin.AiInference, q.Origin);   // D4: máy đề xuất, người công nhận
        Assert.All(q.Buoc, b => Assert.Empty(b.DauHieu));
    }

    [Fact]
    public void Khong_co_buoc_xac_nhan_thi_khong_sinh_buoc_XN()
    {
        var q = SopDraftToProcess.Dung(Nhap(coXacNhan: false), Guid.CreateVersion7(), null, phienBan: 1);

        Assert.Equal(["K1", "K2", "XL"], q.Buoc.OrderBy(b => b.ThuTu).Select(b => b.Ma));
    }

    /// <summary>
    /// L1: bước tra tri thức theo CHỦ ĐỀ. Bước kiểm K1 tra đúng nhãn mà record có nhánh đi ra từ K1 mang — hai
    /// phía dùng CHUNG một hàm ghép nhãn, nên lệch nhau là không thể.
    /// </summary>
    [Fact]
    public void Chu_de_cua_buoc_kiem_KHOP_nhan_tren_tri_thuc_sinh_tu_cung_ban_nhap()
    {
        var nhap = Nhap(coXacNhan: false);
        var q = SopDraftToProcess.Dung(nhap, Guid.CreateVersion7(), null, phienBan: 1);
        var tri = SopDraftToKnowledge.Map(nhap, Guid.CreateVersion7());

        var chuDeK1 = q.Buoc.Single(b => b.Ma == "K1").ChuDe;
        var mangNhanK1 = tri.Records.Where(r => r.Assertions.Any(a =>
            a.Kind == AssertionKind.Applicability && a.Content == chuDeK1)).ToList();

        var r1 = Assert.Single(mangNhanK1);
        Assert.Contains(r1.Assertions, a => a.Kind == AssertionKind.Handling && a.Content == "Sửa cấu hình");

        Assert.Equal(SopDraftToKnowledge.NhanNhom("nhom-thu"), q.Buoc.Single(b => b.Ma == "XL").ChuDe);
    }
}

/// <summary>Bất biến của định nghĩa và lượt chạy — `N5-1`, `N6-1`, PR1.</summary>
public sealed class ProcessDefinitionTests
{
    private static ProcessDefinition Nhap()
    {
        var d = new ProcessDefinition
        {
            TenantId = Guid.CreateVersion7(), Nhom = "n", Ten = "t", PhienBan = 1, Origin = Origin.HumanAssessment,
        };
        d.Buoc.Add(new ProcessStep { TenantId = d.TenantId, ProcessDefinitionId = d.Id, ThuTu = 1, Ma = "K1", NoiDung = "x", ChuDe = "c" });
        return d;
    }

    [Fact]
    public void Duyet_roi_thi_KHONG_sua_buoc_duoc_nua()
    {
        var d = Nhap();
        d.Approve("an", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true);

        Assert.Throws<InvalidOperationException>(() => d.ThayBuoc(
            [new ProcessStep { TenantId = d.TenantId, ProcessDefinitionId = d.Id, ThuTu = 1, Ma = "K9", NoiDung = "y", ChuDe = "c" }]));
    }

    [Fact]
    public void Duyet_lan_hai_thi_bi_tu_choi()
    {
        var d = Nhap();
        d.Approve("an", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true);

        Assert.Throws<InvalidOperationException>(() => d.Approve("an", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: true));
    }

    [Fact]
    public void Chua_xac_nhan_du_lieu_thuong_mai_thi_khong_duyet_duoc() =>
        Assert.Throws<InvalidOperationException>(() => Nhap().Approve("an", VisibilityScope.NoiBo, xacNhanDuLieuThuongMai: false));

    [Fact]
    public void Ma_buoc_trung_thi_bi_tu_choi()
    {
        var d = Nhap();
        Assert.Throws<ArgumentException>(() => ProcessDefinition.KiemBuoc(
        [
            d.Buoc[0],
            new ProcessStep { TenantId = d.TenantId, ProcessDefinitionId = d.Id, ThuTu = 2, Ma = "K1", NoiDung = "y", ChuDe = "c" },
        ]));
    }

    [Fact]
    public void Bo_luot_chay_lan_hai_thi_bi_tu_choi()
    {
        var run = new ProcessRun { TenantId = Guid.CreateVersion7(), CaseId = Guid.CreateVersion7(), ProcessDefinitionId = Guid.CreateVersion7() };
        run.Bo("chị Lan", "khớp nhầm");

        Assert.Throws<InvalidOperationException>(() => run.Bo("anh Bình", "lý do khác"));
        Assert.Equal("chị Lan", run.AbandonedBy);
    }
}
