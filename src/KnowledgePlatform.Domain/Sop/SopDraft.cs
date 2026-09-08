using System.Text.Json.Serialization;

namespace KnowledgePlatform.Domain.Sop;

/// <summary>
/// Một BẢN NHÁP SOP dạng bước kiểm — thứ `ISopDrafter` phải sinh ra.
///
/// ⚠ TÊN TRƯỜNG JSON LÀ TIẾNG VIỆT VÀ CỐ Ý GIỮ NGUYÊN. Đây không phải sở thích: hai bản
/// nháp dựng TAY ở `docs/ket-qua-phan-tich/cay-quyet-dinh-*.json` đã có phép kiểm chạy
/// được (`scripts/jira-export/nhom_sop.py --kiem-cay`, 11 phép đột biến, 11/11 bắt được).
/// Nếu bản do máy sinh dùng tên trường khác thì phải viết lại bộ eval — và bộ eval là thứ
/// đắt nhất đang có. Xem `docs/11` §4 và §9.1.
///
/// Hình dạng này đến từ việc dựng TAY hai nhóm rồi so, không từ suy đoán trên bàn:
/// <list type="bullet">
///   <item>nhóm 1 (phân quyền) ra một CÂY PHÂN NHÁNH LỒNG NHAU</item>
///   <item>nhóm 2 (NCC từ chối payload) ra một BẢNG TRA + vòng lặp 4 bước</item>
/// </list>
/// Nên <see cref="BangTraMaLoi"/> là <c>null</c> với nhóm dạng cây, và một nhánh trỏ tới
/// <c>"BANG"</c> khi nó dẫn sang bảng. `docs/11` §9: đầu ra KHÔNG được cứng hoá là "cây".
/// </summary>
public sealed record SopDraft
{
    [JsonPropertyName("nhom")]
    public required string Nhom { get; init; }

    [JsonPropertyName("soCaseDungSau")]
    public required int SoCaseDungSau { get; init; }

    [JsonPropertyName("trieuChungVao")]
    public required IReadOnlyList<SopSymptom> TrieuChungVao { get; init; }

    [JsonPropertyName("buocKiem")]
    public required IReadOnlyList<SopCheckStep> BuocKiem { get; init; }

    [JsonPropertyName("bangTraMaLoi")]
    public SopErrorCodeTable? BangTraMaLoi { get; init; }

    [JsonPropertyName("buocSua")]
    public required IReadOnlyList<SopFixStep> BuocSua { get; init; }

    [JsonPropertyName("buocXacNhan")]
    public SopConfirmStep? BuocXacNhan { get; init; }

    /// <summary>
    /// Những chỗ bản nháp TỰ NHẬN là không đủ bằng chứng. `G6`/`AP3`: nói ra chỗ mình
    /// không biết là một phần của đầu ra, không phải một lời xin lỗi.
    /// </summary>
    [JsonPropertyName("khoangTrongPhaiBiet")]
    public IReadOnlyList<string>? KhoangTrongPhaiBiet { get; init; }
}

/// <param name="MoTa">Câu khách nói hoặc màn hình báo, viết bằng lời của người dùng.</param>
/// <param name="Case">Mã ticket chống lưng. ⚠ Phải TỒN TẠI trong nhóm — eval kiểm điều này.</param>
public sealed record SopSymptom(
    [property: JsonPropertyName("moTa")] string MoTa,
    [property: JsonPropertyName("case")] IReadOnlyList<string> Case);

/// <summary>
/// Một bước kiểm. Bảy trường, và mỗi trường có mặt vì một chỗ đã vấp khi dựng tay —
/// xem `docs/11` §4 (danh sách gốc) và §9.1 (ba trường nhóm thứ hai đòi thêm).
/// </summary>
public sealed record SopCheckStep
{
    [JsonPropertyName("ma")]
    public required string Ma { get; init; }

    [JsonPropertyName("cauHoi")]
    public required string CauHoi { get; init; }

    /// <summary>Mở màn hình NÀO. Nhóm 1 có hai nguyên nhân dùng hai màn quản trị khác nhau
    /// với triệu chứng khách kể giống hệt — nên "xem ở đâu" là nội dung, không phải trang trí.</summary>
    [JsonPropertyName("noiXem")]
    public required string NoiXem { get; init; }

