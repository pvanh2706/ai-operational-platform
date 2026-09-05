export const meta = {
  name: 'dem-nguyen-nhan-rk4',
  description: 'Đếm nguyên nhân trên 150 case hoá đơn thật để giải R-K4: tập nguyên nhân là hữu hạn nhỏ (~10) hay mở (hàng trăm)?',
  phases: [
    { title: 'Rút', detail: '10 agent, mỗi agent 15 case, rút nguyên nhân THÔ' },
    { title: 'Gộp', detail: '3 agent GỘP ĐỘC LẬP — bước này là chỗ hai vòng trước phân kỳ' },
    { title: 'Phản biện', detail: 'một tìm gộp quá tay, một tìm chia quá vụn' },
    { title: 'Tổng hợp', detail: 'kết luận cho R-K4' },
  ],
}

const DIR = 'C:/Users/Admin/AppData/Local/Temp/claude/d--MiniProject-23-ai-operational-platform/3f71f800-11f6-413a-ad85-459d78cbe323/scratchpad'

const BOICANH = `
BỐI CẢNH — đọc kỹ, nó quyết định cái gì là câu trả lời đúng.

Dự án "AI Operational Knowledge & Process Platform" của ezCloud (phần mềm quản lý khách
sạn: ezFolio, ezCloudhotel). Nền tảng gom mảnh tri thức rải rác trong Jira thành bản
nháp SOP cho người duyệt.

CÂU HỎI DUY NHẤT CẦN TRẢ LỜI (ký hiệu \`R-K4\` trong dự án):

    Một LOẠI vấn đề có bao nhiêu nguyên nhân khác nhau?

Nó quyết định một thứ rất đắt, và chọn sai tạo ra đúng thứ dự án gọi là "giàn giáo tạm":

    hữu hạn nhỏ (~5-10)  ->  đây là bài toán PHÂN LOẠI. KHÔNG dựng vector DB / RAG.
    mở, hàng trăm        ->  đúng là bài toán TÌM KIẾM. Phải dựng cả stack đó.

Giả định hiện tại của dự án là "5-10 nguyên nhân", và nó đứng trên **n=1** — chưa ai đếm.
Một vòng phân tích trước trên 32 case đã thất bại theo cách rất đáng biết: hai lượt phân
tích đọc CÙNG một dữ liệu ra HAI kết luận ngược nhau (một bên ">10, chưa bão hoà", bên
kia "<5"). Cả hai đều lập luận được. Kết luận đúng lúc đó là "n=32 quá nhỏ".

Corpus lần này: **150 case đã đóng, cùng MỘT chủ đề (hoá đơn điện tử), trải 12 tháng**,
lấy từ Jira thật bằng JQL. Đây là phép đếm thật đầu tiên của dự án.

BA GUARDRAIL BẮT BUỘC TUÂN:
- \`G4\` UNKNOWN LÀ DỮ LIỆU HẠNG NHẤT. Nếu evidence không chứa nguyên nhân thì phải nói
  "không xác định được". ÉP RA một nguyên nhân là làm hỏng chính phép đếm — và đây là
  cạm bẫy lớn nhất của nhiệm vụ này, vì mô hình luôn có thể bịa ra một nguyên nhân
  nghe hợp lý từ một tiêu đề.
- \`G3\` FACT ≠ AI INFERENCE. Phân biệt "evidence NÓI RÕ nguyên nhân" với "tôi SUY RA".
- \`G6\`/\`AP3\` không đoán nguồn gốc. Nguyên nhân phải trích dẫn được.

MỘT SỰ THẬT ĐÃ ĐO VỀ CORPUS NÀY, phải biết trước khi đọc:
Case ĐÃ ĐÓNG ở Jira này thường KHÔNG ghi cách xử lý. Đo trên một corpus khác: 7/7 case
đã đóng chỉ ghi "hết triệu chứng" ("Done nhé" — 23 ký tự; một case đóng bằng đúng một
tấm ảnh), còn nguyên nhân thật thì nằm ở case CÒN MỞ. Lý do: bước kết luận xảy ra trên
điện thoại và trong phiên remote desktop, Jira chỉ giữ lại cái bóng.
=> Nên tỉ lệ "không xác định được" CAO là một kết quả HOÀN TOÀN HỢP LỆ, và có lẽ là kết
   quả đúng. Đừng cố làm nó thấp đi.

Chuỗi [CREDENTIAL] trong dữ liệu là chỗ đã che thông tin đăng nhập. Bỏ qua, nó không
liên quan tới nguyên nhân.
`

