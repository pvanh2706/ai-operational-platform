// Từ DỮ KIỆN của một case (`GET /internal/cases/{k}/dong-chay`) ra trạng thái từng ô của sơ đồ luồng README.
//
// Đây là chỗ DUY NHẤT diễn giải dữ kiện thành "ô nào đã qua" — host cố ý không làm việc này (IM-51 lựa chọn 1).
// Hàm THUẦN, không chạm Vue: kiểm được bằng `npm run kiem:so-do` mà không cần trình duyệt.
//
// ⚠ Thêm một ô so với README: `DT` (định tuyến nhóm). README để nó ẩn trong ô D ("quy trình ACTIVE của nhóm định
//   tuyến"), nhưng đó là bước có AI — và là bước người ta mở console ra để xem nhất.
// ⚠ Ô Kênh 2 (A2, K, S6) KHÔNG có ở đây: chúng không thuộc về một case.

import type { DongChayCase } from './kieu'

export type TrangThaiO = 'qua' | 'dung' | 'chua'

export interface OSoDo {
  ma: string
  ten: string
  trangThai: TrangThaiO
  /** Một dòng: đã qua thì nói qua bằng gì, dừng thì nói vì sao. */
  chiTiet: string
  /** Mục chi tiết trên trang dòng chảy để cuộn tới khi bấm ô. */
  muc: string
}

export interface CanhSoDo {
  tu: string
  den: string
  /** Mũi tên nét đứt của README: đường lùi C ⇢ G và vòng phản hồi J ⇢ G. */
  netDut?: boolean
  nhan?: string
  daDi: boolean
}

export type TangDinhTuyen = 'nguoi-nap' | 'tra-san' | 'model' | 'model-chua-goi' | 'khac'

/** Tầng đã trả lời, đọc từ tiền tố của `nguon` — đúng tiền tố DinhTuyenKho phát ra. */
export function tangDinhTuyen(nguon: string): TangDinhTuyen {
  if (nguon === 'model:chua-goi') return 'model-chua-goi'
  if (nguon.startsWith('nguoi-nap:')) return 'nguoi-nap'
  if (nguon.startsWith('tra-san:')) return 'tra-san'
  if (nguon.startsWith('model:')) return 'model'
  return 'khac'
}

export const TEN_TANG: Record<TangDinhTuyen, string> = {
  'nguoi-nap': 'người nạp',
  'tra-san': 'bảng đo',
  model: 'model',
  'model-chua-goi': 'model chưa hỏi',
  khac: 'nguồn khác',
}

