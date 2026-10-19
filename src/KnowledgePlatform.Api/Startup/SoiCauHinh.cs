using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Api.Webhooks;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Startup;

/// <summary>
/// `--kiem-cau-hinh` — in ra cấu hình ĐANG CÓ HIỆU LỰC và nói thẳng chỗ nào chưa xong.
///
/// 🛑 **Vì sao là một lệnh chạy được chứ không phải một mục trong tài liệu deploy.** Dự án
/// này đã đo được hai lần rằng tài liệu tự lỗi thời trong im lặng: bộ Postman đứng ở 5/13
/// endpoint hơn hai tuần (`IM-38`), và `docs/10` ghi *"newman có sẵn"* trong khi máy mới
/// không có. Một hồ sơ deploy viết ra giấy sẽ đi đúng đường đó — nó mô tả cái máy mà người
/// viết đang ngồi, không mô tả cái máy người đọc đang dựng.
///
/// Lệnh này thì đọc CHÍNH cấu hình đang chạy, nên nó không lỗi thời được.
///
/// ⚠ Nó KHÔNG sửa gì và KHÔNG mở cổng nghe. Chạy được cả khi cấu hình còn sai — đó là điểm.
/// </summary>
public static class SoiCauHinh
{
    public static async Task<int> ChayAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var cfg = services.GetRequiredService<IConfiguration>();
        var tenancy = services.GetRequiredService<IOptions<TenancyOptions>>().Value;
        var auth = services.GetRequiredService<IOptions<AuthOptions>>().Value;
        var link = services.GetRequiredService<IOptions<LinkXemOptions>>().Value;
        var webhook = services.GetRequiredService<IOptions<WebhookOptions>>().Value;
        var congChe = services.GetRequiredService<IOptions<CongCheOptions>>().Value;
        var dinhTuyen = services.GetRequiredService<IOptions<DinhTuyenOptions>>().Value;

        var vanDe = new List<string>();

        Console.WriteLine(new string('=', 70));
        Console.WriteLine("SOI CẤU HÌNH — bản deploy này đang chạy bằng gì");
        Console.WriteLine(new string('=', 70));

        // --- Database ---
        var conn = cfg.GetConnectionString("Default");
        Muc("Chuỗi kết nối");
        if (string.IsNullOrWhiteSpace(conn))
        {
            Sai("THIẾU. Đặt biến môi trường ConnectionStrings__Default (`AR-i`/`C2-1`).");
            vanDe.Add("thiếu chuỗi kết nối");
        }
        else
        {
            Ok(CheMatKhau(conn));
        }

        // --- Chế độ tenancy ---
        Muc("Chế độ deploy");
        Console.WriteLine($"    Tenancy:Mode = {tenancy.Mode}");

        if (tenancy.Mode == TenancyMode.Unspecified)
        {
            Sai("Chưa cấu hình. App sẽ TỪ CHỐI KHỞI ĐỘNG (cố ý — G13 cấm đoán hộ).");
            vanDe.Add("chưa chọn chế độ deploy");
        }
        else if (tenancy.Mode == TenancyMode.DedicatedSingleTenant)
        {
            Console.WriteLine($"    phục vụ đúng một khách: \"{tenancy.TenantExternalKey}\"");
        }

        // --- Xác thực ---
        Muc("Xác thực người gọi (AR-e)");
        if (auth.AcknowledgeUnauthenticatedApi)
        {
            if (tenancy.Mode == TenancyMode.SharedMultiTenant)
            {
                Sai("Auth:AcknowledgeUnauthenticatedApi = true KHÔNG dùng được ở chế độ shared. " +
                    "App sẽ từ chối khởi động.");
                vanDe.Add("cờ chạy-không-khoá bật ở chế độ shared");
            }
            else
            {
                Canh("Auth:AcknowledgeUnauthenticatedApi = TRUE — API chạy KHÔNG cần khoá.");
                Console.WriteLine("       Chỉ dùng cho máy dev. Deploy thật: bỏ cờ này đi.");
                vanDe.Add("API đang chạy không cần khoá");
            }
        }
        else
        {
            Ok("đòi khoá API cho mọi endpoint (trừ /health, /health/ready, /xem/{token})");
        }

