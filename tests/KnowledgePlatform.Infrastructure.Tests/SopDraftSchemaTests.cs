using System.Text.Json;
using KnowledgePlatform.Infrastructure.Sop;

namespace KnowledgePlatform.Infrastructure.Tests;

/// <summary>
/// Schema là PROMPT, không phải khai báo kiểu — nên nó cần test như prompt.
///
/// ⚠ Lớp này KHÔNG dùng <see cref="TestDatabaseFixture"/> và cố ý không cần PostgreSQL:
/// nó chỉ đọc một hằng số chuỗi. Đặt ở project Infrastructure vì `SopDraftSchema` ở đó,
/// không phải vì nó cần hạ tầng.
///
/// Hai lỗ nó canh, cả hai đều là lỗi IM LẶNG:
///   1. Schema viết tay thì một dấu phẩy thừa làm hỏng cả lượt gọi, và chỗ biết là API
///      trả 400 — tức là biết sau khi đã gửi dữ liệu đi. Ở đây biết trước, giá $0.
///   2. `description` của năm ô phân bố là toàn bộ nội dung của `AR-q`. Một lần "dọn cho
///      gọn" là năm ô trở về trần trụi như trước 2026-09-11, và hậu quả — hai người đọc
///      xếp khác nhau — không lộ ra ở bất kỳ phép kiểm nào khác: `--kiem-cay` chỉ đòi
///      phép cộng khớp, mà phép cộng vẫn khớp khi xếp sai ô.
/// </summary>
public sealed class SopDraftSchemaTests
{
    private static JsonElement Schema() => JsonSerializer.Deserialize<JsonElement>(SopDraftSchema.Json);

    private static readonly string[] NamO =
    [
        "ghi-ro-buoc-kiem", "suy-ra-duoc-nhung-khong-ghi", "chi-ghi-ket-luan",
        "buoc-kiem-ngoai-ticket", "khong-phai-chan-doan",
    ];

    /// <summary>Schema viết tay phải là JSON đọc được — biết ở đây thay vì biết ở mã 400.</summary>
    [Fact]
    public void Schema_la_JSON_doc_duoc()
    {
        var ex = Record.Exception(() => Schema());

        Assert.Null(ex);
    }

    /// <summary>
    /// `AR-q`: mỗi ô phải mang ĐỊNH NGHĨA, không chỉ mang TÊN. Ngưỡng 200 ký tự không phải
    /// con số đẹp — nó là mức mà một định nghĩa kèm ít nhất một ca biên không thể xuống
    /// dưới, và nó chặn kiểu "dọn dẹp" thay định nghĩa bằng một dòng mô tả lại chính tên ô.
    /// </summary>
    [Fact]
    public void Nam_o_phan_bo_deu_co_dinh_nghia_khong_chi_co_ten()
    {
        var o = Schema().GetProperty("properties")
                        .GetProperty("phanBoBuocKiemDuocGhiLai")
                        .GetProperty("properties");

        foreach (var ten in NamO)
        {
            Assert.True(o.TryGetProperty(ten, out var oNay), $"thiếu hẳn ô {ten}");
            Assert.True(oNay.TryGetProperty("description", out var mota),
                        $"ô {ten} không có description — model sẽ chỉ đọc được cái TÊN, đúng "
                        + "tình trạng đã làm hai ngữ cảnh xếp khác nhau 3/10 ticket.");
            Assert.True(mota.GetString()!.Length >= 200,
                        $"description của ô {ten} ngắn hơn một định nghĩa có ca biên.");
        }
    }

    /// <summary>
    /// Thứ tự xét phải nằm TRONG schema, vì nó là thứ gỡ chồng lấn giữa các ô. Không có nó
    /// thì năm định nghĩa tốt vẫn cho ra hai cách xếp khác nhau ở ticket rơi vào hai ô.
    /// </summary>
    [Fact]
    public void Thu_tu_xet_cac_o_duoc_noi_ra_trong_schema()
    {
        var mota = Schema().GetProperty("properties")
                           .GetProperty("phanBoBuocKiemDuocGhiLai")
                           .GetProperty("description").GetString()!;

        foreach (var ten in NamO)
        {
            Assert.Contains(ten, mota);
        }

        var viTri = NamO.Select(t => mota.IndexOf(t, StringComparison.Ordinal)).ToArray();
        Assert.True(viTri[4] < viTri[3], "khong-phai-chan-doan phải được xét TRƯỚC — "
                                       + "không có gì để chẩn đoán thì bốn ô kia không áp dụng.");
    }

    /// <summary>
    /// 🛑 Ô `buoc-kiem-ngoai-ticket` về 0 là cách hỏng ĐÃ XẢY RA, không phải rủi ro tưởng
    /// tượng: một ngữ cảnh đọc tên ô thành "kết luận ngoài ticket" rồi dọn sạch ô mang phát
    /// hiện nền của cả dự án. Cái chặn nó là một câu cụ thể, nên canh đúng câu đó.
    /// </summary>
    [Fact]
    public void O_ngoai_ticket_noi_ro_ket_luan_trong_ticket_khong_keo_no_ra_khoi_o()
    {
        var mota = Schema().GetProperty("properties")
                           .GetProperty("phanBoBuocKiemDuocGhiLai")
                           .GetProperty("properties")
                           .GetProperty("buoc-kiem-ngoai-ticket")
                           .GetProperty("description").GetString()!;

        Assert.Contains("KẾT LUẬN", mota);
        Assert.Contains("KHÔNG kéo ticket ra khỏi ô này", mota);
    }
}
