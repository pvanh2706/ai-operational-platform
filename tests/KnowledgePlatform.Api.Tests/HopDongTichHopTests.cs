using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Startup;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Hợp đồng tích hợp sinh từ code (`H4-1`, bước 7).
///
/// 🛑 Test nặng nhất là <c>Them_endpoint_ma_quen_mo_ta_thi_bi_bat</c>: nó giữ **cơ chế** chứ
/// không giữ nội dung. `IM-38` đã đo được cái giá của việc không có nó — bộ Postman đứng ở
/// 5/13 endpoint suốt hơn hai tuần mà không ai nhận ra, vì không có gì đối chiếu nó với
/// thực tế.
/// </summary>
public sealed class HopDongTichHopTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    // =========================================================================
    //  Cơ chế chống lỗi thời — thuần tính toán, không cần host
    // =========================================================================

    /// <summary>
    /// 🛑 Thêm endpoint mà quên viết mô tả thì bị bắt. Đây là thứ giữ cho hợp đồng không tự
    /// mục đi — và nó phải ĐỎ được, nếu không thì nó chỉ là một dòng chữ.
    /// </summary>
    [Fact]
    public void Them_endpoint_ma_quen_mo_ta_thi_bi_bat()
    {
        var thieu = OpenApiMoTa.ThieuMoTa(
        [
            ("GET", "/health"),                       // có mô tả
            ("POST", "/mot-endpoint-vua-them"),        // CHƯA có
        ]);

        Assert.Equal(["POST /mot-endpoint-vua-them"], thieu);
    }

    /// <summary>
    /// 🛑 **Cổng canh phải TỰ TỐ CÁO khi nó bị ngắt.** Bản đầu đọc danh sách route từ DI, lúc
    /// đó còn RỖNG — nó duyệt qua không có gì, không bao giờ ném, và trông hệt như đang canh.
    /// Test cũ vẫn xanh vì nó gọi thẳng `ThieuMoTa`, không đi qua đường nối.
    ///
    /// Chiều `MoTaThua` biến ca đó thành ồn ào: route rỗng -> mọi mô tả đều thừa -> ném.
    /// </summary>
    [Fact]
    public void Danh_sach_route_RONG_lam_MOI_mo_ta_thanh_thua()
    {
        var thua = OpenApiMoTa.MoTaThua([]);

        Assert.Equal(OpenApiMoTa.Bang.Count, thua.Count);
        Assert.NotEmpty(thua);
    }

    [Fact]
    public void Mo_ta_cho_endpoint_da_bi_xoa_thi_bi_bat()
    {
        // Mọi route thật, TRỪ một cái -> mô tả của cái đó thành thừa.
        var tatCa = OpenApiMoTa.Bang.Keys
            .Select(k => (Method: k.Split(' ')[0], Route: k.Split(' ')[1]))
            .Where(r => $"{r.Method} {r.Route}" != "POST /hoi");

        Assert.Equal(["POST /hoi"], OpenApiMoTa.MoTaThua(tatCa));
    }

    [Fact]
    public void Moi_endpoint_dang_co_deu_da_co_mo_ta()
    {
        // Danh sách route thật được đối chiếu lúc KHỞI ĐỘNG (StartupChecks.RequireMoTaEndpoint).
        // Ở đây chỉ khẳng định bảng không rỗng và không có dòng nào bỏ trống chữ.
        Assert.NotEmpty(OpenApiMoTa.Bang);

        Assert.All(OpenApiMoTa.Bang, kv =>
        {
            Assert.False(string.IsNullOrWhiteSpace(kv.Value.TomTat), $"{kv.Key} thiếu tóm tắt.");
            Assert.False(string.IsNullOrWhiteSpace(kv.Value.MoTa), $"{kv.Key} thiếu mô tả.");
            Assert.False(string.IsNullOrWhiteSpace(kv.Value.Nhom), $"{kv.Key} thiếu nhóm.");
        });
    }

    /// <summary>
    /// Bảng viết theo cách bên tích hợp ĐỌC đường dẫn (`{id}`), không theo cách ASP.NET khai
    /// báo nó (`{id:guid}`). Phép quy đổi giữa hai dạng phải đúng, nếu không cổng canh sẽ báo
    /// thiếu mô tả cho những endpoint đã có — một cổng kêu oan thì sẽ bị gỡ.
    /// </summary>
    [Theory]
    [InlineData("/knowledge/{id:guid}", "/knowledge/{id}")]
    [InlineData("/knowledge/{id:guid}/approve", "/knowledge/{id}/approve")]
    [InlineData("/cases/{khoaNguon}/evidence", "/cases/{khoaNguon}/evidence")]
    [InlineData("/health", "/health")]
    public void Bo_rang_buoc_kieu_khoi_route(string vao, string ra)
    {
        Assert.Equal(ra, OpenApiMoTa.BoRangBuocKieu(vao));
    }

    // =========================================================================
    //  Soi cấu hình (bước 6) — hai hàm nhỏ mà hỏng thì hỏng rất nặng
    // =========================================================================

    /// <summary>
    /// 🛑 Lệnh `--kiem-cau-hinh` in ra màn hình, và người ta SẼ dán kết quả vào chat để hỏi.
    /// Mật khẩu database lọt ra theo đường đó là đúng sự cố `0e602c3` lặp lại — lần ấy phải
    /// gỡ credential thật khỏi 5 file đang được git theo dõi.
    /// </summary>
    [Theory]
    [InlineData("Host=localhost;Database=kp;Username=kp_app;Password=bimatthat")]
    [InlineData("Host=localhost;Database=kp;password=bimatthat;Username=kp_app")]
    public void Chuoi_ket_noi_in_ra_KHONG_mang_mat_khau(string conn)
    {
        var che = SoiCauHinh.CheMatKhau(conn);

        Assert.DoesNotContain("bimatthat", che);
        Assert.Contains("Password=***", che);

        // Phần còn lại phải NGUYÊN VẸN — che cả chuỗi thì lệnh mất hết giá trị chẩn đoán.
        Assert.Contains("Database=kp", che);
    }

    /// <summary>Chuỗi chỉ có mỗi mật khẩu cũng phải được che — không có gì khác để bấu víu.</summary>
    [Fact]
    public void Chuoi_chi_co_mat_khau_van_bi_che()
    {
        Assert.Equal("Password=***", SoiCauHinh.CheMatKhau("Password=bimatthat"));
    }

    /// <summary>
    /// Nhận ra địa chỉ trỏ vào chính máy này. Sai ở đây nghĩa là cảnh báo không bật, và cái
    /// bẫy *"chạy đúng với đúng người đi kiểm nó"* đi thẳng vào bản deploy.
    /// </summary>
    [Theory]
    [InlineData("http://localhost:5119", true)]
    [InlineData("http://127.0.0.1:5119", true)]
    [InlineData("http://[::1]:5119", true)]
    [InlineData("http://LOCALHOST:5119", true)]
    [InlineData("http://kp-noi-bo.ezcloud:5119", false)]
    [InlineData("https://kp.ezcloud.vn", false)]
    public void Nhan_ra_dia_chi_tro_vao_chinh_may_nay(string url, bool laLoopback)
    {
        Assert.Equal(laLoopback, SoiCauHinh.LaLoopback(url));
    }

    // =========================================================================
    //  Tài liệu thật, qua host thật
    // =========================================================================

    [Fact]
    public async Task Tai_lieu_mo_duoc_KHONG_CAN_khoa_API()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey).RequireApiKey();

        // Không mang khoá nào. Tài liệu tả HÌNH DẠNG hợp đồng, không chứa dữ liệu khách hàng —
        // và mọi endpoint nó liệt kê vẫn trả 401 nếu gọi không khoá.
        var res = await app.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Moi_thao_tac_trong_tai_lieu_deu_co_tom_tat_va_nhom()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);

        var doc = await app.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var thieu = new List<string>();
        var so = 0;

        foreach (var duong in doc.GetProperty("paths").EnumerateObject())
        {
            foreach (var kieu in duong.Value.EnumerateObject())
            {
                so++;
                var op = kieu.Value;

                var coTomTat = op.TryGetProperty("summary", out var s)
                               && !string.IsNullOrWhiteSpace(s.GetString());
                var coNhom = op.TryGetProperty("tags", out var t) && t.GetArrayLength() > 0;

                if (!coTomTat || !coNhom) thieu.Add($"{kieu.Name.ToUpperInvariant()} {duong.Name}");
            }
        }

        Assert.True(so >= 15, $"Tài liệu chỉ có {so} thao tác — ít hơn số endpoint đang có.");
        Assert.Empty(thieu);
    }

    /// <summary>
    /// Phần đầu tài liệu phải nói ra HAI thứ nó không tả được. Người đọc hợp đồng mà không
    /// biết có một đường webhook gửi ra thì sẽ viết thiếu hẳn một nửa tích hợp.
    /// </summary>
    [Fact]
    public async Task Phan_dau_tai_lieu_noi_ro_cho_nao_KHONG_tao_duoc()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);

        var doc = await app.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var moTa = doc.GetProperty("info").GetProperty("description").GetString()!;

        Assert.Contains("webhook", moTa, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("19_HOP_DONG_TICH_HOP", moTa);
        Assert.Contains("Bearer", moTa);
    }
}
