using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Kênh 1 — đường nhận tín hiệu từ phần mềm có sẵn của khách (`06` §1).
///
/// Tín hiệu vào, và **dừng ở ô "Tìm hoặc tạo Case"** của sơ đồ luồng. Các ô sau
/// chưa build, nên phần lớn giá trị của bộ test này không phải "endpoint chạy
/// đúng" mà là ba thứ dễ sai im lặng:
///
///   · tín hiệu đến hai lần KHÔNG sinh case trùng
///   · tín hiệu của khách A KHÔNG ghi vào dữ liệu khách B
///   · lô quá lớn bị TỪ CHỐI, không bị cắt bớt im lặng
/// </summary>
public sealed class CaseSignalTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private const string SignalPath = "/signals/case-observed";
    private const string BoundaryPath = "/internal/tenant-boundary";

    private sealed record BatchResult(int Received, int Created, List<SignalResult> Results);
    private sealed record SignalResult(string SourceReference, Guid CaseId, bool Created);
    private sealed record BoundaryResponse(Guid TenantId, VisibleRows RowsVisibleWithoutTenantFilter);
    private sealed record VisibleRows(int Cases, int Knowledge);

    private static object Signal(string sourceReference, string subject = "Booking OTA không về PMS") =>
        new { sourceReference, subject, sourceCreatedAt = (DateTimeOffset?)null, sourceResolvedAt = (DateTimeOffset?)null };

    private static string Unique(string prefix) => $"{prefix}:{Guid.CreateVersion7()}";

    // =====================================================================
    //  Đường chính
    // =====================================================================

    [Fact]
    public async Task Tin_hieu_tao_case_moi_va_dem_case_tang_dung_bang_so_tin_hieu()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var before = await CountCasesAsync(client);

        var response = await client.PostAsJsonAsync(SignalPath,
            new[] { Signal(Unique("jira")), Signal(Unique("jira")), Signal(Unique("crm")) });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<BatchResult>();

        Assert.Equal(3, body!.Received);
        Assert.Equal(3, body.Created);
        Assert.All(body.Results, r => Assert.True(r.Created));
        Assert.Equal(before + 3, await CountCasesAsync(client));
    }

    /// <summary>
    /// Tín hiệu đến hai lần là chuyện bình thường của mọi hệ thống tích hợp — bên
    /// gửi retry, webhook gửi lại, job đồng bộ chạy lại. Nếu mỗi lần sinh một case
    /// mới thì kho case phình lên bằng dữ liệu trùng, và Path A đi gom về sẽ đếm
    /// một việc thành mười. Không crash, không báo.
    /// </summary>
    [Fact]
    public async Task Gui_lai_cung_mot_tin_hieu_khong_sinh_case_trung()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var sourceReference = Unique("jira");
        var before = await CountCasesAsync(client);

        var first = await PostSignalsAsync(client, Signal(sourceReference));
        var second = await PostSignalsAsync(client, Signal(sourceReference));
        var third = await PostSignalsAsync(client, Signal(sourceReference));

        Assert.True(first.Results[0].Created);
        Assert.False(second.Results[0].Created);
        Assert.False(third.Results[0].Created);

        // Cùng một case, không phải ba case.
        Assert.Equal(first.Results[0].CaseId, second.Results[0].CaseId);
        Assert.Equal(first.Results[0].CaseId, third.Results[0].CaseId);
        Assert.Equal(before + 1, await CountCasesAsync(client));
    }

    /// <summary>
    /// Hai khách hàng dùng Jira riêng, nên khoá <c>jira:ES-1234</c> tồn tại ở cả
    /// hai mà là hai việc khác nhau. Unique index là <c>(TenantId, SourceReference)</c>
    /// — nếu ai đó bỏ <c>TenantId</c> ra khỏi index đó, tín hiệu của khách B sẽ
    /// **trả về case của khách A** thay vì tạo case mới. Đó là rò rỉ dữ liệu qua
    /// một đường không ai nghĩ tới.
    /// </summary>
    [Fact]
    public async Task Hai_khach_hang_dung_cung_mot_sourceReference_khong_dap_len_nhau()
    {
        using var factory = ApiFactory.Shared();
        using var client = factory.CreateClient();

        var shared = "jira:ES-1234";

        var forA = await PostSignalsAsync(client, Signal(shared), apiKey: db.TenantAApiKey);
        var forB = await PostSignalsAsync(client, Signal(shared), apiKey: db.TenantBApiKey);

        Assert.True(forA.Results[0].Created);
        Assert.True(forB.Results[0].Created);
        Assert.NotEqual(forA.Results[0].CaseId, forB.Results[0].CaseId);
    }

    /// <summary>
    /// `G11` ở dạng test: response KHÔNG được có chỗ nào trông như thể các ô sau
    /// của sơ đồ (khớp quy trình, suy ra bước, tra tri thức, trả gợi ý) đã tồn tại.
    /// Một trường <c>suggestions: []</c> sẽ làm bên gọi tưởng đường đó đã có và
    /// chỉ đang rỗng.
    /// </summary>
    [Fact]
    public async Task Response_khong_hua_hen_gi_ve_cac_o_chua_build()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(SignalPath, new[] { Signal(Unique("jira")) });
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var keys = json.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n);

        Assert.Equal(["created", "received", "results"], keys);
    }

    // =====================================================================
    //  Xác thực — endpoint GHI, nên cái chốt nghiêm hơn endpoint đọc
    // =====================================================================

    [Fact]
    public async Task Doi_khoa_ma_khong_gui_khoa_thi_401()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey).RequireApiKey();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(SignalPath, new[] { Signal(Unique("jira")) });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Gui_sai_khoa_thi_401()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey).RequireApiKey();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, SignalPath)
        {
            Content = JsonContent.Create(new[] { Signal(Unique("jira")) }),
        };
        request.Headers.Authorization = ApiDatabaseFixture.Bearer("kp_saitoet_saitoet");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }

    /// <summary>
    /// 🛑 Lỗ mà `AR-e` đóng lại, và nó KHÔNG hiển nhiên: khoá HỢP LỆ của khách B, gửi vào
    /// một bản deploy dedicated của khách A. Không có cổng này thì request chạy dưới tenant
    /// A — tức khoá của B ghi được vào dữ liệu của A.
    ///
    /// ⚠ RLS không cứu được ca này: nó chỉ biết tenant đã resolve, không biết AI đưa ra nó.
    /// Đây đúng chỗ mà "xác thực" và "phân quyền dữ liệu" là hai việc khác nhau.
    /// </summary>
    [Fact]
    public async Task Khoa_HOP_LE_cua_khach_KHAC_gui_vao_ban_dedicated_thi_401()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey).RequireApiKey();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, SignalPath)
        {
            Content = JsonContent.Create(new[] { Signal(Unique("jira")) }),
        };
        request.Headers.Authorization = ApiDatabaseFixture.Bearer(db.TenantBApiKey);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }

    /// <summary>
    /// Thu hồi phải có tác dụng NGAY — đó là thứ `C1-1` chọn phương án này để có. Một
    /// cache khoá ở đâu đó sẽ làm test này đỏ, và đó là chủ đích.
    /// </summary>
    [Fact]
    public async Task Khoa_da_THU_HOI_thi_401()
    {
        var khoaDir = new Tenancy.ApiKeyDirectory(ApiFactory.ConnectionString);
        var khoa = await khoaDir.TaoAsync(db.TenantAId, "test — sẽ bị thu hồi");

        using var factory = ApiFactory.Dedicated(db.TenantAKey).RequireApiKey();
        using var client = factory.CreateClient();

        // Trước khi thu hồi: dùng được.
        using (var truoc = new HttpRequestMessage(HttpMethod.Post, SignalPath)
               {
                   Content = JsonContent.Create(new[] { Signal(Unique("jira")) }),
               })
        {
            truoc.Headers.Authorization = ApiDatabaseFixture.Bearer(khoa);
            (await client.SendAsync(truoc)).EnsureSuccessStatusCode();
        }

        Assert.Equal(1, await khoaDir.ThuHoiAsync(khoa.Split('_')[1]));

        using var sau = new HttpRequestMessage(HttpMethod.Post, SignalPath)
        {
            Content = JsonContent.Create(new[] { Signal(Unique("jira")) }),
        };
        sau.Headers.Authorization = ApiDatabaseFixture.Bearer(khoa);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(sau)).StatusCode);
    }

    [Fact]
    public async Task Gui_dung_khoa_thi_tao_duoc_case()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey).RequireApiKey();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, SignalPath)
        {
            Content = JsonContent.Create(new[] { Signal(Unique("jira")) }),
        };
        request.Headers.Authorization = ApiDatabaseFixture.Bearer(db.TenantAApiKey);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Equal(1, (await response.Content.ReadFromJsonAsync<BatchResult>())!.Created);
    }

    /// <summary>
    /// Xác thực phải chạy TRƯỚC khi tra tenant. Nếu ngược lại, người không có khoá
    /// vẫn phân biệt được 400 ("khoá tenant này không tồn tại") với 401 — tức là dò
    /// được danh sách khách hàng mà không cần khoá nào.
    /// </summary>
    [Fact]
    public async Task Xac_thuc_chay_truoc_khi_tra_tenant()
    {
        using var factory = ApiFactory.Shared();
        using var client = factory.CreateClient();

        // Không khoá nào cả — và ở chế độ shared thì không khoá cũng nghĩa là không tenant.
        var response = await client.PostAsJsonAsync(SignalPath, new[] { Signal(Unique("jira")) });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// ⚠ **Thay cho test cũ**, và nó ghi lại một thay đổi về BẢN CHẤT, không phải đổi thông
    /// điệp lỗi. Bản cũ kiểm rằng ứng dụng TỪ CHỐI KHỞI ĐỘNG khi endpoint ghi không có khoá
    /// và không ai thừa nhận — cái cổng đó cần thiết vì mặc định lúc ấy là **MỞ**: quên cấu
    /// hình là endpoint ghi phơi ra.
    ///
    /// Với `AR-e`, mặc định đã là **ĐÓNG**: không cấu hình gì thì không khoá nào hợp lệ, và
    /// mọi endpoint trả 401. Không còn trạng thái nguy hiểm nào để chặn lúc khởi động, nên
    /// cổng ấy được gỡ — cái phải kiểm bây giờ là chính cái mặc định đóng đó.
    /// </summary>
    [Fact]
    public async Task Khong_cau_hinh_gi_ca_thi_MAC_DINH_LA_DONG_chu_khong_phai_mo()
    {
        using var factory = ApiFactory.WithSettings(new()
        {
            ["Tenancy:Mode"] = "DedicatedSingleTenant",
            ["Tenancy:TenantExternalKey"] = db.TenantAKey,
            // Không Auth:* gì cả — và đó chính là ca đang kiểm.
        });

        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(SignalPath, new[] { Signal(Unique("jira")) });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =====================================================================
    //  Tenant
    // =====================================================================

    [Fact]
    public async Task Che_do_shared_thieu_khoa_thi_401_va_khong_ghi_gi()
    {
        using var factory = ApiFactory.Shared();
        using var client = factory.CreateClient();

        var sourceReference = Unique("jira");
        var response = await client.PostAsJsonAsync(SignalPath, new[] { Signal(sourceReference) });

        // 400 -> 401 khi `AR-e` thành code. Ở chế độ shared, KHÔNG KHOÁ nghĩa là không có
        // đường nào biết request thuộc khách nào — nên nó dừng ở cổng xác thực, sớm hơn.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Và không có case nào được tạo ra ở đâu cả.
        var afterA = await PostSignalsAsync(client, Signal(sourceReference), apiKey: db.TenantAApiKey);
        Assert.True(afterA.Results[0].Created);
    }

    // =====================================================================
    //  Lô lớn và dữ liệu sai — từ chối CẢ LÔ, không xử lý một phần
    // =====================================================================

    /// <summary>
    /// Cắt bớt im lặng là kiểu thất bại tệ nhất ở đường nạp dữ liệu: bên gửi thấy
    /// 200, tưởng đã nạp hết, và phần thiếu chỉ lộ ra nhiều tuần sau khi có người
    /// hỏi "sao thiếu case".
    /// </summary>
    [Fact]
    public async Task Lo_vuot_tran_thi_tu_choi_ca_lo_va_khong_ghi_gi()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey).With("Ingest:MaxSignalsPerRequest", "2");
        using var client = factory.CreateClient();

        var before = await CountCasesAsync(client);

        var response = await client.PostAsJsonAsync(SignalPath,
            new[] { Signal(Unique("jira")), Signal(Unique("jira")), Signal(Unique("jira")) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("trần là 2", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await CountCasesAsync(client));
    }

    [Fact]
    public async Task Mot_tin_hieu_sai_thi_ca_lo_bi_tu_choi_khong_ghi_mot_nua()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var before = await CountCasesAsync(client);

        var response = await client.PostAsJsonAsync(SignalPath, new[]
        {
            Signal(Unique("jira")),
            Signal(Unique("jira"), subject: ""),   // thiếu subject
            Signal(Unique("jira")),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("[1]", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await CountCasesAsync(client));
    }

    [Fact]
    public async Task Lo_rong_thi_400()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(SignalPath, Array.Empty<object>());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =====================================================================

    private static async Task<BatchResult> PostSignalsAsync(
        HttpClient client, object signal, string? apiKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SignalPath)
        {
            Content = JsonContent.Create(new[] { signal }),
        };

        if (apiKey is not null) request.Headers.Authorization = ApiDatabaseFixture.Bearer(apiKey);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<BatchResult>())!;
    }

    private static async Task<int> CountCasesAsync(HttpClient client)
    {
        var body = await client.GetFromJsonAsync<BoundaryResponse>(BoundaryPath);
        return body!.RowsVisibleWithoutTenantFilter.Cases;
    }
}
