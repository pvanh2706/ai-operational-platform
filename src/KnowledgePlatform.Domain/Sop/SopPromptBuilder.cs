using System.Text;
using KnowledgePlatform.Domain.Redaction;

namespace KnowledgePlatform.Domain.Sop;

/// <summary>Một mẩu evidence đem đi soạn nháp.</summary>
public sealed record EvidenceForDrafting(string SourceReference, string Content, DateTimeOffset ObservedAt);

/// <summary>Một case đem đi soạn nháp, kèm mọi mẩu evidence của nó.</summary>
public sealed record CaseForDrafting(
    string SourceReference,
    string Subject,
    IReadOnlyList<EvidenceForDrafting> Evidence);

/// <summary>
/// Đầu vào của một lượt soạn nháp: MỘT nhóm nguyên nhân, kèm toàn bộ case + evidence.
///
/// ⚠ Vì sao đầu vào là "một nhóm" chứ không phải "kết quả truy vấn": `docs/07` §4 sửa thứ
/// tự lần hai (2026-09-08) — `taxonomy-19-nhom-hoa-don.json` đã map sẵn nhóm → mã case,
/// nên soạn nháp chạy được NGAY mà không cần retrieval. Retrieval vẫn cần, nhưng cho case
/// MỚI ĐẾN. Chỗ nối là <see cref="SopDraftRequest.Cases"/>: sau này nó nhận đầu ra của
/// truy vấn thay vì nhận danh sách từ taxonomy, không phải sửa gì ở đây.
/// </summary>
public sealed record SopDraftRequest
{
    public required string GroupName { get; init; }

    /// <summary>Mô tả nhóm từ taxonomy — cho model biết ranh giới nhóm, KHÔNG phải đáp án.</summary>
    public string? GroupDescription { get; init; }

    public required IReadOnlyList<CaseForDrafting> Cases { get; init; }

    /// <summary>Mã ticket được phép xuất hiện trong bản nháp. Suy từ <see cref="Cases"/>.</summary>
    public IReadOnlySet<string> AllowedCaseKeys =>
        Cases.Select(c => c.SourceReference).ToHashSet(StringComparer.Ordinal);
}

/// <summary>
/// Kết quả dựng payload, kèm SỐ CHỖ ĐÃ CHE. `AR-o` đòi mỗi lần gọi phải ghi lại con số này.
/// </summary>
public sealed record SopPrompt(string SystemText, string UserText, int RedactedCount)
{
    public bool AnythingRedacted => RedactedCount > 0;
}

/// <summary>
/// Dựng payload cho một lượt soạn nháp — VÀ chạy cổng che trước khi trả về.
///
/// ⚠ ĐÂY LÀ CHỖ `AR-o` ĐƯỢC THỰC THI, và nó cố ý nằm ở tầng Domain chứ không ở tầng gọi
/// HTTP. Lý do: một cổng che nằm cạnh chỗ gọi mạng thì chỉ cần một đường gọi mới là đi
/// vòng qua được nó, im lặng. Nằm ở đây thì **không có cách nào dựng payload mà không qua
/// cổng** — và điều đó kiểm được bằng test không cần mạng (`IM-18`).
///
/// Ba điều lớp này KHÔNG làm, cố ý:
/// <list type="bullet">
///   <item>không gọi mạng — nó chỉ trả về chữ</item>
///   <item>không cắt bớt evidence. Cắt là quyết định về Ý NGHĨA, và `AR-m` (nội dung đã bị
///     người gửi rút lại) chưa chốt: cắt transcript thành nhiều mẩu thì đoạn đã bị rút sẽ
///     vào kho không mang dấu hiệu gì. Cắt trước khi chốt `AR-m` là làm sai theo cách không
///     ghép lại được.</item>
///   <item>không tự nghĩ ra mã ticket. Danh sách hợp lệ đi kèm request và eval kiểm lại.</item>
/// </list>
/// </summary>
public static class SopPromptBuilder
{
    /// <summary>
    /// Chỉ dẫn cho model. Mọi câu trong đây đến từ một chỗ đã VẤP hoặc đã ĐO khi dựng tay
    /// hai bản nháp đầu tiên — xem `docs/11`. Đừng thêm câu vì nghe hay.
    /// </summary>
    public const string SystemText = """
        Bạn đang soạn một BẢN NHÁP quy trình xử lý (SOP) từ các ticket support thật, để một
        người làm support đọc và SỬA. Bản nháp của bạn là bản A; bản họ sửa là bản B; hệ
        thống đo khoảng cách giữa hai bản. Nên một bản nháp TRUNG THỰC có giá trị hơn một
        bản nghe chắc chắn.

        SÁU LUẬT, theo thứ tự quan trọng:

        1. KHÔNG BAO GIỜ bịa mã ticket. Chỉ dùng mã có trong dữ liệu được đưa. Một nhánh
           không có ticket nào chống lưng thì để danh sách ticket RỖNG — rỗng là hợp lệ và
           là thông tin, không phải lỗi cần che.

        2. Mỗi nhánh phải khai `chungCu`:
             "evidence-noi-ro"  ticket GHI RÕ bước kiểm đó (có câu hỏi, có chỗ xem, có
                                giá trị quan sát được)
             "toi-suy-ra"       bạn chỉ suy từ KẾT LUẬN của nhân viên
           Phần lớn ticket ghi nguyên nhân mà KHÔNG ghi cách biết. Nếu bạn khai
           "evidence-noi-ro" cho một nhánh mà ticket chỉ ghi kết luận, đó là lỗi nặng nhất
           bạn có thể mắc ở đây — nó làm người duyệt tin vào một chỗ không có gì.

        3. Một BƯỚC KIỂM là: một câu hỏi + mở màn hình NÀO + các giá trị quan sát được +
           mỗi giá trị đi tới đâu. "Kiểm tra phân quyền" không phải bước kiểm, đó là một
           cái nhãn. "Tài khoản nào đang phát hành, và tài khoản đó có trong danh sách
           người dùng của trang quản trị không" là bước kiểm.

        4. Nếu một bước kiểm cần QUYỀN mà người xử lý có thể không có, khai `quyenCan`.
           Đã gặp thật: bước kiểm đầu tiên của một nhóm cần quyền vào mục Nhật ký, và
           khách trả lời "đâu phải ai cũng có quyền vào mục cấu hình này".

        5. Điều kiện đóng phải QUAN SÁT ĐƯỢC. "Hết báo lỗi" không dùng được: có nhóm mà
           4/10 ticket ngay từ đầu đã không có báo lỗi nào.

        6. Nếu các nguyên nhân trong nhóm phân biệt nhau bằng MÃ LỖI chứ bằng một chuỗi
           câu hỏi lồng nhau, hãy dùng `bangTraMaLoi` thay vì nhồi mọi thứ vào nhánh. Khoá
           một dòng bảng là CẶP (nhà cung cấp, mã lỗi) — hai nhà cung cấp trả cùng một mã
           vẫn có thể trỏ tới hai trường khác nhau.

        Chỗ nào bạn không biết thì viết vào `khoangTrongPhaiBiet`. Nói ra chỗ mình không
        biết là một phần của việc, không phải một lời xin lỗi.

        Một số mẩu có đoạn `[ĐÃ CHE]` — đó là thông tin đăng nhập đã được che trước khi
        gửi. Cứ coi như "ở đây từng có credential" và đi tiếp; đừng cố suy ra giá trị.
        """;

