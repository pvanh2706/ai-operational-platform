using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Domain.Sop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgePlatform.Api.Tests;

/// <summary>Model hỏi đáp giả — trả câu dựng sẵn, nhớ prompt đã nhận.</summary>
public sealed class HoiDapGia(Func<HoiDapPrompt, KetQuaModelHoiDap> traLoi) : IGoiModelHoiDap
{
    public HoiDapPrompt? DaNhan { get; private set; }

    public int SoLanGoi { get; private set; }

    public Task<KetQuaModelHoiDap> HoiAsync(HoiDapPrompt prompt, CancellationToken ct = default)
    {
        SoLanGoi++;
        DaNhan = prompt;
        return Task.FromResult(traLoi(prompt));
    }
}

/// <summary>
/// Hỏi tự do về một case / một tài liệu — `Q3-1` (2026-10-22).
///
/// 🛑 Bốn luật mà nếu một lần sửa làm rơi thì KHÔNG test nào khác bắt được, vì đường này là đường đầu tiên cho model sinh
/// chữ từ vật liệu chưa duyệt: (1) cổng che chạy trước khi gửi, (2) mẩu đã rút lại không đi, (3) phát biểu không trỏ được
/// về mẩu có thật bị bỏ, (4) mọi câu trả lời mang nhãn CHƯA DUYỆT. Mỗi test dưới đây giữ một luật.
/// </summary>
public sealed class HoiDapTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>
{
    private static KetQuaModelHoiDap Tra(bool biet, params (string NoiDung, string[] Nguon)[] p) =>
        new(biet, [.. p.Select(x => new PhatBieuModel(x.NoiDung, x.Nguon))], "");

    /// <summary>Mỗi test một khách RIÊNG — biên lai là "đếm được bao nhiêu" của cả tenant (xem SoanNhapTests).</summary>
    private async Task<(ApiFactory App, Guid TenantId)> MoiKhachAsync(HoiDapGia? model = null, int? tranThang = null)
    {
        var (key, id) = await db.TaoTenantMoiAsync();
        var app = ApiFactory.Dedicated(key);
        if (tranThang is { } t) app = app.With("HoiDap:TranThang", t.ToString());
        if (model is not null) app = app.WithServices(s => s.AddSingleton(new ModelHoiDap(_ => model)));
        return (app, id);
    }

    /// <summary>Một case: mẩu thường, mẩu mang mật khẩu, và mẩu ĐÃ RÚT LẠI.</summary>
    private static async Task<string> DungCaseAsync(HttpClient c)
    {
        var khoa = $"jira:HD-{Guid.CreateVersion7():N}";
        (await c.PostAsJsonAsync("/signals/case-observed", new[] { new { sourceReference = khoa, subject = "Không phát hành được hoá đơn" } }))
            .EnsureSuccessStatusCode();

        (await c.PostAsJsonAsync("/signals/case-evidence", new[]
        {
            new { sourceReference = $"{khoa}#m1", caseSourceReference = khoa, content = "Báo lỗi E0042 khi bấm phát hành", subTenantCode = "khong-xac-dinh" },
            new { sourceReference = $"{khoa}#m2", caseSourceReference = khoa, content = "Mật khẩu: 24680 để vào máy khách", subTenantCode = "khong-xac-dinh" },
            new { sourceReference = $"{khoa}#m3", caseSourceReference = khoa, content = "DOAN-DA-RUT-LAI-KHONG-DUOC-DI", subTenantCode = "khong-xac-dinh" },
        })).EnsureSuccessStatusCode();

        (await c.PostAsJsonAsync("/evidence/rut-lai", new { khoaNguon = $"{khoa}#m3", nguoiDanhDau = "test", lyDo = "rút" }))
            .EnsureSuccessStatusCode();

        return khoa;
    }

