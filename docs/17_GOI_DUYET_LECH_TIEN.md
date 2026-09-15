# 17 — GÓI DUYỆT: một bản nháp quy trình xử lý, cần người làm support sửa

> **File này được SINH TỰ ĐỘNG** từ `scripts/jira-export/goi_duyet.py` và từ một file
> cây quyết định. Nếu bạn bảo trì repo thì đừng sửa trực tiếp vào đây — sửa file
> JSON rồi sinh lại. Người DUYỆT thì cứ ghi thoải mái vào bản của mình.

---

## Gửi anh/chị làm support

Chúng tôi đang thử dựng lại **quy trình xử lý** cho một loại sự cố hay gặp về hoá đơn
điện tử, bằng cách đọc lại 8 ticket đã đóng trên Jira. Kết quả ở dưới.

**Nhóm này khác: 6/8 ticket CÓ ghi lại cách chẩn đoán**, nên bản dưới đây dựng được
chi tiết hơn — phần lớn chữ trong đó là chữ của chính anh/chị, chúng tôi chỉ xếp lại.
Những chỗ vẫn phải đoán thì được đánh dấu ⚠ **CHÚNG TÔI TỰ ĐOÁN** ngay tại chỗ đoán.
**Điều đó làm câu hỏi đổi:** không phải "chúng tôi đoán sai chỗ nào" mà *"xếp như vậy có
đúng thứ tự anh/chị làm không, và thiếu bước nào"*.

**Mất bao lâu:** khoảng 20—30 phút. **Không cần đọc lại ticket** — mã ticket để
ở đó cho anh/chị tra khi muốn, không phải để anh/chị đi kiểm tra chúng tôi.

**Trả lời thế nào:** ghi thẳng vào cột **“Sửa lại”**, hoặc trả lời theo số câu ở phần
cuối. Đúng thì để trống — **ô trống nghĩa là “đúng”**, không phải “chưa xem”.

🛑 **Chỗ hữu ích nhất lại là chỗ không có ô trống nào sẵn:** *bước kiểm nào chúng
tôi KHÔNG CÓ trong danh sách* — thứ anh/chị vẫn làm mà không ai ghi vào ticket.
Xin ghi vào mục **“Còn thiếu”** ở cuối mỗi phần. Kể cả *"gọi điện hỏi khách"* hay
*"remote vào xem"* — đó vẫn là bước kiểm, và đúng là loại không bao giờ được ghi lại.

---

# Phần 1. Lệch cấu hình tiền/thuế/định dạng số giữa PMS – ezInvoice.Admin – NCC

*Dạng quy trình chúng tôi thấy: BẢNG ĐỐI CHIẾU BA NƠI — khoá một dòng là (CỘT TIỀN bị lệch, NƠI khai sai) → ra cách sửa. Đầu vào của bảng KHÔNG đọc được từ màn hình nào: nó phải được DỰNG RA bằng cách mở cùng một hoá đơn ở ba nơi rồi so từng cột.*

**Số ticket đứng sau phần này:** 8

## 1.1 Khách báo vào bằng những câu nào

| Khách nói / màn hình báo | Ticket | Sửa lại |
|---|---|---|
| "sao hđ bên chị xuất ra toàn để 0% từ đầu tháng 7 tới giờ" — thuế suất trên hoá đơn về 0% trong khi cấu hình vẫn ghi có thuế, và đã kéo dài nhiều tháng trước khi có ai báo. | ES-342493 | |
| "hóa đơn xuất ra lỗi" / "lỗi tiền" — khách chỉ gửi ảnh, chữ duy nhất mô tả sự cố là "sai chỗ trục thành tiền". | ES-341296 | |
| Xem trước PDF ở hai nơi ra HAI đơn giá khác nhau cho cùng một dòng, chênh đúng bằng khoản chiết khấu. | ES-344307 | |
| Cột Số lượng hiển thị 4 chữ số thập phân, khách muốn 1; và cột Đơn giá / Thành tiền không đồng nhất với nhau. | ES-344027 | |
| "khách sạn xuất hóa đơn các sản phẩm thuế 10% sang bên trang nhà cung cấp hiển thị 0%" — kèm log. | ES-344810 | |
| "hóa đơn đang bị làm tròn sai ở cột tổng tiền thanh toán => tiền đang không khớp giữa các cột", khách phải sửa tay từng tờ khi xuất. | ES-344141 | |
| Nhà cung cấp phản hồi rằng dữ liệu bên mình truyền sang đang sai — không phải khách phát hiện mà là NCC báo ngược lại. | ES-342309 | |
| Nhân một dòng ra thì số lượng × đơn giá không ra đúng thành tiền mong muốn, lệch vài chục đồng ở tổng sau thuế. | ES-343367 | |

