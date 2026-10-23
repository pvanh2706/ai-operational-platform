// Phiên TRẢI NGHIỆM — kiểu dữ liệu + phép tính THUẦN (thống kê, so sánh, xuất Markdown). Không chạm Vue, không gọi mạng:
// kiểm bằng `npm run kiem:phien`. Nơi lưu: ui_server, scripts/giao-dien/phien_trai_nghiem.py.
//
// 🛑 Ba thứ cố ý KHÔNG trộn:
//   · KẾT CỤC VẬN HÀNH (trả lời / không biết / lỗi vận hành / đang chờ) — hệ thống đã làm gì.
//   · ĐÁNH GIÁ của người thử (đúng? dẫn chứng đủ? làm theo được?) — mặc định "chưa đánh giá", không bao giờ suy từ HTTP 200.
//   · CHẤM ĐỊNH TUYẾN trên đáp án bịa của giả lập — chỉ nói về bước định tuyến, không về chất lượng hướng dẫn/SOP.
// "Không biết" không mặc nhiên là "sai"; "đánh giá tốt" không mặc nhiên là "đã dùng" (USED là lời khai riêng ở host).

import type { DongCham } from './apiGiaLap'
import type { KetQuaHoiDap } from './kieu'

export const DINH_DANG = 'kp-phien-trai-nghiem/1'

export interface NguCanh {
  /** Khoá thư mục ở ui_server: `cong-<cổng>.<tenantId>` — mọi đọc/ghi đều phải nói khoá này. */
  khoa: string
  cong: number
  app: string
  moTa: string
  tenantId: string | null
  cheDoTenant: string | null
  moiTruong: 'gia-lap' | 'dev'
}

export const DO_DUNG = {
  chua: 'Chưa đánh giá', dung: 'Đúng', 'mot-phan': 'Đúng một phần', sai: 'Sai', 'chua-du-co-so': 'Chưa đủ cơ sở',
} as const
export const DAN_CHUNG = {
  chua: 'Chưa đánh giá', du: 'Đủ', 'mot-phan': 'Một phần', 'khong-ho-tro': 'Không hỗ trợ', 'khong-ap-dung': 'Không áp dụng',
} as const
export const THUC_HIEN = {
  chua: 'Chưa đánh giá', 'lam-theo-duoc': 'Làm theo được', 'can-bo-sung': 'Cần bổ sung', 'khong-dung-duoc': 'Không dùng được',
  'khong-ap-dung': 'Không áp dụng',
} as const

export interface DanhGia {
  doDung: keyof typeof DO_DUNG
  danChung: keyof typeof DAN_CHUNG
  thucHienDuoc: keyof typeof THUC_HIEN
  nhanXet: string
  /** Câu trả lời / hướng dẫn mà người thử MONG ĐỢI. */
  mongDoi: string
  luc: string | null
}

export const CHIEU = [
  { k: 'doDung', ten: 'Độ đúng nội dung', nhan: DO_DUNG },
  { k: 'danChung', ten: 'Dẫn chứng hỗ trợ', nhan: DAN_CHUNG },
  { k: 'thucHienDuoc', ten: 'Tính thực hiện được', nhan: THUC_HIEN },
] as const

export type KetCuc = 'tra-loi' | 'khong-biet' | 'loi-van-hanh' | 'dang-cho' | 'thao-tac'

export const TEN_KET_CUC: Record<KetCuc, string> = {
  'tra-loi': 'trả lời',
  'khong-biet': 'không biết',
  'loi-van-hanh': 'lỗi vận hành',
  'dang-cho': 'đang chờ / bị gián đoạn',
  'thao-tac': 'thao tác bản nháp',
}

/** Một thao tác trên bản nháp — sửa và duyệt là HAI việc, mỗi đối tượng một dòng. */
export interface ThaoTac {
  luc: string
  viec: 'sua-tri-thuc' | 'sua-buoc' | 'duyet-tri-thuc' | 'duyet-quy-trinh'
  doiTuong: string
  ten: string
  ok: boolean
  http: number | null
  trangThaiSau: string | null
  loi: string | null
  /** Sửa bước: bản bước TRƯỚC khi thay — host không giữ (PUT thay toàn bộ), console giữ hộ ở đây. */
  banTruoc?: unknown
  chiTiet?: unknown
}

