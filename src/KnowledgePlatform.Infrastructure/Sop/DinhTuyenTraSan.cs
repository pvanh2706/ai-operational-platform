using System.Text.Json;
using System.Text.Json.Serialization;
using KnowledgePlatform.Domain.Sop;

namespace KnowledgePlatform.Infrastructure.Sop;

/// <summary>
/// <see cref="IDinhTuyenNhom"/> chạy bằng KẾT QUẢ ĐÃ ĐO SẴN — không gọi API, không tốn credit.
///
/// 🛑 ĐỌC KỸ CHỖ NÀY TRƯỚC KHI TIN MỘT DEMO CHẠY XANH. Bản này **không định tuyến gì cả**.
/// Nó tra một bảng đã có sẵn đáp án, sinh bởi 5 ngữ cảnh sạch ngày 2026-09-10
/// (`docs/ket-qua-phan-tich/dinh-tuyen-150-case.json`). Nó chứng minh **đường ống chạy**:
/// hỏi -> định tuyến -> kho -> trả SOP. Nó KHÔNG chứng minh độ chính xác — con số 84,1%
/// đã đo ở `docs/13` và vẫn nằm ở đó, không phải ở đây.
///
/// ⚠ Và nó TỪ CHỐI đoán khi thiếu mã ticket. Đưa tiêu đề + mô tả của một ticket chưa đo thì
/// câu trả lời trung thực duy nhất là KHÔNG BIẾT — bản tra sẵn không có cơ chế nào để phân
/// loại văn bản mới. Một bản hiện thực "đoán đại cho có" ở đây sẽ làm mọi phép đo về sau
/// thành vô nghĩa, và sẽ làm đúng cái việc `docs/13` §3.2 gọi là tự tin không kiểm được.
///
/// Đổi sang bản gọi model thật: cài <see cref="IDinhTuyenNhom"/> khác trong DI. Nơi gọi
/// không đổi một dòng nào — đó là toàn bộ lý do interface này mỏng (`AR3`).
/// </summary>
public sealed class DinhTuyenTraSan : IDinhTuyenNhom
{
    private readonly Lazy<IReadOnlyDictionary<string, DongTraSan>> _bang;
    private readonly string _duongDan;

    public DinhTuyenTraSan(string duongDanFileKetQua)
    {
        _duongDan = duongDanFileKetQua;
        _bang = new Lazy<IReadOnlyDictionary<string, DongTraSan>>(Doc);
    }

    /// <summary>Số dòng đã nạp — để nơi khởi động log ra, và để test khẳng định là có thật.</summary>
    public int SoDong => _bang.Value.Count;

    public Task<KetQuaDinhTuyen> DinhTuyenAsync(YeuCauDinhTuyen yeuCau, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);

        if (string.IsNullOrWhiteSpace(yeuCau.MaTicket))
        {
            // Trung thực hơn là hữu ích: bản tra sẵn KHÔNG phân loại được văn bản mới.
            return Task.FromResult(KetQuaDinhTuyen.KhongXacDinh(
                "tra-san-khong-co-ma-ticket",
                "Bản định tuyến đang chạy là bản TRA SẴN, chỉ trả lời được cho ticket đã đo. " +
                "Muốn định tuyến ticket mới thì phải cắm bản gọi model thật."));
        }

        var ma = yeuCau.MaTicket.Trim();

        // Khớp CHÍNH XÁC trước; trượt thì thử lại bằng khoá đã bỏ tiền tố nguồn. Xem BoTienToNguon.
        if (!_bang.Value.TryGetValue(ma, out var dong)
            && !(BoTienToNguon(ma) is { } tran && _bang.Value.TryGetValue(tran, out dong)))
        {
            return Task.FromResult(KetQuaDinhTuyen.KhongXacDinh(
                "tra-san-khong-co-trong-bang",
                $"Ticket \"{yeuCau.MaTicket}\" không nằm trong {SoDong} ticket đã đo."));
        }