const RUT_SCHEMA = {
  type: 'object',
  required: ['cases'],
  properties: {
    cases: {
      type: 'array',
      items: {
        type: 'object',
        required: ['key', 'nguyenNhan', 'mucChacChan'],
        properties: {
          key: { type: 'string', description: 'vd ES-346481' },
          nguyenNhan: {
            type: 'string',
            description: 'MỘT câu, cụ thể, ở mức CƠ CHẾ chứ không phải triệu chứng. ' +
              'Nếu không xác định được thì ghi đúng chữ "KHÔNG XÁC ĐỊNH ĐƯỢC".',
          },
          mucChacChan: {
            type: 'string',
            enum: ['evidence-noi-ro', 'toi-suy-ra', 'khong-xac-dinh-duoc'],
          },
          trichDan: { type: 'string', description: 'nguyên văn đoạn chống lưng, tối đa 200 ký tự. Rỗng nếu không xác định được.' },
          buocXuLy: {
            type: 'array',
            items: { type: 'string' },
            description: 'Các bước xử lý THẤY ĐƯỢC trong evidence (vd "kiểm cấu hình hoá đơn", ' +
              '"gọi khách", "remote vào máy", "tra log"). Rỗng nếu không thấy bước nào. ' +
              'Đây là đầu vào cho con số "14/20 case đã làm bước này" mà dự án cần.',
          },
        },
      },
    },
    nhanXet: { type: 'string', description: 'Điều đáng nói về lô này, nếu có' },
  },
}

const GOP_SCHEMA = {
  type: 'object',
  required: ['nhom', 'soNhom', 'lyDoRanhGioi', 'daBaoHoa', 'giaiThich'],
  properties: {
    nhom: {
      type: 'array',
      items: {
        type: 'object',
        required: ['ten', 'moTa', 'caseKeys'],
        properties: {
          ten: { type: 'string', description: 'tên nhóm nguyên nhân, ngắn' },
          moTa: { type: 'string', description: 'cơ chế chung của nhóm này' },
          caseKeys: { type: 'array', items: { type: 'string' }, description: 'MỌI case thuộc nhóm' },
        },
      },
    },
    soNhom: { type: 'integer' },
    khongXacDinhDuoc: { type: 'array', items: { type: 'string' }, description: 'case không xác định được nguyên nhân' },
    lyDoRanhGioi: { type: 'string', description: 'Vì sao gộp/tách ở ĐÚNG chỗ đó. Đây là phần quan trọng nhất.' },
    daBaoHoa: { type: 'boolean', description: 'Nhóm mới có ngừng xuất hiện khi đọc thêm case không?' },
    giaiThich: { type: 'string', description: 'Bằng chứng cho daBaoHoa. Nếu chưa bão hoà thì nói rõ.' },
  },
}

const PHAN_BIEN_SCHEMA = {
  type: 'object',
  required: ['ketLuan', 'bangChung', 'soNhomDeXuat'],
  properties: {
    ketLuan: { type: 'string' },
    bangChung: { type: 'array', items: { type: 'string' }, description: 'Ví dụ CỤ THỂ kèm case key' },
    soNhomDeXuat: { type: 'integer', description: 'Theo bạn con số đúng là bao nhiêu' },
    doTinCay: { type: 'string', enum: ['cao', 'trung', 'thap'] },
  },
}

log('Corpus: 150 case hoá đơn đã đóng, 12 tháng, 345 evidence, credential đã che')

// ---------------------------------------------------------------- Phase 1: RÚT
phase('Rút')

