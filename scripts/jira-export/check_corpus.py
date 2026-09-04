#!/usr/bin/env python3
"""Kiểm một corpus dry-run TRƯỚC KHI nạp. Chạy được trên bất kỳ cặp file dry-run nào.

Sinh ra từ hai lần vấp CÙNG MỘT HÌNH DẠNG LỖI trong một ngày (2026-09-04):

    machineReadability = "High"  ở 128/128 mẩu evidence     -> `AR-k`
    Mã khách sạn       = -1.0    ở  32/32  case trên Jira   -> `AR-l`

Cả hai là **một trường phủ 100% với MỘT giá trị duy nhất**. Nó tệ hơn trường rỗng:
trường rỗng thì ai cũng thấy là thiếu, còn trường phủ-100%-một-giá-trị thì trông như đã
có dữ liệu, và mọi phép kiểm *"trường này có được điền không?"* đều trả lời CÓ. Chỉ phép
kiểm *"trường này có mấy giá trị PHÂN BIỆT?"* mới thấy.

    LUẬT: với mọi trường dùng làm ranh giới hay bộ lọc, đếm số giá trị PHÂN BIỆT,
    không đếm độ phủ. Một giá trị duy nhất = coi như rỗng.

Script trả **mã thoát khác 0** khi có phát hiện chặn, để cắm được vào CI. Nó cố ý KHÔNG
sửa gì — chỉ nói ra, vì mọi cách sửa đều là quyết định (`AR-j` chưa chốt luật che).

    python scripts/jira-export/check_corpus.py
    python scripts/jira-export/check_corpus.py fixture-cases.json fixture-evidence.json
"""

from __future__ import annotations

import hashlib
import json
import os
import re
import sys
from collections import Counter

for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

HERE = os.path.dirname(os.path.abspath(__file__))
# Một nhãn chiếm từ đây trở lên thì trường coi như không phân loại được gì.
NGUONG_HANG_SO = 0.95

chan: list[str] = []      # phát hiện CHẶN nạp
canh_bao: list[str] = []  # đáng biết, không chặn


def muc(tieu_de: str) -> None:
    print("\n" + "=" * 68)
    print(tieu_de)
    print("=" * 68)


def kiem_truong_phan_loai(evidence: list) -> None:
    """LUẬT CHÍNH của file này. Xem docstring đầu file."""
    muc("1. TRƯỜNG PHÂN LOẠI - đếm giá trị PHÂN BIỆT, không đếm độ phủ")
    if not evidence:
        return
    n = len(evidence)
    for truong in ("machineReadability",):
        c = Counter((e.get(truong) or "(rỗng)") for e in evidence)
        print(f"\n  {truong}: {len(c)} giá trị phân biệt trên {n} mẩu")
        for gt, so in c.most_common():
            ty_le = so / n
            co = "   <-- HẰNG SỐ" if ty_le >= NGUONG_HANG_SO else ""
            print(f"      {so:>4}x  ({ty_le:>5.1%})  {gt}{co}")
        cao_nhat = c.most_common(1)[0][1] / n
        if cao_nhat >= NGUONG_HANG_SO:
            chan.append(
                f"`{truong}` có một nhãn chiếm {cao_nhat:.1%} - trường không phân loại "
                "được gì. Ai lọc bằng nó là lọc gần hết dữ liệu (AR-k).")


def kiem_trung_byte(evidence: list, cases: list) -> None:
    muc("2. BẢN TRÙNG BYTE - làm hỏng phép đếm 'x/N case đã làm bước này'")
    for ten, du_lieu, noi_dung in (("evidence", evidence, "content"),
                                   ("case", cases, "subject")):
        theo_hash: dict[str, list[str]] = {}
        for x in du_lieu:
            h = hashlib.sha256((x.get(noi_dung) or "").encode()).hexdigest()
            theo_hash.setdefault(h, []).append(x.get("sourceReference", "?"))
        trung = {h: v for h, v in theo_hash.items() if len(v) > 1}
        print(f"  {ten}: {len(trung)} nhóm trùng byte")
        for v in list(trung.values())[:6]:
            print("      " + " == ".join(v))
        if trung:
            canh_bao.append(f"{len(trung)} nhóm {ten} trùng byte - khử trùng trước khi nạp.")


