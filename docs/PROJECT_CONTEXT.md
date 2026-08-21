# PROJECT_CONTEXT.md

> **Project working name:** AI Operational Knowledge & Process Platform  
> **Status:** Product Discovery → Product Foundation  
> **Last consolidated:** 2026-08-18  
> **Purpose:** Source of truth để conversation/AI agent mới hiểu đúng project mà không phải đọc lại toàn bộ lịch sử.
>
> **Nhãn sử dụng trong tài liệu**
> - **CONFIRMED** — đã được người dùng/nghiệp vụ xác nhận.
> - **EVIDENCE-SUPPORTED** — được dữ liệu Discovery ủng hộ nhưng vẫn có giới hạn.
> - **HYPOTHESIS** — giả thuyết cần kiểm chứng thêm.
> - **PROPOSED** — hướng thiết kế/roadmap đang đề xuất, chưa phải quyết định cuối.
> - **OPEN QUESTION** — chưa đủ dữ liệu để chốt.

---

# 1. Executive Summary

Project ban đầu xuất phát từ bài toán cụ thể:

> Customer issue được tạo trong Jira → Support/Technical điều tra → xử lý → đóng ticket → tri thức xử lý bị phân tán trong comment, ảnh, log, code, trí nhớ cá nhân hoặc tài liệu khác. Khi vấn đề tương tự xuất hiện, người mới thường không biết tri thức đã tồn tại và lại phải hỏi người cũ hoặc chuyển Technical.

Qua nhiều vòng Discovery với dữ liệu Jira thật, Product Vision đã mở rộng:

> **Nền tảng AI giúp doanh nghiệp tìm đúng tri thức, hiểu đúng quy trình, xác định công việc đang ở bước nào, đề xuất bước tiếp theo, thu hoạch tri thức mới từ hoạt động thực tế, và dần tự động hóa các hành động đủ an toàn.**

Jira không còn được xem là sản phẩm, mà là **connector / operational data source đầu tiên**.

Vision hiện tại hỗ trợ cả doanh nghiệp:
1. Đã có SOP/tài liệu → AI tìm và hướng dẫn đúng lúc.
2. Chưa có tài liệu nhưng có dữ liệu hoạt động → AI phát hiện pattern, draft tri thức/quy trình, người xác nhận.
3. Có cả tài liệu và dữ liệu hoạt động → AI so sánh quy trình chính thức với cách làm thực tế, phát hiện drift/gap.
4. Khi quy trình đủ rõ và action đủ an toàn → AI chuyển từ **Suggest → Prepare → Execute with Approval → Safe Autonomy**.

---

# 2. Product Evolution

## 2.1 Giai đoạn đầu — Jira Support Assistant

### Initial problem
- Customer issue được tạo.
- Support/Developer đọc log, DB, source, Git, release...
- Xử lý xong.
- Jira comment thường chỉ còn “đã xử lý”, “đã báo khách”, “Fixed”.
- Case tương tự xuất hiện → team có thể phải điều tra lại.

### Initial idea
Khi ticket mới xuất hiện, AI:
- tìm Jira tương tự;
- tìm commit/release liên quan;
- gợi ý root cause/resolution;
- giúp Support giảm thời gian điều tra.

Ví dụ:

```text
New Case
↓
Similar to ES-xxxx
↓
Previous root cause / resolution / evidence
↓
Suggested first action
```

Đây vẫn là use case tốt, nhưng Discovery cho thấy nó chỉ là **một phần** của bài toán.

---

## 2.2 Discovery làm lộ bài toán Knowledge Capture

Nhiều historical case **không có đủ tri thức để reuse**.

Vấn đề không chỉ là:

> “Không tìm được case cũ.”

Mà còn là:

> “Kể cả tìm được case cũ, hồ sơ cũng không ghi rõ đã làm gì.”

Distinction quan trọng:

### KNOWLEDGE_WAS_NOT_CAPTURED
Tri thức từng tồn tại trong đầu người xử lý hoặc trong quá trình xử lý, nhưng hồ sơ không lưu đủ.

### REUSE_OPPORTUNITY_MISSED
Tri thức cũ đã đủ dùng, nhưng case sau không có dấu vết tham chiếu/tái sử dụng.

Hai vấn đề cần hai giải pháp khác nhau:

```text
Knowledge chưa được capture
→ Search/RAG không cứu được
→ cần Knowledge Capture

Knowledge đã có nhưng khó tìm
→ cần Retrieval / Recommendation / Process Guidance
```

---

## 2.3 Phát hiện nghiệp vụ mới — Knowledge Already Exists

Case thực tế:

> “Booking Traveloka không về PMS.”

Công ty **đã có tài liệu/quy trình xử lý**, nhưng người mới:
- không biết tài liệu tồn tại;
- không biết tìm ở đâu;
- vẫn phải hỏi người cũ.

