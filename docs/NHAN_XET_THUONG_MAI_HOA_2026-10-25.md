# Nhận xét về khả năng thương mại hóa dự án

> **Ngày đánh giá:** 2026-10-25.  
> **Ngày lưu vào tài liệu:** 2026-10-26.  
> **Trạng thái:** Nhận xét và đề xuất để chủ dự án xem xét; các đề xuất chưa phải quyết định đã chốt.  
> Nội dung được lưu từ nhận xét trong cuộc trò chuyện. Kết quả kiểm thử và nhận định phản ánh thời điểm đánh giá.

**Mình thấy dự án có tiềm năng thương mại đáng để thử. Nhưng hiện tại, phần kỹ thuật trưởng thành hơn phần chứng minh khách hàng sẽ nhận được giá trị và chịu trả tiền.** Hướng phù hợp nhất là một đợt triển khai thử có thu phí, phạm vi hẹp, có người đồng hành. Mình chưa coi đây là SaaS sẵn sàng bán rộng.

Mình đã đọc tài liệu sản phẩm, kiến trúc, các phép đo và đối chiếu với code của luồng nạp dữ liệu, định tuyến, soạn/duyệt tri thức, hướng dẫn quy trình và phân quyền. Mình cũng chạy lại kiểm thử: **607/607 test .NET đạt**, kiểm tra kiểu Vue/TypeScript đạt, kiểm thử phiên trải nghiệm đạt **37/37**. Bài kiểm sơ đồ chưa chạy được vì dịch vụ local ở cổng 8791 chưa bật. Mình chưa kiểm chứng chất lượng bằng model thật và chưa sửa code.

**Điểm mình đánh giá cao nhất là cách nhìn bài toán.**

Bạn đã đi tới một nhận định có giá trị: nhiều doanh nghiệp không chỉ khó tìm tri thức; họ còn **chưa ghi lại được tri thức cần tìm**. Ticket đóng rồi nhưng cách chẩn đoán vẫn nằm trong đầu senior, trong cuộc gọi hoặc phiên remote.

Vòng sản phẩm “gom dữ liệu → soạn nháp → người sửa/duyệt → đưa vào ca mới → ghi nhận kết quả” nhắm đúng vấn đề đó. Nếu làm tốt, khách mua được ba lợi ích cụ thể:

- Nhân viên mới tự xử lý được nhiều việc hơn.
- Senior giảm thời gian trả lời lặp lại.
- Kinh nghiệm xử lý được giữ lại khi nhân sự thay đổi.

Đây là câu chuyện bán hàng dễ hiểu hơn tên “AI Operational Knowledge & Process Platform”.

**Phần nền móng cũng có nhiều quyết định tốt.** Tách Case, Knowledge và Process; giữ nguồn của từng phát biểu; phân biệt AI suy luận với thông tin đã kiểm chứng; có trạng thái “không biết”; kiểm tra cách ly tenant trên PostgreSQL thật; có webhook ký HMAC, thử lại và chống trùng. Những thứ này có ích cho một sản phẩm doanh nghiệp vận hành lâu dài.

Mình cũng thích việc tài liệu ghi lại cả giả định bị bác và số đo không đẹp. Đó là một điểm mạnh thực sự của dự án.

Tuy vậy, có sáu vấn đề mình sẽ đặt lên bàn trước khi thương mại hóa.

**1. Bạn đang chứng minh “hệ thống hoạt động” tốt hơn “hệ thống đáng mua”.**

Nghiệm thu “onboard được khách thứ hai mà không sửa code” là một mốc kỹ thuật tốt. Nhưng khách thứ hai nhận được bao nhiêu hướng dẫn hữu ích, tiết kiệm bao nhiêu công và có muốn tiếp tục dùng hay không là câu chuyện khác. Chính [tiêu chí nghiệm thu hiện tại](18_QUYET_DINH_MUC_3.md) cũng ghi rõ khoảng cách này.

Trong phần mình đọc, chưa thấy bằng chứng đủ mạnh về:

- Người dùng quay lại sử dụng đều đặn.
- Thời gian xử lý giảm sau khi tính cả công kiểm tra câu trả lời.
- Senior tiết kiệm được nhiều công hơn công họ bỏ ra duyệt tri thức.
- Một khách hàng độc lập chấp nhận trả tiền.

