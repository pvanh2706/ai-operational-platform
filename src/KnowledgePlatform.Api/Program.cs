using KnowledgePlatform.Api.Documents;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Api.Signals;
using KnowledgePlatform.Api.Startup;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Api.Webhooks;
using KnowledgePlatform.Domain.Documents;
using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Documents;
using KnowledgePlatform.Infrastructure.Persistence;
using KnowledgePlatform.Infrastructure.Sop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// =============================================================================
//  Project host của sản phẩm. `06` §1: "Bề mặt là API nhận tín hiệu + một widget
//  nhúng" — service phản ứng theo SỰ KIỆN, không phải app người dùng đăng nhập vào.
//
//  Slice hiện tại chưa có đường nhận tín hiệu. Việc của host này là làm cho
//  RANH GIỚI TENANT sống được trong một request thật: trước nó, `ITenantContext`
//  chỉ có hợp đồng mà không có thân, vì không có "request" nào tồn tại.
//
//  ⚠ Không chỗ nào dưới đây đọc cấu hình TRƯỚC khi build. Cố ý: mọi giá trị đều
//    đến từ `IConfiguration` lúc chạy, nên test tích hợp ghi đè được, và bản
//    deploy dedicated với bản shared dùng đúng cùng một đường code (`G13`).
// =============================================================================

