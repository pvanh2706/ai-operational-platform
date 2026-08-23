# 02 — Success Metrics v1

## AI Operational Knowledge & Process Platform

> **Chốt:** 2026-08-23 · `CONFIRMED`
> **Giải:** Open Question `Q-E` — treo từ khi tài liệu 02 bị mất.
> **Phạm vi:** thước đo thành công của **MVP**, cho **first use case** (OTA Booking Not Received) tại **khách #0**.

---

# 0. File này là gì và không phải gì

**Là:** bộ thước đo để trả lời *"MVP có thành công hay không"*, cùng với điều kiện đọc từng thước đo cho đúng.

**Không phải:** khôi phục tài liệu 02. `docs/02_PRODUCT_FOUNDATION_V1.md` vẫn **MISSING**, và file này chỉ lấy lại **một** phần của nó.

```text
Tài liệu 02 đã mất, mang theo:
  · capability contract chi tiết   →  VẪN MẤT
  · non-goals                      →  VẪN MẤT
  · Success Metrics                →  ĐÃ LẤY LẠI, chính là file này
```

**Dứt khoát không phải:** dashboard, tracking implementation, event schema. Đó là Workstream 06/07.

## Vì sao là một file riêng

Success Metrics đã **mất một lần** vì nằm trong một tài liệu bị mất, và không ai phát hiện (`00_CURRENT_STATE.md` §6.1). Nó lại được tham chiếu từ nhiều workstream. Nên nó có file riêng, tên nói rõ nội dung.

⚠️ Đây là ngoại lệ có ý thức với cảnh báo ở §6.7 (*"tốc độ sản xuất tài liệu đã vượt tốc độ sử dụng"*). File này phải **ngắn**. Nếu nó phình lên thành một vòng modeling nữa thì đã dùng sai.

---

# 1. Quyết định nền: Success Metric ≠ Eval metric · `CONFIRMED` (M1)

```text
SUCCESS METRIC   →  sản phẩm có tạo ra giá trị cho tổ chức không?
EVAL METRIC      →  hệ thống có chính xác không, trên tập cố định CÓ ĐÁP ÁN?
```

Cả hai đều cần. Nhưng **không được thay nhau**.

## Vì sao đây là quyết định, không phải chi tiết từ ngữ

Ứng viên gốc *"% case hệ thống chỉ ĐÚNG nguyên nhân"* (§8.1-KQ) là một **eval metric** bị xếp nhầm chỗ. Nếu để nó làm thước đo thành công thì có một failure mode rất cụ thể:

> Hệ thống chỉ đúng nguyên nhân 90% nhưng không ai xem gợi ý
> → Eval = 0.9 · Giá trị thật = 0 · Và ta ship.

Nhất quán với phân biệt đã `CONFIRMED` ở `AGENT.md` §6: **`Knowledge Retrieved ≠ Knowledge Used`**.

## Vai trò của Eval trong D5

`D5 hệ quả 1` gọi bộ eval là *cơ chế* biến "model mạnh lên" thành "phần mềm mạnh lên". **Cơ chế**, không phải **thước đo giá trị**. M1 chỉ là giữ đúng nghĩa đó.

---

# 2. Bộ Success Metrics — ba tầng

Ba tầng theo **thời điểm đo được**, không theo mức quan trọng. Tầng 0 đo được từ tuần 1; Tầng 2 cần nhiều tháng.

```text
TẦNG 0  vòng lặp có chạy không     tuần 1      hoạt động ở trạng thái 0 tri thức
TẦNG 1  có ai dùng không            tháng 1-2   cần đã có tri thức để gợi ý
TẦNG 2  có tạo giá trị không        nhiều tháng lagging, cần baseline
```

## 2.1 TẦNG 0 — Vòng lặp có chạy không · `CONFIRMED` (M2)

**Là Success Metric CHÍNH của tháng đầu.**

```text
(a) số bản nháp Path A được duyệt thành tri thức chính thức
(b) MỨC SỬA của người duyệt:  diff(bản nháp AI, bản đã duyệt)
    → thô: % assertion bị sửa / xoá / thêm
```

### Vì sao Tầng 0 phải tồn tại

Ngày đầu tại khách #0: **0 KnowledgeRecord**. Đã xác nhận không có SOP viết (§8.1-KQ), và `S6` chốt nạp tài liệu **không** tự sinh record.

Nghĩa là cả ba ứng viên gốc đều **không đo được ở tháng 1**:

```text
% chỉ đúng nguyên nhân  →  0 record thì không chỉ được gì. Undefined.
độ phủ                   →  0 / mẫu số, mà mẫu số chưa ai đếm (§8.2)
% escalate oan           →  không có baseline (Round 3)
```

