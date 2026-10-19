using KnowledgePlatform.Domain.Cases;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Process;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KnowledgePlatform.Api.QuyTrinh;

/// <summary>
/// Ô `D` + `E` + `F` của sơ đồ trong MỘT chỗ: case → khớp quy trình đã duyệt → đang ở bước nào → bước hiện tại.
///
/// 🎯 Ô `D` (khớp case với quy trình) CHƯA TỪNG được thiết kế ở tài liệu nào (`PC`:1359-1366 chỉ gọi tên nó).
/// Cách khớp ở đây là lựa chọn KỸ THUẬT (`IM-49` lựa chọn 3): định tuyến case vào NHÓM, rồi lấy quy trình
/// ACTIVE của nhóm đó. Một chỗ quyết định, để `/hoi`, webhook và đường xác nhận bước không ai tự khớp riêng.
///
/// ⚠ Ba luật, và lý do:
/// <list type="bullet">
///   <item>case ĐANG CÓ lượt chạy mở thì dùng nó, KHÔNG khớp lại — kể cả khi nhóm đã có phiên bản mới
///     (`N6-1`) hay định tuyến đã đổi. Đổi giữa chừng là để tiến độ nhảy mà không ai bấm gì;</item>
///   <item>lượt chạy đã bị NGƯỜI bỏ thì không tự dựng lại cho đúng cặp (case, quy trình) đó — nếu không thì
///     "bỏ" chẳng có nghĩa gì;</item>
///   <item>tiến độ đọc MỌI mẩu của case, kể cả mẩu đã rút — <see cref="TienDoQuyTrinh"/> tự bỏ mẩu rút lại,
///     để luật đó nằm ở MỘT chỗ.</item>
/// </list>
/// </summary>
public sealed class KhopQuyTrinh(AppDbContext db, ITenantContext tenant, IDinhTuyenNhom dinhTuyen)
{
    /// <summary>
    /// Định tuyến một case ĐÃ CÓ: khoá nguồn (cho bản tra sẵn) + tiêu đề + mẩu SỚM NHẤT chưa rút lại.
    ///
    /// ⚠ Đúng định nghĩa `docs/13` §1 — thứ có mặt LÚC TICKET ĐẾN, không phải cả transcript (transcript chứa
    /// sẵn câu chẩn đoán; đưa vào là đưa đáp án vào đề). Lời xác nhận bước KHÔNG tính là mô tả.
    /// </summary>
    public async Task<KetQuaDinhTuyen> DinhTuyenCaseAsync(CanonicalCase c, CancellationToken ct = default) =>
        await dinhTuyen.DinhTuyenAsync(await YeuCauAsync(c, ct), ct);

    /// <summary>Như <see cref="DinhTuyenCaseAsync"/> nhưng KHÔNG gọi model, KHÔNG ghi biên lai — cho đường xem (`P2-1`).</summary>
    public async Task<KetQuaDinhTuyen> XemDinhTuyenCaseAsync(CanonicalCase c, CancellationToken ct = default) =>
        await dinhTuyen.XemAsync(await YeuCauAsync(c, ct), ct);

    private async Task<YeuCauDinhTuyen> YeuCauAsync(CanonicalCase c, CancellationToken ct)
    {
        var moTa = await db.EvidenceItems
            .Where(e => e.ObservedInCaseId == c.Id && e.RetractedAt == null && e.XacNhanProcessStepId == null)
            .OrderBy(e => e.ObservedAt)
            .ThenBy(e => e.IngestedAt)
            .Select(e => e.Content)
            .FirstOrDefaultAsync(ct);

        return new YeuCauDinhTuyen(c.SourceReference, c.Subject, moTa);
    }

