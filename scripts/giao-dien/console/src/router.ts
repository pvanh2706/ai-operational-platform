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
    { path: '/gia-lap', name: 'gia-lap', component: () => import('./views/GiaLap.vue') },
    // Nhóm Q (2026-10-22): nạp theo mã/file + hỏi tự do · phần mềm khách gửi/nhận · cấu hình AI chung.
    { path: '/nap-hoi', name: 'nap-hoi', component: () => import('./views/NapHoi.vue') },
    { path: '/phan-mem-khach', name: 'phan-mem-khach', component: () => import('./views/PhanMemKhach.vue') },
    { path: '/cau-hinh', name: 'cau-hinh', component: () => import('./views/CauHinh.vue') },
    // Tự trải nghiệm (2026-10-23): đọc/sửa/duyệt bản nháp · lịch sử + tổng kết phiên · kịch bản thử có kết quả mong đợi.
    { path: '/ban-nhap', name: 'ban-nhap', component: () => import('./views/BanNhap.vue') },
    { path: '/phien', name: 'phien', component: () => import('./views/LichSuPhien.vue') },
    { path: '/kich-ban', name: 'kich-ban', component: () => import('./views/KichBan.vue') },
  ],
})