Điều đó không chứng minh sản phẩm thiếu giá trị. Nó cho thấy **câu hỏi kinh doanh lớn nhất vẫn chưa được trả lời**.

**2. Chất lượng AI chưa đủ cơ sở để đưa thành lời hứa bán hàng.**

Con số **84,1%** là tín hiệu đáng khích lệ, nhưng được đo trên **88 ca có nhãn**, thuộc một bộ dữ liệu và một cách chạy cụ thể. Trong 62 ca không xác định được nguyên nhân từ hồ sơ, hệ thống vẫn gán nhóm cho 25 ca. Đây là kết luận chưa kiểm chứng được, không thể coi đơn giản là đúng hoặc sai. [Chi tiết phép đo](13_DINH_TUYEN_ISSUE_MOI.md).

Luồng model hiện tại còn dùng ngưỡng tự chấm **0,9 chưa được hiệu chuẩn bằng phép đo**. Vì vậy, “model tự tin cao” hiện chưa đủ để chứng minh “hướng dẫn đáng tin”.

Trước khi bán, mình muốn thấy phép đo trên ticket mới, chạy từng ticket bằng đúng prompt/model sẽ triển khai, với người nghiệp vụ chấm độc lập. Đo cả hướng dẫn đúng, hướng dẫn sai và từ chối đúng; đừng chỉ đo khả năng chọn nhóm.

**3. Phân quyền đang là giới hạn sản phẩm thật, không chỉ là việc hoàn thiện sau.**

Code hiện **chặn duyệt tri thức và quy trình ở chế độ shared multi-tenant** vì ranh giới dữ liệu giữa các khách sạn trong cùng tenant chưa được giải quyết. Đây là cách chặn có trách nhiệm, đồng thời cũng có nghĩa vòng sản phẩm cốt lõi chưa hoàn chỉnh trong chế độ đó. [Cổng duyệt hiện tại](../src/KnowledgePlatform.Api/Knowledge/CongDuyetARl.cs).

Ngoài ra, API key xác thực bên tích hợp; danh tính người duyệt vẫn được truyền vào bằng một chuỗi khai báo. Chưa thể xem đây là cơ chế chứng minh người đó có quyền đọc toàn bộ nguồn và quyền xuất bản tri thức. [Luồng duyệt](../src/KnowledgePlatform.Api/Knowledge/KnowledgeApprovalHandler.cs).

Triển khai riêng cho từng công ty là lựa chọn hợp lý để thử nghiệm. Nhưng nó **không tự giải quyết quyền truy cập giữa nhân viên, phòng ban hoặc khách hàng con bên trong công ty đó**. Nếu giao trách nhiệm này cho hệ thống tích hợp, cần quy định và kiểm chứng rõ.

**4. Có một điểm yếu trực tiếp trong logic hướng dẫn quy trình.**

Khi bước được cấu hình dấu hiệu bằng từ khóa, code dùng phép kiểm `Contains` để xác định bước đã hoàn thành. Vì thế, dấu hiệu “xem log” cũng khớp với “chưa xem log” hoặc “cần xem log”. [Đoạn code](../src/KnowledgePlatform.Domain/Process/TienDoQuyTrinh.cs).

Hậu quả là hệ thống có thể đưa nhân viên sang bước sau khi bước trước chưa làm. Đây là lỗi nghiệp vụ đáng ưu tiên hơn việc thêm tính năng.

Trong pilot, mình đề xuất các bước quan trọng chỉ hoàn tất bằng xác nhận rõ ràng hoặc sự kiện có cấu trúc đáng tin cậy. Dấu hiệu văn bản có thể dùng để đề nghị xác nhận.

**5. Chi phí duyệt tri thức có thể quyết định sản phẩm sống hay chết.**

Dự án từng gặp tình huống người duyệt không sửa gì, kể cả những nhánh bản nháp đánh dấu là suy đoán. Tài liệu đã nhận ra rằng “không sửa” chưa chắc có nghĩa là “đã đọc và đồng ý”. [Ghi nhận về lượt duyệt](02_SUCCESS_METRICS_V1.md).

