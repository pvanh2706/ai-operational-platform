<script setup lang="ts">
// Đọc · sửa · duyệt MỘT bộ bản nháp SOP (các bản ghi tri thức + quy trình sinh từ cùng bản nháp).
//
// Ba thao tác, ba đường RIÊNG của host — cố ý không gộp (S7, M2):
//   đọc   GET  /knowledge/{id} · GET /processes/{id}          không tác dụng phụ
//   sửa   POST /knowledge/{id}/sua                              tạo bản B, bản A còn nguyên — KHÔNG duyệt
//         PUT  /processes/{id}/buoc                             chỉ DRAFT (N5-1); THAY toàn bộ bước, host không giữ bản cũ
//   duyệt POST /knowledge/{id}/approve · /processes/{id}/approve  từng đối tượng, báo từng kết quả
//
// 🛑 Lời khai AR-n (`xacNhanDuLieuThuongMai`) và phạm vi xem do NGƯỜI chọn — không điền sẵn. Cổng shared (AR-l) vẫn ở host:
//   app ở chế độ shared trả 409, và màn này hiện đúng lý do đó.
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { onBeforeRouteLeave, useRoute } from 'vue-router'
import { api, moTaLoi, LoiGoiApp, type YeuCauDuyet } from '../api'
import DanhGiaLuot from '../components/DanhGiaLuot.vue'
import PhatBieuDoc from '../components/PhatBieuDoc.vue'
import PhienBar from '../components/PhienBar.vue'
import { chuoiLichSu, laHoTro, MUC_KIEM_CHUNG, NGUON_GOC, nhomTheoLoai, PHAM_VI_XEM, TRANG_THAI } from '../doc'
import { gio } from '../gio'
import type { AssertionCoLichSu, BuocChiTiet, LichSuTriThuc, QuyTrinhChiTiet } from '../kieu'
import { danhGiaTrong, idMoi, type DanhGia, type Luot, type ThaoTac } from '../phien'
import { useKetNoi } from '../stores/ketNoi'
import { usePhien } from '../stores/phien'

const route = useRoute()
const ketNoi = useKetNoi()
const phien = usePhien()

const cong = computed(() => Number(route.query.cong) || ketNoi.cong)
const triIds = computed(() => String(route.query.tri ?? '').split(',').map((s) => s.trim()).filter(Boolean))
const qtId = computed(() => (route.query.qt ? String(route.query.qt) : null))
const nhom = computed(() => (route.query.nhom ? String(route.query.nhom) : null))

// ------------------------------------------------------------------ đọc

interface OTri { id: string; data: LichSuTriThuc | null; loi: string | null }
const tri = ref<OTri[]>([])
const qt = ref<{ data: QuyTrinhChiTiet | null; loi: string | null } | null>(null)
const dangTai = ref(false)

async function taiTri(id: string): Promise<OTri> {
  try {
    return { id, data: await api.triThuc(cong.value, id), loi: null }
  } catch (e) {
    return { id, data: null, loi: e instanceof LoiGoiApp && e.status === 404 ? `Không có bản ghi ${id} trong app :${cong.value} (sai app, hoặc khách khác).` : moTaLoi(e) }
  }
}
async function taiQt(id: string) {
  try {
    return { data: await api.quyTrinh(cong.value, id), loi: null }
  } catch (e) {
    return { data: null, loi: moTaLoi(e) }
  }
}
async function taiHet() {
  dangTai.value = true
  try {
    tri.value = await Promise.all(triIds.value.map(taiTri))
    qt.value = qtId.value ? await taiQt(qtId.value) : null
  } finally {
    dangTai.value = false
  }
}
async function taiLaiTri(id: string) {
  const moi = await taiTri(id)
  tri.value = tri.value.map((t) => (t.id === id ? moi : t))
}
watch([cong, triIds, qtId], () => void taiHet(), { immediate: true })

const hienHanh = (r: LichSuTriThuc) => r.assertion.filter((a) => a.conHieuLuc)

// ------------------------------------------------------------------ sửa tri thức

