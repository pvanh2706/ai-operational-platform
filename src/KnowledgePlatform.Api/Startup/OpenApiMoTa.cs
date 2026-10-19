using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace KnowledgePlatform.Api.Startup;

/// <summary>Mô tả một endpoint cho bên tích hợp đọc.</summary>
/// <param name="Nhom">Gom nhóm trong tài liệu. Bên tích hợp đọc theo VIỆC, không theo đường dẫn.</param>
public sealed record MoTaEndpoint(string Nhom, string TomTat, string MoTa);

/// <summary>
/// **HỢP ĐỒNG TÍCH HỢP, viết ở MỘT chỗ.** `H4-1` chốt sinh tài liệu tự động; đây là phần
/// duy nhất không sinh được — lời giải thích cho người đọc.
///
/// 🛑 **Vì sao gom vào một bảng thay vì rắc `.WithSummary()` khắp `Program.cs`:**
/// <list type="bullet">
///   <item>Toàn bộ chữ mà BÊN NGOÀI đọc nằm trong một file — review được như một tài liệu,
///     thay vì phải ghép từ 17 chỗ.</item>
///   <item>Và quan trọng hơn: bảng này **đối chiếu được với danh sách route thật**. Thêm
///     endpoint mà quên mô tả thì <see cref="StartupChecks"/> **NÉM LÚC KHỞI ĐỘNG** — cùng
///     cơ chế `RlsGuard` dùng cho bảng thiếu RLS. Rắc rải rác thì không ai đối chiếu được,
///     và `IM-38` đã đo đúng chuyện đó xảy ra: bộ Postman đứng ở 5/13 endpoint mà không ai
///     nhận ra, vì không có gì so nó với thực tế.</item>
/// </list>
///
/// ⚠ **OpenAPI KHÔNG tả được đường webhook GỬI RA** — nó chỉ tả những gì bên ngoài GỌI VÀO.
/// Thứ bên tích hợp cần nhất (hình dạng gói, cách kiểm chữ ký) vì thế nằm ở
/// `docs/19_HOP_DONG_TICH_HOP.md`, và phần <see cref="MoTaTaiLieu"/> dưới đây trỏ sang đó.
/// </summary>
public static partial class OpenApiMoTa
{
    private const string NhomHaTang = "Hạ tầng";
    private const string NhomTinHieu = "Kênh 1 — nhận tín hiệu";
    private const string NhomTriThuc = "Tri thức";
    private const string NhomHoi = "Hỏi & trả lời";
    private const string NhomTichHop = "Tích hợp";
    private const string NhomKenh2 = "Kênh 2 — tài liệu";
    private const string NhomQuyTrinh = "Quy trình";
    private const string NhomConsole = "Console nội bộ";

