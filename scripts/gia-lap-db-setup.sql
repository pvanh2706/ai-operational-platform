-- Database RIÊNG cho trang giả lập hai luồng — `O6-1` (docs/18 nhóm O, 2026-10-19).
-- Chạy bằng SUPERUSER, MỘT lần cho mỗi máy (cần role kp_app của dev-db-setup.sql có sẵn):
--
--   psql -U postgres -h localhost -f scripts/gia-lap-db-setup.sql
--
-- Rồi tạo schema + khách hàng (không cần superuser):
--
--   dotnet ef database update --project src/KnowledgePlatform.Infrastructure ^
--     --startup-project src/KnowledgePlatform.Infrastructure ^
--     --connection "Host=localhost;Port=5432;Database=kp_gialap;Username=kp_app;Password=123456"
--   psql -U kp_app -h localhost -d kp_gialap -f scripts/gia-lap-seed-tenant.sql
--
-- 🛑 VÌ SAO KHÔNG DÙNG kp_dev: host chỉ đọc MỘT taxonomy (19 nhóm hoá đơn thật), nên ticket
--   giả buộc phải rơi vào nhóm THẬT. Duyệt một quy trình giả cho nhóm X sẽ đẩy quy trình ACTIVE
--   của nhóm X trong kp_dev sang DEPRECATED (N6-1), và SOP giả sẽ trả lời /hoi cho ticket
--   fixture thật cùng nhóm. Không crash, không đỏ test nào.
--
-- Làm lại từ đầu: DROP DATABASE kp_gialap; rồi chạy lại cả ba lệnh trên.

CREATE DATABASE kp_gialap OWNER kp_app;