def quet_bi_mat(evidence: list) -> list[tuple[str, str, str]]:
    """Trả về [(khoá nguồn, hình dạng nào bắt được, giá trị)]. TÁCH RIÊNG để ĐO ĐƯỢC.

    ⚠ Vì sao tách khỏi phần in: đo recall bằng cách đọc lại output là một phép đo sai,
    và đã sai thật khi thử (2026-09-04). Hai lý do, cả hai đều âm thầm:
      · output bị cắt bớt cho dễ đọc, nên "không thấy trong output" ≠ "luật không bắt"
      · chính dòng cảnh báo cuối hàm có chứa `80771` và `0304746657`, nên phép đo
        precision đếm luôn dòng cảnh báo và báo là ăn nhầm
    Một luật cần đo thì phải trả về DỮ LIỆU, không trả về chữ đã định dạng để người đọc.
    """
    CONG_CU = r"(?i)ultra ?view|utlatra|teamview|anydesk"
    # ⚠ Dấu tiếng Việt phải liệt kê ĐỦ: "khẩu" là kh-ẩ-u, không phải kh-â-u. Bản đầu
    # thiếu chữ 'ẩ' và vì thế trượt đúng một credential ("Mật khẩu: 46169").
    NHAN = (r"(?i)(?:m[aậâạ][tj]?\s*kh[aâẩ]u|matkhau|pass(?:word)?|pw|pwd"
            r"|ID(?:\s*c[uủ]a\s*b[aạ]n)?)")
    SO = r"\d{2,3}[ ]\d{3}[ ]\d{3}|\d{4,9}"

    cong_cu = re.compile(CONG_CU)
    nhan_va_so = re.compile(NHAN + r"\s*[:=]?\s*(" + SO + r")")
    so_tran = re.compile(r"^\s*(?:" + SO + r")\s*$")
    lay_so = re.compile(SO)
    # `account`/`username` cũng vào đây: bản đầu chỉ bắt password nên để lọt cả hai
    # tài khoản VNPT. Một cặp đăng nhập thiếu nửa nào cũng vẫn là nửa bị rò.
    key_json = re.compile(
        r"(?i)\"?(a?c?pass\w*|password|matkhau|token|secret|api[_-]?key|account"
        r"|user(?:name)?|login)\"?\s*[\":=]\s*\"?([^\"\s,}]{3,})")

    # ⚠ Bỏ URL trước khi quét. Đo được một false positive thật trên corpus 12 tháng:
    # `...&table_id=23&id=2469&area=4` bị bắt vì nhãn `ID` khớp `id=`. Tham số truy vấn
    # trong URL không phải bí mật, và che nó đi thì mất một đường dẫn mà người duyệt cần
    # để mở lại đúng màn hình đang lỗi. Thay bằng khoảng trắng chứ không xoá hẳn, để
    # hình dạng 3 ("dãy số trần ở dòng kế tiếp") không bị lệch số dòng.
    url = re.compile(r"https?://\S+")

    ra: list[tuple[str, str, str]] = []
    for e in evidence:
        noi_dung = e.get("content") or ""
        dong = [url.sub(" <URL> ", d) for d in noi_dung.splitlines()]
        ref = e.get("sourceReference", "?")
        for i, d in enumerate(dong):
            for m in nhan_va_so.finditer(d):          # hình dạng 1
                ra.append((ref, "nhãn+số", m.group(1)))
            mc = cong_cu.search(d)
            if mc:                                    # hình dạng 2
                for s in lay_so.findall(d[mc.end():]):
                    ra.append((ref, "công cụ+số", s))
            if mc or nhan_va_so.search(d):            # hình dạng 3
                for k in range(i + 1, min(i + 7, len(dong))):
                    if so_tran.match(dong[k]):
                        ra.append((ref, "số trần", dong[k].strip()))
        for m in key_json.finditer(url.sub(" <URL> ", noi_dung)):  # hình dạng 4
            ra.append((ref, "key JSON", m.group(2)[:40]))
    return ra