    /// <summary>
    /// Quyền cần có để LÀM được bước kiểm này, nếu bước này bị chặn bởi phân quyền.
    /// Trường này sinh ra từ `docs/11` §9.1: bước kiểm đầu tiên của nhóm 2 cần quyền vào
    /// Nhật ký, mà lễ tân thường không có — tức nguyên nhân của nhóm 1 chặn bước kiểm của
    /// nhóm 2. Một thư viện SOP xếp theo nguyên nhân không thấy được quan hệ đó.
    /// </summary>
    [JsonPropertyName("quyenCan")]
    public string? QuyenCan { get; init; }

    [JsonPropertyName("nhanh")]
    public required IReadOnlyList<SopBranch> Nhanh { get; init; }

    /// <summary>Nguyên văn câu trong ticket mà bước kiểm này rút ra từ đó. Rỗng là hợp lệ,
    /// và là thông tin: nghĩa là không ticket nào ghi bước kiểm này.</summary>
    [JsonPropertyName("nguyenVan")]
    public string? NguyenVan { get; init; }

    [JsonPropertyName("bayDaVapThat")]
    public string? BayDaVapThat { get; init; }
}

/// <summary>
/// Một nhánh của bước kiểm.
/// </summary>
/// <param name="QuanSat">Thấy gì.</param>
/// <param name="DiToi">Mã bước tiếp: một `SopCheckStep.Ma`, một `SopFixStep.Ma`,
/// hoặc <c>"BANG"</c> / <c>"K-XN"</c> / <c>"NGOAI-PHAM-VI"</c>.</param>
/// <param name="Case">Mã ticket chống lưng nhánh này. RỖNG LÀ HỢP LỆ.</param>
/// <param name="ChungCu">
/// <c>"evidence-noi-ro"</c> khi có ticket GHI RÕ bước kiểm, <c>"toi-suy-ra"</c> khi chỉ
/// suy từ kết luận.
/// ⚠ Trường này là BẮT BUỘC Ở TẦNG NHÁNH, không phải tầng SOP — `docs/11` §4. Lý do đã
/// đo: cây nhóm 1 có 7/13 nhánh có nguồn và 6 nhánh là suy đoán; một nhãn ở tầng SOP nói
/// được gì về chỗ đó? Không gì cả.
/// ⚠ Và model TỰ KHAI trường này thì KHÔNG đủ tin: eval phải kiểm `Case` có thật.
/// </param>
public sealed record SopBranch(
    [property: JsonPropertyName("quanSat")] string QuanSat,
    [property: JsonPropertyName("diToi")] string DiToi,
    [property: JsonPropertyName("case")] IReadOnlyList<string> Case,
    [property: JsonPropertyName("chungCu")] string ChungCu);

/// <summary>
/// Bảng tra mã lỗi — topology của nhóm KHÔNG có hình cây.
/// </summary>
public sealed record SopErrorCodeTable(
    [property: JsonPropertyName("dong")] IReadOnlyList<SopErrorCodeRow> Dong);

/// <summary>
/// Một dòng bảng tra.
/// ⚠ KHOÁ LÀ CẶP (nhà cung cấp, mã lỗi), không phải mã lỗi một mình — đã đo: VNPT và MISA
/// trả mã khác nhau, và dòng MISA là dòng duy nhất mà sửa dữ liệu khách KHÔNG giải quyết
/// được. Tra bằng mã lỗi một mình sẽ dẫn người ta đi sửa mò. `docs/11` §9.1.
/// </summary>
public sealed record SopErrorCodeRow(
    [property: JsonPropertyName("ncc")] string Ncc,
    [property: JsonPropertyName("maLoi")] string MaLoi,
    [property: JsonPropertyName("truongBiTuChoi")] string TruongBiTuChoi,
    [property: JsonPropertyName("diToi")] string DiToi,
    [property: JsonPropertyName("case")] IReadOnlyList<string> Case,
    [property: JsonPropertyName("chungCu")] string ChungCu);

public sealed record SopFixStep(
    [property: JsonPropertyName("ma")] string Ma,
    [property: JsonPropertyName("viec")] string Viec,
    [property: JsonPropertyName("case")] IReadOnlyList<string> Case);

/// <summary>
/// Điều kiện đóng.
/// ⚠ Phải là một thứ QUAN SÁT ĐƯỢC, không phải "hết báo lỗi" — đã đo ở nhóm 2: 4/10 case
/// ngay từ đầu không có báo lỗi nào, màn hình chỉ nói "đang xử lý, thử lại sau".
/// </summary>
public sealed record SopConfirmStep(
    [property: JsonPropertyName("viec")] string Viec,
    [property: JsonPropertyName("case")] IReadOnlyList<string> Case);