// 🛑 Mọi câu app in ra đều bằng tiếng Việt, và trên Windows khi stdout BỊ CHUYỂN HƯỚNG
//   thì .NET mã hoá bằng codepage OEM của máy — nơi `ấ`, `ệ`, `ữ` không tồn tại. Chúng bị
//   thay bằng `?` NGAY LÚC GHI, nên phía đọc không giải mã lại được: đo thật 2026-09-28,
//   "Không thấy file" ra thành "Không th?y file" với mọi codepage thử qua.
//
//   Chỗ này im lặng đúng kiểu khó chịu nhất: chạy tay trong console thì đẹp, mà log CI,
//   file redirect và công cụ gọi vào đều nhận chữ vỡ — tức là chỉ hỏng ở nơi KHÔNG ai
//   ngồi nhìn. Đặt lại encoding khi (và chỉ khi) bị chuyển hướng, để console tương tác
//   giữ nguyên hành vi cũ.
if (Console.IsOutputRedirected || Console.IsErrorRedirected)
{
    Console.OutputEncoding = new System.Text.UTF8Encoding(false);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TenancyOptions>(
    builder.Configuration.GetSection(TenancyOptions.SectionName));
builder.Services.Configure<IngestOptions>(
    builder.Configuration.GetSection(IngestOptions.SectionName));
builder.Services.Configure<DinhTuyenOptions>(
    builder.Configuration.GetSection(DinhTuyenOptions.SectionName));
builder.Services.Configure<AuthOptions>(
    builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<LinkXemOptions>(
    builder.Configuration.GetSection(LinkXemOptions.SectionName));
builder.Services.Configure<WebhookOptions>(
    builder.Configuration.GetSection(WebhookOptions.SectionName));
builder.Services.Configure<SoanNhapOptions>(
    builder.Configuration.GetSection(SoanNhapOptions.SectionName));
builder.Services.Configure<KhoFileOptions>(
    builder.Configuration.GetSection(KhoFileOptions.SectionName));
builder.Services.Configure<CongCheOptions>(
    builder.Configuration.GetSection(CongCheOptions.SectionName));

// `N1-2` — MỘT cửa cho mọi lần che. Singleton vì nó chỉ đọc một cờ cấu hình, không chạm tenant.
builder.Services.AddSingleton<CongChe>();

// Danh bạ tenant nằm NGOÀI ranh giới tenant — xem TenantDirectory.
builder.Services.AddSingleton(sp => new TenantDirectory(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")!));

// Khoá API (`AR-e`) cũng nằm NGOÀI ranh giới tenant, và vì một lý do KHÁC —
// xem ApiKeyDirectory. Chính nó quyết định tenant, nên không thể nằm sau cánh cửa đó.
builder.Services.AddSingleton(sp => new ApiKeyDirectory(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")!));

// Token xem tri thức (`D5-3`) — cũng ngoài ranh giới tenant, cùng lý do với ApiKeyDirectory:
// người bấm link không mang theo thứ gì khác, nên chính token là thứ quyết định tenant.
builder.Services.AddSingleton(sp => new LinkTokenDirectory(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")!));

// Đồng hồ tiêm được, để test kiểm ca HẾT HẠN mà không phải chờ thật.
builder.Services.AddSingleton(TimeProvider.System);

// Đặt một lần lúc khởi động bởi StartupChecks, không phải mỗi request.
builder.Services.AddSingleton<DedicatedTenant>();

// G13: cả hai chế độ deploy đi qua cùng một cài đặt ITenantContext. Chế độ nào
// thì do TenantResolutionMiddleware quyết, không phải do đường code khác nhau.
builder.Services.AddScoped<RequestTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<RequestTenantContext>());
builder.Services.AddScoped<CallerIdentity>();
builder.Services.AddScoped<TenantEndpointFilter>();
builder.Services.AddScoped<ApiKeyEndpointFilter>();
builder.Services.AddScoped<CaseSignalHandler>();
builder.Services.AddScoped<CaseStatusHandler>();
builder.Services.AddScoped<CaseEvidenceHandler>();
builder.Services.AddScoped<CaseEvidenceReadHandler>();
builder.Services.AddScoped<EvidenceRetractionHandler>();
builder.Services.AddScoped<SopDraftIngestHandler>();
builder.Services.AddScoped<KnowledgeApprovalHandler>();
builder.Services.AddScoped<KnowledgeRevisionHandler>();
builder.Services.AddScoped<HoiHandler>();
builder.Services.AddScoped<LinkTokenContext>();
builder.Services.AddScoped<LinkTokenEndpointFilter>();
builder.Services.AddScoped<PhatLinkHandler>();
builder.Services.AddScoped<XemLinkHandler>();
builder.Services.AddScoped<VongKhongCredit>();
builder.Services.AddScoped<SoanNhapHandler>();
builder.Services.AddScoped<DocumentHandler>();

// `AR5` — file gốc ở kho, DB chỉ giữ metadata. Interface MỎNG theo khuôn `AR3`.
//
// ⚠ `H3-2` chốt MinIO; bản đang cắm là ĐĨA CỤC BỘ, và lý do nói thẳng ở IKhoFile: máy dựng
//   bản này không có container runtime, nên một bản MinIO viết ra sẽ là code CHƯA TỪNG CHẠY.
builder.Services.AddSingleton<IKhoFile>(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<KhoFileOptions>>().Value;

    return new KhoFileDia(cfg.ThuMucGoc
        ?? Path.Combine(Path.GetTempPath(), "kp-kho-file"));
});

// `AR3` — MỘT DÒNG để đổi bản hiện thực, và đây là dòng đó.
//
// 🛑 Không có khoá thì cắm bản NÉM kèm hướng dẫn, KHÔNG để trống DI (host chết vì một tính
//   năng chưa dùng tới) và KHÔNG trả bản nháp rỗng (nó đi thẳng vào kho, trông như thật).
//
// ⚠ Khoá đọc ĐÚNG MỘT LẦN và đưa thẳng cho client — xem KhoaAnthropic cho lỗi đã có ở đây
//   (kiểm một tên, client đọc một tên khác).
builder.Services.AddScoped<ISopDrafter>(sp =>
    KhoaAnthropic.Doc(sp.GetRequiredService<IConfiguration>()) is { } khoaAnthropic
        ? new AnthropicSopDrafter(
            new Anthropic.AnthropicClient { ApiKey = khoaAnthropic },
            new SopDrafterOptions { QuaCongChe = sp.GetRequiredService<CongChe>().DaQuaCongChe })
        : new SopDrafterChuaCoCredit());
builder.Services.AddScoped<WebhookConfigHandler>();
builder.Services.AddScoped<WebhookSender>();

// `D1-1` — đường TRẢ KẾT QUẢ VỀ. Hàng đợi bền + vòng quét nền, xem WebhookSenderService.
builder.Services.AddHttpClient(WebhookSender.TenHttpClient);

if (builder.Configuration.GetValue($"{WebhookOptions.SectionName}:BatVongQuet", true))
{
    builder.Services.AddHostedService<WebhookSenderService>();
}

// AR3 — interface mỏng, một dòng để đổi bản hiện thực. Bản đang cắm KHÔNG gọi API và
// KHÔNG định tuyến: nó tra bảng kết quả đã đo (`docs/13`). Xem DinhTuyenTraSan.
// Singleton vì nó chỉ đọc một file một lần rồi giữ trong bộ nhớ; không có trạng thái
// theo request, và KHÔNG chạm tenant — nên nó không phải chỗ rò ranh giới.
// Bảng đã đo: đọc file MỘT LẦN, nên singleton. Nó không chạm tenant.
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<DinhTuyenOptions>>().Value;
    return new DinhTuyenTraSanNguon(
        new DinhTuyenTraSan(cfg.DuongDanBangTraSan ?? DinhTuyenOptions.DuongDanMacDinh()));
});

// Bản đang cắm: KHO KẾT QUẢ ĐÃ NẠP của khách hàng này, rơi về bảng đã đo nếu chưa có.
// SCOPED vì nó đọc dữ liệu của tenant trong request — xem DinhTuyenKho.
builder.Services.AddScoped<IDinhTuyenNhom, DinhTuyenKho>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default");

    options.UseNpgsql(connectionString)
        // MẮT XÍCH: đưa tenant của request này xuống tới policy RLS của Postgres.
        // Lấy ITenantContext từ scope của request, nên mỗi request có đúng tenant
        // của nó. Thiếu dòng này thì C# biết tenant mà Postgres không biết, và
        // mọi truy vấn trả về 0 dòng (`IM-10`).
        .AddInterceptors(new TenantConnectionInterceptor(sp.GetRequiredService<ITenantContext>()));
});

builder.Services.AddProblemDetails();

// `H4-1` — hợp đồng tích hợp SINH TỪ CODE, không viết tay. `IM-38` đã đo được rằng bản viết
// tay tự lỗi thời trong im lặng: bộ Postman đứng ở 5/13 endpoint mà không ai nhận ra.
//
// ⚠ Gói `Microsoft.AspNetCore.OpenApi` là FIRST-PARTY của .NET, không phải Swashbuckle hay
//   NSwag — nên lo ngại "thêm một dependency bên thứ ba" ghi ở `docs/18` §H4 không áp ở đây.
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<OpenApiMoTa.TrinhGan>());

var app = builder.Build();

// Lệnh vận hành của `AR-e` (tạo/thu hồi khoá): chạy rồi THOÁT, không mở cổng nghe.
// Cố ý đứng TRƯỚC StartupChecks — xem AdminCommands.
if (await AdminCommands.TryRunAsync(args, app.Services)) return;

// Thất bại nào ở đây cũng là KHÔNG KHỞI ĐỘNG ĐƯỢC, không phải cảnh báo.
await StartupChecks.RunAsync(app.Services);

app.UseMiddleware<TenantResolutionMiddleware>();

// Hợp đồng tích hợp, sinh từ chính các endpoint bên dưới: GET /openapi/v1.json
//
// ⚠ KHÔNG đòi khoá, cố ý: nó tả HÌNH DẠNG hợp đồng, không chứa dữ liệu khách hàng nào. Mọi
//   endpoint nó liệt kê vẫn trả 401 nếu gọi không khoá. Giấu tài liệu đi không làm hệ thống
//   an toàn hơn — nó chỉ làm bên tích hợp phải đoán.
app.MapOpenApi();

// --- Liveness: không chạm database, không cần tenant ---
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// --- Readiness: database sống VÀ ranh giới tenant còn nguyên ---
// Tách khỏi /health vì hai câu hỏi khác nhau: "tiến trình còn sống" và "phục vụ
// được chưa". StartupChecks đã kiểm một lần lúc start; cái này để phát hiện nếu
// ai đó tắt RLS trên database ĐANG CHẠY.
app.MapGet("/health/ready", async (AppDbContext db, CancellationToken ct) =>
{
    try
    {
        // ⚠ CỐ Ý dùng phép kiểm NÔNG ở đây, khác với StartupChecks (dùng phép sâu).
        //
        // Hai chỗ hỏi hai câu khác nhau. Readiness hỏi "tôi phục vụ được không", và nó
        // phải trả lời về những bảng tiến trình NÀY thật sự đọc ghi. Phép kiểm sâu còn
        // đòi "không relation lạ nào trong schema" — đúng cho lúc khởi động, nhưng ở
        // readiness nó tạo một đường MẤT DỊCH VỤ: deploy cuốn chiếu, bản N+1 chạy
        // migration tạo bảng mới, các tiến trình bản N cũ không biết bảng đó và đồng
        // loạt trả 503 — rút cả đội đang khoẻ ra khỏi luồng vì một bảng chúng không đụng.
        //
        // Nới ở đây KHÔNG mở lỗ: chiều "quên khai entity mới" vẫn bị chặn ở startup,
        // là nơi bản build và schema được nhìn cùng một lúc.
        await RlsGuard.VerifyAsync(db, RlsScanDepth.DeclaredTablesOnly, ct);
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Chưa phục vụ được",
            detail: ex.Message);
    }
});

