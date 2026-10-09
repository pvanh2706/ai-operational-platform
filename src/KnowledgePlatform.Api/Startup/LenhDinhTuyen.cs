using System.Diagnostics;
using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Api.Tenancy;

namespace KnowledgePlatform.Api.Startup;

/// <summary>
/// Hai lệnh của **vòng không-credit** cho định tuyến (`--xuat-dinh-tuyen`, `--nap-dinh-tuyen`).
///
/// <code>
///   -- --xuat-dinh-tuyen &lt;mã-khách&gt; &lt;file-ra&gt; [số-lượng]
///   ...dán nội dung file cho một ngữ cảnh AI, lưu JSON nó trả về...
///   -- --nap-dinh-tuyen &lt;mã-khách&gt; &lt;file-vào&gt; &lt;tên người nạp&gt;
/// </code>
///
/// ⚠ Là lệnh CLI chứ không phải endpoint, cùng lý do với `--tao-khoa`: file xuất ra chứa dữ
/// liệu khách THẬT, và một endpoint trả nó về qua HTTP là thêm một đường ra phải canh.
/// </summary>
public static class LenhDinhTuyen
{
    public static async Task XuatAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Dùng: --xuat-dinh-tuyen <mã-khách> <file-ra> [số-lượng, mặc định 50]");
            Environment.ExitCode = 2;
            return;
        }

        var soLuong = args.Length > 3 && int.TryParse(args[3], out var n) ? n : 50;

        var duong = Path.GetFullPath(args[2]);
        if (!KiemChoGhi(duong)) { Environment.ExitCode = 7; return; }

        var taxonomy = DocTaxonomy(services);
        if (taxonomy is null) { Environment.ExitCode = 5; return; }

        await TrongScopeAsync(args[1], services, ct, async sp =>
        {
            var vong = sp.GetRequiredService<VongKhongCredit>();
            var ticket = await vong.ChonTicketAsync(soLuong, ct);

            if (ticket.Count == 0)
            {
                Console.WriteLine("Không có ticket nào chưa định tuyến. Không ghi gì.");
                return;
            }

            await File.WriteAllTextAsync(duong, VongKhongCredit.DungGoi(
                ticket, taxonomy.Value, sp.GetRequiredService<CongChe>().DaQuaCongChe), ct);

            Console.WriteLine($"Đã xuất {ticket.Count} ticket -> {duong}");
            Console.WriteLine();
            Console.WriteLine("Bước tiếp:");
            Console.WriteLine("  1. Mở file, dán TOÀN BỘ cho một ngữ cảnh AI sạch.");
            Console.WriteLine("  2. Lưu khối JSON nó trả về (phần `ketQua`) ra một file.");
            Console.WriteLine($"  3. --nap-dinh-tuyen {args[1]} <file-đó> \"<tên bạn>\"");
            Console.WriteLine();
            Console.WriteLine("⚠ File này là dữ liệu khách THẬT (đã che bí mật). Đừng commit,");
            Console.WriteLine("  đừng dán vào nơi công khai.");
        });
    }

    public static async Task NapAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        if (args.Length < 4)
        {
            Console.Error.WriteLine("Dùng: --nap-dinh-tuyen <mã-khách> <file-vào> <tên người nạp>");
            Environment.ExitCode = 2;
            return;
        }

        var duong = Path.GetFullPath(args[2]);
        if (!File.Exists(duong))
        {
            Console.Error.WriteLine($"Không thấy file: {duong}");
            Environment.ExitCode = 4;
            return;
        }

        var taxonomy = DocTaxonomy(services);
        if (taxonomy is null) { Environment.ExitCode = 5; return; }

        var tenNhom = taxonomy.Value.GetProperty("nhom").EnumerateArray()
            .Select(x => x.GetProperty("ten").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        List<KetQuaDinhTuyenNap>? dong;
        try
        {
            dong = DocKetQua(await File.ReadAllTextAsync(duong, ct));
        }
        catch (JsonException e)
        {
            Console.Error.WriteLine($"File không đọc được thành JSON: {e.Message}");
            Environment.ExitCode = 4;
            return;
        }

        if (dong is null || dong.Count == 0)
        {
            Console.Error.WriteLine("Không thấy dòng kết quả nào. File phải có mảng `ketQua`, hoặc chính nó là mảng.");
            Environment.ExitCode = 4;
            return;
        }

        await TrongScopeAsync(args[1], services, ct, async sp =>
        {
            var kq = await sp.GetRequiredService<VongKhongCredit>()
                .NapAsync(dong, tenNhom, args[3], ct);

            Console.WriteLine($"Đã nạp {kq.DaNap} dòng mới · đè {kq.DaDe} dòng cũ · bỏ qua {kq.BoQua.Count}");

            foreach (var b in kq.BoQua) Console.WriteLine($"   · {b}");

            if (kq.BoQua.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("⚠ Dòng bị bỏ qua KHÔNG được sửa hộ. Tên nhóm phải chép nguyên từ taxonomy —");
                Console.WriteLine("  \"sửa cho giống\" là cách biến một lỗi nhìn thấy được thành một lỗi im lặng.");
            }

            Console.WriteLine();
            Console.WriteLine("⚠ Nạp xong KHÔNG có nghĩa là định tuyến đã đúng. Chấm lại bằng:");
            Console.WriteLine("     python scripts/jira-export/cham_routing.py <file kết quả>");
            Console.WriteLine("  Con số 84,1% ở docs/13 đo trên MỘT prompt và MỘT model — không mang sang đây được.");
        });
    }

    /// <summary>Mở một scope với tenant đặt tường minh — không có middleware trong lệnh CLI.</summary>
    internal static async Task TrongScopeAsync(
        string maKhach, IServiceProvider services, CancellationToken ct, Func<IServiceProvider, Task> viec)
    {
        var id = await services.GetRequiredService<TenantDirectory>().FindIdByExternalKeyAsync(maKhach, ct);

        if (id is null)
        {
            Console.Error.WriteLine($"Không có khách hàng nào mang mã \"{maKhach}\".");
            Environment.ExitCode = 3;
            return;
        }

        await using var scope = services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(id.Value);

        await viec(scope.ServiceProvider);
    }

    /// <summary>
    /// Nhận cả hai hình dạng: <c>{"ketQua": [...]}</c> và một mảng trần.
    ///
    /// ⚠ Cố ý dễ dãi ở ĐÚNG chỗ này: người dùng đang chép-dán từ một cửa sổ chat, và bắt họ
    /// gọt đúng một hình dạng là đẩy một bước thủ công nữa vào một quy trình vốn đã thủ công.
    /// Dễ dãi về HÌNH DẠNG BỌC NGOÀI, nghiêm khắc về NỘI DUNG (tên nhóm) — hai chuyện khác nhau.
    /// </summary>
    private static List<KetQuaDinhTuyenNap>? DocKetQua(string noi)
    {
        var tuyChon = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        using var doc = JsonDocument.Parse(noi);

        var goc = doc.RootElement;

        if (goc.ValueKind == JsonValueKind.Object && goc.TryGetProperty("ketQua", out var mang))
        {
            goc = mang;
        }

        return goc.ValueKind == JsonValueKind.Array
            ? goc.Deserialize<List<KetQuaDinhTuyenNap>>(tuyChon)
            : null;
    }

    private static JsonElement? DocTaxonomy(IServiceProvider services)
    {
        var cfg = services.GetRequiredService<IConfiguration>();
        var duong = cfg["DinhTuyen:DuongDanTaxonomy"] ?? DuongTaxonomyMacDinh();

        if (!File.Exists(duong))
        {
            Console.Error.WriteLine($"Không thấy taxonomy: {duong}");
            Console.Error.WriteLine("Đặt DinhTuyen:DuongDanTaxonomy nếu nó nằm chỗ khác.");
            return null;
        }

        return JsonDocument.Parse(File.ReadAllText(duong)).RootElement.Clone();
    }

    // Một chỗ dò đường mặc định cho cả CLI lẫn API phản hồi (P6) — xem TaxonomyNhom.
    private static string DuongTaxonomyMacDinh() => TaxonomyNhom.DuongMacDinh();

    /// <summary>
    /// Chốt chép nguyên từ <c>SoanNhapRunner --xuat-payload</c>: file chứa dữ liệu khách thật,
    /// nên nếu nó nằm trong repo thì git PHẢI đang chặn.
    ///
    /// ⚠ Hỏi git chứ không so tên thư mục — so tên là đoán. Repo đã rò một lần theo đúng hình
    /// dạng này: một file dump mà `.gitignore` không chặn (commit `888c8e7`).
    /// </summary>
    internal static bool KiemChoGhi(string duong)
    {
        var thuMuc = Path.GetDirectoryName(duong);
        if (!string.IsNullOrEmpty(thuMuc) && !Directory.Exists(thuMuc))
        {
            Console.Error.WriteLine($"Thư mục không tồn tại: {thuMuc}");
            return false;
        }

        var goc = TimGocRepo();
        if (goc is null) return true;   // ngoài repo -> git không có ý kiến gì

        if (!duong.StartsWith(goc + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (GitDangChan(goc, duong)) return true;

        Console.Error.WriteLine("🛑 TỪ CHỐI GHI — chỗ ghi này nằm trong repo và git KHÔNG chặn:");
        Console.Error.WriteLine("   " + duong);
        Console.Error.WriteLine();
        Console.Error.WriteLine("File này chứa nội dung ticket thật (đã che bí mật, nhưng vẫn là dữ liệu");
        Console.Error.WriteLine("khách). Ghi ra ngoài repo, hoặc vào chỗ .gitignore chặn.");
        return false;
    }

    private static string? TimGocRepo()
    {
        var thu = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && thu is not null; i++)
        {
            if (Directory.Exists(Path.Combine(thu, ".git"))) return Path.GetFullPath(thu);
            thu = Path.GetDirectoryName(thu);
        }

        return null;
    }

    private static bool GitDangChan(string goc, string duong)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("git", $"check-ignore -q \"{duong}\"")
            {
                WorkingDirectory = goc,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            if (p is null) return false;
            p.WaitForExit(5000);
            return p.ExitCode == 0;
        }
        catch (Exception)
        {
            // Không chạy được git -> KHÔNG coi là đang chặn. Nghiêng về phía từ chối ghi.
            return false;
        }
    }
}
