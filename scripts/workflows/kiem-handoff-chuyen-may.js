export const meta = {
  name: 'kiem-handoff-chuyen-may',
  description: 'Kiểm tài liệu bàn giao: người mới trên máy trắng có dựng lại và làm tiếp được không, thiếu gì',
  phases: [
    { title: 'Thử', detail: '7 góc độc lập, mỗi góc một câu hỏi cụ thể' },
    { title: 'Tổng hợp', detail: 'gộp thành danh sách phải sửa' },
  ],
}

const REPO = 'D:/MiniProject/23.ai-operational-platform'

const BOICANH = `
BỐI CẢNH.

Bạn là người **mới hoàn toàn** ngồi trước một **máy trắng** vừa \`git clone\` repo
\`${REPO}\` về. Bạn chưa từng thấy dự án này. Bạn KHÔNG có:
  · corpus Jira (dry-run-*.json, spread-*.json, fixture-*.json — đều trong .gitignore)
  · jira-config.bat (chứa PAT, trong .gitignore)
  · database kp_dev
  · bộ nhớ của agent phiên trước (nằm ngoài repo)

Người chủ dự án vừa làm hai ngày rất dày trên máy cũ và sắp chuyển máy. Họ đã viết
\`docs/10_CHUYEN_MAY.md\` để bàn giao. **Việc của bạn là tìm chỗ bàn giao đó THIẾU** —
không phải xác nhận nó ổn.

⚠ ĐỌC REPO THẬT. Mở file, chạy grep, đối chiếu. Đừng suy đoán từ tên file. Một phát
hiện không trích dẫn được đường dẫn + dòng thì không tính.

⚠ THÁI ĐỘ: dự án này đã phải **dọn 9 lần mâu thuẫn giữa các tài liệu** nói về cùng một
thứ, và bệnh "từ vựng song song" tái phát 3 lần trong một workstream. Chủ dự án đã ghi
rõ yêu cầu: *"phản biện TRƯỚC khi đề xuất, không chỉ đồng ý"*. Một báo cáo nói "mọi thứ
đều ổn" là một báo cáo vô dụng ở đây. Nếu bạn thật sự không tìm được lỗ nào trong góc
của mình thì nói thẳng là không tìm được — đừng bịa ra một lỗ để có cái mà nộp.
`

const PHAT_HIEN = {
  type: 'object',
  required: ['tomTat', 'phatHien'],
  properties: {
    tomTat: { type: 'string', description: '2-4 câu: góc này có làm tiếp được không, kẹt ở đâu' },
    lamDuocKhong: { type: 'string', enum: ['duoc', 'duoc-nhung-vap', 'khong-duoc'] },
    phatHien: {
      type: 'array',
      items: {
        type: 'object',
        required: ['mucDo', 'thieuGi', 'oDau', 'haiThatLaGi', 'suaThenao'],
        properties: {
          mucDo: { type: 'string', enum: ['chan', 'lam-mat-thoi-gian', 'nho'] },
          thieuGi: { type: 'string', description: 'thiếu đúng cái gì' },
          oDau: { type: 'string', description: 'file:dòng hoặc tên file cần sửa' },
          haiThatLaGi: { type: 'string', description: 'người mới sẽ vấp thế nào, CỤ THỂ' },
          suaThenao: { type: 'string', description: 'câu/đoạn nên thêm, viết sẵn được thì viết' },
        },
      },
    },
  },
}

