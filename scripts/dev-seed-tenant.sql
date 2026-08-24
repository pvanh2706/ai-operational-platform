-- Tạo công ty khách hàng cho máy dev. Chạy SAU khi đã apply migration:
--
--   psql -U kp_app -h localhost -d kp_dev -f scripts/dev-seed-tenant.sql
--
-- ExternalKey phải khớp Tenancy:TenantExternalKey trong
-- src/KnowledgePlatform.Api/appsettings.Development.json — nếu lệch thì app TỪ CHỐI
-- KHỞI ĐỘNG kèm thông báo chỉ rõ khoá nào không tìm thấy. Đó là hành vi cố ý: một
-- bản deploy riêng không tìm thấy khách hàng của mình thì thà không chạy còn hơn
-- chạy bình thường mà không thấy dữ liệu nào.
--
-- kp.tenant là bảng DUY NHẤT không có row-level security — nó là danh bạ, không
-- phải dữ liệu của một khách hàng nào (xem 07 §3 IM-14). Nên chèn được mà không
-- cần đặt app.current_tenant.

INSERT INTO kp.tenant("Id", "Name", "ExternalKey", "CreatedAt")
VALUES (gen_random_uuid(), 'Khach hang dev', 'dev-tenant', now())
ON CONFLICT ("ExternalKey") DO NOTHING;

SELECT "Id", "Name", "ExternalKey" FROM kp.tenant;
