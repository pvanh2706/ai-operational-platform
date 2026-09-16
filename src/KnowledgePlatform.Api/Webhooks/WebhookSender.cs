using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Domain.Redaction;
using KnowledgePlatform.Domain.Webhooks;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Webhooks;

public sealed class WebhookOptions
{
    public const string SectionName = "Webhook";

    /// <summary>
    /// Bật vòng quét nền. Tắt thì hàng đợi VẪN được xếp, chỉ không có ai gửi.
    ///
    /// ⚠ Bộ test tắt cái này, và lý do đáng ghi: một vòng lặp nền chạy song song sẽ nhặt mất
    /// chính dòng mà test đang chuẩn bị kiểm — test đỏ NGẪU NHIÊN, và người ta sẽ chạy lại
    /// cho tới khi xanh rồi đi tiếp. Test gọi thẳng <see cref="WebhookSender"/> thay vì chờ.
    /// </summary>
    public bool BatVongQuet { get; set; } = true;

    /// <summary>Bao lâu quét hàng đợi một lần.</summary>
    public int ChuKyGiay { get; set; } = 15;

    /// <summary>Số lượt gửi tối đa xử lý trong một vòng quét, cho mỗi khách hàng.</summary>
    public int MoiLuotToiDa { get; set; } = 20;

    /// <summary>Chờ tối đa một lượt POST.</summary>
    public int TimeoutGiay { get; set; } = 20;

    /// <summary>Độ dài tối đa của phần tóm tắt trong gói gửi đi.</summary>
    public int TomTatToiDa { get; set; } = 280;
}

