<script setup lang="ts">
// Màn Cấu hình AI (Q6-1): MỘT chỗ chọn mỗi bước có AI đi API hay xuất/nạp File, và với API thì nhà cung cấp nào.
// Lưu ở ui_server (cau-hinh-ai.json, gitignore) — trang giả lập và màn Nạp & hỏi đọc chung.
//
// 🛑 Khoá KHÔNG nhập ở đây (Q7-1): chúng ở appsettings.Local.json / biến môi trường, và màn này chỉ biết CÓ hay KHÔNG.
// ⚠ Định tuyến chọn nhà cung cấp ở HOST (DinhTuyen:NhaCungCap) — nó chạy tự động trên mọi webhook, nên lựa chọn ở
//   console không chạm được nó. Màn này chỉ HIỆN.
import { computed, onMounted, ref, watch } from 'vue'
import ThuKhoa from '../components/ThuKhoa.vue'
import type { CauHinhAi, NhaCungCap } from '../kieu'
import { useCauHinhAi } from '../stores/cauHinhAi'
import { useKetNoi } from '../stores/ketNoi'

const ketNoi = useKetNoi()
const ai = useCauHinhAi()

const nhap = ref<CauHinhAi | null>(null)
const bao = ref<{ ok: boolean; text: string } | null>(null)
const dangGhi = ref(false)

async function tai() {
  await ai.tai(ketNoi.cong)
  nhap.value = ai.cauHinh ? JSON.parse(JSON.stringify(ai.cauHinh)) : null
}
onMounted(tai)
watch(() => ketNoi.cong, tai)

const doi = computed(() => JSON.stringify(nhap.value) !== JSON.stringify(ai.cauHinh))

async function ghi() {
  if (!nhap.value) return
  dangGhi.value = true
  bao.value = null
  try {
    await ai.ghi(nhap.value)
    bao.value = { ok: true, text: 'Đã lưu. Trang giả lập và màn Nạp & hỏi dùng cấu hình này từ lượt bấm kế tiếp.' }
  } catch (e) {
    bao.value = { ok: false, text: e instanceof Error ? e.message : String(e) }
  } finally {
    dangGhi.value = false
  }
}

const TEN: Record<NhaCungCap, string> = { anthropic: 'Anthropic', openai: 'OpenAI' }
const nhaCungCap = computed(() => (ai.trangThai ? [ai.trangThai.anthropic, ai.trangThai.openAI] : []))

const BUOC = [
  { khoa: 'soanNhap', ten: 'Soạn nháp SOP', moTa: 'luồng ngược — gom case một nhóm, model viết bản nháp quy trình (DRAFT)' },
  { khoa: 'hoiDap', ten: 'Hỏi đáp về issue / tài liệu', moTa: 'màn Nạp & hỏi — câu trả lời CHƯA DUYỆT, trỏ về mẩu nguồn' },
] as const
</script>

