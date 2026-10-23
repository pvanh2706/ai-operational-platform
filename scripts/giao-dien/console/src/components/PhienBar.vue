<script setup lang="ts">
// Mở đầu một phiên trải nghiệm: đang dùng DỮ LIỆU nào (app, tenant), CHẾ ĐỘ AI nào, ghi vào PHIÊN nào.
// Chỉ đọc khi mở — tạo phiên là một cú bấm; lượt đầu tiên mà chưa chọn phiên thì store tự tạo một phiên mặc định.
import { computed, ref, watch } from 'vue'
import { useCauHinhAi } from '../stores/cauHinhAi'
import { usePhien } from '../stores/phien'

const props = defineProps<{ cong: number; nguoi?: string }>()
const phien = usePhien()
const ai = useCauHinhAi()

const s = computed(() => phien.o(props.cong))
const moTao = ref(false)
const ten = ref('')
const nguoiMoi = ref(props.nguoi ?? '')
const loi = ref<string | null>(null)

watch(() => props.cong, (c) => {
  void phien.tai(c, true)
  // Màn nào không tự tải cấu hình AI (Bản nháp, Lịch sử) thì thanh này tải — chỉ đọc, không gọi model.
  if (!ai.cauHinh || !ai.trangThai) void ai.tai(c)
}, { immediate: true })

async function chon(e: Event) {
  loi.value = null
  try {
    await phien.chon(props.cong, (e.target as HTMLSelectElement).value || null)
  } catch (x) {
    loi.value = x instanceof Error ? x.message : String(x)
  }
}

async function tao() {
  loi.value = null
  try {
    await phien.taoMoi(props.cong, ten.value, nguoiMoi.value)
    moTao.value = false
    ten.value = ''
  } catch (x) {
    loi.value = x instanceof Error ? x.message : String(x)
  }
}

const cheDo = (b: 'dinhTuyen' | 'soanNhap' | 'hoiDap') =>
  ai.cauHinh ? (ai.cheDoHieuLuc[b] === 'api' ? `API${b === 'dinhTuyen' ? '' : ` · ${ai.cauHinh[b].nhaCungCap}`}` : 'File') : '—'
</script>

<template>
  <div class="phien-bar">
    <div class="ngu-canh">
      <span class="nhan-mo">Dữ liệu</span>
      <template v-if="s.nguCanh">
        <b>{{ s.nguCanh.app }}</b> :{{ cong }} · {{ s.nguCanh.moTa }} ·
        tenant <code :title="s.nguCanh.tenantId ?? ''">{{ s.nguCanh.tenantId?.slice(0, 8) ?? 'không xác định' }}</code>
        <span class="mo">({{ s.nguCanh.cheDoTenant ?? 'app không trả lời' }})</span>
      </template>
      <span v-else class="mo">đang xác định…</span>
      <span class="nhan-mo">AI</span>
      <span class="mo">định tuyến {{ cheDo('dinhTuyen') }} · soạn nháp {{ cheDo('soanNhap') }} · hỏi đáp {{ cheDo('hoiDap') }}</span>
    </div>
    <div class="chon">
      <span class="nhan-mo">Phiên</span>
      <select :value="s.hienTai?.id ?? ''" @change="chon">
        <option value="">— chưa chọn (lượt đầu sẽ tạo phiên mới) —</option>
        <option v-for="p in s.ds" :key="p.id" :value="p.id">{{ p.ten }} · {{ p.soLuot }} lượt</option>
      </select>
      <button class="phu nho" @click="moTao = !moTao">Phiên mới</button>
      <RouterLink :to="{ name: 'phien', query: { cong: String(cong) } }">Lịch sử &amp; tổng kết →</RouterLink>
    </div>
    <div v-if="moTao" class="tao">
      <label>Tên phiên <input v-model="ten" placeholder="vd. Thử nhóm hoá đơn — sáng 23/10" /></label>
      <label>Người thao tác (lời khai) <input v-model="nguoiMoi" /></label>
      <button class="nho" @click="tao">Tạo</button>
    </div>
    <div v-if="loi || s.loi" class="bao loi">{{ loi ?? s.loi }}</div>
    <div class="mo nho-chu">Lịch sử lưu ở ui_server (<code>scripts/giao-dien/phien-trai-nghiem/</code>, git bỏ qua), tách theo cổng + tenant —
      chung cho mọi trình duyệt trên máy này; xoá ở màn Lịch sử. Người thao tác là lời khai, chưa xác thực danh tính.</div>
  </div>
</template>

<style scoped>
.phien-bar { border: 1px solid var(--vien); border-radius: 10px; background: #fbfcfd; padding: 8px 12px; margin-bottom: 14px; font-size: 13px; }
.ngu-canh, .chon, .tao { display: flex; flex-wrap: wrap; gap: 6px 10px; align-items: center; }
.chon { margin-top: 6px; }
.tao { margin-top: 6px; }
.tao label { display: inline-flex; flex-direction: column; font-size: 12px; font-weight: 600; }
input, select { font: inherit; padding: 4px 8px; border: 1px solid var(--vien); border-radius: 6px; max-width: 100%; }
select { min-width: 0; max-width: 340px; }
.nhan-mo { font-size: 11px; font-weight: 700; color: var(--mo); text-transform: uppercase; letter-spacing: .04em; }
button.nho { padding: 3px 9px; font-size: 12px; }
.nho-chu { font-size: 11px; margin-top: 4px; }
</style>