## 1.2 Các bước kiểm, theo thứ tự chúng tôi đoán

> 🛑 **Thứ tự này chúng tôi TỰ ĐẶT.** Không ticket nào ghi thứ tự kiểm, nên
> đây là chỗ sai nhiều nhất. Nếu thực tế anh/chị kiểm theo thứ tự khác, xin ghi ra.

### K0. Bản hoá đơn đang bị kêu sai được XEM / XUẤT từ đâu: từ PMS, hay từ trang quản trị ezInvoice (admin.invoice), hay là bản PDF cuối cùng bên nhà cung cấp? Và bản mà người đang nói chuyện với bạn nhìn thấy có phải cùng một nơi không?

- **Xem ở:** Hỏi thẳng người báo, và tự mở lại cùng một hoá đơn ở cả hai nơi của ezCloud (Xem trước PDF tại PMS và Xem trước PDF tại ezInvoice.Admin) để biết hai bản có khác nhau không.
- **Cần quyền:** Xem được cả PMS lẫn trang quản trị ezInvoice của khách sạn đó.
- **Vì sao chúng tôi đặt ở bước này:** Vì cùng một hoá đơn có thể ra hai con số khác nhau ở hai nơi. Mọi bước kiểm sau đều là phép SO SÁNH, mà so sai nguồn thì kết luận sai theo.
- **Lấy từ ticket:** "ẢNh hóa đơn em xuất ở trên là xuất tại PMS hay admin.invoice vậy em / Vì chị xem các hóa đơn khách sạn đã xuất thì đang xuất từ admin.invoice và hôm nay vẫn thể hiện 4 chữ số thập phân" (ES-344027)

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| Hai nơi cho ra hai con số khác nhau cho cùng một dòng. | `BANG` | ✔ có ca thật | ES-344307, ES-344027 | |
| Hai nơi của ezCloud giống nhau, nhưng bản PDF cuối bên nhà cung cấp mới là bản sai. | `K3` | ✔ có ca thật | ES-344141, ES-344810 | |
| Chưa xác định được người báo đang nhìn bản nào — chỉ có một tấm ảnh chụp hoá đơn. | `K0` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | ES-341296, ES-343367 | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** 🛑 Bước này sinh ra từ hai lần vấp thật, và cả hai lần đều kết thúc bằng việc một bên báo ĐÃ XONG trong khi bên kia vẫn thấy sai. ES-344027: nội bộ kiểm trên bản xuất từ PMS thấy đúng, khách xuất từ trang quản trị vẫn ra 4 chữ số — mất hai ngày cho đúng chỗ lệch đó. ES-344307: cấu hình đã đồng nhất, PDF tại PMS đúng, PDF xuất có điều chỉnh từ trang quản trị vẫn sai.

### K1. CỘT nào trên hoá đơn lệch: thuế suất, loại hoá đơn, đơn giá, số chữ số thập phân, hay tiền thuế / tổng cộng? Và lệch so với cái gì — so với PMS, hay so với con số khách tính tay?

