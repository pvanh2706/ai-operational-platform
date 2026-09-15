using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Nạp một BẢN NHÁP SOP vào kho tri thức — mảnh (a) của phạm vi demo chốt 2026-09-13.
///
/// Trước đường này, bản nháp do máy sinh chỉ là một file JSON: `SoanNhapRunner` ghi ra đĩa
/// rồi dừng. Kho tri thức vì thế RỖNG, và `Approve()` không nơi nào gọi được vì không có gì
/// để duyệt. Đây là chỗ nối hai đầu đó lại.
///
/// 🛑 NÓ KHÔNG DUYỆT GÌ CẢ. Record vào kho ở trạng thái <c>Draft</c>, `VisibilityScope` còn
/// trống. `D4`: người công nhận, không phải AI — và "nạp" không phải "công nhận". Muốn nó
/// trả lời được cho ai thì phải có người gọi đường duyệt.
///
/// ⚠ IDEMPOTENT THEO CẶP <c>(nhóm, CauseName)</c>. Nạp lại cùng một bản nháp thì không sinh
/// record trùng. Và nếu record đã có, nội dung cũ **KHÔNG bị ghi đè** — cùng luật với
/// `IM-20`: ghi đè im lặng là sửa lại quá khứ, kéo theo mọi thứ đang dẫn chứng bằng nó.
///
/// 🛑 Vì sao CẶP chứ không phải mình <c>CauseName</c> (dù `T1` nói identity là nguyên nhân):
/// <c>CauseName</c> lấy từ `quanSat`, mà `quanSat` là LỜI MODEL VIẾT — "Danh sách rỗng", "Ô
/// trống nhưng chọn được" trùng nhau giữa các nhóm là chuyện thường. So tên một mình thì bản
/// nháp của nhóm thứ hai bị gộp ÂM THẦM vào record của nhóm thứ nhất: nạp 3, tạo 1, trả 200,
/// không ai biết. Bộ test API đã bắt đúng ca đó.
///
/// ⚠ CHƯA CÓ UNIQUE INDEX trên <c>(tenant_id, cause_name)</c>. Nghĩa là hai request nạp
/// cùng lúc VẪN sinh được record trùng — kiểm-rồi-ghi không nguyên tử. Khác với đường tín
/// hiệu (có unique index làm chốt cuối). Ghi rõ ở đây thay vì để người đọc tưởng đã kín;
/// với phạm vi demo (một người gọi tay) thì chưa chặn được việc gì.
/// </summary>
public sealed class SopDraftIngestHandler(AppDbContext db, ITenantContext tenant)
{
    public async Task<SopDraftIngestResult> HandleAsync(SopDraft draft, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var mapped = SopDraftToKnowledge.Map(draft, tenant.TenantId);

        // Tra một lượt cho cả lô, không phải một câu mỗi record.
        //
        // 🛑 KHỬ TRÙNG THEO CẶP (NHÓM, TÊN NGUYÊN NHÂN) — không theo tên nguyên nhân một mình.
        // Bản đầu của handler này so tên nguyên nhân trên toàn tenant, và bộ test API bắt được
        // ngay: hai bản nháp của HAI NHÓM KHÁC NHAU mà có chung một câu quan sát (`quanSat` là
        // lời model viết, rất dễ trùng: "Danh sách rỗng", "Ô trống nhưng chọn được") sẽ bị gộp
        // ÂM THẦM thành một record — nạp 3 nguyên nhân, tạo 1, và không có gì báo.
        //
        // Gộp im lặng tệ hơn trùng lặp: trùng thì đếm ra là thấy, còn gộp thì record của nhóm
        // sau biến mất mà response vẫn trả 200. Cùng họ với lỗi "trường phủ 100% một giá trị"
        // mà dự án đã vấp hai lần trong một ngày (`AR-k`, `AR-l`).
        var nhanNhom = SopDraftToKnowledge.NhanNhom(draft.Nhom);
        var tenNguyenNhan = mapped.Records.Select(r => r.CauseName).Distinct().ToList();
        var daCo = await db.KnowledgeRecords
            .Where(r => r.Assertions.Any(a =>
                a.Kind == AssertionKind.Applicability && a.Content == nhanNhom))
            .Where(r => tenNguyenNhan.Contains(r.CauseName))
            .Select(r => r.CauseName)
            .ToListAsync(ct);

        var daCoSet = daCo.ToHashSet(StringComparer.Ordinal);

        var them = mapped.Records.Where(r => !daCoSet.Contains(r.CauseName)).ToList();
        foreach (var r in them)
        {
            db.KnowledgeRecords.Add(r);
        }

        if (them.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return new SopDraftIngestResult(
            Nhom: draft.Nhom,
            NhanDuoc: mapped.Records.Count,
            DaTao: them.Count,
            DaCoTuTruoc: mapped.Records.Count - them.Count,
            BuocSuaMoCoi: mapped.BuocSuaMoCoi,
            NhanhTroLacLoi: mapped.NhanhTroLacLoi,
            NguyenNhan: them.Select(r => new NguyenNhanDaTao(
                r.Id,
                r.CauseName,
                r.DisplayState,
                r.Assertions.Count)).ToList());
    }
}

/// <summary>
/// Kết quả nạp.
///
/// ⚠ Hai danh sách cảnh báo được trả RA NGOÀI response, không chỉ ghi log. Bản nháp thật do
/// máy sinh CÓ một bước sửa mồ côi (`S8`) — người nạp phải thấy điều đó ngay lúc nạp, chứ
/// không phải phát hiện sáu tuần sau khi có ai hỏi vì sao một nhánh không bao giờ chạy tới.
///
/// ⚠ Cảnh báo KHÔNG chặn việc nạp, cố ý: `S8` là dữ liệu THẬT, chặn nó là không nạp được
/// chính bản nháp đang có. Nói ra rồi vẫn nạp, khác với nuốt im lặng.
/// </summary>
public sealed record SopDraftIngestResult(
    string Nhom,
    int NhanDuoc,
    int DaTao,
    int DaCoTuTruoc,
    IReadOnlyList<string> BuocSuaMoCoi,
    IReadOnlyList<string> NhanhTroLacLoi,
    IReadOnlyList<NguyenNhanDaTao> NguyenNhan);

public sealed record NguyenNhanDaTao(Guid Id, string TenNguyenNhan, string TrangThai, int SoAssertion);
