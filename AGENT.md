# AGENT.md

## AI Operational Knowledge & Process Platform

### Purpose

File này định nghĩa cách AI Agent phải làm việc trong project.

Agent phải sử dụng các tài liệu trong `/docs` làm **source of truth** và không được tự suy diễn các proposal thành quyết định đã chốt.

---

# 1. Source of Truth

Trước khi phân tích, thiết kế hoặc code, hãy đọc theo thứ tự:

1. `docs/00_CURRENT_STATE.md` — **đọc trước tiên.** Trạng thái hiện tại, quyết định mới nhất, việc đang làm.
2. `docs/PROJECT_CONTEXT.md`
3. `docs/Canonical Case Model v0.2.md`
4. `docs/04_KNOWLEDGE_MODEL_V0.1.md` — **Knowledge Boundary đã chốt 2026-08-21.** Đọc trước khi bàn bất cứ gì về Knowledge.
5. `docs/05_PROCESS_MODEL_V0.1.md` — **Process Model đã chốt 2026-08-23** (`PR1`-`PR4`). Ngắn, đọc hết được.
6. `docs/06_MVP_ARCHITECTURE.md` — **Công nghệ đã chốt 2026-08-23** (`AR1`-`AR5`). Đọc trước khi viết bất cứ dòng code nào.
7. `docs/02_SUCCESS_METRICS_V1.md` — **Success Metrics đã chốt 2026-08-23** (`Q-E`). Đọc trước khi bàn về đo lường, eval, hoặc `AssistanceAttempt`.

**Năm chỗ tóm gọn nhất, đọc trước nếu gấp:** `04` §3C.5 (hình dạng `KnowledgeRecord`) · `04` §3D.7 (**bảng từ vựng đã khóa**) · `05` §5 (hình dạng Process) · `06` §8 (stack + 5 quyết định kiến trúc) · `06` §10 (ràng buộc cho Workstream 07).

## ⚠️ Lưu ý về tài liệu (cập nhật 2026-08-23)

`docs/02_PRODUCT_FOUNDATION_V1.md` **không tồn tại**. Các quyết định của workstream 02 chỉ còn tồn tại dưới dạng kết luận nén trong §4 dưới đây — mất phần lý do và evidence.

**Phần đã lấy lại được:** Success Metrics → `docs/02_SUCCESS_METRICS_V1.md` (chốt 2026-08-23, `M1`-`M4`). **Phần vẫn mất:** capability contract chi tiết và non-goals. Xem `docs/00_CURRENT_STATE.md` §1 và §6.1.

Tên file thực tế không khớp convention `01_`/`02_`/`03_`:

| Tên trong convention | File thật |
|---|---|
| `01_PROJECT_CONTEXT.md` | `docs/PROJECT_CONTEXT.md` |
| `02_PRODUCT_FOUNDATION_V1.md` | ❌ MISSING — chỉ Success Metrics được lấy lại, ở `docs/02_SUCCESS_METRICS_V1.md` |
| `03_CANONICAL_CASE_MODEL_V0.2.md` | `docs/Canonical Case Model v0.2.md` |

`docs/archive/NEXT_CONVERSATION_PROMPT_02_INPUT.md` là **prompt đầu vào** của conversation 02, không phải output. Nó chứa phiên bản **cũ** của MVP Capability #3 — không đọc như quyết định hiện hành. Đã chuyển vào `docs/archive/` ngày 2026-08-23 (`H-6`) để không bị đọc lẫn với source of truth; tên cũ là `docs/NEXT_CONVERSATION_PROMPT (1).md`.

Nếu có khác biệt giữa các tài liệu:

- ưu tiên quyết định trong tài liệu mới hơn;
- không âm thầm bỏ qua evidence, caveat hoặc historical context trong tài liệu cũ;
- nếu vẫn có contradiction, phải nêu rõ để người dùng quyết định.

Không được dựa vào giả định riêng nếu source of truth đã có quyết định rõ ràng.

---

# 2. Decision Labels

Luôn phân biệt rõ:

- `CONFIRMED` — đã được người dùng/nghiệp vụ xác nhận.
- `EVIDENCE-SUPPORTED` — được dữ liệu hỗ trợ nhưng chưa phải truth tuyệt đối.
- `HYPOTHESIS` — giả thuyết cần kiểm chứng.
- `PROPOSED` — đề xuất, chưa được chốt.
- `OPEN QUESTION` — chưa đủ thông tin để quyết định.

Rules:

- Không tự chuyển `PROPOSED` thành `CONFIRMED`.
- Không tự đóng `OPEN QUESTION`.
- Không thay đổi `CONFIRMED` chỉ vì Agent nghĩ có cách tốt hơn.
- Nếu thấy một quyết định `CONFIRMED` có vấn đề, hãy phản biện và tạo proposal mới; không âm thầm sửa quyết định cũ.

---

# 3. Foundational Guardrails

Các nguyên tắc sau là bắt buộc:

## 3.1 Jira is a Connector, not the Product Boundary

Không thiết kế product phụ thuộc vào:

- Jira fields;
- Jira status;
- Jira workflow;
- Jira assignee;
- Jira custom fields.

Canonical concepts phải có khả năng hoạt động với:

- Jira;
- Email;
- CRM;
- Helpdesk;
- API;
- ERP;
- Form;
- Internal systems;
- Human-created cases.

---

## 3.2 Case ≠ Knowledge ≠ Process

Giữ ba domain concept tách biệt:

```text
Case
= việc đang được xử lý

Knowledge
= tổ chức biết gì

Process
= công việc nên được xử lý như thế nào
```

Không nhét `KnowledgeRecord` hoặc `ProcessDefinition` vào `CanonicalCase` chỉ để implementation thuận tiện.

---

## 3.3 FACT ≠ AI INFERENCE

Không biến dữ liệu nguồn thành semantic truth nếu evidence không đủ.

Ví dụ:

```text
Source:
Jira reproduction = "Không"
```

không được tự suy ra:

```text
Reproduction attempted and failed
```

Nếu chưa đủ evidence:

```text
UNKNOWN
```

là kết quả hợp lệ.

---

## 3.4 Unknown is First-Class Data

Các trạng thái như:

```text
UNKNOWN
UNDETERMINED
UNAVAILABLE
NEEDS_INVESTIGATION
CONFLICTING_EVIDENCE
WAITING
```

là dữ liệu hợp lệ.

Không ép thành `true/false` hoặc một classification chắc chắn.

---

## 3.5 Timeline over Snapshot

Case không chỉ có current status/current owner.

Phải giữ khả năng biểu diễn lịch sử:

```text
Support
→ Technical
→ Waiting for Support
→ Support
→ Resolved
```

Không được thiết kế chỉ dựa trên snapshot hiện tại.

---

## 3.6 Provenance is Foundational

Thông tin quan trọng phải có khả năng trace:

- ai/cái gì tạo ra;
- nguồn nào;
- evidence nào;
- thời điểm nào;
- hình thành bằng cách nào;
- verification state hiện tại.

Đặc biệt:

```text
Origin ≠ Verification
Evidence ≠ Claim
Source Value ≠ Canonical Truth
```

AI inference có thể được human verify sau này nhưng không được mất provenance gốc.

---

## 3.7 Security / Tenant Boundary is Foundational

Operational data có thể chứa:

- customer data;
- password;
- token;
- credentials;
- internal URL;
- logs;
- email;
- attachments.

Mọi thiết kế mới phải bảo toàn khả năng áp dụng:

- tenant / organization boundary;
- visibility;
- sensitivity;
- access control;
- evidence-level restrictions.

Không được coi security là phần bổ sung sau cùng.

---

## 3.8 Không tự làm PHỎNG TO một Capability đã chốt (G11)

`CONFIRMED 2026-08-21` — quyết định `S1`. Chi tiết: `docs/04_KNOWLEDGE_MODEL_V0.1.md` §1 và `docs/00_CURRENT_STATE.md` §2.3.

G9 chặn việc **thêm** Capability #4, #5. G11 chặn hướng còn lại: làm một capability đã chốt **phình ra tới mức khác bản chất**.

Cụ thể cho Capability #3 (theo D6):