        // --- Cổng che (N1-2) ---
        Muc("Cổng che dữ liệu (AR-o, AR-n · N1-2)");
        if (!congChe.TatTamVaChapNhanRuiRo)
        {
            Ok("ĐANG CHẠY — dữ liệu đi ra (model, webhook, /xem) và vào kho đều qua cổng che");
        }
        else if (tenancy.Mode == TenancyMode.SharedMultiTenant)
        {
            Sai("CongChe:TatTamVaChapNhanRuiRo = true ở chế độ shared. App sẽ TỪ CHỐI KHỞI ĐỘNG.");
            vanDe.Add("cờ tắt cổng che bật ở chế độ shared");
        }
        else
        {
            Canh("CongChe:TatTamVaChapNhanRuiRo = TRUE — MỌI CỔNG CHE ĐANG TẮT.");
            Console.WriteLine("       Mật khẩu/token đi NGUYÊN VĂN tới model, webhook, /xem và vào kho.");
            Console.WriteLine("       Khâu nạp là cửa MỘT CHIỀU: tắt cờ đi cũng không che lại mẩu đã vào.");
            vanDe.Add("cổng che đang tắt (N1-2)");
        }

        // --- Khoá model (O2-1): CÓ hay KHÔNG, và từ đâu — không bao giờ in giá trị ---
        Muc("Khoá Anthropic (O2-1)");
        var coKhoa = KhoaAnthropic.Doc(cfg) is not null;
        var local = services.GetService<NguonCauHinhLocal>();
        if (local?.DuongDan is { } duongLocal) Console.WriteLine($"    đã nạp {duongLocal}");
        if (local?.LyDoKhongNap is { } lyDo) Canh($"{CauHinhLocal.TenFile}: {lyDo}");
        if (coKhoa)
        {
            Ok("CÓ khoá — soạn nháp (POST /knowledge/soan-nhap) gọi model thật, tính tiền.");
        }
        else
        {
            Console.WriteLine("    KHÔNG có khoá — dùng vòng xuất/nạp file (--xuat-dinh-tuyen, --xuat-soan-nhap, giao diện).");
        }

        // --- Định tuyến ticket MỚI bằng model (P8, N7-1 PROPOSED) ---
        Muc("Định tuyến ticket mới bằng model (N7-1)");
        if (!dinhTuyen.GoiModel)
        {
            Console.WriteLine("    TẮT (DinhTuyen:GoiModel = false) — ticket ngoài bảng đo nhận KHÔNG BIẾT.");
        }
        else if (!coKhoa)
        {
            Canh("DinhTuyen:GoiModel = true nhưng KHÔNG có khoá Anthropic — vẫn không gọi được, ticket mới nhận KHÔNG BIẾT.");
            vanDe.Add("bật định tuyến bằng model mà thiếu khoá");
        }
        else
        {
            Canh($"BẬT — mọi ticket ngoài bảng đo đều gọi model (tính tiền). Ngưỡng {dinhTuyen.NguongTinCay:0.00} CHƯA ĐO, " +
                 $"trần {dinhTuyen.TranThang} lượt/tháng/khách.");
        }

        // --- Địa chỉ link ---
        Muc("Link:PublicBaseUrl");
        if (string.IsNullOrWhiteSpace(link.PublicBaseUrl))
        {
            Canh("CHƯA ĐẶT. Endpoint phát link sẽ trả 409 — cố ý, vì đoán sẽ sinh ra " +
                 "link localhost chỉ mở được trên chính máy này.");
            vanDe.Add("chưa đặt Link:PublicBaseUrl");
        }
        else if (LaLoopback(link.PublicBaseUrl))
        {
            Canh($"{link.PublicBaseUrl} — TRỎ VÀO CHÍNH MÁY NÀY.");
            Console.WriteLine("       Link phát ra sẽ mở được khi BẠN bấm, và CHẾT khi gửi cho người khác.");
            Console.WriteLine("       Đặt bằng tên máy trong LAN/VPN (H1b-1), không phải localhost.");
            vanDe.Add("Link:PublicBaseUrl trỏ vào loopback");
        }
        else
        {
            Ok($"{link.PublicBaseUrl}  (hạn mặc định {link.HanMacDinhNgay} ngày, trần {link.HanToiDaNgay})");
        }

        // --- Webhook ---
        Muc("Đường trả kết quả về (D1-1)");
        Console.WriteLine(webhook.BatVongQuet
            ? $"    vòng quét BẬT, chu kỳ {webhook.ChuKyGiay}s, tối đa {webhook.MoiLuotToiDa} lượt/khách"
            : "    vòng quét TẮT — hàng đợi vẫn được xếp, nhưng KHÔNG ai gửi đi.");

