# 13 — ĐỊNH TUYẾN ISSUE MỚI

## Một ticket vừa đến thuộc nhóm nguyên nhân nào?

> **Đo ngày 2026-09-10.** Phép đo chặn quyết định "tích hợp để có issue mới thì hệ thống
> hướng dẫn cách giải quyết". Chạy lại được:
> `python scripts/jira-export/cham_routing.py <ket-lo1..5>.json`

---

# 0. Vì sao phải đo cái này, và vì sao nó KHÔNG phải phép đo retrieval

Cả tính năng "issue mới → hướng dẫn" đứng trên một bước mà **chưa có phép đo nào**: định
tuyến ticket mới vào một trong 19 nhóm nguyên nhân (`docs/09`).

Dự án đã có một con số dễ bị dùng nhầm cho câu này: `thu_retrieval.py` đo **34%** so với
đoán mù **31%**, AUC 0,61, trần lý thuyết 22%. Con số đó là đo **RETRIEVAL** — *tìm case
cũ giống case mới*. Định tuyến là bài toán khác: **phân vào 19 lớp đã biết trước**. Lấy
34% mà suy ra "định tuyến cũng vô vọng" là suy sai, và suy ngược lại cũng sai.

---

# 1. Thiết kế, và ba đường rò đáp án đã bịt

**Đầu vào = đúng thứ tồn tại lúc ticket đến:** tiêu đề + phần mô tả. **Không** comment,
**không** transcript.

⚠ Đây là chỗ dễ làm hỏng phép đo nhất. Transcript chứa sẵn câu chẩn đoán — đưa vào là đưa
đáp án vào đề. `thu_retrieval.py` đã đo được rằng dùng cả transcript còn **tệ hơn** dùng
tin nhắn đầu (22% so với 34%), nên đây không phải phòng xa mà là một lỗi đã xảy ra.

**Đáp án:** nhãn nhóm của `taxonomy-19-nhom-hoa-don.json` (88/150 case), hoặc "không có
nhóm" với 62 case mà `nguyen-nhan-150-case.json` ghi `khong-xac-dinh-duoc`.

```text
Ba đường rò đã bịt, mỗi cái vì một lý do cụ thể:
1  BỎ `caseKeys` khỏi bộ đề          — nó LÀ đáp án: nhóm nào gồm ticket nào
2  XOÁ mã `ES-` khỏi `moTa`          — 1/19 nhóm có lộ mã ticket trong phần mô tả
3  CHUYỂN file đáp án ra khỏi thư mục làm việc — nó từng nằm cạnh bộ đề
Và: KHÔNG cho biết kích cỡ nhóm, để không ai calibrate theo phân bố.
```

**Bẫy trộn lẫn:** 62 case `khong-xac-dinh-duoc` được trộn vào, không đánh dấu. Bộ định
tuyến phải tự nói "KHONG-BIET". Không có bẫy này thì phép đo chỉ đo được *khi có đáp án
thì có tìm ra không*, mà bỏ mất câu quan trọng hơn cho sản phẩm: *khi KHÔNG có đáp án thì
nó có im lặng không*.

**Cách chạy:** 150 case xáo trộn (seed 20260910), chia 5 lô × 30, mỗi lô một ngữ cảnh
SẠCH bị cấm đọc repo / `git log` / mọi file có chữ `dapan`, chạy song song.

⚠ **Hai giới hạn của thiết kế, ghi vì chúng có thể làm số đẹp hơn thực tế:** (a) trong
một lô, ngữ cảnh thấy 30 ticket cùng lúc — đã dặn xét độc lập nhưng **không cưỡng chế
được**; chạy từng ticket một có thể ra số khác. (b) trường `nguon` là **tự khai**, nên
mọi phép tách theo nó có phần vòng quanh.

---

# 2. Kết quả

```text
--- 88 case CÓ NHÃN -------------------------------------------------------
  gán ĐÚNG nhóm            74 / 88    84,1%     <- accuracy
  gán SAI nhóm              5 / 88     5,7%
  trả KHONG-BIET            9 / 88    10,2%
  precision khi DÁM gán    74 / 79    93,7%     <- con số sản phẩm quan tâm

--- 62 case KHÔNG XÁC ĐỊNH ĐƯỢC NGUYÊN NHÂN -------------------------------
  đúng đắn trả KHONG-BIET  37 / 62    59,7%
  DÁM GÁN nhóm             25 / 62    40,3%     <- chỗ đáng lo

--- ĐƯỜNG CƠ SỞ (bắt buộc) ------------------------------------------------
  luôn đoán nhóm lớn nhất  10 / 88    11,4%  ·  tự tin gán 100% trên case rỗng
  luôn trả KHONG-BIET       0 / 88     0,0%  ·  đúng đắn 100% trên case rỗng
```

