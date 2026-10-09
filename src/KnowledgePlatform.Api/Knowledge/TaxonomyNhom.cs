using System.Text.Json;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>
/// Danh sách tên nhóm hợp lệ của taxonomy — nạp MỘT lần. Tách ra 2026-10-10 (P6) khi chỗ dùng thứ hai ra đời:
/// phản hồi "nhóm đúng là X" (`E2-1`) phải bị từ chối nếu X không có trong taxonomy, đúng như đường nạp định
/// tuyến của vòng không-credit. Hai chỗ tự đọc file là hai chỗ sẽ đọc hai file khác nhau.
///
/// ⚠ Không có file thì <see cref="TenNhom"/> là <c>null</c> — chỗ dùng phải TỪ CHỐI, không được coi "không biết
/// taxonomy" là "nhóm nào cũng hợp lệ". Một tên nhóm bịa đi vào kho định tuyến là cùng loại lỗi `G6` chặn.
/// </summary>
public sealed class TaxonomyNhom(IConfiguration cfg)
{
    private readonly Lazy<IReadOnlySet<string>?> _ten = new(() => Doc(cfg));

    public IReadOnlySet<string>? TenNhom => _ten.Value;

    public string DuongDan => cfg["DinhTuyen:DuongDanTaxonomy"] ?? DuongMacDinh();

    private static IReadOnlySet<string>? Doc(IConfiguration cfg)
    {
        var duong = cfg["DinhTuyen:DuongDanTaxonomy"] ?? DuongMacDinh();
        if (!File.Exists(duong)) return null;

        using var doc = JsonDocument.Parse(File.ReadAllText(duong));
        return doc.RootElement.GetProperty("nhom").EnumerateArray()
            .Select(x => x.GetProperty("ten").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Dò ngược từ thư mục chạy tới gốc repo — cùng cách <c>DinhTuyenOptions.DuongDanMacDinh</c>.</summary>
    public static string DuongMacDinh()
    {
        var goc = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && goc is not null; i++)
        {
            var thu = Path.Combine(goc, "docs", "ket-qua-phan-tich", "taxonomy-19-nhom-hoa-don.json");
            if (File.Exists(thu)) return thu;
            goc = Path.GetDirectoryName(goc);
        }

        return "docs/ket-qua-phan-tich/taxonomy-19-nhom-hoa-don.json";
    }
}
