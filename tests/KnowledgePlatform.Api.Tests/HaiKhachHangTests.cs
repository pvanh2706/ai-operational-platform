using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgePlatform.Api.Webhooks;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Tests;

/// <summary>
/// **PHÉP KIỂM NGHIỆM THU CỦA MỨC 3** — `I2-1`: *"onboard được khách thứ hai mà không sửa code"*.
///
/// Mọi file test khác hỏi *"tính năng này chạy đúng không"*. File này hỏi một câu khác:
/// **"hai khách hàng chạy cạnh nhau thì có lẫn vào nhau không"** — và nó nhắm thẳng vào phần
/// hạ tầng dựng ở bước 1 và 2 (`IM-39`, `IM-40`), phần mà `TenantBoundaryThroughHttpTests`
/// viết trước đó nên không biết gì về.
///
/// 🛑 **Chỗ rủi ro nhất, và là lý do file này tồn tại:** <see cref="WebhookSenderService"/>
/// là nơi DUY NHẤT ngoài middleware gọi <c>Resolve()</c>. RLS **không cứu được** kiểu lỗi ở
/// đó — tenant đã được resolve, chỉ là resolve nhầm người. Mọi cơ chế còn lại của dự án đều
/// đứng trên giả định rằng tenant của request là đúng.
/// </summary>
public sealed class HaiKhachHangTests(ApiDatabaseFixture db) : IClassFixture<ApiDatabaseFixture>, IDisposable
{
    private readonly BenNhanGia _benNhan = new();
    private readonly List<string> _fileTam = [];

    public void Dispose()
    {
        foreach (var f in _fileTam.Where(File.Exists)) File.Delete(f);
    }

    private sealed record Khach(
        string Ten, string TenantKey, Guid TenantId, string Nhom, string Ticket, string Bang, string Dich);

    private async Task<Khach> MoiKhachAsync(string ten)
    {
        var (tenantKey, tenantId) = await db.TaoTenantMoiAsync();
        var id = Guid.CreateVersion7().ToString("N");
        var nhom = $"nhom-{ten}-{id}";
        var ticket = $"jira-{ten}-{id}";

        var duong = Path.Combine(Path.GetTempPath(), $"dt-{ten}-{id}.json");
        File.WriteAllText(duong, JsonSerializer.Serialize(new
        {
            ketQua = new Dictionary<string, object>
            {
                [ticket] = new { nhom, nguon = "noi-ro-trong-mo-ta", vietTat = "test" },
            },
        }));
        _fileTam.Add(duong);

        return new Khach(ten, tenantKey, tenantId, nhom, ticket, duong,
            $"http://ben-nhan.thu-nghiem/{ten}");
    }

