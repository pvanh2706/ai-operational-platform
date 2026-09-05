#!/usr/bin/env python3
"""Biến corpus dry-run thật thành FIXTURE nạp được vào kp_dev.

Quyết định 1 (2026-09-04): lô 32 case CHỈ vào kp_dev làm fixture, KHÔNG vào kho
tri thức thật. Xem docs/07_MVP_IMPLEMENTATION.md muc AR-j.

VÌ SAO KHÔNG NẠP THẲNG FILE DRY-RUN:
Corpus chứa 6 bộ thông tin đăng nhập CÒN SỐNG của 6 khách sạn thật, và email thật
của khách hàng. Fixture sống lâu hơn mọi thứ khác trong repo — nó bị chép sang máy
khác, bị dump ra khi debug, bị đưa vào issue khi báo lỗi. Nhân bản secret vào đó là
tạo thêm bản sao của đúng thứ vừa phải đi xoay.

VÌ SAO KHÔNG XOÁ HẲN:
Fixture này tồn tại để TEST LUẬT CHE. Xoá sạch thì không còn gì để luật bắt, và bộ
test sẽ xanh vì không có việc gì làm — đúng kiểu "biết xanh mà không biết đỏ" mà cả
dự án đang chống.

CÁCH LÀM: thay giá trị thật bằng giá trị GIẢ GIỮ NGUYÊN HÌNH DẠNG. Cùng số chữ số,
cùng cách nhóm, cùng vị trí trong câu. Luật che nhìn thấy y hệt, còn bí mật thì không
đi theo.

⚠ Thay bằng danh sách TƯỜNG MINH, không phải regex. Cố ý: regex trên dữ liệu thật vừa
bỏ sót (đo được 11%) vừa ăn nhầm dữ liệu cần giữ — `80771` là số đặt phòng và
`0304746657` là mã số thuế, cả hai PHẢI ở lại. Danh sách tay thì review được từng dòng.

Chạy:  python scripts/jira-export/make_fixture.py
Ra:    scripts/jira-export/fixture-cases.json + fixture-evidence.json
"""

from __future__ import annotations

import json
import os
import sys

# Windows: khi output bi chuyen huong (pipe, ghi file, hoac chay tu trinh khac), Python
# dat stdout ve codepage ANSI cua he thong - cp1252 tren may nay - va moi chu tieng Viet
# lam no nem UnicodeEncodeError. Da vap that 2026-09-04: script chay dung khi in ra
# console, chet ngay khi `| head`. Ep UTF-8 o day, khong bat nguoi chay phai nho dat
# PYTHONIOENCODING - thu chi hong luc bi pipe la thu se hong dung luc dang go loi.
for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

HERE = os.path.dirname(os.path.abspath(__file__))

# (giá trị thật, giá trị giả cùng hình dạng, mô tả để in ra và để review)
# Nguồn: vòng quét corpus 2026-09-04, đã phản biện đối kháng từng cái.
THAY_THE = [
    # --- Bộ nguy hiểm nhất: hoá đơn điện tử VNPT, ES-346481 ---
    ("Vnpt@2026",   "Xxxx@0000", "mật khẩu VNPT (dùng cho CẢ HAI tài khoản)"),
    ("ketoankhachsan", "taikhoanmau", "tài khoản dịch vụ VNPT"),
    ("4500621073sv", "0000000000sv", "tài khoản portal VNPT (= MST + 'sv')"),
    # MST 4500621073 KHÔNG thay: nó là định danh công khai của doanh nghiệp, và nó là
    # thứ luật che PHẢI học cách bỏ qua. Thay nó đi là dạy luật sai.

    # --- Năm bộ điều khiển từ xa. ID gắn cứng theo máy nên nguy hơn mật khẩu ---
    ("24235840",    "10000001", "Ultraviewer ID — Osaka hotel (ES-346661)"),
    ("46169",       "10001",    "Ultraviewer mật khẩu — Osaka hotel"),
    ("70 761 691",  "10 000 002", "Ultraviewer ID — The One Hotel 2 (ES-346602)"),
    ("64467",       "10002",    "Ultraviewer mật khẩu — The One Hotel 2"),
    ("78 726 060",  "10 000 003", "Ultraviewer ID — Eden Star Saigon (ES-346425)"),
    ("22297",       "10003",    "Ultraviewer mật khẩu — Eden Star Saigon"),
    ("107 293 745", "100 000 004", "Ultraviewer ID — ANCARINE (ES-346406)"),
    ("55663",       "10004",    "Ultraviewer mật khẩu — ANCARINE"),
    ("50439160",    "10000005", "Ultraviewer ID — Sơn Tiên (ES-346405)"),
    ("21548",       "10005",    "Ultraviewer mật khẩu — Sơn Tiên"),

    # --- Email cá nhân thật của khách hàng ---
    ("kimngocchuong1008@gmail.com", "khachhang001@example.com", "email khách hàng"),
    ("dathoanggg17092002@gmail.com", "khachhang002@example.com", "email khách hàng"),
    ("mirahhotel.sales@gmail.com",  "khachsan003@example.com", "email khách sạn"),
    ("chanhvy@gmail.com",           "khachhang004@example.com", "email khách hàng"),

    # --- Số điện thoại cá nhân ---
    ("0708095465",  "0900000001", "số điện thoại khách sạn (ES-346425)"),
    ("0982 048 187", "0900 000 002", "di động trong chữ ký email (ES-346759)"),
]

