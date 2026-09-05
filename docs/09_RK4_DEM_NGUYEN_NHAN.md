> **Nguồn:** đếm thật trên **150 case hoá đơn điện tử đã đóng** xuất từ Jira ezCloud
> (`project in ("ezCloud Support Team")`, chủ đề hoá đơn), chạy 2026-09-04 bằng 16 agent:
> 10 lượt rút nguyên nhân độc lập → **3 lượt GỘP theo ba tiêu chí khác nhau** → 2 phản
> biện đối kháng (một tìm "gộp quá tay", một tìm "chia quá vụn") → 1 tổng hợp.
>
> **Vì sao ba lượt gộp độc lập:** vòng đếm trước trên 32 case thất bại vì hai lượt phân
> tích đọc CÙNG dữ liệu ra HAI kết luận ngược nhau, mỗi lượt tự chọn một độ mịn rồi lập
> luận trên đó. Lần này ba tiêu chí được ĐẶT TRƯỚC và ba con số được đem so — chính độ
> phân kỳ giữa chúng là dữ liệu.
>
> ⚠ **Một giới hạn của mẫu, đã đo:** 150 case này lấy bằng `ORDER BY resolved DESC` nên
> trải **24 ngày**, không phải 12 tháng như tên gọi ban đầu. Xem `07` §5 `R-K4`. Mẫu rải
> đều 12 tháng (144 case / 528 evidence) đã xuất sẵn bằng `sample_spread.py` nhưng CHƯA
> đếm — và theo đúng §6 dưới đây, đếm lại trên nó **không chữa được** vấn đề gốc.
>
> ⚠ Script kiểm bằng code: cả ba lượt gộp đều 88 + 62 = 150, **không bỏ sót, không gán
> trùng case nào**. Nên độ phân kỳ 6/19/66 KHÔNG do agent làm việc cẩu thả.

# Kết luận `R-K4` — Một loại vấn đề có bao nhiêu nguyên nhân?

## 1. Câu trả lời một dòng

**HỮU HẠN — có cấu trúc lặp lại thật, không phải "mở, hàng trăm" — nhưng KHÔNG NHỎ như giả định đang đứng: cỡ 19 nhóm (khoảng 18–30) cho MỘT chủ đề, và đó là cận dưới, đo trên 59% corpus.**

Phép đếm này **thành công một nửa và thất bại một nửa**, cần nói rõ ngay:
- **Thất bại**: nó không cho ra "một con số". Cùng 88 case, cắt khác nhau ra 6 / 19 / 66, hai phản biện ra 18 / 78 — lệch 13 lần. Không có "số nguyên nhân" trong dữ liệu này.
- **Thành công**: nó đủ để **khai tử giả định 5–10**, đủ để đo trần chất lượng nguồn (41%), và đủ để chốt kiến trúc — vì mọi cách cắt đều nằm **cùng một phía** của quyết định.

## 2. Con số và khoảng

| Tiêu chí gộp | Số nhóm | Ước lượng còn bao nhiêu nhóm CHƯA gặp | Hình dạng phân bố |
|---|---|---|---|
| Lớp kiến trúc (`chat`) | **6** | 0 — nhưng đóng do **định nghĩa** | Phẳng: 21/16/14/14/12/11 |
| Một SOP dùng được (`theo-sop`) | **19** | ~1 (tổng ~20) | **Có đầu và đuôi thật** |
| Một cách sửa cụ thể (`long`) | **66** | ~200 (tổng ~267) | 53/66 nhóm chỉ 1 case |
| Phản biện gộp-quá-tay | ≥78 | không có trần | — |
| Phản biện chia-quá-vụn | 18 (17–19) | — | — |

Ba con số **XA nhau**. Theo đúng luật đã đặt trước: **con số này phụ thuộc tiêu chí gộp, không phụ thuộc dữ liệu, nên KHÔNG được chốt kiến trúc dựa vào nó.** Bằng chứng định lượng sạch nhất: ước lượng "còn bao nhiêu nhóm chưa thấy" nhảy từ **0% lên 300%** chỉ vì đổi tiêu chí, trên **y nguyên 88 case**.

Nhưng **một thứ trong đó LÀ thuộc tính của dữ liệu**: chỉ ở mức "một SOP dùng được" mới có phân bố đầu-đuôi thật — **6 nhóm lớn nhất phủ 47/88 = 53% case có nguyên nhân, 10 nhóm phủ 75%**, hai nhóm đầu mỗi nhóm 10 case. Lượt 6 phẳng đều (dấu hiệu của một phân hoạch được **thiết kế**, không phải phát hiện); lượt 66 gần như toàn singleton (nó đã **đập vỡ cái đầu**). Chỉ 19 giữ được cả đầu lẫn đuôi — nó là mức duy nhất đang **đo dữ liệu**.

