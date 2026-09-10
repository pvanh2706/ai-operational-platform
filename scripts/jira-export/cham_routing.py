#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""Chấm phép đo ROUTING: một ticket MỚI ĐẾN thuộc nhóm nguyên nhân nào?

Vì sao phép đo này tồn tại
--------------------------
Cả tính năng "có issue mới thì hệ thống hướng dẫn cách giải quyết" đứng trên một
bước chưa ai đo: ĐỊNH TUYẾN ticket mới vào một trong 19 nhóm nguyên nhân.

Con số 34% / AUC 0,61 ở `thu_retrieval.py` là đo RETRIEVAL (tìm case giống), KHÔNG
phải đo ROUTING (phân vào 19 lớp). Hai bài toán khác nhau.

Đầu vào của phép đo là ĐÚNG thứ tồn tại lúc ticket đến: tiêu đề + phần mô tả.
KHÔNG có comment, KHÔNG có transcript — vì transcript chứa sẵn câu chẩn đoán, và
`thu_retrieval.py` đã đo được rằng dùng cả transcript còn TỆ HƠN dùng tin nhắn đầu.
Đưa transcript vào đây là đưa đáp án vào đề.

⚠ Ba cái bẫy mà bộ chấm này canh, vì thiếu một cái là ra số vô nghĩa:

1. ĐOÁN MÙ. Một bộ định tuyến luôn trả nhóm lớn nhất cũng có accuracy > 0. Không so
   với nó thì mọi con số đều "nghe được". `thu_retrieval.py` học bài này rồi: FTS
   34% trông ổn cho tới lúc biết đoán mù được 31%.

2. TỰ TIN SAI vs NÓI KHÔNG BIẾT. 62/150 case trong corpus có nguyên nhân
   "khong-xac-dinh-duoc". Một bộ định tuyến gán nhóm cho những case đó là đang sinh
   ra hướng dẫn SAI cho nhân viên. Nên phải đo riêng tỉ lệ đó, và precision (gán thì
   có đúng không) quan trọng hơn accuracy thô.

3. BỊA TÊN NHÓM. Nếu bộ định tuyến trả về một tên nhóm không có trong taxonomy thì
   đó là bịa, cùng loại lỗi mà `G6`/`AP3` chặn ở bản nháp SOP. Script trả mã thoát
   khác 0 khi gặp — cắm được vào CI.

Cách dùng
---------
    python scripts/jira-export/cham_routing.py <ket-lo1.json> [<ket-lo2.json> ...]