/// <summary>
/// Gửi kết quả về phần mềm của khách (`D1-1`). Đây là **đường ra thứ ba** của sản phẩm, sau
/// payload gọi model và link xem tri thức.
///
/// 🎯 **Cố ý dùng lại <see cref="HoiHandler"/> thay vì tự truy vấn kho.** Nếu tự truy vấn thì
/// sẽ có HAI chỗ quyết định *"biết hay không biết"*, và hai chỗ đó sẽ lệch nhau — chuyện gần
/// như chắc chắn xảy ra, vì hai cửa KHÔNG BIẾT (`docs/13`) là loại luật dễ quên một vế. Lúc
/// lệch thì `/hoi` nói không biết còn webhook gửi ra một hướng dẫn, hoặc ngược lại; và không
/// ai phát hiện vì cả hai đều "chạy".
///
/// 🛑 **Cổng che `AR-o` canh đường này.** Phần tóm tắt đi thẳng vào hệ thống của bên thứ ba
/// (comment Jira, log của họ) và không có đường thu hồi.
/// </summary>
public sealed class WebhookSender(
    AppDbContext db,
    HoiHandler hoi,
    PhatLinkHandler phatLink,
    IHttpClientFactory httpClientFactory,
    TimeProvider clock,
    IOptions<WebhookOptions> options,
    ILogger<WebhookSender> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly WebhookOptions _options = options.Value;

    /// <summary>
    /// Xử lý những lượt gửi ĐẾN HẠN của tenant hiện tại. Trả số lượt đã gửi xong.
    ///
    /// ⚠ Gọi được thẳng từ test — cố ý tách khỏi vòng lặp nền. Một vòng lặp nền chỉ kiểm được
    /// bằng cách chờ, và test phải chờ là test sẽ đỏ ngẫu nhiên trên máy chậm.
    /// </summary>
    public async Task<int> GuiDenHanAsync(CancellationToken ct = default)
    {
        var bayGio = clock.GetUtcNow();

        var dich = await db.Set<WebhookEndpoint>().FirstOrDefaultAsync(e => e.Active, ct);
        if (dich is null) return 0;

        var dsGui = await db.Set<WebhookDelivery>()
            .Where(d => d.State == WebhookDeliveryState.Pending && d.NextAttemptAt <= bayGio)
            .OrderBy(d => d.CreatedAt)
            .Take(_options.MoiLuotToiDa)
            .ToListAsync(ct);

        var xong = 0;

        foreach (var luot in dsGui)
        {
            try
            {
                var payload = await DungGoiAsync(luot, ct);
                await GuiAsync(dich, luot, payload, ct);
                luot.GhiThanhCong(clock.GetUtcNow());
                xong++;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // ⚠ Bắt rộng là CÓ CHỦ ĐÍCH ở đúng chỗ này: một lượt gửi hỏng không được làm
                // chết cả vòng quét, nếu không thì một dòng độc chặn hàng đợi của mọi dòng sau.
                // Lỗi được GHI LẠI vào chính dòng đó, không nuốt im lặng.
                luot.GhiThatBai($"{e.GetType().Name}: {e.Message}", clock.GetUtcNow());
                logger.LogWarning(e, "Gửi webhook {MaGoi} thất bại lượt {Luot}", luot.Id, luot.Attempts);
            }
        }

        if (dsGui.Count > 0) await db.SaveChangesAsync(ct);
        return xong;
    }

    private async Task<WebhookPayload> DungGoiAsync(WebhookDelivery luot, CancellationToken ct)
    {
        // Đúng đường mà `/hoi` đi — kể cả hai cửa KHÔNG BIẾT.
        var traLoi = await hoi.HoiAsync(
            new CauHoi(luot.CaseSourceReference, luot.Subject, null), ct);

        var huongDan = new List<WebhookHuongDan>();

        foreach (var h in traLoi.HuongDan ?? [])
        {
            // Link có thể không phát được (thiếu Link:PublicBaseUrl). Khi đó tóm tắt vẫn đi —
            // mất đường đọc đầy đủ còn hơn không gửi gì cả.
            var link = await phatLink.PhatAsync(h.Id, "webhook", null, ct);

            huongDan.Add(new WebhookHuongDan(
                Id: h.Id,
                TenNguyenNhan: Che(h.TenNguyenNhan),
                TrangThai: h.TrangThai,
                TomTat: TomTat(h),
                Link: link.Phat ? link.Url : null,
                LinkHetHanLuc: link.Phat ? link.HetHanLuc : null));
        }

        return new WebhookPayload(
            PhienBan: WebhookPayload.PhienBanHienTai,
            MaGoi: luot.Id,
            Luc: clock.GetUtcNow(),
            Case: new WebhookCase(luot.CaseSourceReference, Che(luot.Subject)),
            BietTraLoi: traLoi.BietTraLoi,
            Nhom: traLoi.Nhom,
            NguonDinhTuyen: traLoi.NguonDinhTuyen,
            LyDoKhongBiet: traLoi.LyDoKhongBiet,
            HuongDan: huongDan);
    }

    /// <summary>
    /// Tóm tắt = bước xử lý đầu tiên, đã che và đã cắt.
    ///
    /// ⚠ Cắt ở đây chỉ để gói gọn, KHÔNG phải để giấu: bản đầy đủ nằm sau link. Và cắt thì
    /// phải NÓI RA bằng dấu `…` — một đoạn bị cụt lặng lẽ đọc ra thành một hướng dẫn thiếu bước.
    /// </summary>
    private string TomTat(HuongDan h)
    {
        var noi = h.CachXuLy.FirstOrDefault()?.NoiDung
                  ?? h.CachNhanRa.FirstOrDefault()?.NoiDung
                  ?? h.TenNguyenNhan;

        var che = Che(noi);
        return che.Length <= _options.TomTatToiDa ? che : che[.._options.TomTatToiDa].TrimEnd() + "…";
    }

    private static string Che(string? s) => EgressRedactor.Redact(s).Text;

    private async Task GuiAsync(WebhookEndpoint dich, WebhookDelivery luot, WebhookPayload payload, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(payload, Json);

        using var req = new HttpRequestMessage(HttpMethod.Post, dich.Url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        // Ký trên ĐÚNG chuỗi byte gửi đi. Ký trên object rồi serialize lại là cách chữ ký
        // đúng với một thứ khác với thứ bên kia nhận được.
        req.Headers.TryAddWithoutValidation(HeaderChuKy, "sha256=" + Ky(dich.SigningSecret, body));
        req.Headers.TryAddWithoutValidation(HeaderMaGoi, luot.Id.ToString());
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var client = httpClientFactory.CreateClient(TenHttpClient);
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutGiay);

        using var res = await client.SendAsync(req, ct);

        if (!res.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"bên nhận trả {(int)res.StatusCode} {res.ReasonPhrase}");
        }
    }

    public const string TenHttpClient = "webhook";
    public const string HeaderChuKy = "X-KP-Signature";
    public const string HeaderMaGoi = "X-KP-Delivery";

    /// <summary>HMAC-SHA256 hex thường, trên đúng thân gói.</summary>
    public static string Ky(string biMat, string body) =>
        Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(biMat), Encoding.UTF8.GetBytes(body)));
}