    [Fact]
    public async Task Xuat_prompt_da_che_bo_mau_rut_lai_va_khong_ghi_gi()
    {
        var (app, tenantId) = await MoiKhachAsync();
        using var _ = app;
        var c = app.CreateClient();
        var khoa = await DungCaseAsync(c);

        var res = await c.PostAsJsonAsync("/internal/hoi-dap/xuat-prompt", new { khoaNguon = khoa, cauHoi = "Lỗi gì?" });
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var user = kq.GetProperty("userText").GetString()!;
        Assert.Contains("[N1]", user);
        Assert.Contains("E0042", user);
        Assert.DoesNotContain("24680", user);                        // luật 1 — cổng che
        Assert.DoesNotContain("DOAN-DA-RUT-LAI", user);              // luật 2 — AR-m
        Assert.Equal("CHUA-DUYET", kq.GetProperty("trangThai").GetString());

        await using var ctx = db.OpenContext(tenantId);
        Assert.False(await ctx.Set<LuotHoiDap>().AnyAsync());
    }

    [Fact]
    public async Task Hoi_bang_model_bo_phat_bieu_khong_tro_ve_mau_co_that_va_ghi_bien_lai()
    {
        var model = new HoiDapGia(_ => Tra(true, ("Lỗi là E0042", ["N2"]), ("Do máy chủ thuế", ["N99"]), ("Không nguồn", [])));
        var (app, tenantId) = await MoiKhachAsync(model);
        using var _ = app;
        var c = app.CreateClient();
        var khoa = await DungCaseAsync(c);

        var res = await c.PostAsJsonAsync("/internal/hoi-dap", new { khoaNguon = khoa, cauHoi = "Lỗi gì?", nguoiHoi = "chị Lan" });
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal(1, kq.GetProperty("phatBieu").GetArrayLength());     // luật 3
        Assert.Equal(2, kq.GetProperty("soPhatBieuBiBo").GetInt32());
        Assert.Equal("CHUA-DUYET", kq.GetProperty("trangThai").GetString());   // luật 4
        Assert.DoesNotContain("24680", model.DaNhan!.UserText);

        await using var ctx = db.OpenContext(tenantId);
        var luot = await ctx.Set<LuotHoiDap>().SingleAsync();
        Assert.True(luot.ThanhCong);
        Assert.Equal(("case", khoa, "anthropic"), (luot.LoaiNguon, luot.MaNguon, luot.NhaCungCap));
    }

