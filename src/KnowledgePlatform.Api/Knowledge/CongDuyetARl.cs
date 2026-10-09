using KnowledgePlatform.Api.Tenancy;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Cổng `AR-l` cho MỌI lượt duyệt — tri thức lẫn quy trình. Tách ra 2026-10-10 (P4) khi lượt duyệt thứ hai
/// (quy trình) ra đời: hai chỗ tự viết cùng một luật là hai chỗ sẽ lệch nhau.
///
/// 🛑 Duyệt LÀ xuất bản. Ranh giới khách sạn A ↔ B chưa được thực thi ở tầng nào, và rò xảy ra ở khâu xuất
/// bản chứ không ở khâu truy vấn hàng — RLS không cứu được. Nên chế độ shared TỪ CHỐI DUYỆT. Xem
/// <see cref="KnowledgeApprovalHandler"/> cho lập luận đầy đủ; muốn gỡ thì phải giải `AR-l`, không phải sửa đây.
/// </summary>
public static class CongDuyetARl
{
    /// <summary>Lý do chặn, hoặc <c>null</c> nếu được duyệt.</summary>
    public static string? LyDoChan(TenancyOptions tenancy, string thu) =>
        tenancy.Mode == TenancyMode.SharedMultiTenant
            ? $"AR-l chưa giải: chế độ shared không được duyệt {thu}. " +
              "Duyệt là XUẤT BẢN, và ranh giới khách sạn A ↔ B hiện chưa được thực thi ở tầng nào — " +
              "RLS không cứu được kiểu rò này vì nó xảy ra ở khâu xuất bản, không ở khâu truy vấn."
            : null;
}
