# 00 — CURRENT STATE / SESSION HANDOFF

## AI Operational Knowledge & Process Platform

> **Cập nhật:** 2026-08-21 (buổi 2 — D6 chốt, **Step 1 + Step 2 CHỐT**, §8.1 đã chạy)
> **Mục đích:** File này là điểm vào cho phiên làm việc tiếp theo. Đọc file này TRƯỚC, rồi mới đọc các tài liệu khác.
> **Dành cho:** AI Agent hoặc người mới tiếp tục project, kể cả trên máy khác.

---

---

# TL;DR — đọc 30 giây này trước

```text
DỰ ÁN     AI Operational Knowledge & Process Platform
          Sản phẩm ĐỂ BÁN (D1), multi-tenant từ ngày đầu.
          Bản build đầu: engine gợi ý quy trình + tri thức, nhúng được (D2).

STAGE     Domain Modeling. Workstream 04 — Knowledge Model v0.1.
          Step 1 Knowledge Boundary        ✅ CHỐT  → 04 §1
          Step 2 Concepts & Granularity    ✅ CHỐT  → 04 §3
          Step 3 Knowledge ↔ Case ↔ Process 🔵 TIẾP THEO

CHƯA CODE. Chốt công nghệ là quyền của người dùng — AGENT.md §10.1.
          Đúng thời điểm chốt là Workstream 06, không phải bây giờ.
```

## Ba con số phải nhớ

```text
10 / 30 / 60   SOP có và tìm được 10% · trong đầu người 30% · rải rác 60%
               → Capability 1 (retrieval) ngày đầu gần như không có gì để trả

5 bước         Quy trình THẬT của first use case (§8.1-KQ):
               Kibana → response → tài liệu → issue cũ → ĐƯA RA KẾT LUẬN
               Tuyến tính, KHÔNG nhánh. Giá trị nằm trọn ở bước cuối,
               và đó là bước duy nhất không ai ghi lại.

5-10           Số loại nguyên nhân của first use case.  ⚠ n=1, chưa xác nhận.
               → kho tri thức ~10 record, không phải 500.
               → toàn bộ 04 §3.5 đứng trên con số này. Xem R-K4.
```

## Việc tiếp theo, theo thứ tự giá trị

```text
1  §8.2  ĐẾM 20 case OTA gần nhất — nguyên nhân rơi vào mấy nhóm?
         Việc của NGƯỜI DÙNG, ngoài thiết kế. Giá trị cao nhất còn lại.
         Xác nhận/bác bỏ con số 5-10 → xác nhận/bác bỏ 04 §3.5.
         Cũng cho Q-E (Success Metrics) con số thật thay vì ứng viên.

2  Step 3  Knowledge ↔ Case ↔ Process. Mang theo N-3b, N-6, N-7 (04 §3.6).

3  H-3..H-7  Housekeeping, gộp một lần cuối workstream 04.
```

## Cách làm việc mà người dùng đã yêu cầu

```text
· Hỏi từng quyết định qua FORM để tích chọn, không liệt kê rồi chờ trả lời bằng chữ
· Phản biện TRƯỚC khi đề xuất. Không chỉ đồng ý.
· Ngôn ngữ dễ hiểu, tránh thuật ngữ không cần thiết
· Ghi quyết định xuống tài liệu NGAY khi chốt — dự án đã từng mất tài liệu 02
  cùng toàn bộ Success Metrics mà không ai phát hiện
· Không tự chuyển PROPOSED → CONFIRMED, không tự đóng OPEN QUESTION
```

## Tổng số quyết định đã chốt trong workstream 04

```text
Step 1   S1-S8, K-B9, Q-B, Q-C     11 quyết định   → 04 §1, §4
Step 2   T1-T4                      4 quyết định   → 04 §3, §4
Nền      D1-D6, G1-G11              → AGENT.md §3, §4, §4B
```

# 1. Đọc gì, theo thứ tự nào

```text
1. docs/00_CURRENT_STATE.md          ← file này. Trạng thái hiện tại + việc đang làm
2. AGENT.md                          ← cách agent phải làm việc trong project
3. docs/PROJECT_CONTEXT.md           ← Discovery + Vision (consolidated 2026-08-18)
4. docs/Canonical Case Model v0.2.md ← Domain Model đã chốt
5. docs/04_KNOWLEDGE_MODEL_V0.1.md   ← Knowledge Boundary đã chốt (Step 1)
```

## ⚠️ Cảnh báo về tài liệu

**`docs/02_PRODUCT_FOUNDATION_V1.md` KHÔNG TỒN TẠI.**

`AGENT.md` §1 yêu cầu đọc ba tài liệu theo tên `01_` / `02_` / `03_`. Thực tế:

| AGENT.md nói | File thật | Trạng thái |
|---|---|---|
| `docs/01_PROJECT_CONTEXT.md` | `docs/PROJECT_CONTEXT.md` | ✅ có, khác tên |
| `docs/02_PRODUCT_FOUNDATION_V1.md` | — | ❌ **MISSING** |
| `docs/03_CANONICAL_CASE_MODEL_V0.2.md` | `docs/Canonical Case Model v0.2.md` | ✅ có, khác tên |

`docs/NEXT_CONVERSATION_PROMPT (1).md` là **prompt đầu vào** của conversation 02, **không phải** output. Nó chứa phiên bản **cũ** của MVP Capability #3 → đừng đọc nó như quyết định hiện hành.

**Hệ quả của việc mất tài liệu 02:** không có capability contract chi tiết, và đặc biệt là **không biết Success Metrics của MVP là gì**. Đây vẫn là `OPEN QUESTION` quan trọng chưa giải quyết.

---

# 2. Quyết định đã chốt

## 2.1 Từ trước (đã có trong AGENT.md / tài liệu cũ) — `CONFIRMED`

```text
Primary Persona      New Support Employee
Secondary Persona    Technical / L3
First Use Case       OTA Booking Not Received Assistance

3 MVP Value Capabilities:
  1. Contextual Knowledge Retrieval
  2. Process Guidance
  3. Assistance Outcome & Knowledge Capture
```

Guardrails nền tảng (chi tiết trong AGENT.md §3):

```text
G1  Jira là connector, không phải product boundary
G2  Case ≠ Knowledge ≠ Process
G3  FACT ≠ AI INFERENCE
G4  Unknown là dữ liệu hạng nhất
G5  Timeline hơn Snapshot
G6  Provenance là nền tảng (Origin ≠ Verification)
G7  Security / Tenant boundary là nền tảng
G8  Không dùng numeric LLM confidence như truth
G9  Không tự thêm MVP Capability #4, #5
G10 Không tự chuyển PROPOSED → CONFIRMED, không tự đóng OPEN QUESTION
```

Canonical Case Model v0.2 đã chốt ở mức Domain Modeling — xem `docs/Canonical Case Model v0.2.md` §17 Decision Register.

## 2.2 Chốt mới trong phiên 2026-08-20/21 — `CONFIRMED`

Bốn quyết định mới, do người dùng xác nhận trực tiếp:

### D1 — Đây là sản phẩm để BÁN, không phải tool nội bộ
Multi-tenant là yêu cầu ngày đầu, không phải bảo hiểm cho tương lai.

Tri thức đến từ nhiều nguồn: tài liệu công ty tự nạp, Jira, email, **tài liệu AI tự tìm được trong source code (nếu được phân quyền)**, tài liệu có phân quyền nội bộ.

Doanh nghiệp **không dùng Jira** vẫn phải dùng được. Ví dụ đã nêu: nhúng vào CRM — mỗi khi user kéo deal sang stage mới, AI gợi ý bước tiếp theo hoặc cách xử lý để tăng tỉ lệ won.

### D2 — Bản build đầu tiên: (3) engine gợi ý quy trình + tri thức, nhúng được
Chọn trong 4 hướng đã nêu:

```text
(1) Nền tảng tri thức có phân quyền, khách tự nạp tài liệu
(2) AI đọc source code rút ra tri thức                    → v2, nhạy cảm quyền nhất
(3) Engine gợi ý quy trình + tri thức, nhúng được          ← ĐÃ CHỌN
(4) Case intelligence trên dữ liệu vận hành
```

Lý do chọn (3): phổ quát nhất (chạy cho cả support và sales); là phần **mạnh lên** khi model mạnh lên; khó bị vendor thương mại hóa nhất.

### D3 — Khách hàng #0 là công ty của người dùng
Test nội bộ trước. Nhưng **tenant boundary có trong mô hình dữ liệu từ ngày đầu** — nhồi vào sau rất đắt.

Lưu ý: pitch là "nhúng vào CRM/helpdesk của khách", nhưng host app đầu tiên là hệ thống support của chính công ty. Interface nhúng nên được thiết kế generic ngay, dù mới có một host.

### D4 — Nguyên tắc tự chủ của AI
```text
AI tự ĐỀ XUẤT tri thức mới                  →  CÓ
AI tự CÔNG NHẬN thành tri thức chính thức   →  KHÔNG
Mở dần theo eval, không theo model confidence
```

Nhất quán với guardrail đã có: `OBSERVED PATTERN → AI DRAFT → HUMAN REVIEW → VERIFIED KNOWLEDGE`, và Autonomy Level 1→5 trong PROJECT_CONTEXT §5.8.

### D5 — Nguyên tắc "model mạnh lên thì phần mềm mạnh lên" — `CONFIRMED` (người dùng nêu, agent đề xuất cách áp dụng)

Mọi thứ build ra phải phân loại được:

| Tài sản bền — model mạnh lên thì giá trị hơn | Giàn giáo tạm — model mạnh lên thì thành nợ |
|---|---|
| Dữ liệu đã kết nối + phân quyền | Prompt phức tạp nhiều tầng |
| Provenance | Pipeline cắt chunk, template extraction |
| Lịch sử outcome (cái gì đã giúp) | Luật if-else bù model yếu |
| Bộ eval | Multi-agent dựng để bù reasoning yếu |
| Connector / tích hợp | Cơ chế sửa lỗi model thủ công |
| Trạng thái quy trình | |

**Câu hỏi test cho mọi feature:** *"Nếu sang năm có model mạnh gấp 10, cái này thành giá trị hơn hay thành rác?"*

Bốn hệ quả:
1. **Bộ eval là first-class, không phải phase sau.** Eval là *cơ chế* biến "model mạnh lên" thành "phần mềm mạnh lên". Không eval → đổi model là đánh cược. Cần có từ MVP dù chỉ 20 case gán nhãn tay.
2. **Không đưa giới hạn hôm nay vào domain model** (chunk size, context limit, embedding dimension = tham số hạ tầng).
3. **Nút cổ chai sẽ dịch từ "AI có hiểu không" sang "AI có được phép xem không".** Model càng mạnh, retrieval càng thành commodity → lợi thế dồn về permission + provenance + outcome. Đây là lý do mạnh nhất để coi phần đó là cốt lõi sản phẩm.
4. **Nâng năng lực bằng policy, không bằng rewrite.** Mức tự chủ là cấu hình theo action; model mạnh lên = nâng level cho action đã có eval chứng minh.

---

## 2.3 Chốt trong phiên 2026-08-21 (buổi 2) — `CONFIRMED`

### D6 — "Gom nhiều case cũ thành một SOP theo yêu cầu" NẰM TRONG Capability 3 của MVP