# Cặp trùng byte đã xác nhận bằng hash — giữ lại CẢ HAI trong fixture, cố ý.
# Fixture phải chứa được cả ca xấu, nếu không thì luật khử trùng không có gì để bắt.
GHI_CHU_TRUNG = [
    ("jira:ES-346619#description", "jira:ES-346618#description"),
    ("jira:ES-346761#comment-803556", "jira:ES-346608#comment-803527"),
]


def main() -> None:
    # ⚠ Bắt FileNotFoundError và nói rõ phải làm gì. Đo được 2026-09-05 bằng cách clone
    # sạch repo rồi chạy: bản trước chết bằng traceback, và người mới không có cách nào
    # đoán ra rằng file bị thiếu là CỐ Ý (nó trong .gitignore vì là dữ liệu khách hàng).
    # Một script bắt buộc phải chạy sau một script khác thì phải TỰ NÓI RA điều đó.
    try:
        with open(os.path.join(HERE, "dry-run-cases.json"), encoding="utf-8") as f:
            cases = json.load(f)
        with open(os.path.join(HERE, "dry-run-evidence.json"), encoding="utf-8") as f:
            evidence = json.load(f)
    except FileNotFoundError as e:
        print(f"Chưa có {os.path.basename(e.filename)}.\n"
              "File này KHÔNG nằm trong repo (dữ liệu vận hành của khách, xem .gitignore).\n"
              "Chạy trước:\n"
              '  cmd /c "call scripts\\jira-export\\jira-config.bat && '
              'python scripts\\jira-export\\export_jira_to_channel1.py --dry-run"',
              file=sys.stderr)
        sys.exit(2)

    dem = {mo_ta: 0 for _, _, mo_ta in THAY_THE}
    cham_vao = set()

    def thay(text: str, ref: str) -> str:
        for that, gia, mo_ta in THAY_THE:
            if that in text:
                dem[mo_ta] += text.count(that)
                cham_vao.add(ref)
                text = text.replace(that, gia)
        return text

    for c in cases:
        c["subject"] = thay(c["subject"], c["sourceReference"])
    for e in evidence:
        e["content"] = thay(e["content"], e["sourceReference"])

    print("=" * 66)
    print("ĐÃ THAY (giá trị thật -> giả cùng hình dạng)")
    print("=" * 66)
    tong = 0
    for _, _, mo_ta in THAY_THE:
        n = dem[mo_ta]
        tong += n
        trang_thai = f"{n} chỗ" if n else "KHÔNG TÌM THẤY  <-- kiểm lại"
        print(f"  {trang_thai:<22} {mo_ta}")
    print(f"\n  Tổng {tong} chỗ, trên {len(cham_vao)} mẩu.")

    # Kiểm ngược: những thứ PHẢI Ở LẠI thì phải còn nguyên. Không có phép kiểm này
    # thì một luật che quá tay sẽ đi qua mà không ai biết.
    print("\n" + "=" * 66)
    print("KIỂM NGƯỢC — dữ liệu CẦN GIỮ phải còn nguyên")
    print("=" * 66)
    ca_kho = [
        ("80771", "số đặt phòng — hình dạng giống mật khẩu 5 số"),
        ("0304746657", "mã số thuế — hình dạng giống số điện thoại"),
        ("4500621073", "MST khách sạn — định danh công khai"),
        ("Ultraview", "tên công cụ — bước nghiệp vụ SẠCH, SOP cần nó"),
    ]
    het = "".join(e["content"] for e in evidence)
    for chuoi, vi_sao in ca_kho:
        con = chuoi in het
        print(f"  {'CÒN' if con else 'MẤT  <-- SAI':<14} {chuoi:<14} {vi_sao}")

    print("\n" + "=" * 66)
    print("CA XẤU GIỮ LẠI CÓ CHỦ ĐÍCH (fixture phải dạy được luật)")
    print("=" * 66)
    for a, b in GHI_CHU_TRUNG:
        print(f"  trùng byte: {a}\n              {b}")
    n_ngan = sum(1 for e in evidence if len(e["content"].strip()) < 30)
    n_high = sum(1 for e in evidence if e["machineReadability"] == "High")
    print(f"  mẩu dưới 30 ký tự: {n_ngan}  (để test luật lọc rác)")
    print(f"  nhãn High: {n_high}/{len(evidence)}  (giữ nguyên — đây LÀ bug AR-k,"
          f" fixture phải tái hiện được nó)")

    for ten, data in (("fixture-cases.json", cases), ("fixture-evidence.json", evidence)):
        p = os.path.join(HERE, ten)
        with open(p, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        print(f"\n  đã ghi {p}")

    print("\nNạp vào kp_dev (app phải đang chạy):")
    print("  python scripts/jira-export/load_fixture.py")


if __name__ == "__main__":
    main()
