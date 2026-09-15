# 14 — QUYẾT ĐỊNH CHỜ CHỦ DỰ ÁN

> **Sinh 2026-09-15.** Mười câu đang chờ chính bạn. Không câu nào code trả lời hộ được —
> đây là quyết định sản phẩm, domain, hoặc pháp lý.
>
> **Cách điền:** đánh `[x]` vào ô mình chọn. Để trống = **chưa quyết**, và đó là một câu
> trả lời hợp lệ (nó khác với "quên"). Mỗi câu chọn **đúng một** ô, trừ chỗ ghi rõ khác.
>
> **Kiểm lại sau khi điền:**
> ```bash
> python scripts/doc_quyet_dinh.py docs/14_QUYET_DINH_CHO_CHU_DU_AN.md
> ```
> Nó đếm câu đã quyết, và **báo lỗi nếu một câu bị tích hai ô** — thứ rất dễ xảy ra khi
> sửa đi sửa lại mà mắt không bắt được.
>
> ⚠ **File này KHÔNG phải nơi lưu quyết định lâu dài.** Điền xong thì quyết định phải được
> chép sang `docs/07` §5 (mục `AR-*` tương ứng) kèm **lý do**, theo đúng luật của dự án:
> giữ nguyên lập luận cũ, chỉ đổi nhãn trạng thái và thêm con trỏ. Mất phần *"vì sao"* là
> mất mát lớn nhất — đã xảy ra một lần với `docs/02`.

---

## ✅ ĐÃ ĐIỀN — 2026-09-15

Chủ dự án trả lời qua form hỏi đáp (ba lượt: nhóm A → B → C), agent điền lại vào đây.
**11/11 câu đã chọn.**

⚠ **Ghi ra một dữ kiện để người đọc sau biết:** cả **11/11** đều là phương án
`(đề xuất)`. Điều đó không làm quyết định kém giá trị hơn — các đề xuất đều có số đo
chống lưng, ghi ngay trong từng mục. Nhưng nó có nghĩa đây **không phải 11 lần cân nhắc
độc lập**, nên đừng đọc sự đồng thuận này thành bằng chứng độc lập cho từng câu.
Câu nào về sau đo lại thấy sai thì mở lại — không câu nào ở đây là không đảo được.

---

## Đọc 30 giây: ba nhóm, và vì sao xếp theo thứ tự này

```text
NHÓM A  đang CHẶN việc tiếp theo                    3 câu  ~15 phút
NHÓM B  phải quyết TRƯỚC khi đụng thêm corpus       3 câu  ~20 phút
NHÓM C  chưa chặn, nhưng đừng để quên               4 câu  ~15 phút
```

Nếu chỉ có 15 phút: **làm nhóm A**. Ba câu đó chặn thứ đang chạy dở, và một trong ba
(`AR-a`) vừa mới thành chịu lực vì đợt build ngày 13.

---

# NHÓM A — đang CHẶN việc tiếp theo

## A1 · `AR-q` — Chốt bộ định nghĩa 5 ô phân bố

**Câu hỏi:** bộ định nghĩa + ca biên cho 5 ô `phanBoBuocKiemDuocGhiLai` (đang nằm trong
`description` của `SopDraftSchema`) đã dùng được chưa, hay còn phải sửa?

**Đã đo được gì** — bốn lượt, mỗi lượt một ngữ cảnh sạch:

```text
mốc cũ, ô chỉ có TÊN (IM-29)                          7/10 khớp
vòng 1, định nghĩa CÓ TRÍCH NGUYÊN VĂN ticket nhóm 1  10/10 khớp   ← SỐ BỊ THỔI
vòng 2, định nghĩa viết theo HÌNH DẠNG                 8/10 khớp   ← số dùng được
NHÓM 2, nhóm CHƯA HỀ dùng để viết định nghĩa          10/10 KHỚP TUYỆT ĐỐI
        (trùng nhau từng mã case, kể cả hai ô rỗng)
```

🛑 Vòng 1 **không được đọc là thành công**: một ngữ cảnh khai rõ nó xử ca khó bằng lý do
*"ví dụ đích danh thắng ca biên tổng quát"* — tức nó **nhận ra ví dụ**, không **áp dụng
luật**. Bỏ câu trích nguyên văn thì tụt xuống 8/10.

