#!/usr/bin/env python3
"""Lấy vật liệu của MỘT nhóm nguyên nhân ra để dựng SOP — không phải quay lại Jira.

Sinh ra 2026-09-07, khi bắt đầu dựng cây quyết định cho nhóm SOP đầu tiên. Ba việc
script này làm, mỗi việc vì một chỗ đã vấp:

1. `--liet-ke`   đếm case của từng nhóm bằng `caseKeys`.
   Vì tài liệu từng ghi "nhóm SOP lớn nhất" ở số ít, trong khi có HAI nhóm cùng 10 case.
   Đếm bằng code thì không ai phải nhớ.

2. `--nhom <tên>` in ra toàn bộ vật liệu của một nhóm: tiêu đề case, nguyên nhân đã rút,
   và NGUYÊN VĂN mọi mẩu evidence.
   Vì `docs/10` §6 từng ghi rằng bằng chứng then chốt "phải kéo lại từ Jira" — sai:
   nó đã nằm trong corpus 150 case trên đĩa. Kéo lại Jira là ~6 phút và một lần chạm
   vào dữ liệu khách không cần thiết.

3. `--trung-lap`  tìm những đoạn văn bản LẶP NGUYÊN VĂN qua nhiều case.
   Vì phép đọc tay đã tìm được một khối 793 ký tự giống hệt ở `ES-346396` và `ES-341290`,
   cách nhau 3 tuần, cùng một nhân viên gõ. Đó là một SOP đã tồn tại dưới dạng
   copy-paste — tức "10%" của con số 10/30/60 KHÔNG nằm trong tài liệu mà nằm trong
   luồng ticket. Việc đó đo được, nên phải đo bằng code chứ không bằng mắt.

    python scripts/jira-export/nhom_sop.py --liet-ke
    python scripts/jira-export/nhom_sop.py --nhom "Phân quyền"
    python scripts/jira-export/nhom_sop.py --nhom "Phân quyền" --ra nhom.txt
    python scripts/jira-export/nhom_sop.py --trung-lap --toi-thieu 200

⚠ Mặc định đọc `fixture-*.json` (credential đã thay bằng giá trị GIẢ giữ nguyên hình
  dạng), KHÔNG đọc `dry-run-*.json`. Cùng 150 case / 345 mẩu, nhưng bản fixture không
  nhân bản bí mật ra thêm một chỗ nữa. Muốn bản thô thì truyền `--cases/--evidence`.
"""

from __future__ import annotations

import argparse
import difflib
import io
import json
import os
import re
import sys
from collections import defaultdict

for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

HERE = os.path.dirname(os.path.abspath(__file__))
GOC = os.path.dirname(os.path.dirname(HERE))
PHAN_TICH = os.path.join(GOC, "docs", "ket-qua-phan-tich")

# Cửa sổ dùng để phát hiện trùng lặp. Nhỏ quá thì bắt cả câu chào mẫu.
CUA_SO = 120
# ⚠ BƯỚC PHẢI LÀ 1, và đây là một lỗi đã xảy ra thật trong bản đầu của file này.
# Bản đầu dùng bước 20 cho CẢ HAI chuỗi. Hai chuỗi lấy cửa sổ theo lưới riêng, nên một
# đoạn giống hệt nhau chỉ khớp khi độ lệch vị trí giữa hai bên đúng bội số của 20 —
# xác suất 1/20. Nó bỏ sót ĐÚNG khối 784 ký tự đã tìm được bằng tay ở `ES-346396` ↔
# `ES-341290`, tức phép kiểm báo "không có" trong khi thứ nó đi tìm đang nằm đó.
# Tìm ra vì con số đo bằng code bị đem so với con số đo bằng tay. Giữ nguyên chú thích
# này: đó là cùng một hình dạng lỗi với `IM-22` (guard báo xanh trong khi dữ liệu rò).
BUOC = 1


def doc_json(duong_dan: str):
    with io.open(duong_dan, encoding="utf-8") as f:
        return json.load(f)


def khoa(tham_chieu: str) -> str:
    """`jira:ES-123#comment-9` -> `ES-123`."""
    return tham_chieu.replace("jira:", "").split("#")[0]


def nap(args):
    tax = doc_json(os.path.join(PHAN_TICH, "taxonomy-19-nhom-hoa-don.json"))
    nn = doc_json(os.path.join(PHAN_TICH, "nguyen-nhan-150-case.json"))
    cases = doc_json(args.cases)
    evidence = doc_json(args.evidence)
    theo_case = defaultdict(list)
    for e in evidence:
        theo_case[khoa(e["caseSourceReference"])].append(e)
    return (
        tax,
        {c["key"]: c for c in nn["cases"]},
        {khoa(c["sourceReference"]): c for c in cases},
        theo_case,
    )


