# 04 — Knowledge Model v0.1

## AI Operational Knowledge & Process Platform

> **Tạo:** 2026-08-21
> **Stage:** Domain Modeling
> **Trạng thái:** Step 1 (Knowledge Boundary) và Step 2 (Concepts & Granularity) — `CONFIRMED`. Step 3–5 chưa làm.
> **Đọc trước:** `docs/00_CURRENT_STATE.md` · `AGENT.md` · `docs/PROJECT_CONTEXT.md` · `docs/Canonical Case Model v0.2.md`

---

# 0. File này là gì và không phải gì

**Là:** ranh giới của domain Knowledge — cái gì là Knowledge, cái gì không, và vì sao. Cộng với các quy tắc phân định để tranh luận sau này được giải bằng **test**, không bằng ý kiến.

**Chưa phải:** danh sách entity với đầy đủ thuộc tính, lifecycle chi tiết, vocabulary đã khóa. Đó là Step 3–5.

**Dứt khoát không phải:** database schema, vector DB, embedding strategy, RAG framework, REST API, frontend, microservices.

## Điều kiện dừng của cả tài liệu này

Theo `00_CURRENT_STATE.md` §6.7: Knowledge Model **không có dataset đối chiếu** tương đương 700 case của Canonical Case v0.2. Đào sâu mà không có dữ liệu đối chiếu là *đoán một cách cẩn thận*, không phải rigor.

```text
Mục tiêu: vừa đủ để build được first use case. Không sâu bằng Case v0.2.
Quy tắc: câu hỏi nào KHÔNG chặn việc build → ghi vào Open Questions và đi tiếp.
Thời hạn: Knowledge Model v0.1 + Process Model v0.1 chốt trong ~2 tuần.
```

---

# 1. Knowledge Boundary — Step 1 · `CONFIRMED 2026-08-21`

Toàn bộ §1 đã được người dùng xác nhận trực tiếp qua 9 quyết định (`S1`–`S8` + `K-B9`). Phân tích dẫn tới từng quyết định được giữ ở `00_CURRENT_STATE.md` §5.9 và §5.10 — không lặp lại ở đây.

## 1.1 Định nghĩa Knowledge · `CONFIRMED` (S2)

> **Knowledge = một phát biểu tái sử dụng được, ở mức LỚP tình huống (không phải một case cụ thể), về việc nghiệp vụ vận hành thế nào hoặc một loại tình huống nên được xử lý thế nào — có applicability scope, có provenance, và có một mức verification (bao gồm cả mức "chưa ai duyệt"); tồn tại độc lập với bất kỳ Case cụ thể nào.**

Từ khoá: **reusable · class-level · case-independent · có provenance**

### Điểm quan trọng nhất của định nghĩa này

*"Được tổ chức chấp nhận"* là **một trạng thái trên timeline**, **không** phải điều kiện để vào domain.

```text
SAI  — "được chấp nhận" là điều kiện vào cửa
       → 60% fragment, bản nháp AI, email senior chưa duyệt
         đều nằm NGOÀI mô hình tri thức của chính sản phẩm
       → không có chỗ nào để đặt chúng

ĐÚNG — "được chấp nhận" là một state
       → mọi thứ trên nằm TRONG domain, ở state tương ứng
       → provenance liên tục draft → verified (G6 yêu cầu điều này)
```

Hệ quả: `Q-J` được trả lời — Draft là **state của KnowledgeRecord**, không phải entity riêng.

## 1.2 Bộ test phân định · `CONFIRMED` (S3)

Đây **không** phải 4 discriminator. Đây là **2 test biên giới + 2 phép phân loại**.

```text
── TEST BIÊN GIỚI: có phải Knowledge không? ──────────────────

T1  CASE-INDEPENDENCE
    Xóa Case đã sinh ra nó → phát biểu còn giá trị không?
    Không → dữ liệu của Case, không phải Knowledge.

T2  CLASS-LEVEL
    Nó nói về một LỚP tình huống, hay một instance?
    "Booking ABC123 không về vì parser lỗi"        → instance → CaseClaim
    "OTA payload dạng X sẽ bị parser < v2.3 drop"  → class    → Knowledge

    Pass T1 + T2  →  LÀ Knowledge.

── PHÉP PHÂN LOẠI: ở trạng thái nào, ưu tiên bao nhiêu? ──────

T3  ORGANIZATIONAL ACCEPTANCE      → gán TRẠNG THÁI
    Đã có ai/quy trình nào chấp nhận nó ở mức nào chưa?
    Chưa → state DRAFT. Vẫn là Knowledge.

T4  DECISION VALUE                 → gán ƯU TIÊN
    Nó giúp quyết định / hành động / giải thích?
    Không → KHÔNG PROMOTE.
    Tuyệt đối không "không persist".
```

### Vì sao T4 là "không promote" chứ không phải "không persist"

Một ứng viên bị loại **chính nó là dữ liệu**:

- nó chỉ ra rằng ở chủ đề đó có khoảng trống;
- nó là tín hiệu eval — AI đề xuất gì mà con người từ chối.

Xóa đi là mất cả hai, và trái tinh thần `G4` (Unknown là hạng nhất) + `G6` (provenance là nền tảng). **Từ chối là một quyết định được ghi lại, không phải một phép xóa.**

## 1.3 Bốn chiều của Knowledge · `CONFIRMED`

```text
Applicability  — áp dụng cho tình huống nào (phiên bản, khách hàng, hệ thống nào)
Authority      — ai nói, được ai chấp nhận, ở mức nào
Visibility     — ai được phép thấy                          (do D1: sản phẩm để BÁN)
Derivation     — sinh ra từ đâu
```

Chiều `Authority` kiếm được chỗ đứng qua stress-test §2.3: cần phân biệt được *"một chuyên gia nói câu này một lần trong email"* với *"tổ chức đã review và công bố"*. Một trục `DRAFT / VERIFIED` không diễn đạt được hai thứ đó.

## 1.4 Kernel dùng chung ba domain · `CONFIRMED` (S4)

Bốn chiều ở §1.3 **không phải phát minh của Knowledge Model**. `CaseClaim` trong Canonical Case v0.2 **đã** có Origin + Verification + Evidence.

