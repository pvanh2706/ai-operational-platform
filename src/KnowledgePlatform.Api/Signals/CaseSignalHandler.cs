using KnowledgePlatform.Domain.Cases;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Ô "Tìm hoặc tạo Case" của sơ đồ luồng tín hiệu.
///
/// ⚠ **Nó dừng ở đúng đây, cố ý.** Các ô sau trong sơ đồ — khớp quy trình đã duyệt,
/// suy ra đang ở bước nào, tra tri thức, trả gợi ý — đều chưa build. Trả về một
/// gợi ý rỗng hay một trường <c>suggestions: []</c> sẽ làm bên gọi tưởng đường đó
/// đã tồn tại. `G11`: không tự phỏng to một capability đã chốt.
///
/// Nên response chỉ nói đúng thứ đã xảy ra: case nào, mới hay đã có.
/// </summary>
public sealed class CaseSignalHandler(AppDbContext db, ITenantContext tenant)
{
    /// <summary>
    /// Xử lý cả lô. Mỗi tín hiệu là một phần tử; lô một phần tử là ca thường gặp.
    ///
    /// Vì sao MỘT đường code cho cả tín hiệu lẻ và nạp lịch sử hàng loạt: tách hai
    /// endpoint là tạo hai đường code làm cùng một việc, và đường ít chạy hơn sẽ
    /// mục dần mà không ai biết — cùng lý do với `IM-12`.
    /// </summary>
    public async Task<CaseSignalBatchResult> HandleAsync(
        IReadOnlyList<CaseObservedSignal> signals, CancellationToken ct = default)
    {
        var results = new List<CaseSignalResult>(signals.Count);

        foreach (var signal in signals)
        {
            results.Add(await FindOrCreateAsync(signal, ct));
        }

        return new CaseSignalBatchResult(
            Received: signals.Count,
            Created: results.Count(r => r.Created),
            Results: results);
    }

    private async Task<CaseSignalResult> FindOrCreateAsync(CaseObservedSignal signal, CancellationToken ct)
    {
        var existing = await db.Cases
            .Where(c => c.SourceReference == signal.SourceReference)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);

        // Không cần điều kiện TenantId ở câu trên: global query filter thêm nó, và
        // RLS chặn ở tầng database kể cả khi filter bị đi vòng. Hai tầng, `AR2`.
        if (existing != Guid.Empty)
        {
            return new CaseSignalResult(signal.SourceReference, existing, Created: false);
        }

        var newCase = new CanonicalCase
        {
            TenantId = tenant.TenantId,
            Subject = signal.Subject,
            SourceReference = signal.SourceReference,
            SourceCreatedAt = signal.SourceCreatedAt,
            SourceResolvedAt = signal.SourceResolvedAt,
        };

        db.Cases.Add(newCase);

        try
        {
            await db.SaveChangesAsync(ct);
            return new CaseSignalResult(signal.SourceReference, newCase.Id, Created: true);
        }
        catch (DbUpdateException ex) when (IsDuplicateSourceReference(ex))
        {
            // Hai tín hiệu giống nhau tới cùng lúc. Kiểm-rồi-ghi không phải nguyên
            // tử, nên unique index là chỗ chốt cuối — và nó vừa làm việc của nó.
            // Đọc lại thay vì trả lỗi: bên gửi không làm gì sai cả.
            db.Entry(newCase).State = EntityState.Detached;

            var id = await db.Cases
                .Where(c => c.SourceReference == signal.SourceReference)
                .Select(c => c.Id)
                .FirstAsync(ct);

            return new CaseSignalResult(signal.SourceReference, id, Created: false);
        }
    }

    private static bool IsDuplicateSourceReference(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