const LO = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
const ketQuaRut = await parallel(LO.map(i => () =>
  agent(`${BOICANH}

NHIỆM VỤ: đọc ${DIR}/rk4-lo-${i}.txt (15 case, mỗi case kèm toàn bộ evidence của nó) và
rút NGUYÊN NHÂN cho TỪNG case.

Với mỗi case, trả về đúng bốn thứ:

1. \`nguyenNhan\` — MỘT câu, ở mức CƠ CHẾ, không phải triệu chứng.
     triệu chứng (SAI): "hoá đơn không đồng bộ sang VNPT"
     cơ chế (ĐÚNG):     "ký hiệu hoá đơn cấu hình sai nên VNPT từ chối payload"
   Nếu evidence không đủ để nói cơ chế → ghi đúng chữ "KHÔNG XÁC ĐỊNH ĐƯỢC".

2. \`mucChacChan\` — ba mức, và ranh giới giữa chúng quan trọng hơn cả nguyên nhân:
     evidence-noi-ro       evidence có câu nói thẳng nguyên nhân hoặc cách sửa
     toi-suy-ra            tôi ghép từ triệu chứng + kiến thức chung. CÓ THỂ SAI.
     khong-xac-dinh-duoc   evidence chỉ có triệu chứng, hoặc chỉ có "đã xong"
   ⚠ Đừng dùng "toi-suy-ra" để tránh phải nói "không xác định được". Nếu bạn không có
     câu nào trong evidence để trích thì đó là "khong-xac-dinh-duoc", không phải suy ra.

3. \`trichDan\` — nguyên văn đoạn chống lưng. Không có thì để rỗng, đừng diễn giải.

4. \`buocXuLy\` — các bước xử lý THẤY ĐƯỢC trong evidence. Dùng từ NGẮN và LẶP LẠI ĐƯỢC
   (vd "kiểm cấu hình", "remote vào máy", "gọi khách", "tra log", "sửa dữ liệu",
   "báo dev", "hướng dẫn khách") để lô khác dùng cùng từ. Không thấy bước nào thì rỗng.

⚠ CẠM BẪY LỚN NHẤT: mô hình luôn bịa được một nguyên nhân nghe hợp lý từ một tiêu đề.
  Nếu lô của bạn có 10/15 case "không xác định được" thì HÃY TRẢ VỀ ĐÚNG NHƯ VẬY. Con số
  đó là dữ liệu thật và nó quyết định một kiến trúc; làm nó đẹp lên là phá phép đếm.`,
    { label: `rút:lô-${i}`, phase: 'Rút', schema: RUT_SCHEMA })
))

const tatCa = ketQuaRut.filter(Boolean).flatMap(r => r.cases || [])
const theoMuc = {}
for (const c of tatCa) theoMuc[c.mucChacChan] = (theoMuc[c.mucChacChan] || 0) + 1
log(`Rút xong ${tatCa.length}/150 case — ${JSON.stringify(theoMuc)}`)

// Danh sách nguyên nhân thô, gửi nguyên cho cả ba agent gộp.
const danhSachTho = tatCa
  .map(c => `${c.key} [${c.mucChacChan}] ${c.nguyenNhan}`)
  .join('\n')

const buocDem = {}
for (const c of tatCa) for (const b of (c.buocXuLy || [])) {
  const k = b.toLowerCase().trim()
  buocDem[k] = (buocDem[k] || 0) + 1
}

// ---------------------------------------------------------------- Phase 2: GỘP
phase('Gộp')

const GOC_NHIN = [
  {
    key: 'chat',
    huong: `Gộp CHẶT: ưu tiên ít nhóm, mỗi nhóm là một CƠ CHẾ KỸ THUẬT rộng. Hai nguyên
nhân cùng cơ chế nhưng khác chi tiết cấu hình thì CÙNG nhóm.`,
  },
  {
    key: 'long',
    huong: `Tách LỎNG: ưu tiên phân biệt, hai nguyên nhân cần HAI CÁCH SỬA KHÁC NHAU thì
là HAI nhóm — kể cả khi cơ chế nghe giống nhau.`,
  },
  {
    key: 'theo-sop',
    huong: `Gộp theo TIÊU CHÍ DÙNG ĐƯỢC: hai nguyên nhân cùng nhóm nếu và chỉ nếu MỘT
bản SOP duy nhất hướng dẫn xử lý được cả hai. Đây là tiêu chí gần nhất với mục đích thật
của sản phẩm — nhóm là để viết SOP, không phải để phân loại cho đẹp.`,
  },
]