```text
              ┌──────────────────────────────────────────┐
              │  KERNEL DÙNG CHUNG                       │
              │  Origin · Evidence · Verification level  │
              │  Applicability · Visibility · Authority  │
              └──────────────────────────────────────────┘
                   ▲              ▲               ▲
                   │              │               │
              CaseClaim      KnowledgeRecord   ProcessDefinition
              (đã có v0.2)    (04, file này)    (05, sắp làm)
```

### Ba quy tắc kèm theo

**(1) Danh sách bước có MỘT nhà duy nhất: Process domain.**
Kể cả khi nó vừa được AI gom và chưa ai duyệt — lúc đó nó là `ProcessDefinition` ở state DRAFT. **Không có bản sao thứ hai trong Knowledge domain.**

**(2) Verification level tách khỏi Lifecycle state.**
Đây là cách giải mâu thuẫn §6.9 (`VERIFIED` xuất hiện ở hai vocabulary với hai nghĩa khác nhau):

```text
VERIFICATION LEVEL  — mức tin, dùng chung ba domain
                      (v0.2 §7.3: SPECULATIVE / PLAUSIBLE / SUPPORTED /
                       VERIFIED / CONFLICTING / INVALIDATED — chưa khóa)

LIFECYCLE STATE     — mức công bố, riêng từng domain
                      (PROJECT_CONTEXT §8.3: DRAFT / ACTIVE / NEEDS_REVIEW /
                       DEPRECATED / SUPERSEDED — chưa khóa)
```

`VERIFIED` chỉ còn ở trục thứ nhất. Vocabulary chính xác → Step 5.

**(3) Kỷ luật từ vựng.**

```text
"SOP"                =  tài liệu con người đọc (carrier)
"ProcessDefinition"  =  thứ hệ thống dùng để dẫn từng bước
```

Nếu team gọi cả hai là "SOP", hai domain sẽ lại nhập nhèm.

### Cái giá phải trả — ghi rõ để không bị quên

Knowledge Model v0.1 và Process Model v0.1 **không thể làm tuần tự hoàn toàn**. Chúng chia nhau kernel ở trên. Mối nối đó tồn tại thật — nó không do thiết kế sinh ra, nó do việc *đầu ra đầu tiên của MVP là một danh sách bước* sinh ra.

→ `AGENT.md` §7 ghi *"Process Model ⚪ AFTER KNOWLEDGE"*. Điều đó vẫn đúng về **thứ tự hoàn thành**, nhưng **không** đúng nếu hiểu là "hoàn toàn tách rời".

## 1.5 Bảng ranh giới · `CONFIRMED`

| Concept | Trả lời câu hỏi | Scope | Case-independent? | Là Knowledge? |
|---|---|---|---|---|
| **Case** | Việc gì đang được xử lý? | 1 instance | ✗ | **Không** — `CONFIRMED` v0.2 |
| **Historical Case** | Việc gì đã từng được xử lý? | 1 past instance | ✗ | **Không** → precedent/evidence |
| **Evidence** | Ta quan sát được gì? | 1 observation | ✓ | **Không** — `CONFIRMED` v0.2 §9 |
| **Document** | Tri thức được ghi ở đâu? | container | ✓ | **Không** → carrier/source |
| **Technical Finding** | Cơ chế kỹ thuật nào giải thích? | thường 1 case | ✗ khi mới sinh | **Chưa** → CaseClaim, tới khi generalize |
| **Process** | Việc nên xử lý thế nào, theo bước? | 1 lớp công việc, có step + per-case state | ✓ | **Không** → domain riêng (05) |
| **Knowledge** | Tổ chức khẳng định gì, áp dụng được? | 1 lớp tình huống | ✓ | **Có** — kể cả ở state DRAFT |

**Lưu ý so với bản nháp ở `00_CURRENT_STATE.md` §5.5:** dòng *"AI Knowledge Draft"* đã bị bỏ khỏi bảng — theo `S2`, nó là **state của Knowledge**, không phải concept riêng.

## 1.6 Boundary claims · `CONFIRMED`

### K-B1 — Knowledge là case-independent
Case không bao giờ *trở thành* Knowledge. Historical Case dù hữu ích vẫn là **precedent/evidence**.

**Hệ quả đã chốt (`Q-C`):** Capability #1 trả về **hai loại kết quả, có nhãn tách biệt, không trộn vào nhau**:

```text
ĐÃ DUYỆT            KnowledgeRecord
CASE TƯƠNG TỰ       Historical Case — precedent, KHÔNG phải tri thức đã duyệt
```

Cỗ máy tìm case cũ tương tự **buộc phải build** vì nó là dependency của Capability 3 (xem `D6` hệ quả 1). Việc bày nó ra ở Capability 1 vì thế gần như miễn phí — và cần thiết, vì với con số 10% thì ngày đầu Capability 1 gần như chỉ có case cũ để trả.

### K-B2 — Document ≠ Knowledge
Document là **carrier** có identity / version / access control riêng. Knowledge **cite** Document. Chống rủi ro biến Knowledge Model thành Document Management Model. Xem §1.9.

### K-B3 — Evidence ≠ Knowledge
Evidence gắn với **một thời điểm và một nguồn**. Knowledge là phát biểu **class-level**. Evidence `SUPPORT` / `REFUTE` / `CONTEXT_FOR` Knowledge.

### K-B4 — Technical Finding chỉ là CaseClaim cho tới khi được generalize + accept
Bước generalization là **một quyết định**, không phải promotion tự động. Đây là chỗ `G3` (FACT ≠ AI INFERENCE) dễ vỡ nhất.

### K-B5 — AI Draft là một STATE, không phải một entity
```text
Origin = AI_INFERENCE  →  giữ VĨNH VIỄN, kể cả sau khi human verify   (v0.2 §7.4)
State  = DRAFT         →  thay đổi được
```
Retrieval lọc theo state bằng **policy**, không bằng cách đặt draft ở một chỗ khác.

