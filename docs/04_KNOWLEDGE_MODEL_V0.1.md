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

> ## ⚠️ `R-A1` — §3.5 chỉ phân tích MỘT trong HAI bài toán tìm kiếm
>
> **Phát hiện 2026-08-23 (Workstream 06).** Sinh ra từ câu hỏi của người dùng về việc khách nạp nhiều tài liệu PDF/Word.
>
> Ở MVP có **hai** bài toán tìm kiếm khác nhau. §3.5 chỉ xét bài toán thứ nhất:
>
> ```text
> BÀI TOÁN 1 — khớp bằng chứng với 1 trong ~10 nguyên nhân
>    §3.5 kết luận: PHÂN LOẠI, không phải semantic search
>    → ĐÚNG trong phạm vi nó xét
>    → §8.2 xác nhận hoặc bác bỏ bài toán này
>
> BÀI TOÁN 2 — tìm đúng tài liệu trong N tài liệu khách nạp
>    ⚠ §3.5 KHÔNG xét. Và N có thể là hàng trăm.
>    → §8.2 KHÔNG nói gì về bài toán này
> ```
>
> **§3.5 không sai** — nó đúng trong phạm vi nó xét (đơn vị Knowledge, ~10 record). Nhưng câu *"ở MVP không cần vector DB"* dễ bị đọc rộng hơn phạm vi đó, và bài toán 2 là bài toán tìm kiếm **thật**.
>
> **Câu trả lời hiện tại cho bài toán 2** (`AR4`, `06_MVP_ARCHITECTURE.md` §5): vẫn chưa cần vector DB, nhưng vì **lý do khác** — tài liệu ở bước B3 là *tài liệu hệ thống (API/field/behavior)*, đầy tên field và mã lỗi cụ thể, nên **tìm theo từ khoá thường thắng tìm theo ngữ nghĩa**. Dùng Postgres full-text search trước; `pgvector` khi **đo được** là không đủ.
>
> **Hệ quả cho §8.2:** nó quyết định bài toán 1, **không** quyết định bài toán 2. Câu *"có dựng vector DB không"* thực ra là hai câu hỏi, và cả hai đều có đường nâng cấp rẻ (`AR1`: pgvector là extension của Postgres, không phải service mới).

## 3.6 Còn `OPEN` sau Step 2 — trạng thái lúc đóng Step 2

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

> ✅ **Cập nhật sau Step 3 (2026-08-23):**
>
> ```text
> N-3b  → chuyển sang Step 4, làm cùng N-5 (cùng họ: câu hỏi BIỂU DIỄN)
> N-6   ✅ RESOLVED (L3) — phát biểu phân biệt nằm TRONG record theo T1
> N-7   ✅ RESOLVED (L3) — Case ↔ Knowledge nhiều-nhiều, evidence riêng mỗi link
> N-8   → vẫn Step 5, giờ có SUPERSEDES của L4 làm nguyên liệu
> ```
>
> ⚠️ Nhãn *"chưa có ca thật"* của `N-6`/`N-7` ở trên **hoá ra không phải lý do phải hoãn**: cả hai không đòi thiết kế mới, chỉ đòi kết luận rằng **không cần entity nào**. Xem §3B.3. Giữ nguyên đoạn trên để thấy đánh giá lúc đó.

---

# 3B. Knowledge ↔ Case ↔ Process — Step 3 · `CONFIRMED 2026-08-23`

Bốn quyết định `L1`–`L4` (`L` = **liên kết**), cộng một contradiction giữa hai tài liệu được giải.

Step này khác Step 1 và 2 ở một điểm: **phần lớn công việc đã được làm sẵn** ở `Canonical Case Model v0.2` §11. Step 3 chủ yếu **vá ba lỗ** và **gộp từ vựng trùng**, không phát minh quan hệ mới. Xem §3B.5.

## 3B.0 Contradiction đã giải — Case KHÔNG invalidate Official Knowledge

```text
PROJECT_CONTEXT §13.6            Canonical Case v0.2 §11.2
Case → produces / validates /    "Case không có authority trực tiếp:
       INVALIDATES               Case → INVALIDATE Official Knowledge"
       KnowledgeRecord
```

**v0.2 thắng** — theo luật recency + tính cụ thể ở `AGENT.md` §1. Đường đúng:

```text
Case evidence  →  Knowledge review process  →  Knowledge lifecycle decision
```

**Vì sao không phải chi tiết từ ngữ:** nếu Case invalidate được Knowledge trực tiếp thì `D4` (*AI/hệ thống không tự công nhận tri thức*) bị hở một đường sau — một Case do AI xử lý có thể âm thầm hạ cấp tri thức đã được người duyệt. Guardrail phải kín ở cả hai chiều: không tự **thêm**, cũng không tự **bỏ**.

→ Housekeeping `H-8`: sửa `PROJECT_CONTEXT` §13.6. Làm **ngay**, vì đây là contradiction thật (cùng loại `H-1`/`H-2`).

## 3B.1 Bước của Process trỏ tới Knowledge THEO CHỦ ĐỀ · `CONFIRMED` (L1)

> **Một `ProcessStep` có thể khai báo: "tại bước này, tra tri thức về chủ đề X" — trỏ tới một TẬP tri thức theo chủ đề/applicability, KHÔNG trỏ tới từng record cụ thể.**

### Lỗ mà L1 vá

```text
CÓ:     KnowledgeRecord  →  ProcessDefinition     (T2: "làm quy trình nhiều bước này")
CÓ:     Case → ProcessRun → ProcessDefinition     (v0.2 §11.1)
THIẾU:  bước của Process  →  Knowledge
```

Quy trình thật của first use case đòi đúng chiều đang thiếu:

```text
B1 Kibana → B2 response → B3 tài liệu → B4 issue cũ    =  PROCESS
B5 ĐƯA RA KẾT LUẬN                                      =  tra KNOWLEDGE tại bước này
```

`B5` là **bước của quy trình**, nhưng nội dung nó cần là **Knowledge**. Không có link này thì quy trình thật **không biểu diễn được trong model** — và `B5` chính là chỗ chứa toàn bộ giá trị (§8.1-KQ mục B). Đây là lỗ **chặn việc build**, nên phải giải ở Step 3 theo luật `R-K3`.

### Vì sao theo CHỦ ĐỀ, không phải theo từng record

```text
trỏ từng record  →  mỗi lần Path A sinh một nguyên nhân mới
                    phải sửa ProcessDefinition
                 →  Path A sinh nguyên nhân mới LIÊN TỤC (đó là việc của nó)
                 →  quy trình và tri thức dính chặt vào nhau

trỏ theo chủ đề  →  thêm nguyên nhân thứ 11 không cần chạm vào SOP
```

Đây cũng là điều `K-B6` đòi: **bước** thuộc Process, **kết luận** thuộc Knowledge. L1 giữ đúng ranh giới đó thay vì nhét danh sách nguyên nhân vào trong quy trình.

### Điều L1 KHÔNG làm

```text
L1 không cho ProcessStep CHỨA Knowledge     →  vẫn giữ G2 và S4
L1 không tạo bản sao thứ hai của danh sách bước
L1 không nói cách CHỌN record nào trong tập  →  đó là matching/classification,
                                                thuộc Workstream 06 (xem §3.5)
```

## 3B.2 Thang "tri thức được dùng đến mức nào" — 5 mốc · `CONFIRMED` (L2)

> **Từ vựng DUY NHẤT, thay cả ba bộ đang tồn tại.**

```text
1  RETRIEVED   hệ thống lấy ra                 (có thể không bao giờ hiện)
2  SHOWN       hiện ra trước mắt người
3  OPENED      người mở / đọc
4  ACCEPTED    người chấp nhận
5  USED        có mặt trong KẾT LUẬN của case
```

### Vấn đề mà L2 chữa — ba bộ từ vựng song song

```text
Case v0.2 §11.2      Retrieved · Referenced · Used
Case v0.2 §11.3      retrieved · shown · accepted/rejected · actually used
M3 (2026-08-23)      hiện ra · mở · chấp nhận · có mặt trong kết luận
```

Đây là bệnh §6.9 (*hai vocabulary song song*) tái phát ở chỗ mới. Ba bộ **không xung đột** — chúng chồng nhau lệch. L2 gộp lại.

⚠️ **Ghi rõ để không mất trách nhiệm:** bộ thứ ba do `M3` sinh ra ngày 2026-08-23 mà **không đối chiếu** v0.2 §11.2/§11.3. `L2` **tinh chỉnh `M3`**, không phải mở lại nó — chuỗi 4 mốc của M3 vẫn nằm trong 5 mốc này. Xem `02_SUCCESS_METRICS_V1.md` §2.2.

### Vì sao bỏ `Referenced`

`Referenced` của §11.2 nằm lơ lửng giữa `OPENED` và `ACCEPTED`, và **không thêm thông tin quyết định nào**: biết một tri thức "được nhắc tới" mà không biết nó được chấp nhận hay được dùng thì không giúp quyết định gì. Còn nếu nó có nghĩa *"xuất hiện trong hồ sơ case"* thì đó là dữ liệu của Case, không phải mức độ sử dụng.

### Vì sao 5 mốc chứ không phải một cờ "đã dùng"

