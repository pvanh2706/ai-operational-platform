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
> 📌 **§1-§8 nói về nhóm ĐẦU TIÊN. §9 là nhóm THỨ HAI, làm cùng ngày** — và nó sửa
> kết luận của §4: hai nhóm cùng chủ đề mà **topology khác nhau**, nên đầu ra không được
> cứng hoá là "cây". Đọc §4 xong thì đọc tiếp §9.1.
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

> 🛑 **ĐÃ ĐƯỢC §9 SỬA, cùng ngày — đọc cả hai.** Bảng dưới đây đúng nhưng **chưa đủ**:
> nhóm thứ hai đòi thêm **ba trường nữa** (nhà cung cấp là một phần của khoá · bước kiểm
> có thể bị chặn bởi QUYỀN · điều kiện đóng), và nó **không có hình cây**. Xem §9.1.

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

## 5. Ba chỗ tài liệu ghi chưa đúng, phát hiện khi làm

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
python scripts/jira-export/nhom_sop.py --kiem-cay \
  docs/ket-qua-phan-tich/cay-quyet-dinh-ncc-tu-choi-payload.json
```

`--kiem-cay` đã được thử bằng **11 phép đột biến, 11/11 bị bắt**. Năm phép đầu trên cây
(bỏ một case khỏi phân bố · đếm một case
hai lần · nhánh trỏ tới bước sửa không tồn tại · case lạ không thuộc nhóm · nhánh mất
trường `chungCu`). Sáu phép sau trên **bảng tra** của nhóm 2: hai dòng cùng khoá
(nhà cung cấp, mã lỗi) · dòng thiếu trường bị từ chối · dòng trỏ tới bước sửa không tồn
tại · dòng mất `chungCu` · dòng mang case ngoài nhóm · bỏ `buocXacNhan` khi còn nhánh trỏ
tới nó. Nó là bộ xương của eval sau này cho `ISoạnNhápSOP`: máy sinh ra bản nháp thì
cũng phải đi qua đúng phép kiểm này.

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
`M2`; ~~(2) dựng cây thứ hai cho nhóm *NCC từ chối payload*~~ — ✅ **ĐÃ LÀM cùng ngày, kết
quả ở §9.** Câu trả lời: phần lõi chịu được, topology thì không.

---

## 9. CÂY THỨ HAI, cùng ngày — phần lõi chịu được, TOPOLOGY thì không

> Viết ngay sau §1-§8 để trả lời đúng câu §8 đặt ra: hình dạng đầu ra ở §4 có chịu được
> nhóm khác không? Nhóm thử: *NCC từ chối payload* (10 case, **21 mẩu** — gấp 1,5 lần
> vật liệu của nhóm đầu). Bản máy đọc được:
> [`cay-quyet-dinh-ncc-tu-choi-payload.json`](ket-qua-phan-tich/cay-quyet-dinh-ncc-tu-choi-payload.json).

**Câu trả lời: KHÔNG, không phải như một cái cây.**

```text
nhóm 1   CÂY PHÂN NHÁNH LỒNG NHAU     mỗi bước kiểm thu hẹp bước sau; bốn nguyên nhân
         5 bước kiểm · 13 nhánh        là bốn TẦNG phân quyền, kiểm từ ngoài vào trong

nhóm 2   BẢNG TRA + VÒNG LẶP 4 BƯỚC   lấy mã lỗi → tra ra TRƯỜNG bị từ chối → sửa
         3 bước kiểm · 6 dòng bảng     trường tại nguồn → phát hành lại, xác nhận CÓ SỐ
                                       Các dòng khác nhau về TRƯỜNG, giống hệt nhau
                                       về THAO TÁC. Không có gì để lồng.
