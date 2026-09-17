#!/usr/bin/env python3
"""Đếm số issue của từng CỬA SỔ THỜI GIAN trước khi kéo về.

    run-dem.bat                 đếm 13w / 26w / 52w (3 / 6 / 12 tháng)
    run-dem.bat 4w 13w 26w      đếm cửa sổ tự chọn

Vì sao có script này: `MAX_ISSUES` và cửa sổ thời gian là HAI cái chặn khác nhau,
và cái nào đang chặn thì phải ĐO chứ không đoán. Đo được ngày 2026-09-18: JQL
12 tháng khớp nhiều hơn 150, nên trần 150 mới là thứ đang chặn — 150 case lấy về
chỉ trải 4,5 tháng. Hạ cửa sổ xuống 6 tháng lúc đó KHÔNG đổi một dòng nào.

⚠ Script này chỉ gửi `maxResults=0` và đọc trường `total`. Nó KHÔNG kéo summary,
  description hay comment về — nên chạy bao nhiêu lần cũng không sinh ra corpus,
  không đụng tới dry-run-*.json, và không có gì phải quét credential sau đó.

⚠ JQL KHÔNG CÓ ĐƠN VỊ THÁNG. Đơn vị hợp lệ là w/d/h/m (m = phút, không phải tháng).
  Nên 3 tháng viết là -13w, 6 tháng -26w, 12 tháng -52w.
"""
from __future__ import annotations

import base64
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

TIMEOUT = 30

# 🛑 Mọi mệnh đề về ngày đóng, ở CẢ HAI dạng: ngày cứng ("2026-09-04") và ngày
# tương đối (-365d). Bắt cả hai vì file cấu hình thật đang dùng dạng cứng, còn
# file mẫu dùng dạng tương đối — script phải thay được cả hai.
_MENH_DE_NGAY = re.compile(
    r'(resolved|resolutiondate|created|updated)\s*[<>]=?\s*("[^"]*"|-?\d+[wdhm])\s*(AND\s+)?',
    re.IGNORECASE)


def die(msg: str) -> None:
    print(f"LỖI: {msg}")
    sys.exit(1)


def auth_header() -> str:
    pat = os.environ.get("JIRA_PAT", "").strip()
    user = os.environ.get("JIRA_USER", "").strip()
    pw = os.environ.get("JIRA_PASS", "") or os.environ.get("JIRA_PASSWORD", "")
    if pat:
        return f"Bearer {pat}"
    if user and pw:
        return "Basic " + base64.b64encode(f"{user}:{pw}".encode()).decode()
    die("thiếu xác thực: đặt JIRA_PAT, hoặc JIRA_USER + JIRA_PASS.")
    raise AssertionError  # không tới được


def tach(jql: str) -> tuple[str, str]:
    """Tách phần lọc và phần ORDER BY."""
    m = re.search(r'\s+ORDER\s+BY\s+', jql, re.IGNORECASE)
    return (jql[:m.start()], jql[m.start():]) if m else (jql, "")


def doi_cua_so(jql: str, cua_so: str) -> str:
    """Thay MỌI mệnh đề ngày bằng đúng một mệnh đề `resolved >= -<cua_so>`."""
    loc, order = tach(jql)
    loc = _MENH_DE_NGAY.sub("", loc).strip()
    loc = re.sub(r'^\s*AND\s+', '', loc, flags=re.IGNORECASE).strip()
    loc = re.sub(r'\s+AND\s*$', '', loc, flags=re.IGNORECASE).strip()
    moi = f"resolved >= -{cua_so}"
    return f"{moi} AND {loc}{order}" if loc else f"{moi}{order}"


def dem(base: str, auth: str, jql: str) -> int | str:
    params = urllib.parse.urlencode({"jql": jql, "maxResults": 0, "fields": "key"})
    req = urllib.request.Request(f"{base}/rest/api/2/search?{params}",
                                 headers={"Authorization": auth, "Accept": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=TIMEOUT) as r:
            return json.loads(r.read().decode("utf-8")).get("total", -1)
    except urllib.error.HTTPError as ex:
        # In nguyên lý do Jira trả về — JQL sai cú pháp là ca thường gặp nhất, và
        # thông báo của Jira chỉ thẳng vào chỗ sai.
        try:
            chi_tiet = json.loads(ex.read().decode("utf-8")).get("errorMessages", [])
        except Exception:
            chi_tiet = []
        return f"HTTP {ex.code}: {'; '.join(chi_tiet) or ex.reason}"
    except Exception as ex:  # noqa: BLE001 — mạng, DNS, timeout đều chỉ cần nói ra
        return f"{type(ex).__name__}: {ex}"


def main() -> int:
    base = os.environ.get("JIRA_BASE_URL", "").rstrip("/")
    if not base:
        die("thiếu JIRA_BASE_URL.")
    goc = os.environ.get("JIRA_JQL", "").strip()
    if not goc:
        die("thiếu JIRA_JQL.")

    auth = auth_header()
    cua_so = sys.argv[1:] or ["13w", "26w", "52w"]
    tran = int(os.environ.get("MAX_ISSUES", "0"))

    print(f"Jira: {base}")
    print(f"MAX_ISSUES đang đặt: {tran or 'KHÔNG TRẦN'}\n")

    n_goc = dem(base, auth, goc)
    print(f"  JQL hiện tại (nguyên văn trong jira-config.bat) : {n_goc}")

    if re.search(r'resolved\s*<\s*"', goc, re.IGNORECASE):
        print("     ⚠ JQL này có CHẶN TRÊN CỨNG (resolved < \"...\") — phần mới nhất bị "
              "bỏ im lặng, và khoảng bỏ sót rộng thêm mỗi ngày.")

    print()
    for cs in cua_so:
        jql = doi_cua_so(goc, cs)
        n = dem(base, auth, jql)
        thang = {"13w": "3 tháng", "26w": "6 tháng", "52w": "12 tháng"}.get(cs, cs)
        canh = ""
        if isinstance(n, int) and tran and n > tran:
            canh = f"   <- TRẦN {tran} CHẶN, chỉ lấy {tran} case mới nhất"
        elif isinstance(n, int) and tran:
            canh = "   <- cửa sổ chặn, không phải trần"
        print(f"  resolved >= -{cs:<4} ({thang:<8}) : {n}{canh}")

    print("\nJQL đầy đủ của cửa sổ đầu tiên, dán được thẳng vào jira-config.bat:")
    print(f'  set "JIRA_JQL={doi_cua_so(goc, cua_so[0])}"')
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
