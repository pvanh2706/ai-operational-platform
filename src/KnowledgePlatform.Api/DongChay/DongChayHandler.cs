using KnowledgePlatform.Api.QuyTrinh;
using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Domain.GoiY;
using KnowledgePlatform.Domain.Process;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Webhooks;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.DongChay;

/// <summary>
/// Hai đường ĐỌC cho console nội bộ (`P2-1`, docs/18 nhóm P, 2026-10-19): danh sách case, và DÒNG CHẢY của một case —
/// mọi thứ hệ thống đã làm với nó, theo thứ tự của sơ đồ luồng.
///
/// 🛑 **Ba luật, mỗi luật vì một chuyện đã thấy:**
/// <list type="number">
///   <item><b>KHÔNG tác dụng phụ.</b> Định tuyến đi qua <see cref="KhopQuyTrinh.XemDinhTuyenCaseAsync"/> — không gọi model,
///     không ghi biên lai; khớp quy trình chạy với <c>taoLuotChay: false</c>. Lúc viết đường này mới thấy
///     <c>GET /cases/{k}/quy-trinh</c> (ghi "CHỈ ĐỌC") đi qua định tuyến ĐẦY ĐỦ: bật <c>GoiModel</c> thì một lượt GET
///     vào case mới là một lượt gọi model tính tiền. Màn hình xem không được đi theo lối đó.</item>
///   <item><b>Trả DỮ KIỆN, không trả cách hiểu.</b> Việc tô ô A–J của sơ đồ là của giao diện. Trộn "ô nào đã qua"
///     vào hợp đồng là đóng băng một cách vẽ thành thứ bên tích hợp phải giữ.</item>
///   <item><b>Mỗi kết quả định tuyến mang NGUỒN</b> (<c>nguoi-nap:</c> · <c>tra-san:</c> · <c>model:</c>), và biên lai
///     model đi kèm nguyên trạng — kể cả lượt hỏng, kể cả lượt dưới ngưỡng. Một màn hình chỉ hiện câu trả lời cuối
///     là màn hình làm người ta tưởng định tuyến "chạy rồi" (`IM-45`).</item>
/// </list>
///
/// ⚠ Nội dung evidence trả RÚT GỌN (<see cref="DoDaiRutGon"/> ký tự). Bản đầy đủ đã có đường riêng
/// (<c>GET /cases/{k}/evidence</c>); nhân bản nó ra đây là thêm một chỗ dữ liệu khách đi ra mà không thêm gì để nhìn.
/// </summary>
public sealed class DongChayHandler(AppDbContext db, KhopQuyTrinh khop)
{
    public const int DoDaiRutGon = 280;
    public const int SoLuongMacDinh = 50;
    public const int SoLuongToiDa = 200;

