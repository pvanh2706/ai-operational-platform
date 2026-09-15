#!/usr/bin/env python3
"""Đếm `diff(A, B)` giữa bản nháp NGƯỜI dựng (A) và bản MÁY sinh (B) của cùng một nhóm.

`M2` cần con số này, và cho tới 2026-09-15 nó vẫn được làm bằng mắt: đọc hai file rồi
gõ tay một bảng sáu số vào `docs/07`. Việc đó có hai chỗ hỏng, và cả hai đã xảy ra:

1. **Con số gõ tay không nói được nó đếm gì.** Bảng cũ so "bước kiểm 5 / 4" — nhưng
   5 bước của A và 4 bước của B không phải cùng một trục, và không ai kiểm lại được.

2. **Hai bản dùng hai bộ NHÃN khác nhau mà không ai biết.** Năm ô phân bố chốt ngày
   2026-09-11; hai cây bản A viết 09-07/08 nên đặt tên ô theo ý người viết lúc đó.
   `--kiem-cay` cộng mọi ô bất kể tên nên không bắt được, và phép so A↔B theo ô thì
   im lặng vô nghĩa: mọi ô đều "khác". Chỗ này đã được migrate 2026-09-15.

🛑 SCRIPT NÀY CỐ Ý KHÔNG TRẢ VỀ MỘT ĐIỂM SỐ. Cách tính `diff(A,B)` vẫn là câu CHƯA
QUYẾT (`docs/11` §8 mục 3: mức chứng cứ đặt ở tầng nhánh hay tầng SOP, và diff có đọc
theo nhánh-không-nguồn hay không). Một script tự chọn một cách tính rồi in ra "32%" sẽ
biến một câu chưa quyết thành một con số trông như đã quyết. Nên nó in BA cách tính
cạnh nhau, có nhãn, và để người đọc chọn — hoặc để chính bảng này giúp chốt.

    python scripts/jira-export/diff_ab.py <A.json> <B.json>
    python scripts/jira-export/diff_ab.py --tat-ca      # mọi cặp bắt được theo tên nhóm
"""

from __future__ import annotations

import argparse
import glob
import io
import json
import os
import sys

for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

HERE = os.path.dirname(os.path.abspath(__file__))
GOC = os.path.dirname(os.path.dirname(HERE))
PHAN_TICH = os.path.join(GOC, "docs", "ket-qua-phan-tich")

NAM_O = ["ghi-ro-buoc-kiem", "suy-ra-duoc-nhung-khong-ghi", "chi-ghi-ket-luan",
         "buoc-kiem-ngoai-ticket", "khong-phai-chan-doan"]

# Tên bảng mà một cây có thể mang. Xem `nhom_sop.py` BANG_BIET — ba nhóm ra ba topology.
TEN_BANG = ("bangTraMaLoi", "bangDoiChieuCot")


def doc(p):
    with io.open(p, encoding="utf-8") as f:
        return json.load(f)


def cac_dong_bang(cay):
    return [d for ten in TEN_BANG for d in (cay.get(ten) or {}).get("dong") or []]


def mo_ta(cay):
    """Đếm thô — cùng trục đếm cho cả hai bên, không diễn giải."""
    bang = cac_dong_bang(cay)
    nhanh = [n for b in cay["buocKiem"] for n in b["nhanh"]]
    return {
        "bước kiểm": len(cay["buocKiem"]),
        "bước sửa": len(cay["buocSua"]),
        "nhánh": len(nhanh),
        "dòng bảng": len(bang),
        "nhánh CÓ nguồn": sum(1 for x in nhanh + bang if x.get("chungCu") == "evidence-noi-ro"),
        "triệu chứng vào": len(cay["trieuChungVao"]),
        "khoảng trống tự khai": len(cay.get("khoangTrongPhaiBiet") or []),
    }


def o_cua_case(cay):
    """case -> tên ô phân bố. Đây là phép so CHẶT NHẤT có được: cùng một tập ticket,
    mỗi ticket vào đúng một ô, nên bất đồng chỉ ra được đích danh từng ticket."""
    ra = {}
    pb = cay.get("phanBoBuocKiemDuocGhiLai") or {}
    for o in NAM_O:
        for c in (pb.get(o) or {}).get("case") or []:
            ra[c] = o
    return ra


def case_duoc_dung(cay):
    """Mọi mã ticket mà bản nháp thực sự dẫn ở đâu đó — trừ phần phân bố.

    Phân bố bị loại CỐ Ý: nó buộc phải liệt kê đủ 100% ticket của nhóm (phép cộng phải
    khớp), nên gộp nó vào thì mọi bản nháp đều phủ 100% và con số mất hết ý nghĩa.
    """
    ra = set()
    for b in cay["buocKiem"]:
        for n in b["nhanh"]:
            ra |= set(n.get("case") or [])
    for d in cac_dong_bang(cay):
        ra |= set(d.get("case") or [])
    for s in cay["buocSua"]:
        ra |= set(s.get("case") or [])
    for t in cay["trieuChungVao"]:
        ra |= set(t.get("case") or [])
    xn = cay.get("buocXacNhan") or {}
    ra |= set(xn.get("case") or [])
    return ra


def in_bang_hai_cot(tieu_de, hang):
    rong = max(len(k) for k, _, _ in hang)
    print("  %-*s   %5s %5s %7s" % (rong, tieu_de, "A", "B", "lệch"))
    for ten, a, b in hang:
        dau = "" if a == b else "  <-"
        print("  %-*s   %5s %5s %7s%s"
              % (rong, ten, a, b, ("%+d" % (b - a)) if a != b else "·", dau))


