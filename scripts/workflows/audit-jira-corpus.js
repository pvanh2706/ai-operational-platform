export const meta = {
  name: 'audit-jira-corpus',
  description: 'Quét corpus Jira thật (32 case / 128 evidence) trước khi nạp: bí mật, PII, rò rỉ chéo khách hàng, chủ đề, nguyên nhân, ràng buộc FTS',
  phases: [
    { title: 'Quét', detail: '9 lăng kính độc lập trên cùng corpus' },
    { title: 'Kiểm chứng', detail: 'phản biện đối kháng từng phát hiện rủi ro' },
    { title: 'Tổng hợp', detail: 'gộp thành khuyến nghị nạp hay không nạp' },
  ],
}

const DIR = 'C:/Users/Admin/AppData/Local/Temp/claude/d--MiniProject-23-ai-operational-platform/3f71f800-11f6-413a-ad85-459d78cbe323/scratchpad'

const BOICANH = `
BỐI CẢNH DỰ ÁN (đọc kỹ, nó quyết định cái gì là vấn đề):

Đây là "AI Operational Knowledge & Process Platform" — sản phẩm ĐỂ BÁN, multi-tenant
từ ngày đầu. Nó gom mảnh tri thức rải rác (comment Jira, email, ghi chú) thành bản nháp
SOP để người duyệt. Khách hàng #0 là chính công ty chủ dự án: ezCloud, làm phần mềm quản
lý khách sạn (ezFolio, ezCloudhotel). Mỗi KHÁCH SẠN là một khách hàng của ezCloud.

Dữ liệu bạn sắp đọc là issue THẬT từ Jira "ezCloud Support Team", vừa xuất ra và CHƯA
nạp vào database. Nó sắp được đưa vào bảng evidence_item, rồi Path A sẽ gom ~20 case
liên quan + toàn bộ evidence của chúng vào MỘT request gửi cho model để soạn nháp SOP.
Bản nháp đó được người duyệt rồi thành tri thức chính thức, hiện ra cho mọi nhân viên.

Vì thế: thứ gì lọt vào evidence_item thì (a) model sẽ đọc, (b) có thể xuất hiện lại
trong SOP, (c) hiện ra cho người khác. Đó là lý do phải soi TRƯỚC khi nạp.

Guardrail liên quan:
- G7: ranh giới tenant là NỀN TẢNG. Dữ liệu khách sạn A không được lọt sang khách sạn B.
- G6/AP3: provenance là nền tảng, không được đoán nguồn gốc.
- S8: giá trị của bản nháp gom nằm ở một PHÂN BỐ — "14/20 case đã làm bước này".
  Con số đó chỉ đúng nếu evidence sạch và đếm được.
- R-K4: cả thiết kế đứng trên giả định "một loại vấn đề có 5-10 nguyên nhân",
  và con số đó CHƯA AI ĐẾM. n=1.

FILE CẦN ĐỌC (đọc HẾT cả ba, đừng chỉ đọc một):
  ${DIR}/corpus-cases.txt        — 32 case: khoá nguồn, mốc thời gian, tiêu đề
  ${DIR}/corpus-evidence-1.txt   — 69 mẩu evidence
  ${DIR}/corpus-evidence-2.txt   — 59 mẩu evidence
`

const PHAT_HIEN = {
  type: 'object',
  required: ['summary', 'findings'],
  properties: {
    summary: { type: 'string', description: 'Kết luận 2-4 câu, có số liệu' },
    findings: {
      type: 'array',
      items: {
        type: 'object',
        required: ['sourceReference', 'severity', 'what', 'why'],
        properties: {
          sourceReference: { type: 'string', description: 'khoá nguồn của mẩu evidence, vd jira:ES-346481#comment-802977' },
          severity: { type: 'string', enum: ['cao', 'trung', 'thap'] },
          excerpt: { type: 'string', description: 'trích đúng đoạn có vấn đề, tối đa 200 ký tự' },
          what: { type: 'string', description: 'phát hiện gì' },
          why: { type: 'string', description: 'vì sao nó là vấn đề KHI NẠP VÀO KHO TRI THỨC' },
        },
      },
    },
  },
}