export interface Luot {
  id: string
  loai: 'hoi-dap' | 'hoi-gia-lap' | 'ban-nhap'
  batDau: string
  ketThuc: string | null
  /** Đo ở GIAO DIỆN, từ bấm nút tới lúc có kết quả — gồm mạng, ui_server, host; không phải thời gian model xử lý. */
  thoiGianChoMs: number | null
  /** Có giá trị = thời gian trên đo cho CẢ LÔ ngần ấy ticket (hỏi lại cả nhóm), không phải cho riêng lượt này. */
  thoiGianCaLo?: number
  /** LỜI KHAI — console chưa xác thực danh tính cá nhân. */
  nguoiThaoTac: string | null
  nguon: { loai: 'case' | 'tai-lieu' | 'ticket-gia-lap' | 'ban-nhap'; khoa: string; ten: string } | null
  cauHoi: string | null
  cheDo: 'api' | 'file' | null
  /** Chỉ lấy từ KẾT QUẢ host trả — không gán theo cấu hình khi không biết. */
  nhaCungCap: string | null
  model: string | null
  ketCuc: KetCuc
  loi: { http: number | null; lyDo: string | null; text: string } | null
  /** Bản chụp kết quả hỏi tự do — gồm các mẩu nguồn ĐÃ gửi model (đã qua cổng che). */
  hoiDap?: KetQuaHoiDap | null
  /** Bản chụp một ticket hỏi lại trong giả lập — hướng dẫn kèm KHOÁ dẫn chứng, không kèm nội dung mẩu. */
  giaLap?: DongCham | null
  banNhap?: { tri: string[]; qt: string | null; nhom: string | null; thaoTac: ThaoTac[] }
  lienKet: { maLuotGoiY?: string | null; truocDo?: string | null; triThuc?: string[]; quyTrinh?: string | null }
  kichBan: string | null
  danhGia: DanhGia
}

export interface PhienTraiNghiem {
  dinhDang: string
  id: string
  ten: string
  nguCanh: NguCanh
  kichBan: string | null
  nguoiThaoTac: string | null
  ghiChu: string
  taoLuc: string
  capNhatLuc: string
  luot: Luot[]
}

export function danhGiaTrong(): DanhGia {
  return { doDung: 'chua', danChung: 'chua', thucHienDuoc: 'chua', nhanXet: '', mongDoi: '', luc: null }
}

export function idMoi(): string {
  // crypto.randomUUID cần secure context — 127.0.0.1 là secure context, nhưng giữ đường lùi cho chắc.
  return globalThis.crypto?.randomUUID?.() ?? `${Date.now().toString(16)}-${Math.random().toString(16).slice(2, 14)}`
}

export function daDanhGia(d: DanhGia): boolean {
  return d.doDung !== 'chua' || d.danChung !== 'chua' || d.thucHienDuoc !== 'chua' || !!d.nhanXet.trim() || !!d.mongDoi.trim()
}

/** Kết cục vận hành của một lượt giả lập, từ hàng `/hoi`. */
export function ketCucGiaLap(d: DongCham): KetCuc {
  if (d.loiVanHanh || (d.http != null && d.http !== 200)) return 'loi-van-hanh'
  return d.bietTraLoi ? 'tra-loi' : 'khong-biet'
}

/** Lượt "cần mở lại": người thử đã đánh dấu sai/thiếu/cần bổ sung ở BẤT KỲ chiều nào. */
export function canXemLai(l: Luot): string[] {
  const d = l.danhGia
  const ly: string[] = []
  if (d.doDung === 'sai') ly.push('nội dung sai')
  if (d.doDung === 'mot-phan') ly.push('đúng một phần')
  if (d.danChung === 'khong-ho-tro') ly.push('dẫn chứng không hỗ trợ')
  if (d.danChung === 'mot-phan') ly.push('dẫn chứng một phần')
  if (d.thucHienDuoc === 'can-bo-sung') ly.push('cần bổ sung')
  if (d.thucHienDuoc === 'khong-dung-duoc') ly.push('không dùng được')
  return ly
}

