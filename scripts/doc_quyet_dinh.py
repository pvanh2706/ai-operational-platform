#!/usr/bin/env python3
"""Đọc một file quyết định/khảo sát đã điền tay và nói xem nó đã dùng được chưa.

Vì sao cần script chứ không đọc mắt: hai file `docs/14` và `docs/15` được điền bằng cách
đánh `[x]`, sửa đi sửa lại nhiều lượt. Hai lỗi dưới đây mắt rất dễ bỏ qua, và cả hai đều
làm người đọc lại hiểu SAI chứ không phải hiểu THIẾU:

  · TÍCH HAI Ô ở một câu "chọn một"  -> không biết người điền chọn gì; nguy hiểm hơn để
    trống, vì để trống thì ai cũng thấy là chưa quyết.
  · ĐÁNH DẤU SAI CÚ PHÁP (`[X]`, `[x ]`, `(x)`) -> trông như đã điền mà máy không đọc ra.

Nó KHÔNG chấm nội dung và không biết câu trả lời nào đúng. Nó chỉ trả lời: file này đã
điền xong chưa, và có chỗ nào mâu thuẫn không.

    python scripts/doc_quyet_dinh.py docs/14_QUYET_DINH_CHO_CHU_DU_AN.md
    python scripts/doc_quyet_dinh.py docs/15_KHAO_SAT_VAN_HANH.md
    python scripts/doc_quyet_dinh.py docs/14_*.md docs/15_*.md      # nhiều file một lượt

Mã thoát:  0 = không có mâu thuẫn (kể cả khi chưa điền gì)
           1 = có câu tích nhiều ô ở chỗ "chọn một", hoặc có dấu tích sai cú pháp
           2 = không đọc được file

⚠ Mã thoát 0 KHÔNG có nghĩa là "đã quyết xong". Đọc dòng tổng kết — số câu còn trống nằm
  ở đó. Một phép kiểm báo xanh khi chưa có gì để kiểm là một phép kiểm nguy hiểm.
"""

from __future__ import annotations

import argparse
import io
import re
import sys

# Quy ước của repo: SCRIPT TỰ ÉP UTF-8, không bắt người gọi đặt PYTHONIOENCODING. Thiếu
# khối này thì trên Windows một dòng kết quả có dấu làm script chết bằng UnicodeEncodeError
# ở giữa chừng — và nó chết ở chiều GHI, tức sau khi đã đọc đúng, nên thông điệp lỗi trỏ
# vào mã hoá trong khi việc kiểm thật ra đã chạy xong. Chính phép kiểm này bắt được lỗi đó.
for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

# `- [x] **A1-2 — ...`  ·  nhóm theo MÃ (A1, B2a) chứ không theo tiêu đề: câu B2 có hai
# quyết định con (B2a, B2b) nằm chung một tiêu đề, gom theo tiêu đề sẽ báo nhầm là mâu thuẫn.
O_CO_MA = re.compile(r"^\s*[-*]\s*\[([ xX])\]\s*\*\*([A-Za-z]+\d+[a-z]?)-(\d+)\b")

# `- [ ] Có, tôi mở Kibana trước` — ô của bảng khảo sát, không mang mã.
O_KHONG_MA = re.compile(r"^\s*[-*]\s*\[([ xX])\]\s+(.*)$")

# `## A1 · ...` hoặc `## Câu 3 — ...`
TIEU_DE = re.compile(r"^#{2,3}\s+(.*)$")

# Dấu tích SAI cú pháp — trông như đã điền mà không đọc ra được.
SAI_CU_PHAP = re.compile(r"^\s*[-*]\s*(\[[^\]]{2,}\]|\((?:x|X)\))\s")

# Câu cho phép tích nhiều ô phải TỰ KHAI bằng đúng cụm này trong tiêu đề hoặc ngay trên nó.
CHO_NHIEU = "tích nhiều ô"


