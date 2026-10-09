using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Redaction;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Bản tri thức mà token của request này mở. Scoped, đặt bởi <see cref="LinkTokenEndpointFilter"/>.
/// </summary>
public sealed class LinkTokenContext
{
    public Guid? KnowledgeRecordId { get; private set; }

    /// <summary>Lượt gợi ý đã phát link này, nếu có (P6) — để ghi nấc OPENED.</summary>
    public Guid? LuotGoiYId { get; private set; }

    internal void Dat(Guid recordId, Guid? luotGoiYId = null)
    {
        KnowledgeRecordId = recordId;
        LuotGoiYId = luotGoiYId;
    }
}

/// <summary>
/// Đổi token trong đường dẫn thành tenant + bản tri thức. Cổng xác thực của đường
/// <c>GET /xem/{token}</c>.
///
/// ⚠ **Vì sao là endpoint filter chứ không phải middleware**, khác với khoá API: token nằm
/// trong ĐƯỜNG DẪN, không trong header. Middleware chạy trước routing nên chưa biết giá trị
/// route — muốn đọc thì phải tự bóc chuỗi đường dẫn, tức là chép lại luật routing bằng tay.
/// Filter chạy sau routing và trước handler, đúng chỗ cần. Interceptor của <c>AppDbContext</c>
/// đọc <c>ITenantContext</c> lúc MỞ connection, việc xảy ra bên trong handler — nên resolve ở
/// đây vẫn kịp.
/// </summary>
public sealed class LinkTokenEndpointFilter(
    LinkTokenDirectory directory,
    RequestTenantContext tenantContext,
    LinkTokenContext linkContext,
    TimeProvider clock) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var token = context.HttpContext.Request.RouteValues["token"] as string;
        var mo = await directory.MoAsync(token, clock.GetUtcNow(), context.HttpContext.RequestAborted);

        if (mo is null)
        {
            // 404 cho MỌI kiểu hỏng — sai, hết hạn, đã thu hồi, không tồn tại. Phân biệt
            // chúng là nói cho người cầm link biết token đó TỪNG có thật, đủ để dò xem một
            // bản tri thức có tồn tại hay không.
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Link này không mở được",
                detail: "Link sai, đã hết hạn, hoặc đã bị thu hồi. Xin người gửi phát lại link mới.");
        }

        tenantContext.Resolve(mo.TenantId);
        linkContext.Dat(mo.KnowledgeRecordId, mo.LuotGoiYId);

        return await next(context);
    }
}

/// <summary>
/// Đọc một bản tri thức bằng LINK (`D5-3`), và đây là **đường ra thứ hai** của sản phẩm.
///
/// 🛑 **Cổng che `AR-o` phải canh đường này, không chỉ đường gọi model.** Thêm một cửa ra mà
/// quên cổng che là đúng hình dạng lỗi mà `IM-25` sinh ra để chặn — và cửa này nguy hơn cửa
/// kia ở một điểm: người cầm link có thể chuyển tiếp nó, còn payload gửi model thì không.
///
/// 🛑 **Chỉ bản ĐÃ DUYỆT mới phát link được** (xem <see cref="PhatLinkHandler"/>), nhưng
/// đường ĐỌC vẫn kiểm lại: một bản đã duyệt có thể bị sửa và rơi về <c>NeedsReview</c> sau
/// khi link đã phát. Kiểm ở một chỗ rồi tin mãi là cách một cổng đúng trở thành cổng hở.
/// </summary>
public sealed class XemLinkHandler(
    AppDbContext db, LinkTokenContext linkContext, Startup.CongChe congChe, GoiY.GoiYHandler goiY)
{
    /// <summary>
    /// Mở một bản tri thức bằng link — và ghi nấc OPENED vào lượt gợi ý đã phát link, nếu có (ô `J`, P6).
    /// Ghi CẢ lần mở không được (<c>DatDuoc = false</c>): một người bấm link rồi gặp "đang xem lại" cũng là dữ kiện.
    /// </summary>
    public async Task<XemKetQua> XemAsync(CancellationToken ct = default)
    {
        var kq = await MoAsync(ct);

        if (linkContext.LuotGoiYId is { } luot)
        {
            await goiY.GhiMoLinkAsync(luot, kq.MoDuoc, ct);
        }

        return kq;
    }

    private async Task<XemKetQua> MoAsync(CancellationToken ct)
    {
        var id = linkContext.KnowledgeRecordId
                 ?? throw new InvalidOperationException("Chưa có token nào được mở cho request này.");

        var record = await db.KnowledgeRecords
            .KemDanChung()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        // RLS đã chặn record của tenant khác, nên tới đây null nghĩa là đã bị xoá.
        if (record is null) return XemKetQua.KhongMoDuoc("Bản tri thức này không còn nữa.");

        if (record.Lifecycle != StoredLifecycleState.Active)
        {
            // Đã phát link rồi mới rơi khỏi Active — ví dụ có người sửa và nó thành
            // NEEDS_REVIEW. Nói thẳng thay vì trả nội dung chưa ai soát lại.
            return XemKetQua.KhongMoDuoc(
                "Bản tri thức này đang được xem lại nên tạm thời không mở được. Hỏi lại sau khi nó được duyệt.");
        }

        // Ô `I` (P5): mỗi phát biểu kèm dẫn chứng TRỎ VỀ NGUỒN — khoá nguồn, không nội dung mẩu.
        var danChung = await DanChungDauRa.ChuanBiAsync(db, [record], ct);

        try
        {
            return new XemKetQua(
                MoDuoc: true,
                LyDo: null,
                Id: record.Id,
                TenNguyenNhan: Che(record.CauseName),
                TrangThai: record.DisplayState,
                PhamViXem: record.VisibilityScope?.ToWire(),
                CachNhanRa: Che(record, AssertionKind.Recognition, danChung),
                CachXuLy: Che(record, AssertionKind.Handling, danChung),
                DieuKienApDung: Che(record, AssertionKind.Applicability, danChung),
                DaQuaCongChe: congChe.DaQuaCongChe);
        }
        catch (EgressBlockedException e)
        {
            // Fail closed. `AR-o` chọn CHẶN chứ không hỏi ở ranh giới gửi ra, và một trang
            // báo lỗi đọc được vẫn tốt hơn nhiều so với 500 — người đọc biết phải làm gì.
            return XemKetQua.KhongMoDuoc(
                $"Nội dung này chưa gửi ra được: cổng che ở khâu gửi ra vẫn bắt được thứ nghi là bí mật " +
                $"sau khi đã che ({e.Message}). Sửa nội dung bản tri thức rồi duyệt lại.");
        }
    }

    private string Che(string? s) => congChe.CheDuongRa(s).Text;

    private PhatBieu[] Che(KnowledgeRecord r, AssertionKind kind, Func<Assertion, IReadOnlyList<DanChungRef>> danChung) =>
    [
        .. r.Assertions
            .Where(a => a.Kind == kind && a.IsCurrent)
            .Select(a => new PhatBieu(Che(a.Content), a.Verification.ToString(), a.Origin.ToString(), danChung(a)))
    ];
}