Hai lý do trừ điểm cụ thể, không phải chuyện gu:
- **66 yếu nhất**, vì ví dụ tách hàng đầu của nó **sai về sự kiện**: nó tách cặp kiểm toán bằng lý lẽ "một bên được bảo 'chạy kiểm toán đi'" — evidence cho thấy **không case nào** được bảo vậy; cả ba đều đóng bằng đúng một hành động là đổi ngày hoá đơn. Đây là `G3` bị vi phạm ở chỗ **không ai canh**: guardrail cấm bịa NGUYÊN NHÂN, không có guardrail nào cấm **bịa RANH GIỚI** từ cách diễn đạt của người ghi ticket.
- **19 cũng chưa sạch**: nó tự đặt luật "tách khi đổi màn hình / hệ thống / người làm" rồi vi phạm ở 4 nhóm (nhóm 1 gộp 3 hệ thống + 3 chủ thể; nhóm 2 mang tên "tra mã lỗi" nhưng 1/3 thành viên không có mã lỗi nào; nhóm 12 gộp "chỉ cần giải thích" với một nợ tuân thủ pháp lý đang chờ release; nhóm 15 gộp 3 cách sửa với 2 điều kiện đóng case). Sửa các chỗ đó đẩy 19 lên **25–30**. Nên khoảng thật là **18–30, tâm ~20**.

## 3. Tỉ lệ không xác định được — 62/150 = 41,3%

**Đây là phát hiện quan trọng hơn con số.** Nó nói về **NGUỒN DỮ LIỆU**, không về tập nguyên nhân, và có ba xác nhận độc lập ngay trong corpus này:
- **26/150 case (17%)** tự ghi bước "remote vào máy" / "xin remote" — tức bước kết luận xảy ra **ngoài Jira**, theo chính nhật ký case.
- Chỉ **15/150 (10%)** có bước "tra log". Chẩn đoán phần lớn không dựa trên bằng chứng ghi lại được.
- **Xu hướng đi xuống**: 15/30 (50%) ở 30 case mới nhất, so với 8/30 (27%) ở 30 case cũ nhất (theo proxy mã ES, không phải ngày thật).

Ba hệ quả:

**(a) Đếm thêm case ĐÃ ĐÓNG cùng loại sẽ không nâng độ tin — nguồn đang xấu đi, không tốt lên.** Cửa sổ mới nhất vừa là chỗ dùng để kiểm bão hoà, vừa là chỗ mỏng bằng chứng nhất.

**(b) 41% này lệch CÓ HỆ THỐNG, không phải nhiễu trung tính.** Cơ chế nào viết được bằng một dòng thì còn lại ("chưa phân quyền", "sai CCCD"); cơ chế nào phải điều tra mới biết thì mất. Hệ quả vận hành nặng: **bộ phân loại huấn luyện trên corpus này sẽ tự tin nhất ở đúng chỗ ít cần nhất, và mù ở chỗ cần nhất.**

**(c) Điểm sáng duy nhất**: kỷ luật giữ được. 84/88 case có nguyên nhân là "evidence nói rõ", chỉ **4 case (4,5%)** là suy ra; script xác nhận cả ba lượt không bỏ sót, không gán trùng (88+62=150). Sai số **không** đến từ việc bịa nguyên nhân. Nó đến từ 41% đã mất và từ tiêu chí gộp.

## 4. Bão hoà — mọi con số là CẬN DƯỚI

- **6: nói đã bão hoà — bỏ qua bằng chứng này.** Chính tác giả viết: *"Một lớp thứ 7 chỉ xuất hiện nếu kiến trúc mọc thêm lớp, chứ không phải nếu đọc thêm case."* Bão hoà do định nghĩa không phải phát hiện từ 150 case.
- **19: chưa bão hoà, nhưng sát.** Nhóm mới cuối cùng ở vị trí 131/150; ước lượng còn ~1 nhóm.
- **66: chưa, và không có dấu hiệu nào.** Tích luỹ 13/28/41/55/66 gần như **thẳng**; tỉ lệ nhóm mới 81/79/68/74/73% không bẹt; **case CUỐI CÙNG của corpus mở một nhóm mới** và nhóm đó không có case thứ hai.

Một chỉnh sửa phương pháp: thứ tự 150 dòng **không** theo thời gian. Điều này **không** phá đường bão hoà — trộn thứ tự đúng là cách chuẩn để hỏi "thêm dữ liệu có thêm nhóm không". Nó chỉ phá các phát biểu về **xu hướng thời gian**. Đừng dùng corpus này để nói "lỗi mới đang sinh thêm theo thời gian".