### K-B6 — Process ≠ Knowledge, có rule phân định
```text
Có ordered/conditional STEP  +  theo dõi được "đang ở bước nào" cho từng Case
    →  PROCESS

Là assertion / explanation / applicability / khuyến nghị có điều kiện,
không có per-case execution state
    →  KNOWLEDGE

Một SOP document có thể là SOURCE cho cả hai.
```
Rule này đã được stress-test ở cả hai vertical (§2.1 và §2.4) và chạy đúng, không phải bẻ.

### K-B7 — Tri thức trong đầu người chưa phải Knowledge của hệ thống
`PROJECT_CONTEXT` §5.2 liệt kê *"Human knowledge / senior memory"* dưới mục KNOWLEDGE — đó là **knowledge source**, không phải KnowledgeRecord. Chỉ vào model sau khi externalize.

Đặc biệt quan trọng vì **30%** SOP nằm trong đầu người. Đường vào duy nhất khả thi là **Path B** (§1.8).

### K-B8 — Capability 3 có hai nửa, ở hai domain khác nhau
Xem §1.8.

### K-B9 — Evidence được phép trỏ trực tiếp vào Knowledge, không qua Case
```text
Evidence  →  SUPPORT / REFUTE / CONTEXT_FOR  →  Knowledge
             (không cần Case làm trung gian)
```

Vì sao cần: một email của senior, một tin Zalo, một ghi chú rời **không thuộc case nào**. Với 60% là fragment rải rác, đây không phải trường hợp hiếm.

Vì sao an toàn: v0.2 §9 **đã** viết *"Một EvidenceItem có thể liên quan nhiều Case"* và *"Không mặc định 1 Evidence = owned exclusively by 1 Case"*. Đây là mở rộng nhỏ theo đúng hướng model đã đi, không phải bẻ model.

**Mở rộng so với v0.2 §11.2** — chỗ đó chỉ mô tả đường *"Case contributes evidence toward Knowledge"*. Đường trực tiếp giờ cũng hợp lệ. Cần ghi ngược vào Canonical Case v0.2 khi cập nhật tài liệu.

## 1.7 Negative list — Knowledge KHÔNG bao giờ là · `CONFIRMED`

```text
chat / comment log
audit trail
AssistanceAttempt telemetry
metric / analytics
giá trị field của một Case
embedding / index artifact          ← infrastructure, không phải domain
phát biểu của khách hàng            ← là Evidence hoặc CaseClaim
```

## 1.8 Capability 3 có hai path · `CONFIRMED` (S5 / K-B8)

Hai nửa của Capability #3 có **kinh tế học hoàn toàn khác nhau** và **domain đầu ra khác nhau**.

```text
╔═ PATH A — KÉO (pull) ══════════════════════════════════════════════╗
║  Ai khởi động     người dùng: "tôi cần SOP cho chủ đề X"           ║
║  Đầu vào          N case liên quan (tập CÓ GIỚI HẠN) + email + note ║
║  Đầu ra           1 bản nháp                                        ║
║  Tần suất         mỗi chủ đề một lần                                ║
║  Ngân sách chú ý  PHÚT — người ta chủ động xin, sẵn sàng bỏ công    ║
║  Domain đầu ra    Knowledge  (+ Process nếu là danh sách bước)       ║
╚════════════════════════════════════════════════════════════════════╝

╔═ PATH B — ĐẨY (push) ══════════════════════════════════════════════╗
║  Ai khởi động     hệ thống nhắc lúc đóng case                       ║
║  Đầu vào          1 case — những gì ĐÃ CÓ trong case                ║
║  Đầu ra           hồ sơ Case dày hơn                               ║
║  Tần suất         500 lần                                           ║
║  Ngân sách chú ý  GIÂY — người ta không xin, đang muốn đóng case    ║
║  Domain đầu ra    KHÔNG PHẢI Knowledge                              ║
╚════════════════════════════════════════════════════════════════════╝
```

### Điểm then chốt

> **Path B không tạo ra Knowledge.** Nó chỉ làm dày hồ sơ Case — `CaseAction` / `CaseClaim` / `CaseOutcome` / `EvidenceItem`, tất cả đã có sẵn trong v0.2 — để **sau này Path A gom được**.

### Ba hệ quả

**(1) §6.4 hết mâu thuẫn.** Ràng buộc *"nếu cần hơn ~20 giây chú ý thì nó sẽ rỗng"* áp cho **Path B**, nơi người dùng chỉ xác nhận tóm tắt case của **chính mình**. Nó **không** áp cho Path A — duyệt một SOP 9 bước tốn 30 phút là hoàn toàn hợp lý, vì người ta chủ động xin nó.

**(2) Knowledge domain sạch.** Không có cái sọt "fragment chưa xử lý" nằm trong Knowledge Model.

**(3) Chặn đúng nguy cơ đã nêu ở §6.4.** Nguy cơ *"Capability 3 chính là cái field `Version đang sử dụng` mặc áo đẹp hơn"* — cái field đó là Path B, và Path B giờ **chỉ được phép hỏi những gì đã có trong case**.

## 1.9 Nạp tài liệu KHÔNG tự sinh KnowledgeRecord · `CONFIRMED` (S6)

```text
nạp tài liệu     →  tạo Document (carrier) + nội dung đọc được
                    KHÔNG tự tạo KnowledgeRecord

KnowledgeRecord  →  chỉ sinh ra khi có một HÀNH VI KHẲNG ĐỊNH:
                    · người viết ra, hoặc
                    · Path A gom rồi người duyệt
```

> **KnowledgeRecord lưu những gì tổ chức đã KHẲNG ĐỊNH. Nó không lưu tất cả những gì tổ chức CÓ.** Phần "có" nằm ở Document.

Lý do: `D5` xếp *"pipeline cắt chunk, template extraction"* vào cột **GIÀN GIÁO TẠM** — đúng cái sẽ thành nợ khi model mạnh lên. Quyết định này làm Knowledge Model nhỏ lại đáng kể và nhất quán với D5 thay vì chống lại nó.

## 1.10 Visibility rule cho MVP · `CONFIRMED` (S7)

