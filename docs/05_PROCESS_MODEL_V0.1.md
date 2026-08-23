# 05 — Process Model v0.1

## AI Operational Knowledge & Process Platform

> **Chốt:** 2026-08-23 · `CONFIRMED` · bốn quyết định `PR1`–`PR4`
> **Phạm vi:** cố ý NHỎ — vừa đủ để build first use case. Một phiên, không phải năm step.
> **Trước đó:** `04_KNOWLEDGE_MODEL_V0.1.md` đóng cùng ngày (23 quyết định).

---

# 0. File này là gì và không phải gì

**Là:** phần Process còn thiếu để build được first use case, sau khi trừ đi những gì `Canonical Case v0.2` và `Knowledge Model v0.1` đã có.

**Không phải:** một process/workflow engine đầy đủ. Không nhánh, không điều kiện, không ngoại lệ, không BPMN. Xem `PR2` để biết vì sao — và vì sao đó là quyết định có căn cứ, không phải cắt bớt cho nhanh.

**Dứt khoát không phải:** database schema, API, orchestration framework, state machine library. Đó là Workstream 06/07.

## Vì sao file này ngắn — và ngắn là đúng

Ba căn cứ, đều từ tài liệu:

```text
1  §6.7 (00_CURRENT_STATE)   "Knowledge v0.1 + Process v0.1 chốt trong ~2 tuần,
                              ở mức vừa đủ để build. KHÔNG cần sâu bằng Case v0.2."
                              Failure mode: "không bao giờ làm ra thứ gì."

2  §8.1-KQ                   SOP THẬT tuyến tính, KHÔNG nhánh. Phần lớn độ phức
                              tạp của một process model là nhánh/điều kiện/ngoại lệ
                              — đúng cái dữ liệu thật nói KHÔNG tồn tại. (H-7)

3  Case v0.2 §11.1 + 04      Phần lớn Process đã được quyết ở nơi khác — xem §1.
```

## Điều kiện dừng

```text
Mục tiêu: đủ để engine gợi ý được "bạn đang ở bước nào, bước tiếp theo là gì".
Quy tắc: câu hỏi nào KHÔNG chặn việc build → §7 Open Questions và đi tiếp.
Sau file này: Workstream 06 — MVP Architecture, và ĐÓ là lúc chốt công nghệ.
```

---

# 1. Đã có sẵn — không quyết lại ở đây

```text
Case → ProcessRun → ProcessDefinition                    v0.2 §11.1  CONFIRMED
một Case có thể có NHIỀU ProcessRun                      v0.2 §11.1  CONFIRMED
ProcessRun có thể bị chọn SAI hoặc ABANDONED             v0.2 §11.1  CONFIRMED
CanonicalCase KHÔNG chứa ProcessDefinition               v0.2 §11.1  CONFIRMED
CaseAction/CaseClaim/Evidence cung cấp observations
   để ProcessRun xác định process state                  v0.2 §11.1  CONFIRMED
ProcessStep CONSULTS một TẬP Knowledge theo CHỦ ĐỀ       L1          CONFIRMED
KnowledgeRecord → ProcessDefinition khi action nhiều bước T2         CONFIRMED
Danh sách bước có MỘT NHÀ DUY NHẤT: Process domain       S4          CONFIRMED
Kernel dùng chung — KHÔNG định nghĩa lại vocabulary      S4          CONFIRMED
   → bảng khóa duy nhất: 04 §3D.7
Ba nguồn xác định process state:
   SYSTEM FACT · USER CONFIRMATION · AI INFERENCE        §5.4        CONFIRMED
Quy tắc phân định Process vs Knowledge                   K-B6        CONFIRMED
   có thứ tự bước + theo dõi được "đang ở bước nào" → PROCESS
   một khuyến nghị đơn, có điều kiện                 → KNOWLEDGE
```

→ Bốn quyết định dưới đây là **toàn bộ** phần còn thiếu.

---

# 2. Trạng thái của một bước được SUY RA · `CONFIRMED` (PR1)

> **Mỗi bước khai báo *bằng chứng nào thì coi là XONG*. Trạng thái KHÔNG được lưu — nó được suy ra từ evidence.**

```text
bước XONG        ⟸ bằng chứng mà bước đó khai báo đã tồn tại
bước HIỆN TẠI    ⟸ bước chưa-xong ĐẦU TIÊN
next best action ⟸ bước hiện tại + tập tri thức bước đó tra (L1)
```

## v0.2 §11.1 đã nói điều này, chỉ chưa ai đọc kỹ

> *"CaseAction, CaseClaim và Evidence cung cấp observations để ProcessRun **xác định** process state."*

