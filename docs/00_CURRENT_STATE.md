# 00 — CURRENT STATE / SESSION HANDOFF

## AI Operational Knowledge & Process Platform

> **Cập nhật:** 2026-09-04 — 🛑 **CÓ DỮ LIỆU JIRA THẬT, VÀ NÓ KHÔNG NẠP ĐƯỢC NGUYÊN TRẠNG.**
> Người dùng chạy script export, ra **32 case / 128 evidence** (project ES, 4 ngày đầu
> tháng 9). MỚI CHẠY `--dry-run`, **CHƯA NẠP** — `kp_dev` vẫn 0 case, 0 evidence.
> Đã quét bằng 28 agent: 13 phát hiện rủi ro đứng vững sau phản biện đối kháng, 5 bị bác.
>
> ⚠ **VIỆC GẤP, KHÔNG LIÊN QUAN TỚI CODE:** trong `ES-346481#comment-802977` có mật khẩu
> tài khoản hoá đơn điện tử VNPT của khách sạn MST 4500621073, **còn sống** — nhà cung
> cấp trả `OK` hai lần trong chính mẩu đó ngày 01/09. Ai đọc Jira đều thấy. Cần xoay
> mật khẩu và rà hoá đơn đã phát hành trên dải 1/002. Che corpus KHÔNG gỡ được việc nó
> đang nằm công khai trong Jira. Thêm 5 bộ ID+mật khẩu Ultraviewer của 5 khách sạn khác.
> Sinh `AR-j`.
>
> ⚠ **BUG THẬT TRONG ĐƯỜNG NẠP VỪA BUILD:** `machineReadability` = `High` cho **128/128**
> mẩu, kể cả mẩu 5 ký tự. Trường phân loại không phân loại được gì. Ai lọc bằng nó là
> lọc 100% dữ liệu. Sinh `AR-k`.
>
> ⚠ **`AR-h` ĐO LẠI TRÊN CORPUS THẬT — hai ràng buộc TỆ HƠN bản cũ, một cái MỚI:**
> dấu `-` làm truy vấn trả về **0 dòng kể cả chính nó** (bản cũ ghi "gần như toàn bộ
> kho" — ngược chiều); dump đè tiêu đề **29:1** chứ không phải 4:1; và **tiếng Việt gõ
> không dấu KHÔNG khớp gì** (30/32 tiêu đề có dấu) — cái này hoàn toàn mới.
>
> 🛑 **PHÁT HIỆN NẶNG NHẤT, chạm vào TIỀN ĐỀ của sản phẩm chứ không phải corpus:**
> **cờ "đã xong" đi NGƯỢC với hàm lượng tri thức.** Cả 7 case đã đóng đều kết thúc bằng
> câu xác nhận *hết triệu chứng*, không câu nào ghi *đã làm gì*: "Done nhé" (23 ký tự),
> "Khách tạo được thẻ thành công rồi" (33), một case đóng bằng đúng một tấm ảnh.
> Trong khi mọi mẩu có nguyên nhân + cách sửa thật đều nằm ở case **CÒN MỞ**.
> → Bộ lọc tự nhiên nhất ("chỉ lấy case đã đóng") sẽ **vứt đi đúng phần có giá trị**.
> Lý do: bước kết luận không biến mất, nó **xảy ra ở nơi khác** — 6 case chuyển sang
> phiên remote desktop, 2 case "gọi khách không được", 1 case ghi thẳng *"như đã trao
> đổi qua điện thoại"*. Chính 5 mẩu credential ở `AR-j` là **biên lai** của 5 lần chẩn
> đoán diễn ra trên màn hình remote và không để lại chữ nào trong ticket.
> Trong 128 mẩu: Kibana được nhắc **0 lần** (§8.1-KQ nói đó là bước 1 của quy trình thật).
>
> ⚠ Corpus KHÔNG gom nổi một SOP: chủ đề dày nhất (hoá đơn) có 7 case → **7 nguyên nhân
> khác nhau, 0 cái lặp lại**. Con số S8 trung thực nhất viết ra được là **"1/7"**, không
> phải "14/20". Chủ đề first use case (OTA booking) = **0/32 case**. Và `R-K4` vẫn KHÔNG
> đếm được: hai vòng phân tích đọc CÙNG dữ liệu ra hai kết luận ngược nhau (>10 và <5)
> — bằng chứng rằng n=32 quá nhỏ, chưa phải bằng chứng cho hướng nào.
> → Ghi "chưa đếm được", ĐỪNG chốt kiến trúc tìm kiếm dựa trên nó.
>
> ✅ **BA QUYẾT ĐỊNH CHỐT CÙNG NGÀY** (người dùng, sau khi đọc kết quả quét):
> ```text
> 1  Lô 32 case CHỈ vào kp_dev làm fixture, KHÔNG vào kho tri thức thật.  → AR-j
>    Không phải vì rủi ro mà vì lợi ích bằng không: SOP tốt nhất gom được
>    là 4 bước, MỖI BƯỚC n=1. Làm fixture thì nó có giá trị thật — đã bắt
>    được AR-k ngay lần đọc đầu.
> 2  Ranh giới KHÁCH SẠN A ↔ B LÀ ranh giới bảo mật. Thêm sub-tenant vào    → AR-l
>    evidence_item NGAY, lúc bảng còn rỗng. ⚠ RLS KHÔNG cứu được kiểu rò
>    này — rò xảy ra ở khâu XUẤT BẢN SOP, không ở khâu truy vấn hàng.
> 3  ĐẾM nguyên nhân trên corpus 12 tháng TRƯỚC khi chốt kiến trúc tìm      → R-K4
>    kiếm. ~1 ngày công, chặn được quyết định đắt nhất còn lại (vector DB).
>    ✅ ĐÃ ĐẾM 2026-09-04, và phép thử retrieval đã chạy 2026-09-05.
>    ⚠ HẠ CẤP 2026-09-05: kết quả là PHÉP ĐO TRÊN MỘT NGUỒN, KHÔNG phải quyết
>      định kiến trúc — xem 07 §5 R-K4. Một khách, một nguồn, một chủ đề.
> ```
> ---
> ### 🔄 CẬP NHẬT CUỐI NGÀY 2026-09-04 — đã chạy thật trên Jira, hai quyết định có kết quả
>
> **Quyết định 2 (sub-tenant) BỊ CHẶN — không có nguồn nào dùng được.** Chạy
> `scripts/jira-export/discover_fields.py` trên Jira thật rồi đo lại trên đúng 32 case
> của corpus: trường `Mã khách sạn` [customfield_12710] **TỒN TẠI, phủ 100%, và bằng
> `-1.0` ở 32/32 case** — kiểu number, giá trị -1 là sentinel "chưa xác định", tức nó
> được thiết kế có ý định rồi không bao giờ được điền. `Tên khách sạn / Resort` thì 25/32
> rỗng và 7 case còn lại ghi "ezCloud - Customer Support" (tên TEAM). `C247ExtentionID`
> là extension tổng đài của NHÂN VIÊN. Chi tiết + hai nhánh xử lý ở `07` §5 `AR-l`.
>
> 🛑 **MẪU LỖI LẶP HAI LẦN TRONG MỘT NGÀY — điều đáng mang đi nhất của phiên này.**
> `machineReadability` = High ở 128/128 (`AR-k`) và `Mã khách sạn` = -1.0 ở 32/32
> (`AR-l`) là **cùng một hình dạng**: một trường phủ 100% với MỘT giá trị duy nhất.
> Nó tệ hơn trường rỗng, vì trường rỗng thì ai cũng thấy là thiếu, còn trường
> phủ-100%-một-giá-trị thì **trông như đã có dữ liệu** — mọi phép kiểm *"trường này có
> được điền không?"* đều trả lời CÓ.
> → **LUẬT:** với mọi trường dùng làm ranh giới hay bộ lọc, **đếm số giá trị PHÂN BIỆT,
>   không đếm độ phủ.** Một giá trị = coi như rỗng. Đã thành phép kiểm chạy được:
>   `scripts/jira-export/check_corpus.py` (trả mã thoát ≠ 0, cắm được vào CI).
>
> **Quyết định 3 (đếm R-K4): corpus ĐÃ VỀ, và cơ sở ước lượng cũ sai 20 lần.**
> Đếm thật bằng JQL: **2 723** case chủ đề hoá đơn đã đóng trong 12 tháng (ước cũ ~140),
> **38 451** case toàn project, 896 case khoá từ. Lý do sai: corpus 4 ngày lọc thêm
> `"Kỹ thuật phụ trách" is not EMPTY` — một tập con rất nhỏ. **Đừng ngoại suy quy mô từ
> một mẫu mà chính mình đã lọc.**
> → Đã xuất **mẫu 150 case / 345 evidence** (`MAX_ISSUES=150`, `ORDER BY resolved DESC`).
>   ⚠ Mẫu THIÊN LỆCH VỀ THỜI GIAN, không rải đều 12 tháng. Phép đếm nguyên nhân là
>   việc CÒN LẠI, chưa làm.
>
> 🛑 **CORPUS 12 THÁNG XÁC NHẬN ĐỘC LẬP phát hiện "cờ đã xong đi ngược hàm lượng tri
> thức" — và mạnh hơn dự kiến.** Corpus mới có **100% case đã đóng** (150/150), nhưng
> chỉ **1,6 mẩu dùng được mỗi case** — MỎNG HƠN corpus 4 ngày (2,5), vốn chỉ có 22% case
> đã đóng. Và **38/150 case (25%) có 0 mẩu dùng được.** Case đóng lâu hơn không giàu
> tri thức hơn; nó nghèo hơn.
>
> ⚠ **Corpus mới mang thêm bí mật, gồm một loại chưa từng thấy.** Phép kiểm bắt 14 chỗ:
> một **JWT token thật** (`ES-338386#comment-789479`), `Pass: KKlai123`
> (`ES-342304`), `pass: 92255` (`ES-343712`), `pass: 17106` (`ES-346584`), và 7 ID
> Ultraviewer. JWT là loại `AR-j` chưa liệt kê — nó không có hình dạng số nào cả.
>
> ✅ **Luật che đã được ĐO recall, không chỉ được viết.** Dùng danh sách 13 credential
> đã xác định tay trong `make_fixture.py` làm đáp án: luật theo-DÒNG bắt **11%**, luật
> theo-NGỮ-CẢNH bản đầu bắt **23%**, bản sửa theo BỐN HÌNH DẠNG thật bắt **92%** (12/13)
> với **0 lần ăn nhầm** dữ liệu cần giữ. ⚠ Con số 92% đo trên CHÍNH corpus mà luật được
> sửa theo, nên nó là **cận TRÊN**, không phải ước lượng đúng cho corpus khác.
> → Một luật che chưa đo recall là một luật tạo ra sự an tâm giả.
>
> 🛑 **CREDENTIAL KHÔNG PHẢI CA CÁ BIỆT — NÓ LÀ MẪU HÀNH VI.** Luật che chạy trên
> corpus 150 case bắt **40 chỗ trên 18/345 mẩu = 5,2%**, tức **cứ ~19 mẩu evidence có
> một mẩu chứa credential**. Phần lớn là CẶP ID + mật khẩu Ultraviewer, gửi thành hai
> tin nhắn liên tiếp: `214 250 580`/`17106` · `23 996 795`/`01551` ·
> `110 143 652`/`13333` · `79 053 771`/`0279` · `111 444 028`/`86846` và 10 cặp nữa.
> Cộng hai JWT token và `Pass: KKlai123`.
> → Đây không phải một sự cố của `ES-346481` mà là **cách support team làm việc**: xin
>   Ultraviewer để remote vào máy khách. `AR-j` vì thế không giải được bằng cách xử lý
>   vài mẩu — nó cần một luật chạy mỗi lần nạp. Đã có: `check_corpus.py`.
>
> ✅ **Và một false positive đã tìm ra + sửa, ghi lại vì nó là loại dễ bỏ qua nhất:**
> `...&table_id=23&id=2469&area=4` bị bắt vì nhãn `ID` khớp `id=` trong URL. Che nó đi
> thì mất đúng đường dẫn người duyệt cần để mở lại màn hình đang lỗi. Sửa bằng cách bỏ
> URL trước khi quét; recall giữ nguyên 100%, corpus 150 case giảm 48 → 40 chỗ.
>
> ⚠ **Và script export có một lỗ đã sửa:** kéo 150 issue là ~380 request liên tiếp; một
> timeout ở request thứ ~380 làm **mất trắng 6 phút và 328 mẩu đã đọc xong**, vì hàm gọi
> HTTP `die()` ngay. Giờ có retry với backoff, và một issue lỗi thì BỎ QUA issue đó rồi
> nói ra + đếm, thay vì mất cả lô. Chạy lại: **150/150 issue, 0 bỏ qua.**
>
> ---
>
> **VIỆC KẾ TIẾP theo đúng thứ tự ba quyết định trên:** (1) người dùng chạy JQL 12 tháng
> chủ đề hoá đơn — có sẵn trong `scripts/jira-export/jira-config.example.bat`;
> (2) thiết kế sub-tenant rồi sinh migration; (3) đếm nguyên nhân trên corpus mới.
> FTS (`AR-h`) lùi lại SAU phép đếm — đó chính là điều quyết định 3 nói.
>
> ✅ **QUYẾT ĐỊNH 1 ĐÃ THỰC HIỆN XONG cùng ngày.** 32 case + 128 evidence đã nằm trong
> `kp_dev`, credential thật thay bằng giá trị GIẢ GIỮ NGUYÊN HÌNH DẠNG (69 chỗ, xem
> `scripts/jira-export/make_fixture.py`) — giữ hình dạng để luật che vẫn test được, bỏ
> giá trị để không nhân bản bí mật. Chạy lại lần hai: tạo mới 0, idempotent xác nhận.
> ⚠ `kp_dev` còn 33 case rác từ curl/Postman các phiên trước (`crm:` `zalo:` `test:` và
> `jira:<guid>`). KHÔNG xoá — lọc fixture bằng `SourceReference LIKE 'jira:ES-%'`.
>
> 🛑 **VÀ FIXTURE THẬT BẮT NGAY BUG THỨ HAI — `IM-24`.** Lần nạp đầu tiên trả **500**:
> Npgsql từ chối `DateTimeOffset` có offset khác 0, mà Jira Server trả `+07:00`. Một đầu
> vào **hợp lệ theo ISO 8601** bị báo thành lỗi máy chủ. Đã sửa bằng value converter ở
> `ConfigureConventions` (một chỗ, áp cho mọi entity kể cả entity chưa ai viết).
> ⚠ Điều đáng nhớ hơn cái bug: **vì sao 103 test không thấy nó.** Mọi mốc thời gian
> trong bộ test đều do chính bộ test dựng ra, và tay người viết test thì luôn viết UTC.
> Một bộ test tự cấp vật liệu cho mình chỉ kiểm được hình dạng mà người viết nghĩ ra.
> → **105 test** (48 domain + 15 hạ tầng + 42 API), 2 test mới đã chứng minh biết ĐỎ.

> **Cập nhật:** 2026-09-01 — 🛑 **TÌM RA LỖ FAIL-OPEN TRONG CHÍNH `RlsGuard`, ĐÃ SỬA.**
> Guard bản cũ chỉ hỏi *"bảng này có policy nào không"*, không hỏi policy đó **nói gì**.
> Policy PostgreSQL gộp bằng **OR**, nên thêm một policy `USING (true)` là khách A đọc
> được dữ liệu khách B — **trong khi guard vẫn báo xanh**. Đã đo thật, câu SQL tái hiện
> ở `07` §3 `IM-22`. Sinh `IM-22`, `IM-23`; `AR-d` ✅ ĐÓNG 2026-09-03 (người dùng xác
> nhận). Cùng ngày: hết trùng số hiệu — câu "chuỗi kết nối DB" đổi thành `AR-i`.
> **103 test** (48 domain + 15 hạ tầng + 40 API), và 5 phép đột biến chứng minh cả 5
> luật mới của guard đều biết ĐỎ.
> ⚠ Lỗ nằm bên trong cơ chế được dựng riêng để chống rò rỉ. Đó là điều đáng nhớ nhất:
> một cơ chế canh gác cũng cần có người canh nó.
> ⚠ Sinh `AR-h` — **bốn ràng buộc đã đo cho FTS**, ba trong bốn đi ngược trực giác.
> Nặng nhất: **RLS giết index GIN** (toán tử `@@` không leakproof). Đọc trước khi
> build bước (c), kẻo làm xong mới biết index vô dụng.
> ⚠ Cả ba thiết kế do workflow sinh ra đều bị phản biện trả `needs_revision` — **không
> cái nào được ship nguyên trạng**. Chỉ phần `AR-d` được viết lại tay rồi mới code.
>
> **Trước đó:** 2026-08-30 — ✅ **ĐÃ CÓ ĐƯỜNG NẠP NỘI DUNG.** `POST /signals/case-evidence`
> chạy được: một Case giờ mang được comment, ghi chú xử lý, email — thứ mà Path A thật
> sự gom. `AR-f` CHỐT: endpoint riêng, link case **nhận null** (`K-B9`). Sinh `IM-19`..
> `IM-21` và `AR-g`. **97 test** (48 domain + 9 hạ tầng + 40 API), và đã chứng minh biết
> đỏ bằng 5 phép đột biến — trong đó hai phép tách bạch được HAI TẦNG chống trùng:
> bỏ kiểm-trước-khi-ghi mà giữ unique index thì VẪN XANH, bỏ cả hai thì ĐỎ.
> ✅ Bộ Postman lên **23 request / 59 assertion**, chạy thật bằng newman: 0 đỏ.
> ⚠ CHƯA COMMIT — người dùng chọn "chỉ sửa docs, chưa commit".
>
> **Cùng ngày, trước đó:** ⚠️ **PHÁT HIỆN LỖ TRONG KẾ HOẠCH: `evidence_item` KHÔNG CÓ
> ĐƯỜNG GHI NÀO.** Toàn bộ codebase chỉ có một dòng chạm tới nó — khai báo `DbSet`.
> Nghĩa là một `canonical_case` hôm nay là **một dòng chữ**: subject + khoá nguồn +
> hai mốc thời gian, không có comment, không có cách xử lý, không có kết quả.
> Hệ quả: `S8` đòi bản nháp gom mang theo một **phân bố** (*"14/20 case đã làm bước
> này"*), mà con số đó KHÔNG suy ra được từ 20 cái tiêu đề. `06` §5 cũng đã ghi ý
> định rõ: *"1M context → Path A: 20 case **+ evidence** trong MỘT request"*.
> ⚠ Lỗ này KHÔNG nằm trong danh sách §4 "Chưa build" của `07` — nó là chỗ kế hoạch
> bỏ sót, không phải việc đã biết mà chưa tới lượt. Sinh `AR-f`.
> → Thứ tự đã sửa: **nạp evidence** trước, rồi bạn xuất Jira thật, rồi mới FTS.
> ✅ **MÁY NÀY ĐÃ CÓ POSTGRESQL.** 81/81 test xanh (48 domain + 9 hạ tầng + 24 API).
> ✅ Có bộ test API Postman: `scripts/postman/` — 13 request, đã gọi thật vào app đang
> chạy trước khi đóng gói. ⚠ CHƯA COMMIT (untracked).
>
> **Trước đó:** 2026-08-25 buổi 2 — ✅ **LUẬT DOMAIN ĐÃ CÓ TEST RIÊNG, KHÔNG CẦN DB.**
> Sinh `IM-18`. **48 test mới**, chạy 77ms trên máy CHƯA cài PostgreSQL, và đã chứng
> minh biết đỏ bằng 5 phép đột biến vào `src/`. Tổng: **81 test** (48 domain + 33 cần DB).
> ⚠ Phát hiện khi chuyển máy: trước đó **100% test cắm vào PostgreSQL** — luật sinh
> ra từ 23 quyết định của Workstream 04 (`V1` `V3` `S7` `M2`) chưa từng được kiểm lần
> nào. Con số đó vô hình trên máy cũ vì mọi thứ đều xanh. Xem `07` §9.
> ⚠ Lúc đó máy chưa có PostgreSQL → 33 test kia không chạy được. **Đã hết từ 2026-08-30.**
>
> **Trước đó, cùng ngày buổi 1:** ✅ **ĐÃ CÓ KÊNH 1** (đường nhận tín hiệu). Ô "tìm hoặc
> tạo Case" của sơ đồ luồng CHẠY ĐƯỢC: phần mềm của khách gọi vào, hệ thống tạo Case,
> và tín hiệu gửi lại KHÔNG sinh Case trùng (hai lớp: kiểm trước khi ghi + unique
> index có TenantId). Sinh `IM-15`..`IM-17`. **33 test.**
> ⚠ Thứ tự build đã đổi có chủ ý: Kênh 1 làm trước hai ô còn lại của Path A. Lập luận
> cũ trong README không bị xoá — nó vẫn đúng về Kênh 2, nhưng bỏ sót việc Path A cần
> case cũ mà trước Kênh 1 thì không có đường nào đưa case vào.
> ⚠ Endpoint tín hiệu là endpoint GHI nên có chốt riêng (`Ingest:SignalApiKey`, thiếu
> là không start được). Khoá dùng chung KHÔNG phải câu trả lời cho `AR-e`.
> **Trước đó:** 2026-08-24 buổi 2 — ✅ **ĐÃ CÓ PROJECT HOST.** Ranh giới tenant giờ
> sống được trong một **request HTTP thật**, ở CẢ HAI chế độ deploy của `G13`
> (dedicated lấy tenant từ cấu hình · shared lấy từ header). Cấu hình sai là
> **không start được** — 4 ca đã kiểm. 20 test xanh ở hai tầng, và đã chứng minh
> biết đỏ (gỡ interceptor khỏi host → 4 test API đỏ).
> Sinh `IM-12`..`IM-14` và `AR-e` (chế độ shared chưa có xác thực — KHÔNG chặn
> khách hàng #0, vì bản dedicated lấy tenant từ cấu hình).
> **Cùng ngày, buổi 1:** ✅ **RANH GIỚI TENANT ĐÃ ĐÓNG MỘT VÒNG TRÊN DB THẬT.**
> Máy đã có PostgreSQL 18.6. Migration apply thật, `RlsGuard` chạy thật, và có
> **test project đầu tiên** — 9/9 xanh, đã chứng minh biết đỏ. `AR-c` ĐÓNG.
> Sinh 3 quyết định mới `IM-9`..`IM-11` → `docs/07_MVP_IMPLEMENTATION.md` §3.
> Hai thứ chỉ chạy thật mới thấy: policy văng lỗi khi biến session là chuỗi rỗng,
> và **superuser đi vòng qua RLS kể cả khi có FORCE** (làm bộ đo tự hỏng).
> **Trước đó:** 2026-08-23 buổi 7 — ✅ **CÔNG NGHỆ ĐÃ CHỐT.** Workstream 06 đóng
> (`AR1`-`AR5`, `G13`) → `docs/06_MVP_ARCHITECTURE.md`. Stack: **C#/.NET + PostgreSQL**.
> Tiếp theo là **Workstream 07 — MVP Implementation**, và đây là lúc được viết code.
> **Cùng ngày:** buổi 6 — ✅ **DOMAIN MODELING KẾT THÚC.** Workstream 05
> (Process Model v0.1, PR1-PR4) đóng.
> **Cùng ngày:** buổi 5 — ✅ **WORKSTREAM 04 ĐÓNG** (Step 5 chốt, V1-V5; §6.9 đóng)
> **Cùng ngày:** buổi 4 — **Step 4 CHỐT** → `04` §3C (AP1-AP4)
> **Cùng ngày:** buổi 3 — **Step 3 CHỐT** → `04` §3B (L1-L4); H-8 đã sửa
> **Cùng ngày:** buổi 2 — **Q-E CHỐT** → `docs/02_SUCCESS_METRICS_V1.md` (M1-M4)
> **Cùng ngày:** buổi 1 — housekeeping H-1..H-7 đóng hết
>
> 📌 **Đọc nhanh Knowledge Model:** `04` §3C.5 — hình dạng đầy đủ của một
> `KnowledgeRecord` sau cả bốn Step, mọi dòng trỏ về một quyết định đã chốt.
> **Trước đó:** 2026-08-22 (G12 chốt, §8.2 sang phiên bản nhẹ — §2.4)
> **Trước đó:** 2026-08-21 buổi 2 (D6 chốt, Step 1 + Step 2 CHỐT, §8.1 đã chạy)
> **Mục đích:** File này là điểm vào cho phiên làm việc tiếp theo. Đọc file này TRƯỚC, rồi mới đọc các tài liệu khác.
> **Dành cho:** AI Agent hoặc người mới tiếp tục project, kể cả trên máy khác.

---

---

# TL;DR — đọc 30 giây này trước

```text
DỰ ÁN     AI Operational Knowledge & Process Platform
          Sản phẩm ĐỂ BÁN (D1), multi-tenant từ ngày đầu.
          Bản build đầu: engine gợi ý quy trình + tri thức, nhúng được (D2).

STAGE     ✅ DOMAIN MODELING KẾT THÚC 2026-08-23
          ✅ Workstream 04 — Knowledge Model v0.1  (23 quyết định)
          ✅ Workstream 05 — Process Model v0.1    (4 quyết định PR1-PR4)
          ✅ Workstream 06 — MVP Architecture      (5 quyết định AR1-AR5)
             CÔNG NGHỆ ĐÃ CHỐT: C#/.NET + PostgreSQL + blob storage
          🔵 Workstream 07 — MVP Implementation  ← ĐANG LÀM, CÓ CODE
             slice đầu: Path A. Nền móng đã build, build sạch 0 warning.
             → src/ · tests/ · nhật ký quyết định: docs/07_MVP_IMPLEMENTATION.md
             ✅ RLS ĐÃ kiểm trên PostgreSQL 18.6 thật (2026-08-24) — AR-c đóng
             ✅ ĐÃ CÓ PROJECT HOST (KnowledgePlatform.Api) — cả hai chế độ G13
             ✅ ĐÃ CÓ KÊNH 1 — tín hiệu vào, tạo Case, idempotent (2026-08-25)
             ✅ LUẬT DOMAIN ĐÃ CÓ TEST — không cần DB (2026-08-25 buổi 2) ← IM-18
                81 test: 48 domain (chạy ở đâu cũng được, 77ms)
                       + 33 cần PostgreSQL thật (9 DB + 11 HTTP + 13 Kênh 1)
                cả hai bộ đã chứng minh biết ĐỎ, không chỉ biết xanh

          ★ 04 §3C.5  hình dạng đầy đủ của một KnowledgeRecord
          ★ 04 §3D.7  bảng từ vựng ĐÃ KHÓA — tham chiếu duy nhất
          ★ 05 §5     hình dạng đầy đủ ProcessDefinition / ProcessRun
          ★ 06 §8     decision register (stack + 5 quyết định)
          ★ 06 §10    6 ràng buộc dễ sai nhất, mang sang Workstream 07

          Ngoài workstream 04:
          Success Metrics (Q-E)            ✅ CHỐT 2026-08-23
                                           → 02_SUCCESS_METRICS_V1.md

LỊCH SỬ   "CHƯA CODE" đúng cho tới hết Workstream 06. Chốt công nghệ là quyền
          của người dùng — AGENT.md §10.1. Từ Workstream 07 thì được code.
```

## Ba con số phải nhớ

```text
10 / 30 / 60   SOP có và tìm được 10% · trong đầu người 30% · rải rác 60%
               → Capability 1 (retrieval) ngày đầu gần như không có gì để trả

5 bước         Quy trình THẬT của first use case (§8.1-KQ):
               Kibana → response → tài liệu → issue cũ → ĐƯA RA KẾT LUẬN
               Tuyến tính, KHÔNG nhánh. Giá trị nằm trọn ở bước cuối,
               và đó là bước duy nhất không ai ghi lại.

5-10  ❌ ĐÃ BỊ BÁC 2026-09-04. Đo thật trên 150 case: ~19 nhóm (18-30) cho MỘT
               chủ đề, và đó là CẬN DƯỚI. Xem docs/09_RK4_DEM_NGUYEN_NHAN.md.
               Giữ dòng dưới đây vì nó là GIẢ ĐỊNH CŨ mà cả 04 §3.5 đứng trên —
               đọc nó như lịch sử, không như sự thật hiện hành.
5-10           Số loại nguyên nhân của first use case.  ⚠ n=1, chưa xác nhận.
               → kho tri thức ~10 record, không phải 500.
               → toàn bộ 04 §3.5 đứng trên con số này. Xem R-K4.
```

## Việc tiếp theo

> 🛑 **ĐỌC KHỐI NÀY, ĐỪNG ĐỌC PHẦN NGAY DƯỚI NÓ.** Cập nhật 2026-09-05.
> Phần `text` phía dưới là **bản của 2026-08-30** và đã chết: nó ghi việc (a)/(b) là
> "việc kế tiếp" trong khi cả hai đã xong, và (c) FTS đã bị lùi lại sau phép đếm.
> Giữ nguyên vì lập luận trong đó vẫn giải thích được **vì sao** thứ tự từng là như vậy —
> nhưng đừng làm theo nó.
>
> **Ba câu ĐANG CHỜ NGƯỜI DÙNG QUYẾT:**
> ```text
> AR-l   sub-tenant — BỊ CHẶN. Jira không có trường nào dùng được (`Mã khách sạn`
>        = -1.0 ở 32/32 case). Hai nhánh đều NGOÀI code: ezCloud điền trường đó
>        cho thật, hoặc lùi quyết định và ghi rõ ranh giới khách sạn CHƯA được
>        thực thi. → 07 §5 AR-l
> R-K4   Q2 — có mở phép đếm sang case CÒN MỞ và bắt ghi nguyên nhân lúc remote
>        không? Đây là việc DUY NHẤT làm con số 41% tiến lên; đọc thêm case đã
>        đóng thì không, vì tỉ lệ đang xấu đi. → docs/09 §7
> AR-k   luật gán nhãn thay cho `machineReadability` hằng số. → 07 §5 AR-k
> ```
>
> **Việc LÀM ĐƯỢC NGAY, không chờ ai:** dựng **cây quyết định có bước kiểm** cho nhóm
> SOP lớn nhất (*Phân quyền & ký hiệu hoá đơn*, 10 case). Nguyên liệu: `docs/ket-qua-phan-tich/`.
> ⚠ Nhưng biết trước hai chỗ thiếu: (1) repo có 10 mã case + nguyên nhân một dòng nhưng
> **KHÔNG có bước kiểm nào** — mà bước kiểm chính là thứ cây quyết định phải chứa;
> (2) bằng chứng then chốt `ES-346396` phải kéo lại từ Jira (JQL trong
> `jira-config.example.bat` giờ dùng `resolved >= -365d` nên nó lọt vào cửa sổ).
>
> **Đã xong trong hai ngày 2026-09-04/05** (đừng làm lại):
> ```text
> ✅ (a) nạp evidence · (b) xuất Jira thật — cả hai XONG
> ✅ R-K4 ĐÃ ĐẾM: ~19 nhóm/chủ đề, giả định 5-10 bị bác  → docs/09
> ✅ phép thử retrieval ĐÃ CHẠY: AUC 0,61, FTS 34% vs đoán mù 31%
> ⚠ (c) FTS LÙI LẠI SAU phép đếm — và phép đếm cho thấy retrieval theo văn bản
>       không phải cơ chế chính trên nguồn này. Xem 07 §5 R-K4 trước khi build.
> ```
>
> 📌 **Tài liệu sinh ra trong hai ngày đó, chưa có trong danh sách đọc ở §1:**
> `docs/09_RK4_DEM_NGUYEN_NHAN.md` (phép đếm nguyên nhân) ·
> `docs/10_CHUYEN_MAY.md` (dựng lại trên máy khác) ·
> `docs/ket-qua-phan-tich/` (taxonomy + nguyên nhân 150 case) ·
> `scripts/workflows/` (định nghĩa 3 workflow đã chạy).


```text
1  Workstream 07 — tiếp slice Path A. Nền móng, host VÀ Kênh 1 ĐÃ XONG (07 §2).

   ⚠ THỨ TỰ ĐÃ SỬA 2026-08-30. Bản cũ ghi việc kế tiếp là TRUY VẤN "tìm N case cũ
     liên quan". Thứ tự đó đúng nhưng thiếu một mắt xích: `evidence_item` chưa có
     đường ghi, nên FTS sẽ tìm trên tiêu đề 1 dòng và Path A không gom được gì.
     Lập luận cũ KHÔNG bị xoá — nó vẫn đúng rằng FTS là dependency của Path A;
     nó chỉ bỏ sót việc case rỗng nội dung thì tìm được cũng không dùng được.

   a  ✅ XONG 2026-08-30 — NẠP EVIDENCE VÀO KÊNH 1.
      POST /signals/case-evidence · AR-f chốt · 16 test · đã gọi thật.
   b  ⭐ VIỆC CỦA BẠN, LÀ VIỆC KẾ TIẾP: xuất issue OTA thật từ Jira KÈM COMMENT
      rồi đẩy vào Kênh 1. Hai lần gọi, theo thứ tự:
        1. POST /signals/case-observed   ← issue (idempotent, gửi lại vô hại)
        2. POST /signals/case-evidence   ← comment, trỏ về issue qua sourceReference
      Trần mặc định 500 mỗi lô cho mỗi đường. Xem nhóm E của bộ Postman để lấy mẫu
      body đúng. Mở khoá §8.2 (n = 50-200 thay vì đếm tay 20) và AR4-b.
      ✅ SCRIPT ĐÃ CÓ 2026-09-03: scripts/jira-export/export_jira_to_channel1.py
      (Python, chỉ stdlib; Jira Server/DC — PAT qua Bearer, rơi về Basic).
      Việc của bạn thu lại còn: đặt JIRA_BASE_URL + JIRA_PAT + JIRA_JQL, chạy
      --dry-run soi hai file JSON, rồi chạy thật khi app đang chạy local.
      Cách dùng đầy đủ nằm ở docstring đầu script. Script tự chuẩn hoá timestamp
      +0700 → +07:00 (Jira Server trả dạng .NET không đọc được — đã thử và vá).
   c  TRUY VẤN "tìm N case cũ liên quan" — tune trên corpus thật, không phải case bịa.
      AR4: Postgres full-text search TRƯỚC, pgvector khi ĐO ĐƯỢC là không đủ.
      ⚠ ĐỌC `AR-h` (07 §5) TRƯỚC KHI BUILD. Bốn ràng buộc đã đo, và cái nặng nhất là
        RLS giết index GIN — làm xong mới biết index vô dụng thì mất công hai lần.
      ⚠ Chưa có tsvector, chưa có GIN index. Index duy nhất trên canonical_case là
        (TenantId, SourceResolvedAt) — index theo TRẠNG THÁI/THỜI GIAN, dù comment
        ngay phía trên nó trong AppDbContext ghi "index để tìm theo chủ đề".
        Comment và code đang lệch nhau ở đó. Bước (c) phải sinh migration mới.
   d  Sau đó: ISoạnNhápSOP gọi Anthropic SDK · luồng duyệt (S7) · diff(A,B) cho M2.

   ✅ MÁY NÀY ĐÃ CÓ POSTGRESQL (kiểm 2026-08-30). 81/81 test xanh:
     dotnet test src/KnowledgePlatform.slnx
     48 domain (124ms) + 9 hạ tầng (691ms) + 24 API (1s).

   ⚠ Dựng DB local một lần:  psql -U postgres -f scripts/dev-db-setup.sql
     KHÔNG chạy app hay test bằng role superuser — superuser đi vòng qua RLS,
     mọi test cách ly tenant sẽ PASS GIẢ. Test đầu trong bộ test kiểm điều này.

   ⚠ AR-e MỚI: chế độ shared multi-tenant chưa có xác thực nên nó TỪ CHỐI KHỞI
     ĐỘNG trừ khi được thừa nhận tường minh. Cần quyết ở tầng sản phẩm (API key
     theo tenant? mTLS? chữ ký trên payload?). KHÔNG chặn khách hàng #0 — bản
     deploy dedicated lấy tenant từ cấu hình, không từ người gọi.

2  §8.2  ĐẾM CASE OTA — bản nhẹ, ~30 phút, VIỆC CỦA BẠN. Chạy song song.
         Luật quyết định đã chốt TRƯỚC khi đếm (≤15 / ≥40 / ở giữa).
         ⚠ Nó quyết định BÀI TOÁN 1 (khớp bằng chứng với ~10 nguyên nhân),
           KHÔNG quyết định bài toán 2 (tìm tài liệu) — xem R-A1 ở 04 §3.5.
         Công dụng: 04 §3.5 · baseline M4a · 20 nhãn eval · spec §17 mục 21.

3  AR4-b  Đếm khách thực tế có bao nhiêu tài liệu, loại gì. Cùng kiểu §8.2.
          Quyết định khi nào Postgres FTS không đủ và cần pgvector.

✅ CÔNG NGHỆ ĐÃ CHỐT 2026-08-23 → docs/06_MVP_ARCHITECTURE.md (AR1-AR5)
   C#/.NET · PostgreSQL · blob storage · SDK chính thức + interface mỏng
   claude-opus-5 · eval Python riêng · widget Vue3+TS · G13 hai chế độ deploy
✅ DOMAIN MODELING KẾT THÚC 2026-08-23
   Workstream 04 — 23 quyết định · Workstream 05 — 4 quyết định (PR1-PR4)
   Step 3,4,5 của 04 và toàn bộ 05 KHÔNG sinh entity mới nào.
   §6.9 (vocabulary song song) ĐÓNG → bảng khóa duy nhất ở 04 §3D.7.
   Mốc §6.7 (~04/09) đạt sớm: cả hai model xong 23/08.
✅ Q-E ĐÃ CHỐT → 02_SUCCESS_METRICS_V1.md (M1-M4). QM-1 (NGƯỠNG) vẫn OPEN
   → có thước đo mà chưa có ngưỡng thì chưa có điều kiện dừng thật sự.
✅ H-1..H-9 housekeeping ĐÃ ĐÓNG HẾT. Xem §9.
```

## Cách làm việc mà người dùng đã yêu cầu

```text
· Hỏi từng quyết định qua FORM để tích chọn, không liệt kê rồi chờ trả lời bằng chữ
· Phản biện TRƯỚC khi đề xuất. Không chỉ đồng ý.
· Ngôn ngữ dễ hiểu, tránh thuật ngữ không cần thiết
· Ghi quyết định xuống tài liệu NGAY khi chốt — dự án đã từng mất tài liệu 02
  cùng toàn bộ Success Metrics mà không ai phát hiện
· Không tự chuyển PROPOSED → CONFIRMED, không tự đóng OPEN QUESTION
```

## Tổng số quyết định đã chốt trong workstream 04

```text
Step 1   S1-S8, K-B9, Q-B, Q-C     11 quyết định   → 04 §1, §4
Step 2   T1-T4                      4 quyết định   → 04 §3, §4
Step 3   L1-L4                      4 quyết định   → 04 §3B, §4
Step 4   AP1-AP4                    4 quyết định   → 04 §3C, §4
Step 5   V1-V5                      5 quyết định   → 04 §3D, §4
WS 05    PR1-PR4                    4 quyết định   → 05 §6
WS 06    AR1-AR5 (+G13)             5 quyết định   → 06 §8   ← CÔNG NGHỆ
Nền      D1-D6, G1-G12              → AGENT.md §3, §4, §4B
Metrics  M1-M4 (Q-E)                4 quyết định   → 02_SUCCESS_METRICS_V1.md §5
```

# 0. ĐANG CHUYỂN MÁY? → `docs/10_CHUYEN_MAY.md`

Bốn thứ KHÔNG nằm trong git và sẽ mất khi đổi máy: corpus Jira, `jira-config.bat` (có
PAT), database `kp_dev`, và **bộ nhớ của agent**. File `10` nói cách dựng lại từng cái,
và đã **chép sẵn phần bộ nhớ thuộc về dự án** vào chính nó — đó là lý do file đó tồn tại.

⚠ Kết quả hai workflow của 2026-09-04/05 (~3,7 triệu token, chạy lại tốn hàng giờ) ĐÃ
được đưa vào repo ở `docs/ket-qua-phan-tich/` nên KHÔNG mất.

---

# 1. Đọc gì, theo thứ tự nào

```text
1. docs/00_CURRENT_STATE.md          ← file này. Trạng thái hiện tại + việc đang làm
2. AGENT.md                          ← cách agent phải làm việc trong project
3. docs/PROJECT_CONTEXT.md           ← Discovery + Vision (consolidated 2026-08-18)
4. docs/Canonical Case Model v0.2.md ← Domain Model đã chốt
5. docs/04_KNOWLEDGE_MODEL_V0.1.md   ← Knowledge Boundary + Concepts (Step 1, 2)
6. docs/02_SUCCESS_METRICS_V1.md     ← Success Metrics đã chốt (Q-E, M1-M4)
```

## ⚠️ Cảnh báo về tài liệu

**`docs/02_PRODUCT_FOUNDATION_V1.md` KHÔNG TỒN TẠI.**

`AGENT.md` §1 yêu cầu đọc ba tài liệu theo tên `01_` / `02_` / `03_`. Thực tế:

| AGENT.md nói | File thật | Trạng thái |
|---|---|---|
| `docs/01_PROJECT_CONTEXT.md` | `docs/PROJECT_CONTEXT.md` | ✅ có, khác tên |
| `docs/02_PRODUCT_FOUNDATION_V1.md` | — | ❌ **MISSING** |
| `docs/03_CANONICAL_CASE_MODEL_V0.2.md` | `docs/Canonical Case Model v0.2.md` | ✅ có, khác tên |

`docs/archive/NEXT_CONVERSATION_PROMPT_02_INPUT.md` là **prompt đầu vào** của conversation 02, **không phải** output. Nó chứa phiên bản **cũ** của MVP Capability #3 → đừng đọc nó như quyết định hiện hành. Đã archive 2026-08-23 (`H-6`); tên cũ là `docs/NEXT_CONVERSATION_PROMPT (1).md`.

**Hệ quả của việc mất tài liệu 02 — đã đỡ một phần 2026-08-23:**

```text
Success Metrics             ✅ ĐÃ LẤY LẠI  → docs/02_SUCCESS_METRICS_V1.md
                                              (Q-E RESOLVED, M1-M4)
capability contract chi tiết ❌ VẪN MẤT
non-goals                    ❌ VẪN MẤT
lý do + evidence của 02      ❌ VẪN MẤT
```

⚠️ Success Metrics được **dựng lại**, không phải tìm lại — nó là quyết định mới ngày 2026-08-23, không phải bản gốc của tài liệu 02. Và `QM-1` (ngưỡng cụ thể) vẫn `OPEN`: **có thước đo mà chưa có ngưỡng thì vẫn chưa có điều kiện dừng, chỉ mới có cách nhìn.**

---

# 2. Quyết định đã chốt

## 2.1 Từ trước (đã có trong AGENT.md / tài liệu cũ) — `CONFIRMED`

```text
Primary Persona      New Support Employee
Secondary Persona    Technical / L3
First Use Case       OTA Booking Not Received Assistance

3 MVP Value Capabilities:
  1. Contextual Knowledge Retrieval
  2. Process Guidance
  3. Assistance Outcome & Knowledge Capture
```

Guardrails nền tảng (chi tiết trong AGENT.md §3):

```text
G1  Jira là connector, không phải product boundary
G2  Case ≠ Knowledge ≠ Process
G3  FACT ≠ AI INFERENCE
G4  Unknown là dữ liệu hạng nhất
G5  Timeline hơn Snapshot
G6  Provenance là nền tảng (Origin ≠ Verification)
G7  Security / Tenant boundary là nền tảng
G8  Không dùng numeric LLM confidence như truth
G9  Không tự thêm MVP Capability #4, #5
G10 Không tự chuyển PROPOSED → CONFIRMED, không tự đóng OPEN QUESTION
G11 Không làm PHỒNG TO một capability đã chốt          (S1, 2026-08-21)
G12 Tỉ trọng tri thức của khách là THAM SỐ, không phải
    hằng số thiết kế; thứ tự bật capability là cấu hình (2026-08-22, §2.4)
```

Canonical Case Model v0.2 đã chốt ở mức Domain Modeling — xem `docs/Canonical Case Model v0.2.md` §17 Decision Register.

## 2.2 Chốt mới trong phiên 2026-08-20/21 — `CONFIRMED`

Bốn quyết định mới, do người dùng xác nhận trực tiếp:

### D1 — Đây là sản phẩm để BÁN, không phải tool nội bộ
Multi-tenant là yêu cầu ngày đầu, không phải bảo hiểm cho tương lai.

Tri thức đến từ nhiều nguồn: tài liệu công ty tự nạp, Jira, email, **tài liệu AI tự tìm được trong source code (nếu được phân quyền)**, tài liệu có phân quyền nội bộ.

Doanh nghiệp **không dùng Jira** vẫn phải dùng được. Ví dụ đã nêu: nhúng vào CRM — mỗi khi user kéo deal sang stage mới, AI gợi ý bước tiếp theo hoặc cách xử lý để tăng tỉ lệ won.

### D2 — Bản build đầu tiên: (3) engine gợi ý quy trình + tri thức, nhúng được
Chọn trong 4 hướng đã nêu:

```text
(1) Nền tảng tri thức có phân quyền, khách tự nạp tài liệu
(2) AI đọc source code rút ra tri thức                    → v2, nhạy cảm quyền nhất
(3) Engine gợi ý quy trình + tri thức, nhúng được          ← ĐÃ CHỌN
(4) Case intelligence trên dữ liệu vận hành
```

Lý do chọn (3): phổ quát nhất (chạy cho cả support và sales); là phần **mạnh lên** khi model mạnh lên; khó bị vendor thương mại hóa nhất.

### D3 — Khách hàng #0 là công ty của người dùng
Test nội bộ trước. Nhưng **tenant boundary có trong mô hình dữ liệu từ ngày đầu** — nhồi vào sau rất đắt.

Lưu ý: pitch là "nhúng vào CRM/helpdesk của khách", nhưng host app đầu tiên là hệ thống support của chính công ty. Interface nhúng nên được thiết kế generic ngay, dù mới có một host.

### D4 — Nguyên tắc tự chủ của AI
```text
AI tự ĐỀ XUẤT tri thức mới                  →  CÓ
AI tự CÔNG NHẬN thành tri thức chính thức   →  KHÔNG
Mở dần theo eval, không theo model confidence
```

Nhất quán với guardrail đã có: `OBSERVED PATTERN → AI DRAFT → HUMAN REVIEW → VERIFIED KNOWLEDGE`, và Autonomy Level 1→5 trong PROJECT_CONTEXT §5.8.

### D5 — Nguyên tắc "model mạnh lên thì phần mềm mạnh lên" — `CONFIRMED` (người dùng nêu, agent đề xuất cách áp dụng)

Mọi thứ build ra phải phân loại được:

| Tài sản bền — model mạnh lên thì giá trị hơn | Giàn giáo tạm — model mạnh lên thì thành nợ |
|---|---|
| Dữ liệu đã kết nối + phân quyền | Prompt phức tạp nhiều tầng |
| Provenance | Pipeline cắt chunk, template extraction |
| Lịch sử outcome (cái gì đã giúp) | Luật if-else bù model yếu |
| Bộ eval | Multi-agent dựng để bù reasoning yếu |
| Connector / tích hợp | Cơ chế sửa lỗi model thủ công |
| Trạng thái quy trình | |

**Câu hỏi test cho mọi feature:** *"Nếu sang năm có model mạnh gấp 10, cái này thành giá trị hơn hay thành rác?"*

Bốn hệ quả:
1. **Bộ eval là first-class, không phải phase sau.** Eval là *cơ chế* biến "model mạnh lên" thành "phần mềm mạnh lên". Không eval → đổi model là đánh cược. Cần có từ MVP dù chỉ 20 case gán nhãn tay.
2. **Không đưa giới hạn hôm nay vào domain model** (chunk size, context limit, embedding dimension = tham số hạ tầng).
3. **Nút cổ chai sẽ dịch từ "AI có hiểu không" sang "AI có được phép xem không".** Model càng mạnh, retrieval càng thành commodity → lợi thế dồn về permission + provenance + outcome. Đây là lý do mạnh nhất để coi phần đó là cốt lõi sản phẩm.
4. **Nâng năng lực bằng policy, không bằng rewrite.** Mức tự chủ là cấu hình theo action; model mạnh lên = nâng level cho action đã có eval chứng minh.

---

## 2.3 Chốt trong phiên 2026-08-21 (buổi 2) — `CONFIRMED`

### D6 — "Gom nhiều case cũ thành một SOP theo yêu cầu" NẰM TRONG Capability 3 của MVP

Người dùng xác nhận trực tiếp. Đây là Open Question `Q-A` — quyết định duy nhất đang chặn Step 1 của Workstream 04. Toàn bộ phân tích dẫn tới câu hỏi này vẫn được giữ nguyên ở §4 (không rewrite history).

Chọn **phiên bản nhẹ** đã đề xuất ở §4 — *"gom theo yêu cầu"*, không phải *"tự đào quy luật"*.

**Phạm vi chính xác — cột phải là guardrail, không phải mô tả:**

```text
THUỘC D6                                    KHÔNG THUỘC D6
người nói "tôi cần SOP cho chủ đề X"        AI tự quyết chủ đề nào cần SOP
kéo một tập CÓ GIỚI HẠN case liên quan      quét toàn bộ corpus tìm quy luật mới
AI soạn nháp → người sửa → người duyệt      AI tự công nhận (vi phạm D4)
một hành động, có điểm đầu và điểm cuối     job chạy nền liên tục
```

Cột phải chính là `Process Discovery` (PROJECT_CONTEXT §7.2) + `Knowledge Gap Detection` (§8.2) — đã có nhãn *future capability* ở `PROJECT_CONTEXT.md` §17. D6 **không** kéo chúng vào MVP, **không** tạo Capability #4 → giữ G9.

> ⚠️ **Cảnh báo trôi phạm vi.** Sẽ có lúc ai đó đề nghị *"hay là mình tự động phát hiện chủ đề nào cần SOP luôn"*. Câu đó nghe rất hợp lý và nó là cột phải. Ghi xuống đây để lần sau có chỗ đối chiếu.

**Ba hệ quả đã ghi nhận:**

**(1) `Q-C` co lại — không còn là câu hỏi tốn kém.**
Để gom N case cũ thành SOP, hệ thống buộc phải tìm được N case cũ liên quan. Nên:

```text
cỗ máy "tìm case cũ tương tự"  →  giờ là DEPENDENCY của Capability 3
                                  không còn là lựa chọn của Capability 1
```

Q-C chuyển từ *"có build không?"* (đắt) thành *"có bày ra cho người dùng thấy không?"* (gần như miễn phí, vì phần đắt đã trả rồi).

**(2) D6 là bánh đà của D5 — điểm này chưa từng được ghi.**
`D5 hệ quả 1` đòi bộ eval là first-class, nhưng gán nhãn tay thì không ai muốn làm. D6 giải quyết miễn phí:

```text
AI soạn nháp SOP     →  bản A
người sửa và duyệt   →  bản B
diff(A, B)           →  đúng cái nhãn eval cần
```

Mỗi lần dùng tính năng sinh ra một cặp *(nháp, bản người sửa)* — tín hiệu chất lượng cao nhất có thể có, vì nó ghi lại **con người sửa gì**. Sinh ra bởi hành vi dùng sản phẩm, không phải bởi một phase gán nhãn riêng.

→ D6 không chỉ là một feature. Nó là **cơ chế** biến D5 từ nguyên tắc thành thứ chạy được.

**(3) D6 là điều kiện để D2 dùng được ở khách hàng đầu tiên.**
Engine gợi ý quy trình (D2) không có gì để gợi ý nếu khách chưa có quy trình nào trong hệ thống. Với 10% (§3), engine đó gần như im lặng ngày đầu. D6 là thứ nạp đạn cho nó. Đã kiểm chứng ở cả vertical thứ hai: rule *"khách ở Negotiation im hơn 7 ngày → gửi case study cùng ngành"* cũng được suy ra từ các deal won/lost — cùng cơ chế, khác tên.

---

## 2.4 Chốt trong phiên 2026-08-22 — `CONFIRMED`

Xuất phát từ một câu phản biện của người dùng: *"Sao phải đếm 20 case OTA nhỉ. Mình nghĩ nên làm linh hoạt để công ty tự thao tác và thấy lựa chọn tốt nhất cho công ty mình. Có thể MVP tốt do phù hợp với công ty hiện tại nhưng mang sang công ty khác thì tỉ trọng % SOP sẽ khác."*

### Chỗ lẫn đã được tách ra — ghi lại vì nó sẽ lặp lại

Hai con số bị gộp làm một trong cách tài liệu đang trình bày:

```text
10/30/60            tỉ trọng SOP của MỘT công ty
                    → ĐÚNG là khác nhau giữa các công ty.  Đây là G12.

"5-10 nguyên nhân"  một loại vấn đề có bao nhiêu nguyên nhân có thể?
                    → câu hỏi về CẤU TRÚC của vấn đề, không về công ty.
                    → đây là thứ §8.2 đo, và là thứ §3.5 của file 04 đứng trên.
```

§8.2 **không** đo tỉ trọng SOP. Nó trả lời câu: *tập nguyên nhân của một loại vấn đề là hữu hạn nhỏ hay mở?* Câu đó quyết định một thứ đắt:

```text
hữu hạn nhỏ (~10)  →  Capability 1 = bài toán PHÂN LOẠI. Không dựng vector DB/RAG.
mở, hàng trăm      →  đúng là bài toán tìm kiếm. Phải dựng cả stack đó.
```

Chọn sai hướng này tạo ra đúng thứ `D5` gọi là **giàn giáo tạm**.

### Ba quyết định

**(1) `G12` — tỉ trọng tri thức là tham số của khách, không phải hằng số thiết kế.** Chốt ở **dạng rộng**: gồm cả *"thứ tự bật capability là cấu hình theo từng khách"*. Toàn văn: `AGENT.md` §3.9. Đây là đóng góp thật của câu phản biện — `D5 hệ quả 2` trước đó chỉ nói về giới hạn hạ tầng (chunk size, context limit), **chưa** nói về phân bố dữ liệu của khách.

**(2) §8.2 chuyển sang phiên bản nhẹ, có luật quyết định chốt TRƯỚC.** Xem §8.2 đã viết lại.

**(3) *"Công ty tự đo trạng thái tri thức của mình"* = future capability.** Ghi vào `PROJECT_CONTEXT.md` §17 (mục 21). **Không** vào MVP, **không** nhét vào Capability 3 — đó đúng là cột phải của `G11`, và §2.3 đã cảnh báo trước về dạng đề nghị này.

### Hai điều đã ghi nhận, không phải quyết định

**§8.2 KHÔNG chặn Step 3.** Tài liệu gọi nó là *"việc giá trị cao nhất còn lại"* — đó là nói về **giá trị**, không phải **chặn**. Theo quy tắc §6.7 (*"câu hỏi nào không chặn việc build thì ghi vào Open Questions và đi tiếp"*), Step 3 chạy được ngay. Trước phiên này cách trình bày ở TL;DR dễ bị đọc thành cổng chặn — đã sửa.

**Rủi ro "MVP chỉ vừa với khách #0" nhỏ hơn vẻ ngoài.** Thứ tự vòng lặp *"gom trước, tìm sau"* ở §3 vẫn mang nhãn `PROPOSED, chưa chốt` — nó chưa bao giờ được đóng thành thiết kế. G12 giờ khoá luôn khả năng nó bị đóng thành hằng số.

---

## 2.5 Chốt trong phiên 2026-08-23 — `CONFIRMED` · `Q-E` ĐÃ GIẢI

Toàn văn: **`docs/02_SUCCESS_METRICS_V1.md`**. Đây là bản nén.

```text
M1   Success Metric ≠ Eval metric
     ứng viên "% chỉ đúng nguyên nhân" → chuyển sang bộ EVAL
M2   TẦNG 0 = Success Metric CHÍNH tháng đầu
     số nháp Path A được duyệt + mức sửa diff(A,B) + tỉ lệ bỏ giữa đường
M3   TẦNG 1 = chuỗi 4 bước ĐẦY ĐỦ
     hiện ra → được mở → được chấp nhận → có mặt trong KẾT LUẬN
M4a  TẦNG 2 = "% escalate oan", vai trò LAGGING, kèm 3 cảnh báo
M4b  "độ phủ nguyên nhân" = leading indicator NỘI BỘ TENANT (G12)
```

### Bốn phản biện dẫn tới bộ metric này — ghi lại vì chúng dễ bị quên

**(1) Cả ba ứng viên gốc đều KHÔNG đo được ở tháng 1.** Ngày đầu có **0 KnowledgeRecord** (không có SOP viết + `S6`). Nên: ứng viên 1 undefined, ứng viên 3 là `0/mẫu số` chưa biết, ứng viên 2 không có baseline. Nhưng tháng đầu là lúc cần biết nhất *có nên tiếp tục hay không* → sinh ra Tầng 0.

**(2) Ứng viên 1 là eval metric bị xếp nhầm chỗ.** Failure mode cụ thể: *chỉ đúng 90% nhưng không ai xem gợi ý → Eval = 0.9, giá trị = 0, và ta ship.*

**(3) Không ứng viên nào dùng phân biệt đã CONFIRMED `Knowledge Retrieved ≠ Knowledge Used`** (`AGENT.md` §6). Đáng chú ý hơn: `P8` trong problem taxonomy **đã** ghi sẵn giải pháp (*AssistanceAttempt · knowledge used · accept/reject · outcome*) mà ba ứng viên không có cái nào → sinh ra Tầng 1.

**(4) `G12` vừa chốt hôm trước loại ứng viên 3 khỏi vai trò thước đo sản phẩm.** Mẫu số *"tổng nguyên nhân đã biết"* là đặc điểm của một khách; khách B có thể có 200 nguyên nhân → không port được.

### Hai điều đáng ghi

**Q-E được làm TRƯỚC Step 3 có lý do kỹ thuật, không phải tùy hứng.** `AssistanceAttempt` là cái máy ghi lại thước đo. Thiết kế máy ghi trước khi biết cần đo gì thì phải thiết kế hai lần. `02_SUCCESS_METRICS_V1.md` §4 đã viết ra **metric đòi dữ liệu gì** — đó là đầu vào trực tiếp cho Step 3.

**Tầng 2 đòi "trạng thái kho tri thức tại một thời điểm quá khứ"** — đúng guardrail `G5` (Timeline over Snapshot). Guardrail cũ trả cổ tức lần nữa, không phát sinh yêu cầu mới.

### ⚠️ Q-E chưa đóng hoàn toàn

`QM-1` — **ngưỡng** cụ thể của từng thước đo — vẫn `OPEN`. Đặt ngưỡng bằng cách đoán bây giờ thì tệ hơn không đặt; phải chạy thật vài tuần.

> Có thước đo mà không có ngưỡng thì **chưa có điều kiện dừng** — chỉ mới có **cách nhìn**. Đó là tiến bộ thật nhưng chưa đủ.

---

# 3. Con số quan trọng nhất: 10 / 30 / 60

Người dùng cung cấp 2026-08-21. Thực tế SOP tại công ty:

```text
SOP có, tìm được (Drive/Confluence/Zalo)              10%
SOP chỉ nằm trong đầu người                           30%
SOP rải rác: vài comment Jira, một email, ghi chú     60%
```

## Hệ quả: MVP đang xếp ngược so với dữ liệu

Nếu build retrieval trước, **9/10 lần hệ thống trả về không có gì.**

Phần 60% là chỗ AI có lợi thế lớn nhất, vì:
- fragments đã tồn tại trong Jira/email, máy đọc được, đã có quyền truy cập
- AI giỏi thật ở việc gom mảnh rời thành bản nháp
- người chỉ phải sửa và xác nhận, không phải viết từ đầu
- đầu ra là SOP → nạp ngược vào Capability 1 và 2, biến 60% thành 10%

## Thứ tự vòng lặp đề xuất — `PROPOSED`, chưa chốt

Vẫn đúng 3 capability đã chốt (không vi phạm G9), chỉ đổi **điểm vào**:

```text
Gom tri thức rải rác   →  tạo ra SOP đầu tiên        (Cap 3, phần capture)
Tìm đúng tri thức      →  tìm ra SOP vừa tạo          (Cap 1)
Dẫn từng bước          →  dẫn theo SOP đó             (Cap 2)
Ghi lại sau mỗi case   →  SOP tự dày lên              (Cap 3, phần còn lại)
```

## Đây là điểm bán, không phải điểm yếu

> Hầu hết sản phẩm knowledge **giả định khách đã có tài liệu**. Khách không có thì mua về không dùng được.
> Sản phẩm này **không cần khách có sẵn tài liệu** — nó tạo ra tài liệu từ dữ liệu vận hành của khách.

Đó là câu trả lời cho cold start, và khó copy. Nên đưa vào pitch.

## Tính năng "đề xuất công ty bổ sung SOP" — không phải Capability #4

Người dùng đề nghị thêm tính năng này. Agent đề xuất: đây **không** phải capability mới, mà là **cách trả lời trung thực của Capability 1 khi thất bại** — chính là guardrail G4 (Unknown là hạng nhất) áp vào bề mặt sản phẩm:

```text
Chưa có tài liệu về việc này.

Nhưng tôi tìm thấy 14 case tương tự đã được xử lý.
→ Soạn bản nháp SOP từ 14 case đó?          [Soạn thử]
→ Hoặc tải lên tài liệu nếu công ty đã có.   [Tải lên]
```

Nó chuyển thẳng người dùng sang Capability 3 → vòng lặp tự khép.

---

# 4. Quyết định đã chặn Step 1 — nay ĐÃ GIẢI

> ## ✅ "Gom nhiều case cũ thành một SOP theo yêu cầu" có nằm trong Capability 3 của MVP không?

Tình trạng: **`RESOLVED 2026-08-21` — CÓ, ở phiên bản "gom theo yêu cầu".** Xem `D6` §2.3.

Phần còn lại của §4 là **phân tích lịch sử dẫn tới quyết định** — giữ nguyên, không rewrite. Đọc để hiểu *vì sao* D6 được chốt như vậy, đặc biệt là mismatch giữa hai tài liệu và hai nhánh hệ quả.

## Vì sao đây là câu hỏi thật, không phải chi tiết

Có một mismatch chưa được giải quyết giữa hai tài liệu:

```text
PROJECT_CONTEXT.md §16 (cũ)                 AGENT.md §4 (mới)
"Knowledge/Process Draft         vs         "Assistance Outcome
 from Operational Data"                      & Knowledge Capture"
 status: PROPOSED                            status: CONFIRMED
```

Đây không phải đổi tên — hai capability khác bản chất:

| | Bản cũ | Bản mới (đã chốt) |
|---|---|---|
| Nguồn | corpus lịch sử, N cases | case đang xử lý, 1 case |
| Thời điểm | offline / retrospective | inline / lúc đóng case |
| Kỹ thuật | pattern mining, clustering | hỏi + draft + confirm |
| Vấn đề giải | P4 + P2 | P2 + P8 |

Áp rule recency của AGENT.md §1 → bản mới thắng. Có corroboration độc lập: `Canonical Case Model v0.2.md` §11.3 viết *"hỗ trợ Eval và Capability #3"* khi nói về `AssistanceAttempt` → doc v0.2 đã hiểu Capability #3 theo nghĩa mới.

**NHƯNG** con số 60% (§3) làm câu hỏi này **mở lại theo hướng ngược**: nếu không có SOP để retrieve, việc đầu tiên sản phẩm phải làm là *tạo ra* SOP.

## Phiên bản nhẹ mà agent đề xuất — `PROPOSED`

**"Gom theo yêu cầu"** thay vì **"tự đào quy luật"**:

| Bản cũ (đã bỏ) | Bản đề xuất |
|---|---|
| AI tự đào 500 case, tự phát hiện quy luật chưa ai biết | Người nói *"tôi cần SOP cho case OTA booking không về"* |
| Không giới hạn phạm vi, giống bài toán ML mở | AI kéo ~20 case liên quan + email + comment đúng chủ đề đó |
| Không biết khi nào xong | AI soạn bản nháp → người sửa → xong |
| Đắt, rủi ro cao | Rẻ, có ranh giới, người điều khiển |

Cùng giá trị, khoảng 10% công sức, không có bài toán ML vô hạn ở giữa.

## Hai nhánh hệ quả

```text
NẾU CÓ  → Step 1 phải định nghĩa rõ trạng thái
          "tri thức được gom từ nhiều nguồn rời, chưa ai duyệt"
          vì đó là 60% thực tế.

NẾU KHÔNG → MVP chỉ phục vụ được 10% trường hợp.
            Phải nói rõ điều này với team ngay từ đầu
            để không ai kỳ vọng sai.
```

Agent nghiêng về **CÓ**, ở phiên bản "gom theo yêu cầu". Nhưng đây là quyết định về phạm vi MVP — thuộc quyền người dùng.

---

# 5. Step 1 — Define Knowledge Boundary  (✅ ĐÃ CHỐT — kết quả ở docs/04_KNOWLEDGE_MODEL_V0.1.md)

Workstream: `04 — Knowledge Model v0.1`. Stage: Domain Modeling.

**Chỉ làm Step 1.** Không thiết kế toàn bộ Knowledge Model. Không schema, không C# entity, không REST API, không vector DB, không embedding, không RAG, không frontend, không microservices, không production architecture.

Ưu tiên: `Business meaning → Domain boundary → Concepts → Relationships → Provenance → Lifecycle → Rules`

## 5.1 Câu hỏi trung tâm

> **Knowledge là gì và không phải là gì trong product này?**

Phải phân biệt được: Knowledge · Evidence · Case · Historical Case · Document · Process · Technical Finding · AI-generated Knowledge Draft

## 5.2 Định nghĩa ứng viên — `PROPOSED`

> **Knowledge = một phát biểu tái sử dụng được về việc thế giới nghiệp vụ vận hành thế nào, hoặc một loại tình huống nên được xử lý thế nào; được tổ chức chấp nhận ở một mức verification xác định; có applicability scope; và tồn tại độc lập với bất kỳ Case cụ thể nào.**

Từ khoá: **reusable · class-level · organizationally accepted · case-independent**

## 5.3 Discriminator Test — `PROPOSED`

Mục đích: tranh luận "cái này có phải Knowledge không" được giải quyết bằng **test**, không bằng ý kiến.

```text
T1  CASE-INDEPENDENCE
    Xóa Case đã sinh ra nó → phát biểu còn giá trị không?
    Không → dữ liệu của Case, không phải Knowledge.

T2  CLASS-LEVEL
    Nó nói về một LỚP tình huống, hay một instance?
    "Booking ABC123 không về vì parser lỗi"           → instance → CaseClaim
    "OTA payload dạng X sẽ bị parser < v2.3 drop"     → class    → Knowledge candidate

T3  ORGANIZATIONAL ACCEPTANCE
    Đã có ai/quy trình nào chấp nhận nó ở một mức nào chưa?
    Chưa → Draft.

T4  DECISION VALUE
    Nó giúp quyết định / hành động / giải thích?
    Không → đúng nhưng vô dụng; không persist.

Pass cả 4 → Knowledge.
```

## 5.4 Bốn chiều của Knowledge — `PROPOSED`

Đã cập nhật sau quyết định D1 (phân quyền + đa nguồn):

```text
Applicability  — áp dụng cho tình huống nào (phiên bản, khách hàng, hệ thống nào)
Authority      — ai nói, được ai chấp nhận, ở mức nào
Visibility     — ai được phép thấy                              ← MỚI, do D1
Derivation     — sinh ra từ đâu                                 ← quan trọng hơn dự kiến
```

## 5.5 Bảng ranh giới 8 concept — `PROPOSED`

| Concept | Trả lời | Scope | Case-independent? | Có verification lifecycle? | Là Knowledge? |
|---|---|---|---|---|---|
| **Case** | Việc gì đang được xử lý? | 1 instance | ✗ | ✗ (có case lifecycle) | **Không** — `CONFIRMED` v0.2 |
| **Historical Case** | Việc gì đã từng được xử lý? | 1 past instance | ✗ | ✗ | **Không** → precedent/evidence |
| **Evidence** | Ta quan sát được gì? | 1 observation | ✓ (nhiều nơi cite được) | ✗ (có integrity/availability) | **Không** — `CONFIRMED` v0.2 §9 |
| **Document** | Tri thức được ghi ở đâu? | container | ✓ | ✗ (có version của file) | **Không** → carrier/source |
| **Technical Finding** | Cơ chế kỹ thuật nào giải thích? | thường 1 case | ✗ khi mới sinh | chỉ sau khi generalize | **Chưa** → CaseClaim |
| **Process** | Việc nên xử lý thế nào? | 1 lớp công việc, có step + state | ✓ | ✓ | **Không** → domain riêng (05) |
| **AI Knowledge Draft** | AI đề xuất tổ chức biết gì? | candidate | ✓ | ở state DRAFT | **Chưa** |
| **Knowledge** | Tổ chức biết gì, áp dụng được? | 1 lớp tình huống | ✓ | ✓ | **Có** |

## 5.6 Bảy boundary claim — accept/reject từng cái, đều đang `PROPOSED`

**K-B1 · Knowledge là case-independent.** Case không bao giờ *trở thành* Knowledge. Historical Case dù hữu ích vẫn là precedent/evidence.
→ Hệ quả cần quyết: Capability #1 khi đó trả về **hai loại object khác nhau** (KnowledgeRecord + Historical Case). PROJECT_CONTEXT §15 Layer B liệt kê chúng tách riêng. MVP coi là một loại kết quả hay hai?

**K-B2 · Document ≠ Knowledge.** Document là carrier có identity/version/access riêng; Knowledge **cite** Document. Chống rủi ro biến Knowledge Model thành Document Management Model.

**K-B3 · Evidence ≠ Knowledge.** Evidence gắn với một thời điểm và một nguồn; Knowledge là phát biểu class-level. Evidence **support/refute** Knowledge — soi chiếu quan hệ `Evidence → Claim` đã CONFIRMED ở v0.2.

**K-B4 · Technical Finding chỉ là CaseClaim cho tới khi được generalize + accept.** Bước generalization là **một quyết định**, không phải promotion tự động. Bảo vệ G3.

**K-B5 · AI Knowledge Draft không phải Knowledge.** Origin = `AI_INFERENCE` giữ **vĩnh viễn** kể cả sau khi human verify (v0.2 §7.4).
→ Câu hỏi để Step 2/3: Draft là **state của KnowledgeRecord** hay **entity riêng**? Agent nghiêng về *state* (giữ continuity provenance draft→verified như G6 yêu cầu) + policy filter ở retrieval.

**K-B6 · Process ≠ Knowledge — cần rule phân định, không chỉ tuyên bố.** Rule đề xuất:
```text
Có ordered/conditional STEP + theo dõi được "đang ở bước nào" cho từng Case  →  PROCESS
Là assertion / explanation / applicability, không có per-case execution state →  KNOWLEDGE
Một SOP document có thể là SOURCE cho cả hai.
```
Đây là claim agent ít tự tin nhất và **quan trọng nhất** — vì SOP của first use case chính là một step list. Với D2 (chọn engine gợi ý quy trình) thì claim này càng load-bearing.

**K-B7 · Tri thức trong đầu người chưa phải Knowledge của hệ thống.** PROJECT_CONTEXT §5.2 liệt kê "Human knowledge / senior memory" dưới mục KNOWLEDGE — đó là **knowledge source**, không phải KnowledgeRecord. Chỉ vào model sau khi externalize. **Đặc biệt quan trọng vì 30% SOP nằm trong đầu người.**

**Negative list — Knowledge KHÔNG bao giờ là:** chat/comment log · audit trail · `AssistanceAttempt` telemetry · metric/analytics · giá trị field của một Case · embedding/index artifact (infrastructure) · phát biểu của khách hàng.

## 5.7 Câu hỏi khó nhất của Step 1 — sinh ra từ D1

> **Tri thức mà AI tổng hợp từ nguồn bị giới hạn quyền thì ai được xem?**

Ví dụ cụ thể: AI được cấp quyền đọc private repo, suy ra *"bản dưới 2.3 bỏ qua payload OTA dạng X"*.

```text
Support xem được không?     họ không có quyền vào repo
Cho xem   → đã rò rỉ thông tin từ nguồn giới hạn chưa?
Không cho → tri thức vô dụng, vì đúng người cần lại không thấy
```

Đây **không** phải câu hỏi hạ tầng. Nó quyết định **"Knowledge" là một loại hay hai loại** — và nếu làm sai, sản phẩm bán ra sẽ bị khách doanh nghiệp chặn ở vòng security review.

**Hướng đề xuất** (`PROPOSED`): tách **kết luận** khỏi **dẫn chứng**. Kết luận có thể có visibility rộng hơn nguồn sinh ra nó — nhưng việc mở rộng phải là **hành động tường minh của người có quyền**, có ghi nhận ai quyết định và khi nào. Không bao giờ là mặc định của hệ thống.

Cùng khuôn với `AI DRAFT → HUMAN REVIEW` đã chốt (D4), chỉ áp cho *quyền xem* thay vì cho *nội dung*.

## 5.8 Trình tự chạy Step 1

```text
1  Accept/reject/sửa định nghĩa §5.2
2  Accept/reject/sửa discriminator test §5.3
3  Đi qua K-B1 → K-B7, chốt từng claim một
4  Chốt §5.7 (câu hỏi quyền xem) — hoặc ghi nhận là OPEN nếu chưa đủ thông tin
5  Stress-test bằng First Use Case OTA booking:
     - SOP "booking không về PMS"                  → Knowledge? Process? cả hai?
     - "Parser < v2.3 drop payload dạng X"         → Knowledge? Technical Finding?
     - Jira ES-123 đã fix cùng vấn đề              → Knowledge? Precedent?
     - Screenshot log khách gửi                    → Evidence
6  Stress-test bằng ba thực tế 10/30/60:
     - 10%  SOP có, tìm được         → Document → Knowledge?
     - 30%  SOP trong đầu người      → K-B7
     - 60%  SOP rải rác fragments    → trạng thái "gom từ nhiều nguồn, chưa duyệt"
7  Stress-test non-Jira: SOP .docx trên Drive + email hướng dẫn của senior
8  Stress-test vertical thứ hai: ví dụ CRM deal (xem §6.6)
9  Ghi kết quả vào docs/04_KNOWLEDGE_MODEL_V0.1.md §1, giữ nhãn + Open Questions
     → chỉ tạo file SAU khi Step 1 được người dùng chốt
10 Sang Step 2 (Knowledge Concepts & Granularity)
```

---

## 5.9 Bảy quyết định để đóng Step 1 — **TẤT CẢ ĐÃ `CONFIRMED 2026-08-21`**

> ✅ **S1–S7 đã được người dùng chốt, cộng thêm S8, K-B9, Q-B, Q-C.**
> Kết quả chính thức nằm ở **`docs/04_KNOWLEDGE_MODEL_V0.1.md` §1** — đó là source of truth.
> Phần dưới đây giữ nguyên **lập luận** dẫn tới từng quyết định (không rewrite history).
> Người dùng chốt **đúng phương án đề xuất** ở cả 9 câu.

Sinh ra từ phiên 2026-08-21 buổi 2, sau khi D6 được chốt.

### S1 — Guardrail phạm vi D6
Ghi xuống rằng D6 giới hạn ở *"gom theo yêu cầu"*; *"tự phát hiện chủ đề cần SOP"* là future capability, không phải MVP. → đã ghi ở §2.3, cần xác nhận là guardrail chính thức.

### S2 — Sửa định nghĩa Knowledge ở §5.2
Định nghĩa hiện tại đặt *"được tổ chức chấp nhận"* làm **điều kiện vào cửa**. Hệ quả: 60% fragment, bản nháp AI, email senior chưa duyệt — **tất cả nằm ngoài mô hình tri thức của chính sản phẩm**, và không có chỗ nào để đặt chúng.

Chẩn đoán: §5.2 đang định nghĩa **state `VERIFIED KNOWLEDGE`**, không phải **domain Knowledge**.

Đề xuất sửa:

> **Knowledge = một phát biểu tái sử dụng được, ở mức LỚP tình huống (không phải một case cụ thể), về việc nghiệp vụ vận hành thế nào hoặc một loại tình huống nên được xử lý thế nào — có applicability scope, có provenance, và có một mức verification (bao gồm cả mức "chưa ai duyệt"); tồn tại độc lập với bất kỳ Case cụ thể nào.**

Thay đổi duy nhất: *"được chấp nhận"* chuyển từ **điều kiện vào cửa** → **một trạng thái trên timeline**.

Hệ quả: trả lời luôn `Q-J` theo hướng **state**, đúng như K-B5 đã nghiêng, giữ được provenance liên tục draft→verified như G6 đòi.

### S3 — Sửa Discriminator Test §5.3
Hai lỗi kỹ thuật:

```text
T1 CASE-INDEPENDENCE  →  quyết định CÓ PHẢI Knowledge          (discriminator)
T2 CLASS-LEVEL        →  quyết định CÓ PHẢI Knowledge          (discriminator)
T3 ORG ACCEPTANCE     →  quyết định Ở TRẠNG THÁI NÀO           (không phải discriminator)
T4 DECISION VALUE     →  quyết định CÓ ĐÁNG ƯU TIÊN            (không phải discriminator)
```

- Câu *"Pass cả 4 → Knowledge"* **tự mâu thuẫn với T3** (T3 đã ghi "Chưa → Draft", tức vẫn trong model). Sửa thành **2 test biên giới + 2 phép phân loại**.
- T4 hiện ghi *"không persist"* → **vi phạm tinh thần G4 + G6**. Một ứng viên bị loại chính nó là dữ liệu: nó chỉ ra khoảng trống ở chủ đề đó, và nó là tín hiệu eval (AI đề xuất gì mà người từ chối). Sửa **"không persist"** → **"không PROMOTE"**. Từ chối là một **quyết định được ghi lại**, không phải một phép xóa.

### S4 — Kernel dùng chung cho ba domain
`CaseClaim` v0.2 **đã** có Origin + Verification + Evidence. Nên bốn chiều ở §5.4 không phải phát minh của Knowledge Model.

```text
              ┌──────────────────────────────────────────┐
              │  KERNEL DÙNG CHUNG                       │
              │  Origin · Evidence · Verification level  │
              │  Applicability · Visibility · Authority  │
              └──────────────────────────────────────────┘
                   ▲              ▲               ▲
              CaseClaim      KnowledgeRecord   ProcessDefinition
              (đã có v0.2)    (Step 1-5)        (Workstream 05)
```

**Danh sách bước có MỘT nhà duy nhất: Process domain**, ở state DRAFT khi mới được AI gom. Không có bản sao thứ hai.

Vì sao cần: đầu ra của D6 là một SOP = danh sách bước. Áp rule K-B6 thì **cái đầu tiên MVP tạo ra lại thuộc Process domain**, trong khi ta đang làm Knowledge Model. Kernel dùng chung làm mâu thuẫn đó biến mất thay vì phải chọn bên.

Ba cái lợi:
1. Không phải chọn giữa "nhận thua" và "bẻ K-B6".
2. **Giải luôn §6.9** — một bộ *verification level* dùng chung + *lifecycle state* riêng từng domain → `VERIFIED` thôi trùng nghĩa.
3. Knowledge Model v0.1 nhỏ lại đáng kể → đúng §6.7.

Cái giá: Knowledge Model v0.1 và Process Model v0.1 **không thể làm tuần tự hoàn toàn** nữa; chúng chia nhau một mối nối. Mối nối đó tồn tại thật, không phải do ta thiết kế ra.

Kỷ luật từ vựng đi kèm: *"SOP"* = tài liệu con người đọc (carrier); *"ProcessDefinition"* = thứ hệ thống dùng để dẫn từng bước. Nếu team cứ gọi cả hai là "SOP", hai domain sẽ lại nhập nhèm.

### S5 — `K-B8`: Capability 3 có hai nửa, ở hai domain khác nhau
Tài liệu đang gộp hai thứ có **kinh tế học hoàn toàn khác nhau**:

```text
PATH A — KÉO (pull), do người yêu cầu
  N case  →  1 bản nháp SOP/Knowledge
  Tần suất: mỗi chủ đề một lần
  Ngân sách chú ý: PHÚT — người ta chủ động xin, sẵn sàng bỏ công
  Domain đầu ra: Knowledge (+ Process nếu là danh sách bước)

PATH B — ĐẨY (push), hệ thống nhắc lúc đóng case
  1 case  →  một mảnh ghi nhận
  Tần suất: 500 lần
  Ngân sách chú ý: GIÂY — người ta không xin, đang muốn đóng case
  Domain đầu ra: KHÔNG PHẢI Knowledge
```

**Điểm then chốt: Path B không tạo ra Knowledge.** Nó chỉ làm dày hồ sơ Case (`CaseAction` / `CaseClaim` / `CaseOutcome` — v0.2 đã có sẵn hết), để **sau này Path A gom được**.

Nếu chấp nhận:
- **§6.4 hết mâu thuẫn.** Ràng buộc 20 giây áp cho Path B (xác nhận tóm tắt case của *chính mình*), không áp cho Path A (duyệt một SOP 9 bước tốn 30 phút là hợp lý — người ta chủ động xin nó).
- Knowledge domain **sạch**: chỉ chứa phát biểu class-level, không có sọt "fragment chưa xử lý".
- Nguy cơ *"Capability 3 chính là cái field `Version đang sử dụng` mặc áo đẹp hơn"* (§6.4) bị chặn đúng chỗ: cái field đó là Path B, và Path B giờ chỉ được phép hỏi những gì **đã có** trong case.

### S6 — Nạp tài liệu KHÔNG tự sinh KnowledgeRecord
`D5` đã trả lời gián tiếp: *"pipeline cắt chunk, template extraction"* nằm ở cột **GIÀN GIÁO TẠM**. Một pipeline bóc tách .docx thành KnowledgeRecord là đúng cái sẽ thành nợ khi model mạnh lên.

```text
nạp tài liệu     →  tạo Document (carrier) + nội dung đọc được
                    KHÔNG tự tạo KnowledgeRecord

KnowledgeRecord  →  chỉ sinh ra khi có một HÀNH VI KHẲNG ĐỊNH:
                    người viết ra, hoặc Path A gom rồi người duyệt
```

> **KnowledgeRecord lưu những gì tổ chức đã KHẲNG ĐỊNH, không lưu tất cả những gì tổ chức CÓ.** Phần "có" nằm ở Document.

Làm Knowledge Model nhỏ lại rất nhiều, và nhất quán với D5 thay vì chống lại D5.

### S7 — Quy tắc visibility cho MVP (thu hẹp `Q-D`, không giải)
`D2` đã đẩy ví dụ đáng sợ nhất của §5.7 (AI đọc private repo) sang **v2** → bớt phần nóng nhất. Nhưng vấn đề vẫn sống ở dạng nhẹ: một comment trong Jira project nội bộ mà Support không xem được, gom vào SOP → SOP đó ai xem được?

Quy tắc duy nhất đề xuất cho MVP:

```text
Mặc định:  visibility của tri thức tổng hợp = HẸP NHẤT trong các nguồn của nó
Mở rộng:   là một HÀNH VI TƯỜNG MINH của người thấy được TẤT CẢ nguồn
           ghi lại: ai mở, khi nào, mở từ đâu tới đâu
Không bao giờ: hệ thống tự mở
```

Cùng khuôn với `D4`, chỉ áp cho *quyền xem* thay vì *nội dung*.

Model chỉ cần ba chỗ chứa — quyết định hôm nay, rất rẻ:
```text
1. visibility của bản thân KnowledgeRecord
2. visibility của từng nguồn chống lưng nó
3. ai mở rộng, khi nào, lý do
```

Phần khó (bôi đen chọn lọc, tách kết luận khỏi dẫn chứng ở mức từng câu) → **v2**, `Q-D` giữ nguyên `OPEN`.

**⚠️ Hệ quả bất lợi phải nói thẳng:** với quy tắc "hẹp nhất", **60% sẽ tệ đi trước khi tốt lên**. Một SOP gom từ 20 case Jira, nếu có một comment nội bộ, sẽ **vô hình với đúng bạn support mới cần nó**.

→ Bước **mở quyền xem phải là một bước hiển thị rõ trong luồng Capability 3**, không phải một trang cấu hình admin nhớ ra sau. Nếu để sau, tính năng sẽ "chạy được" trong demo và "im lặng vô dụng" trong thực tế.

→ Ràng buộc về người duyệt:
```text
duyệt nội dung   →  cần người GIỎI NGHIỆP VỤ      (senior support / L3)
duyệt quyền xem  →  cần người THẤY ĐƯỢC MỌI NGUỒN
```
Hai người khác nhau → tắc. Ép một người không đủ quyền → rò rỉ. **Đề xuất MVP:** người duyệt phải là người thấy được tất cả nguồn; duyệt nội dung + quyền xem trong **một hành động**; log cả hai. Thu hẹp đáng kể `Q-G`.

---

### S8 — Cấu trúc bản nháp gom · `CONFIRMED`
Sinh ra từ stress-test 60% (§5.10). Bản nháp gom từ N case mang theo một **phân bố**, không phải một phát biểu → **evidence link ở mức TỪNG PHÁT BIỂU** + **`CONFLICTING` bắt buộc**. Chi tiết: `04_KNOWLEDGE_MODEL_V0.1.md` §1.11.

### K-B9 — Evidence trỏ trực tiếp vào Knowledge · `CONFIRMED`
Chi tiết: `04_KNOWLEDGE_MODEL_V0.1.md` §1.6.

---

## 5.10 Kết quả stress-test Step 1 — `CONFIRMED`, sinh ra 2026-08-21 buổi 2

### First Use Case — OTA booking không về PMS

| Vật thể | Phân loại | Ghi chú |
|---|---|---|
| SOP "booking không về PMS" | **Document** (carrier) + **ProcessDefinition** (phần bước) + **Knowledge** (phần không phải bước, vd *"không có incoming log → lỗi phía OTA"*) | Ba thứ, không phải một. Đúng như K-B6 dự đoán. Đây là câu trả lời đề xuất cho `Q-B`. |
| *"Parser < v2.3 drop payload dạng X"* | **Knowledge** — ví dụ sạch nhất | class-level ✓, case-independent ✓, có giá trị quyết định ✓. `applicability` = khoảng version. Cũng là vật thể có vấn đề quyền xem ở §5.7. |
| Jira ES-123 đã fix cùng vấn đề | **Historical Case** → precedent/evidence | Không phải Knowledge (K-B1). Là **nguyên liệu** của D6. |
| Screenshot log khách gửi | **Evidence**, machine readability = thấp | Đúng trạng thái `KNOWLEDGE_EXISTS_NOT_RETRIEVABLE` ở §6.3. |

SOP tách thành ba vật thể là **kết quả đúng**, không phải dấu hiệu model sai. Nhưng nó dẫn thẳng tới S6.

### Ba thực tế 10 / 30 / 60

- **10%** — xem `S6`. Tài liệu ở lại dạng Document; KnowledgeRecord chỉ sinh khi có hành vi khẳng định.
- **30%** — `K-B7` áp trực tiếp: chưa vào model. Đường vào **duy nhất** khả thi là **Path B** (`S5`). Nên §6.4 không phải lời khuyên UX — **nó là điều kiện để 30% tồn tại trong sản phẩm.**
- **60%** — **kết quả tốt: không cần entity mới nào.**

```text
comment Jira, action, outcome  →  v0.2 đã có (CaseClaim/CaseAction/CaseOutcome/Evidence)
email, ghi chú Zalo rời        →  Document/SourceReference → Evidence   (cần K-B9)
"gom lại"                      →  không phải entity, mà là một TRUY VẤN + một HÀNH VI
```

60% không đòi thêm concept. Nó đòi **một truy vấn tìm case liên quan** + **một hành vi tổng hợp**. Đầu tư vào Case v0.2 trả cổ tức lần nữa.

### ⚠️ Chỗ đang bị bỏ sót — quan trọng

Một bản nháp gom từ 20 case **khác về bản chất** với bản nháp viết từ 1 case. Bản gom mang theo một **phân bố**, không phải một phát biểu:

```text
bước "kiểm tra room mapping"        →  14/20 case đã làm
bước "gọi OTA trước khi check log"  →  6/20 làm, 8/20 làm ngược lại    ← XUNG ĐỘT
```

Nếu bản nháp được lưu như một khối văn bản với danh sách nguồn ở cuối, **ta ném đi đúng thứ giá trị nhất**: bước nào được bao nhiêu case chống lưng, và chỗ nào các case **không đồng ý với nhau**.

Chỗ không đồng ý chính là chỗ người duyệt cần nhìn — nó cho phép duyệt trong 10 phút thay vì 2 giờ, vì họ chỉ phán xét mấy điểm tranh chấp; phần còn lại đã có 14/20 đồng thuận.

→ **Knowledge/Process draft cần giữ evidence link ở mức TỪNG PHÁT BIỂU, không phải ở mức tài liệu.**
→ **Trạng thái `CONFLICTING` là BẮT BUỘC, không phải tùy chọn** — gom N case thì xung đột là chuyện thường ngày. (`CONFLICTING` đã có trong Case v0.2 §7.3, **thiếu** trong `PROJECT_CONTEXT` §13.4 — xem §6.9.)

Điều này cũng trả lời câu §4 đặt ra (*"nếu CÓ thì Step 1 phải định nghĩa rõ trạng thái tri thức gom từ nhiều nguồn rời, chưa ai duyệt"*):

```text
trạng thái đó = state DRAFT
              + origin AI_INFERENCE
              + evidence link theo từng phát biểu
              + có thể CONFLICTING
→ KHÔNG cần entity mới.
```

### Non-Jira: SOP .docx trên Drive + email hướng dẫn của senior

- **.docx** → Document (dạng A), có version + ACL riêng. Không tự thành Knowledge (`S6`).
- **Email của senior** → ca thú vị nhất. Phát biểu class-level do **một con người có chuyên môn** đưa ra. **Không** phải AI draft. Cũng **không** được tổ chức duyệt.

Ca này chứng minh chiều **Authority** (§5.4) kiếm được chỗ đứng: cần phân biệt

```text
"một chuyên gia nói câu này một lần trong email"
        vs
"tổ chức đã review và công bố"
```

Một trục `DRAFT / VERIFIED` không diễn đạt được hai thứ này → củng cố `S4`: **verification level** (mức tin) phải tách khỏi **lifecycle state** (mức công bố). Đúng vấn đề §6.9.

### Vertical thứ hai: CRM deal

Rule *"khách ở Negotiation im hơn 7 ngày → gửi case study cùng ngành"*: có điều kiện + có hành động, nhưng **không có thứ tự bước** và **không theo dõi "đang ở bước nào"** → **KNOWLEDGE**. Rule K-B6 chạy đúng, không phải bẻ gì.

Hai điều học được:
1. Ở vertical sales, Knowledge phần lớn là **khuyến nghị có điều kiện** (*nếu X thì làm Y*), không phải **giải thích cơ chế** (*parser drop payload*). Đây là loại **dễ lẫn với Process nhất** → rule K-B6 sẽ bị vắt kiệt ở đây, không phải ở support. Nhớ khi làm Step 2 (Knowledge types).
2. Rule đó suy ra từ các deal won/lost → **đúng cơ chế D6, khác tên**. D6 phổ quát, không đặc thù support.

### `K-B9` mới — Evidence phải trỏ trực tiếp vào Knowledge được, không qua Case — `PROPOSED`

Lỗ hiện tại: v0.2 §9 định nghĩa Evidence hỗ trợ *Claim / Problem / Action / Event* — đều scoped vào Case. §11.2 nói Case *"contributes evidence toward Knowledge"* — qua trung gian Case.

Nhưng một email của senior, hay một tin Zalo, **không thuộc case nào**. Với 60% là fragment rải rác, đây không phải trường hợp hiếm.

Đáng mừng: v0.2 §9 **đã** viết *"Một EvidenceItem có thể liên quan nhiều Case"* và *"Không mặc định 1 Evidence = owned exclusively by 1 Case"*. Nên đây là **mở rộng nhỏ, an toàn**, không phải bẻ model: cho phép `Evidence → SUPPORT/REFUTE → Knowledge` trực tiếp.

---

## 5.11 Cảnh báo về thứ tự — §8.1 nặng hơn trước

§8.1 nói: hỏi 2 người xem SOP thật nằm ở đâu, 30 phút, giá trị cao nhất.

Với D6 vừa chốt, câu đó **nặng hơn**. Vì D6 nói: sản phẩm sẽ **tự sinh ra** SOP đầu tiên. Nếu không biết một SOP thật trông thế nào (mấy bước? có rẽ nhánh? ai viết? cập nhật lần cuối bao giờ?), thì ta đang thiết kế **cỗ máy sản xuất một thứ chưa từng nhìn thấy**.

30 phút của §8.1 sẽ **kiểm chứng hoặc đánh sập `S4`** — phần đề xuất thay đổi nhiều nhất và cũng là phần ít tự tin nhất.

---

# 6. Phát hiện / phản biện KHÔNG ĐƯỢC MẤT

## 6.1 Thiếu tài liệu Product Foundation v1 — `xử lý MỘT PHẦN 2026-08-23`
Xem §1. Mất theo: capability contract, non-goals, và **Success Metrics**.

**Success Metrics đã được dựng lại** 2026-08-23 → `docs/02_SUCCESS_METRICS_V1.md` (`Q-E` RESOLVED bởi `M1`-`M4`). Không phải tìm lại bản gốc — là quyết định mới.

**Vẫn mất:** capability contract chi tiết, non-goals, và toàn bộ phần lý do/evidence của workstream 02.

⚠️ Và cảnh báo gốc chỉ đúng một nửa bây giờ: đã có **thước đo**, nhưng `QM-1` (ngưỡng cụ thể — *"bao nhiêu thì gọi là thành công?"*) vẫn `OPEN`. Có thước đo mà không có ngưỡng thì vẫn **chưa có điều kiện dừng**. Ngưỡng phải đợi chạy thật vài tuần; đoán bây giờ thì tệ hơn là không đặt.

## 6.2 Bất đối xứng evidence — `cần lưu ý khi thiết kế`
```text
P2 KNOWLEDGE_NOT_CAPTURED   → dataset-supported mạnh (306/500 thiếu action steps; ~45% NOT_REUSABLE)
                              → nhưng phục vụ Capability #3

P1 KNOWLEDGE_NOT_DISCOVERED → chỉ dựa trên MỘT anecdote (Traveloka)
                              → nhưng là nền của Capability #1 + #2
```
Round 3 kết luận: *"Knowledge reuse không consistently observable/measurable từ Jira records"* → **không có baseline định lượng cho P1**.

Hệ quả thiết kế: **đừng giả định tồn tại corpus SOP lớn và chất lượng.** Con số 10% ở §3 đã xác nhận điều này. → `applicability` và `coverage/gap` cần là first-class sớm.

## 6.3 Cần BA trạng thái tri thức, không phải hai — `PROPOSED`
Round 3 **cố tình không OCR/download attachments** → tri thức có thể **đã tồn tại** trong screenshot mà phép đo đếm là "thiếu".
```text
KNOWLEDGE_ABSENT                    không ai biết
KNOWLEDGE_EXISTS_NOT_RETRIEVABLE    có, nhưng trong ảnh chưa OCR / cuộc gọi / đầu người
KNOWLEDGE_EXISTS_RETRIEVABLE        có và tìm được
```
Giá trị sản phẩm chính là chuyển trạng thái **2 → 3**. Nếu model chỉ có absent/exists thì **không đo được đúng thứ mình đang bán**. Metadata `Machine Readability` đã có trong Evidence model v0.2 §9 — dùng lại được. Con số 30% (trong đầu người) chính là trạng thái 2.

## 6.4 Chi phí capture phải gần bằng 0 — `PROPOSED`, ràng buộc thiết kế cứng
Dữ liệu nói: 306/500 case không ghi bước xử lý → **người ta đã không chịu ghi**. Và field `Version đang sử dụng` trống **100/100** vì là việc thêm không thấy lợi.

→ **Capability 3 có nguy cơ là đúng cái field đó, mặc áo đẹp hơn.**

Ràng buộc đề nghị: AI soạn từ những gì **đã có sẵn trong case** (actions, evidence, timeline, outcome); người chỉ confirm/reject/sửa một dòng. **Nếu cần hơn ~20 giây chú ý của người dùng, nó sẽ rỗng giống cái field kia.**

## 6.5 Process Guidance phổ quát hơn Knowledge Retrieval — `quan sát chiến lược`
Ví dụ CRM của người dùng thực chất là **~90% Capability 2**, gần như không cần Capability 1. Case support thì cần cả hai.
→ Process Guidance là capability phổ quát hơn; Retrieval mang tính đặc thù support. Đáng cân nhắc khi xếp thứ tự MVP, vì muốn bán nhiều ngành thì cái đi trước nên dùng được ở nhiều ngành. Nhất quán với D2.

## 6.6 Canonical Case v0.2 đã chịu được vertical thứ hai — `đã kiểm chứng`
Ép ví dụ CRM vào model, vừa khít không phải bẻ gì:
```text
Case              = Deal #4471 với khách hàng X
Subject           = chốt hợp đồng          (không có CaseProblem — hợp lệ)
Origination       = nhập từ CRM
CurrentState      = stage "Negotiation"
CaseEvent         = STAGE_CHANGED
OwnershipSegment  = sales rep A
CaseAction        = gọi khách, gửi báo giá
CaseOutcome       = WON / LOST
ProcessRun        = sales playbook
Knowledge         = "khách ở Negotiation im hơn 7 ngày → gửi case study cùng ngành"
```
**Đây là bằng chứng khoản đầu tư vào domain model đã có lãi.** Nếu ngày đó model dính vào field Jira thì bây giờ phải làm lại từ đầu. Dùng ví dụ này khi cần thuyết phục về giá trị của domain-first.

## 6.7 Rủi ro lớn nhất của dự án: chết vì modeling — `phản biện về phương pháp`
Cách domain-first là **đúng**, và Case v0.2 chất lượng cao được **vì có 700 case thật để đối chiếu**.

Knowledge Model **không có dataset tương đương**. Process Model cũng không.

> Đào sâu mà không có dữ liệu đối chiếu thì không phải rigor — đó là **đoán một cách cẩn thận**.

Repo có guardrail chống scope explosion (§12) và chống premature architecture (§10), nhưng **không có guardrail chống premature modeling**, và không có điều kiện dừng. Bằng chứng rủi ro này đã hiện hữu: tài liệu 02 mất mà không ai phát hiện → tốc độ sản xuất tài liệu đã vượt tốc độ sử dụng tài liệu.

Failure mode không phải "làm sai thứ" mà là **"không bao giờ làm ra thứ gì"**.

**Đề nghị:** Knowledge Model v0.1 + Process Model v0.1 chốt trong **~2 tuần**, ở mức "vừa đủ để build được first use case". Quy tắc: câu hỏi nào **không chặn** việc build thì ghi vào Open Questions và đi tiếp. Hai model này **không cần** sâu bằng Case v0.2.

## 6.8 Đồng hồ cạnh tranh — `cân nhắc chiến lược`
Lớp "chĩa AI vào ticket và tài liệu" đang bị thương mại hóa nhanh. Tài liệu nhận định đúng rằng phần phòng thủ được là **process state + outcome + provenance** (PROJECT_CONTEXT §19), không phải retrieval. Nhưng phần đó chỉ có giá trị nếu **đi tới được**. Nhất quán với D5 hệ quả 3.

## 6.9 Hai vocabulary verification song song — `cần giải quyết ở Step 5`
```text
PROJECT_CONTEXT §13.4 (claim ladder):    SPECULATIVE PLAUSIBLE SUPPORTED VERIFIED INVALIDATED
Canonical Case v0.2 §7.3:                + CONFLICTING
PROJECT_CONTEXT §8.3 (knowledge health): DRAFT VERIFIED ACTIVE NEEDS_REVIEW DEPRECATED SUPERSEDED
```
`VERIFIED` xuất hiện ở cả hai với nghĩa khác nhau (mức xác minh của claim vs state publication). Sẽ thành contradiction nếu Knowledge Model không tách rõ **verification level** và **lifecycle state**.

## 6.10 Tài liệu cũ có phần đã lỗi thời — `housekeeping`
- `PROJECT_CONTEXT.md` §14.2 mô tả CanonicalCase root gồm `Intake`/`TriageState`/`ReproductionState`/`WaitingState` — **v0.2 đã loại bỏ có chủ đích** (guardrail R7). Nên đánh dấu §14.2 là SUPERSEDED.
- `PROJECT_CONTEXT.md` §16 tự nói *"Exact 3 MVP capabilities chưa được formally locked"* — trái AGENT.md §4 (`CONFIRMED`). Cùng nguyên nhân: doc 02 đã lock nhưng PROJECT_CONTEXT chưa cập nhật ngược.
- ✅ `NEXT_CONVERSATION_PROMPT (1).md` là input prompt, chứa Capability #3 bản cũ → **đã archive 2026-08-23** (`H-6`) vào `docs/archive/NEXT_CONVERSATION_PROMPT_02_INPUT.md`, có banner cảnh báo ở đầu file.
- Tên file không khớp convention `01_`/`02_`/`03_` trong AGENT.md §1.

---

# 7. Câu trả lời connector — ở mức khái niệm

Người dùng hỏi: *"Với nhiều nguồn tri thức, có phải viết service riêng cho từng loại không?"*

⚠️ Phần này là **câu trả lời khái niệm**, không phải quyết định kiến trúc. Kiến trúc thật thuộc workstream `06 — MVP Architecture`.

## Trả lời: Có — nhưng chỉ một lớp rất mỏng

Sai lầm phổ biến: viết N pipeline hoàn chỉnh cho N nguồn. Hình dạng đúng: **nhiều adapter mỏng + một pipeline dùng chung.**

```text
ADAPTER RIÊNG từng nguồn — mỏng, KHÔNG có logic nghiệp vụ
  · kết nối & xác thực
  · liệt kê và lấy item
  · phát hiện thay đổi (cursor / webhook / commit SHA)
  · đọc quyền gốc của nguồn
        ↓  phát ra: dữ liệu thô + tham chiếu nguồn + quyền gốc
        ↓
PIPELINE DÙNG CHUNG — viết một lần, mọi nguồn đi qua
  · trích text theo format (PDF/DOCX/HTML/MD)
  · phát hiện & che dữ liệu nhạy cảm
  · gắn provenance
  · chuẩn hóa quyền gốc → tenant + visibility của mình
  · map vào khái niệm canonical
  · quản version & thay đổi
        ↓
Knowledge / Evidence / Case — không biết dữ liệu đến từ nguồn nào
```

## Kỷ luật quan trọng nhất: adapter không ra quyết định về ý nghĩa

Adapter nói *"Jira status đổi thành 'Escalated to L3'"*. Nó **không** được nói *"nghĩa là ENTERED_TECHNICAL_QUEUE"*. Việc dịch nghĩa thuộc lớp canonical, phải **lưu lại được và dịch lại được** nếu sau phát hiện sai.

Đây đã là guardrail R5 trong Canonical Case v0.2: *"Connector có thể map source wording sai semantic meaning… Raw source observations phải được giữ để audit/remap."* → **Domain model đã trả lời trước câu hỏi kiến trúc này.**

## Không phải N nguồn, mà ~5 DẠNG nguồn

| Dạng | Nguồn ví dụ | Đặc điểm phải xử lý |
|---|---|---|
| **A** Tài liệu tĩnh có phân quyền | Drive, SharePoint, Confluence, khách upload | version, ACL, ít thay đổi |
| **B** Bản ghi công việc có timeline | Jira, CRM, helpdesk, ERP | chuyển trạng thái, comment, attachment |
| **C** Luồng hội thoại | email, Teams, Slack, Zalo | theo thread, khó xác định ranh giới, quyền riêng tư |
| **D** Mã nguồn & lịch sử | Git, PR, release notes | rất lớn, quyền theo repo |
| **E** Tín hiệu hệ thống | log, monitoring, API | volume lớn, giá trị theo thời điểm |

Chi phí biên rất lệch:
```text
Nguồn đầu tiên của một DẠNG MỚI   →  đắt (phải làm cả phần chung cho dạng đó)
Nguồn thứ hai CÙNG DẠNG            →  ngày, không phải tuần
```
Làm Jira (dạng B) xong thì thêm HubSpot/Freshdesk rẻ hẳn. Nhảy sang email (dạng C) gần như bắt đầu lại.

## Về MCP
MCP là chuẩn đáng dùng cho bài toán kết nối nhiều nguồn, đã có server cho nhiều tool phổ biến → tiết kiệm phần adapter.

Nhưng rõ ràng về giới hạn: **MCP cho *đường vào*, không cho *pipeline canonical*.** Provenance, chuẩn hóa quyền, phát hiện thay đổi, quản version vẫn phải tự làm. Và MCP thiên về gọi theo yêu cầu hơn đồng bộ hàng loạt → thực tế cần cả hai đường: MCP cho tra cứu tương tác, đường đồng bộ riêng cho corpus cần index.

## MVP nên có 2 nguồn — `PROPOSED`
1. **Jira** (dạng B) — 60% fragments ở đó, vừa là nguồn tri thức vừa là nguồn Case
2. **Tải tài liệu lên** (dạng A, đơn giản nhất) — 10% đã có tài liệu, và tính năng "đề nghị bổ sung SOP" cần đường nhận tài liệu

Email (dạng C) messy nhất → v2. Code (dạng D) ấn tượng nhất và nhạy cảm quyền nhất → dứt khoát v2 (nhất quán với D2).

## Điểm nối quan trọng
Tài liệu Drive, comment Jira, email và file code là **bốn loại vật chứa với bốn mô hình quyền khác nhau**. Tri thức rút ra từ chúng buộc phải mang theo: từ vật chứa nào, quyền gì, còn mới hay cũ. Khi một tri thức được chống bởi dẫn chứng từ nhiều vật chứa có quyền xem khác nhau → quay lại đúng §5.7.

→ **Câu hỏi connector và câu hỏi Step 1 là cùng một câu hỏi boundary, chỉ đứng ở phía đầu vào.**

---

# 8. Việc ngoài thiết kế — nên làm song song

## 8.1 Đi tìm SOP thật — 30 phút, giá trị cao nhất

> ⛔ **CHẶN STEP 2** — `CONFIRMED 2026-08-21`. Người dùng quyết: chạy §8.1 **trước** Step 2.
> Lý do: Step 2 là Knowledge Concepts & Granularity, và câu *"một KnowledgeRecord to bằng nào"*
> gần như không trả lời đúng được nếu chưa thấy một SOP thật. Xem `04` §6 R-K2.
Hỏi 2 người: bạn support kỳ cựu nhất, và người xử lý case OTA gần nhất — *"khi gặp case này, anh/chị làm theo cái gì?"*

Cần lấy về: SOP (nếu có) nằm ở đâu, format gì, bao nhiêu bước, có rẽ nhánh không, cập nhật lần cuối bao giờ.

Kết quả dùng để: stress-test Step 1 bằng tri thức thật thay vì ví dụ tự nghĩ. **Không thể thiết kế cái hộp trước khi biết bên trong đựng gì.**

---

# 8.1-KQ. KẾT QUẢ §8.1 — SOP THẬT · `EVIDENCE-SUPPORTED 2026-08-21`

## Nguyên văn người dùng cung cấp

> *"Mọi người lấy dữ liệu ở Kibana rồi xem response trả về sau đó xem tài liệu và issue xử lý trước đó để đưa ra kết luận"*

**Giữ nguyên văn. Mọi thứ dưới đây là phân tích, nhãn `PROPOSED`, chưa được xác nhận.**

## Cấu trúc suy ra

```text
B1  lấy dữ liệu ở Kibana            → source type E (tín hiệu hệ thống)
B2  xem response trả về             → source type E
B3  xem tài liệu                    → source type A   ("tài liệu gì" — chưa rõ)
B4  xem issue xử lý trước đó        → source type B (historical case)
B5  đưa ra kết luận                 → PHÁN XÉT CỦA CON NGƯỜI
```

## Ba điều được kiểm chứng

### (1) `S4` sống — và ranh giới rơi đúng chỗ tự nhiên

B1–B4 có per-case execution state (*"đã lấy Kibana chưa?"* trả lời được) → **Process**, đúng rule K-B6.
B5 là phán xét, không phải bước quan sát được → **Knowledge**.

Ranh giới Knowledge/Process rơi đúng vào khe giữa **GOM** và **KẾT LUẬN**. Sạch hơn dự kiến.

### (2) `Q-C` được xác nhận mạnh hơn mức đã chốt

*"Xem issue xử lý trước đó"* là **B4 của quy trình thật**. Nghĩa là "tìm case cũ tương tự" không phải nice-to-have, cũng không chỉ là dependency của Capability 3 — **nó là một bước của quy trình mà con người đang làm hằng ngày**.

### (3) `S5` Path A nhắm đúng đích — xem §8.1-KQ mục "Tinh chỉnh 10/30/60"

## Ba điều bị thay đổi

### (A) SOP thật KHÔNG có nhánh — trái với SOP tưởng tượng trong tài liệu

```text
PROJECT_CONTEXT §5.3 (tưởng tượng)      SOP thật (người dùng cung cấp)
────────────────────────────────────    ──────────────────────────────
Check booking exists                    B1  Kibana
→ Check room mapping                    B2  response
→ Check rate mapping                    B3  tài liệu
→ Check incoming log                    B4  issue cũ
→ No log        → contact OTA           B5  kết luận
→ Has log fail  → Technical
CÓ ĐIỀU KIỆN, CÓ NHÁNH                  TUYẾN TÍNH, KHÔNG NHÁNH
= decision procedure                    = evidence checklist + phán xét
```

Hai hình dạng khác nhau về bản chất. **`PROJECT_CONTEXT` §5.3 nên bị đánh dấu là ví dụ minh hoạ, không phải SOP thật.** → thêm mục housekeeping `H-7`.

### (B) Toàn bộ giá trị nằm ở B5 — bước duy nhất không ai ghi lại

```text
B1-B4  gom bằng chứng    →  làm một lần là biết. Giá trị bão hoà sau tuần đầu.
B5     kết luận          →  giá trị vĩnh viễn. KHÔNG AI GHI LẠI.
```

Đây chính là thứ mà con số **306/500 case không ghi bước xử lý** (§10.5 PROJECT_CONTEXT) đang đo. Cái thiếu không phải *quy trình*, mà là *luật kết luận*.

Hệ quả cho Primary Persona (`New Support Employee`):

```text
Capability 2 trên B1-B4  →  có giá trị TUẦN ĐẦU, rồi bão hoà
Capability 1 + luật B5   →  giá trị lâu dài, và là thứ quyết định
                            có escalate lên Technical oan hay không (P1)
```

⚠️ Đây là **phản biện với §6.5** (*"Process Guidance phổ quát hơn Knowledge Retrieval"*). §6.5 vẫn đúng về **độ phổ quát giữa các ngành**, nhưng với **first use case** thì Process Guidance đơn thuần gần như rỗng: dẫn người ta qua 4 bước hiển nhiên không giải được vấn đề. Không mâu thuẫn, nhưng phải nói rõ để không kỳ vọng sai vào Capability 2.

### (C) Tinh chỉnh 10/30/60 — cái thiếu là tri thức KẾT LUẬN, không phải tri thức QUY TRÌNH

```text
10%  có tài liệu, tìm được   →  tài liệu về HỆ THỐNG (API, field, behavior)
                                 = B3, và nó KHÔNG chứa luật kết luận
30%  trong đầu người         →  luật KẾT LUẬN
60%  rải rác fragments       →  luật KẾT LUẬN, dưới dạng dấu vết trong case cũ
```

→ 90% đang thiếu **gần như toàn bộ là conclusion knowledge.** Path A (gom N case) nhắm đúng đích.

→ **Câu trả lời sơ bộ cho `N-3` (granularity, Step 2):**

```text
Một KnowledgeRecord  ≈  MỘT LUẬT KẾT LUẬN
   "thấy tín hiệu X trong log + response Y  →  nguyên nhân Z  →  làm W"

KHÔNG phải "một SOP".  KHÔNG phải "một tài liệu".
```

Đây là thứ §8.1 lẽ ra phải sinh ra, và nó đã sinh ra.

### (D) Hai bước đầu thuộc source type E — nguồn KHÔNG có trong 2 nguồn MVP đã đề xuất

`§7` đề xuất MVP dùng 2 nguồn: **Jira (B)** + **tải tài liệu lên (A)**. Type E (log/monitoring/API) không nằm trong đó.

Nhưng **B1 và B2 của quy trình thật đều là type E.**

```text
B1 Kibana         → type E
B2 response       → type E
B3 tài liệu       → type A   ✓ có trong MVP
B4 issue cũ       → type B   ✓ có trong MVP
```

Đây là vấn đề phạm vi MVP thật, `OPEN`. Ba hướng, **chưa chọn**:

```text
(a) MVP không đọc Kibana — AI hướng dẫn người tự lấy, người dán kết quả vào.
    AI làm B3+B4+B5. Rẻ nhất, và B5 vẫn là phần giá trị nhất.
(b) Thêm Kibana thành nguồn thứ 3 — phình scope; type E có volume lớn,
    giá trị theo thời điểm.
(c) Query Kibana THEO YÊU CẦU cho một booking ID cụ thể, không index corpus log.
    Khác hoàn toàn về chi phí so với (b). §7 đã nói MCP phù hợp đúng dạng này.
```

⚠️ Không quyết ở đây. Đây là kiến trúc → Workstream 06, và `AGENT.md` §10.1 nói chốt công nghệ là quyền người dùng.

**Điều đáng mừng:** về mặt **domain model** thì Kibana không đòi gì mới. Kết quả query Kibana là `EvidenceItem` + `SourceReference`, `Origin = SYSTEM_FACT` (có phạm vi, v0.2 §7.6), machine readability cao. Canonical Case v0.2 đã phủ được. **Câu hỏi là phạm vi MVP, không phải mô hình.**

## Ba câu bổ sung — trả lời 2026-08-21

```text
Có tài liệu viết?    KHÔNG có SOP xử lý. CÓ tài liệu hệ thống (API/field/behavior).
Tập kết luận?        HỮU HẠN NHỎ — khoảng 5-10 loại nguyên nhân.
Hỏi mấy người?       1 người.   ⚠ n=1, không suy rộng.
```

### Hệ quả (1) — first use case nằm trọn trong 30% + 60%

```text
10%  tài liệu HỆ THỐNG có     →  đó là B3, và nó KHÔNG chứa luật kết luận
30%  luật kết luận trong đầu người
60%  luật kết luận rải rác trong case cũ
```

Phân tích ở mục (C) được xác nhận: **cái thiếu là conclusion knowledge, không phải procedure knowledge.**

⚠️ **Hệ quả về thứ tự MVP:** Capability 1 ngày đầu **không có SOP nào để retrieve** cho chính use case dùng để demo. Nó chỉ retrieve được tài liệu hệ thống — thứ trả lời *"field này là gì"*, không trả lời *"case này bị gì"*. → củng cố thứ tự vòng lặp `PROPOSED` ở §3 (Path A trước).

### Hệ quả (2) — "5-10 nguyên nhân" là dữ liệu quan trọng nhất của phiên này

**(a) Kho tri thức của first use case rất nhỏ.**

```text
~5-10 nguyên nhân  ≈  ~5-10 KnowledgeRecord cho toàn bộ first use case
```

Không phải 500. Xây và duyệt xong trong một ngày.

**(b) Ở quy mô này, Capability 1 KHÔNG phải bài toán retrieval.**

Với 10 record thì không cần vector DB, không cần RAG, không cần chunking. Bài toán thật là:

```text
KHÔNG PHẢI  "tìm tài liệu nào liên quan"
MÀ LÀ       "bằng chứng của case này khớp với nguyên nhân nào trong 10 cái đã biết"
            → đây là bài toán PHÂN LOẠI, không phải tìm kiếm
```

Nhất quán với `D5`: chunking/RAG pipeline nằm ở cột **giàn giáo tạm**. Ở quy mô 10 record thì dựng chúng lên là tự tạo nợ mà không được gì.

⚠️ Đây **không** phải đề nghị đổi Capability #1 (giữ G9). Capability #1 vẫn là `Contextual Knowledge Retrieval`. Chỉ là: ở MVP, cách hiện thực đúng của nó là matching/classification, không phải semantic search.

**(c) Bộ eval trở nên rẻ và định lượng được.**

Tập nhãn hữu hạn + case lịch sử đã có outcome = bài toán phân loại có đáp án. Đây là `D5 hệ quả 1` trở thành cụ thể, không còn là nguyên tắc.

**(d) Lần đầu có ứng viên cho `Q-E` (Success Metrics — mất cùng tài liệu 02).**

Với tập nguyên nhân hữu hạn, ba thước đo sau đo được:

```text
· % case hệ thống chỉ ĐÚNG nguyên nhân
· % case escalate lên Technical mà đáng ra không cần    (đo trực tiếp P1)
· độ phủ: đã có luật cho bao nhiêu / tổng số nguyên nhân đã biết
```

`Q-E` vẫn `OPEN` — đây là ứng viên, không phải quyết định. Nhưng trước đó ta **không có cách nào** đo, giờ có.

> ✅ **Cập nhật 2026-08-23 — `Q-E` đã giải, xem §2.5 và `docs/02_SUCCESS_METRICS_V1.md`.**
> Cả ba ứng viên trên **đều bị sửa vai trò**, không lấy nguyên cái nào:
>
> ```text
> "% chỉ đúng nguyên nhân"  →  sang bộ EVAL, không phải Success Metric   (M1)
> "% escalate oan"          →  giữ, nhưng LAGGING + 3 cảnh báo           (M4a)
> "độ phủ nguyên nhân"      →  leading indicator NỘI BỘ TENANT           (M4b)
> ```
>
> Lý do chính: **cả ba đều không đo được ở tháng 1**, vì ngày đầu có 0 KnowledgeRecord. Đoạn phân tích trên không thấy lỗ đó — giữ nguyên để biết vì sao.

**(e) Nếu chỉ có 5-10 nguyên nhân thì cùng một đáp án đã bị tìm lại rất nhiều lần.**
Khớp với `REUSE_OPPORTUNITY_MISSED` và `KNOWLEDGE_WAS_NOT_CAPTURED` (§10.7). Đây là câu nói mạnh nhất cho pitch — nhưng cần §8.2 đếm để định lượng, đừng dùng khi chưa có số.

### Cảnh báo về độ tin cậy — `n=1`

Con số 5-10 là **ước lượng của một người có kinh nghiệm**. Người kinh nghiệm nén thực tế lại; người mới thấy hỗn loạn. Hai rủi ro:

```text
· con số thật có thể lớn hơn (người kinh nghiệm gộp nhóm vô thức)
· không biết độ biến thiên giữa người với người → không biết có PROCESS_DRIFT (P7) hay không
```

→ **§8.2 (đếm 20 case OTA gần nhất) giờ là việc có giá trị cao nhất còn lại.** Nó xác nhận hoặc bác bỏ cả (a), (c), (d).

## Còn thiếu so với yêu cầu của §8.1

§8.1 yêu cầu lấy về: SOP nằm ở đâu, **format gì**, bao nhiêu bước, **có rẽ nhánh không**, **cập nhật lần cuối bao giờ**.

```text
✓ bao nhiêu bước       5
✓ có rẽ nhánh không    không, tuyến tính
✓ có tài liệu VIẾT nào không?     KHÔNG có SOP xử lý; CÓ tài liệu hệ thống
✓ "tài liệu" ở B3 là gì?          tài liệu hệ thống (API/field/behavior)
✓ hỏi mấy người?                  1 người  ⚠ n=1
✗ cập nhật lần cuối bao giờ?      chưa hỏi — ít quan trọng vì không có SOP viết
```

⚠️ **ĐÃ XÁC NHẬN: không có SOP viết.** Capability 1 ngày đầu chỉ retrieve được tài liệu hệ thống — thứ trả lời *"field này là gì"*, không trả lời *"case này bị gì"*. Đó là một sự thật về thứ tự MVP, không phải chi tiết.

## 8.2 Đếm case OTA — PHIÊN BẢN NHẸ · `CONFIRMED 2026-08-22`

> ⚠️ **KHÔNG chặn Step 3.** Xem §2.4. Làm được thì tốt, không làm thì Step 3 vẫn chạy.
> Nhưng nó quyết định trước một thứ đắt ở Workstream 06: có phải dựng vector DB / RAG không.

### Câu hỏi duy nhất cần trả lời

> **Tập nguyên nhân của "booking OTA không về PMS" là hữu hạn nhỏ, hay mở?**

Không cần con số chính xác. Cần **câu trả lời nhị phân**.

### Luật quyết định — CHỐT TRƯỚC KHI ĐẾM

Chốt trước để phép đếm không biến thành một vòng modeling nữa (đúng rủi ro `R-K3`):

```text
≤ 15 nhóm nguyên nhân, có lặp lại   →  §3.5 (file 04) ĐỨNG
                                       Capability 1 ở MVP = phân loại
                                       KHÔNG dựng vector DB / RAG / chunking

≥ 40 nhóm, hoặc gần như không lặp   →  §3.5 SẬP
                                       retrieval là bài toán thật, cần index
                                       → đánh dấu lại §3.5, R-K4 thành hiện thực

16-39, hoặc không kết luận được     →  giữ §3.5 nhãn n=1, quyết ở Workstream 06
```

`T1` / `T2` / `T4` **không** phụ thuộc kết quả này — xem `04` §6 R-K4. Chỉ §3.5 dễ vỡ.

### Vì sao 20 case là đủ

20 case đủ cho câu **nhị phân**, dù không đủ để chốt con số chính xác:

```text
20 case → 8 nhóm, có lặp        →  tín hiệu "hữu hạn" khá mạnh
20 case → 19 nhóm khác nhau     →  tín hiệu "mở" rất mạnh
```

Nên không cần làm to. Việc này là của **người dùng**, ngoài thiết kế.

### Hai thứ thu được kèm, gần như miễn phí

**(1) 20 nhãn eval đầu tiên.** 20 case đã có outcome + tập nguyên nhân hữu hạn = bài toán phân loại **có đáp án**. `D5 hệ quả 1` đòi bộ eval là first-class và ai cũng không muốn gán nhãn tay — đây là 20 nhãn không phải xin ai. Xem §8.4.

**(2) Bản spec cho future capability "công ty tự đo trạng thái tri thức".**

> Không thể tự động hoá một phép đo chưa ai từng thực hiện một lần.

Làm bằng tay một lần thì biết: hỏi câu gì ra câu trả lời dùng được, dữ liệu nào thật sự đếm được, đầu ra trông thế nào mới giúp quyết định. Đó chính là spec của tính năng ở `PROJECT_CONTEXT` §17 mục 21 — và tính năng đó có **giá trị tiền-bán-hàng**: một báo cáo *"công ty anh đang ở 10/30/60, nên bắt đầu từ đây"*.

### Phần bản gốc §8.2 — vẫn còn giá trị, ưu tiên thấp hơn

Bản gốc còn hai việc, giữ lại vì chúng phục vụ `Q-E` chứ không phục vụ §3.5:

```text
· Hỏi 5 bạn support mới: "bạn có biết tài liệu X tồn tại không?"
· Đếm: bao nhiêu case escalate lên Technical mà SOP đáng ra đã đủ?
  → đo trực tiếp P1, hiện P1 chỉ có 1 anecdote (§6.2)
```

## 8.3 Khôi phục Success Metrics — ✅ ĐÃ LÀM 2026-08-23
Kết quả: `docs/02_SUCCESS_METRICS_V1.md`. `Q-E` RESOLVED bởi `M1`-`M4`.

Ba tầng, tách khỏi bộ eval:

```text
M1   Success Metric ≠ Eval metric. "% chỉ đúng nguyên nhân" → sang bộ EVAL
M2   TẦNG 0, metric CHÍNH tháng đầu: số nháp Path A được duyệt + mức sửa
     diff(A,B) + tỉ lệ bỏ giữa đường.  Đo được ở trạng thái 0 tri thức.
M3   TẦNG 1: chuỗi 4 bước hiện → mở → chấp nhận → có mặt trong kết luận
M4a  TẦNG 2: "% escalate oan" — lagging, 3 cảnh báo (không baseline,
     volume nhỏ, "đáng ra" là phán xét)
M4b  "độ phủ nguyên nhân" → leading indicator NỘI BỘ TENANT, không phải
     Success Metric của sản phẩm (G12)
```

⚠️ **Còn thiếu ngưỡng** (`QM-1`). Chưa có ngưỡng thì chưa có điều kiện dừng.

## 8.4 Bộ eval từ MVP — dù chỉ 20 case gán nhãn tay
Xem D5 hệ quả 1. Đây là cơ chế biến "model mạnh lên" thành "phần mềm mạnh lên".

---

# 9. Open Questions còn mở

## Chặn Step 1
```text
Q-A  RESOLVED 2026-08-21 → CÓ, phiên bản "gom theo yêu cầu".      Xem D6 §2.3
Q-B  RESOLVED → SOP = Document + ProcessDefinition + Knowledge (BA vật thể)
Q-C  RESOLVED → CÓ. Cap 1 trả HAI loại kết quả có nhãn tách biệt:
                KnowledgeRecord và Historical Case
Q-D  Phần dễ RESOLVED (quy tắc visibility MVP, xem S7 / 04 §1.10)
     Phần khó vẫn OPEN → v2: tách kết luận/dẫn chứng mức từng câu, redaction
```

> ✅ **Step 1 đã đóng.** Toàn bộ Q-A/Q-B/Q-C + S1-S8 + K-B9 nằm ở
> `docs/04_KNOWLEDGE_MODEL_V0.1.md` §4 Decision Register.

## Chín quyết định đóng Step 1 — tất cả `CONFIRMED 2026-08-21`
```text
S1  Guardrail phạm vi D6 → nâng thành G11 (AGENT.md §3.8)
S2  Định nghĩa Knowledge: "được chấp nhận" là TRẠNG THÁI  → Q-J trả lời: state
S3  2 discriminator + 2 phép phân loại; T4 = "không PROMOTE"
S4  Kernel dùng chung 3 domain; bước có MỘT nhà = Process; verification ≠ lifecycle
S5  K-B8: Path A tạo Knowledge, Path B chỉ làm dày Case
S6  Nạp tài liệu KHÔNG tự sinh KnowledgeRecord
S7  Visibility "hẹp nhất + mở rộng tường minh", bước mở quyền TRONG luồng Cap 3
S8  Bản nháp gom: evidence link TỪNG PHÁT BIỂU + CONFLICTING bắt buộc
K-B9 Evidence trỏ trực tiếp vào Knowledge, không qua Case

Source of truth: docs/04_KNOWLEDGE_MODEL_V0.1.md §1 + §3
```

## Sinh ra từ Step 1 — cần Step 2-5
```text
N-1  ✅ RESOLVED (V1+V2+V3) — hai trục đã khóa, KHÔNG từ nào trùng.
     VERIFIED bỏ khỏi lifecycle. Bảng khóa duy nhất: 04 §3D.7. §6.9 ĐÓNG.
N-2  ✅ RESOLVED (T3) — hai type: DIAGNOSTIC + CONDITIONAL_RECOMMENDATION
N-3  ✅ RESOLVED (T1) — đơn vị = MỘT NGUYÊN NHÂN, kèm cách nhận ra
N-3b ✅ RESOLVED (AP4) — một "cách nhận ra" = MỘT ASSERTION, T4 đã giải sẵn
N-4  ✅ RESOLVED (L4) — SUPERSEDES · REFINES · CONTRADICTS,
     state SUPERSEDED là SUY RA từ quan hệ, không lưu riêng
N-5  ✅ RESOLVED (AP1 + AP2) — applicability là ASSERTION, không cấu trúc.
     tenant→visibility (S7) · thời gian→lifecycle (Step 5) ·
     hệ thống→chưa có ca thật (AP-a) · version→assertion
Auth ✅ RESOLVED (V5) — Authority = Actor, đã có trong Case v0.2 §7.
     KHÔNG cần trục thứ ba. Không mô hình hóa chức danh/mức chuyên môn.
N-6  ✅ RESOLVED (L3) — phát biểu phân biệt nằm TRONG record (T1)
N-7  ✅ RESOLVED (L3) — Case ↔ Knowledge nhiều-nhiều, evidence riêng mỗi link
N-8  ✅ RESOLVED (V4) — tách HAI ca đi hai trục khác nhau:
     (a) vẫn ĐÚNG, hết ai gặp → DEPRECATED, verification KHÔNG đổi
     (b) từng đúng, giờ SAI   → INVALIDATED → NEEDS_REVIEW
     Ca (a) là bằng chứng việc tách hai trục kiếm được chỗ đứng.

Sinh ra từ Step 3, không chặn build:
L1-a Chọn record nào trong TẬP mà ProcessStep trỏ tới?  → Workstream 06
L2-a Ai ghi nhận mốc USED — hệ thống tự phát hiện hay người xác nhận? → Q-H
```

## Housekeeping ghi ngược vào tài liệu cũ
```text
H-1  ✅ ĐÃ LÀM 2026-08-21 — PROJECT_CONTEXT §13.4 đã thêm CONFLICTING
                            + ghi rõ ladder này là verification level,
                              không phải lifecycle state ở §8.3
H-2  ✅ ĐÃ LÀM 2026-08-21 — Case v0.2 §11.2 đã thêm đường
                            Evidence → Knowledge trực tiếp (K-B9)

H-3  ✅ ĐÃ LÀM 2026-08-23 — PROJECT_CONTEXT §5.2 "Human knowledge" đã ghi rõ
                            là knowledge SOURCE, không phải KnowledgeRecord (K-B7)
                            + đường vào duy nhất cho 30% là Path B (S5)
H-4  ✅ ĐÃ LÀM 2026-08-23 — PROJECT_CONTEXT §14.2 đánh dấu SUPERSEDED,
                            ghi root ĐÚNG của v0.2 + lý do loại 4 field (R7)
H-5  ✅ ĐÃ LÀM 2026-08-23 — PROJECT_CONTEXT §16 đánh dấu SUPERSEDED,
                            ghi 3 capability đã lock + bảng so sánh
                            Capability #3 bản cũ vs bản chốt
                            ⚠ Q-E lúc đó vẫn OPEN → ĐÃ GIẢI cùng ngày, xem §2.5
H-6  ✅ ĐÃ LÀM 2026-08-23 — đã git mv sang
                            docs/archive/NEXT_CONVERSATION_PROMPT_02_INPUT.md
                            + banner cảnh báo đầu file; 5 chỗ tham chiếu đã sửa
H-7  ✅ ĐÃ LÀM 2026-08-23 — PROJECT_CONTEXT §5.3 đánh dấu là VÍ DỤ MINH HOẠ
                            TỰ NGHĨ + bảng đối chiếu với SOP thật (§8.1-KQ).
                            Ví dụ được GIỮ LẠI vì vẫn đúng để minh hoạ
                            khái niệm Process, chỉ đổi nhãn.
```
H-1/H-2 làm ngay 2026-08-21 vì là **contradiction thật** do S8/K-B9 sinh ra.
H-3..H-7 là nhãn lỗi thời, không gây sai → đã gộp làm một lần 2026-08-23.

> ✅ **Toàn bộ H-1..H-7 đã đóng.** Không rewrite history: mọi chỗ đều giữ
> nội dung cũ + thêm banner nêu rõ cái gì sai và nguồn đúng ở đâu (AGENT.md §13).

**Còn một mục housekeeping CHƯA làm, chưa có số:** tên file không khớp convention
`01_` / `02_` / `03_` ở AGENT.md §1 (xem §6.10). Đổi tên `PROJECT_CONTEXT.md` và
`Canonical Case Model v0.2.md` sẽ kéo theo sửa tham chiếu ở nhiều file.
→ Cân nhắc gộp vào lúc tạo `05_PROCESS_MODEL_V0.1.md`, làm một lần.

## Cần trước Step 2–3
```text
Q-E  ✅ RESOLVED 2026-08-23 → docs/02_SUCCESS_METRICS_V1.md (M1-M4)
     Còn QM-1: ngưỡng cụ thể của từng thước đo — cần chạy thật mới có cơ sở
Q-F  PARTIAL 2026-08-21 → có nguyên văn + cấu trúc 5 bước. Xem §8.1-KQ.
     Còn thiếu: có tài liệu VIẾT không, format, cập nhật lần cuối, hỏi mấy người.
Q-G  Ai có quyền verify Knowledge? Có phải Technical/L3?      (PROJECT_CONTEXT Q7)
Q-H  AI có được suggest update knowledge đã verified?         (PROJECT_CONTEXT Q8)
Q-I  Vai trò Secondary Persona L3 trong 3 MVP capabilities?   (không capability nào framing cho L3)
Q-J  Draft là state của KnowledgeRecord hay entity riêng?     (K-B5)
```

## Từ Canonical Case v0.2 §16 — vẫn mở
```text
OQ1 Case identity resolution        OQ4 Concurrent primary ownership
OQ2 Split/Merge semantics           OQ5 Exact vocabularies    ← ảnh hưởng Knowledge Model
OQ3 Case vs Incident policy         OQ6 Projection strategy
```

## Security / tenant — nâng độ ưu tiên vì D1
```text
Data nào được gửi external LLM? Data nào bắt buộc internal? Tenant boundaries?
(PROJECT_CONTEXT §24 Q16–Q18 — giờ là yêu cầu bán hàng, không còn là câu hỏi xa)
```

---

# 10. Prompt để mở phiên làm việc mới

Copy nguyên khối này vào một conversation Claude mới:

```text
Tôi đang tiếp tục project AI Operational Knowledge & Process Platform.

Trước khi làm gì, hãy đọc theo thứ tự:
1. docs/00_CURRENT_STATE.md   ← đọc file này TRƯỚC, nó là trạng thái hiện tại
2. AGENT.md
3. docs/PROJECT_CONTEXT.md
4. docs/Canonical Case Model v0.2.md

Lưu ý: docs/02_PRODUCT_FOUNDATION_V1.md KHÔNG tồn tại — xem cảnh báo ở
00_CURRENT_STATE.md §1.

Yêu cầu làm việc:
- Phân biệt rõ CONFIRMED / EVIDENCE-SUPPORTED / HYPOTHESIS / PROPOSED / OPEN QUESTION
- Không tự chuyển PROPOSED thành CONFIRMED, không tự đóng OPEN QUESTION
- Làm việc như design partner, chủ động phản biện, không chỉ đồng ý
- Chưa code, chưa thiết kế architecture
- Trả lời bằng ngôn ngữ dễ hiểu, tránh thuật ngữ không cần thiết

Đọc thêm:
- docs/04_KNOWLEDGE_MODEL_V0.1.md — Step 1,2,3,4 ĐÃ CHỐT (§1, §3, §3B, §3C)
  → đọc §3C.5 TRƯỚC: hình dạng đầy đủ của một KnowledgeRecord sau cả 4 Step
- docs/02_SUCCESS_METRICS_V1.md   — Success Metrics ĐÃ CHỐT (M1-M4)

Đọc thêm: docs/06_MVP_ARCHITECTURE.md — CÔNG NGHỆ ĐÃ CHỐT (AR1-AR5).

Việc hôm nay:
Workstream 07 — MVP Implementation. Điều kiện AGENT.md §10.1 đã thoả:
tôi đã chốt công nghệ.

Stack: C#/.NET · PostgreSQL (RLS cho tenant) · blob storage cho file ·
package `Anthropic` + interface mỏng hai hàm · claude-opus-5 · eval bằng
Python (script riêng) · widget nhúng Vue3+TS.

⚠ ĐỌC 06 §10 TRƯỚC KHI VIẾT DÒNG ĐẦU — 6 ràng buộc dễ sai nhất. Hai chỗ
  dễ sai IM LẶNG nhất:
    AP3  origin/actor/evidence/verification gắn TỪNG ASSERTION. Gán sai
         origin là lỗi provenance (G6) — không crash, không ai thấy.
    G13  tenant từ cấu hình/ngữ cảnh request, KHÔNG từ hằng số toàn cục.

32 quyết định đã chốt: S1-S8, K-B9, Q-B, Q-C, T1-T4, L1-L4, AP1-AP4,
V1-V5 (04) · PR1-PR4 (05) · AR1-AR5 (06). Đừng mở lại nếu không có
evidence mới. Bảng từ vựng khóa duy nhất: 04 §3D.7 — KHÔNG định nghĩa
lại vocabulary ở tầng code.

⚠ HỎI TÔI xem §8.2 (đếm case OTA, bản nhẹ) đã chạy chưa — nhưng nó KHÔNG
  chặn việc gì, đừng dừng chờ nó. File 04 §3.5 đứng trên con số "5-10 nguyên
  nhân" với n=1 (R-K4). T1/T2/T4 độc lập với con số đó.
  Luật quyết định của §8.2 đã chốt TRƯỚC khi đếm — đừng mở lại.

⚠ Trước khi chốt BẤT KỲ vocabulary mới nào, grep tài liệu cũ trước.
  §6.9 đã tái phát HAI lần (M3 vs Case v0.2 §11.2/§11.3 — xem §16).
  Đây là một hạng rủi ro, không phải sự cố một lần.

⚠ G12 (AGENT.md §3.9) mới chốt 2026-08-22: tỉ trọng tri thức của một khách
  là THAM SỐ, không phải hằng số thiết kế. Đừng hardcode 10/30/60 vào bất
  cứ đề xuất nào. Nhưng G12 KHÔNG phải giấy phép "build cả ba đường rồi để
  khách chọn" — xem đường biên ở §3.9 và cảnh báo cold start ở §2.4.

⛔ Không viết code. Chốt công nghệ là quyền của tôi — xem AGENT.md §10.1.

Bắt đầu bằng cách:
1. Xác nhận bạn đã đọc và hiểu trạng thái hiện tại (tóm tắt ngắn, không dài dòng)
2. Nhắc lại ranh giới đã chốt ở 04 §1 và §3 để chắc là không đi lệch
3. Phản biện trước khi đề xuất, rồi hỏi tôi từng quyết định một qua form để tích chọn
```

---

# 11. Nhật ký phiên 2026-08-20/21

Việc đã làm:
- Review toàn bộ source of truth; phát hiện thiếu `02_PRODUCT_FOUNDATION_V1.md`
- Phát hiện mismatch Capability #3 giữa PROJECT_CONTEXT và AGENT.md; xác định bản mới đúng bằng corroboration từ v0.2 §11.3
- Chốt 5 quyết định mới D1–D5 (§2.2)
- Nhận con số 10/30/60 và phân tích hệ quả với thứ tự MVP (§3)
- Trả lời câu hỏi connector ở mức khái niệm (§7)
- Chuẩn bị đầy đủ nội dung Step 1 (§5) — chưa chốt, chờ người dùng review
- Tạo bản trình bày cho team/lãnh đạo: `Hai-bai-toan-tri-thuc.html` (bản HTML standalone ở thư mục gốc project)

Chưa làm:
- Chưa chốt Step 1
- Chưa tạo `docs/04_KNOWLEDGE_MODEL_V0.1.md`
- Chưa viết code, chưa thiết kế architecture

---

# 12. Nhật ký phiên 2026-08-21 buổi 2

Việc đã làm:
- **Chốt D6** (§2.3) — Q-A đã giải: "gom nhiều case cũ thành SOP theo yêu cầu" NẰM TRONG Capability 3, ở phiên bản "gom theo yêu cầu". Kèm guardrail phạm vi và 3 hệ quả.
- Ghi nhận hai hệ quả chưa từng có trong tài liệu: **D6 là bánh đà của D5** (diff nháp/bản duyệt = nhãn eval miễn phí), và **D6 là điều kiện để D2 dùng được ở khách đầu tiên**.
- Chạy Step 1: phản biện định nghĩa §5.2 và discriminator test §5.3; đi qua K-B1→K-B7; đề xuất K-B8 (Path A/Path B) và K-B9 (Evidence trỏ trực tiếp Knowledge).
- Phát hiện mâu thuẫn: đầu ra của D6 là danh sách bước → theo K-B6 thì rơi vào Process domain, trong khi đang làm Knowledge Model. Đề xuất giải bằng **kernel dùng chung** (S4) — giải luôn §6.9.
- Chạy 4 stress-test (§5.10): OTA, 10/30/60, non-Jira, CRM.
- Phát hiện chỗ bị bỏ sót: bản nháp gom từ N case mang theo **một phân bố**, không phải một phát biểu → cần evidence link ở mức từng phát biểu + `CONFLICTING` là bắt buộc.
- Đóng gói **7 quyết định S1-S7** (§5.9) — toàn bộ những gì còn chặn việc đóng Step 1.

- **Chốt cả 9 quyết định** (S1-S8 + K-B9) — người dùng chọn đúng phương án đề xuất ở cả 9 câu.
- Chốt luôn `Q-B` (SOP = ba vật thể) và `Q-C` (Cap 1 trả hai loại kết quả).
- Nâng guardrail phạm vi D6 thành **G11** trong `AGENT.md` §3.8.
- **Tạo `docs/04_KNOWLEDGE_MODEL_V0.1.md`** — Step 1 đóng.

- Ghi `AGENT.md` §10.1: **chốt công nghệ là quyền của người dùng** — phải báo trước khi code.
- Làm housekeeping **H-1** (PROJECT_CONTEXT §13.4 thêm `CONFLICTING`) và **H-2** (Case v0.2 §11.2 thêm đường Evidence → Knowledge trực tiếp).

Bốn quyết định cuối phiên — `CONFIRMED`:
```text
1  §8.1 (đi tìm SOP thật) CHẶN Step 2 — chạy trước, không làm song song
2  Thứ tự workstream: GIỮ TUẦN TỰ 04 → 05, chỉ chú thích kernel là của chung
   → không tách kernel thành tài liệu riêng (tránh thêm một vòng modeling, §6.7)
3  Housekeeping: làm ngay H-1 + H-2; H-3..H-6 gộp cuối workstream 04
4  Dừng phiên ở đây, chưa sang Step 2
```

- **Chạy §8.1** — người dùng cung cấp quy trình thật + ba câu bổ sung. Kết quả: §8.1-KQ.
  Ba phát hiện lớn: SOP thật **tuyến tính, không nhánh** (trái §5.3 PROJECT_CONTEXT) ·
  giá trị nằm ở B5 *"đưa ra kết luận"* — bước duy nhất không ai ghi lại ·
  tập kết luận **HỮU HẠN ~5-10 nguyên nhân**.
- **Chốt Step 2** (`T1`-`T4`) — người dùng chọn đúng phương án đề xuất cả 4 câu.
  Kết quả ở `docs/04_KNOWLEDGE_MODEL_V0.1.md` §3.
  Nổi bật: đơn vị Knowledge = MỘT NGUYÊN NHÂN · verification gắn TỪNG ASSERTION ·
  chỉ HAI type (loại REFERENCE do S6, loại POLICY do chưa có ca thật).
- Ghi nhận §3.5: ở quy mô ~10 record, Capability 1 là bài toán **phân loại**,
  không phải semantic search → không dựng RAG/vector DB (D5 giàn giáo tạm).
- Phát hiện `H-7` và rủi ro mới `R-K4` (n=1 cho con số 5-10).

Chưa làm:
- Chưa làm Step 3 (Knowledge ↔ Case ↔ Process)
- Chưa chạy §8.2 (đếm 20 case OTA) — việc giá trị cao nhất còn lại, xem R-K4
- Chưa xử lý H-3..H-7

Trạng thái stage:
```text
Product Foundation             ✅  (nhưng artifact mất — xem §6.1)
Canonical Case Model v0.2      ✅
Knowledge Model v0.1           🔵 ĐANG LÀM
                                  Step 1 ✅ CHỐT  → 04 §1  Boundary
                                  Step 2 ✅ CHỐT  → 04 §3  Concepts & Granularity
                                  Step 3 🔵 tiếp theo
Process Model                  ⚪ sau Knowledge — nhưng xem 04 §1.4: chia nhau
                                  KERNEL, không tách rời hoàn toàn được
MVP Architecture               ⚪ later
MVP Implementation             ⚪ later
```

---

# 13. Nhật ký phiên 2026-08-22

Phiên ngắn, không thiết kế. Người dùng mới tiếp nhận lại dự án, đọc lại toàn bộ và phản biện một chỗ.

Việc đã làm:
- Người dùng phản biện §8.2: *"Sao phải đếm 20 case OTA nhỉ. Mình nghĩ nên làm linh hoạt để công ty tự thao tác... mang sang công ty khác thì tỉ trọng % SOP sẽ khác."*
- **Tách được một chỗ lẫn** mà tài liệu đang gây ra: `10/30/60` (đặc điểm của một khách) bị đọc lẫn với `"5-10 nguyên nhân"` (cấu trúc của vấn đề). §8.2 đo cái thứ hai. Ghi ở §2.4 vì chỗ lẫn này sẽ lặp lại.
- **Xác nhận người dùng đúng về một điểm tài liệu trình bày sai:** §8.2 **không chặn** Step 3. Tài liệu gọi nó *"việc giá trị cao nhất còn lại"* và TL;DR xếp nó ở mục 1 → dễ đọc thành cổng chặn. Theo quy tắc §6.7 của chính dự án thì Step 3 chạy được ngay. Đã sửa TL;DR và prompt §10.
- **Chốt `G12`** ở dạng rộng → `AGENT.md` §3.9. Đây là đóng góp thật của câu phản biện: `D5 hệ quả 2` trước đó chỉ nói về giới hạn hạ tầng, chưa nói về phân bố dữ liệu của khách.
- **Chốt §8.2 phiên bản nhẹ**, có **luật quyết định chốt trước khi đếm** (≤15 / ≥40 / ở giữa) để phép đếm không thành một vòng modeling nữa (R-K3). Ghi nhận 20 case là đủ cho câu nhị phân dù không đủ cho con số chính xác.
- **Xếp *"công ty tự đo trạng thái tri thức"* thành future capability** → `PROJECT_CONTEXT` §17 mục 21. Không vào MVP: nhét vào Capability 3 là đúng cột phải của `G11`, và §2.3 đã cảnh báo trước về dạng đề nghị này.
- Ghi nhận §8.2 làm bằng tay là **bản spec** của mục 21 — *không thể tự động hoá một phép đo chưa ai từng thực hiện một lần* — cộng 20 nhãn eval gần như miễn phí (D5 hệ quả 1).

Ba phản biện đã nêu với người dùng, đã được chấp nhận:
```text
1  "Linh hoạt" không miễn phí — build cả ba đường rồi để khách chọn là 3x
   công việc, đúng failure mode §6.7 ("không bao giờ làm ra thứ gì")
2  "Linh hoạt" giả định đã có nội dung — khách #0 KHÔNG có SOP viết nào,
   bày ba lựa chọn = bày ba màn hình trống. Cold start là bài toán THỨ TỰ,
   không phải bài toán CẤU HÌNH → đó là lý do D6 tồn tại
3  Đề nghị này nằm sát ranh giới G11 → phải vẽ ranh giới trước khi làm
```

Chưa làm:
- Chưa làm Step 3 (Knowledge ↔ Case ↔ Process) — **giờ là việc số 1, không còn gì chặn**
- Chưa chạy §8.2 (bản nhẹ) — chạy song song

---

# 14. Nhật ký phiên 2026-08-23 — Housekeeping H-3..H-7 ĐÓNG

Phiên dọn tài liệu, không có quyết định thiết kế mới.

## Việc đã làm

```text
H-3  PROJECT_CONTEXT §5.2   "Human knowledge" = knowledge SOURCE, không phải
                            KnowledgeRecord (K-B7). Ghi rõ 30% chỉ vào được
                            model qua Path B (S5).
H-4  PROJECT_CONTEXT §14.2  SUPERSEDED. Ghi root ĐÚNG của Case v0.2 và lý do
                            loại Intake/TriageState/ReproductionState/
                            WaitingState khỏi root (guardrail R7, đã đối chiếu
                            nguyên văn v0.2 §R7 và mục CONFIRMED root).
H-5  PROJECT_CONTEXT §16    SUPERSEDED. Ghi 3 capability đã lock + bảng so sánh
                            Capability #3 bản cũ vs bản chốt, và ghi rõ phần
                            "gom N case" không mất mà quay lại qua D6/Path A.
H-6  file input cũ          git mv → docs/archive/
                            NEXT_CONVERSATION_PROMPT_02_INPUT.md
                            + banner cảnh báo. 5 chỗ tham chiếu đã sửa.
H-7  PROJECT_CONTEXT §5.3   VÍ DỤ MINH HOẠ TỰ NGHĨ + bảng đối chiếu SOP thật.
                            Ví dụ GIỮ LẠI vì vẫn đúng để minh hoạ khái niệm
                            Process — chỉ sai khi bị đọc như dữ liệu thật.
```

## Nguyên tắc đã áp

Không rewrite history (`AGENT.md` §13): **không xoá** nội dung cũ ở chỗ nào. Mỗi chỗ giữ nguyên văn + thêm banner nói rõ **cái gì sai**, **vì sao**, và **nguồn đúng ở đâu**. Lý do: các mục này là bằng chứng về đường đi của dự án, và §6.1 đã cho thấy mất tài liệu thì mất luôn phần "vì sao".

## Ghi nhận trong lúc dọn

- **H-5 làm lộ rõ một thứ vẫn thiếu thật:** `Q-E` (Success Metrics). H-5 chỉ sửa được nhãn *"chưa formally locked"* của 3 capability; **thước đo thì vẫn không có**. Đó là Open Question, không phải housekeeping — và giờ nó là việc treo lâu nhất của dự án. → **`Q-E` đã được giải ngay buổi sau cùng ngày, xem §15.**
- **Một mục housekeeping còn lại, chưa có số:** tên file không khớp convention `01_`/`02_`/`03_` (`AGENT.md` §1, §6.10). Không sửa trong phiên này vì kéo theo sửa tham chiếu ở nhiều file → đề nghị gộp vào lúc tạo `05_PROCESS_MODEL_V0.1.md`.

## Chưa làm

- Chưa làm **Step 3** (Knowledge ↔ Case ↔ Process)
- Chưa chạy **§8.2** bản nhẹ

---

# 15. Nhật ký phiên 2026-08-23 (buổi 2) — `Q-E` ĐÓNG

## Việc đã làm

- **Chốt `Q-E`** — Success Metrics của MVP. Bốn quyết định `M1`-`M4`, người dùng chọn đúng phương án đề xuất cả 4 câu.
- **Tạo `docs/02_SUCCESS_METRICS_V1.md`** — file riêng, vì Success Metrics đã **mất một lần** cùng tài liệu 02 và được tham chiếu từ nhiều workstream. Ghi rõ trong file: đây là **dựng lại**, không phải tìm lại bản gốc.
- Cập nhật ngược: `AGENT.md` §1 + §13, và ở file này: §1, §2.5, §6.1, §8.3, §9, TL;DR.

## Phát hiện trong lúc làm

- **Ba ứng viên metric ở §8.1-KQ đều không đo được ở tháng 1** — vì ngày đầu có 0 KnowledgeRecord. Đây là lỗ mà không ai thấy khi đề xuất chúng. Sinh ra **Tầng 0**.
- **Một ứng viên là eval metric bị xếp nhầm thành Success Metric.** Sinh ra `M1` — quyết định nền của cả bộ.
- **`P8` trong problem taxonomy đã ghi sẵn metric cần có** (*AssistanceAttempt · knowledge used · accept/reject · outcome*) mà ba ứng viên không dùng. Taxonomy cũ trả lời trước câu hỏi mới — giống hệt trường hợp `R5` trả lời trước câu hỏi connector ở §7.
- **`G12` chốt hôm trước có tác dụng ngay hôm sau**: nó là lý do `M4b` bị xuống hạng khỏi thước đo sản phẩm. Guardrail dùng được là guardrail chặn được một quyết định cụ thể.

## Thay đổi trạng thái

```text
Q-E   OPEN (treo lâu nhất)  →  ✅ RESOLVED bởi M1-M4
QM-1  MỚI                   →  OPEN. Ngưỡng cụ thể. Cần chạy thật.
QM-4  MỚI                   →  OPEN. Nếu L3 là người duyệt tri thức thì
                               Tầng 0 đang đo công của L3, không phải trải
                               nghiệm của Primary Persona. Gắn với Q-I + Q-G.
§8.2  thêm một công dụng    →  còn là cách duy nhất lấy BASELINE cho M4a
```

## Chưa làm

- Chưa chạy **§8.2**
- Chưa chốt **`QM-1`** (ngưỡng) — cố ý hoãn, không phải bỏ sót

---

# 16. Nhật ký phiên 2026-08-23 (buổi 3) — Step 3 ĐÓNG

Bốn quyết định `L1`-`L4` (`L` = **liên kết**). Toàn văn: `docs/04_KNOWLEDGE_MODEL_V0.1.md` §3B.

## Kết quả

```text
L1   ProcessStep trỏ tới Knowledge THEO CHỦ ĐỀ, không trỏ từng record
L2   Thang 5 mốc, từ vựng DUY NHẤT: RETRIEVED → SHOWN → OPENED → ACCEPTED → USED
L3   N-6 + N-7 giải, KHÔNG thêm entity
L4   SUPERSEDES · REFINES · CONTRADICTS; state SUPERSEDED là SUY RA từ quan hệ
H-8  Contradiction đã giải: Case KHÔNG invalidate Official Knowledge
```

**Step 3 không sinh entity mới nào.** Đúng điều kiện dừng ở `04` §0.

## Bốn phát hiện

**(1) Lỗ chặn build: không có đường từ bước Process → Knowledge.** Chiều `Knowledge → ProcessDefinition` đã có (`T2`), chiều ngược thì không. Nhưng `B5 "đưa ra kết luận"` của quy trình thật là **bước của quy trình** cần nội dung **Knowledge**. Không có `L1` thì quy trình thật không biểu diễn được trong model — và B5 là chỗ chứa toàn bộ giá trị.

**(2) Ba bộ từ vựng song song, và bộ thứ ba do chính tôi tạo ra hôm qua.** `M3` chốt chuỗi 4 mốc mà **không đối chiếu** Case v0.2 §11.2/§11.3. Đây là bệnh §6.9 tái phát ở chỗ mới. `L2` gộp cả ba. Ghi lại để thấy: **§6.9 không phải sự cố một lần, nó là một hạng rủi ro** — mỗi lần chốt vocabulary mới đều phải grep tài liệu cũ trước.

**(3) Contradiction thật giữa hai tài liệu (`H-8`).** PROJECT_CONTEXT §13.6 cho Case `invalidates` Knowledge; Case v0.2 §11.2 cấm. Điều đáng ngại không phải sai từ ngữ, mà là: nếu Case invalidate được thì **`D4` hở một đường sau** — một Case do AI xử lý có thể âm thầm hạ cấp tri thức đã được người duyệt. Guardrail phải kín cả hai chiều: không tự **thêm**, cũng không tự **bỏ**. Đã sửa ngay, cùng loại H-1/H-2.

**(4) `N-6`/`N-7` giải được mà không cần chờ §8.2** — trái với nhãn *"chưa có ca thật"* ở `04` §3.6. Vì cả hai không đòi thiết kế mới: `N-7` là cardinality mà v0.2 đã hỗ trợ; `N-6` là một phát biểu về *cách nhận ra*, mà `T1` đã đặt nằm trong record. Trả lời *"không cần entity nào"* là kết luận rẻ và **sai an toàn** — nếu §8.2 tìm ra ca phức tạp hơn thì thêm sau vẫn được.

## Case v0.2 trả cổ tức lần thứ ba

Phần lớn quan hệ cross-domain của Step 3 **đã có sẵn** ở v0.2 §11 (xem `04` §3B.5). Step 3 chỉ vá ba lỗ. Hai lần trước: vertical CRM (§6.6) và `K-B9`.

## Nguyên tắc rút ra, dùng lại được ở Step 5

> **Nếu một state chỉ đúng khi tồn tại một quan hệ, thì state đó là phép chiếu của quan hệ, không phải dữ liệu độc lập.**

Rút ra từ `L4` (`SUPERSEDED` vs `SUPERSEDES`). Step 5 khóa vocabulary lifecycle + verification — áp nguyên tắc này vào đó thì tránh được §6.9 lần thứ ba.

## Đã ghi ngược vào tài liệu

```text
Case v0.2 §11.1   + L1 (ProcessStep → CONSULTS → tập Knowledge theo chủ đề)
Case v0.2 §11.2   + L2 (thang 5 mốc, bỏ Referenced) + L3 (nhiều-nhiều)
Case v0.2 §11.3   + AssistanceAttempt phải ghi 5 mốc riêng biệt
PROJECT_CONTEXT §13.6  + H-8 (invalidates là sai)
02_SUCCESS_METRICS §2.2  + L2 tinh chỉnh M3 từ 4 mốc lên 5
AGENT.md §7, §8   + Step 3 chốt, Step 4 tiếp theo
```

## Chưa làm

- Chưa chạy **§8.2**
- Chưa chốt **`QM-1`** (ngưỡng)
- Xem tiếp §18 — cùng ngày, Step 5 đã đóng và workstream 04 kết thúc

---

# 17. Nhật ký phiên 2026-08-23 (buổi 4) — Step 4 ĐÓNG

Bốn quyết định `AP1`-`AP4`. Toàn văn: `docs/04_KNOWLEDGE_MODEL_V0.1.md` §3C.

## Kết quả

```text
AP1  Applicability là ASSERTION, kể cả version. KHÔNG field có cấu trúc.
AP2  N-5 co từ 4 chiều xuống 1: tenant→visibility · thời gian→lifecycle
     · hệ thống→chưa có ca thật · version→assertion
AP3  Provenance (origin) gắn ở TỪNG ASSERTION, khớp T4 + S8
AP4  N-3b: một "cách nhận ra" = MỘT ASSERTION — T4 đã giải sẵn
```

**Step 4 không sinh entity mới nào**, và **ba trong bốn câu trả lời là "không cần thêm gì"**. Đó là kết quả, không phải thất bại — đúng điều kiện dừng ở `04` §0.

## Ba phát hiện

**(1) `N-5` có một lỗi phân loại: tenant không phải applicability.** Hai trục khác nhau — *"có áp dụng không"* vs *"có được thấy không"*. Quan trọng vì `AP1` vừa quyết applicability là **chữ**, dựa vào model đọc; nếu tenant nằm trong đó thì **ranh giới tenant thành thứ do model suy luận**, trái `G7`. Ranh giới tenant không được phép mềm; applicability thì được.

**(2) Ngay cả `version` cũng không nên thành field có cấu trúc — vì `G12`.** Đây là điểm ít ngờ nhất. Một field `versionRange` nghe vô hại nhưng **giả định mọi khách đều có một hệ thống được đánh version**. `G12` (chốt 22/08) cấm đúng điều đó. Guardrail hai ngày tuổi chặn được một quyết định cụ thể ở Step 4 — lần thứ hai `G12` có tác dụng.

**(3) `AP3` là thứ làm `M2` tính được.** `M2` định nghĩa mức sửa của người duyệt là *"% assertion bị sửa/xoá/thêm"*. Nếu origin gắn ở mức record thì con số đó không tính được. `AP3` gắn origin ở từng assertion → `diff(A,B)` đọc được ở mức assertion, và `K-B5` được giữ đúng (origin `AI_INFERENCE` không mất sau khi người verify).

## Lần đầu vẽ được trọn vẹn KnowledgeRecord

`04` §3C.5 — hình dạng đầy đủ sau bốn Step. Mọi dòng trỏ về một quyết định đã chốt, không có gì mới. Đây là chỗ nên đọc đầu tiên khi cần hiểu nhanh Knowledge Model.

Có một **bất đối xứng có ý thức** được ghi rõ ở đó:

```text
origin · evidence · verification   →  gắn ở TỪNG ASSERTION
visibility                          →  gắn ở MỨC RECORD
```

Không phải bỏ sót — `Q-D` (visibility mức từng câu) đã được hoãn sang v2 ngay từ `S7`.

## Nguyên tắc `L4` được dùng lại ngay

> *Nếu một thông tin chỉ đúng khi suy từ các thành phần, thì nó là phép chiếu, không phải dữ liệu độc lập.*

Rút ra ở `L4` (Step 3) cho `SUPERSEDED`. `AP3` dùng lại nó để **từ chối** lưu origin ở cả hai mức. Một nguyên tắc dùng được hai lần trong hai Step liền — đáng mang sang Step 5.

## Chưa làm

- Chưa chạy **§8.2**
- Chưa chốt **`QM-1`** (ngưỡng)

---

# 18. Nhật ký phiên 2026-08-23 (buổi 5) — Step 5 ĐÓNG · WORKSTREAM 04 KẾT THÚC

Năm quyết định `V1`-`V5`. Toàn văn: `docs/04_KNOWLEDGE_MODEL_V0.1.md` §3D.

## Kết quả

```text
V1  Hai trục, KHÔNG từ nào trùng. VERIFIED bỏ khỏi trục LIFECYCLE.
    Thang verification KHÔNG phải đường thẳng: 4 mức + CONFLICTING +
    INVALIDATED nằm NGOÀI thang.
V2  verification/origin/actor/evidence → ASSERTION; lifecycle+visibility → RECORD
V3  LƯU DRAFT/ACTIVE/DEPRECATED · SUY RA NEEDS_REVIEW/SUPERSEDED
V4  N-8 tách HAI ca, đi hai trục khác nhau
V5  Authority = Actor, đã có trong v0.2 §7. Không cần trục thứ ba.
H-9 PROJECT_CONTEXT §8.3 đã sửa. §6.9 ĐÓNG.
```

## Kỷ luật grep có tác dụng ngay trong phiên đầu áp dụng

Tôi tự đặt luật *"grep toàn bộ tài liệu trước khi chốt vocabulary"* ở cuối buổi 3. Buổi 5 áp dụng, và nó bắt được **ba** thứ:

**(1) `Provenance` của v0.2 §7 có SÁU thành phần**, không phải hai như tôi tưởng: `Origin · Actor · Source · Evidence · Time · Verification`. Chính `Actor` là nhà của `Authority` — nếu không grep thì `V5` đã tạo một trục thứ ba không cần thiết.

**(2) `VERIFIED` đã bị lặng lẽ bỏ khỏi lifecycle khi viết file 04**, ở §1.4, mà không ai ghi lại. Nên §6.9 thực ra đã tái phát **BA** lần, không phải hai.

**(3) Lỗi của chính tôi ở buổi 4.** `AP3`/§3C.5 ghi `origin = AI_INFERENCE | HUMAN | SYSTEM_FACT`. Sai — v0.2 §7.1 có **5** giá trị, và `HUMAN` **gộp mất** phân biệt mà v0.2 §7.5 dựng riêng một mục để bảo vệ: `USER_CONFIRMED` ≠ sự thật khách quan. Đã sửa.

> Lần thứ ba của §6.9 là lần đầu bị bắt **TRƯỚC** khi thành quyết định. Đó là toàn bộ giá trị của kỷ luật grep, và nó trả tiền ngay lần đầu dùng.

## Ca chứng minh việc tách hai trục kiếm được chỗ đứng

`V4` là chỗ `N-1` trả tiền, không phải chỗ nó gọn gàng từ ngữ:

```text
"parser < 2.3 drop payload dạng X"  →  không còn khách nào chạy < 2.3
   verification  VẪN VERIFIED   ← nó VẪN ĐÚNG
   lifecycle     DEPRECATED     ← chỉ là không còn ai gặp
```

Nếu chỉ có **một** trục thì buộc phải gắn `INVALIDATED` cho một phát biểu **vẫn đúng** → một lời nói sai nằm trong dữ liệu (`G3`), bộ eval nhận nhãn sai (`D5 hệ quả 1`), và nếu sau này có khách chạy bản cũ thì tri thức đúng đã bị đánh dấu là sai.

## Nguyên tắc `L4` dùng lần thứ ba

> *Nếu một thông tin chỉ đúng khi suy từ thứ khác, thì nó là phép chiếu, không phải dữ liệu độc lập.*

`L4` (SUPERSEDED) → `AP3` (từ chối origin ở hai mức) → `V3` (NEEDS_REVIEW + SUPERSEDED là suy ra). Ba step liền. Đáng mang sang Workstream 05.

Hệ quả cụ thể của `V3`: **`NEEDS_REVIEW` được KÍCH HOẠT, không phải ai đó tự chọn** — tri thức có assertion vừa bị bác bỏ không nằm im ở `ACTIVE` chờ người để ý. Và nó **không rút tri thức khỏi retrieval**, chỉ gắn cờ — cùng triết lý `S8`: bày chỗ xung đột ra, đó là chỗ người ta cần nhìn.

## `V2` giải thích một bất đối xứng, không chỉ ghi nhận nó

§3C.5 (buổi 4) ghi nhận *ba thứ per-assertion, visibility per-record* và gọi là "có ý thức vì Q-D hoãn sang v2". `V2` cho lý do sâu hơn:

> Visibility ở mức record **vì duyệt ở mức record**. Và duyệt ở mức record **vì `S7` đã gộp duyệt-nội-dung với mở-quyền-xem thành một hành động.**

Không phải hai quyết định tình cờ khớp nhau — là **một** quyết định (`S7`) nhìn từ hai phía.

## Workstream 04 đóng

```text
23 quyết định CONFIRMED:
  S1-S8 · K-B9 · Q-B · Q-C · T1-T4 · L1-L4 · AP1-AP4 · V1-V5

Điều kiện dừng §0 đã kiểm bằng quy trình thật → 04 §3D.8: ĐỦ ĐỂ BUILD.
Step 3, 4, 5 KHÔNG sinh entity mới nào — chỉ gộp, vá, loại bỏ.
```

Ba step cuối không thêm entity là **dấu hiệu model đã hội tụ**, không phải dấu hiệu làm ít. Và nó là bằng chứng ngược cho `R-K3` (chết vì modeling): workstream này dừng đúng lúc thay vì đào tiếp.

## Chưa làm

- Chưa chạy **§8.2**
- Chưa chốt **`QM-1`** (ngưỡng)
- Xem tiếp §19 — cùng ngày, Workstream 05 đóng và Domain Modeling kết thúc

---

# 19. Nhật ký phiên 2026-08-23 (buổi 6) — WORKSTREAM 05 ĐÓNG · DOMAIN MODELING KẾT THÚC

Người dùng nêu một lo ngại đúng lúc: *"Bao giờ mới tới bước chốt công nghệ nhỉ. Mình không muốn lún sâu quá vào các phỏng đoán trên lý thuyết."*

Đó là gọi đúng `R-K3` — guardrail của chính dự án. Nó **đổi cách làm** Workstream 05.

## Quyết định phạm vi: Workstream 05 làm NHỎ, một phiên

Ba căn cứ, đều từ tài liệu, không phải nhượng bộ cho nhanh:

```text
1  §6.7   "Process v0.1 chốt ở mức vừa đủ để build. KHÔNG cần sâu bằng Case v0.2."
2  §8.1-KQ  SOP THẬT tuyến tính, không nhánh (H-7). Phần lớn độ phức tạp của một
            process model là nhánh/điều kiện/ngoại lệ — đúng cái dữ liệu thật
            nói KHÔNG tồn tại.
3  v0.2 §11.1 + L1 + T2 + S4 + §5.4   phần lớn Process ĐÃ được quyết ở nơi khác.
```

Kết quả: **bốn quyết định**, `PR1`-`PR4`, và `docs/05_PROCESS_MODEL_V0.1.md` ngắn hơn `04` khoảng mười lần.

## Bốn quyết định

```text
PR1  Trạng thái bước SUY RA từ evidence, không lưu cờ
PR2  ProcessDefinition v0.1 = danh sách bước TUYẾN TÍNH, không nhánh
PR3  Escalation thuộc KNOWLEDGE (một kết luận của B5), không phải nhánh Process
PR4  Chờ/bị chặn ở lại mức Case (WaitingSegment v0.2), không làm bản thứ hai
```

**Không entity mới.** `ProcessDefinition` và `ProcessRun` đều đã được v0.2 §11.1 công nhận từ trước.

## Hai phát hiện

**(1) `v0.2 §11.1` đã nói `PR1` từ trước, chỉ chưa ai đọc kỹ.** Câu *"CaseAction, CaseClaim và Evidence cung cấp observations để ProcessRun **xác định** process state"* đã hàm ý state là kết quả của một phép suy, không phải một cờ ai bật. Lần thứ tư một guardrail/câu cũ trả lời trước câu hỏi mới (trước đó: `R5`→connector, `P8`→metric, `Actor`→Authority).

**(2) `PR1` giải một chỗ mà `G3` đòi mà chưa ai chỉ ra cách làm.** Ba nguồn xác định state ở §5.4 khớp luôn qua `origin`:

```text
SYSTEM FACT       → EvidenceItem, origin = SYSTEM_FACT
USER CONFIRMATION → "tôi check rồi" LÀ evidence, origin = USER_CONFIRMED
AI INFERENCE      → evidence, origin = AI_INFERENCE
```

Nghĩa là **AI suy luận một bước đã xong thì bị đánh dấu TỰ ĐỘNG** — không cần cờ riêng, nên không có chỗ để quên đánh dấu.

## Nguyên tắc `L4` dùng lần thứ tư

```text
L4  (SUPERSEDED)  →  AP3 (origin không hai mức)  →  V3 (NEEDS_REVIEW)  →  PR1 (bước)
```

Bốn step liên tiếp. Nên coi là **nguyên tắc thiết kế của dự án**, không phải mẹo cục bộ:

> *Nếu một thông tin chỉ đúng khi suy từ thứ khác, thì nó là phép chiếu, không phải dữ liệu độc lập.*

## §8.2 chuyển từ "song song" sang "CHẶN"

Trước phiên này tôi vẫn nói §8.2 không chặn gì. Với Workstream 06 sắp tới thì điều đó **đổi**:

```text
§8.2  → tập nguyên nhân hữu hạn hay mở?
      → 04 §3.5 và AP1 đúng hay sai
      → CÓ DỰNG vector DB / RAG hay không
      → quyết định công nghệ ĐẮT NHẤT và KHÓ ĐẢO NHẤT của Workstream 06
```

→ Chạy §8.2 trước khi vào 06 thì quyết bằng **số**, không quyết bằng ước lượng n=1.

## Trả lời câu hỏi về mốc thời gian

```text
Workstream 04, 05   Domain Modeling     ✅ XONG 2026-08-23
Workstream 06       MVP Architecture    🔵 ĐÂY là lúc chốt công nghệ
Workstream 07       MVP Implementation  ⚪ chỉ sau khi người dùng đã chốt
```

Mốc §6.7 (~04/09 cho cả hai model) **đạt sớm hơn dự kiến 12 ngày**.

## Chưa làm

- Chưa chạy **§8.2**
- Chưa chốt **`QM-1`** (ngưỡng Success Metrics)
- Xem tiếp §20 — cùng ngày, Workstream 06 đóng và **công nghệ đã chốt**

---

# 20. Nhật ký phiên 2026-08-23 (buổi 7) — WORKSTREAM 06 ĐÓNG · CÔNG NGHỆ ĐÃ CHỐT

Năm quyết định `AR1`-`AR5` + guardrail `G13`. Toàn văn: **`docs/06_MVP_ARCHITECTURE.md`**.

## Thông tin mới quan trọng nhất: mô hình tích hợp

Người dùng mô tả: sản phẩm là **service phản ứng theo SỰ KIỆN** — phần mềm có sẵn của khách phát tín hiệu (issue mới / đổi trạng thái / user hỏi tài liệu) thì sản phẩm này mới thức tỉnh và xử lý.

Đây là mô tả rõ ràng nhất về hình dạng sản phẩm từ đầu dự án. Nó xác nhận `D2` + `D3`, và ở MVP nó đơn giản hoá kha khá: **không cần quản lý phiên đăng nhập, không cần frontend riêng.**

## Năm quyết định

```text
AR1  C#/.NET + PostgreSQL + blob storage. Eval = Python riêng. Widget Vue3+TS.
AR2  tenant → DB (RLS) · visibility → ứng dụng. Kèm G13.
AR3  SDK chính thức + interface MỎNG hai hàm. Không framework, không gateway.
AR4  Tài liệu: blob storage + model đọc PDF nguyên bản + Postgres FTS trước.
AR5  "Quét dữ liệu": nghĩa NẠP thuộc MVP · nghĩa TỰ TÌM CHỦ ĐỀ để v2 (G11).
```

## Bốn phát hiện

**(1) Agent định nói sai, và kỷ luật đọc tài liệu bắt được.** Tôi định lập luận *"Python/TS có ecosystem LLM tốt hơn nên chọn ngôn ngữ theo đó"*. Đọc tài liệu tham chiếu ra: Anthropic có **SDK chính thức cho cả 7 ngôn ngữ** kể cả C#. Nghĩa là LLM **không phải** yếu tố chọn ngôn ngữ — team biết gì mới là yếu tố áp đảo. Đây là lần thứ hai kỷ luật "đọc trước khi nói" cứu một quyết định (lần đầu: grep bắt lỗi Origin ở buổi 5).

**(2) Lo ngại "khách đòi server riêng" tự tan — và RLS chính là thứ làm được điều đó.** Một bản deploy riêng là *cùng code, cùng schema*, chỉ khác cấu hình. Thứ **sẽ** chặn không phải cơ chế cô lập mà là hardcode giả định "một DB dùng chung" → sinh ra `G13`.

**(3) `§3.5` chỉ phân tích MỘT trong HAI bài toán tìm kiếm.** Câu hỏi của người dùng về việc khách nạp nhiều PDF/Word làm lộ ra điều này:

```text
Bài toán 1  khớp bằng chứng với ~10 nguyên nhân   §3.5 xét, §8.2 quyết
Bài toán 2  tìm đúng tài liệu trong N tài liệu    §3.5 KHÔNG xét, §8.2 KHÔNG quyết
```

§3.5 **không sai** — đúng trong phạm vi nó xét. Nhưng câu *"MVP không cần vector DB"* dễ bị đọc rộng hơn phạm vi đó. Ghi thành rủi ro `R-A1` ở `04` §3.5, không rewrite §3.5.

Câu trả lời cho bài toán 2 (`AR4`): vẫn chưa cần vector, nhưng vì **lý do khác** — tài liệu hệ thống đầy tên field/API/mã lỗi, nên **từ khoá thắng ngữ nghĩa**. Postgres FTS trước, `pgvector` khi đo được là không đủ.

**(4) `S5` cắt đúng khớp tự nhiên — lần thứ hai.** `S5` chia Path A/Path B theo *ngân sách chú ý của người dùng* (phút vs giây). Hoá ra nó chia đúng theo **ranh giới hạ tầng**: Path A → Batches API (nửa giá, chậm); Path B → realtime. Một phân chia domain trùng khít một phân chia kỹ thuật, không phải do ai thiết kế ra.

## Năm tính năng API map 1:1 vào quyết định domain

Cổ tức của domain-first, ghi ở `06` §7:

```text
structured outputs  →  §3.5 bài toán phân loại, shape đã validate
prompt caching      →  ~10 record là prefix ổn định (~0.1× giá input)
1M context          →  Path A: 20 case trong MỘT request, §3.5 thành số đo
Batches API 50%     →  Path A không nhạy latency (S5)
inference_geo       →  D1 bán cho doanh nghiệp đòi data residency
```

Điểm đáng chú ý: **kho tri thức nhỏ (~10 record) hoá ra là LỢI THẾ chi phí**, không phải điểm yếu — nó là một prefix ổn định để cache.

## Cảnh báo §2.3 được dùng đúng mục đích, 2 ngày sau khi viết

§2.3 ghi: *"Sẽ có lúc ai đó đề nghị 'hay là mình tự động phát hiện chủ đề nào cần SOP luôn'. Câu đó nghe rất hợp lý và nó là cột phải. Ghi xuống đây để lần sau có chỗ đối chiếu."*

Người dùng nêu *"cấu hình để quét dữ liệu riêng"*. Nhờ có §2.3 mà tách được hai nghĩa ngay: **nạp dữ liệu** (được, §7 đã lên kế hoạch) vs **tự tìm chủ đề cần SOP** (đúng cột phải G11 → v2). Người dùng chọn *"cả hai, nhưng nghĩa 2 để v2"* → `G11` nguyên vẹn.

## Chưa làm

- Chưa vào **Workstream 07** — đọc `06` §10 trước khi viết dòng đầu
- Chưa chạy **§8.2** · chưa chạy **AR4-b** (đếm tài liệu thật)
- Chưa chốt **`QM-1`** (ngưỡng Success Metrics)