Người dùng xác nhận trực tiếp. Đây là Open Question `Q-A` — quyết định duy nhất đang chặn Step 1 của Workstream 04. Toàn bộ phân tích dẫn tới câu hỏi này vẫn được giữ nguyên ở §4 (không rewrite history).

Chọn **phiên bản nhẹ** đã đề xuất ở §4 — *"gom theo yêu cầu"*, không phải *"tự đào quy luật"*.

**Phạm vi chính xác — cột phải là guardrail, không phải mô tả:**

```text
THUỘC D6                                    KHÔNG THUỘC D6
người nói "tôi cần SOP cho chủ đề X"        AI tự quyết chủ đề nào cần SOP
kéo một tập CÓ GIỚI HẠN case liên quan      quét toàn bộ corpus tìm quy luật mới
AI soạn nháp → người sửa → người duyệt      AI tự công nhận (vi phạm D4)
một hành động, có điểm đầu và điểm cuối     job chạy nền liên tục
```

Cột phải chính là `Process Discovery` (PROJECT_CONTEXT §7.2) + `Knowledge Gap Detection` (§8.2) — đã có nhãn *future capability* ở `PROJECT_CONTEXT.md` §17. D6 **không** kéo chúng vào MVP, **không** tạo Capability #4 → giữ G9.

> ⚠️ **Cảnh báo trôi phạm vi.** Sẽ có lúc ai đó đề nghị *"hay là mình tự động phát hiện chủ đề nào cần SOP luôn"*. Câu đó nghe rất hợp lý và nó là cột phải. Ghi xuống đây để lần sau có chỗ đối chiếu.

**Ba hệ quả đã ghi nhận:**

**(1) `Q-C` co lại — không còn là câu hỏi tốn kém.**
Để gom N case cũ thành SOP, hệ thống buộc phải tìm được N case cũ liên quan. Nên:

```text
cỗ máy "tìm case cũ tương tự"  →  giờ là DEPENDENCY của Capability 3
                                  không còn là lựa chọn của Capability 1
```

Q-C chuyển từ *"có build không?"* (đắt) thành *"có bày ra cho người dùng thấy không?"* (gần như miễn phí, vì phần đắt đã trả rồi).

**(2) D6 là bánh đà của D5 — điểm này chưa từng được ghi.**
`D5 hệ quả 1` đòi bộ eval là first-class, nhưng gán nhãn tay thì không ai muốn làm. D6 giải quyết miễn phí:

```text
AI soạn nháp SOP     →  bản A
người sửa và duyệt   →  bản B
diff(A, B)           →  đúng cái nhãn eval cần
```

Mỗi lần dùng tính năng sinh ra một cặp *(nháp, bản người sửa)* — tín hiệu chất lượng cao nhất có thể có, vì nó ghi lại **con người sửa gì**. Sinh ra bởi hành vi dùng sản phẩm, không phải bởi một phase gán nhãn riêng.

→ D6 không chỉ là một feature. Nó là **cơ chế** biến D5 từ nguyên tắc thành thứ chạy được.

**(3) D6 là điều kiện để D2 dùng được ở khách hàng đầu tiên.**
Engine gợi ý quy trình (D2) không có gì để gợi ý nếu khách chưa có quy trình nào trong hệ thống. Với 10% (§3), engine đó gần như im lặng ngày đầu. D6 là thứ nạp đạn cho nó. Đã kiểm chứng ở cả vertical thứ hai: rule *"khách ở Negotiation im hơn 7 ngày → gửi case study cùng ngành"* cũng được suy ra từ các deal won/lost — cùng cơ chế, khác tên.

---

# 3. Con số quan trọng nhất: 10 / 30 / 60

Người dùng cung cấp 2026-08-21. Thực tế SOP tại công ty:

```text
SOP có, tìm được (Drive/Confluence/Zalo)              10%
SOP chỉ nằm trong đầu người                           30%
SOP rải rác: vài comment Jira, một email, ghi chú     60%
```

## Hệ quả: MVP đang xếp ngược so với dữ liệu

Nếu build retrieval trước, **9/10 lần hệ thống trả về không có gì.**

Phần 60% là chỗ AI có lợi thế lớn nhất, vì:
- fragments đã tồn tại trong Jira/email, máy đọc được, đã có quyền truy cập
- AI giỏi thật ở việc gom mảnh rời thành bản nháp
- người chỉ phải sửa và xác nhận, không phải viết từ đầu
- đầu ra là SOP → nạp ngược vào Capability 1 và 2, biến 60% thành 10%

## Thứ tự vòng lặp đề xuất — `PROPOSED`, chưa chốt

Vẫn đúng 3 capability đã chốt (không vi phạm G9), chỉ đổi **điểm vào**:

```text
Gom tri thức rải rác   →  tạo ra SOP đầu tiên        (Cap 3, phần capture)
Tìm đúng tri thức      →  tìm ra SOP vừa tạo          (Cap 1)
Dẫn từng bước          →  dẫn theo SOP đó             (Cap 2)
Ghi lại sau mỗi case   →  SOP tự dày lên              (Cap 3, phần còn lại)
```

## Đây là điểm bán, không phải điểm yếu

> Hầu hết sản phẩm knowledge **giả định khách đã có tài liệu**. Khách không có thì mua về không dùng được.
> Sản phẩm này **không cần khách có sẵn tài liệu** — nó tạo ra tài liệu từ dữ liệu vận hành của khách.

Đó là câu trả lời cho cold start, và khó copy. Nên đưa vào pitch.

## Tính năng "đề xuất công ty bổ sung SOP" — không phải Capability #4

Người dùng đề nghị thêm tính năng này. Agent đề xuất: đây **không** phải capability mới, mà là **cách trả lời trung thực của Capability 1 khi thất bại** — chính là guardrail G4 (Unknown là hạng nhất) áp vào bề mặt sản phẩm:

```text
Chưa có tài liệu về việc này.

Nhưng tôi tìm thấy 14 case tương tự đã được xử lý.
→ Soạn bản nháp SOP từ 14 case đó?          [Soạn thử]
→ Hoặc tải lên tài liệu nếu công ty đã có.   [Tải lên]
```

Nó chuyển thẳng người dùng sang Capability 3 → vòng lặp tự khép.

---

# 4. Quyết định đã chặn Step 1 — nay ĐÃ GIẢI

> ## ✅ "Gom nhiều case cũ thành một SOP theo yêu cầu" có nằm trong Capability 3 của MVP không?

Tình trạng: **`RESOLVED 2026-08-21` — CÓ, ở phiên bản "gom theo yêu cầu".** Xem `D6` §2.3.

Phần còn lại của §4 là **phân tích lịch sử dẫn tới quyết định** — giữ nguyên, không rewrite. Đọc để hiểu *vì sao* D6 được chốt như vậy, đặc biệt là mismatch giữa hai tài liệu và hai nhánh hệ quả.

## Vì sao đây là câu hỏi thật, không phải chi tiết

Có một mismatch chưa được giải quyết giữa hai tài liệu:

```text
PROJECT_CONTEXT.md §16 (cũ)                 AGENT.md §4 (mới)
"Knowledge/Process Draft         vs         "Assistance Outcome
 from Operational Data"                      & Knowledge Capture"
 status: PROPOSED                            status: CONFIRMED
```

Đây không phải đổi tên — hai capability khác bản chất:

| | Bản cũ | Bản mới (đã chốt) |
|---|---|---|
| Nguồn | corpus lịch sử, N cases | case đang xử lý, 1 case |
| Thời điểm | offline / retrospective | inline / lúc đóng case |
| Kỹ thuật | pattern mining, clustering | hỏi + draft + confirm |
| Vấn đề giải | P4 + P2 | P2 + P8 |

Áp rule recency của AGENT.md §1 → bản mới thắng. Có corroboration độc lập: `Canonical Case Model v0.2.md` §11.3 viết *"hỗ trợ Eval và Capability #3"* khi nói về `AssistanceAttempt` → doc v0.2 đã hiểu Capability #3 theo nghĩa mới.

**NHƯNG** con số 60% (§3) làm câu hỏi này **mở lại theo hướng ngược**: nếu không có SOP để retrieve, việc đầu tiên sản phẩm phải làm là *tạo ra* SOP.

## Phiên bản nhẹ mà agent đề xuất — `PROPOSED`

**"Gom theo yêu cầu"** thay vì **"tự đào quy luật"**:

| Bản cũ (đã bỏ) | Bản đề xuất |
|---|---|
| AI tự đào 500 case, tự phát hiện quy luật chưa ai biết | Người nói *"tôi cần SOP cho case OTA booking không về"* |
| Không giới hạn phạm vi, giống bài toán ML mở | AI kéo ~20 case liên quan + email + comment đúng chủ đề đó |
| Không biết khi nào xong | AI soạn bản nháp → người sửa → xong |
| Đắt, rủi ro cao | Rẻ, có ranh giới, người điều khiển |

Cùng giá trị, khoảng 10% công sức, không có bài toán ML vô hạn ở giữa.

## Hai nhánh hệ quả

```text
NẾU CÓ  → Step 1 phải định nghĩa rõ trạng thái
          "tri thức được gom từ nhiều nguồn rời, chưa ai duyệt"
          vì đó là 60% thực tế.

NẾU KHÔNG → MVP chỉ phục vụ được 10% trường hợp.
            Phải nói rõ điều này với team ngay từ đầu
            để không ai kỳ vọng sai.
```

Agent nghiêng về **CÓ**, ở phiên bản "gom theo yêu cầu". Nhưng đây là quyết định về phạm vi MVP — thuộc quyền người dùng.

---

# 5. Step 1 — Define Knowledge Boundary  (✅ ĐÃ CHỐT — kết quả ở docs/04_KNOWLEDGE_MODEL_V0.1.md)

Workstream: `04 — Knowledge Model v0.1`. Stage: Domain Modeling.

**Chỉ làm Step 1.** Không thiết kế toàn bộ Knowledge Model. Không schema, không C# entity, không REST API, không vector DB, không embedding, không RAG, không frontend, không microservices, không production architecture.

Ưu tiên: `Business meaning → Domain boundary → Concepts → Relationships → Provenance → Lifecycle → Rules`

## 5.1 Câu hỏi trung tâm

> **Knowledge là gì và không phải là gì trong product này?**

Phải phân biệt được: Knowledge · Evidence · Case · Historical Case · Document · Process · Technical Finding · AI-generated Knowledge Draft

## 5.2 Định nghĩa ứng viên — `PROPOSED`

> **Knowledge = một phát biểu tái sử dụng được về việc thế giới nghiệp vụ vận hành thế nào, hoặc một loại tình huống nên được xử lý thế nào; được tổ chức chấp nhận ở một mức verification xác định; có applicability scope; và tồn tại độc lập với bất kỳ Case cụ thể nào.**

Từ khoá: **reusable · class-level · organizationally accepted · case-independent**

## 5.3 Discriminator Test — `PROPOSED`

Mục đích: tranh luận "cái này có phải Knowledge không" được giải quyết bằng **test**, không bằng ý kiến.

