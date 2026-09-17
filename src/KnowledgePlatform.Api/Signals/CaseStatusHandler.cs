using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Xử lý tín hiệu **đổi trạng thái** (`F1-1`). Xem <see cref="CaseStatusSignal"/> cho lý do
/// nó cố ý không làm gì thêm.
///
/// ⚠ **KHÔNG xếp hàng gửi kết quả về.** Đường tín hiệu loại MỘT có làm việc đó
/// (<see cref="CaseSignalHandler"/>), đường này thì không — và đó là khác biệt dễ "sửa nhầm
/// cho nhất quán" nhất trong file này. Một case đóng lại không sinh ra câu hỏi mới nào; bắn
/// webhook cho nó là gửi cho khách một thông báo *"đây, hướng dẫn cho việc bạn vừa làm xong"*.
/// </summary>
public sealed class CaseStatusHandler(AppDbContext db)
{
    public async Task<CaseStatusBatchResult> HandleAsync(
        IReadOnlyList<CaseStatusSignal> signals, CancellationToken ct = default)
    {
        var results = new List<CaseStatusResult>(signals.Count);

        // Tra một lượt cho cả lô thay vì một câu mỗi tín hiệu — cùng lý do với
        // SopDraftIngestHandler. Lô nạp lịch sử có thể vài trăm phần tử.
        var khoa = signals.Select(s => s.SourceReference).Distinct().ToList();
        var cases = await db.Cases
            .Where(c => khoa.Contains(c.SourceReference))
            .ToDictionaryAsync(c => c.SourceReference, ct);

        foreach (var tin in signals)
        {
            if (!cases.TryGetValue(tin.SourceReference, out var c))
            {
                results.Add(new CaseStatusResult(
                    tin.SourceReference, null, CaseStatusOutcome.KhongThayCase));
                continue;
            }

            // `DaXongONguon` đã được kiểm không-null ở cửa vào; ở đây chỉ còn đọc giá trị.
            var moc = tin.DaXongONguon == true
                ? tin.SourceResolvedAt ?? DateTimeOffset.UtcNow
                : (DateTimeOffset?)null;

            // Gửi lại đúng trạng thái đang có KHÔNG phải lỗi — tín hiệu đến hai lần là
            // chuyện bình thường của mọi hệ thống tích hợp (cùng lý do `IM-20`).
            //
            // ⚠ So theo CÓ/KHÔNG CÓ mốc, không so từng mili-giây: bên gửi lại cùng một sự
            //   kiện "đã xong" có thể kèm mốc lệch vài giây, và coi đó là một lần đổi nữa
            //   sẽ làm mọi phép đếm "case đổi trạng thái hôm nay" nói dối.
            var dangXong = c.SourceResolvedAt.HasValue;
            var seXong = moc.HasValue;

            if (dangXong == seXong)
            {
                results.Add(new CaseStatusResult(
                    tin.SourceReference, c.Id, CaseStatusOutcome.KhongDoi));
                continue;
            }

            c.SourceResolvedAt = moc;
            results.Add(new CaseStatusResult(tin.SourceReference, c.Id, CaseStatusOutcome.DaCapNhat));
        }

        await db.SaveChangesAsync(ct);

        return new CaseStatusBatchResult(
            Received: signals.Count,
            DaCapNhat: results.Count(r => r.TrangThai == CaseStatusOutcome.DaCapNhat),
            KhongThayCase: results.Count(r => r.TrangThai == CaseStatusOutcome.KhongThayCase),
            Results: results);
    }
}
