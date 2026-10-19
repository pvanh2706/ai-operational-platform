namespace KnowledgePlatform.Api.Startup;

/// <summary>Kết quả của lần nạp <c>appsettings.Local.json</c> — để <c>--kiem-cau-hinh</c> nói ra được.</summary>
/// <param name="DuongDan">File đã nạp, hoặc <c>null</c>.</param>
/// <param name="LyDoKhongNap">Vì sao không nạp, khi có file mà không nạp. <c>null</c> = không có gì để nói.</param>
public sealed record NguonCauHinhLocal(string? DuongDan, string? LyDoKhongNap);

/// <summary>
/// Nạp <c>appsettings.Local.json</c> ở GỐC REPO vào cấu hình của host — `O2-1` (docs/18, 2026-10-19).
///
/// 🛑 Vì sao có lớp này: tới 2026-10-19 file đó CHỈ <c>tools/SoanNhapRunner</c> đọc. Chủ dự án đặt khoá
/// vào đó (đúng như <c>appsettings.Local.example.json</c> hướng dẫn), rồi <c>dotnet run</c> host — và host
/// không thấy khoá: định tuyến trả KHÔNG BIẾT, soạn nháp trả <c>chua-co-credit</c>. Không crash. Cùng
/// hình dạng với lỗi hai-tên-một-khoá mà <see cref="KhoaAnthropic"/> đã sửa: hai chỗ đọc, hai sự thật.
///
/// Ba cái chốt, mỗi cái vì một lý do riêng:
/// <list type="number">
///   <item><b>Chỉ ở môi trường Development.</b> File này là tiện ích của MÁY DEV. Bản deploy thật lấy khoá
///     từ biến môi trường như trước (`AR-i`), và bộ test chạy ở Production nên không thừa hưởng khoá của
///     máy đang chạy test — một khoá thật lọt vào test là một lượt gọi model tính tiền.</item>
///   <item><b>Hỏi <c>git check-ignore</c> trước khi đọc</b> — chép cổng của <c>SoanNhapRunner</c>. Lập luận
///     gốc của câu "khoá KHÔNG đi qua appsettings" là FILE ĐƯỢC GIT THEO DÕI; git không chặn thì không đọc.
///     Không gọi được git cũng coi là không chặn: fail closed.</item>
///   <item><b>Biến môi trường và tham số dòng lệnh vẫn THẮNG file.</b> Chúng được nạp lại SAU file, nên
///     người đặt <c>Anthropic__ApiKey</c> trong cửa sổ lệnh không bị một file nằm im trên đĩa lặng lẽ đè.</item>
/// </list>
/// </summary>
public static class CauHinhLocal
{
    public const string TenFile = "appsettings.Local.json";

    public static NguonCauHinhLocal Nap(WebApplicationBuilder builder, string[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!builder.Environment.IsDevelopment()) return new(null, null);

        var goc = TimGocRepo(builder.Environment.ContentRootPath);
        if (goc is null) return new(null, null);

        var duong = Path.Combine(goc, TenFile);
        if (!File.Exists(duong)) return new(null, null);

        if (!GitDangChan(goc, duong))
        {
            const string lyDo = "git KHÔNG chặn file này (hoặc không gọi được git) — khoá trong file được theo dõi " +
                                "là khoá đi vào git history. Không đọc. Thêm vào .gitignore, hoặc dùng biến môi trường.";
            Console.Error.WriteLine($"🛑 Bỏ qua {TenFile}: {lyDo}");
            return new(null, lyDo);
        }

        builder.Configuration.AddJsonFile(duong, optional: true, reloadOnChange: false);
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddCommandLine(args);

        return new(duong, null);
    }

    /// <summary>Dò ngược tới thư mục có <c>.git</c> — cùng cách <c>SoanNhapRunner.TimGocRepo</c>.</summary>
    private static string? TimGocRepo(string batDau)
    {
        var d = new DirectoryInfo(batDau);
        while (d is not null && !Directory.Exists(Path.Combine(d.FullName, ".git")) && !File.Exists(Path.Combine(d.FullName, ".git")))
        {
            d = d.Parent;
        }

        return d?.FullName;
    }

    private static bool GitDangChan(string goc, string duongDan)
    {
        try
        {
            using var pr = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                ArgumentList = { "check-ignore", "-q", duongDan },
                WorkingDirectory = goc,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            if (pr is null) return false;

            pr.WaitForExit();
            return pr.ExitCode == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
