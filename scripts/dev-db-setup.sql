-- Dựng database cho môi trường dev/test trên máy local.
-- Chạy bằng SUPERUSER (thường là postgres):
--
--   psql -U postgres -h localhost -f scripts/dev-db-setup.sql
--
-- ⚠ VÌ SAO PHẢI CÓ ROLE RIÊNG, KHÔNG DÙNG LUÔN `postgres`:
--   Superuser ĐI VÒNG QUA row-level security, kể cả khi bảng có
--   FORCE ROW LEVEL SECURITY. Đã kiểm thật: `postgres` đọc được dữ liệu của
--   MỌI tenant trong cùng một câu SELECT.
--   → App và test chạy bằng `postgres` thì RLS bằng KHÔNG, và mọi test cách ly
--     tenant sẽ PASS GIẢ. Đây là kiểu thất bại im lặng mà cả `G7` lẫn `IM-5`
--     đang cố chặn.
--   Test `Role_chay_test_phai_bi_RLS_rang_buoc` kiểm đúng điều này và sẽ FAIL
--   nếu ai đó trỏ test vào một role superuser.

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'kp_app') THEN
    CREATE ROLE kp_app LOGIN PASSWORD '123456'
      NOSUPERUSER NOCREATEROLE NOCREATEDB NOBYPASSRLS;
  END IF;
END $$;

-- Mật khẩu ở trên chỉ dành cho máy dev. Deploy thật lấy từ secret store,
-- truyền vào qua biến môi trường (xem TestDatabase.ConnectionString).

-- `kp_app` là CHỦ SỞ HỮU database và các bảng — cố ý. FORCE ROW LEVEL SECURITY
-- trong migration đầu tiên (`IM-5`) tồn tại chính để chủ sở hữu KHÔNG được miễn
-- policy. Test chạy bằng chủ sở hữu nên nó kiểm đúng ca khó nhất.
CREATE DATABASE kp_dev      OWNER kp_app;   -- chạy app lúc phát triển
CREATE DATABASE kp_test     OWNER kp_app;   -- test tầng Infrastructure
CREATE DATABASE kp_api_test OWNER kp_app;   -- test tích hợp tầng API

-- Ba database riêng, không dùng chung: một test trong bộ Infrastructure TẮT RLS
-- tạm thời để kiểm RlsGuard biết ném. Dùng chung DB thì test đó làm test khác
-- đỏ ngẫu nhiên khi hai bộ chạy song song.