```text
T1  CASE-INDEPENDENCE
    Xóa Case đã sinh ra nó → phát biểu còn giá trị không?
    Không → dữ liệu của Case, không phải Knowledge.

T2  CLASS-LEVEL
    Nó nói về một LỚP tình huống, hay một instance?
    "Booking ABC123 không về vì parser lỗi"           → instance → CaseClaim
    "OTA payload dạng X sẽ bị parser < v2.3 drop"     → class    → Knowledge candidate

T3  ORGANIZATIONAL ACCEPTANCE
    Đã có ai/quy trình nào chấp nhận nó ở một mức nào chưa?
    Chưa → Draft.

T4  DECISION VALUE
    Nó giúp quyết định / hành động / giải thích?
    Không → đúng nhưng vô dụng; không persist.

Pass cả 4 → Knowledge.
```

## 5.4 Bốn chiều của Knowledge — `PROPOSED`

Đã cập nhật sau quyết định D1 (phân quyền + đa nguồn):

```text
Applicability  — áp dụng cho tình huống nào (phiên bản, khách hàng, hệ thống nào)
Authority      — ai nói, được ai chấp nhận, ở mức nào
Visibility     — ai được phép thấy                              ← MỚI, do D1
Derivation     — sinh ra từ đâu                                 ← quan trọng hơn dự kiến
```

## 5.5 Bảng ranh giới 8 concept — `PROPOSED`

| Concept | Trả lời | Scope | Case-independent? | Có verification lifecycle? | Là Knowledge? |
|---|---|---|---|---|---|
| **Case** | Việc gì đang được xử lý? | 1 instance | ✗ | ✗ (có case lifecycle) | **Không** — `CONFIRMED` v0.2 |
| **Historical Case** | Việc gì đã từng được xử lý? | 1 past instance | ✗ | ✗ | **Không** → precedent/evidence |
| **Evidence** | Ta quan sát được gì? | 1 observation | ✓ (nhiều nơi cite được) | ✗ (có integrity/availability) | **Không** — `CONFIRMED` v0.2 §9 |
| **Document** | Tri thức được ghi ở đâu? | container | ✓ | ✗ (có version của file) | **Không** → carrier/source |
| **Technical Finding** | Cơ chế kỹ thuật nào giải thích? | thường 1 case | ✗ khi mới sinh | chỉ sau khi generalize | **Chưa** → CaseClaim |
| **Process** | Việc nên xử lý thế nào? | 1 lớp công việc, có step + state | ✓ | ✓ | **Không** → domain riêng (05) |
| **AI Knowledge Draft** | AI đề xuất tổ chức biết gì? | candidate | ✓ | ở state DRAFT | **Chưa** |
| **Knowledge** | Tổ chức biết gì, áp dụng được? | 1 lớp tình huống | ✓ | ✓ | **Có** |

## 5.6 Bảy boundary claim — accept/reject từng cái, đều đang `PROPOSED`

**K-B1 · Knowledge là case-independent.** Case không bao giờ *trở thành* Knowledge. Historical Case dù hữu ích vẫn là precedent/evidence.
→ Hệ quả cần quyết: Capability #1 khi đó trả về **hai loại object khác nhau** (KnowledgeRecord + Historical Case). PROJECT_CONTEXT §15 Layer B liệt kê chúng tách riêng. MVP coi là một loại kết quả hay hai?

**K-B2 · Document ≠ Knowledge.** Document là carrier có identity/version/access riêng; Knowledge **cite** Document. Chống rủi ro biến Knowledge Model thành Document Management Model.

**K-B3 · Evidence ≠ Knowledge.** Evidence gắn với một thời điểm và một nguồn; Knowledge là phát biểu class-level. Evidence **support/refute** Knowledge — soi chiếu quan hệ `Evidence → Claim` đã CONFIRMED ở v0.2.

**K-B4 · Technical Finding chỉ là CaseClaim cho tới khi được generalize + accept.** Bước generalization là **một quyết định**, không phải promotion tự động. Bảo vệ G3.

**K-B5 · AI Knowledge Draft không phải Knowledge.** Origin = `AI_INFERENCE` giữ **vĩnh viễn** kể cả sau khi human verify (v0.2 §7.4).
→ Câu hỏi để Step 2/3: Draft là **state của KnowledgeRecord** hay **entity riêng**? Agent nghiêng về *state* (giữ continuity provenance draft→verified như G6 yêu cầu) + policy filter ở retrieval.

**K-B6 · Process ≠ Knowledge — cần rule phân định, không chỉ tuyên bố.** Rule đề xuất:
```text
Có ordered/conditional STEP + theo dõi được "đang ở bước nào" cho từng Case  →  PROCESS
Là assertion / explanation / applicability, không có per-case execution state →  KNOWLEDGE
Một SOP document có thể là SOURCE cho cả hai.
```
Đây là claim agent ít tự tin nhất và **quan trọng nhất** — vì SOP của first use case chính là một step list. Với D2 (chọn engine gợi ý quy trình) thì claim này càng load-bearing.

**K-B7 · Tri thức trong đầu người chưa phải Knowledge của hệ thống.** PROJECT_CONTEXT §5.2 liệt kê "Human knowledge / senior memory" dưới mục KNOWLEDGE — đó là **knowledge source**, không phải KnowledgeRecord. Chỉ vào model sau khi externalize. **Đặc biệt quan trọng vì 30% SOP nằm trong đầu người.**

**Negative list — Knowledge KHÔNG bao giờ là:** chat/comment log · audit trail · `AssistanceAttempt` telemetry · metric/analytics · giá trị field của một Case · embedding/index artifact (infrastructure) · phát biểu của khách hàng.

## 5.7 Câu hỏi khó nhất của Step 1 — sinh ra từ D1

> **Tri thức mà AI tổng hợp từ nguồn bị giới hạn quyền thì ai được xem?**

Ví dụ cụ thể: AI được cấp quyền đọc private repo, suy ra *"bản dưới 2.3 bỏ qua payload OTA dạng X"*.

```text
Support xem được không?     họ không có quyền vào repo
Cho xem   → đã rò rỉ thông tin từ nguồn giới hạn chưa?
Không cho → tri thức vô dụng, vì đúng người cần lại không thấy
```

Đây **không** phải câu hỏi hạ tầng. Nó quyết định **"Knowledge" là một loại hay hai loại** — và nếu làm sai, sản phẩm bán ra sẽ bị khách doanh nghiệp chặn ở vòng security review.

**Hướng đề xuất** (`PROPOSED`): tách **kết luận** khỏi **dẫn chứng**. Kết luận có thể có visibility rộng hơn nguồn sinh ra nó — nhưng việc mở rộng phải là **hành động tường minh của người có quyền**, có ghi nhận ai quyết định và khi nào. Không bao giờ là mặc định của hệ thống.

Cùng khuôn với `AI DRAFT → HUMAN REVIEW` đã chốt (D4), chỉ áp cho *quyền xem* thay vì cho *nội dung*.

## 5.8 Trình tự chạy Step 1

```text
1  Accept/reject/sửa định nghĩa §5.2
2  Accept/reject/sửa discriminator test §5.3
3  Đi qua K-B1 → K-B7, chốt từng claim một
4  Chốt §5.7 (câu hỏi quyền xem) — hoặc ghi nhận là OPEN nếu chưa đủ thông tin
5  Stress-test bằng First Use Case OTA booking:
     - SOP "booking không về PMS"                  → Knowledge? Process? cả hai?
     - "Parser < v2.3 drop payload dạng X"         → Knowledge? Technical Finding?
     - Jira ES-123 đã fix cùng vấn đề              → Knowledge? Precedent?
     - Screenshot log khách gửi                    → Evidence
6  Stress-test bằng ba thực tế 10/30/60:
     - 10%  SOP có, tìm được         → Document → Knowledge?
     - 30%  SOP trong đầu người      → K-B7
     - 60%  SOP rải rác fragments    → trạng thái "gom từ nhiều nguồn, chưa duyệt"
7  Stress-test non-Jira: SOP .docx trên Drive + email hướng dẫn của senior
8  Stress-test vertical thứ hai: ví dụ CRM deal (xem §6.6)
9  Ghi kết quả vào docs/04_KNOWLEDGE_MODEL_V0.1.md §1, giữ nhãn + Open Questions
     → chỉ tạo file SAU khi Step 1 được người dùng chốt
10 Sang Step 2 (Knowledge Concepts & Granularity)
```

---

## 5.9 Bảy quyết định để đóng Step 1 — **TẤT CẢ ĐÃ `CONFIRMED 2026-08-21`**

> ✅ **S1–S7 đã được người dùng chốt, cộng thêm S8, K-B9, Q-B, Q-C.**
> Kết quả chính thức nằm ở **`docs/04_KNOWLEDGE_MODEL_V0.1.md` §1** — đó là source of truth.
> Phần dưới đây giữ nguyên **lập luận** dẫn tới từng quyết định (không rewrite history).
> Người dùng chốt **đúng phương án đề xuất** ở cả 9 câu.

Sinh ra từ phiên 2026-08-21 buổi 2, sau khi D6 được chốt.

### S1 — Guardrail phạm vi D6
Ghi xuống rằng D6 giới hạn ở *"gom theo yêu cầu"*; *"tự phát hiện chủ đề cần SOP"* là future capability, không phải MVP. → đã ghi ở §2.3, cần xác nhận là guardrail chính thức.

### S2 — Sửa định nghĩa Knowledge ở §5.2
Định nghĩa hiện tại đặt *"được tổ chức chấp nhận"* làm **điều kiện vào cửa**. Hệ quả: 60% fragment, bản nháp AI, email senior chưa duyệt — **tất cả nằm ngoài mô hình tri thức của chính sản phẩm**, và không có chỗ nào để đặt chúng.

Chẩn đoán: §5.2 đang định nghĩa **state `VERIFIED KNOWLEDGE`**, không phải **domain Knowledge**.

Đề xuất sửa:

> **Knowledge = một phát biểu tái sử dụng được, ở mức LỚP tình huống (không phải một case cụ thể), về việc nghiệp vụ vận hành thế nào hoặc một loại tình huống nên được xử lý thế nào — có applicability scope, có provenance, và có một mức verification (bao gồm cả mức "chưa ai duyệt"); tồn tại độc lập với bất kỳ Case cụ thể nào.**

Thay đổi duy nhất: *"được chấp nhận"* chuyển từ **điều kiện vào cửa** → **một trạng thái trên timeline**.

Hệ quả: trả lời luôn `Q-J` theo hướng **state**, đúng như K-B5 đã nghiêng, giữ được provenance liên tục draft→verified như G6 đòi.

### S3 — Sửa Discriminator Test §5.3
Hai lỗi kỹ thuật:

```text
T1 CASE-INDEPENDENCE  →  quyết định CÓ PHẢI Knowledge          (discriminator)
T2 CLASS-LEVEL        →  quyết định CÓ PHẢI Knowledge          (discriminator)
T3 ORG ACCEPTANCE     →  quyết định Ở TRẠNG THÁI NÀO           (không phải discriminator)
T4 DECISION VALUE     →  quyết định CÓ ĐÁNG ƯU TIÊN            (không phải discriminator)
```

- Câu *"Pass cả 4 → Knowledge"* **tự mâu thuẫn với T3** (T3 đã ghi "Chưa → Draft", tức vẫn trong model). Sửa thành **2 test biên giới + 2 phép phân loại**.
- T4 hiện ghi *"không persist"* → **vi phạm tinh thần G4 + G6**. Một ứng viên bị loại chính nó là dữ liệu: nó chỉ ra khoảng trống ở chủ đề đó, và nó là tín hiệu eval (AI đề xuất gì mà người từ chối). Sửa **"không persist"** → **"không PROMOTE"**. Từ chối là một **quyết định được ghi lại**, không phải một phép xóa.