🎯 Con số có nghĩa là **10/10 của nhóm 2**, vì nhóm đó chưa hề được dùng để viết một chữ
nào trong định nghĩa — và nó còn là phép thử KHÓ hơn chứ không dễ hơn.

**Vì sao gấp:** đây là **dụng cụ đo của `M2`**. Chưa chốt thì mọi con số `M2` về sau đứng
trên một cái thước chưa ai công nhận.

**Chọn một:**

- [x] **A1-1 — CHỐT, dùng nguyên trạng.** (đề xuất) Bộ định nghĩa hiện tại thành chuẩn.
      10/10 trên một nhóm chưa hề dùng để hiệu chỉnh là bằng chứng tổng quát hoá đủ mạnh.
- [ ] **A1-2 — CHỐT, nhưng đo thêm một nhóm thứ ba trước.** Chậm hơn, và cần credit hoặc
      một lượt `--xuat-payload` nữa.
- [ ] **A1-3 — CHƯA CHỐT, tôi muốn tự đọc định nghĩa 5 ô trước.** Chúng nằm ở
      `src/KnowledgePlatform.Infrastructure/Sop/SopDraftSchema.cs`.
- [ ] **A1-4 — Sửa lại.** Ghi chỗ cần sửa vào phần ghi chú bên dưới.

**Nếu để trống:** `M2` vẫn đo được về mặt kỹ thuật, nhưng con số của nó không có thước
được công nhận đứng sau — và đó đúng là loại nợ mà dự án này đã trả một lần.

**Ghi chú của bạn:**

>
>

---

## A2 · `AR-a` — Tập giá trị của nhãn QUYỀN XEM

**Câu hỏi:** `visibilityScope` được nhận những giá trị nào?

**Vì sao câu này VỪA MỚI thành gấp:** ngày 13 tôi build đường duyệt, và nó nhận
`phamViXem` là **chuỗi tự do**. Đo trên `kp_dev` hôm nay:

```text
noi-bo   7 record        ← chuỗi tôi gõ khi chạy thử, KHÔNG ai chốt nó
(NULL)   1 record
```

Bảy bản ghi thật đang mang một nhãn mà **không có quyết định nào đứng sau**. Càng duyệt
thêm thì càng nhiều dòng phải sửa nếu về sau khoá tập giá trị lại.

**Bối cảnh:** `S7` khoá **quy tắc** (mặc định hẹp nhất; mở rộng phải là hành vi tường minh
của người thấy được tất cả nguồn; hệ thống không bao giờ tự mở) nhưng **không khoá tập giá
trị**. Code cố ý để dạng chuỗi đục thay vì tự phát minh enum (`IM-3`) — vì phát minh
vocabulary ở tầng code đúng là bệnh `§6.9` mà workstream 04 đã mất ba lần để chữa.

**Chọn một:**

- [x] **A2-1 — Ba giá trị: `rieng-tu` · `noi-bo` · `toan-cong-ty`.** (đề xuất) Đủ cho
      `S7`, và "hẹp nhất" có nghĩa rõ ràng. Siết thành enum ở code.
- [ ] **A2-2 — Hai giá trị: `noi-bo` · `toan-cong-ty`.** Đơn giản nhất. Bỏ mức riêng tư.
- [ ] **A2-3 — Theo PHÒNG BAN** (`support` · `trien-khai` · `ky-thuat` · …). Mạnh hơn
      nhưng kéo theo câu "ai thuộc phòng nào" — mà `AR-e` (xác thực) còn chưa giải.
- [ ] **A2-4 — Giữ chuỗi tự do, chưa khoá.** Chấp nhận là sau này phải sửa dữ liệu.

**Nếu để trống:** mỗi lần duyệt lại sinh thêm một dòng mang nhãn chưa được công nhận.

**Ghi chú của bạn:**

>
>

---

## A3 · `AR-p` — Máy có được trích case NGOÀI nhóm không?

**Câu hỏi:** khi dựng bản nháp SOP cho một nhóm, máy có được dẫn case của nhóm KHÁC để
làm **bước loại trừ** không?

