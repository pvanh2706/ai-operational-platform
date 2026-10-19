<script setup lang="ts">
// Danh sách case. Bộ lọc nằm trong QUERY STRING (?tienTo=&soLuong=) — bấm vào case rồi bấm Back là về đúng bộ lọc,
// và gửi link cho người khác là họ thấy đúng thứ mình thấy.
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api } from '../api'
import NhanNguon from '../components/NhanNguon.vue'
import { gio } from '../gio'
import type { CaseTrongDanhSach } from '../kieu'
import { useKetNoi } from '../stores/ketNoi'

const ketNoi = useKetNoi()
const route = useRoute()
const router = useRouter()

const tienTo = ref(String(route.query.tienTo ?? ''))
const soLuong = ref(Number(route.query.soLuong ?? 50) || 50)
const cases = ref<CaseTrongDanhSach[] | null>(null)
const loi = ref<string | null>(null)
const dangTai = ref(false)

async function tai() {
  dangTai.value = true
  loi.value = null
  try {
    cases.value = await api.danhSachCase(ketNoi.cong, tienTo.value.trim() || undefined, soLuong.value)
  } catch (e) {
    cases.value = null
    loi.value = e instanceof Error ? e.message : String(e)
  } finally {
    dangTai.value = false
  }
}

function loc() {
  void router.replace({ query: { ...(tienTo.value.trim() ? { tienTo: tienTo.value.trim() } : {}), soLuong: soLuong.value } })
}

watch(() => route.query, () => void tai())
watch(() => ketNoi.cong, () => void tai(), { immediate: true })

const tom = computed(() => {
  const ds = cases.value ?? []
  return {
    coNhom: ds.filter((c) => c.nhomHieuLuc).length,
    coGoiY: ds.filter((c) => c.soLuotGoiY > 0).length,
    dangChay: ds.filter((c) => c.coLuotChayMo).length,
  }
})
</script>

<template>
  <section class="the">
    <h2>Case · {{ ketNoi.app.ten }}</h2>
    <form class="loc" @submit.prevent="loc">
      <label>Tiền tố khoá nguồn
        <input v-model="tienTo" placeholder="gialap:  ·  jira:ES-  ·  để trống = mọi case" />
      </label>
      <label>Số dòng
        <select v-model.number="soLuong"><option :value="20">20</option><option :value="50">50</option><option :value="200">200 (trần)</option></select>
      </label>
      <button type="submit" :disabled="dangTai">{{ dangTai ? 'Đang tải…' : 'Lọc' }}</button>
    </form>
    <p class="mo">
      Nhóm là câu trả lời <code>/hoi</code> SẼ dùng, tính KHÔNG gọi model. Nhãn bên cạnh nói nó đến từ tầng nào.
    </p>

    <div v-if="loi" class="bao loi">{{ loi }}</div>
    <template v-else-if="cases">
      <p v-if="!cases.length" class="mo">Không có case nào khớp.</p>
      <template v-else>
        <div class="tom mo">
          {{ cases.length }} case · {{ tom.coNhom }} có nhóm · {{ tom.coGoiY }} đã được hỏi · {{ tom.dangChay }} đang chạy quy trình
        </div>
        <div class="cuon">
          <table>
            <thead>
              <tr><th>Khoá nguồn · tiêu đề</th><th>Nạp lúc</th><th class="so">Mẩu</th><th class="so">Gợi ý</th><th>Quy trình</th><th>Nhóm hiệu lực</th></tr>
            </thead>
            <tbody>
              <tr v-for="c in cases" :key="c.khoaNguon" @click="router.push({ name: 'case', params: { khoa: c.khoaNguon } })">
                <td><code>{{ c.khoaNguon }}</code><br />{{ c.tieuDe }}</td>
                <td class="mo">{{ gio(c.napLuc) }}</td>
                <td class="so">{{ c.soMau }}</td>
                <td class="so">{{ c.soLuotGoiY }}</td>
                <td>{{ c.coLuotChayMo ? 'đang chạy' : '—' }}</td>
                <td>
                  <span :class="{ mo: !c.nhomHieuLuc }">{{ c.nhomHieuLuc ?? 'không biết' }}</span><br />
                  <NhanNguon :nguon="c.nguonDinhTuyen" />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </template>
  </section>
</template>

<style scoped>
.loc { display: flex; gap: 12px; align-items: flex-end; flex-wrap: wrap; }
.loc label { display: flex; flex-direction: column; font-size: 12px; font-weight: 600; gap: 4px; }
.loc input { width: 340px; max-width: 100%; }
.loc input, .loc select { font: inherit; padding: 7px 9px; border: 1px solid var(--vien); border-radius: 6px; }
.tom { margin: 10px 0 0; }
.cuon { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 8px; }
th, td { text-align: left; padding: 7px 8px; border-bottom: 1px solid var(--vien); vertical-align: top; }
th { font-size: 11px; color: var(--mo); text-transform: uppercase; letter-spacing: .04em; }
tbody tr { cursor: pointer; }
tbody tr:hover { background: #f7f9fc; }
.so { text-align: right; font-variant-numeric: tabular-nums; }
</style>