    /// <summary>
    /// Dựng payload. Ném <see cref="EgressBlockedException"/> nếu cổng che không làm sạch
    /// được — `AR-o` fail closed.
    /// </summary>
    public static SopPrompt Build(SopDraftRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Cases.Count == 0)
        {
            throw new ArgumentException("Không có case nào để soạn nháp.", nameof(request));
        }

        var sb = new StringBuilder();

        sb.Append("NHÓM NGUYÊN NHÂN: ").AppendLine(request.GroupName);
        if (!string.IsNullOrWhiteSpace(request.GroupDescription))
        {
            sb.Append("Mô tả nhóm: ").AppendLine(request.GroupDescription);
        }

        sb.Append("Số ticket: ").Append(request.Cases.Count).AppendLine();
        sb.AppendLine();
        sb.AppendLine("Mã ticket được phép dùng — KHÔNG dùng mã nào khác:");
        foreach (var key in request.Cases.Select(c => c.SourceReference))
        {
            sb.Append("  ").AppendLine(key);
        }

        foreach (var c in request.Cases)
        {
            sb.AppendLine();
            sb.AppendLine("────────────────────────────────────────");
            sb.Append("TICKET ").Append(c.SourceReference).Append(" — ").AppendLine(c.Subject);

            foreach (var e in c.Evidence.OrderBy(e => e.ObservedAt))
            {
                sb.Append("  --- ").Append(e.SourceReference)
                    .Append(" (").Append(e.ObservedAt.ToString("O")).AppendLine(")");
                sb.AppendLine(e.Content);
            }
        }

        // 🛑 CHE TRÊN PAYLOAD ĐÃ GHÉP, KHÔNG CHE TỪNG MẨU RỒI GHÉP.
        //
        // Bản đầu của hàm này che từng mẩu evidence rồi mới nối lại, và test
        // `Ghep_nhieu_mau_khong_tao_ra_lo_moi` bắt được ngay: hình dạng 3 nhìn TỚI TRƯỚC
        // 6 dòng, nên một từ khoá ở cuối mẩu này và một dãy số trần ở đầu mẩu sau — tách
        // riêng thì cả hai vô hại — ghép lại thành đúng một cặp đăng nhập. Che từng mẩu
        // đếm 0 chỗ, và cổng chỉ chặn được vì có bước quét lại toàn payload.
        //
        // Đó cũng đúng chữ mà `AR-o` đã dùng: che trên **PAYLOAD** trước mỗi lần gọi.
        // Ghi lại vì nó là một mẫu sẽ tái diễn: một luật đúng trên từng phần KHÔNG tự
        // động đúng trên phần ghép lại.
        //
        // ⚠ Và phần khung do chính hàm này sinh ra (dòng `TICKET ES-…`, dòng `--- …#comment`)
        // an toàn trước cả bốn hình dạng: chúng không phải dòng chỉ-có-số (hình dạng 3),
        // không mang nhãn mật khẩu (1), không mang tên công cụ remote (2), và không có key
        // JSON (4). Nếu ai đổi khung thì phải kiểm lại đúng bốn điều đó — đổi khung thành
        // một dòng chỉ có số là tự che mất mã ticket của mình.
        var daChe = EgressRedactor.Redact(sb.ToString());
        EgressRedactor.EnsureSafeToSend(daChe.Text);

        return new SopPrompt(SystemText, daChe.Text, daChe.RedactedCount);
    }
}