export interface PhanBo {
  ten: string
  /** Số lượt ĐÃ đánh giá ở chiều này — mẫu số của các tỉ lệ. 0 = "chưa có đánh giá", KHÔNG phải 0%. */
  mauSo: number
  /** Số lượt hỏi CHƯA đánh giá ở chiều này. */
  chua: number
  dem: { gia: string; ten: string; so: number }[]
}

export interface ThongKe {
  tong: number
  luotHoi: number
  /** Thành công về VẬN HÀNH = trả lời + không biết. */
  thanhCongVanHanh: number
  ketCuc: Record<KetCuc, number>
  daDanhGia: number
  chuaDanhGia: number
  phanBo: PhanBo[]
  /** Chấm định tuyến trên đáp án giả — chỉ các lượt giả lập không lỗi vận hành. */
  dinhTuyen: { dung: number; sai: number; khongBiet: number; mauSo: number }
  canXemLai: { luot: Luot; lyDo: string[] }[]
  banNhap: { luot: Luot; soSua: number; duyetOk: number; duyetHong: number; doiTuongDaDuyet: string[] }[]
}

export function thongKe(luot: Luot[]): ThongKe {
  const hoi = luot.filter((l) => l.loai !== 'ban-nhap')
  const ketCuc: Record<KetCuc, number> = { 'tra-loi': 0, 'khong-biet': 0, 'loi-van-hanh': 0, 'dang-cho': 0, 'thao-tac': 0 }
  for (const l of luot) ketCuc[l.ketCuc]++

  // Đánh giá tính trên lượt HỎI; lượt bản nháp đánh giá SOP — vẫn đếm vào "đã/chưa đánh giá" để không sót.
  const daDG = luot.filter((l) => daDanhGia(l.danhGia)).length

  const phanBo: PhanBo[] = CHIEU.map((c) => {
    const nhan = c.nhan as Record<string, string>
    const dem = Object.entries(nhan)
      .filter(([g]) => g !== 'chua')
      .map(([gia, ten]) => ({ gia, ten, so: luot.filter((l) => l.danhGia[c.k] === gia).length }))
    const mauSo = dem.reduce((n, d) => n + d.so, 0)
    return { ten: c.ten, mauSo, chua: luot.length - mauSo, dem }
  })

  const gl = hoi.filter((l) => l.loai === 'hoi-gia-lap' && l.giaLap && l.ketCuc !== 'loi-van-hanh' && l.ketCuc !== 'dang-cho')
  const dinhTuyen = {
    dung: gl.filter((l) => l.giaLap!.ketQua === 'dung').length,
    sai: gl.filter((l) => l.giaLap!.ketQua === 'sai').length,
    khongBiet: gl.filter((l) => l.giaLap!.ketQua === 'khong-biet').length,
    mauSo: gl.length,
  }

  return {
    tong: luot.length,
    luotHoi: hoi.length,
    thanhCongVanHanh: ketCuc['tra-loi'] + ketCuc['khong-biet'],
    ketCuc,
    daDanhGia: daDG,
    chuaDanhGia: luot.length - daDG,
    phanBo,
    dinhTuyen,
    canXemLai: luot.map((l) => ({ luot: l, lyDo: canXemLai(l) })).filter((x) => x.lyDo.length),
    banNhap: luot
      .filter((l) => l.loai === 'ban-nhap' && l.banNhap)
      .map((l) => {
        const tt = l.banNhap!.thaoTac
        return {
          luot: l,
          soSua: tt.filter((t) => t.viec.startsWith('sua') && t.ok).length,
          duyetOk: tt.filter((t) => t.viec.startsWith('duyet') && t.ok).length,
          duyetHong: tt.filter((t) => t.viec.startsWith('duyet') && !t.ok).length,
          doiTuongDaDuyet: [...new Set(tt.filter((t) => t.viec.startsWith('duyet') && t.ok).map((t) => t.ten))],
        }
      }),
  }
}

/** "a/b" — hoặc "chưa có đánh giá" khi mẫu số là 0. Không bao giờ in 0% như một kết luận. */
export function tiLe(so: number, mauSo: number): string {
  if (!mauSo) return 'chưa có đánh giá'
  return `${so}/${mauSo} (${Math.round((so / mauSo) * 100)}%)`
}

