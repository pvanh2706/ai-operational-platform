# 03 — Canonical Case Model v0.2

## AI Operational Knowledge & Process Platform

**Stage:** Domain Modeling
**Status:** v0.2 — Domain semantics đã được chốt
**Previous stage:** Product Foundation v1
**Recommended next workstream:** Knowledge Model hoặc Process Model

---

# 1. Definition of Case

## Canonical Case là gì?

`Canonical Case (Case chuẩn)` là:

> **Một đơn vị công việc nghiệp vụ có ranh giới, được tổ chức theo dõi vì có một tình huống cần được hiểu, quyết định, xử lý hoặc đưa đến một kết quả; Case có lifecycle riêng và có thể được hình thành từ một hoặc nhiều nguồn.**

Cách hiểu ngắn hơn:

> **Case = một việc đang được tổ chức xử lý và theo dõi tới một outcome.**

Một Case có thể xuất phát từ:

```text
Problem
Request
Question
Exception
Alert
Incident report
Investigation need
System detection
...
```

Case có thể tồn tại ngay cả khi:

```text
Problem     = UNKNOWN
Root Cause  = UNKNOWN
Category    = UNDETERMINED
Process     = chưa xác định
```

Case identity không phụ thuộc việc chúng ta đã hiểu đúng nguyên nhân hay chưa.

---

# 2. Case Boundary

## 2.1 Case ≠ Problem

```text
Case
= đơn vị công việc đang được xử lý

CaseProblem
= condition/vấn đề cụ thể mà Case đang cố hiểu hoặc giải quyết
```

Ví dụ:

```text
Case:
Booking Traveloka ABC123 không về PMS

CaseProblem:
Booking ABC123 không xuất hiện trong PMS

Root-cause Claim:
Parser không hỗ trợ payload OTA mới
```

Case identity không thay đổi khi hiểu biết về root cause thay đổi.

---

## 2.2 Case ≠ Incident

Một Incident có thể ảnh hưởng nhiều Case:

```text
                Incident
              /    |     \
          Case A Case B Case C
```

Không ép các Case thành một Case chỉ vì chúng có cùng technical cause.

Incident model chưa thuộc phạm vi v0.2.

---

## 2.3 Case ≠ Request

Request là điều một actor yêu cầu.

Case là operational work được tạo ra để xử lý tình huống.

```text
Request → có thể tạo Case

Case → không bắt buộc có Request
```

Monitoring/API có thể tạo Case mà không có requester.

---

## 2.4 Case ≠ Task

Task/Action thường là công việc được thực hiện trong một Case.

```text
Case:
Booking không về

Actions:
Check booking
Check mapping
Check log
Escalate Technical
```

Một Task có thể trở thành Case riêng nếu nó được quản lý với lifecycle, ownership và outcome độc lập.

---

## 2.5 Case ≠ Conversation

Email, chat, phone call hoặc comment là interaction/source/evidence liên quan đến Case.

Conversation không định nghĩa Case.

---

## 2.6 Case ≠ Ticket

```text
Jira Issue
CRM Request
Helpdesk Ticket
Email Thread
        ↓
Canonical Case
```

Ticket là representation ở source system.

Canonical Case là business concept độc lập source.

Không mặc định:

```text
1 Source Ticket = 1 Canonical Case
```

---

# 3. Root CanonicalCase Structure

Root được giữ nhỏ và source-agnostic:

```text
CanonicalCase
├── Identity
├── OrganizationalScope
├── Origination
├── Subject
├── Context
└── CurrentState
```

## Identity

Định danh ổn định của Canonical Case.

```text
Case Identity ≠ Source Identity
```

Jira key, CRM ID hay Email ID không phải Case identity.

---

## OrganizationalScope

Xác định Case thuộc organizational / tenant / security boundary nào.

Nó không đồng nghĩa với:

```text
Customer
Hotel
Affected System
```

Các concept đó thuộc Party hoặc Context.

---

## Origination

Trả lời:

> Case được hình thành như thế nào?

Ví dụ semantic categories có thể gồm:

```text
HUMAN_REPORTED
SOURCE_IMPORTED
SYSTEM_DETECTED
INTERNALLY_CREATED
AI_IDENTIFIED
UNKNOWN
```

Exact vocabulary sẽ được chốt khi thiết kế detailed domain contract.

