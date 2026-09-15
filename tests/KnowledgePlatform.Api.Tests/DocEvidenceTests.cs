using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// `AR-g` — đường ĐỌC evidence của một case, chốt 2026-09-15 (phiếu `docs/14` §C4).
///
/// Trước đường này hệ thống chỉ có đường GHI, và chính bộ test phải mở thẳng `AppDbContext`
/// để kiểm những hành vi âm thầm. Một cơ chế chỉ kiểm được bằng cách đi vòng qua sản phẩm
/// là một cơ chế chưa ai dùng thử.
/// </summary>
public sealed class DocEvidenceTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private static ApiFactory App(string tenantKey) => ApiFactory.Dedicated(tenantKey);

    /// <summary>Tạo một case kèm evidence qua đúng hai endpoint NẠP của sản phẩm.</summary>
    private static async Task<string> DungCaseAsync(HttpClient c, string? khoaTuChon = null)
    {
        var khoa = khoaTuChon ?? $"jira:T-{Guid.CreateVersion7():N}";

        var r1 = await c.PostAsJsonAsync("/signals/case-observed", new[]
        {
            new { sourceReference = khoa, subject = "Không phát hành được hoá đơn" },
        });
        r1.EnsureSuccessStatusCode();

        var r2 = await c.PostAsJsonAsync("/signals/case-evidence", new object[]
        {
            new
            {
                caseSourceReference = khoa,
                sourceReference = khoa + "#description",
                content = "Khách báo ô ký hiệu trống khi phát hành.",
                observedAt = "2026-09-01T08:00:00Z",
                machineReadability = "Medium",
                subTenantCode = "17468",
            },
            new
            {
                caseSourceReference = khoa,
                sourceReference = khoa + "#comment-1",
                content = "{ \"code\": \"E0042\", \"message\": \"ky hieu khong ton tai\" }",
                observedAt = "2026-09-01T09:30:00Z",
                machineReadability = "High",
                subTenantCode = "12027",
            },
        });
        r2.EnsureSuccessStatusCode();

        return khoa;
    }

    [Fact]
    public async Task Doc_duoc_evidence_cua_mot_case()
    {
        using var app = App(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = await DungCaseAsync(c);

        var res = await c.GetAsync($"/cases/{Uri.EscapeDataString(khoa)}/evidence");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(khoa, kq.GetProperty("khoaNguon").GetString());
        Assert.Equal(2, kq.GetProperty("soMau").GetInt32());
        Assert.Equal(2, kq.GetProperty("mau").GetArrayLength());
    }

    /// <summary>
    /// Xếp theo thời điểm QUAN SÁT, không theo thời điểm nạp. Đọc một transcript mà thứ tự
    /// đảo lộn thì không lần lại được ai làm gì trước.
    /// </summary>
    [Fact]
    public async Task Evidence_xep_theo_thoi_diem_quan_sat()
    {
        using var app = App(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = await DungCaseAsync(c);

        var kq = await c.GetFromJsonAsync<JsonElement>($"/cases/{Uri.EscapeDataString(khoa)}/evidence");
        var moc = kq.GetProperty("mau").EnumerateArray()
            .Select(m => m.GetProperty("quanSatLuc").GetDateTimeOffset()).ToList();

        Assert.Equal(moc.OrderBy(x => x), moc);
    }

    /// <summary>
    /// 🛑 Nhãn `MachineReadability` ĐÃ LƯU phải đọc lại được — đây chính là trường mà `AR-k`
    /// đo được là hằng số `High` ở 128/128 mẩu. Không có đường đọc thì một lỗi kiểu đó chỉ
    /// lộ ra khi có người mở database lên xem.
    /// </summary>
    [Fact]
    public async Task Nhan_may_doc_duoc_hien_ra_dung_nhu_da_luu()
    {
        using var app = App(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = await DungCaseAsync(c);

        var kq = await c.GetFromJsonAsync<JsonElement>($"/cases/{Uri.EscapeDataString(khoa)}/evidence");
        var nhan = kq.GetProperty("mau").EnumerateArray()
            .Select(m => m.GetProperty("mayDocDuoc").GetString()).ToList();

        // Hai mẩu khai hai nhãn KHÁC NHAU, và phải đọc lại ra đúng hai nhãn đó — nếu một
        // ngày nào đó cả hai cùng ra `High` thì đúng hình dạng lỗi của `AR-k` đã quay lại.
        Assert.Contains("Medium", nhan);
        Assert.Contains("High", nhan);
        Assert.Equal(2, nhan.Distinct().Count());
    }

    [Fact]
    public async Task Case_khong_ton_tai_thi_404()
    {
        using var app = App(db.TenantAKey);

        var res = await app.CreateClient().GetAsync("/cases/jira%3AKHONG-CO/evidence");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    /// <summary>Case của khách A không đọc được từ request của khách B — biểu hiện "không thấy".</summary>
    [Fact]
    public async Task Khach_B_khong_doc_duoc_evidence_cua_khach_A()
    {
        using var appA = App(db.TenantAKey);
        var khoa = await DungCaseAsync(appA.CreateClient());

        using var appB = App(db.TenantBKey);
        var res = await appB.CreateClient().GetAsync($"/cases/{Uri.EscapeDataString(khoa)}/evidence");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    /// <summary>
    /// 🛑 GHIM MỘT GIỚI HẠN ĐÃ ĐO CỦA HÌNH DẠNG ENDPOINT NÀY — `AR-g` chốt ngày 2026-09-15
    /// chọn dạng đường dẫn, và lúc chốt chưa ai biết điều dưới đây.
    ///
    /// `G1` nói khoá nguồn là CHUỖI TỰ DO, và README nêu thẳng ví dụ `crm:deal/4471`. Nhưng
    /// `{khoaNguon}` là MỘT đoạn đường dẫn, và **ASP.NET giải mã `%2F` thành `/` TRƯỚC khi
    /// định tuyến** — nên khoá có dấu gạch chéo **không đọc ra được, kể cả khi mã hoá đúng**.
    ///
    /// ⚠ Đây KHÔNG phải test mô tả hành vi mong muốn. Nó ghim hành vi THẬT, đo được, để:
    ///   1  bên tích hợp không phải phát hiện bằng một cái 404 không giải thích nổi;
    ///   2  khi có người sửa (thêm dạng query, hoặc đổi sang route catch-all ở cuối) thì
    ///      test này ĐỎ và buộc phải cập nhật hợp đồng — thay vì lặng lẽ đổi hành vi.
    ///
    /// Khoá dạng `jira:ES-123` (áp đảo về số lượng) KHÔNG dính giới hạn này — chỉ dấu `/` thôi.
    /// </summary>
    [Fact]
    public async Task Khoa_nguon_co_dau_gach_cheo_HIEN_CHUA_doc_ra_duoc()
    {
        using var app = App(db.TenantAKey);
        var c = app.CreateClient();
        var khoa = await DungCaseAsync(c, $"crm:deal/{Guid.CreateVersion7():N}");

        var maHoa = await c.GetAsync($"/cases/{Uri.EscapeDataString(khoa)}/evidence");
        var thoTuc = await c.GetAsync($"/cases/{khoa}/evidence");

        // Cả hai cách đều KHÔNG tới được — %2F bị giải mã trước khi khớp route.
        Assert.Equal(HttpStatusCode.NotFound, maHoa.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, thoTuc.StatusCode);

        // Nhưng case thì CÓ THẬT trong kho — chứng minh lỗi nằm ở hình dạng URL, không
        // phải ở đường ghi hay ở dữ liệu. Không có vế này thì ca trên đọc ra thành
        // "chắc case chưa được tạo", và người sửa sẽ đi tìm sai chỗ.
        var lai = await c.PostAsJsonAsync("/signals/case-observed", new[]
        {
            new { sourceReference = khoa, subject = "gửi lại để chứng minh case đã tồn tại" },
        });
        var kq = await lai.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, kq.GetProperty("created").GetInt32());
    }
}
