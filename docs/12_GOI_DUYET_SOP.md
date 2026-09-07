# 12 — GÓI DUYỆT: hai bản nháp quy trình xử lý, cần người làm support sửa

> **File này được SINH TỰ ĐỘNG** từ `scripts/jira-export/goi_duyet.py` và từ hai file
> cây quyết định. Nếu bạn bảo trì repo thì đừng sửa trực tiếp vào đây — sửa file
> JSON rồi sinh lại. Người DUYỆT thì cứ ghi thoải mái vào bản của mình.

---

## Gửi anh/chị làm support

Chúng tôi đang thử dựng lại **quy trình xử lý** cho hai loại sự cố hay gặp về hoá đơn
điện tử, bằng cách đọc lại 20 ticket đã đóng trên Jira. Kết quả ở dưới.

**Chỗ chúng tôi chắc chắn còn sai: ticket ghi được *kết luận*, nhưng hầu như không ghi
*cách anh/chị biết*.** Có ticket kết thúc bằng đúng tám chữ — *"Sai CCCD"*. Những
chỗ như vậy chúng tôi **tự đoán**, và đã đánh dấu ⚠ **CHÚNG TÔI TỰ ĐOÁN** ngay tại chỗ đoán.
Chỉ anh/chị nói được chỗ nào đoán sai.

**Mất bao lâu:** khoảng 20—30 phút. **Không cần đọc lại ticket** — mã ticket để
ở đó cho anh/chị tra khi muốn, không phải để anh/chị đi kiểm tra chúng tôi.

**Trả lời thế nào:** ghi thẳng vào cột **“Sửa lại”**, hoặc trả lời theo số câu ở phần
cuối. Đúng thì để trống — **ô trống nghĩa là “đúng”**, không phải “chưa xem”.

🛑 **Chỗ hữu ích nhất lại là chỗ không có ô trống nào sẵn:** *bước kiểm nào chúng
tôi KHÔNG CÓ trong danh sách* — thứ anh/chị vẫn làm mà không ai ghi vào ticket.
Xin ghi vào mục **“Còn thiếu”** ở cuối mỗi phần. Kể cả *"gọi điện hỏi khách"* hay
*"remote vào xem"* — đó vẫn là bước kiểm, và đúng là loại không bao giờ được ghi lại.

---

# Phần 1. Phân quyền & ký hiệu hoá đơn chưa sẵn sàng

**Số ticket đứng sau phần này:** 10

## 1.1 Khách báo vào bằng những câu nào

| Khách nói / màn hình báo | Ticket | Sửa lại |
|---|---|---|
| "không xuất được hoá đơn" kèm một ảnh chụp màn hình | ES-343662, ES-341317 | |
| form phát hành báo đỏ / "Validation form error" | ES-341290, ES-346647 | |
| màn phát hành KHÔNG hiện mục "Ký hiệu hoá đơn" để chọn, hoặc chọn thì danh sách rỗng | ES-346039, ES-346396 | |
| xem trước PDF trả "Ký hiệu hoá đơn không tồn tại" | ES-346559 | |
| không mở được màn kế toán / xuất hoá đơn | ES-343733 | |

> ⚠ Bốn nguyên nhân bên dưới có triệu chứng KHÁCH KỂ gần như trùng nhau. Thứ phân nhánh là một bước kiểm phải THỰC HIỆN, không phải một câu phải ĐỌC. **Đúng không, hay khách vẫn nói khác nhau ở chỗ nào?**

## 1.2 Các bước kiểm, theo thứ tự chúng tôi đoán

> 🛑 **Thứ tự này chúng tôi TỰ ĐẶT.** Không ticket nào ghi thứ tự kiểm, nên
> đây là chỗ sai nhiều nhất. Nếu thực tế anh/chị kiểm theo thứ tự khác, xin ghi ra.

### K1. Trên form phát hành của ĐÚNG đặt phòng đang lỗi, ký hiệu hoá đơn đã được chọn chưa?

