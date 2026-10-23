// Kiểm phép tính THUẦN của phiên trải nghiệm (src/phien.ts) + cách đọc lịch sử phát biểu (src/doc.ts) — không cần app:
//
//   npm run kiem:phien
//
// Rủi ro được kiểm (đề bài §9): thống kê đúng MẪU SỐ, không đánh đồng "chạy được" với "đúng", "chưa đánh giá" không thành
// 0%, lỗi vận hành tách khỏi "không biết", lịch sử sửa giữ BẢN GỐC, so sánh nói ra khi đầu vào khác, Markdown khớp số liệu.

import { caseTuKhoaMau, chuoiLichSu, laHoTro, nhomTheoLoai } from '../src/doc.ts'
import {
  canXemLai, danhGiaTrong, khacBiet, ketCucGiaLap, thongKe, tiLe, xuatMarkdown,
  type DanhGia, type Luot, type PhienTraiNghiem,
} from '../src/phien.ts'
import type { DongCham } from '../src/apiGiaLap.ts'
import type { AssertionCoLichSu } from '../src/kieu.ts'

let sai = 0
let chay = 0
function kiem(nhan: string, that: unknown, mong: unknown) {
  chay++
  const ok = JSON.stringify(that) === JSON.stringify(mong)
  if (!ok) sai++
  console.log(`  ${ok ? 'ok ' : 'SAI'}  ${nhan}${ok ? '' : `: nhận ${JSON.stringify(that)}, mong ${JSON.stringify(mong)}`}`)
}

let dem = 0
function dg(s: Partial<DanhGia> = {}): DanhGia {
  return { ...danhGiaTrong(), ...s }
}
function luot(s: Partial<Luot>): Luot {
  dem++
  return {
    id: `luot-${String(dem).padStart(4, '0')}`, loai: 'hoi-dap', batDau: '2026-10-23T01:00:00Z', ketThuc: '2026-10-23T01:00:05Z',
    thoiGianChoMs: 5000, nguoiThaoTac: 'kiem', nguon: { loai: 'case', khoa: 'gialap:x-001', ten: 'case 1' }, cauHoi: 'hỏi gì?',
    cheDo: 'file', nhaCungCap: null, model: null, ketCuc: 'tra-loi', loi: null, lienKet: {}, kichBan: null, danhGia: dg(), ...s,
  }
}
function dongGL(s: Partial<DongCham>): DongCham {
  return {
    khoa: 'gialap:x-001', tieuDe: 't', nhomDung: 'A', nhom: 'A', nguon: 'nguoi-nap:kiem', giaiThich: null, ketQua: 'dung',
    http: 200, loiVanHanh: null, bietTraLoi: true, lyDoKhongBiet: null, soHuongDan: 1, huongDan: [], buocHienTai: 'K1',
    noiDungBuoc: 'b1', daXongHet: false, lyDoKhongCoQuyTrinh: null, maLuotGoiY: null, ...s,
  }
}

console.log('\n── Phiên RỖNG và phiên CHƯA ĐÁNH GIÁ: không bao giờ ra 0% như một kết luận')
{
  const tk = thongKe([])
  kiem('phiên rỗng: tổng 0, mẫu số mọi chiều 0', [tk.tong, ...tk.phanBo.map((p) => p.mauSo)], [0, 0, 0, 0])
  kiem('tiLe(0, 0) = "chưa có đánh giá", không phải 0%', tiLe(0, 0), 'chưa có đánh giá')
  const chua = thongKe([luot({}), luot({ ketCuc: 'khong-biet' })])
  kiem('hai lượt chưa đánh giá → đã 0 / chưa 2', [chua.daDanhGia, chua.chuaDanhGia], [0, 2])
  kiem('… và mẫu số độ đúng = 0 (không phải 2)', chua.phanBo[0]!.mauSo, 0)
  const md = xuatMarkdown({ ...phienMau([luot({})]) }, 'x')
  kiem('Markdown của phiên chưa đánh giá nói "chưa có đánh giá"', md.includes('mẫu số 0 (chưa có đánh giá)'), true)
}

function phienMau(ds: Luot[]): PhienTraiNghiem {
  return {
    dinhDang: 'kp-phien-trai-nghiem/1', id: 'phien-kiem-0001', ten: 'Phiên kiểm', kichBan: null, nguoiThaoTac: 'kiem', ghiChu: '',
    taoLuc: '2026-10-23T01:00:00Z', capNhatLuc: '2026-10-23T02:00:00Z', luot: ds,
    nguCanh: { khoa: 'cong-5120.t', cong: 5120, app: 'App giả lập', moTa: '', tenantId: 't', cheDoTenant: 'DedicatedSingleTenant', moiTruong: 'gia-lap' },
  }
}