    /// <summary>Khoá là <c>"METHOD /đường/dẫn"</c>, đúng dạng route pattern của ASP.NET.</summary>
    public static IReadOnlyDictionary<string, MoTaEndpoint> Bang { get; } =
        new Dictionary<string, MoTaEndpoint>(StringComparer.Ordinal)
        {
            ["GET /health"] = new(NhomHaTang,
                "Còn sống không",
                "Không chạm database, không cần khoá. Dùng cho load balancer."),

            ["GET /health/ready"] = new(NhomHaTang,
                "Sẵn sàng nhận việc chưa",
                "Chạm database và kiểm ranh giới tenant. Trả 503 khi chưa sẵn sàng. " +
                "⚠ Phép kiểm ở đây NÔNG HƠN lúc khởi động, cố ý: deploy cuốn chiếu không được " +
                "rút cả đội tiến trình khoẻ ra khỏi luồng."),

            ["GET /internal/tenant-boundary"] = new(NhomHaTang,
                "Đếm dữ liệu mà khoá này thấy được",
                "Đếm bằng SQL THÔ không có điều kiện tenant — nên con số trả về chính là bằng " +
                "chứng row-level security đang chặn. Dùng để tự kiểm, không phải để báo cáo."),

            ["GET /internal/sub-tenant-coverage"] = new(NhomHaTang,
                "Điều kiện mở lại ranh giới khách sạn (AR-l)",
                "Đếm số mã khách sạn PHÂN BIỆT trong kho. ⚠ Có TRƯỜNG không phải có DỮ LIỆU: " +
                "đọc ô `canTro` trước khi đọc `datDieuKienMoLai`."),

            ["POST /signals/case-observed"] = new(NhomTinHieu,
                "Báo có việc mới ở nguồn",
                "Tạo Case nếu `sourceReference` chưa có, không thì trả lại Case cũ — GỬI LẠI " +
                "KHÔNG SINH BẢN THỨ HAI. Vượt trần thì TỪ CHỐI CẢ LÔ kèm nói rõ trần, cố ý " +
                "không cắt bớt im lặng. Nếu khách đã khai báo webhook, mỗi Case MỚI sẽ xếp " +
                "một lượt trả kết quả về."),

            ["POST /signals/case-status"] = new(NhomTinHieu,
                "Báo việc ở nguồn đổi trạng thái",
                "`daXongONguon` là BẮT BUỘC (true/false) — cố ý không suy từ việc có hay không " +
                "có `sourceResolvedAt`, vì \"chưa gửi\" và \"đã mở lại\" đều cho ra một ô trống. " +
                "Case chưa từng được tạo thì trả `khong-thay-case` cho riêng phần tử đó, KHÔNG " +
                "tự tạo — một case dựng từ tín hiệu này sẽ không có tiêu đề. " +
                "🛑 Endpoint này CỐ Ý không kích hoạt gì thêm: case đã đóng đo được là MỎNG HƠN " +
                "case còn mở, nên gom tri thức lúc đóng case là vứt đúng phần có giá trị."),

            ["POST /signals/case-evidence"] = new(NhomTinHieu,
                "Nạp mẩu dẫn chứng của một case",
                "`subTenantCode` là BẮT BUỘC (AR-l): chưa biết thì khai `khong-xac-dinh`, " +
                "không thuộc khách sạn nào thì khai `khong-thuoc-khach-san`. Bỏ trống là 400 — " +
                "cố ý không tự điền hộ, vì \"chưa biết\" khác \"chưa ai hỏi\". " +
                "Nội dung đi qua luật che dữ liệu bên thứ ba ngay tại cửa vào — TRỪ KHI bản deploy " +
                "bật cờ `CongChe:TatTamVaChapNhanRuiRo` (N1-2); khi đó mẩu vào kho NGUYÊN VĂN và " +
                "không che lại được về sau. 🎯 P7: mẩu MỚI làm case bước sang bước khác của quy trình " +
                "đang chạy thì một lượt trả kết quả về được xếp hàng (`suKien: buoc-hien-tai-doi`) — " +
                "gửi lại cùng mẩu không xếp thêm."),

            ["POST /evidence/rut-lai"] = new(NhomTinHieu,
                "Đánh dấu một mẩu đã bị NGƯỜI GỬI rút lại",
                "CHỈ NGƯỜI đặt được dấu này (AR-m). Hợp đồng tín hiệu cố ý không mang ba trường " +
                "đó, nên connector không có đường nào tự suy từ câu chữ."),

            ["GET /cases/{khoaNguon}/evidence"] = new(NhomTinHieu,
                "Đọc lại evidence của một case",
                "Tra theo KHOÁ NGUỒN — thứ bên gửi đã biết về case của chính họ. ⚠ Khoá nguồn " +
                "chứa dấu `/` phải mã hoá thành `%2F`: đó là ràng buộc của HTTP, không phải của " +
                "sản phẩm."),

            ["POST /documents"] = new(NhomKenh2,
                "Nạp một tài liệu lên (multipart/form-data)",
                "Trường `file` là nội dung; `subTenantCode` và `nguoiNap` là hai trường chữ bắt buộc. " +
                "Nạp lại cùng một file KHÔNG sinh bản thứ hai — so bằng SHA-256 của nội dung. " +
                "🛑 NẠP TÀI LIỆU KHÔNG SINH RA TRI THỨC (`S6`): file vào kho là một VẬT CHỨA, " +
                "không phải một phát biểu ai chịu trách nhiệm. Tri thức chỉ ra đời khi có NGƯỜI duyệt."),

            ["GET /documents"] = new(NhomKenh2,
                "Liệt kê tài liệu của khách hàng này",
                "Chỉ siêu dữ liệu. Nội dung tải qua `/documents/{id}/tai`."),

            ["GET /documents/{id}/chu"] = new(NhomKenh2,
                "Đọc chữ trích được của một tài liệu",
                "P9 (ô K): `trangThaiTrichChu` = `da-trich` · `chua-ho-tro` (PDF, ảnh — KHÔNG có OCR, N4-1) · " +
                "`khong-giai-ma-duoc` (không phải UTF-8 — cố ý không đoán bảng mã) · `loi`. ⚠ Chữ trích ra KHÔNG phải " +
                "tri thức (S6), và CHƯA qua cổng che nào."),

            ["GET /documents/{id}/tai"] = new(NhomKenh2,
                "Tải nội dung một tài liệu",
                "404 cho cả hai ca: không có tài liệu đó trong phạm vi khách hàng này, VÀ có dòng " +
                "trong kho dữ liệu nhưng nội dung không còn. Ca thứ hai nghĩa là kho và DB đã lệch nhau."),

            ["POST /knowledge/sop-draft"] = new(NhomTriThuc,
                "Nạp một bản nháp quy trình",
                "Nạp KHÔNG phải duyệt: record vào kho ở trạng thái `DRAFT` và KHÔNG ra khỏi " +
                "`/hoi` cho tới khi có người duyệt. Idempotent theo cặp (nhóm, tên nguyên nhân)."),

            ["POST /knowledge/soan-nhap"] = new(NhomTriThuc,
                "Nhờ model soạn một bản nháp quy trình cho một nhóm",
                "Gom case đã được định tuyến vào nhóm đó, gọi model, rồi nạp kết quả vào kho ở " +
                "trạng thái `DRAFT`. **Lượt này TỐN TIỀN** — có trần tháng (`E3-1`), và mỗi lượt " +
                "được ghi biên lai kể cả khi hỏng. " +
                "⚠ `nguoiYeuCau` là LỜI KHAI, không phải phép xác thực: sản phẩm biết request " +
                "thuộc công ty nào, chưa biết người nào. " +
                "🛑 Chưa cấu hình khoá model thì trả 409 kèm `lyDo: chua-co-credit` và ba bước " +
                "của vòng thay thế — KHÔNG hỏng im lặng, KHÔNG trả bản nháp rỗng."),

            ["POST /knowledge/{id}/approve"] = new(NhomTriThuc,
                "Duyệt — nội dung và quyền xem trong MỘT hành động",
                "`xacNhanDuLieuThuongMai` là LỜI KHAI của người duyệt rằng đã soát dữ liệu " +
                "thương mại của khách; thiếu là 400. Đó KHÔNG phải một phép kiểm tự động — loại " +
                "dữ liệu đó không có hình dạng nào để bắt bằng luật. " +
                "🛑 Ở chế độ nhiều khách dùng chung, endpoint này TỪ CHỐI bằng 409 (AR-l chưa giải)."),

            ["POST /knowledge/{id}/sua"] = new(NhomTriThuc,
                "Người sửa lại một bản đã duyệt",
                "Bản gốc KHÔNG bị đè — cả hai phiên bản cùng nằm trong kho. Record tự mang cờ " +
                "`NEEDS_REVIEW`, nhưng KHÔNG bị rút khỏi `/hoi`: người đang xử lý ticket cần bản " +
                "tốt nhất hiện có kèm cảnh báo, không cần một cánh cửa đóng."),

            ["GET /knowledge/{id}"] = new(NhomTriThuc,
                "Đọc một tri thức kèm TOÀN BỘ lịch sử",
                "Khác `/hoi` ở chỗ trả cả bản cũ lẫn bản mới — đây là đường đọc để so bản máy " +
                "soạn với bản người sửa."),

            ["POST /knowledge/{id}/link"] = new(NhomTichHop,
                "Phát một link xem, có hạn và thu hồi được",
                "CHỈ phát được cho bản ĐÃ DUYỆT. Link mang token riêng, mở được nhiều lần trong " +
                "hạn. Cần cấu hình `Link:PublicBaseUrl` — thiếu là 409 chứ KHÔNG đoán, vì đoán " +
                "sẽ sinh ra link `localhost` chỉ mở được trên chính máy chạy app."),

            ["GET /xem/{token}"] = new(NhomTichHop,
                "Mở một tri thức bằng link",
                "KHÔNG cần khoá API — chính token là giấy thông hành. Nội dung đi qua cổng che " +
                "trước khi ra, trừ khi bản deploy bật cờ N1-2 — trường `daQuaCongChe` nói rõ lượt " +
                "đọc này có qua cổng không. Link sai, hết hạn hay đã thu hồi đều trả 404 GIỐNG HỆT NHAU, cố ý: " +
                "phân biệt chúng là nói cho người cầm link biết token đó từng có thật."),

            ["POST /hoi"] = new(NhomHoi,
                "Hỏi một câu, nhận hướng dẫn hoặc KHÔNG BIẾT",
                "`bietTraLoi: false` là một câu trả lời HỢP LỆ, không phải lỗi — đọc `lyDoKhongBiet` " +
                "để biết cửa nào đã chặn. Chỉ trả về tri thức ĐÃ DUYỆT. Mỗi phát biểu mang mức " +
                "kiểm chứng riêng; `MÂU THUẪN` là một giá trị hạng nhất, không phải lỗi. " +
                "⚠ Đây CŨNG là cửa cho loại tín hiệu THỨ BA của Kênh 1 — *người dùng hỏi về tài " +
                "liệu* (`F2-1`). Cố ý không có endpoint riêng: một cửa cho mọi câu hỏi nghĩa là " +
                "một cổng KHÔNG BIẾT. ⚠ Nội dung trả về KHÔNG đi qua cổng che — chỉ gói webhook " +
                "và trang /xem đi qua (ghi rõ 2026-10-10; câu cũ ở đây ngụ ý ngược lại). " +
                "🎯 `maTicket` là một case ĐÃ CÓ (khoá đầy đủ hoặc mã trần) thì câu trả lời đi theo BƯỚC HIỆN " +
                "TẠI của quy trình đã duyệt — ô `quyTrinh`, `traTheo: buoc|nhom`; chưa có quy trình thì trả theo " +
                "nhóm và `lyDoKhongCoQuyTrinh` nói vì sao. Mỗi phát biểu kèm `danChung`: khoá nguồn của mẩu và " +
                "ticket, KHÔNG nội dung."),

            ["PUT /internal/webhook"] = new(NhomTichHop,
                "Khai báo nơi nhận kết quả trả về",
                "Bí mật ký CHỈ hiện ra đúng một lần, ngay sau khi tạo. Đổi địa chỉ thì KHÔNG đổi " +
                "bí mật — bên nhận đang kiểm chữ ký bằng bí mật cũ. " +
                "⚠ Hình dạng gói gửi đi và cách kiểm chữ ký: xem docs/19_HOP_DONG_TICH_HOP.md."),

            ["GET /internal/webhook"] = new(NhomTichHop,
                "Đọc lại cấu hình nơi nhận",
                "KHÔNG trả bí mật ký. Mất thì khai báo lại để sinh cái mới."),

            // --- Ô J (P6, 2026-10-10) ---

            ["POST /goi-y/{id}/phan-hoi"] = new(NhomHoi,
                "Khai một gợi ý đã được NHẬN / DÙNG chưa",
                "`id` là `maLuotGoiY` trong câu trả lời của /hoi hoặc trong gói webhook. `nac`: `accepted` | `used`, " +
                "`datDuoc` BẮT BUỘC (true/false), `nguoiKhai` BẮT BUỘC — đây là LỜI KHAI (N3-1). Ba nấc kia " +
                "(RETRIEVED · SHOWN · OPENED) hệ thống tự ghi. `nhomDung` (E2-1): định tuyến sai thì khai nhóm ĐÚNG " +
                "(phải có trong taxonomy) — lượt hỏi sau của chính case đó đi theo nhóm này."),

            // --- Console nội bộ (P2-1, 2026-10-19) ---

            ["GET /internal/cases"] = new(NhomConsole,
                "Danh sách case — mới nạp trước",
                "Lọc theo tiền tố khoá nguồn bằng `?tienTo=`, tối đa `?soLuong=` (mặc định 50, trần 200). Mỗi dòng kèm " +
                "nhóm HIỆU LỰC và NGUỒN của nó (`nguoi-nap:` · `tra-san:` · `model:`), tính KHÔNG gọi model — " +
                "`model:chua-goi` nghĩa là chưa ai hỏi ticket này từ khi bật định tuyến bằng model."),

            ["GET /internal/cases/{khoaNguon}/dong-chay"] = new(NhomConsole,
                "Dòng chảy của một case",
                "Mọi thứ hệ thống đã làm với case: evidence (rút gọn; bản đủ ở `GET /cases/{khoaNguon}/evidence`), định " +
                "tuyến hiệu lực + kết quả người nạp + MỌI biên lai model (kể cả lượt hỏng), lượt chạy quy trình + tiến độ, " +
                "lượt gợi ý + các nấc, lượt gửi kết quả về. 🛑 KHÔNG tác dụng phụ: không gọi model, không ghi biên lai, " +
                "không tạo lượt chạy. Trả DỮ KIỆN — cách vẽ thành sơ đồ là việc của giao diện."),

            ["GET /internal/goi-y/thong-ke"] = new(NhomHaTang,
                "Phễu 5 nấc của các lượt gợi ý",
                "Mỗi nấc đếm số LƯỢT có ít nhất một sự kiện đạt / không đạt. ⚠ SHOWN = đã GIAO, không phải người đã " +
                "đọc; ACCEPTED/USED là lời khai. Lọc từ một mốc bằng `?tu=`."),

            // --- Quy trình (P4, 2026-10-10 — mở lại I1) ---

            ["POST /processes"] = new(NhomQuyTrinh,
                "Viết tay một quy trình (vào DRAFT)",
                "Bước là danh sách TUYẾN TÍNH — không nhánh (PR2): \"thấy X thì là nguyên nhân Y\" là tri thức, " +
                "không phải bước. Mỗi bước có `dauHieu` (cụm từ mà một mẩu evidence chứa thì coi như bước đã làm) " +
                "và `chuDe` (tri thức bước này tra; bỏ trống = theo nhóm). Mỗi lần tạo là một PHIÊN BẢN mới của nhóm."),

            ["GET /processes"] = new(NhomQuyTrinh,
                "Liệt kê quy trình (lọc theo `nhom`)",
                "Mọi phiên bản, cả DRAFT và DEPRECATED. Mỗi nhóm có TỐI ĐA MỘT bản ACTIVE."),

            ["GET /processes/{id}"] = new(NhomQuyTrinh,
                "Đọc một quy trình kèm các bước",
                "`canhBao` nói bước nào chưa có dấu hiệu — bước đó chỉ xong được bằng lời xác nhận."),

            ["PUT /processes/{id}/buoc"] = new(NhomQuyTrinh,
                "Thay toàn bộ bước — CHỈ khi còn DRAFT",
                "Bản ACTIVE BẤT BIẾN (N5-1, PROPOSED): sửa thì 409, muốn đổi thì tạo phiên bản mới. Đây là chỗ " +
                "người duyệt thêm dấu hiệu cho quy trình sinh từ bản nháp SOP."),

            ["POST /processes/{id}/approve"] = new(NhomQuyTrinh,
                "Duyệt một quy trình",
                "Cùng ba chốt với duyệt tri thức: `nguoiDuyet` + `phamViXem` (S7), `xacNhanDuLieuThuongMai` (AR-n), " +
                "và chế độ shared TỪ CHỐI (AR-l). Duyệt bản mới thì bản ACTIVE cũ của cùng nhóm thành DEPRECATED; " +
                "case đang chạy dở bản cũ VẪN chạy bản cũ (N6-1, PROPOSED)."),

            ["GET /cases/{khoaNguon}/quy-trinh"] = new(NhomQuyTrinh,
                "Case đang ở bước nào",
                "CHỈ ĐỌC — không tạo gì, KHÔNG gọi model (case model chưa xếp thì trả `dinh-tuyen-khong-biet`). Tiến độ là PHÉP CHIẾU từ evidence, không có cờ nào được lưu (PR1): bước " +
                "xong ⟸ một mẩu chứa dấu hiệu của bước, hoặc một lời xác nhận trỏ đúng bước. Mỗi bước xong kèm " +
                "`canCu` — mẩu nào, bằng cách nào — để phản bác được. `coQuyTrinh: false` kèm `lyDoKhongCo`."),

            ["POST /quy-trinh/xac-nhan-buoc"] = new(NhomQuyTrinh,
                "Khai đã làm một bước",
                "Lời khai được lưu thành một mẩu EVIDENCE có tác giả (N2-1), không phải cờ — rút lại được như mọi " +
                "mẩu khác. `subTenantCode` BẮT BUỘC (AR-l). Case chưa có lượt chạy thì lượt chạy được tạo ở đây."),

            ["POST /quy-trinh/bo"] = new(NhomQuyTrinh,
                "Bỏ lượt chạy quy trình đang mở",
                "CHỈ NGƯỜI, bắt buộc `nguoiBo` + `lyDo`. Bỏ rồi thì hệ thống KHÔNG tự dựng lại lượt chạy cho đúng " +
                "cặp (case, quy trình) đó."),
        };