def kiem_bi_mat(evidence: list) -> None:
    """Quét bí mật theo BỐN HÌNH DẠNG đã thấy trong dữ liệu thật, không theo giá trị.

    ⚠ RECALL ĐÃ ĐO, không phải phỏng đoán. Đáp án là danh sách 13 credential xác định
    bằng tay trong `make_fixture.py` (đã qua phản biện đối kháng):

        luật theo DÒNG (từ khoá phải cùng dòng với giá trị) ->   11%
        luật theo NGỮ CẢNH, bản đầu                        ->   23%
        luật theo BỐN HÌNH DẠNG dưới đây                   ->  100%  (13/13)
        và 0/5 lần ăn nhầm dữ liệu cần giữ

    ⚠ NHƯNG 100% ĐÓ LÀ CẬN TRÊN, KHÔNG PHẢI ƯỚC LƯỢNG ĐÚNG. Luật được sửa THEO chính
    corpus dùng để đo nó — đó là overfit theo định nghĩa. Bằng chứng ngay trong dự án:
    corpus 12 tháng lộ ra một hình dạng thứ NĂM (JWT sau chữ "Token:") mà luật này bắt
    được nhờ `token` tình cờ có trong danh sách key JSON, không nhờ thiết kế. Hình dạng
    thứ sáu sẽ tới, và nó sẽ không may như vậy.
    → Sửa luật thì PHẢI ĐO LẠI. Thêm một mẫu rồi tin là đã tốt hơn chính là cách một
      luật che biến thành sự an tâm giả.

    ⚠ Và một bài học về CÁCH ĐO, đắt hơn con số: lần đầu tôi đo bằng cách đọc lại output
    của hàm in, ra 92% rồi 69% — cả hai đều SAI. Output bị cắt cho dễ đọc nên "không
    thấy" ≠ "không bắt"; và dòng cảnh báo cuối hàm có chứa `80771` nên phép đo precision
    đếm luôn chính dòng cảnh báo. Vì thế `quet_bi_mat` trả về DỮ LIỆU, còn `kiem_bi_mat`
    chỉ in. Muốn đo một thứ thì thứ đó phải trả về dữ liệu.

    Bốn hình dạng, tất cả đều lấy nguyên văn từ corpus:
      1  `ID của bạn: 24235840` / `Mật khẩu: 46169`     nhãn + giá trị CÙNG DÒNG
      2  `Ultraview: 70 761 691    //    64467`          NHIỀU giá trị cùng dòng
      3  `107 293 745` rồi `55663` ở hai tin nhắn        dãy số TRẦN, cách nhau vài dòng
      4  `"Account": "ketoankhachsan"`                   key JSON, key KHÔNG phải password
    """
    muc("3. BÍ MẬT - quét theo BỐN HÌNH DẠNG thật (AR-j)")
    bat = quet_bi_mat(evidence)

    for ref, loai, gt in bat[:24]:
        print(f"      [{loai:<11}] {ref}: {gt}")
    if len(bat) > 24:
        print(f"      ... và {len(bat) - 24} chỗ nữa (không in hết)")

    print(f"\n  Tổng {len(bat)} chỗ nghi là bí mật, trên "
          f"{len({r for r, _, _ in bat})} mẩu.")
    if bat:
        chan.append(f"{len(bat)} chỗ nghi la credential - xu ly truoc khi nap (AR-j).")
    print("  Luu y: luat co Y KHONG bat day so tran khi khong co nhan hay ten cong cu")
    print("  o gan - lam vay se an nham so dat phong va ma so thue, ca hai CAN GIU.")


