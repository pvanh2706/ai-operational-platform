using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Đường ĐỌC evidence của một case — `AR-g`, chốt 2026-09-15 (phiếu `docs/14` §C4).
///
/// Trước đường này, hệ thống có đường GHI mà **không có đường ĐỌC**: không endpoint nào trả
/// về evidence của một case, và bộ test phải mở thẳng `AppDbContext` để kiểm những hành vi
/// âm thầm (nhãn `MachineReadability` đã lưu là gì, gửi lại có bị ghi đè không). Một cơ chế
/// chỉ kiểm được bằng cách đi vòng qua chính sản phẩm là một cơ chế chưa có ai dùng thử.
///
/// ⚠ Tra theo **KHOÁ NGUỒN** chứ không theo `caseId`: đó là thứ bên gửi ĐÃ biết về case
/// của chính họ — cùng lý do `AR-f` chọn khoá nguồn làm mỏ neo. Bắt họ nhớ thêm một `Guid`
/// là bắt họ giữ một bảng ánh xạ, đúng thứ `IM-15` nói tích hợp hệ thống làm không nổi.
///
/// 🛑 KHÔNG có đường đọc evidence KHÔNG thuộc case nào. `K-B9` cho phép loại đó tồn tại, và
/// đường GHI đã nhận nó (`caseSourceReference` nhận null) — nhưng đọc nó ra cần một câu hỏi
/// khác hẳn ("evidence nào không gắn case nào?"), và chưa ai cần. Ghi ở đây để người đọc sau
/// phân biệt "đã cân nhắc rồi rút gọn" với "bỏ sót".
/// </summary>
public sealed class CaseEvidenceReadHandler(AppDbContext db)
{
    public async Task<CaseEvidenceView?> DocAsync(
        string caseSourceReference, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(caseSourceReference)) return null;

        var khoa = caseSourceReference.Trim();

        // Không cần điều kiện TenantId: global query filter thêm nó, RLS chặn ở tầng DB.
        // Nên case của khách A tra từ request của khách B biểu hiện thành "không tìm thấy",
        // đúng như khi nó không tồn tại thật.
        var c = await db.Cases
            .Where(x => x.SourceReference == khoa)
            .Select(x => new { x.Id, x.SourceReference, x.Subject })
            .FirstOrDefaultAsync(ct);

        if (c is null) return null;

        var mau = await db.EvidenceItems
            .Where(e => e.ObservedInCaseId == c.Id)
            .OrderBy(e => e.ObservedAt ?? e.IngestedAt)
            .Select(e => new MauEvidence(
                e.Id,
                e.SourceReference,
                e.Content,
                e.MachineReadability.ToString(),
                e.ObservedAt,
                e.IngestedAt))
            .ToListAsync(ct);

        return new CaseEvidenceView(c.Id, c.SourceReference, c.Subject, mau.Count, mau);
    }
}

/// <param name="MayDocDuoc">
/// Nhãn <c>MachineReadability</c> ĐÃ LƯU. Trả ra vì đó chính là trường mà `AR-k` đo được là
/// hằng số `High` ở 128/128 mẩu — không có đường đọc thì lỗi kiểu đó chỉ lộ ra khi có người
/// mở database lên xem.
/// </param>
public sealed record MauEvidence(
    Guid Id,
    string KhoaNguon,
    string NoiDung,
    string MayDocDuoc,
    DateTimeOffset? QuanSatLuc,
    DateTimeOffset NapLuc);

public sealed record CaseEvidenceView(
    Guid CaseId,
    string KhoaNguon,
    string TieuDe,
    int SoMau,
    IReadOnlyList<MauEvidence> Mau);
