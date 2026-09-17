using KnowledgePlatform.Domain.Documents;

namespace KnowledgePlatform.Infrastructure.Documents;

public sealed class KhoFileOptions
{
    public const string SectionName = "KhoFile";

    /// <summary>Thư mục gốc. Thiếu là KHÔNG dùng được Kênh 2 — cố ý không có mặc định.</summary>
    public string? ThuMucGoc { get; set; }

    /// <summary>Trần kích thước một file, tính bằng MB.</summary>
    public int TranMB { get; set; } = 25;
}

/// <summary>
/// <see cref="IKhoFile"/> chạy trên ĐĨA CỤC BỘ.
///
/// ⚠ **`H3-2` chốt MinIO; đây không phải bản thay thế quyết định đó** — xem phần tóm tắt của
/// <see cref="IKhoFile"/>. Máy dựng bản này không có container runtime, nên một bản MinIO viết
/// ra sẽ là code chưa từng chạy một lần nào.
///
/// 🛑 **Mỗi khách hàng một thư mục riêng, và mã kho do SẢN PHẨM sinh.** Hai chốt, hai lý do:
/// <list type="bullet">
///   <item>thư mục theo tenant: một lỗi ghép đường dẫn không đủ để khách A đọc file khách B —
///     ở đây không có RLS che lưng, nên tầng này phải tự đứng vững;</item>
///   <item>mã do sản phẩm sinh: tên người nạp khai KHÔNG BAO GIỜ chạm tới đường dẫn. Dùng nó
///     là mở thẳng đường đi ngang thư mục (<c>../../</c>) và đường ghi đè file của khách khác.</item>
/// </list>
/// </summary>
public sealed class KhoFileDia(string thuMucGoc) : IKhoFile
{
    public async Task<string> LuuAsync(Guid tenantId, Stream noiDung, CancellationToken ct = default)
    {
        var ma = Guid.CreateVersion7().ToString("N");
        var duong = Duong(tenantId, ma);

        Directory.CreateDirectory(Path.GetDirectoryName(duong)!);

        await using var ra = File.Create(duong);
        await noiDung.CopyToAsync(ra, ct);

        return ma;
    }

    public Task<Stream?> DocAsync(Guid tenantId, string maKho, CancellationToken ct = default)
    {
        if (!HopLe(maKho)) return Task.FromResult<Stream?>(null);

        var duong = Duong(tenantId, maKho);

        return Task.FromResult<Stream?>(
            File.Exists(duong) ? File.OpenRead(duong) : null);
    }

    public Task XoaAsync(Guid tenantId, string maKho, CancellationToken ct = default)
    {
        if (!HopLe(maKho)) return Task.CompletedTask;

        var duong = Duong(tenantId, maKho);
        if (File.Exists(duong)) File.Delete(duong);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Mã kho phải là 32 ký tự hex — đúng thứ <see cref="LuuAsync"/> sinh ra.
    ///
    /// ⚠ Kiểm ở đây dù mã vốn do sản phẩm sinh: mã đi qua database rồi quay lại, và một ngày
    /// nào đó sẽ có đường khác gọi vào. Một hàm ghép đường dẫn mà tin vào đầu vào của mình là
    /// chỗ mà mọi lỗi đi ngang thư mục bắt đầu.
    /// </summary>
    private static bool HopLe(string maKho) =>
        maKho.Length == 32 && maKho.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));

    private string Duong(Guid tenantId, string maKho) =>
        Path.Combine(thuMucGoc, tenantId.ToString("N"), maKho);
}
