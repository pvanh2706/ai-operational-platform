using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Nạp một BẢN NHÁP SOP vào kho tri thức — mảnh (a) của phạm vi demo chốt 2026-09-13.
///
/// Trước đường này, bản nháp do máy sinh chỉ là một file JSON: `SoanNhapRunner` ghi ra đĩa
/// rồi dừng. Kho tri thức vì thế RỖNG, và `Approve()` không nơi nào gọi được vì không có gì
/// để duyệt. Đây là chỗ nối hai đầu đó lại.
///
/// 🛑 NÓ KHÔNG DUYỆT GÌ CẢ. Record vào kho ở trạng thái <c>Draft</c>, `VisibilityScope` còn
/// trống. `D4`: người công nhận, không phải AI — và "nạp" không phải "công nhận". Muốn nó
/// trả lời được cho ai thì phải có người gọi đường duyệt.
///
/// ⚠ IDEMPOTENT THEO CẶP <c>(nhóm, CauseName)</c>. Nạp lại cùng một bản nháp thì không sinh
/// record trùng. Và nếu record đã có, nội dung cũ **KHÔNG bị ghi đè** — cùng luật với
/// `IM-20`: ghi đè im lặng là sửa lại quá khứ, kéo theo mọi thứ đang dẫn chứng bằng nó.
///
/// 🛑 Vì sao CẶP chứ không phải mình <c>CauseName</c> (dù `T1` nói identity là nguyên nhân):
/// <c>CauseName</c> lấy từ `quanSat`, mà `quanSat` là LỜI MODEL VIẾT — "Danh sách rỗng", "Ô
/// trống nhưng chọn được" trùng nhau giữa các nhóm là chuyện thường. So tên một mình thì bản
/// nháp của nhóm thứ hai bị gộp ÂM THẦM vào record của nhóm thứ nhất: nạp 3, tạo 1, trả 200,
/// không ai biết. Bộ test API đã bắt đúng ca đó.
///
/// ⚠ CHƯA CÓ UNIQUE INDEX trên <c>(tenant_id, cause_name)</c>. Nghĩa là hai request nạp
/// cùng lúc VẪN sinh được record trùng — kiểm-rồi-ghi không nguyên tử. Khác với đường tín
/// hiệu (có unique index làm chốt cuối). Ghi rõ ở đây thay vì để người đọc tưởng đã kín;
/// với phạm vi demo (một người gọi tay) thì chưa chặn được việc gì.
///
/// 🎯 **Từ 2026-10-10 (P3) đường này nối DẪN CHỨNG** — ô `H` của sơ đồ. Trước đó
/// <c>assertion_evidence</c> có 0 dòng: bản nháp mang mã ticket ở mọi nhánh, phép map vứt đi.
/// Giờ mỗi mã ticket được giải thành case, và MỌI mẩu chưa rút lại của case đó được nối vào
/// phát biểu trích nó. Xem <see cref="NoiDanChungAsync"/> cho ba chỗ phải chọn.
/// </summary>
public sealed class SopDraftIngestHandler(AppDbContext db, ITenantContext tenant)
{
    private static readonly JsonSerializerOptions JsonChuan = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public async Task<SopDraftIngestResult> HandleAsync(SopDraft draft, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var banGoc = await LuuBanGocAsync(draft, ct);
        var mapped = SopDraftToKnowledge.Map(draft, tenant.TenantId);

        // Tra một lượt cho cả lô, không phải một câu mỗi record.
        //
        // 🛑 KHỬ TRÙNG THEO CẶP (NHÓM, TÊN NGUYÊN NHÂN) — không theo tên nguyên nhân một mình.
        // Bản đầu của handler này so tên nguyên nhân trên toàn tenant, và bộ test API bắt được
        // ngay: hai bản nháp của HAI NHÓM KHÁC NHAU mà có chung một câu quan sát (`quanSat` là
        // lời model viết, rất dễ trùng: "Danh sách rỗng", "Ô trống nhưng chọn được") sẽ bị gộp
        // ÂM THẦM thành một record — nạp 3 nguyên nhân, tạo 1, và không có gì báo.
        //
        // Gộp im lặng tệ hơn trùng lặp: trùng thì đếm ra là thấy, còn gộp thì record của nhóm
        // sau biến mất mà response vẫn trả 200. Cùng họ với lỗi "trường phủ 100% một giá trị"
        // mà dự án đã vấp hai lần trong một ngày (`AR-k`, `AR-l`).
        //
        // ⚠ Nạp KÈM dẫn chứng (KemDanChung): record đã có vẫn có thể được ĐIỀN BÙ liên kết, và
        //   điền bù phải biết phát biểu nào đã có liên kết rồi.
        var nhanNhom = SopDraftToKnowledge.NhanNhom(draft.Nhom);
        var tenNguyenNhan = mapped.Records.Select(r => r.CauseName).Distinct().ToList();
        var daCo = await db.KnowledgeRecords
            .KemDanChung()
            .Where(r => r.Assertions.Any(a =>
                a.Kind == AssertionKind.Applicability && a.Content == nhanNhom))
            .Where(r => tenNguyenNhan.Contains(r.CauseName))
            .ToListAsync(ct);

        var daCoTheoTen = daCo
            .GroupBy(r => r.CauseName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        // Nối dẫn chứng TRƯỚC khi Add: record mới vào DbContext cùng cả đồ thị (assertion → liên
        // kết) trong MỘT lượt, thay vì để EF đoán trạng thái của con được thêm sau.
        var danChung = await NoiDanChungAsync(mapped, daCoTheoTen, ct);

        var them = mapped.Records.Where(r => !daCoTheoTen.ContainsKey(r.CauseName)).ToList();
        foreach (var r in them)
        {
            db.KnowledgeRecords.Add(r);
        }

        await db.SaveChangesAsync(ct);

        return new SopDraftIngestResult(
            Nhom: draft.Nhom,
            NhanDuoc: mapped.Records.Count,
            DaTao: them.Count,
            DaCoTuTruoc: mapped.Records.Count - them.Count,
            BuocSuaMoCoi: mapped.BuocSuaMoCoi,
            NhanhTroLacLoi: mapped.NhanhTroLacLoi,
            NguyenNhan: them.Select(r => new NguyenNhanDaTao(
                r.Id,
                r.CauseName,
                r.DisplayState,
                r.Assertions.Count)).ToList(),
            DanChung: danChung,
            BanNhapGoc: banGoc);
    }

    /// <summary>Lưu bản nháp gốc (dạng serialize chuẩn), đúng một lần theo hash. Xem <see cref="SopDraftGoc"/>.</summary>
    private async Task<BanNhapGocDaLuu> LuuBanGocAsync(SopDraft draft, CancellationToken ct)
    {
        var noiDung = JsonSerializer.Serialize(draft, JsonChuan);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(noiDung)));