        if (!webhook.BatVongQuet) vanDe.Add("vòng quét webhook đang tắt");

        // --- Khách hàng và khoá ---
        Muc("Khách hàng trong danh bạ");
        if (string.IsNullOrWhiteSpace(conn))
        {
            Console.WriteLine("    (bỏ qua — chưa có chuỗi kết nối)");
        }
        else
        {
            await LietKeKhachAsync(services, tenancy, vanDe, ct);
        }

        // --- Tổng kết ---
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));

        if (vanDe.Count == 0)
        {
            Console.WriteLine("✅ Không thấy vấn đề nào. Cấu hình này deploy thật được.");
            return 0;
        }

        Console.WriteLine($"⚠ {vanDe.Count} chỗ cần xem lại:");
        foreach (var v in vanDe) Console.WriteLine($"   · {v}");
        Console.WriteLine();
        Console.WriteLine("⚠ Không phải chỗ nào cũng là lỗi — máy dev thì mấy cảnh báo trên là bình thường.");
        Console.WriteLine("  Danh sách này để bạn ĐỌC QUA trước khi gọi một bản deploy là xong.");

        // Mã thoát 0: đây là lệnh SOI, không phải lệnh chặn. Trả khác 0 thì người ta sẽ cắm
        // nó vào CI rồi tắt nó đi ở đúng ngày nó bắt đầu nói điều khó nghe.
        return 0;
    }

    private static async Task LietKeKhachAsync(
        IServiceProvider services, TenancyOptions tenancy, List<string> vanDe, CancellationToken ct)
    {
        var khoaDir = services.GetRequiredService<ApiKeyDirectory>();

        IReadOnlyList<(Guid Id, string Ten, string ExternalKey)> khach;
        try
        {
            khach = await khoaDir.LietKeKhachAsync(ct);
        }
        catch (Exception e)
        {
            Sai($"Không đọc được danh bạ: {e.Message}");
            vanDe.Add("không kết nối được database");
            return;
        }

        if (khach.Count == 0)
        {
            Canh("CHƯA CÓ khách hàng nào. Dựng bằng: --tao-khach-hang <mã> \"<tên>\"");
            vanDe.Add("chưa có khách hàng nào");
            return;
        }

        foreach (var (id, ten, externalKey) in khach)
        {
            var (con, tong) = await khoaDir.DemAsync(id, ct);
            var la = tenancy.Mode == TenancyMode.DedicatedSingleTenant
                     && externalKey == tenancy.TenantExternalKey;

            var dau = la ? "->" : "  ";
            Console.WriteLine($"  {dau} {externalKey,-28} {ten}");
            Console.WriteLine($"       khoá API: {con} còn hiệu lực / {tong} tổng");

            if (con == 0)
            {
                Console.WriteLine($"       ⚠ chưa có khoá dùng được — tạo bằng: --tao-khoa {externalKey} \"<nhãn>\"");
                vanDe.Add($"khách \"{externalKey}\" chưa có khoá API còn hiệu lực");
            }
        }

        if (tenancy.Mode == TenancyMode.DedicatedSingleTenant
            && !khach.Any(k => k.ExternalKey == tenancy.TenantExternalKey))
        {
            Sai($"Tenancy:TenantExternalKey = \"{tenancy.TenantExternalKey}\" KHÔNG có trong danh bạ. " +
                "App sẽ từ chối khởi động.");
            vanDe.Add("tenant của bản deploy không tồn tại");
        }
    }

    /// <summary>
    /// Che mật khẩu trong chuỗi kết nối. ⚠ Lệnh này in ra màn hình và người ta sẽ dán kết quả
    /// vào chat để hỏi — nên chuỗi kết nối KHÔNG được ra nguyên vẹn.
    /// </summary>
    internal static string CheMatKhau(string conn) =>
        string.Join(';', conn.Split(';').Select(phan =>
            phan.TrimStart().StartsWith("Password", StringComparison.OrdinalIgnoreCase)
                ? "Password=***"
                : phan));

    internal static bool LaLoopback(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u)
        && (u.IsLoopback || u.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));

    private static void Muc(string ten)
    {
        Console.WriteLine();
        Console.WriteLine($"── {ten}");
    }

    private static void Ok(string s) => Console.WriteLine($"    ✅ {s}");
    private static void Canh(string s) => Console.WriteLine($"    ⚠  {s}");
    private static void Sai(string s) => Console.WriteLine($"    🛑 {s}");
}