    /// <summary>Model tự nói "biết" mà không dẫn được mẩu nào — vẫn là KHÔNG BIẾT, 200 chứ không phải lỗi.</summary>
    [Fact]
    public async Task Noi_biet_ma_khong_dan_duoc_nguon_thi_KHONG_BIET()
    {
        var model = new HoiDapGia(_ => Tra(true, ("Chắc là do mạng", ["X1"])));
        var (app, _) = await MoiKhachAsync(model);
        using var __ = app;
        var c = app.CreateClient();
        var khoa = await DungCaseAsync(c);

        var kq = await (await c.PostAsJsonAsync("/internal/hoi-dap", new { khoaNguon = khoa, cauHoi = "Vì sao?", nguoiHoi = "a" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Equal("khong-co-nguon-dan-chung", kq.GetProperty("lyDoKhongBiet").GetString());
        Assert.False(kq.GetProperty("loiVanHanh").GetBoolean());
    }

    [Fact]
    public async Task Khong_co_khoa_thi_409_chi_che_do_File_va_khong_ghi_bien_lai()
    {
        var (app, tenantId) = await MoiKhachAsync();
        using var _ = app;
        var c = app.CreateClient();
        var khoa = await DungCaseAsync(c);

        var res = await c.PostAsJsonAsync("/internal/hoi-dap", new { khoaNguon = khoa, cauHoi = "Lỗi gì?", nguoiHoi = "a" });
        var body = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("chua-co-credit", body);
        Assert.Contains("xuat-prompt", body);

        await using var ctx = db.OpenContext(tenantId);
        Assert.False(await ctx.Set<LuotHoiDap>().AnyAsync());
    }

    [Fact]
    public async Task Tran_thang_dem_ca_luot_truoc_va_chan_luot_sau()
    {
        var model = new HoiDapGia(_ => Tra(false));
        var (app, _) = await MoiKhachAsync(model, tranThang: 1);
        using var __ = app;
        var c = app.CreateClient();
        var khoa = await DungCaseAsync(c);
        var than = new { khoaNguon = khoa, cauHoi = "Lỗi gì?", nguoiHoi = "a" };

        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/internal/hoi-dap", than)).StatusCode);
        var lan2 = await c.PostAsJsonAsync("/internal/hoi-dap", than);

        Assert.Equal(HttpStatusCode.Conflict, lan2.StatusCode);
        Assert.Contains("vuot-tran-thang", await lan2.Content.ReadAsStringAsync());
        Assert.Equal(1, model.SoLanGoi);
    }

    [Fact]
    public async Task Nap_ket_qua_file_go_hang_rao_va_doi_chieu_bang_cung_luat()
    {
        var (app, _) = await MoiKhachAsync();
        using var __ = app;
        var c = app.CreateClient();
        var khoa = await DungCaseAsync(c);

        var ketQua = "```json\n{\"bietTraLoi\":true,\"phatBieu\":[{\"noiDung\":\"E0042\",\"nguon\":[\"N2\"]},{\"noiDung\":\"bịa\",\"nguon\":[\"N9\"]}],\"ghiChu\":\"\"}\n```";
        var res = await c.PostAsJsonAsync("/internal/hoi-dap/nap-ket-qua", new { khoaNguon = khoa, cauHoi = "Lỗi gì?", ketQua });
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("file", kq.GetProperty("cheDo").GetString());
        Assert.Equal(1, kq.GetProperty("phatBieu").GetArrayLength());
        Assert.Equal(1, kq.GetProperty("soPhatBieuBiBo").GetInt32());

        var hong = await c.PostAsJsonAsync("/internal/hoi-dap/nap-ket-qua", new { khoaNguon = khoa, cauHoi = "x", ketQua = "không phải JSON" });
        Assert.Equal(HttpStatusCode.BadRequest, hong.StatusCode);
    }

    [Fact]
    public async Task Hoi_ve_tai_lieu_chia_doan_co_ma()
    {
        var model = new HoiDapGia(p => Tra(true, ("Bước 2 là ký số", [p.NguonDaChe[0].Ma])));
        var (app, _) = await MoiKhachAsync(model);
        using var __ = app;
        var c = app.CreateClient();

        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes($"Hướng dẫn phát hành {Guid.CreateVersion7()}\nBước 1: chọn ký hiệu\nBước 2: ký số"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(file, "file", "huong-dan.txt");
        form.Add(new StringContent("khong-xac-dinh"), "subTenantCode");
        form.Add(new StringContent("chị Lan"), "nguoiNap");
        var nap = await (await c.PostAsync("/documents", form)).Content.ReadFromJsonAsync<JsonElement>();
        var id = nap.GetProperty("id").GetGuid();

        var res = await c.PostAsJsonAsync("/internal/hoi-dap", new { taiLieuId = id, cauHoi = "Bước 2 là gì?", nguoiHoi = "a" });
        var kq = await res.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True(kq.GetProperty("bietTraLoi").GetBoolean());
        Assert.Contains("huong-dan.txt", kq.GetProperty("nguon")[0].GetProperty("nhan").GetString());
    }

    [Theory]
    [InlineData("""{"cauHoi":"x","nguoiHoi":"a"}""", HttpStatusCode.BadRequest)]                                  // không nguồn
    [InlineData("""{"khoaNguon":"k","taiLieuId":"00000000-0000-0000-0000-000000000001","cauHoi":"x","nguoiHoi":"a"}""", HttpStatusCode.BadRequest)]
    [InlineData("""{"khoaNguon":"khong-co-case-nay","cauHoi":"x","nguoiHoi":"a"}""", HttpStatusCode.NotFound)]
    [InlineData("""{"khoaNguon":"k","cauHoi":"x"}""", HttpStatusCode.BadRequest)]                                  // thiếu nguoiHoi
    [InlineData("""{"khoaNguon":"k","cauHoi":"x","nguoiHoi":"a","nhaCungCap":"gemini"}""", HttpStatusCode.BadRequest)]
    public async Task Than_sai_bi_tu_choi_dung_ma(string than, HttpStatusCode ma)
    {
        var (app, _) = await MoiKhachAsync(new HoiDapGia(_ => Tra(false)));
        using var __ = app;

        var res = await app.CreateClient().PostAsync("/internal/hoi-dap", new StringContent(than, Encoding.UTF8, "application/json"));

        Assert.Equal(ma, res.StatusCode);
    }
}