def doc(duong: str) -> list[str]:
    with io.open(duong, encoding="utf-8") as f:
        return f.read().split("\n")


def phan_tich(dong: list[str]) -> tuple[dict, list, list]:
    """Trả về (nhóm-có-mã, nhóm-không-mã, danh-sách-dòng-sai-cú-pháp)."""
    co_ma: dict[str, dict] = {}
    khong_ma: list[dict] = []
    sai: list[tuple[int, str]] = []

    tieu_de = "(đầu file)"
    trong_khoi_code = False
    hien_tai: dict | None = None

    for so, d in enumerate(dong, 1):
        if d.lstrip().startswith("```"):
            trong_khoi_code = not trong_khoi_code
            continue
        if trong_khoi_code:
            # Ô trong khối ``` là VÍ DỤ hoặc danh sách việc, không phải ô để điền.
            continue

        m = TIEU_DE.match(d)
        if m:
            tieu_de = m.group(1).strip()
            hien_tai = None
            continue

        m = O_CO_MA.match(d)
        if m:
            danh_dau, ma, so_o = m.group(1), m.group(2), m.group(3)
            g = co_ma.setdefault(ma, {"tieu_de": tieu_de, "o": [], "cho_nhieu": False})
            g["o"].append((so_o, danh_dau.strip().lower() == "x", so))
            continue

        m = O_KHONG_MA.match(d)
        if m:
            danh_dau, nhan = m.group(1), m.group(2).strip()
            if hien_tai is None:
                hien_tai = {"tieu_de": tieu_de, "o": [], "cho_nhieu": False}
                khong_ma.append(hien_tai)
            hien_tai["o"].append((nhan[:48], danh_dau.strip().lower() == "x", so))
            continue

        if SAI_CU_PHAP.match(d):
            sai.append((so, d.strip()[:70]))

        # ⚠ BẤT KỲ dòng nào không phải ô tích đều ĐÓNG cụm đang mở — kể cả dòng trống.
        # Một tiêu đề có thể chứa NHIỀU cụm: bảng khảo sát hay có câu chính (chọn một) rồi
        # một câu phụ (tích nhiều ô). Gom theo TIÊU ĐỀ thì hai cụm dính làm một, và một bảng
        # điền HOÀN TOÀN ĐÚNG sẽ bị báo mâu thuẫn. Phép kiểm kêu oan thì sẽ bị bỏ qua, nên
        # nó tệ hơn là không có phép kiểm.
        hien_tai = None

    return co_ma, khong_ma, sai


def danh_dau_cho_nhieu(dong: list[str], co_ma: dict, khong_ma: list) -> None:
    """Tìm cụm 'tích nhiều ô' và gắn cờ cho nhóm đứng ngay sau nó."""
    vt = [i for i, d in enumerate(dong) if CHO_NHIEU in d]
    if not vt:
        return
    for nhom in list(co_ma.values()) + khong_ma:
        if not nhom["o"]:
            continue
        dong_dau = nhom["o"][0][2]
        # cụm nằm trong khoảng 6 dòng ngay trước ô đầu tiên
        if any(dong_dau - 7 <= i + 1 < dong_dau for i in vt):
            nhom["cho_nhieu"] = True


