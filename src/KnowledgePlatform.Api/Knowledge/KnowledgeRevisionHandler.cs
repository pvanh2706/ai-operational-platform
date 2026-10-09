using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Nạp BẢN B — thứ người làm support sửa lại — vào kho, mà KHÔNG đè bản A.
///
/// 🛑 **ĐÂY LÀ VẾ CÒN THIẾU CỦA `M2`.** `M2` = *số nháp được duyệt + mức sửa `diff(A, B)`*,
/// trong đó A là bản máy sinh và B là bản người sửa. Tính tới 2026-09-13, A đã nằm trong
/// kho nhưng **B chưa có đường nào để vào** — nên `M2` không đo được gì, dù cả hai nửa của
/// nó đều đã được thiết kế từ lâu.
///
/// Và nó phải là một đường RIÊNG, không gộp vào đường duyệt: `S7` nói duyệt là *mở quyền
/// xem*, còn đây là *đổi nội dung*. Gộp hai việc thì không phân biệt được "người duyệt đồng
/// ý nguyên văn" với "người duyệt sửa rồi mới đồng ý" — mà đúng khoảng cách giữa hai ca đó
/// LÀ con số `M2` muốn đo.
///
/// ⚠ **Lô bị từ chối TRỌN VẸN, không sửa một nửa.** Kiểm hết mọi mục trước khi đổi dòng đầu
/// tiên — cùng luật với `IM-16` và với đường nạp evidence. Sửa được 3/5 rồi báo lỗi thì
/// người duyệt không biết phải gửi lại những mục nào.
///
/// ⚠ **Sửa xong thì record thành `NEEDS_REVIEW`, và đó là ĐÚNG.** `V3`: assertion mới hơn
/// lần duyệt gần nhất sẽ kích hoạt cờ. Tri thức KHÔNG bị rút khỏi đường trả lời (V3 nói gắn
/// cờ chứ không rút) nhưng ai hỏi cũng thấy ngay là nó đã đổi kể từ lần duyệt cuối. Muốn
/// sạch cờ thì duyệt lại — và lần duyệt lại đó chính là vế "số nháp được duyệt" của `M2`.
/// </summary>
public sealed class KnowledgeRevisionHandler(AppDbContext db)
{
    public async Task<KetQuaSua> SuaAsync(Guid id, YeuCauSua yeuCau, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);

        var record = await db.KnowledgeRecords
            .KemDanChung()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (record is null)
        {
            return KetQuaSua.KhongThay();
        }

        // --- Kiểm TOÀN BỘ lô trước khi đổi dòng đầu tiên ---
        var loi = new Dictionary<string, string[]>();
        for (var i = 0; i < yeuCau.Sua.Count; i++)
        {
            var m = yeuCau.Sua[i];
            var cu = record.Assertions.SingleOrDefault(a => a.Id == m.AssertionId);

            if (cu is null)
            {
                loi[$"[{i}]"] = [$"Assertion {m.AssertionId} không thuộc tri thức {id}."];
            }
            else if (!cu.IsCurrent)
            {
                loi[$"[{i}]"] =
                [
                    $"Assertion {m.AssertionId} đã bị thay bởi một bản sửa trước đó. " +
                    "Sửa bản CÒN HIỆU LỰC — xem GET /knowledge/{id} để biết bản nào đang hiệu lực.",
                ];
            }
            else if (string.IsNullOrWhiteSpace(m.NoiDungMoi))
            {
                loi[$"[{i}]"] = ["noiDungMoi không được để trống."];
            }
            else if (!Enum.TryParse<VerificationLevel>(m.MucKiemChung, ignoreCase: true, out _))
            {
                loi[$"[{i}]"] =
                [
                    $"mucKiemChung không nhận ra giá trị \"{m.MucKiemChung}\". " +
                    $"Nhận: {string.Join(", ", Enum.GetNames<VerificationLevel>())}.",
                ];
            }
            else if (m.NguonGoc is not null
                     && (!Enum.TryParse<Origin>(m.NguonGoc, ignoreCase: true, out var o)
                         || o == Origin.AiInference))
            {
                loi[$"[{i}]"] =
                [
                    $"nguonGoc \"{m.NguonGoc}\" không dùng được. Bản sửa là lời của NGƯỜI, nên " +
                    "AiInference bị từ chối — M2 chỉ đọc được khi hai vế khác origin.",
                ];
            }
        }