const ketQuaGop = await parallel(GOC_NHIN.map(g => () =>
  agent(`${BOICANH}

NHIỆM VỤ: GỘP danh sách nguyên nhân thô dưới đây thành các NHÓM, rồi trả lời R-K4.

⚠ ĐÂY LÀ BƯỚC ĐÃ LÀM HỎNG VÒNG PHÂN TÍCH TRƯỚC. Hai lượt gộp trên cùng dữ liệu ra hai
con số ngược nhau, vì mỗi lượt tự chọn một độ mịn rồi lập luận trên đó. Nên lần này ba
agent gộp theo BA HƯỚNG KHÁC NHAU, độc lập, và ba con số sẽ được đem so.

HƯỚNG CỦA BẠN:
${g.huong}

Hãy gộp THEO ĐÚNG HƯỚNG NÀY, đừng cố tìm điểm cân bằng — điểm cân bằng là việc của bước
sau. Vai trò của bạn là cho biết con số ra bao nhiêu KHI dùng tiêu chí này.

DANH SÁCH ${tatCa.length} NGUYÊN NHÂN THÔ (case · mức chắc chắn · nguyên nhân):
${danhSachTho}

YÊU CẦU:
1. \`nhom\` — mỗi nhóm có tên, mô tả cơ chế, và MỌI case key thuộc nó.
   ⚠ Case có mức \`khong-xac-dinh-duoc\` KHÔNG được nhét vào nhóm nào — đưa chúng vào
     \`khongXacDinhDuoc\`. Nhét chúng vào một nhóm là biến "không biết" thành "biết",
     đúng thứ \`G4\` sinh ra để chặn.
   ⚠ MỌI case phải xuất hiện ĐÚNG MỘT LẦN, trong \`nhom\` hoặc trong \`khongXacDinhDuoc\`.
     Tổng phải bằng ${tatCa.length}. Bước sau sẽ kiểm bằng code.
2. \`lyDoRanhGioi\` — vì sao gộp/tách ở ĐÚNG chỗ đó. Nêu ít nhất một cặp nguyên nhân mà
   bạn đã cân nhắc rồi quyết định gộp, và một cặp quyết định tách. Đây là phần quan
   trọng nhất của câu trả lời, quan trọng hơn con số.
3. \`daBaoHoa\` — nhóm mới có ngừng xuất hiện không? Cách kiểm: xếp case theo thứ tự,
   nhóm thứ k xuất hiện lần đầu ở case nào? Nếu nhóm mới vẫn xuất hiện ở 20% case cuối
   thì CHƯA bão hoà, và con số bạn đưa ra là cận DƯỚI của thực tế.
4. \`giaiThich\` — bằng chứng cho kết luận bão hoà. Nếu mẫu quá nhỏ để nói thì nói thẳng.`,
    { label: `gộp:${g.key}`, phase: 'Gộp', schema: GOP_SCHEMA })
    .then(r => ({ goc: g.key, ...(r || {}) }))
))

// --- Kiểm bằng CODE: agent có bỏ sót hay gán trùng case không? ---
const gopSach = ketQuaGop.filter(Boolean).filter(r => r.nhom)
const kiemGop = gopSach.map(r => {
  const trongNhom = (r.nhom || []).flatMap(n => n.caseKeys || [])
  const khong = r.khongXacDinhDuoc || []
  const het = [...trongNhom, ...khong]
  const dem = {}
  for (const k of het) dem[k] = (dem[k] || 0) + 1
  const trung = Object.entries(dem).filter(([, n]) => n > 1).map(([k]) => k)
  const thieu = tatCa.map(c => c.key).filter(k => !dem[k])
  return {
    goc: r.goc,
    soNhom: (r.nhom || []).length,
    soCaseTrongNhom: trongNhom.length,
    soKhongXacDinh: khong.length,
    tong: het.length,
    caseGanTrung: trung,
    caseBiBoSot: thieu,
    daBaoHoa: r.daBaoHoa,
  }
})
log(`Gộp xong: ${kiemGop.map(k => `${k.goc}=${k.soNhom} nhóm`).join(' · ')}`)
for (const k of kiemGop) {
  if (k.caseBiBoSot.length || k.caseGanTrung.length) {
    log(`⚠ ${k.goc}: bỏ sót ${k.caseBiBoSot.length} case, gán trùng ${k.caseGanTrung.length} — con số của hướng này KHÔNG đáng tin nguyên trạng`)
  }
}