Đây là cảnh báo thương mại rất lớn: **sản phẩm có thể tiết kiệm công cho support nhưng tạo thêm việc cho senior**.

Cần đo giá trị ròng:

> Công tiết kiệm khi xử lý ca − công sửa/duyệt/cập nhật tri thức − công vận hành và chi phí AI.

Nếu không đo phần trừ, demo sẽ đẹp hơn hiệu quả thật. Khả năng khiến senior muốn đóng góp tri thức là một phần cốt lõi của sản phẩm.

**6. Phạm vi sản phẩm và tài liệu đang rộng hơn mức bằng chứng hiện có.**

Tầm nhìn bao gồm support, CRM, ERP và nhiều nguồn dữ liệu. Tuy nhiên, bằng chứng thực nghiệm mạnh nhất hiện vẫn tập trung vào support và nhóm lỗi hóa đơn của một công ty. Taxonomy trong code cũng đang được nạp theo cấu hình chung của host, mặc định là 19 nhóm hóa đơn; chưa phải một cơ chế cấu hình độc lập theo từng tenant. [Taxonomy hiện tại](../src/KnowledgePlatform.Api/Knowledge/TaxonomyNhom.cs).

Console hiện được thiết kế cho thử nghiệm nội bộ. Nó hữu ích để hiểu hệ thống, nhưng trải nghiệm mà nhân viên support cần là: **ngay trong ticket, thấy việc nên làm tiếp, căn cứ và cách phản hồi**.

Tài liệu thì rất nhiều, nhưng trạng thái cũ và mới nằm xen nhau; có đoạn ghi “chưa code” rồi bên dưới liệt kê đã hoàn tất. Điều này làm người mới khó xác định đâu là sự thật hiện hành. Mình sẽ giữ lịch sử riêng và rút bản trạng thái hiện tại xuống thật ngắn.

**Về cạnh tranh, mình sẽ không dựa vào “AI hỏi đáp có trích nguồn” làm điểm khác biệt chính.** Intercom Copilot đã hỗ trợ hỏi đáp từ tài liệu nội bộ, lịch sử hội thoại/ticket và kiểm tra nguồn; Jira Service Management cũng có AI hỗ trợ từ knowledge base. Đây là những năng lực đã hiện diện trong sản phẩm của họ. [Intercom](https://www.intercom.com/help/en/articles/8587194-how-to-use-copilot), [Atlassian](https://support.atlassian.com/organization-administration/docs/atlassian-intelligence-features-in-jira-service-management/).

Theo mình, cơ hội của bạn nằm ở **một nghiệp vụ cụ thể mà bạn hiểu sâu**, tích hợp tốt vào cách đội đó làm việc, và biến kinh nghiệm xử lý thành hướng dẫn đã kiểm chứng. Đây là hướng tạo khác biệt cần thử, chưa phải lợi thế cạnh tranh đã được chứng minh.

Nếu mình chịu trách nhiệm thương mại hóa, mình sẽ đề xuất:

- **Khách đầu tiên:** đội support phần mềm có nhiều ca lặp lại và phụ thuộc senior. Người mua mục tiêu là trưởng bộ phận support hoặc lãnh đạo kỹ thuật.
- **Phạm vi đầu tiên:** một nhóm lỗi có đủ lưu lượng và dữ liệu, chẳng hạn một phần nghiệp vụ hóa đơn đang có nền thực nghiệm.
- **Gói bán:** thử nghiệm trả phí 4–6 tuần, gồm triển khai, chuẩn bị kho tri thức ban đầu và đo hiệu quả; sau đó mới chốt phí duy trì. Đây là đề xuất thử nghiệm, chưa có cơ sở để chốt mức giá.
- **Tiêu chí quyết định:** người dùng dùng lại; thời gian xử lý giảm với chất lượng không giảm; công duyệt ở mức chấp nhận được; khách muốn gia hạn bằng tiền thật.

Mốc tiếp theo mình muốn dự án đạt là **một khách hàng ngoài công ty của bạn dùng trên ca thật, có số liệu trước/sau và đồng ý tiếp tục trả tiền**. Mốc đó sẽ giúp quyết định nên đầu tư tiếp vào đâu rõ hơn rất nhiều so với hoàn thành thêm các ô trong sơ đồ kiến trúc.
