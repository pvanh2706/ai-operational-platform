# 18 — QUYẾT ĐỊNH ĐỂ ĐI HẾT MỨC 3

> **Sinh 2026-09-16.** Mười bảy câu chặn việc *"code liên tục tới khi hoàn thành mức 3"*
> (mức 3 = **bán được cho khách thứ hai**, xem `docs/00` §"Việc tiếp theo").
>
> File này KHÁC `docs/14`: `14` hỏi những câu domain/pháp lý đã tồn tại từ trước, còn file
> này hỏi những câu **sinh ra từ mục tiêu tích hợp**. Ba câu trong đây (`H1`-`H3`) là câu
> mà `AGENT.md` §10.1 **cấm agent tự chọn** — hạ tầng, deployment, hosting.
>
> **Cách điền:** đánh `[x]` vào ô mình chọn. Để trống = **chưa quyết**, và đó là một câu
> trả lời hợp lệ. Mỗi câu chọn **đúng một** ô, trừ chỗ ghi rõ khác.
>
> **Kiểm lại sau khi điền:**
> ```bash
> python scripts/doc_quyet_dinh.py docs/18_QUYET_DINH_MUC_3.md
> ```
>
> ⚠ **File này KHÔNG phải nơi lưu quyết định lâu dài.** Điền xong thì quyết định phải được
> chép sang `docs/07` §5 kèm **lý do**, giữ nguyên lập luận cũ. Mất phần *"vì sao"* là mất
> mát lớn nhất — đã xảy ra một lần với `docs/02`.

---

## Trạng thái

```text
NHÓM D  đường TRẢ KẾT QUẢ NGƯỢC      ✅ 5/5 CHỐT 2026-09-16   · D5 do chính D3 sinh ra
NHÓM E  định tuyến thật + chi phí     ✅ 3/3 CHỐT 2026-09-16
NHÓM F  hai loại tín hiệu còn lại     ✅ 2/2 CHỐT 2026-09-16
NHÓM G  AR-l — ranh giới khách sạn    ✅ 1/1 CHỐT 2026-09-16
NHÓM H  bốn câu §10.1 bắt buộc hỏi    ✅ 5/5 CHỐT 2026-09-16   · H1b do chính H1 sinh ra
NHÓM I  phạm vi nghiệm thu            ✅ 2/2 CHỐT 2026-09-16
NHÓM J  cách làm việc                 ✅ 2/2 CHỐT 2026-09-16
                                      ────────────────────────
                                      ✅ 20/20 — điền xong trong MỘT phiên
NHÓM N  hoàn thiện sơ đồ luồng        ✅ 4/7 CHỐT 2026-10-10   · 3 câu PROPOSED chờ chủ dự án
        (thêm 2026-10-10, sinh từ việc MỞ LẠI I1)
NHÓM O  giả lập 2 luồng + 2 cách gán AI ✅ 6/6 CHỐT 2026-10-19   · O5 sinh ra lúc đọc code
        (thêm 2026-10-19, từ câu hỏi "đã hoàn thiện chưa / test bằng dữ liệu giả")
NHÓM P  giao diện Vue 3 — luồng chạy    ✅ 6/6 CHỐT 2026-10-19   · console nội bộ, KHÔNG phải widget
```

🛑 **20/20 KHÔNG nghĩa là hết chặn.** Ba thứ còn chặn và **không thứ nào gỡ được bằng một
quyết định** — chúng cần tài nguyên hoặc cần người khác làm:

```text
TÀI NGUYÊN  · credit Anthropic (tài khoản chờ tổ chức duyệt) -> chặn định tuyến thật
            · credential Jira trên máy này -> chặn export lại corpus
            · field mã khách sạn trong Jira (G1-1) -> chặn đóng AR-l
NGƯỜI KHÁC  · support trả gói duyệt docs/12, 16, 17 -> kho vẫn 0/19 nhóm được người duyệt
```

⚠ **Một dữ kiện để người đọc sau tự cân:** cộng dồn với `docs/14`, đã có **23 lượt chọn**,
trong đó **22 rơi đúng vào phương án `(đề xuất)`**. Các đề xuất đều có số đo chống lưng —
nhưng 22 lần đồng thuận **không phải** 22 lần cân nhắc độc lập.
✅ **Lượt thứ 23 (`H1`) là lượt đầu tiên chủ dự án tự viết ra một phương án khác** — và nó
lập tức phơi ra hai chỗ va chạm mà cả bốn ô agent đưa đều không chạm tới (xem `H1`). Đó
chính là thứ mục cảnh báo này lo thiếu.

## 🎯 Một hình dạng CHUNG lộ ra sau 9 câu — ghi ra vì không câu nào tự nó cho thấy

Ba quyết định độc lập nhau cùng đòi **một thứ mà hệ thống hôm nay KHÔNG CÓ**: biết người
gọi là ai.

```text
D3-1 tóm tắt + link      -> link phải biết ai mở được          -> D5
E3-1 chỉ người có quyền  -> phải biết người bấm là ai
S7   nhãn quyền xem      -> đã có nhãn, chưa có ai để đối chiếu
```

Hôm nay `X-Tenant-Key` chỉ nói *"request này thuộc công ty nào"*, **không nói người nào**.
→ Phần xác thực NGƯỜI (khác `AR-e`, vốn là xác thực HỆ THỐNG gọi vào) bị kéo lên sớm hơn
kế hoạch cũ, và nó là **tiền đề chung** của cả ba, không phải chi tiết của riêng câu nào.

---

# NHÓM D — đường TRẢ KẾT QUẢ NGƯỢC · ✅ CHỐT 2026-09-16

**Vì sao nhóm này đi trước:** đây là chỗ **duy nhất** của cả 11 bước chưa có *cả* quyết định
*lẫn* code. `grep "HttpClient|webhook|callback"` trong `src/` trả về **0 dòng**. Sản phẩm tự
mô tả là *"thức tỉnh, xử lý, và trả kết quả về"* nhưng hôm nay chỉ có `POST /hoi` đồng bộ —
bên kia phải tự hỏi mới có câu trả lời.

## D1 · Kết quả đi từ hệ thống này về phần mềm của khách bằng cách nào?

- [x] **D1-1 — Webhook ra**: hệ thống chủ động `POST` kết quả về URL khách đăng ký.
      (đề xuất — không phải giữ credential của hệ nguồn, và là hình dạng tích hợp mà bên
      thứ ba đọc hợp đồng là cắm được)
- [ ] **D1-2 — Bên kia tự hỏi**: giữ nguyên `POST /hoi`, không xây đường ra.
- [ ] **D1-3 — Ghi thẳng vào ticket**: comment ngược vào Jira bằng token khách cấp.
- [ ] **D1-4 — Hàng đợi**: đẩy vào queue, bên kia tiêu thụ.

**Hệ quả phải code theo, ghi ra kẻo quên:**

