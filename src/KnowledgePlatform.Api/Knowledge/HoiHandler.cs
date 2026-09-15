using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Đường vào cho một CÂU HỎI — mảnh (c), và cổng "KHÔNG BIẾT" — mảnh (d).
///
/// Tới 2026-09-13 host chỉ có hai endpoint NẠP cộng hai endpoint hạ tầng: **không có cửa nào
/// cho một câu hỏi đi vào**. Đây là cửa đó.
///
/// 🛑 **CỔNG "KHÔNG BIẾT" LÀ PHẦN CỦA SẢN PHẨM, KHÔNG PHẢI TUỲ CHỌN** — và nó có HAI CỬA
/// ĐỘC LẬP. Cửa thứ hai là cửa dễ quên nhất, nên nói trước:
///
/// <list type="number">
///   <item><b>Định tuyến không biết.</b> `docs/13` §3.2 đã đo: trên 62 case mà hồ sơ KHÔNG
///   BAO GIỜ ghi ra nguyên nhân, bộ định tuyến vẫn dám gán 40,3%. Bật tính năng mà thiếu
///   cửa này thì ~4/10 ticket không xác định được nguyên nhân sẽ nhận một hướng dẫn KHÔNG
///   KIỂM ĐƯỢC.</item>
///   <item><b>Biết nhóm nhưng nhóm đó KHÔNG CÓ SOP ĐÃ DUYỆT.</b> `docs/13` nói thẳng:
///   *"Định tuyến đúng nhóm chỉ có nghĩa NẾU nhóm đó có một SOP đã duyệt để trả về."*
///   Tính tới 2026-09-13 là **0/19 nhóm**. Không có cửa này thì hệ thống định tuyến đúng
///   rồi trả về rỗng, và người dùng đọc rỗng thành "không có vấn đề gì".</item>
/// </list>
///
/// 🛑 **CHỈ TRẢ VỀ RECORD ĐÃ DUYỆT.** Truy vấn lọc <c>Lifecycle == Active</c>, và đó là ràng
/// buộc an toàn cốt lõi của cả đường này: bản nháp do máy sinh nằm ở <c>Draft</c> với
/// <c>Origin.AiInference</c>: chưa người nào nhìn qua. Trả nó ra cho người làm thật là đúng
/// thứ `D4` sinh ra để chặn. Có test riêng giữ điều này.
/// </summary>
public sealed class HoiHandler(AppDbContext db, IDinhTuyenNhom dinhTuyen)
{
    public async Task<CauTraLoi> HoiAsync(CauHoi cauHoi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cauHoi);

        var tuyen = await dinhTuyen.DinhTuyenAsync(
            new YeuCauDinhTuyen(cauHoi.MaTicket, cauHoi.TieuDe, cauHoi.MoTa), ct);

        // --- CỬA 1: định tuyến không biết ---
        if (!tuyen.BietNhom)
        {
            return CauTraLoi.KhongBiet(
                LyDo: "dinh-tuyen-khong-biet",
                GiaiThich: tuyen.GiaiThich,
                Nguon: tuyen.Nguon,
                Nhom: null);
        }

        var nhanNhom = SopDraftToKnowledge.NhanNhom(tuyen.Nhom!);

        // Chỉ record ĐÃ DUYỆT. `Active` là trạng thái duy nhất mà `Approve()` đặt được.
        var records = await db.KnowledgeRecords
            .Include(r => r.Assertions)
            .Where(r => r.Lifecycle == StoredLifecycleState.Active)
            .Where(r => r.Assertions.Any(a =>
                a.Kind == AssertionKind.Applicability && a.Content == nhanNhom))
            .ToListAsync(ct);

        // --- CỬA 2: biết nhóm nhưng nhóm chưa có SOP nào được duyệt ---
        if (records.Count == 0)
        {
            return CauTraLoi.KhongBiet(
                LyDo: "nhom-chua-co-sop-duoc-duyet",
                GiaiThich: $"Định tuyến vào nhóm \"{tuyen.Nhom}\", nhưng nhóm này chưa có tri thức nào " +
                           "được người duyệt. Nạp bản nháp rồi duyệt trước khi hỏi lại.",
                Nguon: tuyen.Nguon,
                Nhom: tuyen.Nhom);
        }

