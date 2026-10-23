// Cách ĐỌC tri thức / hướng dẫn / dẫn chứng cho người — dùng chung cho Giả lập bước 5, màn Bản nháp SOP và lịch sử phiên.
//
// Hàm THUẦN, không chạm Vue (cùng luật với soDo.ts): kiểm được bằng `npm run kiem:phien` không cần trình duyệt.
// ⚠ Chỉ DIỄN GIẢI nhãn host đã gắn — không tự nâng/hạ mức kiểm chứng, không tự suy "có nguồn" từ nội dung.

import type { AssertionCoLichSu } from './kieu'

/** Mức kiểm chứng — KnowledgeVocabulary.cs `VerificationLevel`. Thang 1–4, hai mức NGOÀI thang. */
export const MUC_KIEM_CHUNG: Record<string, { ten: string; loai: 'yeu' | 'vua' | 'manh' | 'tranh-chap' }> = {
  Speculative: { ten: 'suy đoán', loai: 'yeu' },
  Plausible: { ten: 'có lý', loai: 'yeu' },
  Supported: { ten: 'có nguồn hỗ trợ', loai: 'vua' },
  Verified: { ten: 'đã kiểm chứng', loai: 'manh' },
  Conflicting: { ten: 'nguồn mâu thuẫn', loai: 'tranh-chap' },
  Invalidated: { ten: 'đã bị bác', loai: 'tranh-chap' },
}

/** Nguồn gốc — `Origin`. AiInference = máy suy; người sửa thì thành HumanAssessment. */
export const NGUON_GOC: Record<string, string> = {
  SystemFact: 'hệ thống quan sát',
  UserConfirmed: 'người dùng nói vậy',
  AiInference: 'AI suy luận',
  HumanAssessment: 'người đánh giá',
}

/** Loại phát biểu — `AssertionKind`, theo thứ tự người đọc cần. */
export const LOAI_PHAT_BIEU: { kind: string; ten: string }[] = [
  { kind: 'CauseExists', ten: 'Nguyên nhân tồn tại' },
  { kind: 'Applicability', ten: 'Điều kiện áp dụng' },
  { kind: 'Recognition', ten: 'Cách nhận ra' },
  { kind: 'Handling', ten: 'Cách xử lý' },
]

export const QUAN_HE: Record<string, string> = {
  Support: 'hỗ trợ',
  ContextFor: 'bối cảnh',
  Refute: 'bác bỏ',
}

/** Vì sao /hoi trả KHÔNG BIẾT — HoiHandler.cs. Không biết là một CÂU TRẢ LỜI hợp lệ, không phải lỗi. */
export const LY_DO_KHONG_BIET: Record<string, string> = {
  'dinh-tuyen-khong-biet': 'Không định tuyến được ticket vào nhóm nào — chưa ai nạp nhóm, bảng tra không có, model chưa gọi hoặc dưới ngưỡng.',
  'nhom-chua-co-sop-duoc-duyet': 'Biết nhóm, nhưng nhóm này chưa có tri thức nào được NGƯỜI duyệt.',
}

/** Vì sao case không đi theo quy trình — KhopQuyTrinh.cs. */
export const LY_DO_KHONG_CO_QUY_TRINH: Record<string, string> = {
  'nhom-chua-co-quy-trinh-duoc-duyet': 'Nhóm chưa có quy trình ACTIVE — tri thức trả theo mức nhóm.',
  'dinh-tuyen-khong-biet': 'Chưa biết nhóm nên không khớp được quy trình.',
  'luot-chay-da-bi-bo': 'Lượt chạy của case đã bị người bỏ.',
}

/** Trạng thái hiển thị của tri thức / quy trình — đọc NGUYÊN nhãn host trả, chỉ thêm lời. */
export const TRANG_THAI: Record<string, { ten: string; loai: 'chua' | 'canh' | 'xong' | 'cu' }> = {
  DRAFT: { ten: 'chưa duyệt', loai: 'chua' },
  NEEDS_REVIEW: { ten: 'cần xem lại', loai: 'canh' },
  ACTIVE: { ten: 'đã duyệt', loai: 'xong' },
  DEPRECATED: { ten: 'đã thay bằng phiên bản mới', loai: 'cu' },
  SUPERSEDED: { ten: 'đã bị thay thế', loai: 'cu' },
}

/** Phạm vi xem — VisibilityScopes (tập đã khoá, AR-a). Thứ tự = hẹp → rộng. */
export const PHAM_VI_XEM = [
  { gia: 'rieng-tu', ten: 'riêng tư (hẹp nhất)' },
  { gia: 'noi-bo', ten: 'nội bộ' },
  { gia: 'toan-cong-ty', ten: 'toàn công ty (rộng nhất)' },
] as const

