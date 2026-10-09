using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KnowledgePlatform.Api.Documents;
using KnowledgePlatform.Api.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// Kênh 2 — ô `A2` (job quét) và ô `K` (nội dung đọc được), P9 2026-10-10, KHÔNG thêm thư viện (`N4-1`).
///
/// 🛑 Ba điều file này giữ: quét lại KHÔNG nạp lại (khử trùng theo nội dung) · `S6` vẫn đứng — quét cả thư mục cũng
/// KHÔNG sinh một tri thức nào · cấu hình nguồn thiếu mã khách sạn là KHÔNG KHỞI ĐỘNG (một job nền sai cấu hình thì sai
/// IM LẶNG mỗi 5 phút, ở nơi không ai ngồi nhìn).
/// </summary>
public sealed class QuetThuMucTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private readonly List<string> _thuMucTam = [];

    public void Dispose()
    {
        foreach (var d in _thuMucTam.Where(Directory.Exists)) Directory.Delete(d, recursive: true);
    }

    private string ThuMucMoi()
    {
        var d = Path.Combine(Path.GetTempPath(), $"kp-qtm-{Guid.CreateVersion7():N}");
        Directory.CreateDirectory(d);
        _thuMucTam.Add(d);
        return d;
    }

    private (ApiFactory App, string ThuMuc) App(string tenantKey, string? subTenant = "khong-xac-dinh")
    {
        var thuMuc = ThuMucMoi();
        var app = ApiFactory.Dedicated(tenantKey)
            .With("KhoFile:ThuMucGoc", ThuMucMoi())
            .With("KenhHai:BatVongQuet", "false")
            .With("KenhHai:Nguon:0:ThuMuc", thuMuc)
            .With("KenhHai:Nguon:0:SubTenantCode", subTenant)
            .With("KenhHai:Nguon:0:NguoiNap", "job-quet-thu-muc");
        return (app, thuMuc);
    }

    private static async Task<KetQuaQuet> QuetAsync(ApiFactory app, Guid tenantId, string thuMuc)
    {
        await using var scope = app.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(tenantId);
        return await scope.ServiceProvider.GetRequiredService<QuetThuMuc>().QuetAsync(
            new NguonQuetThuMuc { ThuMuc = thuMuc, SubTenantCode = "khong-xac-dinh", NguoiNap = "job-quet-thu-muc" });
    }

    private static byte[] Docx(string chu)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var w = new StreamWriter(zip.CreateEntry("word/document.xml").Open(), new UTF8Encoding(false));
            w.Write($"""<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body><w:p><w:r><w:t>{chu}</w:t></w:r></w:p></w:body></w:document>""");
        }

        return ms.ToArray();
    }

    // =========================================================================

    [Fact]
    public async Task Quet_nap_moi_file_DUNG_MOT_LAN_va_file_moi_thi_nap_them()
    {
        var (key, tenantId) = await db.TaoTenantMoiAsync();
        var (app, thuMuc) = App(key);
        using var _ = app;
        app.CreateClient();   // khởi động host — StartupChecks chạy ở đây

        File.WriteAllText(Path.Combine(thuMuc, "huong-dan.txt"), "Bước 1: xem nhật ký kết nối");
        File.WriteAllBytes(Path.Combine(thuMuc, "quy-trinh.docx"), Docx("Bước 2: kiểm phân quyền"));
        File.WriteAllText(Path.Combine(thuMuc, "~$tam.docx"), "file tạm của Office");

        var lan1 = await QuetAsync(app, tenantId, thuMuc);
        Assert.Equal(2, lan1.Moi);

        var lan2 = await QuetAsync(app, tenantId, thuMuc);
        Assert.Equal(0, lan2.Moi);

        File.WriteAllText(Path.Combine(thuMuc, "moi.md"), "# Bước 3");
        var lan3 = await QuetAsync(app, tenantId, thuMuc);
        Assert.Equal(1, lan3.Moi);

        await using var ctx = db.OpenContext(tenantId);
        var ds = await ctx.Set<Domain.Documents.Document>().ToListAsync();
        Assert.Equal(3, ds.Count);
        Assert.All(ds, d => Assert.Equal(Domain.Documents.TrichChu.DaTrich, d.TrangThaiTrichChu));
        Assert.Contains(ds, d => d.NoiDungChu == "Bước 2: kiểm phân quyền");
    }

    /// <summary>🛑 `S6`: quét cả thư mục tài liệu KHÔNG làm kho tri thức tăng một dòng nào.</summary>
    [Fact]
    public async Task Quet_thu_muc_KHONG_sinh_tri_thuc_S6()
    {
        var (key, tenantId) = await db.TaoTenantMoiAsync();
        var (app, thuMuc) = App(key);
        using var _ = app;
        app.CreateClient();

        File.WriteAllText(Path.Combine(thuMuc, "sop.txt"), "Nguyên nhân: lệch cấu hình. Cách xử lý: sửa cấu hình.");
        await QuetAsync(app, tenantId, thuMuc);

        await using var ctx = db.OpenContext(tenantId);
        Assert.Equal(0, await ctx.KnowledgeRecords.CountAsync());
    }

    [Fact]
    public void Nguon_quet_thieu_SubTenantCode_thi_KHONG_KHOI_DONG()
    {
        var (app, _) = App(db.TenantAKey, subTenant: null);
        using var __ = app;

        var ex = Assert.Throws<InvalidOperationException>(() => app.CreateClient());
        Assert.Contains("SubTenantCode", ex.Message);
    }

    /// <summary>Ô K qua HTTP: nạp .docx → đọc được CHỮ, không chỉ tải được file.</summary>
    [Fact]
    public async Task Nap_docx_qua_HTTP_roi_doc_duoc_CHU()
    {
        var (app, _) = App(db.TenantAKey);
        using var __ = app;
        var c = app.CreateClient();

        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Docx("Kiểm ô ký hiệu trước khi phát hành"));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        form.Add(file, "file", $"sop-{Guid.CreateVersion7():N}.docx");
        form.Add(new StringContent("khong-xac-dinh"), "subTenantCode");
        form.Add(new StringContent("chị Lan"), "nguoiNap");

        var nap = await (await c.PostAsync("/documents", form)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("da-trich", nap.GetProperty("trangThaiTrichChu").GetString());

        var chu = await (await c.GetAsync($"/documents/{nap.GetProperty("id").GetGuid()}/chu")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Kiểm ô ký hiệu trước khi phát hành", chu.GetProperty("noiDungChu").GetString());
    }

    /// <summary>PDF: nạp ĐƯỢC (file gốc vào kho), chỉ là CHƯA đọc được chữ — và nói rõ vì sao.</summary>
    [Fact]
    public async Task Nap_pdf_van_NAP_duoc_nhung_bao_chua_ho_tro()
    {
        var (app, _) = App(db.TenantAKey);
        using var __ = app;
        var c = app.CreateClient();

        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.ASCII.GetBytes($"%PDF-1.4 gia {Guid.CreateVersion7()}"));
        form.Add(file, "file", "ban-scan.pdf");
        form.Add(new StringContent("khong-xac-dinh"), "subTenantCode");
        form.Add(new StringContent("chị Lan"), "nguoiNap");

        var res = await c.PostAsync("/documents", form);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var nap = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("chua-ho-tro", nap.GetProperty("trangThaiTrichChu").GetString());
    }
}