        // Cùng một assertion bị sửa hai lần trong MỘT lô: mục thứ hai sẽ vấp bản vừa tạo ra
        // ở mục thứ nhất. Bắt ở đây để lỗi nói đúng nguyên nhân, thay vì báo "đã bị thay".
        var trung = yeuCau.Sua
            .GroupBy(m => m.AssertionId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var t in trung)
        {
            loi[$"assertionId {t}"] =
            [
                "Cùng một assertion xuất hiện nhiều lần trong một lô. Gộp thành MỘT bản sửa — " +
                "hai bản sửa nối tiếp trong cùng một lô làm diff(A,B) có hai vế B.",
            ];
        }

        if (loi.Count > 0)
        {
            return KetQuaSua.KhongHopLe(loi);
        }

        // --- Áp dụng ---
        var daSua = new List<MotBanSua>(yeuCau.Sua.Count);
        foreach (var m in yeuCau.Sua)
        {
            var cu = record.Assertions.Single(a => a.Id == m.AssertionId);
            var noiDungCu = cu.Content;
            var kiemChungCu = cu.Verification;
            var nguonGocCu = cu.Origin;

            var moi = record.ReviseAssertion(
                m.AssertionId,
                m.NoiDungMoi,
                // Người làm support sửa một bản nháp là NHẬN ĐỊNH CÓ CHUYÊN MÔN, nên mặc định
                // là HumanAssessment chứ không phải UserConfirmed — `UserConfirmed` nghĩa là
                // "người dùng đã nói vậy", một sự thật về LỜI NÓI chứ không về nội dung (v0.2 §7.5).
                m.NguonGoc is null ? Origin.HumanAssessment : Enum.Parse<Origin>(m.NguonGoc, true),
                Enum.Parse<VerificationLevel>(m.MucKiemChung, true),
                yeuCau.NguoiSua);

            daSua.Add(new MotBanSua(
                Kind: cu.Kind.ToString(),
                BanA: new PhienBan(cu.Id, noiDungCu, kiemChungCu.ToString(), nguonGocCu.ToString()),
                BanB: new PhienBan(moi.Id, moi.Content, moi.Verification.ToString(), moi.Origin.ToString())));
        }

        await db.SaveChangesAsync(ct);

        return KetQuaSua.Xong(new BanGhiDaSua(
            record.Id,
            record.CauseName,
            TrangThai: record.DisplayState,
            CanDuyetLai: record.NeedsReview,
            DaSua: daSua));
    }

    /// <summary>
    /// Đọc lại MỘT tri thức kèm TOÀN BỘ lịch sử assertion — cả bản đã bị thay.
    ///
    /// 🛑 Đây là chỗ `diff(A, B)` ĐỌC ĐƯỢC. Đường `/hoi` cố ý chỉ trả bản còn hiệu lực (người
    /// đang xử lý ticket không cần lịch sử), nên nếu không có đường này thì bản A vẫn nằm
    /// trong database mà **không có cách nào lấy ra** — tức `M2` vẫn không đo được, dù dữ
    /// liệu đã đủ. Ghi lại vì đó là một cách hỏng rất im lặng: dữ liệu đúng, đường đọc thiếu.
    /// </summary>
    public async Task<LichSuTriThuc?> DocAsync(Guid id, CancellationToken ct = default)
    {
        var record = await db.KnowledgeRecords
            .KemDanChung()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (record is null) return null;

        return new LichSuTriThuc(
            record.Id,
            record.CauseName,
            record.DisplayState,
            record.VisibilityScope?.ToWire(),
            CanDuyetLai: record.NeedsReview,
            Assertion: [.. record.Assertions
                .OrderBy(a => a.CreatedAt)
                .Select(a => new AssertionCoLichSu(
                    a.Id,
                    a.Kind.ToString(),
                    a.Content,
                    a.Verification.ToString(),
                    a.Origin.ToString(),
                    a.ActorLabel,
                    a.IsCurrent,
                    a.ReplacedByAssertionId,
                    a.CreatedAt,
                    DanChungMau.Tu(a)))]);
    }
}