Nó cho biết **rơi ở đâu** — mỗi chỗ rơi là một vấn đề khác nhau:

```text
1→2  lấy ra nhưng không hiện        →  vấn đề xếp hạng / ngưỡng
2→3  hiện mà không ai mở            →  vấn đề trình bày / thời điểm
3→4  mở ra rồi thấy không đúng      →  vấn đề CHẤT LƯỢNG tri thức
4→5  chấp nhận nhưng không dùng     →  vấn đề tin cậy, hoặc chưa đủ cụ thể
```

Một con số gộp thì không tách được bốn nguyên nhân này. Đây là `M3` đã lập luận, giữ nguyên.

Và nó thực thi phân biệt đã `CONFIRMED` ở `AGENT.md` §6: **`Knowledge Retrieved ≠ Knowledge Used`** — giờ có 5 mốc thay vì một lời tuyên bố.

## 3B.3 N-6 và N-7 — giải, KHÔNG thêm entity · `CONFIRMED` (L3)

Cả hai từng mang nhãn *"chưa có ca thật → Step 3"* (§3.6). Rà lại thì **không cái nào đòi entity mới**.

### N-7 — một case có HAI nguyên nhân đồng thời

> **`Case ↔ Knowledge` là quan hệ NHIỀU-NHIỀU. Mỗi link mang evidence và verification RIÊNG.**

v0.2 đã hỗ trợ sẵn: *"một Case may contain multiple Problems"* (`PROJECT_CONTEXT` §14.1), và §9 đã viết *"một EvidenceItem có thể liên quan nhiều Case"*.

Điểm quan trọng là **evidence riêng từng link**: một case có thể có nguyên nhân A ở mức `VERIFIED` và nguyên nhân B ở mức `SPECULATIVE`. Gộp thành một mức là vi phạm `G3` và `T4` — đúng lý lẽ đã dùng ở §3.4.

### N-6 — hai nguyên nhân cùng gây một triệu chứng

> **Cái phân biệt chúng (*"nếu thấy thêm X thì là A, không phải B"*) TỰ NÓ là một phát biểu về CÁCH NHẬN RA → nằm TRONG record, theo `T1`.**

Không cần relation mới. Chỉ cần relation khi phát biểu đó nói về **cặp**, không nói về từng cái — và khi đó nó là `CONTRADICTS`/`REFINES` của `L4`, vẫn không phải entity mới.

Có một lý do `D5` để **không** dựng bảng "phân biệt A với B" như một cấu trúc riêng:

```text
bảng phân biệt viết tay        →  GIÀN GIÁO TẠM. Model mạnh lên thì nó tự
                                  đọc hai record rồi suy ra được.
phát biểu do NGƯỜI xác nhận   →  TÀI SẢN BỀN. Nó là tri thức thật.
```

Đặt nó vào *cách nhận ra* của record thì nó là loại thứ hai. Đặt nó vào một bảng riêng thì dễ thành loại thứ nhất.

### Vì sao giải được mà không vi phạm `R-K3`

`R-K3` cấm **thiết kế khi không có dữ liệu đối chiếu**. Nhưng L3 không thiết kế gì mới — nó **kết luận rằng không cần thiết kế gì**, dựa trên quyết định đã có (`T1`, `T4`) và cấu trúc v0.2 đã có. Trả lời *"không cần entity nào"* là một kết luận rẻ và có thể sai an toàn: nếu §8.2 tìm ra ca thật phức tạp hơn, thêm vào sau vẫn được.

## 3B.4 Knowledge ↔ Knowledge · `CONFIRMED` (L4)

> **Ba quan hệ: `SUPERSEDES` · `REFINES` · `CONTRADICTS`.**
> **Lifecycle state `SUPERSEDED` được SUY RA từ quan hệ, KHÔNG lưu riêng.**

### Cái bẫy mà L4 tránh

`SUPERSEDED` đang là một **lifecycle state** ở `PROJECT_CONTEXT` §8.3. Nếu chốt quan hệ `SUPERSEDES` **và** state `SUPERSEDED` độc lập thì cùng một sự thật được lưu hai chỗ, và chúng sẽ lệch nhau — đúng bệnh §6.9 mà `S4` được sinh ra để chữa.

```text
NGUỒN DUY NHẤT:  quan hệ  A SUPERSEDES B
SUY RA:          B có lifecycle state SUPERSEDED
```

Nguyên tắc chung rút ra: **nếu một state chỉ đúng khi tồn tại một quan hệ, thì state đó là phép chiếu của quan hệ, không phải dữ liệu độc lập.** Áp lại được cho Step 5 khi khóa vocabulary.

### `CONTRADICTS` có ca thật ngay

`S8` đã làm `CONFLICTING` **bắt buộc** (bản nháp gom từ N case luôn có chỗ các case không đồng ý). `CONTRADICTS` là chỗ ghi lại điều đó ở mức giữa hai record — không phải quan hệ suy đoán.

`SUPERSEDES` và `REFINES` thì **chưa có ca thật** ở kho 0 record. Vẫn chốt vì chúng rẻ và vì `N-8` (nguyên nhân từng đúng, giờ sai vì hệ thống đổi) đã hẹn Step 5 — `SUPERSEDES` là thứ N-8 sẽ cần.

## 3B.5 Điều Step 3 KHÔNG phải phát minh — `Canonical Case v0.2` đã có

Đây là lần thứ **ba** khoản đầu tư vào Case v0.2 trả cổ tức (hai lần trước: vertical CRM ở §2.4, và `K-B9`).

```text
Case → ProcessRun → ProcessDefinition            v0.2 §11.1  ✓ đã có
Case reference / use / contribute evidence /
     support applicability / challenge Knowledge  v0.2 §11.2  ✓ đã có
Case KHÔNG invalidate Official Knowledge          v0.2 §11.2  ✓ đã có → §3B.0
AssistanceAttempt tách khỏi core Case             v0.2 §11.3  ✓ đã có
Evidence → Knowledge trực tiếp                    v0.2 §11.2  ✓ K-B9
ProcessRun state suy từ CaseAction/Claim/Evidence v0.2 §11.1  ✓ đã có
```

Step 3 chỉ thêm: `L1` (một chiều còn thiếu), `L2` (gộp từ vựng), `L3` (cardinality + không thêm entity), `L4` (quan hệ giữa hai Knowledge).

→ **Không có entity mới nào trong Step 3.** Đó là kết quả tốt, và nhất quán với điều kiện dừng ở §0 (*vừa đủ để build được first use case*).

## 3B.6 Còn `OPEN` sau Step 3

```text
N-3b  Một "cách nhận ra" to bằng nào? (một log line? một pattern? tổ hợp?)
      → chuyển sang Step 4. Nó là câu hỏi BIỂU DIỄN, cùng họ với N-5
        (applicability biểu diễn thế nào). Làm cùng nhau thì rẻ hơn.
N-8   Nguyên nhân từng đúng, giờ sai vì hệ thống đổi → lifecycle nào?
      → Step 5. Giờ đã có SUPERSEDES của L4 làm nguyên liệu.
L1-a  Chọn record nào trong TẬP mà ProcessStep trỏ tới?
      → matching/classification. Workstream 06, không phải domain model (§3.5).
L2-a  Ai/cái gì ghi nhận mốc USED? Hệ thống tự phát hiện hay người xác nhận?
      → gắn với Q-H và cách hiện thực. Chưa chặn việc build.
```

---

# 3C. Applicability & Provenance — Step 4 · `CONFIRMED 2026-08-23`

Bốn quyết định `AP1`–`AP4`.

Step 4 **nhỏ hơn tên gọi của nó**, và ba trong bốn câu trả lời là *"không cần thêm gì"*. Đó là **kết quả**, không phải thất bại — đúng điều kiện dừng ở §0 (*vừa đủ để build first use case*) và đúng luật `R-K3`.

## 3C.1 Applicability là một ASSERTION, kể cả version · `CONFIRMED` (AP1)

> **Applicability là một assertion như mọi assertion khác trong record — có evidence và verification level riêng (`T4`). Nội dung của nó *tình cờ* nói về version.**
> **Thêm cấu trúc CHỈ KHI có ca thật buộc phải thêm, và phải ghi lại ca đó là ca nào.**

### Bằng chứng thật có bao nhiêu

Toàn bộ dự án có **đúng một** ví dụ applicability: *"parser < v2.3 drop payload OTA dạng X"* (§2.1). Một điểm dữ liệu.

Thiết kế một cấu trúc biểu thức applicability trên một điểm dữ liệu là `R-K3` ở dạng rõ nhất: *đào sâu mà không có dữ liệu đối chiếu là đoán một cách cẩn thận, không phải rigor.*

### Vì sao ngay cả `version` cũng không nên thành field có cấu trúc

Đây là điểm ít ngờ nhất của Step 4. Một field `versionRange` nghe vô hại, nhưng nó **giả định mọi khách đều có một hệ thống được đánh version**:

```text
khách #0   version của PMS của họ           →  field chạy tốt
khách B    có thể không có "version" nào theo nghĩa đó
```