- **Xem ở:** Đặt cạnh nhau: bản xem trước tại PMS, bản xem trước tại ezInvoice.Admin, và bản PDF bên nhà cung cấp. So theo CỘT, không so cảm tính "tiền sai".
- **Lấy từ ticket:** "Hiện hóa đơn đang bị làm tròn sai ở cột tổng tiền thanh toán => tiền đang không khớp giữa các cột. Nhờ team kiểm tra xem vấn đề làm tròn này từ phía nào" (ES-344141)

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| Thuế suất ra 0% trong khi đáng lẽ phải có thuế. | `BANG` | ✔ có ca thật | ES-342493, ES-344810 | |
| Cột thuế và thành tiền sai cùng lúc, và loại hoá đơn hai bên vốn đã khác nhau. | `BANG` | ✔ có ca thật | ES-341296 | |
| Đơn giá lệch, phần chênh đúng bằng khoản chiết khấu. | `BANG` | ✔ có ca thật | ES-344307 | |
| Số chữ số sau dấu phẩy không như mong muốn, hoặc ba cột không cùng số chữ số. | `BANG` | ✔ có ca thật | ES-344027 | |
| Từng dòng đúng nhưng dòng Tổng cộng / tiền thuế không khớp. | `K2` | ✔ có ca thật | ES-344141, ES-343367 | |
| Nhà cung cấp nói một TRƯỜNG trong dữ liệu truyền sang mang sai giá trị. | `K4` | ✔ có ca thật | ES-342309 | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** Câu khách nói hầu như luôn là "hoá đơn sai tiền" — cùng một câu cho sáu nguyên nhân khác nhau ở bảng dưới. Không quy được về CỘT thì bước tiếp theo là đoán.

### K2. Nhân lại bằng tay: số lượng × đơn giá có ra đúng thành tiền không, và cộng thuế vào có ra đúng tổng mong muốn không?

- **Xem ở:** Chính dòng bị kêu sai trên bản PDF, và con số tổng mà khách nói là đúng. Làm trên giấy / máy tính, không cần vào hệ thống.
- **Lấy từ ticket:** "Với thành tiền 14991200 thì tiền sau thuế sẽ là 17000020.8. Lệch với tổng tiền đúng 17000000. => thành tiền phải là số khác. Do đơn giá khách để chẵn nên khi nhân lại sẽ lệch tiền" (ES-343367)

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| Nhân lại KHÔNG tái tạo được tổng, và đơn giá đang là số chẵn. | `S7` | ✔ có ca thật | ES-343367 | |
| Dữ liệu ezCloud truyền đi mang số lẻ, còn bản hiển thị bên nhà cung cấp là số nguyên. | `S7` | ✔ có ca thật | ES-344141 | |
| Nhân lại ra đúng, tổng vẫn sai. | `K3` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | — | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** 🛑 Đây là bước kiểm RẺ NHẤT của cả nhóm — làm được mà không cần quyền, không cần vào hệ thống, không cần hỏi ai — mà lại đứng sau cùng trong cả hai ticket dùng nó. ES-343367 đi một vòng sửa dòng chi tiết cho khớp trước, rồi tổng sau thuế lệch theo, mới quay lại làm phép nhân. Sửa cho khớp một cột bằng cách đổi số của cột đó là cách làm hỏng cột kế bên.

### K3. Cùng một mục cấu hình đó được khai thế nào ở CẢ BA nơi — PMS, ezInvoice.Admin, và mẫu/loại hoá đơn bên nhà cung cấp — ba giá trị có bằng nhau không?

- **Xem ở:** Mục cấu hình tương ứng với cột đang lệch ở PMS; cùng mục đó ở ezInvoice.Admin; và mẫu hoá đơn / loại hoá đơn đang đăng ký bên nhà cung cấp.
- **Cần quyền:** Quyền vào cấu hình ở cả PMS lẫn ezInvoice.Admin. Phần bên nhà cung cấp thường phải nhờ khách mở hoặc hỏi nhà cung cấp.
- **Lấy từ ticket:** "Tại PMS: Cấu hình Chiết khấu đang chọn 'Cộng vào cột đơn giá hàng hóa/dịch vụ' ... Tại ezInvoice.Admin: Cấu hình Chiết khấu đang chọn 'Tách riêng thành cột tiền chiết khấu' ... cần đồng nhất option cấu hình giữa PMS và ezInvoice.Admin" (ES-344307)

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| Hai nơi của ezCloud khai khác nhau. | `BANG` | ✔ có ca thật | ES-344307, ES-344027 | |
| Hai nơi của ezCloud giống nhau, nhưng LOẠI hoá đơn bên nhà cung cấp khác loại đang cấu hình bên PMS. | `S2` | ✔ có ca thật | ES-341296 | |
| Mức thuế đang dùng chưa có phương thức hiển thị nào được khai trong cấu hình hoá đơn. | `S5` | ✔ có ca thật | ES-344810, ES-342309 | |
| Cấu hình thuế/phí vừa được đổi gần đây và tuỳ chọn tương ứng ở Cấu hình kết nối chưa được bật lại. | `S1` | ✔ có ca thật | ES-342493 | |
| Ba nơi khai giống nhau mà số vẫn lệch. | `K4` | ✔ có ca thật | ES-344307 | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** Không ticket nào nói nơi nào là NGUỒN SỰ THẬT khi hai bên lệch. Trong lô ticket đã đọc, người xử lý luôn sửa ezInvoice.Admin cho khớp PMS — nhưng không ai phát biểu đó là luật, nên đây có thể chỉ là thói quen của một người.