Điều này tạo loại vấn đề:

### KNOWLEDGE_NOT_DISCOVERED

```text
Knowledge exists
↓
Employee does not know it exists
↓
Employee asks senior / Technical
↓
Organization pays support cost again
```

Đây là use case MVP tiềm năng mạnh vì:
- dễ chứng minh value;
- ít rủi ro hơn AI tự điều tra source/log/DB;
- áp dụng ngay với SOP hiện có;
- giải quyết onboarding người mới.

---

## 2.4 Từ Knowledge sang Process

Nếu doanh nghiệp cung cấp quy trình:

```text
1. Check A
2. Nếu A đúng → Check B
3. Nếu B sai → Action C
4. Nếu vẫn lỗi → Escalate
```

AI không chỉ trả tài liệu.

AI nên hiểu:

```text
User đang xử lý Case X
↓
Relevant Process = P
↓
Current Step = 2
↓
Evidence của Step 1 đã đủ
↓
Next Best Step = 3
```

Từ đây product chuyển từ:

> Knowledge Search

sang:

> **Knowledge + Process Guidance**

và xa hơn:

> **Knowledge + Process + Action + Learning**

---

# 3. Current Product Vision

## 3.1 Working vision statement

> **Biến kinh nghiệm, dữ liệu hoạt động và quy trình của tổ chức thành tri thức có thể tìm lại, áp dụng, đo lường và dần tự động hóa.**

Một cách diễn đạt khác:

> **Đưa đúng tri thức và đúng bước xử lý đến đúng người, đúng thời điểm; nếu tri thức chưa tồn tại thì giúp tổ chức tạo ra nó từ hoạt động thực tế.**

## 3.2 Working product name

**AI Operational Knowledge & Process Platform**

Previous names:
- AI Resolution Platform
- AI Operational Knowledge & Resolution Platform
- Jira Support Assistant

Tên hiện tại rộng hơn Jira và rộng hơn “resolution”.  
Status: **PROPOSED**

---

# 4. Core Product Loop

```text
          ORGANIZATIONAL DATA
                  │
                  ▼
            1. DISCOVER
      Detect patterns / knowledge /
      real operational processes
                  │
                  ▼
            2. FORMALIZE
      Knowledge + Process + Rules
                  │
                  ▼
              3. GUIDE
      Right knowledge / right step
                  │
                  ▼
             4. EXECUTE
        Human or AI performs action
                  │
                  ▼
             5. OBSERVE
        Capture actual result/evidence
                  │
                  ▼
              6. LEARN
      Improve knowledge/process/evals
                  │
                  └──────────────↺
```

Tên ngắn hơn:

> **Capture → Organize → Retrieve → Apply → Learn**

---

# 5. Core Domain Concepts

## 5.1 CASE — “Việc gì đang cần xử lý?”

Case là một đơn vị công việc/vấn đề/yêu cầu cần xử lý.

Nguồn có thể là:
- Jira
- Helpdesk
- Email
- CRM
- Form
- Teams/Slack
- API
- ERP
- Internal system
- Human-created task

### Architectural principle
**Case không phụ thuộc Jira.**

Jira Issue chỉ là external/source representation của Case.

---

## 5.2 KNOWLEDGE — “Tổ chức biết gì về việc này?”

### Explicit knowledge
- SOP
- Wiki
- PDF
- DOCX
- Troubleshooting guide
- Runbook
- Policy
- FAQ

### Operational knowledge
- Historical cases
- Comments
- Actions
- Resolution trails
- Repeated behavior

### Technical evidence
- Git
- Commit
- PR
- Release
- Logs
- DB
- Monitoring
- API response
- Source code

### Human knowledge
- Senior engineer memory
- Support experience
- Tribal knowledge

### Derived knowledge
AI tổng hợp từ evidence, nhưng phải giữ:
- provenance;
- evidence;
- verification state;
- human review khi cần.

---

## 5.3 PROCESS — “Quy trình chuẩn là gì?”

Process mô tả cách xử lý một loại công việc.

Ví dụ:

```text
Booking OTA not received
↓
Check booking exists
↓
Check room mapping
↓
Check rate mapping
↓
Check incoming log
↓
No log → contact OTA
Has log but processing failure → Technical
```

Process có thể có:
- steps;
- branches;
- conditions;
- exceptions;
- escalation paths;
- approvals;
- required evidence;
- automated actions.

---

## 5.4 PROCESS STATE — “Người dùng đang ở bước nào?”

AI không nên chỉ đoán current step.

Nguồn xác định Process State:

### A. SYSTEM FACT
Hệ thống quan sát được:
- API call completed;
- DB check completed;
- status changed;
- action result exists.