def kiem(duong: str) -> int:
    try:
        dong = doc(duong)
    except OSError as e:
        sys.stdout.write("KHONG DOC DUOC %s: %s\n" % (duong, e))
        return 2

    co_ma, khong_ma, sai = phan_tich(dong)
    danh_dau_cho_nhieu(dong, co_ma, khong_ma)

    sys.stdout.write("\n=== %s ===\n" % duong)

    mau_thuan = []
    con_trong = []
    da_quyet = []

    for ma in sorted(co_ma):
        g = co_ma[ma]
        tick = [o for o in g["o"] if o[1]]
        if len(tick) == 0:
            con_trong.append((ma, g["tieu_de"]))
        elif len(tick) == 1 or g["cho_nhieu"]:
            da_quyet.append((ma, ", ".join("%s-%s" % (ma, t[0]) for t in tick), g["tieu_de"]))
        else:
            mau_thuan.append((ma, [("%s-%s" % (ma, t[0]), t[2]) for t in tick], g["tieu_de"]))

    # --- Bảng có mã (file quyết định) ---
    if co_ma:
        sys.stdout.write("\nCAU CO MA (file quyet dinh)\n")
        for ma, chon, td in da_quyet:
            sys.stdout.write("  [DA QUYET] %-5s -> %-12s %s\n" % (ma, chon, td[:52]))
        for ma, td in con_trong:
            sys.stdout.write("  [ TRONG  ] %-5s %s\n" % (ma, td[:64]))
        for ma, chon, td in mau_thuan:
            sys.stdout.write("  [MAU THUAN] %s tich %d o: %s\n"
                             % (ma, len(chon), ", ".join("%s (dong %d)" % c for c in chon)))

    # --- Bảng không mã (file khảo sát) ---
    tl_tra_loi = tl_trong = 0
    if khong_ma:
        sys.stdout.write("\nCAU KHONG MA (file khao sat)\n")
        for nhom in khong_ma:
            tick = [o for o in nhom["o"] if o[1]]
            # Một tiêu đề có thể chứa NHIỀU cụm, nên in kèm ô ĐẦU TIÊN để phân biệt chúng:
            # hai dòng cùng tên "Câu 2" thì người đọc không biết dòng nào còn trống.
            nhan = "%s / %s" % (nhom["tieu_de"][:38], nhom["o"][0][0][:26])
            co = "  [nhieu o]" if nhom["cho_nhieu"] else ""
            if tick:
                tl_tra_loi += 1
                sys.stdout.write("  [DA TRA LOI] %-68s (%d o)%s\n" % (nhan, len(tick), co))
            else:
                tl_trong += 1
                sys.stdout.write("  [  TRONG   ] %-68s%s\n" % (nhan, co))
            if len(tick) > 1 and not nhom["cho_nhieu"]:
                mau_thuan.append((nhom["tieu_de"][:40],
                                  [(t[0], t[2]) for t in tick], nhom["tieu_de"]))

    if sai:
        sys.stdout.write("\nDAU TICH SAI CU PHAP (trong nhu da dien ma may khong doc ra)\n")
        for so, d in sai:
            sys.stdout.write("  dong %-5d %s\n" % (so, d))

    # --- Tổng kết ---
    tong_ma = len(co_ma)
    sys.stdout.write("\nTONG KET\n")
    if tong_ma:
        sys.stdout.write("  quyet dinh: %d/%d da chon · %d con trong\n"
                         % (len(da_quyet), tong_ma, len(con_trong)))
    if khong_ma:
        sys.stdout.write("  khao sat  : %d/%d da tra loi · %d con trong\n"
                         % (tl_tra_loi, tl_tra_loi + tl_trong, tl_trong))

    if mau_thuan or sai:
        sys.stdout.write("  KET QUA   : CO VAN DE - xem muc tren, sua roi chay lai.\n")
        return 1

    if tong_ma and not da_quyet and not (khong_ma and tl_tra_loi):
        sys.stdout.write("  KET QUA   : chua dien gi ca. Khong mau thuan, nhung cung CHUA DUNG DUOC.\n")
        return 0

    sys.stdout.write("  KET QUA   : khong mau thuan.")
    if con_trong or (khong_ma and tl_trong):
        sys.stdout.write(" ⚠ Van con cau de trong - xem muc tren.\n")
    else:
        sys.stdout.write(" Da dien het.\n")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("file", nargs="+", help="file .md đã điền")
    args = ap.parse_args()

    ma = 0
    for f in args.file:
        ma = max(ma, kiem(f))
    sys.stdout.write("\n")
    return ma


if __name__ == "__main__":
    sys.exit(main())