```

→ **`ISoạnNhápSOP` không được cứng hoá đầu ra là "cây".** Phần lõi ở §4 (câu hỏi · nơi
xem · giá trị quan sát · nhánh · nguồn · mức chứng cứ · số case đứng sau) **giữ nguyên,
đúng cho cả hai nhóm** — nhưng một *nhánh* phải trỏ được sang một **dòng bảng**, không
chỉ sang một bước kế tiếp. Phép kiểm `--kiem-cay` đã phải mở rộng đúng chỗ đó, và 6 phép
đột biến mới trên bảng tra đều bị bắt (6/6).

### 9.1 Ba trường MỚI mà nhóm 1 không cần — nên §4 chưa đủ

| Trường | Vì sao nhóm 1 không thấy | Bằng chứng ở nhóm 2 |
|---|---|---|
| **`nhà cung cấp`** là một phần của KHOÁ | Nhóm 1 chỉ có một hệ thống quản trị | VNPT trả `ERR:1518` / `5000` / `SDThoai` / `InvalidInvoiceDate`; MISA trả *"số hoá đơn không liên tục"* — và **dòng MISA là dòng duy nhất mà sửa dữ liệu khách KHÔNG giải quyết được**. Tra bảng bằng mã lỗi một mình sẽ dẫn người ta đi sửa mò. |
| **`quyenCan`** — bước kiểm có thể bị CHẶN | Nhóm 1 mọi bước kiểm đều do support làm | 🛑 Bước kiểm ĐẦU TIÊN của nhóm 2 là *"xem mã lỗi ở Nhật ký → Nhật ký kết nối"*, và khách đáp thẳng: *"Đâu phải ai cũng có quyền vào mục cấu hình này đâu em"* (`ES-345623`). |
| **điều kiện đóng** | Nhóm 1 luôn có một lỗi để hết | Phải là *"đã có **SỐ** hoá đơn"*, KHÔNG phải *"hết báo lỗi"* — vì **4/10 case ngay từ đầu không có báo lỗi nào**. `ES-343348` tự nói ra: *"hệ thống không cho chị phát hành lại, nói là có hoá đơn nháp rồi, trong khi hoá đơn nháp chưa có số"*. |

🛑 **Và trường `quyenCan` mang một phát hiện lớn hơn chính nó: nguyên nhân của NHÓM 1
chặn bước kiểm của NHÓM 2.** Hai nhóm nằm độc lập trong taxonomy — được tách ra bằng
nguyên nhân — nhưng ở **tầng thao tác** thì nhóm 2 phụ thuộc nhóm 1. Một thư viện SOP
xếp theo nguyên nhân sẽ không thấy quan hệ này, vì nó không phải quan hệ nguyên nhân.

### 9.2 ✅ 8/10 case ghi rõ bước kiểm (nhóm 1: 4/10) — và lý do KHÔNG phải vì nhóm này dễ hơn

```text
ghi rõ bước kiểm VÀ có mã lỗi nguyên văn   5/10   ES-345257 · ES-345857 · ES-345623
                                                  · ES-343712 · ES-344862