```text
· Webhook là một đường GỬI RA MỚI -> nó PHẢI đi qua cổng che AR-o, không được
  đi vòng. Hôm nay cổng đó mới canh đường response của API.
· Cần URL + secret theo TỪNG tenant -> chạm AR-e nhưng ở chiều NGƯỢC (ký gói
  gửi đi), không thay thế AR-e.
· Phải có khoá idempotency + retry: bên nhận sập thì không được mất kết quả,
  gửi lại không được sinh hai bản. Cùng lý do với chống trùng của Kênh 1.
```

## D2 · Ai là người ĐỌC kết quả trả về?

Câu này quyết định luôn cổng che `AR-o` và nhãn quyền xem `S7`.

- [x] **D2-1 — Chỉ nhân viên support** (nội bộ ezCloud). (đề xuất)
- [ ] **D2-2 — Cả khách hàng cuối** (nhân viên khách sạn).
- [ ] **D2-3 — Cả hai, tách theo nhãn quyền xem.**

**🛑 Hệ quả với `AR-l` — ghi ra, và CỐ Ý không tự đóng:**

Đường rò mà `AR-l` mô tả nằm ở **khâu xuất bản SOP**: Path A gom evidence của nhiều khách
sạn (đúng quyền, RLS xanh) rồi sinh một bản nháp được duyệt và hiện cho mọi người. Nếu
*"mọi người"* chỉ là nhân viên support ezCloud — những người **vốn đã đọc được ticket của
mọi khách sạn trong Jira** — thì bản SOP không đưa dữ liệu qua một ranh giới nào chưa từng
bị vượt.

```text
D2-1 LÀM NHẸ    đường rò qua khâu xuất bản, TRONG một tenant
D2-1 KHÔNG ĐỤNG cổng 409 — cổng đó canh ranh giới TENANT (khách A vs khách B),
                không phải sub-tenant. Ranh giới khác, cơ chế khác.
D2-1 CÒN DƯ     support xử lý khách sạn A đọc SOP có chi tiết của khách sạn B rồi
                nói lại cho A. Đó là đường NGƯỜI, không phải đường hệ thống — và
                nó đúng hình dạng mà AR-n B2b đã gọi tên (giá hợp đồng, hạn hợp
                đồng, ý định rời bỏ).
```

→ `AR-l` vì thế **vẫn mở**, và câu `G1` bên dưới vẫn phải trả lời.

## D3 · Một lượt trả về mang theo cái gì?

- [x] **D3-1 — Tóm tắt + link** mở bản đầy đủ. (đề xuất)
- [ ] **D3-2 — SOP đầy đủ** trong chính gói webhook.
- [ ] **D3-3 — Chỉ nhóm nguyên nhân + độ tin**, không kèm nội dung.

**🛑 Câu này TỰ SINH RA một câu chặn mới — xem `D5`.** Một cái link thì phải có chỗ để mở,
và chỗ đó phải biết người mở là ai. Hôm nay `GET /knowledge/{id}` chỉ đòi header
`X-Tenant-Key`: **biết khoá là đọc được**. Chọn `D3-1` nghĩa là kéo phần xác thực NGƯỜI ĐỌC
lên sớm, chứ không đẩy nó về sau như kế hoạch cũ.

## D4 · Khi cổng KHÔNG BIẾT chặn thì trả gì?

- [x] **D4-1 — Nói thẳng KHÔNG BIẾT**, kèm lý do nào đã chặn. (đề xuất — khớp `G3.4`
      *Unknown is First-Class Data*, và `/hoi` hôm nay đã làm đúng vậy)
- [ ] **D4-2 — Im lặng**, không bắn webhook.
- [ ] **D4-3 — Trả nhóm gần nhất** kèm cảnh báo độ tin thấp.

```text
Hai giá trị lyDoKhongBiet đã có sẵn trong code, gói webhook phải mang theo:
  dinh-tuyen-khong-biet          định tuyến không ra nhóm
  nhom-chua-co-sop-duoc-duyet    ra nhóm rồi nhưng nhóm đó chưa có SOP
⚠ D4-1 nghĩa là webhook bắn cho MỌI tín hiệu, kể cả lúc không có gì để nói.
  Hôm nay kho có 0/19 nhóm được người duyệt -> gần như 19/19 sẽ là KHÔNG BIẾT.
```

## D5 · Ai mở được cái link của `D3-1`?

**Sinh ra từ chính `D3`.** Hôm nay không có gì xác thực người đọc: `GET /knowledge/{id}`
chỉ kiểm `X-Tenant-Key`, tức một cái link lọt ra ngoài là đọc được.

- [ ] **D5-1 — Widget Vue3 + đăng nhập riêng** của sản phẩm. (`AR2` đã chốt Vue3+TS cho
      widget nhúng, nhưng chưa có dòng code nào)
- [ ] **D5-2 — Dùng luôn tài khoản Jira** (OAuth/SSO về hệ nguồn) — người đã đăng nhập
      Jira thì mở được, không phải nhớ mật khẩu thứ hai.
- [x] **D5-3 — Link mang token dùng một lần, có hạn** — không cần xác thực người, rẻ nhất.
      (đề xuất — bịt được lỗ *"biết khoá là đọc được"* với ít code nhất, và không chặn
      đường làm widget `D5-1` về sau)
- [ ] **D5-4 — Không xác thực người đọc**, giữ nguyên chốt tenant như hôm nay.

**🛑 Một mâu thuẫn trong chính chữ "một lần" — phải giải trước khi code:**

```text
webhook có RETRY (D1)      gửi lại cùng một link -> token đã tiêu thì lần hai vô nghĩa
SOP là tri thức DÙNG CHUNG  hai support cùng đọc một SOP là chuyện thường, không
                            phải ngoại lệ -> "một lần" cắt mất người thứ hai
```

→ **Mặc định agent sẽ code, chủ dự án bác được bất cứ lúc nào:** token gắn với
`(knowledge_record, tenant)`, **mở được nhiều lần trong hạn**, hạn tính bằng ngày, thu hồi
được. Tức *"một lần"* hiểu là **một lần PHÁT**, không phải một lần MỞ. Lý do: `D2-1` nói
người đọc đều là support nội bộ, nên rủi ro chuyền tay thấp hơn hẳn rủi ro link chết giữa
chừng — và một cơ chế mà người dùng thật thấy *"hỏng"* thì họ sẽ đi đường vòng.

---

# NHÓM E — định tuyến thật + chi phí model · ⬜ CHƯA TRẢ LỜI

## E1 · Ngưỡng cổng KHÔNG BIẾT — thà im lặng hay thà đoán?

Hôm nay cổng là **nhị phân** (có hay không có SOP đã duyệt), chưa có ngưỡng nào được đo.
Số đã đo ở `docs/13`:

