using System.Security.Cryptography;
using KnowledgePlatform.Domain.Documents;
using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Documents;

/// <param name="TrungVoi">Id của tài liệu đã có cùng nội dung, nếu nạp trùng.</param>
/// <param name="TrangThaiTrichChu">P9: <c>da-trich</c> · <c>chua-ho-tro</c> · <c>khong-giai-ma-duoc</c> · <c>loi</c>.</param>
public sealed record KetQuaNapTaiLieu(
    Guid Id, string TenKhaiBao, long KichThuoc, string Hash, bool DaCoTuTruoc, Guid? TrungVoi,
    string? TrangThaiTrichChu = null, string? GhiChuTrichChu = null);

public sealed record TomTatTaiLieu(
    Guid Id, string TenKhaiBao, string LoaiNoiDung, long KichThuoc,
    string SubTenantCode, string NguoiNap, DateTimeOffset NapLuc, string? TrangThaiTrichChu);

/// <summary>Chữ trích được của một tài liệu (ô `K`).</summary>
public sealed record ChuTaiLieu(Guid Id, string TenKhaiBao, string? TrangThaiTrichChu, string? GhiChu, string? NoiDungChu);

/// <summary>
/// Kênh 2 — đường nạp/đồng bộ tài liệu (`AR5`).
///
/// 🛑 **Nó mang VẬT CHỨA vào, không mang TRI THỨC vào** (`S6`). Đây là lý do Kênh 2 được hoãn
/// suốt: build xong đường này thì kho tri thức **vẫn là 0**. Tri thức chỉ ra đời khi có hành
/// vi khẳng định của một NGƯỜI — và đường đó là `/knowledge/{id}/approve`, không phải đường này.
/// </summary>
public sealed class DocumentHandler(AppDbContext db, ITenantContext tenant, IKhoFile kho)
{
    public async Task<KetQuaNapTaiLieu> NapAsync(
        string tenKhaiBao,
        string loaiNoiDung,
        string subTenantCode,
        string nguoiNap,
        Stream noiDung,
        CancellationToken ct = default)
    {
        // Băm TRƯỚC khi ghi kho: nạp trùng thì không tốn thêm một bản sao trên đĩa, và câu
        // "file này đã có chưa" trả lời được mà không phải so từng byte.
        using var bo = new MemoryStream();
        await noiDung.CopyToAsync(bo, ct);
        bo.Position = 0;

        var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(bo, ct));
        bo.Position = 0;

        var daCo = await db.Set<Document>().FirstOrDefaultAsync(d => d.Hash == hash, ct);

        if (daCo is not null)
        {
            // Nạp lại cùng một file KHÔNG phải lỗi — job đồng bộ chạy lại là chuyện thường,
            // cùng lý do `IM-20` ở đường tín hiệu. Trả lại bản đã có.
            return new KetQuaNapTaiLieu(
                daCo.Id, daCo.TenKhaiBao, daCo.KichThuoc, daCo.Hash, DaCoTuTruoc: true, TrungVoi: daCo.Id,
                daCo.TrangThaiTrichChu, daCo.GhiChuTrichChu);
        }

        // Ô K (P9): trích chữ NGAY lúc nạp, từ đúng chuỗi byte vừa băm. Trích hỏng KHÔNG chặn việc nạp — file gốc
        // vẫn vào kho, và trạng thái nói rõ vì sao chưa đọc được (`N4-1`: không thêm thư viện).
        var chu = TrichChu.Trich(tenKhaiBao, bo.ToArray());

        var maKho = await kho.LuuAsync(tenant.TenantId, bo, ct);

        var tl = new Document
        {
            TenantId = tenant.TenantId,
            TenKhaiBao = tenKhaiBao,
            LoaiNoiDung = loaiNoiDung,
            KichThuoc = bo.Length,
            MaKho = maKho,
            Hash = hash,
            SubTenantCode = subTenantCode,
            NguoiNap = nguoiNap,
            TrangThaiTrichChu = chu.TrangThai,
            NoiDungChu = chu.NoiDungChu,
            GhiChuTrichChu = chu.GhiChu,
        };

