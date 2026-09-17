using KnowledgePlatform.Domain.Tenancy;

namespace KnowledgePlatform.Domain.Documents;

/// <summary>
/// Một tài liệu khách nạp lên — đích của **Kênh 2** (`AR5`).
///
/// 🛑 **NẠP TÀI LIỆU KHÔNG SINH RA TRI THỨC.** `S6` chốt điều này từ Workstream 04, và nó là
/// lý do Kênh 2 được hoãn suốt: build xong đường nạp thì kho tri thức **vẫn là 0**, vì tri
/// thức chỉ ra đời khi có **hành vi khẳng định của một người** (duyệt). Một file .docx nằm
/// trong kho là một VẬT CHỨA, không phải một phát biểu ai chịu trách nhiệm.
///
/// → Có một test giữ riêng điều đó. Nếu ngày nào đó nạp tài liệu tự đẻ ra `KnowledgeRecord`,
///   test ấy phải đỏ — vì lúc đó cả `D4` lẫn hai cổng của `/hoi` đều bị đi vòng qua.
///
/// ⚠ **Bảng này chỉ giữ SIÊU DỮ LIỆU.** Nội dung nằm ở kho file (<c>IKhoFile</c>) — `AR5`:
/// *file gốc ở blob storage, DB chỉ giữ metadata*. Nhét cả file vào Postgres thì backup, sao
/// chép và di chuyển đều đắt lên theo một cách không ai tính trước.
/// </summary>
public sealed class Document : ITenantScoped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    /// <summary>Tên file người nạp khai. KHÔNG dùng làm đường dẫn — xem <see cref="MaKho"/>.</summary>
    public required string TenKhaiBao { get; init; }

    public required string LoaiNoiDung { get; init; }

    public required long KichThuoc { get; init; }

    /// <summary>
    /// Khoá của file trong kho. Do sản phẩm SINH RA, không lấy từ tên người nạp khai.
    ///
    /// 🛑 Dùng tên khai báo làm đường dẫn là mở thẳng đường đi ngang thư mục
    /// (<c>../../etc/...</c>) và đường ghi đè file của khách khác. Tên khai báo chỉ để HIỂN THỊ.
    /// </summary>
    public required string MaKho { get; init; }

    /// <summary>SHA-256 của nội dung, hex thường. Để nhận ra nạp trùng và để đối chiếu sau này.</summary>
    public required string Hash { get; init; }

    /// <summary>
    /// Mã khách sạn (`AR-l`). BẮT BUỘC ở cửa vào, cùng luật với evidence: *"chưa biết"* và
    /// *"chưa ai hỏi"* là hai chuyện khác nhau, và tự điền hộ thì về sau không dựng lại được.
    /// </summary>
    public required string SubTenantCode { get; init; }

    public required string NguoiNap { get; init; }

    public DateTimeOffset NapLuc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Kho file — `AR5`, và là interface MỎNG theo đúng khuôn `AR3` đã dùng hai lần
/// (<c>ISopDrafter</c>, <c>IDinhTuyenNhom</c>).
///
/// ⚠ **`H3-2` chốt MinIO**, nhưng bản đang cắm là <b>đĩa cục bộ</b> — và lý do phải nói thẳng:
/// máy dựng bản này KHÔNG có container runtime, nên một bản hiện thực MinIO viết ra ở đây sẽ
/// là **code chưa từng chạy một lần nào**. Dự án đã đo được cái giá của thứ đó nhiều lần
/// (`IM-38`, `IM-42`): code trông như đang làm việc thì tệ hơn code vắng mặt.
///
/// → Bản MinIO là MỘT class cài interface này, cắm bằng một dòng DI, viết vào ngày có MinIO
///   đứng sẵn để chạy thật. Đĩa cục bộ không phải lựa chọn thay thế `H3-2`; nó là chỗ đứng
///   cho tới lúc đó.
/// </summary>
public interface IKhoFile
{
    /// <summary>Lưu và trả về mã kho. Mã do bản hiện thực sinh.</summary>
    Task<string> LuuAsync(Guid tenantId, Stream noiDung, CancellationToken ct = default);

    /// <summary>Đọc lại. <c>null</c> nghĩa là không còn.</summary>
    Task<Stream?> DocAsync(Guid tenantId, string maKho, CancellationToken ct = default);

    Task XoaAsync(Guid tenantId, string maKho, CancellationToken ct = default);
}