    /// <summary>Khoá tra bảng cho một route.</summary>
    public static string Khoa(string method, string route) => $"{method} /{route.TrimStart('/')}";

    /// <summary>
    /// Route nào chưa có mô tả. Rỗng nghĩa là hợp đồng còn đủ.
    ///
    /// ⚠ Bỏ phần ràng buộc kiểu khỏi route (<c>{id:guid}</c> -> <c>{id}</c>): bảng viết theo
    /// cách bên tích hợp ĐỌC đường dẫn, không theo cách ASP.NET khai báo nó.
    /// </summary>
    public static IReadOnlyList<string> ThieuMoTa(IEnumerable<(string Method, string Route)> routes) =>
        [.. routes
            .Select(r => Khoa(r.Method, BoRangBuocKieu(r.Route)))
            .Where(k => !Bang.ContainsKey(k))
            .Distinct()
            .OrderBy(k => k, StringComparer.Ordinal)];

    /// <summary>
    /// Mô tả cho endpoint KHÔNG còn tồn tại. Chiều ngược của <see cref="ThieuMoTa"/>.
    ///
    /// 🛑 **Chiều này tồn tại vì một lý do cụ thể, không phải cho đối xứng: nó làm cổng canh
    /// TỰ PHÁT HIỆN ĐƯỢC KHI CHÍNH NÓ BỊ NGẮT.** Bản đầu đọc danh sách route từ DI, và lúc
    /// gọi thì danh sách ấy còn RỖNG — cổng duyệt qua không có gì, không bao giờ ném, và
    /// trông hệt như đang canh. Với chiều này, danh sách rỗng nghĩa là **cả 17 mô tả đều
    /// thành thừa** -> ném ngay.
    ///
    /// ⚠ Một cơ chế canh gác mà hỏng thành IM LẶNG là cơ chế tệ nhất: nó còn lấy mất cả sự
    /// cảnh giác của người đọc. Đây là giá của việc ấy, trả bằng mười dòng.
    /// </summary>
    public static IReadOnlyList<string> MoTaThua(IEnumerable<(string Method, string Route)> routes)
    {
        var that = routes.Select(r => Khoa(r.Method, BoRangBuocKieu(r.Route))).ToHashSet(StringComparer.Ordinal);

        return [.. Bang.Keys.Where(k => !that.Contains(k)).OrderBy(k => k, StringComparer.Ordinal)];
    }