### K4. Đường kết nối có đang trỏ đúng chỗ không: token hiển thị ở PMS có trùng với token ở ezInvoice.Admin không, và bản xuất tuỳ chỉnh có báo lỗi kết nối nào không?

- **Xem ở:** Mục token/kết nối ở ezInvoice.Admin, đối chiếu với token hiển thị trong PMS. Kèm nhật ký đồng bộ của lần xuất gần nhất để xem dữ liệu truyền đi mang thuế suất nào.
- **Cần quyền:** Quyền xem cấu hình kết nối và nhật ký đồng bộ — thường là người nội bộ, khách không tự làm được.
- **Lấy từ ticket:** "Token kết nối tại ezInvoice Admin và token hiển thị tại PMS đang là 2 token khác nhau (Có thể do đã xóa token cũ và tạo 1 token mới nhưng chưa cập nhật lại => Xuất tùy chỉnh hiện tại đang báo lỗi HOTEL_NOT_FOUND do không đúng token)" (ES-344810)

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| Hai token khác nhau, và bản xuất tuỳ chỉnh báo HOTEL_NOT_FOUND. | `S6` | ✔ có ca thật | ES-344810 | |
| Token đúng, dữ liệu truyền đi đã mang đúng giá trị, nhưng bản hiển thị bên nhà cung cấp vẫn khác. | `S8` | ✔ có ca thật | ES-344141 | |
| Cấu hình đúng ở cả hai nơi mà chức năng xem trước / phát hành không áp theo cấu hình đó. | `S8` | ✔ có ca thật | ES-344307 | |
| Một TRƯỜNG trong dữ liệu truyền đi mang ý nghĩa khác với thứ nhà cung cấp kỳ vọng ở trường đó. | `S8` | ✔ có ca thật | ES-342309 | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** 🛑 Ở đây có hai nguyên nhân chồng lên nhau trong CÙNG một ticket — token sai VÀ thiếu khai phương thức hiển thị thuế — và sửa một cái thì lỗi vẫn còn, chỉ đổi thông điệp (sửa token xong thì phát hành báo một mã lỗi khác). Một SOP một-nguyên-nhân-một-lần sẽ dừng sớm ở đây.

### K5. Sau khi sửa cấu hình: đặt phòng đang thử có được lấy lại dữ liệu theo cấu hình MỚI không, hay nó vẫn đang giữ dữ liệu sinh ra theo cấu hình cũ?

- **Xem ở:** Trạng thái của chính đặt phòng đó (đã trả phòng hay chưa) trước khi bấm xem trước / xuất lại.
- **Cần quyền:** Quyền thao tác trên đặt phòng, gồm cả hoàn tác trả phòng.
- **Lấy từ ticket:** "Lưu ý: Đối với BK đã CO, cần undo CO rồi mới thực hiện xuất hóa đơn tùy chỉnh hoặc Xem trước/Xuất để dữ liệu được lấy theo cấu hình mới nhất" (ES-344810)

| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|
| Đặt phòng đã trả phòng — phải hoàn tác trả phòng rồi mới xuất lại thì dữ liệu mới theo cấu hình mới. | `K-XN` | ✔ có ca thật | ES-344810 | |
| Cấu hình đã sửa, xuất lại vẫn ra số cũ, và không phải do trạng thái đặt phòng. | `K4` | ⚠ **CHÚNG TÔI TỰ ĐOÁN** | ES-342493 | |

> **Chỗ chúng tôi thấy dễ vấp — có đúng vậy không?** Bước này chỉ được viết ra MỘT lần trong cả nhóm, dưới dạng một dòng lưu ý cuối một comment dài. Nếu nó đúng là điều kiện chung thì mọi lần "đã sửa mà vẫn sai" đều phải đi qua đây trước khi kết luận là bug — và đó chính là chỗ tôi cần người làm nghề xác nhận.