### S4 — Kernel dùng chung cho ba domain
`CaseClaim` v0.2 **đã** có Origin + Verification + Evidence. Nên bốn chiều ở §5.4 không phải phát minh của Knowledge Model.

```text
              ┌──────────────────────────────────────────┐
              │  KERNEL DÙNG CHUNG                       │
              │  Origin · Evidence · Verification level  │
              │  Applicability · Visibility · Authority  │
              └──────────────────────────────────────────┘
                   ▲              ▲               ▲
              CaseClaim      KnowledgeRecord   ProcessDefinition
              (đã có v0.2)    (Step 1-5)        (Workstream 05)
```

**Danh sách bước có MỘT nhà duy nhất: Process domain**, ở state DRAFT khi mới được AI gom. Không có bản sao thứ hai.

Vì sao cần: đầu ra của D6 là một SOP = danh sách bước. Áp rule K-B6 thì **cái đầu tiên MVP tạo ra lại thuộc Process domain**, trong khi ta đang làm Knowledge Model. Kernel dùng chung làm mâu thuẫn đó biến mất thay vì phải chọn bên.

Ba cái lợi:
1. Không phải chọn giữa "nhận thua" và "bẻ K-B6".
2. **Giải luôn §6.9** — một bộ *verification level* dùng chung + *lifecycle state* riêng từng domain → `VERIFIED` thôi trùng nghĩa.
3. Knowledge Model v0.1 nhỏ lại đáng kể → đúng §6.7.

Cái giá: Knowledge Model v0.1 và Process Model v0.1 **không thể làm tuần tự hoàn toàn** nữa; chúng chia nhau một mối nối. Mối nối đó tồn tại thật, không phải do ta thiết kế ra.

Kỷ luật từ vựng đi kèm: *"SOP"* = tài liệu con người đọc (carrier); *"ProcessDefinition"* = thứ hệ thống dùng để dẫn từng bước. Nếu team cứ gọi cả hai là "SOP", hai domain sẽ lại nhập nhèm.

### S5 — `K-B8`: Capability 3 có hai nửa, ở hai domain khác nhau
Tài liệu đang gộp hai thứ có **kinh tế học hoàn toàn khác nhau**:

```text
PATH A — KÉO (pull), do người yêu cầu
  N case  →  1 bản nháp SOP/Knowledge
  Tần suất: mỗi chủ đề một lần
  Ngân sách chú ý: PHÚT — người ta chủ động xin, sẵn sàng bỏ công
  Domain đầu ra: Knowledge (+ Process nếu là danh sách bước)

PATH B — ĐẨY (push), hệ thống nhắc lúc đóng case
  1 case  →  một mảnh ghi nhận
  Tần suất: 500 lần
  Ngân sách chú ý: GIÂY — người ta không xin, đang muốn đóng case
  Domain đầu ra: KHÔNG PHẢI Knowledge
```

**Điểm then chốt: Path B không tạo ra Knowledge.** Nó chỉ làm dày hồ sơ Case (`CaseAction` / `CaseClaim` / `CaseOutcome` — v0.2 đã có sẵn hết), để **sau này Path A gom được**.

Nếu chấp nhận:
- **§6.4 hết mâu thuẫn.** Ràng buộc 20 giây áp cho Path B (xác nhận tóm tắt case của *chính mình*), không áp cho Path A (duyệt một SOP 9 bước tốn 30 phút là hợp lý — người ta chủ động xin nó).
- Knowledge domain **sạch**: chỉ chứa phát biểu class-level, không có sọt "fragment chưa xử lý".
- Nguy cơ *"Capability 3 chính là cái field `Version đang sử dụng` mặc áo đẹp hơn"* (§6.4) bị chặn đúng chỗ: cái field đó là Path B, và Path B giờ chỉ được phép hỏi những gì **đã có** trong case.

### S6 — Nạp tài liệu KHÔNG tự sinh KnowledgeRecord
`D5` đã trả lời gián tiếp: *"pipeline cắt chunk, template extraction"* nằm ở cột **GIÀN GIÁO TẠM**. Một pipeline bóc tách .docx thành KnowledgeRecord là đúng cái sẽ thành nợ khi model mạnh lên.

```text
nạp tài liệu     →  tạo Document (carrier) + nội dung đọc được
                    KHÔNG tự tạo KnowledgeRecord

KnowledgeRecord  →  chỉ sinh ra khi có một HÀNH VI KHẲNG ĐỊNH:
                    người viết ra, hoặc Path A gom rồi người duyệt
```

> **KnowledgeRecord lưu những gì tổ chức đã KHẲNG ĐỊNH, không lưu tất cả những gì tổ chức CÓ.** Phần "có" nằm ở Document.

Làm Knowledge Model nhỏ lại rất nhiều, và nhất quán với D5 thay vì chống lại D5.

### S7 — Quy tắc visibility cho MVP (thu hẹp `Q-D`, không giải)
`D2` đã đẩy ví dụ đáng sợ nhất của §5.7 (AI đọc private repo) sang **v2** → bớt phần nóng nhất. Nhưng vấn đề vẫn sống ở dạng nhẹ: một comment trong Jira project nội bộ mà Support không xem được, gom vào SOP → SOP đó ai xem được?

Quy tắc duy nhất đề xuất cho MVP:

```text
Mặc định:  visibility của tri thức tổng hợp = HẸP NHẤT trong các nguồn của nó
Mở rộng:   là một HÀNH VI TƯỜNG MINH của người thấy được TẤT CẢ nguồn
           ghi lại: ai mở, khi nào, mở từ đâu tới đâu
Không bao giờ: hệ thống tự mở
```

Cùng khuôn với `D4`, chỉ áp cho *quyền xem* thay vì *nội dung*.

Model chỉ cần ba chỗ chứa — quyết định hôm nay, rất rẻ:
```text
1. visibility của bản thân KnowledgeRecord
2. visibility của từng nguồn chống lưng nó
3. ai mở rộng, khi nào, lý do
```

Phần khó (bôi đen chọn lọc, tách kết luận khỏi dẫn chứng ở mức từng câu) → **v2**, `Q-D` giữ nguyên `OPEN`.

**⚠️ Hệ quả bất lợi phải nói thẳng:** với quy tắc "hẹp nhất", **60% sẽ tệ đi trước khi tốt lên**. Một SOP gom từ 20 case Jira, nếu có một comment nội bộ, sẽ **vô hình với đúng bạn support mới cần nó**.

→ Bước **mở quyền xem phải là một bước hiển thị rõ trong luồng Capability 3**, không phải một trang cấu hình admin nhớ ra sau. Nếu để sau, tính năng sẽ "chạy được" trong demo và "im lặng vô dụng" trong thực tế.

→ Ràng buộc về người duyệt:
```text
duyệt nội dung   →  cần người GIỎI NGHIỆP VỤ      (senior support / L3)
duyệt quyền xem  →  cần người THẤY ĐƯỢC MỌI NGUỒN
```
Hai người khác nhau → tắc. Ép một người không đủ quyền → rò rỉ. **Đề xuất MVP:** người duyệt phải là người thấy được tất cả nguồn; duyệt nội dung + quyền xem trong **một hành động**; log cả hai. Thu hẹp đáng kể `Q-G`.

---

### S8 — Cấu trúc bản nháp gom · `CONFIRMED`
Sinh ra từ stress-test 60% (§5.10). Bản nháp gom từ N case mang theo một **phân bố**, không phải một phát biểu → **evidence link ở mức TỪNG PHÁT BIỂU** + **`CONFLICTING` bắt buộc**. Chi tiết: `04_KNOWLEDGE_MODEL_V0.1.md` §1.11.

### K-B9 — Evidence trỏ trực tiếp vào Knowledge · `CONFIRMED`
Chi tiết: `04_KNOWLEDGE_MODEL_V0.1.md` §1.6.

---

## 5.10 Kết quả stress-test Step 1 — `CONFIRMED`, sinh ra 2026-08-21 buổi 2

### First Use Case — OTA booking không về PMS

| Vật thể | Phân loại | Ghi chú |
|---|---|---|
| SOP "booking không về PMS" | **Document** (carrier) + **ProcessDefinition** (phần bước) + **Knowledge** (phần không phải bước, vd *"không có incoming log → lỗi phía OTA"*) | Ba thứ, không phải một. Đúng như K-B6 dự đoán. Đây là câu trả lời đề xuất cho `Q-B`. |
| *"Parser < v2.3 drop payload dạng X"* | **Knowledge** — ví dụ sạch nhất | class-level ✓, case-independent ✓, có giá trị quyết định ✓. `applicability` = khoảng version. Cũng là vật thể có vấn đề quyền xem ở §5.7. |
| Jira ES-123 đã fix cùng vấn đề | **Historical Case** → precedent/evidence | Không phải Knowledge (K-B1). Là **nguyên liệu** của D6. |
| Screenshot log khách gửi | **Evidence**, machine readability = thấp | Đúng trạng thái `KNOWLEDGE_EXISTS_NOT_RETRIEVABLE` ở §6.3. |

SOP tách thành ba vật thể là **kết quả đúng**, không phải dấu hiệu model sai. Nhưng nó dẫn thẳng tới S6.

### Ba thực tế 10 / 30 / 60

- **10%** — xem `S6`. Tài liệu ở lại dạng Document; KnowledgeRecord chỉ sinh khi có hành vi khẳng định.
- **30%** — `K-B7` áp trực tiếp: chưa vào model. Đường vào **duy nhất** khả thi là **Path B** (`S5`). Nên §6.4 không phải lời khuyên UX — **nó là điều kiện để 30% tồn tại trong sản phẩm.**
- **60%** — **kết quả tốt: không cần entity mới nào.**

```text
comment Jira, action, outcome  →  v0.2 đã có (CaseClaim/CaseAction/CaseOutcome/Evidence)
email, ghi chú Zalo rời        →  Document/SourceReference → Evidence   (cần K-B9)
"gom lại"                      →  không phải entity, mà là một TRUY VẤN + một HÀNH VI
```

60% không đòi thêm concept. Nó đòi **một truy vấn tìm case liên quan** + **một hành vi tổng hợp**. Đầu tư vào Case v0.2 trả cổ tức lần nữa.

### ⚠️ Chỗ đang bị bỏ sót — quan trọng

Một bản nháp gom từ 20 case **khác về bản chất** với bản nháp viết từ 1 case. Bản gom mang theo một **phân bố**, không phải một phát biểu:

```text
bước "kiểm tra room mapping"        →  14/20 case đã làm
bước "gọi OTA trước khi check log"  →  6/20 làm, 8/20 làm ngược lại    ← XUNG ĐỘT
```

Nếu bản nháp được lưu như một khối văn bản với danh sách nguồn ở cuối, **ta ném đi đúng thứ giá trị nhất**: bước nào được bao nhiêu case chống lưng, và chỗ nào các case **không đồng ý với nhau**.

Chỗ không đồng ý chính là chỗ người duyệt cần nhìn — nó cho phép duyệt trong 10 phút thay vì 2 giờ, vì họ chỉ phán xét mấy điểm tranh chấp; phần còn lại đã có 14/20 đồng thuận.

