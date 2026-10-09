using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Api.Startup;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Domain.Knowledge;
using KnowledgePlatform.Domain.Process;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace KnowledgePlatform.Api.QuyTrinh;

/// <summary>
/// Đường vào cho quy trình (Capability 2) — mở lại `I1` ngày 2026-10-10. Xem <see cref="ProcessDefinition"/>.
///
/// ⚠ Mọi lỗi KIỂM ĐƯỢC trả về như một kết cục có tên (để endpoint chọn đúng mã HTTP), không ném. Ném chỉ
/// dành cho thứ không lường trước — cùng khuôn với các handler tri thức.
/// </summary>
public sealed class QuyTrinhHandler(
    AppDbContext db,
    ITenantContext tenant,
    KhopQuyTrinh khop,
    CongChe congChe,
    IOptions<TenancyOptions> tenancy)
{
    // =========================================================================
    //  Định nghĩa
    // =========================================================================

    public async Task<KetQua<QuyTrinhChiTiet>> TaoAsync(YeuCauTaoQuyTrinh yc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(yc.Nhom) || string.IsNullOrWhiteSpace(yc.Ten) || string.IsNullOrWhiteSpace(yc.NguoiTao))
        {
            return KetQua<QuyTrinhChiTiet>.Sai("Thiếu `nhom`, `ten` hoặc `nguoiTao`.");
        }

        var phienBan = (await db.Set<ProcessDefinition>()
            .Where(d => d.Nhom == yc.Nhom)
            .MaxAsync(d => (int?)d.PhienBan, ct) ?? 0) + 1;

        var dn = new ProcessDefinition
        {
            TenantId = tenant.TenantId,
            Nhom = yc.Nhom.Trim(),
            Ten = yc.Ten.Trim(),
            PhienBan = phienBan,
            // Người VIẾT một quy trình là nhận định có chuyên môn — cùng lý do bản B mặc định HumanAssessment.
            Origin = Origin.HumanAssessment,
            ActorLabel = yc.NguoiTao.Trim(),
        };

        if (DungBuoc(dn, yc.Buoc) is { } sai) return KetQua<QuyTrinhChiTiet>.Sai(sai);

        db.Set<ProcessDefinition>().Add(dn);
        await db.SaveChangesAsync(ct);

        return KetQua<QuyTrinhChiTiet>.Xong(QuyTrinhChiTiet.Tu(dn));
    }

    public async Task<IReadOnlyList<QuyTrinhTomTat>> LietKeAsync(string? nhom, CancellationToken ct = default) =>
        (await db.Set<ProcessDefinition>()
            .Where(d => nhom == null || d.Nhom == nhom)
            .OrderBy(d => d.Nhom).ThenByDescending(d => d.PhienBan)
            .Select(d => new { d.Id, d.Nhom, d.Ten, d.PhienBan, d.Lifecycle, SoBuoc = d.Buoc.Count })
            .ToListAsync(ct))
        .Select(d => new QuyTrinhTomTat(d.Id, d.Nhom, d.Ten, d.PhienBan, d.Lifecycle.ToString().ToUpperInvariant(), d.SoBuoc))
        .ToList();

    public async Task<QuyTrinhChiTiet?> DocAsync(Guid id, CancellationToken ct = default)
    {
        var dn = await db.Set<ProcessDefinition>().Include(d => d.Buoc).FirstOrDefaultAsync(d => d.Id == id, ct);
        return dn is null ? null : QuyTrinhChiTiet.Tu(dn);
    }

    /// <summary>Thay toàn bộ bước — chỉ DRAFT (`N5-1`). Đây là chỗ người duyệt THÊM DẤU HIỆU cho bước sinh từ bản nháp.</summary>
    public async Task<KetQua<QuyTrinhChiTiet>> ThayBuocAsync(Guid id, YeuCauThayBuoc yc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(yc.NguoiSua)) return KetQua<QuyTrinhChiTiet>.Sai("Thiếu `nguoiSua`.");

        var dn = await db.Set<ProcessDefinition>().Include(d => d.Buoc).FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dn is null) return KetQua<QuyTrinhChiTiet>.KhongThay();

        if (dn.Lifecycle != StoredLifecycleState.Draft)
        {
            return KetQua<QuyTrinhChiTiet>.XungDot(
                $"Quy trình đang {dn.Lifecycle.ToString().ToUpper()} — chỉ DRAFT mới sửa được bước (N5-1). " +
                "Muốn đổi thì tạo phiên bản mới bằng POST /processes.");
        }

        var moi = new ProcessDefinition
        {
            TenantId = dn.TenantId, Nhom = dn.Nhom, Ten = dn.Ten, PhienBan = dn.PhienBan, Origin = dn.Origin,
        };
        if (DungBuoc(moi, yc.Buoc) is { } sai) return KetQua<QuyTrinhChiTiet>.Sai(sai);

        var buocMoi = moi.Buoc.Select(b => new ProcessStep
        {
            TenantId = dn.TenantId, ProcessDefinitionId = dn.Id, ThuTu = b.ThuTu, Ma = b.Ma,
            NoiDung = b.NoiDung, NoiXem = b.NoiXem, DauHieu = b.DauHieu, ChuDe = b.ChuDe,
        }).ToList();

        // ⚠ HAI lượt ghi trong MỘT giao dịch: xoá bước cũ rồi mới thêm bước mới. Một lượt thì EF có thể
        //   chèn bước mới TRƯỚC khi xoá bước cũ, và unique index (định nghĩa, thứ tự) nổ ngay ở bước 1.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Set<ProcessStep>().RemoveRange(dn.Buoc);
        await db.SaveChangesAsync(ct);

        dn.ThayBuoc(buocMoi);
        db.Set<ProcessStep>().AddRange(buocMoi);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
        return KetQua<QuyTrinhChiTiet>.Xong(QuyTrinhChiTiet.Tu(dn));
    }

    /// <summary>
    /// Duyệt — S7 + `AR-n` + cổng `AR-l`, đúng như duyệt tri thức. Duyệt bản mới thì bản ACTIVE cũ của cùng
    /// nhóm thành DEPRECATED (`N6-1`); lượt chạy đang dở của bản cũ KHÔNG bị kéo theo.
    /// </summary>
    public async Task<KetQua<QuyTrinhChiTiet>> DuyetAsync(
        Guid id, string nguoiDuyet, VisibilityScope phamViXem, bool xacNhanDuLieuThuongMai, string? lyDo,
        CancellationToken ct = default)
    {
        if (CongDuyetARl.LyDoChan(tenancy.Value, "quy trình") is { } chan)
        {
            return KetQua<QuyTrinhChiTiet>.XungDot(chan);
        }

        var dn = await db.Set<ProcessDefinition>().Include(d => d.Buoc).FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dn is null) return KetQua<QuyTrinhChiTiet>.KhongThay();

        if (dn.Lifecycle != StoredLifecycleState.Draft)
        {
            return KetQua<QuyTrinhChiTiet>.XungDot(
                $"Quy trình đang {dn.Lifecycle.ToString().ToUpper()} — chỉ duyệt được bản DRAFT (N5-1).");
        }

        if (dn.Buoc.Count == 0) return KetQua<QuyTrinhChiTiet>.Sai("Quy trình không có bước nào.");

        // ⚠ HAI lượt ghi: rút bản cũ TRƯỚC, rồi mới bật bản mới. Unique index "một nhóm một bản ACTIVE" sẽ nổ
        //   nếu EF ghi bản mới trước — và một lượt SaveChanges không hứa thứ tự giữa hai UPDATE.
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var cu = await db.Set<ProcessDefinition>()
            .Where(d => d.Nhom == dn.Nhom && d.Lifecycle == StoredLifecycleState.Active && d.Id != dn.Id)
            .ToListAsync(ct);
        foreach (var x in cu) x.Deprecate();
        await db.SaveChangesAsync(ct);

        dn.Approve(nguoiDuyet, phamViXem, xacNhanDuLieuThuongMai, lyDo);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return KetQua<QuyTrinhChiTiet>.XungDot(
                "Một phiên bản khác của nhóm này vừa được duyệt cùng lúc. Đọc lại rồi duyệt lại.");
        }

        await tx.CommitAsync(ct);
        return KetQua<QuyTrinhChiTiet>.Xong(QuyTrinhChiTiet.Tu(dn, rutCu: cu.Select(x => x.Id).ToList()));
    }

    // =========================================================================
    //  Case
    // =========================================================================

    /// <summary>Chỉ ĐỌC — không tạo lượt chạy nào. Chưa có lượt chạy thì nói sẽ khớp quy trình nào.</summary>
    public async Task<TienDoCase?> TienDoAsync(string khoaNguon, CancellationToken ct = default)
    {
        var c = await db.Cases.FirstOrDefaultAsync(x => x.SourceReference == khoaNguon, ct);
        if (c is null) return null;

        var tuyen = await khop.DinhTuyenCaseAsync(c, ct);
        return TienDoCase.Tu(khoaNguon, await khop.KhopAsync(c, tuyen, taoLuotChay: false, ct));
    }

    /// <summary>
    /// Lời xác nhận "đã làm bước X" → một mẩu EVIDENCE có tác giả (`N2-1`, PR1), không phải cờ. Tạo lượt chạy
    /// nếu case chưa có — xác nhận một bước là bằng chứng rõ nhất rằng case đang chạy quy trình đó.
    /// </summary>
    public async Task<KetQua<TienDoCase>> XacNhanBuocAsync(YeuCauXacNhanBuoc yc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(yc.KhoaNguon) || string.IsNullOrWhiteSpace(yc.MaBuoc)
            || string.IsNullOrWhiteSpace(yc.NguoiXacNhan))
        {
            return KetQua<TienDoCase>.Sai("Thiếu `khoaNguon`, `maBuoc` hoặc `nguoiXacNhan`.");
        }

        // ⚠ Cùng luật `AR-l` với đường nạp evidence: bên gửi PHẢI khai mã khách sạn, kể cả cho lời xác nhận.
        if (string.IsNullOrWhiteSpace(yc.SubTenantCode) || !SubTenantCodes.TryChuanHoa(yc.SubTenantCode, out var maKs))
        {
            return KetQua<TienDoCase>.Sai(
                $"`subTenantCode` bắt buộc (AR-l). Chưa biết thì khai \"{SubTenantCodes.KhongXacDinh}\".");
        }

        var c = await db.Cases.FirstOrDefaultAsync(x => x.SourceReference == yc.KhoaNguon, ct);
        if (c is null) return KetQua<TienDoCase>.KhongThay();

        var tuyen = await khop.DinhTuyenCaseAsync(c, ct);
        var kq = await khop.KhopAsync(c, tuyen, taoLuotChay: true, ct);

        if (!kq.KhopDuoc)
        {
            return KetQua<TienDoCase>.XungDot($"Case này chưa khớp được quy trình nào ({kq.LyDo}): {kq.GiaiThich}");
        }

        var buoc = kq.DinhNghia!.Buoc.FirstOrDefault(b => string.Equals(b.Ma, yc.MaBuoc.Trim(), StringComparison.Ordinal));
        if (buoc is null)
        {
            return KetQua<TienDoCase>.Sai(
                $"Quy trình \"{kq.DinhNghia.Ten}\" (phiên bản {kq.DinhNghia.PhienBan}) không có bước \"{yc.MaBuoc}\". " +
                $"Có: {string.Join(", ", kq.DinhNghia.Buoc.OrderBy(b => b.ThuTu).Select(b => b.Ma))}.");
        }

        var noiDung = $"Đã làm bước {buoc.Ma} ({buoc.NoiDung}) — xác nhận bởi {yc.NguoiXacNhan.Trim()}." +
                      (string.IsNullOrWhiteSpace(yc.GhiChu) ? "" : $" Ghi chú: {yc.GhiChu.Trim()}");

        db.EvidenceItems.Add(new EvidenceItem
        {
            TenantId = tenant.TenantId,
            Content = congChe.CheBenThuBa(noiDung).Text,
            SourceReference = $"kp:xac-nhan:{Guid.CreateVersion7():N}",
            ObservedInCaseId = c.Id,
            ObservedAt = DateTimeOffset.UtcNow,
            SubTenantCode = maKs,
            Origin = Origin.UserConfirmed,
            XacNhanProcessStepId = buoc.Id,
            DaCheBenThuBa = congChe.DaQuaCongChe,
        });
        await db.SaveChangesAsync(ct);

        return KetQua<TienDoCase>.Xong(TienDoCase.Tu(yc.KhoaNguon, await khop.KhopAsync(c, tuyen, taoLuotChay: false, ct)));
    }

    /// <summary>Bỏ lượt chạy đang mở — CHỈ NGƯỜI (`05`:103).</summary>
    public async Task<KetQua<LuotChayDaBo>> BoAsync(YeuCauBoLuotChay yc, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(yc.KhoaNguon) || string.IsNullOrWhiteSpace(yc.NguoiBo) || string.IsNullOrWhiteSpace(yc.LyDo))
        {
            return KetQua<LuotChayDaBo>.Sai("Thiếu `khoaNguon`, `nguoiBo` hoặc `lyDo` — một dấu vết không có tác giả và lý do thì không tra lại được.");
        }

        var c = await db.Cases.FirstOrDefaultAsync(x => x.SourceReference == yc.KhoaNguon, ct);
        if (c is null) return KetQua<LuotChayDaBo>.KhongThay();

        var luot = await db.Set<ProcessRun>().FirstOrDefaultAsync(r => r.CaseId == c.Id && r.AbandonedAt == null, ct);
        if (luot is null) return KetQua<LuotChayDaBo>.XungDot("Case này không có lượt chạy quy trình nào đang mở.");

        luot.Bo(yc.NguoiBo, yc.LyDo);
        await db.SaveChangesAsync(ct);

        return KetQua<LuotChayDaBo>.Xong(new LuotChayDaBo(luot.Id, luot.ProcessDefinitionId, luot.AbandonedAt!.Value, luot.AbandonedBy!));
    }

    /// <summary>Dựng bước từ yêu cầu. Trả lỗi đọc được, hoặc <c>null</c>.</summary>
    private static string? DungBuoc(ProcessDefinition dn, IReadOnlyList<YeuCauBuoc>? buoc)
    {
        if (buoc is null || buoc.Count == 0) return "Quy trình phải có ít nhất một bước.";

        var ds = buoc.Select((b, i) => new ProcessStep
        {
            TenantId = dn.TenantId,
            ProcessDefinitionId = dn.Id,
            ThuTu = i + 1,
            Ma = (b.Ma ?? string.Empty).Trim(),
            NoiDung = (b.NoiDung ?? string.Empty).Trim(),
            NoiXem = string.IsNullOrWhiteSpace(b.NoiXem) ? null : b.NoiXem.Trim(),
            DauHieu = [.. (b.DauHieu ?? []).Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => d.Trim())],
            // Bỏ trống thì tra theo NHÓM — đúng đường rơi về mà ô G đang chạy.
            ChuDe = string.IsNullOrWhiteSpace(b.ChuDe) ? SopDraftToKnowledge.NhanNhom(dn.Nhom) : b.ChuDe.Trim(),
        }).ToList();

        try
        {
            ProcessDefinition.KiemBuoc(ds);
        }
        catch (ArgumentException e)
        {
            return e.Message;
        }

        dn.Buoc.AddRange(ds);
        return null;
    }
}

