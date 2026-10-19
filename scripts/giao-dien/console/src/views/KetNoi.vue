<script setup lang="ts">
// Màn của bước 2 (P12-2): chứng minh ĐƯỜNG ỐNG — trình duyệt → ui_server → host — chạy được với cả hai đường
// đọc mới. Các màn thật (sơ đồ, danh sách, dòng chảy) là bước 3.
import { ref, watch } from 'vue'
import { api } from '../api'
import type { CaseTrongDanhSach } from '../kieu'
import { useKetNoi } from '../stores/ketNoi'

const ketNoi = useKetNoi()
const cases = ref<CaseTrongDanhSach[] | null>(null)
const loi = ref<string | null>(null)

async function taiCase() {
  cases.value = null
  loi.value = null
  try {
    cases.value = await api.danhSachCase(ketNoi.cong, undefined, 10)
  } catch (e) {
    loi.value = e instanceof Error ? e.message : String(e)
  }
}

watch(() => ketNoi.song, (s) => s && void taiCase(), { immediate: true })
</script>

<template>
  <section class="the">
    <h2>{{ ketNoi.app.ten }} · cổng {{ ketNoi.cong }}</h2>
    <p class="mo">{{ ketNoi.app.moTa }}</p>
    <div v-if="ketNoi.song === null" class="bao chu-y">Đang kiểm…</div>
    <div v-else-if="ketNoi.song" class="bao ok">
      App trả lời <code>/health/ready</code> · hợp đồng có <b>{{ ketNoi.soThaoTac }}</b> thao tác.
    </div>
    <div v-else class="bao loi">
      Không nối được: {{ ketNoi.loi }}<br />
      Cổng 5120 chạy bằng <code>scripts\giao-dien\run-gia-lap-app.bat</code>; cổng 5119 bằng
      <code>dotnet run --project src/KnowledgePlatform.Api</code>.
    </div>
    <button class="phu" style="margin-top: 10px" @click="ketNoi.kiemTra()">Kiểm lại</button>
  </section>

  <section v-if="ketNoi.song" class="the">
    <h2>10 case mới nạp gần nhất</h2>
    <div v-if="loi" class="bao loi">{{ loi }}</div>
    <p v-else-if="cases && !cases.length" class="mo">Kho chưa có case nào.</p>
    <table v-else-if="cases">
      <thead>
        <tr><th>Khoá nguồn</th><th>Tiêu đề</th><th>Mẩu</th><th>Nhóm hiệu lực · nguồn</th></tr>
      </thead>
      <tbody>
        <tr v-for="c in cases" :key="c.khoaNguon">
          <td><code>{{ c.khoaNguon }}</code></td>
          <td>{{ c.tieuDe }}</td>
          <td>{{ c.soMau }}</td>
          <td>
            {{ c.nhomHieuLuc ?? 'không biết' }}<br />
            <span class="mo">{{ c.nguonDinhTuyen }}</span>
          </td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<style scoped>
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid var(--vien); vertical-align: top; }
th { font-size: 11px; color: var(--mo); text-transform: uppercase; letter-spacing: .04em; }
</style>
