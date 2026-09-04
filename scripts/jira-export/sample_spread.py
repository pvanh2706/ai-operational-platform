#!/usr/bin/env python3
"""Lấy mẫu RẢI ĐỀU theo tháng, thay vì "N case gần nhất".

⚠ SINH RA TỪ MỘT LỖI LẤY MẪU THẬT, ghi lại vì nó rất dễ lặp lại.
`export_jira_to_channel1.py` với `ORDER BY resolved DESC` + `MAX_ISSUES=150` được gọi là
"mẫu 12 tháng", nhưng đo lại thì 150 case đó chỉ trải **24 ngày** (2026-08-11 →
2026-09-03). Lý do đơn giản mà không ai nghĩ tới lúc đặt JQL: project có ~2723 case
hoá đơn đã đóng mỗi năm, nên 150 case *gần nhất* chỉ ăn hết 6,6% khoảng thời gian.

Hệ quả cho `R-K4` (đếm nguyên nhân): nếu tập nguyên nhân thay đổi theo phiên bản sản
phẩm — mà nó gần như chắc chắn có, vì mỗi bản vá đóng lại một nhóm nguyên nhân và mở ra
nhóm khác — thì mẫu 24 ngày cho con số **cận dưới rất xa**. Đếm trên đó rồi kết luận
"tập nguyên nhân hữu hạn nhỏ" là đúng thứ dẫn tới quyết định kiến trúc sai.

    "N case gần nhất" KHÔNG phải mẫu của N tháng. Nó là mẫu của một cửa sổ hẹp mà
    độ hẹp phụ thuộc lưu lượng — và lưu lượng thì không ai kiểm tra khi viết JQL.

Script này chia khoảng thời gian thành từng tháng và lấy đều mỗi tháng, nên số case mỗi
tháng bằng nhau kể cả khi lưu lượng thật lệch nhau nhiều lần.

    cmd /c "call scripts\\jira-export\\jira-config.bat && python scripts\\jira-export\\sample_spread.py"

Đọc `JIRA_JQL` làm phần LỌC (bỏ mọi mệnh đề ngày và ORDER BY nếu có), rồi tự thêm khoảng
ngày cho từng tháng. Ghi ra `spread-cases.json` + `spread-evidence.json` để không đè
`dry-run-*.json`.
"""

from __future__ import annotations

import importlib.util
import io
import json
import os
import re
import sys
from datetime import date

HERE = os.path.dirname(os.path.abspath(__file__))

for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass


def nap_module(ten: str, duong_dan: str):
    """Nạp một file .py cạnh script này thành module.

    ⚠ `sys.modules[ten] = m` PHẢI đứng trước `exec_module`, không phải sau. `@dataclass`
    tra `sys.modules[cls.__module__]` ngay lúc decorator chạy, nên thiếu dòng đó thì
    module đích chết với `AttributeError: 'NoneType' object has no attribute '__dict__'`
    — một thông báo không hề nhắc gì tới nguyên nhân thật.
    """
    spec = importlib.util.spec_from_file_location(ten, duong_dan)
    m = importlib.util.module_from_spec(spec)
    sys.modules[ten] = m
    spec.loader.exec_module(m)
    return m


# Dùng lại nguyên phần gọi Jira của export script — kể cả retry và chuẩn hoá timestamp.
# Viết lại một bản thứ hai là tạo ra đúng cái bẫy `IM-12`: hai đường code cùng đọc Jira,
# đường ít chạy hơn mục dần.
ex = nap_module("ex", os.path.join(HERE, "export_jira_to_channel1.py"))

SO_THANG = 12
MOI_THANG = 12  # 12 × 12 = 144 case, cùng cỡ với mẫu 150 để so được


def bo_menh_de_ngay(jql: str) -> str:
    """Bỏ mọi mệnh đề ngày và ORDER BY khỏi JQL của người dùng, giữ phần lọc chủ đề."""
    j = re.sub(r"(?i)\s+ORDER\s+BY\s+.*$", "", jql).strip()
    # Bỏ "AND <field> >= "..."" / "< "...."" cho created/resolved/resolutiondate/updated
    j = re.sub(r'(?i)\s+AND\s+(?:created(?:Date)?|resolved|resolutiondate|updated)\s*'
               r'[<>=!]+\s*("[^"]*"|\S+)', "", j)
    return j.strip()


def cac_thang(den: date, so: int) -> list[tuple[str, str]]:
    """Trả [(dau, cuoi)] cho `so` tháng gần nhất tính lùi từ `den`, cũ trước."""
    moc = []
    y, m = den.year, den.month
    for _ in range(so + 1):
        moc.append(date(y, m, 1))
        m -= 1
        if m == 0:
            y, m = y - 1, 12
    moc.reverse()
    return [(moc[i].isoformat(), moc[i + 1].isoformat()) for i in range(len(moc) - 1)]


def main() -> None:
    cfg = ex.read_config()
    loc = bo_menh_de_ngay(cfg.jql)
    print(f"Jira:      {cfg.jira_base}")
    print(f"Phần lọc:  {loc}")
    print(f"Lấy {MOI_THANG} case × {SO_THANG} tháng = {MOI_THANG * SO_THANG} case\n")

    hom_nay = date.fromisoformat(os.environ.get("SAMPLE_UNTIL", "2026-09-04"))
    tat_ca: list[dict] = []
    thong_ke: list[tuple[str, int, int]] = []

    for dau, cuoi in cac_thang(hom_nay, SO_THANG):
        jql = f'{loc} AND resolved >= "{dau}" AND resolved < "{cuoi}" ORDER BY resolved ASC'
        rieng = ex.Config(jira_base=cfg.jira_base, jira_auth_header=cfg.jira_auth_header,
                          jql=jql, app_base=cfg.app_base, signal_key=cfg.signal_key,
                          max_issues=MOI_THANG)
        try:
            issues = ex.fetch_issues(rieng)
        except ex.LoiThoangQua as e:
            print(f"  {dau}: LỖI MẠNG, bỏ tháng này — {e}")
            thong_ke.append((dau, 0, 0))
            continue
        # fetch_issues in tổng của cả tháng trước khi cắt; lấy lại để biết mức lấy mẫu
        tat_ca.extend(issues)
        thong_ke.append((dau, len(issues), 0))
        print(f"  {dau} → {cuoi}: lấy {len(issues)} case")

    if not tat_ca:
        print("Không lấy được case nào.", file=sys.stderr)
        sys.exit(1)

    print(f"\nĐọc description + comment cho {len(tat_ca)} case…")
    cases, evidence = ex.build_signals(cfg, tat_ca)

    for ten, data in (("spread-cases.json", cases), ("spread-evidence.json", evidence)):
        p = os.path.join(HERE, ten)
        io.open(p, "w", encoding="utf-8").write(
            json.dumps(data, ensure_ascii=False, indent=2))
        print(f"  đã ghi {p}")

    xong = [c for c in cases if c.get("sourceResolvedAt")]
    if xong:
        moc = sorted(c["sourceResolvedAt"] for c in xong)
        print(f"\n  {len(cases)} case, {len(evidence)} evidence")
        print(f"  trải từ {moc[0][:10]} đến {moc[-1][:10]}  "
              f"<-- SO CHỖ NÀY với mẫu 'N case gần nhất'")
    print("\nKiểm trước khi dùng:")
    print("  python scripts/jira-export/check_corpus.py spread-cases.json spread-evidence.json")


if __name__ == "__main__":
    main()