```text
88 case CÓ NHÃN     gán đúng 74/88  84,1%   ·  precision khi dám gán 74/79  93,7%
62 case HỒ SƠ RỖNG  dám gán 25/62   40,3%   <- tự tin KHÔNG kiểm được
ĐƯỜNG CƠ SỞ         đoán nhóm lớn nhất: 11,4%
```

- [x] **E1-1 — Thà im lặng**: chỉ trả lời khi model tự tin cao; chấp nhận nhiều KHÔNG BIẾT.
      (đề xuất — khớp `D4-1` và `G3.4`; chiều nguy hiểm đã ĐO được là chiều đoán bừa trên
      hồ sơ rỗng, không phải chiều im lặng)
- [ ] **E1-2 — Thà đoán**: trả nhóm gần nhất kèm độ tin, để người tự lọc.
- [ ] **E1-3 — Chưa chốt ngưỡng, đo trước** trên gói duyệt có người sửa rồi mới quyết.

⚠ **`E1-1` chưa thi hành được hôm nay, và không phải vì thiếu code.** Bản định tuyến đang
chạy là `DinhTuyenTraSan` — tra bảng đã đo sẵn, **không sinh ra điểm tự tin nào để đặt
ngưỡng lên**. Cổng vì thế vẫn nhị phân cho tới khi có bản gọi model thật (chặn ở credit).
→ Code theo `E1-1` nghĩa là: để ngưỡng thành cấu hình ngay từ đầu, mặc định đặt ở phía im
lặng, và **ghi rõ trong response rằng ngưỡng chưa được đo** — đừng để một số tạm trông như
một số đã đo.

## E2 · Người dùng có được SỬA nhãn định tuyến sai không?

- [x] **E2-1 — Có**, và bản sửa quay lại thành dữ liệu huấn luyện/eval. (đề xuất — `docs/13`
      đo được **4/5** lỗi định tuyến là **seam giữa hai nhóm kề nhau**, không có lỗi nào
      hoang đường. Nhãn do người sửa vì thế là vật liệu để sửa chính ranh giới taxonomy —
      quan sát độc lập thứ tư về cùng một điều, sau `docs/11` §9, `AR-p` và `docs/13` §3)
- [ ] **E2-2 — Có**, nhưng chỉ ghi nhận, không dùng lại.
- [ ] **E2-3 — Không**, ngoài phạm vi mức 3.

## E3 · Sinh nháp SOP tốn tiền — ai được bấm, trần bao nhiêu?

- [x] **E3-1 — Chỉ người có quyền duyệt**, có trần tháng. (đề xuất — người tiêu tiền cũng
      là người chịu trách nhiệm duyệt chính bản nháp đó)
- [ ] **E3-2 — Ai cũng bấm được**, có trần tháng.
- [ ] **E3-3 — Chạy theo lô offline**, không có nút bấm lúc chạy.

⚠ **`E3-1` đòi một thứ hệ thống chưa có: biết người bấm là ai.** Xem mục *"hình dạng chung"*
ở đầu file — đây là chỗ thứ hai, sau `D5`, cùng đòi phần xác thực NGƯỜI.

---

# NHÓM F — hai loại tín hiệu còn lại của Kênh 1 · ⬜ CHƯA TRẢ LỜI

## F1 · Tín hiệu "đổi trạng thái" đến thì hệ thống LÀM GÌ?

Hôm nay Kênh 1 dừng ở ô *"tìm hoặc tạo Case"* (`IM-15`) và response nói đúng điều đó.

- [x] **F1-1 — Cập nhật Case, không làm gì thêm.** (đề xuất — và lý do là một SỐ ĐO, không
      phải sở thích: corpus 12 tháng có 100% case đã đóng mà chỉ **1,6 mẩu dùng được mỗi
      case**, mỏng hơn corpus 4 ngày vốn chỉ 22% đã đóng nhưng được **2,5**. Kích hoạt gom
      lúc đóng case là build đúng cái bộ lọc *"chỉ lấy case đã đóng"* mà dự án đã đo là vứt
      đúng phần có giá trị)
- [ ] **F1-2 — Case đóng thì kích hoạt gom tri thức** từ case đó.
- [ ] **F1-3 — Ngoài phạm vi mức 3.**

## F2 · "Người dùng hỏi về tài liệu" đi vào đường nào?

- [x] **F2-1 — Dùng luôn `POST /hoi`** đã có. (đề xuất — một cửa cho mọi câu hỏi, một cổng
      KHÔNG BIẾT, và cổng che `AR-o` chỉ phải canh một đường ra thay vì hai)
- [ ] **F2-2 — Đường riêng**, vì câu hỏi về tài liệu khác câu hỏi về sự cố.
- [ ] **F2-3 — Ngoài phạm vi mức 3.**

---

# NHÓM G — `AR-l`, ranh giới khách sạn A ↔ B · ⬜ CHƯA TRẢ LỜI

## G1 · Lấy đâu ra ≥2 giá trị phân biệt cho `subTenantCode`?

Đo trên `kp_dev` sau khi nạp lại bằng fixture: **345 mẩu khai `khong-xac-dinh`**,
`soMaPhanBiet` = **0**, `datDieuKienMoLai` = **FALSE**. Connector Jira **không có gì để
khai** (`customfield_12710` = `-1.0` ở 32/32).

