# 11 — CÂY QUYẾT ĐỊNH SOP ĐẦU TIÊN, dựng tay trên dữ liệu thật

> **Viết 2026-09-07.** Đây là việc *"làm được ngay, không chờ ai"* mà `00` §Việc tiếp theo
> đã ghi, và người dùng chọn làm trước.
>
> **Nó là gì:** một bản nháp SOP dạng cây quyết định cho nhóm nguyên nhân lớn nhất của chủ
> đề hoá đơn, dựng bằng tay từ 10 case Jira thật. Bản máy đọc được:
> [`docs/ket-qua-phan-tich/cay-quyet-dinh-phan-quyen-ky-hieu.json`](ket-qua-phan-tich/cay-quyet-dinh-phan-quyen-ky-hieu.json).
>
> **Nó tồn tại để làm gì:** trả lời một câu hỏi thiết kế mà không đọc tài liệu nào trả lời
> được — *một bản nháp SOP phải MANG THEO những gì thì người duyệt duyệt được?* `S8` chốt
> rằng nháp phải mang theo một **phân bố**; làm thật một cái mới thấy phân bố đó trông như
> thế nào, và nó không giống ví dụ trong `S8`.
>
> ⚠ **Phạm vi, theo đúng luật ở `10` §7:** một khách, một nguồn, một chủ đề, 10 case. Dùng
> được để **tìm ra hình dạng đầu ra** và để **bác vài cách làm hiển nhiên mà sai** — KHÔNG
> dùng được để chốt kiến trúc.

---

## 1. Cây quyết định

Bốn nguyên nhân, **một cửa vào**. Triệu chứng khách kể gần như trùng nhau: *"không xuất
được hoá đơn"* + một ảnh chụp, hoặc form báo đỏ *"Validation form error"*, hoặc màn phát
hành không có mục ký hiệu để chọn, hoặc xem trước PDF trả *"Ký hiệu hoá đơn không tồn tại"*.

```text
K1  Trên form phát hành của ĐÚNG đặt phòng đang lỗi, ký hiệu đã được chọn chưa?
    xem ở: form phát hành trong PMS — làm được ngay trên ảnh khách đã gửi
    ├─ chưa chọn ................................. S0   3/10 case   ✔ có nguồn
    ├─ mở ra thì danh sách ký hiệu RỖNG .......... K2   2/10 case   ✔ có nguồn
    ├─ không mở được cả màn hình đó .............. K3               ⚠ tôi suy ra
    └─ đã chọn mà vẫn lỗi ........................ K4               ⚠ tôi suy ra

K2  TÀI KHOẢN NÀO đang phát hành, và tài khoản đó đã được thêm + phân quyền
    ký hiệu trên trang quản trị hoá đơn chưa?     ← bước phân nhánh THẬT
    xem ở: admin.ezinvoice.vn → Cấu hình → Quản lý người dùng, rồi → Ký hiệu
    ├─ chưa có trong danh sách người dùng ........ S1   1/10 case   ✔ có nguồn
    ├─ có user, chưa phân quyền ký hiệu .......... S2   3/10 case   ⚠ tôi suy ra
    ├─ cả cơ sở chưa có ký hiệu nào .............. S3   0/10 case   ⚠ tôi suy ra
    └─ cả hai đều đủ ............................. K3               ⚠ tôi suy ra

K3  Role của tài khoản trong PMS có quyền "Thêm hoá đơn" không?
    xem ở: phân quyền role trong PMS — KHÔNG phải trên trang quản trị hoá đơn
    └─ thiếu quyền ............................... S4   1/10 case   ✔ có nguồn

K4  Đây có phải cơ sở / mã số thuế MỚI vừa tạo không?
    xem ở: cấu hình ký hiệu của TỪNG cơ sở, và thử đăng nhập bằng MST mới
    └─ một cơ sở khai chưa đủ, hoặc quản trị chỉ vào được MST cũ .. S5   1/10 ✔

K5  (LOẠI TRỪ) Trình duyệt đang đăng nhập nhiều tài khoản cùng lúc không?
    └─ có → RA KHỎI SOP này, sang nhóm "xung đột phiên"

K-XN  Phát hành lại ĐÚNG đặt phòng ban đầu, xác nhận ra được số hoá đơn.
```