    public async Task<IReadOnlyList<CaseTrongDanhSach>> LietKeAsync(string? tienTo, int? soLuong, CancellationToken ct = default)
    {
        var n = Math.Clamp(soLuong ?? SoLuongMacDinh, 1, SoLuongToiDa);
        var loc = string.IsNullOrWhiteSpace(tienTo) ? null : tienTo.Trim();

        var cases = await db.Cases
            .Where(c => loc == null || c.SourceReference.StartsWith(loc))
            .OrderByDescending(c => c.IngestedAt)
            .Take(n)
            .ToListAsync(ct);

        var ids = cases.Select(c => c.Id).ToList();

        var soMau = await db.EvidenceItems
            .Where(e => e.ObservedInCaseId != null && ids.Contains(e.ObservedInCaseId!.Value))
            .GroupBy(e => e.ObservedInCaseId!.Value)
            .Select(g => new { g.Key, So = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.So, ct);

        var soGoiY = await db.Set<LuotGoiY>()
            .Where(l => l.CaseId != null && ids.Contains(l.CaseId!.Value))
            .GroupBy(l => l.CaseId!.Value)
            .Select(g => new { g.Key, So = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.So, ct);

        var coLuotChay = (await db.Set<ProcessRun>()
                .Where(r => ids.Contains(r.CaseId) && r.AbandonedAt == null)
                .Select(r => r.CaseId)
                .ToListAsync(ct))
            .ToHashSet();

        var ra = new List<CaseTrongDanhSach>(cases.Count);
        foreach (var c in cases)
        {
            // Mỗi case một lượt tra (người nạp → bảng đo → model đã lưu). Trần 200 dòng giữ chi phí này nhỏ;
            // tính lại cho từng dòng là để danh sách nói ĐÚNG câu /hoi sẽ nói, không phải một bản đoán rẻ hơn.
            var tuyen = await khop.XemDinhTuyenCaseAsync(c, ct);
            ra.Add(new CaseTrongDanhSach(
                c.SourceReference, c.Subject, c.IngestedAt, c.SourceResolvedAt,
                soMau.GetValueOrDefault(c.Id), soGoiY.GetValueOrDefault(c.Id), coLuotChay.Contains(c.Id),
                tuyen.Nhom, tuyen.Nguon));
        }

        return ra;
    }

    public async Task<DongChayCase?> DocAsync(string khoaNguon, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(khoaNguon)) return null;

        var c = await db.Cases.FirstOrDefaultAsync(x => x.SourceReference == khoaNguon.Trim(), ct);
        if (c is null) return null;

        // --- evidence: cả mẩu đã rút (đánh dấu), cả lời xác nhận bước (trỏ về mã bước) ---
        var buocTheoId = await db.Set<ProcessStep>()
            .Select(b => new { b.Id, b.Ma })
            .ToDictionaryAsync(b => b.Id, b => b.Ma, ct);

        var evidence = (await db.EvidenceItems
                .Where(e => e.ObservedInCaseId == c.Id)
                .OrderBy(e => e.ObservedAt ?? e.IngestedAt)
                .ThenBy(e => e.IngestedAt)
                .ToListAsync(ct))
            .Select(e => new MauTrongDongChay(
                e.SourceReference,
                e.ObservedAt,
                e.IngestedAt,
                RutGon(e.Content),
                e.Content.Length,
                e.MachineReadability.ToString(),
                e.SubTenantCode,
                e.RetractedAt,
                e.XacNhanProcessStepId is { } bId ? buocTheoId.GetValueOrDefault(bId) ?? "?" : null))
            .ToList();

        // --- định tuyến: câu trả lời hiệu lực + từng tầng đã lưu ---
        var hieuLuc = await khop.XemDinhTuyenCaseAsync(c, ct);

        var nguoiNap = await db.Set<DinhTuyenDaNap>()
            .Where(x => x.KhoaNguon == c.SourceReference)
            .Select(x => new DinhTuyenNguoiNap(x.Nhom, x.Nguon, x.GiaiThich, x.NguoiNap, x.NapLuc))
            .FirstOrDefaultAsync(ct);

        var bienLai = await db.Set<LuotDinhTuyenModel>()
            .Where(x => x.KhoaNguon == c.SourceReference)
            .OrderBy(x => x.Luc)
            .Select(x => new BienLaiModel(x.Luc, x.ThanhCong, x.Nhom, x.DoTinCay, x.Nguon, x.GiaiThich, x.Loi, x.SoChoDaChe))
            .ToListAsync(ct);

        // --- quy trình: mọi lượt chạy (kể cả đã bỏ) + tiến độ XEM (không tạo lượt chạy) ---
        // ⚠ Đọc thô rồi ánh xạ trong bộ nhớ: enum.ToString() và đếm trên cột mảng KHÔNG chắc dịch được sang SQL,
        //   và một phép chiếu không dịch được chỉ nổ lúc chạy, ở đúng màn hình người ta mở để chẩn đoán.
        var runs = await db.Set<ProcessRun>().Where(r => r.CaseId == c.Id).OrderBy(r => r.StartedAt).ToListAsync(ct);
        var dnIds = runs.Select(r => r.ProcessDefinitionId).Distinct().ToList();
        var dinhNghia = await db.Set<ProcessDefinition>().Where(d => dnIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, ct);

        var luotChay = runs
            .Select(r =>
            {
                var d = dinhNghia[r.ProcessDefinitionId];
                return new LuotChayTrongDongChay(r.Id, d.Id, d.Nhom, d.Ten, d.PhienBan, d.Lifecycle.ToString().ToUpperInvariant(),
                    r.StartedAt, r.AbandonedAt, r.AbandonedBy, r.AbandonReason);
            })
            .ToList();

        var tienDo = TienDoCase.Tu(c.SourceReference, await khop.KhopAsync(c, hieuLuc, taoLuotChay: false, ct));

        // --- gợi ý (ô G–J) + gửi kết quả về (D1-1) ---
        var goiY = await db.Set<LuotGoiY>()
            .Where(l => l.CaseId == c.Id)
            .OrderBy(l => l.Luc)
            .ToListAsync(ct);
        var goiYIds = goiY.Select(l => l.Id).ToList();
        var suKien = (await db.Set<LuotGoiYSuKien>()
                .Where(s => goiYIds.Contains(s.LuotGoiYId))
                .OrderBy(s => s.Luc)
                .ToListAsync(ct))
            .ToLookup(s => s.LuotGoiYId);

        var guiVe = (await db.Set<WebhookDelivery>()
                .Where(w => w.CaseId == c.Id)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync(ct))
            .Select(w => new LuotGuiVe(w.Id, w.State.ToString(), w.Attempts, w.CreatedAt, w.DeliveredAt, w.LastError, w.SuKien))
            .ToList();

        return new DongChayCase(
            c.SourceReference, c.Id, c.Subject, c.SourceCreatedAt, c.SourceResolvedAt, c.IngestedAt,
            evidence,
            new DinhTuyenTrongDongChay(hieuLuc.Nhom, hieuLuc.Nguon, hieuLuc.GiaiThich, nguoiNap, bienLai),
            new QuyTrinhTrongDongChay(luotChay, tienDo),
            [.. goiY.Select(l => new LuotGoiYTrongDongChay(
                l.Id, l.Luc, l.KenhRa, l.Nhom, l.NguonDinhTuyen, l.BietTraLoi, l.LyDoKhongBiet, l.MaBuoc, l.KnowledgeRecordIds.Count,
                [.. suKien[l.Id].Select(s => new SuKienGoiY(
                    s.Nac.ToString().ToUpperInvariant(), s.DatDuoc, s.Luc, s.NguoiKhai, s.GhiChu, s.NhomDung))]))],
            guiVe);
    }

    private static string RutGon(string s) =>
        s.Length <= DoDaiRutGon ? s : s[..DoDaiRutGon].TrimEnd() + "…";
}

// ============================================================================ hình dạng trả ra

/// <param name="NhomHieuLuc">Nhóm mà /hoi sẽ dùng cho case này, tính KHÔNG gọi model. <c>null</c> = không biết.</param>
/// <param name="NguonDinhTuyen">Tầng đã trả lời — <c>nguoi-nap:</c> · <c>tra-san:</c> · <c>model:</c> (kể cả <c>model:chua-goi</c>).</param>
public sealed record CaseTrongDanhSach(
    string KhoaNguon, string TieuDe, DateTimeOffset NapLuc, DateTimeOffset? XongONguonLuc,
    int SoMau, int SoLuotGoiY, bool CoLuotChayMo, string? NhomHieuLuc, string NguonDinhTuyen);

public sealed record DongChayCase(
    string KhoaNguon, Guid CaseId, string TieuDe,
    DateTimeOffset? TaoONguonLuc, DateTimeOffset? XongONguonLuc, DateTimeOffset NapLuc,
    IReadOnlyList<MauTrongDongChay> Evidence,
    DinhTuyenTrongDongChay DinhTuyen,
    QuyTrinhTrongDongChay QuyTrinh,
    IReadOnlyList<LuotGoiYTrongDongChay> GoiY,
    IReadOnlyList<LuotGuiVe> GuiVe);

/// <param name="NoiDungRutGon">Tối đa <see cref="DongChayHandler.DoDaiRutGon"/> ký tự — bản đủ ở <c>GET /cases/{k}/evidence</c>.</param>
/// <param name="XacNhanBuoc">Mã bước nếu mẩu này là LỜI XÁC NHẬN bước (`N2-1`), không thì <c>null</c>.</param>
public sealed record MauTrongDongChay(
    string KhoaNguon, DateTimeOffset? QuanSatLuc, DateTimeOffset NapLuc, string NoiDungRutGon, int DoDai,
    string MayDoc, string? MaKhachSan, DateTimeOffset? RutLaiLuc, string? XacNhanBuoc);

/// <param name="Nhom">Câu trả lời HIỆU LỰC — đúng thứ /hoi dùng, trừ ticket model chưa được hỏi (<c>model:chua-goi</c>).</param>
/// <param name="BienLaiModel">Mọi lượt gọi model cho case này, nguyên trạng — lượt hỏng và lượt dưới ngưỡng cũng có.</param>
public sealed record DinhTuyenTrongDongChay(
    string? Nhom, string Nguon, string? GiaiThich, DinhTuyenNguoiNap? NguoiNap, IReadOnlyList<BienLaiModel> BienLaiModel);

public sealed record DinhTuyenNguoiNap(string Nhom, string Nguon, string? GiaiThich, string NguoiNap, DateTimeOffset NapLuc);

public sealed record BienLaiModel(
    DateTimeOffset Luc, bool ThanhCong, string? Nhom, double? DoTinCay, string? Nguon, string? GiaiThich, string? Loi, int SoChoDaChe);

/// <param name="TienDo">Tiến độ tính KHÔNG tạo lượt chạy — cùng hình dạng <c>GET /cases/{k}/quy-trinh</c>.</param>
public sealed record QuyTrinhTrongDongChay(IReadOnlyList<LuotChayTrongDongChay> LuotChay, TienDoCase TienDo);

public sealed record LuotChayTrongDongChay(
    Guid Id, Guid QuyTrinhId, string Nhom, string Ten, int PhienBan, string TrangThaiQuyTrinh,
    DateTimeOffset BatDau, DateTimeOffset? BoLuc, string? BoBoi, string? LyDoBo);

public sealed record LuotGoiYTrongDongChay(
    Guid Id, DateTimeOffset Luc, string KenhRa, string? Nhom, string NguonDinhTuyen, bool BietTraLoi,
    string? LyDoKhongBiet, string? MaBuoc, int SoTriThuc, IReadOnlyList<SuKienGoiY> SuKien);

/// <param name="NguoiKhai">Có giá trị thì nấc này là LỜI KHAI (ACCEPTED/USED), không phải hệ thống tự ghi.</param>
public sealed record SuKienGoiY(string Nac, bool DatDuoc, DateTimeOffset Luc, string? NguoiKhai, string? GhiChu, string? NhomDung);

public sealed record LuotGuiVe(
    Guid Id, string TrangThai, int SoLan, DateTimeOffset TaoLuc, DateTimeOffset? GuiLuc, string? LoiCuoi, string? SuKien);
