import { createRouter, createWebHistory } from 'vue-router'
import KetNoi from './views/KetNoi.vue'

// Base '/console/' khớp vite.config.ts và đường ui_server.py phục vụ. ui_server trả index.html cho mọi đường
// con không phải file, nên tải lại trang ở /console/case/... vẫn ra đúng màn.
export const router = createRouter({
  history: createWebHistory('/console/'),
  routes: [
    { path: '/', name: 'ket-noi', component: KetNoi },
    // Bước 3 (P12-3): sơ đồ luồng · danh sách case · dòng chảy một case. Bước 4: giả lập.
  ],
})