→ **Knowledge/Process draft cần giữ evidence link ở mức TỪNG PHÁT BIỂU, không phải ở mức tài liệu.**
→ **Trạng thái `CONFLICTING` là BẮT BUỘC, không phải tùy chọn** — gom N case thì xung đột là chuyện thường ngày. (`CONFLICTING` đã có trong Case v0.2 §7.3, **thiếu** trong `PROJECT_CONTEXT` §13.4 — xem §6.9.)

Điều này cũng trả lời câu §4 đặt ra (*"nếu CÓ thì Step 1 phải định nghĩa rõ trạng thái tri thức gom từ nhiều nguồn rời, chưa ai duyệt"*):

```text
trạng thái đó = state DRAFT
              + origin AI_INFERENCE
              + evidence link theo từng phát biểu
              + có thể CONFLICTING
→ KHÔNG cần entity mới.
```

### Non-Jira: SOP .docx trên Drive + email hướng dẫn của senior

- **.docx** → Document (dạng A), có version + ACL riêng. Không tự thành Knowledge (`S6`).
- **Email của senior** → ca thú vị nhất. Phát biểu class-level do **một con người có chuyên môn** đưa ra. **Không** phải AI draft. Cũng **không** được tổ chức duyệt.

Ca này chứng minh chiều **Authority** (§5.4) kiếm được chỗ đứng: cần phân biệt

```text
"một chuyên gia nói câu này một lần trong email"
        vs
"tổ chức đã review và công bố"
```

Một trục `DRAFT / VERIFIED` không diễn đạt được hai thứ này → củng cố `S4`: **verification level** (mức tin) phải tách khỏi **lifecycle state** (mức công bố). Đúng vấn đề §6.9.

### Vertical thứ hai: CRM deal

Rule *"khách ở Negotiation im hơn 7 ngày → gửi case study cùng ngành"*: có điều kiện + có hành động, nhưng **không có thứ tự bước** và **không theo dõi "đang ở bước nào"** → **KNOWLEDGE**. Rule K-B6 chạy đúng, không phải bẻ gì.

Hai điều học được:
1. Ở vertical sales, Knowledge phần lớn là **khuyến nghị có điều kiện** (*nếu X thì làm Y*), không phải **giải thích cơ chế** (*parser drop payload*). Đây là loại **dễ lẫn với Process nhất** → rule K-B6 sẽ bị vắt kiệt ở đây, không phải ở support. Nhớ khi làm Step 2 (Knowledge types).
2. Rule đó suy ra từ các deal won/lost → **đúng cơ chế D6, khác tên**. D6 phổ quát, không đặc thù support.

### `K-B9` mới — Evidence phải trỏ trực tiếp vào Knowledge được, không qua Case — `PROPOSED`

Lỗ hiện tại: v0.2 §9 định nghĩa Evidence hỗ trợ *Claim / Problem / Action / Event* — đều scoped vào Case. §11.2 nói Case *"contributes evidence toward Knowledge"* — qua trung gian Case.

Nhưng một email của senior, hay một tin Zalo, **không thuộc case nào**. Với 60% là fragment rải rác, đây không phải trường hợp hiếm.

Đáng mừng: v0.2 §9 **đã** viết *"Một EvidenceItem có thể liên quan nhiều Case"* và *"Không mặc định 1 Evidence = owned exclusively by 1 Case"*. Nên đây là **mở rộng nhỏ, an toàn**, không phải bẻ model: cho phép `Evidence → SUPPORT/REFUTE → Knowledge` trực tiếp.

---

## 5.11 Cảnh báo về thứ tự — §8.1 nặng hơn trước

§8.1 nói: hỏi 2 người xem SOP thật nằm ở đâu, 30 phút, giá trị cao nhất.

Với D6 vừa chốt, câu đó **nặng hơn**. Vì D6 nói: sản phẩm sẽ **tự sinh ra** SOP đầu tiên. Nếu không biết một SOP thật trông thế nào (mấy bước? có rẽ nhánh? ai viết? cập nhật lần cuối bao giờ?), thì ta đang thiết kế **cỗ máy sản xuất một thứ chưa từng nhìn thấy**.

30 phút của §8.1 sẽ **kiểm chứng hoặc đánh sập `S4`** — phần đề xuất thay đổi nhiều nhất và cũng là phần ít tự tin nhất.

---

# 6. Phát hiện / phản biện KHÔNG ĐƯỢC MẤT

## 6.1 Thiếu tài liệu Product Foundation v1 — `chưa xử lý`
Xem §1. Mất theo: capability contract, non-goals, và **Success Metrics**. Không biết "thành công" là gì thì không có điều kiện dừng cho bất kỳ workstream nào. Cần tìm lại hoặc dựng lại + chốt lại thước đo.

## 6.2 Bất đối xứng evidence — `cần lưu ý khi thiết kế`
```text
P2 KNOWLEDGE_NOT_CAPTURED   → dataset-supported mạnh (306/500 thiếu action steps; ~45% NOT_REUSABLE)
                              → nhưng phục vụ Capability #3

P1 KNOWLEDGE_NOT_DISCOVERED → chỉ dựa trên MỘT anecdote (Traveloka)
                              → nhưng là nền của Capability #1 + #2
```
Round 3 kết luận: *"Knowledge reuse không consistently observable/measurable từ Jira records"* → **không có baseline định lượng cho P1**.

Hệ quả thiết kế: **đừng giả định tồn tại corpus SOP lớn và chất lượng.** Con số 10% ở §3 đã xác nhận điều này. → `applicability` và `coverage/gap` cần là first-class sớm.

## 6.3 Cần BA trạng thái tri thức, không phải hai — `PROPOSED`
Round 3 **cố tình không OCR/download attachments** → tri thức có thể **đã tồn tại** trong screenshot mà phép đo đếm là "thiếu".
```text
KNOWLEDGE_ABSENT                    không ai biết
KNOWLEDGE_EXISTS_NOT_RETRIEVABLE    có, nhưng trong ảnh chưa OCR / cuộc gọi / đầu người
KNOWLEDGE_EXISTS_RETRIEVABLE        có và tìm được
```
Giá trị sản phẩm chính là chuyển trạng thái **2 → 3**. Nếu model chỉ có absent/exists thì **không đo được đúng thứ mình đang bán**. Metadata `Machine Readability` đã có trong Evidence model v0.2 §9 — dùng lại được. Con số 30% (trong đầu người) chính là trạng thái 2.

## 6.4 Chi phí capture phải gần bằng 0 — `PROPOSED`, ràng buộc thiết kế cứng
Dữ liệu nói: 306/500 case không ghi bước xử lý → **người ta đã không chịu ghi**. Và field `Version đang sử dụng` trống **100/100** vì là việc thêm không thấy lợi.

→ **Capability 3 có nguy cơ là đúng cái field đó, mặc áo đẹp hơn.**

Ràng buộc đề nghị: AI soạn từ những gì **đã có sẵn trong case** (actions, evidence, timeline, outcome); người chỉ confirm/reject/sửa một dòng. **Nếu cần hơn ~20 giây chú ý của người dùng, nó sẽ rỗng giống cái field kia.**

## 6.5 Process Guidance phổ quát hơn Knowledge Retrieval — `quan sát chiến lược`
Ví dụ CRM của người dùng thực chất là **~90% Capability 2**, gần như không cần Capability 1. Case support thì cần cả hai.
→ Process Guidance là capability phổ quát hơn; Retrieval mang tính đặc thù support. Đáng cân nhắc khi xếp thứ tự MVP, vì muốn bán nhiều ngành thì cái đi trước nên dùng được ở nhiều ngành. Nhất quán với D2.

## 6.6 Canonical Case v0.2 đã chịu được vertical thứ hai — `đã kiểm chứng`
Ép ví dụ CRM vào model, vừa khít không phải bẻ gì:
```text
Case              = Deal #4471 với khách hàng X
Subject           = chốt hợp đồng          (không có CaseProblem — hợp lệ)
Origination       = nhập từ CRM
CurrentState      = stage "Negotiation"
CaseEvent         = STAGE_CHANGED
OwnershipSegment  = sales rep A
CaseAction        = gọi khách, gửi báo giá
CaseOutcome       = WON / LOST
ProcessRun        = sales playbook
Knowledge         = "khách ở Negotiation im hơn 7 ngày → gửi case study cùng ngành"
```
**Đây là bằng chứng khoản đầu tư vào domain model đã có lãi.** Nếu ngày đó model dính vào field Jira thì bây giờ phải làm lại từ đầu. Dùng ví dụ này khi cần thuyết phục về giá trị của domain-first.

## 6.7 Rủi ro lớn nhất của dự án: chết vì modeling — `phản biện về phương pháp`
Cách domain-first là **đúng**, và Case v0.2 chất lượng cao được **vì có 700 case thật để đối chiếu**.

Knowledge Model **không có dataset tương đương**. Process Model cũng không.

> Đào sâu mà không có dữ liệu đối chiếu thì không phải rigor — đó là **đoán một cách cẩn thận**.

Repo có guardrail chống scope explosion (§12) và chống premature architecture (§10), nhưng **không có guardrail chống premature modeling**, và không có điều kiện dừng. Bằng chứng rủi ro này đã hiện hữu: tài liệu 02 mất mà không ai phát hiện → tốc độ sản xuất tài liệu đã vượt tốc độ sử dụng tài liệu.

Failure mode không phải "làm sai thứ" mà là **"không bao giờ làm ra thứ gì"**.

**Đề nghị:** Knowledge Model v0.1 + Process Model v0.1 chốt trong **~2 tuần**, ở mức "vừa đủ để build được first use case". Quy tắc: câu hỏi nào **không chặn** việc build thì ghi vào Open Questions và đi tiếp. Hai model này **không cần** sâu bằng Case v0.2.

## 6.8 Đồng hồ cạnh tranh — `cân nhắc chiến lược`
Lớp "chĩa AI vào ticket và tài liệu" đang bị thương mại hóa nhanh. Tài liệu nhận định đúng rằng phần phòng thủ được là **process state + outcome + provenance** (PROJECT_CONTEXT §19), không phải retrieval. Nhưng phần đó chỉ có giá trị nếu **đi tới được**. Nhất quán với D5 hệ quả 3.

## 6.9 Hai vocabulary verification song song — `cần giải quyết ở Step 5`
```text
PROJECT_CONTEXT §13.4 (claim ladder):    SPECULATIVE PLAUSIBLE SUPPORTED VERIFIED INVALIDATED
Canonical Case v0.2 §7.3:                + CONFLICTING
PROJECT_CONTEXT §8.3 (knowledge health): DRAFT VERIFIED ACTIVE NEEDS_REVIEW DEPRECATED SUPERSEDED
```
`VERIFIED` xuất hiện ở cả hai với nghĩa khác nhau (mức xác minh của claim vs state publication). Sẽ thành contradiction nếu Knowledge Model không tách rõ **verification level** và **lifecycle state**.

