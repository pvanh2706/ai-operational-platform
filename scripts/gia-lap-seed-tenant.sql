-- Khách hàng của database giả lập (kp_gialap) — xem scripts/gia-lap-db-setup.sql.
-- Chạy lại vô hại.

INSERT INTO kp.tenant("Id", "Name", "ExternalKey", "CreatedAt")
VALUES (gen_random_uuid(), 'Khach hang gia lap', 'gia-lap', now())
ON CONFLICT ("ExternalKey") DO NOTHING;

SELECT "Id", "Name", "ExternalKey" FROM kp.tenant;
