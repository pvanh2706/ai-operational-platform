using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// ĐƯỜNG NẠP BẢN B qua HTTP thật — thứ người làm support sửa lại.
///
/// 🛑 Đây là vế còn thiếu của `M2`. A (máy sinh) vào kho được từ 2026-09-13 sáng, nhưng B
/// chưa có đường nào — nên `M2` không đo được gì dù cả hai nửa đều đã thiết kế xong từ lâu.
///
/// Hai test nặng nhất:
/// <list type="bullet">
///   <item><c>Lo_sai_mot_muc_thi_KHONG_muc_nao_duoc_ghi</c> — sửa được 3/5 rồi báo lỗi thì
///   người duyệt không biết phải gửi lại những mục nào.</item>
///   <item><c>Doc_lai_thi_van_thay_ban_A</c> — `/hoi` cố ý chỉ trả bản còn hiệu lực, nên nếu
///   thiếu đường đọc lịch sử thì bản A nằm trong database mà KHÔNG CÓ CÁCH NÀO lấy ra.
///   Dữ liệu đúng, đường đọc thiếu — một cách hỏng rất im lặng.</item>
/// </list>
/// </summary>
public sealed class BanBSuaTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private readonly List<string> _fileTam = [];

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    /// <summary>Bối cảnh độc lập — xem chú thích cùng tên ở <c>VongTriThucTests</c>.</summary>
    private (string Nhom, string Ticket, string Bang) MoiBoi()
    {
        var id = Guid.CreateVersion7().ToString("N");
        var nhom = $"nhom-sua-{id}";
        var ticket = $"ES-S{id}";
        var duong = Path.Combine(Path.GetTempPath(), $"dinh-tuyen-sua-{id}.json");
        File.WriteAllText(duong, JsonSerializer.Serialize(new
        {
            ketQua = new Dictionary<string, object>
            {
                [ticket] = new { nhom, nguon = "noi-ro-trong-mo-ta", vietTat = "test" },
            },
        }));
        _fileTam.Add(duong);
        return (nhom, ticket, duong);
    }

    private static SopDraft Nhap(string nhom) => new()
    {
        Nhom = nhom,
        SoCaseDungSau = 1,
        TrieuChungVao = [new SopSymptom("Không phát hành được", ["ES-9001"])],
        BuocKiem =
        [
            new SopCheckStep
            {
                Ma = "K1",
                CauHoi = "Ô ký hiệu có giá trị không?",
                NoiXem = "Màn phát hành",
                Nhanh = [new SopBranch("Ô trống", "S1", ["ES-9001"], "evidence-noi-ro")],
            },
        ],
        BuocSua = [new SopFixStep("S1", "Chọn ký hiệu rồi phát hành lại", ["ES-9001"])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(1, ["ES-9001"]),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, [])),
    };

    /// <summary>Nạp nháp rồi trả về (id record, danh sách assertion hiện hành).</summary>
    private static async Task<(Guid Id, List<JsonElement> Assertion)> NapAsync(HttpClient c, string nhom)
    {
        var res = await c.PostAsJsonAsync("/knowledge/sop-draft", Nhap(nhom));
        res.EnsureSuccessStatusCode();
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();
        var id = kq.GetProperty("nguyenNhan").EnumerateArray().First().GetProperty("id").GetGuid();

        var doc = await c.GetFromJsonAsync<JsonElement>($"/knowledge/{id}");
        var ds = doc.GetProperty("assertion").EnumerateArray()
            .Where(a => a.GetProperty("conHieuLuc").GetBoolean()).ToList();
        return (id, ds);
    }

    private static Guid IdCua(List<JsonElement> ds, string kind) =>
        ds.First(a => a.GetProperty("kind").GetString() == kind).GetProperty("id").GetGuid();

    // =========================================================================

    [Fact]
    public async Task Sua_thi_response_bay_ca_ban_A_lan_ban_B()
    {
        var b = MoiBoi();
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var c = app.CreateClient();
        var (id, ds) = await NapAsync(c, b.Nhom);

        var res = await c.PostAsJsonAsync($"/knowledge/{id}/sua", new
        {
            nguoiSua = "chị Lan (support)",
            sua = new[]
            {
                new { assertionId = IdCua(ds, "Handling"), noiDungMoi = "Vào Cấu hình > Ký hiệu, thêm ký hiệu TRƯỚC rồi mới phát hành", mucKiemChung = "Verified" },
            },
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        var m = kq.GetProperty("daSua").EnumerateArray().Single();
        Assert.Equal("Handling", m.GetProperty("kind").GetString());
        Assert.Equal("AiInference", m.GetProperty("banA").GetProperty("nguonGoc").GetString());
        Assert.Equal("HumanAssessment", m.GetProperty("banB").GetProperty("nguonGoc").GetString());
        Assert.Contains("Cấu hình", m.GetProperty("banB").GetProperty("noiDung").GetString()!);
    }

    /// <summary>🛑 Bản A phải ĐỌC LẠI ĐƯỢC. Không có đường này thì `M2` không đo được.</summary>
    [Fact]
    public async Task Doc_lai_thi_van_thay_ban_A()
    {
        var b = MoiBoi();
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var c = app.CreateClient();
        var (id, ds) = await NapAsync(c, b.Nhom);
        var idA = IdCua(ds, "Handling");

        await c.PostAsJsonAsync($"/knowledge/{id}/sua", new
        {
            nguoiSua = "chị Lan",
            sua = new[] { new { assertionId = idA, noiDungMoi = "bản người sửa", mucKiemChung = "Verified" } },
        });

        var doc = await c.GetFromJsonAsync<JsonElement>($"/knowledge/{id}");
        var tatCa = doc.GetProperty("assertion").EnumerateArray().ToList();

        var banA = tatCa.Single(a => a.GetProperty("id").GetGuid() == idA);
        Assert.False(banA.GetProperty("conHieuLuc").GetBoolean());
        Assert.Equal("AiInference", banA.GetProperty("nguonGoc").GetString());
        Assert.Contains("Chọn ký hiệu", banA.GetProperty("noiDung").GetString()!);
        Assert.NotNull(banA.GetProperty("biThayBoi").GetString());

        var banB = tatCa.Single(a =>
            a.GetProperty("kind").GetString() == "Handling" && a.GetProperty("conHieuLuc").GetBoolean());
        Assert.Equal("bản người sửa", banB.GetProperty("noiDung").GetString());
    }

    /// <summary>
    /// 🛑 LÔ BỊ TỪ CHỐI TRỌN VẸN. Mục thứ nhất hợp lệ, mục thứ hai sai — không mục nào
    /// được ghi. Sửa một nửa thì người duyệt không biết phải gửi lại những gì.
    /// </summary>
    [Fact]
    public async Task Lo_sai_mot_muc_thi_KHONG_muc_nao_duoc_ghi()
    {
        var b = MoiBoi();
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var c = app.CreateClient();
        var (id, ds) = await NapAsync(c, b.Nhom);

        var res = await c.PostAsJsonAsync($"/knowledge/{id}/sua", new
        {
            nguoiSua = "chị Lan",
            sua = new object[]
            {
                new { assertionId = IdCua(ds, "Handling"), noiDungMoi = "hợp lệ", mucKiemChung = "Verified" },
                new { assertionId = Guid.CreateVersion7(), noiDungMoi = "trỏ vào assertion không tồn tại", mucKiemChung = "Verified" },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        // và mục HỢP LỆ cũng KHÔNG được ghi
        var doc = await c.GetFromJsonAsync<JsonElement>($"/knowledge/{id}");
        Assert.All(doc.GetProperty("assertion").EnumerateArray(),
            a => Assert.True(a.GetProperty("conHieuLuc").GetBoolean()));
        Assert.All(doc.GetProperty("assertion").EnumerateArray(),
            a => Assert.Equal("AiInference", a.GetProperty("nguonGoc").GetString()));
    }

    /// <summary>Cùng một assertion hai lần trong một lô → diff(A,B) sẽ có hai vế B.</summary>
    [Fact]
    public async Task Cung_assertion_hai_lan_trong_mot_lo_thi_tu_choi()
    {
        var b = MoiBoi();
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var c = app.CreateClient();
        var (id, ds) = await NapAsync(c, b.Nhom);
        var idA = IdCua(ds, "Handling");

        var res = await c.PostAsJsonAsync($"/knowledge/{id}/sua", new
        {
            nguoiSua = "chị Lan",
            sua = new[]
            {
                new { assertionId = idA, noiDungMoi = "lần một", mucKiemChung = "Verified" },
                new { assertionId = idA, noiDungMoi = "lần hai", mucKiemChung = "Verified" },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>`M2` chỉ đọc được khi hai vế khác origin — máy không được tự sửa mình.</summary>
    [Fact]
    public async Task Ban_sua_khai_origin_AiInference_thi_tu_choi()
    {
        var b = MoiBoi();
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var c = app.CreateClient();
        var (id, ds) = await NapAsync(c, b.Nhom);

        var res = await c.PostAsJsonAsync($"/knowledge/{id}/sua", new
        {
            nguoiSua = "máy",
            sua = new[]
            {
                new { assertionId = IdCua(ds, "Handling"), noiDungMoi = "máy tự sửa", mucKiemChung = "Verified", nguonGoc = "AiInference" },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("M2", await res.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// 🛑 SAU KHI SỬA, `/hoi` trả BẢN MỚI và trạng thái `NEEDS_REVIEW` — `V3` gắn cờ chứ
    /// không rút tri thức khỏi đường trả lời. Người đang xử lý ticket vẫn dùng được, và
    /// vẫn thấy ngay là nó đã đổi kể từ lần duyệt cuối.
    /// </summary>
    [Fact]
    public async Task Sau_khi_sua_thi_hoi_ra_ban_moi_va_co_NEEDS_REVIEW()
    {
        var b = MoiBoi();
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var c = app.CreateClient();
        var (id, ds) = await NapAsync(c, b.Nhom);

        await c.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "chị Lan", phamViXem = "noi-bo" });

        await c.PostAsJsonAsync($"/knowledge/{id}/sua", new
        {
            nguoiSua = "chị Lan",
            sua = new[] { new { assertionId = IdCua(ds, "Handling"), noiDungMoi = "cách xử lý ĐÃ SỬA", mucKiemChung = "Verified" } },
        });

        var hoi = await c.PostAsJsonAsync("/hoi", new { maTicket = b.Ticket });
        var kq = await hoi.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(kq.GetProperty("bietTraLoi").GetBoolean());
        var h = kq.GetProperty("huongDan").EnumerateArray().Single();
        Assert.Equal("NEEDS_REVIEW", h.GetProperty("trangThai").GetString());

        var xuLy = h.GetProperty("cachXuLy").EnumerateArray().Single();
        Assert.Equal("cách xử lý ĐÃ SỬA", xuLy.GetProperty("noiDung").GetString());
        Assert.Equal("HumanAssessment", xuLy.GetProperty("nguonGoc").GetString());
    }

    /// <summary>Duyệt lại sau khi sửa thì sạch cờ — đó là vế "số nháp được duyệt" của `M2`.</summary>
    [Fact]
    public async Task Duyet_lai_sau_khi_sua_thi_sach_co()
    {
        var b = MoiBoi();
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var c = app.CreateClient();
        var (id, ds) = await NapAsync(c, b.Nhom);

        await c.PostAsJsonAsync($"/knowledge/{id}/approve", new { nguoiDuyet = "chị Lan", phamViXem = "noi-bo" });
        await c.PostAsJsonAsync($"/knowledge/{id}/sua", new
        {
            nguoiSua = "chị Lan",
            sua = new[] { new { assertionId = IdCua(ds, "Handling"), noiDungMoi = "đã sửa", mucKiemChung = "Verified" } },
        });
        await c.PostAsJsonAsync($"/knowledge/{id}/approve",
            new { nguoiDuyet = "chị Lan", phamViXem = "noi-bo", lyDo = "duyệt lại sau khi sửa" });

        var doc = await c.GetFromJsonAsync<JsonElement>($"/knowledge/{id}");
        Assert.False(doc.GetProperty("canDuyetLai").GetBoolean());
        Assert.Equal("ACTIVE", doc.GetProperty("trangThai").GetString());
    }

    [Fact]
    public async Task Thieu_nguoi_sua_thi_tu_choi()
    {
        var b = MoiBoi();
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var c = app.CreateClient();
        var (id, ds) = await NapAsync(c, b.Nhom);

        var res = await c.PostAsJsonAsync($"/knowledge/{id}/sua", new
        {
            nguoiSua = "",
            sua = new[] { new { assertionId = IdCua(ds, "Handling"), noiDungMoi = "x", mucKiemChung = "Verified" } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Lo_rong_thi_tu_choi()
    {
        var b = MoiBoi();
        using var app = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var c = app.CreateClient();
        var (id, _) = await NapAsync(c, b.Nhom);

        var res = await c.PostAsJsonAsync($"/knowledge/{id}/sua",
            new { nguoiSua = "chị Lan", sua = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    /// <summary>Khách B không sửa được tri thức của khách A, và biểu hiện là "không thấy".</summary>
    [Fact]
    public async Task Khach_B_khong_sua_duoc_tri_thuc_cua_khach_A()
    {
        var b = MoiBoi();
        using var appA = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var (id, ds) = await NapAsync(appA.CreateClient(), b.Nhom);

        using var appB = ApiFactory.Dedicated(db.TenantBKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var res = await appB.CreateClient().PostAsJsonAsync($"/knowledge/{id}/sua", new
        {
            nguoiSua = "người của B",
            sua = new[] { new { assertionId = IdCua(ds, "Handling"), noiDungMoi = "x", mucKiemChung = "Verified" } },
        });

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    /// <summary>Và cũng không ĐỌC được — cùng biểu hiện.</summary>
    [Fact]
    public async Task Khach_B_khong_doc_duoc_tri_thuc_cua_khach_A()
    {
        var b = MoiBoi();
        using var appA = ApiFactory.Dedicated(db.TenantAKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var (id, _) = await NapAsync(appA.CreateClient(), b.Nhom);

        using var appB = ApiFactory.Dedicated(db.TenantBKey).With("DinhTuyen:DuongDanBangTraSan", b.Bang);
        var res = await appB.CreateClient().GetAsync($"/knowledge/{id}");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
