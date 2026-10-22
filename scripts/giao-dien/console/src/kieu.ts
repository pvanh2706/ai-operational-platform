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

/** `POST /internal/thu-khoa-model` — Infrastructure/Sop/ThuKhoaModel.cs. Không bao giờ chứa khoá. */
export interface KetQuaThuKhoa {
  coKhoa: boolean
  /** true = khoá được nhận VÀ model tồn tại · false = bị từ chối · null = không thử được (mạng, 5xx). */
  hopLe: boolean | null
  lyDo: 'khong-co-khoa' | 'thieu-model' | 'khoa-bi-tu-choi' | 'model-khong-ton-tai' | 'het-credit' | 'loi-khac' | null
  model: string
  tenModelHienThi: string | null
  giaiThich: string
  /** `Q1-1` — nhà cung cấp đã thử. */
  nhaCungCap: NhaCungCap
}

export type NhaCungCap = 'anthropic' | 'openai'
export type CheDoAi = 'api' | 'file'

/** `GET /internal/ai` — Api/Knowledge/CacModel.cs. Không bao giờ chứa khoá. */
export interface TrangThaiNhaCungCap {
  ten: NhaCungCap
  coKhoa: boolean
  model: string | null
  /** Có khoá (và với OpenAI: có model). CHƯA nói khoá hợp lệ — đó là việc của Thử khoá. */
  dungDuoc: boolean
  lyDo: string | null
}
export interface TrangThaiBuocAi {
  nhaCungCap: NhaCungCap
  dungDuoc: boolean
  tranThang: number
}
export interface TrangThaiAi {
  anthropic: TrangThaiNhaCungCap
  openAI: TrangThaiNhaCungCap
  dinhTuyen: TrangThaiBuocAi
  dinhTuyenGoiModel: boolean
  dinhTuyenNguongTinCay: number
  soanNhap: TrangThaiBuocAi
  hoiDap: TrangThaiBuocAi
}

/** `cau-hinh-ai.json` của ui_server (Q6-1). Định tuyến KHÔNG có nhà cung cấp ở đây — đó là cấu hình host. */
export interface CauHinhAi {
  dinhTuyen: { cheDo: CheDoAi }
  soanNhap: { cheDo: CheDoAi; nhaCungCap: NhaCungCap }
  hoiDap: { cheDo: CheDoAi; nhaCungCap: NhaCungCap }
}

/** `GET /documents` — Api/Documents/DocumentHandler.cs. */
export interface TaiLieu {
  id: string
  tenKhaiBao: string
  loaiNoiDung: string
  kichThuoc: number
  subTenantCode: string
  nguoiNap: string
  napLuc: string
  trangThaiTrichChu: string | null
}
export interface KetQuaNapTaiLieu {
  id: string
  tenKhaiBao: string
  kichThuoc: number
  daCoTuTruoc: boolean
  trungVoi: string | null
  trangThaiTrichChu: string | null
  ghiChuTrichChu: string | null
}

/** `POST /internal/hoi-dap*` — Api/Knowledge/HoiDapHandler.cs (Q3-1). */
export interface NguonHoiDap { ma: string; nhan: string; noiDung: string }
export interface KetQuaHoiDap {
  bietTraLoi: boolean
  /** true = không hỏi được vì vận hành (trần, khoá…) — host trả 409. */
  loiVanHanh: boolean
  lyDoKhongBiet: string | null
  giaiThich: string | null
  phatBieu: { noiDung: string; nguon: string[] }[]
  soPhatBieuBiBo: number
  ghiChu: string | null
  nguon: NguonHoiDap[]
  nguonBiCat: boolean
  soChoDaChe: number
  cheDo: CheDoAi
  nhaCungCap: NhaCungCap | null
  model: string | null
  /** Luôn `CHUA-DUYET`. */
  trangThai: string
}
export interface PromptHoiDap {
  systemText: string
  userText: string
  schema: string
  nguon: NguonHoiDap[]
  nguonBiCat: boolean
  soChoDaChe: number
  huongDan: string
  trangThai: string
}

/** ProblemDetails của ASP.NET — mọi lỗi 4xx/5xx của host. */
export interface LoiHost {
  title?: string
  detail?: string
  status?: number
  lyDo?: string
  /** 409 của `/internal/hoi-dap` mang kèm kết quả (mẩu đã gửi, lý do). */
  ketQua?: KetQuaHoiDap
}