// --- Ranh giới tenant, đo từ bên trong một request thật ---
//
// Đây là endpoint HẠ TẦNG, không phải bề mặt sản phẩm (G11: không tự phỏng to
// capability đã chốt). Nó trả lời đúng một câu hỏi vận hành: "trên bản deploy
// NÀY, ranh giới tenant có đang sống không?"
//
// Con số quan trọng là `rowsVisibleWithoutTenantFilter`: một câu SQL thô CỐ Ý
// không có điều kiện tenant. Nếu RLS đang làm việc, nó chỉ đếm được dữ liệu của
// khách hàng gọi request này. Hai khách hàng gọi cùng endpoint này phải thấy hai
// con số khác nhau — và đó là toàn bộ lời hứa của AR2, đo được bằng một lệnh curl.
app.MapGet("/internal/tenant-boundary", async (
        AppDbContext db,
        ITenantContext tenant,
        IOptions<TenancyOptions> tenancy,
        CancellationToken ct) =>
    {
        var cases = await db.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM kp.canonical_case")
            .FirstAsync(ct);

        var knowledge = await db.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM kp.knowledge_record")
            .FirstAsync(ct);

        // Thêm 2026-08-30 cùng với đường nạp evidence (`AR-f`). evidence_item cũng là
        // bảng tenant-scoped, nên nó cũng phải đo được bằng chính phép đo này — và nếu
        // không đo thì lời hứa "lô bị từ chối thì KHÔNG ghi gì" của đường evidence
        // không có cách nào kiểm từ ngoài, khác hẳn với case.
        var evidence = await db.Database
            .SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM kp.evidence_item")
            .FirstAsync(ct);

        return Results.Ok(new
        {
            mode = tenancy.Value.Mode.ToString(),
            tenantId = tenant.TenantId,
            rowsVisibleWithoutTenantFilter = new { cases, evidence, knowledge },
        });
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

// --- KÊNH 1: đường nhận tín hiệu từ phần mềm có sẵn của khách (06 §1) ---
//
// Response dừng ở ô "Tìm hoặc tạo Case" của sơ đồ luồng, và cố ý không có chỗ nào
// trông như thể các ô sau đã có (G11). Khớp quy trình đã duyệt và suy ra bước hiện tại
// vẫn CHƯA BUILD. Tra tri thức + trả gợi ý thì CÓ, nhưng đi đường RIÊNG: case mới được
// xếp hàng gửi kết quả về (D1-1, xem CaseSignalHandler) — không nằm trong response này.
//
// Nhận một MẢNG tín hiệu, không phải một tín hiệu. Lô một phần tử là ca thường
// gặp; lô lớn là đường nạp case lịch sử. Một đường code cho cả hai — xem
// CaseSignalHandler.
//
// Thứ tự filter quan trọng: xác thực TRƯỚC khi tra tenant, để người gọi không có
// khoá cũng không dò được khoá tenant nào tồn tại.
app.MapPost("/signals/case-observed", async (
        List<CaseObservedSignal> signals,
        CaseSignalHandler handler,
        IOptions<IngestOptions> ingest,
        CancellationToken ct) =>
    {
        if (signals.Count == 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Không có tín hiệu nào",
                detail: "Body phải là một mảng có ít nhất một tín hiệu.");
        }

        var max = ingest.Value.MaxSignalsPerRequest;
        if (signals.Count > max)
        {
            // Từ chối cả lô, KHÔNG cắt bớt. Cắt bớt im lặng đọc ra thành "đã nạp
            // hết" trong khi không phải — đúng loại thất bại im lặng của dự án này.
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Lô tín hiệu quá lớn",
                detail: $"Nhận {signals.Count} tín hiệu, trần là {max}. Chia nhỏ rồi gửi lại — " +
                        "cố ý không xử lý một phần, để bạn không tưởng đã nạp hết.");
        }

        var invalid = signals
            .Select((s, i) => (Index: i, Error: Validate(s)))
            .Where(x => x.Error is not null)
            .ToList();

        if (invalid.Count > 0)
        {
            return Results.ValidationProblem(
                invalid.ToDictionary(x => $"[{x.Index}]", x => new[] { x.Error! }),
                title: "Tín hiệu không hợp lệ");
        }

        return Results.Ok(await handler.HandleAsync(signals, ct));
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

// --- KÊNH 1, tín hiệu loại THỨ HAI: việc ở nguồn đổi trạng thái (F1-1) ---
//
// 🛑 Nó CỐ Ý không làm gì ngoài cập nhật Case, và lý do là một SỐ ĐO chứ không phải trực
//   giác: corpus 12 tháng có 100% case đã đóng mà chỉ 1,6 mẩu dùng được mỗi case, mỏng hơn
//   corpus 4 ngày (2,5) vốn chỉ 22% đã đóng. Kích hoạt gom tri thức lúc đóng case là build
//   đúng bộ lọc mà dự án đã ĐO được là vứt đi phần có giá trị. Xem CaseStatusSignal.
app.MapPost("/signals/case-status", async (
        List<CaseStatusSignal> signals,
        CaseStatusHandler handler,
        IOptions<IngestOptions> ingest,
        CancellationToken ct) =>
    {
        if (signals.Count == 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Khong co tin hieu nao",
                detail: "Body phai la mot mang co it nhat mot tin hieu.");
        }

        var max = ingest.Value.MaxSignalsPerRequest;
        if (signals.Count > max)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Lo tin hieu qua lon",
                detail: $"Nhan {signals.Count} tin hieu, tran la {max}. Chia nho roi gui lai.");
        }

        var invalid = signals
            .Select((s, i) => (Index: i, Error: ValidateStatus(s)))
            .Where(x => x.Error is not null)
            .ToList();

        if (invalid.Count > 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Co tin hieu khong hop le",
                detail: string.Join("; ", invalid.Select(x => $"[{x.Index}] {x.Error}")));
        }

        return Results.Ok(await handler.HandleAsync(signals, ct));
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();