### B. USER CONFIRMATION
Người dùng xác nhận:
> “Đã kiểm tra mapping, mapping đúng.”

### C. AI INFERENCE
AI suy luận từ hội thoại/evidence.

AI inference phải được đánh dấu rõ.

Ví dụ:

```text
Process: OTA Booking Troubleshooting

Step 1  ✅ Booking exists          [SYSTEM/USER FACT]
Step 2  ✅ Room mapping checked    [USER CONFIRMED]
Step 3  ⏳ Rate mapping            [CURRENT]
Step 4  ○ Incoming log
Step 5  ○ Vendor / Technical
```

---

## 5.5 ACTION — “Bước tiếp theo có thể làm gì?”

Action có thể là:
- hướng dẫn;
- hỏi thêm thông tin;
- mở tài liệu;
- gọi API;
- chạy check;
- tạo ticket;
- update field;
- chuẩn bị script;
- thay đổi cấu hình;
- thực hiện action thật.

Action nên có:
- risk;
- reversibility;
- required permission;
- human approval requirement;
- evidence/result.

---

## 5.6 EVIDENCE — “Dựa vào đâu?”

Các loại evidence:
- Jira field
- Comment
- Attachment
- Screenshot
- Phone call
- Log
- DB result
- Source code
- Git commit
- Document
- API response
- User statement

Evidence cần metadata:
- source;
- timestamp;
- visibility;
- sensitivity;
- tenant boundary;
- machine readability;
- actor;
- ingestion time.

---

## 5.7 EVALUATION — “AI có thực sự giúp không?”

Hệ thống cần đo:
- suggestion accepted/rejected;
- resolution success;
- reused knowledge;
- time saved;
- human correction;
- false recommendation;
- process completion;
- automation success/failure.

---

## 5.8 AUTONOMY — “AI được phép làm đến đâu?”

```text
Level 1 — GUIDE
AI hướng dẫn.

Level 2 — SUGGEST
AI đề xuất next action.

Level 3 — PREPARE
AI chuẩn bị action/script/request.

Level 4 — EXECUTE WITH APPROVAL
Human approve → AI execute.

Level 5 — SAFE AUTONOMY
AI tự làm action low-risk, bounded, reversible,
có evidence và được Eval chứng minh.
```

Autonomy không dựa đơn thuần vào model confidence.

---

# 6. Knowledge Lifecycle

## 6.1 Doanh nghiệp đã có tài liệu

```text
Docs / SOP / Wiki
↓
Ingest
↓
Structure & index
↓
Connect to Problem/Process
↓
Incoming Case
↓
Retrieve relevant knowledge
↓
Guide user
↓
Observe result
```

Use case điển hình:
> Người mới không biết SOP tồn tại.

## 6.2 Không có tài liệu nhưng có operational data

```text
Jira / CRM / ERP / Email / Logs
↓
Pattern Discovery
↓
Repeated actions / decisions
↓
AI drafts Knowledge / Process
↓
Human validates
↓
Official Knowledge
```

AI không được tự biến pattern thành official truth.

Required progression:

```text
OBSERVED PATTERN
→ AI DRAFT
→ HUMAN REVIEW
→ VERIFIED KNOWLEDGE
```

## 6.3 Tri thức mới phát sinh trong lúc xử lý

Điểm capture tự nhiên:
- trước khi Resolve;
- sau Technical Handback;
- sau action quan trọng;
- sau khi xác định nguyên nhân/hướng xử lý.

Thay vì bắt người dùng viết tài liệu, AI có thể hỏi:

> “Bạn đã làm gì để xử lý case này?”

Sau đó AI draft:
- problem;
- key evidence;
- action;
- result;
- applicability;
- unresolved unknowns.

---

# 7. Process Intelligence Capabilities

## 7.1 Process Guidance
AI biết relevant process và user đang ở bước nào.

Output:
- current step;
- completed steps;
- missing evidence;
- next best action;
- branch conditions;
- escalation conditions.

Status: **CORE FUTURE DIRECTION**

## 7.2 Process Discovery
AI phân tích operational data và phát hiện process thực tế.

Ví dụ:

```text
80% case dạng X:
Support
→ Request Booking ID
→ Check Mapping
→ Escalate Technical
→ Check Log
→ Return Support
→ Resolve
```

AI đề xuất formalize thành SOP.

Status: **HIGH-VALUE FUTURE CAPABILITY**

## 7.3 Process Drift Detection
So sánh:

```text
Official SOP
vs
Actual operational behavior
```

Ví dụ:
- SOP nói 1 → 2 → 3.
- 72% case thực tế làm 1 → 2 → X → 3.
- AI cảnh báo SOP có thể thiếu X.

Status: **HIGH-VALUE FUTURE CAPABILITY**