`G12` (`AGENT.md` §3.9) nói đặc điểm của khách là **tham số**, không phải hằng số thiết kế. Một `versionRange` có cấu trúc là đúng cái `G12` vừa cấm — nhồi thế giới của khách #0 vào domain model.

Và `D5`: một field cấu trúc cần pipeline parse/normalize version, thứ **model mạnh lên thì tự đọc được**. Cột giàn giáo tạm.

### Tiền lệ đang được dùng lại

`T3` đã loại type `POLICY` vì *"chưa có ca thật"*, kèm câu:

> *"Thêm một type về sau rất dễ. Bỏ một type đã có dữ liệu chạy trên nó thì rất khó."*

`AP1` áp đúng nguyên tắc đó cho applicability.

### Cái giá phải trả — ghi rõ

```text
applicability là chữ  →  hệ thống KHÔNG lọc trước được
                      →  phải dựa vào model đọc và suy luận
```

Chấp nhận được ở quy mô ~10 record, vì §3.5 đã xác định Capability 1 ở MVP là **bài toán phân loại bằng suy luận**, không phải lọc bằng predicate. Nếu §8.2 cho ra 40+ nguyên nhân thì `AP1` phải xem lại cùng §3.5.

## 3C.2 `N-5` co từ 4 chiều xuống 1 — phân loại lại · `CONFIRMED` (AP2)

`N-5` viết *"applicability biểu diễn thế nào (version range, tenant, hệ thống, thời gian)"*. Ba trong bốn chiều **không thuộc applicability**.

```text
TENANT      →  KHÔNG phải applicability. Thuộc trục VISIBILITY.
               Đã quyết ở S7 / §1.10 (hẹp nhất + mở rộng tường minh).

THỜI GIAN   →  "nguyên nhân này đúng trong giai đoạn X" thực chất là N-8
               (từng đúng, giờ sai vì hệ thống đổi) → LIFECYCLE, Step 5.

HỆ THỐNG    →  chưa có ca thật. Khách #0 chỉ có một hệ thống. → OPEN.

VERSION     →  chiều DUY NHẤT có ca thật → và theo AP1, là assertion.
```

### Vì sao tách tenant ra là quan trọng, không phải chuyện từ ngữ

```text
APPLICABILITY  →  tri thức này có ÁP DỤNG cho tình huống này không?
VISIBILITY     →  tổ chức này có ĐƯỢC THẤY tri thức này không?
```

`AP1` vừa quyết applicability là **chữ**, dựa vào model đọc. Nếu tenant nằm trong applicability thì **ranh giới tenant trở thành thứ do model suy luận** — trái `G7` (*security/tenant boundary là nền tảng, không phải phần bù*).

Ranh giới tenant không được phép mềm. Applicability thì được.

## 3C.3 Provenance gắn ở TỪNG ASSERTION · `CONFIRMED` (AP3)

> **Một `KnowledgeRecord` KHÔNG có một origin duy nhất, KHÔNG có một tác giả duy nhất.**

`T4` gắn **verification** ở từng assertion. `S8` gắn **evidence** ở từng assertion. `AP3` gắn **origin** ở cùng chỗ đó.

### Trường hợp sai nếu gắn ở mức record

```text
RECORD   origin = AI_INFERENCE          (vì Path A gom từ 20 case)

  A1  "nguyên nhân này tồn tại"          ← AI gom được từ 20 case
  A2  "nhận ra bằng log pattern ..."     ← AI gom được
  A3  "xử lý: nâng version"              ← SENIOR TỰ VIẾT VÀO
  A4  "cũng gây lỗi ở luồng Y"           ← AI suy đoán, 1 case
```

Ghi cả record là `AI_INFERENCE` thì **mất đúng thứ `G6` muốn giữ**: `A3` do một người có chuyên môn tự viết, và điều đó thay đổi mức đáng tin của nó. Ghi cả record là `HUMAN` thì `A4` được thăng oan.

Đây **cùng một lập luận** §3.4 đã dùng cho verification level, chỉ áp cho origin:

> *Một con số duy nhất cho cả bốn thì buộc phải nói quá hoặc nói thiếu.*

### Không lưu ở cả hai mức

Có thể muốn giữ thêm một origin ở mức record (*"record này sinh ra thế nào"*). Không làm — áp nguyên tắc vừa rút ra ở `L4`:

> **Nếu một thông tin chỉ đúng khi suy từ các thành phần, thì nó là phép chiếu, không phải dữ liệu độc lập.**

*"Record này sinh ra thế nào"* suy được từ origin của các assertion. Lưu riêng thì hai chỗ sẽ lệch nhau — đúng bệnh §6.9.

### Hệ quả cho Path A và cho bộ eval

```text
bản nháp Path A  →  mọi assertion origin = AI_INFERENCE
người sửa/duyệt  →  assertion nào bị sửa thì origin đổi, assertion nào
                    giữ nguyên thì vẫn AI_INFERENCE
                 →  diff(A,B) của M2 giờ đọc được Ở MỨC ASSERTION
```

`M2` (mức sửa của người duyệt) trước đó là *"% assertion bị sửa/xoá/thêm"* — `AP3` là thứ làm con số đó tính được. Và `K-B5` được giữ đúng: origin `AI_INFERENCE` **không mất** sau khi người verify; nó nằm cạnh verification level, không bị ghi đè.

## 3C.4 `N-3b` — đã được `T4` giải · `CONFIRMED` (AP4)

> **Một "cách nhận ra" = MỘT ASSERTION. Bên trong nó là một log line hay một tổ hợp — đó là NỘI DUNG, không đổi ĐƠN VỊ của model.**

Ví dụ thật ở §3.4 đã cho thấy đơn vị:

```text
A2  nhận ra bằng log pattern "..."   SUPPORTED   12/14 case
    └── MỘT assertion · MỘT verification level · MỘT bộ evidence
```

Nếu cách nhận ra là *"log X **cùng với** response Y"* thì nó vẫn là một assertion — vì nó được xác minh như một khối (*"tổ hợp này xuất hiện ở 12/14 case"*), không xác minh rời từng nửa.

### Vì sao KHÔNG tách recognition pattern thành đối tượng dùng chung

Nghe hợp lý: hai nguyên nhân cùng triệu chứng (`N-6`) thì pattern bị lặp ở hai record, có thể lệch nhau. Nhưng `T1` đã lập luận thứ tự bền vững:

```text
tín hiệu    →  đổi khi log/API đổi format      ← DỄ MỤC NHẤT
cách xử lý  →  đổi theo thời gian, theo version
nguyên nhân →  sống lâu                         ← BỀN NHẤT  → làm identity
```

Biến thứ **dễ mục nhất** thành entity hạng nhất là đi ngược `T1` và `D5`. Và pattern trùng nhau ở hai record **không phải lỗi** — nó chính là dữ liệu nói *"hai nguyên nhân này trông giống nhau"*, đúng thứ `L3`/`N-6` cần.

## 3C.5 Hình dạng đầy đủ của một KnowledgeRecord — sau bốn Step

Đây là lần đầu vẽ được trọn vẹn. Mọi dòng đều trỏ về một quyết định đã chốt, không có gì mới.

```text
KNOWLEDGE RECORD  =  một NGUYÊN NHÂN + một cụm ASSERTION về nó

  identity     nguyên nhân / cơ chế                              T1
  type         DIAGNOSTIC | CONDITIONAL_RECOMMENDATION            T3
  visibility   hẹp nhất trong các nguồn + ai mở rộng, khi nào     S7

  assertion[]  ── mỗi assertion mang RIÊNG bốn thứ:

     nội dung       "nguyên nhân này tồn tại"
                    "nhận ra bằng log pattern ..."          ← N-3b/AP4
                    "áp dụng cho bản < 2.3"                 ← applicability/AP1
                    "xử lý: nâng version"                   ← T2
     origin         SYSTEM_FACT | USER_CONFIRMED |           AP3
                    AI_INFERENCE | HUMAN_ASSESSMENT |        (v0.2 §7.1
                    IMPORTED_SOURCE_ASSERTION                 — khóa ở §3D.7)
     actor          ai/cái gì đưa ra assertion này           AP3 · V5
     evidence[]     → EvidenceItem, kể cả trỏ trực tiếp      S8 · K-B9
     verification   SPECULATIVE→PLAUSIBLE→SUPPORTED→VERIFIED T4 · S8 · V1
                    CONFLICTING | INVALIDATED  (ngoài thang)

  lifecycle (MỨC RECORD, không per assertion)                  V2
     lưu       DRAFT · ACTIVE · DEPRECATED                     V3
     suy ra    NEEDS_REVIEW · SUPERSEDED                        V3 · L4

  quan hệ
     → ProcessDefinition      khi xử lý cần NHIỀU BƯỚC          T2
     → Knowledge              SUPERSEDES · REFINES · CONTRADICTS L4
     ← ProcessStep CONSULTS   theo CHỦ ĐỀ, không theo record     L1
     ↔ Case                   NHIỀU-NHIỀU, evidence riêng mỗi link L3
     ↔ AssistanceAttempt      5 mốc RETRIEVED…USED               L2
```

