using Xunit;

// =============================================================================
//  Các class test trong project này chạy TUẦN TỰ, không song song.
//
//  Vì sao: một test (`Ready_chuyen_sang_503_khi_RLS_bi_tat_tren_database_dang_chay`)
//  TẮT row-level security trên kp.assertion trong khoảng vài chục milli-giây để
//  chứng minh /health/ready biết báo 503. Trong khoảng đó, bất kỳ host nào khởi
//  động sẽ chạy StartupChecks → RlsGuard → thấy một bảng không có RLS → NÉM.
//
//  Nghĩa là mọi test tạo host mới trong khoảng đó đều đỏ, ngẫu nhiên, và thông
//  báo lỗi trỏ vào RLS chứ không trỏ vào nguyên nhân thật. Đã gặp: test
//  Gui_dung_khoa_thi_tao_duoc_case đỏ khi chạy cả bộ, xanh khi chạy một mình.
//
//  ⚠ Điều đáng chú ý là cơ chế bảo vệ hoạt động ĐÚNG — RlsGuard chặn một host
//    khởi động trên database có RLS bị tắt, đúng như IM-7 thiết kế. Cái sai là
//    bộ test tự tạo ra trạng thái đó rồi để test khác đi vào.
//
//  Hai cách sửa: cho test kia một database riêng, hoặc chạy tuần tự. Chọn tuần
//  tự vì cả bộ chỉ mất ~1 giây, và thêm một database thứ tư là thêm một thứ để
//  quên khi dựng máy mới.
// =============================================================================

[assembly: CollectionBehavior(DisableTestParallelization = true)]
