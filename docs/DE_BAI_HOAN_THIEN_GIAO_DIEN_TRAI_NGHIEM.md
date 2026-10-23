# Đề bài cho AI: hoàn thiện giao diện tự trải nghiệm và đánh giá kết quả

> Ngày lập: 2026-10-23 (Asia/Bangkok).
> Người dùng yêu cầu lập tài liệu để giao cho AI bổ sung giao diện.
> Trạng thái: **đề xuất triển khai**, không thay thế các quyết định domain và kiến trúc đã chốt.
> Căn cứ: kiểm tra mã nguồn console và các API liên quan; chưa chạy nghiệm thu trực tiếp giao diện trong lượt lập tài liệu này.

## 1. Yêu cầu thực hiện

Hãy triển khai các bổ sung dưới đây trên dự án hiện có. Mục tiêu là để chủ dự án tự thao tác trên giao diện, đọc được kết quả thực tế, đối chiếu nguồn và lưu đánh giá mà không phải mở mã nguồn hoặc tự gọi API.

Làm theo từng phần, ưu tiên P0 rồi P1. Tái sử dụng Vue 3, TypeScript, các API và cơ chế lưu trữ hiện có; tránh viết lại hệ thống. Khi hoàn thành, báo cáo phần đã làm, cách chạy, kết quả kiểm tra và phần chưa kiểm chứng.

Đầu ra mong muốn của một phiên trải nghiệm:

1. Biết đang dùng dữ liệu nào, app nào, chế độ AI nào.
2. Chạy được một tình huống có đầu vào và kết quả mong đợi rõ ràng.
3. Đọc đầy đủ câu trả lời, hướng dẫn và bản nháp quy trình.
4. Mở được bằng chứng hỗ trợ từng nội dung quan trọng.
5. Sửa và duyệt qua luồng hiện có; phân biệt lưu sửa với xuất bản.
6. Ghi nhận nhận xét, xem lại lịch sử và xuất kết quả để đánh giá.

## 2. Tài liệu và ràng buộc cần đọc

Đọc `AGENT.md` ở gốc repo và tuân thủ thứ tự source of truth trong đó. Đặc biệt đối chiếu:

- `docs/00_CURRENT_STATE.md`: trạng thái và quyết định mới nhất.
- `docs/04_KNOWLEDGE_MODEL_V0.1.md`: provenance, verification, lifecycle và vocabulary.
- `docs/05_PROCESS_MODEL_V0.1.md`: phạm vi mô hình quy trình.
- `docs/06_MVP_ARCHITECTURE.md`: kiến trúc và các ràng buộc triển khai.
- `docs/02_SUCCESS_METRICS_V1.md`: phân biệt đánh giá chất lượng, việc sử dụng và giá trị kinh doanh.
- `docs/18_QUYET_DINH_MUC_3.md`: quyết định console và nhóm Q.
- `docs/19_HOP_DONG_TICH_HOP.md`: hợp đồng sản phẩm.
- `docs/21_HUONG_DAN_TEST.md`: cách chạy và kiểm tra.

Giữ nguyên các nguyên tắc:

- Console này là công cụ nội bộ. Không tự chuyển thành cổng SaaS hoặc thay thế widget nhúng.
- `/internal/hoi-dap` đọc vật liệu chưa duyệt; kết quả luôn mang nhãn **CHƯA DUYỆT**. Không tự đưa câu trả lời vào kho tri thức.
- `/hoi` sử dụng luồng tri thức đã duyệt. Không trộn hai loại kết quả vào một nhãn chất lượng chung.
- Người đánh giá câu trả lời không đồng nghĩa với người xuất bản tri thức. Nút đánh giá không được gọi API duyệt.
- Giữ cổng duyệt ở chế độ shared, tenant boundary, phạm vi xem và cổng che dữ liệu. Không bỏ chặn để làm demo xanh.
- Mở trang, xem lịch sử, mở dẫn chứng hoặc tổng kết không được tự gọi model, xác nhận bước hay ghi nhận USED.
- Giả lập tiếp tục chạy ở `kp_gialap` / cổng 5120. Không ghi dữ liệu giả vào `kp_dev`.
- Không thêm workflow engine, tự khám phá chủ đề SOP, tự duyệt hoặc capability sản phẩm mới trong nhiệm vụ này.
- Nội dung AI và nguồn phải được hiển thị an toàn; không render HTML chưa làm sạch bằng `v-html`.