## 7.4 Exception Learning
Capture:
- exceptions;
- known failure modes;
- special conditions;
- branch-specific resolution.

---

# 8. Knowledge Intelligence Capabilities

## 8.1 Knowledge Retrieval
Đưa đúng tri thức khi Case xuất hiện.

User không cần biết:
- keyword;
- document title;
- folder;
- SOP exists.

## 8.2 Knowledge Gap Detection
AI chủ động tìm:
- vấn đề lặp lại;
- escalation lặp lại;
- không có tài liệu;
- resolution success nhưng knowledge thiếu.

Priority có thể dựa trên:

```text
Frequency
× Handling Cost
× Escalation Cost
× Business Impact
× Reusability Opportunity
```

## 8.3 Knowledge Health

Lifecycle:

```text
DRAFT
VERIFIED
ACTIVE
NEEDS_REVIEW
DEPRECATED
SUPERSEDED
```

## 8.4 Decision Knowledge

Capture không chỉ “làm gì” mà cả “tại sao”.

```text
Condition
Decision
Reason
Alternative
Risk
Evidence
Outcome
```

---

# 9. Proactive Assistance

AI không nên chờ user hỏi:

> “Có tài liệu nào không?”

Khi Case xuất hiện:

```text
New Case
↓
AI understands issue
↓
AI finds:
- Knowledge
- Process
- Similar Case
- Known Exception
↓
AI surfaces assistance automatically
```

---

# 10. Discovery History

## 10.1 Round 1 — 100 newest Jira cases

Goal:
Stress-test Canonical Case Draft 1 bằng dữ liệu thật.

Broad findings:
- Jira capture routing/handoff nhiều hơn technical investigation.
- Timeline rất quan trọng.
- Ownership không đơn giản là một person.
- Attachments/images lớn.
- Investigation/root cause thường thiếu.
- Case không đồng nghĩa Bug.
- Resolution có thể là USER_GUIDANCE, CONFIG_CHANGE, WORKAROUND, DATA_FIX, CODE_FIX, UPGRADE, EXTERNAL_SYSTEM_ACTION, UNKNOWN.

Result:
Draft 1 không đủ.

## 10.2 Round 2 — 100 cases với custom fields đầy đủ

### Critical finding
`Version đang sử dụng`:
- field được export;
- rỗng 100/100.

Meaning:

> **Thêm field không tạo ra knowledge.**

Other findings:
- team account ownership phổ biến;
- triage nhiều case “Không chắc chắn lắm”;
- reproduction field không thể diễn giải quá mạnh;
- lifecycle cần event history;
- evidence visibility/sensitivity cần model;
- snapshot không đủ.

Architectural impact:
- CaseEvent
- OwnershipSegment
- Classification provenance
- WaitingState
- TriageState
- Reproducibility
- Evidence metadata
- ResolutionAction

## 10.3 Business rules CONFIRMED by human

### Rule 1
`Đây là lỗi phần mềm? = Không chắc chắn lắm`

Support **chủ động chọn**.

Interpretation:
`UNDETERMINED` là triage state thật.

### Rule 2
`Bạn tái lập lại được lỗi? = Không`

Nhiều trường hợp chưa thử vẫn chọn `Không`.

Do đó:

```text
SourceAnswer = "Không"
```

không được suy ra:

```text
ATTEMPTED_AND_FAILED
CANNOT_VERIFY
```

Nếu không có evidence khác:

```text
ActualReproductionState = UNKNOWN
```

### Rule 3
`Escalated to L3`

chỉ nghĩa:
> Case đang nằm trong hàng đợi Technical.

Semantic event:

```text
ENTERED_TECHNICAL_QUEUE
```

### Rule 4

```text
Escalated to L3
→ Waiting for support
```

thường nghĩa:
> Technical đã xử lý phần của mình và trả Support kiểm tra/tiếp tục.

Semantic event:

```text
TECHNICAL_HANDBACK
```

### Rule 5
Chỉ `Resolved` mới coi Case đã xử lý xong.

## 10.4 Round 3 — 500 historical Fixed cases

### Dataset warning
Expectation:
> 3–12 months history.

Reality:
- 500 cases;
- all Fixed;
- resolution dates chỉ tập trung khoảng 25.5 ngày;
- khoảng 3 tháng trước thời điểm Discovery.

Therefore:
Round 3 **không thể chứng minh knowledge bị quên sau 6–12 tháng**.

## 10.5 Round 3 — Knowledge quality

Heuristic:

```text
REUSABLE             179  (35.8%)
PARTIALLY_REUSABLE    92  (18.4%)
NOT_REUSABLE         227  (45.4%)
NOT_APPLICABLE         2  (0.4%)
```

Important:
Các nhãn này là **INFERENCE**, không phải human-labeled ground truth.