const PHAN_TICH = {
  type: 'object',
  required: ['summary', 'findings'],
  properties: {
    summary: { type: 'string', description: 'Kết luận 2-5 câu, PHẢI có số liệu cụ thể' },
    findings: { type: 'array', items: { type: 'string' }, description: 'Từng phát hiện, mỗi cái một dòng, kèm số' },
    tableRows: { type: 'array', items: { type: 'string' }, description: 'Nếu có phân loại đếm được: mỗi dòng "nhãn | số lượng | ví dụ khoá nguồn"' },
    khuyenNghi: { type: 'array', items: { type: 'string' }, description: 'Việc cụ thể nên làm, nếu có' },
  },
}

const PHAN_QUYET = {
  type: 'object',
  required: ['refuted', 'reason', 'realSeverity'],
  properties: {
    refuted: { type: 'boolean', description: 'true = phát hiện này SAI hoặc không đáng lo' },
    reason: { type: 'string' },
    realSeverity: { type: 'string', enum: ['cao', 'trung', 'thap', 'khong-phai-van-de'] },
    hanhDong: { type: 'string', description: 'nếu có thật thì phải làm gì cụ thể' },
  },
}

// --- Ba chiều RỦI RO: phát hiện sẽ bị phản biện đối kháng ---
const RUI_RO = [
  {
    key: 'bi-mat',
    prompt: `${BOICANH}

NHIỆM VỤ: tìm BÍ MẬT trong corpus — thứ mà nếu nằm trong kho tri thức là sự cố bảo mật.

Tìm: mật khẩu, tài khoản kèm mật khẩu, API key, token, chuỗi kết nối database, khoá ký
số, chứng thư, mã OTP, đường dẫn kèm credential, cấu hình có secret, thông tin đăng nhập
hệ thống thuế/hoá đơn điện tử, thông tin thẻ ngân hàng.

Đã biết chắc có ít nhất MỘT: trong jira:ES-346481#comment-802977 có "ACpass":"Vnpt@2026".
Tìm cho HẾT phần còn lại — đừng dừng ở cái đã biết.

Với mỗi phát hiện, trích đúng đoạn (che bớt phần nhạy cảm nếu quá dài) và nói rõ vì sao
nó nguy hiểm KHI nằm trong evidence_item mà model sẽ đọc.`,
  },
  {
    key: 'du-lieu-ca-nhan',
    prompt: `${BOICANH}

NHIỆM VỤ: tìm DỮ LIỆU CÁ NHÂN trong corpus.

Tìm: họ tên người thật (khách lưu trú, nhân viên khách sạn, nhân viên support), số điện
thoại, email, số CCCD/CMND/hộ chiếu, ngày sinh, địa chỉ nhà, số thẻ, mã đặt phòng gắn với
tên người, ảnh chụp màn hình có thông tin khách.

⚠ Phân biệt cho rõ, đây là chỗ dễ nhầm nhất:
  - Tên KHÁCH SẠN (vd "Ocean Dunes Resort") là tên tổ chức — KHÔNG phải dữ liệu cá nhân,
    và nó CẦN cho tri thức vì nó là ngữ cảnh nghiệp vụ.
  - Tên KHÁCH LƯU TRÚ hoặc người liên hệ (vd "Nguyễn Thị Ngọc Hân") LÀ dữ liệu cá nhân.
Đếm riêng hai loại, đừng gộp.

Nói rõ mỗi phát hiện có cần cho giá trị tri thức không, hay chỉ là dư thừa xoá được.`,
  },
  {
    key: 'ro-ri-cheo-khach',
    prompt: `${BOICANH}

NHIỆM VỤ: tìm dấu hiệu RÒ RỈ CHÉO GIỮA CÁC KHÁCH SẠN — đây là chiều quan trọng nhất
vì nó chạm thẳng G7.

Bối cảnh: ezCloud có nhiều khách sạn dùng chung phần mềm. Mỗi issue thường thuộc về MỘT
khách sạn (thường ghi trong tiêu đề dạng "[Tên khách sạn] ..."). Nhưng comment có thể
nhắc tới khách sạn khác, dán dữ liệu của khách sạn khác, hoặc so sánh "bên A cũng bị".

Tìm:
- evidence của case thuộc khách sạn X nhưng chứa dữ liệu/mã/tên của khách sạn Y
- comment dán log, payload, hoặc bản ghi có mã nhiều khách sạn cùng lúc
- tham chiếu chéo giữa các case của khách sạn khác nhau
- bất kỳ chỗ nào một khách sạn có thể suy ra thông tin vận hành của khách sạn khác

⚠ Trong bản deploy hiện tại, TOÀN BỘ 32 case này sẽ nằm trong CÙNG MỘT tenant
(tenant = ezCloud, không phải tenant = từng khách sạn). Nên hãy trả lời cả câu này:
ranh giới tenant hiện tại có đặt ĐÚNG CHỖ không, hay nó đang gộp nhiều khách sạn vào
một kho mà lẽ ra phải tách? Đây là câu hỏi thiết kế, hãy trả lời thẳng.`,
  },
]