```text
Mặc định       visibility của tri thức tổng hợp = HẸP NHẤT trong các nguồn của nó
Mở rộng        là một HÀNH VI TƯỜNG MINH của người thấy được TẤT CẢ nguồn
               ghi lại: ai mở, khi nào, mở từ đâu tới đâu, lý do
Hệ thống       KHÔNG BAO GIỜ tự mở
```

Cùng khuôn với `D4` (AI đề xuất được, AI không công nhận được), chỉ áp cho **quyền xem** thay vì cho **nội dung**.

### Ba chỗ chứa mà model phải có

```text
1. visibility của bản thân KnowledgeRecord
2. visibility của từng nguồn chống lưng nó
3. ai mở rộng, khi nào, lý do
```

### Bước mở quyền nằm TRONG luồng Capability 3

Không phải một trang cấu hình admin.

Lý do là một hệ quả bất lợi phải nói thẳng: với quy tắc "hẹp nhất", **60% sẽ tệ đi trước khi tốt lên**. Một SOP gom từ 20 case Jira, nếu có một comment trong project nội bộ, sẽ **vô hình với đúng bạn support mới cần nó**. Nếu để bước mở quyền cho sau, tính năng sẽ *"chạy được trong demo và im lặng vô dụng trong thực tế"*.

### Ràng buộc về người duyệt

```text
duyệt nội dung   →  cần người GIỎI NGHIỆP VỤ         (senior support / L3)
duyệt quyền xem  →  cần người THẤY ĐƯỢC MỌI NGUỒN
```

Hai người khác nhau → tắc. Ép một người không đủ quyền → rò rỉ.

**Quy tắc MVP:** người duyệt **phải** là người thấy được tất cả nguồn; duyệt nội dung + quyền xem trong **một hành động**; log cả hai. Thu hẹp đáng kể `Q-G`.

### Phần vẫn OPEN

Tách kết luận khỏi dẫn chứng ở mức từng câu, bôi đen chọn lọc, redaction → **v2**. `Q-D` giữ nhãn `OPEN`.

## 1.11 Cấu trúc bản nháp gom · `CONFIRMED` (S8)

Một bản nháp gom từ 20 case **khác về bản chất** với bản nháp viết từ 1 case: nó mang theo một **phân bố**, không phải một phát biểu.

```text
bước "kiểm tra room mapping"        →  14/20 case đã làm        ✓ đồng thuận
bước "gọi OTA trước khi check log"  →  6/20 làm, 8/20 làm ngược  ⚠ CONFLICTING
```

### Hai yêu cầu bắt buộc

**(1) Evidence link ở mức TỪNG PHÁT BIỂU, không phải mức tài liệu.**
Nếu lưu như một khối văn bản với danh sách nguồn ở cuối, ta ném đi đúng thứ giá trị nhất: bước nào được bao nhiêu case chống lưng, và **chỗ nào các case không đồng ý với nhau**.

**(2) Trạng thái `CONFLICTING` là bắt buộc, không phải tùy chọn.**
Gom N case thì xung đột là chuyện thường ngày, không phải ngoại lệ. (`CONFLICTING` đã có trong v0.2 §7.3; **thiếu** trong `PROJECT_CONTEXT` §13.4 — xem §5 Open Questions.)

### Vì sao đây là yêu cầu về giá trị, không phải chi tiết kỹ thuật

Chỗ các case không đồng ý **chính là chỗ người duyệt cần nhìn**. Nó cho phép duyệt trong 10 phút thay vì 2 giờ: họ chỉ phán xét mấy điểm tranh chấp, phần còn lại đã có 14/20 đồng thuận. Không có nó, Path A đắt tới mức không ai dùng.

### Trạng thái "tri thức gom từ nhiều nguồn rời, chưa ai duyệt"

Đây là câu hỏi mà `00_CURRENT_STATE.md` §4 đặt ra. Trả lời:

```text
state DRAFT
+ origin AI_INFERENCE
+ evidence link theo từng phát biểu
+ có thể CONFLICTING

→ KHÔNG cần entity mới.
```

---

# 2. Stress-test đã chạy · `CONFIRMED` là đã chạy, kết luận theo §1

## 2.1 First Use Case — OTA booking không về PMS

| Vật thể | Phân loại | Ghi chú |
|---|---|---|
| SOP "booking không về PMS" | **Document** (carrier) + **ProcessDefinition** (phần bước) + **Knowledge** (phần không phải bước) | Ba thứ, không phải một. Đây là câu trả lời cho `Q-B`. |
| *"Không có incoming log → lỗi phía OTA"* | **Knowledge** | Assertion, không có bước, không có per-case state → K-B6. |
| *"Parser < v2.3 drop payload dạng X"* | **Knowledge** — ví dụ sạch nhất | class-level ✓ case-independent ✓ decision value ✓. `applicability` = khoảng version. Cũng là vật thể có vấn đề visibility ở §1.10. |
| Jira ES-123 đã fix cùng vấn đề | **Historical Case** → precedent | Không phải Knowledge (K-B1). Là **nguyên liệu** của Path A. |
| Screenshot log khách gửi | **Evidence**, machine readability thấp | Trạng thái `KNOWLEDGE_EXISTS_NOT_RETRIEVABLE` (§4, khối `PROPOSED`). |

SOP tách thành ba vật thể là **kết quả đúng**, không phải dấu hiệu model sai.

## 2.2 Ba thực tế 10 / 30 / 60

| | Thực tế | Đường vào model |
|---|---|---|
| **10%** | SOP có, tìm được | → **Document** (§1.9). KnowledgeRecord chỉ sinh khi có hành vi khẳng định. |
| **30%** | SOP trong đầu người | → **K-B7**: chưa vào model. Đường vào duy nhất khả thi là **Path B**. |
| **60%** | SOP rải rác fragments | → **không cần entity mới nào** |

Chi tiết 60%:

```text
comment Jira, action, outcome  →  v0.2 đã có (CaseClaim/CaseAction/CaseOutcome/Evidence)
email, ghi chú Zalo rời        →  Document / SourceReference → Evidence   (K-B9)
"gom lại"                      →  không phải entity, mà là một TRUY VẤN + một HÀNH VI
```

**Kết luận đáng ghi:** 60% không đòi thêm concept. Nó đòi một **truy vấn tìm case liên quan** và một **hành vi tổng hợp**. Đầu tư vào Canonical Case v0.2 trả cổ tức lần nữa.