def so_sanh(pa, pb):
    a, b = doc(pa), doc(pb)
    if a["nhom"] != b["nhom"]:
        print("🛑 Hai file KHÔNG cùng nhóm:\n   A: %s\n   B: %s" % (a["nhom"], b["nhom"]),
              file=sys.stderr)
        return 1

    print("=" * 78)
    print("diff(A, B) — %s" % a["nhom"])
    print("  A (người dựng) : %s" % os.path.basename(pa))
    print("  B (máy sinh)   : %s" % os.path.basename(pb))
    print("  %d ticket" % a["soCaseDungSau"])
    print()

    print("1. MÔ TẢ — đếm thô, cùng trục")
    ma, mb = mo_ta(a), mo_ta(b)
    in_bang_hai_cot("", [(k, ma[k], mb[k]) for k in ma])
    print()

    print("2. PHÂN BỐ — so theo TỪNG TICKET (phép so chặt nhất)")
    oa, ob = o_cua_case(a), o_cua_case(b)
    chung = sorted(set(oa) & set(ob))
    lech = [c for c in chung if oa[c] != ob[c]]
    print("  cùng ô : %d/%d" % (len(chung) - len(lech), len(chung)))
    print("  khác ô : %d/%d" % (len(lech), len(chung)))
    for c in lech:
        print("     %-12s A: %-28s B: %s" % (c, oa[c], ob[c]))
    chi_a, chi_b = sorted(set(oa) - set(ob)), sorted(set(ob) - set(oa))
    if chi_a or chi_b:
        print("  ⚠ ticket chỉ có trong phân bố của một bên — phép cộng của bên kia thiếu:")
        if chi_a:
            print("     chỉ A: %s" % ", ".join(chi_a))
        if chi_b:
            print("     chỉ B: %s" % ", ".join(chi_b))
    print()

    print("3. ĐỘ PHỦ NGUỒN — ticket nào được DẪN ở thân bản nháp (không tính phân bố)")
    da, db = case_duoc_dung(a), case_duoc_dung(b)
    tong = a["soCaseDungSau"]
    print("  A dẫn %d/%d ticket · B dẫn %d/%d" % (len(da), tong, len(db), tong))
    if da - db:
        print("     chỉ A dẫn: %s" % ", ".join(sorted(da - db)))
    if db - da:
        print("     chỉ B dẫn: %s" % ", ".join(sorted(db - da)))
    print()

    print("4. BA CÁCH TÍNH — 🛑 CHƯA CHỐT cách nào (docs/11 §8 mục 3)")
    c1 = len(lech) / len(chung) if chung else 0
    truc = ["bước kiểm", "bước sửa", "nhánh", "dòng bảng", "triệu chứng vào"]
    tong_a = sum(ma[k] for k in truc)
    c2 = sum(abs(ma[k] - mb[k]) for k in truc) / tong_a if tong_a else 0
    nga = ma["nhánh CÓ nguồn"] / max(1, ma["nhánh"] + ma["dòng bảng"])
    ngb = mb["nhánh CÓ nguồn"] / max(1, mb["nhánh"] + mb["dòng bảng"])
    print("  (a) chỉ tính bất đồng PHÂN BỐ, theo ticket        %5.1f%%  (%d/%d ticket)"
          % (c1 * 100, len(lech), len(chung)))
    print("  (b) chênh lệch MÔ TẢ, chuẩn hoá theo bản A        %5.1f%%" % (c2 * 100))
    print("  (c) chênh lệch TỈ LỆ NHÁNH CÓ NGUỒN               %5.1f điểm %%  (A %.0f%% · B %.0f%%)"
          % (abs(nga - ngb) * 100, nga * 100, ngb * 100))
    print()
    print("  ⚠ Ba con số này KHÔNG cộng lại thành một điểm, và cố ý không có điểm tổng.")
    print("    (a) đo bất đồng NHẬN ĐỊNH trên cùng một tập ticket — chặt nhất, nhưng chỉ")
    print("        phủ đúng một trường. (b) đo độ chi tiết, và bản dài hơn không đồng nghĩa")
    print("        đúng hơn. (c) đo mức chống lưng bằng ca thật, đây là thứ gần `S8` nhất.")
    return 0


def bat_cap():
    """Bắt cặp A/B theo tên nhóm bên trong file, không theo tên file."""
    theo_nhom = {}
    for p in sorted(glob.glob(os.path.join(PHAN_TICH, "*.json"))):
        try:
            d = doc(p)
        except (ValueError, OSError):
            continue
        if not isinstance(d, dict) or "buocKiem" not in d:
            continue
        ben = "B" if os.path.basename(p).startswith("may-sinh") else "A"
        theo_nhom.setdefault(d["nhom"], {}).setdefault(ben, []).append(p)
    cap = []
    for nhom, ben in theo_nhom.items():
        if ben.get("A") and ben.get("B"):
            cap.append((ben["A"][0], ben["B"][0]))
        else:
            thieu = "B (máy sinh)" if not ben.get("B") else "A (người dựng)"
            print("· bỏ qua %s — chưa có bản %s" % (nhom[:52], thieu))
    return cap


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("file", nargs="*", help="A.json B.json")
    ap.add_argument("--tat-ca", action="store_true", help="mọi cặp bắt được trong thư mục")
    args = ap.parse_args()

    if args.tat_ca:
        cap = bat_cap()
        if not cap:
            print("Không bắt được cặp A/B nào.", file=sys.stderr)
            return 1
        ma = 0
        for pa, pb in cap:
            ma |= so_sanh(pa, pb)
            print()
        return ma
    if len(args.file) != 2:
        ap.print_help()
        return 2
    return so_sanh(args.file[0], args.file[1])


if __name__ == "__main__":
    sys.exit(main())