// --- Sáu chiều PHÂN TÍCH: trả số liệu, không cần phản biện ---
const PHAN_TICH_DIMS = [
  {
    key: 'chu-de',
    prompt: `${BOICANH}

NHIỆM VỤ: phân loại 32 case theo CHỦ ĐỀ NGHIỆP VỤ và đếm.

Đây là đầu vào trực tiếp cho §8.2 của dự án — phép đếm quyết định một thứ đắt:
  tập nguyên nhân hữu hạn nhỏ (~10)  → bài toán PHÂN LOẠI, KHÔNG dựng vector DB
  mở, hàng trăm                       → đúng là bài toán tìm kiếm, phải dựng cả stack

Việc cần làm:
1. Đọc 32 tiêu đề + evidence của chúng, gom thành các nhóm chủ đề tự nhiên.
2. Đếm mỗi nhóm bao nhiêu case, cho ví dụ khoá nguồn.
3. Trả lời thẳng: có bao nhiêu case thuộc chủ đề "OTA / booking không về / đồng bộ
   booking"? Đây là first use case của dự án, và cần biết corpus này có phục vụ được nó
   không, hay nó là một mớ hỗn tạp mọi loại vấn đề.
4. Nhóm lớn nhất có bao nhiêu case? Nếu muốn gom SOP thì nhóm nào đủ dày để gom?

Dùng tableRows cho bảng đếm.`,
  },
  {
    key: 'nguyen-nhan',
    prompt: `${BOICANH}

NHIỆM VỤ: kiểm chứng giả định "5-10 nguyên nhân cho một loại vấn đề" (R-K4).

Cả thiết kế của dự án đứng trên con số này và CHƯA AI ĐẾM. Bạn là người đếm đầu tiên.

Việc cần làm:
1. Với những case ĐÃ XONG (có mốc "xong", 7 case) — đọc evidence và rút ra: nguyên nhân
   thật sự là gì? Ghi rõ từng case.
2. Với case CHƯA XONG (25 case) — có suy ra được nguyên nhân từ comment không, hay
   comment dừng ở mức mô tả triệu chứng? Đây là câu quan trọng: nếu comment không chứa
   KẾT LUẬN thì Path A gom được cũng không học được gì.
3. Đếm số nguyên nhân KHÁC NHAU tìm được. So với giả định 5-10: khớp, ít hơn, hay nhiều hơn?
4. Nói thẳng nếu mẫu quá nhỏ để kết luận — đừng ép ra một con số nghe hay.

⚠ §8.1-KQ nói quy trình thật có 5 bước, tuyến tính: Kibana → response → tài liệu →
issue cũ → ĐƯA RA KẾT LUẬN. Và "giá trị nằm trọn ở bước cuối, đó là bước duy nhất không
ai ghi lại". Hãy kiểm điều đó trên dữ liệu thật: bước KẾT LUẬN có được ghi trong comment
không, hay đúng là nó biến mất?`,
  },
  {
    key: 'rang-buoc-fts',
    prompt: `${BOICANH}

NHIỆM VỤ: kiểm bốn ràng buộc full-text search đã ĐO ĐƯỢC, áp lên dữ liệu THẬT này.

Bốn ràng buộc (đã đo trên PostgreSQL 18 của dự án, đều là THẤT BẠI IM LẶNG — không
crash, chỉ trả kết quả sai):

1) RLS GIẾT INDEX GIN. Toán tử @@ không leakproof nên nó không bao giờ thành index
   condition khi RLS bật. Cột tsvector lưu sẵn là thứ chịu lực, index GIN là đồ thừa.
   → Với 128 mẩu evidence dài trung bình 559 ký tự (max 5764), hãy ước lượng: quét tuần
     tự trên cỡ dữ liệu này có chấp nhận được không? Và ở cỡ nào thì không?

2) MỘT DẤU GẠCH NGANG ĐẢO NGƯỢC TRUY VẤN. websearch_to_tsquery('simple', 'khong or -ve')
   biến '-ve' thành PHỦ ĐỊNH, và truy vấn trả về gần như toàn bộ kho.
   → Tìm trong corpus: có bao nhiêu tiêu đề/nội dung chứa token bắt đầu bằng dấu '-'?
     Người dùng dán tiêu đề Jira vào ô tìm kiếm là chuyện thường — hãy trích ví dụ THẬT
     từ corpus này sẽ kích hoạt lỗi đó.

3) websearch_to_tsquery NÉM với chuỗi dài ("stack depth limit exceeded").
   → Tiêu đề dài nhất trong corpus bao nhiêu ký tự? Có đủ dài để gây lỗi không?

4) ts_rank_cd KHÔNG CÓ TRẦN theo số lần lặp: comment lặp một từ 10 lần (trọng số B) được
   điểm 4, tiêu đề đúng chủ đề (trọng số A) được 1. Comment dài đè bẹp tiêu đề 4:1.
   → Tìm trong corpus những mẩu evidence sẽ gây ra đúng chuyện đó: log dump, payload
     XML/JSON, chuỗi lặp. Trích khoá nguồn cụ thể. Ước lượng chúng chiếm bao nhiêu %.

Ngoài ra: tiếng Việt CÓ DẤU với cấu hình 'simple' thì token hoá thế nào? "phòng" và
"phong" có thành hai token khác nhau không, và điều đó ảnh hưởng gì tới việc tìm? Đây là
corpus tiếng Việt, mà 'simple' không có bộ stemmer tiếng Việt.`,
  },
  {
    key: 'rac-va-nhieu',
    prompt: `${BOICANH}

NHIỆM VỤ: tìm evidence RÁC — mẩu không mang giá trị tri thức nào nhưng vẫn chiếm chỗ
trong kho gom và vẫn được đếm vào con số "14/20 case đã làm bước này".

Đã biết vài cái: có comment chỉ chứa "80771", có comment chỉ chứa "ES-346619", có comment
chỉ chứa "0304746657". Tìm cho hết.

Tìm:
- comment chỉ có một con số, một mã, một đường link trần
- comment tự động do bot/workflow Jira sinh ra (chuyển trạng thái, gán người, nhắc hạn)
- comment lặp lại nội dung của comment khác
- chữ ký, lời chào, "đã nhận", "ok em", "cảm ơn anh" — lịch sự nhưng rỗng nghĩa
- ảnh/file đính kèm chỉ còn lại metadata JSON (vd {"url":...,"fileName":"image.png"})
  → loại này đặc biệt quan trọng: nó được khai machineReadability=High nhưng nội dung
    thật nằm trong ẢNH mà hệ thống không đọc được. Đó đúng là trạng thái
    KNOWLEDGE_EXISTS_NOT_RETRIEVABLE mà sản phẩm cần nhìn thấy chứ không được giấu đi.

Đếm: bao nhiêu / 128 mẩu là rác? Bao nhiêu là ảnh không đọc được? Sau khi bỏ rác thì
mỗi case còn trung bình mấy mẩu thật?

Trả lời thẳng một câu: với corpus sau khi lọc rác, Path A có đủ chất liệu để gom một SOP
tử tế không?`,
  },
  {
    key: 'nhan-doc-may',
    prompt: `${BOICANH}

NHIỆM VỤ: kiểm nhãn machineReadability mà script đã gán.

Script xuất Jira gán CỨNG machineReadability="High" cho MỌI mẩu, với lý do ghi trong
code: "connector biết nó đang đẩy text thuần từ REST API nên khai High là khai thật".

Hãy phản biện điều đó trên dữ liệu thật. Nguyên tắc IM-19 nói: "Tự gán High cho mọi thứ
là text sẽ dán nhãn sai cho ảnh chưa OCR — đúng trạng thái KNOWLEDGE_EXISTS_NOT_RETRIEVABLE
mà sản phẩm cần nhìn thấy."

Việc cần làm:
1. Đếm bao nhiêu mẩu THỰC SỰ là text đọc được và hiểu được.
2. Đếm bao nhiêu mẩu chỉ là metadata của ảnh/file — nội dung thật nằm trong ảnh.
3. Đếm bao nhiêu mẩu là dump kỹ thuật (XML/JSON/log) — máy đọc được nhưng là dữ liệu
   máy, không phải tri thức người viết.
4. Đề xuất luật gán nhãn cụ thể mà script nên dùng thay cho "luôn High". Viết ra dưới
   dạng luật kiểm được, đừng nói chung chung.

Nhãn sai không làm hệ thống crash — nó làm sản phẩm TƯỞNG mình đọc được thứ nó không đọc
được. Đó là loại sai tệ nhất trong dự án này.`,
  },
  {
    key: 'gia-tri-path-a',
    prompt: `${BOICANH}

NHIỆM VỤ: trả lời câu hỏi đắt nhất — corpus này có ĐỦ để Path A tạo ra giá trị không?

Path A: người nói "tôi cần SOP cho chủ đề X" → hệ thống kéo ~20 case liên quan + evidence
→ model soạn nháp SOP → người sửa → duyệt. Giá trị đo bằng M2: số nháp được duyệt + mức
sửa diff(A,B) + tỉ lệ bỏ giữa đường.

Sự thật về corpus này:
- 32 case, nhưng chỉ 7 case ĐÃ XONG (có mốc resolved). 25 case còn đang mở.
- JQL đã dùng: issue của "ezCloud Support Team", loại Service Request/Incident/Leakage,
  có "Kỹ thuật phụ trách", TẠO TỪ ĐẦU THÁNG NÀY (createdDate >= startOfMonth()).
  Hôm nay là 2026-09-04, nên đây là dữ liệu của ĐÚNG 4 NGÀY.

Việc cần làm:
1. Với 7 case đã xong: đọc evidence của chúng. Chúng có chứa CÁCH XỬ LÝ và KẾT QUẢ không,
   hay chỉ có mô tả vấn đề? Trích dẫn cụ thể.
2. Nếu bây giờ gom 7 case đó thành một SOP, bản nháp đó sẽ nói được gì? Hãy thử phác ra
   thật — nếu phác không nổi thì nói rõ vì sao.
3. Con số "14/20 case đã làm bước này" mà S8 đòi: với corpus này có tính ra được không?
4. Đề xuất JQL TỐT HƠN. Nói rõ nên đổi gì và vì sao. Cân nhắc: cần case đã xong, cần đủ
   nhiều (§8.2 nói n=50-200), cần cùng một loại vấn đề để gom được, và cần khoảng thời
   gian đủ dài. Viết JQL cụ thể chạy được, đừng mô tả chung chung.
5. Trả lời một câu dứt khoát: nạp corpus 4 ngày này vào kho là ĐÚNG hay LÃNG PHÍ?
   Được gì, hỏng gì. Nếu nạp thì nạp cả 32 hay chỉ 7 case đã xong?`,
  },
]

