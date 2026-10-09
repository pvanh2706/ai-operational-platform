using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Api.QuyTrinh;
using KnowledgePlatform.Domain.GoiY;
using KnowledgePlatform.Domain.Process;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.GoiY;

/// <summary>
/// Ô `J` của sơ đồ — *"ghi lại đã gợi ý gì, người có dùng không"* — và vòng nét đứt `J → G`. P6, 2026-10-10.
///
/// 🛑 Ba nấc đầu HỆ THỐNG tự ghi, hai nấc cuối là LỜI KHAI (`N3-1`, đóng `L2-a`). Ghi đúng bản chất ở đây:
/// hệ thống hôm nay biết request thuộc CÔNG TY nào, không biết thuộc NGƯỜI nào (`docs/18` §"hình dạng chung"),
/// nên <c>nguoiKhai</c> là một chuỗi tự khai — cùng loại với <c>nguoiDuyet</c>.
///
/// 🎯 Vòng `J → G`: phản hồi mang <c>nhomDung</c> (`E2-1`) ghi thẳng vào kho định tuyến của CHÍNH case đó, nên
/// lượt hỏi sau của case đi đúng nhóm. Đó là vòng nạp lại có thật duy nhất hôm nay; phần "cho bộ eval" là đọc
/// bảng này ra (nhóm định tuyến nói gì · người sửa thành gì), không phải một cơ chế riêng.
/// </summary>
public sealed class GoiYHandler(AppDbContext db, ITenantContext tenant, TaxonomyNhom taxonomy, TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Dựng một lượt (CHƯA lưu) cho câu trả lời này, kèm nấc RETRIEVED.</summary>
    public LuotGoiY TaoLuot(string kenhRa, CauTraLoi traLoi)
    {
        var luot = new LuotGoiY
        {
            TenantId = tenant.TenantId,
            KenhRa = kenhRa,
            CaseId = traLoi.CaseId,
            KhoaNguonCase = traLoi.KhoaNguonCase,
            Nhom = traLoi.Nhom,
            NguonDinhTuyen = traLoi.NguonDinhTuyen,
            BietTraLoi = traLoi.BietTraLoi,
            LyDoKhongBiet = traLoi.LyDoKhongBiet,
            ProcessDefinitionId = traLoi.QuyTrinh?.Id,
            ProcessRunId = traLoi.QuyTrinh?.LuotChayId,
            MaBuoc = traLoi.QuyTrinh?.BuocHienTai?.Ma,
            KnowledgeRecordIds = [.. traLoi.HuongDan.Select(h => h.Id)],
            Luc = clock.GetUtcNow(),
        };

        // RETRIEVED: hệ thống đã TRA kho. Không biết trả lời thì vẫn là một lượt tra — chỉ là không đạt.
        luot.Ghi(NacGoiY.Retrieved, traLoi.BietTraLoi, clock.GetUtcNow());
        return luot;
    }

    public static string ChupAnh<T>(T giaTri) => JsonSerializer.Serialize(giaTri, Json);

    /// <summary>
    /// <c>/hoi</c>: response trả ngay, nên giao = trả. Ghi RETRIEVED + SHOWN và ảnh chụp đúng thứ trả ra.
    /// ⚠ SHOWN ở đây nghĩa là "đã trả response", không phải "người đã đọc".
    /// </summary>
    public async Task<CauTraLoi> GhiHoiAsync(CauTraLoi traLoi, CancellationToken ct = default)
    {
        var luot = TaoLuot("hoi", traLoi);
        var traRa = traLoi with { MaLuotGoiY = luot.Id };

        luot.NoiDung = ChupAnh(traRa);
        luot.Ghi(NacGoiY.Shown, true, clock.GetUtcNow());

        db.Set<LuotGoiY>().Add(luot);
        await db.SaveChangesAsync(ct);
        return traRa;
    }

    /// <summary>
    /// OPENED — có người mở link xem đầy đủ. Ghi MỖI lần mở (append-only); phễu đếm số LƯỢT có ít nhất một lần.
    /// Lượt gợi ý không còn (hoặc của khách khác — RLS đã lo) thì bỏ qua: mở link không được hỏng vì sổ ghi.
    /// </summary>
    public async Task GhiMoLinkAsync(Guid luotGoiYId, bool moDuoc, CancellationToken ct = default)
    {
        if (!await db.Set<LuotGoiY>().AnyAsync(x => x.Id == luotGoiYId, ct)) return;

        db.Set<LuotGoiYSuKien>().Add(new LuotGoiYSuKien
        {
            TenantId = tenant.TenantId, LuotGoiYId = luotGoiYId, Nac = NacGoiY.Opened, DatDuoc = moDuoc,
            Luc = clock.GetUtcNow(),
        });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Hai nấc LỜI KHAI. <c>nhomDung</c> (`E2-1`) thì sửa kho định tuyến của case — vòng `J → G`.
    /// </summary>
    public async Task<KetQua<PhanHoiDaGhi>> PhanHoiAsync(Guid id, YeuCauPhanHoi yc, CancellationToken ct = default)
    {
        if (!Enum.TryParse<NacGoiY>(yc.Nac, ignoreCase: true, out var nac) || nac is not (NacGoiY.Accepted or NacGoiY.Used))
        {
            return KetQua<PhanHoiDaGhi>.Sai(
                $"`nac` phải là \"accepted\" hoặc \"used\" — ba nấc kia do hệ thống tự ghi. Nhận: \"{yc.Nac}\".");
        }

        if (yc.DatDuoc is null)
        {
            return KetQua<PhanHoiDaGhi>.Sai(
                "`datDuoc` bắt buộc (true/false). Bỏ trống thì \"không dùng\" và \"chưa nói\" trông giống hệt nhau.");
        }

        if (string.IsNullOrWhiteSpace(yc.NguoiKhai))
        {
            return KetQua<PhanHoiDaGhi>.Sai("`nguoiKhai` bắt buộc — một lời khai không có tác giả thì không tra lại được.");
        }

        var luot = await db.Set<LuotGoiY>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (luot is null) return KetQua<PhanHoiDaGhi>.KhongThay();

        SuaDinhTuyen? daSua = null;

        if (!string.IsNullOrWhiteSpace(yc.NhomDung))
        {
            var nhom = yc.NhomDung.Trim();

            if (taxonomy.TenNhom is not { } hopLe)
            {
                return KetQua<PhanHoiDaGhi>.XungDot(
                    $"Không đọc được taxonomy ({taxonomy.DuongDan}) nên không kiểm được nhóm \"{nhom}\". " +
                    "Cố ý TỪ CHỐI thay vì nhận bừa — một tên nhóm bịa vào kho định tuyến là lỗi G6.");
            }

            if (nhom != KetQuaDinhTuyen.KhongBiet && !hopLe.Contains(nhom))
            {
                return KetQua<PhanHoiDaGhi>.Sai($"Nhóm \"{nhom}\" KHÔNG có trong taxonomy. Không sửa hộ, không tìm nhóm gần nhất.");
            }

            if (luot.KhoaNguonCase is null || luot.CaseId is null)
            {
                return KetQua<PhanHoiDaGhi>.Sai(
                    "Lượt gợi ý này không gắn với case nào trong kho, nên không có gì để sửa định tuyến. " +
                    "Vẫn gửi phản hồi được — bỏ `nhomDung`.");
            }

            daSua = await SuaDinhTuyenAsync(luot, nhom, yc.NguoiKhai.Trim(), yc.GhiChu, nac, ct);
        }

        luot.Ghi(nac, yc.DatDuoc.Value, clock.GetUtcNow(), yc.NguoiKhai.Trim(), yc.GhiChu?.Trim(), yc.NhomDung?.Trim());
        db.Set<LuotGoiYSuKien>().Add(luot.SuKien[^1]);
        await db.SaveChangesAsync(ct);

        return KetQua<PhanHoiDaGhi>.Xong(new PhanHoiDaGhi(luot.Id, nac.ToString().ToUpperInvariant(), yc.DatDuoc.Value, daSua));
    }

    /// <summary>
    /// Ghi "nhóm đúng là X" vào kho định tuyến của case. Người nói ⇒ người thắng: kho định tuyến tra kết quả người
    /// nạp TRƯỚC bảng đo (<c>DinhTuyenKho</c>).
    ///
    /// ⚠ Case đang chạy một quy trình của nhóm CŨ thì lượt chạy đó bị BỎ, bởi chính người khai, kèm lý do. Không bỏ
    /// thì sửa định tuyến không có tác dụng: case đang chạy thì giữ quy trình của nó (`N6-1`). Đây vẫn là hành vi
    /// của NGƯỜI — khớp luật "chỉ người bỏ lượt chạy" (`05`:103).
    /// </summary>
    private async Task<SuaDinhTuyen> SuaDinhTuyenAsync(
        LuotGoiY luot, string nhom, string nguoiKhai, string? ghiChu, NacGoiY nac, CancellationToken ct)
    {
        var khoa = luot.KhoaNguonCase!;
        var cu = await db.Set<DinhTuyenDaNap>().FirstOrDefaultAsync(x => x.KhoaNguon == khoa, ct);
        var nhomCu = cu?.Nhom ?? luot.Nhom;

        if (cu is null)
        {
            db.Set<DinhTuyenDaNap>().Add(new DinhTuyenDaNap
            {
                TenantId = tenant.TenantId, KhoaNguon = khoa, Nhom = nhom, Nguon = $"phan-hoi-{nac.ToString().ToLowerInvariant()}",
                GiaiThich = ghiChu, NguoiNap = nguoiKhai,
            });
        }
        else
        {
            cu.Nhom = nhom;
            cu.Nguon = $"phan-hoi-{nac.ToString().ToLowerInvariant()}";
            cu.GiaiThich = ghiChu;
            cu.NguoiNap = nguoiKhai;
        }

        Guid? daBoLuotChay = null;
        var dangChay = await db.Set<ProcessRun>().FirstOrDefaultAsync(r => r.CaseId == luot.CaseId && r.AbandonedAt == null, ct);
        if (dangChay is not null)
        {
            var nhomCuaRun = await db.Set<ProcessDefinition>()
                .Where(d => d.Id == dangChay.ProcessDefinitionId).Select(d => d.Nhom).FirstAsync(ct);

            if (!string.Equals(nhomCuaRun, nhom, StringComparison.Ordinal))
            {
                dangChay.Bo(nguoiKhai, $"Phản hồi lượt gợi ý {luot.Id}: nhóm đúng là \"{nhom}\", không phải \"{nhomCuaRun}\".");
                daBoLuotChay = dangChay.Id;
            }
        }

        return new SuaDinhTuyen(khoa, nhomCu, nhom, daBoLuotChay);
    }

    /// <summary>Phễu 5 nấc: mỗi nấc đếm số LƯỢT có ít nhất một sự kiện đạt / không đạt.</summary>
    public async Task<ThongKeGoiY> ThongKeAsync(DateTimeOffset? tu, CancellationToken ct = default)
    {
        var luot = db.Set<LuotGoiY>().Where(x => tu == null || x.Luc >= tu);

        var soLuot = await luot.CountAsync(ct);
        var theoKenh = await luot.GroupBy(x => x.KenhRa).Select(g => new { g.Key, So = g.Count() }).ToListAsync(ct);

        var suKien = await db.Set<LuotGoiYSuKien>()
            .Where(s => luot.Any(l => l.Id == s.LuotGoiYId))
            .Select(s => new { s.LuotGoiYId, s.Nac, s.DatDuoc })
            .Distinct()
            .ToListAsync(ct);

        var theoNac = Enum.GetValues<NacGoiY>()
            .Select(n => new DemNac(
                n.ToString().ToUpperInvariant(),
                suKien.Where(s => s.Nac == n && s.DatDuoc).Select(s => s.LuotGoiYId).Distinct().Count(),
                suKien.Where(s => s.Nac == n && !s.DatDuoc).Select(s => s.LuotGoiYId).Distinct().Count()))
            .ToList();

        return new ThongKeGoiY(
            soLuot, theoNac, theoKenh.ToDictionary(x => x.Key, x => x.So),
            "SHOWN = hệ thống đã giao, KHÔNG phải người đã đọc. ACCEPTED/USED là LỜI KHAI, không gì nối chúng với " +
            "kết luận của case. Mỗi lượt THỬ gửi webhook là một lượt — lượt hỏng chỉ có RETRIEVED.");
    }
}

/// <param name="Nac"><c>accepted</c> hoặc <c>used</c>.</param>
/// <param name="DatDuoc">BẮT BUỘC: <c>true</c> = có / <c>false</c> = không.</param>
/// <param name="NhomDung">`E2-1`: nhóm định tuyến ĐÚNG, nếu người dùng thấy định tuyến sai.</param>
public sealed record YeuCauPhanHoi(string? Nac, bool? DatDuoc, string? NguoiKhai, string? GhiChu, string? NhomDung);

/// <param name="DaBoLuotChay">Lượt chạy quy trình của nhóm cũ vừa bị bỏ (bởi chính người khai) — nếu có.</param>
public sealed record SuaDinhTuyen(string KhoaNguon, string? NhomCu, string NhomMoi, Guid? DaBoLuotChay);

public sealed record PhanHoiDaGhi(Guid LuotGoiYId, string Nac, bool DatDuoc, SuaDinhTuyen? DaSuaDinhTuyen);

public sealed record DemNac(string Nac, int SoLuotDat, int SoLuotKhongDat);

public sealed record ThongKeGoiY(int SoLuot, IReadOnlyList<DemNac> TheoNac, IReadOnlyDictionary<string, int> TheoKenh, string LuuY);
