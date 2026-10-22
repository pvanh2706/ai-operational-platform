<script setup lang="ts">
// Phần mềm khách (Q8-1): console đóng vai phần mềm của khách.
//   GỬI  — soạn một tín hiệu, gửi thẳng vào /signals/* hoặc /hoi của app đang chọn, xem phản hồi TRẢ NGAY (đồng bộ)
//   NHẬN — ui_server đăng ký một đường nhận webhook với host; kết quả host GỬI VỀ sau đó hiện ở hộp nhận, kèm kết quả
//          kiểm chữ ký X-KP-Signature (docs/19 §3.2) như một bên tích hợp đúng phải làm
// ⚠ Gửi đi bằng đúng HTTP sản phẩm có (qua /app/<cổng>) — không lối tắt nào, nên thấy gì ở đây là sản phẩm làm được.
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { apiUi, type GoiNhan } from '../apiUi'
import { useKetNoi } from '../stores/ketNoi'

const ketNoi = useKetNoi()

// ------------------------------------------------------------- gửi

const ma = ref(moi())
function moi() {
  return `khach-gia-lap:${new Date().toISOString().slice(0, 10)}-${Math.random().toString(36).slice(2, 7)}`
}

const MAU = computed(() => ({
  'case-observed': {
    ten: 'Có việc mới',
    duong: '/signals/case-observed',
    than: [{ sourceReference: ma.value, subject: 'Không phát hành được hoá đơn — màn hình báo E0042' }],
  },
  'case-evidence': {
    ten: 'Thêm một mẩu evidence',
    duong: '/signals/case-evidence',
    than: [{
      sourceReference: `${ma.value}#m1`, caseSourceReference: ma.value,
      content: 'Khách gửi ảnh chụp: E0042 khi bấm Phát hành, ký hiệu 1C26TAA', subTenantCode: 'khong-xac-dinh',
    }],
  },
  'case-status': {
    ten: 'Việc ở nguồn đổi trạng thái',
    duong: '/signals/case-status',
    than: [{ sourceReference: ma.value, daXongONguon: true, sourceResolvedAt: new Date().toISOString() }],
  },
  hoi: { ten: 'Hỏi: ticket này nên làm gì', duong: '/hoi', than: { maTicket: ma.value } },
}))
type LoaiMau = keyof typeof MAU.value

const loai = ref<LoaiMau>('case-observed')
const than = ref('')
const duong = ref('')
function napMau() {
  const m = MAU.value[loai.value]
  duong.value = m.duong
  than.value = JSON.stringify(m.than, null, 2)
}
watch([loai, ma], napMau, { immediate: true })

const lich = ref<{ luc: string; duong: string; http: number; ms: number; than: string; phanHoi: string }[]>([])
const dangGui = ref(false)
const loiGui = ref<string | null>(null)