> ⚠️ **Bản đầu của §3C.5 ghi `origin = AI_INFERENCE | HUMAN | SYSTEM_FACT` — SAI.**
> `HUMAN` không có trong v0.2 §7.1, và nó **gộp mất** phân biệt mà v0.2 §7.5 dựng
> riêng một mục để bảo vệ: `USER_CONFIRMED` ≠ sự thật khách quan. Grep của Step 5
> bắt được. Đã sửa. Xem §3D.6.

### ⚠️ Một bất đối xứng CÓ Ý THỨC, không phải bỏ sót

```text
origin · evidence · verification   →  gắn ở TỪNG ASSERTION
visibility                          →  gắn ở MỨC RECORD
```

`Q-D` (tách kết luận khỏi dẫn chứng ở **mức từng câu**, redaction chọn lọc) đã được hoãn sang **v2** ngay từ `S7`. Nên visibility ở mức record là **quyết định đã có**, không phải chỗ quên. Khi `Q-D` được giải thì bất đối xứng này mới biến mất.

## 3C.6 `Authority` — đẩy sang Step 5

`S4` liệt kê `Authority` trong kernel dùng chung nhưng chưa ai định nghĩa. Nó **có** ca thật (email hướng dẫn của senior, §2.3).

Nhưng chính §2.3 đã kết luận ca đó *"củng cố `S4`: **verification level** phải tách khỏi **lifecycle state**"* — tức nó là `N-1`, thuộc **Step 5**. Phân biệt mà Authority cần diễn đạt:

```text
"một chuyên gia nói câu này một lần trong email"
        vs
"tổ chức đã review và công bố"
```

Đó đúng là hai trục verification/lifecycle. Định nghĩa Authority riêng ở Step 4 rồi Step 5 lại khóa vocabulary sẽ tạo bản thứ hai của cùng một sự thật — §6.9 lần thứ ba.

→ `Authority` chờ Step 5, làm cùng `N-1`.

## 3C.7 Còn `OPEN` sau Step 4 — trạng thái lúc đóng Step 4

```text
AP-a  Chiều "hệ thống nào" của applicability — chưa có ca thật.
      Sẽ có ngay khi bán cho khách thứ hai. Ghi lại để không quên.
AP-b  Ca thật nào sẽ buộc applicability phải có cấu trúc?
      → điều kiện xem lại AP1. Nếu §8.2 ra 40+ nguyên nhân thì xem lại
        cùng §3.5.
N-1   Verification level vocabulary + Lifecycle state vocabulary   → Step 5
N-8   Nguyên nhân từng đúng, giờ sai vì hệ thống đổi               → Step 5
      (bao gồm cả chiều "thời gian" của N-5, theo AP2)
Auth  Authority                                                    → Step 5
Q-D   Visibility ở mức từng câu + redaction                        → v2
```

> ✅ **Cập nhật sau Step 5 (cùng ngày):** `N-1` ✅ (V1+V2+V3) · `N-8` ✅ (V4) ·
> `Auth` ✅ (V5 — là `Actor` của v0.2 §7, không cần trục thứ ba).
> `AP-a`, `AP-b`, `Q-D` vẫn OPEN, không chặn build. Xem §3D.9.
>
> ⚠️ Step 5 cũng **sửa một lỗi của Step 4**: danh sách giá trị `origin` ở §3C.5
> viết sai. Xem §3D.6. `AP3` (provenance per assertion) không đổi.

---

# 3D. Lifecycle & Verification vocabulary — Step 5 · `CONFIRMED 2026-08-23`

**Step cuối của workstream 04.** Bốn quyết định `V1`–`V4`, cộng `Authority` được giải bằng phân rã, cộng một lỗi từ vựng của Step 4 được sửa.

Đây là chỗ **§6.9 phải chết**. Nên bước đầu tiên là grep toàn bộ tài liệu — và nó bắt được ba thứ trước khi chốt bất cứ gì.

## 3D.0 Kết quả grep — bốn từ vựng đang tồn tại

```text
Case v0.2 §7      Provenance = Origin · Actor · Source · Evidence · Time · Verification
Case v0.2 §7.1    Origin: SYSTEM_FACT · USER_CONFIRMED · AI_INFERENCE ·
                          HUMAN_ASSESSMENT · IMPORTED_SOURCE_ASSERTION
Case v0.2 §7.3    Verification: SPECULATIVE · PLAUSIBLE · SUPPORTED ·
                                VERIFIED · CONFLICTING · INVALIDATED
PROJECT_CONTEXT §8.3   Lifecycle: DRAFT · VERIFIED · ACTIVE · NEEDS_REVIEW ·
                                  DEPRECATED · SUPERSEDED
```

### Hai thứ grep bắt được mà không ai biết

**(1) `VERIFIED` đã bị lặng lẽ bỏ khỏi lifecycle khi viết file này.**

```text
PROJECT_CONTEXT §8.3   DRAFT · VERIFIED · ACTIVE · NEEDS_REVIEW · DEPRECATED · SUPERSEDED
04 §1.4 (trích §8.3)   DRAFT ·            ACTIVE · NEEDS_REVIEW · DEPRECATED · SUPERSEDED
```

Không ai ghi lại việc bỏ đó. Nên **§6.9 đã tái phát BA lần**, không phải hai: `H-1` (`CONFLICTING` thiếu), `L2` (ba bộ từ vựng mức-độ-dùng), và divergence này.

**(2) `AP3` của Step 4 tự tạo bộ từ vựng Origin thứ hai — lỗi của phiên trước.** Xem §3D.6.

## 3D.1 Hai trục, hai bộ từ vựng, KHÔNG từ nào trùng · `CONFIRMED` (V1)

> **`VERIFIED` bị bỏ khỏi trục LIFECYCLE. Trục verification giữ nguyên 6 giá trị của Case v0.2 §7.3.**

```text
TRỤC VERIFICATION — "nhận định này được xác minh tới mức nào?"
   SPECULATIVE → PLAUSIBLE → SUPPORTED → VERIFIED     ← THANG, đơn điệu tăng
   CONFLICTING                                        ← KHÔNG trên thang
   INVALIDATED                                        ← KHÔNG trên thang

TRỤC LIFECYCLE — "tổ chức đã công bố tri thức này chưa, còn dùng không?"
   DRAFT · ACTIVE · NEEDS_REVIEW · DEPRECATED · SUPERSEDED
```

### Vì sao bên lifecycle nhường, không phải bên verification

Hai bên **không cân sức**:

```text
VERIFIED ở verification   dùng trong ví dụ thật §3.4 (A1, A3)
                          CONFIRMED ở Case v0.2 §7.3 và §7.4
                          là KERNEL DÙNG CHUNG, CaseClaim cũng dùng   → nặng

VERIFIED ở lifecycle      trùng nghĩa với ACTIVE ngay trong cùng danh sách
                          04 §1.4 đã bỏ rồi mà không ai phản đối       → nhẹ
```

Nó vốn đang cố nói *"đã được duyệt"* — mà `ACTIVE` đã nói điều đó. **Bỏ một giá trị giải được cả trùng nghĩa và xung đột.**

→ Housekeeping `H-9`: sửa `PROJECT_CONTEXT` §8.3.

### Thang verification KHÔNG phải một đường thẳng

§13.4 trình bày cả 6 giá trị như một *ladder*. Sai, và cái sai này có hậu quả thật:

`S8` làm `CONFLICTING` **bắt buộc** — một bản nháp gom từ 20 case luôn có chỗ các case không đồng ý, và **chính chỗ đó là chỗ người duyệt cần nhìn**. Nếu `CONFLICTING` bị đọc như *"giữa SUPPORTED và VERIFIED"* thì nó bị xếp hạng như một mức tin trung bình, và chỗ tranh chấp **biến mất khỏi mắt người duyệt**.

```text
CONFLICTING   =  bằng chứng chỉ HAI HƯỚNG. Không phải "hơi tin".
INVALIDATED   =  từng tin, nay bị bác. Không phải "rất không tin".
```

### `PLAUSIBLE` chưa có ca thật — nhưng vẫn giữ

Ví dụ thật ở §3.4 chỉ dùng `VERIFIED`, `SUPPORTED`, `SPECULATIVE`. `PLAUSIBLE` chưa dùng lần nào.

Theo tiền lệ `T3` thì đáng bỏ. **Nhưng không bỏ**, vì `S4`: đây là **kernel dùng chung** cho `CaseClaim` và `ProcessDefinition`. Ngưỡng để **bỏ** một giá trị khỏi kernel phải cao hơn ngưỡng để thêm — bỏ ở đây có thể làm hỏng domain Case mà Knowledge Model không thấy.

→ Giữ, ghi nhận là chưa dùng. Nếu Workstream 05 cũng không dùng thì bàn lại lúc đó.

## 3D.2 Verification gắn ở ASSERTION, lifecycle gắn ở RECORD · `CONFIRMED` (V2)

```text
verification level  →  TỪNG ASSERTION        T4 đã chốt
origin · actor      →  TỪNG ASSERTION        AP3 đã chốt
evidence            →  TỪNG ASSERTION        S8 đã chốt
lifecycle state     →  MỨC RECORD            V2
visibility          →  MỨC RECORD            S7
```

### Vì sao lifecycle ở mức record

`S7` đã quyết: duyệt **nội dung** và mở **quyền xem** là **MỘT hành động**, do một người thấy được mọi nguồn. Một hành động thì tạo ra một chuyển trạng thái, ở một mức.

