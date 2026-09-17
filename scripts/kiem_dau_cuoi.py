#!/usr/bin/env python3
"""Gọi THẬT cả 15 endpoint của sản phẩm vào một app đang chạy, và nói ra chỗ nào hỏng.

    dotnet run --project src/KnowledgePlatform.Api      # cửa sổ khác
    python scripts/kiem_dau_cuoi.py

Mã thoát:  0 = mọi phép kiểm đúng  ·  1 = có chỗ sai  ·  2 = không gọi được app.

VÌ SAO FILE NÀY TỒN TẠI
-----------------------
`dotnet test` có 302 phép thử và chúng KHÔNG thấy được thứ file này thấy. Lý do không phải
chúng viết ẩu — mà vì **bộ test tự dựng payload của chính nó**. Mọi bên gửi THẬT đều nằm
ngoài tầm nhìn của trình biên dịch: hai script Python, một bộ Postman, và về sau là phần mềm
của khách. Siết một luật ở cửa vào thì trình biên dịch bắt được 27 chỗ gọi trong C# — và
không bắt được chỗ nào trong số đó.

Hai lần vấp THẬT trong hai ngày, cả hai chỉ lộ ra khi CHẠY:

  2026-09-16  `AR-l` bắt buộc khai mã khách sạn -> `load_fixture.py` chết 400 CẢ LÔ. Đó là
              đường DUY NHẤT dựng lại dữ liệu trên máy không có credential Jira. Không test
              nào đỏ, vì không test nào biết file fixture tồn tại.          (`IM-36`)
  2026-09-16  Luật che bên thứ ba dán dấu che lên ba thẻ VỐN TRỐNG, vì corpus thật viết
              xuống dòng bằng chuỗi thoát nguyên văn còn test viết xuống dòng thật. Tay
              người viết test không nghĩ ra hình dạng đó.                   (`IM-37`)

Và một con số để thấy khoảng trống: sản phẩm có **15 endpoint**, bộ Postman phủ **5**, tức
**8 endpoint chưa từng được gọi bởi bất cứ thứ gì ngoài bộ test C#** — gồm cả vòng tri thức.

⚠ HAI MỤC, HAI CÂU HỎI KHÁC NHAU — đừng gộp:
  A-E  "endpoint còn gọi được không?"  — payload do CHÍNH file này dựng, nên nó luôn khai
       đủ trường mới. Mục này sẽ xanh ngay cả khi mọi bên gửi khác của repo đã chết.
  F    "bên gửi THẬT của repo còn chạy được không?" — chạy chính `load_fixture.py`. Đây mới
       là mục bắt được `IM-36`, và nó phải tồn tại riêng vì A-E về bản chất không thấy được.

⚠ VÌ SAO KHÔNG DÙNG NEWMAN: bộ Postman cần `newman`, thứ KHÔNG có trên mọi máy (đã kiểm
2026-09-16: máy này không có, `npx --no-install` từ chối cài). Một phép kiểm chỉ chạy được
trên một số máy là một phép kiểm sẽ bị bỏ qua. File này chỉ dùng thư viện chuẩn của Python.

⚠ NÓ KHÔNG THAY BỘ TEST. Bộ test kiểm LUẬT (và kiểm được cả những nhánh không diễn đạt được
qua HTTP); file này kiểm HỢP ĐỒNG — rằng đường dây thật còn gọi được sau khi ai đó siết một
cửa vào. Hai việc khác nhau, và cái thứ hai là cái đã hỏng hai lần.

⚠ NÓ GHI DỮ LIỆU THẬT vào kho đang trỏ tới. Mọi khoá đều mang tiền tố `kiem-dau-cuoi:` nên
lọc ra được. Chạy trên kho dev/CI, đừng chạy vào kho của khách.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import urllib.error
import urllib.parse
import re
import urllib.request
import uuid

# Windows: stdout bi chuyen huong thi Python dat ve codepage ANSI (cp1252 tren may nay) va
# moi chu tieng Viet lam no nem UnicodeEncodeError - script chay dung khi in ra console, chet
# ngay khi `| head`. Ep UTF-8 o day, khong bat nguoi chay phai nho dat PYTHONIOENCODING.
for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

GOC = os.environ.get("APP_BASE", "http://localhost:5119").rstrip("/")
# `AR-e` 2026-09-16: khoá API theo TỪNG KHÁCH, thay khoá dùng chung cũ.
# Để trống khi máy dev bật Auth:AcknowledgeUnauthenticatedApi — giữ đường này chạy
# được trên máy không có gì cả, đúng bài học `IM-36`.
KHOA_API = os.environ.get("KP_API_KEY")
HERE = os.path.dirname(os.path.abspath(__file__))
BANG_DINH_TUYEN = os.path.join(
    HERE, "..", "docs", "ket-qua-phan-tich", "dinh-tuyen-150-case.json")

DAU = "kiem-dau-cuoi"
LAN = uuid.uuid4().hex[:10]

_sai: list[str] = []
_da_chay = 0
_da_tao = {"case": 0, "evidence": 0, "knowledge": 0}


# ---------------------------------------------------------------- hạ tầng gọi

_MAC_DINH = object()

# Mọi đường đã gọi trong lượt chạy này, dạng đã chuẩn hoá — dùng cho mục J.
_da_goi: set[tuple[str, str]] = set()


def _chuan_hoa(duong: str) -> str:
    """Đưa một đường dẫn thật về dạng route của hợp đồng: /knowledge/<guid> -> /knowledge/{id}."""
    d = duong.split("?")[0]
    d = re.sub(r"/kpl_[A-Za-z0-9_-]+", "/{token}", d)
    d = re.sub(r"/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
               "/{id}", d)
    d = re.sub(r"^/cases/[^/]+/evidence$", "/cases/{khoaNguon}/evidence", d)
    return d


def goi(method: str, duong: str, body=None, khoa=_MAC_DINH, them=None) -> tuple[int, object]:
    """`khoa`: bỏ trống = dùng KP_API_KEY · None = KHÔNG gửi khoá · chuỗi = gửi đúng chuỗi đó."""
    data = json.dumps(body).encode("utf-8") if body is not None else None
    req = urllib.request.Request(GOC + duong, data=data, method=method)
    if data:
        req.add_header("Content-Type", "application/json")
    _da_goi.add((method.upper(), _chuan_hoa(duong)))

    dung = KHOA_API if khoa is _MAC_DINH else khoa
    if dung:
        req.add_header("Authorization", "Bearer " + dung)
    for k, v in (them or {}).items():
        req.add_header(k, v)
    try:
        with urllib.request.urlopen(req, timeout=30) as r:
            raw = r.read().decode("utf-8")
            return r.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8")
        try:
            return e.code, json.loads(raw)
        except ValueError:
            return e.code, raw
    except urllib.error.URLError as e:
        print(f"\n🛑 KHÔNG GỌI ĐƯỢC {GOC} — {e.reason}")
        print("   App chưa chạy? `dotnet run --project src/KnowledgePlatform.Api`")
        sys.exit(2)


_doi_khoa: bool | None = None


def app_doi_khoa() -> bool:
    """
    App có THẬT SỰ đòi khoá API không — hỏi thẳng, hỏi một lần, nhớ lại.

    ⚠ Ba mục từng suy điều này từ biến môi trường `KP_API_KEY` và ĐỎ HÀNG LOẠT vì lý do không
    liên quan: máy dev bật `Auth:AcknowledgeUnauthenticatedApi` thì mọi endpoint mở toang.
    Suy ra một trạng thái thay vì hỏi nó là cùng hình dạng lỗi với phép kiểm chế độ shared ở
    `kiem_hai_khach.py`.
    """
    global _doi_khoa
    if _doi_khoa is None:
        ma, _ = goi("GET", "/internal/tenant-boundary", khoa=None)
        _doi_khoa = ma == 401
    return _doi_khoa


def kiem(nhan: str, that, mong) -> bool:
    """Một phép kiểm. In ra ngay, và nhớ chỗ sai để tổng kết ở cuối."""
    global _da_chay
    _da_chay += 1
    ok = that == mong
    if ok:
        print(f"  ok   {nhan}")
    else:
        _sai.append(nhan)
        print(f"  SAI  {nhan}: nhận {that!r}, mong {mong!r}")
    return ok


def muc(ten: str) -> None:
    print(f"\n── {ten} " + "─" * max(0, 62 - len(ten)))


def khoa(hau_to: str) -> str:
    return f"{DAU}:{LAN}:{hau_to}"


# ---------------------------------------------------------------- vật liệu

# Payload hoá đơn: HÌNH DẠNG thật (ES-346481), GIÁ TRỊ bịa hoàn toàn. Đừng chép giá trị thật
# vào đây — xem scripts/quet_credential_repo.py để biết vì sao dòng này phải có.
PAYLOAD_HOA_DON = (
    "NCC tra ve loi khi phat hanh:\n"
    "<NDHDon>\n"
    "  <NMua>\n"
    "    <Ten>Cong ty TNHH Mot Thanh Vien Khong Co That</Ten>\n"
    "    <MST>0100000001</MST>\n"
    "    <DChi>12 Duong Bia, Phuong Ao</DChi>\n"
    # The rong kieu CORPUS THAT: xuong dong la CHUOI THOAT nguyen van, khong phai ky tu
    # xuong dong. Day chinh la hinh dang da bat duoc loi cua IM-37 - dung bo no di.
    "    <SDThoai>\\r\\n    </SDThoai>\n"
    "    <STKNHang />\n"
    "  </NMua>\n"
    "  <TToan><TgTTTBSo>3240000</TgTTTBSo></TToan>\n"
    "</NDHDon>\n"
    "Ma loi: E0042"
)


def nhap_sop(nhom: str, ma_case: str) -> dict:
    """Bản nháp SOP tối thiểu mà `SopDraftSchema` nhận."""
    return {
        "nhom": nhom,
        "soCaseDungSau": 1,
        "trieuChungVao": [{"moTa": "Khong phat hanh duoc hoa don", "case": [ma_case]}],
        "buocKiem": [{
            "ma": "K1",
            "cauHoi": "O ky hieu co gia tri khong?",
            "noiXem": "Man phat hanh hoa don",
            "nhanh": [
                {"quanSat": "O trong nhung chon duoc", "diToi": "S1",
                 "case": [ma_case], "chungCu": "evidence-noi-ro"},
            ],
        }],
        "buocSua": [{"ma": "S1", "viec": "Chon ky hieu tren man phat hanh", "case": [ma_case]}],
        "phanBoBuocKiemDuocGhiLai": {
            "ghi-ro-buoc-kiem": {"so": 1, "case": [ma_case]},
            "suy-ra-duoc-nhung-khong-ghi": {"so": 0, "case": []},
            "chi-ghi-ket-luan": {"so": 0, "case": []},
            "buoc-kiem-ngoai-ticket": {"so": 0, "case": []},
            "khong-phai-chan-doan": {"so": 0, "case": []},
        },
    }


def ticket_co_nhom() -> tuple[str, str]:
    """Một mã ticket THẬT có nhóm trong bảng tra sẵn — không bịa mã.

    Đọc từ bảng thay vì ghim cứng: bảng này được đo lại (`docs/13`), và một mã ghim cứng sẽ
    lặng lẽ trở thành KHONG-BIET ở lần đo sau, làm phép kiểm đỏ vì lý do không liên quan.
    """
    with open(BANG_DINH_TUYEN, encoding="utf-8") as f:
        bang = json.load(f)
    for ma, kq in bang["ketQua"].items():
        if kq.get("nhom") and kq["nhom"] != "KHONG-BIET":
            return ma, kq["nhom"]
    raise SystemExit("Bảng định tuyến không có mã nào mang nhóm — kiểm lại docs/13.")


# ---------------------------------------------------------------- các phép kiểm

def suc_khoe() -> None:
    muc("A · Sức khoẻ  (3 endpoint)")
    s, b = goi("GET", "/health")
    kiem("GET /health", (s, (b or {}).get("status")), (200, "ok"))

    s, b = goi("GET", "/health/ready")
    kiem("GET /health/ready", (s, (b or {}).get("status")), (200, "ready"))

    s, b = goi("GET", "/internal/tenant-boundary")
    kiem("GET /internal/tenant-boundary", s, 200)
    # Không khẳng định con số: nó đếm bằng SQL thô không có điều kiện tenant, nên giá trị
    # phụ thuộc kho. Chỉ đòi hình dạng còn đúng.
    kiem("  ... có ô rowsVisibleWithoutTenantFilter",
         isinstance(b, dict) and "rowsVisibleWithoutTenantFilter" in b, True)


def duong_nap() -> tuple[str, str]:
    muc("B · Đường nạp  (2 endpoint)")
    ma_case = khoa("case")

    s, b = goi("POST", "/signals/case-observed",
               [{"sourceReference": ma_case, "subject": "Kiem dau cuoi - NCC tu choi payload"}])
    kiem("POST /signals/case-observed", (s, b["created"]), (200, 1))
    _da_tao["case"] += 1

    s, b = goi("POST", "/signals/case-observed",
               [{"sourceReference": ma_case, "subject": "Kiem dau cuoi - gui lai"}])
    kiem("  ... gửi lại thì KHÔNG sinh case trùng", (s, b["created"]), (200, 0))

    # AR-l: bỏ trống mã khách sạn là 400, KHÔNG âm thầm về "khong-xac-dinh".
    s, _ = goi("POST", "/signals/case-evidence", [{
        "caseSourceReference": ma_case, "sourceReference": khoa("ev-thieu-ma"),
        "content": "Thieu subTenantCode", "machineReadability": "Medium",
    }])
    kiem("POST /signals/case-evidence  thiếu subTenantCode -> 400 (AR-l)", s, 400)

    ma_ev = khoa("ev")
    s, b = goi("POST", "/signals/case-evidence", [
        {"caseSourceReference": ma_case, "sourceReference": ma_ev,
         "content": PAYLOAD_HOA_DON, "machineReadability": "High",
         "subTenantCode": "khong-xac-dinh"},
        {"caseSourceReference": ma_case, "sourceReference": khoa("ev-2"),
         "content": "Da kiem Nhat ky ket noi, thay ma loi E0042",
         "machineReadability": "Medium", "subTenantCode": "17468"},
    ])
    kiem("POST /signals/case-evidence  nạp 2 mẩu", (s, b["created"]), (200, 2))
    _da_tao["evidence"] += 2

    # AR-n: khối bên mua bị che, và con số phải nói ra.
    # 🛑 Payload có BỐN thẻ trong khối bên mua, nhưng chỉ BA thẻ có nội dung. Thẻ
    # <SDThoai> rỗng theo kiểu corpus thật (chuỗi thoát nguyên văn) và KHÔNG được tính.
    # Con số 4 ở đây nghĩa là luật lại dán dấu che lên một thẻ vốn trống — đúng lỗi IM-37.
    kiem("  ... AR-n che ĐÚNG 3 (thẻ rỗng kiểu corpus thật KHÔNG bị che)",
         b["thirdPartyRedactedTotal"], 3)
    # AR-l: chỉ 1 mã thật (17468) -> chưa đủ 2 giá trị phân biệt.
    kiem("  ... AR-l đếm 1 mã phân biệt", b["distinctSubTenantCodes"], 1)
    kiem("  ... AR-l có cảnh báo", isinstance(b["subTenantWarning"], str), True)

    # --- Tín hiệu loại THỨ HAI: đổi trạng thái (F1-1) ---
    s, b = goi("POST", "/signals/case-status",
               [{"sourceReference": ma_case, "daXongONguon": True}])
    kiem("POST /signals/case-status  báo đã xong", (s, b["daCapNhat"]), (200, 1))

    s, b = goi("POST", "/signals/case-status",
               [{"sourceReference": ma_case, "daXongONguon": True}])
    kiem("  ... gửi lại đúng trạng thái đang có -> khong-doi", b["results"][0]["trangThai"], "khong-doi")

    s, b = goi("POST", "/signals/case-status",
               [{"sourceReference": f"{DAU}:khong-bao-gio-ton-tai", "daXongONguon": False}])
    kiem("  ... case chưa tồn tại -> khong-thay-case, KHÔNG tự tạo",
         (b["khongThayCase"], b["daCapNhat"]), (1, 0))

    # Bỏ trống `daXongONguon` là 400: "chưa gửi" và "đã mở lại" phải phân biệt được.
    s, _ = goi("POST", "/signals/case-status", [{"sourceReference": ma_case}])
    kiem("  ... thiếu daXongONguon -> 400", s, 400)

    return ma_case, ma_ev


def duong_doc(ma_case: str) -> None:
    muc("C · Đường đọc  (2 endpoint)")
    s, b = goi("GET", f"/cases/{urllib.parse.quote(ma_case, safe='')}/evidence")
    kiem("GET /cases/{khoaNguon}/evidence", (s, b["soMau"]), (200, 2))
    kiem("  ... chưa có mẩu nào bị rút", b["soMauDaRutLai"], 0)

    het = json.dumps(b, ensure_ascii=False)
    kiem("  ... AR-n: định danh bên thứ ba KHÔNG đi ra",
         "Cong ty TNHH Mot Thanh Vien Khong Co That" in het, False)
    kiem("  ... AR-n: số tiền VẪN CÒN (nhóm 3 cần nó)", "3240000" in het, True)

    s, b = goi("GET", "/internal/sub-tenant-coverage")
    kiem("GET /internal/sub-tenant-coverage", s, 200)
    kiem("  ... có ô canTro (đừng đọc datDieuKienMoLai một mình)",
         isinstance(b, dict) and "canTro" in b, True)


def rut_lai(ma_ev: str) -> None:
    muc("D · AR-m · rút lại  (1 endpoint)")
    s, _ = goi("POST", "/evidence/rut-lai",
               {"khoaNguon": ma_ev, "nguoiDanhDau": "kiem-dau-cuoi", "lyDo": "Nguoi gui rut lai"})
    kiem("POST /evidence/rut-lai", s, 200)

    s, _ = goi("POST", "/evidence/rut-lai",
               {"khoaNguon": ma_ev, "nguoiDanhDau": "nguoi khac", "lyDo": "Ly do khac"})
    kiem("  ... rút lần hai -> 409, giữ lần đầu", s, 409)

    s, _ = goi("POST", "/evidence/rut-lai",
               {"khoaNguon": khoa("khong-ton-tai"), "nguoiDanhDau": "x", "lyDo": "y"})
    kiem("  ... khoá không tồn tại -> 404", s, 404)

    s, _ = goi("POST", "/evidence/rut-lai", {"khoaNguon": ma_ev, "lyDo": "thieu nguoi"})
    kiem("  ... thiếu nguoiDanhDau -> 400 (B1-1: chỉ NGƯỜI)", s, 400)


def vong_tri_thuc() -> str | None:
    muc("E · Vòng tri thức  (5 endpoint)")
    ma_ticket, nhom_that = ticket_co_nhom()
    nhom = f"{DAU}-{LAN}-{nhom_that}"   # nhóm RIÊNG mỗi lượt, để không đụng kho có sẵn
    ma_case = khoa("sop-case")

    s, b = goi("POST", "/knowledge/sop-draft", nhap_sop(nhom, ma_case))
    if not kiem("POST /knowledge/sop-draft", s, 200):
        print(f"       -> {json.dumps(b, ensure_ascii=False)[:400]}")
        return None
    ids = [r["id"] for r in b.get("nguyenNhan", [])] if isinstance(b, dict) else []
    _da_tao["knowledge"] += len(ids)
    kiem("  ... sinh ít nhất 1 record", len(ids) >= 1, True)
    if not ids:
        return None
    rid = ids[0]

    s, b = goi("POST", "/hoi", {"maTicket": ma_ticket})
    kiem("POST /hoi", s, 200)
    kiem("  ... trả 200 cho cả hai kết cục (không biết KHÔNG phải lỗi)",
         isinstance(b, dict) and "bietTraLoi" in b, True)

    # AR-n (B2b): duyệt mà chưa khai đã soát dữ liệu thương mại -> 400.
    s, _ = goi("POST", f"/knowledge/{rid}/approve",
               {"nguoiDuyet": "kiem-dau-cuoi", "phamViXem": "noi-bo"})
    kiem("POST /knowledge/{id}/approve  thiếu xác nhận -> 400 (AR-n)", s, 400)

    # AR-a: giá trị quyền xem ngoài tập đã chốt -> 400.
    s, _ = goi("POST", f"/knowledge/{rid}/approve",
               {"nguoiDuyet": "kiem-dau-cuoi", "phamViXem": "team:ops",
                "xacNhanDuLieuThuongMai": True})
    kiem("  ... phamViXem lạ -> 400 (AR-a)", s, 400)

    s, b = goi("POST", f"/knowledge/{rid}/approve",
               {"nguoiDuyet": "kiem-dau-cuoi", "phamViXem": "noi-bo",
                "xacNhanDuLieuThuongMai": True})
    kiem("  ... khai đủ -> 200", (s, b["trangThaiSau"]), (200, "ACTIVE"))

    s, b = goi("GET", f"/knowledge/{rid}")
    kiem("GET /knowledge/{id}", s, 200)
    ds = b.get("assertion", []) if isinstance(b, dict) else []
    so_truoc = len(ds)

    assertion_id = ds[0]["id"] if so_truoc > 0 else None
    if assertion_id:
        s, _ = goi("POST", f"/knowledge/{rid}/sua", {
            "nguoiSua": "kiem-dau-cuoi",
            "sua": [{"assertionId": assertion_id, "noiDungMoi": "ban sua cua nguoi",
                     "mucKiemChung": "Verified"}],
        })
        kiem("POST /knowledge/{id}/sua", s, 200)

        s, b = goi("GET", f"/knowledge/{rid}")
        kiem("  ... bản A KHÔNG bị đè (số phát biểu tăng)",
             len(b.get("assertion", [])) > so_truoc, True)
    else:
        kiem("GET /knowledge/{id} trả ra phát biểu để sửa được", False, True)

    return rid

def ben_gui_that() -> None:
    """Chạy chính các script NẠP của repo, không chỉ gọi endpoint bằng payload tự dựng.

    🛑 ĐÂY LÀ MỤC BẮT ĐƯỢC `IM-36`, và là lý do mục này tồn tại riêng. Phần trên kiểm hợp
    đồng bằng payload do CHÍNH FILE NÀY dựng — nên nó luôn khai đủ trường mới, và sẽ xanh
    ngay cả khi mọi bên gửi khác của repo đã chết. Ngày 2026-09-16 đúng chuyện đó đã xảy ra:
    `AR-l` thêm một trường bắt buộc, `load_fixture.py` nhận 400 CẢ LÔ, và không gì báo gì —
    302 test xanh, mọi endpoint xanh, chỉ con đường bàn giao là chết.
    → Cách duy nhất bắt được: CHẠY chính nó.

    ⚠ Chạy lại vô hại: cả hai đường nạp idempotent, nên lượt thứ hai trả `created 0`.
    """
    muc("F · Bên gửi THẬT của repo")

    fixture = os.path.join(HERE, "jira-export", "fixture-evidence.json")
    if not os.path.exists(fixture):
        print("  bỏ qua  load_fixture.py — chưa có fixture-*.json trên máy này.")
        print("          Dựng bằng: python scripts/jira-export/make_fixture.py")
        return

    r = subprocess.run(
        [sys.executable, os.path.join(HERE, "jira-export", "load_fixture.py")],
        capture_output=True, text=True, encoding="utf-8", errors="replace",
        env={**os.environ, "APP_BASE": GOC},
    )
    if not kiem("load_fixture.py chạy được (mã thoát 0)", r.returncode, 0):
        duoi = (r.stdout or "")[-500:] + (r.stderr or "")[-500:]
        print(f"       -> {duoi.strip()[:700]}")


def link_xem(rid: str | None) -> None:
    """
    `D5-3` — link xem tri thức, nửa "link" của `D3-1`.

    ⚠ Mục này cần `Link:PublicBaseUrl`. Thiếu thì NÓI RA và bỏ qua, không im lặng báo xanh.
    """
    muc("H · Link xem tri thức  (D5-3)")

    if rid is None:
        print("  ⚠ BỎ QUA — mục E chưa duyệt được record nào để phát link.")
        return

    s, b = goi("POST", f"/knowledge/{rid}/link", {"nguoiPhat": "kiem-dau-cuoi"})

    if s == 409 and isinstance(b, dict) and "PublicBaseUrl" in str(b.get("detail", "")):
        print("  ⚠ BỎ QUA — app chưa cấu hình Link:PublicBaseUrl.")
        print("    Chạy lại với Link__PublicBaseUrl=http://<tên máy trong LAN>:5119")
        print("    ⚠ Và đó KHÔNG phải lỗi: từ chối còn hơn đoán ra link localhost.")
        return

    if not kiem("POST /knowledge/{id}/link  -> 200", s, 200):
        return

    token = b.get("token", "")
    url = b.get("url", "")
    kiem("  ... token mang tiền tố kpl_", token.startswith("kpl_"), True)
    kiem("  ... url dựng đúng hình dạng", url.endswith(f"/xem/{token}"), True)

    # ⚠ Phép kiểm "không phải localhost" chỉ có nghĩa khi Link:PublicBaseUrl KHÁC địa chỉ đang
    #   gọi. Trùng nhau thì không cách nào phân biệt "đọc từ cấu hình" với "suy từ request" —
    #   nói ra thay vì báo một màu xanh không chứng minh được gì.
    if url.startswith(GOC):
        print("       (không phân biệt được cấu hình/suy-từ-request: PublicBaseUrl trùng địa chỉ gọi)")
    else:
        kiem("  ... và địa chỉ gốc đến từ CẤU HÌNH, không suy từ request", True, True)

    # Mở bằng link, KHÔNG gửi khoá API — chính token là giấy thông hành.
    s, b = goi("GET", f"/xem/{token}", khoa=None)
    kiem("GET /xem/{token} không cần khoá API -> 200", s, 200)
    kiem("  ... mở được và có nội dung", isinstance(b, dict) and b.get("moDuoc") is True, True)

    s, _ = goi("GET", "/xem/kpl_khongcothat_khongcothat", khoa=None)
    kiem("token bịa -> 404 (giống hệt hết hạn / đã thu hồi)", s, 404)

    if app_doi_khoa():
        s, _ = goi("POST", f"/knowledge/{rid}/link", {"hanNgay": 7}, khoa=None)
        kiem("phát link mà không có khoá API -> 401", s, 401)


def duong_gui_ve() -> None:
    """
    `D1-1` — đường TRẢ KẾT QUẢ VỀ. Ở đây chỉ kiểm hai endpoint CẤU HÌNH.

    ⚠ Việc gửi thật cần một bên nhận đang chạy, nên nó KHÔNG nằm trong file này — nó được
    giữ bằng bộ test (`WebhookGuiVeTests`, có bên nhận giả) cộng một lần chạy thật đã ghi
    ở `07` §3 `IM-40`. Nói ra chỗ này KHÔNG được kiểm còn hơn để người đọc tưởng là có.
    """
    muc("I · Đường trả kết quả về  (D1-1)")

    s, _ = goi("PUT", "/internal/webhook", {"url": "khong-phai-url"})
    kiem("PUT /internal/webhook  url rác -> 400", s, 400)

    dich = f"http://127.0.0.1:5199/nhan-{DAU}"
    s, b = goi("PUT", "/internal/webhook", {"url": dich})
    if not kiem("PUT /internal/webhook  url đúng -> 200", s, 200):
        return

    # Bí mật ký chỉ hiện ra lúc TẠO MỚI. Lần chạy sau (đã có dòng) thì nó là null — cả hai
    # đều đúng, nên phép kiểm hỏi đúng thứ hỏi được: có ô đó trong response hay không.
    kiem("  ... response có ô biMatKy", isinstance(b, dict) and "biMatKy" in b, True)

    s, b = goi("GET", "/internal/webhook")
    kiem("GET /internal/webhook -> 200", s, 200)
    kiem("  ... đọc lại KHÔNG trả bí mật ký", (b or {}).get("biMatKy"), None)

    if app_doi_khoa():
        s, _ = goi("GET", "/internal/webhook", khoa=None)
        kiem("đọc cấu hình mà không có khoá API -> 401", s, 401)


def soan_nhap() -> None:
    """
    Bước 4 — soạn nháp từ trong host. Trên máy dev KHÔNG có khoá model, nên đường đúng phải
    trả 409 kèm `chua-co-credit` và chỉ ra vòng thay thế.

    ⚠ Đây là phép kiểm cho HÀNH VI LÚC THIẾU, không phải cho lượt gọi thật. Lượt gọi thật
    chỉ chứng minh được khi có credit — nói ra thay vì để người đọc tưởng là đã kiểm.
    """
    muc("K · Soạn nháp từ trong host  (bước 4)")

    s, _ = goi("POST", "/knowledge/soan-nhap", {"nhom": "x"})
    kiem("thiếu nguoiYeuCau -> 400", s, 400)

    s, b = goi("POST", "/knowledge/soan-nhap",
               {"nhom": f"nhom-khong-ai-dinh-tuyen-{DAU}", "nguoiYeuCau": "kiem-dau-cuoi"})
    kiem("nhóm chưa có case -> 409", s, 409)
    kiem("  ... và nói rõ lý do", (b or {}).get("lyDo"), "nhom-khong-co-case")


def phu_hop_dong() -> None:
    """
    🛑 **Phép kiểm tự soi chính nó.** Đối chiếu những đường file này ĐÃ GỌI với hợp đồng
    `GET /openapi/v1.json` — thứ sinh thẳng từ code nên không lỗi thời được.

    Vì sao cần: `IM-38` đã ĐO được rằng một bộ kiểm tự nó lỗi thời trong im lặng (bộ Postman
    đứng ở 5/13 endpoint hơn hai tuần). Thêm một endpoint mà quên gọi nó ở đây thì mục này
    ĐỎ ngay — không còn phải trông vào việc ai đó nhớ ra.

    ⚠ Phải chạy CUỐI CÙNG: nó đọc những gì các mục trước đã gọi.
    """
    muc("J · Bộ kiểm này có còn phủ hết hợp đồng không")

    s, doc = goi("GET", "/openapi/v1.json", khoa=None)
    if not kiem("GET /openapi/v1.json -> 200", s, 200):
        return

    hop_dong = {
        (kieu.upper(), duong)
        for duong, muc_duong in (doc or {}).get("paths", {}).items()
        for kieu in muc_duong
    }

    kiem("hợp đồng liệt kê ít nhất 15 thao tác", len(hop_dong) >= 15, True)

    thieu = sorted(hop_dong - _da_goi)
    if thieu:
        for kieu, duong in thieu:
            print(f"       chưa gọi: {kieu} {duong}")

    kiem("mọi thao tác trong hợp đồng đều được file này gọi", len(thieu), 0)

    # Chiều ngược lại cũng đáng biết: gọi một đường KHÔNG có trong hợp đồng nghĩa là hợp
    # đồng thiếu, hoặc đường đó đã bị bỏ mà bộ kiểm chưa hay.
    thua = sorted(x for x in _da_goi - hop_dong if not x[1].startswith("/openapi"))
    if thua:
        for kieu, duong in thua:
            print(f"       gọi nhưng KHÔNG có trong hợp đồng: {kieu} {duong}")

    kiem("không gọi đường nào nằm ngoài hợp đồng", len(thua), 0)


def xac_thuc() -> None:
    """
    `AR-e` — cổng xác thực người gọi. Mục này KIỂM ĐƯỢC RẤT ÍT nếu app đang chạy ở chế độ
    thừa nhận không khoá, nên nó nói thẳng ra điều đó thay vì im lặng báo xanh: một phép
    kiểm xanh khi không có gì để kiểm là phép kiểm nguy hiểm (`doc_quyet_dinh.py` đã ghi
    đúng câu này, và nó đúng ở cả đây).
    """
    muc("G · Xác thực người gọi  (AR-e)")

    if not KHOA_API:
        print("  ⚠ BỎ QUA — chưa đặt KP_API_KEY.")
        print("    Mục này chỉ có nghĩa khi chạy:")
        print("      Auth__AcknowledgeUnauthenticatedApi=false  +  KP_API_KEY=kp_...")
        print("    ⚠ Đừng đọc phần còn lại của file này thành 'xác thực đã được kiểm'.")
        return

    # 🛑 HỎI APP, ĐỪNG SUY TỪ BIẾN MÔI TRƯỜNG.
    #
    # Bản đầu coi "có KP_API_KEY" là "app đang đòi khoá" — hai chuyện khác nhau. Máy dev bật
    # Auth:AcknowledgeUnauthenticatedApi thì mọi endpoint mở toang, và cả mục này ĐỎ HÀNG LOẠT
    # vì lý do không liên quan gì tới xác thực. Cùng hình dạng lỗi với phép kiểm chế độ shared
    # ở `kiem_hai_khach.py`: suy ra một trạng thái thay vì hỏi thẳng nó.
    if not app_doi_khoa():
        print("  ⚠ BỎ QUA — app đang bật cờ Auth:AcknowledgeUnauthenticatedApi (chạy KHÔNG cần khoá).")
        print("    Gọi không khoá vẫn ra 200, nên mọi phép kiểm ở mục này vô nghĩa.")
        print("    Chạy lại app với Auth__AcknowledgeUnauthenticatedApi=false thì mục này mới đo được.")
        return

    s, _ = goi("GET", "/internal/tenant-boundary", khoa=None)
    kiem("không gửi khoá -> 401", s, 401)

    s, _ = goi("GET", "/internal/tenant-boundary", khoa="kp_khongcothat_khongcothat")
    kiem("khoá bịa -> 401 (giống hệt ca thiếu khoá, không lộ khoá nào có thật)", s, 401)

    # Header CŨ đã chết. Kiểm nó cụ thể, vì "còn nhận header cũ" là kiểu hỏng im lặng nhất:
    # mọi thứ vẫn chạy, và lỗ hổng mà AR-e vừa vá thì vẫn mở qua đường cũ.
    s, _ = goi("GET", "/internal/tenant-boundary", khoa=None, them={"X-Tenant-Key": "dev-tenant"})
    kiem("X-Tenant-Key một mình -> 401 (header cũ KHÔNG còn mở được cửa)", s, 401)

    s, b = goi("GET", "/internal/tenant-boundary")
    kiem("khoá đúng -> 200", s, 200)
    kiem("  ... và trả đúng hình dạng cũ",
         isinstance(b, dict) and "rowsVisibleWithoutTenantFilter" in b, True)

    s, _ = goi("POST", "/signals/case-observed",
               [{"sourceReference": f"{DAU}:are-1", "subject": "kiểm AR-e"}], khoa=None)
    kiem("endpoint GHI không khoá -> 401", s, 401)


def main() -> int:
    print("=" * 68)
    print(f"KIỂM ĐẦU-CUỐI  ·  {GOC}  ·  lượt {LAN}")
    print("=" * 68)

    suc_khoe()
    ma_case, ma_ev = duong_nap()
    duong_doc(ma_case)
    rut_lai(ma_ev)
    rid = vong_tri_thuc()
    ben_gui_that()
    xac_thuc()
    link_xem(rid)
    duong_gui_ve()
    soan_nhap()
    phu_hop_dong()

    print("\n" + "=" * 68)
    print(f"{_da_chay} phép kiểm  ·  {len(_sai)} sai")
    print(f"Đã ghi vào kho: {_da_tao['case']} case · {_da_tao['evidence']} evidence · "
          f"{_da_tao['knowledge']} record tri thức  (tiền tố `{DAU}:`)")
    if _sai:
        print("\n🛑 CHỖ SAI:")
        for x in _sai:
            print(f"   · {x}")
        print("\n⚠ Sai ở đây KHÔNG có nghĩa bộ test đỏ. Hai thứ kiểm hai việc khác nhau — và")
        print("  đúng chỗ này là chỗ đã hỏng hai lần mà 302 test vẫn xanh (IM-36, IM-37).")
        return 1

    print("\n✅ Cả 19 endpoint còn gọi được, và các cổng còn chặn đúng chỗ.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
