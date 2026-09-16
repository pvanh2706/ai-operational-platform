using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Domain.Redaction;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// `AR-n` nhánh B2a — dữ liệu BÊN THỨ BA bị che **trước khi chạm database**, qua HTTP thật.
///
/// 🛑 **MỌI GIÁ TRỊ TRONG FILE NÀY LÀ GIẢ.** Hình dạng lấy từ payload thật
/// (`ES-346481#comment-802977`), nội dung thì bịa — xem `scripts/quet_credential_repo.py` để
/// biết vì sao dòng này phải có ở đây.
/// </summary>
public sealed class DuLieuBenThuBaTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private const string NapEvidence = "/signals/case-evidence";

    private const string Payload = """
        Khach gui payload NCC tra ve loi:
        <NDHDon>
          <NMua>
            <Ten>Cong ty TNHH Mot Thanh Vien ABC</Ten>
            <MST>0100000001</MST>
            <DChi>12 Duong Khong Co That, Phuong Ao</DChi>
            <STKNHang />
          </NMua>
          <DSHHDVu>
            <HHDVu><DGia>1500000</DGia><ThTien>3000000</ThTien><TSuat>8%</TSuat></HHDVu>
          </DSHHDVu>
          <TToan><TgTTTBSo>3240000</TgTTTBSo></TToan>
        </NDHDon>
        Ma loi: E0042
        """;

    private static string Khoa(string p) => $"{p}:{Guid.CreateVersion7():N}";

    private static async Task<string> DungCaseAsync(HttpClient c)
    {
        var khoa = Khoa("jira:ES");
        (await c.PostAsJsonAsync("/signals/case-observed", new[]
        {
            new { sourceReference = khoa, subject = "NCC tu choi payload" },
        })).EnsureSuccessStatusCode();
        return khoa;
    }

    private static object Mau(string khoaCase, string khoaMau, string noiDung) => new
    {
        caseSourceReference = khoaCase,
        sourceReference = khoaMau,
        content = noiDung,
        observedAt = (DateTimeOffset?)null,
        machineReadability = "High",
        subTenantCode = "khong-xac-dinh",
    };

    /// <summary>
    /// 🎯 Vòng đầy đủ: nạp một payload có khối bên mua → định danh KHÔNG vào được database,
    /// và response nói ra đã che mấy chỗ.
    /// </summary>
    [Fact]
    public async Task Dinh_danh_ben_thu_ba_KHONG_cham_duoc_database()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var khoaMau = khoaCase + "#comment-1";

        var res = await c.PostAsJsonAsync(NapEvidence, new[] { Mau(khoaCase, khoaMau, Payload) });
        res.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(3, json.GetProperty("thirdPartyRedactedTotal").GetInt32());
        Assert.Equal(3, json.GetProperty("results")[0].GetProperty("thirdPartyRedacted").GetInt32());

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.EvidenceItems.SingleAsync(e => e.SourceReference == khoaMau);

        Assert.DoesNotContain("Cong ty TNHH Mot Thanh Vien ABC", luu.Content);
        Assert.DoesNotContain("0100000001", luu.Content);
        Assert.DoesNotContain("Duong Khong Co That", luu.Content);
        Assert.Contains(ThirdPartyRedactor.Marker, luu.Content);
    }

    /// <summary>
    /// 🛑 **PHÉP THỬ ĐẮT NHẤT CỦA NHÁNH NÀY**, và nó bảo vệ một thứ dễ bị "sửa cho an toàn":
    /// số tiền và mã lỗi PHẢI còn.
    ///
    /// Nhóm SOP dày nhất trong 19 nhóm là *"Lệch cấu hình tiền/thuế"* (8 case, 63 mẩu) và nó
    /// chẩn đoán bằng cách so từng cột tiền của cùng một hoá đơn ở ba nơi. Ai đó thấy payload
    /// hoá đơn rồi che cả khối cho chắc sẽ vứt đúng bằng chứng của nhóm dày nhất — lần thứ BA
    /// một bộ lọc nghe rất hợp lý vứt đúng phần có giá trị.
    /// </summary>
    [Fact]
    public async Task So_tien_va_ma_loi_VAN_CON_sau_khi_che()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);
        var khoaMau = khoaCase + "#comment-1";

        (await c.PostAsJsonAsync(NapEvidence, new[] { Mau(khoaCase, khoaMau, Payload) }))
            .EnsureSuccessStatusCode();

        await using var ctx = db.OpenContext(db.TenantAId);
        var luu = await ctx.EvidenceItems.SingleAsync(e => e.SourceReference == khoaMau);

        Assert.Contains("<DGia>1500000</DGia>", luu.Content);
        Assert.Contains("<TgTTTBSo>3240000</TgTTTBSo>", luu.Content);
        Assert.Contains("E0042", luu.Content);

        // Thẻ rỗng để yên: "không có" phải phân biệt được với "có nhưng bị giấu" — chính sự
        // rỗng đó là thứ nhóm 2 cần để biết NCC từ chối vì trường nào THIẾU.
        Assert.Contains("<STKNHang />", luu.Content);
    }

    /// <summary>Dấu che đi ra được qua đường đọc — người duyệt phải thấy chỗ nào đã bị che.</summary>
    [Fact]
    public async Task Duong_doc_hien_ra_dau_che_chu_khong_giau()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);

        (await c.PostAsJsonAsync(NapEvidence,
            new[] { Mau(khoaCase, khoaCase + "#comment-1", Payload) })).EnsureSuccessStatusCode();

        var doc = await c.GetAsync($"/cases/{Uri.EscapeDataString(khoaCase)}/evidence");
        var raw = await doc.Content.ReadAsStringAsync();

        Assert.Contains(ThirdPartyRedactor.Marker, raw);
        Assert.DoesNotContain("Cong ty TNHH Mot Thanh Vien ABC", raw);
    }

    /// <summary>
    /// Mẩu thường thì con số là 0 — và <b>0 KHÔNG nghĩa là "mẩu này sạch"</b>. Trên 873 mẩu
    /// thật, khối <c>&lt;NMua&gt;</c> xuất hiện ở ĐÚNG MỘT mẩu, nên độ phủ của luật chưa đo
    /// được. 0 chỉ nghĩa là luật không thấy gì.
    /// </summary>
    [Fact]
    public async Task Mau_khong_co_khoi_ben_mua_thi_dem_ra_0()
    {
        using var app = ApiFactory.Dedicated(db.TenantAKey);
        using var c = app.CreateClient();
        var khoaCase = await DungCaseAsync(c);

        var res = await c.PostAsJsonAsync(NapEvidence, new[]
        {
            Mau(khoaCase, khoaCase + "#comment-2", "Da kiem Nhat ky ket noi, ma loi E0042"),
        });
        res.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(0, json.GetProperty("thirdPartyRedactedTotal").GetInt32());
    }
}