Câu đó **đã** hàm ý state là kết quả của một phép suy, không phải một cờ ai đó bật.

## Ba nguồn ở §5.4 khớp luôn, không cần cơ chế riêng

```text
SYSTEM FACT        → EvidenceItem, origin = SYSTEM_FACT
USER CONFIRMATION  → "tôi đã check mapping rồi" LÀ một EvidenceItem,
                     origin = USER_CONFIRMED
AI INFERENCE       → EvidenceItem, origin = AI_INFERENCE
```

Điều này giải một vấn đề mà `G3` (FACT ≠ AI INFERENCE) đòi mà chưa ai chỉ ra cách làm: **AI suy luận một bước đã xong thì bị đánh dấu TỰ ĐỘNG**, vì origin đi cùng evidence. Không cần thêm cờ *"cái này do AI đoán"* — và do đó không có chỗ để quên đánh dấu.

Nhớ `v0.2 §7.5`: `USER_CONFIRMED` ≠ sự thật khách quan. *"Mapping đúng rồi"* là fact rằng **user đã nói vậy**. Bước được coi là xong dựa trên một phát biểu của user, và điều đó **hiện rõ** trong dữ liệu chứ không bị làm phẳng.

## Hệ quả: ProcessRun lưu rất ít

```text
LƯU     → Case nào · ProcessDefinition nào · bắt đầu khi nào
        · có bị ABANDONED không (người quyết định, giống DEPRECATED)
SUY RA  → toàn bộ trạng thái tiến độ
```

## Nguyên tắc `L4` lần thứ tư

> *Nếu một thông tin chỉ đúng khi suy từ thứ khác, thì nó là phép chiếu, không phải dữ liệu độc lập.*

`L4` (`SUPERSEDED`) → `AP3` (từ chối origin hai mức) → `V3` (`NEEDS_REVIEW`) → `PR1` (trạng thái bước). Bốn lần, bốn step liên tiếp. Nó nên được coi là nguyên tắc thiết kế của dự án, không phải mẹo cục bộ.

## Cái giá — ghi rõ

```text
· "xong" chỉ đúng bằng mức bằng chứng khai báo cho nó
· khai báo bằng chứng quá lỏng → bước tự xong oan
· khai báo quá chặt → bước không bao giờ xong, người dùng bị chặn
```

→ Chất lượng của khai báo *"bằng chứng nào thì xong"* là chỗ dễ sai nhất của model này. Nhưng nó **hiện ra được** — khác với một cờ boolean, chỗ sai nằm im.

---

# 3. `ProcessDefinition` v0.1 là danh sách bước TUYẾN TÍNH · `CONFIRMED` (PR2)

> **Không nhánh. Không điều kiện. Không ngoại lệ. Không đường escalation.**

## Vì sao — không có ca thật nào

```text
SOP THẬT (§8.1-KQ)         B1 Kibana → B2 response → B3 tài liệu → B4 issue cũ
                            → B5 kết luận.  TUYẾN TÍNH.
SOP có nhánh ở §5.3         đã đánh dấu là VÍ DỤ MINH HOẠ TỰ NGHĨ (H-7)
Vertical thứ hai (CRM)      rule "im 7 ngày → gửi case study" đã bị K-B6 xếp
                            sang KNOWLEDGE, không sinh nhánh Process
```

Tiền lệ đang dùng lại: `T3` loại type `POLICY` vì *"chưa có ca thật"*, kèm câu

> *"Thêm một type về sau rất dễ. Bỏ một type đã có dữ liệu chạy trên nó thì rất khó."*

Nhánh và điều kiện là **phần đắt nhất** của bất kỳ process model. Dựng nó cho một thứ dữ liệu thật nói là không tồn tại là `R-K3` ở dạng rõ nhất.

## Điều kiện xem lại `PR2`

```text
Khi có một SOP THẬT có nhánh → mở lại PR2, và GHI LẠI SOP đó là cái nào.
```

Không mở lại vì *"chắc sau này sẽ cần"*.

---

# 4. Escalation thuộc Knowledge · Chờ thuộc Case · `CONFIRMED` (PR3, PR4)

## PR3 — Escalation KHÔNG phải một nhánh của Process

*"Escalate lên Technical"* là **một kết luận của B5**. Và B5 là Knowledge: `T1` (một nguyên nhân, kèm cách nhận ra) + `T2` (action đơn lẻ nằm trong record).

```text
SAI    Process có nhánh "nếu chưa xác định được → escalate"
       → "escalate hay không" thành điều kiện → cần nhánh → phá PR2
       → và danh sách bước có hai nhà → phá S4

ĐÚNG  B5 tra Knowledge (L1). Một trong các nguyên nhân có thể là
      UNKNOWN/chưa xác định, và hành động khuyến nghị của nó là escalate.
```

