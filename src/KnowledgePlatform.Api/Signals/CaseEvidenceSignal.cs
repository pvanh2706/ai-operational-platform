using KnowledgePlatform.Domain.Evidence;

namespace KnowledgePlatform.Api.Signals;

/// <summary>
/// Một mẩu quan sát được ở hệ thống nguồn: comment Jira, ghi chú xử lý, một email,
/// một tin Zalo. `AR-f` (chốt 2026-08-30) mở đường này.
///
/// ⚠ **Vì sao nó là endpoint riêng chứ không lồng vào tín hiệu case.**
/// `K-B9` đã chốt: evidence được phép trỏ THẲNG vào Knowledge, không qua Case — vì
/// email của senior hay tin nhắn Zalo không thuộc case nào, và với thực tế 60%
/// fragment rải rác thì đó không phải trường hợp hiếm. Nếu cửa duy nhất để evidence
/// vào hệ thống là bên trong một tín hiệu case, thì loại evidence đó **vĩnh viễn
/// không có đường vào** — không phải "chưa hỗ trợ", mà là không thể. Rồi cũng phải
/// mở cửa thứ hai, và lúc đó mới đúng cái bẫy `IM-12` cảnh báo: hai đường code cùng
/// tạo ra <c>evidence_item</c>, đường ít chạy hơn mục dần mà không ai biết.
///
/// Một cửa duy nhất cho evidence, và quan hệ với Case là **tuỳ chọn**.
/// </summary>
/// <param name="CaseSourceReference">
/// Khoá nguồn của Case mà quan sát này thuộc về — <c>"jira:ES-1234"</c>.
///
/// <c>null</c> LÀ HỢP LỆ và là chủ đích (`K-B9`): mẩu tri thức rời không thuộc case nào.
/// Nhưng nếu CÓ khai mà case không tồn tại thì **cả lô bị từ chối**: nhận rồi lặng lẽ
/// để link rỗng sẽ biến một lỗi thứ tự gọi thành dữ liệu sai không ai thấy.
///
/// ⚠ Bên gửi không cần "nhớ đã gửi case chưa": <c>/signals/case-observed</c> idempotent,
/// nên cứ gửi case trước mỗi lần, lần hai trả <c>created: 0</c> và vô hại.
/// </param>
/// <param name="SourceReference">
/// Khoá của chính mẩu quan sát ở nguồn — <c>"jira:ES-1234#comment-7"</c>.
/// Khoá chống trùng, unique <c>(TenantId, SourceReference)</c>.
/// </param>
/// <param name="Content">Nội dung quan sát được, ở dạng đọc được. v0.2 R5: giữ nguyên văn nguồn.</param>
/// <param name="ObservedAt">Thời điểm quan sát được Ở NGUỒN — khác <c>IngestedAt</c> (lúc ta biết).</param>
/// <param name="MachineReadability">
/// Máy đọc được đến mức nào: <c>Unknown</c> · <c>Low</c> · <c>Medium</c> · <c>High</c>.
///
/// **Bên gửi khai, hệ thống KHÔNG suy.** Connector biết nó đang đẩy text hay đường dẫn
/// ảnh; hệ thống nhìn vào một chuỗi thì không biết. Tự gán <c>High</c> cho mọi thứ là
/// text sẽ dán nhãn sai cho ảnh chưa OCR — đúng trạng thái
/// <c>KNOWLEDGE_EXISTS_NOT_RETRIEVABLE</c> ở §6.3 mà sản phẩm cần nhìn thấy.
///
/// Thiếu thì là <c>Unknown</c> — enum đã có sẵn giá trị đó, và "chưa ai nói" là một
/// câu trả lời thật. Sai chính tả thì **400**, không âm thầm về <c>Unknown</c>.
/// </param>
/// <param name="SubTenantCode">
/// Mã khách sạn mà mẩu này thuộc về — `AR-l`, chốt 2026-09-15 (phiếu `docs/14` §C3 = C3-1).
///
/// 🛑 **BẮT BUỘC KHAI, và bỏ trống là 400 chứ KHÔNG tự hiểu thành "chưa biết".** Đây là chỗ
/// dễ thấy thừa nhất của cả hợp đồng này, nên ghi rõ vì sao: hai câu *"bên gửi chưa biết"*
/// và *"bên gửi chưa đọc tài liệu"* khác nhau đúng ở chỗ quan trọng nhất, và nếu cửa vào tự
/// điền hộ thì về sau không có cách nào dựng lại khác biệt đó.
///
/// Nhận ba loại giá trị: một mã khách sạn thật · <c>khong-xac-dinh</c> (đã được hỏi, chưa
/// biết) · <c>khong-thuoc-khach-san</c> (khẳng định KHÔNG thuộc khách sạn nào — việc nội bộ
/// ezCloud, ezGolf, hoặc một chuỗi nhiều cơ sở chưa tách được).
///
/// ⚠ **Hai giá trị dành riêng KHÔNG tính vào điều kiện mở lại `AR-l`** (*"≥2 giá trị PHÂN
/// BIỆT"*): đổi từ sentinel này sang sentinel kia không phân biệt được khách sạn nào với
/// khách sạn nào. Xem <see cref="KnowledgePlatform.Domain.Evidence.SubTenantCodes"/>.
///
/// 🛑 Và ĐỪNG suy mã từ tiêu đề ticket — đã đo: 8/32 case có mã trong tiêu đề, và ngay phần
/// đó còn lệch với thân bài. Suy sai ở đây không phải một nhãn sai; nó là dữ liệu của khách
/// sạn A nằm trong SOP của khách sạn B.
/// </param>
public sealed record CaseEvidenceSignal(
    string? CaseSourceReference,
    string SourceReference,
    string Content,
    DateTimeOffset? ObservedAt,
    string? MachineReadability,
    string? SubTenantCode);

