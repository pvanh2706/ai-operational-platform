using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Luồng duyệt — mảnh (b). `KnowledgeRecord.Approve()` đã có ở Domain từ 2026-08-24 nhưng
/// tới 2026-09-13 vẫn **không nơi nào gọi**; đây là đường đầu tiên với tới được nó.
///
/// `S7`: duyệt nội dung và mở quyền xem là MỘT hành động, log cả hai. Điều đó đã được ép ở
/// chữ ký của <c>Approve()</c> — không truyền phạm vi xem thì không gọi được.
///
/// 🛑 **CỔNG `AR-l` — VÀ NÓ LÀ CỔNG CHẠY ĐƯỢC, KHÔNG PHẢI LỜI DẶN.**
/// Tài liệu dự án đã ghi rõ từ 2026-09-07: ranh giới KHÁCH SẠN A ↔ B chưa được thực thi ở
/// bất kỳ tầng nào, và phải mở lại **trước** khi có luồng duyệt thật — vì rò xảy ra ở khâu
/// **xuất bản SOP**, không ở khâu truy vấn hàng. RLS không cứu được kiểu rò đó: Path A gom
/// evidence của nhiều khách sạn (đúng quyền, RLS xanh) rồi sinh một bản nháp được duyệt và
/// hiện cho mọi người. Dữ liệu đi qua ranh giới bằng CỬA CHÍNH.
///
/// Phạm vi demo chốt 2026-09-13 tránh chuyện đó bằng cách khoá ở một tenant, không xuất bản
/// ra ngoài. Nhưng một phạm vi chỉ nằm trong tài liệu thì không chặn được gì — nên nó nằm ở
/// đây: **chế độ shared thì TỪ CHỐI DUYỆT.** Muốn duyệt ở chế độ shared thì phải giải
/// `AR-l` trước, không phải sửa dòng này.
/// </summary>
public sealed class KnowledgeApprovalHandler(AppDbContext db, IOptions<TenancyOptions> tenancy)
{
    public async Task<KetQuaDuyet> DuyetAsync(
        Guid id, string nguoiDuyet, VisibilityScope phamViXem, bool xacNhanDuLieuThuongMai,
        string? lyDo, CancellationToken ct = default)
    {

        if (CongDuyetARl.LyDoChan(tenancy.Value, "tri thức") is { } chan)
        {
            return KetQuaDuyet.BiChan(chan);
        }

        var record = await db.KnowledgeRecords
            .KemDanChung()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (record is null)
        {
            // Không phân biệt "không tồn tại" với "của khách khác": global query filter và
            // RLS làm record của tenant khác biểu hiện y hệt như không có. Cố ý.
            return KetQuaDuyet.KhongThay();
        }

        var truoc = record.DisplayState;

        // D4: chuỗi này là NGƯỜI, và hệ thống không có cách nào tự chứng minh điều đó.
        // Nó chỉ làm được một việc: bắt phải khai ra, để lần duyệt có chủ thể tra được.
        record.Approve(nguoiDuyet, phamViXem, xacNhanDuLieuThuongMai, lyDo);

        await db.SaveChangesAsync(ct);

        return KetQuaDuyet.Xong(new BanGhiDaDuyet(
            record.Id,
            record.CauseName,
            TrangThaiTruoc: truoc,
            TrangThaiSau: record.DisplayState,
            PhamViXem: record.VisibilityScope!.Value.ToWire(),
            NguoiDuyet: record.LastApproval!.ApprovedByActor,
            LucDuyet: record.LastApproval.ApprovedAt,
            SoAssertion: record.Assertions.Count));
    }
}

/// <param name="NguoiDuyet">
/// Ai duyệt. `S7` đòi người duyệt là người thấy được TẤT CẢ nguồn — điều đó phải được kiểm
/// ở tầng ứng dụng trước khi gọi vào Domain, và **hiện chưa kiểm được** (`AR-e` còn mở:
/// chưa có xác thực người gọi). Trường này vì thế là một lời KHAI, không phải một bằng chứng.
/// </param>
/// <param name="PhamViXem">
/// Nhãn quyền xem. ⚠ Tập giá trị CHƯA ĐƯỢC KHOÁ (`AR-a`) — cố ý để dạng chuỗi đục thay vì
/// tự phát minh một enum ở tầng code.
/// </param>
/// <param name="XacNhanDuLieuThuongMai">
/// `AR-n` (B2b-1): người duyệt khai đã soát dữ liệu THƯƠNG MẠI trong các mẩu nguồn —
/// giá hợp đồng theo năm, hạn hợp đồng, ý định ngừng dùng của một khách hàng
/// (`ES-346608#comment-803265`). Thiếu hoặc <c>false</c> thì **400**.
///
/// 🛑 Đây là LỜI KHAI, không phải phép kiểm — loại dữ liệu này không có hình dạng nào để bắt
/// bằng regex (chính `AR-n` ghi thế, và đó là lý do nhánh này chọn NGƯỜI). Nó làm được đúng
/// một việc: buộc phải có người nói ra, và lưu lại ai đã nói.
/// </param>
public sealed record YeuCauDuyet(
    string NguoiDuyet, string PhamViXem, bool? XacNhanDuLieuThuongMai, string? LyDo);

public sealed record BanGhiDaDuyet(
    Guid Id,
    string TenNguyenNhan,
    string TrangThaiTruoc,
    string TrangThaiSau,
    string PhamViXem,
    string NguoiDuyet,
    DateTimeOffset LucDuyet,
    int SoAssertion);

/// <summary>Ba kết cục, phân biệt tường minh để endpoint chọn đúng mã HTTP.</summary>
public sealed record KetQuaDuyet(BanGhiDaDuyet? BanGhi, string? LyDoChan, bool ThayBanGhi)
{
    public static KetQuaDuyet Xong(BanGhiDaDuyet b) => new(b, null, true);
    public static KetQuaDuyet KhongThay() => new(null, null, false);
    public static KetQuaDuyet BiChan(string lyDo) => new(null, lyDo, false);
}
