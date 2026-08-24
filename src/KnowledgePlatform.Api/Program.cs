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
        await RlsGuard.VerifyAsync(db, ct);
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

        return Results.Ok(new
        {
            mode = tenancy.Value.Mode.ToString(),
            tenantId = tenant.TenantId,
            rowsVisibleWithoutTenantFilter = new { cases, knowledge },
        });
    })
    .AddEndpointFilter<TenantEndpointFilter>();

app.Run();
