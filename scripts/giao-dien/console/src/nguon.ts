// Đọc evidence của MỘT case để mở dẫn chứng — qua đúng đường `GET /cases/{k}/evidence`, nơi luật rút lại (AR-m) đã thực
// thi. Không có đường đọc mẩu thứ hai: hướng dẫn và tri thức chỉ mang KHOÁ mẩu, cố ý (HoiHandler.cs `DanChungDauRa`).
//
// Nhớ theo (cổng, case) trong RAM của trang: mở mười dẫn chứng của cùng một ticket không gọi host mười lần. Lỗi thì không
// nhớ — bấm lại là thử lại. Tải lại trang là đọc lại, nên thứ hiện ra luôn là bản LÚC ĐỌC, không phải bản chụp lúc hỏi.
import { api } from './api'
import type { CaseEvidenceView } from './kieu'

const nho = new Map<string, Promise<CaseEvidenceView>>()

export function docEvidence(cong: number, khoaCase: string): Promise<CaseEvidenceView> {
  const k = `${cong}|${khoaCase}`
  let p = nho.get(k)
  if (!p) {
    p = api.evidenceCase(cong, khoaCase)
    p.catch(() => nho.delete(k))
    nho.set(k, p)
  }
  return p
}

/** Sau một thao tác GHI evidence (xác nhận bước, rút lại) bản nhớ đã cũ. */
export function quenEvidence(cong: number, khoaCase?: string) {
  for (const k of [...nho.keys()]) if (k.startsWith(`${cong}|`) && (!khoaCase || k === `${cong}|${khoaCase}`)) nho.delete(k)
}