Mỗi file kết quả có dạng {"ES-xxxxx": {"nhom": ..., "nguon": ..., "vietTat": ...}}.
Đáp án được dựng lại TỪ REPO (taxonomy + nguyen-nhan-150-case), không truyền vào.
"""

import io
import json
import os
import sys
from collections import Counter

# Python tren Windows: khi output bi pipe, stdout ve cp1252 va moi chu tieng Viet nem
# UnicodeEncodeError. Quy uoc cua repo la SCRIPT TU EP UTF-8, khong bat nguoi goi dat
# PYTHONIOENCODING — xem docs/10 §5.2. Khoi nay chep tu check_corpus.py.
for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

KHONG_BIET = "KHONG-BIET"


def goc_repo():
    d = os.path.dirname(os.path.abspath(__file__))
    while d and not os.path.isdir(os.path.join(d, ".git")):
        cha = os.path.dirname(d)
        if cha == d:
            raise SystemExit("Khong tim thay goc repo (.git)")
        d = cha
    return d


def doc_json(p):
    # encoding tuong minh: Python tren Windows mac dinh cp1252, xem docs/07 IM-28
    return json.load(io.open(p, encoding="utf-8"))


def main(argv):
    # `--phan`: cham khi CHUA co du 150 case. Co that su can, vi 5 lo chay song song
    # va bat duoc loi hinh dang o lo dau re hon nhieu so voi phat hien sau khi ca 5 xong.
    # ⚠ Mac dinh la CHAN khi thieu case: mot bo cham im lang bo qua case thieu se bao
    # accuracy tren tap con ma trong nhu accuracy tren ca corpus.
    phan = "--phan" in argv
    argv = [a for a in argv if a != "--phan"]

    if not argv:
        raise SystemExit(__doc__)

    goc = goc_repo()
    kq = os.path.join(goc, "docs", "ket-qua-phan-tich")
    tax = doc_json(os.path.join(kq, "taxonomy-19-nhom-hoa-don.json"))
    ngn = doc_json(os.path.join(kq, "nguyen-nhan-150-case.json"))
    cases = doc_json(os.path.join(goc, "scripts", "jira-export", "fixture-cases.json"))
    evs = doc_json(os.path.join(goc, "scripts", "jira-export", "fixture-evidence.json"))

    ten_nhom = {g["ten"] for g in tax["nhom"]}
    co_nhan = {}
    for g in tax["nhom"]:
        for k in g["caseKeys"]:
            co_nhan[k] = g["ten"]
    kich_co = {g["ten"]: len(g["caseKeys"]) for g in tax["nhom"]}
    muc_chac = {c["key"]: c["mucChacChan"] for c in ngn["cases"]}

    def khoa(s):
        return s.replace("jira:", "").split("#")[0]

    tat_ca = [khoa(c["sourceReference"]) for c in cases]
    co_mo_ta = {
        khoa(e["caseSourceReference"])
        for e in evs
        if e["sourceReference"].endswith("#description")
    }

    # ── gom ket qua tu cac lo ─────────────────────────────────────────────────
    # Nhan hai hinh dang: map phang {ma: {...}} (thu ma tung lo ghi ra), hoac ban da gop
    # co khoi xuat xu {_ghiChu, _soTicket, ketQua} — dung quy uoc cua nguyen-nhan-150-case.
    # Khoa bat dau bang "_" luon la sieu du lieu, khong phai mot ticket.
    tra_loi = {}
    for p in argv:
        raw = doc_json(p)
        muc = raw.get("ketQua", raw) if isinstance(raw, dict) else raw
        for k, v in muc.items():
            if k.startswith("_"):
                continue
            if k in tra_loi:
                print("LOI: %s xuat hien o hai lo" % k)
                return 2
            tra_loi[k] = v

    loi_chan = []

    thieu = [k for k in tat_ca if k not in tra_loi]
    la = [k for k in tra_loi if k not in set(tat_ca)]
    if thieu and not phan:
        loi_chan.append("thieu %d case: %s (dung --phan de cham mot phan)"
                        % (len(thieu), ", ".join(thieu[:8])))
    if la:
        loi_chan.append("co %d ma khong thuoc corpus: %s" % (len(la), ", ".join(la[:8])))

    bia = sorted({v.get("nhom") for v in tra_loi.values()
                  if v.get("nhom") not in ten_nhom and v.get("nhom") != KHONG_BIET})
    if bia:
        loi_chan.append("BIA TEN NHOM (%d): %s" % (len(bia), " | ".join(bia[:5])))

    print("=" * 78)
    print("CHAM ROUTING — %d ticket, %d nhom" % (len(tra_loi), len(ten_nhom)))
    print("=" * 78)

    if loi_chan:
        print()
        for m in loi_chan:
            print("  [CHAN] " + m)
        print()
        print("Khong cham tiep: bo ket qua khong dung hinh dang.")
        return 1

    if phan and thieu:
        # Chi cham tren case DA co tra loi, va NOI RA do phu — mot ti le tren tap con
        # ma khong ghi do phu la mot ti le de bi doc nham thanh ti le tren ca corpus.
        print()
        print("  [MOT PHAN] cham tren %d/%d case (con thieu %d)"
              % (len(tat_ca) - len(thieu), len(tat_ca), len(thieu)))
        tat_ca = [k for k in tat_ca if k in tra_loi]

    # ── phan chia ─────────────────────────────────────────────────────────────
    nhan = [k for k in tat_ca if k in co_nhan]
    khong_nhan = [k for k in tat_ca if k not in co_nhan]

    def la_kb(k):
        return tra_loi[k].get("nhom") == KHONG_BIET

    gan_dung = [k for k in nhan if not la_kb(k) and tra_loi[k]["nhom"] == co_nhan[k]]
    gan_sai = [k for k in nhan if not la_kb(k) and tra_loi[k]["nhom"] != co_nhan[k]]
    nhan_kb = [k for k in nhan if la_kb(k)]

    kn_gan = [k for k in khong_nhan if not la_kb(k)]
    kn_kb = [k for k in khong_nhan if la_kb(k)]

    def pct(a, b):
        return "%5.1f%%" % (100.0 * a / b) if b else "  n/a"

    print()
    print("--- 88 case CO NHAN (dap an = mot trong 19 nhom) ---")
    print("  gan DUNG nhom      : %3d / %d  %s   <- accuracy" % (len(gan_dung), len(nhan), pct(len(gan_dung), len(nhan))))
    print("  gan SAI nhom       : %3d / %d  %s" % (len(gan_sai), len(nhan), pct(len(gan_sai), len(nhan))))
    print("  tra KHONG-BIET     : %3d / %d  %s" % (len(nhan_kb), len(nhan), pct(len(nhan_kb), len(nhan))))
    da_gan = len(gan_dung) + len(gan_sai)
    print("  precision khi DAM gan: %3d / %d  %s   <- con so san pham quan tam" % (len(gan_dung), da_gan, pct(len(gan_dung), da_gan)))

    print()
    print("--- 62 case KHONG XAC DINH DUOC NGUYEN NHAN (dap an = khong co nhom) ---")
    print("  dung dan tra KHONG-BIET : %3d / %d  %s" % (len(kn_kb), len(khong_nhan), pct(len(kn_kb), len(khong_nhan))))
    print("  TU TIN GAN nhom (sai)   : %3d / %d  %s   <- huong dan SAI cho nhan vien" % (len(kn_gan), len(khong_nhan), pct(len(kn_gan), len(khong_nhan))))

    # ── duong co so: doan mu ──────────────────────────────────────────────────
    lon_nhat = max(kich_co, key=lambda t: kich_co[t])
    print()
    print("--- DUONG CO SO (bat buoc, xem docstring) ---")
    print("  luon tra nhom lon nhat (%s...)" % lon_nhat[:34])
    print("     accuracy tren 88 case co nhan : %s  (%d/%d)"
          % (pct(kich_co[lon_nhat], len(nhan)), kich_co[lon_nhat], len(nhan)))
    print("     tu tin gan tren 62 case rong  : 100.0%  (khong bao gio noi khong biet)")
    print("  luon tra KHONG-BIET")
    print("     accuracy tren 88 case co nhan :   0.0%")
    print("     dung dan tren 62 case rong    : 100.0%")

    # ── tach theo nguon va theo co mo ta hay khong ────────────────────────────
    print()
    print("--- Tach theo NGUON tu khai (chi tren 88 case co nhan) ---")
    for ng in ("noi-ro-trong-mo-ta", "suy-tu-trieu-chung"):
        tap = [k for k in nhan if tra_loi[k].get("nguon") == ng and not la_kb(k)]
        d = [k for k in tap if tra_loi[k]["nhom"] == co_nhan[k]]
        print("  %-20s %3d case, dung %3d  %s" % (ng, len(tap), len(d), pct(len(d), len(tap))))

    print()
    print("--- Tach theo LUC DEN CO GI (chi tren 88 case co nhan) ---")
    for nhan_hieu, dk in (("co tieu de + mo ta", True), ("CHI co tieu de", False)):
        tap = [k for k in nhan if (k in co_mo_ta) == dk]
        d = [k for k in tap if not la_kb(k) and tra_loi[k]["nhom"] == co_nhan[k]]
        kb = [k for k in tap if la_kb(k)]
        print("  %-20s %3d case, dung %3d  %s, khong-biet %d"
              % (nhan_hieu, len(tap), len(d), pct(len(d), len(tap)), len(kb)))

    print()
    print("--- Nhom bi gan NHIEU NHAT so voi kich co thuc ---")
    dem = Counter(v["nhom"] for v in tra_loi.values() if v.get("nhom") != KHONG_BIET)
    for ten, so in dem.most_common(6):
        print("  %-52s gan %3d  |  thuc %2d" % (ten[:52], so, kich_co.get(ten, 0)))
    print("  tong tra KHONG-BIET: %d / %d  %s"
          % (len(nhan_kb) + len(kn_kb), len(tat_ca), pct(len(nhan_kb) + len(kn_kb), len(tat_ca))))

    print()
    print("--- 10 case gan SAI dau tien (de doc bang mat) ---")
    for k in gan_sai[:10]:
        print("  %s  dap an: %s" % (k, co_nhan[k][:44]))
        print("            gan   : %s" % tra_loi[k]["nhom"][:44])
        print("            vi sao: %s" % (tra_loi[k].get("vietTat") or "")[:66])

    print()
    print("=" * 78)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
