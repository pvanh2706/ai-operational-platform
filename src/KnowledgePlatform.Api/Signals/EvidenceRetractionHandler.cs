using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Đánh dấu một mẩu evidence là ĐÃ BỊ NGƯỜI GỬI RÚT LẠI — `AR-m`, chốt 2026-09-15
/// (phiếu `docs/14` §B1 = B1-1).
///
/// 🛑 **ĐÂY LÀ ĐƯỜNG CỦA NGƯỜI, VÀ ĐÓ LÀ TOÀN BỘ NỘI DUNG CỦA QUYẾT ĐỊNH.** `B1-1` chọn
/// *ít tín hiệu mà mỗi cái đều đúng* thay vì *nhiều tín hiệu mà không biết cái nào tin được*:
/// người gửi rút lại bằng CÂU CHỮ ("dạ anh/chị bỏ qua giúp em tin nhắn trên ạ"), và suy ra
/// từ câu chữ là ĐOÁN — trái `G6`/`AP3`. Nên đường NẠP không có cách nào đặt ba trường này,
/// và endpoint này bắt buộc khai người đánh dấu.
///
/// ⚠ **Khoá nguồn nằm trong THÂN, không nằm trong đường dẫn — và đây là chỗ học từ `AR-g`
/// ngay trong cùng tuần.** `AR-g` vừa đo được rằng khoá nguồn chứa dấu <c>/</c> KHÔNG đọc ra
/// được qua đường dẫn (ASP.NET giải mã <c>%2F</c> TRƯỚC khi định tuyến). Khoá của evidence
/// còn khó hơn khoá của case: nó chứa <c>#</c> (<c>jira:ES-1234#comment-7</c>), thứ mà client
/// nào quên mã hoá là cắt mất nửa sau ngay tại trình duyệt. Đặt vào thân là tránh cả hai, và
/// nó KHÔNG mâu thuẫn với `C4-1`: `C4-1` chọn hình dạng cho đường ĐỌC của case, không phải
/// cho mọi đường có khoá nguồn.
/// </summary>
public sealed class EvidenceRetractionHandler(AppDbContext db)
{
    public async Task<KetQuaRutLai> RutLaiAsync(
        string khoaNguon, string nguoiDanhDau, string lyDo, CancellationToken ct = default)
    {
        var khoa = khoaNguon.Trim();

        // Không cần điều kiện TenantId: global query filter thêm nó, RLS chặn ở tầng DB.
        var mau = await db.EvidenceItems
            .FirstOrDefaultAsync(e => e.SourceReference == khoa, ct);

        if (mau is null) return new KetQuaRutLai(KetCucRutLai.KhongTimThay, null, null, null);

        if (mau.DaRutLai)
        {
            // Giữ nguyên lần đánh dấu ĐẦU. Ghi đè là xoá mất tác giả và lý do của lần đó —
            // tức mất đúng thứ ba cột này sinh ra để giữ.
            return new KetQuaRutLai(
                KetCucRutLai.DaRutLaiTuTruoc, mau.Id, mau.RetractedBy, mau.RetractedAt);
        }

        mau.RutLai(nguoiDanhDau, lyDo);
        await db.SaveChangesAsync(ct);

        return new KetQuaRutLai(KetCucRutLai.DaDanhDau, mau.Id, mau.RetractedBy, mau.RetractedAt);
    }
}

/// <param name="KhoaNguon">Khoá nguồn của mẩu — <c>"jira:ES-346661#description"</c>.</param>
/// <param name="NguoiDanhDau">
/// Ai đánh dấu. Bắt buộc: `B1-1` chốt CHỈ NGƯỜI điền tay, và một dấu vết không có tác giả
/// thì không phân biệt được với một dấu vết do script đặt — đúng bài học của `M2` phần (c).
/// </param>
/// <param name="LyDo">
/// Vì sao. Bắt buộc, và cố ý không có danh sách chọn sẵn: lý do thật của ca `ES-346661` là
/// *"người gửi nói gửi nhầm nội dung, đoạn này là của case khác"* — một câu mà không bộ mã
/// dựng sẵn nào đoán trước được.
/// </param>
public sealed record YeuCauRutLai(string? KhoaNguon, string? NguoiDanhDau, string? LyDo);

public enum KetCucRutLai
{
    KhongTimThay,
    DaRutLaiTuTruoc,
    DaDanhDau,
}

public sealed record KetQuaRutLai(
    KetCucRutLai KetCuc,
    Guid? EvidenceId,
    string? NguoiDanhDau,
    DateTimeOffset? Luc);
