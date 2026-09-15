#!/usr/bin/env python3
"""Quét xem có CREDENTIAL THẬT nào lọt vào file được GIT THEO DÕI hay không.

    python scripts/quet_credential_repo.py              # quét cây làm việc
    python scripts/quet_credential_repo.py --lich-su    # quét thêm TOÀN BỘ git history

Mã thoát:  0 = sạch  ·  1 = có phát hiện CHẶN  ·  2 = không dựng được tập đối chiếu.

VÌ SAO PHÉP QUÉT NÀY QUÉT THEO GIÁ TRỊ, KHÔNG QUÉT THEO HÌNH DẠNG
-----------------------------------------------------------------
`check_corpus.py` và `SecretShapeScanner` quét theo HÌNH DẠNG, vì chúng đứng ở chỗ dữ
liệu vận hành đi qua và ở đó chưa ai biết giá trị nào là bí mật. Ở đây thì ngược lại:
file được git theo dõi là mã nguồn và tài liệu, nơi `pass: 12345` là một giá trị GIẢ
trong test và một dòng chỉ-có-số là một con số bình thường. Quét hình dạng ở đây cho ra
gần như toàn false positive, và một phép kiểm toàn báo động giả là một phép kiểm sẽ bị
tắt.

Thứ thật sự cần bắt là chuyện ĐÃ XẢY RA: một giá trị thật từ corpus bị chép tay vào
tài liệu hoặc vào test. Nên tập đối chiếu được dựng từ các nguồn NGOÀI git, rồi tìm đúng
những giá trị đó trong các file TRONG git.

⚠ VÌ SAO NÓ CẦN TỒN TẠI: ngày 2026-09-11, trong lúc viết test cho chính cổng che, bốn
credential thật đã bị dán vào `docs/07`, `SecretShapeScanner.cs` và `EgressRedactionTests.cs`
— dù ngay đầu file test đó đã có dòng "MỌI GIÁ TRỊ TRONG FILE NÀY LÀ GIẢ, đừng ai thêm
vào". Lần đó bắt được vì có người nhớ quét tay. Phép kiểm này để lần sau không phải nhớ.

HAI MỨC, VÀ MỨC THỨ HAI CHỈ NỔ KHI CÓ ĐỦ CẶP
--------------------------------------------
· MẠNH  giá trị ≥ 7 ký tự, dạng `nn nnn nnn`, không phải số, hoặc có tên trong
        `redact-list.json`. Một lần trùng là đủ để chặn.
· YẾU   số 4-6 chữ số (mật khẩu Ultraviewer hay dài đúng 5). Tự nó KHÔNG nhận dạng
        được: `19210` là mật khẩu hay một con số bình thường thì nhìn giá trị không
        biết được. Nên nó chỉ bị báo khi **ID đi cùng nó trong corpus cũng xuất hiện
        trong CÙNG file** — tức khi cả cặp đăng nhập đã lọt ra.

⚠ Bản đầu của file này xét giá trị yếu một mình và ra **401 báo động**, gần như toàn
  `2026`, mã lỗi `1518` và số hiệu case. Giữ ghi chú đó ở `cap_tu_van_ban` vì nó là lý
  do cấu trúc dữ liệu ở đây là CẶP chứ không phải một tập phẳng.

⚠ GIỚI HẠN PHẢI BIẾT: phép quét chỉ thấy được giá trị CÓ TRONG nguồn đối chiếu tại máy
này. Máy chưa kéo corpus về thì tập đối chiếu rỗng và script nói thẳng điều đó thay vì
báo "sạch" — xem `--lich-su` và mã thoát 2. Nó KHÔNG thay thế việc không chép credential
vào file ngay từ đầu.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys

# Quy ước của repo: SCRIPT TỰ ÉP UTF-8, không bắt người gọi đặt PYTHONIOENCODING.
# Thiếu khối này thì chạy qua pipe trên máy trắng là chết bằng UnicodeEncodeError.
for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

GOC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

SO = r"(?<!\d)(?:\d{2,3}[ ]\d{3}[ ]\d{3}|\d{4,9})(?!\d)"
NHAN = (r"(?i)(?:m[aậâạ][tj]?\s*kh[aâẩ]u|matkhau|pass(?:word)?|pw|pwd"
        r"|ID(?:\s*c[uủ]a\s*b[aạ]n)?)")
CONG_CU = r"(?i)ultra ?view|utlatra|teamview|anydesk"
URL = re.compile(r"https?://\S+")
so_tran = re.compile(r"^\s*(?:" + SO + r")\s*$")
lay_so = re.compile(SO)
nhan_va_so = re.compile(NHAN + r"\s*[:=]?\s*(" + SO + r")")
cong_cu = re.compile(CONG_CU)


def che(v: str) -> str:
    """Che giá trị trước khi in. Phép kiểm về rò rỉ mà tự in bí mật ra log thì vô nghĩa —
    output của nó đi vào CI, vào terminal, vào chỗ người ta chụp màn hình gửi nhau."""
    if len(v) <= 4:
        return "•" * len(v)
    return v[:2] + "•" * (len(v) - 3) + v[-1:]


def cap_tu_van_ban(text: str) -> list[tuple[str, set[str]]]:
    """Rút các CẶP (giá trị mạnh, các giá trị yếu đi kèm) khỏi một khối văn bản.

    ⚠ VÌ SAO PHẢI LÀ CẶP, KHÔNG PHẢI MỘT TẬP PHẲNG — hỏng thật, sửa 2026-09-11: bản đầu
    gom mọi số ≥ 4 ký tự vào một tập rồi tìm từng cái trong repo. Nó ra **401 cảnh báo**,
    gần như toàn `2026` (năm), `1518` (mã lỗi NCC) và số hiệu case (`345857`) — những thứ
    có mặt hợp lệ khắp tài liệu. Một phép kiểm toàn báo động giả là phép kiểm sẽ bị tắt,
    và chính docstring của file này đã nói đúng điều đó trước khi nó tự mắc phải.

    Cái sửa được nó là mô hình đe doạ, không phải ngưỡng: **một mật khẩu 5 số tự nó
    không nhận dạng được** — `19210` là mật khẩu hay là một con số bình thường thì không
    có cách nào biết khi chỉ nhìn giá trị. Nó chỉ trở thành bằng chứng khi ID đi cùng nó
    CŨNG xuất hiện trong cùng file. Nên giá trị yếu không bao giờ được xét một mình.
    """
    ra: list[tuple[str, set[str]]] = []
    dong = [URL.sub(lambda m: " " * len(m.group()), d) for d in text.splitlines()]

    def gan(i: int) -> set[str]:
        """Các số trong 3 dòng CÓ NỘI DUNG quanh dòng i — nửa kia của cặp đăng nhập."""
        canh: set[str] = set()
        for buoc in (-1, 1):
            daxet, k = 0, i + buoc
            while 0 <= k < len(dong) and daxet < 3:
                if dong[k].strip():
                    daxet += 1
                    canh.update(lay_so.findall(dong[k]))
                k += buoc
        return canh

    for i, d in enumerate(dong):
        goc: set[str] = set()
        for m in nhan_va_so.finditer(d):
            goc.add(m.group(1))
        mc = cong_cu.search(d)
        if mc:
            goc.update(lay_so.findall(d[mc.end():]))
        if so_tran.match(d):
            goc.add(d.strip())
        for v in goc:
            v = v.strip()
            if len(v) < 4:
                continue
            canh = {c.strip() for c in gan(i) if len(c.strip()) >= 4 and c.strip() != v}
            ra.append((v, canh))
    return ra


def dung_tap_doi_chieu() -> tuple[set[str], dict[str, set[str]], list[str]]:
    """Dựng tập giá trị thật, CHỈ từ nguồn nằm NGOÀI git.

    Trả về (giá trị MẠNH, bản đồ giá trị YẾU -> các giá trị mạnh đi kèm nó, tên nguồn).
    """
    manh_ra: set[str] = set()
    ban: dict[str, set[str]] = {}
    nguon: list[str] = []

    def nap(caps):
        for v, canh in caps:
            if manh(v):
                manh_ra.add(v)
            else:
                ban.setdefault(v, set()).update(c for c in canh if manh(c))
            # ⚠ HÀNG XÓM MẠNH CŨNG PHẢI VÀO TẬP MẠNH — thiếu dòng này là lỗi đã xảy ra
            # thật, và tự phép tự-kiểm bắt được: hai trong bốn giá trị bị rò ngày
            # 2026-09-11 vẫn lọt. Lý do y hệt lý do cổng che cũ sót — nửa ID của cặp
            # thường nằm TRONG một câu ("ultra : <id>", "@Cs Ezcloud <id>"), nên nó
            # không bao giờ tự mình là "giá trị gốc" của một hình dạng; nó chỉ xuất
            # hiện với tư cách hàng xóm của dòng chỉ-có-số.
            manh_ra.update(c for c in canh if manh(c))

    p = os.path.join(GOC, "scripts", "jira-export", "redact-list.json")
    if os.path.exists(p):
        with open(p, encoding="utf-8") as f:
            for m in json.load(f).get("muc", []):
                if m.get("that"):
                    v = str(m["that"]).strip()
                    # redact-list là giá trị ĐÃ ĐƯỢC NGƯỜI XÁC NHẬN là bí mật thật, nên
                    # kể cả số ngắn cũng vào mức chặn — không cần bạn đồng hành.
                    manh_ra.add(v)
        nguon.append("redact-list.json")

    for ten in ("fixture-evidence.json", "fixture-cases.json", "dry-run-evidence.json",
                "spread-evidence.json"):
        p = os.path.join(GOC, "scripts", "jira-export", ten)
        if not os.path.exists(p):
            continue
        with open(p, encoding="utf-8") as f:
            data = json.load(f)
        muc = data if isinstance(data, list) else next(
            (v for v in data.values() if isinstance(v, list)), [])
        for e in muc:
            if isinstance(e, dict):
                nap(cap_tu_van_ban(str(e.get("content") or e.get("title") or "")))
        nguon.append(ten)

    for ten in sorted(os.listdir(GOC)):
        if ten.startswith("nhom") and ten.endswith(".txt"):
            with open(os.path.join(GOC, ten), encoding="utf-8") as f:
                nap(cap_tu_van_ban(f.read()))
            nguon.append(ten)

    return manh_ra, {k: v for k, v in ban.items() if v}, nguon


def manh(v: str) -> bool:
    """Giá trị đủ đặc trưng để một lần trùng là đáng chặn."""
    return len(v) >= 7 or " " in v or not v.isdigit()


def bo_qua() -> list[dict]:
    p = os.path.join(GOC, "scripts", "quet-credential-daduyet.json")
    if not os.path.exists(p):
        return []
    with open(p, encoding="utf-8") as f:
        return json.load(f).get("muc", [])


def file_git_theo_doi() -> list[str]:
    r = subprocess.run(["git", "-C", GOC, "ls-files"],
                       capture_output=True, text=True, encoding="utf-8")
    return [d for d in r.stdout.splitlines() if d.strip()]


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--lich-su", action="store_true",
                    help="quét thêm toàn bộ git history, không chỉ cây làm việc")
    args = ap.parse_args()

    manh_gt, ban, nguon = dung_tap_doi_chieu()
    if not manh_gt:
        print("🛑 KHÔNG dựng được tập đối chiếu — không có corpus nào tại máy này.")
        print("   Đây KHÔNG phải 'sạch'. Kéo corpus về theo docs/10 §3 rồi chạy lại.")
        sys.exit(2)

    print("Tập đối chiếu: %d giá trị mạnh · %d giá trị yếu có cặp · %d nguồn ngoài git"
          % (len(manh_gt), len(ban), len(nguon)))
    print("  " + " · ".join(nguon))

    duyet = bo_qua()
    chan: list[str] = []

    def da_duyet(duong: str, v: str) -> bool:
        return any(b.get("duongDan") == duong and b.get("giaTriChe") == che(v)
                   for b in duyet)

    for duong in file_git_theo_doi():
        p = os.path.join(GOC, duong)
        if not os.path.isfile(p):
            continue
        try:
            with open(p, encoding="utf-8") as f:
                noi_dung = f.read()
        except (UnicodeDecodeError, OSError):
            continue
        dong = noi_dung.splitlines()

        for i, d in enumerate(dong, 1):
            for v in manh_gt:
                if v in d and not da_duyet(duong, v):
                    chan.append("%s:%d  %s  (giá trị mạnh)" % (duong, i, che(v)))

        # Giá trị yếu: chỉ tính khi BẠN ĐỒNG HÀNH của nó cũng có mặt trong CÙNG file.
        for yeu, bandong in ban.items():
            if yeu not in noi_dung or da_duyet(duong, yeu):
                continue
            co = [b for b in bandong if b in noi_dung]
            if not co:
                continue
            for i, d in enumerate(dong, 1):
                if yeu in d:
                    chan.append("%s:%d  %s  (cặp với %s)"
                                % (duong, i, che(yeu), che(co[0])))

    if args.lich_su:
        rev = subprocess.run(["git", "-C", GOC, "rev-list", "--all"],
                             capture_output=True, text=True, encoding="utf-8")
        commits = rev.stdout.split()
        print("")
        print("Quét git history: %d commit…" % len(commits))
        for v in sorted(manh_gt):
            r = subprocess.run(["git", "-C", GOC, "grep", "-l", "-F", v] + commits,
                               capture_output=True, text=True, encoding="utf-8")
            if r.stdout.strip():
                n = len(set(r.stdout.strip().splitlines()))
                chan.append("git history: %s  (%d chỗ — KHÔNG xoá được bằng sửa file)"
                            % (che(v), n))

    print()
    if chan:
        print("🛑 %d chỗ CHẶN — giá trị thật nằm trong file được git theo dõi:" % len(set(chan)))
        for c in sorted(set(chan)):
            print("   " + c)
        print()
        print("   Sửa: thay bằng giá trị GIẢ giữ nguyên hình dạng (quy ước của")
        print("   redact-list.json). Nếu nó đã vào git history thì sửa file KHÔNG gỡ")
        print("   được khỏi history — xem docs/10 §0, và xoay credential phía khách.")
        print("   Chỗ đã được chủ dự án chấp nhận thì khai ở scripts/quet-credential-daduyet.json")
    else:
        print("✅ Sạch: không giá trị nào của corpus xuất hiện trong file git theo dõi.")

    sys.exit(1 if chan else 0)


if __name__ == "__main__":
    main()