---

## Subject

`CaseSubject (chủ đề của Case)` trả lời:

> **Case này đang xử lý việc gì?**

Ví dụ:

```text
OTA booking ABC123 not received by PMS
```

hoặc:

```text
Customer requests billing-email change
```

Subject không phải root cause hay classification.

Case có thể có Subject nhưng không có CaseProblem.

---

## Context

`CaseContext (ngữ cảnh Case)` là **current contextual projection**, không phải generic truth bag.

Nó có thể tổng hợp:

```text
Relevant business entity references
+
provenance-bearing claims
```

Ví dụ:

```text
Hotel        = Hotel A
Booking      = ABC123
OTA          = Traveloka
System       = PMS
Version      = UNKNOWN
Environment  = Production
```

Mọi canonical context quan trọng phải trace được về source/evidence/derivation.

Không được:

```text
AI extracts value
↓
put into Context
↓
lose provenance
```

---

## CurrentState

`CurrentState` trả lời:

> Case hiện đang ở trạng thái tổng quát nào?

Nó là `Projection (trạng thái tổng hợp hiện tại)`, không phải historical source of truth.

```text
Timeline / Temporal entities
            ↓
       CurrentState
```

Ví dụ:

```text
Lifecycle   = WAITING
WaitingFor  = CUSTOMER
Owner       = SUPPORT
```

Historical truth vẫn nằm trong Events/Segments.

---

# 4. Related Entities

Conceptual view:

```text
CanonicalCase
│
├── CaseParty[]
├── CaseEvent[]
├── OwnershipSegment[]
├── WaitingSegment[]
├── CaseProblem[]
├── CaseClaim[]
├── Classification[]
├── EvidenceItem[]
├── CaseAction[]
├── CaseOutcome[]
├── SourceReference[]
└── CaseRelation[]
```

Đây là conceptual relationship, không khẳng định tất cả phải trở thành collection/property trực tiếp trong một code class.

---

## 4.1 CaseParty

Biểu diễn actor/organization/system có vai trò đối với Case.

Ví dụ:

```text
REPORTER
REQUESTER
CUSTOMER
AFFECTED_PARTY
CONTACT
OBSERVER
```

Vocabulary chưa khóa.

```text
CaseParty ≠ Owner
```

Ví dụ:

```text
Reporter       = Customer A
Affected Party = Hotel X
Owner          = Support Team
```

Case không bắt buộc phải có human reporter.

---

## 4.2 CaseEvent

`CaseEvent (sự kiện Case)` biểu diễn:

> Một occurrence có ý nghĩa semantic đối với lifecycle/state của Case.

Ví dụ:

```text
CASE_CREATED
ENTERED_TECHNICAL_QUEUE
TECHNICAL_HANDBACK
REOPENED
RESOLVED
```

CaseEvent không phải generic audit log.

Không phải mọi:

```text
attachment upload
comment
API call
action
```

đều cần trở thành CaseEvent.

---

## 4.3 OwnershipSegment

`OwnershipSegment (giai đoạn chịu trách nhiệm)` là `Temporal Entity (thực thể theo thời gian)`.

Trả lời:

> Trong khoảng thời gian này, ai chịu trách nhiệm đưa Case tiến về phía trước?

Owner không chỉ là Person.

Có thể là:

```text
PERSON
TEAM
QUEUE
SYSTEM
EXTERNAL_PARTY
```

Ví dụ:

```text
09:00–09:30 Support Team
09:30–10:45 Technical Queue
10:45–11:10 Support Team
```

---

## 4.4 WaitingSegment

Không dùng:

```text
Waiting = true
```

Mà dùng temporal segment:

```text
WaitingSegment
├── WaitingFor
├── Reason
├── Start
├── End
└── Provenance
```

Ví dụ:

```text
WAITING_CUSTOMER
WAITING_SUPPORT
WAITING_TECHNICAL
WAITING_EXTERNAL_VENDOR
WAITING_SYSTEM
UNKNOWN
```

Exact values chưa khóa.

### Waiting ≠ Ownership

Có thể:

```text
Owner      = Support Team
WaitingFor = Customer
```

---

## 4.5 CaseProblem

`CaseProblem` là:

> Một condition/vấn đề cụ thể của Case mà Case đang cố hiểu hoặc giải quyết.

