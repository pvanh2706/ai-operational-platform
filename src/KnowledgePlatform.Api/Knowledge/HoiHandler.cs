using System.Text.Json.Serialization;
using KnowledgePlatform.Api.QuyTrinh;
using KnowledgePlatform.Api.Signals;
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
///
/// 🎯 **Từ 2026-10-10 (P5) đây là ô `G` + `I` của sơ đồ, theo BƯỚC chứ không chỉ theo nhóm.** Mã ticket
/// là một case ĐÃ CÓ thì: khớp quy trình (ô `D`, <see cref="KhopQuyTrinh"/>) → bước hiện tại → tra tri thức
/// theo CHỦ ĐỀ của bước đó (L1) → không có thì rơi về mức nhóm như trước. Mỗi phát biểu trả kèm DẪN CHỨNG
/// trỏ về nguồn (ô `I`) — khoá nguồn của mẩu và của ticket, KHÔNG kèm nội dung mẩu.
///
/// ⚠ Lõi này dùng CHUNG cho <c>/hoi</c> và webhook (<c>WebhookSender</c>) — một chỗ quyết định
/// "biết hay không biết", vì hai chỗ thì sẽ lệch nhau.
/// </summary>
public sealed class HoiHandler(AppDbContext db, IDinhTuyenNhom dinhTuyen, KhopQuyTrinh khop)
{
    public async Task<CauTraLoi> HoiAsync(CauHoi cauHoi, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cauHoi);

        // Mã ticket là một case ĐÃ CÓ? (khớp chính xác, rồi khớp đuôi — người hỏi hay gõ mã trần.)
        var c = string.IsNullOrWhiteSpace(cauHoi.MaTicket)
            ? null
            : await TraCaseTheoMa.TimMotAsync(db, cauHoi.MaTicket, ct);

        // Case đã có thì định tuyến bằng CHÍNH dữ liệu của case (tiêu đề + mẩu sớm nhất) — đúng thứ có mặt
        // lúc ticket đến (`docs/13` §1), không phụ thuộc người hỏi gõ lại tiêu đề hay không.
        var tuyen = c is not null
            ? await khop.DinhTuyenCaseAsync(c, ct)
            : await dinhTuyen.DinhTuyenAsync(new YeuCauDinhTuyen(cauHoi.MaTicket, cauHoi.TieuDe, cauHoi.MoTa), ct);

        // Ô D: case đang chạy quy trình nào, đang ở bước nào. Hỏi về một case là dấu hiệu case đang được xử lý,
        // nên lượt chạy được tạo ở đây (POST) — nhưng CHỈ khi có quy trình ĐÃ DUYỆT cho nhóm.
        var kq = c is null ? null : await khop.KhopAsync(c, tuyen, taoLuotChay: true, ct);
        var coQuyTrinh = kq is { KhopDuoc: true };

        // --- CỬA 1: định tuyến không biết — trừ khi case ĐÃ đang chạy một quy trình (nhóm đến từ nó) ---
        if (!tuyen.BietNhom && !coQuyTrinh)
        {
            return CauTraLoi.KhongBiet(
                LyDo: "dinh-tuyen-khong-biet",
                GiaiThich: tuyen.GiaiThich,
                Nguon: tuyen.Nguon,
                Nhom: null) with { LyDoKhongCoQuyTrinh = kq?.LyDo };
        }

        var nhom = coQuyTrinh ? kq!.DinhNghia!.Nhom : tuyen.Nhom!;
        var nhanNhom = SopDraftToKnowledge.NhanNhom(nhom);
        var buoc = coQuyTrinh ? kq!.TienDo!.BuocHienTai : null;

        // --- Ô G: tra theo CHỦ ĐỀ của bước hiện tại; không có thì rơi về mức nhóm ---
        var traTheo = "nhom";
        var records = new List<KnowledgeRecord>();

        if (buoc is not null && !string.Equals(buoc.ChuDe, nhanNhom, StringComparison.Ordinal))
        {
            records = await TraTheoNhanAsync(buoc.ChuDe, ct);
            if (records.Count > 0) traTheo = "buoc";
        }

        if (records.Count == 0)
        {
            records = await TraTheoNhanAsync(nhanNhom, ct);
        }

        var quyTrinh = coQuyTrinh ? QuyTrinhHienTai.Tu(kq!, traTheo) : null;