log('Đọc corpus: 32 case, 128 evidence, 3 file trong scratchpad')

phase('Quét')

// Chiều rủi ro: quét rồi phản biện đối kháng TỪNG phát hiện, không chờ chiều khác xong.
const CAP_KIEM_CHUNG = 6
const ketQuaRuiRo = pipeline(
  RUI_RO,
  d => agent(d.prompt, { label: `quét:${d.key}`, phase: 'Quét', schema: PHAT_HIEN }),
  (kq, d) => {
    if (!kq || !kq.findings || kq.findings.length === 0) return { dim: d.key, summary: kq ? kq.summary : 'không chạy được', verified: [] }
    const canKiem = kq.findings.slice(0, CAP_KIEM_CHUNG)
    if (kq.findings.length > CAP_KIEM_CHUNG) {
      log(`⚠ ${d.key}: ${kq.findings.length} phát hiện, chỉ phản biện ${CAP_KIEM_CHUNG} cái nặng nhất — ${kq.findings.length - CAP_KIEM_CHUNG} cái còn lại KHÔNG được kiểm`)
    }
    return parallel(canKiem.map((f, i) => () =>
      agent(`${BOICANH}

NHIỆM VỤ: PHẢN BIỆN một phát hiện. Mặc định của bạn là BÁC BỎ — chỉ công nhận khi bạn
tự kiểm được bằng chứng trong file.

Phát hiện cần phản biện:
  nguồn:    ${f.sourceReference}
  mức:      ${f.severity}
  nội dung: ${f.what}
  lý do:    ${f.why}
  trích:    ${f.excerpt || '(không có)'}

Việc bạn phải làm:
1. MỞ FILE và tìm đúng ${f.sourceReference}. Nếu không tìm thấy → refuted=true, vì phát
   hiện trỏ vào thứ không tồn tại.
2. Đọc nguyên văn. Trích dẫn có bị cắt xén cho có vẻ nghiêm trọng hơn thực tế không?
3. Hỏi ba câu:
   - Đây có thật là thứ nó nói không, hay chỉ giống? (vd chuỗi trông như mật khẩu nhưng
     là tên biến; số trông như CCCD nhưng là mã đơn hàng)
   - Nó có phải dữ liệu SỐNG không, hay đã hết hạn / là dữ liệu mẫu / môi trường test?
   - Nếu nạp vào evidence_item thì HẬU QUẢ THẬT là gì? Nói cụ thể, đừng nói "rủi ro bảo mật".
4. Nếu công nhận: hành động cụ thể phải làm là gì? (che, bỏ mẩu đó, bỏ cả case, hay
   nạp nhưng đánh dấu?)

Không chắc thì refuted=true. Cảnh báo sai làm người ta ngừng tin cảnh báo.`,
        { label: `phản biện:${d.key}#${i + 1}`, phase: 'Kiểm chứng', schema: PHAN_QUYET })
        .then(v => ({ finding: f, verdict: v }))
    )).then(vs => ({
      dim: d.key,
      summary: kq.summary,
      tongPhatHien: kq.findings.length,
      soDuocKiem: canKiem.length,
      verified: vs.filter(Boolean),
      chuaKiem: kq.findings.slice(CAP_KIEM_CHUNG),
    }))
  }
)

