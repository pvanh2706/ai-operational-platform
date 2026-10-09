using System.Text.Json;
using System.Text.Json.Serialization;
using KnowledgePlatform.Domain.Redaction;
using KnowledgePlatform.Domain.Sop;
using KnowledgePlatform.Domain.Tenancy;
using KnowledgePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgePlatform.Api.Knowledge;

/// <summary>Một ticket cần định tuyến, ở dạng SẼ được đưa cho model đọc.</summary>
public sealed record TicketCanDinhTuyen(
    [property: JsonPropertyName("khoaNguon")] string KhoaNguon,
    [property: JsonPropertyName("tieuDe")] string TieuDe,
    [property: JsonPropertyName("moTa")] string? MoTa);

/// <summary>Một dòng kết quả do người vận hành dán về.</summary>
public sealed record KetQuaDinhTuyenNap(
    [property: JsonPropertyName("khoaNguon")] string? KhoaNguon,
    [property: JsonPropertyName("nhom")] string? Nhom,
    [property: JsonPropertyName("nguon")] string? Nguon,
    [property: JsonPropertyName("giaiThich")] string? GiaiThich);

/// <param name="DaNap">Số dòng ghi vào kho.</param>
/// <param name="DaDe">Số dòng ĐÈ lên kết quả nạp trước đó.</param>
/// <param name="BoQua">Dòng bị từ chối, kèm lý do.</param>
public sealed record KetQuaNap(int DaNap, int DaDe, IReadOnlyList<string> BoQua);