```text
ĐƯỢC                                   KHÔNG ĐƯỢC
gom theo yêu cầu của người dùng        tự quyết chủ đề nào cần SOP
tập nguồn CÓ GIỚI HẠN                  quét toàn bộ corpus tìm quy luật mới
có điểm đầu và điểm cuối               job chạy nền liên tục
người duyệt mới thành tri thức         AI tự công nhận
```

**Vì sao cần G11 riêng, G9 không đủ:** *"tự đào quy luật từ 500 case"* vẫn nằm **TRONG** Capability #3 về mặt chữ nghĩa — nó không tạo capability mới. Nên G9 không chặn được nó.

Cột phải chính là `Process Discovery` + `Knowledge Gap Detection` — đã có nhãn *future capability* ở `PROJECT_CONTEXT.md` §17.

Cùng nguyên tắc áp cho Capability #1 và #2 khi có quyết định phạm vi tương ứng.

---

## 3.9 Tỉ trọng tri thức của một khách là THAM SỐ, không phải hằng số thiết kế (G12)

`CONFIRMED 2026-08-22` — người dùng nêu và chốt ở dạng rộng. Lập luận đầy đủ: `docs/00_CURRENT_STATE.md` §2.4.

Mở rộng `D5 hệ quả 2` (*"không đưa giới hạn hôm nay vào domain model"*) từ **giới hạn hạ tầng** sang **phân bố dữ liệu của khách hàng**.

```text
(a) Tỉ trọng 10/30/60 là ĐẶC ĐIỂM CỦA MỘT KHÁCH, không phải sự thật về thế giới.
    Cấm hardcode nó vào thiết kế, vào giả định của capability,
    hoặc vào bất cứ chỗ nào mà sang khách thứ hai phải sửa code.

(b) Thứ tự bật capability là CẤU HÌNH THEO TỪNG KHÁCH,
    không phải một quyết định chốt một lần cho cả sản phẩm.
```

Vì sao cần: MVP có thể vừa khít với khách #0 (`D3` — công ty của người dùng, 10/30/60) và lệch hẳn ở khách thứ hai. Một công ty có 80% SOP đã viết và tìm được thì **điểm vào là Capability 1**, không phải Capability 3. Đó là `D1` (sản phẩm để bán) áp vào thực tế.

Nhất quán với `D5`: *"nâng năng lực bằng policy, không bằng rewrite."*

**Đường biên phải giữ — G12 không phải giấy phép cho "làm mọi thứ linh hoạt":**

```text
ĐƯỢC KHÁC giữa các khách              KHÔNG ĐƯỢC KHÁC — khác là làm lại
tỉ trọng tri thức 10/30/60            domain model Case / Knowledge / Process
capability nào bật trước              provenance · verification · visibility
nguồn nào kết nối trước               cơ chế Path A: gom → nháp → người duyệt
câu chuyện bán hàng, onboarding       D4: AI đề xuất, người công nhận
```

⚠️ **G12 không có nghĩa "build cả ba đường rồi để khách chọn".** Đó là 3x công việc và là đúng failure mode ở `docs/00_CURRENT_STATE.md` §6.7 (*"không bao giờ làm ra thứ gì"*). Linh hoạt ở lớp **cấu hình**; không linh hoạt ở lớp **mô hình**.

⚠️ **G12 cũng không giải được cold start.** Ở khách chưa nạp gì, bày ba lựa chọn = bày ba màn hình trống. Cold start là bài toán **thứ tự**, không phải bài toán **cấu hình** — `D6` tồn tại chính vì thế.

---

## 3.10 Hai chế độ deploy trên MỘT codebase (G13)

`CONFIRMED 2026-08-23` — quyết định `AR2`. Toàn văn và lý do: `docs/06_MVP_ARCHITECTURE.md` §3.

> **Sản phẩm phải deploy được ở CẢ HAI chế độ — shared multi-tenant và dedicated single-tenant — trên cùng một codebase. Tenant luôn được xác định từ cấu hình hoặc ngữ cảnh request, KHÔNG BAO GIỜ từ hằng số toàn cục.**