export function tinhSoDo(dc: DongChayCase): { o: OSoDo[]; canh: CanhSoDo[] } {
  const dt = dc.dinhTuyen
  const td = dc.quyTrinh.tienDo
  const goiY = dc.goiY
  const suKien = goiY.flatMap((g) => g.suKien)

  const daBiet = goiY.filter((g) => g.bietTraLoi)
  // ⚠ Một lượt /hoi KHÔNG có nghĩa là đã tra tri thức: không có nhóm thì không có gì để tra. Bản đầu tô G xanh chỉ vì
  //   có lượt hỏi — ticket KHÔNG BIẾT nhóm hiện như đã đi qua G (thấy được trên bản đồ lô, 2026-10-19).
  const coNhom = goiY.filter((g) => g.nhom)
  // Tương tự I: giao đi câu "KHÔNG BIẾT" là một câu trả lời trung thực (E1-1), nhưng không phải một GỢI Ý.
  const daGiao = goiY.filter((g) => g.suKien.some((s) => s.nac === 'SHOWN' && s.datDuoc))
  const daGiaoGoiY = daGiao.filter((g) => g.bietTraLoi)
  const loiKhai = suKien.filter((s) => s.nguoiKhai && (s.nac === 'ACCEPTED' || s.nac === 'USED'))
  const soBuocXong = td.buoc.filter((b) => b.daXong).length

  const o: OSoDo[] = [
    { ma: 'A', ten: 'Kênh 1 — tín hiệu', trangThai: 'qua', muc: 'evidence',
      chiTiet: `${dc.evidence.length} mẩu evidence` },
    { ma: 'B', ten: 'Xác định tenant', trangThai: 'qua', muc: 'dau',
      chiTiet: 'đọc được case ⇒ ranh giới tenant đã khớp' },
    { ma: 'C', ten: 'Tìm hoặc tạo Case', trangThai: 'qua', muc: 'dau',
      chiTiet: dc.guiVe.length ? `xếp ${dc.guiVe.length} lượt gửi kết quả về` : 'chưa xếp lượt gửi về nào' },
    {
      ma: 'ĐT', ten: 'Định tuyến nhóm', muc: 'dinh-tuyen',
      trangThai: dt.nhom ? 'qua' : 'dung',
      chiTiet: dt.nhom ? `${dt.nhom} · ${TEN_TANG[tangDinhTuyen(dt.nguon)]}` : `KHÔNG BIẾT · ${TEN_TANG[tangDinhTuyen(dt.nguon)]}`,
    },
    {
      ma: 'D', ten: 'Khớp quy trình đã duyệt', muc: 'quy-trinh',
      trangThai: td.coQuyTrinh ? 'qua' : dt.nhom ? 'dung' : 'chua',
      chiTiet: td.coQuyTrinh && td.quyTrinh
        ? `${td.quyTrinh.ten} · phiên bản ${td.quyTrinh.phienBan}`
        : td.lyDoKhongCo ?? 'chưa khớp',
    },
    {
      ma: 'E', ten: 'Suy ra đang ở bước nào', muc: 'quy-trinh',
      trangThai: td.coQuyTrinh ? 'qua' : 'chua',
      chiTiet: td.coQuyTrinh ? `${soBuocXong}/${td.buoc.length} bước xong` : '—',
    },
    {
      ma: 'F', ten: 'Bước tiếp theo', muc: 'quy-trinh',
      trangThai: td.coQuyTrinh ? 'qua' : 'chua',
      chiTiet: td.daXongHet ? 'đã xong hết' : td.buocHienTai ? `${td.buocHienTai.ma} · ${td.buocHienTai.noiDung}` : '—',
    },
    {
      ma: 'G', ten: 'Tra tri thức', muc: 'goi-y',
      trangThai: coNhom.length ? 'qua' : 'chua',
      chiTiet: coNhom.length
        ? `${coNhom.length} lượt tra${td.coQuyTrinh ? ' theo bước' : ' theo NHÓM (đường lùi)'}`
        : goiY.length ? `${goiY.length} lượt hỏi, nhưng không có nhóm để tra` : 'chưa ai hỏi case này',
    },
    {
      ma: 'H', ten: 'Tri thức + dẫn chứng', muc: 'goi-y',
      trangThai: daBiet.length ? 'qua' : coNhom.length ? 'dung' : 'chua',
      chiTiet: daBiet.length
        ? `${daBiet.length}/${goiY.length} lượt biết trả lời`
        : coNhom.at(-1)?.lyDoKhongBiet ?? '—',
    },
    {
      ma: 'I', ten: 'Trả gợi ý', muc: 'goi-y',
      trangThai: daGiaoGoiY.length ? 'qua' : daGiao.length ? 'dung' : 'chua',
      chiTiet: daGiaoGoiY.length
        ? `đã giao qua ${[...new Set(daGiaoGoiY.map((g) => g.kenhRa))].join(', ')}`
        : daGiao.length ? 'chỉ giao đi câu KHÔNG BIẾT' : '—',
    },
    {
      ma: 'J', ten: 'Ghi lại — có dùng không', muc: 'goi-y',
      trangThai: goiY.length ? 'qua' : 'chua',
      chiTiet: loiKhai.length ? `${loiKhai.length} lời khai (${loiKhai.map((s) => s.nac).join(', ')})` : goiY.length ? 'chưa ai khai đã dùng' : '—',
    },
  ]

  const tt = Object.fromEntries(o.map((x) => [x.ma, x.trangThai])) as Record<string, TrangThaiO>
  const qua = (m: string) => tt[m] === 'qua'
  const chuoi = ['A', 'B', 'C', 'ĐT', 'D', 'E', 'F', 'G', 'H', 'I', 'J']

  const canh: CanhSoDo[] = chuoi.slice(1).map((den, i) => {
    const tu = chuoi[i]!
    // F → G chỉ "đã đi" khi đi qua quy trình; không có quy trình thì G đến bằng đường lùi C ⇢ G.
    const daDi = tu === 'F' ? qua('F') && qua('G') : qua(tu) && qua(den)
    return { tu, den, daDi }
  })
  canh.push({ tu: 'C', den: 'G', netDut: true, nhan: 'đường lùi — nhóm chưa có quy trình', daDi: qua('G') && !qua('D') })
  canh.push({
    tu: 'J', den: 'G', netDut: true, nhan: 'người khai nhóm đúng',
    daDi: suKien.some((s) => s.nhomDung),
  })

  return { o, canh }
}

// ============================================================================ dòng thời gian gộp

export type LoaiSuKien = 'evidence' | 'xac-nhan' | 'rut-lai' | 'nguoi-nap' | 'model' | 'luot-chay' | 'goi-y' | 'nac' | 'gui-ve'

export interface MocThoiGian {
  luc: string
  loai: LoaiSuKien
  tieuDe: string
  chiTiet?: string
  /** Đỏ: lượt model hỏng, lượt gửi về lỗi, nấc không đạt. */
  xau?: boolean
}