const sua = ref<Record<string, { noiDung: string; muc: string }>>({})
const loiSua = ref<Record<string, string>>({})

function batDauSua(a: AssertionCoLichSu) {
  sua.value = { ...sua.value, [a.id]: { noiDung: a.noiDung, muc: a.mucKiemChung } }
}
function huySua(id: string) {
  const s = { ...sua.value }
  delete s[id]
  sua.value = s
}
function doiCua(r: LichSuTriThuc) {
  return hienHanh(r)
    .filter((a) => sua.value[a.id] && (sua.value[a.id]!.noiDung.trim() !== a.noiDung || sua.value[a.id]!.muc !== a.mucKiemChung))
    .map((a) => ({ assertionId: a.id, noiDungMoi: sua.value[a.id]!.noiDung.trim(), mucKiemChung: sua.value[a.id]!.muc }))
}

// ------------------------------------------------------------------ sửa bước quy trình

interface BuocSua { ma: string; noiDung: string; noiXem: string; dauHieu: string; chuDe: string }
const buocSua = ref<BuocSua[] | null>(null)
const loiBuoc = ref<string | null>(null)

function batDauSuaBuoc() {
  buocSua.value = (qt.value?.data?.buoc ?? []).map((b) => ({
    ma: b.ma, noiDung: b.noiDung, noiXem: b.noiXem ?? '', dauHieu: b.dauHieu.join('\n'), chuDe: b.chuDe,
  }))
}
const buocMoi = computed<Omit<BuocChiTiet, 'thuTu'>[] | null>(() =>
  buocSua.value?.map((b) => ({
    ma: b.ma.trim(), noiDung: b.noiDung.trim(), noiXem: b.noiXem.trim() || null,
    dauHieu: b.dauHieu.split('\n').map((x) => x.trim()).filter(Boolean), chuDe: b.chuDe.trim(),
  })) ?? null)
const buocDoi = computed(() => {
  if (!buocMoi.value || !qt.value?.data) return false
  const cu = qt.value.data.buoc.map(({ thuTu: _, ...b }) => b)
  return JSON.stringify(cu) !== JSON.stringify(buocMoi.value)
})

const soChuaLuu = computed(() => tri.value.reduce((n, t) => n + (t.data ? doiCua(t.data).length : 0), 0) + (buocDoi.value ? 1 : 0))

// Thay đổi chưa lưu không được mất ÂM THẦM: rời route hỏi lại, đóng/tải lại tab thì trình duyệt hỏi.
onBeforeRouteLeave(() => (soChuaLuu.value ? window.confirm(`Còn ${soChuaLuu.value} chỉnh sửa CHƯA LƯU. Rời trang sẽ mất chúng. Vẫn rời?`) : true))
function chanDong(e: BeforeUnloadEvent) {
  if (soChuaLuu.value) e.preventDefault()
}
onMounted(() => window.addEventListener('beforeunload', chanDong))
onBeforeUnmount(() => window.removeEventListener('beforeunload', chanDong))

// ------------------------------------------------------------------ người thao tác + lượt trong phiên

const nguoi = ref('')
const ban = ref<string | null>(null)

const khoaBo = computed(() => `${[...triIds.value].sort().join(',')}|${qtId.value ?? ''}`)
const luotBN = ref<Luot | null>(null)
const loiPhien = ref<string | null>(null)

