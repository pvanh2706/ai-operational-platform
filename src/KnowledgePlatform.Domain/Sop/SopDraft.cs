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
    /// Phân bố mà `S8` đòi: trong số ticket của nhóm, bao nhiêu ticket GHI LẠI bước kiểm.
    ///
    /// 🛑 BẮT BUỘC, và đây là chỗ khó nhất của cả hình dạng này — không phải vì nội dung mà
    /// vì PHÉP CỘNG: tổng các ô phải bằng đúng số ticket của nhóm, không ticket nào bị đếm
    /// hai lần, không ticket nào bị bỏ quên. `--kiem-cay` kiểm cả ba điều đó.
    ///
    /// ⚠ Thiếu khối này là lỗ đã tìm ra 2026-09-08 bằng một phép thử KHÔNG TỐN TIỀN: cho
    /// chính hai cây dựng tay đi qua kiểu C# rồi ghi lại, `--kiem-cay` trả 3 phát hiện CHẶN.
    /// Nếu chờ lượt gọi API mới biết thì mất tiền để phát hiện một lỗi kiểu dữ liệu.
    /// </summary>
    [JsonPropertyName("phanBoBuocKiemDuocGhiLai")]
    public required SopDistribution PhanBoBuocKiemDuocGhiLai { get; init; }

    /// <summary>
    /// Ticket xuất hiện trong bản nháp mà KHÔNG thuộc nhóm — kèm lý do vì sao nó ở đó.
    /// Khoá là mã ticket, giá trị là lời giải thích.
    ///
    /// Có mặt vì một ca thật: bước LOẠI TRỪ của nhóm 1 trỏ tới `ES-343036`, một ca thuần
    /// của nhóm "xung đột phiên". Không có chỗ giải thích thì `--kiem-cay` coi đó là bịa
    /// nguồn — mà nó là cố ý. `null` khi bản nháp không dùng ticket ngoài nhóm.
    /// </summary>
    [JsonPropertyName("_haiCaseLech")]
    public IReadOnlyDictionary<string, string>? HaiCaseLech { get; init; }

    /// <summary>
    /// Những chỗ bản nháp TỰ NHẬN là không đủ bằng chứng. `G6`/`AP3`: nói ra chỗ mình
    /// không biết là một phần của đầu ra, không phải một lời xin lỗi.
    /// </summary>
    [JsonPropertyName("khoangTrongPhaiBiet")]
    public IReadOnlyList<string>? KhoangTrongPhaiBiet { get; init; }
}

/// <summary>
/// Phân bố "bước kiểm có được ghi lại hay không", với BỘ Ô CỐ ĐỊNH.
///
/// ⚠ VÌ SAO CỐ ĐỊNH mà không để mỗi nhóm tự đặt tên ô: hai cây dựng tay đặt tên ô khác
/// nhau (nhóm 1 có 5 ô, nhóm 2 có 3 ô, và ô của nhóm 2 là bản chia nhỏ của nhóm 1). Một
/// phân bố mà mỗi nhóm tự đặt tên là một phân bố **không so được giữa các nhóm** — mà so
/// được chính là lý do `S8` đòi phân bố. Nên bộ ô này là bộ hợp của cả hai, đủ để chứa
/// những gì đã đo trên 20 ticket.
///
/// ⚠ Hai cây dựng tay GIỮ NGUYÊN tên ô riêng của chúng — `nhom_sop.py --kiem-cay` nhận cả
/// hai dạng (nó chỉ đòi phép cộng khớp). Đừng sửa hai file đó để "cho khớp": chúng là bản
/// A của phép đo `M2`, sửa là làm hỏng mốc so.
/// </summary>
public sealed record SopDistribution(
    /// <summary>Ticket ghi RÕ bước kiểm: có câu hỏi, có chỗ xem, có giá trị quan sát được.</summary>
    [property: JsonPropertyName("ghi-ro-buoc-kiem")] SopDistributionBucket GhiRoBuocKiem,
    /// <summary>Suy ra được từ mẩu (ví dụ nhìn ảnh khách gửi), nhưng bước kiểm không được viết ra.</summary>
    [property: JsonPropertyName("suy-ra-duoc-nhung-khong-ghi")] SopDistributionBucket SuyRaDuocNhungKhongGhi,
    /// <summary>Chỉ ghi KẾT LUẬN, không ghi cách biết. Ví dụ thật: một ticket đóng bằng tám chữ "Sai CCCD".</summary>
    [property: JsonPropertyName("chi-ghi-ket-luan")] SopDistributionBucket ChiGhiKetLuan,
    /// <summary>Bước kiểm xảy ra NGOÀI ticket — qua remote desktop hoặc điện thoại.</summary>
    [property: JsonPropertyName("buoc-kiem-ngoai-ticket")] SopDistributionBucket BuocKiemNgoaiTicket,
    /// <summary>Không phải một ca chẩn đoán (hỏi-đáp, yêu cầu nội bộ, v.v.).</summary>
    [property: JsonPropertyName("khong-phai-chan-doan")] SopDistributionBucket KhongPhaiChanDoan);

/// <summary>
/// Một ô của phân bố. <paramref name="So"/> phải bằng đúng số phần tử của
/// <paramref name="Case"/> — dư thừa CỐ Ý: hai con số phải khớp nhau là một phép kiểm rẻ
/// bắt được sự cẩu thả, và `--kiem-cay` kiểm đúng điều đó.
/// </summary>
public sealed record SopDistributionBucket(
    [property: JsonPropertyName("so")] int So,
    [property: JsonPropertyName("case")] IReadOnlyList<string> Case);

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