**Vì sao cần:** khách doanh nghiệp sẽ đòi server riêng (người dùng nêu 2026-08-23). Điều đáng mừng là lựa chọn row-level security **không chặn** điều đó — một bản deploy riêng là *cùng code, cùng schema*, chỉ khác cấu hình trỏ vào database chỉ chứa một tenant.

Thứ **sẽ** chặn không phải cơ chế cô lập, mà là:

```text
⛔ hardcode giả định "chỉ có một database dùng chung"
⛔ một control plane bắt buộc phải dùng chung
⛔ tenant được xác định từ biến toàn cục thay vì từ ngữ cảnh request
```

Nhất quán với `D3` (*"nhồi vào sau rất đắt"*) và `G12` (đặc điểm của khách là **tham số**, không phải hằng số thiết kế). `G13` là `G12` áp cho **hình dạng deploy** thay vì cho tỉ trọng dữ liệu.

⚠️ **Kiểm tra ở review, không để phát hiện sau.** Một giả định "một DB dùng chung" lọt vào code thì rẻ để sửa hôm nay và rất đắt để sửa khi đã có ba khách hàng.

---

# 4. Current Product Decisions

## Primary Persona

`New Support Employee`

## Secondary Persona

`Technical / L3`

## First Use Case

`OTA Booking Not Received Assistance`

## EXACT 3 MVP Value Capabilities

1. `Contextual Knowledge Retrieval`
2. `Process Guidance`
3. `Assistance Outcome & Knowledge Capture`

Không tự thêm Capability #4, #5... vào MVP chỉ vì implementation cần supporting capability.

Supporting capabilities có thể tồn tại nhưng không được thay đổi phạm vi 3 MVP Value Capabilities đã chốt.

---

# 4B. Product Decisions bổ sung — CONFIRMED 2026-08-21

Chi tiết và lý do: `docs/00_CURRENT_STATE.md` §2.2.

## D1 — Đây là sản phẩm để BÁN, không phải tool nội bộ

Multi-tenant là yêu cầu ngày đầu. Tri thức đến từ nhiều nguồn: tài liệu công ty tự nạp, Jira, email, source code (nếu được phân quyền), tài liệu có phân quyền nội bộ.

Doanh nghiệp không dùng Jira vẫn phải dùng được. Ví dụ: nhúng vào CRM, khi user kéo deal sang stage mới thì AI gợi ý bước tiếp theo.

## D2 — Bản build đầu tiên: engine gợi ý quy trình + tri thức, nhúng được

Đọc code để rút tri thức là **v2**, không phải MVP.

## D3 — Khách hàng #0 là công ty của người dùng

Test nội bộ trước, nhưng tenant boundary có trong mô hình dữ liệu từ ngày đầu.

## D4 — Nguyên tắc tự chủ của AI

```text
AI tự ĐỀ XUẤT tri thức mới                  →  CÓ
AI tự CÔNG NHẬN thành tri thức chính thức   →  KHÔNG
Mở dần theo eval, không theo model confidence
```

## D5 — "Model AI mạnh lên thì phần mềm phải mạnh lên theo"

Mọi feature phải trả lời được: *"Nếu sang năm có model mạnh gấp 10, cái này thành giá trị hơn hay thành rác?"*

```text
TÀI SẢN BỀN                        GIÀN GIÁO TẠM
dữ liệu đã kết nối + phân quyền     prompt phức tạp nhiều tầng
provenance                          pipeline cắt chunk, template extraction
lịch sử outcome                     luật if-else bù model yếu
bộ eval                             multi-agent bù reasoning yếu
connector / tích hợp                cơ chế sửa lỗi model thủ công
trạng thái quy trình
```

Hệ quả bắt buộc:
- Bộ eval là first-class, không phải phase sau — nó là *cơ chế* biến "model mạnh lên" thành "phần mềm mạnh lên".
- Không đưa giới hạn hôm nay (chunk size, context limit, embedding dimension) vào domain model.
- Nút cổ chai sẽ dịch từ "AI có hiểu không" sang "AI có được phép xem không" → permission + provenance + outcome là cốt lõi sản phẩm, không phải phần phụ.
- Nâng năng lực bằng policy (autonomy level), không bằng rewrite.