Và §6.4 là ràng buộc cứng: *chi phí capture phải gần bằng 0.* Bắt người duyệt bấm phê duyệt từng assertion là đúng cái đã làm field `Version đang sử dụng` trống 100/100.

### `V2` GIẢI THÍCH bất đối xứng đã ghi ở §3C.5

§3C.5 ghi nhận một bất đối xứng (*ba thứ per-assertion, visibility per-record*) và gọi nó là "có ý thức, do `Q-D` hoãn sang v2". Đúng, nhưng chưa đầy đủ. Lý do sâu hơn:

> **Visibility ở mức record vì DUYỆT ở mức record. Và duyệt ở mức record vì `S7` đã gộp duyệt-nội-dung với mở-quyền-xem thành một hành động.**

Bất đối xứng đó không phải hai quyết định độc lập tình cờ khớp nhau — nó là **một** quyết định (`S7`) nhìn từ hai phía. Khi `Q-D` được giải ở v2 thì cả visibility **và** lifecycle sẽ cùng đi xuống mức mịn hơn, hoặc cả hai cùng đứng.

## 3D.3 Lưu ba, suy ra hai · `CONFIRMED` (V3)

> **LƯU: `DRAFT` · `ACTIVE` · `DEPRECATED`**
> **SUY RA: `NEEDS_REVIEW` · `SUPERSEDED`**

Đây là lần thứ **ba** áp nguyên tắc rút ra ở `L4`:

> *Nếu một thông tin chỉ đúng khi suy từ thứ khác, thì nó là phép chiếu, không phải dữ liệu độc lập.*

### Ba state được lưu

```text
DRAFT       chưa từng được duyệt
ACTIVE      đã được duyệt (nội dung + quyền xem, một hành động — S7)
DEPRECATED  người có quyền RÚT nó, không có bản thay thế
```

### Hai state được suy ra

```text
SUPERSEDED   ⟸  tồn tại quan hệ  A SUPERSEDES nó          (L4 đã chốt)
             khác DEPRECATED: có bản thay thế CỤ THỂ

NEEDS_REVIEW ⟸  BẤT KỲ điều kiện nào sau đây:
                · có assertion thêm sau lần duyệt cuối, chưa được duyệt
                · có assertion ở verification = INVALIDATED
                · có assertion ở verification = CONFLICTING
                · có quan hệ CONTRADICTS tới một record khác
                · một nguồn chống lưng bị đổi hoặc bị xoá
```

**Hệ quả quan trọng: `NEEDS_REVIEW` được KÍCH HOẠT, không phải ai đó tự chọn.** Một tri thức có assertion vừa bị bác bỏ sẽ **không** nằm im ở `ACTIVE` cho tới khi có người để ý.

### Hai hệ quả phải nói rõ

**(1) `NEEDS_REVIEW` KHÔNG rút tri thức khỏi retrieval — nó gắn cờ.**

Rút đi thì mất đúng giá trị sản phẩm đang bán (§6.3: chuyển tri thức từ *không tìm được* sang *tìm được*). Và nó trái `G4`: hiển thị *"tri thức này đang có tranh chấp"* là thông tin, không phải lý do để im lặng. Cùng triết lý `S8` — **bày chỗ xung đột ra, đó là chỗ người ta cần nhìn.**

**(2) `SUPERSEDED` thắng `NEEDS_REVIEW` khi cả hai cùng đúng.** Không có lý do bắt review một thứ đã bị thay thế.

### Máy trạng thái

```text
          duyệt (S7: nội dung + quyền xem, MỘT hành động)
DRAFT  ─────────────────────────────────────────────────►  ACTIVE
                                                             │
                                              người RÚT      │
                                       ◄─────────────────────┤
                                    DEPRECATED               │
                                                             │
  hiển thị thêm (suy ra, KHÔNG đổi state lưu):               │
      NEEDS_REVIEW   nếu có trigger ở trên  ◄────────────────┤
      SUPERSEDED     nếu có A SUPERSEDES nó ◄────────────────┘
                     (thắng NEEDS_REVIEW)
```

## 3D.4 `N-8` tách thành HAI ca khác nhau · `CONFIRMED` (V4)

`N-8` hỏi: *nguyên nhân từng đúng, giờ sai vì hệ thống đổi — lifecycle nào?* Câu hỏi giả định **một** ca. Thực ra là **hai**, và chúng đi hai trục khác nhau.

```text
CA (a) — vẫn ĐÚNG, nhưng không còn ai gặp
   "parser < 2.3 drop payload dạng X"   →  không còn khách nào chạy < 2.3
   verification  KHÔNG ĐỔI  (vẫn VERIFIED — nó vẫn đúng!)
   lifecycle     DEPRECATED
   → applicability hết hiệu lực, phát biểu thì không

CA (b) — từng đúng, giờ SAI
   "phải gọi OTA trước khi check log"   →  quy trình đã đổi, làm vậy giờ là sai
   verification  INVALIDATED
   lifecycle     → NEEDS_REVIEW (suy ra), rồi người quyết DEPRECATED
```

### Ca (a) là bằng chứng việc tách hai trục kiếm được chỗ đứng

Nếu chỉ có **một** trục, ca (a) buộc phải gắn nhãn `INVALIDATED` cho một phát biểu **vẫn đúng**. Ba hậu quả:

```text
· một lời nói sai nằm trong dữ liệu           → vi phạm G3
· bộ eval nhận nhãn sai                        → phá D5 hệ quả 1
· sau này nâng cấp lại khách cũ lên < 2.3?
  tri thức đúng đã bị đánh dấu là sai          → mất tri thức thật
```

`N-1` không phải chuyện gọn gàng từ ngữ. Ca (a) là chỗ nó trả tiền.

### `AP2` đã gửi chiều "thời gian" tới đây, và nó có nhà

`AP2` chuyển chiều *thời gian* của applicability sang lifecycle. Nó rơi đúng ca (a): applicability có giới hạn thời gian → tới lúc nào đó không còn khớp ai → `DEPRECATED`. **Không cần biểu diễn thời gian riêng.** Routing của `AP2` đúng.

## 3D.5 `Authority` — đã có nhà: `Actor` trong v0.2 §7 · `CONFIRMED` (V5)

`S4` đưa `Authority` vào kernel nhưng chưa ai định nghĩa. §2.3 cho nó một ca thật (email hướng dẫn của senior). Grep cho thấy **v0.2 §7 đã có `Actor`** như một thành phần của Provenance.

Phân biệt mà §2.3 cần, ghép từ những thứ đã có:

```text
"một chuyên gia nói câu này một lần trong email"
   Origin = HUMAN_ASSESSMENT · Actor = senior X · lifecycle = DRAFT

"tổ chức đã review và công bố"
   lifecycle = ACTIVE

"vendor OTA nói rằng..."
   Origin = IMPORTED_SOURCE_ASSERTION · Actor = vendor
```

→ **`Authority` KHÔNG cần trục thứ ba.** Nó là `Actor` + `Origin` + hai trục đã khóa.

Và điều này xác nhận `AP3` đúng khi gắn provenance ở từng assertion: `Actor` của `A3` (senior tự viết) khác `Actor` của `A1` (AI gom từ 20 case).

⚠️ **Không mô hình hóa "chức danh / mức chuyên môn" của Actor.** Đó là org/permission model, ngoài phạm vi. Quyền duyệt đã được quyết trên cơ sở khác: `S7` — *người duyệt phải là người thấy được mọi nguồn*.

## 3D.6 Sửa lỗi từ vựng của Step 4 — `AP3` / §3C.5

Hôm nay ở §3C.5 viết `origin = AI_INFERENCE | HUMAN | SYSTEM_FACT`. **Sai.** v0.2 §7.1 có 5 giá trị, và `HUMAN` không phải một trong đó:

```text
SAI (§3C.5 bản đầu)          ĐÚNG (v0.2 §7.1)
AI_INFERENCE                 SYSTEM_FACT
HUMAN            ←  gộp mất  USER_CONFIRMED
SYSTEM_FACT                  AI_INFERENCE
                             HUMAN_ASSESSMENT
                             IMPORTED_SOURCE_ASSERTION
```

`HUMAN` gộp mất phân biệt mà v0.2 §7.5 đã dựng riêng một mục để bảo vệ: **`USER_CONFIRMED` ≠ sự thật khách quan.** *"User nói mapping đúng rồi"* là fact rằng **user đã nói vậy**, không phải fact rằng mapping đúng. Gộp `USER_CONFIRMED` với `HUMAN_ASSESSMENT` thành `HUMAN` là xóa đúng ranh giới đó.

→ §3C.5 đã được sửa dùng 5 giá trị của v0.2. `AP3` (provenance per assertion) **không đổi** — chỉ danh sách giá trị được sửa.

→ Đây là §6.9 lần thứ ba, nhưng là lần đầu bị bắt **trước** khi vào tài liệu như quyết định. Kỷ luật grep có tác dụng ngay trong phiên đầu áp dụng nó.

## 3D.7 Từ vựng đã KHÓA — bảng tham chiếu duy nhất

