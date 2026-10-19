import { createRouter, createWebHistory } from 'vue-router'
import DanhSach from './views/DanhSach.vue'
import KetNoi from './views/KetNoi.vue'

// Base '/console/' khớp vite.config.ts và đường ui_server.py phục vụ. ui_server trả index.html cho mọi đường
// con không phải file, nên tải lại trang ở /console/case/... vẫn ra đúng màn.
export const router = createRouter({
  history: createWebHistory('/console/'),
  routes: [
    { path: '/', name: 'danh-sach', component: DanhSach },
    // Khoá nguồn chứa `:` `/` `#` — vue-router mã hoá param, và ui_server không giải mã đường /console/.
    { path: '/case/:khoa', name: 'case', component: () => import('./views/DongChay.vue') },
    { path: '/ket-noi', name: 'ket-noi', component: KetNoi },
    // Bước 4 (P12-4): giả lập.
  ],
})
