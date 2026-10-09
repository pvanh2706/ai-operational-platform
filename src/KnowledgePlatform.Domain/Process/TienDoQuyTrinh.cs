using System.Globalization;
using System.Text;
using KnowledgePlatform.Domain.Evidence;

namespace KnowledgePlatform.Domain.Process;

/// <summary>
/// Ô `E` + `F` của sơ đồ: case đang ở bước nào, suy RA TỪ EVIDENCE — không đọc cờ nào (PR1).
///
/// 🛑 Hàm THUẦN, không chạm hạ tầng (`IM-18`): đầu vào là định nghĩa + các mẩu evidence của case, đầu ra là
/// phép chiếu. Đây là chỗ DUY NHẤT quyết định "bước này xong chưa", nên nó phải test được không cần database.
///
/// Luật (`N2-1`, chốt 2026-10-10):
/// <list type="bullet">
///   <item>bước XONG ⟸ có mẩu chưa rút lại chứa một DẤU HIỆU của bước (so không dấu, không hoa thường),
///     HOẶC có mẩu là LỜI XÁC NHẬN trỏ đúng bước đó;</item>
///   <item>bước HIỆN TẠI = bước chưa xong ĐẦU TIÊN theo thứ tự (PR1). Không có trường "bước tiếp theo"
///     riêng — PR1 chỉ định nghĩa một thứ;</item>
///   <item>mẩu đã RÚT LẠI (`AR-m`) không làm xong bước nào.</item>
/// </list>
///
/// ⚠ Cái giá nói thẳng (`05`:113-121): dấu hiệu lỏng thì bước xong GIẢ, chặt thì người dùng bị kẹt. Vì thế
/// mỗi bước xong trả kèm CĂN CỨ — mẩu nào, bằng cách nào — để người đọc phản bác được (`G4`).
/// </summary>
public static class TienDoQuyTrinh
{
    public static TienDo SuyRa(ProcessDefinition dinhNghia, IEnumerable<EvidenceItem> evidence)
    {
        ArgumentNullException.ThrowIfNull(dinhNghia);
        ArgumentNullException.ThrowIfNull(evidence);

        var dungDuoc = evidence.Where(e => !e.DaRutLai).ToList();
        var daChuanHoa = dungDuoc.ToDictionary(e => e.Id, e => ChuanHoa(e.Content));

        var cacBuoc = dinhNghia.Buoc
            .OrderBy(b => b.ThuTu)
            .Select(b =>
            {
                var canCu = new List<CanCuXong>();
                var dauHieu = b.DauHieu
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Select(d => (Goc: d, Chuan: ChuanHoa(d)))
                    .ToList();

                foreach (var e in dungDuoc)
                {
                    if (e.XacNhanProcessStepId == b.Id)
                    {
                        canCu.Add(new CanCuXong(e.Id, e.SourceReference, "xac-nhan"));
                        continue;
                    }

                    var khop = dauHieu.FirstOrDefault(d => daChuanHoa[e.Id].Contains(d.Chuan, StringComparison.Ordinal));
                    if (khop.Goc is not null)
                    {
                        canCu.Add(new CanCuXong(e.Id, e.SourceReference, $"dau-hieu:{khop.Goc}"));
                    }
                }

                return new TienDoBuoc(b.Id, b.ThuTu, b.Ma, b.NoiDung, b.NoiXem, b.ChuDe, canCu.Count > 0, canCu);
            })
            .ToList();

        var hienTai = cacBuoc.FirstOrDefault(b => !b.DaXong);
        return new TienDo(cacBuoc, hienTai);
    }

    /// <summary>
    /// Bỏ dấu, chữ thường. Tiếng Việt trong ticket thật lẫn cả có dấu lẫn không dấu (khách gõ điện thoại,
    /// nhân viên gõ máy tính) — so nguyên văn là bỏ sót một nửa.
    /// ⚠ <c>đ</c> không phải dấu kết hợp trong Unicode, nên phải đổi tay.
    /// </summary>
    public static string ChuanHoa(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var tach = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(tach.Length);

        foreach (var c in tach)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(c switch { 'đ' => 'd', 'Đ' => 'D', _ => c });
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}

/// <param name="BuocHienTai"><c>null</c> nghĩa là mọi bước đã xong.</param>
public sealed record TienDo(IReadOnlyList<TienDoBuoc> Buoc, TienDoBuoc? BuocHienTai)
{
    public bool DaXongHet => BuocHienTai is null;
}

public sealed record TienDoBuoc(
    Guid Id,
    int ThuTu,
    string Ma,
    string NoiDung,
    string? NoiXem,
    string ChuDe,
    bool DaXong,
    IReadOnlyList<CanCuXong> CanCu);

/// <summary>Vì sao một bước được coi là xong: mẩu nào, bằng cách nào (<c>xac-nhan</c> hay <c>dau-hieu:…</c>).</summary>
public sealed record CanCuXong(Guid MauId, string KhoaNguonMau, string Cach);
