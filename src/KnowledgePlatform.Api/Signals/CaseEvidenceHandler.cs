using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Nạp evidence — nội dung của những gì đã xảy ra trong một case.
///
/// ⚠ **Vì sao ô này phải có trước truy vấn "tìm N case cũ liên quan".**
/// Trước `AR-f`, một <c>canonical_case</c> là MỘT DÒNG CHỮ: subject + khoá nguồn +
/// hai mốc thời gian. `S8` lại đòi bản nháp gom mang theo một PHÂN BỐ — *"bước kiểm
/// room mapping: 14/20 case đã làm"*, *"gọi OTA trước khi check log: 6/20 làm,
/// 8/20 làm ngược lại"*. Con số 14/20 KHÔNG suy ra được từ 20 cái tiêu đề.
///
/// Tìm được 20 case rỗng nội dung rồi đưa cho model thì cái trả về là một SOP nghe
/// hợp lý mà không dựa trên gì — đúng thứ `G6`/`AP3` sinh ra để chặn, và nó làm hỏng
/// `M2` ngay tại nguồn (`M2` đo *số nháp được duyệt + mức sửa diff(A,B)*).
///
/// ⚠ **Ô này DỪNG Ở ĐÂY, cố ý.** Nó KHÔNG rút ra assertion, KHÔNG tạo KnowledgeRecord,
/// KHÔNG suy ra bước quy trình. `S6`: nạp nội dung không tự sinh tri thức — tri thức
/// chỉ sinh khi có hành vi khẳng định. Response nói đúng thứ đã xảy ra: mẩu nào,
/// gắn case nào, mới hay đã có (`G11`).
/// </summary>
public sealed class CaseEvidenceHandler(AppDbContext db, ITenantContext tenant)
{
    /// <summary>
    /// Tra một lượt mọi khoá case được nhắc tới trong lô. MỘT câu truy vấn cho cả lô,
    /// không phải một câu mỗi phần tử.
    ///
    /// Tách khỏi <see cref="HandleAsync"/> vì nó phục vụ tầng KIỂM TRA: phải biết đủ
    /// case nào thiếu TRƯỚC khi ghi dòng đầu tiên, nếu không thì lô nhắc tới một case
    /// không tồn tại sẽ ghi được một nửa — đúng thứ nhóm C của bộ test API cấm.
    ///
    /// Không cần điều kiện TenantId: global query filter thêm nó, RLS chặn ở tầng
    /// database. Nên khoá case của khách A không tra ra được từ request của khách B —
    /// nó biểu hiện thành "không tìm thấy", đúng như khi nó không tồn tại thật.
    /// </summary>
    public async Task<Dictionary<string, Guid>> FindCaseIdsAsync(
        IReadOnlyCollection<string> sourceReferences, CancellationToken ct = default)
    {
        if (sourceReferences.Count == 0) return [];

        var rows = await db.Cases
            .Where(c => sourceReferences.Contains(c.SourceReference))
            .Select(c => new { c.SourceReference, c.Id })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.SourceReference, r => r.Id);
    }

    /// <summary>
    /// Xử lý cả lô. <paramref name="caseIds"/> là kết quả <see cref="FindCaseIdsAsync"/>
    /// đã được tầng kiểm tra xác nhận là đủ — tới đây không còn khoá nào thiếu.
    /// </summary>
    public async Task<CaseEvidenceBatchResult> HandleAsync(
        IReadOnlyList<CaseEvidenceSignal> signals,
        IReadOnlyDictionary<string, Guid> caseIds,
        CancellationToken ct = default)
    {
        var results = new List<CaseEvidenceResult>(signals.Count);

        foreach (var signal in signals)
        {
            Guid? caseId = string.IsNullOrWhiteSpace(signal.CaseSourceReference)
                ? null
                : caseIds[signal.CaseSourceReference];

            results.Add(await FindOrCreateAsync(signal, caseId, ct));
        }

        return new CaseEvidenceBatchResult(
            Received: signals.Count,
            Created: results.Count(r => r.Created),
            Results: results);
    }

    private async Task<CaseEvidenceResult> FindOrCreateAsync(
        CaseEvidenceSignal signal, Guid? caseId, CancellationToken ct)
    {
        var existing = await db.EvidenceItems
            .Where(e => e.SourceReference == signal.SourceReference)
            .Select(e => new { e.Id, e.ObservedInCaseId })
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            // ⚠ Gửi lại cùng khoá với nội dung KHÁC thì bản đã lưu KHÔNG bị đổi.
            // Cố ý, và không phải vì lười: `K-B3` nói evidence gắn với MỘT thời điểm
            // và MỘT nguồn. Ghi đè lặng lẽ là sửa lại quá khứ — và nó sẽ kéo theo
            // mọi assertion đang dẫn chứng bằng mẩu này, mà không cảnh báo ai.
            // Muốn nói "nguồn đã đổi" thì đó là một mẩu evidence MỚI với khoá mới.
            return new CaseEvidenceResult(
                signal.SourceReference, existing.Id, existing.ObservedInCaseId, Created: false);
        }

        EvidenceReadability.TryParse(signal.MachineReadability, out var readability);

        var item = new EvidenceItem
        {
            TenantId = tenant.TenantId,
            Content = signal.Content,
            SourceReference = signal.SourceReference,
            ObservedInCaseId = caseId,
            MachineReadability = readability,
            ObservedAt = signal.ObservedAt,
        };

        db.EvidenceItems.Add(item);

        try
        {
            await db.SaveChangesAsync(ct);
            return new CaseEvidenceResult(signal.SourceReference, item.Id, caseId, Created: true);
        }
        catch (DbUpdateException ex) when (IsDuplicateSourceReference(ex))
        {
            // Hai mẩu giống nhau tới cùng lúc. Kiểm-rồi-ghi không nguyên tử, nên
            // unique index là chỗ chốt cuối — và nó vừa làm việc của nó. Đọc lại
            // thay vì trả lỗi: bên gửi không làm gì sai. Giống hệt CaseSignalHandler.
            db.Entry(item).State = EntityState.Detached;

            var row = await db.EvidenceItems
                .Where(e => e.SourceReference == signal.SourceReference)
                .Select(e => new { e.Id, e.ObservedInCaseId })
                .FirstAsync(ct);

            return new CaseEvidenceResult(
                signal.SourceReference, row.Id, row.ObservedInCaseId, Created: false);
        }
    }

    private static bool IsDuplicateSourceReference(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