const GOC = [
  {
    key: 'dung-moi-truong',
    prompt: `GÓC 1 — DỰNG MÔI TRƯỜNG.

Làm theo đúng \`docs/10_CHUYEN_MAY.md\` §2, từng lệnh một, **và kiểm từng lệnh có chạy
được không** bằng cách đối chiếu với repo thật:
  · file \`scripts/dev-db-setup.sql\` có tồn tại không? nó tạo những gì?
  · \`scripts/dev-seed-tenant.sql\` có tồn tại không?
  · đường dẫn solution trong lệnh có đúng không?
  · chuỗi kết nối trong lệnh \`dotnet ef\` có khớp với \`appsettings.Development.json\` không?
  · phiên bản .NET nào? repo có nói không? máy mới cài sai bản thì sao?
  · có bước nào BẮT BUỘC mà §2 quên không? (gợi ý: kiểm \`Program.cs\` và
    \`StartupChecks.cs\` xem app TỪ CHỐI KHỞI ĐỘNG khi thiếu gì)

Nói rõ: chạy hết §2 xong thì app chạy được chưa, hay còn thiếu bước?`,
  },
  {
    key: 'lay-lai-corpus',
    prompt: `GÓC 2 — LẤY LẠI CORPUS JIRA.

Làm theo \`docs/10_CHUYEN_MAY.md\` §3. Kiểm:
  · \`jira-config.example.bat\` có đủ biến mà các script cần không? Đối chiếu với
    \`export_jira_to_channel1.py\`, \`sample_spread.py\`, \`discover_fields.py\`,
    \`load_fixture.py\` — script nào đọc biến môi trường nào?
  · JQL mẫu trong example có chạy được ngay không, hay phải sửa gì trước?
  · \`sample_spread.py\` có biến \`SAMPLE_UNTIL\` mặc định là ngày cứng — người dùng
    tương lai có biết phải đổi không? Điều gì xảy ra nếu họ không đổi?
  · Thứ tự script: phải chạy cái nào trước cái nào? §3 có nói đủ không?
  · \`make_fixture.py\` có danh sách credential CỨNG của corpus cũ — chạy nó trên corpus
    MỚI thì sao? Có ai cảnh báo không?`,
  },
  {
    key: 'hieu-trang-thai',
    prompt: `GÓC 3 — HIỂU DỰ ÁN ĐANG Ở ĐÂU.

Đọc \`docs/00_CURRENT_STATE.md\` như người mới. Trả lời được không:
  · dự án đang làm gì, giai đoạn nào, slice nào?
  · cái gì ĐÃ chạy được, cái gì mới chỉ có hình dạng dữ liệu?
  · bao nhiêu test, chạy bằng lệnh gì?
  · quyết định nào vừa chốt trong hai ngày qua, và ai chốt?

⚠ Chú ý đặc biệt: \`00_CURRENT_STATE.md\` rất dài (>2000 dòng) và được viết bằng cách
CHÈN THÊM lên đầu qua nhiều phiên. Hãy tìm chỗ **phần đầu mâu thuẫn với phần sau** —
ví dụ số test, ngày tháng, trạng thái một quyết định. Trích dẫn số dòng cả hai chỗ.`,
  },
  {
    key: 'mau-thuan-tai-lieu',
    prompt: `GÓC 4 — MÂU THUẪN GIỮA CÁC TÀI LIỆU.

Đây là góc quan trọng nhất, vì dự án đã dọn 9 lần mâu thuẫn và nó vẫn tái phát.

Đối chiếu chéo \`README.md\`, \`docs/00_CURRENT_STATE.md\`, \`docs/07_MVP_IMPLEMENTATION.md\`,
\`docs/09_RK4_DEM_NGUYEN_NHAN.md\`, \`docs/10_CHUYEN_MAY.md\` về các con số và trạng thái:
  · số test (grep "test" kèm số)
  · số case / số evidence của các corpus
  · trạng thái các câu hỏi mở: AR-e, AR-g, AR-h, AR-i, AR-j, AR-k, AR-l, R-K4
  · kết luận về vector DB — có chỗ nào CÒN nói nó là "quyết định kiến trúc" không?
    (hôm nay vừa hạ cấp thành "phép đo trên một nguồn" — kiểm xem đã sửa HẾT chưa)
  · con số nguyên nhân: 5-10 (cũ) vs 19 (mới) — chỗ nào còn ghi con số cũ mà không
    đánh dấu là đã bị bác?

Mỗi mâu thuẫn: trích file:dòng CẢ HAI chỗ.`,
  },
  {
    key: 'lam-tiep-viec-gi',
    prompt: `GÓC 5 — LÀM TIẾP VIỆC GÌ.

Người mới đã dựng xong máy, đã đọc tài liệu. Giờ họ hỏi: "tôi bắt tay vào làm gì?"

Kiểm xem tài liệu trả lời được không:
  · việc kế tiếp là gì, và tại sao là nó chứ không phải việc khác?
  · việc nào đang chờ NGƯỜI DÙNG quyết, việc nào làm được ngay?
  · nếu bắt tay vào "việc làm được ngay" mà \`10\` §6 nêu (dựng cây quyết định có bước
    kiểm cho nhóm SOP lớn nhất) — họ có đủ nguyên liệu không? File nào chứa nguyên
    liệu đó? Kiểm \`docs/ket-qua-phan-tich/\` xem có thật không và có đọc được không.
  · \`ES-346396\` được nhắc như bằng chứng then chốt — người mới có xem được nội dung
    case đó không, hay nó chỉ còn là một mã số không tra được? Đây là câu quan trọng:
    nếu bằng chứng không tra lại được thì lập luận đứng trên nó có còn kiểm chứng được?`,
  },
  {
    key: 'thu-da-mat',
    prompt: `GÓC 6 — THỨ ĐÃ TÌM RA MÀ KHÔNG ĐƯỢC GHI XUỐNG.

Đây là góc tìm cái KHÔNG có. Khó nhất, và đúng thứ chủ dự án lo nhất — họ đã từng mất
cả một tài liệu cùng toàn bộ Success Metrics mà không ai phát hiện.

Cách làm: đọc \`git log --stat\` của 15 commit gần nhất và \`git show --stat\` vài commit,
xem hai ngày qua đã làm gì. Rồi với mỗi phát hiện/quyết định/số đo trong các commit
message, kiểm xem nó có được ghi vào TÀI LIỆU không, hay chỉ sống trong commit message.

⚠ Commit message KHÔNG phải tài liệu: nó không hiện ra khi ai đó đọc \`docs/\`, và người
mới sẽ không đọc 15 commit message.

Liệt kê cụ thể: số đo nào, phát hiện nào, quyết định nào chỉ có trong commit message
hoặc chỉ có trong code comment mà không có trong docs.`,
  },
  {
    key: 'script-co-chay-duoc',
    prompt: `GÓC 7 — SCRIPT CÓ CHẠY ĐƯỢC TRÊN MÁY TRẮNG KHÔNG.

Đọc từng file trong \`scripts/jira-export/\` và kiểm phụ thuộc ẩn:
  · script nào cần thư viện ngoài stdlib? Repo có ghi phải cài gì không? (gợi ý:
    kiểm \`thu_retrieval.py\`) Có \`requirements.txt\` không?
  · script nào đọc file mà file đó trong .gitignore (tức máy trắng không có)? Nó báo
    lỗi rõ ràng hay chết với traceback khó hiểu?
  · script nào có đường dẫn CỨNG tới máy cũ? grep \`D:\\\\MiniProject\` và \`C:\\\\Users\`
    trong toàn bộ \`scripts/\`.
  · \`thu_retrieval.py\` đọc taxonomy từ đâu? Đường dẫn mặc định có đúng với chỗ file
    thật đang nằm (\`docs/ket-qua-phan-tich/\`) không?
  · Python phiên bản nào? Có chỗ nào dùng cú pháp chỉ có ở bản mới không?`,
  },
]