**Vì sao thứ tự này:** K1 rẻ nhất và phủ 3/10. K2 là nhánh lớn nhất — 4/10 ca chẩn đoán,
5/10 nếu tính cả `ES-344424` là hỏi-đáp cùng nguyên nhân — nhưng đắt hơn: phải hỏi khách
một câu và mở trang quản trị. ⚠ **Không case nào ghi thứ tự kiểm** — thứ tự trên do tôi
đặt theo chi phí và tần suất, không phải theo một quy trình đã quan sát được.

**Ba cái bẫy có thật trong corpus, không suy ra được từ mô tả nhóm:**

| Bẫy | Ca thật |
|---|---|
| Muốn **thao tác** phân quyền thì phải đăng nhập bằng một tài khoản **đã được** phân quyền. Không nói rõ thì khách sẽ thử bằng chính tài khoản đang bị chặn. | `ES-346396` — nhân viên phải chỉ đích danh dùng tài khoản nào |
| *"Ký hiệu này khách vẫn đang xuất bình thường tại nhà cung cấp"* **KHÔNG** chứng minh ký hiệu đã được khai trong hệ thống. | `ES-346559` — chính câu đó làm phép kiểm bị bỏ qua một vòng |
| K2 và K3 có triệu chứng khách kể **giống hệt nhau** nhưng chỗ phải sửa là **hai màn quản trị khác nhau**. Nhầm chỗ thì kiểm mãi không thấy gì sai. | `ES-343733` (role PMS) vs `ES-346039` (quyền ký hiệu) |

---

## 2. Nó dựng được từ gì — và 6/10 case không ghi bước kiểm

Đây là **phân bố mà `S8` đòi**, đo thật trên nhóm này:

```text
ghi rõ bước kiểm                      4/10   ES-346396 · ES-346039 · ES-341290 · ES-346559
suy ra được từ mẩu, nhưng không ghi   2/10   ES-341317 · ES-343662   (nhìn ảnh khách gửi)
chỉ ghi KẾT LUẬN, không ghi cách biết 2/10   ES-346647 · ES-346065   ("Chưa phân quyền", 15 ký tự)
bước kiểm xảy ra NGOÀI ticket         1/10   ES-343733               (remote vào máy khách)
không phải chẩn đoán                  1/10   ES-344424               (hỏi-đáp)
```

🛑 **Và đây là điều đáng ghi nhất của mục này: `S8` lấy ví dụ *"bước kiểm room mapping:
14/20 case đã làm"*. Con số thật của nhóm dày nhất là `4/10`, và phần lớn phân bố nói về
SỰ THIẾU chứ không về sự lặp lại.** Một bản nháp gom trung thực sẽ mang theo những dòng
kiểu *"bước này 4/10 case ghi lại; 1 case làm qua remote nên không ai biết đã làm gì"*.
Nếu `ISoạnNhápSOP` chỉ mang theo phần lặp lại, nó sẽ trình bày một SOP nghe chắc chắn hơn
dữ liệu đứng sau nó — đúng thứ `G6`/`AP3` sinh ra để chặn.

⚠ Cũng vì thế, **7/13 nhánh của cây có nguồn thật, 6 nhánh còn lại do tôi suy từ kết
luận.** Mỗi nhánh trong file JSON mang một trường `chungCu` nói rõ nó thuộc loại nào, và
`--kiem-cay` bắt buộc trường đó phải có.

---

## 3. Bốn phát hiện đo được

### 3.1 🛑 Trong 115 khối văn bản lặp NGUYÊN VĂN trên toàn corpus, đúng **1** khối chứa bước làm

Đo bằng `nhom_sop.py --trung-lap --toi-thieu 300` trên cả 150 case: 115 cặp case chia nhau
một khối ≥300 ký tự giống hệt. Phân loại 115 khối đó:

```text
91   câu trả lời MẪU        "Kính gửi Quý khách…", thông báo kênh Zalo đang thử nghiệm,
                            "Trạng thái yêu cầu: chờ khách phản hồi"
21   nhắc việc TỰ ĐỘNG      "Issue này đã 3 ngày khách hàng không phản hồi…"
 2   form yêu cầu nội bộ
 1   BƯỚC LÀM THẬT          ES-341290 ↔ ES-346396 — khối 784 ký tự
```

→ **Cách làm hiển nhiên nhất đã bị bác bằng số:** *"tìm văn bản lặp lại nhiều lần thì ra
SOP"* cho **độ chính xác 1/115 = 0,9%**. Corpus lặp lại liên tục, nhưng gần như toàn bộ
sự lặp lại là **bản mẫu được dán tự động**, không phải tri thức được truyền lại.

### 3.2 ✅ SOP của nhóm này ĐÃ TỒN TẠI — dưới dạng đoạn copy-paste trong đầu một nhân viên

Khối 784 ký tự đó là một SOP hoàn chỉnh: `B1` thêm người dùng, `B2` phân quyền ký hiệu,
kèm **một nhánh điều kiện** (*"nếu chưa có ký hiệu thì thêm ký hiệu như sau"*) và 3 ảnh
minh hoạ. Nó được **cùng một nhân viên** gõ ra ở hai case cách nhau **21 ngày** (10/08 và
31/08), giống nhau tới từng ký tự.

→ Đây là *"10%"* của con số `10 / 30 / 60`, nhìn thấy tận mắt: nó **không** nằm trong tài
liệu ai cũng tìm được, nó nằm **trong luồng ticket** và trong bộ đệm copy-paste của một
người. Hệ quả cho sản phẩm: với nhóm này, việc của Path A **không phải phát minh ra SOP**
mà là **tìm lại và tái dùng** một SOP đã có. Hai việc đó có thước đo khác nhau — và cái
thứ hai kiểm chứng được, vì đã có bản gốc để đối chiếu.

### 3.3 🛑 Không tín hiệu văn bản nào tách được bốn nguyên nhân — và đây là CƠ CHẾ đứng sau AUC 0,61

Hai phép đo độc lập trên cùng nhóm:

- **Triệu chứng khách kể trùng nhau.** Cả bốn nguyên nhân vào bằng cùng một câu.
- **Đích đến cũng trùng.** Trang `admin.ezinvoice.vn` được nhắc ở **6 case thuộc 3 nhóm
  nguyên nhân khác nhau** — nên *"kết thúc tại một màn quản trị"* không phải dấu hiệu
  nhận dạng nhóm này, dù mô tả nhóm trong taxonomy có ghi như vậy.

→ `docs/09` §5 đo được **AUC 0,61** và kết luận "tín hiệu văn bản quá yếu". Mục này nói
**vì sao**, chứ không chỉ nói **bao nhiêu**: thứ phân nhánh **không nằm trong văn bản**.
Nó là một **bước kiểm phải THỰC HIỆN** — mở đúng màn hình, hỏi đúng một câu về tài khoản.
Retrieval mạnh hơn không lấp được khoảng đó, vì thông tin cần thiết chưa từng được viết ra.

⚠ **Nhưng đọc đúng phạm vi:** đây là phát biểu về nhóm này trên nguồn này. Một nguồn có
trường có kiểm soát (CRM: stage, giá trị, ngành) thì bước kiểm có thể đã nằm sẵn trong dữ
liệu. `G12` — đặc điểm dữ liệu của một khách là tham số, không phải hằng số thiết kế.

### 3.4 ⚠ Một case mang HAI nguyên nhân thuộc HAI nhóm — nên "một case = một nguyên nhân" là giả định của phép đếm

`ES-341290` chứa cả nguyên nhân của nhóm này (chưa phân quyền ký hiệu) **và** nguyên nhân
của nhóm *"xung đột phiên do nhiều tài khoản trên cùng một trình duyệt"* (lệch số thập
phân). Taxonomy buộc mỗi case vào **đúng một** nhóm — và phép kiểm bằng code của `docs/09`
xác nhận không case nào bị gán trùng, tức ca hai-nguyên-nhân **bị đếm một lần**.

