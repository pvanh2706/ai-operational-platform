using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Domain.Evidence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Kênh 1, cửa thứ hai — nạp NỘI DUNG của case (`AR-f`, chốt 2026-08-30).
///
/// Trước đường này một <c>canonical_case</c> là MỘT DÒNG CHỮ, và Path A không có gì
/// để gom. Nên phần lớn giá trị của bộ test này không phải "endpoint chạy đúng" mà là
/// bốn thứ dễ sai im lặng:
///
///   · cùng một comment gửi hai lần KHÔNG sinh hai dòng — `S8` đếm 14/20 thì con số
///     đó phải là 14 thật, không phải 14 vì có mẩu bị nhân đôi
///   · evidence KHÔNG gắn vào case của khách hàng khác được, kể cả khi biết khoá
///   · lô nhắc tới một case không tồn tại bị từ chối TRỌN VẸN, không ghi một nửa
///   · nhãn <c>machineReadability</c> sai chính tả KHÔNG âm thầm về <c>Unknown</c>
/// </summary>
public sealed class CaseEvidenceTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private const string SignalPath = "/signals/case-observed";
    private const string EvidencePath = "/signals/case-evidence";
    private const string BoundaryPath = "/internal/tenant-boundary";

    private sealed record EvidenceBatchResult(int Received, int Created, List<EvidenceResult> Results);
    private sealed record EvidenceResult(string SourceReference, Guid EvidenceId, Guid? CaseId, bool Created);
    private sealed record BoundaryResponse(VisibleRows RowsVisibleWithoutTenantFilter);
    private sealed record VisibleRows(int Cases, int Evidence, int Knowledge);

    private static string Unique(string prefix) => $"{prefix}:{Guid.CreateVersion7()}";

    private static object Evidence(
        string sourceReference,
        string? caseSourceReference = null,
        string content = "Da kiem room mapping, khong thay lech",
        string? machineReadability = null) =>
        new
        {
            caseSourceReference,
            sourceReference,
            content,
            observedAt = (DateTimeOffset?)null,
            machineReadability,
        };

    // =====================================================================
    //  Đường chính
    // =====================================================================

    [Fact]
    public async Task Evidence_gan_vao_case_va_dem_evidence_tang_dung_bang_so_mau()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var caseReference = await NewCaseAsync(client);
        var before = await CountEvidenceAsync(client);

        var body = await PostEvidenceAsync(client,
            Evidence(Unique("jira"), caseReference),
            Evidence(Unique("jira"), caseReference),
            Evidence(Unique("jira"), caseReference));

        Assert.Equal(3, body.Received);
        Assert.Equal(3, body.Created);
        Assert.All(body.Results, r => Assert.True(r.Created));
        Assert.All(body.Results, r => Assert.NotNull(r.CaseId));

        // Cả ba trỏ về CÙNG một case — không phải mỗi mẩu tự tạo một case riêng.
        Assert.Single(body.Results.Select(r => r.CaseId).Distinct());
        Assert.Equal(before + 3, await CountEvidenceAsync(client));
    }

    /// <summary>
    /// Vì sao đây là phép thử quan trọng nhất của bộ này: `S8` nói giá trị của bản
    /// nháp gom nằm ở PHÂN BỐ — *"bước kiểm room mapping: 14/20 case đã làm"*. Một
    /// comment bị nhân đôi làm sai đúng con số đó, và sai theo hướng không ai nhìn
    /// ra: bản nháp vẫn đọc trôi chảy, chỉ có tỉ lệ là bịa.
    /// </summary>
    [Fact]
    public async Task Gui_lai_cung_mot_evidence_khong_sinh_ban_trung()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var caseReference = await NewCaseAsync(client);
        var evidenceReference = Unique("jira-comment");
        var before = await CountEvidenceAsync(client);

        var first = await PostEvidenceAsync(client, Evidence(evidenceReference, caseReference));
        var second = await PostEvidenceAsync(client, Evidence(evidenceReference, caseReference));
        var third = await PostEvidenceAsync(client, Evidence(evidenceReference, caseReference));

        Assert.True(first.Results[0].Created);
        Assert.False(second.Results[0].Created);
        Assert.False(third.Results[0].Created);

        Assert.Equal(first.Results[0].EvidenceId, second.Results[0].EvidenceId);
        Assert.Equal(first.Results[0].EvidenceId, third.Results[0].EvidenceId);
        Assert.Equal(before + 1, await CountEvidenceAsync(client));
    }

    /// <summary>
    /// `K-B9` ở dạng test. Một email của senior hay một tin Zalo không thuộc case nào,
    /// và với thực tế 60% fragment rải rác thì đó không phải trường hợp hiếm.
    ///
    /// ⚠ Nếu test này đỏ vì ai đó làm <c>caseSourceReference</c> thành bắt buộc, thì
    /// cái mất KHÔNG phải một tiện ích — mà là toàn bộ đường vào của 60%.
    /// </summary>
    [Fact]
    public async Task Evidence_khong_thuoc_case_nao_van_nap_duoc()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var before = await CountEvidenceAsync(client);

        var body = await PostEvidenceAsync(client,
            Evidence(Unique("email"), caseSourceReference: null,
                content: "Senior noi: khong co incoming log thi loi phia OTA"));

        Assert.Equal(1, body.Created);
        Assert.Null(body.Results[0].CaseId);
        Assert.Equal(before + 1, await CountEvidenceAsync(client));
    }

    /// <summary>
    /// `G11` ở dạng test — giống hệt phép thử cùng tên của Kênh 1. Response KHÔNG
    /// được có trường nào trông như thể các ô sau đã tồn tại. `S6` nói rõ: nạp nội
    /// dung KHÔNG tự sinh tri thức, nên một trường <c>assertions: []</c> hay
    /// <c>knowledgeRecords: []</c> ở đây sẽ là lời hứa suông.
    /// </summary>
    [Fact]
    public async Task Response_khong_hua_hen_gi_ve_cac_o_chua_build()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(EvidencePath, new[] { Evidence(Unique("jira-comment")) });
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var keys = json.RootElement.EnumerateObject().Select(p => p.Name).OrderBy(n => n);

        Assert.Equal(["created", "received", "results"], keys);
    }

    // =====================================================================
    //  Ranh giới khách hàng
    // =====================================================================

    /// <summary>
    /// Khoá nguồn của khách hàng khác KHÔNG được dùng để gắn evidence, kể cả khi
    /// người gọi biết chính xác khoá đó.
    ///
    /// Và cách nó từ chối cũng quan trọng: câu trả lời là *"không có Case nào mang
    /// khoá này"* — **y hệt** câu trả lời khi khoá đó không tồn tại ở đâu cả. Hai
    /// câu trả lời khác nhau ở hai tình huống này là một kênh dò: gọi thử một loạt
    /// khoá là biết được khách hàng khác đang có những case nào.
    /// </summary>
    [Fact]
    public async Task Khong_gan_duoc_evidence_vao_case_cua_khach_hang_khac()
    {
        using var factory = ApiFactory.Shared();
        using var client = factory.CreateClient();

        // Case này thuộc khách hàng B.
        var caseOfB = await NewCaseAsync(client, db.TenantBKey);

        var evidenceOfA = await CountEvidenceAsync(client, db.TenantAKey);
        var evidenceOfB = await CountEvidenceAsync(client, db.TenantBKey);

        // Khách hàng A biết khoá đó và thử gắn vào.
        var response = await PostEvidenceRawAsync(client,
            [Evidence(Unique("jira-comment"), caseOfB)], db.TenantAKey);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("không tồn tại", await response.Content.ReadAsStringAsync());

        // Không ghi gì, cho cả hai bên.
        Assert.Equal(evidenceOfA, await CountEvidenceAsync(client, db.TenantAKey));
        Assert.Equal(evidenceOfB, await CountEvidenceAsync(client, db.TenantBKey));
    }

    [Fact]
    public async Task Hai_khach_hang_dung_cung_mot_sourceReference_khong_dap_len_nhau()
    {
        using var factory = ApiFactory.Shared();
        using var client = factory.CreateClient();

        var shared = "jira:ES-1234#comment-7";

        var forA = await PostEvidenceAsync(client, [Evidence(shared)], db.TenantAKey);
        var forB = await PostEvidenceAsync(client, [Evidence(shared)], db.TenantBKey);

        Assert.True(forA.Results[0].Created);
        Assert.True(forB.Results[0].Created);
        Assert.NotEqual(forA.Results[0].EvidenceId, forB.Results[0].EvidenceId);
    }

    // =====================================================================
    //  Ca lỗi — từ chối CẢ LÔ, không xử lý một phần
    // =====================================================================

    [Fact]
    public async Task Case_duoc_tham_chieu_khong_ton_tai_thi_tu_choi_ca_lo_khong_ghi_mot_nua()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var caseReference = await NewCaseAsync(client);
        var before = await CountEvidenceAsync(client);

        var response = await PostEvidenceRawAsync(client,
        [
            Evidence(Unique("jira-comment"), caseReference),          // [0] hợp lệ
            Evidence(Unique("jira-comment"), "jira:KHONG-CO-THAT"),   // [1] case không có
            Evidence(Unique("jira-comment"), caseReference),          // [2] hợp lệ
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("[1]", payload);
        Assert.Contains("jira:KHONG-CO-THAT", payload);

        // [0] và [2] KHÔNG được tạo.
        Assert.Equal(before, await CountEvidenceAsync(client));
    }

    [Fact]
    public async Task Thieu_content_thi_ca_lo_bi_tu_choi_khong_ghi_mot_nua()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var before = await CountEvidenceAsync(client);

        var response = await PostEvidenceRawAsync(client,
        [
            Evidence(Unique("jira-comment")),
            Evidence(Unique("jira-comment"), content: ""),
            Evidence(Unique("jira-comment")),
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("[1]", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await CountEvidenceAsync(client));
    }

    /// <summary>
    /// Nội dung toàn dấu cách cũng là rỗng. Nhận nó vào là để một dòng rỗng nghĩa
    /// nằm trong kho gom, rồi Path A sẽ đưa chính dòng đó cho model như một quan sát
    /// thật — rác trong kho gom nguy hiểm hơn rác ở một tiêu đề.
    /// </summary>
    [Fact]
    public async Task Content_toan_dau_cach_cung_bi_tu_choi()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var response = await PostEvidenceRawAsync(client, [Evidence(Unique("jira-comment"), content: "   ")]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("content", await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Nhãn sai chính tả phải là 400, KHÔNG được âm thầm về <c>Unknown</c>.
    ///
    /// Âm thầm về Unknown nghe như "chấp nhận rộng rãi", nhưng nó biến một lỗi cấu
    /// hình connector thành dữ liệu sai vĩnh viễn: cả kho evidence dán nhãn Unknown
    /// trong khi bên gửi tưởng đã khai High. Không ai phát hiện cho tới lúc có người
    /// hỏi vì sao §6.3 không phân biệt được ba trạng thái coverage.
    /// </summary>
    [Fact]
    public async Task machineReadability_sai_chinh_ta_thi_400_khong_am_tham_ve_Unknown()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var before = await CountEvidenceAsync(client);

        var response = await PostEvidenceRawAsync(client,
            [Evidence(Unique("jira-comment"), machineReadability: "HIGHT")]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("HIGHT", payload);
        Assert.Contains("High", payload);      // nói luôn giá trị nhận được là gì
        Assert.Equal(before, await CountEvidenceAsync(client));
    }

    [Fact]
    public async Task machineReadability_bo_trong_thi_luu_la_Unknown()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var reference = Unique("jira-comment");
        await PostEvidenceAsync(client, Evidence(reference, machineReadability: null));

        await using var context = db.OpenContext(db.TenantAId);
        var stored = await context.EvidenceItems.SingleAsync(e => e.SourceReference == reference);

        Assert.Equal(MachineReadability.Unknown, stored.MachineReadability);
    }

    [Fact]
    public async Task machineReadability_khai_dung_thi_luu_dung()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var reference = Unique("kibana");
        await PostEvidenceAsync(client, Evidence(reference, machineReadability: "High"));

        await using var context = db.OpenContext(db.TenantAId);
        var stored = await context.EvidenceItems.SingleAsync(e => e.SourceReference == reference);

        Assert.Equal(MachineReadability.High, stored.MachineReadability);
    }

    /// <summary>
    /// `K-B3`: evidence gắn với MỘT thời điểm và MỘT nguồn. Gửi lại cùng khoá với nội
    /// dung khác thì bản đã lưu KHÔNG đổi — ghi đè lặng lẽ là sửa lại quá khứ, và nó
    /// kéo theo mọi assertion đang dẫn chứng bằng mẩu này mà không cảnh báo ai.
    /// </summary>
    [Fact]
    public async Task Gui_lai_voi_noi_dung_khac_thi_KHONG_ghi_de_ban_cu()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var reference = Unique("jira-comment");
        await PostEvidenceAsync(client, Evidence(reference, content: "Ban dau"));
        var again = await PostEvidenceAsync(client, Evidence(reference, content: "Ban sua lai"));

        Assert.False(again.Results[0].Created);

        await using var context = db.OpenContext(db.TenantAId);
        var stored = await context.EvidenceItems.SingleAsync(e => e.SourceReference == reference);

        Assert.Equal("Ban dau", stored.Content);
    }

    [Fact]
    public async Task Lo_vuot_tran_thi_tu_choi_ca_lo_va_khong_ghi_gi()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey).With("Ingest:MaxEvidencePerRequest", "2");
        using var client = factory.CreateClient();

        var before = await CountEvidenceAsync(client);

        var response = await PostEvidenceRawAsync(client,
        [
            Evidence(Unique("jira-comment")),
            Evidence(Unique("jira-comment")),
            Evidence(Unique("jira-comment")),
        ]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("trần là 2", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await CountEvidenceAsync(client));
    }

    /// <summary>
    /// Trần của evidence RIÊNG với trần của tín hiệu case: một tín hiệu case là bốn
    /// trường ngắn, một mẩu evidence mang cả nội dung comment. Hạ trần evidence xuống
    /// KHÔNG được hạ theo trần của case.
    /// </summary>
    [Fact]
    public async Task Tran_evidence_va_tran_tin_hieu_case_doc_lap_voi_nhau()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey).With("Ingest:MaxEvidencePerRequest", "1");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(SignalPath, new[]
        {
            new { sourceReference = Unique("jira"), subject = "Case mot" },
            new { sourceReference = Unique("jira"), subject = "Case hai" },
        });

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Lo_rong_thi_400()
    {
        using var factory = ApiFactory.Dedicated(db.TenantAKey);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(EvidencePath, Array.Empty<object>());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // =====================================================================

    private static async Task<string> NewCaseAsync(HttpClient client, string? tenantKey = null)
    {
        var reference = Unique("jira");

        using var request = new HttpRequestMessage(HttpMethod.Post, SignalPath)
        {
            Content = JsonContent.Create(new[]
            {
                new { sourceReference = reference, subject = "Booking OTA khong ve PMS" },
            }),
        };

        if (tenantKey is not null) request.Headers.Add("X-Tenant-Key", tenantKey);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return reference;
    }

    private static async Task<HttpResponseMessage> PostEvidenceRawAsync(
        HttpClient client, object[] items, string? tenantKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, EvidencePath)
        {
            Content = JsonContent.Create(items),
        };

        if (tenantKey is not null) request.Headers.Add("X-Tenant-Key", tenantKey);

        return await client.SendAsync(request);
    }

    private static Task<EvidenceBatchResult> PostEvidenceAsync(HttpClient client, params object[] items) =>
        PostEvidenceAsync(client, items, tenantKey: null);

    private static async Task<EvidenceBatchResult> PostEvidenceAsync(
        HttpClient client, object[] items, string? tenantKey)
    {
        var response = await PostEvidenceRawAsync(client, items, tenantKey);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<EvidenceBatchResult>())!;
    }

    private static async Task<int> CountEvidenceAsync(HttpClient client, string? tenantKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BoundaryPath);

        if (tenantKey is not null) request.Headers.Add("X-Tenant-Key", tenantKey);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<BoundaryResponse>();
        return body!.RowsVisibleWithoutTenantFilter.Evidence;
    }
}