// =============================================================================
//  Hình dạng vào / ra
// =============================================================================

public sealed record YeuCauBuoc(string? Ma, string? NoiDung, string? NoiXem, IReadOnlyList<string>? DauHieu, string? ChuDe);

public sealed record YeuCauTaoQuyTrinh(string? Nhom, string? Ten, string? NguoiTao, IReadOnlyList<YeuCauBuoc>? Buoc);

public sealed record YeuCauThayBuoc(string? NguoiSua, IReadOnlyList<YeuCauBuoc>? Buoc);

public sealed record YeuCauDuyetQuyTrinh(string? NguoiDuyet, string? PhamViXem, bool? XacNhanDuLieuThuongMai, string? LyDo);

/// <param name="SubTenantCode">BẮT BUỘC, cùng luật `AR-l` với đường nạp evidence.</param>
public sealed record YeuCauXacNhanBuoc(string? KhoaNguon, string? MaBuoc, string? NguoiXacNhan, string? SubTenantCode, string? GhiChu);

public sealed record YeuCauBoLuotChay(string? KhoaNguon, string? NguoiBo, string? LyDo);

public sealed record QuyTrinhTomTat(Guid Id, string Nhom, string Ten, int PhienBan, string TrangThai, int SoBuoc);

public sealed record BuocChiTiet(int ThuTu, string Ma, string NoiDung, string? NoiXem, IReadOnlyList<string> DauHieu, string ChuDe);