**Đã đo được gì:** bản B do máy sinh **thiếu hẳn `K5`** của bản người dựng — *"trình duyệt
đang đăng nhập nhiều tài khoản cùng lúc không?"*. `K5` không phải nhánh yếu: nó là
`evidence-noi-ro`, chống lưng bởi `ES-341290` **và** `ES-343036`. Mà `ES-343036` **không
thuộc nhóm** — bản người dựng cố ý MƯỢN nó từ nhóm "xung đột phiên", và khai rõ chỗ mượn.

🛑 **Và phép kiểm chống bịa nguồn đã QUA MỘT CÁCH TẦM THƯỜNG:** bản B đặt
`_haiCaseLech: null` và trích 9 mã đều trong nhóm — nên nó qua, nhưng **không phải vì nó
trung thực, mà vì nó không với tới được vùng bị cấm**. Một phép kiểm qua kiểu đó thì chưa
kiểm gì cả.

⚠ **Đây là quan sát ĐỘC LẬP THỨ BA về cùng một điều:**

```text
docs/11 §9   bước kiểm đầu của nhóm 2 bị chặn bởi đúng nguyên nhân của nhóm 1
AR-p         bản người dựng phải MƯỢN case nhóm khác để dựng K5
docs/13      4/5 lỗi định tuyến nằm ở SEAM giữa hai nhóm kề nhau
```

→ Ba lần, ba cách đo khác nhau, cùng một kết luận: **ranh giới nhóm của taxonomy không
phải ranh giới hợp lệ của một SOP.**

**Chọn một:**

- [x] **A3-1 — CHO mượn, kèm ba điều kiện.** (đề xuất) Cấp corpus rộng hơn một nhóm ·
      phép kiểm chống bịa nguồn đổi từ *"trong nhóm"* sang *"trong corpus"* · **BẮT KHAI**
      chỗ mượn (đúng như `_haiCaseLech` của bản người dựng).
- [ ] **A3-2 — KHÔNG cho mượn.** Máy vĩnh viễn không dựng được bước loại trừ liên nhóm.
      ⚠ Nếu chọn ô này thì đó là **trần CẤU TRÚC**, không phải hạn chế tạm của prompt —
      phải ghi vào tài liệu sản phẩm, đừng chờ prompt tốt hơn.
- [ ] **A3-3 — Vẽ lại taxonomy trước** (ví dụ gom quanh nhà cung cấp), rồi câu này tự
      tan. Giải cả ba quan sát ở trên cùng lúc, nhưng là việc lớn nhất.

**Nếu để trống:** bản nháp máy sinh sẽ tiếp tục thiếu bước loại trừ, và bộ eval tiếp tục
báo xanh vì lý do sai.

**Ghi chú của bạn:**

>
>

---

# NHÓM B — phải quyết TRƯỚC khi đụng thêm corpus

> Ba câu này có chung một tính chất: **quyết muộn thì không sửa lại được.** Không phải vì
> code khó sửa, mà vì dữ liệu đã bị cắt / đã được công bố thì không gom lại được.

## B1 · `AR-m` — Nội dung người gửi ĐÃ RÚT LẠI

**Câu hỏi:** một đoạn mà người gửi đã nói *"bỏ qua giúp em"* thì đánh dấu thế nào?

**Ca thật, đã đọc nguyên văn:** `ES-346661#description` chứa một đoạn giải thích nguyên
nhân overbooking đầy đủ, kèm **hai mã booking OTA** (Trip.com và Expedia). **Mười sáu phút
sau**, cùng người gửi viết: *"dạ anh/chị bỏ qua giúp em tin nhắn trên ạ, em gửi nhầm nội
dung"*. Tiêu đề case là *"lỗi không sinh mã đặt phòng"* — đoạn kia là nội dung của một case
**khác**.

⚠ **Vì sao hôm nay chưa hỏng và ngày mai sẽ hỏng:** hôm nay cả đoạn sai lẫn câu đính chính
nằm CHUNG một mẩu 1 916 ký tự, nên model đọc mẩu đó sẽ thấy câu đính chính và tự bỏ qua.
Đó là **may**, không phải thiết kế. Ngày nào pipeline cắt transcript thành nhiều mẩu — việc
rất có thể sẽ làm, vì `AR-h` đã đo rằng **mẩu dài đè bẹp xếp hạng 29:1** — thì đoạn đã bị
rút sẽ vào kho **không mang dấu hiệu gì**. Nguyên nhân overbooking của khách A sẽ xuất hiện
trong SOP về mã đặt phòng.