        return new CauTraLoi(
            BietTraLoi: true,
            Nhom: tuyen.Nhom,
            NguonDinhTuyen: tuyen.Nguon,
            GiaiThichDinhTuyen: tuyen.GiaiThich,
            LyDoKhongBiet: null,
            HuongDan: [.. records.Select(ToHuongDan)]);
    }

    /// <summary>
    /// Bày ra cả mức kiểm chứng của từng phát biểu, không chỉ nội dung.
    ///
    /// `S8`/`G4`: bày chỗ yếu ra chính là thứ người đọc cần — giấu nó đi thì người ta không
    /// phản bác được, mà phản bác được là toàn bộ giá trị của cách làm này. Một bước nhận ra
    /// mang nhãn `Speculative` phải đọc ra khác hẳn một bước mang `Supported`.
    /// </summary>
    private static HuongDan ToHuongDan(KnowledgeRecord r) => new(
        Id: r.Id,
        TenNguyenNhan: r.CauseName,
        TrangThai: r.DisplayState,
        PhamViXem: r.VisibilityScope?.ToWire(),
        CachNhanRa: [.. r.Assertions
            .Where(a => a.Kind == AssertionKind.Recognition && a.IsCurrent)
            .Select(a => new PhatBieu(a.Content, a.Verification.ToString(), a.Origin.ToString()))],
        CachXuLy: [.. r.Assertions
            .Where(a => a.Kind == AssertionKind.Handling && a.IsCurrent)
            .Select(a => new PhatBieu(a.Content, a.Verification.ToString(), a.Origin.ToString()))],
        DieuKienApDung: [.. r.Assertions
            .Where(a => a.Kind == AssertionKind.Applicability && a.IsCurrent)
            .Select(a => new PhatBieu(a.Content, a.Verification.ToString(), a.Origin.ToString()))]);
}

/// <summary>
/// Câu hỏi đi vào.
///
/// ⚠ <see cref="MaTicket"/> chỉ dùng được với bản định tuyến TRA SẴN. `docs/13` §1: phép đo
/// dùng đúng thứ có mặt LÚC TICKET ĐẾN — tiêu đề + mô tả. Bản gọi model thật sẽ bỏ qua mã.
/// </summary>
public sealed record CauHoi(string? MaTicket, string? TieuDe, string? MoTa);

/// <param name="BietTraLoi"><c>false</c> nghĩa là KHÔNG BIẾT — một câu trả lời hợp lệ.</param>
/// <param name="LyDoKhongBiet">Cửa nào đã chặn: <c>dinh-tuyen-khong-biet</c> hay <c>nhom-chua-co-sop-duoc-duyet</c>.</param>
public sealed record CauTraLoi(
    bool BietTraLoi,
    string? Nhom,
    string NguonDinhTuyen,
    string? GiaiThichDinhTuyen,
    string? LyDoKhongBiet,
    IReadOnlyList<HuongDan> HuongDan)
{
    public static CauTraLoi KhongBiet(string LyDo, string? GiaiThich, string Nguon, string? Nhom) =>
        new(false, Nhom, Nguon, GiaiThich, LyDo, []);
}

public sealed record HuongDan(
    Guid Id,
    string TenNguyenNhan,
    string TrangThai,
    string? PhamViXem,
    IReadOnlyList<PhatBieu> CachNhanRa,
    IReadOnlyList<PhatBieu> CachXuLy,
    IReadOnlyList<PhatBieu> DieuKienApDung);

/// <summary>Một phát biểu, kèm mức kiểm chứng và nguồn gốc. Cả ba luôn đi cùng nhau (`AP3`).</summary>
public sealed record PhatBieu(string NoiDung, string MucKiemChung, string NguonGoc);