**Về 30%:** §6.4 (chi phí capture gần bằng 0) **không** phải một lời khuyên UX — **nó là điều kiện để 30% tồn tại trong sản phẩm.** Nếu Path B không thật sự rẻ, 30% vĩnh viễn ở ngoài.

## 2.3 Non-Jira — SOP .docx trên Drive + email hướng dẫn của senior

- **.docx** → Document (nguồn dạng A), có version + ACL riêng. Không tự thành Knowledge.
- **Email của senior** → phát biểu class-level do **một con người có chuyên môn** đưa ra. **Không** phải AI draft. Cũng **không** được tổ chức duyệt.

Ca thứ hai là lý do chiều `Authority` phải có: cần phân biệt *"một chuyên gia nói câu này một lần trong email"* với *"tổ chức đã review và công bố"* — và một trục `DRAFT / VERIFIED` không làm được. → củng cố §1.4 quy tắc (2).

## 2.4 Vertical thứ hai — CRM deal

Rule *"khách ở Negotiation im hơn 7 ngày → gửi case study cùng ngành"*:

```text
có điều kiện  ✓        có hành động  ✓
có thứ tự bước  ✗      theo dõi "đang ở bước nào"  ✗
→ KNOWLEDGE
```

Rule K-B6 chạy đúng, không phải bẻ gì.

Hai điều học được:

1. Ở vertical sales, Knowledge phần lớn là **khuyến nghị có điều kiện** (*nếu X thì làm Y*), không phải **giải thích cơ chế** (*parser drop payload*). Đây là loại **dễ lẫn với Process nhất** → rule K-B6 sẽ bị vắt kiệt ở đây, không phải ở support. **Ghi nhớ cho Step 2 (Knowledge types).**
2. Rule đó được suy ra từ các deal won/lost → **đúng cơ chế Path A, khác tên**. Path A phổ quát, không đặc thù support. Nhất quán với `D2` và §6.5.

---

# 3. Knowledge Concepts & Granularity — Step 2 · `CONFIRMED 2026-08-21`

Bốn quyết định `T1`–`T4`. Đây là Step đầu tiên **có dữ liệu thật** để dựa vào: kết quả §8.1 ở `00_CURRENT_STATE.md` §8.1-KQ.

Dữ liệu quyết định hình dạng của Step này:

```text
Quy trình thật:  Kibana → response → tài liệu → issue cũ → ĐƯA RA KẾT LUẬN
Giá trị nằm ở:   bước cuối — bước duy nhất không ai ghi lại
Tập kết luận:    HỮU HẠN NHỎ, ~5-10 loại nguyên nhân      ⚠ n=1
```

## 3.1 Đơn vị của Knowledge · `CONFIRMED` (T1)

> **Một KnowledgeRecord = MỘT NGUYÊN NHÂN / CƠ CHẾ, kèm cách nhận ra nó.**

Cấu trúc ở mức khái niệm (chưa phải field — đó là Step 4):

```text
nguyên nhân     ← IDENTITY của record
cách nhận ra    ← một hoặc NHIỀU pattern bằng chứng
áp dụng cho     ← applicability
xử lý           ← xem §3.2
```

### Vì sao identity là nguyên nhân, không phải luật hay assertion

**(1) Nguyên nhân là thứ bền nhất trong ba thành phần.**

```text
tín hiệu    → đổi khi log/API đổi format
cách xử lý  → đổi theo thời gian, theo version
nguyên nhân → "parser < 2.3 không hỗ trợ payload dạng X" sống lâu
```

Nhất quán với `D5`: đặt identity vào thứ bền nhất, không vào thứ dễ mục.

**(2) Path A hội tụ, không phình.**

```text
case mới  →  so với tập nguyên nhân đã biết
             KHỚP        → thêm recognition pattern + tăng số case chống lưng
             KHÔNG KHỚP  → ứng viên NGUYÊN NHÂN MỚI → người duyệt
```

Đây là vòng lặp **có điểm hội tụ**. Khác hẳn "gom văn bản" — nó là **bồi đắp một tập hữu hạn**. Với con số 5-10 thì nó hội tụ về ~10 record cho toàn bộ first use case.

**(3) Applicability gắn tự nhiên vào nguyên nhân** — `version < 2.3` là thuộc tính của nguyên nhân, không phải của tín hiệu.

**(4) Độ phủ đo được** — *"đã có luật cho 7 trong 9 nguyên nhân đã biết"*. Không diễn đạt được nếu đơn vị là luật hoặc assertion.

**(5) Đóng vòng với `K-B4`** — một `Technical Finding` sau khi được generalize + accept trở thành **đúng một DIAGNOSTIC record**. Bước generalization giờ có đích cụ thể, không còn trừu tượng.

### Phản biện đã cân nhắc và trả lời

> *Người support cần cái MAPPING (thấy X thì là Z), không cần biết "nguyên nhân Z tồn tại".*

Đúng — nhưng mapping **nằm trong** record, ở phần *cách nhận ra*. Identity là nguyên nhân; nội dung có cả cách nhận ra. Không mất tính dùng được, mà tránh được việc nội dung nguyên nhân bị lặp ở 3 record rồi lệch nhau.

## 3.2 "Làm gì" nằm ở đâu · `CONFIRMED` (T2)

```text
action ĐƠN LẺ                          →  nằm TRONG KnowledgeRecord
  "escalate Technical"                     dạng khuyến nghị
  "liên hệ OTA"
  "yêu cầu khách gửi lại booking ID"

action NHIỀU BƯỚC + theo dõi tiến độ   →  KnowledgeRecord TRỎ tới
  "quy trình nâng version"                 ProcessDefinition
```

Đúng rule `K-B6`, không phát minh thêm gì. Tránh được hai thất bại đối xứng:

```text
mọi action → Process    :  sinh hàng chục ProcessDefinition MỘT bước → vô nghĩa
mọi action → Knowledge  :  phá S4, danh sách bước có hai nhà → lệch nhau
```

## 3.3 Knowledge types cho MVP · `CONFIRMED` (T3)

