using System.Text.Json;
using Anthropic;
using KnowledgePlatform.Domain.Redaction;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Sop;

// Soạn một bản nháp SOP cho MỘT nhóm nguyên nhân, bằng model, rồi ghi ra file JSON để
// đưa vào bộ eval đã có: `python scripts/jira-export/nhom_sop.py --kiem-cay <file>`.
//
// ⚠ MỖI LẦN CHẠY LÀ TỐN TIỀN THẬT. Một nhóm 10 case đo được ~$0,16 (claude-opus-5).
// ⚠ MỖI LẦN CHẠY LÀ DỮ LIỆU KHÁCH ĐI RA NGOÀI (`AR-o`) — cổng che chạy trước, và số chỗ
//   đã che được in ra. Con số đó phải được đọc, không phải để cho đẹp.
//
//   dotnet run --project tools/SoanNhapRunner -- "Phân quyền" [--ra <file.json>]
//
// Đầu vào: docs/ket-qua-phan-tich/taxonomy-19-nhom-hoa-don.json  (nhóm -> mã case)
//          scripts/jira-export/fixture-*.json                    (case + evidence)
// Corpus KHÔNG theo git; dựng lại theo docs/10 §3.

var mauTen = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
if (string.IsNullOrWhiteSpace(mauTen))
{
    Console.Error.WriteLine("Thiếu tên nhóm. Ví dụ:");
    Console.Error.WriteLine("  dotnet run --project tools/SoanNhapRunner -- \"Phân quyền\"");
    Console.Error.WriteLine("Xem danh sách nhóm: python scripts/jira-export/nhom_sop.py --liet-ke");
    return 2;
}

var iRa = Array.IndexOf(args, "--ra");
var duongRa = iRa >= 0 && iRa + 1 < args.Length ? args[iRa + 1] : null;

var goc = TimGocRepo();
var pTaxonomy = Path.Combine(goc, "docs", "ket-qua-phan-tich", "taxonomy-19-nhom-hoa-don.json");
var pCases = Path.Combine(goc, "scripts", "jira-export", "fixture-cases.json");
var pEvidence = Path.Combine(goc, "scripts", "jira-export", "fixture-evidence.json");

foreach (var p in new[] { pTaxonomy, pCases, pEvidence })
{
    if (!File.Exists(p))
    {
        Console.Error.WriteLine($"Thiếu {p}");
        Console.Error.WriteLine("Corpus KHÔNG theo git (dữ liệu khách hàng). Dựng lại theo docs/10 §3.");
        return 1;
    }
}

// ── Đọc nhóm ──────────────────────────────────────────────────────────────────
using var taxDoc = JsonDocument.Parse(File.ReadAllText(pTaxonomy));
var nhomKhop = taxDoc.RootElement.GetProperty("nhom").EnumerateArray()
    .Where(g => g.GetProperty("ten").GetString()!.Contains(mauTen, StringComparison.OrdinalIgnoreCase))
    .ToList();

if (nhomKhop.Count != 1)
{
    Console.Error.WriteLine($"Khớp {nhomKhop.Count} nhóm với \"{mauTen}\". Cần đúng một.");
    foreach (var g in nhomKhop)
    {
        Console.Error.WriteLine("  · " + g.GetProperty("ten").GetString());
    }

    return 2;
}

var nhom = nhomKhop[0];
var tenNhom = nhom.GetProperty("ten").GetString()!;
var moTaNhom = nhom.TryGetProperty("moTa", out var mt) ? mt.GetString() : null;
var maCase = nhom.GetProperty("caseKeys").EnumerateArray().Select(x => x.GetString()!).ToList();

// ── Đọc case + evidence ───────────────────────────────────────────────────────
using var caseDoc = JsonDocument.Parse(File.ReadAllText(pCases));
using var evDoc = JsonDocument.Parse(File.ReadAllText(pEvidence));

string Khoa(string tk) => tk.Replace("jira:", "").Split('#')[0];

var tieuDe = caseDoc.RootElement.EnumerateArray()
    .ToDictionary(c => Khoa(c.GetProperty("sourceReference").GetString()!),
                  c => c.GetProperty("subject").GetString() ?? "");

var evTheoCase = evDoc.RootElement.EnumerateArray()
    .GroupBy(e => Khoa(e.GetProperty("caseSourceReference").GetString()!))
    .ToDictionary(g => g.Key, g => g.Select(e => new EvidenceForDrafting(
        e.GetProperty("sourceReference").GetString()!,
        e.GetProperty("content").GetString() ?? "",
        e.GetProperty("observedAt").GetDateTimeOffset())).ToList());

var cases = maCase
    .Where(k => tieuDe.ContainsKey(k))
    .Select(k => new CaseForDrafting(k, tieuDe[k],
        evTheoCase.TryGetValue(k, out var ds) ? ds : []))
    .ToList();