ghi rõ bước kiểm, KHÔNG có mã lỗi          3/10   ES-346136 · ES-346027 · ES-345813
bước kiểm qua remote, chỉ còn kết luận     2/10   ES-343348 · ES-342328
```

Chỗ dày lên nằm ở **các mẩu NỘI BỘ**: `ES-345257` có một thread phản biện 6 mẩu giữa
triển khai và kỹ thuật; `ES-345857` và `ES-345257` **dán nguyên văn payload** nhà cung
cấp trả về. Không mẩu nào trong số đó là mẩu nói với khách.

→ **Hệ quả đo được cho connector, và nó đổi cách nạp dữ liệu:** nguồn tốt nhất của bước
kiểm là **comment nội bộ**, không phải hội thoại với khách. Corpus hiện lấy cả hai; nếu
một ngày nào đó có ai lọc *"chỉ lấy phần khách đọc được"* — một bộ lọc nghe rất hợp lý,
vì đó là phần sạch và lịch sự — thì sẽ vứt đi đúng 5/10 bước kiểm của nhóm này.

⚠ Cùng hình dạng với phát hiện *"cờ đã xong đi ngược hàm lượng tri thức"* ở `00`: bộ lọc
tự nhiên nhất lại vứt đi đúng phần có giá trị. Đây là lần thứ hai mẫu đó xuất hiện, trên
một chiều khác.

### 9.3 🛑 Bảng tra mã lỗi là thứ khách ĐÃ HỎI và ĐƯỢC TRẢ LỜI LÀ KHÔNG CÓ

`ES-345623`, nguyên văn của khách:

> *"E có bảng liệt kê các mã lỗi không, nhỡ đâu có lúc không liên hệ tụi em được thì còn
> biết đường xử lý"*

Nhân viên trả lời: nhà cung cấp **chưa cung cấp bảng mã lỗi** nên chưa gửi được cho khách
sạn; hệ thống *"đang chỉ gửi diễn giải thông báo cho một số mã lỗi mà NCC xác nhận"*.

→ Đây là **nhu cầu có biên lai**, không phải nhu cầu suy đoán — và bảng 6 dòng dựng từ 10
case chính là artefact đầu tiên sản phẩm này giao được cho nhóm này. Nó cũng là ca dễ
kiểm chứng nhất: đưa bảng cho support, họ nói ngay dòng nào sai.

⚠ Nhưng 1/6 dòng có chứng cứ yếu (mã `5000`, `mucChacChan` của chính bản rút nguyên nhân
là `toi-suy-ra`), và **không dòng nào được nhà cung cấp xác nhận**.

### 9.4 ⚠ Mã lỗi CÓ trong payload nhưng không đến được người sửa

**4/10 case màn hình không hiện mã nào** — chỉ *"hệ thống đang xử lý, vui lòng thử lại
sau"*, hoặc hoá đơn im lặng rơi về nháp. Trong khi hai case **dán nguyên văn JSON** chứng
minh payload có mang `ErrorCode`. Nên bước 1 của SOP nhóm này — *tra mã lỗi* — là **bước
mà hệ thống không cho người sửa làm**, và đường vòng (đọc log) lại cần một quyền họ không có.

Đó là một quan sát về **sản phẩm của khách**, không về corpus: thông tin đã tồn tại, đúng
lúc, đúng chỗ, chỉ không được đưa tới người cần. Sản phẩm này sinh ra để làm đúng việc đó
— nhưng ghi ra ở đây như một **quan sát**, không phải một cam kết tính năng.

### 9.5 Hai con số cập nhật, và một câu chưa ai trả lời

- **Case mang thêm một vấn đề KHÁC: 4/20 = 20%** (nhóm 2 có 3 — khoản thanh toán âm ·
  báo cáo chạy chậm · bug màn tìm kiếm; nhóm 1 có 1). Ranh giới *"một case = một vấn đề"*
  vỡ ở một phần năm số case, đo trên hai nhóm.
- **Nhánh có nguồn: nhóm 2 đạt 16/17, nhóm 1 chỉ 7/13.** Cùng một người viết, cùng một
  ngày, cùng một phương pháp — khác nhau là vật liệu.
- ⚠ **Câu chưa ai trả lời, ghi để không mất** (`ES-345257`): số điện thoại bị NCC từ chối
  được **hệ thống tự kéo từ booking**, không do khách gõ. Triển khai lập luận *"mọi booking
  tương tự sẽ lỗi hàng loạt, đây là việc hai bên kỹ thuật thống nhất"*; kỹ thuật lập luận
  *"hệ thống cho nhập tự do, dữ liệu vốn đã sai định dạng, đây là vấn đề dữ liệu đầu vào"*.
  Cả hai đúng trong phạm vi của mình. **Một SOP sinh tự động từ case này sẽ mặc định rằng
  bảo khách sửa là cách đúng** — và đó là một quyết định sản phẩm bị lẫn vào một bước SOP.