Key gaps:
- action steps thiếu ở 306/500;
- real resolution actor unavailable;
- reproduction actual state unknown nhiều case;
- root cause text thường thiếu;
- resolution action type thường không suy ra được;
- attachments phổ biến nhưng cố tình không OCR/download.

## 10.6 Round 3 — Repeated problems

Tool produced:
- 7 ExactLink pairs;
- 25 StrongCandidate pairs;
- 633 WeakCandidate pairs;
- 21 cohesive groups;
- 49/500 cases trong cohesive groups;
- 14 groups cross-organization.

Caveat:
Ngay cả `Cloners` không đảm bảo technical recurrence.

## 10.7 Round 3 — Reinvestigation

Trong 32 high-confidence candidate pairs:

```text
REUSE_OPPORTUNITY_MISSED       2
KNOWLEDGE_WAS_NOT_CAPTURED     3
CONCURRENT_DUPLICATE_EFFORT    3
NOT_FLAGGED                   24
```

### REUSE_OPPORTUNITY_MISSED
Earlier case có usable knowledge, later case không có reference rõ.

Potential solution:
> retrieval/discovery.

### KNOWLEDGE_WAS_NOT_CAPTURED
Earlier case thiếu actionable detail.

Potential solution:
> capture/formalization.

### CONCURRENT_DUPLICATE_EFFORT
Cases overlap in time.

Không phải evidence của “forgotten old knowledge”.

## 10.8 Round 3 — Knowledge reuse evidence

```text
ExplicitReuse      117
PossibleReuse        9
NoReuseEvidence    374
```

`NO_REUSE_EVIDENCE` không có nghĩa user không reuse.

Dataset hỗ trợ kết luận:
> **Knowledge reuse không consistently observable/measurable từ Jira records.**

## 10.9 Original business hypothesis

Original hypothesis:
> Organization solved a problem before, later forgot it, and reinvestigated.

Current verdict:
### `PARTIALLY_SUPPORTED`

Supported:
- knowledge thường không được capture reusable;
- reference/reuse khó quan sát;
- có repeated problem candidates;
- có một số possible re-investigation.

Not yet supported:
- forgot after 6–12 months;
- re-investigation widespread;
- lack of reference = human forgot;
- repeated checking = wasted work.

---

# 11. Critical New Insight

Discovery chỉ nhìn Jira.

Nhưng company knowledge có thể ở nơi khác.

Therefore:

> **“Jira không chứa knowledge” ≠ “Công ty không có knowledge.”**

Knowledge có thể ở:
- SOP docs;
- wiki;
- internal drive;
- code;
- release notes;
- chat;
- phone;
- personal memory.

---

# 12. Problem Taxonomy

## P1 — KNOWLEDGE_NOT_DISCOVERED
Knowledge exists, user không biết.

Solution:
- contextual retrieval;
- proactive recommendation;
- process guidance.

## P2 — KNOWLEDGE_NOT_CAPTURED
Knowledge phát sinh nhưng không lưu reusable.

Solution:
- capture at natural moments;
- AI-drafted documentation;
- minimal human confirmation.

## P3 — KNOWLEDGE_FRAGMENTED
Knowledge nằm rải rác nhiều nguồn.

Solution:
- semantic relations;
- provenance;
- source linking.

## P4 — PROCESS_NOT_FORMALIZED
People làm theo pattern lặp lại nhưng không có SOP.

Solution:
- Process Discovery;
- AI draft;
- human validation.

## P5 — PROCESS_NOT_DISCOVERED
Official process có nhưng employee không biết.

Solution:
- contextual process matching;
- proactive guide.

## P6 — PROCESS_STATE_UNKNOWN
AI biết process nhưng không biết user đang ở đâu.

Solution:
- system evidence;
- user confirmation;
- AI inference with provenance.

## P7 — PROCESS_DRIFT
Official SOP khác actual behavior.

Solution:
- drift detection;
- knowledge health;
- review workflow.

## P8 — REUSE_NOT_MEASURABLE
Organization không đo được knowledge nào đã giúp case nào.

Solution:
- AssistanceAttempt;
- knowledge used;
- accept/reject;
- outcome;
- time saved.

## P9 — SAFE_AUTOMATION_OPPORTUNITY
Một số process stable, repeatable, low-risk.

Solution:
- capability execution;
- approval;
- autonomy.

---

# 13. Current Architecture Principles

## 13.1 Source-agnostic

Không model:

```text
Product = Jira Assistant
```

Model:

```text
External Source
→ Canonical Case / Evidence / Knowledge / Process
```

Jira = Connector #1.

## 13.2 Source facts khác AI-derived claims

Ví dụ:

```text
FACT:
Jira reproduction = "Không"

Không được tự đổi thành:
"Attempted and failed"

DERIVED:
ActualReproductionState = UNKNOWN
```

