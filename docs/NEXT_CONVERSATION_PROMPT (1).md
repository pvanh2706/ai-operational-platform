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