// --- KÊNH 1, cửa thứ hai: NỘI DUNG của case (AR-f, chốt 2026-08-30) ---
//
// Trước đường này, một case là MỘT DÒNG CHỮ và Path A không có gì để gom. Xem
// CaseEvidenceHandler để biết vì sao ô này phải có trước truy vấn "tìm N case cũ".
//
// Vì sao endpoint RIÊNG chứ không lồng vào /signals/case-observed: K-B9 — evidence
// được phép không thuộc case nào. Lồng vào thì loại đó vĩnh viễn không có đường vào.
// Xem CaseEvidenceSignal.
//
// Thứ tự kiểm tra ở đây quan trọng và cố ý giống nhóm C của bộ test API: hình dạng
// từng phần tử TRƯỚC, rồi mới tra case, và cả hai đều xong TRƯỚC khi ghi dòng đầu
// tiên. Lô nhắc tới một case không tồn tại bị từ chối TRỌN VẸN, không ghi một nửa.
app.MapPost("/signals/case-evidence", async (
        List<CaseEvidenceSignal> signals,
        CaseEvidenceHandler handler,
        IOptions<IngestOptions> ingest,
        CancellationToken ct) =>
    {
        if (signals.Count == 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Không có evidence nào",
                detail: "Body phải là một mảng có ít nhất một mẩu evidence.");
        }

        var max = ingest.Value.MaxEvidencePerRequest;
        if (signals.Count > max)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Lô evidence quá lớn",
                detail: $"Nhận {signals.Count} mẩu, trần là {max}. Chia nhỏ rồi gửi lại — " +
                        "cố ý không xử lý một phần, để bạn không tưởng đã nạp hết.");
        }

        var invalid = signals
            .Select((s, i) => (Index: i, Error: ValidateEvidence(s)))
            .Where(x => x.Error is not null)
            .ToList();

        if (invalid.Count > 0)
        {
            return Results.ValidationProblem(
                invalid.ToDictionary(x => $"[{x.Index}]", x => new[] { x.Error! }),
                title: "Evidence không hợp lệ");
        }

        // Case được nhắc tới phải tồn tại. Ba cách xử lý khi thiếu, hai cách sai:
        //   tự tạo case rỗng thay thế  → sinh case không subject, trái luật của Kênh 1
        //   nhận rồi để link NULL      → evidence trôi lơ lửng, IM LẶNG   ← cấm
        //   từ chối, nói rõ thiếu cái nào                                 ← chọn cái này
        //
        // Nghe như bắt bên gửi phải xếp thứ tự, nhưng không: /signals/case-observed
        // idempotent, nên cứ gửi case trước mỗi lần, lần hai trả created 0 và vô hại.
        // Gánh nặng thật chỉ là "gọi hai lần", KHÔNG phải "tự nhớ đã gửi gì" — đúng
        // thứ IM-15 nói tích hợp hệ thống không làm được.
        var named = signals
            .Select((s, i) => (Index: i, Reference: s.CaseSourceReference))
            .Where(x => !string.IsNullOrWhiteSpace(x.Reference))
            .ToList();

        var caseIds = await handler.FindCaseIdsAsync(
            named.Select(x => x.Reference!).Distinct().ToList(), ct);

        var missing = named.Where(x => !caseIds.ContainsKey(x.Reference!)).ToList();
        if (missing.Count > 0)
        {
            return Results.ValidationProblem(
                missing.ToDictionary(
                    x => $"[{x.Index}]",
                    x => new[]
                    {
                        $"Không có Case nào mang khoá nguồn \"{x.Reference}\". Gửi tín hiệu case " +
                        "vào /signals/case-observed trước — endpoint đó gửi lại được vô hại.",
                    }),
                title: "Case được tham chiếu không tồn tại");
        }

        return Results.Ok(await handler.HandleAsync(signals, caseIds, ct));
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

// =============================================================================
//  VÒNG TRI THỨC — mảnh (a)(b)(c)(d) của phạm vi demo chốt 2026-09-13.
//
//  Trước ba endpoint dưới đây, host chỉ có đường NẠP: tín hiệu đi vào được, nhưng
//  không có cửa nào cho một CÂU HỎI, và `KnowledgeRecord.Approve()` — có ở Domain
//  từ 2026-08-24 — KHÔNG NƠI NÀO gọi tới. Kho tri thức vì thế rỗng theo đúng nghĩa
//  cấu trúc: không phải chưa ai nạp, mà là không có đường nào để nạp.
//
//  ⚠ ĐỌC `docs/00` §"Việc tiếp theo" (2026-09-13) trước khi đọc một demo xanh thành
//    "tính năng đã dùng được". Bộ định tuyến đang cắm là bản TRA SẴN: nó chứng minh
//    ĐƯỜNG ỐNG chạy, KHÔNG chứng minh độ chính xác.
// =============================================================================

// --- (a) Nạp một bản nháp SOP vào kho, ở trạng thái Draft ---
//
// Endpoint GHI, nên cùng cặp filter với đường tín hiệu. Nó KHÔNG duyệt gì: `D4` nói
// người công nhận chứ không phải AI, và "nạp" không phải "công nhận".
app.MapPost("/knowledge/sop-draft", async (
        SopDraft draft,
        SopDraftIngestHandler handler,
        CancellationToken ct) =>
    {
        // 🛑 Kiểm CẢ BÊN TRONG, không chỉ hai trường ngoài cùng.
        //
        // `SopDraft` khai `required` gần hết, nhưng `required` chỉ ràng buộc mã C# KHỞI TẠO
        // nó — một gói JSON thiếu trường (hoặc gõ SAI TÊN trường) vẫn dựng được object với
        // `null` nằm bên trong, rồi nổ bằng NullReferenceException ở tận tầng chuyển đổi:
        // **500, không phải 400**. Đã xảy ra thật với `dieuKien` gõ thay cho `quanSat`.
        //
        // ⚠ Bộ test C# không bao giờ thấy ca này: nó luôn dựng SopDraft bằng object
        //   initializer, nơi trình biên dịch ép đủ trường. Chỉ payload VIẾT TAY — đúng thứ
        //   bên tích hợp gửi — mới tạo ra được. Xem SopDraftKiemTra.
        if (SopDraftKiemTra.KiemTra(draft) is { } choSai)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bản nháp không dùng được",
                detail: choSai);
        }

        return Results.Ok(await handler.HandleAsync(draft, ct));
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

// --- SOẠN NHÁP từ trong host: gom case của một nhóm -> gọi model -> nạp (bước 4) ---
//
// 🎯 Nhóm lấy từ KHO ĐỊNH TUYẾN (`IM-45`), không từ file taxonomy: vòng không-credit của
//   bước 3 nạp "ticket nào thuộc nhóm nào", vòng này đọc chính nó ra. Hai vòng nối vào nhau.
//
// 🛑 Chưa có credit thì nó KHÔNG hỏng im lặng — trả 409 kèm đúng ba bước của vòng thay thế.
app.MapPost("/knowledge/soan-nhap", async (
        YeuCauSoanNhap body,
        SoanNhapHandler handler,
        CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(body?.Nhom) || string.IsNullOrWhiteSpace(body.NguoiYeuCau))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thiếu nhom hoặc nguoiYeuCau",
                detail: "`nguoiYeuCau` là LỜI KHAI của người bấm nút tốn tiền (E3-1) — sản phẩm " +
                        "hôm nay biết request thuộc công ty nào, chưa biết người nào.");
        }

        var kq = await handler.SoanAsync(body.Nhom, body.NguoiYeuCau, ct);

        return kq.Thanh
            ? Results.Ok(kq)
            : Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: kq.LyDo switch
                {
                    "chua-co-credit" => "Chưa gọi model được",
                    "vuot-tran-thang" => "Đã chạm trần tháng",
                    "nhom-khong-co-case" => "Nhóm chưa có case nào",
                    _ => "Không soạn nháp được",
                },
                detail: kq.GiaiThich,
                extensions: new Dictionary<string, object?> { ["lyDo"] = kq.LyDo });
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();


