using KnowledgePlatform.Domain.Evidence;
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

        var tatCa = await db.EvidenceItems
            .Where(e => e.ObservedInCaseId == c.Id)
            .OrderBy(e => e.ObservedAt ?? e.IngestedAt)
            .Select(e => new
            {
                e.Id,
                e.SourceReference,
                e.Content,
                MayDoc = e.MachineReadability.ToString(),
                e.ObservedAt,
                e.IngestedAt,
                e.SubTenantCode,
                e.RetractedAt,
                e.RetractedBy,
                e.RetractedReason,
            })
            .ToListAsync(ct);

        // 🛑 HAI DANH SÁCH, HAI KIỂU — không phải một danh sách kèm một cờ `daRutLai`.
        //
        // `AR-m` nói mẩu đã bị người gửi rút lại KHÔNG được Path A gom vào bản nháp, còn
        // `G6`/`AP3` nói dấu vết phải GIỮ (người duyệt cần phân biệt "đã che/đã rút" với
        // "không có gì"). Một cờ boolean thoả được cả hai câu đó trên giấy, nhưng nó đặt
        // trách nhiệm lọc lên MỌI nơi gọi về sau — và chỗ quên lọc đầu tiên sẽ không đỏ
        // test nào, chỉ âm thầm đưa đoạn overbooking của khách A vào SOP mã đặt phòng.
        // Hai KIỂU khác nhau thì quên lọc là không biên dịch được.
        var dung = tatCa
            .Where(e => e.RetractedAt is null)
            .Select(e => new MauEvidence(
                e.Id, e.SourceReference, e.Content, e.MayDoc,
                e.ObservedAt, e.IngestedAt, e.SubTenantCode))
            .ToList();

        var daRut = tatCa
            .Where(e => e.RetractedAt is not null)
            .Select(e => new MauEvidenceDaRutLai(
                e.Id, e.SourceReference, e.MayDoc, e.ObservedAt,
                e.RetractedAt!.Value, e.RetractedBy!, e.RetractedReason!))
            .ToList();

        return new CaseEvidenceView(
            c.Id, c.SourceReference, c.Subject, dung.Count, daRut.Count, dung, daRut);
    }

    /// <summary>
    /// Điều kiện MỞ LẠI của `AR-l`, viết thành một câu hỏi CHẠY ĐƯỢC.
    ///
    /// Tài liệu đã cố ý phát biểu điều kiện đó dạng đo được chứ không phải một ngày trong
    /// lịch: *"khi có MỘT nguồn cho ra ≥2 giá trị PHÂN BIỆT cho sub-tenant trên corpus"*.
    /// Một điều kiện chỉ nằm trong tài liệu là một điều kiện sẽ được tuyên bố là "đã đạt"
    /// bởi người không đọc nó — nên nó nằm ở đây, và nó tự đếm.
    ///
    /// 🛑 Hai giá trị dành riêng KHÔNG được tính. Nếu tính thì chỉ cần đổi từ
    /// <c>khong-xac-dinh</c> sang <c>khong-thuoc-khach-san</c> là "đạt điều kiện", trong khi
    /// không khách sạn nào được phân biệt với khách sạn nào — đúng thứ `Mã khách sạn = -1.0`
    /// đã dạy, chỉ khác là lần này ta tự gây ra.
    /// </summary>
    public async Task<SubTenantCoverage> DoPhuMaKhachSanAsync(CancellationToken ct = default)
    {
        var ma = await db.EvidenceItems
            .Select(e => e.SubTenantCode)
            .ToListAsync(ct);

        var thatSu = ma
            .Where(x => x is not null && !SubTenantCodes.LaGiaTriDanhRieng(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var chuaKhai = ma.Count(x => x is null);
        var danhRieng = ma.Count(x => SubTenantCodes.LaGiaTriDanhRieng(x));

        // 🛑 VÌ SAO CÓ Ô `canTro` RIÊNG, và vì sao nó KHÔNG được gộp vào `datDieuKienMoLai`.
        //
        // Điều kiện của `AR-l` viết nguyên văn là "≥2 giá trị PHÂN BIỆT", nên ô kia trả lời
        // ĐÚNG câu đó và không hơn — sửa ngưỡng của một quyết định đã chốt là việc của chủ
        // dự án, không phải của chỗ này. Nhưng đo thật trên `kp_dev` ngày 2026-09-16 cho ra
        // một cặp số mà chữ "đạt" một mình sẽ nói dối:
        //
        //     mã phân biệt 2  ->  ĐẠT       (và 2 mã đó đến từ đúng 2 dòng vừa nạp lúc thử)
        //     chưa khai  137/141            (97% kho không có gì để thực thi ranh giới)
        //
        // Một người đọc `datDieuKienMoLai: true` rồi gỡ cổng 409 sẽ mở ranh giới cho một kho
        // mà 97% số mẩu không biết thuộc khách sạn nào. `canTro` nói thẳng chỗ đó ra, và để
        // nguyên quyền quyết định cho người đọc.
        var canTro = new List<string>();

        if (chuaKhai > 0)
        {
            canTro.Add(
                $"{chuaKhai}/{ma.Count} mẩu CHƯA KHAI (nạp trước luật AR-l, cột NULL). Ranh giới " +
                "khách sạn không thực thi được trên những mẩu đó — muốn có mã thì phải nạp lại " +
                "vào kho sạch, vì K-B3 cấm ghi đè quá khứ.");
        }

        if (danhRieng > 0)
        {
            canTro.Add(
                $"{danhRieng}/{ma.Count} mẩu khai giá trị dành riêng ({SubTenantCodes.GiaTriDanhRieng}) " +
                "— đã được hỏi, nhưng vẫn không phân biệt được khách sạn nào với khách sạn nào.");
        }

        return new SubTenantCoverage(
            TongMau: ma.Count,
            ChuaKhai: chuaKhai,
            DaKhaiKhongXacDinh: ma.Count(x => string.Equals(x, SubTenantCodes.KhongXacDinh, StringComparison.OrdinalIgnoreCase)),
            DaKhaiKhongThuocKhachSan: ma.Count(x => string.Equals(x, SubTenantCodes.KhongThuocKhachSan, StringComparison.OrdinalIgnoreCase)),
            SoMaPhanBiet: thatSu.Count,
            MaDaThay: thatSu,
            DatDieuKienMoLai: thatSu.Count >= 2,
            CanTro: canTro,
            DieuKien: "AR-l mở lại khi có ≥2 giá trị PHÂN BIỆT (không tính " +
                      $"{SubTenantCodes.GiaTriDanhRieng}). Chưa đạt thì chế độ shared vẫn TỪ CHỐI DUYỆT (409). " +
                      "⚠ datDieuKienMoLai trả lời ĐÚNG câu chữ của AR-l và KHÔNG hơn — đọc canTro trước khi gỡ cổng.");
    }
}

/// <param name="ChuaKhai">
/// Số mẩu có <c>SubTenantCode</c> NULL — nạp TRƯỚC khi có luật `AR-l`, chưa ai từng được hỏi.
/// KHÁC HẲN <paramref name="DaKhaiKhongXacDinh"/>, và đó là lý do hai con số này không gộp.
/// </param>
/// <param name="MaDaThay">
/// Chính các giá trị phân biệt, không chỉ số đếm. Một con số "2" mà không nói ra HAI GIÁ TRỊ
/// NÀO thì không kiểm lại được — và phép đếm này là thứ gỡ một cổng bảo mật.
/// </param>
/// <param name="DatDieuKienMoLai">
/// Trả lời ĐÚNG câu chữ của `AR-l` ("≥2 giá trị phân biệt") và KHÔNG hơn.
/// 🛑 <c>true</c> ở đây KHÔNG có nghĩa "gỡ được cổng 409" — đọc <paramref name="CanTro"/>.
/// </param>
/// <param name="CanTro">
/// Những thứ làm cho việc mở lại vẫn là non, dù câu chữ đã đạt. Rỗng = không còn gì để nói.
/// </param>
public sealed record SubTenantCoverage(
    int TongMau,
    int ChuaKhai,
    int DaKhaiKhongXacDinh,
    int DaKhaiKhongThuocKhachSan,
    int SoMaPhanBiet,
    IReadOnlyList<string> MaDaThay,
    bool DatDieuKienMoLai,
    IReadOnlyList<string> CanTro,
    string DieuKien);

/// <param name="MayDocDuoc">
/// Nhãn <c>MachineReadability</c> ĐÃ LƯU. Trả ra vì đó chính là trường mà `AR-k` đo được là
/// hằng số `High` ở 128/128 mẩu — không có đường đọc thì lỗi kiểu đó chỉ lộ ra khi có người
/// mở database lên xem.
/// </param>
/// <param name="MaKhachSan">
/// <c>null</c> = mẩu nạp TRƯỚC `AR-l`, chưa ai từng khai. Trả ra vì đó là con số quyết định
/// bao giờ ranh giới khách sạn mở lại được — giấu nó đi thì `AR-l` lại thành một dòng trong
/// tài liệu thay vì một thứ đo được.
/// </param>
public sealed record MauEvidence(
    Guid Id,
    string KhoaNguon,
    string NoiDung,
    string MayDocDuoc,
    DateTimeOffset? QuanSatLuc,
    DateTimeOffset NapLuc,
    string? MaKhachSan);

/// <summary>
/// Mẩu ĐÃ BỊ NGƯỜI GỬI RÚT LẠI — `AR-m`. Kiểu RIÊNG, không phải <see cref="MauEvidence"/>
/// kèm một cờ, để chỗ nào quên lọc thì không biên dịch được thay vì âm thầm gom nhầm.
///
/// ⚠ **KHÔNG mang <c>NoiDung</c>, và đó là điểm chính.** Dấu vết được giữ (ai rút, lúc nào,
/// vì sao) nhưng nội dung đã rút thì không đi tiếp qua đường đọc — nếu không thì mọi thứ
/// `AR-m` sinh ra để chặn vẫn đi ra bằng đúng endpoint này. Cần đọc lại nguyên văn thì đó là
/// một câu hỏi khác (soi kho, có kiểm soát), không phải đường đọc thường ngày.
/// </summary>
public sealed record MauEvidenceDaRutLai(
    Guid Id,
    string KhoaNguon,
    string MayDocDuoc,
    DateTimeOffset? QuanSatLuc,
    DateTimeOffset RutLaiLuc,
    string RutLaiBoi,
    string RutLaiLyDo);

/// <param name="SoMau">Số mẩu DÙNG ĐƯỢC — đã trừ mẩu bị rút lại.</param>
/// <param name="SoMauDaRutLai">Đếm riêng. `G6`: "không có gì" và "có nhưng đã rút" phải phân biệt được.</param>
public sealed record CaseEvidenceView(
    Guid CaseId,
    string KhoaNguon,
    string TieuDe,
    int SoMau,
    int SoMauDaRutLai,
    IReadOnlyList<MauEvidence> Mau,
    IReadOnlyList<MauEvidenceDaRutLai> MauDaRutLai);