→ Hệ quả cho `R-K4`: con số **19 nhóm** là cận dưới thêm một lần nữa, vì một lý do mới —
không phải vì 41% không xác định được, mà vì **đơn vị đếm là case chứ không phải nguyên
nhân**. Và hệ quả cho SOP: cây phải có **bước loại trừ** (`K5`), nếu không thì một case
hai nguyên nhân sẽ được đóng khi mới sửa xong một nửa.

---

## 4. Hệ quả cho `ISoạnNhápSOP` — hình dạng đầu ra, chưa chốt

Làm tay xong thì thấy một bản nháp dùng được phải mang theo, **cho từng bước kiểm**:

```text
câu hỏi            hỏi gì / thử gì            "ký hiệu đã được chọn chưa?"
nơi xem            mở màn hình nào            và nói rõ là màn hình NÀO, vì hai
                                              nguyên nhân dùng hai màn khác nhau
giá trị quan sát   các đáp án có thể          không phải mô tả tự do
nhánh              mỗi đáp án đi tới đâu      bước sửa, hay bước kiểm tiếp
nguồn              case nào GHI bước kiểm     rỗng là hợp lệ, và là thông tin
mức chứng cứ       đo được / suy ra           BẮT BUỘC — 6/13 nhánh ở đây là suy ra
số case đứng sau   phân bố kiểu S8            kể cả khi con số là 0
```

Hai điều rút ra, cả hai **chưa chốt**, ghi vào để không mất:

- **Trường `mức chứng cứ` phải là bắt buộc ở TẦNG NHÁNH, không phải tầng SOP.** Một SOP có
  4 nhánh có nguồn và 6 nhánh suy ra thì nhãn ở tầng SOP nói được gì? Không gì cả.
- **`M2` đo `diff(A,B)` — và nhánh không có nguồn chính là chỗ người duyệt sẽ sửa.** Nếu
  bản nháp nói rõ nhánh nào là suy đoán, thì diff trở thành **tín hiệu** (*người duyệt sửa
  đúng chỗ ta đã cảnh báo*) thay vì **nhiễu**. Đây là một lựa chọn thiết kế có ảnh hưởng
  trực tiếp tới thước đo, nên nó cần được quyết chứ không nên tự chọn khi code.

---

## 5. Hai chỗ tài liệu ghi chưa đúng, phát hiện khi làm

- 🛑 **`10` §6 và `00` ghi rằng bằng chứng then chốt `ES-346396` "phải kéo lại từ Jira".
  Sai — nó đã nằm trong corpus 150 case trên đĩa** (`fixture-cases.json`). Lần xuất
  2026-09-05 dùng `resolved >= -365d` nên nó đã lọt vào cửa sổ. Làm theo tài liệu là ~6
  phút và một lần chạm vào Jira thật không cần thiết. → `nhom_sop.py --nhom` lấy ra tại chỗ.
- ⚠ **`00` ghi rằng repo "KHÔNG có bước kiểm nào". Đúng một nửa.** `nguyen-nhan-150-case.json`
  có trường `buocXuLy` cho từng case — nhưng nội dung nó là **nhãn LOẠI bước** (`"kiểm phân
  quyền"`, `"hướng dẫn khách"`, `"remote vào máy"`), không phải bước kiểm có *câu hỏi + nơi
  xem + giá trị quan sát*. Khoảng trống là thật, nhưng nó là khoảng trống **về độ phân
  giải**, không phải về sự tồn tại. Phân biệt này quan trọng vì nó đổi việc phải làm: không
  phải đi lấy dữ liệu mới, mà là rút bước kiểm ra khỏi evidence đã có.
- ⚠ Và một chỗ nữa, đã sửa cùng ngày ở `00`: **không có "một nhóm SOP lớn nhất"** — có
  **hai** nhóm cùng 10 case. Chọn nhóm *Phân quyền & ký hiệu* là một lựa chọn (nó chứa
  `ES-346396`), không phải một hệ quả.

---

## 6. Một lỗi của chính phép kiểm vừa viết — cùng hình dạng với `IM-22`

Bản đầu của `--trung-lap` băm cửa sổ trượt với **bước 20 ở cả hai chuỗi**. Hai chuỗi lấy
cửa sổ theo hai lưới riêng, nên một đoạn giống hệt nhau chỉ khớp khi độ lệch vị trí giữa
hai bên đúng bội số của 20 — **xác suất 1/20**.