        db.Set<Document>().Add(tl);
        await db.SaveChangesAsync(ct);

        return new KetQuaNapTaiLieu(tl.Id, tl.TenKhaiBao, tl.KichThuoc, tl.Hash, DaCoTuTruoc: false, TrungVoi: null,
            tl.TrangThaiTrichChu, tl.GhiChuTrichChu);
    }

    public async Task<IReadOnlyList<TomTatTaiLieu>> LietKeAsync(CancellationToken ct = default) =>
        await db.Set<Document>()
            .OrderByDescending(d => d.NapLuc)
            .Select(d => new TomTatTaiLieu(
                d.Id, d.TenKhaiBao, d.LoaiNoiDung, d.KichThuoc, d.SubTenantCode, d.NguoiNap, d.NapLuc, d.TrangThaiTrichChu))
            .ToListAsync(ct);

    /// <summary>Chữ trích được (ô `K`). <c>null</c> = không có tài liệu đó trong phạm vi khách này.</summary>
    public async Task<ChuTaiLieu?> DocChuAsync(Guid id, CancellationToken ct = default) =>
        await db.Set<Document>()
            .Where(d => d.Id == id)
            .Select(d => new ChuTaiLieu(d.Id, d.TenKhaiBao, d.TrangThaiTrichChu, d.GhiChuTrichChu, d.NoiDungChu))
            .FirstOrDefaultAsync(ct);

    /// <summary>Đọc lại nội dung. <c>null</c> nghĩa là không có — RLS đã lo phần của khách khác.</summary>
    public async Task<(Document Meta, Stream NoiDung)?> TaiAsync(Guid id, CancellationToken ct = default)
    {
        var meta = await db.Set<Document>().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (meta is null) return null;

        var noi = await kho.DocAsync(tenant.TenantId, meta.MaKho, ct);

        // Có dòng trong DB mà không có file trong kho — nói ra thay vì trả rỗng. Rỗng ở đây
        // đọc ra thành "tài liệu trống", khác hẳn "kho và DB đã lệch nhau".
        return noi is null ? null : (meta, noi);
    }

    /// <summary>
    /// Kiểm đầu vào ở cửa. Trả chỗ sai đầu tiên, hoặc <c>null</c>.
    ///
    /// ⚠ `subTenantCode` bắt buộc, cùng luật với evidence (`AR-l`): chưa biết thì khai
    /// <c>khong-xac-dinh</c>, không thuộc khách sạn nào thì khai <c>khong-thuoc-khach-san</c>.
    /// Tự điền hộ ở cửa vào thì về sau không dựng lại được.
    /// </summary>
    public static string? KiemTra(string? ten, string? subTenantCode, string? nguoiNap, long kichThuoc, int tranMB)
    {
        if (string.IsNullOrWhiteSpace(ten)) return "Thiếu tên file.";
        if (ten.Length > 512) return "Tên file dài quá 512 ký tự.";
        if (string.IsNullOrWhiteSpace(nguoiNap)) return "Thiếu `nguoiNap`.";
        if (kichThuoc == 0) return "File rỗng.";

        if (kichThuoc > (long)tranMB * 1024 * 1024)
        {
            return $"File {kichThuoc / 1024.0 / 1024:F1} MB, vượt trần {tranMB} MB.";
        }

        if (string.IsNullOrWhiteSpace(subTenantCode))
        {
            return "subTenantCode không được để trống — AR-l: bên gửi PHẢI khai mã khách sạn. " +
                   $"Chưa biết thì khai \"{SubTenantCodes.KhongXacDinh}\"; không thuộc khách sạn nào " +
                   $"thì khai \"{SubTenantCodes.KhongThuocKhachSan}\".";
        }

        return SubTenantCodes.TryChuanHoa(subTenantCode, out _)
            ? null
            : $"subTenantCode dài quá {SubTenantCodes.MaxLength} ký tự.";
    }
}