🛑 **Chỗ khó thật:** người gửi rút lại bằng **câu chữ**, không bằng một nút bấm. Suy ra từ
câu chữ là **đoán** — trái `G6`/`AP3`. Nhưng không suy thì không có tín hiệu nào cả.

**Chọn một:**

- [x] **B1-1 — Thêm cột `RetractedAt` + `RetractedReason` ở tầng MẨU, và CHỈ người điền
      tay.** (đề xuất) Không đoán. Tín hiệu ít nhưng mỗi cái đều đúng.
- [ ] **B1-2 — Cho máy SUY từ câu chữ, nhưng đánh dấu là "nghi ngờ" chứ không phải
      "đã rút".** Nhiều tín hiệu hơn, và không bản nào bị âm thầm tin.
- [ ] **B1-3 — KHÔNG cắt transcript thành nhiều mẩu.** Giữ nguyên cả đoạn, để câu đính
      chính luôn đi kèm. ⚠ Đánh đổi thẳng với `AR-h` (mẩu dài đè bẹp xếp hạng 29:1).
- [ ] **B1-4 — Chưa xử lý, và CHẤP NHẬN rủi ro có ý thức.** Ghi vào tài liệu rủi ro.

**Nếu để trống:** không được chốt cách cắt transcript. Cắt rồi thì không ghép lại được.

**Ghi chú của bạn:**

>
>

---

## B2 · `AR-n` — Dữ liệu BÊN THỨ BA và dữ liệu THƯƠNG MẠI

**Câu hỏi:** hai loại dữ liệu này xử lý thế nào trước khi vào kho tri thức?

**Hai ca thật, hai loại khác hẳn nhau:**

```text
BÊN THỨ BA   ES-346481#comment-802977 — payload hoá đơn có khối <NMua>: tên một ĐƠN VỊ
             SỰ NGHIỆP NHÀ NƯỚC, mã số thuế, địa chỉ đầy đủ, 8 dòng dịch vụ, tổng tiền.
             Không phải dữ liệu ezCloud, cũng không phải của khách sạn — là dữ liệu
             KHÁCH CỦA KHÁCH SẠN, một pháp nhân chưa từng đồng ý xuất hiện ở đâu.
             -> AR-l chốt ranh giới HAI cấp; đây là CẤP THỨ BA nó chưa mô tả.

THƯƠNG MẠI   ES-346608#comment-803265 — giá hợp đồng theo năm của một khách sạn cụ thể,
             hạn hợp đồng, và ý định ngừng dùng để chuyển sang PMS tự xây.
             -> KHÔNG phải bí mật kỹ thuật nên luật che credential (AR-j) không bắt
                được, và KHÔNG CÓ HÌNH DẠNG NÀO để bắt bằng regex.
```

**Chọn một cho BÊN THỨ BA:**

- [x] **B2a-1 — Che khối `<NMua>` và tương đương ở khâu nạp.** (đề xuất) Có hình dạng
      rõ nên bắt được bằng luật, giống `AR-j`.
- [ ] **B2a-2 — Không nạp evidence chứa payload hoá đơn.** An toàn nhất, mất nhiều dữ
      liệu kỹ thuật nhất — mà payload chính là chỗ có mã lỗi để tra.
- [ ] **B2a-3 — Nạp nguyên trạng, chỉ chặn ở khâu XUẤT BẢN SOP.** Gắn với `AR-l`.

**Chọn một cho THƯƠNG MẠI:**

- [x] **B2b-1 — Cần người duyệt xác nhận trước khi một mẩu có dữ liệu thương mại vào
      kho.** (đề xuất) Không có hình dạng để tự động, nên phải có người.
- [ ] **B2b-2 — Lọc theo NGƯỜI GỬI / loại ticket** (ví dụ ticket của kinh doanh thì
      không nạp). Tự động được, nhưng thô.
- [ ] **B2b-3 — Chưa xử lý, chấp nhận rủi ro có ý thức.**

**Nếu để trống:** mỗi lần nạp corpus mới là một lần công bố thêm thứ chưa ai quyết.

**Ghi chú của bạn:**

>
>

---

