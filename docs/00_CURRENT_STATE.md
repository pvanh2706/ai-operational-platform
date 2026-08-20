# 00 — CURRENT STATE / SESSION HANDOFF

## AI Operational Knowledge & Process Platform

> **Cập nhật:** 2026-08-21
> **Mục đích:** File này là điểm vào cho phiên làm việc tiếp theo. Đọc file này TRƯỚC, rồi mới đọc các tài liệu khác.
> **Dành cho:** AI Agent hoặc người mới tiếp tục project, kể cả trên máy khác.

---

# 1. Đọc gì, theo thứ tự nào

```text
1. docs/00_CURRENT_STATE.md          ← file này. Trạng thái hiện tại + việc đang làm
2. AGENT.md                          ← cách agent phải làm việc trong project
3. docs/PROJECT_CONTEXT.md           ← Discovery + Vision (consolidated 2026-08-18)
4. docs/Canonical Case Model v0.2.md ← Domain Model đã chốt
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

# 4. Quyết định DUY NHẤT đang chặn Step 1

> ## ❓ "Gom nhiều case cũ thành một SOP theo yêu cầu" có nằm trong Capability 3 của MVP không?

Tình trạng: `OPEN — cần người dùng quyết`

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

# 5. Việc của phiên tiếp theo: Step 1 — Define Knowledge Boundary

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
Hỏi 2 người: bạn support kỳ cựu nhất, và người xử lý case OTA gần nhất — *"khi gặp case này, anh/chị làm theo cái gì?"*

Cần lấy về: SOP (nếu có) nằm ở đâu, format gì, bao nhiêu bước, có rẽ nhánh không, cập nhật lần cuối bao giờ.

Kết quả dùng để: stress-test Step 1 bằng tri thức thật thay vì ví dụ tự nghĩ. **Không thể thiết kế cái hộp trước khi biết bên trong đựng gì.**

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
Q-A  "Gom nhiều case cũ thành SOP theo yêu cầu" có trong Capability 3 của MVP?   → §4
Q-B  K-B6: SOP "booking không về PMS" là Knowledge, Process, hay Document
     làm source cho cả hai?                                                      → §5.6
Q-C  K-B1: "Tìm case cũ tương tự" có trong Capability #1 của MVP?                → §5.6
Q-D  §5.7: Tri thức suy ra từ nguồn giới hạn quyền thì ai được xem?              → §5.7
```

## Cần trước Step 2–3
```text
Q-E  Success Metrics của MVP là gì?                          (tài liệu 02 mất)
Q-F  SOP thật của first use case trông thế nào?              (§8.1)
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

Việc hôm nay:
Workstream 04 — Knowledge Model v0.1, Step 1: Define Knowledge Boundary.
Nội dung Step 1 đã được chuẩn bị ở 00_CURRENT_STATE.md §5.

Bắt đầu bằng cách:
1. Xác nhận bạn đã đọc và hiểu trạng thái hiện tại (tóm tắt ngắn, không dài dòng)
2. Nêu câu hỏi đang chặn Step 1 (§4) để tôi quyết
3. Rồi chạy Step 1 theo trình tự §5.8
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

Trạng thái stage:
```text
Product Foundation             ✅  (nhưng artifact mất — xem §6.1)
Canonical Case Model v0.2      ✅
Knowledge Model v0.1           🔵 ĐANG LÀM — Step 1 chuẩn bị xong, chờ review
Process Model                  ⚪ sau Knowledge
MVP Architecture               ⚪ later
MVP Implementation             ⚪ later
```