/** Hai lượt khác nhau ở đâu — so sánh chỉ CÔNG BẰNG khi đầu vào giống nhau. */
export function khacBiet(a: Luot, b: Luot): { truong: string; a: string; b: string }[] {
  const lay: [string, (l: Luot) => string][] = [
    ['Loại lượt', (l) => l.loai],
    ['Câu hỏi', (l) => l.cauHoi ?? (l.giaLap ? `/hoi ${l.giaLap.khoa}` : '—')],
    ['Nguồn', (l) => (l.nguon ? `${l.nguon.loai} ${l.nguon.khoa}` : '—')],
    ['Chế độ', (l) => l.cheDo ?? '—'],
    ['Nhà cung cấp / model', (l) => `${l.nhaCungCap ?? 'không rõ'} / ${l.model ?? 'không rõ'}`],
    ['Phiên bản quy trình', (l) => (l.giaLap?.quyTrinh ? `${l.giaLap.quyTrinh.ten} v${l.giaLap.quyTrinh.phienBan}` : '—')],
  ]
  return lay.map(([truong, f]) => ({ truong, a: f(a), b: f(b) })).filter((x) => x.a !== x.b)
}

function dongMd(s: string | null | undefined): string {
  return (s ?? '').replace(/\r?\n/g, ' ').replace(/\|/g, '\\|')
}