Lưu ý SOP thật **có** một bước tên *"vendor / Technical"* trong phiên bản tưởng tượng ở §5.3 — nhưng bản thật thì bước 5 là *"đưa ra kết luận"*, và escalate chỉ là **một trong các kết luận**. `PR3` giữ đúng bản thật.

Điều này cũng nhất quán với `G4`: *"chưa xác định được nguyên nhân"* là một kết quả hợp lệ, có hành động đi kèm — không phải một lỗi của quy trình.

## PR4 — Chờ / bị chặn ở lại mức Case

`Canonical Case v0.2` đã có `WaitingSegment`. Một bước đang chờ khách trả lời **chính là** Case đang chờ.

```text
Process KHÔNG có trạng thái "đang chờ" riêng.
Nếu cần biết vì sao bước hiện tại chưa xong → đọc WaitingSegment của Case.
```

Lý do: cùng nguyên tắc *"một nhà duy nhất"* của `S4`. Hai bản sao sẽ lệch nhau — đúng bệnh §6.9, chỉ ở dạng dữ liệu thay vì dạng từ vựng.

---

# 5. Hình dạng đầy đủ — Process Model v0.1

Mọi dòng trỏ về một quyết định đã chốt.

```text
PROCESS DEFINITION  =  danh sách bước TUYẾN TÍNH cho một LOẠI công việc

  identity      loại công việc
  kernel        Origin · Actor · Source · Evidence · Time · Verification ·
                Applicability · Visibility        S4 · bảng khóa 04 §3D.7
                → ProcessDefinition cũng có DRAFT/ACTIVE/DEPRECATED,
                  vì Path A sinh ra nó ở trạng thái nháp (S4)

  step[]  ── theo THỨ TỰ, KHÔNG nhánh                        PR2
     nội dung           "lấy dữ liệu ở Kibana"
     bằng chứng cần     loại evidence nào thì coi là XONG     PR1
     tra tri thức       → TẬP Knowledge theo CHỦ ĐỀ (tùy chọn) L1

PROCESS RUN  =  một lần chạy definition đó trên MỘT Case

  → Case                 associatedWith                      v0.2 §11.1
  → ProcessDefinition    instanceOf                          v0.2 §11.1

  LƯU      Case nào · Definition nào · bắt đầu khi nào · ABANDONED?
  SUY RA   bước xong      ⟸ bằng chứng của bước tồn tại      PR1
           bước hiện tại  ⟸ bước chưa-xong đầu tiên           PR1
           next action    ⟸ bước hiện tại + tri thức nó tra   PR1 · L1

KHÔNG CÓ ở v0.1:  nhánh · điều kiện · ngoại lệ · escalation-as-step (PR2, PR3)
                  trạng thái chờ riêng — dùng WaitingSegment của Case (PR4)
```

---

# 6. Decision Register

## `CONFIRMED 2026-08-23`

```text
PR1  Trạng thái bước SUY RA từ evidence, KHÔNG lưu cờ.
     Mỗi bước khai báo "bằng chứng nào thì xong".
     → ba nguồn §5.4 khớp qua origin; AI inference bị đánh dấu TỰ ĐỘNG (G3)
     → ProcessRun chỉ lưu: Case · Definition · thời điểm · ABANDONED
     → nguyên tắc L4 lần thứ TƯ
PR2  ProcessDefinition v0.1 = danh sách bước TUYẾN TÍNH.
     Không nhánh/điều kiện/ngoại lệ — KHÔNG CÓ CA THẬT (§8.1-KQ, H-7, K-B6).
     → điều kiện xem lại: khi có một SOP thật có nhánh, và ghi lại SOP đó
PR3  Escalation thuộc KNOWLEDGE, không phải nhánh của Process.
     Nó là một kết luận của B5 (T1 + T2). Giữ PR2 và S4.
PR4  Chờ/bị chặn ở lại mức Case (WaitingSegment, v0.2).
     Process không có bản thứ hai.

KẾT QUẢ: hai entity (ProcessDefinition, ProcessRun) — cả hai đã được
         v0.2 §11.1 công nhận từ trước. KHÔNG có entity mới.
```

## Kế thừa — không mở lại ở đây

```text
v0.2 §11.1   Case → ProcessRun → ProcessDefinition; run có thể abandoned
L1           ProcessStep CONSULTS tập Knowledge theo chủ đề
T2           Knowledge → ProcessDefinition khi action nhiều bước
S4           kernel dùng chung; danh sách bước có MỘT nhà
K-B6         quy tắc phân định Process vs Knowledge
§5.4         ba nguồn xác định process state
04 §3D.7     BẢNG TỪ VỰNG ĐÃ KHÓA — không định nghĩa lại ở file này
```

