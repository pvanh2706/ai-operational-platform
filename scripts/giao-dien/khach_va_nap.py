"""Ba việc của nhóm Q (docs/18, 2026-10-22) phía ui_server — cùng server, cùng luật loopback:

    cấu hình AI       Q6-1  mỗi bước có AI chọn API/File (+ nhà cung cấp) — lưu file local, mọi màn đọc chung
    nạp một mã issue  Q4-1  mã đã có trong kho → mở; chưa có → kéo MỘT issue từ Jira bằng phiên Jira có sẵn
                            → check_corpus → /signals/*. Host vẫn KHÔNG gọi Jira.
    hộp nhận webhook  Q8-1  đóng vai phần mềm khách NHẬN kết quả gửi về — kiểm chữ ký HMAC như bên tích hợp phải kiểm

⚠ Thư viện CHUẨN, như phần còn lại của ui_server.
⚠ Không quyết định gì thay host: cấu hình AI chỉ nói console gọi đường NÀO (API hay xuất/nạp file); luật, trần, biên
  lai vẫn ở host. Định tuyến chọn nhà cung cấp ở HOST (DinhTuyen:NhaCungCap) — file này không đổi được nó.
"""
from __future__ import annotations

import hashlib
import hmac
import io
import json
import os
import re
import subprocess
import sys
import threading
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime, timezone

import console_web

HERE = os.path.dirname(os.path.abspath(__file__))
GOC_REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
JIRA_DIR = os.path.join(GOC_REPO, "scripts", "jira-export")

# ----------------------------------------------------------------- cấu hình AI (Q6-1)

# 🛑 File này KHÔNG chứa khoá (Q7-1: khoá ở appsettings.Local.json). Vẫn để .gitignore chặn: nó là lựa chọn của MỘT máy,
#   không phải của repo — và một file cấu hình được git theo dõi là chỗ đầu tiên người ta dán khoá vào "cho tiện".
FILE_CAU_HINH = os.path.join(HERE, "cau-hinh-ai.json")

BUOC_AI = ("dinhTuyen", "soanNhap", "hoiDap")
CHE_DO = ("api", "file")
NHA_CUNG_CAP = ("anthropic", "openai")

# Mặc định File cho mọi bước: không có gì tốn tiền cho tới khi có người chọn API (cùng tinh thần N7-1).
MAC_DINH = {
    "dinhTuyen": {"cheDo": "file"},
    "soanNhap": {"cheDo": "file", "nhaCungCap": "anthropic"},
    "hoiDap": {"cheDo": "file", "nhaCungCap": "anthropic"},
}


def doc_cau_hinh() -> dict:
    ra = json.loads(json.dumps(MAC_DINH))
    try:
        with open(FILE_CAU_HINH, encoding="utf-8") as f:
            luu = json.load(f)
    except (OSError, ValueError):
        return ra
    for buoc in BUOC_AI:
        v = luu.get(buoc) if isinstance(luu, dict) else None
        if not isinstance(v, dict):
            continue
        if v.get("cheDo") in CHE_DO:
            ra[buoc]["cheDo"] = v["cheDo"]
        if buoc != "dinhTuyen" and v.get("nhaCungCap") in NHA_CUNG_CAP:
            ra[buoc]["nhaCungCap"] = v["nhaCungCap"]
    return ra


def kiem_cau_hinh(moi: dict) -> str | None:
    """Trả câu lỗi, hoặc None. Sai một ô là từ chối CẢ lượt ghi — không ghi nửa vời."""
    if not isinstance(moi, dict):
        return "Cấu hình phải là một object."
    for buoc in BUOC_AI:
        v = moi.get(buoc)
        if not isinstance(v, dict) or v.get("cheDo") not in CHE_DO:
            return f"{buoc}.cheDo phải là một trong {CHE_DO}."
        if buoc != "dinhTuyen" and v.get("nhaCungCap") not in NHA_CUNG_CAP:
            return f"{buoc}.nhaCungCap phải là một trong {NHA_CUNG_CAP}."
        if buoc == "dinhTuyen" and "nhaCungCap" in v:
            return ("dinhTuyen KHÔNG chọn nhà cung cấp ở đây: định tuyến chạy tự động trên mọi webhook, nên nhà cung cấp là "
                    "cấu hình của host (DinhTuyen:NhaCungCap).")
    return None


