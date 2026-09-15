using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// `AR-l` (mã khách sạn do bên gửi khai) và `AR-m` (nội dung người gửi đã rút lại) — hai
/// quyết định chốt 2026-09-15, hiện thực 2026-09-16 trong cùng một migration.
///
/// 🛑 **Điều mà bộ test này CỐ Ý không chứng minh:** rằng ranh giới khách sạn A ↔ B đã được
/// thực thi. Nó CHƯA. Những gì dưới đây kiểm là *bên gửi bị buộc phải khai*, *phép đếm giá
/// trị phân biệt nói đúng sự thật*, và *mẩu đã rút không đi tiếp qua đường đọc*. Cổng thật
/// vẫn là cái cũ — chế độ shared TỪ CHỐI DUYỆT (409) — và commit này không gỡ nó.
/// </summary>
public sealed class MaKhachSanVaRutLaiTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private const string NapEvidence = "/signals/case-evidence";
    private const string RutLai = "/evidence/rut-lai";

    private static ApiFactory App(string tenantKey) => ApiFactory.Dedicated(tenantKey);

    private static string Khoa(string prefix) => $"{prefix}:{Guid.CreateVersion7():N}";

    private static async Task<string> DungCaseAsync(HttpClient c)
    {
        var khoa = Khoa("jira:ES");
        var r = await c.PostAsJsonAsync("/signals/case-observed", new[]
        {
            new { sourceReference = khoa, subject = "Loi khong sinh ma dat phong" },
        });
        r.EnsureSuccessStatusCode();
        return khoa;
    }

    private static object Mau(string khoaCase, string khoaMau, string maKhachSan,
        string noiDung = "Da kiem cau hinh ky hieu, khong thay lech") =>
        new
        {
            caseSourceReference = khoaCase,
            sourceReference = khoaMau,
            content = noiDung,
            observedAt = (DateTimeOffset?)null,
            machineReadability = "Medium",
            subTenantCode = maKhachSan,
        };

    // =====================================================================
    //  AR-l — bên gửi KHAI, và phép đếm nói thật
    // =====================================================================

    /// <summary>
    /// 🛑 Phép thử quan trọng nhất của `AR-l` ở tầng cửa vào: bỏ trống là **400**, KHÔNG âm
    /// thầm về <c>khong-xac-dinh</c>.
    ///
    /// Vì sao nó đáng một test riêng: tự điền hộ nghe rất hợp lý ("bên gửi chưa khai thì rõ
    /// ràng là chưa xác định"), và nó sai đúng ở chỗ không dựng lại được. *"Bên gửi đã cân
    /// nhắc và chưa biết"* khác *"bên gửi chưa đọc tài liệu"* — gộp hai cái đó tại cửa vào
    /// thì về sau không có cột nào tách chúng ra nữa.
    /// </summary>
    [Fact]
    public async Task Bo_trong_ma_khach_san_thi_400_va_noi_ra_hai_gia_tri_danh_rieng()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);

        var res = await c.PostAsJsonAsync(NapEvidence, new object[]
        {
            new
            {
                caseSourceReference = khoaCase,
                sourceReference = Khoa("jira:ES") + "#comment-1",
                content = "Khach bao o ky hieu trong",
                observedAt = (DateTimeOffset?)null,
                machineReadability = "Medium",
                // subTenantCode KHÔNG khai — đây chính là ca đang thử.
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("subTenantCode", body);
        Assert.Contains("khong-xac-dinh", body);
        Assert.Contains("khong-thuoc-khach-san", body);
    }

    /// <summary>Khai rồi thì đọc ra được — nếu không thì cột này không kiểm được từ ngoài.</summary>
    [Fact]
    public async Task Ma_khach_san_khai_xong_thi_doc_ra_duoc_qua_duong_doc()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);

        var r = await c.PostAsJsonAsync(NapEvidence,
            new[] { Mau(khoaCase, khoaCase + "#comment-1", "17468") });
        r.EnsureSuccessStatusCode();

        var doc = await c.GetAsync($"/cases/{Uri.EscapeDataString(khoaCase)}/evidence");
        var json = JsonDocument.Parse(await doc.Content.ReadAsStringAsync());

        var mau = json.RootElement.GetProperty("mau")[0];
        Assert.Equal("17468", mau.GetProperty("maKhachSan").GetString());
    }

    /// <summary>
    /// 🎯 `AR-k`/`AR-l` viết thành code: **đếm số giá trị PHÂN BIỆT, không đếm độ phủ.**
    ///
    /// Lô này phủ 100% — mọi mẩu đều có mã, không mẩu nào trống — và vẫn vô dụng cho ranh
    /// giới khách sạn, vì cả lô chỉ có MỘT giá trị. Đó đúng hình dạng của
    /// <c>Mã khách sạn = -1.0</c> (32/32 case) và <c>machineReadability = High</c> (128/128
    /// mẩu), hai lần vấp trong cùng một ngày. Phép kiểm "trường này có được điền không?" trả
    /// lời CÓ ở cả ba ca.
    /// </summary>
    [Fact]
    public async Task Ca_lo_chi_mot_gia_tri_thi_dem_ra_1_va_CANH_BAO()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var goc = Khoa("jira:ES");

        var res = await c.PostAsJsonAsync(NapEvidence, new[]
        {
            Mau(khoaCase, goc + "#c1", "17468"),
            Mau(khoaCase, goc + "#c2", "17468"),
            Mau(khoaCase, goc + "#c3", "17468"),
        });
        res.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(1, json.GetProperty("distinctSubTenantCodes").GetInt32());
        Assert.Contains("PHÂN BIỆT", json.GetProperty("subTenantWarning").GetString());
    }

    /// <summary>
    /// 🛑 Hai giá trị dành riêng KHÔNG được tính vào phép đếm. Bỏ luật này ra thì chỉ cần đổi
    /// từ <c>khong-xac-dinh</c> sang <c>khong-thuoc-khach-san</c> là "đạt điều kiện mở lại"
    /// của `AR-l` — trong khi không khách sạn nào được phân biệt với khách sạn nào. Ta sẽ tự
    /// gây ra đúng cái bẫy <c>-1.0</c>, chỉ khác là lần này bằng tay mình.
    /// </summary>
    [Fact]
    public async Task Hai_gia_tri_danh_rieng_KHONG_tinh_vao_phep_dem()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var goc = Khoa("jira:ES");

        var res = await c.PostAsJsonAsync(NapEvidence, new[]
        {
            Mau(khoaCase, goc + "#c1", "khong-xac-dinh"),
            Mau(khoaCase, goc + "#c2", "khong-thuoc-khach-san"),
        });
        res.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(0, json.GetProperty("distinctSubTenantCodes").GetInt32());
        Assert.NotNull(json.GetProperty("subTenantWarning").GetString());
    }

    /// <summary>Hai mã THẬT thì không còn gì để cảnh báo — nhánh còn lại của cùng một luật.</summary>
    [Fact]
    public async Task Hai_ma_that_thi_dem_ra_2_va_KHONG_canh_bao()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var goc = Khoa("jira:ES");

        var res = await c.PostAsJsonAsync(NapEvidence, new[]
        {
            Mau(khoaCase, goc + "#c1", "17468"),
            Mau(khoaCase, goc + "#c2", "12027"),
        });
        res.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(2, json.GetProperty("distinctSubTenantCodes").GetInt32());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("subTenantWarning").ValueKind);
    }

    /// <summary>
    /// Cùng một khách sạn viết hoa/thường khác nhau vẫn là MỘT. Nếu đếm thành hai thì chính
    /// cái cổng đang dựa vào con số này bị thổi lên bằng một lỗi gõ phím.
    /// </summary>
    [Fact]
    public async Task Hoa_thuong_khac_nhau_van_la_MOT_khach_san()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var goc = Khoa("jira:ES");

        var res = await c.PostAsJsonAsync(NapEvidence, new[]
        {
            Mau(khoaCase, goc + "#c1", "MIRAH-01"),
            Mau(khoaCase, goc + "#c2", "mirah-01"),
        });
        res.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(1, json.GetProperty("distinctSubTenantCodes").GetInt32());
    }

    /// <summary>
    /// 🛑 **Ca BÁO XANH TRONG KHI HỎNG mà `AR-l` dễ vấp nhất.** Export lại toàn bộ corpus kèm
    /// mã khách sạn, nhận 200, thấy <c>created: 0</c> — và tin rằng mã đã vào kho. Thực tế
    /// `K-B3` cấm ghi đè quá khứ nên không mẩu nào đổi, và `AR-l` đứng yên.
    ///
    /// Không ghi đè là ĐÚNG. Im lặng mới là sai — nên response phải nói ra.
    /// </summary>
    [Fact]
    public async Task Gui_lai_kem_ma_MOI_thi_khong_ghi_de_va_PHAI_noi_ra()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var khoaMau = khoaCase + "#comment-1";

        var lan1 = await c.PostAsJsonAsync(NapEvidence,
            new[] { Mau(khoaCase, khoaMau, "khong-xac-dinh") });
        lan1.EnsureSuccessStatusCode();

        var lan2 = await c.PostAsJsonAsync(NapEvidence, new[] { Mau(khoaCase, khoaMau, "17468") });
        lan2.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await lan2.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(0, json.GetProperty("created").GetInt32());
        Assert.True(json.GetProperty("results")[0].GetProperty("subTenantNotApplied").GetBoolean());
        Assert.Contains("KHÔNG được ghi đè", json.GetProperty("subTenantWarning").GetString());

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.EvidenceItems.SingleAsync(e => e.SourceReference == khoaMau);
        Assert.Equal("khong-xac-dinh", luu.SubTenantCode);
    }

    /// <summary>
    /// Điều kiện MỞ LẠI của `AR-l` hỏi được bằng một lượt gọi, và nó trả ra CẢ các giá trị —
    /// một con số "2" không nói ra hai giá trị nào thì không ai kiểm lại được.
    ///
    /// ⚠ Phép thử khẳng định TÍNH CHẤT chứ không khẳng định con số tuyệt đối: kho test dùng
    /// chung giữa các phép thử, nên "đúng 2" là một điều kiện dễ vỡ vì lý do không liên quan.
    /// Chiều "chưa đạt" được kiểm ở phép thử cảnh báo theo lô, nơi con số là xác định.
    /// </summary>
    [Fact]
    public async Task Do_phu_tra_loi_dieu_kien_mo_lai_va_KHONG_tinh_gia_tri_danh_rieng()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var goc = Khoa("jira:ES");
        var maA = "KS-" + Guid.CreateVersion7().ToString("N")[..8];
        var maB = "KS-" + Guid.CreateVersion7().ToString("N")[..8];

        var nap = await c.PostAsJsonAsync(NapEvidence, new[]
        {
            Mau(khoaCase, goc + "#c1", maA),
            Mau(khoaCase, goc + "#c2", maB),
            Mau(khoaCase, goc + "#c3", "khong-xac-dinh"),
        });
        nap.EnsureSuccessStatusCode();

        var res = await c.GetAsync("/internal/sub-tenant-coverage");
        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

        var maDaThay = json.GetProperty("maDaThay").EnumerateArray()
            .Select(x => x.GetString()).ToList();

        Assert.Contains(maA, maDaThay);
        Assert.Contains(maB, maDaThay);
        Assert.DoesNotContain("khong-xac-dinh", maDaThay);
        Assert.DoesNotContain("khong-thuoc-khach-san", maDaThay);
        Assert.True(json.GetProperty("datDieuKienMoLai").GetBoolean());
        Assert.True(json.GetProperty("daKhaiKhongXacDinh").GetInt32() >= 1);

        // 🛑 `datDieuKienMoLai` một mình SẼ nói dối, và đo thật trên kp_dev ngày 2026-09-16
        // cho đúng cặp số đó: mã phân biệt = 2 -> "ĐẠT", trong khi 137/141 mẩu chưa khai gì.
        // Hai mã kia đến từ đúng hai dòng vừa nạp lúc thử. Ai đọc "true" rồi gỡ cổng 409 là
        // mở ranh giới cho một kho mà 97% số mẩu không biết thuộc khách sạn nào.
        // → `canTro` phải luôn nói ra phần còn thiếu. Phép thử này giữ nó khỏi biến mất.
        var canTro = json.GetProperty("canTro").EnumerateArray()
            .Select(x => x.GetString() ?? string.Empty).ToList();

        Assert.NotEmpty(canTro);
        Assert.Contains(canTro, x => x.Contains("dành riêng"));
    }

    // =====================================================================
    //  AR-m — dấu ĐÃ BỊ NGƯỜI GỬI RÚT LẠI
    // =====================================================================

    /// <summary>
    /// 🎯 Ca thật `ES-346661` chạy hết một vòng: một mẩu vào kho, người đánh dấu nó đã bị
    /// người gửi rút lại, và từ đó nó **rời khỏi danh sách dùng được** — nhưng dấu vết còn
    /// nguyên (`G6`: "không có gì" phải phân biệt được với "có nhưng đã rút").
    /// </summary>
    [Fact]
    public async Task Rut_lai_thi_mau_roi_danh_sach_dung_duoc_nhung_dau_vet_con_nguyen()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var khoaMau = khoaCase + "#description";

        var nap = await c.PostAsJsonAsync(NapEvidence, new[]
        {
            Mau(khoaCase, khoaMau, "17468", "Nguyen nhan overbooking: hai ma booking OTA..."),
            Mau(khoaCase, khoaCase + "#comment-1", "17468", "Da kiem lai cau hinh"),
        });
        nap.EnsureSuccessStatusCode();

        var truoc = await DocAsync(c, khoaCase);
        Assert.Equal(2, truoc.GetProperty("soMau").GetInt32());
        Assert.Equal(0, truoc.GetProperty("soMauDaRutLai").GetInt32());

        var res = await c.PostAsJsonAsync(RutLai, new
        {
            khoaNguon = khoaMau,
            nguoiDanhDau = "anh.phamviet",
            lyDo = "Nguoi gui noi gui nham noi dung, doan nay la cua case khac",
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var sau = await DocAsync(c, khoaCase);
        Assert.Equal(1, sau.GetProperty("soMau").GetInt32());
        Assert.Equal(1, sau.GetProperty("soMauDaRutLai").GetInt32());

        var daRut = sau.GetProperty("mauDaRutLai")[0];
        Assert.Equal(khoaMau, daRut.GetProperty("khoaNguon").GetString());
        Assert.Equal("anh.phamviet", daRut.GetProperty("rutLaiBoi").GetString());
        Assert.Contains("gui nham", daRut.GetProperty("rutLaiLyDo").GetString());
    }

    /// <summary>
    /// 🛑 **Nội dung đã rút KHÔNG đi ra qua đường đọc.** Giữ dấu vết mà vẫn trả nguyên văn thì
    /// mọi thứ `AR-m` sinh ra để chặn vẫn đi ra bằng đúng endpoint này — đoạn overbooking của
    /// khách A vẫn tới tay người đang đọc case về mã đặt phòng.
    /// </summary>
    [Fact]
    public async Task Noi_dung_cua_mau_da_rut_KHONG_di_ra_qua_duong_doc()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var khoaMau = khoaCase + "#description";
        const string biMat = "Ma booking Trip.com 8871234 va Expedia 5540982";

        var nap = await c.PostAsJsonAsync(NapEvidence,
            new[] { Mau(khoaCase, khoaMau, "17468", biMat) });
        nap.EnsureSuccessStatusCode();

        await c.PostAsJsonAsync(RutLai, new
        {
            khoaNguon = khoaMau,
            nguoiDanhDau = "anh.phamviet",
            lyDo = "Nguoi gui rut lai",
        });

        var doc = await c.GetAsync($"/cases/{Uri.EscapeDataString(khoaCase)}/evidence");
        var raw = await doc.Content.ReadAsStringAsync();

        Assert.DoesNotContain(biMat, raw);
        Assert.DoesNotContain("noiDung", JsonDocument.Parse(raw).RootElement
            .GetProperty("mauDaRutLai")[0].EnumerateObject().Select(p => p.Name));
    }

    /// <summary>
    /// Đánh dấu lần hai thì **409**, và tác giả + lý do của lần ĐẦU còn nguyên. Trả 200 ở đây
    /// sẽ nói dối rằng lý do vừa gửi đã được ghi.
    /// </summary>
    [Fact]
    public async Task Rut_lai_lan_hai_thi_409_va_GIU_tac_gia_lan_dau()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var khoaMau = khoaCase + "#description";

        (await c.PostAsJsonAsync(NapEvidence, new[] { Mau(khoaCase, khoaMau, "17468") }))
            .EnsureSuccessStatusCode();

        var lan1 = await c.PostAsJsonAsync(RutLai, new
        {
            khoaNguon = khoaMau, nguoiDanhDau = "nguoi.thu.nhat", lyDo = "Ly do that",
        });
        Assert.Equal(HttpStatusCode.OK, lan1.StatusCode);

        var lan2 = await c.PostAsJsonAsync(RutLai, new
        {
            khoaNguon = khoaMau, nguoiDanhDau = "nguoi.thu.hai", lyDo = "Ly do khac han",
        });
        Assert.Equal(HttpStatusCode.Conflict, lan2.StatusCode);

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.EvidenceItems.SingleAsync(e => e.SourceReference == khoaMau);
        Assert.Equal("nguoi.thu.nhat", luu.RetractedBy);
        Assert.Equal("Ly do that", luu.RetractedReason);
    }

    /// <summary>Không khai người đánh dấu thì 400 — `B1-1` chốt CHỈ NGƯỜI điền tay.</summary>
    [Fact]
    public async Task Khong_khai_nguoi_danh_dau_thi_400()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var khoaMau = khoaCase + "#description";

        (await c.PostAsJsonAsync(NapEvidence, new[] { Mau(khoaCase, khoaMau, "17468") }))
            .EnsureSuccessStatusCode();

        var res = await c.PostAsJsonAsync(RutLai, new
        {
            khoaNguon = khoaMau, nguoiDanhDau = "   ", lyDo = "Nguoi gui rut lai",
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>
    /// 🛑 **Phép thử giữ đúng nội dung của `B1-1`: MÁY KHÔNG ĐẶT ĐƯỢC DẤU NÀY.** Đường nạp gửi
    /// kèm <c>retractedAt</c>/<c>retractedBy</c> thì ba trường đó vẫn NULL — hợp đồng tín hiệu
    /// không mang chúng, nên connector không có cách nào tự suy từ câu chữ rồi ghi vào.
    ///
    /// Không có phép thử này thì luật "chỉ người điền tay" chỉ là một câu trong tài liệu.
    /// </summary>
    [Fact]
    public async Task Duong_NAP_khong_dat_duoc_dau_rut_lai()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var khoaMau = khoaCase + "#description";

        var res = await c.PostAsJsonAsync(NapEvidence, new object[]
        {
            new
            {
                caseSourceReference = khoaCase,
                sourceReference = khoaMau,
                content = "Khach noi bo qua giup em tin nhan tren",
                observedAt = (DateTimeOffset?)null,
                machineReadability = "Medium",
                subTenantCode = "17468",
                retractedAt = DateTimeOffset.UtcNow,
                retractedBy = "script",
                retractedReason = "May tu suy tu cau chu",
            },
        });
        res.EnsureSuccessStatusCode();

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.EvidenceItems.SingleAsync(e => e.SourceReference == khoaMau);

        Assert.Null(luu.RetractedAt);
        Assert.Null(luu.RetractedBy);
        Assert.Null(luu.RetractedReason);
        Assert.False(luu.DaRutLai);
    }

    /// <summary>
    /// Gửi lại mẩu đã bị rút thì dấu vẫn còn. Nếu một lượt đồng bộ lại xoá được dấu rút lại
    /// thì luật này tự huỷ sau lần chạy job kế tiếp — mà không ai thấy gì.
    /// </summary>
    [Fact]
    public async Task Gui_lai_mau_da_rut_thi_dau_van_con()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var khoaMau = khoaCase + "#description";

        (await c.PostAsJsonAsync(NapEvidence, new[] { Mau(khoaCase, khoaMau, "17468") }))
            .EnsureSuccessStatusCode();

        (await c.PostAsJsonAsync(RutLai, new
        {
            khoaNguon = khoaMau, nguoiDanhDau = "anh.phamviet", lyDo = "Nguoi gui rut lai",
        })).EnsureSuccessStatusCode();

        (await c.PostAsJsonAsync(NapEvidence, new[] { Mau(khoaCase, khoaMau, "17468") }))
            .EnsureSuccessStatusCode();

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.EvidenceItems.SingleAsync(e => e.SourceReference == khoaMau);
        Assert.True(luu.DaRutLai);
        Assert.Equal("anh.phamviet", luu.RetractedBy);
    }

    /// <summary>Khoá không có thì 404, không phải 200 im lặng.</summary>
    [Fact]
    public async Task Rut_lai_khoa_khong_ton_tai_thi_404()
    {
        using var app = App(db.TenantAKey);
        using var c = app.CreateClient();

        var res = await c.PostAsJsonAsync(RutLai, new
        {
            khoaNguon = Khoa("jira:KHONG-CO") + "#comment-1",
            nguoiDanhDau = "anh.phamviet",
            lyDo = "Thu mot khoa khong ton tai",
        });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private static async Task<JsonElement> DocAsync(HttpClient c, string khoaCase)
    {
        var res = await c.GetAsync($"/cases/{Uri.EscapeDataString(khoaCase)}/evidence");
        res.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
