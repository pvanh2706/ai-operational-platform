<script setup lang="ts">
import { onMounted } from 'vue'
import { CAC_APP, useKetNoi } from './stores/ketNoi'

const ketNoi = useKetNoi()
onMounted(() => void ketNoi.kiemTra())
</script>

<template>
  <header class="dau">
    <div class="ten">Console nội bộ</div>
    <nav>
      <RouterLink :to="{ name: 'kich-ban' }">Kịch bản</RouterLink>
      <RouterLink :to="{ name: 'danh-sach' }">Case</RouterLink>
      <RouterLink :to="{ name: 'nap-hoi' }">Nạp &amp; hỏi</RouterLink>
      <RouterLink :to="{ name: 'gia-lap' }">Giả lập</RouterLink>
      <RouterLink :to="{ name: 'phan-mem-khach' }">Phần mềm khách</RouterLink>
      <RouterLink :to="{ name: 'phien' }">Phiên</RouterLink>
      <RouterLink :to="{ name: 'cau-hinh' }">Cấu hình AI</RouterLink>
      <RouterLink :to="{ name: 'ket-noi' }">Kết nối</RouterLink>
    </nav>
    <label class="chon-app">
      <span class="mo">App</span>
      <select v-model.number="ketNoi.cong">
        <option v-for="a in CAC_APP" :key="a.cong" :value="a.cong">{{ a.ten }} · :{{ a.cong }}</option>
      </select>
      <span class="den" :class="{ song: ketNoi.song === true, chet: ketNoi.song === false }" :title="ketNoi.loi ?? ''" />
    </label>
  </header>
  <main class="khung">
    <RouterView />
  </main>
</template>

<style scoped>
.dau {
  display: flex; align-items: center; gap: 20px; flex-wrap: wrap;
  padding: 10px 16px; background: var(--the); border-bottom: 1px solid var(--vien);
}
.ten { font-weight: 700; }
nav { display: flex; gap: 6px 14px; flex: 1; flex-wrap: wrap; }
nav a { text-decoration: none; color: var(--mo); font-weight: 600; }
nav a.router-link-exact-active { color: var(--chu); }
.chon-app { display: flex; align-items: center; gap: 8px; }
.chon-app select { font: inherit; padding: 4px 8px; border: 1px solid var(--vien); border-radius: 6px; }
.den { width: 10px; height: 10px; border-radius: 50%; background: var(--vien); }
.den.song { background: var(--xanh); }
.den.chet { background: var(--do); }
.khung { max-width: 1180px; margin: 0 auto; padding: 18px 16px 64px; }
</style>
