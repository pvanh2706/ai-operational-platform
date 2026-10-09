using System.Collections.Concurrent;
using KnowledgePlatform.Api.Tenancy;
using KnowledgePlatform.Domain.Evidence;
using KnowledgePlatform.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace KnowledgePlatform.Api.Documents;

public sealed class KenhHaiOptions
{
    public const string SectionName = "KenhHai";

    /// <summary>Các thư mục cần quét. Rỗng = không quét gì (mặc định).</summary>
    public List<NguonQuetThuMuc> Nguon { get; set; } = [];

    /// <summary>Bao lâu quét một lượt.</summary>
    public int ChuKyGiay { get; set; } = 300;

    /// <summary>Tắt vòng quét nền — bộ test tắt cái này rồi gọi <see cref="QuetThuMuc"/> thẳng (cùng lý do webhook).</summary>
    public bool BatVongQuet { get; set; } = true;
}

/// <summary>
/// Một thư mục cần quét. ⚠ <see cref="SubTenantCode"/> và <see cref="NguoiNap"/> BẮT BUỘC, không có mặc định — cùng
/// luật `AR-l` với mọi đường nạp: "chưa biết" phải là một lời khai, không phải một ô bị bỏ quên.
/// </summary>
public sealed class NguonQuetThuMuc
{
    public string? ThuMuc { get; set; }

    public string? SubTenantCode { get; set; }

    public string? NguoiNap { get; set; }

    /// <summary>Chế độ shared: khách nào sở hữu thư mục này. Chế độ dedicated: bỏ trống (hoặc đúng khách của bản deploy).</summary>
    public string? TenantKey { get; set; }
}

/// <summary>
/// Nhớ file nào đã đọc ở trạng thái nào — để lượt quét sau KHÔNG đọc lại cả thư mục mỗi 5 phút. Singleton.
///
/// ⚠ Chỉ là đường TẮT về hiệu năng, không phải chốt chống trùng: tiến trình khởi động lại thì nó rỗng và cả thư mục
/// được đọc lại — vô hại, vì <see cref="DocumentHandler"/> khử trùng theo SHA-256 của nội dung.
/// </summary>
public sealed class DauVetQuet
{
    private readonly ConcurrentDictionary<(Guid Tenant, string Duong), (long Dai, DateTime Sua)> _daDoc = new();

    public bool DaDoc(Guid tenant, FileInfo f) =>
        _daDoc.TryGetValue((tenant, f.FullName), out var v) && v.Dai == f.Length && v.Sua == f.LastWriteTimeUtc;

    public void Ghi(Guid tenant, FileInfo f) => _daDoc[(tenant, f.FullName)] = (f.Length, f.LastWriteTimeUtc);
}