## 1.3 Bảng đối chiếu: cột nào lệch thì khai sai ở đâu

> 🛑 **Đây là phần chúng tôi cần anh/chị sửa nhất.** Cùng một câu “hoá đơn
> sai tiền” ra tám nguyên nhân khác nhau ở bảng dưới, và chỗ khai sai thì nằm
> rải ở ba nơi: PMS, trang quản trị ezInvoice, và bên nhà cung cấp.

| Cột bị lệch | Khai sai ở đâu | Lưu ý | Sửa ở đâu | Nguồn | Ticket | Sửa lại |
|---|---|---|---|---|---|---|
| Thuế suất về 0% | PMS — Cấu hình kết nối ezInvoice: đổi cấu hình thuế/phí xong chưa bật lại tuỳ chọn tương ứng | Kéo dài nhiều tháng trước khi ai đó báo — sai lặng lẽ, không có thông báo lỗi nào. | `S1` | ✔ có ca thật | ES-342493 | |
| Thuế suất về 0% ở bản bên nhà cung cấp | Cấu hình hoá đơn — thiếu phương thức hiển thị cho chính mức thuế đang dùng | Ở một ticket còn chồng thêm token sai; sửa một cái thì lỗi chỉ đổi thông điệp. | `S5` | ✔ có ca thật | ES-344810, ES-342309 | |
| Cột thuế và thành tiền cùng sai | LOẠI hoá đơn khai lệch giữa PMS và nhà cung cấp (bán hàng ↔ VAT) | Hoá đơn bán hàng không có thuế, hoá đơn VAT bắt buộc có — hai bên khai khác nhau thì cột thuế không có cách nào đúng. | `S2` | ✔ có ca thật | ES-341296 | |
| Đơn giá lệch đúng bằng khoản chiết khấu | Cấu hình Chiết khấu khai khác nhau giữa PMS (cộng vào đơn giá) và ezInvoice.Admin (tách riêng cột) | Đồng nhất hai nơi xong VẪN lệch — phần còn lại là lỗi sản phẩm, phải chuyển kỹ thuật. | `S3` | ✔ có ca thật | ES-344307 | |
| Số chữ số thập phân của Số lượng / Đơn giá / Thành tiền | Chỉ khai một phía (PMS) mà không khai bên ezInvoice.Admin | Riêng cột Thành tiền còn phụ thuộc mẫu hoá đơn bên nhà cung cấp — có mẫu không cho hiển thị phần lẻ, nên khai đủ hai nơi vẫn có thể không ra như mong muốn. | `S4` | ✔ có ca thật | ES-344027 | |
| Tiền thuế / dòng Tổng cộng không khớp các cột | Không ai khai sai — ezCloud truyền số có phần lẻ, nhà cung cấp làm tròn khi hiển thị | Lý do nền được nêu trong ticket: tiền thuế thường không được để lẻ, nên nhà cung cấp làm tròn ở dòng Tổng cộng. Cách sửa là giảm số lẻ ở phía ezCloud, không phải bắt nhà cung cấp đổi. | `S7` | ✔ có ca thật | ES-344141 | |
| Thành tiền của một dòng không tái tạo được từ số lượng × đơn giá | Không phải cấu hình — đơn giá được để tròn số nên phép nhân không về đúng tổng | Sửa thành tiền cho khớp thì tổng sau thuế lệch theo. Chỗ sửa đúng là số chữ số thập phân của ĐƠN GIÁ. | `S7` | ✔ có ca thật | ES-343367 | |
| Một trường trong dữ liệu truyền sang mang sai giá trị | Ánh xạ trường giữa ezCloud và nhà cung cấp hiểu khác nhau (một bên đưa tiền thuế, bên kia chờ tổng tiền) | Không sửa được bằng cấu hình ở bất kỳ nơi nào — phải hai bên kỹ thuật thống nhất lại ý nghĩa của trường. | `S8` | ✔ có ca thật | ES-342309 | |

**Còn kiểu lệch tiền nào hay gặp mà bảng chưa có? Nó khai sai ở đâu?**

```



```

## 1.4 Cách sửa

