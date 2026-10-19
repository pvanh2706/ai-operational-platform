// Gọi các nút của trang giả lập — `/api/gl-*` của ui_server.py (scripts/giao-dien/gia_lap.py). Hình dạng CHÉP từ
// đó; nguồn sự thật là file Python.
//
// ⚠ Giả lập LUÔN chạy trên app 5120 (kp_gialap): các lệnh xuất/nạp CLI mà gia_lap.py gọi thay có chuỗi kết nối
//   ghim vào kp_gialap. Chọn app 5119 ở góc phải KHÔNG đổi chỗ giả lập chạy.

export const CONG_GIA_LAP = 5120

export interface TicketGiaLap {
  khoa: string
  tieuDe: string
  nhomDung: string
}

export interface TrangThaiGiaLap {
  ok: boolean
  appSong: boolean
  chiTietApp: unknown
  coKhoa: boolean
  goiModel: boolean
  cauHinh: string
  nhomDungLech: string[]
  lo: string | null
  tienTo: string | null
  ticket: TicketGiaLap[]
}

export interface DongCham {
  khoa: string
  tieuDe: string
  nhomDung: string
  nhom: string
  nguon: string
  giaiThich: string | null
  ketQua: 'dung' | 'sai' | 'khong-biet'
  bietTraLoi: boolean | null
  lyDoKhongBiet: string | null
  soHuongDan: number
  buocHienTai: string | null
  noiDungBuoc: string | null
  daXongHet: boolean | null
  lyDoKhongCoQuyTrinh: string | null
  maLuotGoiY: string | null
}

export interface KetQuaDinhTuyen {
  ok: boolean
  loi?: string
  maThoat?: number
  baoCao?: string
  dong: DongCham[]
  cham: { dung: number; sai: number; khongBiet: number; tong: number }
  nhom: { ten: string; soCase: number }[]
}

export interface KetQuaXuat {
  ok: boolean
  loi?: string
  baoCao?: string
  duongVao: string
  duongRa: string
  noiDung: string
  lenh: { cau: string; claude: string; codex: string }
}

/** `ketQua` là nguyên văn host trả: KetQuaSoanNhap (đường API), SopDraftIngestResult (đường file), hoặc ProblemDetails. */
export interface KetQuaSoan {
  ok: boolean
  loi?: string
  http?: number
  ketQua: Record<string, unknown> | null
}

export interface KetQuaDuyet {
  ok: boolean
  loi?: string
  dong: { loai: string; ten: string; http: number; trangThai: string | null; loi: string | null }[]
}

async function gl<T>(ten: string, than: object = {}): Promise<T & { ok: boolean; loi?: string }> {
  const r = await fetch(`/api/gl-${ten}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(than),
  })
  try {
    return await r.json()
  } catch {
    return { ok: false, loi: `HTTP ${r.status} — ui_server không trả JSON` } as T & { ok: boolean; loi?: string }
  }
}

export const giaLap = {
  trangThai: () => gl<TrangThaiGiaLap>('trang-thai', { base: `http://localhost:${CONG_GIA_LAP}` }),
  sinh: (soLuong: number) =>
    gl<{ lo: string; tienTo: string; soCase: number; soEvidence: number; ticket: TicketGiaLap[] }>('sinh', { soLuong }),
  dinhTuyenApi: () => gl<KetQuaDinhTuyen>('dinh-tuyen-api'),
  xuatDinhTuyen: () => gl<KetQuaXuat>('xuat-dinh-tuyen'),
  napDinhTuyen: (ketQua: string, nguoi: string) => gl<KetQuaDinhTuyen>('nap-dinh-tuyen', { ketQua, nguoi }),
  soanApi: (nhom: string, nguoi: string) => gl<KetQuaSoan>('soan-api', { nhom, nguoi }),
  xuatSoan: (nhom: string) => gl<KetQuaXuat>('xuat-soan', { nhom }),
  napSoan: (ketQua: string) => gl<KetQuaSoan>('nap-soan', { ketQua }),
  duyet: (nguoi: string) => gl<KetQuaDuyet>('duyet', { nguoi }),
  hoiLai: (nhom: string, khoa: string[]) => gl<{ nhom: string; dong: DongCham[] }>('hoi-lai', { nhom, khoa }),
  xacNhanBuoc: (khoa: string, maBuoc: string, nguoi: string) =>
    gl<{ http: number; ketQua: unknown; dong: DongCham[] }>('xac-nhan-buoc', { khoa, maBuoc, nguoi }),
  phanHoi: (maLuotGoiY: string, nguoi: string) =>
    gl<{ http: number; ketQua: unknown; thongKe: unknown }>('phan-hoi', { maLuotGoiY, nac: 'used', datDuoc: true, nguoi }),
}
