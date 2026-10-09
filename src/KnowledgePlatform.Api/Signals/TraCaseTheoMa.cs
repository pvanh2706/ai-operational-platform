using KnowledgePlatform.Domain.Cases;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Mã ticket → case. MỘT chỗ cho mọi đường nhận một mã do người hoặc model viết: bản nháp SOP (P3) và
/// câu hỏi <c>/hoi</c> (P5). Tách ra 2026-10-10 khi đường thứ hai cần đúng luật của đường thứ nhất.
///
/// Luật: khớp CHÍNH XÁC khoá nguồn trước; trượt thì khớp đuôi <c>:mã</c>. Người và model hay viết mã
/// TRẦN (<c>ES-9001</c>), còn case vào qua connector mang tiền tố nguồn (<c>jira:ES-9001</c>, `G1`).
///
/// 🛑 Khớp đuôi ra HƠN MỘT case thì là MƠ HỒ — không tự chọn. Hai connector cùng đánh số kiểu
/// <c>ES-…</c> là chuyện có thật khi khách có hơn một nguồn; chọn bừa một là nối việc của ticket này vào
/// ticket kia, và không ai nhìn ra.
/// </summary>
public static class TraCaseTheoMa
{
    public static async Task<(Dictionary<string, Guid> TheoMa, IReadOnlyList<string> KhongGiai, IReadOnlyList<string> MoHo)>
        GiaiNhieuAsync(AppDbContext db, IReadOnlyList<string> ma, CancellationToken ct = default)
    {
        var theoMa = new Dictionary<string, Guid>(StringComparer.Ordinal);
        if (ma.Count == 0) return (theoMa, [], []);

        var chinhXac = await db.Cases
            .Where(c => ma.Contains(c.SourceReference))
            .Select(c => new { c.SourceReference, c.Id })
            .ToListAsync(ct);

        foreach (var c in chinhXac) theoMa[c.SourceReference] = c.Id;

        var conLai = ma.Where(m => !theoMa.ContainsKey(m)).ToList();
        var khongGiai = new List<string>();
        var moHo = new List<string>();

        if (conLai.Count == 0) return (theoMa, khongGiai, moHo);

        var mau = conLai.Select(m => "%:" + ThoatLike(m)).ToArray();

        var theoDuoi = await db.Cases
            .Where(c => mau.Any(p => EF.Functions.Like(c.SourceReference, p)))
            .Select(c => new { c.SourceReference, c.Id })
            .ToListAsync(ct);

        foreach (var m in conLai)
        {
            var khop = theoDuoi.Where(c => c.SourceReference.EndsWith(":" + m, StringComparison.Ordinal)).ToList();

            switch (khop.Count)
            {
                case 1: theoMa[m] = khop[0].Id; break;
                case 0: khongGiai.Add(m); break;
                default: moHo.Add(m); break;
            }
        }

        return (theoMa, khongGiai, moHo);
    }

    /// <summary>Một mã → một case, hoặc <c>null</c> khi không có hoặc mơ hồ.</summary>
    public static async Task<CanonicalCase?> TimMotAsync(AppDbContext db, string ma, CancellationToken ct = default)
    {
        var (theoMa, _, _) = await GiaiNhieuAsync(db, [ma.Trim()], ct);
        return theoMa.TryGetValue(ma.Trim(), out var id)
            ? await db.Cases.FirstAsync(c => c.Id == id, ct)
            : null;
    }

    /// <summary>Thoát ký tự đặc biệt của LIKE (PostgreSQL dùng <c>\</c> làm ký tự thoát mặc định).</summary>
    private static string ThoatLike(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal)
         .Replace("%", "\\%", StringComparison.Ordinal)
         .Replace("_", "\\_", StringComparison.Ordinal);
}