log('7 góc độc lập, mỗi góc đóng vai người mới trên máy trắng')

phase('Thử')

const ketQua = await parallel(GOC.map(g => () =>
  agent(`${BOICANH}\n\n${g.prompt}`, { label: `thử:${g.key}`, phase: 'Thử', schema: PHAT_HIEN })
    .then(r => ({ goc: g.key, ...(r || { tomTat: 'không chạy được', phatHien: [] }) }))
))

const sach = ketQua.filter(Boolean)
const tatCa = sach.flatMap(r => (r.phatHien || []).map(p => ({ goc: r.goc, ...p })))
const chan = tatCa.filter(p => p.mucDo === 'chan')
log(`${tatCa.length} phát hiện · ${chan.length} mức CHẶN · ` +
    sach.map(r => `${r.goc}=${r.lamDuocKhong}`).join(' '))

phase('Tổng hợp')

const ketLuan = await agent(`${BOICANH}

NHIỆM VỤ: gộp bảy góc thành MỘT danh sách việc phải sửa, xếp theo thứ tự làm.

Người đọc là chủ dự án, sắp tắt máy cũ và sang máy mới. Họ cần biết: **sửa những gì
TRƯỚC KHI tắt máy này**, vì có thứ chỉ máy cũ mới có.

=== BẢY GÓC ===
${JSON.stringify(sach, null, 1)}

Viết tiếng Việt, và theo đúng thứ tự này:

1. MỘT CÂU đầu tiên: bàn giao hiện tại ĐỦ hay CHƯA ĐỦ để làm tiếp trên máy mới?

2. PHẢI SỬA TRƯỚC KHI TẮT MÁY CŨ — chỉ những thứ mà máy mới KHÔNG tự làm được, ví dụ
   thứ cần dữ liệu chỉ máy cũ có. Mỗi mục: sửa gì, ở file nào, và viết sẵn câu cần thêm
   nếu viết được. Nếu không có mục nào thì nói thẳng là không có — đó là tin tốt và
   người đọc cần biết.

3. SỬA ĐƯỢC SAU, TRÊN MÁY MỚI — xếp theo mức độ.

4. MÂU THUẪN GIỮA TÀI LIỆU — liệt kê riêng, vì dự án đã dọn 9 lần và nó vẫn tái phát.
   Mỗi mâu thuẫn kèm file:dòng cả hai chỗ và nói rõ CHỖ NÀO ĐÚNG.

5. THỨ ĐÃ MẤT — phát hiện/số đo nào chỉ sống trong commit message hoặc code comment mà
   không có trong docs. Đây là mục chủ dự án lo nhất.

6. ĐIỀU BẤT NGỜ NHẤT — một cái. Nếu bảy góc mâu thuẫn nhau ở đâu, nói ra ở đây thay vì
   hoà giải.

Không tô hồng. Nếu bàn giao thật sự ổn ở một mục nào thì nói ổn, đừng bịa việc.`,
  { label: 'tổng hợp', phase: 'Tổng hợp' })

return { soPhatHien: tatCa.length, soChan: chan.length, theoGoc: sach.map(r => ({ goc: r.goc, lamDuocKhong: r.lamDuocKhong, tomTat: r.tomTat })), phatHienChan: chan, ketLuan }