- [x] **G1-1 — Thêm field bắt buộc vào Jira ezCloud**, support điền khi tạo ticket.
      (đề xuất — nguồn DUY NHẤT thật sự sinh ra dữ liệu, và khớp với chốt `C3-1` *"bên gửi
      khai"* ở `docs/14`)
- [ ] **G1-2 — Mỗi khách sạn một tenant riêng**, bỏ hẳn khái niệm sub-tenant.
- [ ] **G1-3 — Suy từ email/tên miền** của người báo. (⚠ đã đo là **không dùng được**:
      chỉ 8/32 case có mã trong tiêu đề, và tên lệch với thân bài)
- [ ] **G1-4 — Chấp nhận `D2-1` là đủ** cho mức 3, ghi rõ `AR-l` còn mở và hoãn.

**🛑 `G1-1` KHÔNG gỡ được bằng code, và nó không nằm trên đường tới hạn của code.**

```text
phía CODE đã xong    endpoint đã BẮT BUỘC subTenantCode từ IM-35, bỏ trống là 400
phía DỮ LIỆU chưa    field chưa tồn tại trong Jira -> chưa ai khai được -> vẫn 0 giá trị
                     phân biệt -> datDieuKienMoLai vẫn FALSE -> cổng 409 GIỮ NGUYÊN
```

→ Nghĩa là agent **code tiếp được ngay**, nhưng `AR-l` chỉ đóng vào ngày có người thêm
field đó vào Jira **và** export lại corpus. Hai việc của tổ chức, không phải của repo.
Ba mốc phải xảy ra theo đúng thứ tự: thêm field → export lại → `soMaPhanBiet` ≥ 2 → mới
được bàn tới chuyện gỡ cổng 409.

---

# NHÓM H — bốn câu `AGENT.md` §10.1 BẮT BUỘC hỏi · ⬜ CHƯA TRẢ LỜI

⚠ §10.1 xếp hạ tầng / deployment / hosting / thư viện lớn vào **quyền của người dùng**.
Agent không được tự chọn, kể cả khi lựa chọn có vẻ hiển nhiên.

## H1 · Deploy ở đâu?

- [ ] **H1-1 — Máy chủ ezCloud tự quản**, chạy như service.
- [ ] **H1-2 — Docker** trên hạ tầng ezCloud. *(agent đề xuất, KHÔNG được chọn)*
- [ ] **H1-3 — IIS trên Windows Server.**
- [ ] **H1-4 — Cloud** (ghi rõ nhà cung cấp ở phần ghi chú).
- [x] **H1-5 — Chạy LOCAL trên Windows trước**, hoãn quyết định hạ tầng thật.

⚠ **`H1-5` do chủ dự án tự viết ra, không nằm trong bốn phương án agent đưa** (2026-09-16).
Đây là lượt **đầu tiên** trong 23 lượt không rơi vào ô `(đề xuất)` — ghi ra vì mục cảnh báo
ở đầu file nói đúng về rủi ro đó, và lần này nó không xảy ra.

**🛑 `H1-5` va vào `D3-1` và vào Kênh 1 — hai chỗ, và chúng KHÁC NHAU:**

```text
webhook RA (D1-1)        máy local gọi ĐI  -> CHẠY ĐƯỢC, không cần địa chỉ công khai
nạp corpus hôm nay       script PULL Jira rồi PUSH vào API local -> CHẠY ĐƯỢC
Jira tự PUSH lúc chạy    Jira phải GỌI TỚI được máy đó  -> ⛔ localhost không nhận được
link của D3-1            support bấm link trên máy CỦA HỌ -> ⛔ http://localhost:5119
                         chỉ mở được trên chính máy chạy app
```

→ Hai dòng đầu chạy được ngay, nên **`H1-5` không chặn việc code**. Hai dòng cuối cần đúng
một thứ: **một địa chỉ mà máy của support tới được** (LAN, VPN, hoặc hostname nội bộ) —
không cần cloud, không cần domain công khai. Xem `H1b`.

## H1b · Máy chạy local đó, support có tới được không?

- [x] **H1b-1 — Có, máy nằm trong LAN/VPN** và support mở được bằng hostname nội bộ.
- [ ] **H1b-2 — Không, chỉ mình chủ dự án dùng** — link của `D3-1` tạm thời chỉ để tự thử.
- [ ] **H1b-3 — Chưa biết**, quyết sau khi có bản chạy được.

✅ **`H1b-1` cứu được `D3-1`:** link dùng thật được, không phải chỉ để tự thử. Và nếu Jira
của ezCloud là bản self-hosted trong cùng mạng thì cả chiều *Jira gọi tới* cũng thông —
tức `H1-5` không cắt mất thứ gì của mức 3.

⚠ **Một luật code phải theo, ghi ra vì làm sai rất dễ mà không ai thấy:** địa chỉ gốc của
link **phải là cấu hình** (`PublicBaseUrl`), **không được suy từ host của request**. Suy từ
request thì lúc chạy trên máy đó nó sinh ra `http://localhost:5119/...` — chạy thử tại chỗ
thấy đúng, gửi cho người khác thì chết. Đúng hình dạng lỗi im lặng mà cả dự án nhắm vào.

## H2 · Postgres production ở đâu, ai giữ backup?

- [ ] **H2-1 — Cùng máy với ứng dụng**, ezCloud tự backup.
- [x] **H2-2 — Máy DB riêng của ezCloud**, đội hạ tầng giữ backup. (đề xuất — RLS là nền
      bảo mật của cả sản phẩm, nên quyền sở hữu backup phải rõ chứ không để mặc định)
- [ ] **H2-3 — Postgres dịch vụ** của nhà cung cấp cloud.

⚠ **`H2-2` là đích, `H1-5` là hiện tại — hai mốc khác nhau, đừng gộp.** Giai đoạn chạy
local thì DB vẫn là `kp_dev` trên chính máy đó. Điều `H2-2` ràng buộc là **code không được
giả định DB nằm cùng máy**: chuỗi kết nối đọc từ biến môi trường (`AR-i`/`C2-1` đã chốt),
không hằng số `localhost` nào lọt vào. Đây đúng loại giả định mà `06` §10 ràng buộc số 2
cảnh báo: rẻ để sửa hôm nay, rất đắt khi đã có ba khách hàng.

## H3 · Blob storage cho Kênh 2?

`AR5` mới chốt *"blob storage"* ở mức khái niệm, chưa chốt cái nào.

- [ ] **H3-1 — Đĩa local** của máy chủ ứng dụng.
- [x] **H3-2 — MinIO** tự dựng (S3-compatible). (đề xuất — nói giao thức S3 nên đổi sang
      S3/Azure về sau không phải sửa code, và dữ liệu vẫn nằm trong nhà)
- [ ] **H3-3 — S3 / Azure Blob.**
- [ ] **H3-4 — Hoãn**: Kênh 2 ngoài phạm vi mức 3.

⚠ **`H3-2` ngầm trả lời luôn `I1`-lân cận: Kênh 2 NẰM TRONG mức 3.** Chọn nơi lưu file tức
là có ý định nhận file. Nếu thật ra muốn hoãn Kênh 2 thì câu này phải quay về `H3-4` — ghi
ra để hai câu không lệch nhau về sau.

## H4 · Hợp đồng tích hợp sinh bằng gì?

Hôm nay: Swagger/OpenAPI = **0 dòng**, Postman phủ **5/13** endpoint và đứng từ 2026-08-30.

- [x] **H4-1 — Thư viện sinh tự động.** (đề xuất — `IM-38` đã ĐO được rằng bản viết tay
      tự lỗi thời trong im lặng: Postman đứng nguyên từ 30-08 trong khi sản phẩm đi từ 5
      lên 13 endpoint)
- [ ] **H4-2 — Viết tay** một file OpenAPI, không thêm dependency.
- [ ] **H4-3 — Không OpenAPI**, chỉ cập nhật bộ Postman cho đủ 13 endpoint.

✅ **Câu "thư viện NÀO" tự giải, và nó KHÔNG thêm dependency bên thứ ba.** Cả solution đang
ở `net10.0`, nên gói `Microsoft.AspNetCore.OpenApi` là **first-party của .NET**, không phải
Swashbuckle hay NSwag (Swashbuckle đã bị gỡ khỏi template .NET từ bản 9). Lo ngại *"thêm
một dependency"* ghi trong ô `H4-1` vì thế không còn đúng.
→ Agent sẽ dùng gói first-party đó trừ khi chủ dự án nói khác.

⚠ **OpenAPI KHÔNG thay được `kiem_dau_cuoi.py`.** Nó tả *hình dạng* endpoint, không chứng
minh endpoint **chạy**. Đúng chỗ `IM-38` đã đo: 8/13 endpoint chưa từng được gọi bởi thứ gì
ngoài bộ test C#, và một file mô tả đẹp đẽ không làm con số đó nhỏ đi.

---

# NHÓM I — phạm vi nghiệm thu · ⬜ CHƯA TRẢ LỜI

## I1 · `ProcessDefinition` / `ProcessRun` có nằm trong mức 3 không?

Thiết kế xong ở `docs/05` §5, chưa có dòng code nào. Đây là bước **nặng nhất** còn lại, và
nó **không** chặn tích hợp.

- [ ] **I1-1 — Không**, để sau mức 3. (đề xuất — bước nặng nhất còn lại, và nó KHÔNG chặn
      tích hợp) · *chốt 2026-09-16 · **MỞ LẠI 2026-10-10**, xem dưới*
- [x] **I1-2 — Có**, mức 3 phải có Capability 2.
- [ ] **I1-3 — Chỉ phần đọc** (xem định nghĩa quy trình), chưa chạy được.

🔄 **MỞ LẠI 2026-10-10 — chuyển từ `I1-1` sang `I1-2`.** Chủ dự án yêu cầu hoàn thiện code cho
**mọi ô** của sơ đồ README §"Luồng chạy khi có một tín hiệu" — trong đó ba ô `D` `E` `F` chính là
Capability 2. Lập luận của `I1-1` bên trên **không sai và không bị xoá**: nó vẫn đúng rằng đây là
bước nặng nhất và không chặn tích hợp. Cái đổi là *mục tiêu*, không phải *số đo*. Các câu sinh
ra từ việc mở lại nằm ở **NHÓM N** cuối file.

✅ **`I1-1` cắt mức 3 từ 11 bước xuống 10.** ⚠ Đừng nhầm hai thứ tên gần giống nhau:
`Capability 2` (chạy quy trình) **ra khỏi** phạm vi, còn `Kênh 2` (nạp tài liệu) **vẫn nằm
trong** — `H3-2` chọn nơi lưu file tức là có ý định nhận file. Hai câu không mâu thuẫn.

## I2 · Nghiệm thu mức 3 bằng gì?

- [x] **I2-1 — Onboard được khách thứ hai** mà không sửa code. (đề xuất — kiểm được bằng
      chính code, không phụ thuộc lịch trả lời của ai)
- [ ] **I2-2 — Đạt `M1`/`M2`** của `docs/02_SUCCESS_METRICS_V1.md`.
- [ ] **I2-3 — Cả hai.**

**🎯 `I2-1` biến hai thứ từ "nên có" thành "định nghĩa của xong":**

```text
AR-e  API key theo từng khách   -> KHÔNG onboard được khách thứ hai nếu thiếu.
                                   Nó thôi là việc tồn đọng, nó LÀ thước đo.
phép kiểm nghiệm thu            -> phải chạy lại được, kiểu kiem_dau_cuoi.py nhưng
                                   dựng HAI tenant và chứng minh chúng không thấy nhau
```

⚠ **Và một chỗ `I2-1` KHÔNG đo, ghi ra kẻo đọc nhầm "xong" thành "dùng được":** nó đo
*onboard được*, không đo *trả lời có ích*. Kho hôm nay có **0/19** nhóm được người support
duyệt, nên một khách thứ hai onboard xong vẫn nhận `KHÔNG BIẾT` cho gần hết câu hỏi. `M1`/
`M2` mới là thước đo điều đó, và nó nằm ngoài tầm code — xem `docs/00` §"nút cổ chai".

---

# NHÓM J — cách làm việc trong lúc code liên tục · ✅ CHỐT 2026-09-16

Hai câu này không phải quyết định sản phẩm, nhưng chúng quyết định chữ **"liên tục"** có
thật hay không. Ghi vào đây thay vì để trong một lượt chat sẽ trôi mất.

## J1 · Agent được tự quyết tới đâu?

- [x] **J1-1 — Tự quyết câu KỸ THUẬT, luôn dừng ở §10.1.** (đề xuất)
- [ ] **J1-2 — Hỏi trước mọi thay đổi có migration.**
- [ ] **J1-3 — Hỏi trước từng bước.**

```text
TỰ QUYẾT        đặt tên · cấu trúc file · migration · cách viết test · refactor
                -> ghi lý do vào docs/07 §3, KHÔNG dừng lại hỏi
LUÔN DỪNG HỎI   ngôn ngữ · framework · database · LLM/model · hạ tầng · deployment
                · thư viện lớn        (AGENT.md §10.1, không có ngoại lệ)
CÂU SẢN PHẨM MỚI  gom vào chính file này thành câu mới, CHỈ dừng khi nó chặn cứng
```

⚠ **Chỗ dễ trượt của `J1-1`, ghi ra để tự canh:** ranh giới *"kỹ thuật"* và *"sản phẩm"*
không phải lúc nào cũng rõ. Luật để phân: nếu quyết định đó **đổi thứ người dùng thấy hoặc
đổi thứ hệ thống hứa**, nó là câu sản phẩm — kể cả khi nó trông như một dòng code. Cổng
409 của `AR-l` từng là đúng loại đó.

## J2 · Commit thế nào?

- [x] **J2-1 — Commit thẳng `main` theo chủ đề.** (đề xuất — chống đúng sự cố đã xảy ra:
      47 file nằm ngoài git 4 ngày, gồm cả code lẫn phiếu quyết định)
- [ ] **J2-2 — Nhánh riêng, mở PR.**
- [ ] **J2-3 — Không tự commit.**

⚠ **`J2-1` đi kèm một nghĩa vụ**, nếu không nó thành đường rò: `0e602c3` đã từng phải gỡ
credential thật khỏi 5 file ĐANG ĐƯỢC THEO DÕI, và tìm ra nhờ quét TRƯỚC LÚC PUSH.
→ Chạy `python scripts/quet_credential_repo.py` trước mỗi commit chạm tới dữ liệu hay
tài liệu sinh từ corpus. Đây là luật tự áp, không phải tuỳ hứng.

---

# NHÓM N — hoàn thiện MỌI ô của sơ đồ luồng tín hiệu · ✅ 4/7 CHỐT 2026-10-10

Sinh ra từ việc **mở lại `I1`** (xem trên). Bốn câu đầu chủ dự án chọn qua form ngày 2026-10-10.
Ba câu sau là chỗ agent PHẢI chọn một hướng để code chạy được, nhưng chúng **đổi thứ người dùng
thấy** — nên theo `J1-1` chúng là câu sản phẩm: code theo phương án `(PROPOSED)`, và chủ dự án
bác được bất cứ lúc nào. Kế hoạch đầy đủ, 11 phase: `docs/00` §"Việc tiếp theo".

## N1 · "Tạm bỏ qua việc ẩn dữ liệu (tài khoản, mật khẩu…)" nghĩa là gì?

- [ ] **N1-1 — Không build thêm che, bỏ chặn lô** ở giao diện quét Jira; giữ cổng che đường ra.
      (đề xuất)
- [x] **N1-2 — Tắt tạm MỌI cổng che bằng MỘT cờ cấu hình**, mặc định tắt cờ (= cổng vẫn chạy).
- [ ] **N1-3 — Chỉ không build thêm**, giữ nguyên mọi cổng.

🛑 **`N1-2` mở lại điều kiện chủ dự án tự đặt ngày 2026-09-08** (`07` §5 — chỉ gửi dữ liệu ticket
cho model *"nếu luật che chạy trên payload trước mỗi lần gọi"*), và nới `AR-o` + `AR-n` B2a **có
điều kiện**. Ba hệ quả code phải giữ, ghi ra để không ai "sửa cho gọn":
```text
1  khâu NẠP là cửa MỘT CHIỀU: mẩu vào lúc cờ bật thì nằm trong DB KHÔNG CHE, tắt cờ không
   che lại được  ->  mỗi mẩu mang dấu "đã qua cổng che bên thứ ba hay chưa"
2  chế độ SharedMultiTenant TỪ CHỐI KHỞI ĐỘNG khi cờ bật — dữ liệu của nhiều công ty
3  mọi lượt khởi động in cảnh báo; gói webhook và /xem mang daQuaCongChe
```
⚠ Corpus thật có credential ở **5,2%** mẩu evidence (`README` §"Rủi ro đang mở"). Bật cờ trên dữ
liệu đó là gửi các giá trị ấy cho Anthropic và vào hệ thống của khách.

## N2 · Ô `E` — một bước quy trình được coi là ĐÃ XONG khi nào?

- [x] **N2-1 — Dấu hiệu trong evidence + người xác nhận.** (đề xuất) Bước khai vài cụm từ; evidence
      chưa rút lại của case chứa cụm đó thì coi là xong. Người/connector khai "đã làm bước K1" thì
      lời khai được lưu thành **EVIDENCE có tác giả** (`PR1`: *"tôi đã check mapping rồi"* chính là
      evidence), không phải một cờ tiến độ.
- [ ] **N2-2 — Chỉ người xác nhận.**
- [ ] **N2-3 — Chỉ dấu hiệu trong evidence.**

⚠ Bước sinh từ bản nháp SOP **chưa có dấu hiệu nào** (`SopDraft` không có trường đó), nên lúc đầu
chúng chỉ xong được bằng xác nhận. Người duyệt thêm dấu hiệu khi quy trình còn ở DRAFT.

## N3 · Ô `J` — ai báo "gợi ý đã được dùng"? (đóng `L2-a` của `04`)

- [x] **N3-1 — Hệ thống tự ghi 3 nấc đầu + API phản hồi cho 2 nấc cuối.** (đề xuất) Nấc theo từ vựng
      đã khoá `L2`: `RETRIEVED` → `SHOWN` → `OPENED` tự ghi; `ACCEPTED` / `USED` do bên tích hợp gọi
      về, kèm tên người khai.
- [ ] **N3-2 — Chỉ tự ghi 3 nấc đầu.**
- [ ] **N3-3 — Tự suy `USED` từ evidence về sau.**

⚠ `ACCEPTED` / `USED` là **LỜI KHAI**, cùng loại với `nguoiDuyet` và `xacNhanDuLieuThuongMai`:
hệ thống chưa biết người gọi là ai (`docs/18` §"hình dạng chung"). Và `SHOWN` ở đây nghĩa là
*hệ thống đã giao tới bên nhận*, không phải *một người đã nhìn thấy*.

## N4 · Kênh 2 — đọc chữ trong tài liệu và job quét làm tới đâu?

- [x] **N4-1 — Không thêm thư viện.** (đề xuất) Trích chữ txt/md/csv/html/json/xml và `.docx` (đọc
      thẳng XML trong file zip bằng thư viện chuẩn); PDF/ảnh ghi *"chưa đọc được"*; job nền quét một
      thư mục cấu hình — thư mục Drive đồng bộ về máy cũng dùng được.
- [ ] **N4-2 — Thêm thư viện cho `.docx`/`.pdf`** (DocumentFormat.OpenXml + PdfPig, `AGENT.md` §10.1).
- [ ] **N4-3 — Hoãn Kênh 2.**

⚠ **Hệ quả nói trước:** ô `A2` và `K` của sơ đồ sẽ **vẫn là `◐`** — không connector Drive/Jira chạy
trong host, không đọc PDF/ảnh. `N4-1` làm đủ phần đạt được không cần thư viện mới, không hơn.

## N5 · `PR-c` — ai được sửa một quy trình đã ACTIVE? · PROPOSED

- [x] **N5-1 — Không ai.** (PROPOSED — agent chọn để code chạy được) DRAFT sửa được; ACTIVE bất biến;
      muốn đổi thì tạo phiên bản mới.
- [ ] **N5-2 — Người duyệt sửa trực tiếp**, có lịch sử.

## N6 · `PR-e` — định nghĩa đổi trong lúc một case đang chạy dở? · PROPOSED

- [x] **N6-1 — Case giữ phiên bản nó bắt đầu.** (PROPOSED) Duyệt phiên bản mới thì bản cũ thành
      DEPRECATED; `ProcessRun` trỏ đúng id bản cũ, nên tiến độ không tự nhảy.
- [ ] **N6-2 — Case chuyển sang phiên bản mới ngay.**

## N7 · Định tuyến ticket MỚI bằng model tốn tiền ở MỌI ticket — bật thế nào? · PROPOSED

`E3-1` đặt trần cho việc **soạn nháp SOP** (người bấm nút). Định tuyến bằng model thì chạy **tự
động trên mọi webhook và mọi `/hoi`** — `E3` chưa bao giờ phủ loại chi phí này.

- [x] **N7-1 — Cờ riêng `DinhTuyen:GoiModel=true` + trần tháng + biên lai ghi TRƯỚC khi gọi.**
      (PROPOSED) Có khoá API thôi thì CHƯA gọi. Ngưỡng tin cậy mặc định 0,9 nghiêng về phía im lặng
      (`E1-1`), và response ghi rõ *"ngưỡng chưa đo"*.
- [ ] **N7-2 — Có khoá là gọi.**

⚠ Model dùng lại đúng hằng số của bộ soạn nháp (`AR3`). Đổi sang model rẻ hơn cho việc phân loại là
câu `AGENT.md` §10.1 — agent không tự chọn.

---

# NHÓM O — giả lập hai luồng + hai cách gán AI · ✅ 6/6 CHỐT 2026-10-19

Sinh từ câu hỏi của chủ dự án ngày 2026-10-19: *"hệ thống đã hoàn thiện chưa? mình muốn test 2 luồng tín
hiệu bằng dữ liệu giả lập; phần gán token có hai lựa chọn — có API token thì cấu hình và dùng; không có thì
xuất prompt ra file để Claude/Codex đọc, trả kết quả, rồi import lại"*. Chọn qua form, sáu câu, hai lượt.

**Hiện trạng đọc được TRƯỚC khi hỏi** (để người đọc sau thấy câu hỏi đứng trên cái gì):
```text
cách 1 (API)   ĐÃ CÓ: KhoaAnthropic đọc Anthropic:ApiKey / ANTHROPIC_API_KEY; soạn nháp gọi model khi
               có khoá; định tuyến gọi model khi có khoá + DinhTuyen:GoiModel=true (N7-1).
               🛑 BẪY: appsettings.Local.json ở gốc repo CHỈ tools/SoanNhapRunner đọc. Host KHÔNG nạp
                  file đó — khoá chủ dự án đặt sẵn ở đó vô hình với `dotnet run`.
cách 2 (file)  ĐÃ CÓ dạng lệnh rời: --xuat-dinh-tuyen/--nap-dinh-tuyen (IM-45) · --xuat-soan-nhap +
               POST /knowledge/sop-draft (IM-46) · giao diện scripts/giao-dien (riêng cho Jira).
               Chưa có công tắc chọn chế độ; prompt viết cho "dán vào khung chat".
```

## O1 · "2 luồng tín hiệu" là hai luồng nào?

- [x] **O1-1 — Xuôi + ngược.** (đề xuất) Xuôi: tín hiệu → case → định tuyến (AI) → quy trình/bước → gợi ý
      → phản hồi. Ngược: gom case một nhóm → AI soạn nháp SOP → duyệt → `/hoi` trả lời. Cả hai đều chạm AI.
- [ ] **O1-2 — Kênh 1 + Kênh 2.** Kênh 2 không gọi AI (`S6`).
- [ ] **O1-3 — Cả ba.**

## O2 · Cách 1 — khoá API cấu hình ở đâu để host đọc được?

- [x] **O2-1 — Host đọc thêm `appsettings.Local.json` ở gốc repo** (đề xuất), biến môi trường vẫn thắng.
- [ ] **O2-2 — Chỉ biến môi trường**, không sửa code.
- [ ] **O2-3 — Màn hình nhập khoá trên giao diện.**

⚠ **`O2-1` đổi một câu cũ trong code:** chú thích của `KhoaAnthropic` ghi *"Khoá vẫn KHÔNG đi qua
appsettings hay log"*. Lập luận gốc là **file được git theo dõi** (`appsettings.Development.json`) —
không phải mọi file JSON. `appsettings.Local.json` đã nằm trong `.gitignore` từ trước và `SoanNhapRunner`
đã đọc nó kèm cổng `git check-ignore`. Host chép đúng cổng đó: git không chặn file → KHÔNG đọc. Và chỉ đọc
ở môi trường `Development` — bản deploy thật vẫn lấy khoá từ biến môi trường như cũ.

## O3 · API token hỗ trợ nhà cung cấp nào?

- [x] **O3-1 — Chỉ Anthropic.** (đề xuất) SDK đã có trong host. Codex vẫn dùng được qua cách 2 (file).
- [ ] **O3-2 — Anthropic + OpenAI** (thư viện mới — `AGENT.md` §10.1).

## O4 · Cách 2 — xuất/nạp file điều khiển thế nào?

- [ ] **O4-1 — Script giả lập có cờ `--ai api|file`.** (đề xuất)
- [x] **O4-2 — Thêm vào giao diện web** (`scripts/giao-dien`).
- [ ] **O4-3 — Chỉ hướng dẫn lệnh có sẵn.**

✅ **Lượt thứ hai chủ dự án KHÔNG chọn ô `(đề xuất)`** (lượt đầu là `H1`). Hệ quả: giả lập là một TRANG của
giao diện chạy local, không phải một script dòng lệnh — người vận hành nhìn thấy từng bước, chép prompt,
dán kết quả, trên cùng một màn hình với vòng quét Jira đã có.

## O5 · Chế độ API: case do MODEL định tuyến có được gom vào luồng ngược không?

Câu này **sinh ra lúc đọc code**, không có trong câu hỏi gốc. Model định tuyến xong thì kết quả nằm ở
`LuotDinhTuyenModel` (biên lai); còn `SoanNhapHandler.GomAsync` chỉ gom từ `DinhTuyenDaNap` (kho NGƯỜI
nạp). Tức là chạy toàn API thì ticket được model xếp đúng nhóm, nhưng soạn nháp trả `nhom-khong-co-case`.

- [x] **O5-1 — Có, nếu qua ngưỡng.** (đề xuất) Gom thêm case có kết quả model **qua ngưỡng tin cậy + có
      trong taxonomy**, khi người CHƯA nạp đè. Đúng câu trả lời mà `/hoi` đang dùng cho case đó. Kết quả
      soạn nháp nói rõ bao nhiêu case đến từ model.
- [ ] **O5-2 — Không, chỉ người nạp.**
- [ ] **O5-3 — Nút định tuyến hàng loạt** ghi vào kho người-nạp với nguồn `model`.

⚠ **Rủi ro mang theo, không biến mất vì đã chọn:** ngưỡng 0,9 vẫn **CHƯA ĐO** (`N7-1`). `O5-1` cho phép
một nhãn chưa kiểm chứng của model đi thêm một bước — vào vật liệu soạn SOP. Lớp chặn còn lại là người
duyệt bản nháp (`S7`/`AR-n`). Người nạp vẫn thắng model, như thứ tự tra của `DinhTuyenKho`.

## O6 · Dữ liệu giả lập đặt ở đâu?

- [x] **O6-1 — Database riêng `kp_gialap`**, khách hàng `gia-lap`, app thứ hai ở cổng 5120. (đề xuất)
- [ ] **O6-2 — `kp_dev`, mã `gialap:`.**

🛑 **Vì sao không dùng `kp_dev` dù tiện hơn:** host chỉ đọc MỘT taxonomy (19 nhóm hoá đơn thật), nên ticket
giả buộc phải rơi vào nhóm THẬT. Duyệt một quy trình giả cho nhóm X sẽ đẩy quy trình đang ACTIVE của nhóm X
trong `kp_dev` sang DEPRECATED (`N6-1`), và SOP giả sẽ trả lời `/hoi` cho ticket fixture thật cùng nhóm.
Không crash, không đỏ test nào — đúng loại hỏng im lặng dự án này dựng ra để chặn.

---

# NHÓM P — giao diện Vue 3 để nhìn luồng chạy và kết quả · ✅ 4/4 CHỐT 2026-10-19

Sinh từ yêu cầu của chủ dự án ngày 2026-10-19, ngay sau nhóm O: *"mình muốn làm giao diện để nhìn rõ hơn luồng
chạy và kết quả. Dùng vue3 (có thể cài thêm thư viện)"*. Chọn qua form, một lượt.

**Đánh giá đưa ra TRƯỚC khi hỏi** (để người đọc sau thấy câu hỏi đứng trên cái gì):
```text
khớp       Vue3+TS đã chốt ở AR2 cho widget nhúng — không phải framework mới. Máy có Node 22 / npm 10.
           Trang HTML thuần (scripts/giao-dien) in bảng từng bước, không vẽ được một ticket đi qua ô nào.
chỗ khó    🛑 THIẾU DỮ LIỆU, không thiếu giao diện: host KHÔNG có đường liệt kê case, không trả biên lai
           định tuyến của một case, không có lượt chạy / lượt gợi ý theo case (đã kiểm Program.cs).
           Thêm đường đọc = thêm một đường dữ liệu khách đi ra — đúng thứ LenhDinhTuyen cố ý tránh.
ranh giới  README: sản phẩm "không phải ứng dụng người dùng đăng nhập vào". Console nội bộ thì hợp lý;
           không vạch ranh giới thì nó trôi thành giao diện sản phẩm trong khi D5-1 chưa chốt.
chi phí    thêm bước build + node_modules + rủi ro chuỗi cung ứng, trong khi script hiện có cố ý chỉ dùng
           thư viện chuẩn Python.
hiển thị   màn nhiều ✅ dễ đọc thành "đã dùng được" — phải luôn hiện NGUỒN của kết quả (người nạp / bảng
           đo / model) và chữ "chưa đo".
```

## P1 · Giao diện Vue này dùng để làm gì?

- [x] **P1-1 — Console nội bộ** (đề xuất): người vận hành/dev xem luồng chạy + kết quả + chạy giả lập. Chỉ
      nghe loopback. Tách hẳn khỏi widget sản phẩm — `D5-1` vẫn để ngỏ.
- [ ] **P1-2 — Bắt đầu widget sản phẩm** (kéo theo `D5-1` phải chốt trước).
- [ ] **P1-3 — Cả hai, chung codebase.**

## P2 · Màn "luồng chạy" lấy dữ liệu từ đâu?

- [x] **P2-1 — Endpoint đọc `/internal/` mới trên host** (đề xuất): danh sách case theo tiền tố + dòng thời
      gian một case (evidence, tầng định tuyến + biên lai, lượt chạy, gợi ý, phản hồi). Có test, vào hợp
      đồng OpenAPI, đi qua RLS như mọi đường khác.
- [ ] **P2-2 — Chỉ endpoint có sẵn.**
- [ ] **P2-3 — Python đọc thẳng DB** (thêm psycopg, đi vòng luật của host).

⚠ **Hệ quả nói trước:** `P2-1` thêm một đường dữ liệu khách đi ra qua HTTP. Nó nằm sau cùng hai cửa của mọi
endpoint (`ApiKeyEndpointFilter` → `TenantEndpointFilter`) và trả nội dung ĐÃ qua cổng che như
`GET /cases/{k}/evidence`. `kiem_dau_cuoi.py` mục J đòi mọi thao tác trong hợp đồng đều được gọi — nên hai đường
mới phải vào script đó cùng commit.

## P3 · Chạy giao diện ở đâu?

- [x] **P3-1 — Build tĩnh, `ui_server.py` phục vụ** (đề xuất) và chuyển tiếp API sang app. Giữ loopback-only,
      host .NET không cần CORS. Lúc phát triển dùng Vite dev server có proxy.
- [ ] **P3-2 — Host .NET phục vụ** (`UseStaticFiles`, lộ ra cùng chỗ với API).
- [ ] **P3-3 — Vite dev server riêng.**

## P4 · Thêm thư viện nào ngoài Vue 3 + Vite + TypeScript? (tích nhiều ô)

- [x] **P4-1 — Vue Flow** (`@vue-flow/core`) (đề xuất): sơ đồ luồng tương tác, tô sáng ô một ticket đã đi qua.
- [ ] **P4-2 — Naive UI.**
- [x] **P4-3 — Vue Router + Pinia.**

## P5 · `GET /cases/{k}/quy-trinh` ghi "CHỈ ĐỌC" mà gọi model khi bật `GoiModel` — xử lý thế nào?

Câu này **sinh ra lúc code P12-1** (`07` §3 `IM-51`), chủ dự án chọn qua form cùng ngày.

- [x] **P5-1 — Sửa: chỉ xem** (đề xuất). Không gọi model, không ghi biên lai. Case model chưa xếp trả
      `dinh-tuyen-khong-biet` cho tới khi `/hoi` hoặc webhook hỏi nó.
- [ ] **P5-2 — Giữ hành vi, sửa lời mô tả hợp đồng.**
- [ ] **P5-3 — Để sau.**

⚠ Hệ quả cho bên tích hợp: bật model rồi thì đường này KHÔNG còn là cách "đánh thức" định tuyến cho một case.
Muốn case được xếp nhóm thì hỏi `/hoi` (hoặc chờ webhook) — đó vốn là hai đường được thiết kế để tiêu tiền.

## P6 · `SopDraftToKnowledge` bỏ qua `bangTraMaLoi` → bản ghi "KHÔNG CÓ ĐƯỜNG NHẬN RA" — xử lý thế nào?

Câu này **sinh ra lúc BẤM THẬT trang giả lập** (P12-4, `07` §3 `IM-54`), chủ dự án chọn qua form cùng ngày. Bản
nháp dẫn tới bước sửa QUA BẢNG TRA (đúng như prompt soạn nháp dặn) cho ra 3/3 bản ghi mồ côi, 0 assertion Recognition
cho mã lỗi. `kp_dev` chưa bị — bản nháp máy sinh thật tình cờ chép dòng bảng sang nhánh.

- [x] **P6-1 — Sửa ánh xạ** (đề xuất). Mỗi dòng bảng tra = một cách nhận ra (assertion Recognition) cho bước sửa nó
      trỏ tới — đúng `AP4` đang áp cho nhánh, và đúng câu `nhom_sop.py` đã viết từ trước: *"Bảng tra: một DÒNG là
      một nhánh"*. Bản ghi cũ KHÔNG tự đổi (append-only); nạp lại bản nháp thì sinh bản ghi mới.
- [ ] **P6-2 — Sửa prompt**, bắt model chép dòng bảng sang nhánh.
- [ ] **P6-3 — Để sau.**

⚠ `AGENT.md` §10.1: thư viện lớn là câu BẮT BUỘC hỏi — bốn thư viện trên (Vue, Vite, Vue Flow, Router+Pinia,
cộng TypeScript) là toàn bộ phạm vi đã được chốt. Thêm thư viện thứ năm (biểu đồ, bộ component…) là hỏi lại.