// ------------------------------------------------------------ Phase 3: PHẢN BIỆN
phase('Phản biện')

const tomTatGop = gopSach.map(r => ({
  goc: r.goc,
  soNhom: r.soNhom,
  tenNhom: (r.nhom || []).map(n => `${n.ten} (${(n.caseKeys || []).length} case)`),
  lyDoRanhGioi: r.lyDoRanhGioi,
  daBaoHoa: r.daBaoHoa,
  giaiThich: r.giaiThich,
  soKhongXacDinh: (r.khongXacDinhDuoc || []).length,
}))

const HAI_HUONG = [
  {
    key: 'gop-qua-tay',
    prompt: `Ba lượt gộp cho ba con số. NHIỆM VỤ CỦA BẠN: tìm chỗ GỘP QUÁ TAY — nơi hai
nguyên nhân cần HAI cách sửa khác nhau bị nhét vào một nhóm, làm con số NHỎ HƠN thực tế.

Đây là chiều sai nguy hiểm hơn, vì nó dẫn tới kết luận "chỉ ~10 nguyên nhân, không cần
vector DB" — một quyết định kiến trúc khó đảo. Nếu con số thật lớn hơn nhiều thì hệ
thống phân loại sẽ âm thầm trả sai và không ai biết vì sao.

Đọc ${DIR}/rk4-lo-1.txt và ${DIR}/rk4-lo-5.txt để tự kiểm bằng dữ liệu gốc — đừng chỉ
lập luận trên tên nhóm.`,
  },
  {
    key: 'chia-qua-vun',
    prompt: `Ba lượt gộp cho ba con số. NHIỆM VỤ CỦA BẠN: tìm chỗ CHIA QUÁ VỤN — nơi hai
biến thể của CÙNG một cơ chế bị tách thành hai nhóm, làm con số LỚN HƠN thực tế.

Chiều sai này dẫn tới "hàng trăm nguyên nhân, phải dựng vector DB" — tức xây đúng thứ
\`D5\` gọi là giàn giáo tạm, tốn công và thành nợ khi model mạnh lên.

⚠ Phép thử sắc nhất: nếu MỘT bản SOP hướng dẫn xử lý được cả hai thì chúng là MỘT nhóm,
vì mục đích của việc nhóm là để viết SOP.

Đọc ${DIR}/rk4-lo-1.txt và ${DIR}/rk4-lo-5.txt để tự kiểm bằng dữ liệu gốc.`,
  },
]

const phanBien = await parallel(HAI_HUONG.map(h => () =>
  agent(`${BOICANH}

${h.prompt}

=== BA LƯỢT GỘP ===
${JSON.stringify(tomTatGop, null, 1)}

=== KIỂM BẰNG CODE (agent nào bỏ sót/gán trùng case thì con số của nó không đáng tin) ===
${JSON.stringify(kiemGop, null, 1)}

=== DANH SÁCH NGUYÊN NHÂN THÔ ===
${danhSachTho}

Trả về: kết luận, bằng chứng CỤ THỂ kèm case key, con số bạn cho là đúng, và độ tin cậy.
Nếu bạn không tìm được chỗ sai nào theo hướng của mình thì NÓI THẲNG là không tìm được —
đó là thông tin có giá trị, còn bịa ra một chỗ sai thì làm loãng cả báo cáo.`,
    { label: `phản biện:${h.key}`, phase: 'Phản biện', schema: PHAN_BIEN_SCHEMA })
    .then(r => ({ huong: h.key, ...(r || {}) }))
))