def ghi_cau_hinh(moi: dict) -> None:
    if subprocess.run(["git", "check-ignore", "-q", FILE_CAU_HINH], cwd=GOC_REPO).returncode != 0:
        raise RuntimeError("cau-hinh-ai.json chưa được .gitignore chặn — không ghi. Thêm dòng "
                           "`scripts/giao-dien/cau-hinh-ai.json` vào .gitignore.")
    sach = {b: {k: moi[b][k] for k in ("cheDo", "nhaCungCap") if k in moi[b]} for b in BUOC_AI}
    with open(FILE_CAU_HINH, "w", encoding="utf-8") as f:
        json.dump(sach, f, ensure_ascii=False, indent=2)


# ----------------------------------------------------------------- gọi host


def goi_host(cong: int, method: str, duong: str, body=None, timeout: int = 60) -> tuple[int, object]:
    if cong not in console_web.CONG_CHO_PHEP:
        raise RuntimeError(f"Cổng {cong} không nằm trong KP_CONSOLE_CONG ({sorted(console_web.CONG_CHO_PHEP)}).")
    data = json.dumps(body, ensure_ascii=False).encode("utf-8") if body is not None else None
    req = urllib.request.Request(f"http://127.0.0.1:{cong}{duong}", data=data, method=method)
    if data is not None:
        req.add_header("Content-Type", "application/json; charset=utf-8")
    try:
        with urllib.request.urlopen(req, timeout=timeout) as r:
            raw = r.read().decode("utf-8", errors="replace")
            return r.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8", errors="replace")
        try:
            return e.code, json.loads(raw)
        except ValueError:
            return e.code, raw
    except urllib.error.URLError as e:
        raise RuntimeError(f"Không gọi được app ở cổng {cong} — {e.reason}. Chạy chay-thu.bat trước.") from e


def case_co_san(cong: int, ma: str) -> str | None:
    """Khoá nguồn của case mang mã này, nếu đã có — thử đúng dạng connector đặt (`jira:<mã>`) rồi mã trần."""
    for khoa in (f"jira:{ma}", ma):
        s, _ = goi_host(cong, "GET", f"/internal/cases/{urllib.parse.quote(khoa, safe='')}/dong-chay")
        if s == 200:
            return khoa
    return None


# ----------------------------------------------------------------- nạp một mã issue (Q4-1)

# Mã Jira: KHOÁ-DỰ-ÁN + số. Kiểm hình dạng TRƯỚC khi ghép vào JQL — một chuỗi tuỳ ý trong `key = "…"` là một JQL tuỳ ý.
HINH_MA_JIRA = re.compile(r"^[A-Z][A-Z0-9_]{0,19}-\d{1,9}$")


class _BatIn(io.TextIOBase):
    """Bắt `print` của module export để trả về giao diện (một issue thì vài dòng — không cần việc nền)."""

    def __init__(self):
        self.dong: list[str] = []

    def write(self, s: str) -> int:
        self.dong.extend(x.rstrip() for x in s.splitlines() if x.strip())
        return len(s)