## 5. Quyết định kiến trúc: **KHÔNG dựng vector DB / RAG. Postgres full-text search là đủ.**

Nói trước cho rõ: **tôi đang bác chính luật quyết định của `R-K4`, không phải điền số vào nó.** Đo được ~19–30/chủ đề, tức KHÔNG phải 5–10 — nhưng kết luận vẫn là "không dựng RAG". Vì con số chưa bao giờ là phép thử đúng.

**Phản biện mạnh nhất chống lại chính khuyến nghị của tôi** (nói trước khi đề xuất): 53/88 = **60% case có nguyên nhân là bản DUY NHẤT của cách sửa đó trong 12 tháng**. Một bộ phân loại cần ví dụ cho mỗi lớp; ở cái đuôi này nó có đúng một ví dụ. Tìm-kiếm-tương-tự trên case cũ xuống cấp mượt hơn phân loại khi mỗi lớp một ví dụ. Đây là lý lẽ thật.

**Vì sao nó vẫn không thắng — bốn thứ, đều trích dẫn được, không thứ nào dựa vào con số:**

1. **Câu trả lời liệt kê được, và corpus tự chứng minh.** Nhân viên đã gõ tay ra SOP hoàn chỉnh ngay trong chat: ES-346396 có B1/B2 kèm nhánh điều kiện, phủ trọn **ba nhóm** mà lượt 66 tách rời; ES-342165 ("lên ncc xóa, xóa ở admin và xóa ở pms"); ES-341316 (kiểm tra NCC đã cấp số chưa **TRƯỚC KHI** phát hành lại). Cái gì con người liệt kê được tại chỗ thì liệt kê được vào bảng. 19–30 dòng/chủ đề, hay cả 78, vẫn là một **BẢNG**, không phải một chỉ mục vector. RAG có giá trị khi **không** liệt kê được — điều kiện đó sai hẳn ở đây.
2. **Đường vào là mã lỗi, không phải văn xuôi**: ERR.1518, Status 5000 / THIRD_PARTY_SPECIFIC_ERROR, InvalidInvoiceDate, HOTEL_NOT_FOUND, 504, "ký hiệu hoá đơn", "kiểm toán", "hoá đơn nháp". Khớp chính xác các chuỗi này đúng là việc của full-text search; embedding không thêm được gì trên một mã lỗi.
3. **Điểm nặng nhất: ở case khó, thông tin phân biệt KHÔNG CÓ TRONG VĂN BẢN.** Cùng triệu chứng "không chọn được ký hiệu hoá đơn" ứng với **ba** cơ chế, và thứ phân biệt chúng là một **PHÉP KIỂM** ("danh sách ký hiệu có rỗng không?"), không phải từ nào trong lời khách báo. ES-341290 là bằng chứng đã xảy ra: nhân viên đoán "chưa chọn ký hiệu" cho một tài khoản không hề có quyền — danh sách rỗng, không có gì để chọn. Tương tự ES-341316 ("NCC đã cấp số chưa?"). **Không công nghệ tìm kiếm nào chữa được chuyện này.** Thứ cần là cây quyết định có bước kiểm — đúng cái mà bản nháp SOP phải chứa. Dựng RAG ở đây là mua sai món.
4. **Nút cổ chai thật là 41% không ghi nguyên nhân.** Tiền bỏ vào stack vector là tiền không bỏ vào việc duy nhất làm con số tiến lên. Dựng vector DB bây giờ đúng nghĩa **"giàn giáo tạm"**: hạ tầng chờ một bài toán chưa được chứng minh là có.

Rủi ro chọn sai **bất đối xứng theo hướng có lợi**: nếu phép thử dưới đây thất bại, pgvector là thứ **cộng thêm** vào cùng một Postgres, không phải làm lại. (Lưu ý kỹ thuật duy nhất: FTS tiếng Việt không có bộ từ điển sẵn, cần `unaccent` + cấu hình `simple`. Việc này nhỏ so với việc viết được 19–30 SOP.)

**ĐIỀU LÀM TÔI ĐỔI Ý — đo được, không phải cảm tính:**
- **Phép thử phải chạy trước khi tiêu tiền**: lấy TIN NHẮN ĐẦU của 88 case có nguyên nhân, tra bằng từ khoá/mã lỗi, đo tỉ lệ nhóm SOP đúng nằm trong top-3. **Dưới ~60–70% thì embedding được thử; trên mức đó thì đóng câu hỏi này lại.** Chi phí: một buổi, không hạ tầng mới. Corpus đã có bằng chứng cả hai chiều — mã lỗi khắp nơi, nhưng ES-346136 vào bằng "báo hệ thống đang xử lý vui lòng thử lại sau", không có từ khoá nào phân biệt.
- Nếu phép đếm trên case **CÒN MỞ** cho ra tập câu trả lời **không địa chỉ hoá được** bằng mã lỗi/tên màn hình, chỉ phân biệt bằng diễn đạt.
- Nếu số **CHỦ ĐỀ** lớn (>50) khiến định tuyến liên-chủ-đề mới là bước khó — kể cả vậy vẫn FTS trước, hybrid sau.