        var cu = await db.Set<SopDraftGoc>()
            .Where(x => x.Hash == hash)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (cu is { } id) return new BanNhapGocDaLuu(id, DaCoTuTruoc: true);

        var moi = new SopDraftGoc
        {
            TenantId = tenant.TenantId,
            Nhom = draft.Nhom,
            Hash = hash,
            NoiDung = noiDung,
        };

        db.Set<SopDraftGoc>().Add(moi);
        return new BanNhapGocDaLuu(moi.Id, DaCoTuTruoc: false);
    }

    /// <summary>
    /// Mã ticket trong bản nháp → case → MỌI mẩu chưa rút lại của case đó → liên kết.
    ///
    /// Ba chỗ phải chọn, và lý do:
    /// <list type="number">
    ///   <item><b>Giải mã case:</b> khớp CHÍNH XÁC trước, rồi khớp đuôi <c>:mã</c>. Bản nháp viết mã
    ///     TRẦN (<c>ES-9001</c>, đường offline bỏ tiền tố) hoặc đầy đủ (<c>jira:ES-9001</c>, đường
    ///     trong host). Khớp đuôi ra HƠN MỘT case thì báo <c>maMoHo</c> — KHÔNG tự chọn: hai nguồn
    ///     cùng đánh số kiểu <c>ES-…</c> là chuyện có thật khi khách có hơn một connector.</item>
    ///   <item><b>Hạt liên kết:</b> mọi mẩu của case, không phải mẩu "liên quan nhất". Bản nháp trích
    ///     TICKET, không trích mẩu; chọn mẩu là đoán thay model — đúng thứ `G6` cấm. Ghi chú của
    ///     liên kết nói rõ nó đến từ mã ticket nào, qua nhánh nào.</item>
    ///   <item><b>Quan hệ:</b> <c>evidence-noi-ro</c> và bước sửa → <c>Support</c>; còn lại (<c>toi-suy-ra</c>)
    ///     → <c>ContextFor</c>. <c>chungCu</c> là lời model tự khai, nên nó chỉ chọn QUAN HỆ, không
    ///     nâng mức kiểm chứng.</item>
    /// </list>
    /// Mẩu đã RÚT LẠI (`AR-m`) không được nối — một đoạn người gửi đã rút thì không chống lưng gì.
    /// </summary>
    private async Task<DanChungDaNoi> NoiDanChungAsync(
        SopMappingResult mapped,
        IReadOnlyDictionary<string, KnowledgeRecord> daCoTheoTen,
        CancellationToken ct)
    {
        var maCase = mapped.TrichDan.Select(t => t.MaCase).Distinct(StringComparer.Ordinal).ToList();
        var (caseTheoMa, khongGiai, moHo) = await GiaiMaCaseAsync(maCase, ct);

        var caseIds = caseTheoMa.Values.Distinct().ToList();
        var mauTheoCase = caseIds.Count == 0
            ? new Dictionary<Guid, List<Guid>>()
            : (await db.EvidenceItems
                    .Where(e => e.ObservedInCaseId != null && caseIds.Contains(e.ObservedInCaseId!.Value))
                    .Where(e => e.RetractedAt == null)
                    .Select(e => new { CaseId = e.ObservedInCaseId!.Value, e.Id })
                    .ToListAsync(ct))
                .GroupBy(x => x.CaseId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var soMoi = 0;
        var soDienBu = 0;

        // Bản ghi ĐÃ CÓ: phát biểu nào của bản nháp ứng với phát biểu nào đã lưu (khớp Kind +
        // Content, chỉ bản AI còn hiệu lực). Điền bù CHỈ vào phát biểu chưa có liên kết nào —
        // phát biểu đã có liên kết thì đã có người hoặc lượt nạp trước quyết định rồi.
        var dichDienBu = new Dictionary<Assertion, Assertion>(ReferenceEqualityComparer.Instance);
        foreach (var record in mapped.Records)
        {
            if (!daCoTheoTen.TryGetValue(record.CauseName, out var daLuu)) continue;

            foreach (var a in record.Assertions)
            {
                var khop = daLuu.Assertions.FirstOrDefault(x =>
                    x.IsCurrent && x.Origin == Origin.AiInference && x.Kind == a.Kind && x.Content == a.Content);

                if (khop is not null && khop.EvidenceLinks.Count == 0) dichDienBu[a] = khop;
            }
        }

        var daNoi = new HashSet<(Guid AssertionId, Guid EvidenceId)>();

        foreach (var t in mapped.TrichDan)
        {
            if (!caseTheoMa.TryGetValue(t.MaCase, out var caseId)) continue;
            if (!mauTheoCase.TryGetValue(caseId, out var mau)) continue;

            var laBanGhiMoi = !daCoTheoTen.ContainsKey(TenRecordCua(mapped, t.Assertion));
            Assertion? dich = laBanGhiMoi ? t.Assertion : dichDienBu.GetValueOrDefault(t.Assertion);
            if (dich is null) continue;

            var quanHe = t.ChungCu is null || t.ChungCu == "evidence-noi-ro"
                ? EvidenceRelation.Support
                : EvidenceRelation.ContextFor;

            foreach (var mauId in mau)
            {
                if (!daNoi.Add((dich.Id, mauId))) continue;

                var lienKet = new AssertionEvidence
                {
                    TenantId = tenant.TenantId,
                    AssertionId = dich.Id,
                    EvidenceItemId = mauId,
                    Relation = quanHe,
                    Note = t.GhiChu,
                };

                if (laBanGhiMoi)
                {
                    // Cả đồ thị của bản ghi mới được Add một lượt — thêm vào navigation là đủ.
                    dich.EvidenceLinks.Add(lienKet);
                    soMoi++;
                }
                else
                {
                    // Cha ĐÃ được theo dõi: Add tường minh, đừng trông vào việc EF tự đoán trạng
                    // thái của con mới trong một cha cũ (bẫy `IM-32`).
                    db.AssertionEvidenceLinks.Add(lienKet);
                    soDienBu++;
                }
            }
        }

        return new DanChungDaNoi(soMoi, soDienBu, khongGiai, moHo);
    }

    private static string TenRecordCua(SopMappingResult mapped, Assertion a) =>
        mapped.Records.First(r => r.Id == a.KnowledgeRecordId).CauseName;

    /// <summary>
    /// Mã ticket → id case. Khớp chính xác, rồi khớp đuôi <c>:mã</c> (một câu <c>LIKE ANY</c>).
    /// </summary>
    private async Task<(Dictionary<string, Guid> TheoMa, IReadOnlyList<string> KhongGiai, IReadOnlyList<string> MoHo)>
        GiaiMaCaseAsync(IReadOnlyList<string> ma, CancellationToken ct)
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

        if (conLai.Count > 0)
        {
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
        }

        return (theoMa, khongGiai, moHo);
    }

    /// <summary>Thoát ký tự đặc biệt của LIKE (PostgreSQL dùng <c>\</c> làm ký tự thoát mặc định).</summary>
    private static string ThoatLike(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal)
         .Replace("%", "\\%", StringComparison.Ordinal)
         .Replace("_", "\\_", StringComparison.Ordinal);
}