- **Xem ở:** Form phát hành hoá đơn trong PMS, tại đặt phòng khách báo lỗi
- **Vì sao chúng tôi đặt ở bước này:** Rẻ nhất, và là nguyên nhân của 3/10 case. Làm được ngay trên ảnh chụp khách đã gửi.
- **Lấy từ ticket:** "Anh chọn giúp em kí hiệu hóa đơn xem có phát hành được không ạ." — bước kiểm được gõ ra dưới dạng một PHÉP THỬ, không phải một câu hỏi.

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| chưa chọn | `S0` | ✔ có ca thật | ES-343662, ES-341317, ES-341290 | |
| mở ra thì danh sách ký hiệu RỖNG, không có gì để chọn | `K2` | ✔ có ca thật | ES-346396, ES-346039 | |
| không mở được cả màn hình đó | `K3` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | ES-343733 | |
| đã chọn, vẫn báo lỗi | `K4` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | ES-346559 | |

### K2. Tài khoản nào đang phát hành, và tài khoản đó đã được thêm + phân quyền ký hiệu trên trang quản trị hoá đơn chưa?

- **Xem ở:** admin.ezinvoice.vn → Cấu hình → Quản lý người dùng, rồi Cấu hình → Ký hiệu → User truy cập/Nhóm truy cập
- **Vì sao chúng tôi đặt ở bước này:** Nhánh lớn nhất: 4/10 ca CHẨN ĐOÁN, cộng ES-344424 là hỏi-đáp cùng nguyên nhân nên 5/10 case của nhóm. Và đây là BƯỚC KIỂM PHÂN NHÁNH THẬT: hai case ghi rõ nó dưới dạng một câu hỏi về TÀI KHOẢN, không về triệu chứng.
- **Lấy từ ticket:** "Anh phát hành bằng người dùng <…> này có phải không ạ" (ES-346396) · "Mình cho em email tài khoản mình đang dùng đăng nhập ạ" (ES-346039).

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| tài khoản chưa có trong danh sách người dùng | `S1` | ✔ có ca thật | ES-346396 | |
| có người dùng nhưng chưa được phân quyền ký hiệu | `S2` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | ES-346039, ES-346647, ES-346065 | |
| danh sách ký hiệu của cả cơ sở đang rỗng | `S3` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | — | |
| cả hai đều đủ | `K3` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | — | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** Muốn THAO TÁC phân quyền thì phải đăng nhập bằng một tài khoản ĐÃ được phân quyền. Nhân viên phải chỉ rõ dùng tài khoản nào (ES-346396) — nếu không, khách sẽ thử bằng chính tài khoản đang bị chặn.

### K3. Role của tài khoản trong PMS có quyền "Thêm hoá đơn" không?

- **Xem ở:** Phân quyền role trong PMS — KHÔNG phải trên trang quản trị hoá đơn
- **Vì sao chúng tôi đặt ở bước này:** Đây là chỗ dễ đi sai hướng nhất: triệu chứng khách kể giống hệt K2, nhưng chỗ phải sửa là MỘT MÀN QUẢN TRỊ KHÁC. Nhầm chỗ thì kiểm mãi không thấy gì sai.
- ⚠ **Chỗ chúng tôi không biết:** ES-343733 KHÔNG ghi bước kiểm nào: nhân viên xin remote vào máy khách rồi sửa role tại đó. Nguyên nhân biết được, đường đi tới nó thì không.

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| role thiếu quyền "Thêm hoá đơn" | `S4` | ✔ có ca thật | ES-343733 | |
| role đủ quyền | `K4` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | — | |

### K4. Đây có phải cơ sở / mã số thuế MỚI vừa được tạo không? Nếu có: đã khai cấu hình ký hiệu cho TỪNG cơ sở chưa, và tài khoản quản trị có vào được mã số thuế mới không?