Nó bỏ sót **đúng khối 784 ký tự đã motivate cả script**, và báo "12 cặp" thay vì 115.

```text
bước 20   →  12 cặp,  KHÔNG có khối SOP    ← phép kiểm báo "không có"
bước  1   → 115 cặp,  CÓ khối SOP          ← 55 giây, chạy một lần
```

🛑 **Tìm ra chỉ vì con số đo bằng code được đem so với con số đo bằng tay.** Nếu tôi tin
script ngay từ đầu, kết luận của §3.1 sẽ là *"corpus không chứa SOP nào"* — ngược hẳn với
sự thật. Cùng hình dạng với `IM-22` (guard báo xanh trong khi dữ liệu đang rò) và với
`AR-k` (trường phân loại là hằng số): **một phép kiểm im lặng trả về "không thấy gì" thì
không phân biệt được với "không có gì".** Chú thích 6 dòng giải thích chuyện này nằm ngay
cạnh hằng số `BUOC` trong script, cố ý.

---

## 7. Chạy lại

```bash
# Đếm case của từng nhóm. Cần corpus, vì nó đếm cả số mẩu evidence.
python scripts/jira-export/nhom_sop.py --liet-ke

# Lấy nguyên văn vật liệu của một nhóm ra đọc
python scripts/jira-export/nhom_sop.py --nhom "Phân quyền" --ra nhom.txt

# Tìm đoạn lặp nguyên văn qua nhiều case (~55 giây trên 150 case)
python scripts/jira-export/nhom_sop.py --trung-lap --toi-thieu 300

# Đối chiếu cây quyết định với taxonomy — mã thoát ≠ 0 nếu phép cộng lệch.
# KHÔNG cần corpus, nên chạy được trên máy trắng và cắm được vào CI.
python scripts/jira-export/nhom_sop.py --kiem-cay \
  docs/ket-qua-phan-tich/cay-quyet-dinh-phan-quyen-ky-hieu.json
```

`--kiem-cay` đã được thử bằng **5 phép đột biến** (bỏ một case khỏi phân bố · đếm một case
hai lần · nhánh trỏ tới bước sửa không tồn tại · case lạ không thuộc nhóm · nhánh mất
trường `chungCu`) — **5/5 bị bắt**. Nó là bộ xương của eval sau này cho `ISoạnNhápSOP`:
máy sinh ra cây thì cũng phải đi qua đúng phép kiểm này.

⚠ Corpus **không** theo git (dữ liệu khách hàng). Ba lệnh đầu cần dựng lại corpus theo
`10` §3. Mặc định đọc `fixture-*.json` — bản đã thay credential bằng giá trị giả giữ
nguyên hình dạng — chứ không đọc `dry-run-*.json`.

---

## 8. Điều mục này KHÔNG kết luận

```text
KHÔNG nói  cây quyết định này đúng      chưa ai làm support duyệt nó. 6/13 nhánh là suy đoán.
KHÔNG nói  19 nhóm nào cũng dựng được   làm 1/19 nhóm, và là nhóm dày nhất, dễ nhất.
KHÔNG nói  văn bản không đáng dùng      nói rằng trên NGUỒN NÀY, cho NHÓM NÀY, thứ phân
                                        nhánh không nằm trong văn bản.
KHÔNG nói  Path A nên bỏ retrieval      AR4 vẫn đúng: FTS trước, đo rồi mới quyết. Xem
                                        07 §5 R-K4 về việc đó là điều kiện chạy lại được.
```

**Việc kế tiếp mà mục này mở ra, chưa làm:** (1) đưa cây cho một người làm support thật
duyệt — đó là phép kiểm duy nhất chưa ai chạy được, và nó cũng là baseline đầu tiên cho
`M2`; (2) dựng cây thứ hai cho nhóm *NCC từ chối payload* (10 case, 21 mẩu — gấp 1,5 lần
vật liệu của nhóm này) để biết hình dạng đầu ra ở §4 có chịu được nhóm khác hay không.