console.log('\n── Phiên TRỘN: đúng, sai, không biết, lỗi vận hành, đang chờ, chưa đánh giá, bản nháp')
{
  const ds = [
    luot({ ketCuc: 'tra-loi', danhGia: dg({ doDung: 'dung', danChung: 'du', thucHienDuoc: 'lam-theo-duoc' }) }),
    luot({ ketCuc: 'tra-loi', danhGia: dg({ doDung: 'sai', danChung: 'khong-ho-tro' }) }),
    // không biết + đánh giá việc từ chối là ĐÚNG — kịch bản 5
    luot({ ketCuc: 'khong-biet', danhGia: dg({ doDung: 'dung', danChung: 'khong-ap-dung' }) }),
    luot({ ketCuc: 'khong-biet' }),
    luot({ ketCuc: 'loi-van-hanh', loi: { http: 502, lyDo: null, text: 'app chết' } }),
    luot({ ketCuc: 'dang-cho', ketThuc: null }),
    luot({ loai: 'ban-nhap', ketCuc: 'thao-tac', banNhap: { tri: ['a'], qt: 'q', nhom: 'A', thaoTac: [
      { luc: 'x', viec: 'sua-tri-thuc', doiTuong: 'a', ten: 'A', ok: true, http: 200, trangThaiSau: 'DRAFT', loi: null },
      { luc: 'x', viec: 'duyet-tri-thuc', doiTuong: 'a', ten: 'A', ok: true, http: 200, trangThaiSau: 'ACTIVE', loi: null },
      { luc: 'x', viec: 'duyet-quy-trinh', doiTuong: 'q', ten: 'Q v1', ok: false, http: 409, trangThaiSau: null, loi: 'chặn' },
    ] }, danhGia: dg({ nhanXet: 'SOP ổn' }) }),
  ]
  const tk = thongKe(ds)
  kiem('tổng 7 · lượt hỏi 6', [tk.tong, tk.luotHoi], [7, 6])
  kiem('kết cục: 2 trả lời · 2 không biết · 1 lỗi · 1 đang chờ · 1 thao tác',
    [tk.ketCuc['tra-loi'], tk.ketCuc['khong-biet'], tk.ketCuc['loi-van-hanh'], tk.ketCuc['dang-cho'], tk.ketCuc['thao-tac']], [2, 2, 1, 1, 1])
  kiem('"chạy được" = trả lời + không biết = 4 (lỗi và đang chờ KHÔNG tính)', tk.thanhCongVanHanh, 4)
  kiem('đã đánh giá 4 (kể cả lượt bản nháp chỉ có nhận xét) · chưa 3', [tk.daDanhGia, tk.chuaDanhGia], [4, 3])
  const doDung = tk.phanBo[0]!
  kiem('độ đúng: mẫu số 3 = số lượt ĐÃ chấm chiều này, không phải 7', doDung.mauSo, 3)
  kiem('độ đúng: đúng 2 (gồm từ chối đúng) · sai 1', [doDung.dem.find((d) => d.gia === 'dung')!.so, doDung.dem.find((d) => d.gia === 'sai')!.so], [2, 1])
  kiem('tỉ lệ in kèm mẫu số', tiLe(2, 3), '2/3 (67%)')
  kiem('cần mở lại: đúng lượt sai + dẫn chứng không hỗ trợ', tk.canXemLai.map((x) => x.lyDo), [['nội dung sai', 'dẫn chứng không hỗ trợ']])
  kiem('bản nháp: 1 sửa · 1 duyệt được · 1 duyệt hỏng — không gộp thành "thành công"',
    [tk.banNhap[0]!.soSua, tk.banNhap[0]!.duyetOk, tk.banNhap[0]!.duyetHong, tk.banNhap[0]!.doiTuongDaDuyet], [1, 1, 1, ['A']])
  kiem('không có lượt giả lập → chấm định tuyến mẫu số 0', tk.dinhTuyen.mauSo, 0)

  const md = xuatMarkdown(phienMau(ds), '2026-10-23T03:00:00Z')
  kiem('Markdown: tổng lượt 7', md.includes('| Tổng lượt | 7 |'), true)
  kiem('Markdown: lỗi vận hành 1', md.includes('| — lỗi vận hành | 1 |'), true)
  kiem('Markdown: tỉ lệ độ đúng có mẫu số', md.includes('- Đúng: 2/3 (67%)'), true)
  kiem('Markdown: đủ 7 mục lượt', (md.match(/^### \d+\./gm) ?? []).length, 7)
  kiem('Markdown: token/chi phí nói "chưa có dữ liệu"', md.includes('token/chi phí: chưa có dữ liệu'), true)
}

console.log('\n── Giả lập: lỗi vận hành ≠ không biết; chấm định tuyến tách riêng')
{
  kiem('HTTP 500 → lỗi vận hành', ketCucGiaLap(dongGL({ http: 500, loiVanHanh: 'x', bietTraLoi: null })), 'loi-van-hanh')
  kiem('200 + bietTraLoi false → không biết', ketCucGiaLap(dongGL({ bietTraLoi: false })), 'khong-biet')
  kiem('200 + bietTraLoi true → trả lời', ketCucGiaLap(dongGL({})), 'tra-loi')
  const ds = [
    luot({ loai: 'hoi-gia-lap', giaLap: dongGL({ ketQua: 'dung' }) }),
    luot({ loai: 'hoi-gia-lap', giaLap: dongGL({ ketQua: 'sai' }), ketCuc: 'khong-biet' }),
    luot({ loai: 'hoi-gia-lap', giaLap: dongGL({ ketQua: 'khong-biet', http: 500 }), ketCuc: 'loi-van-hanh' }),
  ]
  const tk = thongKe(ds)
  kiem('chấm định tuyến bỏ lượt lỗi vận hành khỏi mẫu số', tk.dinhTuyen, { dung: 1, sai: 1, khongBiet: 0, mauSo: 2 })
  kiem('chấm định tuyến KHÔNG đổ vào đánh giá độ đúng', tk.phanBo[0]!.mauSo, 0)
}

console.log('\n── So sánh: nói ra khi đầu vào khác')
{
  const a = luot({ cauHoi: 'A?', cheDo: 'file' })
  const b = luot({ cauHoi: 'B?', cheDo: 'api', nhaCungCap: 'openai', model: 'm' })
  kiem('khác câu hỏi, chế độ, model', khacBiet(a, b).map((x) => x.truong), ['Câu hỏi', 'Chế độ', 'Nhà cung cấp / model'])
  kiem('cùng đầu vào → không khác gì', khacBiet(a, { ...a, id: 'khac' }), [])
}

console.log('\n── Lịch sử phát biểu: giữ BẢN GỐC qua nhiều lần sửa')
{
  const A = (id: string, conHieuLuc: boolean, biThayBoi: string | null, noiDung: string): AssertionCoLichSu => ({
    id, kind: 'Handling', noiDung, mucKiemChung: 'Speculative', nguonGoc: 'AiInference', nguoiGhi: null, conHieuLuc, biThayBoi,
    lucTao: '2026-10-23T00:00:00Z', danChung: [],
  })
  const ds = [A('a', false, 'b', 'gốc'), A('x', true, null, 'khác'), A('b', false, 'c', 'sửa 1'), A('c', true, null, 'sửa 2')]
  kiem('chuỗi của bản hiện tại: gốc → sửa 1 → sửa 2', chuoiLichSu(ds, ds[3]!).map((x) => x.noiDung), ['gốc', 'sửa 1', 'sửa 2'])
  kiem('phát biểu chưa sửa: chuỗi một phần tử', chuoiLichSu(ds, ds[1]!).map((x) => x.id), ['x'])
  kiem('vòng lặp dữ liệu hỏng không treo', chuoiLichSu([A('p', true, 'q', '1'), A('q', false, 'p', '2')], A('p', true, 'q', '1')).length <= 2, true)
  kiem('nhóm theo loại: loại lạ không bị giấu', nhomTheoLoai([{ kind: 'Handling' }, { kind: 'LoaiMoi' }]).map((g) => g.kind), ['Handling', 'LoaiMoi'])
}

console.log('\n── Dẫn chứng')
{
  kiem('Support chưa rút = hỗ trợ', laHoTro({ quanHe: 'Support' }), true)
  kiem('Support đã rút ≠ hỗ trợ', laHoTro({ quanHe: 'Support', daRutLai: true }), false)
  kiem('ContextFor ≠ hỗ trợ', laHoTro({ quanHe: 'ContextFor' }), false)
  kiem('case từ khoá mẩu jira', caseTuKhoaMau('jira:ES-1#comment-7'), 'jira:ES-1')
  kiem('khoá không có # → không đoán', caseTuKhoaMau('crm:deal/4471'), null)
}

console.log('\n── canXemLai: "không biết" chưa đánh giá KHÔNG bị coi là sai')
kiem('lượt không biết chưa chấm → không cần mở lại', canXemLai(luot({ ketCuc: 'khong-biet' })), [])

console.log(`\n${sai ? `✗ ${sai}/${chay} phép kiểm SAI` : `✓ ${chay}/${chay} phép kiểm đúng`}`)
process.exit(sai ? 1 : 0)