        // "KHONG-BIET" trong file là một câu trả lời THẬT của phép đo — 46/150 ticket —
        // không phải dòng thiếu dữ liệu. Giữ nguyên nó, đừng biến thành một nhóm nào đó.
        if (string.Equals(dong.Nhom, KetQuaDinhTuyen.KhongBiet, StringComparison.Ordinal))
        {
            return Task.FromResult(KetQuaDinhTuyen.KhongXacDinh(dong.Nguon ?? "khong-ro", dong.VietTat));
        }

        return Task.FromResult(new KetQuaDinhTuyen(dong.Nhom, dong.Nguon ?? "khong-ro", dong.VietTat));
    }

    /// <summary>
    /// Khoá nguồn của case (<c>jira:ES-345990</c>) → mã trần mà bảng đo dùng (<c>ES-345990</c>).
    /// Trả <c>null</c> khi không có gì để bỏ.
    ///
    /// 🛑 Vì sao phải có: bảng đo lưu mã TRẦN, còn case vào qua connector mang tiền tố nguồn
    /// (`G1` — <c>sourceReference</c> là chuỗi tự do). <c>WebhookSender</c> đưa nguyên khoá nguồn
    /// vào, nên trước 2026-10-10 MỌI lượt webhook trượt bảng và ra KHÔNG BIẾT — kể cả cho chính
    /// 150 ticket đã đo — trong khi <c>/hoi</c> với mã trần thì ra nhóm. Cùng một ticket, hai
    /// đường ra, hai kết cục. Test cũ không thấy vì nó tự dựng bảng có sẵn tiền tố <c>jira:</c>.
    ///
    /// ⚠ Bỏ MỌI tiền tố (phần trước dấu <c>:</c> đầu tiên), không chỉ <c>jira:</c> — gắn cứng tên
    /// một connector ở đây là trái `G1`. Đuôi <c>#…</c> (khoá của một mẩu evidence) cũng bỏ.
    ///
    /// ⚠ Bảng đo là SINGLETON dùng chung mọi khách hàng. Bỏ tiền tố làm bảng trả lời được cho
    /// nhiều khoá hơn — vô hại hôm nay vì bảng chỉ chứa 150 ticket của khách #0, nhưng là thứ
    /// phải nhớ nếu có ngày bảng chứa dữ liệu của hơn một khách.
    /// </summary>
    public static string? BoTienToNguon(string khoa)
    {
        ArgumentNullException.ThrowIfNull(khoa);

        var tran = khoa;

        var thang = tran.IndexOf('#');
        if (thang >= 0) tran = tran[..thang];

        var haiCham = tran.IndexOf(':');
        if (haiCham >= 0) tran = tran[(haiCham + 1)..];

        tran = tran.Trim();
        return tran.Length == 0 || tran == khoa ? null : tran;
    }

    private IReadOnlyDictionary<string, DongTraSan> Doc()
    {
        if (!File.Exists(_duongDan))
        {
            // ⚠ Fail LOUD. Một bộ định tuyến không có bảng tra mà vẫn chạy sẽ trả "không
            // biết" cho mọi ticket — trông y hệt một hệ thống đang làm việc thận trọng.
            // Cùng nguyên tắc với `quet_credential_repo.py`: không có gì để đối chiếu mà
            // báo xanh là phép kiểm nguy hiểm.
            throw new FileNotFoundException(
                $"Không thấy bảng định tuyến tra sẵn: {_duongDan}. " +
                "Thiếu nó thì bộ định tuyến trả KHÔNG BIẾT cho mọi ticket, trông như đang thận trọng.",
                _duongDan);
        }

        var goc = JsonSerializer.Deserialize<FileTraSan>(
            File.ReadAllText(_duongDan),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return goc?.KetQua ?? throw new InvalidDataException(
            $"{_duongDan} không có khối \"ketQua\".");
    }

    private sealed record FileTraSan(
        [property: JsonPropertyName("ketQua")] Dictionary<string, DongTraSan>? KetQua);

    private sealed record DongTraSan(
        [property: JsonPropertyName("nhom")] string? Nhom,
        [property: JsonPropertyName("nguon")] string? Nguon,
        [property: JsonPropertyName("vietTat")] string? VietTat);
}