## 6.10 Tài liệu cũ có phần đã lỗi thời — `housekeeping`
- `PROJECT_CONTEXT.md` §14.2 mô tả CanonicalCase root gồm `Intake`/`TriageState`/`ReproductionState`/`WaitingState` — **v0.2 đã loại bỏ có chủ đích** (guardrail R7). Nên đánh dấu §14.2 là SUPERSEDED.
- `PROJECT_CONTEXT.md` §16 tự nói *"Exact 3 MVP capabilities chưa được formally locked"* — trái AGENT.md §4 (`CONFIRMED`). Cùng nguyên nhân: doc 02 đã lock nhưng PROJECT_CONTEXT chưa cập nhật ngược.
- `NEXT_CONVERSATION_PROMPT (1).md` là input prompt, chứa Capability #3 bản cũ → nên archive để agent tương lai không đọc nhầm.
- Tên file không khớp convention `01_`/`02_`/`03_` trong AGENT.md §1.

---

# 7. Câu trả lời connector — ở mức khái niệm

Người dùng hỏi: *"Với nhiều nguồn tri thức, có phải viết service riêng cho từng loại không?"*

⚠️ Phần này là **câu trả lời khái niệm**, không phải quyết định kiến trúc. Kiến trúc thật thuộc workstream `06 — MVP Architecture`.

## Trả lời: Có — nhưng chỉ một lớp rất mỏng

Sai lầm phổ biến: viết N pipeline hoàn chỉnh cho N nguồn. Hình dạng đúng: **nhiều adapter mỏng + một pipeline dùng chung.**

```text
ADAPTER RIÊNG từng nguồn — mỏng, KHÔNG có logic nghiệp vụ
  · kết nối & xác thực
  · liệt kê và lấy item
  · phát hiện thay đổi (cursor / webhook / commit SHA)
  · đọc quyền gốc của nguồn
        ↓  phát ra: dữ liệu thô + tham chiếu nguồn + quyền gốc
        ↓
PIPELINE DÙNG CHUNG — viết một lần, mọi nguồn đi qua
  · trích text theo format (PDF/DOCX/HTML/MD)
  · phát hiện & che dữ liệu nhạy cảm
  · gắn provenance
  · chuẩn hóa quyền gốc → tenant + visibility của mình
  · map vào khái niệm canonical
  · quản version & thay đổi
        ↓
Knowledge / Evidence / Case — không biết dữ liệu đến từ nguồn nào
```

## Kỷ luật quan trọng nhất: adapter không ra quyết định về ý nghĩa

Adapter nói *"Jira status đổi thành 'Escalated to L3'"*. Nó **không** được nói *"nghĩa là ENTERED_TECHNICAL_QUEUE"*. Việc dịch nghĩa thuộc lớp canonical, phải **lưu lại được và dịch lại được** nếu sau phát hiện sai.

Đây đã là guardrail R5 trong Canonical Case v0.2: *"Connector có thể map source wording sai semantic meaning… Raw source observations phải được giữ để audit/remap."* → **Domain model đã trả lời trước câu hỏi kiến trúc này.**

## Không phải N nguồn, mà ~5 DẠNG nguồn

| Dạng | Nguồn ví dụ | Đặc điểm phải xử lý |
|---|---|---|
| **A** Tài liệu tĩnh có phân quyền | Drive, SharePoint, Confluence, khách upload | version, ACL, ít thay đổi |
| **B** Bản ghi công việc có timeline | Jira, CRM, helpdesk, ERP | chuyển trạng thái, comment, attachment |
| **C** Luồng hội thoại | email, Teams, Slack, Zalo | theo thread, khó xác định ranh giới, quyền riêng tư |
| **D** Mã nguồn & lịch sử | Git, PR, release notes | rất lớn, quyền theo repo |
| **E** Tín hiệu hệ thống | log, monitoring, API | volume lớn, giá trị theo thời điểm |

Chi phí biên rất lệch:
```text
Nguồn đầu tiên của một DẠNG MỚI   →  đắt (phải làm cả phần chung cho dạng đó)
Nguồn thứ hai CÙNG DẠNG            →  ngày, không phải tuần
```
Làm Jira (dạng B) xong thì thêm HubSpot/Freshdesk rẻ hẳn. Nhảy sang email (dạng C) gần như bắt đầu lại.

## Về MCP
MCP là chuẩn đáng dùng cho bài toán kết nối nhiều nguồn, đã có server cho nhiều tool phổ biến → tiết kiệm phần adapter.

Nhưng rõ ràng về giới hạn: **MCP cho *đường vào*, không cho *pipeline canonical*.** Provenance, chuẩn hóa quyền, phát hiện thay đổi, quản version vẫn phải tự làm. Và MCP thiên về gọi theo yêu cầu hơn đồng bộ hàng loạt → thực tế cần cả hai đường: MCP cho tra cứu tương tác, đường đồng bộ riêng cho corpus cần index.

## MVP nên có 2 nguồn — `PROPOSED`
1. **Jira** (dạng B) — 60% fragments ở đó, vừa là nguồn tri thức vừa là nguồn Case
2. **Tải tài liệu lên** (dạng A, đơn giản nhất) — 10% đã có tài liệu, và tính năng "đề nghị bổ sung SOP" cần đường nhận tài liệu

Email (dạng C) messy nhất → v2. Code (dạng D) ấn tượng nhất và nhạy cảm quyền nhất → dứt khoát v2 (nhất quán với D2).

## Điểm nối quan trọng
Tài liệu Drive, comment Jira, email và file code là **bốn loại vật chứa với bốn mô hình quyền khác nhau**. Tri thức rút ra từ chúng buộc phải mang theo: từ vật chứa nào, quyền gì, còn mới hay cũ. Khi một tri thức được chống bởi dẫn chứng từ nhiều vật chứa có quyền xem khác nhau → quay lại đúng §5.7.

→ **Câu hỏi connector và câu hỏi Step 1 là cùng một câu hỏi boundary, chỉ đứng ở phía đầu vào.**

---

# 8. Việc ngoài thiết kế — nên làm song song

## 8.1 Đi tìm SOP thật — 30 phút, giá trị cao nhất

> ⛔ **CHẶN STEP 2** — `CONFIRMED 2026-08-21`. Người dùng quyết: chạy §8.1 **trước** Step 2.
> Lý do: Step 2 là Knowledge Concepts & Granularity, và câu *"một KnowledgeRecord to bằng nào"*
> gần như không trả lời đúng được nếu chưa thấy một SOP thật. Xem `04` §6 R-K2.
Hỏi 2 người: bạn support kỳ cựu nhất, và người xử lý case OTA gần nhất — *"khi gặp case này, anh/chị làm theo cái gì?"*

Cần lấy về: SOP (nếu có) nằm ở đâu, format gì, bao nhiêu bước, có rẽ nhánh không, cập nhật lần cuối bao giờ.

Kết quả dùng để: stress-test Step 1 bằng tri thức thật thay vì ví dụ tự nghĩ. **Không thể thiết kế cái hộp trước khi biết bên trong đựng gì.**

---

# 8.1-KQ. KẾT QUẢ §8.1 — SOP THẬT · `EVIDENCE-SUPPORTED 2026-08-21`

## Nguyên văn người dùng cung cấp

> *"Mọi người lấy dữ liệu ở Kibana rồi xem response trả về sau đó xem tài liệu và issue xử lý trước đó để đưa ra kết luận"*

**Giữ nguyên văn. Mọi thứ dưới đây là phân tích, nhãn `PROPOSED`, chưa được xác nhận.**

## Cấu trúc suy ra

```text
B1  lấy dữ liệu ở Kibana            → source type E (tín hiệu hệ thống)
B2  xem response trả về             → source type E
B3  xem tài liệu                    → source type A   ("tài liệu gì" — chưa rõ)
B4  xem issue xử lý trước đó        → source type B (historical case)
B5  đưa ra kết luận                 → PHÁN XÉT CỦA CON NGƯỜI
```

## Ba điều được kiểm chứng

### (1) `S4` sống — và ranh giới rơi đúng chỗ tự nhiên

B1–B4 có per-case execution state (*"đã lấy Kibana chưa?"* trả lời được) → **Process**, đúng rule K-B6.
B5 là phán xét, không phải bước quan sát được → **Knowledge**.

Ranh giới Knowledge/Process rơi đúng vào khe giữa **GOM** và **KẾT LUẬN**. Sạch hơn dự kiến.

### (2) `Q-C` được xác nhận mạnh hơn mức đã chốt

*"Xem issue xử lý trước đó"* là **B4 của quy trình thật**. Nghĩa là "tìm case cũ tương tự" không phải nice-to-have, cũng không chỉ là dependency của Capability 3 — **nó là một bước của quy trình mà con người đang làm hằng ngày**.

### (3) `S5` Path A nhắm đúng đích — xem §8.1-KQ mục "Tinh chỉnh 10/30/60"

## Ba điều bị thay đổi

### (A) SOP thật KHÔNG có nhánh — trái với SOP tưởng tượng trong tài liệu

```text
PROJECT_CONTEXT §5.3 (tưởng tượng)      SOP thật (người dùng cung cấp)
────────────────────────────────────    ──────────────────────────────
Check booking exists                    B1  Kibana
→ Check room mapping                    B2  response
→ Check rate mapping                    B3  tài liệu
→ Check incoming log                    B4  issue cũ
→ No log        → contact OTA           B5  kết luận
→ Has log fail  → Technical
CÓ ĐIỀU KIỆN, CÓ NHÁNH                  TUYẾN TÍNH, KHÔNG NHÁNH
= decision procedure                    = evidence checklist + phán xét
```

Hai hình dạng khác nhau về bản chất. **`PROJECT_CONTEXT` §5.3 nên bị đánh dấu là ví dụ minh hoạ, không phải SOP thật.** → thêm mục housekeeping `H-7`.

### (B) Toàn bộ giá trị nằm ở B5 — bước duy nhất không ai ghi lại

```text
B1-B4  gom bằng chứng    →  làm một lần là biết. Giá trị bão hoà sau tuần đầu.
B5     kết luận          →  giá trị vĩnh viễn. KHÔNG AI GHI LẠI.
```

Đây chính là thứ mà con số **306/500 case không ghi bước xử lý** (§10.5 PROJECT_CONTEXT) đang đo. Cái thiếu không phải *quy trình*, mà là *luật kết luận*.

Hệ quả cho Primary Persona (`New Support Employee`):

```text
Capability 2 trên B1-B4  →  có giá trị TUẦN ĐẦU, rồi bão hoà
Capability 1 + luật B5   →  giá trị lâu dài, và là thứ quyết định
                            có escalate lên Technical oan hay không (P1)
```

⚠️ Đây là **phản biện với §6.5** (*"Process Guidance phổ quát hơn Knowledge Retrieval"*). §6.5 vẫn đúng về **độ phổ quát giữa các ngành**, nhưng với **first use case** thì Process Guidance đơn thuần gần như rỗng: dẫn người ta qua 4 bước hiển nhiên không giải được vấn đề. Không mâu thuẫn, nhưng phải nói rõ để không kỳ vọng sai vào Capability 2.

### (C) Tinh chỉnh 10/30/60 — cái thiếu là tri thức KẾT LUẬN, không phải tri thức QUY TRÌNH

```text
10%  có tài liệu, tìm được   →  tài liệu về HỆ THỐNG (API, field, behavior)
                                 = B3, và nó KHÔNG chứa luật kết luận
30%  trong đầu người         →  luật KẾT LUẬN
60%  rải rác fragments       →  luật KẾT LUẬN, dưới dạng dấu vết trong case cũ
```

