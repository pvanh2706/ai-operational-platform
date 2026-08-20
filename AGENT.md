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

## ⚠️ Lưu ý về tài liệu (cập nhật 2026-08-21)

`docs/02_PRODUCT_FOUNDATION_V1.md` **không tồn tại**. Các quyết định của workstream 02 chỉ còn tồn tại dưới dạng kết luận nén trong §4 dưới đây — mất phần lý do, evidence và **Success Metrics**. Xem `docs/00_CURRENT_STATE.md` §1 và §6.1.

Tên file thực tế không khớp convention `01_`/`02_`/`03_`:

| Tên trong convention | File thật |
|---|---|
| `01_PROJECT_CONTEXT.md` | `docs/PROJECT_CONTEXT.md` |
| `02_PRODUCT_FOUNDATION_V1.md` | ❌ MISSING |
| `03_CANONICAL_CASE_MODEL_V0.2.md` | `docs/Canonical Case Model v0.2.md` |

`docs/NEXT_CONVERSATION_PROMPT (1).md` là **prompt đầu vào** của conversation 02, không phải output. Nó chứa phiên bản **cũ** của MVP Capability #3 — không đọc như quyết định hiện hành.

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

## Dữ liệu thực tế quan trọng — 10/30/60

```text
SOP có, tìm được (Drive/Confluence/Zalo)           10%
SOP chỉ nằm trong đầu người                        30%
SOP rải rác: vài comment Jira, một email, ghi chú  60%
```

Nghĩa là Capability #1 (retrieval) chỉ có việc để làm ở ~10% trường hợp ngày đầu. Xem `docs/00_CURRENT_STATE.md` §3 và §4 để biết hệ quả với thứ tự MVP và câu hỏi đang chờ quyết.

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

Knowledge Model v0.1           🔵 ĐANG LÀM
                                  Step 1 (Define Knowledge Boundary) đã chuẩn bị xong,
                                  chờ người dùng review. Nội dung: 00_CURRENT_STATE.md §5.
                                  Còn 1 quyết định đang chặn: §4.

Process Model                  ⚪ AFTER KNOWLEDGE
MVP Architecture               ⚪ LATER
MVP Implementation             ⚪ LATER
```

Agent không được tự nhảy sang production architecture hoặc implementation nếu task hiện tại vẫn đang làm Domain Modeling.

---

# 8. Next Workstream

Workstream tiếp theo:

## `04 — Knowledge Model v0.1`

> **Đang làm Step 1 — Define Knowledge Boundary.**
> Nội dung đã chuẩn bị đầy đủ ở `docs/00_CURRENT_STATE.md` §5 (định nghĩa ứng viên, discriminator test 4 câu, 4 chiều của Knowledge, bảng ranh giới 8 concept, 7 boundary claim, trình tự chạy).
> Còn **1 quyết định đang chặn** — `docs/00_CURRENT_STATE.md` §4.
> Không thiết kế toàn bộ Knowledge Model trong một lượt.

Cần làm rõ:

- Knowledge là gì / không phải gì;
- KnowledgeRecord;
- Knowledge types;
- lifecycle;
- verification;
- provenance;
- applicability;
- version compatibility;
- Knowledge ↔ Evidence;
- Knowledge ↔ Case;
- Knowledge ↔ Process;
- Knowledge Draft → Human Review → Verified Knowledge;
- unknown / conflicting knowledge.

Chưa thiết kế trong bước này:

- database schema;
- vector database;
- embedding strategy;
- RAG framework;
- REST API;
- frontend;
- microservices.

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
04_KNOWLEDGE_MODEL_V0.1.md
05_PROCESS_MODEL_V0.1.md
06_MVP_ARCHITECTURE.md
```

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