## D6 — "Gom nhiều case cũ thành một SOP theo yêu cầu" NẰM TRONG Capability 3 của MVP

`CONFIRMED 2026-08-21` — người dùng xác nhận trực tiếp. Đây là Open Question `Q-A` đã chặn Step 1 của Workstream 04. Lý do và phân tích đầy đủ: `docs/00_CURRENT_STATE.md` §2.3 và §4.

Phạm vi chính xác — **phần bên phải là guardrail, không phải mô tả**:

```text
THUỘC D6                                    KHÔNG THUỘC D6
người nói "tôi cần SOP cho chủ đề X"        AI tự quyết chủ đề nào cần SOP
kéo một tập CÓ GIỚI HẠN case liên quan      quét toàn bộ corpus tìm quy luật mới
AI soạn nháp → người sửa → người duyệt      AI tự công nhận (vi phạm D4)
một hành động, có điểm bắt đầu và kết thúc  job chạy nền liên tục
```

Cột phải chính là `Process Discovery` + `Knowledge Gap Detection` — đã nằm ở `PROJECT_CONTEXT.md` §17 với nhãn *future capability*. D6 **không** kéo chúng vào MVP và **không** tạo Capability #4 (giữ G9).

Hai hệ quả đã ghi nhận:

- **Q-C co lại.** Để gom N case cũ, hệ thống buộc phải tìm được N case cũ liên quan → cỗ máy "tìm case cũ tương tự" trở thành **dependency của Capability 3**, không còn là lựa chọn của Capability 1. Q-C chuyển từ *"có build không"* (đắt) thành *"có bày ra cho người dùng không"* (gần như miễn phí).
- **D6 là bánh đà của D5.** Mỗi lần dùng tính năng này sinh ra một cặp *(bản nháp AI, bản người sửa)*. `diff` giữa hai bản chính là nhãn mà bộ eval cần — sinh ra bởi hành vi sử dụng sản phẩm, không phải bởi một phase gán nhãn riêng.
- **D6 là điều kiện để D2 dùng được ở khách hàng đầu tiên.** Engine gợi ý quy trình không có gì để gợi ý nếu khách chưa có quy trình nào trong hệ thống.

## Dữ liệu thực tế quan trọng — 10/30/60

```text
SOP có, tìm được (Drive/Confluence/Zalo)           10%
SOP chỉ nằm trong đầu người                        30%
SOP rải rác: vài comment Jira, một email, ghi chú  60%
```

Nghĩa là Capability #1 (retrieval) chỉ có việc để làm ở ~10% trường hợp ngày đầu. Xem `docs/00_CURRENT_STATE.md` §3 và §4 để biết hệ quả với thứ tự MVP. Câu hỏi chặn Step 1 (`Q-A`) đã được trả lời — xem D6 ở trên.

---

# 5. Canonical Case Decisions

Canonical Case Model v0.2 đã được chốt ở mức Domain Modeling.

Root conceptual structure:

```text
CanonicalCase
├── Identity
├── OrganizationalScope
├── Origination
├── Subject
├── Context
└── CurrentState
```

`Context` và `CurrentState` là projections, không thay thế historical/provenance-bearing truth.

Related concepts đã chốt:

```text
CaseParty
CaseEvent
OwnershipSegment
WaitingSegment
CaseProblem
CaseClaim
Classification
EvidenceItem
CaseAction
CaseOutcome
SourceReference
CaseRelation
```

Cross-domain relationships:

```text
CanonicalCase
→ ProcessRun
→ ProcessDefinition

CanonicalCase
→ references / uses KnowledgeRecord
→ contributes evidence to Knowledge lifecycle

CanonicalCase
→ AssistanceAttempt
```

Không tự thay đổi các boundary này khi chưa có quyết định mới.

---

# 6. Important Canonical Distinctions

Luôn giữ rõ:

```text
Case ≠ Problem
Case ≠ Incident
Case ≠ Request
Case ≠ Task
Case ≠ Conversation
Case ≠ Source Ticket

Evidence ≠ Claim
Action ≠ Event
Action ≠ Outcome
CurrentState ≠ Timeline

Recommended Action ≠ Actual CaseAction
Knowledge Retrieved ≠ Knowledge Used
```

