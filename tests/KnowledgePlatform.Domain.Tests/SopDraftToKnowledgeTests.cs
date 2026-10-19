using System.Text.Json;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Domain.Tests;

/// <summary>
/// Đường từ BẢN NHÁP SOP vào KHO TRI THỨC — `T1` (record = nguyên nhân), `AP4` (một cách
/// nhận ra = một assertion), `D4` (người công nhận, không phải AI).
///
/// ⚠ CÂY THỬ Ở ĐÂY CÓ CHU TRÌNH (`K2 -> K3` và `K3 -> K2`) LÀ CỐ Ý. Cây thật do máy sinh
/// cho nhóm 1 có đúng chu trình đó — xem `docs/ket-qua-phan-tich/may-sinh-phan-quyen-ky-hieu.json`.
/// Một mapper liệt kê "đường từ gốc tới lá" sẽ lặp vô hạn trên dữ liệu thật. Nếu ai đổi
/// mapper sang cách liệt kê đường đi, test này phải TREO hoặc ĐỎ — đó là giá trị của nó.
///
/// ⚠ Và bản nháp thử có một bước sửa MỒ CÔI (`S9`) cũng là cố ý: nháp thật có `S8` không
/// nhánh nào trỏ tới. Một bước sửa không tới được là bước sửa không bao giờ dùng được.
/// </summary>
public sealed class SopDraftToKnowledgeTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// Bản nháp thử, giữ đúng ba đặc điểm của dữ liệu thật: có chu trình, có đích không
    /// phải nguyên nhân, và có bước sửa mồ côi.
    /// </summary>
    private static SopDraft Nhap() => new()
    {
        Nhom = "Nhóm thử",
        SoCaseDungSau = 3,
        TrieuChungVao = [new SopSymptom("Không phát hành được", ["ES-1"])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1",
                CauHoi = "Ô ký hiệu có giá trị không?",
                NoiXem = "Màn phát hành hoá đơn",
                Nhanh =
                [
                    new SopBranch("Ô trống nhưng chọn được", "S1", ["ES-1", "ES-2"], "evidence-noi-ro"),
                    new SopBranch("Danh sách rỗng", "K2", ["ES-3"], "evidence-noi-ro"),
                    new SopBranch("Không phải ca này", "NGOAI-PHAM-VI", [], "toi-suy-ra"),
                ],
            },
            new SopCheckStep
            {
                Ma = "K2",
                CauHoi = "Tài khoản nào đang đăng nhập?",
                NoiXem = "ezInvoice admin",
                QuyenCan = "Truy cập invoice admin",
                Nhanh =
                [
                    new SopBranch("Chưa có trong ezInvoice", "S2", ["ES-3"], "evidence-noi-ro"),
                    // chu trình: K2 -> K3, và K3 -> K2 ở dưới
                    new SopBranch("Đã có quyền, xem tiếp role", "K3", [], "toi-suy-ra"),
                ],
            },
            new SopCheckStep
            {
                Ma = "K3",
                CauHoi = "Role trong PMS có quyền hoá đơn không?",
                NoiXem = "PMS · Phân quyền",
                Nhanh =
                [
                    new SopBranch("Role thiếu quyền Thêm hoá đơn", "S3", [], "toi-suy-ra"),
                    new SopBranch("Chỉ ghi 'chưa phân quyền'", "K2", [], "toi-suy-ra"),
                ],
            },
        ],
        BuocSua =
        [
            new SopFixStep("S1", "Chọn ký hiệu trên màn phát hành", ["ES-1", "ES-2"]),
            new SopFixStep("S2", "Thêm tài khoản vào ezInvoice", ["ES-3"]),
            new SopFixStep("S3", "Thêm quyền Thêm hoá đơn cho role", []),
            new SopFixStep("S9", "Bước sửa không ai trỏ tới", []),
        ],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(3, ["ES-1", "ES-2", "ES-3"]),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, [])),
    };

    /// <summary>
    /// `T1` — một nháp của MỘT NHÓM sinh ra NHIỀU record, mỗi bước sửa một cái.
    /// Đây là điều chủ dự án chốt 2026-09-13, và là lý do phép map không phải 1-1.
    /// </summary>
    [Fact]
    public void Moi_buoc_sua_thanh_mot_record()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        Assert.Equal(4, kq.Records.Count);
        Assert.All(kq.Records, r => Assert.Equal(Tenant, r.TenantId));
    }

    /// <summary>
    /// ⚠ Test QUAN TRỌNG NHẤT của file: cây có chu trình thì map vẫn phải xong.
    /// Nó đỏ (hoặc treo) nếu ai đổi sang cách liệt kê đường từ gốc tới lá.
    /// </summary>
    [Fact]
    public void Cay_co_chu_trinh_van_map_xong()
    {
        var nhap = Nhap();

        // tự khẳng định cây thử THẬT SỰ có chu trình — kẻo sửa dữ liệu thử làm test mất nghĩa
        var k2ToiK3 = nhap.BuocKiem.Single(b => b.Ma == "K2").Nhanh.Any(n => n.DiToi == "K3");
        var k3ToiK2 = nhap.BuocKiem.Single(b => b.Ma == "K3").Nhanh.Any(n => n.DiToi == "K2");
        Assert.True(k2ToiK3 && k3ToiK2, "cây thử phải có chu trình, nếu không test này vô nghĩa");

        var kq = SopDraftToKnowledge.Map(nhap, Tenant);

        Assert.NotEmpty(kq.Records);
    }

    /// <summary>`AP4` — một nhánh = MỘT assertion Recognition, không gộp.</summary>
    [Fact]
    public void Moi_nhanh_thanh_mot_recognition()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        // S1 có đúng 1 nhánh trỏ tới (từ K1)
        var s1 = kq.Records.Single(r => r.Assertions.Any(a =>
            a.Kind == AssertionKind.Handling && a.Content.Contains("Chọn ký hiệu")));
        Assert.Single(s1.Assertions, a => a.Kind == AssertionKind.Recognition);

        // S2 có 1 nhánh trỏ tới (từ K2)
        var s2 = kq.Records.Single(r => r.Assertions.Any(a =>
            a.Kind == AssertionKind.Handling && a.Content.Contains("ezInvoice")));
        Assert.Single(s2.Assertions, a => a.Kind == AssertionKind.Recognition);
    }

    /// <summary>
    /// `D4` — KHÔNG có đường nào để AI tự duyệt. Record ra đời ở `Draft`, và
    /// `VisibilityScope` còn trống: chưa ai mở quyền xem cho nó.
    /// </summary>
    [Fact]
    public void Record_ra_doi_o_Draft_va_chua_mo_quyen_xem()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        Assert.All(kq.Records, r =>
        {
            Assert.Equal(StoredLifecycleState.Draft, r.Lifecycle);
            Assert.Null(r.VisibilityScope);
            Assert.Null(r.LastApproval);
        });
    }

    /// <summary>
    /// `D4`/`AP3` — mọi phát biểu sinh ra ở đây là của MÁY, kể cả khi nhánh tự khai
    /// `evidence-noi-ro`. Nhãn đó nói bản nháp có ticket chống lưng, không biến nó thành
    /// lời của người.
    /// </summary>
    [Fact]
    public void Moi_assertion_mang_origin_AiInference()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        Assert.All(kq.Records, r =>
            Assert.All(r.Assertions, a => Assert.Equal(Origin.AiInference, a.Origin)));
    }

    /// <summary>
    /// `V1` — trần là `Supported`. Một phát biểu do máy sinh, TỰ KHAI là có nguồn, mà
    /// được gắn `Verified` thì cả cái thang mất nghĩa.
    /// </summary>
    [Fact]
    public void Khong_assertion_nao_duoc_gan_Verified()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        Assert.All(kq.Records, r =>
            Assert.All(r.Assertions, a =>
                Assert.True(a.Verification <= VerificationLevel.Supported,
                    $"assertion '{a.Content}' được gắn {a.Verification}")));
    }

    /// <summary>
    /// Nhánh trỏ sang bước kiểm KHÁC là bước trung gian, không phải nguyên nhân —
    /// không được sinh record cho nó.
    /// </summary>
    [Fact]
    public void Nhanh_tro_sang_buoc_kiem_khac_khong_sinh_record()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        Assert.DoesNotContain(kq.Records, r => r.CauseName.Contains("Danh sách rỗng"));
        Assert.DoesNotContain(kq.Records, r => r.CauseName.Contains("xem tiếp role"));
    }

    /// <summary>`NGOAI-PHAM-VI` / `BANG` / `K-XN` không phải nguyên nhân.</summary>
    [Fact]
    public void Dich_khong_phai_nguyen_nhan_thi_khong_sinh_record()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        Assert.DoesNotContain(kq.Records, r => r.CauseName.Contains("Không phải ca này"));
    }

    /// <summary>
    /// Bước sửa mồ côi phải được BÁO RA, không nuốt im lặng. Đây là ca thật: nháp nhóm 1
    /// do máy sinh có `S8` mà không nhánh nào trỏ tới.
    /// </summary>
    [Fact]
    public void Buoc_sua_mo_coi_duoc_bao_ra()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        Assert.Contains("S9", kq.BuocSuaMoCoi);
        Assert.True(kq.CoCanhBao);
    }

    /// <summary>
    /// Và record của bước sửa mồ côi phải NÓI RA điều đó trong chính tên nguyên nhân —
    /// chứ không mang một cái tên nghe bình thường rồi nằm im trong kho.
    /// </summary>
    [Fact]
    public void Record_mo_coi_tu_khai_la_khong_co_duong_nhan_ra()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        var moCoi = kq.Records.Single(r => r.CauseName.Contains("KHÔNG CÓ ĐƯỜNG NHẬN RA"));
        Assert.DoesNotContain(moCoi.Assertions, a => a.Kind == AssertionKind.Recognition);
    }

    /// <summary>
    /// `T1` — tên nguyên nhân lấy từ QUAN SÁT, không từ bước sửa: `viec` mô tả cách chữa,
    /// không mô tả bệnh. Đặt tên bằng cách chữa là đặt identity vào thứ dễ mục (`D5`).
    /// </summary>
    [Fact]
    public void Ten_nguyen_nhan_lay_tu_quan_sat_khong_lay_tu_buoc_sua()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        var s1 = kq.Records.Single(r => r.Assertions.Any(a =>
            a.Kind == AssertionKind.Handling && a.Content.Contains("Chọn ký hiệu")));

        Assert.Equal("Ô trống nhưng chọn được", s1.CauseName);
        Assert.DoesNotContain("Chọn ký hiệu", s1.CauseName);
    }

    /// <summary>
    /// `AP1` — quyền cần có thành assertion `Applicability`, không thành field. Ca thật ở
    /// `docs/11` §9.1: nguyên nhân nhóm 1 chặn bước kiểm của nhóm 2.
    /// </summary>
    [Fact]
    public void Quyen_can_thanh_assertion_Applicability()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        var s2 = kq.Records.Single(r => r.Assertions.Any(a =>
            a.Kind == AssertionKind.Handling && a.Content.Contains("ezInvoice")));

        // ⚠ Lọc theo nội dung, KHÔNG chỉ theo Kind: giờ có HAI loại Applicability trên một
        // record — nhãn nhóm và quyền cần có. Test cũ hỏi "đúng một Applicability" và đã đỏ
        // ngay khi nhãn nhóm được thêm vào; đó là test làm đúng việc của nó.
        var quyen = s2.Assertions.Single(a =>
            a.Kind == AssertionKind.Applicability && a.Content.StartsWith("Cần quyền:", StringComparison.Ordinal));
        Assert.Contains("invoice admin", quyen.Content);
    }

    /// <summary>
    /// `AP1` — MỌI record mang nhãn nhóm dạng assertion `Applicability`. Đây là cầu nối
    /// duy nhất giữa bộ định tuyến (trả tên nhóm) và kho tri thức (chứa record theo nguyên
    /// nhân). Thiếu nó thì định tuyến đúng nhóm vẫn tra ra rỗng.
    ///
    /// ⚠ Cố ý KHÔNG làm thành cột: `docs/13` §3.3 đo được 4/5 lỗi định tuyến nằm ở SEAM giữa
    /// hai nhóm kề nhau, và `AR-p` đang mở đúng câu vẽ lại nhóm. Đóng một taxonomy chưa ổn
    /// định vào schema là cam kết vào thứ vừa đo được là đang lung lay.
    /// </summary>
    [Fact]
    public void Moi_record_mang_nhan_nhom_dang_Applicability()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        Assert.All(kq.Records, r =>
            Assert.Single(r.Assertions, a =>
                a.Kind == AssertionKind.Applicability
                && a.Content == SopDraftToKnowledge.NhanNhom("Nhóm thử")));
    }

    /// <summary>
    /// Cách ghép nhãn nhóm phải nằm ở MỘT chỗ duy nhất — lúc ghi và lúc tra dùng chung
    /// <see cref="SopDraftToKnowledge.NhanNhom"/>. Hai chỗ tự ghép chuỗi là hai chỗ lệch nhau
    /// im lặng, và biểu hiện của nó là "định tuyến đúng mà không trả về gì".
    /// </summary>
    [Fact]
    public void Nhan_nhom_ghep_o_mot_cho_duy_nhat()
    {
        Assert.Equal(
            SopDraftToKnowledge.NhanNhomPrefix + "Phân quyền",
            SopDraftToKnowledge.NhanNhom("Phân quyền"));
    }

    /// <summary>
    /// Mỗi record phải có đúng MỘT `CauseExists` — đó là identity của nó (`T1`).
    /// </summary>
    [Fact]
    public void Moi_record_co_dung_mot_CauseExists()
    {
        var kq = SopDraftToKnowledge.Map(Nhap(), Tenant);

        Assert.All(kq.Records, r =>
            Assert.Single(r.Assertions, a => a.Kind == AssertionKind.CauseExists));
    }

    /// <summary>
    /// Nhánh trỏ tới mã KHÔNG tồn tại là bịa đích (`G6`/`AP3`) — phải báo ra.
    /// </summary>
    [Fact]
    public void Nhanh_tro_toi_ma_khong_ton_tai_thi_bao_ra()
    {
        var nhap = Nhap();
        var hong = nhap with
        {
            BuocKiem =
            [
                new SopCheckStep
                {
                    Ma = "K1",
                    CauHoi = "?",
                    NoiXem = "?",
                    Nhanh = [new SopBranch("thấy gì đó", "S-KHONG-CO", [], "toi-suy-ra")],
                },
            ],
        };

        var kq = SopDraftToKnowledge.Map(hong, Tenant);

        Assert.Contains("S-KHONG-CO", kq.NhanhTroLacLoi);
        Assert.True(kq.CoCanhBao);
    }

    /// <summary>Nháp sạch thì KHÔNG có cảnh báo — kẻo cờ cảnh báo luôn bật thành vô dụng.</summary>
    [Fact]
    public void Nhap_sach_thi_khong_co_canh_bao()
    {
        var nhap = Nhap();
        var sach = nhap with { BuocSua = [.. nhap.BuocSua.Where(s => s.Ma != "S9")] };

        var kq = SopDraftToKnowledge.Map(sach, Tenant);

        Assert.False(kq.CoCanhBao);
        Assert.Empty(kq.BuocSuaMoCoi);
        Assert.Empty(kq.NhanhTroLacLoi);
    }

    // =========================================================================
    //  CHẠY TRÊN DỮ LIỆU THẬT — không phải dữ liệu thử
    // =========================================================================

    /// <summary>Cùng cách tìm gốc repo mà `SoanNhapRunner` đã dùng.</summary>
    private static string TimGocRepo()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, ".git")))
        {
            d = d.Parent;
        }

        return d?.FullName ?? Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// 🛑 Bản nháp THẬT do máy sinh phải map được — đây là thứ dữ liệu thử không thay thế
    /// được. Nó mang chu trình thật (`K2` ↔ `K3`) và bước sửa mồ côi thật (`S8`).
    ///
    /// ⚠ File này ĐI THEO GIT (`git ls-files docs/ket-qua-phan-tich/`), nên thiếu nó là
    /// HỎNG chứ không phải "chưa kiểm được" — test phải ĐỎ, không được bỏ qua im lặng.
    /// </summary>
    [Fact]
    public void Map_duoc_ban_nhap_that_do_may_sinh()
    {
        var duong = Path.Combine(
            TimGocRepo(), "docs", "ket-qua-phan-tich", "may-sinh-phan-quyen-ky-hieu.json");
        Assert.True(File.Exists(duong), $"thiếu bản nháp thật: {duong} — file này đi theo git, thiếu là hỏng");

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var nhap = JsonSerializer.Deserialize<SopDraft>(File.ReadAllText(duong), opts)!;

        var kq = SopDraftToKnowledge.Map(nhap, Tenant);

        // 8 bước sửa trong nháp thật -> 8 record
        Assert.Equal(nhap.BuocSua.Count, kq.Records.Count);

        // `S8` là bước sửa mồ côi THẬT — không nhánh nào trỏ tới nó
        Assert.Contains("S8", kq.BuocSuaMoCoi);

        // không nhánh nào bịa đích
        Assert.Empty(kq.NhanhTroLacLoi);

        // D4 vẫn giữ trên dữ liệu thật: chưa record nào được duyệt
        Assert.All(kq.Records, r => Assert.Equal(StoredLifecycleState.Draft, r.Lifecycle));
    }

    // =========================================================================
    //  P6-1 — một DÒNG bảng tra mã lỗi là một cách nhận ra (2026-10-19)
    // =========================================================================

    /// <summary>Nhóm dạng BẢNG TRA (topology nhóm 2): một bước đọc mã lỗi → BANG; mỗi dòng bảng → một bước sửa.</summary>
    private static SopDraft NhapBang(bool coNhanhSangBang = true) => new()
    {
        Nhom = "NCC từ chối payload",
        SoCaseDungSau = 3,
        TrieuChungVao = [new SopSymptom("NCC trả lỗi khi phát hành", ["ES-1", "ES-2", "ES-3"])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1",
                CauHoi = "NCC trả mã lỗi gì?",
                NoiXem = "Thông báo lỗi trên màn phát hành",
                QuyenCan = "Xem nhật ký phát hành",
                Nhanh = coNhanhSangBang
                    ? [new SopBranch("Có mã lỗi", "BANG", ["ES-1", "ES-2", "ES-3"], "evidence-noi-ro")]
                    : [new SopBranch("Không phải ca này", "NGOAI-PHAM-VI", [], "toi-suy-ra")],
            },
        ],
        BangTraMaLoi = new SopErrorCodeTable(
        [
            new SopErrorCodeRow("VNPT", "E0042", "MST người mua", "S1", ["ES-1", "ES-2"], "evidence-noi-ro"),
            new SopErrorCodeRow("VNPT", "E0107", "CCCD người mua", "S2", ["ES-3"], "toi-suy-ra"),
        ]),
        BuocSua =
        [
            new SopFixStep("S1", "Sửa MST trong hồ sơ khách", ["ES-1", "ES-2"]),
            new SopFixStep("S2", "Chuyển hộ chiếu sang đúng trường", ["ES-3"]),
        ],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(3, ["ES-1", "ES-2", "ES-3"]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    /// <summary>
    /// 🛑 Đúng ca tìm ra khi bấm thật trang giả lập (`IM-54`): trước `P6-1`, cả hai bước sửa ra "KHÔNG CÓ ĐƯỜNG NHẬN
    /// RA" và 0 assertion Recognition — vì phép map chỉ đọc nhánh, mà nhánh duy nhất trỏ sang <c>BANG</c>.
    /// </summary>
    [Fact]
    public void Dong_bang_tra_la_mot_cach_nhan_ra_va_khong_con_mo_coi()
    {
        var kq = SopDraftToKnowledge.Map(NhapBang(), Tenant);

        Assert.Empty(kq.BuocSuaMoCoi);
        Assert.Empty(kq.NhanhTroLacLoi);
        Assert.Equal(2, kq.Records.Count);
        Assert.DoesNotContain(kq.Records, r => r.CauseName.Contains("KHÔNG CÓ ĐƯỜNG NHẬN RA"));

        var s1 = kq.Records.Single(r => r.CauseName.Contains("E0042"));
        var nhanRa = Assert.Single(s1.Assertions, a => a.Kind == AssertionKind.Recognition);
        Assert.Contains("E0042", nhanRa.Content);
        Assert.Contains("MST người mua", nhanRa.Content);
        Assert.Contains("NCC trả mã lỗi gì?", nhanRa.Content);   // vẫn nói NƠI người làm đọc mã lỗi
        Assert.Equal(VerificationLevel.Supported, nhanRa.Verification);

        // Dòng `toi-suy-ra` thì KHÔNG được nâng lên Supported — cùng luật với nhánh.
        var s2 = kq.Records.Single(r => r.CauseName.Contains("E0107"));
        Assert.Equal(VerificationLevel.Speculative,
            s2.Assertions.Single(a => a.Kind == AssertionKind.Recognition).Verification);
    }

    /// <summary>
    /// Dòng bảng mang CHỦ ĐỀ của bước dẫn sang bảng (L1) và QUYỀN của bước đó — nếu không, quy trình tra đúng bước K1
    /// sẽ không thấy các nguyên nhân mà chính K1 là nơi nhận ra.
    /// </summary>
    [Fact]
    public void Dong_bang_mang_chu_de_va_quyen_cua_buoc_dan_sang_bang()
    {
        var s1 = SopDraftToKnowledge.Map(NhapBang(), Tenant).Records.Single(r => r.CauseName.Contains("E0042"));
        var apDung = s1.Assertions.Where(a => a.Kind == AssertionKind.Applicability).Select(a => a.Content).ToList();

        Assert.Contains(SopDraftToKnowledge.NhanChuDe("NCC từ chối payload", "K1"), apDung);
        Assert.Contains("Cần quyền: Xem nhật ký phát hành", apDung);
    }

    /// <summary>Dòng bảng trích ĐÚNG ticket của dòng đó, kèm chungCu của dòng — cùng luật trích dẫn với nhánh.</summary>
    [Fact]
    public void Dong_bang_trich_dung_ticket_cua_dong()
    {
        var kq = SopDraftToKnowledge.Map(NhapBang(), Tenant);
        var s1 = kq.Records.Single(r => r.CauseName.Contains("E0042"));
        var nhanRa = s1.Assertions.Single(a => a.Kind == AssertionKind.Recognition);

        var trich = kq.TrichDan.Where(t => ReferenceEquals(t.Assertion, nhanRa)).ToList();

        Assert.Equal(["ES-1", "ES-2"], trich.Select(t => t.MaCase).Order());
        Assert.All(trich, t => Assert.Equal("evidence-noi-ro", t.ChungCu));
        Assert.All(trich, t => Assert.Contains("bảng tra E0042", t.GhiChu));
    }

    /// <summary>Bản nháp có bảng mà KHÔNG nhánh nào dẫn sang: dòng vẫn là cách nhận ra, chỉ không có chủ đề theo bước.</summary>
    [Fact]
    public void Bang_khong_co_nhanh_dan_sang_van_nhan_ra_nhung_khong_gan_chu_de()
    {
        var kq = SopDraftToKnowledge.Map(NhapBang(coNhanhSangBang: false), Tenant);
        var s1 = kq.Records.Single(r => r.CauseName.Contains("E0042"));

        Assert.Empty(kq.BuocSuaMoCoi);
        Assert.StartsWith("Tra bảng mã lỗi", s1.Assertions.Single(a => a.Kind == AssertionKind.Recognition).Content);
        Assert.DoesNotContain(s1.Assertions, a => a.Content.StartsWith(SopDraftToKnowledge.NhanChuDePrefix));
    }

    /// <summary>
    /// Bản nháp THẬT của nhóm NCC (6 dòng bảng). Mọi bước sửa mà một dòng bảng trỏ tới phải có một Recognition
    /// nhắc mã lỗi của dòng đó — trước `P6-1` điều này chỉ ĐÚNG TÌNH CỜ (model chép dòng sang nhánh).
    /// </summary>
    [Fact]
    public void Ban_nhap_that_nhom_NCC_moi_dong_bang_co_duong_nhan_ra()
    {
        var duong = Path.Combine(TimGocRepo(), "docs", "ket-qua-phan-tich", "may-sinh-ncc-tu-choi-payload.json");
        Assert.True(File.Exists(duong), $"thiếu bản nháp thật: {duong} — file này đi theo git, thiếu là hỏng");

        var nhap = JsonSerializer.Deserialize<SopDraft>(File.ReadAllText(duong), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.NotNull(nhap.BangTraMaLoi);

        var kq = SopDraftToKnowledge.Map(nhap, Tenant);

        foreach (var dong in nhap.BangTraMaLoi!.Dong.Where(d => nhap.BuocSua.Any(s => s.Ma == d.DiToi)))
        {
            var buocSua = nhap.BuocSua.Single(s => s.Ma == dong.DiToi);
            var record = kq.Records.Single(r => r.Assertions.Any(a => a.Kind == AssertionKind.Handling && a.Content == buocSua.Viec));
            Assert.Contains(record.Assertions, a => a.Kind == AssertionKind.Recognition && a.Content.Contains(dong.MaLoi));
        }
    }
}