- **Xem ở:** Cấu hình hoá đơn của từng cơ sở, và thử đăng nhập trang quản trị bằng mã số thuế mới
- **Vì sao chúng tôi đặt ở bước này:** 1/10 case, nhưng nó là nhánh duy nhất mà mọi bước trên đều 'đúng' và vẫn lỗi.

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| một trong các cơ sở chưa được khai đủ | `S5` | ✔ có ca thật | ES-346559 | |
| tài khoản quản trị chỉ vào được mã số thuế CŨ | `S5` | ✔ có ca thật | ES-346559 | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** "Ký hiệu này khách vẫn đang xuất bình thường tại nhà cung cấp" KHÔNG chứng minh ký hiệu đã được khai trong hệ thống. Trong ES-346559 chính câu đó làm phép kiểm bị bỏ qua một vòng.

### K5. Trình duyệt đang đăng nhập nhiều tài khoản cùng lúc không?

- **Xem ở:** Chính trình duyệt của khách
- **Vì sao chúng tôi đặt ở bước này:** Bước LOẠI TRỪ, không thuộc nhóm này — nhưng phải có mặt, vì ES-341290 mang THÊM đúng nguyên nhân đó (lệch số thập phân) và nó thuộc nhóm khác. Bỏ bước này thì một case hai nguyên nhân sẽ được đóng khi mới sửa xong một nửa.

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| có nhiều tài khoản trên cùng trình duyệt | `NGOAI-PHAM-VI` | ✔ có ca thật | ES-341290, ES-343036 | |

## 1.3 Cách sửa

| Mã | Việc | Ticket | Sửa lại |
|---|---|---|---|
| `S0` | Chọn ký hiệu hoá đơn trên form phát hành rồi phát hành lại. | ES-343662, ES-341317, ES-341290 | |
| `S1` | Thêm người dùng trên trang quản trị hoá đơn: Cấu hình → Quản lý người dùng → mời thêm tài khoản khác. | ES-346396 | |
| `S2` | Phân quyền ký hiệu: Cấu hình → Ký hiệu → chọn ký hiệu cần phân quyền → mục User truy cập/Nhóm truy cập → chọn người dùng/nhóm → Thêm → Cập nhật. | ES-346396, ES-346039, ES-341290 | |
| `S3` | Thêm ký hiệu nếu danh sách rỗng: Cấu hình → Ký hiệu → Danh sách ký hiệu → dấu + → nhập Ký hiệu / Mẫu hoá đơn → Lưu. | ES-346396 | |
| `S4` | Thêm quyền "Thêm hoá đơn" vào role trong PMS. | ES-343733 | |
| `S5` | Khai đủ cấu hình ký hiệu cho TỪNG cơ sở, và cấp quyền truy cập trang quản trị theo mã số thuế mới. | ES-346559 | |

## 1.4 Khi nào coi là xong

**Phát hành lại ĐÚNG đặt phòng ban đầu (hoặc xem trước PDF) và xác nhận ra được số hoá đơn.**

> ⚠ Trong lô ticket đã đọc, xác nhận hầu như luôn do NHÂN VIÊN nói ("Em thấy hoá đơn đã xuất thành công rồi ạ"), không do khách xác nhận. Ba case kết thúc bằng khách nói "cảm ơn" — đó không phải xác nhận đã hết lỗi.

Điều kiện này đủ chưa? Nếu chưa thì còn thiếu gì:

```


```

## 1.5 6 chỗ chúng tôi TỰ ĐOÁN — xin trả lời từng chỗ

Mỗi chỗ chỉ cần một trong ba: **đúng** / **sai, phải là …** / **không bao giờ xảy ra**.

| # | Ở bước | Chúng tôi đoán rằng | Đúng / Sai / Không có |
|---|---|---|---|
| 1 | `K1` | không mở được cả màn hình đó | |
| 2 | `K1` | đã chọn, vẫn báo lỗi | |
| 3 | `K2` | có người dùng nhưng chưa được phân quyền ký hiệu | |
| 4 | `K2` | danh sách ký hiệu của cả cơ sở đang rỗng | |
| 5 | `K2` | cả hai đều đủ | |
| 6 | `K3` | role đủ quyền | |

## 1.6 Còn thiếu — bước kiểm anh/chị vẫn làm mà không có ở trên

```




```

---

# Phần 2. NCC từ chối payload — tra mã lỗi, sửa dữ liệu nguồn, phát hành lại