        // --- CỬA 2: biết nhóm nhưng nhóm chưa có SOP nào được duyệt ---
        if (records.Count == 0)
        {
            return CauTraLoi.KhongBiet(
                LyDo: "nhom-chua-co-sop-duoc-duyet",
                GiaiThich: $"Định tuyến vào nhóm \"{nhom}\", nhưng nhóm này chưa có tri thức nào " +
                           "được người duyệt. Nạp bản nháp rồi duyệt trước khi hỏi lại.",
                Nguon: tuyen.Nguon,
                Nhom: nhom) with { QuyTrinh = quyTrinh, LyDoKhongCoQuyTrinh = coQuyTrinh ? null : kq?.LyDo };
        }

        var danChung = await DanChungDauRa.ChuanBiAsync(db, records, ct);

        return new CauTraLoi(
            BietTraLoi: true,
            Nhom: nhom,
            NguonDinhTuyen: tuyen.Nguon,
            GiaiThichDinhTuyen: tuyen.GiaiThich,
            LyDoKhongBiet: null,
            HuongDan: [.. records.Select(r => ToHuongDan(r, danChung))])
        {
            QuyTrinh = quyTrinh,
            LyDoKhongCoQuyTrinh = coQuyTrinh ? null : kq?.LyDo,
        };
    }

    /// <summary>
    /// Chỉ record ĐÃ DUYỆT mang assertion <c>Applicability</c> đúng nhãn (bản còn hiệu lực). Nạp kèm dẫn chứng
    /// tới tận mẩu (<c>KemDanChung</c>) — để V3 (mẩu bị rút sau lần duyệt) không hỏng im lặng.
    /// </summary>
    private Task<List<KnowledgeRecord>> TraTheoNhanAsync(string nhan, CancellationToken ct) =>
        db.KnowledgeRecords
            .KemDanChung()
            .Where(r => r.Lifecycle == StoredLifecycleState.Active)
            .Where(r => r.Assertions.Any(a =>
                a.Kind == AssertionKind.Applicability && a.Content == nhan && a.ReplacedByAssertionId == null))
            .ToListAsync(ct);

    /// <summary>
    /// Bày ra cả mức kiểm chứng của từng phát biểu, không chỉ nội dung.
    ///
    /// `S8`/`G4`: bày chỗ yếu ra chính là thứ người đọc cần — giấu nó đi thì người ta không
    /// phản bác được, mà phản bác được là toàn bộ giá trị của cách làm này. Một bước nhận ra
    /// mang nhãn `Speculative` phải đọc ra khác hẳn một bước mang `Supported`.
    /// </summary>
    private static HuongDan ToHuongDan(KnowledgeRecord r, Func<Assertion, IReadOnlyList<DanChungRef>> danChung) => new(
        Id: r.Id,
        TenNguyenNhan: r.CauseName,
        TrangThai: r.DisplayState,
        PhamViXem: r.VisibilityScope?.ToWire(),
        CachNhanRa: PhatBieuCua(r, AssertionKind.Recognition, danChung),
        CachXuLy: PhatBieuCua(r, AssertionKind.Handling, danChung),
        DieuKienApDung: PhatBieuCua(r, AssertionKind.Applicability, danChung));

    private static IReadOnlyList<PhatBieu> PhatBieuCua(
        KnowledgeRecord r, AssertionKind kind, Func<Assertion, IReadOnlyList<DanChungRef>> danChung) =>
    [
        .. r.Assertions
            .Where(a => a.Kind == kind && a.IsCurrent)
            .Select(a => new PhatBieu(a.Content, a.Verification.ToString(), a.Origin.ToString(), danChung(a)))
    ];
}

/// <summary>
/// Ô `I`: dẫn chứng TRỎ VỀ NGUỒN — khoá nguồn của mẩu và của ticket, KHÔNG có nội dung mẩu.
///
/// ⚠ Không trả nội dung, cố ý: đọc nội dung đi đường của nó (<c>GET /cases/{khoaNguon}/evidence</c>), nơi luật
/// rút lại (`AR-m`) đã thực thi. Một đường đọc thứ hai cho cùng dữ liệu là đường sẽ quên luật.
///
/// ⚠ Mẩu ĐÃ RÚT LẠI không được trích làm dẫn chứng — một đoạn người gửi đã rút thì không chống lưng gì. Record
/// đó vẫn hiện ra (V3 gắn cờ chứ không rút), mang trạng thái <c>NEEDS_REVIEW</c>.
/// </summary>
public static class DanChungDauRa
{
    public static async Task<Func<Assertion, IReadOnlyList<DanChungRef>>> ChuanBiAsync(
        AppDbContext db, IEnumerable<KnowledgeRecord> records, CancellationToken ct = default)
    {
        var caseIds = records
            .SelectMany(r => r.Assertions)
            .SelectMany(a => a.EvidenceLinks)
            .Select(l => l.EvidenceItem?.ObservedInCaseId)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var khoaCase = caseIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.Cases
                .Where(c => caseIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.SourceReference, ct);

        return a =>
        [
            .. a.EvidenceLinks
                .Where(l => l.EvidenceItem is { RetractedAt: null })
                .Select(l => new DanChungRef(
                    l.EvidenceItem!.SourceReference,
                    l.EvidenceItem.ObservedInCaseId is { } cid && khoaCase.TryGetValue(cid, out var k) ? k : null,
                    l.Relation.ToString(),
                    l.Note))
        ];
    }
}