## 13.3 Unknown là first-class data

Ví dụ:

```text
NEEDS_INVESTIGATION
WAITING_CUSTOMER
WAITING_INTERNAL
UNAVAILABLE
UNDETERMINED
UNKNOWN
```

## 13.4 Không dùng numeric LLM confidence như truth

Preferred evidence ladder:

```text
SPECULATIVE
PLAUSIBLE
SUPPORTED
VERIFIED
CONFLICTING      ← bổ sung 2026-08-21, xem ghi chú dưới
INVALIDATED
```

> **Cập nhật 2026-08-21 (H-1) — `CONFIRMED`.**
> `CONFLICTING` bị thiếu ở bản gốc của §13.4 nhưng **đã có** trong
> `Canonical Case Model v0.2` §7.3. Hai vocabulary lệch nhau là một
> contradiction thật, và quyết định `S8` của Knowledge Model v0.1 làm
> `CONFLICTING` trở thành **bắt buộc**, không còn là tùy chọn:
> một bản nháp gom từ N case luôn có chỗ các case không đồng ý với nhau
> (ví dụ *"6/20 case gọi OTA trước khi check log, 8/20 làm ngược lại"*),
> và chính chỗ đó là thứ người duyệt cần nhìn.
>
> Xem `docs/04_KNOWLEDGE_MODEL_V0.1.md` §1.11.
>
> ⚠️ Ladder này là **verification level** (mức tin). Nó **không** phải
> lifecycle state ở §8.3 (`DRAFT / ACTIVE / NEEDS_REVIEW / DEPRECATED /
> SUPERSEDED`). Hai trục khác nhau — xem `04` §1.4 quy tắc (2).
> Vocabulary chính xác của cả hai trục sẽ được khóa ở Knowledge Model Step 5.

## 13.5 Timeline over snapshot

Need temporal entities:
- CaseEvent
- OwnershipSegment
- WaitingState
- Classification changes
- ResolutionAction
- Reopen / invalidation

## 13.6 Knowledge != Case

```text
Case
→ produces / validates / invalidates
KnowledgeRecord

KnowledgeRecord
→ assists
Future Case
```

## 13.7 Process != Case

Possible relationship:

```text
ProcessDefinition
↓
ProcessRun
↓
Case
```

Chưa final.

## 13.8 Human review theo expected value
Review dựa vào:
- recurrence;
- risk;
- impact;
- uncertainty;
- reuse value;
- automation risk.

## 13.9 Security is foundational

Operational data có thể chứa:
- passwords;
- tokens;
- remote credentials;
- email;
- phone;
- internal URLs;
- customer info.

Required concept:

```text
Raw Data
→ Sensitive Data Detection
→ Redaction / Access Control
→ AI Processing
```

---

# 14. Canonical Case Model — Current Direction

## 14.1 Draft 1 không còn đủ

Old:

```text
Case
├── Identity
├── Problem
├── Context
├── Ownership
├── Evidence
├── Investigation
├── Resolution
└── Outcome
```

Problems:
- snapshot-oriented;
- ownership temporal;
- investigation often unavailable;
- one Case may contain multiple Problems;
- several Cases may map one Problem/Incident;
- classifications can disagree;
- outcome overlaps resolution;
- evidence has independent sensitivity/lifecycle.

## 14.2 v0.2 direction

Status: **PROPOSED — NOT FINALIZED**

Potential root:

```text
CanonicalCase
├── Identity
├── CurrentState
├── Intake
├── Context
├── TriageState
├── ReproductionState
├── WaitingState
├── SourceReferences
└── Relations
```

Related entities:

```text
CaseEvent[]
Problem[]
Classification[]
EvidenceItem[]
OwnershipSegment[]
ResolutionAction[]
```

Outside core Case:

```text
CaseAssessment
KnowledgeAssessment
KnowledgeRecord
ProcessDefinition
ProcessRun
AssistanceAttempt
```

---

# 15. Current Capability Map

## Layer A — Intake & Understanding
- Multi-source Case Intake
- Case Normalization
- Case Understanding

## Layer B — Knowledge
- Document Ingestion
- Knowledge Retrieval
- Historical Case Retrieval
- Knowledge Capture
- Knowledge Synthesis
- Knowledge Gap Detection
- Knowledge Health
- Decision Knowledge

## Layer C — Process
- Process Definition
- Process Matching
- Process State Tracking
- Next Best Action
- Process Discovery
- Process Drift Detection
- Exception Learning

## Layer D — Assistance & Action
- Proactive Assistance
- Guided Resolution
- Action Preparation
- Human Approval
- Tool/Capability Execution