---

# 7. Còn `OPEN` — không chặn việc build

```text
PR-a  Nhánh / điều kiện / ngoại lệ            → khi có SOP thật có nhánh (PR2)
PR-b  Một Case có nhiều ProcessRun cùng lúc thì hiển thị thế nào?
      → v0.2 §11.1 đã cho phép nhiều run. Là câu hỏi UX, không phải model.
PR-c  Ai được sửa một ProcessDefinition đã ACTIVE?
      → cùng họ Q-G/Q-H của Knowledge. Chưa chặn build.
PR-d  Bước bị BỎ QUA có chủ đích (người biết chắc không cần làm) —
      là "xong" hay một trạng thái riêng? Chưa có ca thật.
PR-e  Definition đổi phiên bản trong lúc một Run đang chạy dở?
      → Run trỏ tới definition nào? Chưa có ca thật ở kho 1 definition.
```

---

# 8. Kiểm tra điều kiện dừng bằng quy trình thật

```text
B1  lấy dữ liệu ở Kibana        → step, bằng chứng cần = kết quả query   PR1 ✓
B2  xem response trả về         → step, bằng chứng cần = response        PR1 ✓
B3  xem tài liệu                → step, tra Knowledge theo chủ đề        L1  ✓
B4  xem issue xử lý trước đó    → step; "case cũ tương tự" là Q-C, đã chốt ✓
B5  ĐƯA RA KẾT LUẬN             → tra Knowledge; nguyên nhân → hành động T1/T2 ✓
    escalate nếu chưa rõ        → một kết luận, không phải nhánh         PR3 ✓
đang ở bước nào?                → suy ra từ evidence                     PR1 ✓
bước tiếp theo là gì?           → bước chưa-xong đầu tiên + tri thức     PR1 ✓
chờ khách trả lời               → WaitingSegment của Case                PR4 ✓
đo được không?                  → 5 mốc RETRIEVED…USED                   L2  ✓
```

→ **Đủ để build. Workstream 05 đóng ở v0.1.**

---

# 9. Bước tiếp theo: `06 — MVP Architecture`

> **ĐÂY là lúc chốt công nghệ** — `AGENT.md` §10.1. Domain Modeling kết thúc ở đây.

## Phải làm TRƯỚC khi vào 06

```text
§8.2  Đếm case OTA, bản nhẹ (~30 phút, việc của người dùng)
      → tập nguyên nhân HỮU HẠN NHỎ hay MỞ?
      → quyết định 04 §3.5 đúng hay sai
      → quyết định CÓ DỰNG vector DB / RAG hay không
      → đó là quyết định công nghệ ĐẮT NHẤT và KHÓ ĐẢO NHẤT của workstream 06
```

Luật quyết định của §8.2 đã chốt **trước** khi đếm (≤15 / ≥40 / ở giữa) — xem `00_CURRENT_STATE.md` §8.2. Không mở lại.

## Ràng buộc của Workstream 06

```text
AGENT.md §10.1   Chốt công nghệ là QUYỀN CỦA NGƯỜI DÙNG.
                 Agent ĐƯỢC đề xuất kèm đánh đổi, PHẢI chờ xác nhận.
                 Không viết file code đầu tiên rồi mới hỏi.
D5               "Nếu sang năm có model mạnh gấp 10, cái này thành giá trị
                 hơn hay thành rác?" — áp cho từng lựa chọn công nghệ.
D1 · D3 · G7     Multi-tenant + tenant boundary từ NGÀY ĐẦU.
G12              Không hardcode đặc điểm của khách #0 vào thiết kế.
04 §3.5          Ở quy mô ~10 record, Capability 1 là bài toán PHÂN LOẠI,
                 không phải semantic search. ⚠ đứng trên n=1 → xem §8.2.
QM-1             Ngưỡng của Success Metrics vẫn OPEN — cần chạy thật.
```

---

# Nguyên tắc cốt lõi của Process Model v0.1

> **Process nói THỨ TỰ và ĐIỀU KIỆN XONG. Knowledge nói KẾT LUẬN. Case nói CHUYỆN GÌ ĐANG XẢY RA.**
> **Không domain nào giữ bản sao của hai domain kia.**

Và ranh giới đã cứu file này khỏi phình to:

```text
"đang ở bước nào"      →  suy ra, không lưu           PR1
"có nhánh không"       →  không, vì không có ca thật   PR2
"escalate thế nào"     →  Knowledge, không phải bước   PR3
"đang chờ gì"          →  Case, không phải Process     PR4
```