```text
CanonicalCase
1 → 0..N CaseProblem
```

Một Case có thể có nhiều CaseProblem.

`CaseProblem` không phải mọi hypothesis AI nghĩ ra.

Hypothesis/technical findings/root-cause assertions thuộc CaseClaim.

CaseProblem được scoped vào một Case.

Shared Incident/Shared Problem giữa nhiều Case là concept riêng.

---

## 4.6 CaseClaim

`CaseClaim (mệnh đề/nhận định về Case)` là:

> Một phát biểu có thể đúng, sai, chưa xác định hoặc bị tranh chấp về Case, được actor/system/AI đưa ra và có provenance.

Ví dụ:

```text
"Room mapping is correct."

"Booking exists on OTA."

"No incoming log was found."

"Payload structure may be unsupported."

"Unsupported payload caused processing failure."
```

CaseClaim là nơi quan trọng để bảo vệ:

```text
FACT ≠ AI INFERENCE
```

---

## 4.7 Classification

Classification là **structured assertion**, không phải authoritative field trên root.

Ví dụ:

```text
Dimension = SOFTWARE_DEFECT
Value     = UNDETERMINED
```

Sau đó có thể tồn tại đồng thời:

```text
Support:
UNDETERMINED

AI:
LIKELY_YES

Technical:
YES
```

Không overwrite history.

Classification dùng cùng provenance semantics với CaseClaim.

---

## 4.8 EvidenceItem

`EvidenceItem (bằng chứng)` là:

> Artifact hoặc observation có thể được dùng để hỗ trợ, bác bỏ hoặc cung cấp context cho một Claim/Problem/Action/Event.

Ví dụ:

```text
Comment
Attachment
Screenshot
Log
DB Result
API Response
Source Code
Git Commit
Document
User Statement
Phone Call Record
```

Evidence:

```text
≠ Claim
≠ Truth
```

Một log có thể incomplete, stale hoặc bị diễn giải sai.

---

## 4.9 CaseAction

Thay `ResolutionAction` bằng `CaseAction`.

`CaseAction` là:

> Một hành động thực tế được human/system thực hiện hoặc attempted trong quá trình xử lý Case.

Ví dụ:

```text
CHECK
INVESTIGATION
COMMUNICATION
ESCALATION
CONFIG_CHANGE
DATA_FIX
CODE_FIX
WORKAROUND
EXTERNAL_SYSTEM_ACTION
```

Exact action taxonomy chưa khóa.

Quan trọng:

```text
Recommended Action ≠ Actual CaseAction
```

AI recommend người dùng check mapping không có nghĩa mapping đã được check.

---

## 4.10 CaseOutcome

`CaseOutcome (kết quả Case)` là:

> Kết quả nghiệp vụ đạt được tại một điểm closure của Case.

```text
CaseAction  = đã làm gì
CaseOutcome = kết quả là gì
CaseState   = lifecycle hiện ở đâu
```

Ví dụ:

```text
Action:
CONFIG_CHANGE

Outcome:
Booking processing restored

State:
RESOLVED
```

Case có thể Reopen nên có thể có nhiều Outcome theo thời gian.

---

# 5. Entity Relationships

Conceptual relationship model:

```text
                     SourceReference
                        ▲   ▲   ▲
                        │   │   │
CanonicalCase ──────────┘   │   │
     │                      │   │
     ├── CaseEvent ─────────┘   │
     ├── CaseAction ────────────┘
     │
     ├── CaseParty
     ├── OwnershipSegment
     ├── WaitingSegment
     │
     ├── CaseProblem
     │       ▲
     │       │ explains / concerns
     │       │
     ├── CaseClaim ◄──── EvidenceItem
     │       ▲
     │       │
     │  Classification
     │
     └── CaseOutcome
```

Cross-domain:

```text
CanonicalCase ── CaseRelation ── CanonicalCase

CanonicalCase
      │
      └── associatedWith ── ProcessRun
                                │
                           instanceOf
                                │
                        ProcessDefinition

CanonicalCase
      │
      ├── references/uses ── KnowledgeRecord
      └── contributes evidence to Knowledge lifecycle

CanonicalCase
      │
      └── assistedBy ── AssistanceAttempt
```

Potential future:

```text
CanonicalCase → associatedWith → Incident
```

---

# 6. Timeline / Event Model