→ 90% đang thiếu **gần như toàn bộ là conclusion knowledge.** Path A (gom N case) nhắm đúng đích.

→ **Câu trả lời sơ bộ cho `N-3` (granularity, Step 2):**

```text
Một KnowledgeRecord  ≈  MỘT LUẬT KẾT LUẬN
   "thấy tín hiệu X trong log + response Y  →  nguyên nhân Z  →  làm W"

KHÔNG phải "một SOP".  KHÔNG phải "một tài liệu".
```

Đây là thứ §8.1 lẽ ra phải sinh ra, và nó đã sinh ra.

### (D) Hai bước đầu thuộc source type E — nguồn KHÔNG có trong 2 nguồn MVP đã đề xuất

`§7` đề xuất MVP dùng 2 nguồn: **Jira (B)** + **tải tài liệu lên (A)**. Type E (log/monitoring/API) không nằm trong đó.

Nhưng **B1 và B2 của quy trình thật đều là type E.**

```text
B1 Kibana         → type E
B2 response       → type E
B3 tài liệu       → type A   ✓ có trong MVP
B4 issue cũ       → type B   ✓ có trong MVP
```

Đây là vấn đề phạm vi MVP thật, `OPEN`. Ba hướng, **chưa chọn**:

```text
(a) MVP không đọc Kibana — AI hướng dẫn người tự lấy, người dán kết quả vào.
    AI làm B3+B4+B5. Rẻ nhất, và B5 vẫn là phần giá trị nhất.
(b) Thêm Kibana thành nguồn thứ 3 — phình scope; type E có volume lớn,
    giá trị theo thời điểm.
(c) Query Kibana THEO YÊU CẦU cho một booking ID cụ thể, không index corpus log.
    Khác hoàn toàn về chi phí so với (b). §7 đã nói MCP phù hợp đúng dạng này.
```

⚠️ Không quyết ở đây. Đây là kiến trúc → Workstream 06, và `AGENT.md` §10.1 nói chốt công nghệ là quyền người dùng.

**Điều đáng mừng:** về mặt **domain model** thì Kibana không đòi gì mới. Kết quả query Kibana là `EvidenceItem` + `SourceReference`, `Origin = SYSTEM_FACT` (có phạm vi, v0.2 §7.6), machine readability cao. Canonical Case v0.2 đã phủ được. **Câu hỏi là phạm vi MVP, không phải mô hình.**

## Ba câu bổ sung — trả lời 2026-08-21

```text
Có tài liệu viết?    KHÔNG có SOP xử lý. CÓ tài liệu hệ thống (API/field/behavior).
Tập kết luận?        HỮU HẠN NHỎ — khoảng 5-10 loại nguyên nhân.
Hỏi mấy người?       1 người.   ⚠ n=1, không suy rộng.
```

### Hệ quả (1) — first use case nằm trọn trong 30% + 60%

```text
10%  tài liệu HỆ THỐNG có     →  đó là B3, và nó KHÔNG chứa luật kết luận
30%  luật kết luận trong đầu người
60%  luật kết luận rải rác trong case cũ
```

Phân tích ở mục (C) được xác nhận: **cái thiếu là conclusion knowledge, không phải procedure knowledge.**

⚠️ **Hệ quả về thứ tự MVP:** Capability 1 ngày đầu **không có SOP nào để retrieve** cho chính use case dùng để demo. Nó chỉ retrieve được tài liệu hệ thống — thứ trả lời *"field này là gì"*, không trả lời *"case này bị gì"*. → củng cố thứ tự vòng lặp `PROPOSED` ở §3 (Path A trước).

### Hệ quả (2) — "5-10 nguyên nhân" là dữ liệu quan trọng nhất của phiên này

**(a) Kho tri thức của first use case rất nhỏ.**

```text
~5-10 nguyên nhân  ≈  ~5-10 KnowledgeRecord cho toàn bộ first use case
```

Không phải 500. Xây và duyệt xong trong một ngày.

**(b) Ở quy mô này, Capability 1 KHÔNG phải bài toán retrieval.**

Với 10 record thì không cần vector DB, không cần RAG, không cần chunking. Bài toán thật là:

```text
KHÔNG PHẢI  "tìm tài liệu nào liên quan"
MÀ LÀ       "bằng chứng của case này khớp với nguyên nhân nào trong 10 cái đã biết"
            → đây là bài toán PHÂN LOẠI, không phải tìm kiếm
```

Nhất quán với `D5`: chunking/RAG pipeline nằm ở cột **giàn giáo tạm**. Ở quy mô 10 record thì dựng chúng lên là tự tạo nợ mà không được gì.

⚠️ Đây **không** phải đề nghị đổi Capability #1 (giữ G9). Capability #1 vẫn là `Contextual Knowledge Retrieval`. Chỉ là: ở MVP, cách hiện thực đúng của nó là matching/classification, không phải semantic search.

**(c) Bộ eval trở nên rẻ và định lượng được.**

Tập nhãn hữu hạn + case lịch sử đã có outcome = bài toán phân loại có đáp án. Đây là `D5 hệ quả 1` trở thành cụ thể, không còn là nguyên tắc.

**(d) Lần đầu có ứng viên cho `Q-E` (Success Metrics — mất cùng tài liệu 02).**

Với tập nguyên nhân hữu hạn, ba thước đo sau đo được:

```text
· % case hệ thống chỉ ĐÚNG nguyên nhân
· % case escalate lên Technical mà đáng ra không cần    (đo trực tiếp P1)
· độ phủ: đã có luật cho bao nhiêu / tổng số nguyên nhân đã biết
```

`Q-E` vẫn `OPEN` — đây là ứng viên, không phải quyết định. Nhưng trước đó ta **không có cách nào** đo, giờ có.

**(e) Nếu chỉ có 5-10 nguyên nhân thì cùng một đáp án đã bị tìm lại rất nhiều lần.**
Khớp với `REUSE_OPPORTUNITY_MISSED` và `KNOWLEDGE_WAS_NOT_CAPTURED` (§10.7). Đây là câu nói mạnh nhất cho pitch — nhưng cần §8.2 đếm để định lượng, đừng dùng khi chưa có số.

### Cảnh báo về độ tin cậy — `n=1`

Con số 5-10 là **ước lượng của một người có kinh nghiệm**. Người kinh nghiệm nén thực tế lại; người mới thấy hỗn loạn. Hai rủi ro:

```text
· con số thật có thể lớn hơn (người kinh nghiệm gộp nhóm vô thức)
· không biết độ biến thiên giữa người với người → không biết có PROCESS_DRIFT (P7) hay không
```

→ **§8.2 (đếm 20 case OTA gần nhất) giờ là việc có giá trị cao nhất còn lại.** Nó xác nhận hoặc bác bỏ cả (a), (c), (d).

## Còn thiếu so với yêu cầu của §8.1

§8.1 yêu cầu lấy về: SOP nằm ở đâu, **format gì**, bao nhiêu bước, **có rẽ nhánh không**, **cập nhật lần cuối bao giờ**.

```text
✓ bao nhiêu bước       5
✓ có rẽ nhánh không    không, tuyến tính
✓ có tài liệu VIẾT nào không?     KHÔNG có SOP xử lý; CÓ tài liệu hệ thống
✓ "tài liệu" ở B3 là gì?          tài liệu hệ thống (API/field/behavior)
✓ hỏi mấy người?                  1 người  ⚠ n=1
✗ cập nhật lần cuối bao giờ?      chưa hỏi — ít quan trọng vì không có SOP viết
```

⚠️ **ĐÃ XÁC NHẬN: không có SOP viết.** Capability 1 ngày đầu chỉ retrieve được tài liệu hệ thống — thứ trả lời *"field này là gì"*, không trả lời *"case này bị gì"*. Đó là một sự thật về thứ tự MVP, không phải chi tiết.

## 8.2 Đo thử vấn đề A — vài giờ
Hỏi 5 bạn support mới: *"bạn có biết tài liệu X tồn tại không?"* và *"lần cuối gặp case này bạn làm gì?"*
Đếm trong 20 case OTA gần nhất: bao nhiêu case escalate lên Technical mà SOP đáng ra đã đủ?
→ Ra con số để bảo vệ quyết định MVP. Hiện P1 chỉ có 1 anecdote (§6.2).

## 8.3 Khôi phục Success Metrics
Xem §6.1. Không có thước đo thì không có điều kiện dừng.

## 8.4 Bộ eval từ MVP — dù chỉ 20 case gán nhãn tay
Xem D5 hệ quả 1. Đây là cơ chế biến "model mạnh lên" thành "phần mềm mạnh lên".

---

# 9. Open Questions còn mở

## Chặn Step 1
```text
Q-A  RESOLVED 2026-08-21 → CÓ, phiên bản "gom theo yêu cầu".      Xem D6 §2.3
Q-B  RESOLVED → SOP = Document + ProcessDefinition + Knowledge (BA vật thể)
Q-C  RESOLVED → CÓ. Cap 1 trả HAI loại kết quả có nhãn tách biệt:
                KnowledgeRecord và Historical Case
Q-D  Phần dễ RESOLVED (quy tắc visibility MVP, xem S7 / 04 §1.10)
     Phần khó vẫn OPEN → v2: tách kết luận/dẫn chứng mức từng câu, redaction
```

> ✅ **Step 1 đã đóng.** Toàn bộ Q-A/Q-B/Q-C + S1-S8 + K-B9 nằm ở
> `docs/04_KNOWLEDGE_MODEL_V0.1.md` §4 Decision Register.

## Chín quyết định đóng Step 1 — tất cả `CONFIRMED 2026-08-21`
```text
S1  Guardrail phạm vi D6 → nâng thành G11 (AGENT.md §3.8)
S2  Định nghĩa Knowledge: "được chấp nhận" là TRẠNG THÁI  → Q-J trả lời: state
S3  2 discriminator + 2 phép phân loại; T4 = "không PROMOTE"
S4  Kernel dùng chung 3 domain; bước có MỘT nhà = Process; verification ≠ lifecycle
S5  K-B8: Path A tạo Knowledge, Path B chỉ làm dày Case
S6  Nạp tài liệu KHÔNG tự sinh KnowledgeRecord
S7  Visibility "hẹp nhất + mở rộng tường minh", bước mở quyền TRONG luồng Cap 3
S8  Bản nháp gom: evidence link TỪNG PHÁT BIỂU + CONFLICTING bắt buộc
K-B9 Evidence trỏ trực tiếp vào Knowledge, không qua Case

Source of truth: docs/04_KNOWLEDGE_MODEL_V0.1.md §1 + §3
```

## Sinh ra từ Step 1 — cần Step 2-5
```text
N-1  Vocabulary: verification level vs lifecycle state (khóa riêng)  → Step 5
N-2  Knowledge types taxonomy                                        → Step 2
N-3  Granularity: một KnowledgeRecord to bằng nào?                   → Step 2
N-4  Knowledge ↔ Knowledge (supersedes/refines/contradicts)          → Step 3
N-5  Applicability biểu diễn thế nào                                 → Step 4
```

