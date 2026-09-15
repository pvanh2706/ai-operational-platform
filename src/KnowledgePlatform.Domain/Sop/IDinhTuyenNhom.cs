namespace KnowledgePlatform.Domain.Sop;

/// <summary>
/// Một ticket vừa đến thuộc NHÓM NGUYÊN NHÂN nào — `docs/13`.
///
/// ⚠ INTERFACE MỎNG, cùng lý do với <see cref="ISopDrafter"/> (`AR3`): bản hiện thực hôm
/// nay tra kết quả ĐÃ ĐO SẴN (không cần credit API), bản sau gọi model thật. Nơi gọi không
/// được biết mình đang dùng bản nào — nếu nó biết, thì đổi bản là sửa nơi gọi.
///
/// 🛑 VÌ SAO ĐÂY LÀ PHÂN LOẠI CHỨ KHÔNG PHẢI RETRIEVAL — đo trên cùng một nguồn dữ liệu:
/// <list type="bullet">
///   <item>định tuyến vào nhóm: <b>84,1%</b> · đoán mù: 11,4% — hơn 7 lần</item>
///   <item>tìm case giống bằng văn bản (FTS): <b>34%</b> · đoán mù: 31% — hơn 3 điểm</item>
/// </list>
/// Hai cơ chế, cùng dữ liệu, cách nhau rất xa. `docs/13` §0.
/// </summary>
public interface IDinhTuyenNhom
{
    Task<KetQuaDinhTuyen> DinhTuyenAsync(YeuCauDinhTuyen yeuCau, CancellationToken ct = default);
}

/// <summary>
/// Đầu vào của định tuyến.
///
/// ⚠ BA TRƯỜNG, và cả ba đều TUỲ CHỌN là cố ý. `docs/13` §1: phép đo dùng đúng thứ có mặt
/// LÚC TICKET ĐẾN — tiêu đề + mô tả, KHÔNG comment, KHÔNG transcript. Một bản hiện thực
/// đòi thêm gì ngoài hai thứ đó là đang dùng thông tin của tương lai.
///
/// <paramref name="MaTicket"/> có mặt cho bản TRA SẴN, và chỉ cho nó. Bản gọi model thật
/// KHÔNG được dùng trường này — biết mã ticket là biết đáp án.
/// </summary>
public sealed record YeuCauDinhTuyen(string? MaTicket, string? TieuDe, string? MoTa);

/// <summary>
/// Kết quả định tuyến.
///
/// 🛑 <paramref name="Nhom"/> <c>null</c> nghĩa là **KHÔNG BIẾT**, và đó là một câu trả lời
/// HỢP LỆ, không phải một lỗi. `docs/13` §3.2 đã đo vì sao điều này phải nằm trong kiểu dữ
/// liệu chứ không phải trong tài liệu: trên 62 case mà hồ sơ KHÔNG BAO GIỜ ghi ra nguyên
/// nhân, bộ định tuyến vẫn **dám gán 40,3%**. Đó là tự tin KHÔNG KIỂM ĐƯỢC — với một sản
/// phẩm đưa hướng dẫn cho người làm thật, nó nguy hiểm ngang với gán sai.
/// </summary>
/// <param name="Nhom">Tên nhóm, hoặc <c>null</c> khi không biết.</param>
/// <param name="Nguon">Vì sao ra kết quả này — ví dụ <c>noi-ro-trong-mo-ta</c>, <c>suy-tu-trieu-chung</c>.</param>
/// <param name="GiaiThich">Câu giải thích ngắn, để người đọc phản bác được.</param>
public sealed record KetQuaDinhTuyen(string? Nhom, string Nguon, string? GiaiThich)
{
    /// <summary>Giá trị mà bộ chấm và file kết quả dùng cho "không biết".</summary>
    public const string KhongBiet = "KHONG-BIET";

    public bool BietNhom => Nhom is not null;

    public static KetQuaDinhTuyen KhongXacDinh(string nguon, string? giaiThich = null) =>
        new(null, nguon, giaiThich);
}