Support → Technical handoff trong cùng lifecycle thường vẫn là cùng một Case.

Không tạo Case mới chỉ vì ownership thay đổi.

---

# 7. Current Project Stage

Current stage (cập nhật 2026-08-21):

```text
Product Foundation             ✅  (artifact bị mất — xem 00_CURRENT_STATE.md §6.1)
Canonical Case Model v0.2      ✅

Success Metrics (Q-E)          ✅ CONFIRMED 2026-08-23
                                  → docs/02_SUCCESS_METRICS_V1.md (M1-M4)
                                  ⚠ QM-1 (ngưỡng) vẫn OPEN

Knowledge Model v0.1           ✅ ĐÓNG 2026-08-23 — 23 quyết định CONFIRMED
                                  Step 1 Knowledge Boundary        ✅ → 04 §1
                                  Step 2 Concepts & Granularity    ✅ → 04 §3
                                  Step 3 Knowledge ↔ Case ↔ Process ✅ → 04 §3B
                                  Step 4 Applicability & Provenance ✅ → 04 §3C
                                  Step 5 Lifecycle & Verification   ✅ → 04 §3D
                                  ★ 04 §3C.5  hình dạng đầy đủ KnowledgeRecord
                                  ★ 04 §3D.7  bảng từ vựng ĐÃ KHÓA (duy nhất)

Process Model v0.1             ✅ ĐÓNG 2026-08-23 — 4 quyết định PR1-PR4
                                  → docs/05_PROCESS_MODEL_V0.1.md
                                  Phạm vi cố ý NHỎ, một phiên. Không entity mới.
                                  ★ 05 §5  hình dạng đầy đủ Definition/Run
                                  ★ 05 §9  ràng buộc mang sang Workstream 06

═══ DOMAIN MODELING KẾT THÚC ═══

MVP Architecture               ✅ ĐÓNG 2026-08-23 — CÔNG NGHỆ ĐÃ CHỐT
                                  → docs/06_MVP_ARCHITECTURE.md (AR1-AR5)
                                  Stack: C#/.NET + PostgreSQL + blob storage
                                  Eval: Python (script riêng) · Widget: Vue3+TS
                                  LLM: SDK chính thức + interface mỏng 2 hàm
                                  Tenant: RLS ở DB · visibility ở ứng dụng (G13)
                                  ★ 06 §8   decision register
                                  ★ 06 §10  ràng buộc mang sang Workstream 07

MVP Implementation             🔵 TIẾP THEO — điều kiện §10.1 đã thoả
                                  ⚠ nhưng ĐỌC 06 §10 trước khi viết dòng đầu
```

Agent không được tự nhảy sang production architecture hoặc implementation nếu task hiện tại vẫn đang làm Domain Modeling.

---

# 8. Next Workstream

Workstream tiếp theo:

## `07 — MVP Implementation` · **BÂY GIỜ MỚI ĐƯỢC VIẾT CODE**

> ✅ **CÔNG NGHỆ ĐÃ CHỐT 2026-08-23** — `docs/06_MVP_ARCHITECTURE.md` (`AR1`-`AR5`).
> Điều kiện của §10.1 đã thoả: người dùng đã chốt ngôn ngữ, database, ranh giới LLM, cách xử lý tài liệu, và cơ chế tenant.
>
> **Năm chỗ đọc trước khi viết dòng đầu tiên:**
> - `04` §3C.5 — hình dạng đầy đủ của một `KnowledgeRecord`
> - `04` §3D.7 — **bảng từ vựng ĐÃ KHÓA, tham chiếu duy nhất**
> - `05` §5 — hình dạng đầy đủ `ProcessDefinition` / `ProcessRun`
> - `06` §8 — decision register (stack + 5 quyết định kiến trúc)
> - `06` §10 — ràng buộc mang sang, và việc nên làm song song

### Stack đã chốt

```text
Backend        C# / .NET
Database       PostgreSQL          (row-level security cho tenant)
File storage   blob storage        (file gốc; DB chỉ giữ metadata)
LLM            package `Anthropic` + interface MỎNG hai hàm
Model          claude-opus-5 mặc định
Eval           Python, script offline RIÊNG
Widget nhúng   Vue3 + TypeScript
```