*Dạng quy trình chúng tôi thấy: BẢNG TRA + vòng lặp 4 bước — lấy mã lỗi → tra ra TRƯỜNG bị từ chối → sửa trường tại nguồn → phát hành lại và xác nhận CÓ SỐ*

**Số ticket đứng sau phần này:** 10

## 2.1 Khách báo vào bằng những câu nào

| Khách nói / màn hình báo | Ticket | Sửa lại |
|---|---|---|
| hoá đơn dừng ở trạng thái NHÁP, bấm phát hành thì tự rơi về nháp, không báo gì | ES-346027, ES-343712 | |
| màn hình chỉ báo "hệ thống đang xử lý, vui lòng thử lại sau" — KHÔNG có mã lỗi | ES-346136 | |
| trạng thái ghi rõ "NCC từ chối" kèm một mã lỗi | ES-345623, ES-344862 | |
| hoá đơn nháp chưa có số, và hệ thống CHẶN lần phát hành lại vì "đã có hoá đơn nháp" | ES-343348 | |
| không ký/phát hành được, kèm mã lỗi dạng số | ES-345813 | |
| báo lỗi khi xuất, phát hiện qua kênh nội bộ (triển khai báo lên) | ES-345257 | |
| hoá đơn nháp KHÔNG TÌM THẤY trên màn danh sách | ES-343712 | |
| hoá đơn đoàn không lên được, trong khi hoá đơn phòng lẻ bình thường | ES-344862 | |

## 2.2 Các bước kiểm, theo thứ tự chúng tôi đoán

> 🛑 **Thứ tự này chúng tôi TỰ ĐẶT.** Không ticket nào ghi thứ tự kiểm, nên
> đây là chỗ sai nhiều nhất. Nếu thực tế anh/chị kiểm theo thứ tự khác, xin ghi ra.

### K1. Người đang xử lý có XEM ĐƯỢC mã lỗi mà nhà cung cấp trả về không?

- **Xem ở:** Nhật ký → Nhật ký kết nối → Loại: Kết nối ezinvoice
- **Cần quyền:** quyền vào mục Nhật ký / cấu hình — lễ tân thường KHÔNG có
- **Vì sao chúng tôi đặt ở bước này:** Cả nhóm treo trên bước này: có mã lỗi thì tra bảng là xong, không có mã thì phải soát mò. Nhưng nó cũng là bước HAY BỊ CHẶN NHẤT — xem hai nhánh cuối.
- **Lấy từ ticket:** "Nếu trường hợp tương tự xảy ra anh vào mục Nhật ký >> Nhật ký kết nối >> chọn Loại: Kết nối ezinvoice để kiểm tra log trước giúp em" (ES-345623) — bước kiểm được dạy lại cho khách, đúng dạng SOP.

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| có mã lỗi | `BANG` | ✔ có ca thật | ES-345623, ES-344862, ES-345257, ES-345857, ES-343712, ES-345813 | |
| KHÔNG có mã nào — màn hình chỉ nói "đang xử lý, thử lại sau", hoặc hoá đơn im lặng rơi về nháp | `K2` | ✔ có ca thật | ES-346136, ES-346027, ES-343348, ES-342328 | |
| không có QUYỀN vào mục Nhật ký để xem mã lỗi | `S-CHUYEN` | ✔ có ca thật | ES-345623 | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** 🛑 Khách trả lời ngay: "Đâu phải ai cũng có quyền vào mục cấu hình này đâu em." Bước kiểm ĐẦU TIÊN của nhóm này bị chặn bởi đúng nguyên nhân của NHÓM 1 (phân quyền). Hai nhóm nguyên nhân độc lập trong bảng phân nhóm lại phụ thuộc nhau ở tầng THAO TÁC.

### K2. Không có mã lỗi thì soát bốn trường hay bị từ chối nhất, trên chính hoá đơn nháp.

