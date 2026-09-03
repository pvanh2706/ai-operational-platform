using KnowledgePlatform.Api.Signals;
using KnowledgePlatform.Api.Startup;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
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

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TenancyOptions>(
    builder.Configuration.GetSection(TenancyOptions.SectionName));
builder.Services.Configure<IngestOptions>(
    builder.Configuration.GetSection(IngestOptions.SectionName));

// Danh bạ tenant nằm NGOÀI ranh giới tenant — xem TenantDirectory.
builder.Services.AddSingleton(sp => new TenantDirectory(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")!));

// Đặt một lần lúc khởi động bởi StartupChecks, không phải mỗi request.
builder.Services.AddSingleton<DedicatedTenant>();

// G13: cả hai chế độ deploy đi qua cùng một cài đặt ITenantContext. Chế độ nào
// thì do TenantResolutionMiddleware quyết, không phải do đường code khác nhau.
builder.Services.AddScoped<RequestTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<RequestTenantContext>());
builder.Services.AddScoped<TenantEndpointFilter>();
builder.Services.AddScoped<SignalKeyEndpointFilter>();
builder.Services.AddScoped<CaseSignalHandler>();
builder.Services.AddScoped<CaseEvidenceHandler>();

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

var app = builder.Build();

// Thất bại nào ở đây cũng là KHÔNG KHỞI ĐỘNG ĐƯỢC, không phải cảnh báo.
await StartupChecks.RunAsync(app.Services);

app.UseMiddleware<TenantResolutionMiddleware>();

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
    .AddEndpointFilter<TenantEndpointFilter>();

// --- KÊNH 1: đường nhận tín hiệu từ phần mềm có sẵn của khách (06 §1) ---
//
// Tín hiệu đi vào đây và dừng ở ô "Tìm hoặc tạo Case" của sơ đồ luồng. Các ô sau
// — khớp quy trình đã duyệt, suy ra bước hiện tại, tra tri thức, trả gợi ý — CHƯA
// BUILD, và response cố ý không có chỗ nào trông như thể chúng đã có (G11).
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
    .AddEndpointFilter<SignalKeyEndpointFilter>()
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
    .AddEndpointFilter<SignalKeyEndpointFilter>()
    .AddEndpointFilter<TenantEndpointFilter>();

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
    _ => null,
};
