#!/usr/bin/env python3
"""Tra xem Jira có TRƯỜNG NÀO chứa mã khách sạn — chặn quyết định 2 (sub-tenant).

VÌ SAO CẦN SCRIPT NÀY:
Quyết định 2 (2026-09-04) chốt rằng ranh giới khách sạn A ↔ B LÀ ranh giới bảo mật, nên
`evidence_item` phải mang sub-tenant. Nhưng sub-tenant lấy từ đâu thì chưa biết, và có
đúng một cách SAI mà rất dễ chọn: suy từ tiêu đề.

Corpus thật cho thấy tiêu đề CÓ mã số — "[17468 - KHÁCH SẠN SỚM PHÚ QUÝ 3]",
"[16776 -Villa 22 - Dalat]", "18182 - The One Hotel 2" — nên suy từ tiêu đề trông rất
khả thi. Nó không khả thi, và đã đo được vì sao:

    ES-346622  tiêu đề ghi "Mariha"       · thân bài ghi mirahhotel.sales@...
    ES-346618  tiêu đề ghi "Mirah Hotel"  · thân bài ghi "Thành Danh Hotel"
    ES-346619  y hệt ES-346618 (trùng byte)

Ba tên chồng chéo trên ba case cùng chủ đề. Gán sub-tenant bằng cách đọc chữ là gán
nhầm khách sạn ngay từ mẩu evidence đầu tiên — và một khi đã gán nhầm thì mọi tầng
bảo vệ phía sau đều bảo vệ sai chỗ. `G6`/`AP3`: không đoán provenance.

Nên phải lấy từ một TRƯỜNG CÓ KIỂM SOÁT. Script này đi tìm trường đó, và nếu không có
thì nó nói thẳng là không có — đó cũng là một câu trả lời dùng được.

Chạy (dùng lại jira-config.bat đã điền):
    cmd /c "call scripts\\jira-export\\jira-config.bat && python scripts\\jira-export\\discover_fields.py"
"""

from __future__ import annotations

import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

# Mã khách sạn quan sát được trong corpus: 5 chữ số, đôi khi 4.
MA_KHACH_SAN = re.compile(r"^\d{4,6}$")
# Từ khoá gợi ý tên trường, cả tiếng Việt lẫn tiếng Anh.
GOI_Y = re.compile(r"(?i)khách sạn|khach san|hotel|property|cơ sở|co so|tenant|"
                   r"khách hàng|khach hang|customer|account|mã kh|ma kh|client|đơn vị|don vi")
TIMEOUT = 60


def die(msg: str):
    print(f"LỖI: {msg}", file=sys.stderr)
    sys.exit(1)


def auth_header() -> str:
    pat = os.environ.get("JIRA_PAT", "").strip()
    if pat:
        return f"Bearer {pat}"
    import base64
    user = os.environ.get("JIRA_USER", "").strip()
    pw = os.environ.get("JIRA_PASS", "") or os.environ.get("JIRA_PASSWORD", "")
    if user and pw:
        return "Basic " + base64.b64encode(f"{user}:{pw}".encode()).decode()
    die("thiếu xác thực. Chạy qua jira-config.bat, hoặc đặt JIRA_PAT.")


