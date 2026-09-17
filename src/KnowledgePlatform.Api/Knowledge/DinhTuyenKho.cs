using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Persistence;
using KnowledgePlatform.Infrastructure.Sop;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Định tuyến bằng KHO KẾT QUẢ ĐÃ NẠP của khách hàng này, rơi về bảng đã đo nếu chưa có.
///
/// Đây là bản hiện thực của <see cref="IDinhTuyenNhom"/> cho **vòng không-credit**: người vận
/// hành xuất ticket ra, một ngữ cảnh AI khác đọc và trả nhóm, rồi nạp về. Xem
/// <see cref="Domain.Sop.DinhTuyenDaNap"/>.
///
/// 🛑 **Thứ tự tra là KHO TRƯỚC, BẢNG ĐO SAU — và đó là một quyết định, không phải tiện tay.**
/// Bảng `dinh-tuyen-150-case.json` là một PHÉP ĐO ngày 2026-09-10, không phải dữ liệu vận
/// hành. Nếu người vận hành nạp một kết quả cho cùng một mã ticket thì họ đang SỬA một thứ
/// họ thấy sai — ý muốn của người phải thắng ảnh chụp của phép đo.
///
/// ⚠ Và <c>Nguon</c> trả ra NÓI RÕ kết quả đến từ đâu (<c>nguoi-nap:</c> hay
/// <c>tra-san:</c>). Trộn hai nguồn vào một câu trả lời không phân biệt được là cách nhanh
/// nhất để về sau không ai chấm lại được cái gì — đúng chỗ mà `R-K4` đã phải hạ cấp một kết
/// luận vì nó bị viết ở sai mức.
/// </summary>
public sealed class DinhTuyenKho(AppDbContext db, DinhTuyenTraSanNguon traSan) : IDinhTuyenNhom
{
    /// <summary>Tiền tố nguồn cho kết quả do người vận hành nạp.</summary>
    public const string TienToNguoiNap = "nguoi-nap:";

    /// <summary>Tiền tố nguồn cho kết quả lấy từ bảng đã đo.</summary>
    public const string TienToTraSan = "tra-san:";

    public async Task<KetQuaDinhTuyen> DinhTuyenAsync(YeuCauDinhTuyen yeuCau, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);

        var ma = yeuCau.MaTicket?.Trim();

        if (!string.IsNullOrEmpty(ma))
        {
            var daNap = await db.Set<DinhTuyenDaNap>()
                .FirstOrDefaultAsync(x => x.KhoaNguon == ma, ct);

            if (daNap is not null)
            {
                return daNap.BietNhom
                    ? new KetQuaDinhTuyen(daNap.Nhom, TienToNguoiNap + daNap.Nguon, daNap.GiaiThich)
                    : KetQuaDinhTuyen.KhongXacDinh(TienToNguoiNap + daNap.Nguon, daNap.GiaiThich);
            }
        }

        // Chưa nạp -> rơi về bảng đã đo. Nó tự trả KHÔNG BIẾT cho mọi mã ngoài 150 ticket,
        // và đó là câu trả lời trung thực duy nhất của một bảng tra.
        var cu = await traSan.Bang.DinhTuyenAsync(yeuCau, ct);

        return cu with { Nguon = TienToTraSan + cu.Nguon };
    }
}

/// <summary>
/// Bọc <see cref="DinhTuyenTraSan"/> để nó sống ở phạm vi singleton (đọc file một lần) trong
/// khi <see cref="DinhTuyenKho"/> phải là scoped (cần <c>AppDbContext</c> của request).
/// </summary>
public sealed class DinhTuyenTraSanNguon(DinhTuyenTraSan bang)
{
    public DinhTuyenTraSan Bang { get; } = bang;
}