- **Xem ở:** Danh sách hoá đơn → chọn hoá đơn nháp → Xem hoá đơn
- **Vì sao chúng tôi đặt ở bước này:** 4/10 case KHÔNG có mã lỗi nào để tra. Với chúng, đây là bước duy nhất còn lại — và nó là soát danh sách hữu hạn, không phải đoán.

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| số CCCD thừa hoặc thiếu chữ số (đúng phải 12) | `S1` | ✔ có ca thật | ES-346136, ES-343348 | |
| mã số thuế thừa dấu cách hoặc không hợp lệ | `S1` | ✔ có ca thật | ES-346027 | |
| số giấy tờ và số hộ chiếu nhập TRÙNG NHAU | `S1` | ✔ có ca thật | ES-342328 | |
| soát thấy "đúng hết" mà vẫn không phát hành được | `S-REMOTE` | ✔ có ca thật | ES-343348 | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** ⚠ Nhánh cuối là ca THẬT, không phải phòng xa: ES-343348 khách khẳng định "đúng mà", nhân viên xin remote vào máy rồi kết luận đúng một dòng — "Sai CCCD" (8 ký tự). Khách tự soát KHÔNG thay được bước kiểm này.

### K3. Sửa xong rồi thì phát hành LẠI từ đâu — có tìm thấy hoá đơn nháp không?

- **Xem ở:** Danh sách hoá đơn (KHÔNG lọc theo ngày) → chọn hoá đơn nháp → Xem hoá đơn → Phát hành
- **Vì sao chúng tôi đặt ở bước này:** Bước này trông như thủ tục nhưng có 2/10 case tắc ở đúng đây, và một trong hai là BUG SẢN PHẨM, không phải lỗi dữ liệu.
- **Lấy từ ticket:** "Hoá đơn đã tạo hoá đơn nháp rồi ạ" · "anh/chị chọn, và ấn Xem hoá đơn, trong đó sẽ có tuỳ chọn cho phép phát hành hoá đơn" (ES-343712).

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| tìm không thấy hoá đơn nháp vì đang lọc theo ngày hoá đơn | `S-TIM` | ✔ có ca thật | ES-343712 | |
| tìm theo SỐ HOÁ ĐƠN không ra — màn tìm kiếm bị lỗi | `S-TIM` | ✔ có ca thật | ES-343712 | |
| hệ thống chặn phát hành lại vì "đã tồn tại hoá đơn nháp" | `S2` | ✔ có ca thật | ES-343348, ES-345857 | |
| thấy hoá đơn nháp, sửa được | `K-XN` | ✔ có ca thật | ES-346027, ES-343712 | |

## 2.3 Bảng tra mã lỗi

> 🛑 **Nếu cả file này chỉ sửa được một thứ, xin sửa bảng này.** Chính khách
> đã hỏi “có bảng liệt kê mã lỗi không” và được trả lời là chưa có.

| Nhà cung cấp | Mã lỗi | Trường bị từ chối | Sửa ở đâu | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|---|---|
| VNPT | `ERR:1518 / ERR.1518` | trường giấy tờ của khách (CCCD/hộ chiếu) — sai định dạng | `S1` | ✔ có ca thật | ES-345623, ES-344862 | |
| VNPT | `5000` | mã số thuế HOẶC tên người mua không hợp lệ | `S1` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | ES-345813 | |
| VNPT | `SDThoai không hợp lệ` | số điện thoại người mua — chứa mã quốc gia +84 và dấu ngoặc | `S3` | ✔ có ca thật | ES-345257 | |
| VNPT | `InvalidInvoiceDate` | ngày hoá đơn — payload trả về ngày mặc định 0001-01-01 | `S4` | ✔ có ca thật | ES-345857 | |
| MISA | `số hoá đơn không liên tục` | KHÔNG phải trường dữ liệu khách — thuộc dải số của nhà cung cấp | `S-NCC` | ✔ có ca thật | ES-343712 | |
| (bất kỳ) | `KHÔNG CÓ MÃ — chỉ "đang xử lý, thử lại sau" hoặc im lặng rơi về nháp` | chưa biết → phải soát tay bốn trường ở K2 | `K2` | ✔ có ca thật | ES-346136, ES-346027, ES-343348, ES-342328 | |

**Còn mã lỗi nào hay gặp mà bảng chưa có? Mã đó trỏ tới trường nào?**

```



```