def liet_ke(tax, nguyen_nhan, cases, theo_case) -> None:
    print("%-4s %-6s %s" % ("case", "mẩu", "nhóm"))
    print("-" * 78)
    for g in sorted(tax["nhom"], key=lambda g: -len(g["caseKeys"])):
        so_mau = sum(len(theo_case.get(k, [])) for k in g["caseKeys"])
        print("%-4d %-6d %s" % (len(g["caseKeys"]), so_mau, g["ten"]))
    thieu = tax.get("khongXacDinhDuoc")
    if isinstance(thieu, list):
        thieu = len(thieu)
    print("-" * 78)
    print("%-4d %-6s %s" % (sum(len(g["caseKeys"]) for g in tax["nhom"]), "", "TỔNG có nhóm"))
    print("%-4s %-6s %s" % (thieu, "", "không xác định được nguyên nhân"))


def in_nhom(tax, nguyen_nhan, cases, theo_case, mau_ten: str, ra) -> int:
    khop = [g for g in tax["nhom"] if mau_ten.lower() in g["ten"].lower()]
    if not khop:
        print("Không nhóm nào khớp %r. Chạy --liet-ke để xem tên." % mau_ten, file=sys.stderr)
        return 2
    if len(khop) > 1:
        print("Khớp %d nhóm — nói rõ hơn:" % len(khop), file=sys.stderr)
        for g in khop:
            print("  · " + g["ten"], file=sys.stderr)
        return 2
    g = khop[0]
    p = lambda *a: print(*a, file=ra)
    p("=" * 78)
    p("NHÓM: %s   (%d case)" % (g["ten"], len(g["caseKeys"])))
    p("=" * 78)
    p("moTa: " + g["moTa"])
    for ck in g["caseKeys"]:
        c = cases.get(ck, {})
        n = nguyen_nhan.get(ck, {})
        p("")
        p("-" * 78)
        p("%s | %s" % (ck, c.get("subject", "(không có trong corpus này)")))
        if n:
            p("  nguyên nhân : " + str(n.get("nguyenNhan")))
            p("  mức chắc chắn: %s | bước xử lý đã gán nhãn: %s"
              % (n.get("mucChacChan"), ", ".join(n.get("buocXuLy") or [])))
        for e in theo_case.get(ck, []):
            p("  --- %s  (%s, %d ký tự)"
              % (e["sourceReference"], e["observedAt"], len(e["content"])))
            p("  " + e["content"].replace("\n", "\n  "))
    return 0


def trung_lap(theo_case, toi_thieu: int, ra) -> None:
    """Tìm đoạn lặp NGUYÊN VĂN qua nhiều case: băm cửa sổ trượt rồi mới so chi tiết."""
    p = lambda *a: print(*a, file=ra)
    chuan = {}
    for ck, ds in theo_case.items():
        chuan[ck] = re.sub(r"\s+", " ", " ".join(e["content"] for e in ds)).strip()

    vi_tri = defaultdict(set)
    for ck, s in chuan.items():
        for i in range(0, max(1, len(s) - CUA_SO + 1), BUOC):
            vi_tri[hash(s[i:i + CUA_SO])].add(ck)

    ung_vien = set()
    for cks in vi_tri.values():
        if len(cks) >= 2:
            ds = sorted(cks)
            for a in range(len(ds)):
                for b in range(a + 1, len(ds)):
                    ung_vien.add((ds[a], ds[b]))

    ket_qua = []
    for a, b in ung_vien:
        sm = difflib.SequenceMatcher(None, chuan[a], chuan[b], autojunk=False)
        m = max(sm.get_matching_blocks(), key=lambda m: m.size)
        if m.size >= toi_thieu:
            ket_qua.append((m.size, a, b, chuan[a][m.a:m.a + m.size]))

    p("Đoạn lặp NGUYÊN VĂN giữa hai case khác nhau, dài ≥ %d ký tự" % toi_thieu)
    p("(%d cặp ứng viên sau bước băm, %d cặp đứng vững)" % (len(ung_vien), len(ket_qua)))
    p("=" * 78)
    for size, a, b, doan in sorted(ket_qua, reverse=True):
        p("")
        p("%d ký tự — %s ↔ %s" % (size, a, b))
        p("  " + (doan[:400] + (" …[cắt]" if len(doan) > 400 else "")))
    if not ket_qua:
        p("(không có)")
    p("")
    p("⚠ ĐỌC KỸ TRƯỚC KHI KẾT LUẬN: đoạn lặp KHÔNG tự động là một SOP. Phần lớn văn bản")
    p("  lặp trong corpus này là CÂU TRẢ LỜI MẪU (\"Kính gửi Quý khách…\", thông báo kênh")
    p("  Zalo đang thử nghiệm) — lặp vì được dán tự động, không vì ai đó đang truyền lại")
    p("  tri thức. Chỉ đoạn nào chứa BƯỚC LÀM mới là SOP.")