> Mọi tài liệu khác trỏ về đây. Không định nghĩa lại ở chỗ nào.

```text
┌─ PROVENANCE (Case v0.2 §7) — gắn ở TỪNG ASSERTION ────────────────────┐
│                                                                        │
│  Origin      SYSTEM_FACT · USER_CONFIRMED · AI_INFERENCE ·            │
│              HUMAN_ASSESSMENT · IMPORTED_SOURCE_ASSERTION              │
│              → giữ VĨNH VIỄN, không rewrite sau khi verify (v0.2 §7.4) │
│  Actor       ai/cái gì đưa ra assertion đó       ← đây là "Authority"  │
│  Source      nguồn chứa                                                │
│  Evidence[]  → EvidenceItem, kể cả trỏ trực tiếp (K-B9)               │
│  Time        thời điểm                                                 │
│                                                                        │
│  Verification   SPECULATIVE → PLAUSIBLE → SUPPORTED → VERIFIED        │
│                 CONFLICTING     ← KHÔNG trên thang                     │
│                 INVALIDATED     ← KHÔNG trên thang                     │
└────────────────────────────────────────────────────────────────────────┘

┌─ LIFECYCLE — gắn ở MỨC RECORD ────────────────────────────────────────┐
│  LƯU      DRAFT · ACTIVE · DEPRECATED                                 │
│  SUY RA   NEEDS_REVIEW  ⟸ trigger (§3D.3)                             │
│           SUPERSEDED    ⟸ quan hệ A SUPERSEDES nó (L4)                │
└────────────────────────────────────────────────────────────────────────┘

┌─ VISIBILITY — gắn ở MỨC RECORD (S7 / §1.10) ──────────────────────────┐
│  hẹp nhất trong các nguồn · ai mở rộng · khi nào · từ đâu tới đâu      │
└────────────────────────────────────────────────────────────────────────┘

KHÔNG có từ nào xuất hiện ở hai trục.  §6.9 đóng.
```

### Ba trạng thái coverage — KHÔNG thuộc bảng này

`KNOWLEDGE_ABSENT` / `EXISTS_NOT_RETRIEVABLE` / `EXISTS_RETRIEVABLE` (§6.3 của `00_CURRENT_STATE`) là trạng thái của **một CHỦ ĐỀ**, không phải của một `KnowledgeRecord`. Nhà của chúng là concept `Knowledge Gap` — **future capability**, không phải MVP. §4 của file này đã ghi; nhắc lại ở đây để không ai kéo chúng vào bảng khóa.

## 3D.8 Kiểm tra điều kiện dừng của workstream 04

§0 đặt điều kiện: *vừa đủ để build được first use case; câu hỏi nào không chặn build thì ghi Open Questions và đi tiếp.* Kiểm lại bằng quy trình thật (§8.1-KQ):

```text
B1-B4  gom bằng chứng      →  Process domain. ProcessStep CONSULTS Knowledge (L1) ✓
B5     đưa ra kết luận     →  KnowledgeRecord = một nguyên nhân (T1)              ✓
       nhận ra bằng gì     →  assertion, có evidence + verification riêng (AP4/T4) ✓
       áp dụng cho ai      →  assertion (AP1)                                      ✓
       làm gì tiếp         →  trong record, hoặc trỏ ProcessDefinition (T2)        ✓
       ai nói, tin đến đâu →  Origin · Actor · Verification (V1/V5)                ✓
       công bố chưa        →  lifecycle DRAFT/ACTIVE (V1/V2/V3)                    ✓
       đo được không       →  5 mốc RETRIEVED…USED (L2) + M1-M4                    ✓
       ai được xem         →  visibility hẹp nhất + mở rộng tường minh (S7)        ✓
       gom từ 20 case cũ   →  Path A, evidence từng phát biểu, CONFLICTING (S5/S8) ✓
```

→ **Đủ để build. Workstream 04 đóng ở v0.1.**

Không có entity mới nào ở Step 3, Step 4, Step 5. Ba step cuối gần như chỉ **gộp, vá và loại bỏ**.

## 3D.9 Còn `OPEN` sau Step 5

```text
QM-1   Ngưỡng cụ thể của Success Metrics        → cần chạy thật vài tuần
AP-a   Chiều "hệ thống nào" của applicability   → khách thứ hai
AP-b   Ca thật nào buộc applicability có cấu trúc → điều kiện xem lại AP1
L1-a   Chọn record nào trong tập ProcessStep trỏ tới → Workstream 06
L2-a   Ai ghi nhận mốc USED                     → gắn Q-H
Q-D    Visibility mức từng câu + redaction      → v2
Q-G    Ai có quyền verify (đã thu hẹp bởi S7)   → chưa khóa hết
Q-H    AI có được suggest update knowledge đã ACTIVE?
Q-I    Vai trò Secondary Persona L3             → gắn QM-4
PLAUS  PLAUSIBLE chưa có ca thật — bàn lại nếu Workstream 05 cũng không dùng
```

Không câu nào chặn việc build. Đúng luật §0.

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

## Step 3 — `CONFIRMED 2026-08-23`

```text
L1   ProcessStep trỏ tới Knowledge THEO CHỦ ĐỀ, không trỏ từng record
     → vá chiều còn thiếu: bước B5 "đưa ra kết luận" của quy trình thật
     → thêm nguyên nhân thứ 11 không phải sửa ProcessDefinition
L2   Thang 5 mốc, TỪ VỰNG DUY NHẤT thay cả ba bộ đang tồn tại:
       RETRIEVED → SHOWN → OPENED → ACCEPTED → USED
     → bỏ "Referenced" (không thêm thông tin quyết định)
     → TINH CHỈNH M3, không mở lại nó
L3   N-6 + N-7 giải, KHÔNG thêm entity:
       N-7  Case ↔ Knowledge NHIỀU-NHIỀU, evidence + verification RIÊNG mỗi link
       N-6  phát biểu phân biệt nằm TRONG record theo T1
L4   Knowledge ↔ Knowledge: SUPERSEDES · REFINES · CONTRADICTS
     → lifecycle state SUPERSEDED là SUY RA từ quan hệ, KHÔNG lưu riêng
     → nguyên tắc chung: state chỉ đúng khi có một quan hệ = phép chiếu
       của quan hệ, không phải dữ liệu độc lập

§3B.0  Contradiction đã giải: Case KHÔNG invalidate Official Knowledge
       v0.2 §11.2 thắng PROJECT_CONTEXT §13.6 → housekeeping H-8
       Lý do sâu: nếu Case invalidate được thì D4 hở một đường sau

KẾT QUẢ: KHÔNG có entity mới nào trong Step 3.
```

## Step 4 — `CONFIRMED 2026-08-23`

```text
AP1  Applicability là một ASSERTION, kể cả version — có evidence +
     verification riêng (T4). KHÔNG field versionRange có cấu trúc.
     → lý do: G12 (không phải khách nào cũng có "version") + D5 (giàn giáo)
     → tiền lệ T3 khi loại type POLICY: thêm sau dễ, bỏ sau rất khó
     → cái giá: hệ thống không lọc trước được, phải dựa model suy luận.
       Chấp nhận ở quy mô ~10 record (§3.5). Xem lại nếu §8.2 ra 40+.
AP2  N-5 co từ 4 chiều xuống 1 — ba chiều KHÔNG thuộc applicability:
       tenant     → trục VISIBILITY, đã quyết ở S7        (G7: không được mềm)
       thời gian  → LIFECYCLE, là N-8                     → Step 5
       hệ thống   → chưa có ca thật                       → OPEN (AP-a)
       version    → chiều duy nhất có ca thật             → assertion (AP1)
AP3  Provenance (origin) gắn ở TỪNG ASSERTION, khớp T4 + S8.
     → record KHÔNG có một origin/tác giả duy nhất
     → KHÔNG lưu origin ở cả hai mức (nguyên tắc L4: phép chiếu ≠ dữ liệu)
     → làm M2 ("% assertion bị sửa") tính được, và giữ K-B5 (origin
       AI_INFERENCE không mất sau khi người verify)
AP4  N-3b: một "cách nhận ra" = MỘT ASSERTION — T4 đã giải sẵn.
     "Một log line hay một tổ hợp" là NỘI DUNG, không đổi ĐƠN VỊ.
     → KHÔNG tách recognition pattern thành đối tượng dùng chung:
       T1 đã xếp tín hiệu là thứ DỄ MỤC NHẤT

§3C.5  Hình dạng đầy đủ của KnowledgeRecord sau 4 Step — mọi dòng
       trỏ về một quyết định đã chốt, không có gì mới.
§3C.6  Authority → Step 5, làm cùng N-1 (tránh §6.9 lần thứ ba).

KẾT QUẢ: KHÔNG có entity mới nào trong Step 4. Ba trong bốn câu trả lời
         là "không cần thêm gì".
```

## Step 5 — `CONFIRMED 2026-08-23` · STEP CUỐI, workstream 04 ĐÓNG