**Hai type. Mỗi type có ca thật.**

| Type | Ca thật | Hình dạng |
|---|---|---|
| `DIAGNOSTIC` | OTA booking không về PMS | tín hiệu → nguyên nhân |
| `CONDITIONAL_RECOMMENDATION` | CRM deal (§2.4) | điều kiện → hành động |

### Hai type bị loại — và lý do

**`REFERENCE / FACTUAL` — không cần.** `S6` đã để tài liệu hệ thống (API, field, behavior) ở lại dạng **Document**. Đó chính là cái đọc ở bước B3 của quy trình thật. Không có KnowledgeRecord nào cần sinh ra cho nó.

→ Đây là ví dụ `S6` **xoá bớt** một nhánh của model. Kết quả tốt: bớt một type mà không mất gì.

**`POLICY / CONSTRAINT` — chưa có ca thật.** Agent đề xuất type này ở phiên trước (*"không refund khi chưa approve"*), rà lại thì **không có ca thật nào** trong cả hai vertical — tự nghĩ ra. Theo §6.7 (`00_CURRENT_STATE`): không thêm cho tới khi có ca thật.

> Thêm một type về sau rất dễ. Bỏ một type đã có dữ liệu chạy trên nó thì rất khó.

### Cảnh báo kèm theo

Từ stress-test §2.4: `CONDITIONAL_RECOMMENDATION` là type **dễ lẫn với Process nhất**, và nó sẽ bị vắt kiệt ở vertical sales chứ không phải ở support. Rule phân định vẫn là `K-B6`:

```text
có thứ tự bước + theo dõi được đang ở bước nào  →  PROCESS
một khuyến nghị đơn, có điều kiện               →  KNOWLEDGE
```

## 3.4 Verification gắn ở đâu · `CONFIRMED` (T4)

> **Từng assertion bên trong record**, không phải một con số cho cả record.

Ví dụ cụ thể, một record thật:

```text
RECORD   "Parser < 2.3 drop payload OTA dạng X"

  A1  nguyên nhân này tồn tại          VERIFIED     git commit + source code
  A2  nhận ra bằng log pattern "..."   SUPPORTED    12/14 case
  A3  xử lý: nâng version              VERIFIED     đã fix thành công 9 lần
  A4  cũng gây lỗi ở luồng Y           SPECULATIVE  1 case, chưa ai xác nhận
```

Một con số duy nhất cho cả bốn thì **buộc phải nói quá hoặc nói thiếu**:

```text
chọn VERIFIED     →  A4 (1 case, chưa ai xác nhận) được tiếng đã xác minh
chọn SPECULATIVE  →  A1 (có source code chứng minh) bị hạ oan
```

Cả hai đều vi phạm `G3` (FACT ≠ AI INFERENCE) và `G8` (không dùng confidence như truth). Không có lựa chọn thứ ba.

Đây là **hệ quả trực tiếp của `S8`** (evidence link theo từng phát biểu) — không phải scope mới, chỉ là giữ nhất quán.

### Hệ quả về hình dạng của record

> **KnowledgeRecord không phải một khối văn bản.** Nó là một **cụm assertion về cùng một subject**, mỗi assertion mang evidence và verification level riêng.

## 3.5 Hệ quả về quy mô — `phát hiện`, không phải quyết định

Với ~10 record cho toàn bộ first use case:

```text
· Capability 1 ở MVP là bài toán PHÂN LOẠI, không phải semantic search
    "bằng chứng của case này khớp nguyên nhân nào trong 10 cái đã biết"
· không cần vector DB / chunking / RAG ở quy mô này
    → D5 xếp chúng ở cột GIÀN GIÁO TẠM; dựng lên đây là tự tạo nợ
· bộ eval = phân loại có đáp án → rẻ và định lượng được
· Q-E lần đầu có ứng viên metric (xem §5)
```

⚠️ **Không đổi Capability #1** (giữ `G9`). Nó vẫn là `Contextual Knowledge Retrieval`. Đây là nhận định về **cách hiện thực**, không phải về phạm vi.

⚠️ Toàn bộ §3.5 dựa trên con số *"5-10 nguyên nhân"* với **n=1**. `§8.2` (đếm 20 case OTA) xác nhận hoặc bác bỏ nó. Đây là rủi ro `R-K4` ở §6.

## 3.6 Còn `OPEN` sau Step 2

```text
N-3b  Một "cách nhận ra" to bằng nào? (một log line? một pattern? một tổ hợp?)
      → Step 3/4
N-6   Hai nguyên nhân cùng gây một triệu chứng → phân biệt thế nào?
      → chưa có ca thật. Step 3.
N-7   Một case có HAI nguyên nhân đồng thời → model thế nào?
      → chưa có ca thật. Step 3.
N-8   Nguyên nhân bị bác bỏ (từng đúng, giờ sai vì hệ thống đổi) → lifecycle nào?
      → Step 5, cùng vocabulary.
```

---

# 4. Decision Register

## `CONFIRMED 2026-08-21`

```text
S1   Guardrail phạm vi D6 → nâng thành G11 trong AGENT.md §3.8
S2   Định nghĩa Knowledge: "được chấp nhận" là TRẠNG THÁI, không phải điều kiện vào cửa
     → Q-J trả lời: Draft là STATE của KnowledgeRecord
S3   Bộ test = 2 discriminator (T1,T2) + 2 phép phân loại (T3,T4)
     T4 = "không PROMOTE", không phải "không persist"
S4   Kernel dùng chung: Origin/Evidence/Verification/Applicability/Visibility/Authority
     dùng chung cho CaseClaim + KnowledgeRecord + ProcessDefinition
     Danh sách bước có MỘT nhà: Process domain
     Verification level TÁCH KHỎI Lifecycle state  → giải §6.9
S5   K-B8: Path A tạo Knowledge · Path B chỉ làm dày Case
     → ràng buộc 20 giây (§6.4) áp cho Path B, không áp cho Path A
S6   Nạp tài liệu KHÔNG tự sinh KnowledgeRecord
S7   Visibility: HẸP NHẤT mặc định + mở rộng tường minh có log
     bước mở quyền nằm TRONG luồng Capability 3
     người duyệt phải thấy được mọi nguồn
S8   Bản nháp gom: evidence link theo TỪNG PHÁT BIỂU + CONFLICTING bắt buộc
K-B9 Evidence được trỏ trực tiếp vào Knowledge, không qua Case
Q-B  SOP = Document + ProcessDefinition + Knowledge (ba vật thể)
Q-C  Capability 1 trả về HAI loại kết quả có nhãn tách biệt:
     KnowledgeRecord và Historical Case
```