Nhưng tháng đầu là lúc cần biết nhất *có nên tiếp tục hay không*. Tầng 0 là thứ duy nhất lấp được chỗ đó.

### Một phép đo, hai công dụng

Đây là `D6` flywheel đã ghi ở `00_CURRENT_STATE.md` §2.3, giờ thành thước đo:

```text
bản nháp AI      →  bản A
người sửa+duyệt  →  bản B
diff(A, B)       →  vừa là SUCCESS METRIC tầng 0
                    vừa là NHÃN cho bộ eval  (D5 hệ quả 1)
```

Nhãn eval sinh ra bởi **hành vi dùng sản phẩm**, không phải bởi một phase gán nhãn riêng. Đó là lý do M2 chọn cả (b), không chỉ (a).

### ⚠️ Cách đọc — mức sửa cao KHÔNG hẳn là xấu

```text
mức sửa cao  →  có thể AI dở
             →  cũng có thể người duyệt kỹ, hoặc chủ đề khó
```

Con số (b) **một mình không kết luận được gì**. Phải đọc cùng:

```text
TÍN HIỆU XẤU RÕ RÀNG:  bỏ giữa đường — mở bản nháp rồi không duyệt xong
TÍN HIỆU TỐT RÕ RÀNG:  duyệt xong, và lần sau người đó quay lại dùng nữa
```

→ Bắt buộc ghi kèm: **tỉ lệ bỏ giữa đường**. Không có nó thì (b) dễ bị đọc sai theo cả hai hướng.

## 2.2 TẦNG 1 — Có ai dùng không · `CONFIRMED` (M3)

Chuỗi **đầy đủ**, không rút gọn:

```text
1  RETRIEVED   hệ thống lấy ra                 (có thể không bao giờ hiện)
2  SHOWN       hiện ra trước mắt người
3  OPENED      người mở / đọc
4  ACCEPTED    người chấp nhận
5  USED        có mặt trong KẾT LUẬN của case
```

> ### ⚠️ Cập nhật 2026-08-23 (cùng ngày) — `L2` tinh chỉnh `M3` từ 4 mốc lên 5
>
> `M3` bản gốc chốt **4 mốc** (*hiện ra · mở · chấp nhận · có mặt trong kết luận*) mà **không đối chiếu** `Canonical Case Model v0.2` §11.2 và §11.3. Step 3 phát hiện có **ba** bộ từ vựng song song cho cùng một thứ:
>
> ```text
> v0.2 §11.2   Retrieved · Referenced · Used
> v0.2 §11.3   retrieved · shown · accepted/rejected · actually used
> M3           hiện ra · mở · chấp nhận · có mặt trong kết luận
> ```
>
> Đây là bệnh §6.9 (*vocabulary song song*) tái phát ở chỗ mới — và lần này do `M3` gây ra. `L2` gộp cả ba thành **từ vựng duy nhất**: 5 mốc ở trên.
>
> Thay đổi thực chất: thêm `RETRIEVED` ở đầu (v0.2 có, M3 thiếu), bỏ `Referenced` (không thêm thông tin quyết định). **Chuỗi 4 mốc của M3 vẫn nằm nguyên trong 5 mốc này** — L2 tinh chỉnh, không mở lại `M3`.
>
> Xem `04_KNOWLEDGE_MODEL_V0.1.md` §3B.2.

### Vì sao đầy đủ 5 mốc, không phải một cờ "đã dùng"

Mốc cuối là chỗ khác biệt. `§8.1-KQ` đã xác định: toàn bộ giá trị của first use case nằm ở **B5 — "đưa ra kết luận"**, bước duy nhất không ai ghi lại. Một gợi ý được "chấp nhận" mà không góp vào kết luận thì chưa chứng minh được gì.

Thang này cũng cho biết **rơi ở đâu** — mỗi chỗ rơi là một vấn đề khác:

```text
1→2  lấy ra nhưng không hiện       →  vấn đề xếp hạng / ngưỡng
2→3  hiện mà không ai mở            →  vấn đề trình bày / thời điểm
3→4  mở ra rồi thấy không đúng      →  vấn đề CHẤT LƯỢNG tri thức
4→5  chấp nhận nhưng không dùng     →  vấn đề tin cậy, hoặc chưa đủ cụ thể
```

Một con số gộp thì không tách được bốn nguyên nhân này.

### M3 giải P8 trực tiếp