/// <summary>Kết quả xử lý một mẩu evidence.</summary>
/// <param name="CaseId"><c>null</c> = evidence rời, không gắn case nào (`K-B9`).</param>
/// <param name="Created"><c>false</c> = đã có từ trước. KHÔNG phải lỗi — xem `IM-15`.</param>
/// <param name="SubTenantNotApplied">
/// <c>true</c> = mẩu này ĐÃ CÓ từ trước, bên gửi vừa khai một mã khách sạn KHÁC với mã đang
/// lưu (hoặc mẩu cũ chưa có mã), và **mã mới KHÔNG được ghi đè**.
///
/// 🛑 Trường này tồn tại vì nếu không có nó thì đây là một ca BÁO XANH TRONG KHI HỎNG hoàn
/// hảo: export lại toàn bộ corpus kèm mã khách sạn, nhận <c>200 OK</c>, thấy
/// <c>created: 0</c> (đúng, và trông như bình thường vì `IM-15` đã dạy created 0 không phải
/// lỗi), rồi tin rằng mã đã vào kho. Thực tế không mẩu nào đổi, và `AR-l` vẫn đứng yên.
///
/// Không ghi đè là CỐ Ý — `K-B3`: evidence gắn với MỘT thời điểm và MỘT nguồn, ghi đè lặng
/// lẽ là sửa lại quá khứ. Muốn mã khách sạn vào 128 mẩu cũ thì phải nạp lại vào kho sạch,
/// không phải gửi chồng lên.
/// </param>
/// <param name="ThirdPartyRedacted">
/// Số giá trị của BÊN THỨ BA đã bị che ở khâu nạp (`AR-n`). <c>0</c> nghĩa là luật KHÔNG
/// THẤY GÌ — không phải "mẩu này sạch": trên 873 mẩu thật, khối <c>&lt;NMua&gt;</c> chỉ
/// xuất hiện ở ĐÚNG MỘT mẩu, nên độ phủ của luật chưa đo được.
/// </param>
public sealed record CaseEvidenceResult(
    string SourceReference,
    Guid EvidenceId,
    Guid? CaseId,
    bool Created,
    bool SubTenantNotApplied,
    int ThirdPartyRedacted);

/// <param name="Received">Tổng số nhận được — để bên gửi đối chiếu, không phải đoán.</param>
/// <param name="DistinctSubTenantCodes">
/// Số mã khách sạn PHÂN BIỆT trong lô, **không tính hai giá trị dành riêng**.
///
/// Đây là `AR-k`/`AR-l` viết thành code chứ không phải một lời dặn: *"đếm số giá trị PHÂN
/// BIỆT, không đếm độ phủ — một giá trị = coi như rỗng."* Bài học đó đến từ hai trường đã
/// vấp trong CÙNG một ngày (<c>machineReadability = High</c> ở 128/128 mẩu và
/// <c>Mã khách sạn = -1.0</c> ở 32/32 case), và cả hai đều trông như đã có dữ liệu với mọi
/// phép kiểm dạng "trường này có được điền không?".
/// </param>
/// <param name="SubTenantWarning">
/// <c>null</c> = không có gì để nói. Có giá trị = lô này KHÔNG đẩy `AR-l` tiến thêm, và nói
/// thẳng ra thay vì để bên gửi tự suy từ một con số.
/// </param>
/// <param name="ThirdPartyRedactedTotal">
/// Tổng số chỗ đã che của cả lô. `AR-o` đặt ra luật cho cổng che ở khâu GỬI RA: *"phải đếm
/// và ghi lại số chỗ đã che mỗi lần gọi — không để nó chạy im lặng"*. Cổng ở khâu NẠP mượn
/// đúng luật đó, vì cùng một lý do: luật che là một cận TRÊN đã đo, nên nó phải ra số.
/// </param>
public sealed record CaseEvidenceBatchResult(
    int Received,
    int Created,
    int DistinctSubTenantCodes,
    string? SubTenantWarning,
    int ThirdPartyRedactedTotal,
    IReadOnlyList<CaseEvidenceResult> Results);

/// <summary>
/// Đọc nhãn <see cref="MachineReadability"/> từ chuỗi. Một chỗ duy nhất biết cách
/// đọc, dùng bởi cả tầng kiểm tra lẫn tầng ghi — hai chỗ đọc khác nhau là cách
/// "hợp lệ lúc kiểm, khác nghĩa lúc ghi" ra đời.
/// </summary>
public static class EvidenceReadability
{
    /// <summary>Danh sách giá trị nhận được, để thông báo lỗi nói thẳng thay vì bắt đoán.</summary>
    public static string Allowed => string.Join(" · ", Enum.GetNames<MachineReadability>());

    public static bool TryParse(string? raw, out MachineReadability value)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            value = MachineReadability.Unknown;
            return true;
        }

        // KHÔNG dùng Enum.TryParse: nó nhận cả chuỗi số ("1" → Low) và cả giá trị
        // ngoài enum ("7" → (MachineReadability)7, không ném). Khớp theo TÊN.
        foreach (var name in Enum.GetNames<MachineReadability>())
        {
            if (string.Equals(name, raw, StringComparison.OrdinalIgnoreCase))
            {
                value = Enum.Parse<MachineReadability>(name);
                return true;
            }
        }

        value = MachineReadability.Unknown;
        return false;
    }
}