// ------------------------------------------------------------ Phase 4: TỔNG HỢP
phase('Tổng hợp')

const buocTop = Object.entries(buocDem).sort((a, b) => b[1] - a[1]).slice(0, 20)

const ketLuan = await agent(`${BOICANH}

NHIỆM VỤ: viết kết luận cho \`R-K4\`, cho chủ dự án đọc và ra quyết định kiến trúc ngay
sau đó. Họ đã yêu cầu rõ: "phản biện TRƯỚC khi đề xuất, không chỉ đồng ý", và "ngôn ngữ
dễ hiểu, tránh thuật ngữ không cần thiết". Viết tiếng Việt.

=== PHÂN BỐ MỨC CHẮC CHẮN (${tatCa.length} case) ===
${JSON.stringify(theoMuc, null, 1)}

=== BA LƯỢT GỘP ĐỘC LẬP ===
${JSON.stringify(tomTatGop, null, 1)}

=== KIỂM BẰNG CODE ===
${JSON.stringify(kiemGop, null, 1)}

=== HAI PHẢN BIỆN ===
${JSON.stringify(phanBien.filter(Boolean), null, 1)}

=== BƯỚC XỬ LÝ ĐẾM ĐƯỢC (đầu vào cho con số "x/N case đã làm bước này" của S8) ===
${JSON.stringify(buocTop, null, 1)}

Kết luận phải có, theo đúng thứ tự:

1. MỘT CÂU Ở DÒNG ĐẦU: tập nguyên nhân của chủ đề hoá đơn là HỮU HẠN NHỎ hay MỞ? Nếu dữ
   liệu chưa đủ để nói thì nói thẳng "chưa đủ" — đó là kết luận hợp lệ và đã từng là kết
   luận đúng ở vòng trước.

2. CON SỐ, kèm khoảng: ba hướng gộp ra bao nhiêu, chúng cách nhau bao xa, và khoảng cách
   đó nói lên điều gì. Ba con số GẦN nhau nghĩa là tập nguyên nhân có cấu trúc thật; ba
   con số XA nhau nghĩa là con số phụ thuộc tiêu chí gộp chứ không phụ thuộc dữ liệu —
   và khi đó không được chốt kiến trúc dựa vào nó.

3. TỈ LỆ KHÔNG XÁC ĐỊNH ĐƯỢC và ý nghĩa. Nếu phần lớn case đã đóng không ghi nguyên nhân
   thì điều đó nói về NGUỒN DỮ LIỆU, không về tập nguyên nhân — và nó là phát hiện quan
   trọng hơn cả con số, vì nó bảo rằng đếm thêm case cùng loại sẽ không giúp gì.

4. BÃO HOÀ: nhóm mới có ngừng xuất hiện không? Nếu chưa thì con số là cận DƯỚI.

5. TRẢ LỜI DỨT KHOÁT CHO QUYẾT ĐỊNH KIẾN TRÚC: có cần vector DB / RAG hay không, hay
   Postgres full-text search là đủ, hay chưa quyết được. Nói rõ điều gì sẽ làm bạn đổi ý.

6. ĐIỀU BẤT NGỜ NHẤT — một cái, chọn cái nặng nhất. Nếu ba lượt gộp hoặc hai phản biện
   mâu thuẫn nhau ở đâu, nói ra ở đây thay vì hoà giải.

7. TỐI ĐA 2 CÂU HỎI cần chủ dự án quyết, mỗi câu kèm khuyến nghị và lý do. Chỉ đưa vào
   câu mà câu trả lời THAY ĐỔI việc phải làm tiếp.

Không tô hồng. Nếu phép đếm này thất bại thì nói là thất bại và nói vì sao — một phép
đếm thất bại có ích, còn một con số bịa thì dẫn tới quyết định kiến trúc sai.`,
  { label: 'tổng hợp', phase: 'Tổng hợp' })

return {
  soCaseRutDuoc: tatCa.length,
  phanBoMucChacChan: theoMuc,
  baLuotGop: kiemGop,
  phanBien: phanBien.filter(Boolean),
  buocXuLyDemDuoc: buocTop,
  ketLuan,
}