`P8 — REUSE_NOT_MEASURABLE` trong problem taxonomy đã ghi sẵn giải pháp: *AssistanceAttempt · knowledge used · accept/reject · outcome*. M3 chính là nó, và nó thực thi phân biệt `Knowledge Retrieved ≠ Knowledge Used`.

## 2.3 TẦNG 2 — Có tạo giá trị không · `CONFIRMED` (M4a)

```text
% case escalate lên Technical mà tri thức đáng ra đã đủ
```

Đây là thước đo giá trị kinh doanh, đo trực tiếp `P1`. **Giữ lại, nhưng là lagging indicator.**

### ⚠️ Ba cảnh báo bắt buộc ghi kèm

```text
1  KHÔNG CÓ BASELINE
   Round 3 đã kết luận: "Knowledge reuse không consistently observable/
   measurable từ Jira records" → không có điểm khởi đầu định lượng.
   Cách duy nhất lấy baseline là ĐẾM TAY — chính là §8.2.

2  VOLUME NHỎ, NHIỄU LỚN
   Case OTA khoảng 20/tháng. Một thay đổi % cần nhiều tháng mới ra khỏi nhiễu.
   Nhiễu thật: có senior mới, case mix đổi, mùa vụ.

3  "ĐÁNG RA ĐÃ ĐỦ" LÀ PHÁN XÉT
   Cần người review từng case. Chủ quan và tốn công.
   → không dùng làm thước đo tự động; dùng như phép kiểm định kỳ.
```

→ Đừng dùng Tầng 2 để quyết định tiếp tục hay dừng trong 3 tháng đầu. Dùng Tầng 0 và 1 cho việc đó.

## 2.4 Độ phủ nguyên nhân — leading indicator NỘI BỘ TENANT · `CONFIRMED` (M4b)

```text
đã có luật cho bao nhiêu / tổng số nguyên nhân đã biết
```

**Không phải Success Metric của sản phẩm.** Là chỉ báo vận hành trong phạm vi **một khách**.

Lý do — `G12` (`AGENT.md` §3.9): mẫu số *"tổng nguyên nhân đã biết"* là đặc điểm của **một khách**. Khách B có thể có 200 nguyên nhân ở use case đầu. Metric này **không port được**, nên không thể là thước đo sản phẩm.

Vẫn giữ vì nó rất rẻ và là thứ duy nhất trả lời: **Path A có đang bồi đắp kho tri thức hay không.**

⚠️ Mẫu số hiện là **ước lượng n=1** (~5-10 nguyên nhân). `§8.2` xác nhận hoặc bác bỏ. Xem `04` §6 `R-K4`.

---

# 3. Bộ Eval — tách riêng, không phải Success Metric

```text
· % chỉ ĐÚNG nguyên nhân, trên tập cố định CÓ ĐÁP ÁN
· nguồn nhãn: diff(A,B) từ Tầng 0  +  ~20 case gán tay từ §8.2
```

Ở quy mô ~10 nguyên nhân thì đây là **bài toán phân loại có đáp án** → rẻ và định lượng được (`04` §3.5).

⚠️ Ground truth **phải gán tay**. Round 3 đã đo: root cause text thường thiếu, 306/500 case không có action steps. Không lấy được ground truth tự động từ Jira.

---

# 4. Metric này đòi dữ liệu gì — đầu vào cho Step 3 · `PROPOSED`

Đây là lý do `Q-E` được chốt **trước** phần `AssistanceAttempt` của Step 3: thiết kế cái máy ghi trước khi biết cần đo gì thì phải thiết kế hai lần.

```text
TẦNG 0 đòi:   bản nháp Path A giữ được PHIÊN BẢN TRƯỚC KHI SỬA
              → không ghi đè. Cần cả A và B.
              → và trạng thái "đang duyệt / bỏ giữa đường / duyệt xong"
              → ✅ ĐÃ CÓ NỀN: AP3 (Step 4) gắn origin ở TỪNG ASSERTION
                 nên "% assertion bị sửa/xoá/thêm" tính được. Nếu origin
                 ở mức record thì con số này KHÔNG tính được.

TẦNG 1 đòi:   AssistanceAttempt ghi được 5 mốc riêng biệt (L2),
              không phải một cờ "đã dùng"
              → và liên kết tới KẾT LUẬN cuối của case (B5)
              → đã ghi ngược vào Case v0.2 §11.3 ngày 2026-08-23

TẦNG 2 đòi:   sự kiện escalate + trạng thái kho tri thức LÚC ĐÓ
              → tại thời điểm escalate, tri thức đã tồn tại chưa?
              → cần lịch sử, không phải snapshot  (G5: Timeline over Snapshot)

M4b đòi:      danh sách nguyên nhân đã biết của một tenant
              → tách khỏi danh sách nguyên nhân ĐÃ CÓ LUẬT
```

