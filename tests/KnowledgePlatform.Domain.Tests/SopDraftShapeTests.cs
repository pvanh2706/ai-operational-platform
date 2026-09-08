using System.Text.Json;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// Hình dạng <see cref="SopDraft"/> phải đi qua JSON mà KHÔNG mất trường nào.
///
/// Vì sao bộ test này tồn tại, và nó đắt bao nhiêu nếu không có: bản đầu của
/// <see cref="SopDraft"/> THIẾU hai khối mà bộ eval bắt buộc (`phanBoBuocKiemDuocGhiLai`
/// và `_haiCaseLech`). Kiểu C# vẫn build sạch, vẫn serialize sạch — nó chỉ **im lặng bỏ
/// mất** hai khối đó. Tìm ra 2026-09-08 bằng một phép thử KHÔNG TỐN TIỀN: cho chính cây
/// dựng tay đi qua kiểu C# rồi ném lại vào `nhom_sop.py --kiem-cay`, và nhận 3 phát hiện
/// CHẶN. Nếu chờ lượt gọi API mới biết thì đã trả tiền cho một lỗi kiểu dữ liệu.
///
/// ⚠ Trường mới thêm vào `SopDraft` mà không thêm vào bản nháp mẫu ở đây thì test này
/// KHÔNG bắt được — nó chỉ canh việc MẤT trường, không canh việc THIẾU trường. Phép kiểm
/// đầy-đủ-hay-chưa nằm ở `--kiem-cay`, và nó cần một file thật.
/// </summary>
public sealed class SopDraftShapeTests
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static SopDraft MauDayDu() => new()
    {
        Nhom = "Nhóm thử",
        SoCaseDungSau = 3,
        TrieuChungVao = [new SopSymptom("khách nói không xuất được", ["ES-1"])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1",
                CauHoi = "ký hiệu đã được chọn chưa?",
                NoiXem = "form phát hành trong PMS",
                QuyenCan = "quyền vào mục Nhật ký",
                NguyenVan = "\"Anh chọn giúp em ký hiệu\"",
                BayDaVapThat = "khách tự soát không thay được bước này",
                Nhanh =
                [
                    new SopBranch("chưa chọn", "S1", ["ES-1"], "evidence-noi-ro"),
                    new SopBranch("danh sách rỗng", "BANG", [], "toi-suy-ra"),
                ],
            }
        ],
        BangTraMaLoi = new SopErrorCodeTable(
        [
            new SopErrorCodeRow("VNPT", "ERR:1518", "trường giấy tờ", "S1", ["ES-2"], "evidence-noi-ro")
        ]),
        BuocSua = [new SopFixStep("S1", "chọn ký hiệu rồi phát hành lại", ["ES-1"])],
        BuocXacNhan = new SopConfirmStep("phát hành lại, xác nhận CÓ SỐ hoá đơn", ["ES-1"]),
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(1, ["ES-1"]),
            new SopDistributionBucket(1, ["ES-2"]),
            new SopDistributionBucket(1, ["ES-3"]),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, [])),
        HaiCaseLech = new Dictionary<string, string> { ["ES-999"] = "ca thuần của nhóm khác, dùng làm mốc loại trừ" },
        KhoangTrongPhaiBiet = ["không ticket nào ghi thứ tự kiểm"],
    };

    /// <summary>Đi qua JSON rồi về, không mất trường nào.</summary>
    [Fact]
    public void Di_qua_JSON_roi_ve_khong_mat_truong_nao()
    {
        var goc = MauDayDu();

        var json = JsonSerializer.Serialize(goc, Opts);
        var ve = JsonSerializer.Deserialize<SopDraft>(json, Opts)!;

        Assert.Equal(goc.Nhom, ve.Nhom);
        Assert.Equal(goc.SoCaseDungSau, ve.SoCaseDungSau);
        Assert.Equal(goc.BuocKiem.Count, ve.BuocKiem.Count);
        Assert.Equal(goc.BuocSua.Count, ve.BuocSua.Count);
        Assert.Equal(goc.BangTraMaLoi!.Dong.Count, ve.BangTraMaLoi!.Dong.Count);
        Assert.Equal(goc.BuocXacNhan!.Viec, ve.BuocXacNhan!.Viec);
        Assert.Equal(goc.KhoangTrongPhaiBiet!.Count, ve.KhoangTrongPhaiBiet!.Count);
    }

    /// <summary>
    /// 🛑 Hai khối ĐÃ TỪNG BỊ MẤT phải có mặt sau round-trip. Đây là test canh đúng lỗ đã
    /// xảy ra, nên đừng gộp nó vào test trên: gộp thì lý do nó tồn tại biến mất.
    /// </summary>
    [Fact]
    public void Phan_bo_va_haiCaseLech_KHONG_bi_mat()
    {
        var json = JsonSerializer.Serialize(MauDayDu(), Opts);

        Assert.Contains("phanBoBuocKiemDuocGhiLai", json);
        Assert.Contains("_haiCaseLech", json);

        var ve = JsonSerializer.Deserialize<SopDraft>(json, Opts)!;
        Assert.Equal(1, ve.PhanBoBuocKiemDuocGhiLai.GhiRoBuocKiem.So);
        Assert.Equal("ES-999", ve.HaiCaseLech!.Keys.Single());
    }

    /// <summary>Tên trường JSON phải đúng như bộ eval đọc — tiếng Việt, gạch nối, không camelCase.</summary>
    [Fact]
    public void Ten_truong_JSON_dung_nhu_bo_eval_doc()
    {
        var json = JsonSerializer.Serialize(MauDayDu(), Opts);

        foreach (var ten in new[]
                 {
                     "\"nhom\"", "\"soCaseDungSau\"", "\"trieuChungVao\"", "\"buocKiem\"",
                     "\"buocSua\"", "\"bangTraMaLoi\"", "\"buocXacNhan\"",
                     "\"ma\"", "\"cauHoi\"", "\"noiXem\"", "\"quyenCan\"", "\"nhanh\"",
                     "\"quanSat\"", "\"diToi\"", "\"case\"", "\"chungCu\"",
                     "\"ncc\"", "\"maLoi\"", "\"truongBiTuChoi\"", "\"viec\"",
                     "\"ghi-ro-buoc-kiem\"", "\"buoc-kiem-ngoai-ticket\"", "\"so\"",
                 })
        {
            Assert.Contains(ten, json);
        }
    }

    /// <summary>
    /// Phân bố phải cộng lại bằng <see cref="SopDraft.SoCaseDungSau"/>. Bộ eval kiểm điều
    /// này trên file thật; test này chỉ canh rằng hình dạng ĐỦ CHỖ để đếm được — nếu năm ô
    /// không đọc lại được thì phép cộng ở eval sẽ luôn ra 0 và không ai hiểu vì sao.
    /// </summary>
    [Fact]
    public void Phan_bo_dem_lai_duoc_de_eval_cong_duoc()
    {
        var d = MauDayDu();
        var pb = d.PhanBoBuocKiemDuocGhiLai;

        var tong = pb.GhiRoBuocKiem.So + pb.SuyRaDuocNhungKhongGhi.So + pb.ChiGhiKetLuan.So
                   + pb.BuocKiemNgoaiTicket.So + pb.KhongPhaiChanDoan.So;

        Assert.Equal(d.SoCaseDungSau, tong);
    }

    /// <summary>`bangTraMaLoi` null là hợp lệ — nhóm dạng cây không có bảng (`docs/11` §9).</summary>
    [Fact]
    public void Bang_tra_null_la_hop_le()
    {
        var d = MauDayDu() with { BangTraMaLoi = null };

        var ve = JsonSerializer.Deserialize<SopDraft>(JsonSerializer.Serialize(d, Opts), Opts)!;

        Assert.Null(ve.BangTraMaLoi);
    }
}