def kiem_cay(tax, duong_dan: str) -> int:
    """Đối chiếu một cây quyết định với taxonomy. Trả số phát hiện CHẶN.

    Vì sao cần: cây quyết định là một bản nháp SOP viết tay, và thứ dễ sai nhất ở nó
    KHÔNG phải nội dung mà là **phép cộng** — một case bị bỏ quên, một case bị đếm hai
    lần, một nhánh trỏ tới bước sửa không tồn tại. Ba lỗi đó im lặng. Phép kiểm này là
    bộ xương của eval sau này cho `ISoạnNhápSOP`: máy sinh ra cây thì cũng phải qua đây.
    """
    cay = doc_json(duong_dan)
    khop = [g for g in tax["nhom"] if g["ten"] == cay["nhom"]]
    if not khop:
        print("CHẶN: nhóm %r không có trong taxonomy." % cay["nhom"])
        return 1
    trong_nhom = set(khop[0]["caseKeys"])
    ngoai_nhom_duoc_phep = set((cay.get("_haiCaseLech") or {}).keys())
    chan = []

    if len(trong_nhom) != cay.get("soCaseDungSau"):
        chan.append("soCaseDungSau = %s nhưng taxonomy có %d case."
                    % (cay.get("soCaseDungSau"), len(trong_nhom)))

    ma_dich = {b["ma"] for b in cay["buocKiem"]} | {s["ma"] for s in cay["buocSua"]}
    # Đích không phải một bước cụ thể. `BANG` chỉ sang bảng tra (topology của nhóm 2),
    # `K-XN` sang bước xác nhận, `NGOAI-PHAM-VI` sang một nhóm nguyên nhân khác.
    ma_dich.add("NGOAI-PHAM-VI")
    if cay.get("buocXacNhan"):
        ma_dich.add("K-XN")
    bang = (cay.get("bangTraMaLoi") or {}).get("dong") or []
    if bang:
        ma_dich.add("BANG")
    trong_nhanh = set()
    for b in cay["buocKiem"]:
        for n in b["nhanh"]:
            trong_nhanh |= set(n["case"])
            if n["diToi"] not in ma_dich:
                chan.append("%s: nhánh trỏ tới %r — không có bước nào mang mã đó."
                            % (b["ma"], n["diToi"]))
            if n.get("chungCu") not in ("evidence-noi-ro", "toi-suy-ra"):
                chan.append("%s: nhánh %r thiếu chungCu hợp lệ." % (b["ma"], n["quanSat"][:30]))

    # Bảng tra: một DÒNG là một nhánh, nên phải qua đúng những phép kiểm như nhánh.
    # Thêm một ràng buộc riêng: khoá của một dòng là (nhà cung cấp, mã lỗi), không phải
    # mã lỗi một mình — hai NCC trả cùng một mã vẫn có thể trỏ tới hai trường khác nhau.
    khoa_da_gap = set()
    for i, d in enumerate(bang):
        ten = "bảng dòng %d (%s/%s)" % (i + 1, d.get("ncc"), str(d.get("maLoi"))[:24])
        trong_nhanh |= set(d.get("case") or [])
        for truong in ("ncc", "maLoi", "truongBiTuChoi", "diToi"):
            if not d.get(truong):
                chan.append("%s: thiếu trường %r." % (ten, truong))
        if d.get("diToi") and d["diToi"] not in ma_dich:
            chan.append("%s: trỏ tới %r — không có bước nào mang mã đó." % (ten, d["diToi"]))
        if d.get("chungCu") not in ("evidence-noi-ro", "toi-suy-ra"):
            chan.append("%s: thiếu chungCu hợp lệ." % ten)
        khoa = (d.get("ncc"), d.get("maLoi"))
        if khoa in khoa_da_gap:
            chan.append("%s: khoá (nhà cung cấp, mã lỗi) BẮT TRÙNG — hai dòng cùng khoá thì "
                        "tra ra hai trường khác nhau, không ai biết dùng dòng nào." % ten)
        khoa_da_gap.add(khoa)

    la = sorted(trong_nhanh - trong_nhom - ngoai_nhom_duoc_phep)
    if la:
        chan.append("case ở trong cây mà KHÔNG thuộc nhóm và không được giải thích ở "
                    "_haiCaseLech: %s" % ", ".join(la))

    pb = cay.get("phanBoBuocKiemDuocGhiLai") or {}
    dem, tat_ca = 0, []
    for k, v in pb.items():
        if k.startswith("_"):
            continue
        dem += v["so"]
        tat_ca += v["case"]
        if v["so"] != len(v["case"]):
            chan.append("phân bố %r: so = %d nhưng liệt kê %d case." % (k, v["so"], len(v["case"])))
    if dem != len(trong_nhom):
        chan.append("phân bố cộng lại = %d, nhóm có %d case." % (dem, len(trong_nhom)))
    trung = sorted(c for c in set(tat_ca) if tat_ca.count(c) > 1)
    if trung:
        chan.append("case bị đếm HAI LẦN trong phân bố: %s" % ", ".join(trung))
    thieu = sorted(trong_nhom - set(tat_ca))
    if thieu:
        chan.append("case của nhóm KHÔNG có mặt trong phân bố: %s" % ", ".join(thieu))

    print("Kiểm cây: %s" % duong_dan)
    print("  nhóm            : %s (%d case)" % (cay["nhom"], len(trong_nhom)))
    print("  bước kiểm/sửa   : %d / %d" % (len(cay["buocKiem"]), len(cay["buocSua"])))
    nhanh_co_nguon = sum(1 for b in cay["buocKiem"] for n in b["nhanh"]
                         if n.get("chungCu") == "evidence-noi-ro")
    tong_nhanh = sum(len(b["nhanh"]) for b in cay["buocKiem"])
    if bang:
        nhanh_co_nguon += sum(1 for d in bang if d.get("chungCu") == "evidence-noi-ro")
        tong_nhanh += len(bang)
        print("  bảng tra        : %d dòng, khoá (nhà cung cấp, mã lỗi)" % len(bang))
    print("  nhánh có nguồn  : %d/%d (còn lại là suy ra từ kết luận)" % (nhanh_co_nguon, tong_nhanh))
    if chan:
        print("\n%d PHÁT HIỆN CHẶN:" % len(chan))
        for c in chan:
            print("  🛑 " + c)
    else:
        print("\n✅ Phép cộng khớp: không case nào bị bỏ quên hay đếm hai lần.")
    return len(chan)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--liet-ke", action="store_true", help="đếm case của từng nhóm")
    ap.add_argument("--nhom", help="in vật liệu của nhóm có tên khớp chuỗi này")
    ap.add_argument("--trung-lap", action="store_true", help="tìm đoạn lặp nguyên văn")
    ap.add_argument("--kiem-cay", metavar="FILE",
                    help="đối chiếu một cây quyết định với taxonomy (mã thoát ≠ 0 nếu lệch)")
    ap.add_argument("--toi-thieu", type=int, default=200, help="ngưỡng ký tự cho --trung-lap")
    ap.add_argument("--ra", help="ghi ra file thay vì stdout")
    ap.add_argument("--cases", default=os.path.join(HERE, "fixture-cases.json"))
    ap.add_argument("--evidence", default=os.path.join(HERE, "fixture-evidence.json"))
    args = ap.parse_args()

    if not (args.liet_ke or args.nhom or args.trung_lap or args.kiem_cay):
        ap.print_help()
        return 2

    # --kiem-cay KHÔNG cần corpus, chỉ cần taxonomy — nên nó chạy được trên máy trắng,
    # trước cả khi ai kịp xuất lại dữ liệu Jira. Cố ý: một phép kiểm cần corpus mới chạy
    # được là một phép kiểm sẽ không ai chạy.
    if args.kiem_cay:
        tax = doc_json(os.path.join(PHAN_TICH, "taxonomy-19-nhom-hoa-don.json"))
        so_chan = kiem_cay(tax, args.kiem_cay)
        if not (args.liet_ke or args.nhom or args.trung_lap):
            return 1 if so_chan else 0

    for d in (args.cases, args.evidence):
        if not os.path.exists(d):
            print("Thiếu %s — corpus KHÔNG theo git. Dựng lại theo docs/10 §3." % d,
                  file=sys.stderr)
            return 1

    tax, nguyen_nhan, cases, theo_case = nap(args)
    ra = io.open(args.ra, "w", encoding="utf-8") if args.ra else sys.stdout
    try:
        if args.liet_ke:
            liet_ke(tax, nguyen_nhan, cases, theo_case)
        if args.nhom:
            ma = in_nhom(tax, nguyen_nhan, cases, theo_case, args.nhom, ra)
            if ma:
                return ma
        if args.trung_lap:
            trung_lap(theo_case, args.toi_thieu, ra)
    finally:
        if args.ra:
            ra.close()
            print("Đã ghi " + args.ra)
    return 0


if __name__ == "__main__":
    sys.exit(main())