    /// <param name="taoLuotChay">
    /// <c>false</c> = chỉ XEM sẽ khớp quy trình nào, không ghi gì (đường đọc <c>GET /cases/{k}/quy-trinh</c>).
    /// </param>
    public async Task<KetQuaKhop> KhopAsync(
        CanonicalCase c, KetQuaDinhTuyen tuyen, bool taoLuotChay, CancellationToken ct = default)
    {
        var luot = await db.Set<ProcessRun>()
            .FirstOrDefaultAsync(r => r.CaseId == c.Id && r.AbandonedAt == null, ct);

        ProcessDefinition dinhNghia;

        if (luot is not null)
        {
            dinhNghia = await db.Set<ProcessDefinition>()
                .Include(d => d.Buoc)
                .FirstAsync(d => d.Id == luot.ProcessDefinitionId, ct);
        }
        else
        {
            if (!tuyen.BietNhom)
            {
                return KetQuaKhop.KhongKhop("dinh-tuyen-khong-biet", tuyen.GiaiThich);
            }

            var dangDung = await db.Set<ProcessDefinition>()
                .Include(d => d.Buoc)
                .FirstOrDefaultAsync(d => d.Nhom == tuyen.Nhom && d.Lifecycle == StoredLifecycleState.Active, ct);

            if (dangDung is null)
            {
                return KetQuaKhop.KhongKhop("nhom-chua-co-quy-trinh-duoc-duyet",
                    $"Định tuyến vào nhóm \"{tuyen.Nhom}\", nhưng nhóm này chưa có quy trình nào được người duyệt.");
            }

            dinhNghia = dangDung;

            var daBo = await db.Set<ProcessRun>()
                .AnyAsync(r => r.CaseId == c.Id && r.ProcessDefinitionId == dinhNghia.Id && r.AbandonedAt != null, ct);

            if (daBo)
            {
                return KetQuaKhop.KhongKhop("luot-chay-da-bi-bo",
                    "Lượt chạy quy trình này trên case này đã bị người bỏ — hệ thống không tự dựng lại.");
            }

            if (taoLuotChay)
            {
                luot = await TaoLuotChayAsync(c, dinhNghia, ct);
            }
        }

        var evidence = await db.EvidenceItems.Where(e => e.ObservedInCaseId == c.Id).ToListAsync(ct);

        return KetQuaKhop.Khop(dinhNghia, luot, TienDoQuyTrinh.SuyRa(dinhNghia, evidence));
    }

    private async Task<ProcessRun> TaoLuotChayAsync(CanonicalCase c, ProcessDefinition dinhNghia, CancellationToken ct)
    {
        var moi = new ProcessRun { TenantId = tenant.TenantId, CaseId = c.Id, ProcessDefinitionId = dinhNghia.Id };
        db.Set<ProcessRun>().Add(moi);

        try
        {
            await db.SaveChangesAsync(ct);
            return moi;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Hai lượt cùng tạo run cho một case: unique index (một run mở mỗi case) là chốt cuối. Đọc lại,
            // bên gọi không làm gì sai — cùng khuôn CaseSignalHandler.
            db.Entry(moi).State = EntityState.Detached;
            return await db.Set<ProcessRun>().FirstAsync(r => r.CaseId == c.Id && r.AbandonedAt == null, ct);
        }
    }
}

/// <param name="LyDo">Khi không khớp: <c>dinh-tuyen-khong-biet</c> · <c>nhom-chua-co-quy-trinh-duoc-duyet</c> · <c>luot-chay-da-bi-bo</c>.</param>
/// <param name="LuotChay"><c>null</c> khi chỉ XEM (chưa tạo lượt chạy).</param>
public sealed record KetQuaKhop(
    bool KhopDuoc,
    string? LyDo,
    string? GiaiThich,
    ProcessDefinition? DinhNghia,
    ProcessRun? LuotChay,
    TienDo? TienDo)
{
    public static KetQuaKhop KhongKhop(string lyDo, string? giaiThich) => new(false, lyDo, giaiThich, null, null, null);

    public static KetQuaKhop Khop(ProcessDefinition d, ProcessRun? luot, TienDo tienDo) => new(true, null, null, d, luot, tienDo);
}