Timeline is authoritative history; CurrentState is its projection.

Ví dụ:

```text
09:00 CASE_CREATED
      Ownership = Support

09:20 Action: CHECK_MAPPING

09:40 Action: ESCALATE_TO_TECHNICAL
      Event: ENTERED_TECHNICAL_QUEUE
      Ownership: Support → Technical

10:30 Action: CODE_FIX

10:45 Event: TECHNICAL_HANDBACK
      Ownership: Technical → Support
      WaitingFor: Support

11:00 Action: VERIFY_RESULT

11:05 Event: RESOLVED
      Outcome recorded
```

### Source Event vs Semantic Event

Raw source occurrence phải được giữ.

Ví dụ:

```text
Jira:
Status → "Escalated to L3"
```

Canonical interpretation:

```text
CaseEvent:
ENTERED_TECHNICAL_QUEUE
```

Relationship:

```text
Source occurrence
      │
      └── derivedAs
             ↓
Canonical semantic CaseEvent
```

Nếu canonical mapping sau này được phát hiện sai, raw source history vẫn còn.

---

# 7. Provenance Model

`Provenance (nguồn gốc thông tin)` không phải một enum duy nhất.

Provenance phải phân biệt ít nhất:

```text
Provenance
├── Origin
├── Actor
├── Source
├── Evidence
├── Time
└── Verification
```

## 7.1 Origin

Trả lời:

> Thông tin được hình thành bằng cách nào?

Các semantics quan trọng:

```text
SYSTEM_FACT
USER_CONFIRMED
AI_INFERENCE
HUMAN_ASSESSMENT
IMPORTED_SOURCE_ASSERTION
```

Exact vocabulary chưa khóa.

---

## 7.2 Source / Evidence

Trả lời:

> Dựa vào đâu?

Ví dụ:

```text
Jira ES-123
Email message
API response
DB result
Log
Source code
```

---

## 7.3 Verification State

Trả lời:

> Nhận định hiện được xác minh tới mức nào?

Candidate vocabulary:

```text
SPECULATIVE
PLAUSIBLE
SUPPORTED
VERIFIED
CONFLICTING
INVALIDATED
```

Exact lifecycle sẽ được refine sau.

---

## 7.4 Origin ≠ Verification

Ví dụ:

```text
Origin       = AI_INFERENCE
Verification = VERIFIED
VerifiedBy   = Technical
```

Claim ban đầu do AI tạo vẫn giữ origin là AI inference dù sau này được Technical chứng minh đúng.

Không rewrite lịch sử thành:

```text
Origin = SYSTEM_FACT
```

Điều này cần cho traceability và Eval sau này.

---

## 7.5 USER_CONFIRMED ≠ Objective Truth

```text
User says:
"Mapping đúng rồi."
```

Fact chắc chắn:

> User đã đưa ra assertion đó.

Nhưng:

> Mapping thực sự đúng.

vẫn có thể bị evidence khác bác bỏ.

---

## 7.6 SYSTEM_FACT có phạm vi

`SYSTEM_FACT` chỉ có nghĩa:

> Hệ thống trực tiếp quan sát được điều đó trong phạm vi xác định.

Ví dụ:

```text
HTTP response = 200
Booking ABC exists in DB
Jira field value = "Không"
```

Không được suy ra:

```text
Jira field = "Không"
→ reproduction attempted and failed
```

---

# 8. Unknown / Uncertainty Handling

Unknown là dữ liệu hợp lệ.

Ví dụ:

```text
UNKNOWN
UNDETERMINED
UNAVAILABLE
NEEDS_INVESTIGATION
CONFLICTING_EVIDENCE
```

Không ép:

```text
unknown → false
unknown → no
unknown → most likely value
```

Ví dụ:

```text
Source:
Reproduction = "Không"

Canonical:
Actual reproduction state = UNKNOWN
```

Conflicting claims được giữ đồng thời:

```text
Claim A:
Mapping correct
Origin = USER_CONFIRMED

Claim B:
Mapping incorrect
Origin = SYSTEM_FACT
```

Projection có thể trả:

```text
Current Assessment =
CONFLICTING_EVIDENCE
```

Không cần xóa một trong hai claim.

---

# 9. Evidence Model Boundary

Evidence có identity/lifecycle/security riêng.

Metadata semantics tối thiểu cần hỗ trợ:

```text
Origin / Source
ObservedAt / CreatedAt
IngestedAt
Actor
Tenant / Organization
Visibility
Sensitivity
Machine Readability
Availability
Integrity / Verification
```

Evidence có thể:

```text
SUPPORT → Claim
REFUTE  → Claim
CONTEXT_FOR → Claim
```

Evidence cũng có thể:

```text
producedBy      → CaseAction
associatedWith  → CaseEvent
concerns        → CaseProblem
sourcedFrom     → SourceReference
```

Một EvidenceItem có thể liên quan nhiều Case hoặc nhiều Claim.

Không mặc định:

```text
1 Evidence = owned exclusively by 1 Case
```

Security metadata của Evidence không được kế thừa mù từ Case.

Một Case Support có thể chứa Evidence chỉ Technical được xem.

---

# 10. Source Reference Model

`SourceReference (tham chiếu nguồn dữ liệu gốc)` định danh record/object external.

Ví dụ:

```text
Jira ES-123
CRM CR-556
Email thread ABC
Monitoring alert A-887
Git commit abc123
Document SOP-01
```

SourceReference:

```text
≠ canonical truth
```

Một source có thể chứa assertion sai, stale hoặc chưa đủ semantic meaning.

Conceptual cardinality:

```text
CanonicalCase N ↔ M SourceReference
```

Ví dụ một Case:

```text
CASE-001
├── Email Thread
├── CRM Request
└── Jira Issue
```

Một monitoring alert cũng có thể liên quan nhiều Case.

---

# 11. Relationships with Knowledge and Process

## 11.1 Process

CanonicalCase không chứa ProcessDefinition.

Relationship chính:

```text
CanonicalCase
      │
      └── associatedWith
              ↓
          ProcessRun
              │
          instanceOf
              ↓
       ProcessDefinition
```

Một Case có thể có nhiều ProcessRun.

Ví dụ:

```text
ProcessRun A
OTA Booking Troubleshooting

ProcessRun B
Technical Escalation/Handoff
```

ProcessRun có thể bị chọn sai hoặc abandoned mà Case vẫn tồn tại.

CaseAction, CaseClaim và Evidence cung cấp observations để ProcessRun xác định process state.

---

## 11.2 Knowledge

Không đưa `KnowledgeRecord` vào CanonicalCase.

Phải phân biệt:

```text
Knowledge Retrieved
Knowledge Referenced
Knowledge Used
```

Retrieval không đồng nghĩa Knowledge thực sự được áp dụng.

Case có thể:

```text
reference Knowledge
use Knowledge
contribute evidence toward Knowledge
support Knowledge applicability
challenge Knowledge
```

Case không có authority trực tiếp:

```text
Case → INVALIDATE Official Knowledge
```

Đúng hơn:

```text
Case evidence
      ↓
Knowledge review process
      ↓
Knowledge lifecycle decision
```

Tri thức mới:

```text
Case Evidence
+ Claims
+ Actions
+ Outcome
        ↓
AI Knowledge Draft
        ↓
Human Review
        ↓
Verified Knowledge
```

### Bổ sung 2026-08-21 (H-2) — Evidence có đường TRỰC TIẾP tới Knowledge · `CONFIRMED`

Quyết định `K-B9` của Knowledge Model v0.1. §11.2 bản gốc chỉ mô tả đường
*"Case contributes evidence toward Knowledge"* — tức là **qua trung gian Case**.
Đường trực tiếp giờ cũng hợp lệ:

```text
EvidenceItem  →  SUPPORT / REFUTE / CONTEXT_FOR  →  Knowledge
                 (không cần Case làm trung gian)
```

**Vì sao cần:** một email hướng dẫn của senior, một tin Zalo, một ghi chú rời
**không thuộc Case nào**. Với thực tế 60% SOP tồn tại dưới dạng fragment rải rác,
đây không phải trường hợp hiếm. Nếu buộc phải qua Case thì phải tạo Case giả —
hoặc không dùng được nguồn đó.

**Vì sao không phá model:** §9 của tài liệu này **đã** viết
*"Một EvidenceItem có thể liên quan nhiều Case"* và
*"Không mặc định 1 Evidence = owned exclusively by 1 Case"*.
Đây là mở rộng theo đúng hướng model đã đi.