```text
V1   Hai trục, KHÔNG từ nào trùng. VERIFIED bị bỏ khỏi trục LIFECYCLE.
       verification  SPECULATIVE→PLAUSIBLE→SUPPORTED→VERIFIED  (thang)
                     CONFLICTING · INVALIDATED                 (NGOÀI thang)
       lifecycle     DRAFT · ACTIVE · NEEDS_REVIEW · DEPRECATED · SUPERSEDED
     → bên lifecycle nhường vì VERIFIED ở đó trùng nghĩa ACTIVE, và 04 §1.4
       đã lặng lẽ bỏ nó rồi. Bên verification là kernel dùng chung (S4).
     → PLAUSIBLE chưa có ca thật nhưng GIỮ: ngưỡng bỏ khỏi kernel cao hơn
       ngưỡng thêm. Bỏ ở đây có thể hỏng domain Case.
     → housekeeping H-9: PROJECT_CONTEXT §8.3
V2   verification/origin/actor/evidence → TỪNG ASSERTION
     lifecycle + visibility             → MỨC RECORD
     → vì S7: duyệt nội dung + mở quyền xem là MỘT hành động
     → GIẢI THÍCH bất đối xứng §3C.5: đó là MỘT quyết định (S7) nhìn hai phía
V3   LƯU: DRAFT · ACTIVE · DEPRECATED
     SUY RA: NEEDS_REVIEW (từ trigger) · SUPERSEDED (từ quan hệ, L4)
     → NEEDS_REVIEW được KÍCH HOẠT, không phải ai đó tự chọn
     → NEEDS_REVIEW KHÔNG rút tri thức khỏi retrieval, chỉ gắn cờ (G4 + S8)
     → SUPERSEDED thắng NEEDS_REVIEW khi cả hai cùng đúng
     → nguyên tắc L4 áp lần thứ BA
V4   N-8 tách HAI ca, đi hai trục khác nhau:
       (a) vẫn ĐÚNG, hết ai gặp  → lifecycle DEPRECATED, verification KHÔNG đổi
       (b) từng đúng, giờ SAI    → verification INVALIDATED → NEEDS_REVIEW
     → ca (a) là BẰNG CHỨNG việc tách hai trục kiếm được chỗ đứng:
       một trục thì buộc gắn INVALIDATED cho phát biểu vẫn đúng → phá G3 + eval
     → chiều "thời gian" mà AP2 gửi tới đây rơi đúng ca (a). Không cần
       biểu diễn thời gian riêng.
V5   Authority = Actor, đã có trong v0.2 §7. KHÔNG cần trục thứ ba.
     → KHÔNG mô hình hóa chức danh/mức chuyên môn. Quyền duyệt đã quyết
       trên cơ sở khác: S7 (người duyệt phải thấy được mọi nguồn).

§3D.6  SỬA lỗi của Step 4: §3C.5 ghi origin = "AI_INFERENCE|HUMAN|SYSTEM_FACT"
       → SAI. v0.2 §7.1 có 5 giá trị; "HUMAN" gộp mất USER_CONFIRMED vs
       HUMAN_ASSESSMENT — đúng ranh giới v0.2 §7.5 dựng riêng để bảo vệ.
       §6.9 lần thứ BA, nhưng lần đầu bị bắt TRƯỚC khi thành quyết định.
§3D.7  BẢNG TỪ VỰNG ĐÃ KHÓA — tham chiếu duy nhất. §6.9 đóng.
§3D.8  Kiểm tra điều kiện dừng §0 bằng quy trình thật → ĐỦ ĐỂ BUILD.

KẾT QUẢ: KHÔNG có entity mới. Ba step cuối (3, 4, 5) gần như chỉ
         GỘP, VÁ và LOẠI BỎ.
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
Q-E  ✅ RESOLVED 2026-08-23 → docs/02_SUCCESS_METRICS_V1.md (M1-M4)
     Cả ba ứng viên ở §3.5 đều bị SỬA VAI TRÒ, không lấy nguyên:
       · "% chỉ đúng nguyên nhân"  → sang bộ EVAL, không phải Success Metric
       · "% escalate oan"          → giữ, nhưng là LAGGING + 3 cảnh báo
       · "độ phủ nguyên nhân"      → leading indicator NỘI BỘ TENANT (G12)
     Thêm mới: TẦNG 0 (nháp Path A được duyệt + mức sửa) và
       TẦNG 1 (chuỗi hiện → mở → chấp nhận → có mặt trong kết luận)
     ⚠ QM-1 vẫn OPEN: ngưỡng cụ thể. Có thước đo chưa có ngưỡng
       thì chưa có điều kiện dừng.
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
N-1  ✅ RESOLVED (V1 + V2 + V3) — hai trục đã khóa, không từ nào trùng.
     VERIFIED bỏ khỏi lifecycle. Bảng khóa: §3D.7. §6.9 ĐÓNG.
N-2  ✅ RESOLVED (T3) — hai type: DIAGNOSTIC + CONDITIONAL_RECOMMENDATION
N-3  ✅ RESOLVED (T1) — đơn vị = một nguyên nhân/cơ chế
N-3b ✅ RESOLVED (AP4) — một "cách nhận ra" = MỘT ASSERTION.
     T4 đã giải sẵn; "log line hay tổ hợp" là NỘI DUNG, không đổi ĐƠN VỊ.
N-6  ✅ RESOLVED (L3) — phát biểu phân biệt nằm TRONG record theo T1.
     Không cần entity/relation mới.
N-7  ✅ RESOLVED (L3) — Case ↔ Knowledge nhiều-nhiều, evidence riêng mỗi link.
     v0.2 đã hỗ trợ sẵn.
N-8  ✅ RESOLVED (V4) — tách HAI ca: (a) vẫn đúng, hết ai gặp → DEPRECATED,
     verification KHÔNG đổi. (b) từng đúng giờ sai → INVALIDATED.
     Ca (a) là bằng chứng việc tách hai trục kiếm được chỗ đứng.
N-4  ✅ RESOLVED (L4) — SUPERSEDES · REFINES · CONTRADICTS,
     state SUPERSEDED là SUY RA từ quan hệ

Sinh ra từ Step 3, chưa chặn build:
L1-a Chọn record nào trong TẬP mà ProcessStep trỏ tới?  → Workstream 06
L2-a Ai ghi nhận mốc USED — hệ thống tự phát hiện hay người xác nhận?
     → gắn với Q-H
N-5  ✅ RESOLVED (AP1 + AP2) — applicability là ASSERTION, không cấu trúc.
     Bốn chiều tách ra: tenant→visibility (S7) · thời gian→lifecycle (Step 5)
     · hệ thống→chưa có ca thật (AP-a) · version→assertion

Sinh ra từ Step 4:
AP-a Chiều "hệ thống nào" — sẽ có ca thật khi bán cho khách thứ hai
AP-b Ca thật nào buộc applicability phải có cấu trúc? → điều kiện xem lại AP1
Auth ✅ RESOLVED (V5) — Authority = Actor, đã có trong v0.2 §7.
     KHÔNG cần trục thứ ba. Không mô hình hóa chức danh/mức chuyên môn.
```

## Housekeeping ngược vào tài liệu cũ

```text
H-1  ✅ ĐÃ LÀM 2026-08-21 — PROJECT_CONTEXT §13.4 đã thêm CONFLICTING,
     kèm ghi chú ladder này là verification level (không phải lifecycle state §8.3)
H-2  ✅ ĐÃ LÀM 2026-08-21 — Case v0.2 §11.2 đã thêm đường
     Evidence → Knowledge trực tiếp (K-B9), giữ nguyên quy tắc Case không
     invalidate Official Knowledge
H-3  ✅ ĐÃ LÀM 2026-08-23 — PROJECT_CONTEXT §5.2 "Human knowledge" đã đánh dấu
     là knowledge SOURCE, không phải KnowledgeRecord (K-B7)
H-4  ✅ ĐÃ LÀM 2026-08-23 — PROJECT_CONTEXT §14.2 SUPERSEDED, ghi root đúng
     của Case v0.2 + lý do loại Intake/TriageState/ReproductionState/
     WaitingState khỏi root (guardrail R7)
H-5  ✅ ĐÃ LÀM 2026-08-23 — PROJECT_CONTEXT §16 SUPERSEDED, ghi 3 capability
     đã lock + bảng so sánh Capability #3 bản cũ vs bản đã chốt (D6/Path A)
H-6  ✅ ĐÃ LÀM 2026-08-23 — archived thành
     docs/archive/NEXT_CONVERSATION_PROMPT_02_INPUT.md, có banner cảnh báo
H-7  ✅ ĐÃ LÀM 2026-08-23 — PROJECT_CONTEXT §5.3 đánh dấu là VÍ DỤ MINH HOẠ
     TỰ NGHĨ, kèm bảng đối chiếu với SOP thật ở §8.1-KQ (tuyến tính, không nhánh)
H-8  ✅ ĐÃ LÀM 2026-08-23 (Step 3) — PROJECT_CONTEXT §13.6 ghi
     "Case → invalidates KnowledgeRecord", TRÁI Case v0.2 §11.2
     ("Case không có authority trực tiếp INVALIDATE Official Knowledge").
     v0.2 thắng. Làm NGAY vì là contradiction thật, cùng loại H-1/H-2.
     Xem §3B.0.
H-9  ✅ ĐÃ LÀM 2026-08-23 (Step 5) — PROJECT_CONTEXT §8.3 bỏ VERIFIED
     khỏi trục lifecycle (V1). Đây là §6.9 gốc, giờ đóng.
     Kèm ghi chú: khi viết file 04, §1.4 đã lặng lẽ bỏ VERIFIED khỏi
     danh sách đó mà không ai ghi lại — H-9 chính thức hoá.
     Trỏ về §3D.7 làm bảng từ vựng DUY NHẤT.
```