if (cases.Count != maCase.Count)
{
    Console.Error.WriteLine(
        $"⚠ Nhóm có {maCase.Count} mã case nhưng corpus trên đĩa chỉ có {cases.Count}. " +
        "Bản nháp sẽ KHÔNG qua được --kiem-cay (phép cộng lệch). Dựng lại corpus trước.");
    return 1;
}

var request = new SopDraftRequest
{
    GroupName = tenNhom,
    GroupDescription = moTaNhom,
    Cases = cases,
};

Console.WriteLine($"Nhóm      : {tenNhom}");
Console.WriteLine($"Ticket    : {cases.Count}  ·  mẩu evidence: {cases.Sum(c => c.Evidence.Count)}");
Console.WriteLine($"Ký tự vào : {cases.Sum(c => c.Subject.Length + c.Evidence.Sum(e => e.Content.Length)):N0}");

// ── Cổng che chạy TRƯỚC, và nói ra số chỗ đã che ───────────────────────────────
SopPrompt prompt;
try
{
    prompt = SopPromptBuilder.Build(request);
}
catch (EgressBlockedException ex)
{
    Console.Error.WriteLine("🛑 KHÔNG GỬI GÌ CẢ — cổng che chặn (AR-o):");
    Console.Error.WriteLine("   " + ex.Message);
    return 3;
}

Console.WriteLine($"Đã che    : {prompt.RedactedCount} chỗ nghi là bí mật trước khi gửi (AR-o)");
Console.WriteLine();

if (Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") is null
    && Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN") is null)
{
    Console.Error.WriteLine("Chưa có ANTHROPIC_API_KEY (hoặc profile của `ant auth login`).");
    Console.Error.WriteLine("Payload đã dựng và đã che xong, nhưng không gọi được.");
    return 4;
}

// ── Gọi thật ──────────────────────────────────────────────────────────────────
Console.WriteLine("Đang gọi claude-opus-5… (một lượt, không dùng Batches — xem AnthropicSopDrafter)");
var batDau = DateTimeOffset.UtcNow;

var drafter = new AnthropicSopDrafter(new AnthropicClient());
SopDraftResult ketQua;
try
{
    ketQua = await drafter.DraftAsync(request);
}
catch (Exception ex)
{
    Console.Error.WriteLine("Gọi thất bại: " + ex.GetType().Name);
    Console.Error.WriteLine("  " + ex.Message);
    return 5;
}

var giay = (DateTimeOffset.UtcNow - batDau).TotalSeconds;

Console.WriteLine();
Console.WriteLine($"Xong sau  : {giay:F1} giây");
Console.WriteLine($"Token     : {ketQua.InputTokens:N0} vào · {ketQua.OutputTokens:N0} ra");
Console.WriteLine($"Giá       : ${ketQua.EstimatedUsd:F4}  (Batches API sẽ còn một nửa)");
Console.WriteLine($"Bản nháp  : {ketQua.Draft.BuocKiem.Count} bước kiểm · "
                  + $"{ketQua.Draft.BuocSua.Count} bước sửa · "
                  + $"{ketQua.Draft.BangTraMaLoi?.Dong.Count ?? 0} dòng bảng tra");

var soNhanh = ketQua.Draft.BuocKiem.Sum(b => b.Nhanh.Count);
var coNguon = ketQua.Draft.BuocKiem.Sum(b => b.Nhanh.Count(n => n.ChungCu == "evidence-noi-ro"));
Console.WriteLine($"Nhánh     : {coNguon}/{soNhanh} có nguồn (còn lại model tự khai là suy ra)");

// ── Ghi ra để đưa vào bộ eval ─────────────────────────────────────────────────
duongRa ??= Path.Combine(goc, "docs", "ket-qua-phan-tich",
    "may-sinh-" + string.Concat(tenNhom.ToLowerInvariant()
        .Where(c => char.IsAsciiLetterOrDigit(c) || c == ' '))
        .Replace(' ', '-') + ".json");

File.WriteAllText(duongRa, JsonSerializer.Serialize(ketQua.Draft, new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
}));

Console.WriteLine();
Console.WriteLine("Đã ghi    : " + duongRa);
Console.WriteLine("Kiểm ngay : python scripts/jira-export/nhom_sop.py --kiem-cay \"" + duongRa + "\"");
return 0;

static string TimGocRepo()
{
    var d = new DirectoryInfo(AppContext.BaseDirectory);
    while (d is not null && !Directory.Exists(Path.Combine(d.FullName, ".git")))
    {
        d = d.Parent;
    }

    return d?.FullName ?? Directory.GetCurrentDirectory();
}