**Không thay đổi:** Case vẫn **không** có authority trực tiếp invalidate
Official Knowledge (quy tắc ở trên giữ nguyên). Evidence chỉ support/refute;
quyết định lifecycle vẫn thuộc Knowledge review process.

Xem `docs/04_KNOWLEDGE_MODEL_V0.1.md` §1.6 (K-B9).

---

## 11.3 AssistanceAttempt

AI recommendations/retrieval telemetry không thuộc core Case.

```text
CanonicalCase
      │
      └── AssistanceAttempt
```

AssistanceAttempt có thể ghi:

```text
Knowledge retrieved
Knowledge shown
Recommended action
User accepted/rejected
Knowledge actually used
AI response
```

Điều này hỗ trợ Eval và Capability #3 mà không biến CanonicalCase thành AI telemetry container.

---

# 12. Source Mapping Examples

## 12.1 Jira

```text
Jira Issue ES-123
        │
        ├── SourceReference
        │
        ├── Jira Description → Evidence
        │
        ├── Jira Category → Classification
        │
        ├── Jira field values → Source observations
        │
        └── Jira transitions
                 ↓ semantic mapping
             CaseEvent
```

Không map trực tiếp:

```text
JiraStatus      → CanonicalCase.Status
JiraAssignee    → CanonicalCase.Owner
JiraBugField    → CanonicalCase.IsBug
```

nếu chưa có canonical semantics/provenance phù hợp.

---

## 12.2 Email

```text
Email Thread
    │
    └── SourceReference

Email Message
    │
    └── EvidenceItem

Customer statement
    │
    └── CaseClaim
        Origin = imported human assertion

Sender
    │
    └── CaseParty
        Role = Reporter/Requester
```

Email claim không tự trở thành System Fact.

---

## 12.3 CRM

```text
CRM Request
    │
    └── SourceReference

Description
    └── Evidence

CRM Category
    └── Classification
        Origin = IMPORTED_SOURCE_ASSERTION

CRM Customer
    └── CaseParty

CRM Status
    └── Source observation
        ↓ mapping
      Canonical semantic state/event
```

---

## 12.4 Monitoring / API

```text
Monitoring Alert
       │
       └── SourceReference

Metric Snapshot
       └── Evidence

Threshold exceeded
       └── CaseClaim
           Origin = SYSTEM_FACT

Case
Origination = SYSTEM_DETECTED
```

Không cần Reporter hoặc Requester.

---

# 13. OTA Booking End-to-End Example

Case:

```text
Subject:
Booking Traveloka ABC123 không về PMS
```

### Intake

```text
Identity:
CASE-001

OrganizationalScope:
Tenant A

Origination:
HUMAN_REPORTED

Context:
Hotel = Hotel X
OTA = Traveloka
Booking = ABC123
System = PMS

SourceReference:
Jira / Email / CRM
```

### Case opened

```text
CaseEvent:
CASE_CREATED

CaseProblem:
Booking ABC123 không xuất hiện trong PMS

OwnershipSegment:
Support Team
```

### Support checks mapping

```text
CaseAction:
CHECK_ROOM_MAPPING
```

API produces:

```text
EvidenceItem:
Room mapping response
```

Support confirms:

```text
CaseClaim:
"Room mapping is correct"

Origin:
USER_CONFIRMED

Evidence:
Mapping response
```

### Escalation

```text
CaseAction:
ESCALATE_TO_TECHNICAL
```

causes:

```text
CaseEvent:
ENTERED_TECHNICAL_QUEUE
```

and:

```text
Ownership:
Support → Technical Queue
```

### Technical investigation

```text
Evidence:
Incoming payload log

CaseClaim:
"Incoming payload contains unsupported structure"

Evidence:
Source code

CaseClaim:
"Unsupported payload caused booking processing failure"
Verification = VERIFIED
```

### Technical action

```text
CaseAction:
CODE_FIX

Evidence:
Git commit

Action Result:
SUCCESS
```

### Handback

```text
CaseEvent:
TECHNICAL_HANDBACK

Ownership:
Technical → Support

WaitingSegment:
WAITING_SUPPORT
```

### Support verification

```text
CaseAction:
VERIFY_BOOKING_RECEIVED

Evidence:
PMS/API result

CaseClaim:
"Booking ABC123 now exists in PMS"
Origin = SYSTEM_FACT
Verification = VERIFIED
```