function luotMoi(): Luot {
  const luc = new Date().toISOString()
  return {
    id: idMoi(), loai: 'ban-nhap', batDau: luc, ketThuc: luc, thoiGianChoMs: null, nguoiThaoTac: nguoi.value || null,
    nguon: { loai: 'ban-nhap', khoa: khoaBo.value, ten: `bản nháp nhóm ${nhom.value ?? qt.value?.data?.nhom ?? '—'}` },
    cauHoi: null, cheDo: null, nhaCungCap: null, model: null, ketCuc: 'thao-tac', loi: null,
    banNhap: { tri: triIds.value, qt: qtId.value, nhom: nhom.value ?? qt.value?.data?.nhom ?? null, thaoTac: [] },
    lienKet: { triThuc: triIds.value, quyTrinh: qtId.value },
    kichBan: phien.o(cong.value).hienTai?.kichBan ?? null,
    danhGia: danhGiaTrong(),
  }
}
// Lượt chưa ghi — ỔN ĐỊNH theo bộ bản nháp (một object mới mỗi lần render sẽ xoá form đánh giá đang gõ).
const luotTam = ref<Luot>(luotMoi())
watch(khoaBo, () => { luotTam.value = luotMoi() })
// Lượt bản nháp của phiên đang chọn (nếu đã có) — thao tác mới NỐI vào nó thay vì sinh lượt rời rạc.
watch(() => phien.o(cong.value).hienTai, (p) => {
  luotBN.value = p?.luot.find((l) => l.loai === 'ban-nhap' && l.nguon?.khoa === khoaBo.value) ?? null
}, { immediate: true })

async function ghiThaoTac(t: ThaoTac) {
  const l = luotBN.value ?? luotTam.value
  const moi: Luot = { ...l, ketThuc: t.luc, nguoiThaoTac: nguoi.value || l.nguoiThaoTac, banNhap: { ...l.banNhap!, thaoTac: [...l.banNhap!.thaoTac, t] } }
  loiPhien.value = await phien.ghiLuot(cong.value, moi)
  if (!loiPhien.value) luotBN.value = moi
}
async function luuDanhGia(g: DanhGia) {
  const l = luotBN.value ?? luotTam.value
  const moi = { ...l, nguoiThaoTac: nguoi.value || l.nguoiThaoTac, danhGia: g }
  loiPhien.value = await phien.ghiLuot(cong.value, moi)
  if (!loiPhien.value) luotBN.value = moi
}

function thaoTac(viec: ThaoTac['viec'], doiTuong: string, ten: string, kq: { ok: boolean; http: number | null; trangThaiSau: string | null; loi: string | null }, them: Partial<ThaoTac> = {}): ThaoTac {
  return { luc: new Date().toISOString(), viec, doiTuong, ten, ...kq, ...them }
}
const httpCua = (e: unknown) => (e instanceof LoiGoiApp ? e.status : null)

async function luuSua(r: LichSuTriThuc) {
  const ds = doiCua(r)
  if (!ds.length || !nguoi.value.trim()) return
  ban.value = `sua-${r.id}`
  loiSua.value = { ...loiSua.value, [r.id]: '' }
  try {
    const kq = await api.suaTriThuc(cong.value, r.id, { nguoiSua: nguoi.value.trim(), sua: ds })
    for (const m of ds) huySua(m.assertionId)
    await ghiThaoTac(thaoTac('sua-tri-thuc', r.id, r.tenNguyenNhan, { ok: true, http: 200, trangThaiSau: kq.trangThai, loi: null }, { chiTiet: kq.daSua }))
    await taiLaiTri(r.id)
  } catch (e) {
    // Host từ chối CẢ LÔ nếu một mục sai — không mục nào được ghi; giữ nguyên chỗ đang sửa để người dùng chữa.
    loiSua.value = { ...loiSua.value, [r.id]: moTaLoi(e) }
    await ghiThaoTac(thaoTac('sua-tri-thuc', r.id, r.tenNguyenNhan, { ok: false, http: httpCua(e), trangThaiSau: null, loi: moTaLoi(e) }))
  } finally {
    ban.value = null
  }
}

async function luuBuoc() {
  const q = qt.value?.data
  if (!q || !buocMoi.value || !nguoi.value.trim()) return
  ban.value = 'buoc'
  loiBuoc.value = null
  const banTruoc = q.buoc
  try {
    const kq = await api.thayBuoc(cong.value, q.id, { nguoiSua: nguoi.value.trim(), buoc: buocMoi.value })
    buocSua.value = null
    qt.value = { data: kq, loi: null }
    await ghiThaoTac(thaoTac('sua-buoc', q.id, `${q.ten} v${q.phienBan}`, { ok: true, http: 200, trangThaiSau: kq.trangThai, loi: null }, { banTruoc }))
  } catch (e) {
    loiBuoc.value = moTaLoi(e)
    await ghiThaoTac(thaoTac('sua-buoc', q.id, `${q.ten} v${q.phienBan}`, { ok: false, http: httpCua(e), trangThaiSau: null, loi: moTaLoi(e) }))
  } finally {
    ban.value = null
  }
}