## Layer E — Eval & Learning
- Assistance Feedback
- Outcome Evaluation
- Knowledge Reuse Measurement
- Process Effectiveness
- Knowledge/Process Improvement

## Layer F — Autonomy
- Risk Classification
- Approval Policy
- Reversibility
- Safe Auto-execution
- Autonomy Expansion by Eval

---

# 16. MVP — Current Status

User muốn:

> **MVP nhỏ, khoảng 3 core features, nhưng Product Vision phải giữ đầy đủ future capabilities.**

Important:
**Exact 3 MVP capabilities chưa được formally locked.**

## Current leading MVP candidate set — PROPOSED

### Candidate 1 — Contextual Knowledge Retrieval
When a Case appears:
- understand problem;
- find existing SOP/knowledge;
- show right knowledge proactively.

Proof:
> New employee không cần biết document name/location.

### Candidate 2 — Process Guidance
For selected SOP/process:
- determine current step từ evidence/user confirmation;
- show completed steps;
- recommend next step;
- escalate đúng lúc.

Proof:
> New employee xử lý đúng process với ít senior help hơn.

### Candidate 3 — Knowledge/Process Draft from Operational Data
When documentation missing:
- analyze Jira/operational history;
- detect repeated pattern;
- draft Knowledge/Process;
- require human validation.

Proof:
> Company bootstrap knowledge dù documentation poor.

Together:

```text
Knowledge exists
→ Retrieve it

Process exists
→ Guide through it

Neither exists but data exists
→ Draft it
```

Status vẫn **PROPOSED**.

---

# 17. Future High-Value Capabilities

Không được bỏ chỉ vì ngoài MVP:

1. Process Discovery
2. Knowledge Gap Detection
3. Process Drift Detection
4. Knowledge Capture at Resolve
5. Historical Case Matching
6. Exception Learning
7. Knowledge Health
8. Decision Knowledge
9. Proactive Assistance
10. Knowledge Reuse Analytics
11. Human/AI Action Execution
12. Eval-driven Autonomy
13. Cross-system Case Correlation
14. Incident/Problem clustering
15. Root Cause Evidence Graph
16. Knowledge applicability/version compatibility
17. Release/code/log correlation
18. Organizational expertise mapping
19. Onboarding mode
20. Process bottleneck analytics

---

# 18. Example End-to-End — Booking Traveloka not received

## A — SOP exists

```text
Customer: "Booking Traveloka không về PMS"
↓
AI reads Case
↓
AI matches Process
↓
AI finds official SOP
↓
AI asks missing context
↓
AI determines current process step
↓
AI guides:
1. Booking exists?
2. Room mapping?
3. Rate mapping?
4. Incoming log?
5. Vendor or Technical?
↓
User confirms results
↓
AI tracks Process State
↓
Case resolved
↓
AI records:
- path taken
- knowledge used
- outcome
```

## B — No SOP, historical data exists

```text
Many similar Jira cases
↓
AI detects repeated operational pattern
↓
AI drafts troubleshooting process
↓
Human reviews
↓
Verified ProcessDefinition
↓
Future Cases use it
```

## C — SOP exists but behavior changes

```text
Official SOP
≠
Recent successful Cases
↓
AI detects Process Drift
↓
Suggest review
↓
Human updates SOP
```

## D — Stable, low-risk action

```text
"Check mapping via API"
↓
Initially: AI guides human
↓
Later: AI calls read-only API
↓
Eval proves safe
↓
Autonomy expands
```

---

# 19. Product Differentiation

Avoid becoming only:

## “Chat with your documents”
Too weak:
- user still has to ask;
- no process state;
- no outcome tracking;
- no learning.

## “Jira AI assistant”
Too narrow.

## “RAG over historical tickets”
Insufficient vì tickets có thể không có actionable knowledge.

## “Workflow automation tool”
Too rigid nếu bỏ qua unstructured knowledge, exceptions, human judgment.

Desired category:

```text
Case Intelligence
+ Knowledge Intelligence
+ Process Intelligence
+ Action
+ Eval
+ Autonomy
```

---

# 20. Non-goals for Now

Do not prematurely build:
- generic multi-agent theater;
- giant vector architecture before proving value;
- autonomous high-risk actions;
- every connector;
- universal process mining;
- enterprise ontology;
- perfect Knowledge Graph.

---

# 21. Recommended Conversation / Workstream Structure

Giữ conversation hiện tại:

## 01 — Discovery & Product Vision

Mở các conversation mới trong cùng Project:

### 02 — Product Foundation
- lock Product Vision;
- Capability Map;
- MVP;
- success metrics;
- target persona/use case.

### 03 — Canonical Case Model v0.2
- entities;
- relationships;
- provenance;
- states.

### 04 — Knowledge Model
- KnowledgeRecord;
- applicability;
- lifecycle;
- verification.