/// <param name="NguoiSua">Ai sửa. Vào <c>ActorLabel</c> của bản mới, để `M2` tra ngược được.</param>
public sealed record YeuCauSua(string NguoiSua, IReadOnlyList<MotMucSua> Sua);

/// <param name="MucKiemChung">Tên một <see cref="VerificationLevel"/>.</param>
/// <param name="NguonGoc">
/// Tên một <see cref="Origin"/>. Bỏ trống thì là <see cref="Origin.HumanAssessment"/>.
/// <see cref="Origin.AiInference"/> bị TỪ CHỐI — xem <c>KnowledgeRecord.ReviseAssertion</c>.
/// </param>
public sealed record MotMucSua(Guid AssertionId, string NoiDungMoi, string MucKiemChung, string? NguonGoc);

public sealed record PhienBan(Guid Id, string NoiDung, string MucKiemChung, string NguonGoc);

/// <summary>Một lần sửa, bày CẢ HAI VẾ ngay trong response — `M2` đọc được mà không cần tra lại.</summary>
public sealed record MotBanSua(string Kind, PhienBan BanA, PhienBan BanB);

public sealed record BanGhiDaSua(
    Guid Id,
    string TenNguyenNhan,
    string TrangThai,
    bool CanDuyetLai,
    IReadOnlyList<MotBanSua> DaSua);

/// <param name="DanChung">
/// Mẩu nào chống lưng phát biểu này (ô `H`, P3 2026-10-10). Trước P3 mảng này luôn rỗng vì
/// <c>assertion_evidence</c> không có dòng nào.
/// </param>
public sealed record AssertionCoLichSu(
    Guid Id,
    string Kind,
    string NoiDung,
    string MucKiemChung,
    string NguonGoc,
    string? NguoiGhi,
    bool ConHieuLuc,
    Guid? BiThayBoi,
    DateTimeOffset LucTao,
    IReadOnlyList<DanChungMau> DanChung);

/// <summary>
/// Một mẩu dẫn chứng của một phát biểu — TRỎ về nguồn, KHÔNG mang nội dung.
///
/// ⚠ Cố ý không trả <c>Content</c> của mẩu: đọc nội dung đi đường của nó
/// (<c>GET /cases/{khoaNguon}/evidence</c>), nơi luật rút lại (`AR-m`) đã được thực thi. Trả nội
/// dung ở đây là mở đường đọc thứ hai cho cùng một dữ liệu, và đường thứ hai là đường sẽ quên luật.
/// </summary>
/// <param name="DaRutLai">Người gửi đã rút mẩu này (`AR-m`). Nó vẫn hiện ra để thấy được VÌ SAO record cần duyệt lại.</param>
public sealed record DanChungMau(Guid MauId, string? KhoaNguonMau, string QuanHe, string? GhiChu, bool DaRutLai)
{
    public static IReadOnlyList<DanChungMau> Tu(Assertion a) =>
    [
        .. a.EvidenceLinks.Select(l => new DanChungMau(
            l.EvidenceItemId,
            l.EvidenceItem?.SourceReference,
            l.Relation.ToString(),
            l.Note,
            l.EvidenceItem?.RetractedAt is not null))
    ];
}

public sealed record LichSuTriThuc(
    Guid Id,
    string TenNguyenNhan,
    string TrangThai,
    string? PhamViXem,
    bool CanDuyetLai,
    IReadOnlyList<AssertionCoLichSu> Assertion);

/// <summary>Ba kết cục, để endpoint chọn đúng mã HTTP.</summary>
public sealed record KetQuaSua(
    BanGhiDaSua? BanGhi,
    IReadOnlyDictionary<string, string[]>? Loi,
    bool ThayBanGhi)
{
    public static KetQuaSua Xong(BanGhiDaSua b) => new(b, null, true);
    public static KetQuaSua KhongThay() => new(null, null, false);
    public static KetQuaSua KhongHopLe(IReadOnlyDictionary<string, string[]> loi) => new(null, loi, true);
}