    /// <summary><c>{id:guid}</c> -> <c>{id}</c>. Tài liệu OpenAPI cũng bỏ ràng buộc kiểu.</summary>
    public static string BoRangBuocKieu(string route) =>
        RangBuocKieu().Replace(route, "{$1}");

    [System.Text.RegularExpressions.GeneratedRegex(@"\{(\w+):[^}]+\}")]
    private static partial System.Text.RegularExpressions.Regex RangBuocKieu();

    /// <summary>
    /// Gắn mô tả vào tài liệu OpenAPI đã sinh, và viết phần đầu tài liệu.
    /// </summary>
    public sealed class TrinhGan : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument doc, OpenApiDocumentTransformerContext ctx, CancellationToken ct)
        {
            doc.Info = new OpenApiInfo
            {
                Title = "AI Operational Knowledge & Process Platform",
                Version = "v1",
                Description = MoTaTaiLieu,
            };

            foreach (var (duong, muc) in doc.Paths)
            {
                foreach (var (kieu, thaoTac) in muc.Operations ?? [])
                {
                    if (!Bang.TryGetValue(Khoa(kieu.ToString().ToUpperInvariant(), duong), out var mt)) continue;

                    thaoTac.Summary = mt.TomTat;
                    thaoTac.Description = mt.MoTa;
                    thaoTac.Tags = new HashSet<OpenApiTagReference> { new(mt.Nhom, doc) };
                }
            }

            return Task.CompletedTask;
        }
    }

    private const string MoTaTaiLieu = """
        Service phản ứng theo SỰ KIỆN: phần mềm có sẵn của khách phát tín hiệu, sản phẩm này
        thức tỉnh, xử lý, và trả kết quả về.

        ## Xác thực

        Mọi endpoint trừ `/health`, `/health/ready` và `/xem/{token}` đòi một khoá API riêng
        của từng khách hàng:

            Authorization: Bearer kp_<id>_<secret>

        Khoá xác định LUÔN khách hàng — không có header nào để tự khai mình là ai. Sai khoá,
        thiếu khoá, khoá đã thu hồi, hay khoá của khách khác đều trả **401 giống hệt nhau**:
        phân biệt chúng là dựng sẵn một kênh dò.

        ## Hai thứ tài liệu này KHÔNG tả được

        1. **Đường webhook GỬI RA.** OpenAPI chỉ tả những gì bên ngoài gọi VÀO. Hình dạng gói
           trả kết quả, khoá idempotency và cách kiểm chữ ký HMAC nằm ở
           `docs/19_HOP_DONG_TICH_HOP.md`.
        2. **Chất lượng câu trả lời.** Endpoint chạy không có nghĩa là kho có tri thức để trả.
           `bietTraLoi: false` là trạng thái THƯỜNG GẶP hôm nay, không phải lỗi.

        ## Một quy ước xuyên suốt

        Sản phẩm này coi **"không biết" là dữ liệu hạng nhất**. Nhiều endpoint trả 200 kèm một
        ô nói rõ là chưa biết, thay vì trả lỗi hay trả rỗng. Đọc rỗng thành "không có vấn đề
        gì" là đúng thứ thiết kế này sinh ra để chặn.
        """;
}