## Step 2 — `CONFIRMED 2026-08-21`

```text
T1   Một KnowledgeRecord = MỘT NGUYÊN NHÂN / CƠ CHẾ, kèm cách nhận ra nó
     → identity đặt vào thứ bền nhất; Path A hội tụ về tập hữu hạn
     → đóng vòng với K-B4: Technical Finding generalize xong = 1 DIAGNOSTIC record
T2   action ĐƠN LẺ nằm trong Knowledge; action NHIỀU BƯỚC trỏ ProcessDefinition
T3   HAI Knowledge type cho MVP, mỗi type có ca thật:
       DIAGNOSTIC                  (ca OTA)
       CONDITIONAL_RECOMMENDATION  (ca CRM)
     Loại bỏ REFERENCE (S6 để ở Document) và POLICY (chưa có ca thật)
T4   Verification level gắn ở TỪNG ASSERTION trong record, không phải cả record
     → KnowledgeRecord là một CỤM ASSERTION, không phải một khối văn bản
```

## Dữ liệu thực tế nền cho Step 2 — `EVIDENCE-SUPPORTED`, n=1

```text
Quy trình thật (§8.1):  Kibana → response → tài liệu → issue cũ → KẾT LUẬN
Không có SOP viết cho first use case. Chỉ có tài liệu hệ thống.
Tập kết luận HỮU HẠN NHỎ: ~5-10 loại nguyên nhân.
Nguồn: 00_CURRENT_STATE.md §8.1-KQ
```

## Kế thừa từ tài liệu trước — `CONFIRMED`

```text
D6      "Gom nhiều case cũ thành SOP theo yêu cầu" NẰM TRONG Capability 3
        (00_CURRENT_STATE.md §2.3)
G1-G10  Guardrail nền tảng (AGENT.md §3)
G11     Không tự làm phỏng to một Capability đã chốt (AGENT.md §3.8)
K-B1..K-B9  Boundary claims (§1.6 file này)
```

## `PROPOSED` — chưa chốt, cần Step 2+

```text
Ba trạng thái coverage: KNOWLEDGE_ABSENT / EXISTS_NOT_RETRIEVABLE /
  EXISTS_RETRIEVABLE  (00_CURRENT_STATE.md §6.3)
  → KHÔNG phải state của KnowledgeRecord. Đây là trạng thái của một CHỦ ĐỀ.
  → Nơi chứa nó là concept "Knowledge Gap" — mà Gap Detection là FUTURE
    capability (PROJECT_CONTEXT §17), không phải MVP.
  → Step 1 chỉ ghi nhận: concept này TỒN TẠI và KHÔNG PHẢI Knowledge.
    Không thiết kế nó bây giờ. Giữ G9 + G11.

Knowledge types  → Step 2. Stress-test §2.4 cho biết ít nhất phải có:
  · factual / causal          "parser < v2.3 drop payload dạng X"
  · conditional recommendation "nếu X thì làm Y"        ← dễ lẫn Process nhất
  · policy / constraint        "không refund khi chưa approve"
```

---

# 5. Open Questions

## Vẫn `OPEN` — không chặn việc build

```text
Q-D  Tách kết luận khỏi dẫn chứng ở mức từng câu; redaction; bôi đen chọn lọc
     → v2. Quy tắc đủ dùng cho MVP đã có ở §1.10.
Q-E  Success Metrics của MVP là gì?               (tài liệu 02 mất — §6.1)
     → LẦN ĐẦU có ứng viên, nhờ tập nguyên nhân hữu hạn (§3.5):
       · % case hệ thống chỉ ĐÚNG nguyên nhân
       · % case escalate lên Technical mà đáng ra không cần   (đo trực tiếp P1)
       · độ phủ: đã có luật cho bao nhiêu / tổng nguyên nhân đã biết
       Vẫn OPEN — đây là ứng viên, không phải quyết định.
Q-F  PARTIAL 2026-08-21 → có nguyên văn + cấu trúc 5 bước + tập kết luận hữu hạn.
     Còn thiếu: n>1 để biết độ biến thiên. Xem 00_CURRENT_STATE §8.1-KQ.
Q-G  Ai có quyền verify Knowledge?
     → đã thu hẹp: người duyệt phải thấy được mọi nguồn (§1.10)
Q-H  AI có được suggest update knowledge đã verified?
Q-I  Vai trò Secondary Persona (Technical/L3) trong 3 MVP capabilities?
OQ5  Exact vocabularies                            → Step 5
```

## Sinh ra từ Step 1 — cần xử lý ở Step 2–5

```text
N-1  Verification level vocabulary + Lifecycle state vocabulary
     → phải khóa riêng biệt (§1.4 quy tắc 2). Step 5.
N-2  ✅ RESOLVED (T3) — hai type: DIAGNOSTIC + CONDITIONAL_RECOMMENDATION
N-3  ✅ RESOLVED (T1) — đơn vị = một nguyên nhân/cơ chế
N-3b Một "cách nhận ra" to bằng nào?                             → Step 3/4
N-6  Hai nguyên nhân cùng gây một triệu chứng                    → Step 3
N-7  Một case có HAI nguyên nhân đồng thời                       → Step 3
N-8  Nguyên nhân từng đúng, giờ sai vì hệ thống đổi → lifecycle  → Step 5
N-4  Knowledge ↔ Knowledge relationships (supersedes, refines,
     contradicts)                                                → Step 3
N-5  Applicability được biểu diễn thế nào (version range, tenant,
     hệ thống, thời gian)                                        → Step 4
```

## Housekeeping ngược vào tài liệu cũ