### Resolution

```text
CaseEvent:
RESOLVED

CaseOutcome:
Booking successfully received after parser fix
```

Knowledge loop:

```text
Technical findings
+ Evidence
+ Actions
+ Outcome
        ↓
AI Knowledge Draft
        ↓
Human Review
        ↓
Verified Knowledge
```

Process remains outside Case through ProcessRun.

---

# 14. Explicit Non-goals

Canonical Case Model v0.2 chưa thiết kế:

```text
Database schema
SQL
Entity Framework entities
REST API
Microservices
Event infrastructure
Connector implementation
Vector database
RAG architecture
Frontend
Multi-agent architecture
Production security architecture
KnowledgeRecord detailed model
ProcessDefinition detailed model
ProcessRun detailed state model
Incident model
Automation execution
Autonomy policies
Evaluation implementation
```

Đây là Domain Model, không phải implementation architecture.

Không cố xây:

```text
giant enterprise ontology
generic knowledge graph
universal incident management
universal workflow engine
```

ở bước này.

---

# 15. Key Risks

## R1 — Context trở thành generic bag

Nếu Context chứa mọi field từ Jira/CRM/AI extraction, source-specific coupling và provenance leakage sẽ quay trở lại.

Guardrail:

> Context là projection có provenance.

---

## R2 — Claim explosion

Nếu mọi câu nhỏ đều trở thành CaseClaim, model có thể quá nặng và khó sử dụng.

Cần xác định ở bước implementation/domain refinement:

> Claim nào có operational value đủ để persist?

---

## R3 — Event explosion

Nếu mọi activity trở thành CaseEvent thì Case timeline biến thành audit log.

Guardrail:

> CaseEvent chỉ dành cho semantic lifecycle occurrence.

---

## R4 — Provenance complexity

Provenance mạnh là cần thiết nhưng có nguy cơ khiến model/user workflow quá nặng.

Implementation sau này cần tự capture tối đa provenance thay vì yêu cầu người dùng nhập thủ công.

---

## R5 — Canonical mapping sai

Connector có thể map source wording sai semantic meaning.

Ví dụ:

```text
Jira transition
→ wrong canonical event
```

Raw source observations phải được giữ để audit/remap.

---

## R6 — Cross-source duplicate Cases

Email, CRM và Jira có thể cùng tạo Case cho một operational work.

Identity resolution chưa được giải quyết trong v0.2.

---

## R7 — Over-modeling software support

OTA Booking là first use case, nhưng Canonical Case không được phát triển lại thành software-ticket ontology.

Việc loại `ReproductionState` khỏi root là guardrail quan trọng.

---

## R8 — Security leakage

Evidence có thể chứa token, credentials, customer data, internal URL hoặc logs nhạy cảm.

Evidence-level visibility/sensitivity và organizational boundary phải được giữ trong mọi architecture sau này.

---

# 16. Open Questions

## OQ1 — Case Identity Resolution

Nếu:

```text
Email → CASE-001
CRM   → CASE-002
Jira  → CASE-003
```

sau đó phát hiện là cùng một operational work:

```text
merge?
duplicate?
link?
canonical winner?
```

Chưa chốt.

---

## OQ2 — Split / Merge semantics

Có cần first-class semantics:

```text
MERGED_INTO
SPLIT_FROM
DUPLICATE_OF
```

hay chỉ dùng CaseRelation + application workflow?

Chưa chốt.

---

## OQ3 — Case vs Incident creation policy

Một monitoring event ảnh hưởng nhiều customer:

```text
1 Incident + N Cases?
1 Case?
Incident only?
```

Canonical Case v0.2 không khóa policy này.

---

## OQ4 — Concurrent ownership

Một Case có cho phép nhiều primary responsible party cùng lúc hay không?

MVP chưa cần chốt.

---

## OQ5 — Exact vocabularies

Chưa khóa các enum cụ thể cho:

```text
Origination
Case lifecycle state
WaitingFor
PartyRole
ActionType
Claim Origin
VerificationState
CaseRelationType
```

Nên chốt cùng với Process/Knowledge model và MVP implementation contract, không tùy ý invent sớm.

---

## OQ6 — Persisted projection vs calculated projection

Semantics đã chốt:

```text
CurrentState / Context
= projections
```

