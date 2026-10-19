// Kiểm `tinhSoDo` + `dongThoiGian` trên dữ liệu THẬT của app giả lập — không thêm thư viện test nào (§10.1):
//
//   npm run kiem:so-do              (cần app giả lập 5120 + ui_server 8791 đang chạy)
//
// Hai loại phép kiểm:
//   · BẤT BIẾN — đúng với MỌI case, chạy trên cả lô: A/B/C luôn qua, không định tuyến thì D không qua, đường lùi
//     C ⇢ G chỉ sáng khi không có quy trình, dòng thời gian xếp đúng thứ tự.
//   · KỲ VỌNG — trên lô 20261019-045231 đã đi hết vòng ngày 2026-10-19 (IM-50): biết chính xác ô nào phải sáng.
//     Không có lô đó (máy khác) thì phần này BỎ QUA và nói ra, không báo xanh giả.

import { dongThoiGian, tinhSoDo } from '../src/soDo.ts'
import type { CaseTrongDanhSach, DongChayCase } from '../src/kieu.ts'

const GOC = process.env.KP_CONSOLE_GOC ?? 'http://127.0.0.1:8791/app/5120'
const LO = 'gialap:20261019-045231-'

let sai = 0
let chay = 0
function kiem(nhan: string, that: unknown, mong: unknown) {
  chay++
  const ok = JSON.stringify(that) === JSON.stringify(mong)
  if (!ok) sai++
  console.log(`  ${ok ? 'ok ' : 'SAI'}  ${nhan}${ok ? '' : `: nhận ${JSON.stringify(that)}, mong ${JSON.stringify(mong)}`}`)
}

async function lay<T>(duong: string): Promise<T> {
  const r = await fetch(GOC + duong)
  if (!r.ok) throw new Error(`${duong} → HTTP ${r.status}`)
  return (await r.json()) as T
}

const ds = await lay<CaseTrongDanhSach[]>('/internal/cases?tienTo=gialap:&soLuong=200')
console.log(`\n── Bất biến trên ${ds.length} case giả lập`)

const theoKhoa = new Map<string, DongChayCase>()
const saiTruoc = sai
for (const c of ds) {
  const dc = await lay<DongChayCase>(`/internal/cases/${encodeURIComponent(c.khoaNguon)}/dong-chay`)
  theoKhoa.set(c.khoaNguon, dc)
  const { o, canh } = tinhSoDo(dc)
  const t = Object.fromEntries(o.map((x) => [x.ma, x.trangThai]))
  const ten = c.khoaNguon.split(':').pop()

  if (t.A !== 'qua' || t.B !== 'qua' || t.C !== 'qua') kiem(`${ten}: A/B/C luôn qua`, [t.A, t.B, t.C], ['qua', 'qua', 'qua'])
  if (!dc.dinhTuyen.nhom && t.D === 'qua') kiem(`${ten}: không định tuyến thì D KHÔNG qua`, t.D, 'không qua')
  const lui = canh.find((x) => x.tu === 'C' && x.den === 'G')!
  if (lui.daDi !== (t.G === 'qua' && t.D !== 'qua')) kiem(`${ten}: đường lùi sáng ⟺ G qua mà D không`, lui.daDi, !lui.daDi)
  if (dc.quyTrinh.tienDo.coQuyTrinh && lui.daDi) kiem(`${ten}: có quy trình thì đường lùi tắt`, lui.daDi, false)
  const moc = dongThoiGian(dc).map((m) => Date.parse(m.luc))
  if (moc.some((x, i) => i > 0 && x < moc[i - 1]!)) kiem(`${ten}: dòng thời gian tăng dần`, false, true)
  if (o.length !== 11) kiem(`${ten}: đủ 11 ô`, o.length, 11)
  // Không có nhóm thì không có gì để tra — G/H không được sáng (bắt được trên bản đồ lô 2026-10-19).
  if (t['ĐT'] !== 'qua' && !dc.goiY.some((g) => g.nhom) && (t.G === 'qua' || t.H === 'qua'))
    kiem(`${ten}: không có nhóm thì G/H KHÔNG qua`, [t.G, t.H], ['chua', 'chua'])
  // I chỉ xanh khi đã GIAO một gợi ý biết trả lời — giao câu KHÔNG BIẾT không phải gợi ý.
  if (t.I === 'qua' && !dc.goiY.some((g) => g.bietTraLoi)) kiem(`${ten}: I qua ⟹ có lượt biết trả lời`, t.I, 'không qua')
}
// ⚠ Dòng tổng phải ĐẾM dòng sai ở trên, không được in "ok" cố định — bản đầu làm đúng thế, và nó in "ok" ngay dưới
//   bảy dòng SAI lúc thử đột biến. Một dòng tổng nói xanh khi bên trên đỏ là thứ người ta đọc thay cho bên trên.
kiem(`${ds.length} case — mọi bất biến (chi tiết chỉ in dòng SAI)`, sai - saiTruoc, 0)

console.log(`\n── Kỳ vọng trên lô ${LO}… (đã đi hết vòng, IM-50)`)
const mot = theoKhoa.get(LO + '001')
if (!mot) {
  console.log('  ⚠ BỎ QUA — không có lô đó trong kp_gialap (máy khác / DB dựng lại). Bất biến ở trên vẫn đã chạy.')
} else {
  const t = (k: string) => Object.fromEntries(tinhSoDo(theoKhoa.get(LO + k)!).o.map((x) => [x.ma, x.trangThai]))
  const o1 = tinhSoDo(mot)

  kiem('001: đi hết A → J', Object.values(t('001')), Array(11).fill('qua'))
  kiem('001: bước hiện tại K2 (đã xác nhận K1)', mot.quyTrinh.tienDo.buocHienTai?.ma, 'K2')
  kiem('001: ô J nói ra lời khai USED', o1.o.find((x) => x.ma === 'J')!.chiTiet.includes('USED'), true)
  kiem('001: F → G sáng, đường lùi tắt',
    [o1.canh.find((x) => x.tu === 'F')!.daDi, o1.canh.find((x) => x.tu === 'C' && x.den === 'G')!.daDi], [true, false])
  kiem('001: dòng thời gian có lời xác nhận K1 và nấc USED',
    [dongThoiGian(mot).some((m) => m.loai === 'xac-nhan'), dongThoiGian(mot).some((m) => m.tieuDe.startsWith('USED'))], [true, true])

  kiem('006 (tên nhóm bị từ chối): dừng ở ĐT', t('006')['ĐT'], 'dung')
  kiem('006: D chưa tới', t('006').D, 'chua')
  kiem('011 (KHONG-BIET do người nạp): dừng ở ĐT, nguồn người nạp',
    [t('011')['ĐT'], theoKhoa.get(LO + '011')!.dinhTuyen.nguon.startsWith('nguoi-nap:')], ['dung', true])
  kiem('004 (định tuyến được, nhóm CHƯA có quy trình): dừng ở D', t('004').D, 'dung')
  kiem('004: G qua bằng đường lùi, H dừng (chưa có SOP duyệt), I dừng (chỉ giao KHÔNG BIẾT)',
    [t('004').G, t('004').H, t('004').I], ['qua', 'dung', 'dung'])
  kiem('011: không có nhóm → G/H/I chưa tới, J vẫn ghi lại lượt hỏi',
    [t('011').G, t('011').H, t('011').I, t('011').J], ['chua', 'chua', 'dung', 'qua'])
}

console.log(`\n${chay} phép kiểm · ${sai} sai`)
process.exit(sai ? 1 : 0)