### 05 — Process Model
- ProcessDefinition;
- ProcessRun;
- Step;
- Branch;
- Evidence;
- Exception.

### 06 — MVP Architecture
Chỉ sau khi domain + MVP lock.

### 07 — MVP Implementation

### 08 — Eval & Autonomy

### 09 — Future Capabilities

---

# 22. How AI Agents Should Use This File

At task start:

1. Read this file.
2. Identify whether statements are CONFIRMED / EVIDENCE-SUPPORTED / HYPOTHESIS / PROPOSED / OPEN QUESTION.
3. Do not convert PROPOSED → DECIDED.
4. Do not resurrect weakened hypotheses.
5. Challenge assumptions when evidence conflicts.
6. Preserve provenance, unknown, timeline.
7. Keep future capability map without building all of it.
8. Remember Jira is connector, not product boundary.
9. Automation proposals must include risk + approval + eval.
10. Distinguish:
   - knowledge absent;
   - knowledge exists but undiscovered;
   - knowledge fragmented;
   - knowledge inferred.

---

# 23. Discovery Artifacts

## Round 2
- `discovery-summary.md`
- `discovery-worst-cases.md`
- `scoring-method.md`
- `dataset-profile.json`
- `issue-profile.json`
- `worst-cases.json`

## Round 3
- `historical-summary.md`
- `repeated-problems.md`
- `reinvestigation-candidates.md`
- `knowledge-reuse-analysis.md`
- `round3-conclusions.md`
- `historical-profile.json`
- `repeated-problem-groups.json`
- `reinvestigation-candidates.json`

Raw Jira/customer data phải giữ private.

---

# 24. Open Questions for Product Foundation

## Product
1. First target user là ai?
   - New Support?
   - Experienced Support?
   - Technical/L3?
   - Operations/Knowledge Manager?

2. First painful workflow là gì?
3. Exact 3 MVP capabilities?
4. First measurable value:
   - reduced escalation?
   - reduced senior questions?
   - faster first correct action?
   - faster resolution?
   - higher SOP reuse?

## Knowledge
5. SOP/docs hiện nằm ở đâu?
6. Bao nhiêu tài liệu reliable/current?
7. Ai approve knowledge?
8. AI có được suggest update không?

## Process
9. Current processes formal đến mức nào?
10. Steps deterministic hay judgment-heavy?
11. Steps nào verify tự động được?
12. Steps nào cần user confirmation?

## Integration
13. Jira là connector đầu tiên?
14. Connector #2 là gì?
15. MVP cần read-only source systems không?

## Security
16. Data nào được gửi external LLM?
17. Data nào bắt buộc internal?
18. Tenant/organization boundaries?

## Evaluation
19. “AI helped” nghĩa là gì?
20. Làm sao biết recommendation đúng?
21. Đo knowledge reuse thế nào?

---

# 25. Recommended Immediate Next Step

Start conversation:

> **02 — Product Foundation: Vision, Capability Map & MVP**

First goal **không phải architecture**.

Order:

```text
1. Confirm problem taxonomy
2. Lock first target persona
3. Lock first use case
4. Lock exact 3 MVP capabilities
5. Define success metrics
6. Then refine Canonical Case v0.2
```

---

# 26. One-sentence Product Definition

> **AI Operational Knowledge & Process Platform giúp doanh nghiệp đưa đúng tri thức và đúng bước xử lý đến đúng người, đúng thời điểm; đồng thời học từ dữ liệu hoạt động để tạo, cải tiến và dần tự động hóa quy trình.**

Status: **PROPOSED — strong working definition**

---

# 27. Guardrail Against Scope Explosion

```text
VISION
= tất cả capability nền tảng có thể tiến tới

ROADMAP
= thứ tự xây capability

MVP
= capability tối thiểu để chứng minh value proposition

CURRENT SPRINT
= phần nhỏ hơn nữa
```

Không xóa future capabilities chỉ vì ngoài MVP.

Không implement future capabilities chỉ vì chúng được document.

---

# 28. Final Handoff State

## Strong enough
- Jira = Connector #1.
- Case source-agnostic.
- Knowledge Capture ≠ Knowledge Discovery.
- Knowledge Retrieval alone insufficient.
- Process là first-class domain.
- Process State cần Fact / Human Confirmation / AI Inference.
- Timeline/provenance/unknown là foundational.
- Eval + Autonomy giữ vai trò dài hạn.
- Product mở rộng được ngoài software support/Jira.

## Still needs design
- exact MVP;
- target persona;
- Canonical Case v0.2;
- KnowledgeRecord;
- ProcessDefinition/ProcessRun;
- success metrics;
- connector architecture;
- technical architecture.

## Current stage
> **Discovery đủ trưởng thành để chuyển sang Product Foundation.**