🎯 **84,1% so với đoán mù 11,4% — hơn 7 lần.** Đây là **tín hiệu thật**, và nó khác hẳn
retrieval: FTS hơn đoán mù đúng 3 điểm phần trăm (34% vs 31%), tức gần như không thêm tín
hiệu nào. Định tuyến thì có.

→ **Hệ quả kiến trúc:** xương sống của tính năng "issue mới → hướng dẫn" nên là **PHÂN
LOẠI vào nhóm nguyên nhân**, không phải **tìm case giống bằng văn bản**. Hai thứ này từng
bị gộp làm một trong `AR4`/`Q-C`.

---

# 3. Ba chỗ phải đọc kỹ trước khi tin con số 84%

## 3.1 · Phần lớn thắng lợi đến từ việc mô tả ĐÃ NÓI RA nguyên nhân

```text
nguồn tự khai              số case   đúng    tỉ lệ
nói-rõ-trong-mô-tả            63      60     95,2%
suy-từ-triệu-chứng            16      14     87,5%
```

**63/79 ca dám gán là ca mà chính mô tả đã nói ra nguyên nhân.** Nghĩa là bộ định tuyến
phần lớn đang **ĐỌC**, không phải **CHẨN ĐOÁN**. Một trong năm ngữ cảnh nói thẳng điều
này: *"nguồn tín hiệu mạnh nhất không phải tiêu đề mà là phần chat"* — nhân viên dán đoạn
zalo vào mô tả, và trong đoạn đó có sẵn câu chẩn đoán.

⚠ Nhưng **87,5% trên nhóm phải suy** vẫn cao hơn đoán mù rất xa. Nó có chẩn đoán thật,
chỉ là n=16 nên đừng chốt con số đó.

## 3.2 · 40,3% "dám gán" KHÔNG PHẢI 40,3% "gán sai"

Đáp án của 62 case đó là *"hồ sơ chưa bao giờ ghi ra nguyên nhân"*, **không phải** *"nguyên
nhân là cái khác"*. Nên 25 ca kia là **tự tin KHÔNG KIỂM ĐƯỢC**, chưa phải sai đã chứng
minh. Với sản phẩm thì hai thứ nguy hiểm ngang nhau: nhân viên nhận một SOP mà **không có
cách nào biết nó có áp dụng được hay không**.

```text
25 ca dám gán trên case rỗng, tách ra:
  tự khai "suy-từ-triệu-chứng"   16    đang đoán, và biết là đoán
  tự khai "nói-rõ-trong-mô-tả"    9    ĐÁNG NGHI NHẤT — đọc TRIỆU CHỨNG thành NGUYÊN NHÂN
  có mô tả lúc đến               12  ·  chỉ có tiêu đề  13
buocXuLy thật của chúng: hướng dẫn khách 10 · remote 5 · báo dev 3 · ...
```

→ Cụm `remote 5 + báo dev 3` khớp với phát hiện nền của dự án: nguyên nhân **được xác định
ở nơi khác** (remote/điện thoại) và không để lại chữ nào trong ticket. Bộ định tuyến không
chữa được điều đó; nó chỉ đoán vào chỗ trống.

## 3.3 · 4/5 lỗi là SEAM CỦA TAXONOMY, không phải bộ định tuyến kém

```text
ES-343837  đáp án: Kênh kết nối tới NCC mất hiệu lực   -> gán: NCC từ chối payload
ES-342309  đáp án: Lệch cấu hình tiền/thuế             -> gán: NCC từ chối payload
ES-343348  đáp án: NCC từ chối payload                 -> gán: Phát hành không hoàn tất
ES-338878  đáp án: Giới hạn tính năng/lỗi sản phẩm     -> gán: Sửa hoá đơn đã phát hành
ES-343367  đáp án: Lệch cấu hình tiền/thuế             -> gán: Sửa hoá đơn đã phát hành
           (ca này khác: mô tả CHỈ LÀ ẢNH, thiếu đầu vào chứ không phải lệch seam)
```

**Không có lỗi nào là định tuyến hoang đường.** Bốn trong năm là hai nhóm **kề nhau** mà
cùng một mô tả đọc được theo cả hai chiều — ba lỗi dính vào đúng ba nhóm quanh nhà cung
cấp. Cái thứ năm là thiếu đầu vào (mô tả chỉ có ảnh).

🛑 **Đây là quan sát ĐỘC LẬP THỨ BA về cùng một điều**, sau `docs/11` §9 và `AR-p`:
```text
docs/11 §9   bước kiểm đầu của nhóm 2 bị chặn bởi nguyên nhân của nhóm 1
AR-p         bản A phải MƯỢN case nhóm khác để dựng bước loại trừ (K5)
đây (§3.3)   4/5 lỗi định tuyến nằm ở SEAM giữa hai nhóm kề nhau
```
Và một ngữ cảnh còn tự nêu ra chính ca đó: `ES-341290` mang **cả** xung đột trình duyệt
**lẫn** phân quyền. → **Ranh giới nhóm của taxonomy không phải ranh giới hợp lệ**, lần này
đo được ở khâu định tuyến chứ không chỉ ở khâu gom SOP.