/// <summary>
/// Vòng lặp nền gọi <see cref="WebhookSender"/> cho TỪNG khách hàng.
///
/// 🛑 **Duyệt theo tenant chứ không đọc một phát cả bảng, và đó là quyết định có giá.** Đọc
/// một phát thì phải miễn RLS cho hai bảng webhook — tức là mở thêm một lỗ ở đúng nơi `G7`
/// gọi là nền tảng, để đổi lấy vài truy vấn. Duyệt theo tenant giữ cho ranh giới còn hiệu lực
/// **kể cả trong tiến trình nền**, chỗ mà không có request nào để mà kiểm.
///
/// ⚠ Giá phải trả, ghi ra kẻo sau này ai đó ngạc nhiên: một truy vấn cho mỗi khách hàng mỗi
/// chu kỳ. Với vài chục khách thì không đáng kể; tới vài nghìn thì phải đổi cách, và lúc đó
/// đổi sang gì cũng được TRỪ việc miễn RLS.
/// </summary>
public sealed class WebhookSenderService(
    IServiceProvider services,
    IOptions<WebhookOptions> options,
    ILogger<WebhookSenderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var chuKy = TimeSpan.FromSeconds(Math.Max(1, options.Value.ChuKyGiay));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MotVongAsync(stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Vòng lặp nền KHÔNG được chết vì một lỗi — chết im lặng là kiểu hỏng tệ nhất
                // ở đây: mọi thứ trông bình thường, chỉ là không ai nhận được gì nữa.
                logger.LogError(e, "Vòng quét webhook lỗi; sẽ thử lại ở chu kỳ sau.");
            }

            try
            {
                await Task.Delay(chuKy, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Một vòng quét: duyệt TỪNG khách hàng, mỗi khách một scope riêng.
    ///
    /// ⚠ **Public để test gọi được, và đó không phải sự tiện tay.** Đây là chỗ DUY NHẤT
    /// trong hệ thống gọi <c>Resolve()</c> ngoài middleware — tức đúng chỗ một lỗ rò tenant
    /// sẽ nấp, và là chỗ RLS **không cứu được**: tenant đã được resolve, chỉ là resolve nhầm
    /// người. Một cơ chế canh gác mà bộ test không với tới được thì không ai biết nó còn sống.
    /// </summary>
    public async Task MotVongAsync(CancellationToken ct)
    {
        foreach (var tenantId in await PhamViAsync(ct))
        {
            if (ct.IsCancellationRequested) return;

            await using var scope = services.CreateAsyncScope();

            // Không có middleware nào trong tiến trình nền, nên tenant được đặt tường minh.
            // Đây là chỗ DUY NHẤT ngoài middleware gọi Resolve — và nó vẫn đi qua đúng
            // ITenantContext ấy, nên interceptor và RLS phía dưới không biết mình đang ở đâu.
            scope.ServiceProvider.GetRequiredService<Tenancy.RequestTenantContext>().Resolve(tenantId);

            await scope.ServiceProvider.GetRequiredService<WebhookSender>().GuiDenHanAsync(ct);
        }
    }

    /// <summary>
    /// Những khách hàng mà BẢN DEPLOY NÀY được phép làm việc cho.
    ///
    /// 🛑 **Bản dedicated chỉ phục vụ ĐÚNG khách của nó, kể cả khi database tình cờ chứa
    /// nhiều tenant** — và đây là một lỗi THẬT, tìm ra bằng phép kiểm nghiệm thu hai khách
    /// hàng (`I2-1`), không phải bằng đọc lại code.
    ///
    /// Bản đầu duyệt thẳng danh bạ tenant. Nó KHÔNG rò dữ liệu qua ranh giới — gói của khách
    /// B vẫn tới đúng địa chỉ của B — nên `RlsGuard`, policy RLS và mọi test tenant cũ đều
    /// xanh. Cái sai là **phạm vi làm việc**: một máy dựng cho khách A tự đi gọi ra địa chỉ
    /// của khách B. Trái `G13` (*một bản dedicated phục vụ MỘT khách*), và không ai nhìn ra
    /// cho tới lúc có người hỏi *"sao máy này lại gọi ra đó"*.
    ///
    /// ⚠ Đúng loại lỗi mà cả dự án nhắm vào: không crash, không đỏ test nào, chỉ sai im lặng.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> PhamViAsync(CancellationToken ct)
    {
        var cuaBanDeploy = services.GetRequiredService<Tenancy.DedicatedTenant>().TenantId;
        if (cuaBanDeploy.HasValue) return [cuaBanDeploy.Value];

        // Chế độ shared: phục vụ mọi khách, và mỗi khách một scope riêng.
        return await services.GetRequiredService<Tenancy.TenantDirectory>().LietKeIdAsync(ct);
    }
}