def kiem_rang_buoc_fts(cases: list, evidence: list) -> None:
    muc("4. RÀNG BUỘC FTS (AR-h) - ba cái đo được ngay trên văn bản")
    gach = re.compile(r"(?:^|\s)-\S")
    n = sum(1 for c in cases if gach.search(c.get("subject") or ""))
    print(f"  tiêu đề chứa token bắt đầu bằng '-': {n}/{len(cases)}")
    print("      -> dán tiêu đề vào ô tìm là truy vấn trả 0 dòng, KỂ CẢ chính nó")

    co_dau = re.compile(r"[àáảãạăâèéẻẽẹêìíỉĩịòóỏõọôơùúủũụưỳýỷỹỵđ]", re.I)
    nd = sum(1 for c in cases if co_dau.search(c.get("subject") or ""))
    print(f"  tiêu đề có dấu tiếng Việt:           {nd}/{len(cases)}")
    print("      -> gõ KHÔNG DẤU không khớp gì với cấu hình 'simple'")

    def la_dump(t: str | None) -> bool:
        s = (t or "").strip()
        return s.startswith(("{", "[", "<")) or '"Body"' in s or s.count("{") > 8

    dumps = [e for e in evidence if la_dump(e.get("content"))]
    tong = sum(len(e.get("content") or "") for e in evidence) or 1
    ty = sum(len(e.get("content") or "") for e in dumps) / tong
    print(f"  mẩu là dump JSON/XML/log:            {len(dumps)}/{len(evidence)}")
    print(f"      -> chiếm {ty:.0%} TỔNG ký tự; đo được là đè xếp hạng 29:1")
    if ty > 0.2:
        canh_bao.append(f"dump kỹ thuật chiếm {ty:.0%} tổng ký tự - sẽ đè bẹp xếp hạng FTS.")


def kiem_chat_lieu(cases: list, evidence: list) -> None:
    muc("5. CHẤT LIỆU PATH A - sau khi trừ rác thì còn gì")
    theo_case: Counter = Counter()
    for e in evidence:
        noi_dung = (e.get("content") or "").strip()
        if len(noi_dung) >= 120 and not noi_dung.startswith(("{", "[", "<")):
            theo_case[e.get("caseSourceReference") or ""] += 1
    dung_duoc = sum(theo_case.values())
    xong = [c for c in cases if c.get("sourceResolvedAt")]

    print(f"  case:                      {len(cases)}")
    print(f"  case đã xong:              {len(xong)}  ({len(xong) / max(1, len(cases)):.0%})")
    print(f"  evidence:                  {len(evidence)}")
    print(f"  evidence DÙNG ĐƯỢC:        {dung_duoc}  "
          f"({dung_duoc / max(1, len(evidence)):.0%}; >=120 ký tự, không phải dump)")
    print(f"  case có 0 mẩu dùng được:   {len(cases) - len(theo_case)}")
    if cases:
        tb = dung_duoc / len(cases)
        print(f"  trung bình mẩu dùng được/case: {tb:.1f}")
        if tb < 2:
            canh_bao.append(f"chỉ {tb:.1f} mẩu dùng được mỗi case - mỏng cho một bản nháp gom.")

    print("\n  ⚠ 'đã xong' KHÔNG phải tín hiệu tốt để chọn case. Đo được trên corpus")
    print("    2026-09-04: 7/7 case đã đóng chỉ ghi 'hết triệu chứng' ('Done nhé', 23 ký")
    print("    tự), còn nguyên nhân thật nằm ở case CÒN MỞ. Xem 00_CURRENT_STATE.")


def main() -> None:
    a = sys.argv[1] if len(sys.argv) > 1 else "dry-run-cases.json"
    b = sys.argv[2] if len(sys.argv) > 2 else "dry-run-evidence.json"
    try:
        with open(os.path.join(HERE, a), encoding="utf-8") as f:
            cases = json.load(f)
        with open(os.path.join(HERE, b), encoding="utf-8") as f:
            evidence = json.load(f)
    except FileNotFoundError as e:
        print(f"Không thấy {e.filename}. Chạy export --dry-run trước.", file=sys.stderr)
        sys.exit(2)

    print(f"Kiểm {a} ({len(cases)} case) + {b} ({len(evidence)} evidence)")
    kiem_truong_phan_loai(evidence)
    kiem_trung_byte(evidence, cases)
    kiem_bi_mat(evidence)
    kiem_rang_buoc_fts(cases, evidence)
    kiem_chat_lieu(cases, evidence)

    muc("KẾT LUẬN")
    for m in canh_bao:
        print(f"  ⚠  {m}")
    for m in chan:
        print(f"  🛑 {m}")
    if not chan:
        print("  Không có phát hiện chặn.")
        sys.exit(0)
    print(f"\n  {len(chan)} phát hiện CHẶN - đừng nạp vào kho tri thức thật khi chưa xử lý.")
    sys.exit(1)


if __name__ == "__main__":
    main()