/// <summary>
/// Kết quả nạp.
///
/// ⚠ Hai danh sách cảnh báo được trả RA NGOÀI response, không chỉ ghi log. Bản nháp thật do
/// máy sinh CÓ một bước sửa mồ côi (`S8`) — người nạp phải thấy điều đó ngay lúc nạp, chứ
/// không phải phát hiện sáu tuần sau khi có ai hỏi vì sao một nhánh không bao giờ chạy tới.
///
/// ⚠ Cảnh báo KHÔNG chặn việc nạp, cố ý: `S8` là dữ liệu THẬT, chặn nó là không nạp được
/// chính bản nháp đang có. Nói ra rồi vẫn nạp, khác với nuốt im lặng.
/// </summary>
public sealed record SopDraftIngestResult(
    string Nhom,
    int NhanDuoc,
    int DaTao,
    int DaCoTuTruoc,
    IReadOnlyList<string> BuocSuaMoCoi,
    IReadOnlyList<string> NhanhTroLacLoi,
    IReadOnlyList<NguyenNhanDaTao> NguyenNhan,
    DanChungDaNoi DanChung,
    BanNhapGocDaLuu BanNhapGoc);

public sealed record NguyenNhanDaTao(Guid Id, string TenNguyenNhan, string TrangThai, int SoAssertion);