## 6. Điều bất ngờ nhất

Hai phản biện đọc **cùng evidence gốc**, cùng khai "độ tin cao", cùng trích nguyên văn, và ra **78 vs 18 — lệch 4 lần**. Tôi **không hoà giải**, vì cả hai đều đúng, và lý do khiến cả hai đúng mới là điều bất ngờ:

**CASE JIRA KHÔNG PHẢI MỘT ĐƠN VỊ CỦA GÌ CẢ.** Trong cùng 150 case:
- **ES-332789 một mình chứa ≥6 cơ chế**, và chính bộ phận hỗ trợ phải đóng nó lại vì *"các issue đang trùng lặp dễ gây nhầm lẫn, hiện em sẽ close issue cũ và gửi email mới để TÁCH RIÊNG các vấn đề"*;
- **ba ticket ES-337454 / ES-338386 / ES-340759** là cùng một khách (Maison Đông Du), cùng một việc đấu nối, đóng trong **52 giây** (11:01:33 / 11:02:01 / 11:02:25).

Một ticket chứa 6 nguyên nhân, và 3 ticket chứa 1 nguyên nhân — trong cùng một bộ dữ liệu. Vậy mọi phép đếm "số nguyên nhân trên số case" đang chia cho một mẫu số là **độ sạch ticket của Jira**, không phải cấu trúc của hệ thống. Đó là lý do vòng n=32 thất bại, và **n=150 không chữa được nó** — tăng n không sửa được một đơn vị đo sai. Muốn một con số ổn định thì phải đếm trên đơn vị khác: **một cơ chế = một dòng, cho phép một case sinh nhiều dòng**. Corpus đã chỉ rõ nhu cầu đó: 7 case ghi rõ ≥2 cơ chế, 6 case cần 2 SOP, và ít nhất 6 họ cơ chế **không có nhóm nào trong cả ba lượt** (ràng buộc liên tục dải số/ngày theo ký hiệu; thuế TTĐB; kỹ thuật xoá + insert lại bản ghi; ranh giới trách nhiệm với site NCC; xoá dịch vụ không hoàn kho minibar; trễ cam kết như một đường xử lý riêng).

## 7. Hai câu hỏi cần chủ dự án quyết

**Q1. Chốt "một nguyên nhân" = "một SOP dùng được" (mức 19), chứ không phải lớp kiến trúc (6) hay cách sửa cụ thể (66)?**
**Khuyến nghị: CÓ.** Lý do: đó là đơn vị mà sản phẩm thực sự sinh ra (bản nháp SOP), và là **đơn vị duy nhất trong ba đơn vị cho ra phân phối có đầu-đuôi thật** — tức nó đang đo dữ liệu, không đo tiêu chí. Trả lời câu này chốt luôn con số kế hoạch: **18–30 SOP cho MỘT chủ đề, nhân với số chủ đề** — không phải 5–10 cho toàn nền tảng. Nếu không chốt, tranh luận 6-vs-66 sẽ tái diễn ở mọi workstream sau, và mỗi lần lại tốn một vòng phân tích.

**Q2. Có mở phép đếm sang case CÒN MỞ, và bắt ghi nguyên nhân ngay tại thời điểm remote/điện thoại, hay không?**
**Khuyến nghị: CÓ — đây là việc duy nhất làm con số tiến lên; đọc thêm case đã đóng thì không.** Lý do: 41% không ghi nguyên nhân, **tệ hơn ở cửa sổ mới nhất (50%)**, và 26/150 case tự ghi rằng bước kết luận xảy ra qua remote. Nếu câu trả lời là **KHÔNG**, thì phải chấp nhận và **thiết kế theo**: nền tảng chạy trên một tập nguyên nhân lệch về nửa dễ, nên đường **"không xác định được → chuyển người"** phải là chức năng **hạng nhất của v1**, không phải ngoại lệ — 30/150 case (20%) đã kết bằng "báo dev", đường đó vốn đã là một phần của công việc thật, và 79/150 (53%) kết bằng "hướng dẫn khách" chính là phần mà thư viện SOP hữu hạn phục vụ được ngay.