// ------------------------------------------------------------------ duyệt

const phamVi = ref('')
const lyDo = ref('')
const daDoc = ref(false)
const xacNhanTM = ref(false)

const lyDoChan = computed(() => {
  const r: string[] = []
  if (soChuaLuu.value) r.push(`còn ${soChuaLuu.value} chỉnh sửa chưa lưu — lưu hoặc huỷ trước (duyệt không mang theo bản đang gõ)`)
  if (!nguoi.value.trim()) r.push('chưa khai người duyệt')
  if (!phamVi.value) r.push('chưa chọn phạm vi xem — hệ thống không chọn hộ (S7)')
  if (!daDoc.value) r.push('chưa xác nhận đã đọc nội dung và dẫn chứng')
  if (!xacNhanTM.value) r.push('chưa khai đã soát dữ liệu thương mại (AR-n)')
  return r
})

interface DongDuyet { loai: 'tri thức' | 'quy trình'; ten: string; ok: boolean; http: number | null; trangThaiSau: string | null; loi: string | null }
const ketQuaDuyet = ref<DongDuyet[]>([])

function than(): YeuCauDuyet {
  return { nguoiDuyet: nguoi.value.trim(), phamViXem: phamVi.value, xacNhanDuLieuThuongMai: xacNhanTM.value, lyDo: lyDo.value.trim() || undefined }
}

async function duyetTri(r: LichSuTriThuc): Promise<DongDuyet> {
  let d: DongDuyet
  try {
    const kq = await api.duyetTriThuc(cong.value, r.id, than())
    d = { loai: 'tri thức', ten: r.tenNguyenNhan, ok: true, http: 200, trangThaiSau: kq.trangThaiSau, loi: null }
  } catch (e) {
    d = { loai: 'tri thức', ten: r.tenNguyenNhan, ok: false, http: httpCua(e), trangThaiSau: null, loi: moTaLoi(e) }
  }
  await ghiThaoTac(thaoTac('duyet-tri-thuc', r.id, r.tenNguyenNhan, d))
  await taiLaiTri(r.id)
  return d
}
async function duyetQt(q: QuyTrinhChiTiet): Promise<DongDuyet> {
  const ten = `${q.ten} v${q.phienBan}`
  let d: DongDuyet
  try {
    const kq = await api.duyetQuyTrinh(cong.value, q.id, than())
    d = { loai: 'quy trình', ten, ok: true, http: 200, trangThaiSau: kq.trangThai, loi: null }
  } catch (e) {
    d = { loai: 'quy trình', ten, ok: false, http: httpCua(e), trangThaiSau: null, loi: moTaLoi(e) }
  }
  await ghiThaoTac(thaoTac('duyet-quy-trinh', q.id, ten, d))
  qt.value = await taiQt(q.id)
  return d
}

async function duyetMot(viec: () => Promise<DongDuyet>, ten: string) {
  if (lyDoChan.value.length) return
  ban.value = ten
  try {
    ketQuaDuyet.value = [await viec()]
  } finally {
    ban.value = null
  }
}

/** Từng đối tượng một, báo từng kết quả. Một cái hỏng không làm "thành công toàn bộ" — và không rollback cái đã xong. */
async function duyetTatCa() {
  if (lyDoChan.value.length) return
  ban.value = 'tat-ca'
  const ra: DongDuyet[] = []
  try {
    for (const t of tri.value) if (t.data && ['DRAFT', 'NEEDS_REVIEW'].includes(t.data.trangThai)) ra.push(await duyetTri(t.data))
    if (qt.value?.data?.trangThai === 'DRAFT') ra.push(await duyetQt(qt.value.data))
    ketQuaDuyet.value = ra
  } finally {
    ban.value = null
  }
}
const choDuyet = computed(() =>
  tri.value.filter((t) => t.data && ['DRAFT', 'NEEDS_REVIEW'].includes(t.data.trangThai)).length + (qt.value?.data?.trangThai === 'DRAFT' ? 1 : 0))