async function gui() {
  loiGui.value = null
  let data: string
  try {
    data = JSON.stringify(JSON.parse(than.value))
  } catch (e) {
    loiGui.value = `Thân không phải JSON: ${e instanceof Error ? e.message : e}`
    return
  }
  dangGui.value = true
  const bd = performance.now()
  try {
    const r = await fetch(`/app/${ketNoi.cong}${duong.value}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json', Accept: 'application/json' }, body: data,
    })
    const raw = await r.text()
    let dep = raw
    try { dep = JSON.stringify(JSON.parse(raw), null, 2) } catch { /* giữ nguyên văn */ }
    lich.value.unshift({ luc: new Date().toLocaleTimeString('vi'), duong: duong.value, http: r.status, ms: Math.round(performance.now() - bd), than: data, phanHoi: dep })
  } catch (e) {
    loiGui.value = e instanceof Error ? e.message : String(e)
  } finally {
    dangGui.value = false
  }
}

// ------------------------------------------------------------- nhận

const goi = ref<GoiNhan[]>([])
const giuBiMat = ref<boolean | null>(null)
const dangKy = ref<{ url: string; active: boolean } | null>(null)
const baoDk = ref<{ ok: boolean; text: string } | null>(null)
const tuDong = ref(true)
let hen: ReturnType<typeof setInterval> | undefined

const laDuongCuaUi = computed(() => !!dangKy.value?.url?.includes('/khach-gia-lap/webhook/'))

async function taiHop() {
  const k = await apiUi.khachHopNhan(ketNoi.cong)
  if (!k.ok) return
  goi.value = k.goi
  giuBiMat.value = k.giuBiMat
  dangKy.value = k.dangKy
}

async function dangKyHop() {
  baoDk.value = null
  const k = await apiUi.khachDangKy(ketNoi.cong)
  baoDk.value = k.ok
    ? { ok: true, text: `Đã đăng ký ${k.url}. Bí mật ký nằm trong RAM của ui_server — trang này không thấy nó.` }
    : { ok: false, text: k.loi ?? `HTTP ${k.http}` }
  await taiHop()
}

async function xoaHop() {
  await apiUi.khachXoaHop()
  await taiHop()
}

function batTuDong(bat: boolean) {
  clearInterval(hen)
  if (bat) hen = setInterval(() => void taiHop(), 3000)
}
watch(tuDong, batTuDong)
watch(() => ketNoi.cong, () => void taiHop())
onMounted(() => { void taiHop(); batTuDong(tuDong.value) })
onUnmounted(() => clearInterval(hen))

const moGoi = ref<number | null>(null)
const suKien = (g: GoiNhan) => (g.than && typeof g.than === 'object' && 'suKien' in g.than ? String((g.than as Record<string, unknown>).suKien) : '—')
const khoaCase = (g: GoiNhan) => {
  const c = g.than && typeof g.than === 'object' ? (g.than as { case?: { khoaNguon?: string } }).case : undefined
  return c?.khoaNguon ?? '—'
}
</script>

<template>
  <section class="the">
    <h2>Gửi tín hiệu vào app :{{ ketNoi.cong }} — như phần mềm của khách</h2>
    <p class="mo">Mã ticket của phiên: <code>{{ ma }}</code> <button class="phu nho" @click="ma = moi()">mã mới</button>
      — gửi "Có việc mới" trước, rồi thêm evidence / hỏi / đổi trạng thái trên CÙNG mã.</p>
    <div class="mau-nut">
      <button v-for="(m, k) in MAU" :key="k" :class="{ phu: loai !== k }" @click="loai = k as LoaiMau">{{ m.ten }}</button>
    </div>
    <div class="hang">
      <code class="duong">POST {{ duong }}</code>
      <button class="phu nho" @click="napMau">đặt lại mẫu</button>
    </div>
    <textarea v-model="than" rows="9" spellcheck="false" />
    <button :disabled="dangGui" @click="gui">{{ dangGui ? 'Đang gửi…' : 'Gửi' }}</button>
    <div v-if="loiGui" class="bao loi">{{ loiGui }}</div>

    <div v-for="(l, i) in lich" :key="i" class="phan-hoi">
      <div class="dong-dau">
        <span>{{ l.luc }}</span> <code>{{ l.duong }}</code>
        <b :class="l.http < 300 ? 'tot' : l.http < 500 ? 'vang' : 'xau'">HTTP {{ l.http }}</b> <span class="mo">{{ l.ms }} ms</span>
      </div>
      <pre>{{ l.phanHoi }}</pre>
    </div>
  </section>

  <section class="the">
    <h2>Hộp nhận — kết quả host GỬI VỀ</h2>
    <p class="mo">
      Host gửi kết quả về qua webhook (hàng đợi bền, vòng quét ~15 giây, ký HMAC). ui_server đăng ký đường
      <code>/khach-gia-lap/webhook/{{ ketNoi.cong }}</code> làm đích, kiểm chữ ký trên NGUYÊN BYTE, và trả 401 khi chữ ký sai — như
      bên tích hợp phải làm.
    </p>
    <div v-if="ketNoi.cong === 5119" class="bao chu-y">
      ⚠ App :5119 là app dev (kp_dev, khách dev-tenant). Mỗi khách có MỘT đích gửi — đăng ký ở đây là THAY đích hiện có của
      dev-tenant.
    </div>
    <div class="hang">
      <button @click="dangKyHop">Đăng ký hộp nhận với app :{{ ketNoi.cong }}</button>
      <label class="mo"><input v-model="tuDong" type="checkbox" /> tự tải lại mỗi 3 giây</label>
      <button class="phu" @click="taiHop">Tải lại</button>
      <button class="phu" @click="xoaHop">Xoá hộp</button>
    </div>
    <div v-if="baoDk" class="bao" :class="baoDk.ok ? 'ok' : 'loi'">{{ baoDk.text }}</div>
    <div class="mo trang-thai">
      Đích đang khai với host: <code>{{ dangKy?.url ?? 'chưa có' }}</code>
      <span v-if="dangKy && !laDuongCuaUi" class="xau"> — KHÔNG phải hộp nhận này; host gửi đi nơi khác</span>
      · bí mật ký: <b :class="giuBiMat ? 'tot' : 'vang'">{{ giuBiMat ? 'ui_server đang giữ' : 'không giữ — đăng ký lại để kiểm được chữ ký' }}</b>
    </div>

    <table v-if="goi.length">
      <thead><tr><th>Lúc</th><th>Sự kiện</th><th>Case</th><th>Chữ ký</th><th /></tr></thead>
      <tbody>
        <template v-for="(g, i) in goi" :key="g.maGui ?? i">
          <tr>
            <td>{{ new Date(g.luc).toLocaleTimeString('vi') }}</td>
            <td><code>{{ suKien(g) }}</code></td>
            <td><code>{{ khoaCase(g) }}</code></td>
            <td :class="g.chuKyHopLe === true ? 'tot' : g.chuKyHopLe === false ? 'xau' : 'vang'">
              {{ g.chuKyHopLe === true ? '✓ hợp lệ' : g.chuKyHopLe === false ? '✗ SAI — đã trả 401' : '? không kiểm được' }}
            </td>
            <td><button class="phu nho" @click="moGoi = moGoi === i ? null : i">{{ moGoi === i ? 'đóng' : 'xem gói' }}</button></td>
          </tr>
          <tr v-if="moGoi === i"><td colspan="5"><pre>{{ JSON.stringify(g.than, null, 2) }}</pre></td></tr>
        </template>
      </tbody>
    </table>
    <p v-else class="mo">Chưa nhận gói nào. Đăng ký hộp nhận, rồi gửi "Có việc mới" ở trên — gói về sau vài giây.</p>
  </section>
</template>

<style scoped>
.mau-nut { display: flex; gap: 8px; flex-wrap: wrap; margin: 8px 0; }
.hang { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; margin: 8px 0; }
.duong { font-size: 13px; }
textarea { display: block; width: 100%; font: 12.5px ui-monospace, Consolas, monospace; padding: 8px; border: 1px solid var(--vien); border-radius: 6px; }
section > button { margin-top: 8px; }
button.nho { padding: 2px 8px; font-size: 11px; }
.phan-hoi { margin-top: 12px; }
.dong-dau { font-size: 13px; display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
pre { background: #0f1419; color: #d7dce2; padding: 8px 10px; border-radius: 6px; font-size: 12px; white-space: pre-wrap; word-break: break-word; max-height: 360px; overflow: auto; margin: 4px 0; }
.trang-thai { margin: 6px 0; }
table { width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 10px; }
th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid var(--vien); vertical-align: top; }
th { font-size: 11px; color: var(--mo); text-transform: uppercase; }
.tot { color: var(--xanh); } .xau { color: var(--do); } .vang { color: var(--vang); }
</style>