/// <param name="CanhBao">Ví dụ: bước chưa có dấu hiệu nào — chỉ xong được bằng lời xác nhận.</param>
/// <param name="DaRutPhienBanCu">Id các phiên bản ACTIVE cũ vừa bị rút khi duyệt bản này.</param>
public sealed record QuyTrinhChiTiet(
    Guid Id, string Nhom, string Ten, int PhienBan, string TrangThai, string? PhamViXem, string NguonGoc,
    string? NguoiTao, Guid? SinhTuBanNhap, IReadOnlyList<BuocChiTiet> Buoc, IReadOnlyList<string> CanhBao,
    IReadOnlyList<Guid> DaRutPhienBanCu)
{
    public static QuyTrinhChiTiet Tu(ProcessDefinition d, IReadOnlyList<Guid>? rutCu = null)
    {
        var khongDauHieu = d.Buoc.Where(b => b.DauHieu.Count == 0).OrderBy(b => b.ThuTu).Select(b => b.Ma).ToList();
        var canhBao = khongDauHieu.Count == 0
            ? new List<string>()
            : [$"{khongDauHieu.Count} bước chưa có dấu hiệu nào ({string.Join(", ", khongDauHieu)}) — chỉ xong được " +
               "bằng lời xác nhận (POST /quy-trinh/xac-nhan-buoc). Thêm dấu hiệu bằng PUT /processes/{id}/buoc khi còn DRAFT."];

        return new QuyTrinhChiTiet(
            d.Id, d.Nhom, d.Ten, d.PhienBan, d.Lifecycle.ToString().ToUpperInvariant(), d.VisibilityScope?.ToWire(),
            d.Origin.ToString(), d.ActorLabel, d.SopDraftGocId,
            [.. d.Buoc.OrderBy(b => b.ThuTu).Select(b => new BuocChiTiet(b.ThuTu, b.Ma, b.NoiDung, b.NoiXem, b.DauHieu, b.ChuDe))],
            canhBao, rutCu ?? []);
    }
}

