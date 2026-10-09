using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// Ô `H` của sơ đồ (P3, 2026-10-10): bản nháp CÓ mã ticket ở mọi nhánh và mọi bước sửa, và phép
/// map phải GIỮ chúng lại thay vì dùng để chấm điểm rồi vứt.
///
/// 🛑 Trước P3 <c>assertion_evidence</c> có 0 dòng vì đúng chỗ này. Không crash, không đỏ test nào —
/// chỉ là mọi phát biểu do máy sinh không trỏ được về nguồn.
/// </summary>
public sealed class SopDraftTrichDanTests
{
    private static SopDraft Nhap() => new()
    {
        Nhom = "nhom-thu",
        SoCaseDungSau = 4,
        TrieuChungVao = [new SopSymptom("Không phát hành được hoá đơn", ["ES-1"])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1", CauHoi = "Ô ký hiệu có giá trị không?", NoiXem = "Màn phát hành",
                Nhanh = [new SopBranch("Ô trống", "S1", ["ES-1", "ES-2"], "evidence-noi-ro")],
            },
            new SopCheckStep
            {
                Ma = "K2", CauHoi = "Có quyền chọn ký hiệu không?", NoiXem = "Phân quyền",
                Nhanh = [new SopBranch("Không thấy ký hiệu", "S1", ["ES-2", "ES-4"], "toi-suy-ra")],
            },
        ],
        BuocSua = [new SopFixStep("S1", "Chọn ký hiệu rồi phát hành lại", ["ES-3"])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(1, ["ES-1"]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    private static IEnumerable<TrichDanCase> Cua(SopMappingResult kq, AssertionKind kind) =>
        kq.TrichDan.Where(t => t.Assertion.Kind == kind);

    [Fact]
    public void Moi_nhanh_trich_DUNG_ticket_cua_nhanh_do_kem_chungCu()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Guid.CreateVersion7());

        var nhanRa = Cua(kq, AssertionKind.Recognition).ToList();

        var k1 = nhanRa.Where(t => t.Assertion.Content.StartsWith("Ô ký hiệu")).ToList();
        Assert.Equal(["ES-1", "ES-2"], k1.Select(t => t.MaCase));
        Assert.All(k1, t => Assert.Equal("evidence-noi-ro", t.ChungCu));

        var k2 = nhanRa.Where(t => t.Assertion.Content.StartsWith("Có quyền")).ToList();
        Assert.Equal(["ES-2", "ES-4"], k2.Select(t => t.MaCase));
        Assert.All(k2, t => Assert.Equal("toi-suy-ra", t.ChungCu));
    }

    [Fact]
    public void Buoc_sua_trich_ticket_cua_buoc_sua_va_khong_mang_chungCu()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Guid.CreateVersion7());

        var xuLy = Cua(kq, AssertionKind.Handling).Single();

        Assert.Equal("ES-3", xuLy.MaCase);
        Assert.Null(xuLy.ChungCu);
    }

    /// <summary>
    /// Nguyên nhân tồn tại = mọi ticket mà một nhánh dẫn tới nó trích ra. ES-2 xuất hiện ở CẢ HAI
    /// nhánh, nên phải là MỘT dòng — hai dòng là đếm một quan sát thành hai, đúng chỗ `S8` cần chính xác.
    /// </summary>
    [Fact]
    public void Nguyen_nhan_ton_tai_trich_hop_cac_nhanh_va_KHONG_trung()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Guid.CreateVersion7());

        var tonTai = Cua(kq, AssertionKind.CauseExists).Select(t => t.MaCase).ToList();

        Assert.Equal(["ES-1", "ES-2", "ES-4"], tonTai.Order());
    }

    [Fact]
    public void Nhan_nhom_KHONG_trich_ticket_nao()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Guid.CreateVersion7());

        Assert.Empty(Cua(kq, AssertionKind.Applicability));
    }

    /// <summary>Ghi chú nói đủ để người đọc lần ngược: ticket nào, qua nhánh nào, chứng cứ loại gì, phân bố.</summary>
    [Fact]
    public void Ghi_chu_noi_ticket_nhanh_chungCu_va_phan_bo()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Guid.CreateVersion7());

        var ghiChu = Cua(kq, AssertionKind.Recognition).First(t => t.MaCase == "ES-1").GhiChu;

        Assert.Contains("ES-1", ghiChu);
        Assert.Contains("K1→S1", ghiChu);
        Assert.Contains("evidence-noi-ro", ghiChu);
        Assert.Contains("2/4", ghiChu);
    }
}
