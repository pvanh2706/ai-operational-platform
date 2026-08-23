> # ⛔ ARCHIVED — KHÔNG ĐỌC NHƯ QUYẾT ĐỊNH HIỆN HÀNH
>
> **Housekeeping `H-6` — archived 2026-08-23.** Trước đó file này tên `docs/NEXT_CONVERSATION_PROMPT (1).md` và nằm ngay trong `docs/`, nên bị đọc lẫn với tài liệu source of truth.
>
> **File này là PROMPT ĐẦU VÀO** của conversation `02 — Product Foundation`, viết *trước khi* workstream đó chạy. Nó **không phải** output, không phải quyết định.
>
> ⚠️ **Nội dung sai so với hiện tại:** nó chứa **phiên bản cũ của MVP Capability #3**. Bản đã chốt là `Assistance Outcome & Knowledge Capture` (`AGENT.md` §4), khác bản chất với bản cũ — so sánh ở `PROJECT_CONTEXT.md` §16 và `00_CURRENT_STATE.md` §4.
>
> **Giữ lại vì:** `AGENT.md` §13 cấm rewrite history, và file này là bằng chứng duy nhất còn lại về ý định đầu vào của workstream 02 — tài liệu **output** `02_PRODUCT_FOUNDATION_V1.md` đã bị mất (`00_CURRENT_STATE.md` §6.1).
>
> **Nguồn đúng để đọc:**
>
> ```text
> docs/00_CURRENT_STATE.md          trạng thái hiện tại — đọc TRƯỚC
> AGENT.md                          cách làm việc + quyết định đã chốt
> docs/PROJECT_CONTEXT.md           Discovery + Vision
> docs/Canonical Case Model v0.2.md domain model đã chốt
> docs/04_KNOWLEDGE_MODEL_V0.1.md   Knowledge Model, Step 1 + 2 đã chốt
> ```
>
> Prompt để mở phiên làm việc mới nằm ở `docs/00_CURRENT_STATE.md` §10, **không phải file này.**

---

# NEXT_CONVERSATION_PROMPT.md

## Suggested title

**02 — Product Foundation: Vision, Capability Map & MVP**

## Prompt

Tôi đang tiếp tục một project đã qua nhiều vòng Discovery.

Trước khi trả lời hoặc thiết kế bất cứ thứ gì, hãy đọc:

`PROJECT_CONTEXT.md`

File đó là source of truth hiện tại.

Yêu cầu làm việc:

1. Phân biệt rõ CONFIRMED / EVIDENCE-SUPPORTED / HYPOTHESIS / PROPOSED / OPEN QUESTION.
2. Không tự biến proposal cũ thành quyết định đã chốt.
3. Nếu Product Vision, MVP hoặc hướng hiện tại có vấn đề, hãy phản biện. Tôi không muốn AI chỉ đồng ý.
4. Jira chỉ là connector đầu tiên; không thiết kế product phụ thuộc Jira.
5. Giữ các nguyên tắc: provenance, FACT ≠ AI INFERENCE, Unknown là hợp lệ, Case cần timeline, Knowledge ≠ Case, Process ≠ Case, Eval + Autonomy là long-term, security/tenant boundary là foundational.
6. Chưa bắt đầu production architecture/coding.

### Mục tiêu conversation

#### Bước 1 — Review Product Problem
Đọc Problem Taxonomy trong PROJECT_CONTEXT.md.

Phản biện:
- thiếu problem quan trọng nào?
- problem nào overlap?
- product đang giải quyết một problem hay quá nhiều?

#### Bước 2 — Chọn Target Persona đầu tiên
So sánh:
- New Support Employee
- Experienced Support
- Technical/L3
- Operations/Knowledge Manager

Đánh giá theo:
- pain frequency;
- pain severity;
- access to data;
- implementation difficulty;
- measurable ROI;
- risk;
- expansion potential.

#### Bước 3 — Chọn First Use Case
Candidate hiện tại:

“Booking Traveloka/OTA không về PMS”

Company đã có process nhưng người mới không biết tài liệu tồn tại và thường phải hỏi người cũ.

Đánh giá đây có phải MVP use case tốt không. Nếu có case tốt hơn, phản biện và đề xuất.

#### Bước 4 — Lock EXACT 3 MVP Capabilities
Current PROPOSED candidates:

1. Contextual Knowledge Retrieval
2. Process Guidance
3. Knowledge/Process Draft from Operational Data

Không mặc định đây là final.

Mỗi capability cần:
- Problem solved
- User
- Trigger
- Input
- Output
- Dependencies
- Explicit non-goals
- Success metric
- Why MVP / why later

#### Bước 5 — Define MVP Success Metrics
Candidate:
- % Case AI tìm đúng SOP
- % suggestion được dùng
- giảm số lần hỏi senior
- giảm escalation không cần thiết
- time-to-first-correct-action
- SOP completion rate
- recommendation accuracy
- correction rate

Chọn metric phù hợp, không dùng tất cả.

#### Bước 6 — Produce Product Foundation v1
Tổng hợp:
- Product Statement
- Target Persona
- First Use Case
- Problem Definition
- 3 MVP Capabilities
- Non-goals
- Success Metrics
- Future Capability Map
- Key Risks
- Open Questions

### Quan trọng

Chưa:
- thiết kế production architecture;
- frontend/backend;
- vector DB/RAG framework;
- multi-agent;
- tự động hóa action rủi ro.

Chúng ta đang chốt **product cần làm gì trước**, chưa chốt **code bằng gì**.

Hãy chủ động phản biện và làm rõ để hai bên thật sự hiểu cùng một product.