/// <summary>
/// Case đang ở đâu trong quy trình — phép CHIẾU từ evidence, đọc lúc này.
/// </summary>
/// <param name="LyDoKhongCo">Khi <see cref="CoQuyTrinh"/> = false: cửa nào đã chặn (xem <see cref="KetQuaKhop"/>).</param>
/// <param name="DangChay"><c>false</c> khi khớp được quy trình nhưng CHƯA có lượt chạy (đường đọc không tạo gì).</param>
public sealed record TienDoCase(
    string KhoaNguon,
    bool CoQuyTrinh,
    string? LyDoKhongCo,
    string? GiaiThich,
    QuyTrinhTomTat? QuyTrinh,
    Guid? LuotChayId,
    bool DangChay,
    IReadOnlyList<TienDoBuoc> Buoc,
    TienDoBuoc? BuocHienTai,
    bool DaXongHet)
{
    public static TienDoCase Tu(string khoaNguon, KetQuaKhop k) =>
        !k.KhopDuoc
            ? new(khoaNguon, false, k.LyDo, k.GiaiThich, null, null, false, [], null, false)
            : new(khoaNguon, true, null, null,
                new QuyTrinhTomTat(k.DinhNghia!.Id, k.DinhNghia.Nhom, k.DinhNghia.Ten, k.DinhNghia.PhienBan,
                    k.DinhNghia.Lifecycle.ToString().ToUpperInvariant(), k.DinhNghia.Buoc.Count),
                k.LuotChay?.Id, k.LuotChay is not null,
                k.TienDo!.Buoc, k.TienDo.BuocHienTai, k.TienDo.DaXongHet);
}

public sealed record LuotChayDaBo(Guid LuotChayId, Guid QuyTrinhId, DateTimeOffset BoLuc, string NguoiBo);

/// <summary>Bốn kết cục — endpoint chọn mã HTTP theo đúng kết cục, không đoán từ chuỗi lỗi.</summary>
public sealed record KetQua<T>(T? GiaTri, string? Loi, KetCuc KetCuc)
{
    public static KetQua<T> Xong(T v) => new(v, null, KetCuc.Xong);
    public static KetQua<T> Sai(string loi) => new(default, loi, KetCuc.Sai);
    public static KetQua<T> KhongThay() => new(default, null, KetCuc.KhongThay);
    public static KetQua<T> XungDot(string loi) => new(default, loi, KetCuc.XungDot);
}

public enum KetCuc { Xong, Sai, KhongThay, XungDot }
