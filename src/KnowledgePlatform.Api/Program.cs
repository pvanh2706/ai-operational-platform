using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Api.Signals;
using KnowledgePlatform.Api.Startup;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
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

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TenancyOptions>(
    builder.Configuration.GetSection(TenancyOptions.SectionName));
builder.Services.Configure<IngestOptions>(
    builder.Configuration.GetSection(IngestOptions.SectionName));
builder.Services.Configure<DinhTuyenOptions>(
    builder.Configuration.GetSection(DinhTuyenOptions.SectionName));

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
builder.Services.AddScoped<CaseEvidenceReadHandler>();
builder.Services.AddScoped<EvidenceRetractionHandler>();
builder.Services.AddScoped<SopDraftIngestHandler>();
builder.Services.AddScoped<KnowledgeApprovalHandler>();
builder.Services.AddScoped<KnowledgeRevisionHandler>();
builder.Services.AddScoped<HoiHandler>();

// AR3 — interface mỏng, một dòng để đổi bản hiện thực. Bản đang cắm KHÔNG gọi API và
// KHÔNG định tuyến: nó tra bảng kết quả đã đo (`docs/13`). Xem DinhTuyenTraSan.
// Singleton vì nó chỉ đọc một file một lần rồi giữ trong bộ nhớ; không có trạng thái
// theo request, và KHÔNG chạm tenant — nên nó không phải chỗ rò ranh giới.
builder.Services.AddSingleton<IDinhTuyenNhom>(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<DinhTuyenOptions>>().Value;
    return new DinhTuyenTraSan(cfg.DuongDanBangTraSan ?? DinhTuyenOptions.DuongDanMacDinh());
});

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
        if (string.IsNullOrWhiteSpace(draft.Nhom))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bản nháp thiếu tên nhóm",
                detail: "`nhom` là thứ nối bản nháp với bộ định tuyến. Thiếu nó thì record nạp vào " +
                        "kho sẽ không bao giờ được tra ra.");
        }

        if (draft.BuocSua.Count == 0)
        {
            // Không bước sửa nào = không nguyên nhân nào = nạp xong kho vẫn rỗng.
            // Nói ra thay vì trả 200 với created 0, thứ đọc ra thành "đã nạp rồi".
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bản nháp không có bước sửa nào",
                detail: "Mỗi bước sửa là một nguyên nhân (T1). Không có bước sửa nào thì không có " +
                        "gì để nạp — trả 200 ở đây sẽ đọc ra thành \"đã nạp xong\".");
        }

        return Results.Ok(await handler.HandleAsync(draft, ct));
    })
    .AddEndpointFilter<SignalKeyEndpointFilter>()
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
        // ⚠ Đây là LỜI KHAI, KHÔNG phải phép kiểm. Hệ thống không đối chiếu được, và hôm nay
        //   còn không biết mẩu nguồn là những mẩu nào — `assertion_evidence` có 0 dòng. Ghi
        //   thẳng ra đây để không ai đọc ô `true` thành "đã kiểm".
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
    .AddEndpointFilter<SignalKeyEndpointFilter>()
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
    .AddEndpointFilter<SignalKeyEndpointFilter>()
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
    .AddEndpointFilter<SignalKeyEndpointFilter>()
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
    .AddEndpointFilter<SignalKeyEndpointFilter>()
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
    .AddEndpointFilter<SignalKeyEndpointFilter>()
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
    .AddEndpointFilter<SignalKeyEndpointFilter>()
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