// --- (b) Duyệt: nội dung + quyền xem trong MỘT hành động (S7) ---
//
// 🛑 Đây là chỗ `AR-l` chạm vào, và cổng nằm trong handler chứ không phải trong tài
//   liệu: chế độ shared thì TỪ CHỐI. Duyệt LÀ xuất bản, và rò của `AR-l` xảy ra ở
//   khâu xuất bản chứ không ở khâu truy vấn hàng — RLS không cứu được.
app.MapPost("/knowledge/{id:guid}/approve", async (
        Guid id,
        YeuCauDuyet yeuCau,
        KnowledgeApprovalHandler handler,
        CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(yeuCau.NguoiDuyet))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thiếu người duyệt",
                detail: "`nguoiDuyet` bắt buộc. D4: người công nhận, không phải AI — một lần duyệt " +
                        "không có chủ thể thì không tra lại được về sau.");
        }

        if (string.IsNullOrWhiteSpace(yeuCau.PhamViXem))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thiếu phạm vi xem",
                detail: "`phamViXem` bắt buộc. S7: duyệt nội dung và mở quyền xem là MỘT hành động — " +
                        "hệ thống KHÔNG BAO GIỜ tự chọn phạm vi hộ.");
        }

        // TẬP GIÁ TRỊ ĐÃ KHOÁ 2026-09-15 (`AR-a`). Trước đó đây nhận chuỗi tự do, và chính vì
        // thế `kp_dev` có 7 record mang một nhãn không quyết định nào đứng sau.
        // ⚠ Báo ra CẢ DANH SÁCH nhận được, đừng chỉ nói "không hợp lệ": bên tích hợp không đọc
        //   được code của mình, nên một lỗi không nói tập giá trị là bắt họ đoán.
        if (!VisibilityScopes.TryParse(yeuCau.PhamViXem, out var phamViXem))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Phạm vi xem không nằm trong tập đã chốt",
                detail: $"Không nhận ra \"{yeuCau.PhamViXem}\". Nhận: {VisibilityScopes.Allowed}.");
        }

        // 🛑 AR-n (B2b-1) — CỔNG DỮ LIỆU THƯƠNG MẠI, và nó đứng ở đây chứ không ở khâu nạp.
        //
        // Lý do chọn khâu DUYỆT: "vào kho" trong AR-n là vào KHO TRI THỨC, không phải vào
        // bảng evidence. Duyệt LÀ xuất bản — đó là lúc một mẩu thôi nằm trong dữ liệu thô và
        // bắt đầu hiện ra cho người khác qua một bản SOP. Che ở khâu nạp thì không xong được
        // (loại dữ liệu này không có hình dạng), còn gác ở tầng mẩu thì bắt người soát cả
        // những mẩu không bao giờ được dùng tới.
        //
        // ⚠ Đây là LỜI KHAI, KHÔNG phải phép kiểm. Hệ thống không đối chiếu được. Ghi thẳng ra
        //   đây để không ai đọc ô `true` thành "đã kiểm".
        //   (Câu cũ ở đây nói `assertion_evidence` có 0 dòng. Từ P3, 2026-10-10, bản nháp nạp MỚI
        //   có liên kết, nên GET /knowledge/{id} chỉ ra được mẩu nguồn để người duyệt soát — nhưng
        //   lời khai VẪN là lời khai.)
        if (yeuCau.XacNhanDuLieuThuongMai != true)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Chưa xác nhận đã soát dữ liệu thương mại",
                detail: "`xacNhanDuLieuThuongMai` phải là true. AR-n: duyệt là XUẤT BẢN, và dữ liệu " +
                        "thương mại của khách (giá hợp đồng theo năm, hạn hợp đồng, ý định ngừng dùng) " +
                        "KHÔNG có hình dạng nào để bắt bằng luật — ca thật: ES-346608#comment-803265. " +
                        "Không có luật thì phải có người, nên chốt duy nhất là bạn khai đã soát.");
        }

        var kq = await handler.DuyetAsync(
            id, yeuCau.NguoiDuyet, phamViXem, yeuCau.XacNhanDuLieuThuongMai.Value, yeuCau.LyDo, ct);

        if (kq.LyDoChan is not null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Chưa được phép duyệt trên bản deploy này",
                detail: kq.LyDoChan);
        }

        return kq.ThayBanGhi
            ? Results.Ok(kq.BanGhi)
            : Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không thấy tri thức này",
                detail: $"Không có bản ghi {id} trong phạm vi của khách hàng này.");
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

// --- (c)+(d) Hỏi: một ticket vừa đến thì làm gì — và cổng "KHÔNG BIẾT" ---
//
// ⚠ Dùng chung cái chốt của đường tín hiệu, dù đây là endpoint ĐỌC. Lý do: nó trả ra
//   NỘI DUNG tri thức của khách, khác hẳn `/internal/tenant-boundary` (chỉ trả con
//   số đếm). `AR-e` vẫn là câu trả lời thật; đây chỉ là cái chốt duy nhất đang có.
//
// ⚠ Trả 200 cho cả hai kết cục, gồm "KHÔNG BIẾT". Cố ý: không biết là một CÂU TRẢ LỜI
//   hợp lệ của sản phẩm này, không phải một lỗi. Trả 404 ở đây sẽ đẩy người tích hợp
//   vào chỗ coi nó là sự cố rồi thử lại — trong khi thử lại không đổi được gì.
app.MapPost("/hoi", async (
        CauHoi cauHoi,
        HoiHandler handler,
        CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(cauHoi.MaTicket)
            && string.IsNullOrWhiteSpace(cauHoi.TieuDe)
            && string.IsNullOrWhiteSpace(cauHoi.MoTa))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Câu hỏi rỗng",
                detail: "Cần ít nhất một trong `maTicket`, `tieuDe`, `moTa`.");
        }

        return Results.Ok(await handler.HoiAsync(cauHoi, ct));
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();