## B3 · `AR-k` — Luật gán nhãn thay `machineReadability` hằng số

**Câu hỏi:** trường `machineReadability` gán bằng luật nào?

⚠ **Đây là BUG THẬT, không phải câu hỏi thiết kế.** Đo: **128/128** mẩu đều mang nhãn
`High`, kể cả mẩu 5 ký tự chỉ chứa `"80771"`, mẩu 9 ký tự chỉ chứa chính mã case của nó,
và mẩu 35 ký tự chỉ chứa tên file. Ai viết `WHERE machineReadability = 'High'` rồi báo
*"đã lọc rồi"* thì vừa lọc xong **100% dữ liệu**.

**Luật đã rút ra từ đây** (đã thành phép kiểm chạy được): với mọi trường dùng làm ranh giới
hay bộ lọc, **đếm số giá trị PHÂN BIỆT, không đếm độ phủ. Một giá trị = coi như rỗng.**

**Chọn một:**

- [x] **B3-1 — Bên GỬI khai, hệ thống không suy; gõ sai là 400.** (đề xuất — và đây đã là
      hành vi hiện tại của endpoint, xem `IM-19`) Việc còn lại chỉ là **sửa script export**
      cho nó khai đúng thay vì khai `High` cho mọi mẩu.
- [ ] **B3-2 — Hệ thống TỰ suy từ nội dung** (độ dài, tỉ lệ ký tự, có phải log/payload
      không). Tiện hơn, nhưng là một phép đoán nữa và phải đo recall của chính nó.
- [ ] **B3-3 — Bỏ hẳn trường này.** Một trường không ai điền đúng thì tệ hơn không có,
      vì nó *trông như* đã có dữ liệu.

**Nếu để trống:** trường này tiếp tục là một cái bẫy nằm chờ người đầu tiên dùng nó để lọc.

**Ghi chú của bạn:**

>
>

---

# NHÓM C — chưa chặn, nhưng đừng để quên

## C1 · `AR-e` — Xác thực người gọi ở chế độ nhiều khách hàng

**Câu hỏi:** chế độ shared multi-tenant xác thực người gọi bằng cách gì?

**Hiện trạng:** tenant đến từ header `X-Tenant-Key`, và **không có gì kiểm người gọi có
quyền dùng khoá đó** — biết khoá là đọc được dữ liệu. Chế độ này vì thế **từ chối khởi
động** trừ khi có cờ thừa nhận tường minh. Cờ đó không bảo vệ gì; nó chỉ làm việc deploy
một API chưa xác thực thành **quyết định** thay vì **sơ suất**.

⚠ **Không chặn khách hàng đầu tiên** — bản deploy riêng lấy tenant từ cấu hình, không từ
người gọi. Nhưng nó chặn khách hàng thứ hai.

**Chọn một:**

- [x] **C1-1 — API key theo từng khách hàng**, thu hồi được riêng. (đề xuất — rẻ nhất mà
      giải đúng vấn đề: phân biệt được khách A với khách B, và thu hồi được)
- [ ] **C1-2 — mTLS.** Chắc hơn, vận hành nặng hơn.
- [ ] **C1-3 — Chữ ký trên payload.** Chống được replay, phức tạp nhất cho bên tích hợp.
- [ ] **C1-4 — Hoãn: chỉ bán bản deploy riêng, chưa làm shared.**

**Ghi chú của bạn:**

>

---

## C2 · `AR-i` — Chuỗi kết nối và mật khẩu DB ở deploy thật

**Hình dạng đã có:** host đọc `ConnectionStrings:Default` từ `IConfiguration`, nên biến môi
trường hoặc secret provider nào của .NET cũng cắm được **mà không sửa code**, và thiếu nó
là không start được. Còn phải chọn **nguồn**.

**Chọn một:**

- [x] **C2-1 — Biến môi trường** (`ConnectionStrings__Default`). (đề xuất cho khách hàng
      đầu tiên — đủ dùng, không thêm hạ tầng)
- [ ] **C2-2 — Azure Key Vault / AWS Secrets Manager.**
- [ ] **C2-3 — HashiCorp Vault.**
- [ ] **C2-4 — Theo chuẩn sẵn có của hạ tầng ezCloud** — ghi rõ ở phần ghi chú.

**Ghi chú của bạn:**

>

---

