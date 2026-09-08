namespace KnowledgePlatform.Domain.Redaction;

/// <summary>
/// Kết quả che một đoạn văn bản trước khi nó rời khỏi máy.
/// </summary>
/// <param name="Text">Văn bản đã che — thứ ĐƯỢC PHÉP gửi ra.</param>
/// <param name="RedactedCount">Số chỗ đã che. Phải ghi lại mỗi lần gọi (`AR-o`).</param>
/// <param name="Shapes">Hình dạng nào bắt được bao nhiêu chỗ. Dùng để ĐO, không để log giá trị.</param>
public sealed record RedactionResult(string Text, int RedactedCount, IReadOnlyDictionary<string, int> Shapes)
{
    public bool AnythingRedacted => RedactedCount > 0;
}

/// <summary>
/// Ném khi văn bản KHÔNG an toàn để gửi ra ngoài. `AR-o`: fail closed.
/// </summary>
public sealed class EgressBlockedException(string message) : Exception(message);

/// <summary>
/// Cổng che ở ranh giới GỬI RA — mọi byte đi tới một bên thứ ba phải qua đây.
///
/// `AR-o` chốt 2026-09-08: chủ dữ liệu đồng ý cho nội dung ticket đi ra API của nhà cung
/// cấp model, VỚI ĐIỀU KIỆN luật che chạy trên payload trước MỖI lần gọi. Xem `docs/07` §5.
///
/// ⚠ ĐÂY LÀ RANH GIỚI THỨ HAI, KHÔNG PHẢI RANH GIỚI CỦA `AR-j`.
/// `AR-j` / `check_corpus.py` che lúc **nạp vào kho của mình** — dữ liệu vẫn ở trong nhà,
/// và ở đó "nói ra rồi để người quyết" là đủ. Ở đây dữ liệu **rời khỏi máy**: không có
/// bước người xem lại, và không có đường thu hồi. Nên cổng này KHÔNG hỏi, nó CHẶN.
///
/// Ba tính chất, mỗi tính chất vì một lý do đã ghi ở `AR-o`:
/// <list type="bullet">
///   <item><b>Fail closed</b> — che xong QUÉT LẠI; còn bắt được gì ngoài dấu che thì
///     <see cref="EgressBlockedException"/>, không gửi. Một cổng che mà tự tin là mình
///     đã che sạch thì chỉ tạo ra sự an tâm giả.</item>
///   <item><b>Giữ DẤU</b> (`G6`/`AP3`) — chỗ đã che thành <see cref="Marker"/>, không bị
///     xoá lặng lẽ. Người duyệt phải phân biệt được "đã che" với "không có gì".</item>
///   <item><b>Đếm được</b> — <see cref="RedactionResult.RedactedCount"/> để mỗi lần gọi
///     ghi lại được số chỗ đã che. Luật che là cận TRÊN đã đo (13/13 trên chính corpus nó
///     được sửa theo), nên nó phải chạy ra số, không chạy im lặng.</item>
/// </list>
///
/// ⚠ CHỖ CÒN CHƯA CHỐT, và bản này chọn một nhánh — xem `AR-o`: bắt được bí mật thì
/// <b>bỏ cả mẩu</b> hay <b>thay giá trị giữ hình dạng</b>? Bản này thay ĐÚNG GIÁ TRỊ và
/// giữ phần còn lại của mẩu, theo đúng lập luận đã ghi trong `AR-o`: bỏ cả mẩu là mất
/// bước kiểm, vì chính mẩu xin Ultraviewer là **biên lai** của một lần chẩn đoán qua
/// remote — thứ mà `docs/11` đo được là chỉ 4/10 case ghi lại. Đổi nhánh thì sửa đúng
/// <see cref="Redact"/>, không lan ra chỗ khác.
/// </summary>
public static class EgressRedactor
{
    /// <summary>
    /// Dấu thay cho giá trị đã che. Cố ý mang chữ để người đọc bản nháp hiểu ngay, và cố
    /// ý KHÔNG mang giá trị gốc hay độ dài của nó.
    /// </summary>
    public const string Marker = "[ĐÃ CHE]";

    /// <summary>
    /// Che mọi chỗ nghi là bí mật, rồi TỰ KIỂM LẠI. Ném <see cref="EgressBlockedException"/>
    /// nếu sau khi che vẫn còn bắt được gì ở ngoài dấu che.
    /// </summary>
    public static RedactionResult Redact(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return new RedactionResult(content ?? string.Empty, 0, new Dictionary<string, int>());
        }

        var hits = SecretShapeScanner.Scan(content);
        var shapes = hits
            .GroupBy(h => h.Shape)
            .ToDictionary(g => g.Key, g => g.Count());

        // Che từ CUỐI về ĐẦU: thay một đoạn làm lệch mọi vị trí sau nó.
        var text = content;
        foreach (var hit in hits.OrderByDescending(h => h.Index))
        {
            text = string.Concat(text.AsSpan(0, hit.Index), Marker, text.AsSpan(hit.End));
        }

        EnsureSafeToSend(text);
        return new RedactionResult(text, hits.Count, shapes);
    }

    /// <summary>
    /// Quét lại văn bản ĐÃ che. Ném nếu còn bắt được gì mà chỗ đó không phải một dấu che.
    ///
    /// ⚠ Vì sao phải loại trừ dấu che thay vì chọn một dấu "không thể khớp": luật hình dạng
    /// 4 (`"account": "&lt;giá trị&gt;"`) khớp mọi chuỗi từ 3 ký tự không phải dấu nháy /
    /// khoảng trắng / phẩy / ngoặc. Không có dấu che nào vừa đọc được vừa không khớp nó.
    /// Nên phép kiểm phải hỏi *"chỗ bắt được có nằm trong một dấu che không"*, chứ không
    /// hỏi *"còn bắt được gì không"*.
    /// </summary>
    public static void EnsureSafeToSend(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var conLai = SecretShapeScanner.Scan(text)
            .Where(h => !NamTrongDauChe(text, h))
            .ToList();

        if (conLai.Count == 0)
        {
            return;
        }

        // ⚠ KHÔNG đưa giá trị vào thông điệp lỗi. Thông điệp lỗi đi vào log, và log đi ra
        // khỏi máy dễ hơn payload. Chỉ nói HÌNH DẠNG và SỐ CHỖ.
        var mo_ta = string.Join(", ", conLai
            .GroupBy(h => h.Shape)
            .Select(g => $"{g.Key} × {g.Count()}"));

        throw new EgressBlockedException(
            $"KHÔNG GỬI: sau khi che vẫn còn {conLai.Count} chỗ nghi là bí mật ({mo_ta}). " +
            "AR-o: cổng che fail closed. Xem docs/07 §5 AR-o.");
    }

    private static bool NamTrongDauChe(string text, SecretHit hit)
    {
        var tim = 0;
        while (true)
        {
            var i = text.IndexOf(Marker, tim, StringComparison.Ordinal);
            if (i < 0)
            {
                return false;
            }

            if (hit.Index >= i && hit.End <= i + Marker.Length)
            {
                return true;
            }

            tim = i + 1;
        }
    }
}