Nếu tài liệu này lệch hợp đồng thực tế, kiểm tra code và quyết định mới nhất, nêu rõ chênh lệch rồi điều chỉnh cách triển khai trong phạm vi trên.

## 3. Hiện trạng đã kiểm tra

| Khu vực | Đã có | Khoảng trống cần xử lý |
|---|---|---|
| Giả lập, bước 5 | Biết/không biết, bước hiện tại, xác nhận bước, khai đã dùng | Cột Hướng dẫn chỉ hiện số lượng, chưa hiện nội dung |
| Giả lập, bước 3–4 | Soạn/nạp nháp, thống kê, nút duyệt tri thức và quy trình | Thiếu màn đọc đầy đủ, đối chiếu nguồn và sửa trước khi duyệt |
| Nạp & hỏi | Câu hỏi, trả lời theo phát biểu, nút mở đoạn nguồn | Thiếu lưu lịch sử, đánh giá nội dung và ghi chú |
| Case → Dòng chảy | Sơ đồ, timeline, biên lai định tuyến, trạng thái gợi ý, evidence rút gọn | Quan sát kỹ thuật tốt hơn trải nghiệm đọc và đánh giá hướng dẫn |
| Giả lập | Chấm nhóm theo đáp án dữ liệu giả | Chưa có tổng kết phiên thử; điểm định tuyến không đánh giá chất lượng hướng dẫn/SOP |
| Cấu hình AI | Nhà cung cấp/model, chế độ API/File, trạng thái khóa, hạn mức | Chưa tập hợp thành phần mở đầu một phiên trải nghiệm |

Các dấu mốc trong code để bắt đầu kiểm tra:

- `scripts/giao-dien/console/src/views/GiaLap.vue`: `banNhap`, `duyet`, `hoiLai`, `d.soHuongDan`.
- `scripts/giao-dien/gia_lap.py`: `hoi_ca_lo` đã trả `huongDan`; `api_gl_duyet` đang duyệt tập bản nháp của giả lập.
- `scripts/giao-dien/console/src/apiGiaLap.ts`: kiểu `DongCham` cần đối chiếu và bổ sung theo payload thật.
- `scripts/giao-dien/console/src/views/NapHoi.vue`: `chon()` xóa kết quả đang hiển thị; kết quả mới đang lưu trong `ref`.
- `scripts/giao-dien/console/src/views/DongChay.vue`, `components/BanDoLo.vue`.
- `scripts/giao-dien/console/src/api.ts`, `kieu.ts`, `router.ts`.
- `src/KnowledgePlatform.Api/Knowledge/KnowledgeRevisionHandler.cs`, `KnowledgeApprovalHandler.cs`.
- `src/KnowledgePlatform.Api/QuyTrinh/QuyTrinhHandler.cs`.

Các nhận định trên là ảnh chụp trạng thái mã nguồn tại ngày lập tài liệu. Kiểm tra lại trước khi sửa, không giả định chúng vẫn đúng sau các thay đổi khác.

## 4. P0 — Hiển thị đầy đủ hướng dẫn và dẫn chứng

### Yêu cầu

- Ở bước 5 của Giả lập, giữ số lượng để tóm tắt nhưng cho phép mở toàn bộ nội dung hướng dẫn của từng ticket.
- Trình bày theo cấu trúc payload thật: nội dung, mức kiểm chứng, trạng thái cần xem lại và dẫn chứng nếu API có trả.
- Đọc được tên/nội dung bước hiện tại và kết quả sau khi xác nhận hoàn thành.
- Mở được nội dung nguồn theo quyền hiện có. Nếu nguồn không còn truy cập được hoặc không có liên kết, hiển thị rõ lý do; không dựng dẫn chứng thay thế.
- Hiển thị lý do không biết hoặc không có hướng dẫn ngay tại ticket.
- Phân biệt nguồn định tuyến của người nạp, bảng tra và model; không gắn toàn bộ kết quả thành “AI đúng”.
- Bổ sung hoặc tái sử dụng phần đọc này trong màn Dòng chảy khi phù hợp, tránh hai nơi trình bày trái nhau.
- JSON thô chỉ là phần mở rộng phục vụ chẩn đoán, không phải cách đọc chính.

### Nghiệm thu

- Không cần mở DevTools/API vẫn đọc được nội dung của mọi hướng dẫn trả về.
- Mở được dẫn chứng mà API cung cấp và thấy rõ liên kết thiếu/không truy cập được.
- Xác nhận bước xong cập nhật đúng bước tiếp theo; chỉ mở xem không đổi trạng thái.
- Ticket “không biết” vẫn là một kết quả hợp lệ, không bị trình bày như lỗi hệ thống.