// --- BẢN B: người làm support SỬA lại, mà không đè bản A ---
//
// 🛑 Vế còn thiếu của `M2`. A (máy sinh) đã vào kho được từ /knowledge/sop-draft, nhưng
//   tới 2026-09-13 B chưa có đường nào để vào — nên `M2` không đo được gì dù cả hai nửa
//   đều đã thiết kế xong từ lâu.
//
// Đường RIÊNG, cố ý không gộp vào /approve: `S7` nói duyệt là MỞ QUYỀN XEM, còn đây là
// ĐỔI NỘI DUNG. Gộp lại thì không phân biệt được "đồng ý nguyên văn" với "sửa rồi mới
// đồng ý" — mà đúng khoảng cách giữa hai ca đó LÀ con số M2 muốn đo.
app.MapPost("/knowledge/{id:guid}/sua", async (
        Guid id,
        YeuCauSua yeuCau,
        KnowledgeRevisionHandler handler,
        CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(yeuCau.NguoiSua))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thiếu người sửa",
                detail: "`nguoiSua` bắt buộc — một bản sửa không có chủ thể thì M2 không tra ngược được.");
        }

        if (yeuCau.Sua.Count == 0)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Không có mục sửa nào",
                detail: "Gửi lô rỗng rồi nhận 200 sẽ đọc ra thành \"đã ghi nhận\" trong khi không có gì được ghi.");
        }

        var kq = await handler.SuaAsync(id, yeuCau, ct);

        if (kq.Loi is not null)
        {
            // Cả lô bị từ chối, KHÔNG sửa một nửa — cùng luật với IM-16.
            return Results.ValidationProblem(kq.Loi, title: "Lô sửa không hợp lệ, KHÔNG mục nào được ghi");
        }

        return kq.ThayBanGhi
            ? Results.Ok(kq.BanGhi)
            : Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không thấy tri thức này",
                detail: $"Không có bản ghi {id} trong phạm vi của khách hàng này.");
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

// --- Đọc MỘT tri thức kèm TOÀN BỘ lịch sử assertion — chỗ diff(A,B) đọc được ---
//
// ⚠ /hoi cố ý chỉ trả bản CÒN HIỆU LỰC: người đang xử lý ticket không cần lịch sử. Nên
//   nếu thiếu đường này thì bản A vẫn nằm trong database mà KHÔNG CÓ CÁCH NÀO lấy ra —
//   `M2` vẫn không đo được dù dữ liệu đã đủ. Đó là một cách hỏng rất im lặng: dữ liệu
//   đúng, đường đọc thiếu.
app.MapGet("/knowledge/{id:guid}", async (
        Guid id,
        KnowledgeRevisionHandler handler,
        CancellationToken ct) =>
    {
        var kq = await handler.DocAsync(id, ct);

        return kq is not null
            ? Results.Ok(kq)
            : Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không thấy tri thức này",
                detail: $"Không có bản ghi {id} trong phạm vi của khách hàng này.");
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();


// --- PHÁT một link xem cho bản tri thức đã duyệt (D5-3) ---
//
// `D3-1` chốt lượt trả về mang **tóm tắt + link**. Đây là chỗ sinh ra nửa "link" đó.
//
// 🛑 Chỉ bản ĐÃ DUYỆT. Phát link cho bản `Draft` là đưa đầu ra thô của model cho người làm
//   thật — cửa sau đi vòng qua đúng hai cổng mà `/hoi` đang giữ.
app.MapPost("/knowledge/{id:guid}/link", async (
        Guid id,
        PhatLinkYeuCau? body,
        PhatLinkHandler handler,
        CancellationToken ct) =>
    {
        var nguoiPhat = body?.NguoiPhat;
        if (string.IsNullOrWhiteSpace(nguoiPhat))
        {
            // Cùng tinh thần `AR-m`/`B1-1`: dấu vết do NGƯỜI để lại thì phải có tên người.
            // Tự điền hộ "hệ thống" là xoá đúng thứ trả lời được câu "link rò ra kia do ai phát".
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thiếu nguoiPhat",
                detail: "Phải khai ai phát link này — để trả lời được câu \"link rò ra kia do ai phát\".");
        }

        var kq = await handler.PhatAsync(id, nguoiPhat, body?.HanNgay, ct);

        if (kq.Phat) return Results.Ok(kq);

        return kq.LyDo is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không thấy tri thức này",
                detail: $"Không có bản ghi {id} trong phạm vi của khách hàng này.")
            : Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Chưa phát link được",
                detail: kq.LyDo);
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();


// --- XEM bằng link (D5-3) — đường ra THỨ HAI của sản phẩm ---
//
// ⚠ KHÔNG có ApiKeyEndpointFilter, và đó là chủ đích: chính token LÀ giấy thông hành. Đây
//   cũng là endpoint duy nhất lấy tenant từ ĐƯỜNG DẪN thay vì từ header — xem
//   LinkTokenEndpointFilter cho lý do nó là filter chứ không phải middleware.
//
// 🛑 Nội dung đi qua cổng che `AR-o` trước khi ra. Thêm cửa ra mà quên cổng che là đúng
//   hình dạng lỗi `IM-25` sinh ra để chặn.
app.MapGet("/xem/{token}", async (XemLinkHandler handler, CancellationToken ct) =>
    {
        var kq = await handler.XemAsync(ct);

        return kq.MoDuoc
            ? Results.Ok(kq)
            : Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Link đúng nhưng nội dung chưa mở được",
                detail: kq.LyDo);
    })
    .AddEndpointFilter<LinkTokenEndpointFilter>();


// --- KÊNH 2: nạp tài liệu (AR5) ---
//
// 🛑 NẠP TÀI LIỆU KHÔNG SINH RA TRI THỨC (`S6`). Đây là lý do Kênh 2 hoãn suốt: build xong
//   đường này thì kho tri thức VẪN LÀ 0. Tri thức chỉ ra đời khi có hành vi khẳng định của
//   một NGƯỜI — đường đó là /knowledge/{id}/approve. Có test giữ riêng điều này.
app.MapPost("/documents", async (
        HttpRequest req,
        DocumentHandler handler,
        IOptions<KhoFileOptions> kho,
        CancellationToken ct) =>
    {
        if (!req.HasFormContentType)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Phải gửi dạng multipart/form-data",
                detail: "Trường `file` là nội dung; `subTenantCode` và `nguoiNap` là hai trường chữ.");
        }

        var form = await req.ReadFormAsync(ct);
        var file = form.Files["file"];

        if (file is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thiếu trường `file`",
                detail: "Một request nạp tài liệu phải mang đúng một file ở trường tên `file`.");
        }

        var subTenantCode = form["subTenantCode"].ToString();
        var nguoiNap = form["nguoiNap"].ToString();

        var sai = DocumentHandler.KiemTra(file.FileName, subTenantCode, nguoiNap, file.Length, kho.Value.TranMB);
        if (sai is not null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest, title: "Không nạp được", detail: sai);
        }

        await using var noi = file.OpenReadStream();

        var kq = await handler.NapAsync(
            file.FileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            subTenantCode, nguoiNap, noi, ct);

        return Results.Ok(kq);
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

app.MapGet("/documents", async (DocumentHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.LietKeAsync(ct)))
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

