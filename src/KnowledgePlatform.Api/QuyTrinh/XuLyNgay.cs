using KnowledgePlatform.Domain.Process;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Domain.Webhooks;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KnowledgePlatform.Api.QuyTrinh;

/// <summary>
/// *"Cứ nhận dữ liệu về là cho vào luồng trong sơ đồ để xử lý luôn"* (chủ dự án, 2026-10-10) — P7.
///
/// Mẩu evidence mới (hoặc một lời xác nhận bước) có thể làm case BƯỚC SANG bước tiếp theo. Khi đó người đang xử
/// lý cần tri thức của BƯỚC MỚI, không phải của bước họ vừa làm xong — nên một lượt trả kết quả về được xếp hàng.
///
/// 🛑 "Bước đổi" được nhận ra bằng cách SUY tiến độ HAI LẦN — trước và sau lô vừa nhận — chứ không đọc một cờ nào
/// (PR1: không có cột tiến độ). Cùng một hàm thuần <see cref="TienDoQuyTrinh"/> cho cả hai lần.
///
/// ⚠ Bốn chỗ cố ý KHÔNG xếp hàng, và lý do:
/// <list type="bullet">
///   <item>case chưa có lượt chạy quy trình — chưa có "bước" nào để đổi;</item>
///   <item>case ĐÃ XONG Ở NGUỒN — một thông báo "bước tiếp theo" cho việc đã đóng là nhiễu (cùng tinh thần `F1-1`);</item>
///   <item>đã có một lượt gửi ĐANG CHỜ cho case — lượt đó dựng gói LÚC GỬI, nên nó tự mang bước mới nhất;</item>
///   <item>khoá chống trùng (lượt chạy + bước) đã có — gửi lại cùng mẩu, hay nạp lại cả lô, không đẻ lượt thứ hai.</item>
/// </list>
/// ⚠ Rút lại một mẩu (`AR-m`) có thể làm tiến độ LÙI — nhưng đó là hành động của người, không phải dữ liệu nhận
/// về, nên không kích hoạt gì ở đây.
/// </summary>
public sealed class XuLyNgay(AppDbContext db, ITenantContext tenant)
{
    public const string SuKienBuocDoi = "buoc-hien-tai-doi";

    /// <returns>Số lượt gửi vừa xếp hàng.</returns>
    public async Task<int> SauKhiCoEvidenceAsync(IReadOnlyCollection<Guid> mauMoi, CancellationToken ct = default)
    {
        if (mauMoi.Count == 0) return 0;

        // Khách chưa khai đích gửi thì hàng đợi phải rỗng — cùng luật CaseSignalHandler.
        if (!await db.Set<WebhookEndpoint>().AnyAsync(e => e.Active, ct)) return 0;

        var caseIds = await db.EvidenceItems
            .Where(e => mauMoi.Contains(e.Id) && e.ObservedInCaseId != null)
            .Select(e => e.ObservedInCaseId!.Value)
            .Distinct()
            .ToListAsync(ct);

        if (caseIds.Count == 0) return 0;

        var dsLuotChay = await db.Set<ProcessRun>()
            .Where(r => caseIds.Contains(r.CaseId) && r.AbandonedAt == null)
            .ToListAsync(ct);

        var daXep = 0;

        foreach (var run in dsLuotChay)
        {
            var c = await db.Cases.FirstAsync(x => x.Id == run.CaseId, ct);
            if (c.SourceResolvedAt is not null) continue;

            var dangCho = await db.Set<WebhookDelivery>()
                .AnyAsync(d => d.CaseId == c.Id && d.State == WebhookDeliveryState.Pending, ct);
            if (dangCho) continue;

            var dinhNghia = await db.Set<ProcessDefinition>().Include(d => d.Buoc)
                .FirstAsync(d => d.Id == run.ProcessDefinitionId, ct);
            var evidence = await db.EvidenceItems.Where(e => e.ObservedInCaseId == c.Id).ToListAsync(ct);

            var truoc = TienDoQuyTrinh.SuyRa(dinhNghia, evidence.Where(e => !mauMoi.Contains(e.Id)));
            var sau = TienDoQuyTrinh.SuyRa(dinhNghia, evidence);

            if (string.Equals(truoc.BuocHienTai?.Ma, sau.BuocHienTai?.Ma, StringComparison.Ordinal)) continue;

            var khoa = $"{run.Id:N}:{sau.BuocHienTai?.Ma ?? "xong-het"}";
            if (await db.Set<WebhookDelivery>().AnyAsync(d => d.KhoaKhuTrung == khoa, ct)) continue;

            db.Set<WebhookDelivery>().Add(new WebhookDelivery
            {
                TenantId = tenant.TenantId,
                CaseId = c.Id,
                CaseSourceReference = c.SourceReference,
                Subject = c.Subject,
                SuKien = SuKienBuocDoi,
                KhoaKhuTrung = khoa,
            });
            daXep++;
        }

        if (daXep == 0) return 0;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Hai lô cùng làm đổi đúng một bước cùng lúc: unique index trên khoá chống trùng là chốt cuối. Lượt kia
            // đã xếp rồi — bên gửi không làm gì sai, và không ai nhận hai thông báo.
            return 0;
        }

        return daXep;
    }
}