| Mã | Việc | Ticket | Sửa lại |
|---|---|---|---|
| `S1` | Vào Cấu hình > Cấu hình kết nối của ezInvoice, bật lại tuỳ chọn thuế tương ứng sau khi khách sạn đổi cấu hình thuế/phí, rồi lưu. | ES-342493 | |
| `S2` | Đưa LOẠI hoá đơn khai ở PMS về đúng loại khách sạn đang đăng ký bên nhà cung cấp (hoá đơn bán hàng thì không có thuế, hoá đơn VAT thì bắt buộc có). | ES-341296 | |
| `S3` | Đặt cùng một option Chiết khấu ở PMS và ở ezInvoice.Admin. | ES-344307 | |
| `S4` | Khai số chữ số thập phân cho Số lượng và Đơn giá ở CẢ HAI nơi — PMS và ezInvoice.Admin — chứ không chỉ một bên. | ES-344027 | |
| `S5` | Khai phương thức hiển thị cho đúng mức thuế khách sạn đang dùng trong cấu hình hoá đơn. | ES-344810, ES-342309 | |
| `S6` | Cập nhật token kết nối ở PMS cho trùng token đang có ở ezInvoice.Admin (thường xảy ra sau khi token cũ bị xoá và tạo lại). | ES-344810 | |
| `S7` | Cho đơn giá 2 chữ số thập phân và để các cột tiền còn lại 0 chữ số, để phép nhân tái tạo được tổng và để bản hiển thị bên nhà cung cấp không phải làm tròn. | ES-344141, ES-343367 | |
| `S8` | Khi ba nơi đã khai giống nhau mà số vẫn lệch: ghi lại dữ liệu truyền đi của đúng hoá đơn đó rồi chuyển kỹ thuật (hoặc làm việc với nhà cung cấp nếu là ánh xạ trường), và báo khách cách làm tạm cho tới khi có bản sửa. | ES-344307, ES-344141, ES-342309 | |
| `S9` | Với những hoá đơn ĐÃ phát hành sai trước khi sửa cấu hình: hướng dẫn khách điều chỉnh hoặc thay thế trên nhà cung cấp theo đúng nghiệp vụ kế toán — sửa cấu hình không tự sửa được hoá đơn đã phát hành. | ES-341296, ES-342493 | |

## 1.5 Khi nào coi là xong

**Xuất lại (hoặc xem trước) trên ĐÚNG đặt phòng đã lỗi, TỪ ĐÚNG NƠI mà khách vẫn dùng hằng ngày — nếu khách xuất từ trang quản trị thì phải kiểm ở đó, không kiểm hộ bằng bản xuất từ PMS — rồi đối chiếu CẢ BỐN cột (số lượng, đơn giá, thành tiền, tiền thuế/tổng cộng) trên bản PDF thực tế bên nhà cung cấp, không chỉ cột vừa sửa. Nếu đặt phòng đã trả phòng thì hoàn tác trả phòng trước khi xuất lại.**

> ⚠ 🛑 5/8 ticket của nhóm này có ít nhất một vòng ĐÃ SỬA RỒI VẪN SAI, hoặc sửa xong thì hỏng sang cột khác. Đó là lý do điều kiện đóng ở đây dài hơn hai nhóm trước và không được rút gọn thành 'đã sửa cấu hình xong'. Hai nhóm kia không có đặc điểm này.

Điều kiện này đủ chưa? Nếu chưa thì còn thiếu gì:

```


```

## 1.6 3 chỗ chúng tôi TỰ ĐOÁN — xin trả lời từng chỗ

Mỗi chỗ chỉ cần một trong ba: **đúng** / **sai, phải là …** / **không bao giờ xảy ra**.

| # | Ở bước | Chúng tôi đoán rằng | Đúng / Sai / Không có |
|---|---|---|---|
| 1 | `K0` | Chưa xác định được người báo đang nhìn bản nào — chỉ có một tấm ảnh chụp hoá đơn. | |
| 2 | `K2` | Nhân lại ra đúng, tổng vẫn sai. | |
| 3 | `K5` | Cấu hình đã sửa, xuất lại vẫn ra số cũ, và không phải do trạng thái đặt phòng. | |

## 1.7 Còn thiếu — bước kiểm anh/chị vẫn làm mà không có ở trên

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