Nhưng implementation sau này mới quyết định:

```text
calculate on demand?
materialize?
cache?
persist with rebuild capability?
```

---

# 17. Decision Register

## CONFIRMED — Foundational

```text
Jira is Connector, not Product Boundary
Case is source-agnostic
Case ≠ Knowledge
Case ≠ Process
Case ≠ Problem
Case ≠ Incident
Unknown is first-class
Timeline > Snapshot
Fact ≠ AI Inference
Provenance is foundational
Security / Tenant Boundary is foundational
```

## CONFIRMED — Case Boundary

```text
Case has stable identity
Case has lifecycle and outcome
Case may exist without known Problem/root cause
Request may create Case but is not required
Conversation is not Case
Source Ticket is not Case
Task usually belongs to Case
```

## CONFIRMED — Root

```text
Identity
OrganizationalScope
Origination
Subject
Context        // projection
CurrentState   // projection
```

`TriageState`, `ReproductionState` and authoritative `WaitingState` are not generic root fields.

## CONFIRMED — Related Concepts

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

## CONFIRMED — Provenance

```text
Origin ≠ Verification
Evidence ≠ Claim
Source value ≠ canonical truth
AI inference may later be verified without losing AI origin
User confirmation does not equal objective truth
System Fact is limited to direct system observation
Canonical semantic assertions must remain traceable
```

## CONFIRMED — Relationships

```text
Case ↔ SourceReference may be N:M
Case ↔ Case uses CaseRelation
Case ↔ Process uses ProcessRun
Case ↔ Knowledge uses explicit cross-domain relationships
Knowledge retrieved ≠ Knowledge used
Case can challenge/support Knowledge
Case does not directly invalidate official Knowledge
Support → Technical handoff normally remains same Case
Waiting ≠ Ownership
Action ≠ Event ≠ Outcome
```

## PROPOSED / NOT YET VOCABULARY-LOCKED

The conceptual entities and relationships above are confirmed.

Exact taxonomies/enums, detailed attributes and validation rules remain to be defined in later domain refinement.

## OPEN

```text
Case identity resolution
Merge / split policy
Case vs Incident creation policy
Concurrent primary ownership
Exact vocabularies
Projection implementation strategy
```

---

# 18. Canonical Case Model v0.2 — Compact View

```text
                         ┌─────────────────────────┐
External Sources ───────►│      CanonicalCase      │
Jira / Email / CRM / API │                         │
                         │ Identity                │
                         │ OrganizationalScope     │
                         │ Origination             │
                         │ Subject                 │
                         │ Context*                │
                         │ CurrentState*           │
                         └───────────┬─────────────┘
                                     │
        ┌────────────────────────────┼───────────────────────────┐
        │                            │                           │
        ▼                            ▼                           ▼
  Lifecycle / Time             Understanding                Work / Result
  ────────────────             ─────────────                ─────────────
  CaseEvent                    CaseProblem                  CaseAction
  OwnershipSegment             CaseClaim                    CaseOutcome
  WaitingSegment               Classification
                               EvidenceItem
                               CaseParty

        │
        ├──── SourceReference
        ├──── CaseRelation ─────────────► Other Case
        │
        ├──── ProcessRun ───────────────► ProcessDefinition
        │
        ├──── Knowledge relationships ─► KnowledgeRecord
        └──── AssistanceAttempt ────────► AI Assistance / Eval
```

`* Context` và `CurrentState` là projections, không phải nơi thay thế historical/provenance-bearing source of truth.

---

# Final Principle

Có thể tóm tắt Canonical Case Model v0.2 bằng một nguyên tắc:

> **Canonical Case giữ identity ổn định của công việc; timeline ghi điều đã xảy ra; claims ghi điều chúng ta tin/biết về công việc; evidence cho biết dựa vào đâu; actions ghi điều thực sự đã làm; outcome ghi kết quả; provenance bảo đảm AI, con người và source system không bị trộn thành một “truth” giả.**

Và ranh giới quan trọng nhất:

```text
Case
= work being handled

Knowledge
= what the organization knows

Process
= how the work should be handled

Evidence
= what we observed

Claim
= what we assert based on what we observed

Action
= what was actually done

Outcome
= what happened as a result
```

**Canonical Case Model v0.2 hoàn thành ở mức Domain Modeling.**