/** Mọi thứ đã xảy ra với case, xếp theo thời gian — thứ mà các mục chia theo loại không cho thấy: THỨ TỰ. */
export function dongThoiGian(dc: DongChayCase): MocThoiGian[] {
  const m: MocThoiGian[] = []

  for (const e of dc.evidence) {
    const luc = e.quanSatLuc ?? e.napLuc
    if (e.xacNhanBuoc) m.push({ luc, loai: 'xac-nhan', tieuDe: `Xác nhận bước ${e.xacNhanBuoc} xong`, chiTiet: e.noiDungRutGon })
    else m.push({ luc, loai: 'evidence', tieuDe: `Evidence ${e.khoaNguon.split('#').pop()}`, chiTiet: e.noiDungRutGon })
    if (e.rutLaiLuc) m.push({ luc: e.rutLaiLuc, loai: 'rut-lai', tieuDe: `Rút lại ${e.khoaNguon}`, xau: true })
  }

  const nn = dc.dinhTuyen.nguoiNap
  if (nn) m.push({ luc: nn.napLuc, loai: 'nguoi-nap', tieuDe: `Người nạp định tuyến: ${nn.nhom}`, chiTiet: `${nn.nguoiNap} · ${nn.nguon}` })

  for (const b of dc.dinhTuyen.bienLaiModel) {
    m.push({
      luc: b.luc, loai: 'model', xau: !b.thanhCong,
      tieuDe: b.thanhCong ? `Model định tuyến: ${b.nhom ?? 'KHÔNG BIẾT'} (tin ${b.doTinCay?.toFixed(2) ?? '?'})` : 'Model định tuyến HỎNG',
      chiTiet: b.loi ?? b.giaiThich ?? undefined,
    })
  }

  for (const r of dc.quyTrinh.luotChay) {
    m.push({ luc: r.batDau, loai: 'luot-chay', tieuDe: `Bắt đầu lượt chạy: ${r.ten} v${r.phienBan}` })
    if (r.boLuc) m.push({ luc: r.boLuc, loai: 'luot-chay', tieuDe: `Bỏ lượt chạy`, chiTiet: `${r.boBoi}: ${r.lyDoBo}`, xau: true })
  }

  for (const g of dc.goiY) {
    m.push({
      luc: g.luc, loai: 'goi-y',
      tieuDe: `Gợi ý qua ${g.kenhRa}: ${g.bietTraLoi ? `biết trả lời${g.maBuoc ? ` · bước ${g.maBuoc}` : ''}` : 'KHÔNG BIẾT'}`,
      chiTiet: g.bietTraLoi ? `${g.soTriThuc} bản ghi tri thức · ${g.nguonDinhTuyen}` : g.lyDoKhongBiet ?? undefined,
    })
    for (const s of g.suKien) {
      if (s.nac === 'RETRIEVED' || (s.nac === 'SHOWN' && s.datDuoc)) continue // đã nói trong dòng gợi ý
      m.push({
        luc: s.luc, loai: 'nac', xau: !s.datDuoc,
        tieuDe: `${s.nac}${s.datDuoc ? '' : ' — không đạt'}${s.nguoiKhai ? ` · lời khai của ${s.nguoiKhai}` : ''}`,
        chiTiet: [s.ghiChu, s.nhomDung ? `nhóm đúng: ${s.nhomDung}` : null].filter(Boolean).join(' · ') || undefined,
      })
    }
  }

  for (const w of dc.guiVe) {
    m.push({ luc: w.taoLuc, loai: 'gui-ve', tieuDe: `Xếp hàng gửi kết quả về (${w.suKien ?? 'case mới'})` })
    if (w.guiLuc) m.push({ luc: w.guiLuc, loai: 'gui-ve', tieuDe: `Đã gửi về sau ${w.soLan} lần thử` })
    else if (w.loiCuoi) m.push({ luc: w.taoLuc, loai: 'gui-ve', tieuDe: `Gửi về: ${w.trangThai} · ${w.soLan} lần thử`, chiTiet: w.loiCuoi, xau: true })
  }

  // ⚠ So theo THỜI ĐIỂM, không theo chuỗi: phần lẻ của giây dài ngắn khác nhau ("31.9+00:00" và "31.96+00:00")
  //   thì so chuỗi xếp sai, và sai ở đúng chỗ hai việc cách nhau vài phần trăm giây — chỗ thứ tự quan trọng nhất.
  //   sort ổn định: hai mốc trùng giờ giữ thứ tự đẩy vào (evidence trước gợi ý của chính nó).
  return m.sort((a, b) => Date.parse(a.luc) - Date.parse(b.luc))
}
