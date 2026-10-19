"""Phục vụ console Vue (bản build) và chuyển tiếp lời gọi của nó sang host — `P3-1` (docs/18 nhóm P, 2026-10-19).

    /console, /console/...      file tĩnh trong console/dist; đường con không phải file → index.html (router SPA)
    /app/<cổng>/<đường>?<q>     → http://127.0.0.1:<cổng>/<đường>?<q>   — GET / POST / PUT

🛑 Ba luật của đường chuyển tiếp — nó là một PROXY, và proxy mở là một lỗ:
  1. Chỉ tới 127.0.0.1, chỉ các cổng trong `KP_CONSOLE_CONG` (mặc định 5119 app dev, 5120 app giả lập). Không có
     cách nào đưa một địa chỉ tuỳ ý vào URL.
  2. Chỉ chuyển Authorization + Content-Type + Accept. Cookie và header khác của trình duyệt KHÔNG đi theo.
  3. Lỗi của host (4xx/5xx) đi về NGUYÊN TRẠNG — kể cả ProblemDetails. Proxy nuốt lỗi là giấu đúng thứ
     console sinh ra để cho thấy.

⚠ Thư viện CHUẨN, như phần còn lại của ui_server. Node chỉ cần lúc BUILD, không cần lúc chạy.
"""
from __future__ import annotations

import mimetypes
import os
import urllib.error
import urllib.parse
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
DIST = os.path.join(HERE, "console", "dist")

CONG_CHO_PHEP = {int(x) for x in os.environ.get("KP_CONSOLE_CONG", "5119,5120").split(",") if x.strip().isdigit()}

# ⚠ Windows lấy kiểu MIME từ REGISTRY, và trên không ít máy `.js` ra `text/plain` — trình duyệt từ chối chạy module
#   script có kiểu sai, và trang trắng không báo gì. Ghim các đuôi bản build thật sự sinh ra.
MIME = {
    ".html": "text/html; charset=utf-8",
    ".js": "text/javascript; charset=utf-8",
    ".mjs": "text/javascript; charset=utf-8",
    ".css": "text/css; charset=utf-8",
    ".json": "application/json; charset=utf-8",
    ".svg": "image/svg+xml",
    ".png": "image/png",
    ".ico": "image/x-icon",
    ".woff2": "font/woff2",
    ".map": "application/json; charset=utf-8",
}

HEADER_CHUYEN = ("Authorization", "Content-Type", "Accept")


def la_duong_console(duong: str) -> bool:
    return duong == "/console" or duong.startswith("/console/")


def la_duong_app(duong: str) -> bool:
    return duong.startswith("/app/")


def phuc_vu_console(h, duong: str) -> None:
    """Trả file tĩnh của bản build. `h` là BaseHTTPRequestHandler."""
    if not os.path.isfile(os.path.join(DIST, "index.html")):
        _tra_text(h, 503, "text/html; charset=utf-8", (
            "<!doctype html><meta charset=utf-8><title>Console chưa build</title>"
            "<p style='font-family:sans-serif'>Console chưa được build. Chạy "
            "<code>scripts\\giao-dien\\build-console.bat</code> (cần Node), rồi tải lại trang.</p>"
        ).encode("utf-8"))
        return

    con = duong.removeprefix("/console").lstrip("/")
    dich = os.path.normpath(os.path.join(DIST, con)) if con else os.path.join(DIST, "index.html")

    # 🛑 Không cho ra khỏi dist/: `/console/../../appsettings.Local.json` là đúng file chứa khoá API.
    if os.path.commonpath([os.path.abspath(dich), os.path.abspath(DIST)]) != os.path.abspath(DIST):
        h.send_error(404)
        return

    if not os.path.isfile(dich):
        # Đường con của router (vd. /console/case/...) → index.html. Nhưng file tài nguyên thiếu thì 404 thật:
        # trả index.html cho một .js thiếu là biến lỗi "thiếu file" thành lỗi cú pháp khó hiểu.
        if os.path.splitext(con)[1]:
            h.send_error(404)
            return
        dich = os.path.join(DIST, "index.html")

    with open(dich, "rb") as f:
        raw = f.read()
    kieu = MIME.get(os.path.splitext(dich)[1].lower()) or mimetypes.guess_type(dich)[0] or "application/octet-stream"
    # index.html không được cache: bản build mới đổi tên file băm, index cũ trỏ vào file không còn.
    _tra_text(h, 200, kieu, raw, khong_cache=dich.endswith("index.html"))


def chuyen_tiep(h, method: str) -> None:
    """`/app/<cổng>/<đường>` → host trên 127.0.0.1."""
    u = urllib.parse.urlsplit(h.path)
    phan = u.path.split("/", 3)  # ['', 'app', '<cổng>', '<đường còn lại>']
    if len(phan) < 3 or not phan[2].isdigit() or int(phan[2]) not in CONG_CHO_PHEP:
        _tra_text(h, 403, "text/plain; charset=utf-8",
                  f"Cổng không nằm trong KP_CONSOLE_CONG ({sorted(CONG_CHO_PHEP)}).".encode("utf-8"))
        return

    dich = f"http://127.0.0.1:{phan[2]}/{phan[3] if len(phan) > 3 else ''}"
    if u.query:
        dich += "?" + u.query

    n = int(h.headers.get("Content-Length") or 0)
    than = h.rfile.read(n) if n else None

    req = urllib.request.Request(dich, data=than, method=method)
    for ten in HEADER_CHUYEN:
        if h.headers.get(ten):
            req.add_header(ten, h.headers[ten])

    try:
        with urllib.request.urlopen(req, timeout=300) as r:
            _tra_text(h, r.status, r.headers.get("Content-Type") or "application/octet-stream", r.read())
    except urllib.error.HTTPError as e:
        _tra_text(h, e.code, e.headers.get("Content-Type") or "text/plain; charset=utf-8", e.read())
    except urllib.error.URLError as e:
        _tra_text(h, 502, "application/json; charset=utf-8", (
            '{"title":"Không gọi được app ở cổng ' + phan[2] + '","detail":"' +
            str(e.reason).replace('"', "'") + '"}').encode("utf-8"))


def _tra_text(h, ma: int, kieu: str, raw: bytes, khong_cache: bool = False) -> None:
    h.send_response(ma)
    h.send_header("Content-Type", kieu)
    h.send_header("Content-Length", str(len(raw)))
    if khong_cache:
        h.send_header("Cache-Control", "no-store")
    h.end_headers()
    h.wfile.write(raw)