/** Một dẫn chứng như component DanChungDoc nhận — chung cho `/hoi` (DanChungRef) và GET /knowledge/{id} (DanChungMau). */
export interface MotDanChung {
  khoaNguonMau: string | null
  /** Có trong /hoi; KHÔNG có trong GET /knowledge/{id} — khi đó suy từ khoá mẩu rồi đối chiếu id mẩu. */
  khoaNguonCase?: string | null
  mauId?: string
  quanHe: string
  ghiChu: string | null
  daRutLai?: boolean
}

/** Một dẫn chứng tính là CHỐNG LƯNG khi quan hệ là Support và mẩu chưa bị rút lại. ContextFor không chống lưng. */
export function laHoTro(d: { quanHe: string; daRutLai?: boolean }): boolean {
  return d.quanHe === 'Support' && !d.daRutLai
}

/**
 * Lịch sử của MỘT phát biểu đang hiệu lực: đi ngược chuỗi `biThayBoi` tới bản gốc.
 * Trả [gốc, …, hiện tại]. Bản gốc là bản không bị bản nào thay TỚI nó.
 */
export function chuoiLichSu(tatCa: AssertionCoLichSu[], hienTai: AssertionCoLichSu): AssertionCoLichSu[] {
  const truocCua = new Map<string, AssertionCoLichSu>()
  for (const a of tatCa) if (a.biThayBoi) truocCua.set(a.biThayBoi, a)
  const chuoi = [hienTai]
  const daQua = new Set([hienTai.id])
  let p = truocCua.get(hienTai.id)
  while (p && !daQua.has(p.id)) {
    chuoi.unshift(p)
    daQua.add(p.id)
    p = truocCua.get(p.id)
  }
  return chuoi
}

/** Phát biểu ĐANG HIỆU LỰC theo loại, theo thứ tự LOAI_PHAT_BIEU; loại lạ dồn cuối thay vì bị giấu. */
export function nhomTheoLoai<T extends { kind: string }>(ds: T[]): { kind: string; ten: string; ds: T[] }[] {
  const biet = new Set(LOAI_PHAT_BIEU.map((l) => l.kind))
  const ra = LOAI_PHAT_BIEU.map((l) => ({ ...l, ds: ds.filter((a) => a.kind === l.kind) }))
  const la = [...new Set(ds.filter((a) => !biet.has(a.kind)).map((a) => a.kind))]
  return [...ra, ...la.map((k) => ({ kind: k, ten: k, ds: ds.filter((a) => a.kind === k) }))].filter((g) => g.ds.length)
}

/**
 * Case chứa một mẩu, khi host không nói (GET /knowledge/{id} chỉ trả khoá mẩu): phần trước `#` của khoá mẩu.
 * Đây CHỈ là chỗ để TÌM — nơi gọi phải đối chiếu đúng id mẩu trong evidence của case rồi mới hiện nội dung.
 */
export function caseTuKhoaMau(khoaMau: string | null): string | null {
  if (!khoaMau) return null
  const i = khoaMau.indexOf('#')
  return i > 0 ? khoaMau.slice(0, i) : null
}

/**
 * Vì sao /internal/hoi-dap không trả lời — HoiDapHandler.cs. Hai mã đầu là KHÔNG BIẾT về nghiệp vụ (200, hợp lệ); mọi mã
 * còn lại là LỖI VẬN HÀNH (host trả 409, `loiVanHanh: true`) — trần, khoá, model, cổng che.
 */
export const LY_DO_HOI_DAP: Record<string, string> = {
  'khong-co-nguon-dan-chung': 'AI nói biết, nhưng không phát biểu nào dẫn được mẩu có thật — đã bỏ hết.',
  'nguon-khong-noi': 'Nguồn không trả lời được câu hỏi này.',
  'vuot-tran-thang': 'Đã chạm trần tháng (HoiDap:TranThang).',
  'chua-co-credit': 'Nhà cung cấp chưa có khoá / model.',
  'khoa-bi-tu-choi': 'Nhà cung cấp TỪ CHỐI khoá.',
  'model-khong-ton-tai': 'Nhà cung cấp không có model này.',
  'het-credit': 'Tài khoản model hết tiền.',
  'cong-che-chan': 'Cổng che chặn — còn thứ nghi là bí mật sau khi che.',
  'loi-goi-model': 'Gọi model thất bại (xem biên lai).',
}