/// <summary>
/// Dẫn chứng đã nối trong lượt nạp này.
///
/// ⚠ <see cref="MaKhongGiaiDuoc"/> KHÔNG làm lượt nạp thất bại, cố ý — cùng tinh thần với hai
/// danh sách cảnh báo kia. Một mã không giải được có thể là case chưa được nạp vào Kênh 1 (nạp sau
/// rồi nạp lại bản nháp thì được điền bù), hoặc là model bịa mã (`G6`). Hai ca đó cần hai việc khác
/// nhau, và chỉ người đọc response phân biệt được.
/// </summary>
/// <param name="SoLienKetMoi">Liên kết gắn vào record VỪA tạo.</param>
/// <param name="SoLienKetDienBu">Liên kết gắn thêm vào record ĐÃ CÓ mà trước đó chưa có dẫn chứng.</param>
/// <param name="MaMoHo">Mã khớp HƠN MỘT case qua đuôi <c>:mã</c> — cố ý không tự chọn.</param>
public sealed record DanChungDaNoi(
    int SoLienKetMoi,
    int SoLienKetDienBu,
    IReadOnlyList<string> MaKhongGiaiDuoc,
    IReadOnlyList<string> MaMoHo);

public sealed record BanNhapGocDaLuu(Guid Id, bool DaCoTuTruoc);