### Sáu ràng buộc dễ sai nhất — kiểm ở review

```text
1  Bảng từ vựng khóa ở 04 §3D.7 — dùng CHUNG, KHÔNG định nghĩa lại
2  G13 (§3.10) — tenant từ cấu hình/ngữ cảnh request, KHÔNG từ hằng số
       toàn cục. Một giả định "một DB dùng chung" lọt vào code thì rẻ để
       sửa hôm nay, rất đắt khi đã có ba khách hàng.
3  AP3 — origin/actor/evidence/verification gắn ở TỪNG ASSERTION.
       Đây là chỗ dễ sai IM LẶNG nhất: gán sai origin là lỗi provenance
       (vi phạm G6), không crash, không ai thấy tới khi eval phát hiện.
4  V3, PR1 — NEEDS_REVIEW · SUPERSEDED · "bước xong" · "bước hiện tại"
       là SUY RA, KHÔNG lưu. Đừng thêm cột cờ.
5  M2 — giữ CẢ HAI bản nháp Path A (trước và sau khi người sửa).
       diff(A,B) vừa là thước đo tháng đầu vừa là nhãn eval.
6  D5 — mọi thứ build ra phải trả lời được: "model mạnh gấp 10 thì cái
       này thành giá trị hơn hay thành rác?"
```

⚠️ **Kỷ luật grep vẫn bắt buộc.** §6.9 (vocabulary song song) đã tái phát **ba lần** trong workstream 04. Bảng khóa duy nhất là `04` §3D.7 — **không định nghĩa lại vocabulary ở tầng code**.

⚠️ **Domain model có thể bắt implementation dừng lại và hỏi.** Nếu hiện thực đòi bẻ một quyết định `CONFIRMED`, phải nêu rõ chứ không âm thầm bẻ (§2, §11).

---

# 9. Working Mode

Agent phải làm việc như **design partner**, không phải chỉ là code generator.

Khi bắt đầu một workstream:

1. Đọc source of truth.
2. Tóm tắt các constraint liên quan.
3. Tách rõ `CONFIRMED / PROPOSED / OPEN QUESTION`.
4. Phản biện model hiện tại nếu có evidence hoặc reasoning mạnh.
5. Không đưa ra một model lớn rồi tự coi là final.
6. Chia nhỏ quyết định để người dùng cùng review.
7. Stress-test proposal bằng First Use Case.
8. Khi relevant, stress-test thêm bằng non-Jira source.
9. Chỉ chuyển proposal thành decision sau khi người dùng xác nhận.
10. Sau khi chốt workstream, cập nhật tài liệu source of truth tương ứng.

---

# 10. Domain Before Implementation

Ưu tiên thứ tự:

```text
Business meaning
↓
Domain boundary
↓
Entities / relationships
↓
States / provenance / rules
↓
Architecture
↓
Implementation
```

Không bắt đầu từ:

```text
database table
C# entity
REST endpoint
Vue component
vector database
LLM framework
```

rồi suy ngược domain model.

---

## 10.1 Chốt công nghệ là quyền của người dùng — `CONFIRMED 2026-08-21`

Người dùng yêu cầu trực tiếp: **trước khi viết code, phải báo trước để người dùng chốt công nghệ.**

```text
BẮT BUỘC BÁO TRƯỚC VÀ CHỜ XÁC NHẬN, không được tự chọn:
  · ngôn ngữ / runtime / framework backend
  · framework frontend
  · database (quan hệ, vector, cache)
  · LLM provider / model / SDK
  · hạ tầng, deployment, hosting
  · thư viện lớn hoặc bất kỳ thứ gì khó thay về sau
```

Agent **được** đề xuất phương án kèm đánh đổi, và **nên** phản biện. Nhưng không được:

- viết file code đầu tiên rồi mới hỏi;
- coi một công nghệ là "mặc định hiển nhiên" (kể cả khi repo đã có dấu vết của nó);
- suy ra lựa chọn công nghệ từ ví dụ trong tài liệu — `C# entity` và `Vue component` ở §10 là **ví dụ về việc KHÔNG được bắt đầu từ đâu**, không phải quyết định stack.