---

# 4. Hai con số phụ đáng ghi

```text
tổng trả KHONG-BIET             46 / 150   30,7%
case CHỈ có tiêu đề, có nhãn      9 case, đúng 6   66,7%   (n nhỏ)
case có tiêu đề + mô tả, có nhãn 79 case, đúng 68  86,1%
```

⚠ Và một hình dạng đã lộ ngay lúc dựng bộ đề: **chỉ 104/150 case có phần mô tả.** Trong 88
case CÓ nhãn thì 79 có mô tả; tức 46 case "chỉ có tiêu đề" **dồn gần hết vào nhóm không
xác định được nguyên nhân**. → *Thiếu mô tả* và *không lần ra nguyên nhân* là **cùng một
hiện tượng**, không phải hai vấn đề rời nhau.

Lệch gán theo nhóm là **nhẹ** — không có hiện tượng dồn hết vào nhóm lớn nhất:
```text
Phân quyền & ký hiệu            gán 13  |  thực 10
NCC từ chối payload             gán 11  |  thực 10
Khai báo danh mục/cấu hình      gán  9  |  thực  5   <- lệch nhiều nhất
Kênh kết nối tới NCC            gán  8  |  thực  7
Lệch cấu hình tiền/thuế         gán  7  |  thực  8
```

---

# 5. Bộ chấm tự canh ba thứ, và đã chứng minh biết đỏ

```text
1  BỊA TÊN NHÓM        trả về tên không có trong taxonomy -> mã thoát 1
                       ĐÃ THỬ: đổi một tên thành "Nhom toi vua bia ra" -> chặn, exit 1
                       (cùng luật G6/AP3 áp cho bản nháp SOP, nay áp cho định tuyến)
2  THIẾU CASE           mặc định CHẶN; `--phan` mới cho chấm tập con, và NÓI RA độ phủ
                       — một tỉ lệ trên tập con mà không ghi độ phủ rất dễ bị đọc thành
                       tỉ lệ trên cả corpus
3  TRÙNG CASE giữa lô   mã thoát 2
```

Và đường cơ sở **đã chạy thật** chứ không tính bằng tay: dựng một bộ trả lời "luôn nhóm
lớn nhất" rồi cho qua đúng bộ chấm → 11,4%. Đó là cách duy nhất chắc rằng bộ chấm không
tự thổi số.

---

# 6. Việc phép đo này mở ra

```text
1  QUYẾT ĐỊNH KIẾN TRÚC: xương sống là PHÂN LOẠI, không phải RETRIEVAL văn bản.
   Chạm vào AR4 (Postgres FTS trước) và Q-C. FTS 34%/đoán mù 31% so với định tuyến
   84%/đoán mù 11% — hai cơ chế cách nhau rất xa trên CÙNG nguồn dữ liệu.
   ⚠ KHÔNG đọc thành "bỏ retrieval": khi đã vào nhóm rồi, vẫn cần lấy ra case cũ
     của nhóm đó. Nhưng nó không còn là bước ĐẦU TIÊN, và không còn là bước rủi ro nhất.

2  Cổng "KHÔNG BIẾT" phải là phần của sản phẩm, không phải tuỳ chọn. 40,3% dám gán
   trên case rỗng nghĩa là: nếu bật tính năng mà không có cổng này, khoảng 4/10 ticket
   không xác định được nguyên nhân sẽ nhận một hướng dẫn không kiểm được.
   → Ngưỡng phải đo, không đặt bằng cảm giác. Và `M2` phần (c) đã có tinh thần này rồi:
     thiếu bằng chứng thì coi như CHƯA duyệt.

3  Đo lại kiểu TỪNG TICKET MỘT, không theo lô — để bỏ giới hạn §1(a).

4  AR-p nặng thêm một bậc. Nếu gộp/vẽ lại các nhóm quanh nhà cung cấp thì cả 3 lỗi
   seam ở §3.3 và bước loại trừ K5 của AR-p đều được giải cùng lúc. Đây giờ là câu
   về TAXONOMY, không còn là câu về prompt.

5  62 case rỗng là chỗ giá trị bị chặn, và nó KHÔNG phải bài toán của mô hình:
   nguyên nhân xảy ra trên remote/điện thoại. Trùng đúng việc mà R-K4 Q2 đã chốt
   (bắt ghi nguyên nhân lúc remote) — nay có thêm một con số chống lưng cho nó.
```

⚠ **Điều phép đo này KHÔNG nói:** nó không nói tính năng đã dùng được. Kho tri thức vẫn
rỗng (0/19 nhóm được duyệt, `Approve()` chưa có endpoint nào gọi), ranh giới khách sạn vẫn
chưa được thực thi ở bất kỳ tầng nào (`AR-l`), và định tuyến đúng nhóm chỉ có nghĩa **nếu**
nhóm đó có một SOP đã duyệt để trả về.