def get(base: str, path: str, auth: str) -> dict | list:
    req = urllib.request.Request(f"{base}{path}", headers={"Authorization": auth})
    try:
        with urllib.request.urlopen(req, timeout=TIMEOUT) as r:
            return json.loads(r.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        die(f"HTTP {e.code} tại {path}\n{e.read().decode('utf-8', 'replace')[:600]}")
    except urllib.error.URLError as e:
        die(f"không gọi được {base}{path}: {e.reason}")


def phang(gia_tri) -> str:
    """Rút một giá trị field của Jira thành chuỗi đọc được. Field của Jira có thể là
    chuỗi, số, dict {value/name/displayName}, hoặc mảng của những thứ đó."""
    if gia_tri is None:
        return ""
    if isinstance(gia_tri, (str, int, float)):
        return str(gia_tri)
    if isinstance(gia_tri, dict):
        for k in ("value", "name", "displayName", "key"):
            if k in gia_tri:
                return str(gia_tri[k])
        return json.dumps(gia_tri, ensure_ascii=False)[:120]
    if isinstance(gia_tri, list):
        return " | ".join(phang(x) for x in gia_tri[:4])
    return str(gia_tri)[:120]


def main() -> None:
    base = os.environ.get("JIRA_BASE_URL", "").rstrip("/")
    if not base:
        die("thiếu JIRA_BASE_URL.")
    auth = auth_header()

    # Lấy vài issue thật để soi giá trị. Dùng JQL đã điền nếu có, không thì lấy issue
    # gần nhất của project ES — cần dữ liệu THẬT vì tên trường không nói lên nội dung.
    jql = os.environ.get("JIRA_JQL") or "project = ES ORDER BY created DESC"
    q = urllib.parse.urlencode({"jql": jql, "maxResults": 8, "fields": "*all"})
    page = get(base, f"/rest/api/2/search?{q}", auth)
    issues = page.get("issues", [])
    if not issues:
        die("JQL không khớp issue nào.")

    ten_field = {f["id"]: f.get("name", f["id"]) for f in get(base, "/rest/api/2/field", auth)}

    print(f"Jira:  {base}")
    print(f"Soi {len(issues)} issue: {', '.join(i['key'] for i in issues)}\n")

    # Gom giá trị từng field qua nhiều issue. Một field chỉ dùng được làm sub-tenant nếu
    # nó CÓ GIÁ TRỊ ở gần như mọi issue — trường điền tay lúc có lúc không thì vô dụng.
    gia_tri: dict[str, list[str]] = {}
    for issue in issues:
        for fid, val in (issue.get("fields") or {}).items():
            s = phang(val).strip()
            if s:
                gia_tri.setdefault(fid, []).append(s)

    ung_vien, kha_nang, con_lai = [], [], []
    for fid, vals in gia_tri.items():
        ten = ten_field.get(fid, fid)
        do_phu = len(vals)
        # Ứng viên mạnh: giá trị TRÔNG NHƯ mã khách sạn ở phần lớn issue.
        so_ma = sum(1 for v in vals if MA_KHACH_SAN.match(v))
        if so_ma >= max(2, do_phu // 2):
            ung_vien.append((fid, ten, do_phu, vals))
        elif GOI_Y.search(ten):
            kha_nang.append((fid, ten, do_phu, vals))
        elif fid.startswith("customfield_"):
            con_lai.append((fid, ten, do_phu, vals))

    def in_ra(tieu_de: str, muc: list, day_du: bool):
        print("=" * 70)
        print(tieu_de)
        print("=" * 70)
        if not muc:
            print("  (không có)\n")
            return
        for fid, ten, phu, vals in sorted(muc, key=lambda x: -x[2]):
            print(f"  {ten}  [{fid}]   có giá trị ở {phu}/{len(issues)} issue")
            if day_du:
                for v in vals[:5]:
                    print(f"      {v[:100]}")
            print()

    in_ra("ỨNG VIÊN MẠNH — giá trị trông đúng như mã khách sạn (4-6 chữ số)", ung_vien, True)
    in_ra("CÓ THỂ — tên trường nhắc tới khách sạn / khách hàng / cơ sở", kha_nang, True)
    in_ra("CÁC CUSTOM FIELD KHÁC có giá trị (để đối chiếu, không in giá trị)", con_lai, False)

    print("=" * 70)
    print("CÁCH ĐỌC KẾT QUẢ NÀY")
    print("=" * 70)
    print("""  · Có ứng viên mạnh, phủ gần hết issue  -> dùng nó làm sub-tenant. Xong.
  · Chỉ có ở MỘT SỐ issue                -> chưa dùng được: thiếu thì phải CHẶN NẠP
                                             chứ không được đoán. Cần điền cho đủ
                                             trước, hoặc chọn nguồn khác.
  · KHÔNG có trường nào                  -> đây cũng là câu trả lời. Lúc đó lựa chọn
                                             là (a) tạo custom field trong Jira rồi
                                             điền dần, hay (b) lùi quyết định 2 và
                                             ghi rõ evidence CHƯA có sub-tenant.
                                             KHÔNG chọn suy từ tiêu đề - lý do đã đo,
                                             xem docstring đầu file.""")


if __name__ == "__main__":
    main()