/// <param name="KhoaNguonMau">Khoá nguồn của mẩu — ví dụ <c>jira:ES-1#comment-7</c>.</param>
/// <param name="KhoaNguonCase">Ticket chứa mẩu đó — thứ bên tích hợp nhận ra được trong hệ thống của mình.</param>
/// <param name="QuanHe"><c>Support</c> · <c>ContextFor</c> · <c>Refute</c>.</param>
public sealed record DanChungRef(string KhoaNguonMau, string? KhoaNguonCase, string QuanHe, string? GhiChu);

/// <summary>
/// Câu hỏi đi vào.
///
/// ⚠ <see cref="MaTicket"/> chỉ dùng được với bản định tuyến TRA SẴN. `docs/13` §1: phép đo
/// dùng đúng thứ có mặt LÚC TICKET ĐẾN — tiêu đề + mô tả. Bản gọi model thật sẽ bỏ qua mã.
///
/// 🎯 Từ P5: <see cref="MaTicket"/> trỏ tới một case ĐÃ CÓ (khoá nguồn đầy đủ hoặc mã trần) thì câu trả lời
/// đi theo BƯỚC HIỆN TẠI của case đó.
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
    /// <summary>
    /// Case đang ở đâu trong quy trình (P5). Chỉ có mặt khi mã ticket là một case ĐÃ CÓ và khớp được một quy
    /// trình đã duyệt — thiếu ô này KHÔNG phải "quy trình rỗng" (`IM-15`: không đặt chỗ trống trông như có).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QuyTrinhHienTai? QuyTrinh { get; init; }

    /// <summary>
    /// Case ĐÃ CÓ mà không khớp được quy trình nào: vì sao (<c>nhom-chua-co-quy-trinh-duoc-duyet</c>,
    /// <c>dinh-tuyen-khong-biet</c>, <c>luot-chay-da-bi-bo</c>). Khi đó tri thức trả theo mức NHÓM như trước P5.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LyDoKhongCoQuyTrinh { get; init; }

    public static CauTraLoi KhongBiet(string LyDo, string? GiaiThich, string Nguon, string? Nhom) =>
        new(false, Nhom, Nguon, GiaiThich, LyDo, []);
}

/// <param name="TraTheo">
/// <c>buoc</c> = tri thức tra theo chủ đề của bước hiện tại · <c>nhom</c> = rơi về mức nhóm (bước không có tri
/// thức gắn riêng, hoặc đã xong hết các bước).
/// </param>
public sealed record QuyTrinhHienTai(
    Guid Id,
    string Ten,
    int PhienBan,
    Guid? LuotChayId,
    BuocHienTaiRa? BuocHienTai,
    bool DaXongHet,
    string TraTheo)
{
    public static QuyTrinhHienTai Tu(KetQuaKhop k, string traTheo) => new(
        k.DinhNghia!.Id,
        k.DinhNghia.Ten,
        k.DinhNghia.PhienBan,
        k.LuotChay?.Id,
        k.TienDo!.BuocHienTai is { } b ? new BuocHienTaiRa(b.Ma, b.NoiDung, b.NoiXem) : null,
        k.TienDo.DaXongHet,
        traTheo);
}

public sealed record BuocHienTaiRa(string Ma, string NoiDung, string? NoiXem);

public sealed record HuongDan(
    Guid Id,
    string TenNguyenNhan,
    string TrangThai,
    string? PhamViXem,
    IReadOnlyList<PhatBieu> CachNhanRa,
    IReadOnlyList<PhatBieu> CachXuLy,
    IReadOnlyList<PhatBieu> DieuKienApDung);

/// <summary>
/// Một phát biểu, kèm mức kiểm chứng và nguồn gốc. Cả ba luôn đi cùng nhau (`AP3`).
/// Từ P5 kèm <see cref="DanChung"/> — mẩu nào chống lưng nó (ô `I`).
/// </summary>
public sealed record PhatBieu(
    string NoiDung,
    string MucKiemChung,
    string NguonGoc,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<DanChungRef>? DanChung = null);
