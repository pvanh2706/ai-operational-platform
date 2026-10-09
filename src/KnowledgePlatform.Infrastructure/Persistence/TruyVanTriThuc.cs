using KnowledgePlatform.Domain.Knowledge;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Infrastructure.Persistence;

/// <summary>
/// MỘT cách nạp tri thức để TRẢ LỜI hay ĐỔI nó: assertion → liên kết dẫn chứng → mẩu evidence.
///
/// 🛑 Vì sao không để mỗi handler tự viết <c>Include</c>: hai luật quan trọng đọc tới tận mẩu
/// evidence, và cả hai HỎNG IM LẶNG khi thiếu một tầng nạp —
/// <list type="bullet">
///   <item><c>NeedsReview</c> (V3, mẩu nguồn bị rút sau lần duyệt) đọc <c>EvidenceItem.RetractedAt</c>:
///     thiếu tầng cuối thì luật trả <c>false</c>, record trông như chưa có gì đổi;</item>
///   <item><c>ReviseAssertion</c> chép dẫn chứng của bản A sang bản B: thiếu tầng giữa thì chép
///     được 0 dòng, và bản người sửa thành một phát biểu không có nguồn.</item>
/// </list>
/// Không cái nào ném lỗi. Năm chỗ tự viết <c>Include</c> là năm chỗ để quên một tầng.
///
/// ⚠ <see cref="EntityFrameworkQueryableExtensions.AsSplitQuery{TEntity}"/>: ba tầng collection trong
/// một câu JOIN nhân số dòng theo cấp số — một record 10 assertion, mỗi cái 20 mẩu là 200 dòng
/// trùng phần đầu. Tách câu thì mỗi tầng một lượt.
/// </summary>
public static class TruyVanTriThuc
{
    public static IQueryable<KnowledgeRecord> KemDanChung(this IQueryable<KnowledgeRecord> q) =>
        q.Include(r => r.Assertions)
            .ThenInclude(a => a.EvidenceLinks)
            .ThenInclude(l => l.EvidenceItem)
            .AsSplitQuery();
}