app.MapGet("/documents/{id:guid}/tai", async (Guid id, DocumentHandler handler, CancellationToken ct) =>
    {
        var tl = await handler.TaiAsync(id, ct);

        return tl is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không thấy tài liệu này",
                detail: $"Không có tài liệu {id} trong phạm vi của khách hàng này, hoặc nội dung " +
                        "của nó không còn trong kho.")
            : Results.File(tl.Value.NoiDung, tl.Value.Meta.LoaiNoiDung, tl.Value.Meta.TenKhaiBao);
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();


// --- Khai báo ĐÍCH GỬI KẾT QUẢ VỀ (D1-1) ---
//
// ⚠ Bí mật ký chỉ hiện ra ĐÚNG MỘT LẦN, lúc tạo mới — cùng luật với khoá API. Đổi địa chỉ
//   thì KHÔNG đổi bí mật: bên nhận đang kiểm chữ ký bằng bí mật cũ.
app.MapPut("/internal/webhook", async (
        KhaiBaoWebhook body,
        WebhookConfigHandler handler,
        CancellationToken ct) =>
    {
        var kq = await handler.KhaiBaoAsync(body?.Url ?? string.Empty, ct);

        return kq is not null
            ? Results.Ok(kq)
            : Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "URL không dùng được",
                detail: "Phải là một địa chỉ http/https tuyệt đối.");
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

app.MapGet("/internal/webhook", async (WebhookConfigHandler handler, CancellationToken ct) =>
    {
        var kq = await handler.DocAsync(ct);

        return kq is not null
            ? Results.Ok(kq)
            : Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Khách hàng này chưa khai báo đích gửi kết quả về",
                detail: "PUT /internal/webhook để khai báo.");
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();


// --- ĐỌC evidence của một case (AR-g, chốt 2026-09-15) ---
//
// Tra theo KHOÁ NGUỒN — thứ bên gửi đã biết về case của chính họ. Xem CaseEvidenceReadHandler.
//
// ⚠ `{ref}` là MỘT đoạn đường dẫn, nên khoá nguồn chứa dấu `/` (ví dụ `crm:deal/4471`) phải
//   được mã hoá thành `%2F`. Hợp đồng khoá nguồn cố ý là CHUỖI TỰ DO (`G1`), nên ràng buộc
//   này là của HTTP chứ không phải của sản phẩm — và nó được ghim bằng một test riêng thay
//   vì để bên tích hợp tự phát hiện bằng một cái 404 khó hiểu.
app.MapGet("/cases/{khoaNguon}/evidence", async (
        string khoaNguon,
        CaseEvidenceReadHandler handler,
        CancellationToken ct) =>
    {
        var kq = await handler.DocAsync(khoaNguon, ct);

        return kq is not null
            ? Results.Ok(kq)
            : Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không thấy case nào mang khoá nguồn này",
                detail: $"Không có Case nào mang khoá \"{khoaNguon}\" trong phạm vi của khách hàng này. " +
                        "Khoá nguồn có dấu `/` thì phải mã hoá thành %2F.");
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

// --- AR-m: đánh dấu một mẩu ĐÃ BỊ NGƯỜI GỬI RÚT LẠI ---
//
// 🛑 Khoá nguồn nằm trong THÂN, không trong đường dẫn. `AR-g` vừa đo được (cùng tuần) rằng
//    khoá chứa dấu `/` không đọc ra được qua đường dẫn — ASP.NET giải mã %2F TRƯỚC khi định
//    tuyến. Khoá của evidence còn chứa `#` (`jira:ES-1234#comment-7`), thứ mà client nào
//    quên mã hoá là mất nửa sau ngay tại trình duyệt. Đặt vào thân là tránh cả hai, và nó
//    KHÔNG đi ngược `C4-1`: quyết định đó chọn hình dạng cho đường ĐỌC của case.
//
// ⚠ KHÔNG có đường nào cho máy gọi. Đường nạp không mang ba trường rút lại, và endpoint này
//   bắt buộc khai người đánh dấu — `B1-1`: CHỈ NGƯỜI điền tay.
app.MapPost("/evidence/rut-lai", async (
        YeuCauRutLai yeuCau,
        EvidenceRetractionHandler handler,
        CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(yeuCau.KhoaNguon))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thiếu khoaNguon",
                detail: "Phải nói rõ mẩu nào — khoá nguồn của chính mẩu đó, ví dụ \"jira:ES-346661#description\".");
        }

        if (string.IsNullOrWhiteSpace(yeuCau.NguoiDanhDau))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thiếu nguoiDanhDau",
                detail: "AR-m chốt CHỈ NGƯỜI điền tay, nên phải biết là người nào. Một dấu vết " +
                        "không có tác giả thì không phân biệt được với một dấu vết do script đặt.");
        }

        if (string.IsNullOrWhiteSpace(yeuCau.LyDo))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Thiếu lyDo",
                detail: "Vì sao mẩu này bị rút. Cố ý không có danh sách chọn sẵn: lý do thật của " +
                        "ca ES-346661 là \"người gửi nói gửi nhầm, đoạn này là của case khác\".");
        }

        var kq = await handler.RutLaiAsync(yeuCau.KhoaNguon, yeuCau.NguoiDanhDau, yeuCau.LyDo, ct);

        return kq.KetCuc switch
        {
            KetCucRutLai.KhongTimThay => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không thấy mẩu evidence nào mang khoá nguồn này",
                detail: $"Không có mẩu nào mang khoá \"{yeuCau.KhoaNguon}\" trong phạm vi của khách hàng này."),

            // 409 chứ không phải 200: lần đánh dấu thứ hai KHÔNG xảy ra, và trả 200 sẽ nói
            // dối rằng lý do vừa gửi đã được ghi. Giữ lần đầu là cố ý — xem EvidenceItem.RutLai.
            KetCucRutLai.DaRutLaiTuTruoc => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Mẩu này đã được đánh dấu rút lại từ trước",
                detail: $"Đã đánh dấu lúc {kq.Luc:O} bởi \"{kq.NguoiDanhDau}\". Lần này KHÔNG ghi đè — " +
                        "ghi đè sẽ xoá mất tác giả và lý do của lần đầu."),

            _ => Results.Ok(kq),
        };
    })
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

// --- AR-l: điều kiện MỞ LẠI, hỏi được bằng một lượt gọi ---
//
// Tài liệu phát biểu điều kiện mở lại dạng ĐO ĐƯỢC chứ không phải một ngày trong lịch:
// "khi có MỘT nguồn cho ra ≥2 giá trị PHÂN BIỆT". Endpoint này trả lời đúng câu đó, và trả
// ra CẢ các giá trị đã thấy — một con số "2" mà không nói ra hai giá trị nào thì không ai
// kiểm lại được, trong khi đây là phép đếm quyết định việc gỡ một cổng bảo mật.
//
// ⚠ Nó KHÔNG tự gỡ cổng gì. Cổng 409 của chế độ shared vẫn nguyên; đây chỉ là cái thước.
app.MapGet("/internal/sub-tenant-coverage", async (
        CaseEvidenceReadHandler handler,
        CancellationToken ct) => Results.Ok(await handler.DoPhuMaKhachSanAsync(ct)))
    .AddEndpointFilter<ApiKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();