## 5. P0 — Màn đọc, sửa và duyệt bản nháp SOP

### Yêu cầu

- Từ bước 3–4 của Giả lập, mở được chi tiết bản ghi tri thức và quy trình vừa tạo, có ID và trạng thái rõ ràng.
- Hiện các phát biểu đang hiệu lực, nguồn gốc, mức kiểm chứng, dẫn chứng; hiện các bước quy trình theo thứ tự.
- Dùng bố cục nội dung và bằng chứng đặt cạnh nhau khi đủ rộng; trên màn nhỏ vẫn đọc được lần lượt.
- Phân biệt phát biểu có nguồn hỗ trợ với suy đoán/chưa kiểm chứng theo dữ liệu thực tế. Không tự nâng mức kiểm chứng.
- Cho phép sửa nội dung qua API hiện có, bảo toàn bản gốc và lịch sử sửa. Cho xem bản gốc và bản đang hiệu lực.
- Với quy trình, chỉ cho sửa trong trạng thái mà backend cho phép; không mở sửa trực tiếp quy trình ACTIVE.
- Có hai thao tác riêng: **Lưu chỉnh sửa** và **Duyệt/xuất bản**. Lưu sửa không tự duyệt.
- Người duyệt phải đọc được nội dung trước khi gửi duyệt, khai rõ người thao tác/phạm vi xem và chủ động xác nhận việc rà soát theo hợp đồng hiện có.
- Phân biệt duyệt tri thức và duyệt quy trình. Nếu một thao tác thành công, thao tác kia thất bại, hiện đúng trạng thái từng đối tượng; không báo thành công toàn bộ.
- Không tự khai `xacNhanDuLieuThuongMai: true` cho dữ liệu thật. Cơ chế duyệt nhanh dữ liệu giả, nếu còn giữ, phải được ghi rõ là dành cho giả lập.
- Hiện rõ tên người thao tác là lời khai nếu hệ thống chưa xác thực danh tính cá nhân.

API cần đối chiếu và tái sử dụng:

```text
GET  /knowledge/{id}
POST /knowledge/{id}/sua
POST /knowledge/{id}/approve
GET  /processes/{id}
PUT  /processes/{id}/buoc
POST /processes/{id}/approve
```

Không giả định API hiện hỗ trợ thêm/xóa mọi loại phát biểu hoặc mọi thao tác biên tập. Làm phần được hợp đồng hỗ trợ trước và ghi rõ giới hạn.

### Nghiệm thu

- Người dùng đọc được nội dung nháp và bằng chứng trước khi duyệt.
- Sửa một phát biểu, tải lại, vẫn xem được bản gốc và bản mới.
- Thay đổi chưa lưu không bị mất âm thầm khi rời màn hình.
- Trạng thái chưa duyệt/cần xem lại/đã duyệt phản ánh đúng backend sau từng thao tác.
- Thiếu xác nhận bắt buộc hoặc backend chặn duyệt thì giao diện báo đúng lý do.

## 6. P0 — Lưu phiên trải nghiệm, lịch sử và đánh giá

### Phạm vi

Cho phép tạo hoặc chọn một phiên trải nghiệm. Lịch sử tối thiểu bao gồm các lượt hỏi ở Nạp & hỏi và kết quả hỏi lại trong Giả lập; liên kết tới bản nháp đã xem/sửa/duyệt khi có.

Một lượt cần lưu đủ để xem lại:

- ID phiên/lượt, thời điểm bắt đầu và kết thúc, người thao tác theo mức xác thực hiện có.
- Ngữ cảnh app/tenant nếu xác định được, môi trường dev/giả lập, nguồn được chọn và câu hỏi.
- Chế độ API/File, nhà cung cấp/model thực tế nếu xác định được; không gán model của app cho kết quả từ file khi không biết.
- Câu trả lời hoặc lỗi, dẫn chứng ở dạng đã được phép hiển thị, trạng thái kiểm chứng và cảnh báo nguồn bị cắt.
- Thời gian chờ do giao diện đo, ghi rõ đây là thời gian của thao tác, không mặc nhiên là thời gian model xử lý.
- Liên kết đối tượng/phiên bản và ID biên lai nếu payload có cung cấp.
- Kết quả mong đợi, nhận xét và đánh giá của người dùng.

Các chiều đánh giá độc lập:

| Chiều | Giá trị tối thiểu |
|---|---|
| Độ đúng nội dung | Chưa đánh giá / Đúng / Đúng một phần / Sai / Chưa đủ cơ sở |
| Dẫn chứng hỗ trợ | Chưa đánh giá / Đủ / Một phần / Không hỗ trợ / Không áp dụng |
| Tính thực hiện được | Chưa đánh giá / Làm theo được / Cần bổ sung / Không dùng được / Không áp dụng |
| Ghi chú | Nhận xét và câu trả lời/hướng dẫn mong đợi |

“Thiếu thông tin” không mặc nhiên là “sai”; “đã đánh giá tốt” không mặc nhiên là “đã dùng”. Phản hồi ở đây không được tự thay đổi verification, trạng thái xuất bản hoặc nấc USED của sản phẩm.

### Lưu trữ và xuất dữ liệu

- Đổi nguồn, chuyển trang hoặc tải lại trình duyệt vẫn xem lại được lịch sử và nhận xét đã lưu.
- Lượt mới không ghi đè lượt cũ, kể cả khi hỏi cùng một câu. Mở lịch sử không chạy lại yêu cầu.
- Tái sử dụng biên lai/lịch sử hiện có nếu đủ; không nhầm biên lai vận hành với toàn bộ bản chụp câu hỏi và trả lời.
- Chọn phương án lưu nhỏ nhất phù hợp kiến trúc. Nếu lưu cục bộ trong trình duyệt, ghi rõ “chỉ trên trình duyệt này”, phạm vi phân tách app/tenant và cách xóa; không giới thiệu như lịch sử dùng chung.
- Không lưu API key, mật khẩu, secret webhook hoặc dữ liệu nguồn chưa qua cổng che. Khi đổi app/tenant, không tự hiện lại dữ liệu của ngữ cảnh trước.
- Xuất được JSON có phiên bản định dạng, gồm kết quả và đánh giá. Có bản Markdown dễ đọc để chia sẻ khi người dùng chủ động xuất.
- Nếu chưa có token/chi phí từ nguồn đáng tin, hiển thị “chưa có dữ liệu”; không tự tính ra số tiền chính xác.

### Nghiệm thu

- Hỏi hai câu trên hai nguồn, đổi trang rồi tải lại: vẫn mở đúng hai kết quả và nhận xét tương ứng.
- Xem kết quả cũ vẫn dùng bản nguồn đã lưu hoặc thông báo không có bản chụp; không âm thầm thay bằng nguồn mới.
- Xuất một phiên và đối chiếu: số lượt, nội dung, ngữ cảnh và đánh giá khớp giao diện.
- Đổi app/tenant không trộn lịch sử hoặc đánh giá của các ngữ cảnh.

## 7. P1 — Kịch bản thử có kết quả mong đợi

Thêm điểm bắt đầu “Trải nghiệm theo kịch bản”, có hướng dẫn ngay trên giao diện. Tái sử dụng dữ liệu giả hiện có, bổ sung fixture nhỏ nếu cần; không tự lấy dữ liệu khách thật làm ví dụ công khai.

Mỗi kịch bản nêu: mục tiêu, dữ liệu, điều kiện đầu, thao tác, kết quả mong đợi và giới hạn của phép thử.

Tối thiểu có:

1. **Chưa có tri thức được duyệt:** có thể định tuyến đúng nhưng `/hoi` phải trả không biết với lý do phù hợp.
2. **Soạn nháp → xem/sửa → duyệt → hỏi lại:** chỉ ra điểm nội dung bắt đầu được phép sử dụng.
3. **Xác nhận bước:** bước hiện tại chuyển đúng theo quy trình; không nhảy nếu chỉ mở xem.
4. **Hỏi tự do có dẫn chứng:** đối chiếu phát biểu với đoạn nguồn, kết quả vẫn chưa duyệt.
5. **Nguồn thiếu câu trả lời:** cho phép đánh giá việc từ chối trả lời là đúng.
6. **Lỗi vận hành:** khóa không hợp lệ hoặc backend không sẵn sàng được phân biệt với không biết về nghiệp vụ. Dùng giả lập lỗi, không cần phát sinh chi phí thật.

Ghi rõ dữ liệu và đáp án giả kiểm tra luồng xử lý, chưa chứng minh độ chính xác trên dữ liệu khách thật. Không tự điền điểm đánh giá nội dung bằng kết quả HTTP 200.

## 8. P1 — Trang tổng kết và so sánh phiên