    private ApiFactory App(Khach k) =>
        new ApiFactory(new Dictionary<string, string?>
            {
                ["Tenancy:Mode"] = "DedicatedSingleTenant",
                ["Tenancy:TenantExternalKey"] = k.TenantKey,
                ["Auth:AcknowledgeUnauthenticatedApi"] = "true",
                ["Link:PublicBaseUrl"] = "http://kp-noi-bo.thu-nghiem:5119",
                ["DinhTuyen:DuongDanBangTraSan"] = k.Bang,
                ["Webhook:BatVongQuet"] = "false",
            })
            .WithServices(s => s
                .AddHttpClient(WebhookSender.TenHttpClient)
                .ConfigurePrimaryHttpMessageHandler(() => _benNhan));

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
                NoiXem = "Màn phát hành",
                Nhanh = [new SopBranch("Ô trống", "S1", ["ES-9001"], "evidence-noi-ro")],
            },
        ],
        BuocSua = [new SopFixStep("S1", "Chọn ký hiệu rồi phát hành lại", ["ES-9001"])],
        PhanBoBuocKiemDuocGhiLai = new SopDistribution(
            new SopDistributionBucket(2, ["ES-9001", "ES-9002"]),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, []),
            new SopDistributionBucket(0, []), new SopDistributionBucket(0, [])),
    };

    /// <summary>
    /// Dựng một khách từ con số 0, CHỈ bằng API công khai — đúng thứ `I2-1` đòi: khai báo
    /// đích, nạp tri thức, duyệt, rồi bắn một tín hiệu. Không một dòng code nào phải đổi.
    /// </summary>
    private static async Task<HttpClient> OnboardAsync(ApiFactory app, Khach k)
    {
        var c = app.CreateClient();

        (await c.PutAsJsonAsync("/internal/webhook", new { url = k.Dich })).EnsureSuccessStatusCode();

        var nap = await c.PostAsJsonAsync("/knowledge/sop-draft", Nhap(k.Nhom));
        nap.EnsureSuccessStatusCode();

        var kq = await nap.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var r in kq.GetProperty("nguyenNhan").EnumerateArray())
        {
            var duyet = await c.PostAsJsonAsync($"/knowledge/{r.GetProperty("id").GetGuid()}/approve",
                new { nguoiDuyet = "support", phamViXem = "noi-bo", xacNhanDuLieuThuongMai = true });
            duyet.EnsureSuccessStatusCode();
        }

        var tin = await c.PostAsJsonAsync("/signals/case-observed",
            new[] { new { sourceReference = k.Ticket, subject = "Không phát hành được hoá đơn" } });
        tin.EnsureSuccessStatusCode();

        return c;
    }

    /// <summary>Chạy vòng quét NỀN thật — kể cả phần duyệt tenant, chỗ rủi ro nhất.</summary>
    private static async Task VongQuetNenAsync(ApiFactory app)
    {
        var sv = new WebhookSenderService(
            app.Services,
            app.Services.GetRequiredService<IOptions<WebhookOptions>>(),
            app.Services.GetRequiredService<ILogger<WebhookSenderService>>());

        await sv.MotVongAsync(CancellationToken.None);
    }

    // =========================================================================

    /// <summary>
    /// 🛑 Test nặng nhất của cả file. Hai khách, hai đích, một tiến trình — gói của ai phải
    /// tới đúng đích của người đó.
    ///
    /// RLS không đứng gác ở đây: tới lúc gửi thì tenant ĐÃ được resolve. Nếu vòng lặp duyệt
    /// tenant lấy nhầm scope, dữ liệu của khách A đi tới địa chỉ của khách B — và không cơ
    /// chế nào khác của dự án bắt được.
    /// </summary>
    [Fact]
    public async Task Hai_khach_hai_dich_KHONG_lan_goi_cua_nhau()
    {
        var a = await MoiKhachAsync("khacha");
        var b = await MoiKhachAsync("khachb");

        using var appA = App(a);
        using var appB = App(b);

        await OnboardAsync(appA, a);
        await OnboardAsync(appB, b);

        await VongQuetNenAsync(appA);
        await VongQuetNenAsync(appB);

        var toiA = _benNhan.ToiDia(a.Dich);
        var toiB = _benNhan.ToiDia(b.Dich);

        Assert.Single(toiA);
        Assert.Single(toiB);

        // Và nội dung phải là của CHÍNH khách đó — không chỉ "có một gói".
        Assert.Contains(a.Ticket, toiA[0].Body);
        Assert.DoesNotContain(b.Ticket, toiA[0].Body);
        Assert.Contains(b.Ticket, toiB[0].Body);
        Assert.DoesNotContain(a.Ticket, toiB[0].Body);
    }

    /// <summary>
    /// Chữ ký của khách A phải tính bằng bí mật của A. Nếu vòng lặp lấy nhầm cấu hình đích,
    /// gói vẫn tới đúng URL nhưng ký SAI — bên nhận từ chối, và triệu chứng là *"webhook
    /// thỉnh thoảng không qua được"*, thứ rất khó truy.
    /// </summary>
    [Fact]
    public async Task Goi_cua_moi_khach_ky_bang_bi_mat_cua_chinh_khach_do()
    {
        var a = await MoiKhachAsync("kya");
        var b = await MoiKhachAsync("kyb");

        using var appA = App(a);
        using var appB = App(b);
        await OnboardAsync(appA, a);
        await OnboardAsync(appB, b);

        await VongQuetNenAsync(appA);

        await using var ctxA = db.OpenContext(a.TenantId);
        await using var ctxB = db.OpenContext(b.TenantId);
        var biMatA = (await ctxA.Set<WebhookEndpoint>().FirstAsync()).SigningSecret;
        var biMatB = (await ctxB.Set<WebhookEndpoint>().FirstAsync()).SigningSecret;

        Assert.NotEqual(biMatA, biMatB);

        var goiA = _benNhan.ToiDia(a.Dich).Single();
        Assert.Equal("sha256=" + WebhookSender.Ky(biMatA, goiA.Body), goiA.ChuKy);
        Assert.NotEqual("sha256=" + WebhookSender.Ky(biMatB, goiA.Body), goiA.ChuKy);
    }

    /// <summary>
    /// 🛑 Bản deploy DEDICATED của khách A **không được** làm việc thay khách B, kể cả khi
    /// database tình cờ chứa cả hai.
    ///
    /// `G13` nói một bản dedicated phục vụ MỘT khách. Vòng quét nền duyệt danh bạ tenant, nên
    /// nếu không hỏi chế độ deploy thì nó lặng lẽ xử lý hàng đợi của MỌI khách. Đó không phải
    /// rò dữ liệu qua ranh giới (gói của B vẫn tới đích của B) — nó là **làm việc ngoài phạm
    /// vi bản deploy**, và không ai nhìn ra cho tới lúc có người hỏi *"sao máy này lại gọi ra
    /// địa chỉ đó"*.
    /// </summary>
    [Fact]
    public async Task Ban_dedicated_cua_A_KHONG_gui_ho_khach_B()
    {
        var a = await MoiKhachAsync("chia");
        var b = await MoiKhachAsync("chib");

        using var appA = App(a);
        using var appB = App(b);
        await OnboardAsync(appA, a);
        await OnboardAsync(appB, b);

        // CHỈ chạy vòng quét của bản deploy A.
        await VongQuetNenAsync(appA);

        Assert.Single(_benNhan.ToiDia(a.Dich));
        Assert.Empty(_benNhan.ToiDia(b.Dich));
    }

    /// <summary>Khách A không đọc được cấu hình đích của khách B.</summary>
    [Fact]
    public async Task Khach_A_khong_doc_duoc_cau_hinh_webhook_cua_B()
    {
        var a = await MoiKhachAsync("cfga");
        var b = await MoiKhachAsync("cfgb");

        using var appA = App(a);
        using var appB = App(b);
        await OnboardAsync(appB, b);

        var res = await appA.CreateClient().GetAsync("/internal/webhook");

        // A chưa khai báo gì -> 404. Điều quan trọng là nó KHÔNG đọc ra đích của B.
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.DoesNotContain(b.Dich, await res.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Token link của khách A mở ra dữ liệu của A, không phải của B — kể cả khi bảng
    /// <c>knowledge_link_token</c> được MIỄN RLS (`D5-3`).
    /// </summary>
    [Fact]
    public async Task Link_cua_A_mo_ra_du_lieu_cua_A_khong_phai_cua_B()
    {
        var a = await MoiKhachAsync("linka");
        var b = await MoiKhachAsync("linkb");

        using var appA = App(a);
        using var appB = App(b);
        var cA = await OnboardAsync(appA, a);
        await OnboardAsync(appB, b);

        await VongQuetNenAsync(appA);

        var goiA = JsonSerializer.Deserialize<JsonElement>(_benNhan.ToiDia(a.Dich).Single().Body);
        var link = goiA.GetProperty("huongDan").EnumerateArray().First().GetProperty("link").GetString()!;
        var token = link[(link.LastIndexOf('/') + 1)..];

        var body = await (await cA.GetAsync($"/xem/{token}")).Content.ReadAsStringAsync();

        Assert.Contains(a.Nhom, body);
        Assert.DoesNotContain(b.Nhom, body);
    }
}