⚠️ Nhãn `PROPOSED`: đây là **suy ra từ M1-M4**, chưa phải quyết định của Step 3. Step 3 chốt hình dạng thật của `AssistanceAttempt`.

Một điểm đáng chú ý: Tầng 2 đòi **trạng thái kho tri thức tại một thời điểm quá khứ** — đúng guardrail `G5` (Timeline over Snapshot). Guardrail cũ trả cổ tức lần nữa, không phải phát sinh yêu cầu mới.

---

# 5. Decision Register

## `CONFIRMED 2026-08-23`

```text
M1   Success Metric ≠ Eval metric. Ứng viên "% chỉ đúng nguyên nhân"
     chuyển sang bộ EVAL, không phải Success Metric.
M2   TẦNG 0 = Success Metric CHÍNH của tháng đầu:
     số nháp Path A được duyệt + MỨC SỬA diff(A,B).
     Bắt buộc ghi kèm tỉ lệ bỏ giữa đường.
M3   TẦNG 1 = chuỗi ĐẦY ĐỦ, không rút gọn.
     → TINH CHỈNH cùng ngày bởi L2 (Step 3) thành 5 mốc, gộp với
       vocabulary của Case v0.2 §11.2 + §11.3:
       RETRIEVED → SHOWN → OPENED → ACCEPTED → USED
M4a  "% escalate oan" giữ lại, vai trò LAGGING INDICATOR,
     kèm 3 cảnh báo: không baseline · volume nhỏ · "đáng ra" là phán xét.
M4b  "độ phủ nguyên nhân" = leading indicator NỘI BỘ TENANT,
     không phải Success Metric của sản phẩm (G12).

Q-E  RESOLVED 2026-08-23 bởi M1-M4.
```

Người dùng chốt **đúng phương án đề xuất** ở cả 4 câu.

## Kế thừa — `CONFIRMED`, không mở lại ở đây

```text
AGENT.md §6    Knowledge Retrieved ≠ Knowledge Used     → nền của M1 và M3
G5             Timeline over Snapshot                    → điều kiện của Tầng 2
G12            tỉ trọng của khách là tham số             → lý do M4b xuống hạng
D5 hệ quả 1    bộ eval là first-class                    → §3
D6 flywheel    diff(nháp, bản duyệt) = nhãn eval         → M2 phần (b)
P8             REUSE_NOT_MEASURABLE                      → M3 giải trực tiếp
```

---

# 6. Còn `OPEN`

```text
QM-1  Ngưỡng cụ thể là bao nhiêu? "Bao nhiêu bản nháp được duyệt thì gọi
      là thành công?" → CHƯA chốt. Cần chạy thật vài tuần mới có cơ sở.
      Đặt ngưỡng bằng cách đoán bây giờ thì tệ hơn là không đặt.

QM-2  Ai là người đọc bộ metric này định kỳ, và bao lâu một lần?
      → thuộc vận hành, không thuộc domain model.

QM-3  Success Metric cho VERTICAL THỨ HAI (CRM deal) là gì?
      → Tầng 1 và Tầng 0 có vẻ port được; Tầng 2 thì không
        ("escalate lên Technical" không có nghĩa trong sales).
        Chưa cần giải — khách #0 là support (D3).

QM-4  Q-I vẫn OPEN: vai trò Secondary Persona (Technical/L3) trong 3
      capability. Nếu L3 là người duyệt tri thức thì Tầng 0 đang đo
      công của L3, không phải trải nghiệm của Primary Persona.
      → cần giải cùng Q-G (ai có quyền verify).
```

⚠️ `QM-1` là chỗ dễ sai nhất. Có thước đo mà không có ngưỡng thì vẫn chưa có **điều kiện dừng** — chỉ là đã có **cách nhìn**. Đó là tiến bộ thật nhưng chưa đủ, và phải nói rõ để không ai tưởng `Q-E` đã đóng hoàn toàn.

---

# Nguyên tắc cốt lõi

> **Đo giá trị bằng thứ người dùng LÀM, không bằng thứ hệ thống TRẢ VỀ.**

Và thứ tự đọc bắt buộc:

```text
Tầng 0 chạy  →  mới có tri thức để Tầng 1 đo
Tầng 1 chạy  →  mới có lý do tin Tầng 2 sẽ dịch chuyển
Tầng 2 dịch  →  mới có câu chuyện bán hàng
```

Nhảy tầng là cách tự lừa mình nhanh nhất.