/// <summary>
/// Ô `A2` — "job quét" của Kênh 2 (P9, `N4-1`): quét MỘT thư mục cấu hình, nạp từng file qua ĐÚNG đường
/// <c>POST /documents</c> đi (<see cref="DocumentHandler"/>), nên mọi luật của đường đó áp nguyên: khử trùng theo hash,
/// trích chữ, `S6` (nạp tài liệu KHÔNG sinh tri thức).
///
/// ⚠ Drive: không có connector (cần thư viện + khoá — `N4-1` chọn không thêm). Thư mục Drive ĐỒNG BỘ VỀ MÁY dùng được
/// như một thư mục thường — đó là đường duy nhất tới Drive hôm nay, và ô `A2` vì thế vẫn `◐`.
///
/// ⚠ Gọi khi tenant ĐÃ được resolve trong scope — <see cref="QuetThuMucService"/> làm việc đó, test cũng vậy.
/// </summary>
public sealed class QuetThuMuc(DocumentHandler tailieu, DauVetQuet dauVet, IOptions<KhoFileOptions> kho, Domain.Tenancy.ITenantContext tenant,
    ILogger<QuetThuMuc> logger)
{
    public async Task<KetQuaQuet> QuetAsync(NguonQuetThuMuc nguon, CancellationToken ct = default)
    {
        var thuMuc = nguon.ThuMuc!;
        var moi = 0;
        var daCo = 0;
        var boQua = new List<string>();

        foreach (var duong in Directory.EnumerateFiles(thuMuc, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var f = new FileInfo(duong);

            // File tạm của Office (~$…) và file ẩn (.…) không phải tài liệu ai muốn nạp.
            if (f.Name.StartsWith("~$", StringComparison.Ordinal) || f.Name.StartsWith('.')) continue;
            if (dauVet.DaDoc(tenant.TenantId, f)) continue;

            var sai = DocumentHandler.KiemTra(f.Name, nguon.SubTenantCode, nguon.NguoiNap, f.Length, kho.Value.TranMB);
            if (sai is not null)
            {
                boQua.Add($"{Path.GetRelativePath(thuMuc, duong)}: {sai}");
                dauVet.Ghi(tenant.TenantId, f);
                continue;
            }

            try
            {
                await using var noi = File.OpenRead(duong);
                var kq = await tailieu.NapAsync(f.Name, LoaiNoiDung(f.Extension), nguon.SubTenantCode!, nguon.NguoiNap!, noi, ct);
                if (kq.DaCoTuTruoc) daCo++; else moi++;
                dauVet.Ghi(tenant.TenantId, f);
            }
            catch (IOException e)
            {
                // File đang được ghi dở (Drive đang đồng bộ) — lượt sau đọc lại. KHÔNG ghi dấu vết.
                boQua.Add($"{Path.GetRelativePath(thuMuc, duong)}: chưa đọc được ({e.Message}) — thử lại lượt sau");
                logger.LogInformation("Quét {ThuMuc}: bỏ qua {File} — {Loi}", thuMuc, duong, e.Message);
            }
        }

        return new KetQuaQuet(thuMuc, moi, daCo, boQua);
    }

    private static string LoaiNoiDung(string duoi) => duoi.ToLowerInvariant() switch
    {
        ".txt" or ".log" => "text/plain",
        ".md" => "text/markdown",
        ".csv" => "text/csv",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".html" or ".htm" => "text/html",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream",
    };
}

public sealed record KetQuaQuet(string ThuMuc, int Moi, int DaCoTuTruoc, IReadOnlyList<string> BoQua);

/// <summary>
/// Vòng lặp nền — mỗi nguồn một scope, tenant đặt TƯỜNG MINH (khuôn <c>WebhookSenderService.MotVongAsync</c>): không có
/// middleware nào trong tiến trình nền, nên RLS chỉ còn hiệu lực nếu tenant được resolve đúng ở đây.
///
/// 🛑 Bản dedicated chỉ quét cho ĐÚNG khách của nó — <see cref="Startup.StartupChecks"/> đã từ chối nguồn khai khách khác
/// (bài học `I2-1`: máy của khách A tự đi làm việc thay khách B).
/// </summary>
public sealed class QuetThuMucService(IServiceProvider services, IOptions<KenhHaiOptions> options, ILogger<QuetThuMucService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Đọc cấu hình LÚC CHẠY, không lúc đăng ký DI (`IM-12`): không nguồn nào hoặc đã tắt thì không làm gì.
        if (!options.Value.BatVongQuet || options.Value.Nguon.Count == 0) return;

        var chuKy = TimeSpan.FromSeconds(Math.Max(10, options.Value.ChuKyGiay));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MotVongAsync(stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Vòng quét thư mục Kênh 2 lỗi; sẽ thử lại ở chu kỳ sau.");
            }

            try { await Task.Delay(chuKy, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    public async Task MotVongAsync(CancellationToken ct)
    {
        var dedicated = services.GetRequiredService<DedicatedTenant>().TenantId;
        var danhBa = services.GetRequiredService<TenantDirectory>();

        foreach (var nguon in options.Value.Nguon)
        {
            var tenantId = dedicated ?? await danhBa.FindIdByExternalKeyAsync(nguon.TenantKey!, ct);
            if (tenantId is null)
            {
                logger.LogWarning("Quét {ThuMuc}: không thấy khách \"{Khoa}\" — bỏ qua nguồn này.", nguon.ThuMuc, nguon.TenantKey);
                continue;
            }

            await using var scope = services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<RequestTenantContext>().Resolve(tenantId.Value);

            var kq = await scope.ServiceProvider.GetRequiredService<QuetThuMuc>().QuetAsync(nguon, ct);
            if (kq.Moi > 0 || kq.BoQua.Count > 0)
            {
                logger.LogInformation("Quét {ThuMuc}: {Moi} mới · {DaCo} đã có · {BoQua} bỏ qua", kq.ThuMuc, kq.Moi, kq.DaCoTuTruoc, kq.BoQua.Count);
            }
        }
    }
}