/// <summary>
/// **VÒNG KHÔNG-CREDIT cho định tuyến**: xuất ticket ra → một ngữ cảnh AI khác đọc → nạp kết
/// quả về.
///
/// Đúng đường mà `IM-29` đã đi cho bản nháp SOP ngày 2026-09-10, giờ áp cho định tuyến. Nó
/// tồn tại vì tài khoản API chưa có credit, và câu hỏi ĐÁNG nhất vẫn trả lời được mà không
/// cần credit: *đưa đúng dữ liệu này cho một model thì nó phân nhóm được không?*
///
/// 🛑 **Ba cái chốt chép nguyên từ đường xuất payload của `SoanNhapRunner`** — không phải để
/// nhất quán cho đẹp, mà vì mỗi cái đã có một sự cố đứng sau:
/// <list type="number">
///   <item>Nội dung đi qua cổng che `AR-o` TRƯỚC khi ra file. Không nhánh nào xuất được dữ
///     liệu chưa che — repo đã rò một lần theo đúng hình dạng đó (một file dump mà
///     `.gitignore` không chặn, commit `888c8e7`).</item>
///   <item>File xuất ra là dữ liệu khách THẬT (đã che bí mật, nhưng nội dung ticket là thật).
///     Chỗ ghi phải do người gõ lệnh chỉ định, và nếu nằm trong repo thì phải được git chặn.</item>
///   <item>Đường NẠP từ chối tên nhóm không có trong taxonomy. `cham_routing.py` gọi đó là
///     bẫy thứ ba, cùng loại lỗi mà `G6`/`AP3` chặn ở bản nháp SOP.</item>
/// </list>
/// </summary>
public sealed class VongKhongCredit(AppDbContext db, ITenantContext tenant, Startup.CongChe congChe)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Những case CHƯA có kết quả định tuyến nào trong kho.
    ///
    /// ⚠ Đầu vào là <b>tiêu đề + mẩu SỚM NHẤT</b>, đúng định nghĩa của `docs/13` §1: thứ có
    /// mặt LÚC TICKET ĐẾN. Không comment, không transcript — `thu_retrieval.py` đã đo được
    /// rằng đưa cả transcript vào còn TỆ HƠN, vì transcript chứa sẵn câu chẩn đoán. Đưa nó
    /// vào đây là đưa đáp án vào đề.
    /// </summary>
    public async Task<IReadOnlyList<TicketCanDinhTuyen>> ChonTicketAsync(int toiDa, CancellationToken ct = default)
    {
        var daCo = await db.Set<DinhTuyenDaNap>().Select(x => x.KhoaNguon).ToListAsync(ct);

        var cases = await db.Cases
            .Where(c => !daCo.Contains(c.SourceReference))
            .OrderBy(c => c.IngestedAt)
            .Take(toiDa)
            .Select(c => new { c.Id, c.SourceReference, c.Subject })
            .ToListAsync(ct);

        var ids = cases.Select(c => c.Id).ToList();

        // Mẩu SỚM NHẤT của mỗi case. Mẩu người gửi đã rút lại thì bỏ (`AR-m`).
        var moTa = await db.EvidenceItems
            .Where(e => e.ObservedInCaseId != null && ids.Contains(e.ObservedInCaseId!.Value))
            .Where(e => e.RetractedAt == null)
            .OrderBy(e => e.ObservedAt)
            .Select(e => new { CaseId = e.ObservedInCaseId!.Value, e.Content, e.ObservedAt })
            .ToListAsync(ct);

        var dauTien = moTa
            .GroupBy(x => x.CaseId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.ObservedAt).First().Content);

        return
        [
            .. cases.Select(c => new TicketCanDinhTuyen(
                c.SourceReference,
                Che(c.Subject),
                dauTien.TryGetValue(c.Id, out var m) ? Che(m) : null))
        ];
    }

    /// <summary>Dựng gói để dán cho một ngữ cảnh AI đọc.</summary>
    /// <param name="daQuaCongChe">
    /// Nội dung trong <paramref name="ticket"/> có đi qua cổng che không — để câu cảnh báo in vào
    /// file nói ĐÚNG sự thật. Cờ `N1-2` bật thì file chứa dữ liệu nguyên văn, và câu "ĐÃ qua cổng
    /// che" cũ sẽ là một lời nói dối nằm ngay đầu file.
    /// </param>
    public static string DungGoi(
        IReadOnlyList<TicketCanDinhTuyen> ticket, JsonElement taxonomy, bool daQuaCongChe = true) =>
        JsonSerializer.Serialize(new
        {
            _canhBao = daQuaCongChe
                ? "Dữ liệu khách hàng. ĐÃ qua cổng che AR-o. Không commit, không dán ra nơi công khai."
                : "Dữ liệu khách hàng CHƯA CHE — cờ N1-2 đang bật. Có thể chứa mật khẩu/token thật. " +
                  "Không commit, không dán ra nơi công khai.",
            daQuaCongChe,
            huongDan = HuongDan,
            soTicket = ticket.Count,
            taxonomy,
            ticket,
            schemaDauRa = new
            {
                ketQua = new[]
                {
                    new
                    {
                        khoaNguon = "<chép nguyên từ ticket[].khoaNguon>",
                        nhom = "<tên nhóm CHÉP NGUYÊN từ taxonomy, hoặc KHONG-BIET>",
                        nguon = "noi-ro-trong-mo-ta | suy-tu-trieu-chung | khong-du-thong-tin",
                        giaiThich = "<một câu, để người đọc phản bác được>",
                    },
                },
            },
        }, Json);

    /// <summary>
    /// Nạp kết quả về kho của khách hàng hiện tại.
    ///
    /// 🛑 Từ chối mọi dòng có tên nhóm KHÔNG nằm trong taxonomy. Bịa tên nhóm là cùng loại lỗi
    /// mà `G6`/`AP3` chặn ở bản nháp SOP: một hướng dẫn gắn nhãn sai nhóm sẽ được `/hoi` trả
    /// về cho người làm thật, và không có gì báo.
    /// </summary>
    public async Task<KetQuaNap> NapAsync(
        IReadOnlyList<KetQuaDinhTuyenNap> ketQua,
        IReadOnlySet<string> tenNhomHopLe,
        string nguoiNap,
        CancellationToken ct = default)
    {
        var boQua = new List<string>();
        int daNap = 0, daDe = 0;

        // Chỉ nhận khoá nguồn CÓ THẬT trong kho của khách này. Nạp cho một case không tồn tại
        // là tạo ra một dòng không bao giờ được tra tới — rác im lặng.
        var coThat = await db.Cases.Select(c => c.SourceReference).ToListAsync(ct);
        var coThatSet = coThat.ToHashSet(StringComparer.Ordinal);

        var dangCo = await db.Set<DinhTuyenDaNap>().ToDictionaryAsync(x => x.KhoaNguon, ct);

        foreach (var (dong, i) in ketQua.Select((d, i) => (d, i)))
        {
            if (string.IsNullOrWhiteSpace(dong.KhoaNguon))
            {
                boQua.Add($"[{i}] thiếu khoaNguon");
                continue;
            }

            if (string.IsNullOrWhiteSpace(dong.Nhom))
            {
                boQua.Add($"[{i}] {dong.KhoaNguon}: thiếu nhom");
                continue;
            }

            var la = dong.Nhom.Trim();

            if (!string.Equals(la, KetQuaDinhTuyen.KhongBiet, StringComparison.Ordinal)
                && !tenNhomHopLe.Contains(la))
            {
                // Không sửa hộ, không tìm nhóm gần nhất. Một tên bịa ra mà được "sửa cho
                // giống" là cách biến một lỗi nhìn thấy được thành một lỗi im lặng.
                boQua.Add($"[{i}] {dong.KhoaNguon}: nhóm \"{la}\" KHÔNG có trong taxonomy");
                continue;
            }

            if (!coThatSet.Contains(dong.KhoaNguon))
            {
                boQua.Add($"[{i}] {dong.KhoaNguon}: không có case nào mang khoá nguồn này");
                continue;
            }

            if (dangCo.TryGetValue(dong.KhoaNguon, out var cu))
            {
                cu.Nhom = la;
                cu.Nguon = dong.Nguon?.Trim() ?? "nguoi-nap";
                cu.GiaiThich = dong.GiaiThich;
                daDe++;
                continue;
            }

            db.Set<DinhTuyenDaNap>().Add(new DinhTuyenDaNap
            {
                TenantId = tenant.TenantId,
                KhoaNguon = dong.KhoaNguon,
                Nhom = la,
                Nguon = dong.Nguon?.Trim() ?? "nguoi-nap",
                GiaiThich = dong.GiaiThich,
                NguoiNap = nguoiNap,
            });
            daNap++;
        }

        if (daNap > 0 || daDe > 0) await db.SaveChangesAsync(ct);

        return new KetQuaNap(daNap, daDe, boQua);
    }

    private string Che(string? s) => congChe.CheDuongRa(s).Text;

    private const string HuongDan = """
        Bạn nhận một danh sách ticket hỗ trợ và một TAXONOMY gồm các nhóm nguyên nhân.
        Việc của bạn: mỗi ticket thuộc nhóm nào.

        LUẬT:
        1. `nhom` phải CHÉP NGUYÊN một giá trị `ten` trong taxonomy. Không viết tắt, không
           diễn đạt lại, không tự đặt nhóm mới. Sai một chữ là dòng đó bị từ chối.
        2. Không đủ căn cứ thì trả đúng chuỗi "KHONG-BIET". Đó là một câu trả lời ĐÚNG và
           được mong đợi — trên phép đo trước, 62/150 ticket rơi vào đây.
           ⚠ Đoán bừa nguy hiểm hơn nói không biết: hướng dẫn sai sẽ được đưa cho người
           đang xử lý sự cố thật.
        3. `giaiThich` viết một câu, nêu CHỖ TRONG TICKET làm bạn kết luận như vậy — để
           người đọc phản bác được. Đừng viết câu chung chung.
        4. ĐỪNG dùng mã ticket (`khoaNguon`) làm căn cứ. Nó chỉ để ghép kết quả về.
        5. Chỉ đọc `tieuDe` và `moTa`. Đó là tất cả những gì tồn tại lúc ticket vừa đến.

        TRẢ VỀ: đúng một khối JSON theo `schemaDauRa`, mỗi ticket một dòng, không thêm chữ
        nào ngoài JSON.
        """;
}