const tt = (s: string) => TRANG_THAI[s] ?? { ten: s, loai: 'chua' as const }
</script>

<template>
  <PhienBar :cong="cong" :nguoi="nguoi" />
  <section class="the">
    <h2>Bản nháp SOP{{ nhom ? ` — nhóm “${nhom}”` : '' }}</h2>
    <p class="mo">
      Đọc nội dung + dẫn chứng → sửa (nếu cần) → duyệt. <b>Lưu chỉnh sửa</b> và <b>Duyệt / xuất bản</b> là hai việc riêng:
      lưu sửa KHÔNG duyệt; duyệt mới làm nội dung được trả cho người làm thật qua <code>/hoi</code>. Mở màn này không ghi gì lên app.
    </p>
    <div class="hang">
      <label class="o">Người thao tác <span class="mo">(lời khai — console chưa xác thực danh tính)</span>
        <input v-model="nguoi" placeholder="tên của bạn" /></label>
      <button class="phu" :disabled="dangTai" @click="taiHet">{{ dangTai ? 'Đang tải…' : 'Tải lại từ app' }}</button>
      <span class="mo">app :{{ cong }}</span>
    </div>
    <p v-if="!triIds.length && !qtId" class="bao chu-y">Không có bản ghi nào trong đường dẫn. Mở màn này từ bước 3 của Giả lập (nút “Đọc · sửa · duyệt”).</p>
    <p v-if="soChuaLuu" class="bao chu-y">Còn <b>{{ soChuaLuu }}</b> chỉnh sửa chưa lưu.</p>
    <div v-if="loiPhien" class="bao chu-y">{{ loiPhien }}</div>
  </section>

  <!-- tri thức -->
  <section v-for="t in tri" :key="t.id" class="the">
    <div v-if="t.loi" class="bao loi"><code>{{ t.id }}</code> — {{ t.loi }}</div>
    <template v-else-if="t.data">
      <h2>
        {{ t.data.tenNguyenNhan }}
        <span class="tt" :class="tt(t.data.trangThai).loai">{{ tt(t.data.trangThai).ten }} · {{ t.data.trangThai }}</span>
      </h2>
      <div class="mo">
        Tri thức <code>{{ t.data.id }}</code> · phạm vi xem {{ t.data.phamViXem ?? 'chưa mở' }}
        · {{ hienHanh(t.data).length }} phát biểu đang hiệu lực, {{ hienHanh(t.data).filter((a) => a.danChung.some(laHoTro)).length }} có mẩu hỗ trợ
        · {{ t.data.assertion.length - hienHanh(t.data).length }} bản đã bị thay
      </div>
      <p v-if="t.data.canDuyetLai" class="bao chu-y">Cần duyệt lại: có phát biểu mới hơn lần duyệt cuối, mâu thuẫn, hoặc mẩu dẫn chứng đã bị rút (V3).</p>

      <div v-for="g in nhomTheoLoai(hienHanh(t.data))" :key="g.kind" class="phan">
        <div class="tieu">{{ g.ten }}</div>
        <PhatBieuDoc v-for="a in g.ds" :key="a.id" canh :cong="cong" :noi-dung="a.noiDung" :muc-kiem-chung="a.mucKiemChung"
                     :nguon-goc="a.nguonGoc" :dan-chung="a.danChung">
          <template #nhan>
            <span class="mo">ghi bởi {{ a.nguoiGhi ?? '—' }} · {{ gio(a.lucTao) }}</span>
          </template>
          <div v-if="sua[a.id]" class="sua">
            <textarea v-model="sua[a.id]!.noiDung" rows="3" />
            <label>Mức kiểm chứng
              <select v-model="sua[a.id]!.muc">
                <option v-for="(m, k) in MUC_KIEM_CHUNG" :key="k" :value="k">{{ m.ten }} ({{ k }})</option>
              </select>
            </label>
            <span class="mo">Giữ nguyên mức hiện tại trừ khi bạn có căn cứ — sửa câu chữ không làm phát biểu “được kiểm chứng” hơn.
              Bản sửa mang nguồn gốc “người đánh giá”, kế thừa dẫn chứng của bản cũ.</span>
            <button class="phu nho" @click="huySua(a.id)">Huỷ</button>
          </div>
          <div v-else class="nut-pb">
            <button class="phu nho" @click="batDauSua(a)">Sửa</button>
          </div>
          <details v-if="chuoiLichSu(t.data.assertion, a).length > 1" class="lich-su">
            <summary>Lịch sử: {{ chuoiLichSu(t.data.assertion, a).length }} bản — xem bản gốc</summary>
            <ol>
              <li v-for="(b, i) in chuoiLichSu(t.data.assertion, a)" :key="b.id">
                <b>{{ i === 0 ? 'Bản gốc' : b.conHieuLuc ? 'Đang hiệu lực' : `Bản sửa ${i}` }}</b>
                <span class="mo"> · {{ b.nguoiGhi ?? '—' }} · {{ gio(b.lucTao) }} · {{ MUC_KIEM_CHUNG[b.mucKiemChung]?.ten ?? b.mucKiemChung }} · {{ NGUON_GOC[b.nguonGoc] ?? b.nguonGoc }}</span>
                <div class="nd-cu">{{ b.noiDung }}</div>
              </li>
            </ol>
          </details>
        </PhatBieuDoc>
      </div>

      <div class="thao-tac">
        <button :disabled="!!ban || !doiCua(t.data).length || !nguoi.trim()" @click="luuSua(t.data)">
          {{ ban === `sua-${t.data.id}` ? 'Đang lưu…' : `Lưu chỉnh sửa (${doiCua(t.data).length})` }}
        </button>
        <span v-if="doiCua(t.data).length && !nguoi.trim()" class="mo">khai người thao tác để lưu</span>
        <button class="phu" :disabled="!!ban || lyDoChan.length > 0" @click="duyetMot(() => duyetTri(t.data!), `duyet-${t.data.id}`)">
          {{ ban === `duyet-${t.data.id}` ? 'Đang duyệt…' : 'Duyệt riêng bản ghi này' }}
        </button>
      </div>
      <div v-if="loiSua[t.data.id]" class="bao loi"><b>Không lưu được — host từ chối CẢ LÔ, không mục nào được ghi.</b><pre>{{ loiSua[t.data.id] }}</pre></div>
    </template>
    <p v-else class="mo">Đang tải <code>{{ t.id }}</code>…</p>
  </section>

  <!-- quy trình -->
  <section v-if="qt" class="the">
    <div v-if="qt.loi" class="bao loi">Quy trình <code>{{ qtId }}</code> — {{ qt.loi }}</div>
    <template v-else-if="qt.data">
      <h2>
        Quy trình: {{ qt.data.ten }} <span class="mo">v{{ qt.data.phienBan }}</span>
        <span class="tt" :class="tt(qt.data.trangThai).loai">{{ tt(qt.data.trangThai).ten }} · {{ qt.data.trangThai }}</span>
      </h2>
      <div class="mo">
        <code>{{ qt.data.id }}</code> · nhóm {{ qt.data.nhom }} · phạm vi xem {{ qt.data.phamViXem ?? 'chưa mở' }} · nguồn gốc
        {{ NGUON_GOC[qt.data.nguonGoc] ?? qt.data.nguonGoc }} · tạo bởi {{ qt.data.nguoiTao ?? '—' }}
      </div>
      <p class="mo">Bước quy trình không mang dẫn chứng riêng — đối chiếu bằng tri thức cùng chủ đề ở trên.</p>
      <div v-for="(c, i) in qt.data.canhBao" :key="i" class="bao chu-y">{{ c }}</div>

      <ol v-if="!buocSua" class="buoc">
        <li v-for="b in qt.data.buoc" :key="b.thuTu">
          <b>{{ b.ma }}</b> {{ b.noiDung }}
          <div class="mo">chủ đề <code>{{ b.chuDe }}</code>{{ b.noiXem ? ` · xem ở: ${b.noiXem}` : '' }}</div>
          <div class="mo">dấu hiệu xong: {{ b.dauHieu.length ? b.dauHieu.join(' · ') : 'chưa có — chỉ xong được bằng lời xác nhận' }}</div>
        </li>
      </ol>
      <div v-else class="sua-buoc">
        <p class="bao chu-y">⚠ Lưu sẽ <b>THAY toàn bộ bước</b> — host không giữ bản bước cũ. Console ghi bản trước khi thay vào lịch sử phiên.</p>
        <div v-for="(b, i) in buocSua" :key="i" class="mot-buoc">
          <b>{{ b.ma }}</b>
          <label>Nội dung <textarea v-model="b.noiDung" rows="2" /></label>
          <label>Xem ở <input v-model="b.noiXem" /></label>
          <label>Chủ đề <input v-model="b.chuDe" /></label>
          <label>Dấu hiệu xong (mỗi dòng một dấu hiệu) <textarea v-model="b.dauHieu" rows="2" /></label>
        </div>
      </div>
      <div class="thao-tac">
        <template v-if="qt.data.trangThai === 'DRAFT'">
          <button v-if="!buocSua" class="phu" @click="batDauSuaBuoc">Sửa bước</button>
          <template v-else>
            <button :disabled="!!ban || !buocDoi || !nguoi.trim()" @click="luuBuoc">{{ ban === 'buoc' ? 'Đang lưu…' : 'Lưu chỉnh sửa bước' }}</button>
            <button class="phu" @click="buocSua = null">Huỷ</button>
          </template>
        </template>
        <span v-else class="mo">Quy trình {{ qt.data.trangThai }} — không sửa trực tiếp được (N5-1). Muốn đổi thì tạo phiên bản mới.</span>
        <button class="phu" :disabled="!!ban || lyDoChan.length > 0" @click="duyetMot(() => duyetQt(qt!.data!), 'duyet-qt')">
          {{ ban === 'duyet-qt' ? 'Đang duyệt…' : 'Duyệt riêng quy trình' }}
        </button>
      </div>
      <div v-if="loiBuoc" class="bao loi"><pre>{{ loiBuoc }}</pre></div>
    </template>
  </section>

  <!-- duyệt -->
  <section v-if="tri.length || qt" class="the duyet">
    <h2>Duyệt / xuất bản</h2>
    <p class="mo">Duyệt LÀ xuất bản: nội dung bắt đầu được <code>/hoi</code> trả cho người làm thật, trong phạm vi xem bạn chọn. Tri thức và quy trình
      được duyệt TỪNG CÁI — một cái hỏng không làm cái kia hỏng theo, và màn này báo đúng từng cái.</p>
    <div class="hang">
      <label class="o">Phạm vi xem
        <select v-model="phamVi"><option value="" disabled>— chọn —</option><option v-for="p in PHAM_VI_XEM" :key="p.gia" :value="p.gia">{{ p.ten }}</option></select>
      </label>
      <label class="o rong">Lý do / ghi chú duyệt (không bắt buộc) <input v-model="lyDo" /></label>
    </div>
    <label class="tich"><input v-model="daDoc" type="checkbox" /> Tôi đã đọc nội dung và mở dẫn chứng của các đối tượng sẽ duyệt.</label>
    <label class="tich"><input v-model="xacNhanTM" type="checkbox" /> Tôi khai đã soát dữ liệu thương mại của khách trong các mẩu nguồn (giá, hạn
      hợp đồng, ý định rời bỏ) — <b>AR-n</b>. Đây là LỜI KHAI, hệ thống không kiểm được.</label>
    <ul v-if="lyDoChan.length" class="chan"><li v-for="r in lyDoChan" :key="r">{{ r }}</li></ul>
    <button :disabled="!!ban || lyDoChan.length > 0 || !choDuyet" @click="duyetTatCa">
      {{ ban === 'tat-ca' ? 'Đang duyệt…' : `Duyệt ${choDuyet} đối tượng đang chờ` }}
    </button>
    <table v-if="ketQuaDuyet.length">
      <thead><tr><th>Loại</th><th>Tên</th><th>Kết quả</th><th>Trạng thái sau</th><th>Lý do</th></tr></thead>
      <tbody>
        <tr v-for="(d, i) in ketQuaDuyet" :key="i" :class="{ xau: !d.ok }">
          <td>{{ d.loai }}</td><td>{{ d.ten }}</td><td>{{ d.ok ? 'đã duyệt' : 'HỎNG' }}</td>
          <td>{{ d.trangThaiSau ?? '—' }}</td><td><pre v-if="d.loi">{{ d.loi }}</pre></td>
        </tr>
      </tbody>
    </table>
    <p v-if="ketQuaDuyet.length" class="mo">
      {{ ketQuaDuyet.filter((d) => d.ok).length }}/{{ ketQuaDuyet.length }} đối tượng duyệt được. Trạng thái ở các thẻ phía trên đã được đọc lại từ app.
    </p>
  </section>

  <section v-if="tri.length || qt" class="the">
    <h2>Đánh giá bản nháp này</h2>
    <p class="mo">Đánh giá SOP của bạn — tách khỏi việc duyệt. “Không sửa gì” không có nghĩa là đã rà kỹ; ghi nhận xét để người sau biết.</p>
    <DanhGiaLuot :luot="luotBN ?? luotTam" @luu="luuDanhGia" />
  </section>