// 🛑 Đối chiếu DANH SÁCH ROUTE THẬT với bảng mô tả — thêm endpoint mà quên viết hợp đồng
//    thì KHÔNG KHỞI ĐỘNG ĐƯỢC. Phải đứng SAU mọi lệnh Map, nên nó không nằm chung với
//    StartupChecks (chạy trước khi endpoint được khai báo).
//
//    Cùng cơ chế `RlsGuard` dùng cho bảng thiếu RLS, và vì cùng một lý do: cái phải có thì
//    nên tự động (quên là bị bắt). `IM-38` đo được cái giá của việc không có nó.
StartupChecks.RequireMoTaEndpoint(app);

app.Run();

// Kiểm ở đây, không phải bằng DataAnnotations trên record: giới hạn độ dài phải
// khớp schema (xem AppDbContext), và một chỗ duy nhất biết cả hai thì dễ giữ khớp
// hơn là hai chỗ.
static string? Validate(CaseObservedSignal s) => s switch
{
    { SourceReference: null or "" } => "sourceReference không được để trống — nó là thứ làm tín hiệu lặp lại được mà không sinh case trùng.",
    { Subject: null or "" } => "subject không được để trống.",
    _ when s.SourceReference.Length > 512 => "sourceReference dài quá 512 ký tự.",
    _ when s.Subject.Length > 1024 => "subject dài quá 1024 ký tự.",
    _ => null,
};

// ⚠ Dùng IsNullOrWhiteSpace, KHÁC với Validate ở trên (chỉ kiểm null hoặc rỗng).
// Cố ý và không phải để cho đẹp: một `content` toàn dấu cách vẫn tạo ra một dòng
// evidence rỗng nghĩa, và Path A sẽ đưa chính dòng đó cho model như một quan sát
// thật. Rác trong kho gom nguy hiểm hơn rác ở một tiêu đề.
// ⚠ Chỗ lệch giữa hai hàm này là ĐÃ BIẾT, chưa xử lý: Validate ở trên nhận
//   subject toàn dấu cách. Sửa nó là đụng vào hợp đồng đang chạy nên để người dùng quyết.
//
// KHÔNG có trần độ dài cho `content`: cột là `text`, không giới hạn ở schema, nên
// đặt một con số ở đây là tạo ra đúng chỗ lệch mà chú thích của Validate cảnh báo.
// Chặn trên vẫn có — trần body mặc định của Kestrel và MaxEvidencePerRequest.
static string? ValidateEvidence(CaseEvidenceSignal s) => s switch
{
    _ when string.IsNullOrWhiteSpace(s.SourceReference)
        => "sourceReference không được để trống — nó là thứ làm evidence lặp lại được mà không sinh bản trùng.",
    _ when string.IsNullOrWhiteSpace(s.Content)
        => "content không được để trống — một mẩu evidence rỗng nghĩa là rác trong kho gom của Path A.",
    _ when s.SourceReference.Length > 512
        => "sourceReference dài quá 512 ký tự.",
    _ when s.CaseSourceReference is { Length: > 512 }
        => "caseSourceReference dài quá 512 ký tự.",
    _ when !EvidenceReadability.TryParse(s.MachineReadability, out _)
        => $"machineReadability không nhận ra giá trị \"{s.MachineReadability}\". Nhận: {EvidenceReadability.Allowed}. " +
           "Bỏ trống thì là Unknown — cố ý không tự đoán hộ, xem CaseEvidenceSignal.",

    // ⚠ Bỏ trống là 400, KHÔNG âm thầm về `khong-xac-dinh` — `AR-l`. Hai câu "bên gửi chưa
    // biết" và "bên gửi chưa đọc tài liệu" khác nhau đúng ở chỗ quan trọng nhất, và tự điền
    // hộ ở cửa vào thì về sau không dựng lại được. Đây là chỗ DUY NHẤT tính bắt buộc của
    // trường này được thực thi (cột trong DB nullable có lý do — xem EvidenceItem).
    _ when string.IsNullOrWhiteSpace(s.SubTenantCode)
        => "subTenantCode không được để trống — AR-l: bên gửi PHẢI khai mã khách sạn. " +
           $"Chưa biết thì khai \"{SubTenantCodes.KhongXacDinh}\"; không thuộc khách sạn nào " +
           $"(việc nội bộ, ezGolf, chuỗi nhiều cơ sở) thì khai \"{SubTenantCodes.KhongThuocKhachSan}\". " +
           "Cố ý không tự điền hộ: \"chưa biết\" khác \"chưa ai hỏi\".",
    _ when !SubTenantCodes.TryChuanHoa(s.SubTenantCode, out _)
        => $"subTenantCode dài quá {SubTenantCodes.MaxLength} ký tự.",
    _ => null,
};

// ⚠ `daXongONguon` BẮT BUỘC, và cố ý không suy từ việc có hay không có `sourceResolvedAt`.
// Cùng luật với `subTenantCode` của `AR-l`: "chưa gửi" và "đã mở lại" đều cho ra một ô trống,
// mà hai điều đó khác hẳn nhau và về sau không dựng lại được từ dữ liệu.
static string? ValidateStatus(CaseStatusSignal s) => s switch
{
    { SourceReference: null or "" } => "sourceReference không được để trống.",
    _ when s.SourceReference.Length > 512 => "sourceReference dài quá 512 ký tự.",
    { DaXongONguon: null }
        => "daXongONguon không được để trống — phải khai tường minh true hoặc false. " +
           "Bỏ trống thì hai chuyện khác hẳn nhau trông giống hệt: bên gửi CHƯA GỬI trạng " +
           "thái, và việc ở nguồn ĐÃ MỞ LẠI.",
    _ => null,
};

/// <summary>Thân của <c>POST /knowledge/soan-nhap</c>.</summary>
/// <param name="NguoiYeuCau">LỜI KHAI của người bấm nút tốn tiền — KHÔNG phải một phép xác thực.</param>
internal sealed record YeuCauSoanNhap(string? Nhom, string? NguoiYeuCau);

/// <summary>Thân của <c>POST /knowledge/{id}/link</c>.</summary>
/// <param name="NguoiPhat">Ai phát link này. BẮT BUỘC — xem chỗ gọi.</param>
/// <param name="HanNgay">Hạn tính bằng ngày. Bỏ trống thì lấy mặc định của cấu hình.</param>
internal sealed record PhatLinkYeuCau(string? NguoiPhat, int? HanNgay);