## 2.4 Cách sửa

| Mã | Việc | Ticket | Sửa lại |
|---|---|---|---|
| `S1` | Sửa trường bị từ chối tại hồ sơ khách hoặc trên hoá đơn nháp, rồi phát hành lại. | ES-346136, ES-346027, ES-342328, ES-345623, ES-345813, ES-343348 | |
| `S2` | Không tạo hoá đơn mới: mở ĐÚNG hoá đơn nháp đang tồn tại, sửa thông tin rồi phát hành từ trong đó (hoặc xoá nháp nếu sai hẳn). | ES-343348, ES-345857, ES-343712 | |
| `S3` | Sửa số điện thoại người mua về định dạng NCC nhận: bỏ mã quốc gia +84 và mọi dấu ngoặc. | ES-345257 | |
| `S4` | Sửa ngày hoá đơn trên hoá đơn nháp. | ES-345857 | |
| `S-NCC` | Không sửa dữ liệu khách — chuyển sang nhà cung cấp / dev vì nguyên nhân nằm ở dải số hoá đơn. | ES-343712 | |
| `S-REMOTE` | Xin phiên remote để tự soát, khi khách đã soát và khẳng định dữ liệu đúng. | ES-343348, ES-342328 | |
| `S-CHUYEN` | Người đang xử lý không có quyền xem log → yêu cầu quản lý/admin phân quyền, hoặc chuyển cho support. | ES-345623 | |
| `S-TIM` | Vào Danh sách hoá đơn KHÔNG lọc theo ngày; nếu tìm theo số hoá đơn không ra thì tìm bằng ô "Thông tin khác". | ES-343712 | |

## 2.5 Khi nào coi là xong

**Phát hành lại và xác nhận hoá đơn ĐÃ CÓ SỐ.**

> ⚠ 🛑 Điều kiện đóng phải là "đã có SỐ hoá đơn", KHÔNG phải "hết báo lỗi" — vì 4/10 case ngay từ đầu đã không có báo lỗi nào. Trong lô ticket đã đọc, ES-343348 tự hỏi đúng câu này: "hệ thống không cho chị phát hành lại, nói là có hoá đơn nháp rồi, trong khi hoá đơn nháp chưa có số".

Điều kiện này đủ chưa? Nếu chưa thì còn thiếu gì:

```


```

## 2.6 1 chỗ chúng tôi TỰ ĐOÁN — xin trả lời từng chỗ

Mỗi chỗ chỉ cần một trong ba: **đúng** / **sai, phải là …** / **không bao giờ xảy ra**.

| # | Ở bước | Chúng tôi đoán rằng | Đúng / Sai / Không có |
|---|---|---|---|
| 1 | `bảng` | VNPT / 5000 | |

## 2.7 Còn thiếu — bước kiểm anh/chị vẫn làm mà không có ở trên

```




```

---

# Phần cuối. Năm câu xin trả lời bằng chữ

**Câu 1.** Thứ tự các bước kiểm ở trên có đúng thứ tự anh/chị THỰC SỰ làm không? Nếu không, thứ tự đúng là gì?

```


```

**Câu 2.** Có bước nào ở trên mà thực tế **không ai làm** vì không đáng làm — tức chúng tôi đã thêm việc vô ích?

```


```

**Câu 3.** Bước kiểm nào hay phải làm mà **không tự làm được** — thiếu quyền, thiếu màn hình, hay phải nhờ người khác?

```


```

**Câu 4.** Nếu đưa hai quy trình này cho một bạn **mới vào làm**, chỗ nào bạn đó sẽ hiểu sai hoặc làm sai thứ tự?

```


```

**Câu 5.** Có loại sự cố hoá đơn nào **hay gặp hơn** hai loại này mà chúng tôi bỏ qua không?

```


```

---

Cảm ơn anh/chị. Phần anh/chị sửa được dùng để đo xem bản nháp do máy dựng còn lệch
bao nhiêu so với người làm thật — nên **chỗ nào anh/chị sửa nhiều nhất là chỗ
có giá trị nhất**, không phải chỗ làm chúng tôi mất mặt.
