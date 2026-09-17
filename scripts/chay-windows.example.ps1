# =============================================================================
#  Bản MẪU để chạy sản phẩm trên một máy Windows nội bộ (H1-5, H1b-1).
#
#  CHÉP RA rồi sửa — ĐỪNG sửa thẳng file này: nó được git theo dõi, và mật khẩu
#  database không được nằm trong repo (docs/10 §0, và sự cố 0e602c3 đã xảy ra một lần).
#
#      Copy-Item scripts\chay-windows.example.ps1 scripts\chay-windows.ps1
#
#  `scripts/chay-windows.ps1` đã nằm trong .gitignore.
#
#  ⚠ Nguồn sự thật về cấu hình KHÔNG phải file này, mà là:
#      dotnet run --project src/KnowledgePlatform.Api -- --kiem-cau-hinh
#    Chạy nó SAU khi sửa file này, TRƯỚC khi gọi là xong.
# =============================================================================

$ErrorActionPreference = 'Stop'

# --- Database ------------------------------------------------------------------
# AR-i/C2-1: cấu hình đến từ BIẾN MÔI TRƯỜNG, không từ file được git theo dõi.
# ⚠ Role kp_app KHÔNG được là superuser — superuser đi vòng qua row-level security.
$env:ConnectionStrings__Default = 'Host=localhost;Port=5432;Database=kp;Username=kp_app;Password=DOI-CAI-NAY'

# --- Chế độ deploy -------------------------------------------------------------
# G13: không có mặc định, phải khai tường minh.
$env:Tenancy__Mode               = 'DedicatedSingleTenant'
$env:Tenancy__TenantExternalKey  = 'ezcloud'

# --- Xác thực ------------------------------------------------------------------
# ⚠ ĐỪNG bật Auth__AcknowledgeUnauthenticatedApi ở máy phục vụ người thật.
#   Bỏ trống = API đòi khoá cho mọi endpoint. Đó là mặc định ĐÚNG.
#   Tạo khoá: dotnet run --project src/KnowledgePlatform.Api -- --tao-khoa ezcloud "connector Jira"

# --- Địa chỉ link --------------------------------------------------------------
# 🛑 ĐÂY LÀ CÁI BẪY NGUY NHẤT CỦA BẢN DEPLOY LOCAL.
#
#   Link phát ra mang địa chỉ này. Đặt 'localhost' thì link mở được khi BẠN tự bấm trên
#   máy chủ, và CHẾT khi gửi cho người support bấm từ máy của họ — một thất bại chỉ lộ ra
#   với người KHÔNG đi kiểm nó.
#
#   Đặt bằng tên máy trong LAN/VPN. Kiểm bằng cách mở link TỪ MỘT MÁY KHÁC.
$env:Link__PublicBaseUrl = 'http://DOI-THANH-TEN-MAY-TRONG-LAN:5119'

# --- Đường trả kết quả về ------------------------------------------------------
$env:Webhook__ChuKyGiay  = '15'
# $env:Webhook__BatVongQuet = 'false'   # bật dòng này nếu CHƯA muốn gửi ra ngoài

# --- Cổng nghe -----------------------------------------------------------------
# ⚠ ĐỪNG dùng 8765: trên máy dev hiện tại IIS đang giữ cổng đó.
$env:ASPNETCORE_URLS = 'http://0.0.0.0:5119'

# =============================================================================

$duAn = Join-Path $PSScriptRoot '..\src\KnowledgePlatform.Api'

Write-Host 'Soi cấu hình trước khi chạy...' -ForegroundColor Cyan
dotnet run --project $duAn --no-launch-profile -- --kiem-cau-hinh

Write-Host ''
Write-Host 'Khởi động...' -ForegroundColor Cyan
dotnet run --project $duAn --no-launch-profile
