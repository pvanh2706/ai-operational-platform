using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// VÒNG TRI THỨC qua HTTP THẬT: nạp nháp → duyệt → hỏi, cộng cổng "KHÔNG BIẾT".
/// Mảnh (a)(b)(c)(d) của phạm vi demo chốt 2026-09-13.
///
/// ⚠ **MỖI TEST CÓ NHÓM RIÊNG VÀ BẢNG ĐỊNH TUYẾN RIÊNG** — xem <see cref="MoiBoi"/>.
/// Bản đầu của file này dùng CHUNG một tên nhóm cho mọi test, và nó tự làm mình vô nghĩa:
/// cả class chạy trên một database, nên test "chưa duyệt thì phải nói KHÔNG BIẾT" đọc phải
/// record mà test "duyệt rồi thì trả hướng dẫn" vừa duyệt xong, rồi đỏ. Một test đỏ vì test
/// khác còn đỡ; nguy hiểm là chiều ngược lại — nó có thể XANH vì lý do sai.
///
/// ⚠ Bảng định tuyến là bảng RIÊNG CỦA TEST, không phải
/// `docs/ket-qua-phan-tich/dinh-tuyen-150-case.json`: hành vi của cổng phải xác định, không
/// đổi khi ai đó đo lại định tuyến.
///
/// 🛑 Ba test nặng nhất, đọc trước nếu chỉ đọc ba cái:
/// <list type="bullet">
///   <item><c>Chua_duyet_thi_hoi_ra_KHONG_BIET</c> — định tuyến ĐÚNG nhóm mà nhóm chưa có SOP
///   được duyệt thì vẫn phải nói không biết. Cửa mà `docs/13` nhấn mạnh, và là cửa dễ quên.</item>
///   <item><c>Ban_nhap_chua_duyet_KHONG_BAO_GIO_ra_khoi_endpoint_hoi</c> — `D4`.</item>
///   <item><c>Che_do_shared_thi_TU_CHOI_duyet_vi_AR_l_chua_giai</c> — cổng `AR-l` chạy được.</item>
/// </list>
/// </summary>
public sealed class VongTriThucTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private readonly List<string> _fileTam = [];

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists))
        {
            File.Delete(f);
        }
    }

    /// <summary>
    /// Một bối cảnh ĐỘC LẬP cho một test: tên nhóm riêng, hai mã ticket riêng, và một bảng
    /// định tuyến riêng trỏ chúng vào nhau. Không test nào giẫm lên dữ liệu của test nào.
    /// </summary>
    private BoiCanh MoiBoi()
    {
        // ⚠ GUID ĐẦY ĐỦ, KHÔNG CẮT NGẮN. Bản đầu dùng `ToString("N")[..12]` và test
        // `Hai_nhom_khac_nhau_...` XANH KHI CHẠY MỘT MÌNH, ĐỎ KHI CHẠY CẢ BỘ: `Guid.CreateVersion7`
        // xếp theo THỜI GIAN, nên 12 ký tự đầu là timestamp — hai lần gọi trong cùng một mili
        // giây ra CÙNG một chuỗi. Hai bối cảnh lẽ ra độc lập hoá ra dùng chung một tên nhóm.
        // Cắt phần đầu của một id thời-gian-tăng-dần là cắt đúng phần KHÔNG phân biệt được.
        var id = Guid.CreateVersion7().ToString("N");
        var nhom = $"nhom-test-{id}";
        var ticketVaoNhom = $"ES-T{id}-1";
        var ticketKhongBiet = $"ES-T{id}-2";

        var duong = Path.Combine(Path.GetTempPath(), $"dinh-tuyen-test-{id}.json");
        File.WriteAllText(duong, JsonSerializer.Serialize(new
        {
            ketQua = new Dictionary<string, object>
            {
                [ticketVaoNhom] = new { nhom, nguon = "noi-ro-trong-mo-ta", vietTat = "test" },
                [ticketKhongBiet] = new { nhom = "KHONG-BIET", nguon = "suy-tu-trieu-chung", vietTat = "test" },
            },
        }));
        _fileTam.Add(duong);

        return new BoiCanh(nhom, ticketVaoNhom, ticketKhongBiet, duong);
    }

    private sealed record BoiCanh(string Nhom, string TicketVaoNhom, string TicketKhongBiet, string BangDinhTuyen);

    private ApiFactory Dedicated(BoiCanh b, string tenantKey) =>
        ApiFactory.Dedicated(tenantKey).With("DinhTuyen:DuongDanBangTraSan", b.BangDinhTuyen);

    /// <summary>Bản nháp thử — ba bước sửa, trong đó một bước MỒ CÔI (không nhánh nào trỏ tới).</summary>
    private static SopDraft Nhap(string nhom) => new()
    {
        Nhom = nhom,
        SoCaseDungSau = 2,
        TrieuChungVao = [new SopSymptom("Không phát hành được hoá đơn", ["ES-9001"])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1",
                CauHoi = "Ô ký hiệu có giá trị không?",
                NoiXem = "Màn phát hành hoá đơn",
                Nhanh =
                [
                    new SopBranch("Ô trống nhưng chọn được", "S1", ["ES-9001"], "evidence-noi-ro"),
                    new SopBranch("Danh sách rỗng", "S2", [], "toi-suy-ra"),
                ],
            },
        ],
        BuocSua =
        [
            new SopFixStep("S1", "Chọn ký hiệu trên màn phát hành", ["ES-9001"]),
            new SopFixStep("S2", "Thêm ký hiệu trong Cấu hình", []),
            new SopFixStep("S9", "Bước sửa không ai trỏ tới", []),
        ],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(2, ["ES-9001", "ES-9002"]),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, [])),
    };

    // =========================================================================
    //  (a) NẠP BẢN NHÁP
    // =========================================================================

    [Fact]
    public async Task Nap_ban_nhap_thi_sinh_record_o_trang_thai_Draft()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();

        var res = await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, kq.GetProperty("daTao").GetInt32());
        Assert.Equal(0, kq.GetProperty("daCoTuTruoc").GetInt32());

        foreach (var r in kq.GetProperty("nguyenNhan").EnumerateArray())
        {
            Assert.Equal("DRAFT", r.GetProperty("trangThai").GetString());
        }
    }

    /// <summary>
    /// 🛑 HAI NHÓM KHÁC NHAU CÓ CHUNG CÂU QUAN SÁT THÌ VẪN PHẢI RA HAI BỘ RECORD.
    ///
    /// Đây là test sinh ra từ một lỗi thật: bản đầu của handler khử trùng theo `CauseName`
    /// trên TOÀN TENANT, nên bản nháp của nhóm thứ hai bị gộp âm thầm vào record của nhóm thứ
    /// nhất — nạp 3, tạo 1, trả 200, không ai biết. `quanSat` là lời model viết nên trùng nhau
    /// giữa các nhóm là chuyện thường, không phải ca hiếm.
    /// </summary>
    [Fact]
    public async Task Hai_nhom_khac_nhau_cung_cau_quan_sat_thi_khong_bi_gop()
    {
        var b1 = MoiBoi();
        var b2 = MoiBoi();
        using var app = Dedicated(b1, db.TenantAKey);
        var client = app.CreateClient();

        var r1 = await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b1.Nhom));
        var r2 = await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b2.Nhom));

        var kq1 = await r1.Content.ReadFromJsonAsync<JsonElement>();
        var kq2 = await r2.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(3, kq1.GetProperty("daTao").GetInt32());
        Assert.Equal(3, kq2.GetProperty("daTao").GetInt32());
        Assert.Equal(0, kq2.GetProperty("daCoTuTruoc").GetInt32());
    }

    /// <summary>
    /// `T1` — nạp lại cùng bản nháp KHÔNG sinh record trùng. Cùng tinh thần với đường tín
    /// hiệu: gửi lại phải vô hại.
    /// </summary>
    [Fact]
    public async Task Nap_lai_cung_ban_nhap_thi_khong_sinh_record_trung()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom));
        var lanHai = await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom));

        var kq = await lanHai.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, kq.GetProperty("daTao").GetInt32());
        Assert.Equal(3, kq.GetProperty("daCoTuTruoc").GetInt32());
    }

    /// <summary>
    /// Bước sửa mồ côi phải hiện ra NGAY TRONG RESPONSE lúc nạp — không phải nằm trong log để
    /// sáu tuần sau có người đi tìm.
    /// </summary>
    [Fact]
    public async Task Buoc_sua_mo_coi_hien_ra_trong_response_luc_nap()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);

        var res = await app.CreateClient().PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom));
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        var moCoi = kq.GetProperty("buocSuaMoCoi").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("S9", moCoi);
    }

    [Fact]
    public async Task Ban_nhap_khong_co_buoc_sua_nao_thi_tu_choi()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);

        var rong = Nhap(b.Nhom) with { BuocSua = [] };
        var res = await app.CreateClient().PostAsJsonAsync("/knowledge/sop-draft", rong);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Ban_nhap_thieu_ten_nhom_thi_tu_choi()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);

        var res = await app.CreateClient().PostAsJsonAsync("/knowledge/sop-draft", Nhap("   "));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // =========================================================================
    //  (b) DUYỆT
    // =========================================================================

    [Fact]
    public async Task Duyet_thi_record_thanh_Active_va_ghi_lai_ai_mo_quyen_xem()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();

        var id = await NapRoiLayIdDauAsync(client, b.Nhom);

        var res = await client.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "chị Lan (support)", phamViXem = "noi-bo", lyDo = "đã đối chiếu 2 ticket", xacNhanDuLieuThuongMai = true });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("DRAFT", kq.GetProperty("trangThaiTruoc").GetString());
        Assert.Equal("ACTIVE", kq.GetProperty("trangThaiSau").GetString());
        Assert.Equal("noi-bo", kq.GetProperty("phamViXem").GetString());
        Assert.Equal("chị Lan (support)", kq.GetProperty("nguoiDuyet").GetString());
    }

    /// <summary>
    /// 🛑 **CỔNG `AR-n` (B2b-1) — dữ liệu THƯƠNG MẠI.** Chưa khai đã soát thì **400**, và
    /// record **không đổi trạng thái gì cả**.
    ///
    /// Vì sao cổng nằm ở khâu DUYỆT chứ không ở khâu nạp: *"vào kho"* của `AR-n` là vào kho
    /// TRI THỨC. Duyệt LÀ xuất bản — đó là lúc một mẩu thôi nằm trong dữ liệu thô và bắt đầu
    /// hiện ra cho người khác qua một bản SOP. Ca thật: `ES-346608#comment-803265` ghi giá hợp
    /// đồng theo năm của một khách sạn, hạn hợp đồng, và ý định ngừng dùng để chuyển sang PMS
    /// tự xây. Loại đó KHÔNG có hình dạng nào để bắt bằng regex — nên chốt duy nhất là người.
    ///
    /// ⚠ Và phép thử này **không chứng minh dữ liệu thương mại đã bị chặn**. Nó chứng minh
    /// đúng một việc: không ai xuất bản được mà không khai. Hệ thống KHÔNG đối chiếu được lời
    /// khai đó, và hôm nay còn không biết mẩu nguồn là những mẩu nào (`assertion_evidence` có
    /// 0 dòng). Đọc nhầm chỗ này thành "đã kiểm" là đúng thứ `G11` cấm.
    /// </summary>
    [Fact]
    public async Task Chua_xac_nhan_du_lieu_thuong_mai_thi_KHONG_duyet_duoc()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();

        var id = await NapRoiLayIdDauAsync(client, b.Nhom);

        var thieu = await client.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "chị Lan", phamViXem = "noi-bo" });
        Assert.Equal(HttpStatusCode.BadRequest, thieu.StatusCode);
        Assert.Contains("xacNhanDuLieuThuongMai", await thieu.Content.ReadAsStringAsync());

        var khaiFalse = await client.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "chị Lan", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = false });
        Assert.Equal(HttpStatusCode.BadRequest, khaiFalse.StatusCode);

        // Record KHÔNG được nhúc nhích — không có nửa vời.
        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.KnowledgeRecords.FirstAsync(r => r.Id == id);
        Assert.Equal(StoredLifecycleState.Draft, luu.Lifecycle);
        Assert.Null(luu.LastApproval);
    }

    /// <summary>
    /// Khai rồi thì lời khai được **LƯU LẠI** cùng lượt duyệt. Không lưu thì nó chỉ là một ô
    /// tích rồi biến mất — `M2` phần (c) đã dạy đúng điều đó: một lượt duyệt không để lại bằng
    /// chứng thì không phân biệt được với một lượt chưa ai làm.
    /// </summary>
    [Fact]
    public async Task Xac_nhan_roi_thi_loi_khai_duoc_LUU_vao_luot_duyet()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();

        var id = await NapRoiLayIdDauAsync(client, b.Nhom);

        var res = await client.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "chị Lan", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.KnowledgeRecords.FirstAsync(r => r.Id == id);

        Assert.True(luu.LastApproval!.CommercialDataConfirmed);
        Assert.Equal("chị Lan", luu.LastApproval.ApprovedByActor);
    }

    /// <summary>
    /// 🛑 CỔNG `AR-l`, và nó phải CHẠY chứ không chỉ nằm trong tài liệu.
    ///
    /// Duyệt LÀ xuất bản. Ranh giới khách sạn A ↔ B hiện chưa được thực thi ở tầng nào, và
    /// RLS không cứu được kiểu rò đó vì nó xảy ra ở khâu xuất bản chứ không ở khâu truy vấn
    /// hàng. Chế độ shared vì thế không được duyệt cho tới khi `AR-l` có lời giải.
    /// </summary>
    [Fact]
    public async Task Che_do_shared_thi_TU_CHOI_duyet_vi_AR_l_chua_giai()
    {
        var b = MoiBoi();

        // Nạp ở chế độ dedicated (được phép), rồi thử duyệt ở chế độ shared.
        using var dedicated = Dedicated(b, db.TenantAKey);
        var id = await NapRoiLayIdDauAsync(dedicated.CreateClient(), b.Nhom);

        using var shared = ApiFactory.Shared().With("DinhTuyen:DuongDanBangTraSan", b.BangDinhTuyen);
        var client = shared.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Key", db.TenantAKey);

        var res = await client.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "ai đó", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("AR-l", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Duyet_ma_thieu_nguoi_duyet_thi_tu_choi()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();
        var id = await NapRoiLayIdDauAsync(client, b.Nhom);

        var res = await client.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>`S7` — hệ thống KHÔNG BAO GIỜ tự chọn phạm vi xem hộ.</summary>
    [Fact]
    public async Task Duyet_ma_thieu_pham_vi_xem_thi_tu_choi()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();
        var id = await NapRoiLayIdDauAsync(client, b.Nhom);

        var res = await client.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "chị Lan", phamViXem = "", xacNhanDuLieuThuongMai = true });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>
    /// Tri thức của khách A KHÔNG duyệt được từ request của khách B, và biểu hiện là
    /// "không thấy" — đúng như khi nó không tồn tại. Không phân biệt hai ca đó là cố ý.
    /// </summary>
    [Fact]
    public async Task Khach_B_khong_duyet_duoc_tri_thuc_cua_khach_A()
    {
        var b = MoiBoi();
        using var appA = Dedicated(b, db.TenantAKey);
        var id = await NapRoiLayIdDauAsync(appA.CreateClient(), b.Nhom);

        using var appB = Dedicated(b, db.TenantBKey);
        var res = await appB.CreateClient().PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "người của B", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // =========================================================================
    //  (c) + (d) HỎI, VÀ CỔNG "KHÔNG BIẾT"
    // =========================================================================

    /// <summary>
    /// 🛑 CỬA 2 CỦA CỔNG — định tuyến ĐÚNG nhóm, nhưng nhóm đó chưa có SOP nào được duyệt.
    /// `docs/13`: *"Định tuyến đúng nhóm chỉ có nghĩa NẾU nhóm đó có một SOP đã duyệt để
    /// trả về."* Thiếu cửa này thì hệ thống trả rỗng, và rỗng đọc ra thành "không sao cả".
    /// </summary>
    [Fact]
    public async Task Chua_duyet_thi_hoi_ra_KHONG_BIET()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();

        // Nạp nhưng KHÔNG duyệt.
        await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom));

        var res = await client.PostAsJsonAsync("/hoi", new { maTicket = b.TicketVaoNhom });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal("nhom-chua-co-sop-duoc-duyet", kq.GetProperty("lyDoKhongBiet").GetString());

        // Vẫn nói ra nhóm đã định tuyến được — người đọc cần biết nó dừng ở đâu.
        Assert.Equal(b.Nhom, kq.GetProperty("nhom").GetString());
    }

    /// <summary>
    /// 🛑 BẢN NHÁP CHƯA DUYỆT KHÔNG ĐƯỢC RA KHỎI ĐÂY. Nó mang `Origin.AiInference` và chưa
    /// người nào nhìn qua. Đây là ràng buộc an toàn cốt lõi của cả đường hỏi.
    /// </summary>
    [Fact]
    public async Task Ban_nhap_chua_duyet_KHONG_BAO_GIO_ra_khoi_endpoint_hoi()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom));

        var res = await client.PostAsJsonAsync("/hoi", new { maTicket = b.TicketVaoNhom });
        var body = await res.Content.ReadAsStringAsync();

        // Nội dung của bản nháp không được xuất hiện ở bất kỳ đâu trong response.
        Assert.DoesNotContain("Chọn ký hiệu trên màn phát hành", body);
        Assert.DoesNotContain("Ô trống nhưng chọn được", body);
    }

    [Fact]
    public async Task Duyet_roi_thi_hoi_ra_huong_dan()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom));
        await DuyetHetAsync(client, b.Nhom);

        var res = await client.PostAsJsonAsync("/hoi", new { maTicket = b.TicketVaoNhom });
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal(b.Nhom, kq.GetProperty("nhom").GetString());

        var huongDan = kq.GetProperty("huongDan").EnumerateArray().ToList();
        Assert.NotEmpty(huongDan);
        Assert.All(huongDan, h => Assert.Equal("ACTIVE", h.GetProperty("trangThai").GetString()));
    }

    /// <summary>
    /// `AP3` — mỗi phát biểu đi kèm mức kiểm chứng VÀ nguồn gốc. Bày chỗ yếu ra là thứ người
    /// duyệt cần (`G4`/`S8`); giấu đi thì không ai phản bác được.
    ///
    /// Và nguồn gốc phải còn nguyên là `AiInference` SAU KHI DUYỆT: duyệt mở quyền xem, nó
    /// không biến lời của máy thành lời của người — `M2` cần đọc lại được bản gốc.
    /// </summary>
    [Fact]
    public async Task Huong_dan_tra_ve_kem_muc_kiem_chung_va_nguon_goc()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);
        var client = app.CreateClient();

        await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom));
        await DuyetHetAsync(client, b.Nhom);

        var res = await client.PostAsJsonAsync("/hoi", new { maTicket = b.TicketVaoNhom });
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        var cachXuLy = kq.GetProperty("huongDan").EnumerateArray()
            .SelectMany(h => h.GetProperty("cachXuLy").EnumerateArray())
            .ToList();

        Assert.NotEmpty(cachXuLy);
        Assert.All(cachXuLy, p =>
        {
            Assert.Equal("AiInference", p.GetProperty("nguonGoc").GetString());
            Assert.NotNull(p.GetProperty("mucKiemChung").GetString());
        });
    }

    /// <summary>CỬA 1 — bảng đo nói KHÔNG BIẾT thì trả không biết, không đoán một nhóm nào đó.</summary>
    [Fact]
    public async Task Dinh_tuyen_noi_khong_biet_thi_tra_KHONG_BIET()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);

        var res = await app.CreateClient().PostAsJsonAsync("/hoi", new { maTicket = b.TicketKhongBiet });
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal("dinh-tuyen-khong-biet", kq.GetProperty("lyDoKhongBiet").GetString());
    }

    /// <summary>
    /// Bản TRA SẴN không phân loại được văn bản mới, nên đưa tiêu đề + mô tả mà không có mã
    /// ticket thì câu trả lời trung thực duy nhất là KHÔNG BIẾT. Một bản "đoán đại cho có" ở
    /// đây sẽ làm mọi phép đo về sau thành vô nghĩa.
    /// </summary>
    [Fact]
    public async Task Chi_co_tieu_de_mo_ta_thi_ban_tra_san_noi_khong_biet()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);

        var res = await app.CreateClient().PostAsJsonAsync("/hoi", new
        {
            tieuDe = "Không phát hành được hoá đơn",
            moTa = "Khách báo ô ký hiệu trống",
        });

        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal("dinh-tuyen-khong-biet", kq.GetProperty("lyDoKhongBiet").GetString());
    }

    [Fact]
    public async Task Cau_hoi_rong_thi_tu_choi()
    {
        var b = MoiBoi();
        using var app = Dedicated(b, db.TenantAKey);

        var res = await app.CreateClient().PostAsJsonAsync("/hoi",
            new { maTicket = "", tieuDe = "", moTa = "" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>
    /// Tri thức khách A duyệt KHÔNG hiện ra ở câu hỏi của khách B. Cùng bảng định tuyến,
    /// cùng nhóm, cùng mã ticket — khác mỗi tenant.
    /// </summary>
    [Fact]
    public async Task Tri_thuc_cua_khach_A_khong_tra_loi_cho_khach_B()
    {
        var b = MoiBoi();
        using var appA = Dedicated(b, db.TenantAKey);
        var clientA = appA.CreateClient();
        await clientA.PostAsJsonAsync("/knowledge/sop-draft", Nhap(b.Nhom));
        await DuyetHetAsync(clientA, b.Nhom);

        using var appB = Dedicated(b, db.TenantBKey);
        var res = await appB.CreateClient().PostAsJsonAsync("/hoi", new { maTicket = b.TicketVaoNhom });

        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal("nhom-chua-co-sop-duoc-duyet", kq.GetProperty("lyDoKhongBiet").GetString());
    }

    // =========================================================================
    //  Trợ giúp
    // =========================================================================

    private static async Task<Guid> NapRoiLayIdDauAsync(HttpClient client, string nhom)
    {
        var res = await client.PostAsJsonAsync("/knowledge/sop-draft", Nhap(nhom));
        res.EnsureSuccessStatusCode();
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();
        return kq.GetProperty("nguyenNhan").EnumerateArray().First().GetProperty("id").GetGuid();
    }

    /// <summary>Duyệt mọi record của một nhóm; lấy id bằng cách đọc thẳng DB.</summary>
    private async Task DuyetHetAsync(HttpClient client, string nhom)
    {
        var nhan = SopDraftToKnowledge.NhanNhom(nhom);

        await using var ctx = db.OpenContext(db.TenantAId);
        var ids = await ctx.KnowledgeRecords
            .Where(r => r.Assertions.Any(a =>
                a.Kind == AssertionKind.Applicability && a.Content == nhan))
            .Select(r => r.Id)
            .ToListAsync();

        Assert.NotEmpty(ids);

        foreach (var id in ids)
        {
            var res = await client.PostAsJsonAsync($"/knowledge/{id}/approve",
                new { nguoiDuyet = "chị Lan (support)", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true });
            res.EnsureSuccessStatusCode();
        }
    }
}