> ✅ **H-1..H-9 đã đóng hết.** Không rewrite history ở chỗ nào: mọi mục giữ nguyên
> văn cũ + banner nêu rõ cái gì sai, vì sao, và nguồn đúng ở đâu.

> ✅ **H-1..H-7 đã đóng hết** (2026-08-21 và 2026-08-23). Không rewrite history:
> mọi chỗ giữ nội dung cũ + thêm banner nêu rõ cái gì sai và nguồn đúng ở đâu.
> Chi tiết: `00_CURRENT_STATE.md` §9.
>
> ⚠️ Còn một mục chưa có số: **tên file không khớp convention `01_`/`02_`/`03_`**
> (AGENT.md §1). Kéo theo sửa tham chiếu nhiều file → cân nhắc gộp vào lúc tạo
> `05_PROCESS_MODEL_V0.1.md`.

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

### Cập nhật 2026-08-22 — `§8.2` có luật quyết định chốt trước, và KHÔNG chặn Step 3

`§8.2` (`00_CURRENT_STATE.md`) đã chuyển sang **phiên bản nhẹ**: chỉ trả lời câu nhị phân *"tập nguyên nhân hữu hạn nhỏ hay mở?"*, kèm luật quyết định **chốt trước khi đếm** để phép đếm không thành một vòng modeling nữa:

```text
≤ 15 nhóm, có lặp        →  §3.5 ĐỨNG. Không dựng vector DB / RAG.
≥ 40 nhóm, ít lặp        →  §3.5 SẬP. R-K4 thành hiện thực, đánh dấu lại §3.5.
16-39 / không kết luận   →  §3.5 giữ nhãn n=1, quyết ở Workstream 06.
```

⚠️ **`§8.2` không chặn Step 3** — theo quy tắc `R-K3` (*câu hỏi nào không chặn build thì vào §5 và đi tiếp*). Nó chạy **song song**. Lý do vẫn nên chạy: nó quyết định trước một thứ đắt ở Workstream 06 (có dựng vector DB/RAG không), và chọn sai hướng đó tạo ra đúng thứ `D5` gọi là giàn giáo tạm.

Xem `00_CURRENT_STATE.md` §2.4 và §8.2. Lưu ý phân biệt kèm theo ở §2.4: `§8.2` đo **cấu trúc của vấn đề**, không đo **tỉ trọng SOP của công ty** — tỉ trọng là `G12`, một trục khác.

## R-K3 — Chết vì modeling
Xem §0. Failure mode của dự án không phải *"làm sai thứ"* mà là *"không bao giờ làm ra thứ gì"*. Bằng chứng đã hiện hữu: tài liệu 02 mất mà không ai phát hiện → **tốc độ sản xuất tài liệu đã vượt tốc độ sử dụng tài liệu.**

→ Quy tắc tự áp: câu hỏi nào không chặn build thì vào §5 và đi tiếp.

---

# 7. Step tiếp theo

```text
Step 1  Define Knowledge Boundary            ✅ CONFIRMED 2026-08-21  → §1
Step 2  Knowledge Concepts & Granularity     ✅ CONFIRMED 2026-08-21  → §3
Step 3  Knowledge ↔ Case ↔ Process           ✅ CONFIRMED 2026-08-23  → §3B
Step 4  Applicability & Provenance           ✅ CONFIRMED 2026-08-23  → §3C
Step 5  Lifecycle & Verification vocabulary  ✅ CONFIRMED 2026-08-23  → §3D
```

# ✅ WORKSTREAM 04 ĐÓNG — Knowledge Model v0.1

**23 quyết định `CONFIRMED`:** `S1`-`S8` · `K-B9` · `Q-B` · `Q-C` · `T1`-`T4` · `L1`-`L4` · `AP1`-`AP4` · `V1`-`V5`

Điều kiện dừng ở §0 đã được kiểm bằng quy trình thật — xem **§3D.8**. Đủ để build first use case.

```text
§1     Boundary            Knowledge là gì / không phải gì + 9 boundary claim
§3     Concepts            đơn vị = MỘT NGUYÊN NHÂN, 2 type, verification/assertion
§3B    Relationships       Knowledge ↔ Case ↔ Process
§3C    Applicability       + Provenance per assertion
§3D    Vocabulary          hai trục đã khóa, §6.9 đóng
§3C.5  ★ HÌNH DẠNG ĐẦY ĐỦ của một KnowledgeRecord — đọc chỗ này trước
§3D.7  ★ BẢNG TỪ VỰNG ĐÃ KHÓA — tham chiếu duy nhất
```

**Ba step cuối không sinh entity mới nào.** Chúng gộp, vá và loại bỏ. Đó là dấu hiệu model đã hội tụ, không phải dấu hiệu làm ít.

## Bước tiếp theo: `05 — Process Model v0.1`

Mang sang bốn thứ:

```text
1  KERNEL dùng chung (S4)      Origin · Actor · Source · Evidence · Time ·
                               Verification · Applicability · Visibility
                               → ProcessDefinition dùng CHUNG, không định
                                 nghĩa lại. Bảng khóa: §3D.7.
2  L1                          ProcessStep CONSULTS một TẬP Knowledge theo
                               chủ đề → Process Model phải đỡ được liên kết này
3  Danh sách bước có MỘT NHÀ   là Process domain (S4). Knowledge không giữ
                               bản sao thứ hai.
4  SOP thật TUYẾN TÍNH         §8.1-KQ: B1→B5, KHÔNG nhánh. Đừng thiết kế
                               cho decision tree mà thực tế không có (H-7).
```

⚠️ **Workstream 05 có thể bắt 04 sửa lại** — kernel là của chung, đó là hành vi bình thường, không phải thất bại của Step 1. Đã ghi ở quyết định thứ tự workstream 2026-08-21.

⚠️ **Kỷ luật grep là bắt buộc từ giờ.** §6.9 đã tái phát **ba lần** trong workstream 04 (`H-1`, `L2`, và lỗi Origin của `AP3` — xem §3D.6). Lần thứ ba bị bắt trước khi thành quyết định, chỉ vì Step 5 grep trước khi chốt. **Trước khi khóa bất kỳ vocabulary nào ở Workstream 05, grep toàn bộ tài liệu.**

> 📌 **Step 3 không sinh entity mới** — chỉ vá một chiều còn thiếu (`L1`), gộp ba
> từ vựng trùng (`L2`), chốt cardinality (`L3`), và thêm quan hệ Knowledge ↔
> Knowledge (`L4`). Phần lớn quan hệ cross-domain đã có sẵn ở Case v0.2 §11 —
> xem §3B.5. Đây là lần thứ ba khoản đầu tư vào Case v0.2 trả cổ tức.

> ✅ **Cổng chặn §8.1 đã mở** — chạy xong 2026-08-21, kết quả ở `00_CURRENT_STATE.md`
> §8.1-KQ. Step 2 được xây trực tiếp trên dữ liệu đó. Xem R-K2 và R-K4.

**Thứ tự workstream** — `CONFIRMED 2026-08-21`: giữ **tuần tự** 04 → 05.
Không tách kernel (§1.4) thành tài liệu riêng, vì thêm một vòng thiết kế là đúng
rủi ro R-K3. Nhưng ghi rõ: kernel là của chung, nên **Workstream 05 có thể bắt
04 sửa lại** — đó là hành vi bình thường, không phải thất bại của Step 1.

Việc nên làm song song, không thuộc thiết kế:

```text
§8.1  ✅ ĐÃ CHẠY 2026-08-21 → 00_CURRENT_STATE §8.1-KQ
§8.2  ĐẾM CASE OTA — bản nhẹ, có luật quyết định chốt trước (2026-08-22).
      Xác nhận/bác bỏ §3.5. KHÔNG chặn Step 3 — chạy song song. Xem R-K4.
§8.3  ✅ ĐÃ LÀM 2026-08-23 → docs/02_SUCCESS_METRICS_V1.md (Q-E, M1-M4)
      ⚠ QM-1 (ngưỡng) vẫn OPEN
§8.4  Bộ eval — Path A sinh nhãn miễn phí, xem D6 hệ quả (2).
      Giờ có định nghĩa rõ: 02_SUCCESS_METRICS_V1.md §3, tách khỏi
      Success Metric theo M1.
```

> 📌 **Đầu vào MỚI cho Step 3** — `docs/02_SUCCESS_METRICS_V1.md` §4 đã viết ra
> **metric đòi dữ liệu gì**, gồm cả `AssistanceAttempt` phải ghi 4 mốc riêng biệt
> (không phải một cờ *"đã dùng"*) và phải liên kết tới **kết luận cuối** của case.
> Đó là lý do `Q-E` được chốt trước phần `AssistanceAttempt` của Step 3.
> Nhãn ở §4 đó là `PROPOSED` — Step 3 mới chốt hình dạng thật.

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