/** Bản Markdown để chia sẻ — CHỈ khi người dùng chủ động bấm xuất. Cùng dữ liệu với JSON, không thêm kết luận nào. */
export function xuatMarkdown(p: PhienTraiNghiem, luc: string): string {
  const tk = thongKe(p.luot)
  const o: string[] = []
  o.push(`# ${p.ten}`, '')
  o.push(`- Định dạng: \`${p.dinhDang}\` · xuất lúc ${luc}`)
  o.push(`- Ngữ cảnh: ${p.nguCanh.app} (cổng ${p.nguCanh.cong}, ${p.nguCanh.moiTruong}) · tenant \`${p.nguCanh.tenantId ?? 'không xác định'}\``)
  o.push(`- Người thao tác (lời khai, chưa xác thực): ${p.nguoiThaoTac ?? '—'}`)
  if (p.kichBan) o.push(`- Kịch bản: ${p.kichBan}`)
  o.push(`- Tạo ${p.taoLuc} · cập nhật ${p.capNhatLuc}`, '')
  o.push('> Đánh giá dưới đây là của người thử. Nó không đổi mức kiểm chứng, trạng thái duyệt hay nấc USED của sản phẩm.')
  o.push('> Chấm định tuyến dùng đáp án BỊA của giả lập — kiểm luồng xử lý, chưa chứng minh độ chính xác trên dữ liệu thật.', '')
  o.push('## Tổng kết', '')
  o.push(`| Mục | Số |`, `|---|---|`)
  o.push(`| Tổng lượt | ${tk.tong} |`)
  o.push(`| Thành công về vận hành (trả lời + không biết) | ${tk.thanhCongVanHanh} |`)
  for (const k of Object.keys(TEN_KET_CUC) as KetCuc[]) o.push(`| — ${TEN_KET_CUC[k]} | ${tk.ketCuc[k]} |`)
  o.push(`| Đã đánh giá / chưa đánh giá | ${tk.daDanhGia} / ${tk.chuaDanhGia} |`, '')
  for (const pb of tk.phanBo) {
    o.push(`**${pb.ten}** — mẫu số ${pb.mauSo}${pb.mauSo ? '' : ' (chưa có đánh giá)'}, chưa đánh giá ${pb.chua}`)
    for (const d of pb.dem) o.push(`- ${d.ten}: ${tiLe(d.so, pb.mauSo)}`)
    o.push('')
  }
  o.push(`**Chấm định tuyến (đáp án giả)** — ${tk.dinhTuyen.mauSo ? `đúng ${tiLe(tk.dinhTuyen.dung, tk.dinhTuyen.mauSo)}, sai ${tk.dinhTuyen.sai}, không biết ${tk.dinhTuyen.khongBiet}` : 'không có lượt giả lập'}`, '')
  o.push('## Lượt', '')
  p.luot.forEach((l, i) => {
    o.push(`### ${i + 1}. ${TEN_KET_CUC[l.ketCuc]} · ${l.loai} · ${l.batDau}`, '')
    o.push(`- Id: \`${l.id}\`${l.lienKet.truocDo ? ` · sau lượt \`${l.lienKet.truocDo}\`` : ''}`)
    if (l.nguon) o.push(`- Nguồn: ${l.nguon.loai} \`${l.nguon.khoa}\` — ${dongMd(l.nguon.ten)}`)
    if (l.cauHoi) o.push(`- Câu hỏi: ${dongMd(l.cauHoi)}`)
    o.push(`- Chế độ: ${l.cheDo ?? '—'} · nhà cung cấp/model: ${l.nhaCungCap ?? 'không rõ'} / ${l.model ?? 'không rõ'} · token/chi phí: chưa có dữ liệu`)
    o.push(`- Thời gian chờ (đo ở giao diện, gồm mạng + host, không phải thời gian model): ${l.thoiGianChoMs != null ? `${(l.thoiGianChoMs / 1000).toFixed(1)} s` : '—'}${l.thoiGianCaLo ? ` — đo cho CẢ LÔ ${l.thoiGianCaLo} ticket` : ''}`)
    if (l.loi) o.push(`- Lỗi: HTTP ${l.loi.http ?? '—'} ${l.loi.lyDo ?? ''} — ${dongMd(l.loi.text)}`)
    if (l.hoiDap) {
      const k = l.hoiDap
      o.push(`- Trạng thái: ${k.trangThai} · ${k.nguon.length} mẩu đã gửi${k.nguonBiCat ? ' · ⚠ nguồn bị cắt' : ''}${k.soChoDaChe ? ` · đã che ${k.soChoDaChe} chỗ` : ''}`)
      if (!k.bietTraLoi) o.push(`- KHÔNG BIẾT: ${k.lyDoKhongBiet ?? ''}`)
      for (const pb of k.phatBieu) o.push(`  - ${dongMd(pb.noiDung)} [${pb.nguon.join(', ')}]`)
    }
    if (l.giaLap) {
      const d = l.giaLap
      o.push(`- Ticket \`${d.khoa}\` · định tuyến ${d.nhom} (${d.nguon}) · đáp án giả ${d.nhomDung} → ${d.ketQua}`)
      o.push(`- ${d.bietTraLoi ? 'Biết trả lời' : `Không biết: ${d.lyDoKhongBiet ?? ''}`} · bước hiện tại ${d.buocHienTai ?? '—'}${d.noiDungBuoc ? ` — ${dongMd(d.noiDungBuoc)}` : ''}`)
      for (const h of d.huongDan ?? []) {
        o.push(`  - **${dongMd(h.tenNguyenNhan)}** (${h.trangThai})`)
        for (const pb of [...h.dieuKienApDung, ...h.cachNhanRa, ...h.cachXuLy])
          o.push(`    - ${dongMd(pb.noiDung)} _(${pb.mucKiemChung}, ${pb.nguonGoc}, ${pb.danChung?.length ?? 0} dẫn chứng)_`)
      }
    }
    if (l.banNhap) {
      o.push(`- Bản nháp: tri thức ${l.banNhap.tri.map((x) => `\`${x}\``).join(', ') || '—'} · quy trình ${l.banNhap.qt ? `\`${l.banNhap.qt}\`` : '—'}`)
      for (const t of l.banNhap.thaoTac) o.push(`  - ${t.luc} ${t.viec} ${dongMd(t.ten)}: ${t.ok ? 'xong' : 'HỎNG'}${t.http ? ` HTTP ${t.http}` : ''}${t.trangThaiSau ? ` → ${t.trangThaiSau}` : ''}${t.loi ? ` — ${dongMd(t.loi)}` : ''}`)
    }
    const d = l.danhGia
    o.push(`- Đánh giá: độ đúng **${DO_DUNG[d.doDung]}** · dẫn chứng **${DAN_CHUNG[d.danChung]}** · thực hiện **${THUC_HIEN[d.thucHienDuoc]}**`)
    if (d.mongDoi.trim()) o.push(`- Mong đợi: ${dongMd(d.mongDoi)}`)
    if (d.nhanXet.trim()) o.push(`- Nhận xét: ${dongMd(d.nhanXet)}`)
    o.push('')
  })
  return o.join('\n')
}