/// <param name="MoDuoc"><c>false</c> nghĩa là có token hợp lệ nhưng nội dung không trả ra được.</param>
public sealed record XemKetQua(
    bool MoDuoc,
    string? LyDo,
    Guid Id,
    string? TenNguyenNhan,
    string? TrangThai,
    string? PhamViXem,
    PhatBieu[] CachNhanRa,
    PhatBieu[] CachXuLy,
    PhatBieu[] DieuKienApDung,
    [property: System.Text.Json.Serialization.JsonIgnore(
        Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    bool? DaQuaCongChe = null)
{
    // `DaQuaCongChe` (`N1-2`): nội dung trang này CÓ đi qua cổng che không. Chỉ có mặt khi có
    // nội dung — một trang "không mở được" không trả ra chữ nào để mà che.
    public static XemKetQua KhongMoDuoc(string lyDo) =>
        new(false, lyDo, Guid.Empty, null, null, null, [], [], []);
}

/// <summary>
/// Phát một link xem cho một bản tri thức đã duyệt.
/// </summary>
public sealed class PhatLinkHandler(
    AppDbContext db,
    LinkTokenDirectory directory,
    ITenantContext tenant,
    IOptions<LinkXemOptions> options)
{
    private readonly LinkXemOptions _options = options.Value;

    /// <param name="luotGoiYId">Lượt gợi ý phát link này (P6) — webhook truyền vào; phát tay thì bỏ trống.</param>
    public async Task<PhatLinkKetQua> PhatAsync(
        Guid recordId, string nguoiPhat, int? hanNgay, CancellationToken ct = default, Guid? luotGoiYId = null)
    {
        if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
        {
            // Từ chối thay vì đoán. Đoán ở đây nghĩa là sinh ra link `localhost` — chạy thử
            // tại chỗ thấy đúng, gửi cho người khác thì chết. Xem LinkXemOptions.PublicBaseUrl.
            return PhatLinkKetQua.TuChoi(
                "Thiếu cấu hình Link:PublicBaseUrl — địa chỉ mà máy của người đọc tới được. " +
                "Không đoán từ host của request, vì như vậy sẽ sinh ra link chỉ mở được trên chính máy chạy app.");
        }

        var han = TimeSpan.FromDays(Math.Clamp(
            hanNgay ?? _options.HanMacDinhNgay, 1, _options.HanToiDaNgay));

        var record = await db.KnowledgeRecords.FirstOrDefaultAsync(r => r.Id == recordId, ct);
        if (record is null) return PhatLinkKetQua.KhongThay();

        // 🛑 Chỉ bản ĐÃ DUYỆT. Phát link cho một bản `Draft` là đưa đầu ra thô của model cho
        // người làm thật — đúng thứ `D4` và cổng của `/hoi` sinh ra để chặn. Không có dòng
        // này thì link trở thành cửa sau đi vòng qua cả hai.
        if (record.Lifecycle != StoredLifecycleState.Active)
        {
            return PhatLinkKetQua.TuChoi(
                $"Chỉ phát link được cho bản tri thức ĐÃ DUYỆT. Bản này đang ở trạng thái " +
                $"\"{record.DisplayState}\" — duyệt trước đã.");
        }

        var (chuoi, hetHan) = await directory.PhatAsync(tenant.TenantId, recordId, nguoiPhat, han, ct, luotGoiYId);

        return new PhatLinkKetQua(
            Phat: true, LyDo: null,
            Token: chuoi,
            Url: $"{_options.PublicBaseUrl.TrimEnd('/')}/xem/{chuoi}",
            HetHanLuc: hetHan);
    }
}

public sealed record PhatLinkKetQua(bool Phat, string? LyDo, string? Token, string? Url, DateTimeOffset? HetHanLuc)
{
    public static PhatLinkKetQua TuChoi(string lyDo) => new(false, lyDo, null, null, null);

    /// <summary>Không thấy record — và câu trả lời này KHÔNG phân biệt "của khách khác" với "không có".</summary>
    public static PhatLinkKetQua KhongThay() => new(false, null, null, null, null);
}