</template>

<style scoped>
.hang { display: flex; flex-wrap: wrap; gap: 12px; align-items: flex-end; margin: 8px 0; }
.o { display: inline-flex; flex-direction: column; font-size: 12px; font-weight: 600; gap: 4px; min-width: 0; }
.o.rong { flex: 1 1 280px; }
input, select, textarea { font: inherit; font-weight: 400; padding: 5px 8px; border: 1px solid var(--vien); border-radius: 6px; max-width: 100%; }
.tt { font-size: 11px; font-weight: 700; border-radius: 10px; padding: 1px 8px; margin-left: 6px; vertical-align: middle; }
.tt.chua { background: var(--vang-nhat); color: var(--vang); }
.tt.canh { background: var(--do-nhat); color: var(--do); }
.tt.xong { background: var(--xanh-nhat); color: var(--xanh); }
.tt.cu { background: #f0f2f5; color: #667085; }
.phan { margin-top: 10px; }
.tieu { font-size: 11px; font-weight: 700; color: var(--mo); text-transform: uppercase; letter-spacing: .04em; }
.sua { display: flex; flex-direction: column; gap: 4px; margin-top: 6px; }
.sua textarea { width: 100%; }
.sua label { font-size: 12px; font-weight: 600; display: flex; gap: 6px; align-items: center; }
.nut-pb { margin-top: 2px; }
button.nho { padding: 2px 9px; font-size: 12px; align-self: flex-start; }
.lich-su { font-size: 12px; margin-top: 4px; }
.lich-su ol { padding-left: 18px; margin: 4px 0; }
.nd-cu { white-space: pre-wrap; word-break: break-word; }
.thao-tac { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-top: 12px; }
.buoc { padding-left: 20px; }
.buoc li { margin: 6px 0; }
.sua-buoc .mot-buoc { border: 1px solid var(--vien); border-radius: 8px; padding: 6px 10px; margin: 6px 0; display: grid; gap: 4px; }
.sua-buoc label { display: flex; flex-direction: column; font-size: 12px; font-weight: 600; gap: 2px; }
.tich { display: block; font-size: 13px; margin: 6px 0; }
.chan { color: var(--vang); font-size: 13px; margin: 6px 0; }
table { width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 10px; }
th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid var(--vien); vertical-align: top; }
th { font-size: 11px; color: var(--mo); text-transform: uppercase; }
tr.xau td { color: var(--do); }
pre { white-space: pre-wrap; word-break: break-word; margin: 0; font-size: 12px; }
.bao pre { background: transparent; }
</style>