## Housekeeping ghi ngược vào tài liệu cũ
```text
H-1  ✅ ĐÃ LÀM 2026-08-21 — PROJECT_CONTEXT §13.4 đã thêm CONFLICTING
                            + ghi rõ ladder này là verification level,
                              không phải lifecycle state ở §8.3
H-2  ✅ ĐÃ LÀM 2026-08-21 — Case v0.2 §11.2 đã thêm đường
                            Evidence → Knowledge trực tiếp (K-B9)

H-3  ⚪ PROJECT_CONTEXT §5.2: "senior memory" phải ghi là knowledge SOURCE (K-B7)
H-4  ⚪ PROJECT_CONTEXT §14.2 SUPERSEDED bởi Case v0.2 (R7)
H-5  ⚪ PROJECT_CONTEXT §16 nói "chưa formally locked" → trái AGENT.md §4
H-6  ⚪ NEXT_CONVERSATION_PROMPT (1).md nên archive
H-7  ⚪ MỚI — PROJECT_CONTEXT §5.3 mô tả SOP OTA CÓ NHÁNH (check booking →
     mapping → log → rẽ nhánh). SOP thật (§8.1-KQ) TUYẾN TÍNH, không nhánh.
     → phải đánh dấu §5.3 là VÍ DỤ MINH HOẠ, không phải SOP thật.
```
H-1/H-2 làm ngay vì là **contradiction thật** do S8/K-B9 sinh ra.
H-3..H-6 chỉ là nhãn lỗi thời, không gây sai → gộp một lần cuối workstream 04.

## Cần trước Step 2–3
```text
Q-E  Success Metrics của MVP là gì?                          (tài liệu 02 mất)
Q-F  PARTIAL 2026-08-21 → có nguyên văn + cấu trúc 5 bước. Xem §8.1-KQ.
     Còn thiếu: có tài liệu VIẾT không, format, cập nhật lần cuối, hỏi mấy người.
Q-G  Ai có quyền verify Knowledge? Có phải Technical/L3?      (PROJECT_CONTEXT Q7)
Q-H  AI có được suggest update knowledge đã verified?         (PROJECT_CONTEXT Q8)
Q-I  Vai trò Secondary Persona L3 trong 3 MVP capabilities?   (không capability nào framing cho L3)
Q-J  Draft là state của KnowledgeRecord hay entity riêng?     (K-B5)
```

## Từ Canonical Case v0.2 §16 — vẫn mở
```text
OQ1 Case identity resolution        OQ4 Concurrent primary ownership
OQ2 Split/Merge semantics           OQ5 Exact vocabularies    ← ảnh hưởng Knowledge Model
OQ3 Case vs Incident policy         OQ6 Projection strategy
```

## Security / tenant — nâng độ ưu tiên vì D1
```text
Data nào được gửi external LLM? Data nào bắt buộc internal? Tenant boundaries?
(PROJECT_CONTEXT §24 Q16–Q18 — giờ là yêu cầu bán hàng, không còn là câu hỏi xa)
```

---

# 10. Prompt để mở phiên làm việc mới

Copy nguyên khối này vào một conversation Claude mới:

```text
Tôi đang tiếp tục project AI Operational Knowledge & Process Platform.

Trước khi làm gì, hãy đọc theo thứ tự:
1. docs/00_CURRENT_STATE.md   ← đọc file này TRƯỚC, nó là trạng thái hiện tại
2. AGENT.md
3. docs/PROJECT_CONTEXT.md
4. docs/Canonical Case Model v0.2.md

Lưu ý: docs/02_PRODUCT_FOUNDATION_V1.md KHÔNG tồn tại — xem cảnh báo ở
00_CURRENT_STATE.md §1.

Yêu cầu làm việc:
- Phân biệt rõ CONFIRMED / EVIDENCE-SUPPORTED / HYPOTHESIS / PROPOSED / OPEN QUESTION
- Không tự chuyển PROPOSED thành CONFIRMED, không tự đóng OPEN QUESTION
- Làm việc như design partner, chủ động phản biện, không chỉ đồng ý
- Chưa code, chưa thiết kế architecture
- Trả lời bằng ngôn ngữ dễ hiểu, tránh thuật ngữ không cần thiết

Đọc thêm: docs/04_KNOWLEDGE_MODEL_V0.1.md — Step 1 + Step 2 ĐÃ CHỐT (§1 và §3).

Việc hôm nay:
Workstream 04 — Knowledge Model v0.1, Step 3: Knowledge ↔ Case ↔ Process.
Step 1 và Step 2 đã chốt — 15 quyết định. Đừng mở lại S1-S8 / K-B9 / T1-T4
nếu không có evidence mới. Mang theo N-3b, N-6, N-7 ở file 04 §3.6.

⚠ HỎI TÔI xem §8.2 (đếm 20 case OTA) đã chạy chưa. Toàn bộ file 04 §3.5
  đứng trên con số "5-10 nguyên nhân" với n=1 — xem R-K4 ở file 04 §6.
  T1/T2/T4 thì độc lập với con số đó, không bị ảnh hưởng.

⛔ Không viết code. Chốt công nghệ là quyền của tôi — xem AGENT.md §10.1.

Bắt đầu bằng cách:
1. Xác nhận bạn đã đọc và hiểu trạng thái hiện tại (tóm tắt ngắn, không dài dòng)
2. Nhắc lại ranh giới đã chốt ở 04 §1 và §3 để chắc là không đi lệch
3. Phản biện trước khi đề xuất, rồi hỏi tôi từng quyết định một qua form để tích chọn
```

---

# 11. Nhật ký phiên 2026-08-20/21

Việc đã làm:
- Review toàn bộ source of truth; phát hiện thiếu `02_PRODUCT_FOUNDATION_V1.md`
- Phát hiện mismatch Capability #3 giữa PROJECT_CONTEXT và AGENT.md; xác định bản mới đúng bằng corroboration từ v0.2 §11.3
- Chốt 5 quyết định mới D1–D5 (§2.2)
- Nhận con số 10/30/60 và phân tích hệ quả với thứ tự MVP (§3)
- Trả lời câu hỏi connector ở mức khái niệm (§7)
- Chuẩn bị đầy đủ nội dung Step 1 (§5) — chưa chốt, chờ người dùng review
- Tạo bản trình bày cho team/lãnh đạo: `Hai-bai-toan-tri-thuc.html` (bản HTML standalone ở thư mục gốc project)

Chưa làm:
- Chưa chốt Step 1
- Chưa tạo `docs/04_KNOWLEDGE_MODEL_V0.1.md`
- Chưa viết code, chưa thiết kế architecture

---

# 12. Nhật ký phiên 2026-08-21 buổi 2

Việc đã làm:
- **Chốt D6** (§2.3) — Q-A đã giải: "gom nhiều case cũ thành SOP theo yêu cầu" NẰM TRONG Capability 3, ở phiên bản "gom theo yêu cầu". Kèm guardrail phạm vi và 3 hệ quả.
- Ghi nhận hai hệ quả chưa từng có trong tài liệu: **D6 là bánh đà của D5** (diff nháp/bản duyệt = nhãn eval miễn phí), và **D6 là điều kiện để D2 dùng được ở khách đầu tiên**.
- Chạy Step 1: phản biện định nghĩa §5.2 và discriminator test §5.3; đi qua K-B1→K-B7; đề xuất K-B8 (Path A/Path B) và K-B9 (Evidence trỏ trực tiếp Knowledge).
- Phát hiện mâu thuẫn: đầu ra của D6 là danh sách bước → theo K-B6 thì rơi vào Process domain, trong khi đang làm Knowledge Model. Đề xuất giải bằng **kernel dùng chung** (S4) — giải luôn §6.9.
- Chạy 4 stress-test (§5.10): OTA, 10/30/60, non-Jira, CRM.
- Phát hiện chỗ bị bỏ sót: bản nháp gom từ N case mang theo **một phân bố**, không phải một phát biểu → cần evidence link ở mức từng phát biểu + `CONFLICTING` là bắt buộc.
- Đóng gói **7 quyết định S1-S7** (§5.9) — toàn bộ những gì còn chặn việc đóng Step 1.

- **Chốt cả 9 quyết định** (S1-S8 + K-B9) — người dùng chọn đúng phương án đề xuất ở cả 9 câu.
- Chốt luôn `Q-B` (SOP = ba vật thể) và `Q-C` (Cap 1 trả hai loại kết quả).
- Nâng guardrail phạm vi D6 thành **G11** trong `AGENT.md` §3.8.
- **Tạo `docs/04_KNOWLEDGE_MODEL_V0.1.md`** — Step 1 đóng.

- Ghi `AGENT.md` §10.1: **chốt công nghệ là quyền của người dùng** — phải báo trước khi code.
- Làm housekeeping **H-1** (PROJECT_CONTEXT §13.4 thêm `CONFLICTING`) và **H-2** (Case v0.2 §11.2 thêm đường Evidence → Knowledge trực tiếp).

Bốn quyết định cuối phiên — `CONFIRMED`:
```text
1  §8.1 (đi tìm SOP thật) CHẶN Step 2 — chạy trước, không làm song song
2  Thứ tự workstream: GIỮ TUẦN TỰ 04 → 05, chỉ chú thích kernel là của chung
   → không tách kernel thành tài liệu riêng (tránh thêm một vòng modeling, §6.7)
3  Housekeeping: làm ngay H-1 + H-2; H-3..H-6 gộp cuối workstream 04
4  Dừng phiên ở đây, chưa sang Step 2
```

- **Chạy §8.1** — người dùng cung cấp quy trình thật + ba câu bổ sung. Kết quả: §8.1-KQ.
  Ba phát hiện lớn: SOP thật **tuyến tính, không nhánh** (trái §5.3 PROJECT_CONTEXT) ·
  giá trị nằm ở B5 *"đưa ra kết luận"* — bước duy nhất không ai ghi lại ·
  tập kết luận **HỮU HẠN ~5-10 nguyên nhân**.
- **Chốt Step 2** (`T1`-`T4`) — người dùng chọn đúng phương án đề xuất cả 4 câu.
  Kết quả ở `docs/04_KNOWLEDGE_MODEL_V0.1.md` §3.
  Nổi bật: đơn vị Knowledge = MỘT NGUYÊN NHÂN · verification gắn TỪNG ASSERTION ·
  chỉ HAI type (loại REFERENCE do S6, loại POLICY do chưa có ca thật).
- Ghi nhận §3.5: ở quy mô ~10 record, Capability 1 là bài toán **phân loại**,
  không phải semantic search → không dựng RAG/vector DB (D5 giàn giáo tạm).
- Phát hiện `H-7` và rủi ro mới `R-K4` (n=1 cho con số 5-10).

Chưa làm:
- Chưa làm Step 3 (Knowledge ↔ Case ↔ Process)
- Chưa chạy §8.2 (đếm 20 case OTA) — việc giá trị cao nhất còn lại, xem R-K4
- Chưa xử lý H-3..H-7

Trạng thái stage:
```text
Product Foundation             ✅  (nhưng artifact mất — xem §6.1)
Canonical Case Model v0.2      ✅
Knowledge Model v0.1           🔵 ĐANG LÀM
                                  Step 1 ✅ CHỐT  → 04 §1  Boundary
                                  Step 2 ✅ CHỐT  → 04 §3  Concepts & Granularity
                                  Step 3 🔵 tiếp theo
Process Model                  ⚪ sau Knowledge — nhưng xem 04 §1.4: chia nhau
                                  KERNEL, không tách rời hoàn toàn được
MVP Architecture               ⚪ later
MVP Implementation             ⚪ later
```
