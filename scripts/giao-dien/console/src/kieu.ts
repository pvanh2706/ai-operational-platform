// Hình dạng response của host — CHÉP từ record C#, tên trường camelCase như System.Text.Json trả ra.
// ⚠ Nguồn sự thật là C#: src/KnowledgePlatform.Api/DongChay/DongChayHandler.cs và QuyTrinh/QuyTrinhHandler.cs.
//   Đổi bên đó mà quên bên này thì `npm run kiem` KHÔNG bắt được — chỉ lượt chạy thật bắt được.

/** `GET /internal/cases` — một dòng. */
export interface CaseTrongDanhSach {
  khoaNguon: string
  tieuDe: string
  napLuc: string
  xongONguonLuc: string | null
  soMau: number
  soLuotGoiY: number
  coLuotChayMo: boolean
  /** Nhóm /hoi sẽ dùng, tính KHÔNG gọi model. null = không biết. */
  nhomHieuLuc: string | null
  /** `nguoi-nap:` · `tra-san:` · `model:` (kể cả `model:chua-goi`). */
  nguonDinhTuyen: string
}

export interface MauTrongDongChay {
  khoaNguon: string
  quanSatLuc: string | null
  napLuc: string
  noiDungRutGon: string
  doDai: number
  mayDoc: string
  maKhachSan: string | null
  rutLaiLuc: string | null
  /** Mã bước nếu mẩu này là LỜI XÁC NHẬN bước. */
  xacNhanBuoc: string | null
}

export interface DinhTuyenNguoiNap {
  nhom: string
  nguon: string
  giaiThich: string | null
  nguoiNap: string
  napLuc: string
}

export interface BienLaiModel {
  luc: string
  thanhCong: boolean
  nhom: string | null
  doTinCay: number | null
  nguon: string | null
  giaiThich: string | null
  loi: string | null
  soChoDaChe: number
}

export interface DinhTuyenTrongDongChay {
  nhom: string | null
  nguon: string
  giaiThich: string | null
  nguoiNap: DinhTuyenNguoiNap | null
  bienLaiModel: BienLaiModel[]
}

/** Vì sao một bước được coi là xong — mẩu nào, bằng cách nào (dấu hiệu / lời xác nhận). Domain/Process/TienDoQuyTrinh.cs */
export interface CanCuXong {
  mauId: string
  khoaNguonMau: string
  cach: string
}

export interface TienDoBuoc {
  id: string
  thuTu: number
  ma: string
  noiDung: string
  noiXem: string | null
  chuDe: string
  daXong: boolean
  canCu: CanCuXong[]
}

/** Cùng hình dạng `GET /cases/{k}/quy-trinh`. */
export interface TienDoCase {
  khoaNguon: string
  coQuyTrinh: boolean
  lyDoKhongCo: string | null
  giaiThich: string | null
  quyTrinh: { id: string; nhom: string; ten: string; phienBan: number; trangThai: string; soBuoc: number } | null
  luotChayId: string | null
  dangChay: boolean
  buoc: TienDoBuoc[]
  buocHienTai: TienDoBuoc | null
  daXongHet: boolean
}

export interface LuotChayTrongDongChay {
  id: string
  quyTrinhId: string
  nhom: string
  ten: string
  phienBan: number
  trangThaiQuyTrinh: string
  batDau: string
  boLuc: string | null
  boBoi: string | null
  lyDoBo: string | null
}

export interface SuKienGoiY {
  nac: 'RETRIEVED' | 'SHOWN' | 'OPENED' | 'ACCEPTED' | 'USED'
  datDuoc: boolean
  luc: string
  /** Có giá trị = LỜI KHAI, không phải hệ thống tự ghi. */
  nguoiKhai: string | null
  ghiChu: string | null
  nhomDung: string | null
}

export interface LuotGoiYTrongDongChay {
  id: string
  luc: string
  kenhRa: string
  nhom: string | null
  nguonDinhTuyen: string
  bietTraLoi: boolean
  lyDoKhongBiet: string | null
  maBuoc: string | null
  soTriThuc: number
  suKien: SuKienGoiY[]
}

export interface LuotGuiVe {
  id: string
  trangThai: string
  soLan: number
  taoLuc: string
  guiLuc: string | null
  loiCuoi: string | null
  suKien: string | null
}

/** `GET /internal/cases/{k}/dong-chay`. */
export interface DongChayCase {
  khoaNguon: string
  caseId: string
  tieuDe: string
  taoONguonLuc: string | null
  xongONguonLuc: string | null
  napLuc: string
  evidence: MauTrongDongChay[]
  dinhTuyen: DinhTuyenTrongDongChay
  quyTrinh: { luotChay: LuotChayTrongDongChay[]; tienDo: TienDoCase }
  goiY: LuotGoiYTrongDongChay[]
  guiVe: LuotGuiVe[]
}

/** ProblemDetails của ASP.NET — mọi lỗi 4xx/5xx của host. */
export interface LoiHost {
  title?: string
  detail?: string
  status?: number
  lyDo?: string
}