def keo_mot_issue(X, phien: dict, ma: str, cong: int, cong_che_tat_tam: bool, bao_dam_git_bo_qua, duong_corpus) -> dict:
    """Kéo MỘT issue, chạy check_corpus, gửi vào host. Trả dict kết quả cho giao diện."""
    cfg = X.Config(jira_base=phien["base"].rstrip("/"), jira_auth_header=phien["auth"], jql=f'key = "{ma}"',
                   app_base=f"http://127.0.0.1:{cong}", signal_key=None, max_issues=1)

    nhan = "issue-" + ma.lower()
    bat = _BatIn()
    goc = sys.stdout
    sys.stdout = bat
    try:
        issues = X.fetch_issues(cfg)
        if not issues:
            return {"ok": False, "http": 404, "loi": f"Jira không có issue {ma} (hoặc tài khoản không xem được).", "nhatKy": bat.dong}
        cases, evidence = X.build_signals(cfg, issues)
        duong = {}
        for duoi, data in (("cases", cases), ("evidence", evidence)):
            duong[duoi] = duong_corpus(nhan, duoi)
            bao_dam_git_bo_qua(duong[duoi])
            with open(duong[duoi], "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
    finally:
        sys.stdout = goc

    kq = subprocess.run([sys.executable, os.path.join(JIRA_DIR, "check_corpus.py"), duong["cases"], duong["evidence"]],
                        cwd=JIRA_DIR, capture_output=True, text=True, encoding="utf-8",
                        env={**os.environ, "PYTHONIOENCODING": "utf-8"})
    bao_cao = (kq.stdout or "") + (kq.stderr or "")
    chan = "phát hiện CHẶN" in bao_cao

    if chan and not cong_che_tat_tam:
        # 🛑 Cùng cổng với nút "gửi vào kho" của trang quét (AR-j): đường nạp không che credential, cổng này là thứ duy
        #   nhất đứng giữa mật khẩu trong comment và database. Ngoại lệ duy nhất là cờ N1-2 (KP_CONG_CHE_TAT_TAM=1).
        return {"ok": False, "http": 409, "loi": "check_corpus báo CHẶN — issue có chỗ nghi là credential. Không gửi vào kho.",
                "baoCao": bao_cao, "nhatKy": bat.dong}

    sys.stdout = bat
    try:
        X.push(cfg, "/signals/case-observed", cases, "case")
        X.push(cfg, "/signals/case-evidence", evidence, "evidence")
    finally:
        sys.stdout = goc

    return {"ok": True, "daCo": False, "khoaNguon": cases[0]["sourceReference"] if cases else f"jira:{ma}",
            "soCase": len(cases), "soEvidence": len(evidence), "corpusChan": chan, "baoCao": bao_cao, "nhatKy": bat.dong}


# ----------------------------------------------------------------- hộp nhận webhook (Q8-1)

DUONG_NHAN = "/khach-gia-lap/webhook/"

# RAM: tắt ui_server là mất — đúng như mong muốn. Bí mật ký KHÔNG BAO GIỜ đi ra trình duyệt.
BI_MAT: dict[int, str] = {}
HOP_NHAN: list[dict] = []
_KHOA = threading.Lock()


def la_duong_nhan(duong: str) -> bool:
    return duong.startswith(DUONG_NHAN)


def nhan_webhook(h) -> None:
    """Đóng vai phần mềm khách: đọc thân NGUYÊN BYTE, kiểm `X-KP-Signature` như docs/19 §3.2 bắt bên tích hợp kiểm."""
    phan = urllib.parse.urlsplit(h.path).path.removeprefix(DUONG_NHAN)
    cong = int(phan) if phan.isdigit() else None

    n = int(h.headers.get("Content-Length") or 0)
    raw = h.rfile.read(n) if n else b""

    chu_ky = h.headers.get("X-KP-Signature") or ""
    bi_mat = BI_MAT.get(cong) if cong is not None else None
    if bi_mat is None:
        hop_le = None  # không giữ bí mật (ui_server khởi động lại sau khi đăng ký) — không kết luận được
    else:
        mong = "sha256=" + hmac.new(bi_mat.encode("utf-8"), raw, hashlib.sha256).hexdigest()
        hop_le = hmac.compare_digest(mong, chu_ky)

    try:
        than = json.loads(raw.decode("utf-8")) if raw else None
    except ValueError:
        than = raw.decode("utf-8", errors="replace")

    with _KHOA:
        HOP_NHAN.insert(0, {
            "luc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
            "cong": cong,
            "maGui": h.headers.get("X-KP-Delivery"),
            "chuKyHopLe": hop_le,
            "than": than,
        })
        del HOP_NHAN[200:]

    # 🛑 Chữ ký sai → 401, như một bên tích hợp đúng phải làm. Host sẽ thử lại theo lịch (docs/19 §3.3) — và người xem hộp
    #   nhận thấy NGAY lượt đó bị từ chối, thay vì thấy nó "đã nhận".
    ma = 401 if hop_le is False else 200
    h.send_response(ma)
    h.send_header("Content-Length", "0")
    h.end_headers()


def _ui(h):
    """Module ui_server ĐANG CHẠY — nơi Handler được định nghĩa.

    🛑 KHÔNG `import ui_server`: chạy bằng `python ui_server.py` thì module đó mang tên `__main__`, và import lại là nạp
      một BẢN THỨ HAI với PHIEN rỗng — phiên Jira vừa kết nối trông như chưa từng có.
    """
    return sys.modules[type(h).__module__]


class KhachVaNapApi:
    """Trộn vào Handler của ui_server — mỗi `api_*` là một nút trên console."""

    # -- cấu hình AI --

    def api_cau_hinh_ai_doc(self, t: dict) -> None:
        self._tra(200, {"ok": True, "cauHinh": doc_cau_hinh(), "coFile": os.path.isfile(FILE_CAU_HINH)})

    def api_cau_hinh_ai_ghi(self, t: dict) -> None:
        moi = t.get("cauHinh")
        if (loi := kiem_cau_hinh(moi)) is not None:
            self._tra(400, {"loi": loi})
            return
        ghi_cau_hinh(moi)
        self._tra(200, {"ok": True, "cauHinh": doc_cau_hinh()})

    # -- nạp một mã issue --

    def api_nap_issue(self, t: dict) -> None:
        U = _ui(self)

        ma = (t.get("ma") or "").strip()
        cong = int(t.get("cong") or 5120)
        if not ma:
            self._tra(400, {"loi": "Thiếu mã issue."})
            return

        if (khoa := case_co_san(cong, ma)) is not None:
            self._tra(200, {"ok": True, "daCo": True, "khoaNguon": khoa})
            return

        ma_jira = ma.upper()
        if not HINH_MA_JIRA.match(ma_jira):
            self._tra(404, {"loi": f"Không có case nào mang mã \"{ma}\" trong kho, và \"{ma}\" không có hình dạng mã Jira "
                                   "(KHOÁ-123) để kéo về."})
            return
        if not U.PHIEN["auth"]:
            self._tra(409, {"loi": f"Chưa có case {ma_jira} trong kho, và chưa kết nối Jira để kéo về. Kết nối Jira ở ô bên "
                                   "trên (phiên chỉ nằm trong RAM của ui_server)."})
            return

        kq = keo_mot_issue(U.X, U.PHIEN, ma_jira, cong, U.CONG_CHE_TAT_TAM, U.bao_dam_git_bo_qua, U.duong_corpus)
        self._tra(kq.pop("http", 200) if not kq["ok"] else 200, kq)

    # -- phần mềm khách: hộp nhận --

    def api_khach_dang_ky(self, t: dict) -> None:
        cong = int(t.get("cong") or 5120)
        url = f"http://127.0.0.1:{_ui(self).CONG}{DUONG_NHAN}{cong}"
        s, b = goi_host(cong, "PUT", "/internal/webhook", {"url": url})
        if s != 200 or not isinstance(b, dict):
            self._tra(s if s >= 400 else 502, {"loi": "Host không nhận khai báo webhook.", "http": s, "ketQua": b})
            return
        bi_mat = b.get("biMatKy")
        if bi_mat:
            BI_MAT[cong] = bi_mat
        # 🛑 Bí mật ở lại RAM ui_server — trình duyệt chỉ biết là CÓ giữ hay không.
        self._tra(200, {"ok": True, "url": b.get("url"), "active": b.get("active"), "giuBiMat": cong in BI_MAT})

    def api_khach_hop_nhan(self, t: dict) -> None:
        cong = t.get("cong")
        with _KHOA:
            ds = [x for x in HOP_NHAN if cong is None or x["cong"] == int(cong)]
        s, b = goi_host(int(cong or 5120), "GET", "/internal/webhook") if cong else (None, None)
        self._tra(200, {"ok": True, "goi": ds, "giuBiMat": (int(cong) in BI_MAT) if cong else None,
                        "dangKy": b if s == 200 else None})

    def api_khach_xoa_hop(self, t: dict) -> None:
        with _KHOA:
            HOP_NHAN.clear()
        self._tra(200, {"ok": True})