- Tổng số lượt: thành công về vận hành / lỗi vận hành / đang chờ hoặc bị gián đoạn; bên trong các lượt thành công tách biết trả lời và không biết.
- Số lượt đã đánh giá và chưa đánh giá; phân bố từng chiều đánh giá với mẫu số rõ ràng.
- Kết quả định tuyến trên fixture có đáp án được trình bày riêng với đánh giá câu trả lời, hướng dẫn và SOP.
- Liệt kê các lượt sai, thiếu bằng chứng hoặc cần bổ sung để mở lại trực tiếp.
- Xem trạng thái bản nháp, chỉnh sửa và duyệt; không diễn giải “không sửa” thành bằng chứng đã duyệt kỹ.
- So sánh thủ công hai lượt/phiên cạnh nhau. Nêu rõ khác biệt về câu hỏi, nguồn/phiên bản, chế độ và model; không gọi đó là so sánh công bằng nếu đầu vào khác nhau.
- Không suy ra tiết kiệm thời gian xử lý, giảm escalation hoặc ROI chỉ từ điểm định tuyến và tỷ lệ bấm nút.

### Nghiệm thu

- Phiên có cả lượt đúng, sai, không biết, lỗi vận hành và chưa đánh giá được thống kê đúng, không bỏ sót hoặc gộp sai.
- Không có đánh giá thì hiển thị “chưa có đánh giá”, không hiển thị 0% đúng như một kết luận chất lượng.
- Mở tổng kết và so sánh không phát sinh lời gọi model.

## 9. Trình tự triển khai và kiểm tra

1. Xác nhận lại payload, endpoint và cách chạy hiện tại. Ghi các chênh lệch với đề bài trước khi sửa.
2. Làm hiển thị hướng dẫn và dẫn chứng.
3. Làm đọc/sửa/duyệt SOP.
4. Làm phiên, lịch sử và đánh giá; đây là nền cho tổng kết.
5. Làm kịch bản thử và trang tổng kết/so sánh.
6. Cập nhật `docs/21_HUONG_DAN_TEST.md` theo luồng mới và tài liệu liên quan nếu thay hợp đồng.

Kiểm tra phù hợp phạm vi thay đổi:

```powershell
# Tại scripts/giao-dien/console
npm run kiem
npm run build
npm run kiem:so-do

# Tại gốc repo, khi có đổi backend/hợp đồng
dotnet test src/KnowledgePlatform.slnx
python scripts/kiem_dau_cuoi.py
```

Các lệnh backend cần điều kiện database/app theo hướng dẫn hiện hành. Chỉ báo đã chạy khi thực sự chạy và ghi kết quả. Nếu đổi persistence/tenant, chạy thêm phép kiểm liên quan đến cô lập dữ liệu.

Viết kiểm tra có ý nghĩa cho các rủi ro mới: phân tách lịch sử theo ngữ cảnh, khôi phục kết quả sau tải lại, thống kê mẫu số, bảo toàn bản gốc, lỗi từng phần khi duyệt và xem lịch sử không gọi model. Không cần tạo test chỉ để lặp lại markup.

Nghiệm thu bằng trình duyệt trên ít nhất một vòng hoàn chỉnh, không chỉ build thành công. Có thể dùng chế độ File cho vòng không có khóa API; ghi rõ chưa kiểm nhánh API thật nếu chưa chạy. Không thay kết quả thật bằng dữ liệu dựng sẵn rồi báo model đã hoạt động.

## 10. Checklist bàn giao

- [ ] Đọc được hướng dẫn đầy đủ và nguồn của từng ticket.
- [ ] Đọc, sửa, đối chiếu và duyệt SOP qua các thao tác riêng biệt.
- [ ] Không bỏ qua các cổng quyền/duyệt/che dữ liệu hiện có.
- [ ] Lịch sử và đánh giá tồn tại sau chuyển trang/tải lại, phân tách đúng ngữ cảnh.
- [ ] Kịch bản có kết quả mong đợi, bao gồm không biết và lỗi vận hành.
- [ ] Tổng kết dùng đúng mẫu số và không đánh đồng luồng chạy với chất lượng.
- [ ] Xuất JSON và Markdown đối chiếu được với giao diện.
- [ ] Tài liệu chạy thử được cập nhật.
- [ ] Báo cáo các kiểm tra đã chạy, hạn chế và điều chưa được xác nhận bằng chạy thật.

Hoàn thành khi chủ dự án có thể tự bắt đầu một phiên, đi qua tình huống, đọc/đối chiếu nội dung, ghi nhận đánh giá và xuất kết quả mà không cần người giải thích bên cạnh hoặc tự mở API.