<template>
  <div v-if="ai.loi" class="bao loi">{{ ai.loi }}</div>

  <section class="the">
    <h2>Nhà cung cấp model — trên app :{{ ketNoi.cong }}</h2>
    <p class="mo">
      Khoá đặt ở <code>appsettings.Local.json</code> (gốc repo, đã .gitignore) hoặc biến môi trường — sửa xong phải
      <b>khởi động lại app</b>. Màn này không bao giờ thấy khoá. OpenAI còn cần <code>OpenAI:Model</code>: không có mặc định (Q2-1).
    </p>
    <div class="ncc">
      <div v-for="n in nhaCungCap" :key="n.ten" class="o-ncc">
        <div class="ten-ncc">{{ TEN[n.ten] }}</div>
        <div :class="n.coKhoa ? 'tot' : 'mo'">{{ n.coKhoa ? '✓ có khoá' : '○ không có khoá' }}</div>
        <div :class="n.model ? '' : 'xau'">model: <code>{{ n.model ?? 'CHƯA ĐẶT' }}</code></div>
        <div v-if="ai.hopLe[n.ten] === false" class="xau">✗ lần thử gần nhất bị TỪ CHỐI — API của nhà cung cấp này bị khoá</div>
        <ThuKhoa v-if="n.coKhoa" :cong="ketNoi.cong" :nha-cung-cap="n.ten" :model="n.model" @ket-qua="ai.sauThuKhoa" />
      </div>
    </div>
  </section>

  <section v-if="nhap && ai.trangThai" class="the">
    <h2>Mỗi bước có AI đi đường nào</h2>
    <p class="mo">
      <b>API</b>: app tự gọi model — tính tiền, có biên lai và trần tháng. <b>File</b>: xuất prompt ra để Claude Code / Codex
      đọc, rồi dán JSON kết quả vào — không tốn lượt nào của app.
    </p>

    <div class="buoc">
      <div class="ten-buoc">Định tuyến ticket mới <span class="mo">— luồng xuôi, trang giả lập bước 2</span></div>
      <label><input v-model="nhap.dinhTuyen.cheDo" type="radio" value="api" :disabled="!ai.apiChonDuoc('dinhTuyen')" /> API</label>
      <label><input v-model="nhap.dinhTuyen.cheDo" type="radio" value="file" /> File</label>
      <div class="mo">
        Nhà cung cấp: <b>{{ TEN[ai.trangThai.dinhTuyen.nhaCungCap] }}</b> — cấu hình HOST (<code>DinhTuyen:NhaCungCap</code>), vì
        định tuyến chạy tự động trên mọi webhook. <code>DinhTuyen:GoiModel</code>:
        <b :class="ai.trangThai.dinhTuyenGoiModel ? 'tot' : 'xau'">{{ ai.trangThai.dinhTuyenGoiModel ? 'bật' : 'tắt — API trả KHÔNG BIẾT cho ticket mới' }}</b>
        · ngưỡng {{ ai.trangThai.dinhTuyenNguongTinCay }} (chưa đo) · trần {{ ai.trangThai.dinhTuyen.tranThang }} lượt/tháng
      </div>
    </div>

    <div v-for="b in BUOC" :key="b.khoa" class="buoc">
      <div class="ten-buoc">{{ b.ten }} <span class="mo">— {{ b.moTa }}</span></div>
      <label><input v-model="nhap[b.khoa].cheDo" type="radio" value="api" /> API</label>
      <label><input v-model="nhap[b.khoa].cheDo" type="radio" value="file" /> File</label>
      <template v-if="nhap[b.khoa].cheDo === 'api'">
        <span class="mo">qua</span>
        <label v-for="n in (['anthropic', 'openai'] as const)" :key="n">
          <input v-model="nhap[b.khoa].nhaCungCap" type="radio" :value="n" /> {{ TEN[n] }}
        </label>
        <div v-if="!ai.apiChonDuoc(b.khoa, nhap[b.khoa].nhaCungCap)" class="bao chu-y">
          {{ TEN[nhap[b.khoa].nhaCungCap] }} chưa dùng được trên app này — bước này sẽ chạy bằng <b>File</b> cho tới khi có khoá
          (và model, với OpenAI).
        </div>
      </template>
      <div class="mo">trần {{ ai.trangThai[b.khoa].tranThang }} lượt/tháng</div>
    </div>

    <button :disabled="!doi || dangGhi" @click="ghi">{{ dangGhi ? 'Đang lưu…' : doi ? 'Lưu cấu hình' : 'Đã lưu' }}</button>
    <div v-if="bao" class="bao" :class="bao.ok ? 'ok' : 'loi'">{{ bao.text }}</div>
  </section>
</template>

<style scoped>
.ncc { display: flex; gap: 14px; flex-wrap: wrap; }
.o-ncc { flex: 1 1 320px; border: 1px solid var(--vien); border-radius: 8px; padding: 10px 14px; font-size: 13px; min-width: 0; }
.ten-ncc { font-weight: 700; font-size: 15px; margin-bottom: 4px; }
.buoc { border-top: 1px solid var(--vien); padding: 10px 0; font-size: 14px; }
.buoc label { margin-right: 14px; }
.ten-buoc { font-weight: 600; margin-bottom: 4px; }
.tot { color: var(--xanh); } .xau { color: var(--do); }
section button { margin-top: 10px; }
</style>
