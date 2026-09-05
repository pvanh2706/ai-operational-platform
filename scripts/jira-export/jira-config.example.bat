@echo off
rem ================================================================
rem  MAU CAU HINH - dung dien vao file nay.
rem  run-export.bat se tu chep no thanh jira-config.bat (file do da
rem  nam trong .gitignore, nen PAT cua ban khong bao gio bi commit).
rem
rem  Ghi chu: file .bat phai la tieng Viet KHONG DAU - cmd.exe doc
rem  file theo codepage ANSI, chu co dau se lam hong ca file.
rem ================================================================

rem --- BAT BUOC: ba dong nay ---
set "JIRA_BASE_URL=https://jira.cua-cong-ty.vn"
set "JIRA_PAT=dan-personal-access-token-vao-day"

rem JQL duoi day la BAN DA SUA sau khi quet corpus that ngay 2026-09-04.
rem Ly do tung menh de: docs/07_MVP_IMPLEMENTATION.md muc R-K4 va AR-h.
rem
rem  · resolutiondate THAY createdDate: lan chay dau dung createdDate >= startOfMonth()
rem    nen cat dung truoc han SLA - 25/32 case chua ai kip tra loi.
rem  · 12 thang chu khong phai 4 ngay: uoc ~140 case da dong, nam gon trong n=50-200.
rem  · loai Duplicate / Cannot Reproduce: dong vi khong tai lap duoc thi khong mang
rem    nguyen nhan de hoc.
rem  · summary !~ CLONE: bat cap trung byte ngay o nguon, truoc khi ton cong dedup.
rem  · EP MOT CHU DE moi lan chay: corpus cu trai tren 8 dong san pham, gom xuyen
rem    8 san pham la tron nghiep vu khac nhau.
rem  · PHAI viet CA HAI kieu bo dau: trong Postgres "hoa" va "hoa" (co dau khac nhau)
rem    la hai token khac nhau. Do that: tim "khoa tu" ra 5 tieu de, kieu dau kia ra 1,
rem    hai tap KHONG giao nhau.
rem  · KHONG loc theo issuetype: truong nay bi sua SAU khi dong case (ES-346405 ghi
rem    "Team chuyen type thanh SR"), nen cung mot JQL chay hai luc ra hai tap khac nhau.
rem
rem CANH BAO: "da dong" KHONG phai tin hieu tot de chon case - do duoc tren corpus that
rem la 7/7 case da dong chi ghi "het trieu chung", con nguyen nhan that nam o case
rem CON MO. Sau khi lay ve phai loc them o tang nap. Xem 00_CURRENT_STATE muc 2026-09-04.
rem
rem GHI LAI THOI DIEM CHAY JQL, khong chi ghi cau JQL.
rem
rem ⚠ DUNG NGAY TUONG DOI (-365d), KHONG dung ngay cung. Ban truoc ghi cung
rem    resolutiondate < "2026-09-01" va no CHAN MAT chinh bang chung then chot cua
rem    du an: ES-346396 dong luc 2026-09-01T23:14 nen roi ra ngoai cua so, cung 4/10
rem    case cua nhom SOP lon nhat. Mot cua so cung se lang le nghe di theo thoi gian,
rem    va cai no bo mat luon la phan MOI NHAT.

set "JIRA_JQL=project = ES AND resolved >= -365d AND resolution not in (Duplicate, "Cannot Reproduce", "Won't Do", Incomplete) AND summary !~ "CLONE" AND (summary ~ "hóa đơn" OR summary ~ "hoá đơn" OR summary ~ "HĐĐT" OR summary ~ "invoice" OR summary ~ "VNPT") ORDER BY resolutiondate DESC"

rem --- Doi menh de summary de lay chu de khac. MOI CHU DE MOT CORPUS RIENG ---
rem khoa tu:  (summary ~ "khóa từ" OR summary ~ "khoá từ" OR summary ~ "thẻ từ")
rem bao cao:  (summary ~ "báo cáo" OR summary ~ "BAR0" OR summary ~ "HKR0" OR summary ~ "doanh thu")

rem --- Jira ban cu chua co PAT? Xoa dong JIRA_PAT o tren va dung hai dong nay ---
rem set "JIRA_USER=ten-dang-nhap"
rem set "JIRA_PASS=mat-khau"

rem --- Tuy chon, mac dinh da dung cho may dev ---
set "APP_BASE_URL=http://localhost:5119"
set "APP_SIGNAL_KEY="
rem MAX_ISSUES: tran so issue keo ve. 0 = KHONG TRAN.
rem ⚠ DE 150, KHONG de 0. Do that tren project ES: chu de hoa don co ~2723 case da
rem dong trong 12 thang, nen MAX_ISSUES=0 keo ve ~2723 case = ~7000 request len Jira
rem production va mat ~2 gio, thay vi ~6 phut. §8.2 noi n=50-200 la du cho phep dem.
rem De 0 chi khi that su can toan bo, va biet minh dang lam gi.
set "MAX_ISSUES=150"