```text
H-1  ✅ ĐÃ LÀM 2026-08-21 — PROJECT_CONTEXT §13.4 đã thêm CONFLICTING,
     kèm ghi chú ladder này là verification level (không phải lifecycle state §8.3)
H-2  ✅ ĐÃ LÀM 2026-08-21 — Case v0.2 §11.2 đã thêm đường
     Evidence → Knowledge trực tiếp (K-B9), giữ nguyên quy tắc Case không
     invalidate Official Knowledge
H-3  PROJECT_CONTEXT §5.2 liệt kê "Human knowledge / senior memory"
     dưới mục KNOWLEDGE → phải đánh dấu là knowledge SOURCE (K-B7).
H-4  PROJECT_CONTEXT §14.2 đã SUPERSEDED bởi Case v0.2 (guardrail R7).
H-5  PROJECT_CONTEXT §16 tự nói "3 MVP capabilities chưa formally locked"
     → trái AGENT.md §4. Cần cập nhật.
H-6  NEXT_CONVERSATION_PROMPT (1).md nên archive — chứa Capability #3 bản cũ.
```

---

# 6. Rủi ro của chính tài liệu này

## R-K1 — Dataset đối chiếu vẫn rất mỏng
Canonical Case v0.2 chất lượng cao **vì có 700 case thật để đối chiếu**. Knowledge Model không có gì tương đương.

Đã đỡ hơn so với lúc mở Step 1: §8.1 đã cho **một** quan sát thật (quy trình 5 bước + tập nguyên nhân hữu hạn), và Step 2 được xây trực tiếp trên đó. Nhưng đó là **n=1**.

→ §1 vẫn phần lớn là **suy luận có kỷ luật**. §3 có dữ liệu, nhưng một điểm dữ liệu.

## R-K2 — §8.1 đã chạy · `ĐÃ GIẢI TỎA` 2026-08-21
Kết quả: `00_CURRENT_STATE.md` §8.1-KQ.

`S4` **sống sót**, và ranh giới rơi đúng khe tự nhiên giữa **gom bằng chứng** (B1-B4 → Process) và **kết luận** (B5 → Knowledge). Sạch hơn dự kiến.

Nhưng §8.1 đồng thời phát hiện: SOP tưởng tượng ở `PROJECT_CONTEXT` §5.3 **có nhánh**, còn SOP thật **tuyến tính**. Hai hình dạng khác bản chất → housekeeping `H-7`.

## R-K4 — "5-10 nguyên nhân" là n=1, và toàn bộ §3.5 đứng trên nó
Con số 5-10 là ước lượng của **một người có kinh nghiệm**. Người kinh nghiệm gộp nhóm một cách vô thức; con số thật có thể lớn hơn.

Nếu con số thật là 50 thay vì 10 thì:
```text
· §3.5 sai      — retrieval lại thành bài toán thật, cần index
· T1 vẫn đúng   — đơn vị = nguyên nhân không phụ thuộc số lượng
· T4 vẫn đúng   — verification per assertion không phụ thuộc số lượng
```
→ Chỉ §3.5 (nhận định về quy mô) là dễ vỡ. Ba quyết định T1/T2/T4 độc lập với con số.

→ **`§8.2` (đếm 20 case OTA gần nhất) là việc có giá trị cao nhất còn lại.**

## R-K3 — Chết vì modeling
Xem §0. Failure mode của dự án không phải *"làm sai thứ"* mà là *"không bao giờ làm ra thứ gì"*. Bằng chứng đã hiện hữu: tài liệu 02 mất mà không ai phát hiện → **tốc độ sản xuất tài liệu đã vượt tốc độ sử dụng tài liệu.**

→ Quy tắc tự áp: câu hỏi nào không chặn build thì vào §5 và đi tiếp.

---

# 7. Step tiếp theo

```text
Step 1  Define Knowledge Boundary            ✅ CONFIRMED 2026-08-21
Step 2  Knowledge Concepts & Granularity     ✅ CONFIRMED 2026-08-21
Step 3  Knowledge ↔ Case ↔ Process           🔵 TIẾP THEO
        (mang theo N-3b, N-6, N-7 từ §3.6)
Step 4  Applicability & Provenance
Step 5  Lifecycle & Verification vocabulary  ← khóa vocabulary, giải §6.9 dứt điểm
```

> ✅ **Cổng chặn §8.1 đã mở** — chạy xong 2026-08-21, kết quả ở `00_CURRENT_STATE.md`
> §8.1-KQ. Step 2 được xây trực tiếp trên dữ liệu đó. Xem R-K2 và R-K4.

**Thứ tự workstream** — `CONFIRMED 2026-08-21`: giữ **tuần tự** 04 → 05.
Không tách kernel (§1.4) thành tài liệu riêng, vì thêm một vòng thiết kế là đúng
rủi ro R-K3. Nhưng ghi rõ: kernel là của chung, nên **Workstream 05 có thể bắt
04 sửa lại** — đó là hành vi bình thường, không phải thất bại của Step 1.

Việc nên làm song song, không thuộc thiết kế:

```text
§8.1  ✅ ĐÃ CHẠY 2026-08-21 → 00_CURRENT_STATE §8.1-KQ
§8.2  ĐẾM 20 CASE OTA — việc giá trị cao nhất còn lại. Xác nhận/bác bỏ
      con số 5-10 nguyên nhân, tức là xác nhận/bác bỏ §3.5. Xem R-K4.
§8.3  Khôi phục Success Metrics (Q-E)
§8.4  Bộ eval — Path A sinh nhãn miễn phí, xem D6 hệ quả (2)
```

---

# Nguyên tắc cốt lõi của Knowledge Model v0.1

> **Knowledge là những gì tổ chức đã KHẲNG ĐỊNH ở mức lớp tình huống — không phải tất cả những gì tổ chức CÓ, không phải những gì đã xảy ra trong một case, và không phải danh sách bước để làm một việc.**

Và ranh giới quan trọng nhất:

```text
Case      = việc đang được xử lý
Evidence  = dựa vào đâu
Document  = tri thức được ghi ở đâu
Process   = làm theo bước nào, và đang ở bước nào
Knowledge = tổ chức khẳng định gì, áp dụng cho tình huống nào
```