// Chiều phân tích: chạy song song với chiều rủi ro, không chặn nhau.
const ketQuaPhanTich = parallel(PHAN_TICH_DIMS.map(d => () =>
  agent(d.prompt, { label: `phân tích:${d.key}`, phase: 'Quét', schema: PHAN_TICH })
    .then(r => ({ dim: d.key, ...(r || { summary: 'không chạy được', findings: [] }) }))
))

const [ruiRo, phanTich] = await Promise.all([ketQuaRuiRo, ketQuaPhanTich])

const ruiRoSach = ruiRo.filter(Boolean)
const xacNhan = ruiRoSach.flatMap(r =>
  (r.verified || []).filter(v => v.verdict && !v.verdict.refuted)
    .map(v => ({ dim: r.dim, ...v.finding, phanQuyet: v.verdict })))
const biBacBo = ruiRoSach.flatMap(r =>
  (r.verified || []).filter(v => v.verdict && v.verdict.refuted)
    .map(v => ({ dim: r.dim, what: v.finding.what, lyDoBacBo: v.verdict.reason })))

log(`Phản biện xong: ${xacNhan.length} phát hiện rủi ro đứng vững, ${biBacBo.length} bị bác bỏ`)

phase('Tổng hợp')

const tongHop = await agent(`${BOICANH}

NHIỆM VỤ: bạn là người cuối cùng đọc tất cả. Viết bản kết luận cho CHỦ DỰ ÁN — người sẽ
quyết định có bấm nút nạp corpus này vào database hay không, ngay sau khi đọc bạn.

Họ là người thực dụng, đã yêu cầu rõ: "phản biện TRƯỚC khi đề xuất, không chỉ đồng ý",
và "ngôn ngữ dễ hiểu, tránh thuật ngữ không cần thiết". Viết tiếng Việt.

=== PHÁT HIỆN RỦI RO ĐÃ ĐỨNG VỮNG SAU PHẢN BIỆN (${xacNhan.length}) ===
${JSON.stringify(xacNhan, null, 1)}

=== ĐÃ BỊ BÁC BỎ — đừng nhắc lại như thể có thật (${biBacBo.length}) ===
${JSON.stringify(biBacBo, null, 1)}

=== PHÁT HIỆN RỦI RO CHƯA ĐƯỢC PHẢN BIỆN (vượt trần, phải nói rõ là chưa kiểm) ===
${JSON.stringify(ruiRoSach.flatMap(r => (r.chuaKiem || []).map(f => ({ dim: r.dim, what: f.what, sourceReference: f.sourceReference }))), null, 1)}

=== SÁU CHIỀU PHÂN TÍCH ===
${JSON.stringify(phanTich.filter(Boolean), null, 1)}

Bản kết luận phải có, theo đúng thứ tự này:

1. MỘT CÂU TRẢ LỜI DỨT KHOÁT ở dòng đầu: nạp được chưa? Nếu chưa thì thiếu đúng cái gì.

2. NHỮNG THỨ PHẢI XỬ LÝ TRƯỚC KHI NẠP — xếp theo mức nghiêm trọng. Mỗi cái: nó là gì,
   nằm ở đâu (khoá nguồn cụ thể), hậu quả thật nếu bỏ qua, và cách xử lý cụ thể.

3. CORPUS NÀY ĐÁNG GIÁ BAO NHIÊU — trả lời bằng số: bao nhiêu case dùng được, bao nhiêu
   evidence thật sau khi trừ rác, có gom nổi một SOP không.

4. JQL NÊN ĐỔI THÀNH GÌ — viết câu JQL cụ thể chạy được, kèm lý do từng mệnh đề.

5. ĐIỀU BẤT NGỜ NHẤT — thứ mà chủ dự án chưa biết và sẽ thay đổi cách họ nghĩ. Chỉ một,
   chọn cái nặng nhất. Nếu các chiều phân tích mâu thuẫn nhau ở đâu, nói ra ở đây.

6. CÂU HỎI CẦN CHỦ DỰ ÁN QUYẾT — tối đa 3, mỗi câu kèm khuyến nghị của bạn và lý do.
   Chỉ đưa vào những câu mà câu trả lời THAY ĐỔI việc phải làm tiếp.

Yêu cầu về giọng văn: không tô hồng, không dùng "rủi ro bảo mật" chung chung khi có thể
nói "mật khẩu tài khoản hoá đơn điện tử của khách sạn X nằm nguyên văn ở comment Y".
Nếu mẫu quá nhỏ để kết luận điều gì, nói thẳng là quá nhỏ.`,
  { label: 'tổng hợp', phase: 'Tổng hợp' })

return {
  soPhatHienRuiRoDungVung: xacNhan.length,
  soBiBacBo: biBacBo.length,
  phatHienRuiRo: xacNhan,
  phanTich: phanTich.filter(Boolean),
  ketLuan: tongHop,
}