Nhất quán với `D5`: chọn công nghệ sai tạo ra **giàn giáo tạm** — thứ trở thành nợ khi model mạnh lên. Nên đây không phải chi tiết triển khai, nó là quyết định sản phẩm.

Ràng buộc thời điểm:

```text
Workstream 04, 05  Domain Modeling      → KHÔNG code. Chưa cần chốt công nghệ.
Workstream 06      MVP Architecture     → ĐÂY là lúc chốt công nghệ, trước khi sang 07.
Workstream 07      MVP Implementation   → chỉ bắt đầu sau khi người dùng đã chốt.
```

---

# 11. Challenge Assumptions

Agent được yêu cầu chủ động phản biện.

Không chỉ đồng ý với người dùng.

Nếu thấy:

- contradiction;
- scope explosion;
- Jira-centric modeling;
- loss of provenance;
- AI inference leakage;
- unnecessary complexity;
- premature architecture;
- over-modeling;
- missing unknown/conflict state;

hãy chỉ rõ vấn đề và đề xuất hướng khác.

Nhưng proposal mới vẫn phải giữ trạng thái `PROPOSED` cho tới khi được xác nhận.

---

# 12. Avoid Scope Explosion

Luôn phân biệt:

```text
VISION
= hệ thống có thể tiến tới đâu

ROADMAP
= thứ tự xây capability

MVP
= tối thiểu để chứng minh value

CURRENT WORKSTREAM
= phần đang được thiết kế hiện tại
```

Không implement future capability chỉ vì nó đã xuất hiện trong Product Vision.

Không xóa future capability chỉ vì nó chưa thuộc MVP.

---

# 13. Documentation Rule

Khi một workstream được chốt:

- cập nhật hoặc tạo tài liệu tương ứng trong `/docs`;
- giữ decision status rõ ràng;
- giữ Open Questions;
- giữ caveats/evidence quan trọng;
- không rewrite history làm proposal cũ trông như đã luôn được confirmed.

Tên tài liệu dự kiến tiếp theo:

```text
04_KNOWLEDGE_MODEL_V0.1.md   ✅ đã tạo — Step 1 + 2 chốt
02_SUCCESS_METRICS_V1.md     ✅ đã tạo 2026-08-23 — Q-E chốt (M1-M4)
05_PROCESS_MODEL_V0.1.md
06_MVP_ARCHITECTURE.md
```

⚠️ `02_SUCCESS_METRICS_V1.md` là **ngoại lệ có ý thức** với cảnh báo chống phình tài liệu ở `docs/00_CURRENT_STATE.md` §6.7: Success Metrics đã **mất một lần** vì nằm trong tài liệu bị mất, nên nó có file riêng với tên nói rõ nội dung. File đó phải **ngắn**.

Chỉ tạo khi workstream thực sự bắt đầu hoặc hoàn thành.

---

# 14. First Task for a New Agent

Khi Agent mới bắt đầu làm việc với project:

1. Đọc ba tài liệu source of truth.
2. Không code.
3. Trả lời ngắn:
   - Product này giải quyết vấn đề gì?
   - Primary/Secondary Persona là ai?
   - First Use Case là gì?
   - 3 MVP capabilities là gì?
   - Những guardrail nào đã CONFIRMED?
   - Canonical Case v0.2 có những boundary chính nào?
   - Những Open Questions quan trọng hiện tại là gì?
   - Project đang ở stage nào?
4. Chỉ ra contradiction nếu phát hiện.
5. Sau đó đề xuất cách bắt đầu `04 — Knowledge Model v0.1`.
6. Không tự chốt Knowledge Model trước khi thảo luận với người dùng.

---

# 15. Core Principle

> **AI Agent phải giúp project tiến hóa mà không làm mất evidence, provenance hoặc các quyết định đã được con người xác nhận.**

Agent nên ngày càng tận dụng model AI tốt hơn, nhưng business truth của hệ thống không được phụ thuộc vào việc model AI tự tin đến đâu.