## C3 · `AR-l` — Ranh giới KHÁCH SẠN A ↔ KHÁCH SẠN B

**Trạng thái:** đã chốt **nhánh lùi** 2026-09-07 — không phải vì đổi ý về ranh giới, mà vì
đo xong thì **không có nguồn nào để điền**: `Mã khách sạn` là hằng số `-1.0` ở 32/32 case,
`Tên khách sạn / Resort` thì 25/32 rỗng và phần còn lại ghi tên **team**.

⚠ **Câu hỏi còn lại không phải "có phải ranh giới bảo mật không"** (đã chốt: PHẢI), mà là
**khi nào mở lại, và mở bằng nguồn nào.**

**Hôm nay đang được TRÁNH bằng code:** chế độ shared **từ chối duyệt** (409). Đó là tránh,
không phải giải.

**Chọn một:**

- [x] **C3-1 — Thêm một trường BẮT BUỘC ở đường nạp**, bên gửi phải khai mã khách sạn.
      (đề xuất — nguồn duy nhất chắc chắn có giá trị phân biệt)
- [ ] **C3-2 — Sửa quy trình Jira** để trường `Mã khách sạn` được điền thật. Đúng gốc
      nhất, chậm nhất, và phụ thuộc người khác.
- [ ] **C3-3 — Suy từ tài khoản/email trong ticket.** ⚠ Đã đo là **không đáng tin**: chỉ
      8/32 case có mã trong tiêu đề, và tên trong tiêu đề lệch với thân bài.
- [ ] **C3-4 — Giữ nhánh lùi**, và chấp nhận rằng chế độ shared vĩnh viễn không duyệt được
      cho tới khi có nguồn.

**Ghi chú của bạn:**

>

---

## C4 · `AR-g` — Đường ĐỌC evidence ra

**Hiện trạng:** có đường GHI mà **không có đường ĐỌC** — không endpoint nào trả về evidence
của một case. Bộ test phải mở thẳng `AppDbContext` để kiểm.

⚠ Ngày 13 đã thêm `GET /knowledge/{id}` (đọc tri thức + lịch sử assertion). Nhưng
**evidence thì vẫn chưa có đường ra** — hai thứ khác nhau.

**Chọn một:**

- [x] **C4-1 — `GET /cases/{sourceReference}/evidence`.** (đề xuất — khớp cách bên gửi đã
      biết về case, không bắt họ nhớ thêm id nào)
- [ ] **C4-2 — `GET /evidence?caseSourceReference=…`** kèm phân trang.
- [ ] **C4-3 — Chưa cần.** Evidence chỉ phục vụ nội bộ cho Path A, chưa ai cần đọc ra.

**Ghi chú của bạn:**

>

---

# Việc KHÔNG phải quyết định, nhưng đang chặn

Ghi ở đây để không lẫn với mười câu trên — chúng cần **hành động**, không cần lựa chọn:

```text
[ ]  Nạp credit Anthropic          cần tổ chức duyệt. Chặn: định tuyến thật, sinh nháp
                                    cho 18 nhóm còn lại. Khởi động sớm vì có ĐỘ TRỄ.
[ ]  Xoay mật khẩu VNPT             nêu từ 2026-09-04, vẫn chưa làm. Đây là cách RẺ NHẤT
                                    làm phần lớn credential trong git history thành vô hại.
[ ]  Gửi gói duyệt cho người        docs/16 — bản máy sinh, đã sẵn sàng gửi.
     làm support                    Đây là vế B của M2, và không code ra được.
[ ]  Gửi khảo sát vận hành          docs/15 — bảy câu về cách support thật sự làm việc.
```

---

# Sau khi điền

```bash
# 1. Kiểm file đã điền đúng chưa (bắt lỗi tích hai ô, đếm câu đã quyết)
python scripts/doc_quyet_dinh.py docs/14_QUYET_DINH_CHO_CHU_DU_AN.md

# 2. Rồi báo tôi — tôi chép quyết định + LÝ DO sang docs/07 §5, và code phần nào code được.
```

⚠ Đừng để bước 2 rơi. Một quyết định chỉ nằm trong file này là một quyết định **sẽ mất** —
đó chính xác là điều đã xảy ra với `docs/02_PRODUCT_FOUNDATION_V1.md`.
