using System.Text.Json;
using KnowledgePlatform.Api.Knowledge;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Infrastructure.Sop;

namespace KnowledgePlatform.Api.Startup;

/// <summary>
/// `--xuat-soan-nhap` — nửa XUẤT của vòng không-credit cho **soạn nháp SOP**.
///
/// <code>
///   -- --xuat-soan-nhap &lt;mã-khách&gt; "&lt;tên nhóm&gt;" &lt;file-ra&gt;
///   ...dán file cho một ngữ cảnh AI sạch, lưu JSON nó trả về...
///   POST /knowledge/sop-draft  với chính JSON đó
/// </code>
///
/// 🎯 **Khác `SoanNhapRunner --xuat-payload` ở đúng một chỗ, và đó là chỗ quan trọng:** công
/// cụ rời kia đọc corpus TRÊN ĐĨA và không biết gì về tenant. Lệnh này đọc **database của
/// chính khách hàng**, và lấy danh sách case từ **kho định tuyến** (`IM-45`) chứ không từ file
/// taxonomy. Hai vòng không-credit nối vào nhau: định tuyến xong thì soạn nháp có vật liệu.
///
/// ⚠ Nửa NẠP KHÔNG cần lệnh mới — `POST /knowledge/sop-draft` đã có từ `IM-31`, và nó đi qua
/// đúng bộ kiểm mà đường gọi model sẽ đi qua. Thêm một đường nạp thứ hai là thêm một đường
/// để quên.
/// </summary>
public static class LenhSoanNhap
{
    public static async Task XuatAsync(string[] args, IServiceProvider services, CancellationToken ct)
    {
        if (args.Length < 4)
        {
            Console.Error.WriteLine("Dùng: --xuat-soan-nhap <mã-khách> \"<tên nhóm>\" <file-ra>");
            Environment.ExitCode = 2;
            return;
        }

        var nhom = args[2];
        var duong = Path.GetFullPath(args[3]);
        if (!LenhDinhTuyen.KiemChoGhi(duong)) { Environment.ExitCode = 7; return; }

        await LenhDinhTuyen.TrongScopeAsync(args[1], services, ct, async sp =>
        {
            var yeuCau = await sp.GetRequiredService<SoanNhapHandler>().GomAsync(nhom, ct);

            if (yeuCau is null)
            {
                Console.Error.WriteLine($"Chưa có case nào được định tuyến vào nhóm \"{nhom}\".");
                Console.Error.WriteLine("Nạp định tuyến trước: --xuat-dinh-tuyen / --nap-dinh-tuyen.");
                Environment.ExitCode = 3;
                return;
            }

            // 🛑 Cổng che AR-o chạy Ở ĐÂY, trước khi bất cứ byte nào ra file. Nó ném thì
            //    KHÔNG GHI GÌ CẢ — fail closed, không phải "che được bao nhiêu thì ghi bấy nhiêu".
            SopPrompt prompt;
            try
            {
                prompt = SopPromptBuilder.Build(yeuCau);
            }
            catch (Domain.Redaction.EgressBlockedException e)
            {
                Console.Error.WriteLine("🛑 KHÔNG GHI GÌ CẢ — cổng che chặn (AR-o):");
                Console.Error.WriteLine("   " + e.Message);
                Environment.ExitCode = 3;
                return;
            }

            var goi = new
            {
                _canhBao = "Dữ liệu khách hàng. ĐÃ qua cổng che AR-o. Không commit, không dán nơi công khai.",
                nhom,
                soCase = yeuCau.Cases.Count,
                soMauEvidence = yeuCau.Cases.Sum(c => c.Evidence.Count),
                daChe = prompt.RedactedCount,
                system = prompt.SystemText,
                user = prompt.UserText,
                schemaDauRa = JsonSerializer.Deserialize<JsonElement>(SopDraftSchema.Json),
            };

            await File.WriteAllTextAsync(duong, JsonSerializer.Serialize(goi, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }), ct);

            Console.WriteLine($"Đã xuất   : {duong}");
            Console.WriteLine($"Nhóm      : {nhom}");
            Console.WriteLine($"Case      : {yeuCau.Cases.Count} · mẩu evidence: {yeuCau.Cases.Sum(c => c.Evidence.Count)}");
            Console.WriteLine($"Đã che    : {prompt.RedactedCount} chỗ nghi là bí mật (AR-o)");
            Console.WriteLine();
            Console.WriteLine("Bước tiếp:");
            Console.WriteLine("  1. Dán `system` + `user` cho một ngữ cảnh AI sạch, đòi đúng `schemaDauRa`.");
            Console.WriteLine("  2. POST khối JSON nó trả về tới /knowledge/sop-draft");
            Console.WriteLine();
            Console.WriteLine("⚠ Bản nháp vào kho ở trạng thái DRAFT. Nạp KHÔNG phải duyệt — nó không ra");
            Console.WriteLine("  khỏi /hoi cho tới khi có NGƯỜI duyệt (D4).");
        });
    }
}
