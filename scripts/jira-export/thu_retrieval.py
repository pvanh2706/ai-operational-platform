#!/usr/bin/env python3
"""PHÉP THỬ QUYẾT ĐỊNH: Postgres full-text search có đủ để tìm đúng nhóm SOP không?

Đây là phép thử mà kết luận `R-K4` (docs/09) đặt ra để đóng câu hỏi đắt nhất còn lại —
có cần dựng vector DB / RAG hay không. Ngưỡng được ĐẶT TRƯỚC khi chạy, không phải sau:

    top-3 recall < 60%   ->  FTS không đủ, embedding đáng thử
    top-3 recall 60-70%  ->  vùng xám, cần nhìn thêm case thất bại
    top-3 recall > 70%   ->  ĐÓNG câu hỏi. Không dựng vector DB.

CÁCH MÔ PHỎNG — chỗ này quyết định phép thử có ý nghĩa hay không:

Truy vấn là **TIN NHẮN ĐẦU TIÊN của khách**, cắt ngay trước lượt trả lời đầu của nhân
viên. Đó đúng là thứ có trong tay lúc một case mới đến: triệu chứng khách tự mô tả, chưa
ai chẩn đoán gì. Dùng cả transcript làm truy vấn là gian lận — nó chứa sẵn câu kết luận
mà hệ thống đang phải đi tìm.

Xếp hạng theo kiểu **leave-one-out**: khi thử case X, X bị loại khỏi kho. Không loại thì
X luôn tự khớp với chính nó và điểm số vô nghĩa.

Nhãn đúng: nhóm SOP của X theo taxonomy 19 nhóm đã chốt 2026-09-05.
Tính điểm: lấy 3 case đứng đầu, gom nhóm của chúng — nhóm đúng có nằm trong đó không.

⚠ HAI CẤU HÌNH được đo, và khoảng cách giữa chúng là một kết quả riêng:
   `simple`            token thô, phân biệt dấu — bản mặc định
   `simple + unaccent` bỏ dấu, để "danh sach" khớp "danh sách"
   30/32 tiêu đề corpus có dấu tiếng Việt (`AR-h` #5), nên chênh lệch này là thứ phải
   biết trước khi chọn cấu hình cho sản phẩm.

Chạy:  python scripts/jira-export/thu_retrieval.py
"""

from __future__ import annotations

import io
import json
import os
import re
import sys

import psycopg  # nếu thiếu: dùng nhánh psql ở dưới

HERE = os.path.dirname(os.path.abspath(__file__))

for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

DSN = os.environ.get(
    "KP_DSN", "host=localhost port=5432 dbname=kp_dev user=kp_app password=123456")

# Dòng nhãn người nói của transcript Jira: *Tên_<Vai trò>_ngày giờ:*
NHAN_NOI = re.compile(r"^\*(.+?)_<(Khách hàng|Nhân viên)>_", re.M)


def tin_nhan_dau(noi_dung: str) -> str:
    """Cắt lấy phần khách nói TRƯỚC lượt trả lời đầu tiên của nhân viên.

    Không có nhãn người nói (mô tả thường, không phải transcript chat) thì lấy 600 ký tự
    đầu — vẫn là "phần đầu", vẫn chưa có chẩn đoán.
    """
    moc = list(NHAN_NOI.finditer(noi_dung))
    if not moc:
        return noi_dung[:600]
    nhan_vien = next((m for m in moc if m.group(2) == "Nhân viên"), None)
    if nhan_vien is None:
        return noi_dung[:600]
    return noi_dung[: nhan_vien.start()][:1200]


def nap_taxonomy() -> dict:
    """Tìm taxonomy theo thứ tự: biến TAXONOMY -> chỗ mặc định trong repo.

    ⚠ Đường dẫn mặc định trỏ vào `docs/ket-qua-phan-tich/` chứ không phải `docs/`.
    Bản đầu của hàm này trỏ vào `docs/taxonomy19.json` — một chỗ file CHƯA BAO GIỜ nằm;
    lúc đó file còn ở thư mục tạm của phiên làm việc và chỉ chạy được nhờ biến môi
    trường. Ai clone repo về sẽ vấp ngay. Sửa 2026-09-05 khi chuẩn bị chuyển máy.
    """
    tu_bien = os.environ.get("TAXONOMY", "")
    macdinh = os.path.join(HERE, "..", "..", "docs", "ket-qua-phan-tich",
                           "taxonomy-19-nhom-hoa-don.json")
    for p in (tu_bien, macdinh):
        if p and os.path.exists(p):
            return json.load(io.open(p, encoding="utf-8"))
    print(
        "Không tìm thấy taxonomy. Thử hai chỗ:\n"
        f"  biến TAXONOMY = {tu_bien or '(chưa đặt)'}\n"
        f"  mặc định      = {os.path.normpath(macdinh)}\n"
        "File mặc định có trong repo; nếu thiếu thì repo bị cắt xén hoặc bạn đang chạy "
        "từ ngoài cây nguồn.", file=sys.stderr)
    sys.exit(2)


def main() -> None:
    tax = nap_taxonomy()

    nhom_cua = {}
    ten_nhom = {}
    for i, n in enumerate(tax["nhom"]):
        ten_nhom[i] = n["ten"]
        for k in n["caseKeys"]:
            nhom_cua[k] = i

    # Corpus nhận từ tham số dòng lệnh, mặc định là bản dry-run.
    # ⚠ KHÔNG ghi cứng `dry-run-*.json` như bản trước: taxonomy trong repo gán nhãn cho
    # 88 case của corpus `dry-run`, và 0/144 case của corpus `spread` — trong khi
    # `docs/10` §3 lại khuyên dùng `spread` cho mọi phép đếm. Ai làm theo lời khuyên đó
    # sẽ chạy script này trên một corpus không giao nhau với nhãn nào cả.
    f_case = sys.argv[1] if len(sys.argv) > 1 else "dry-run-cases.json"
    f_ev = sys.argv[2] if len(sys.argv) > 2 else "dry-run-evidence.json"
    try:
        cases = json.load(io.open(os.path.join(HERE, f_case), encoding="utf-8"))
        ev = json.load(io.open(os.path.join(HERE, f_ev), encoding="utf-8"))
    except FileNotFoundError as e:
        print(f"Chưa có {os.path.basename(e.filename)}.\n"
              "File này KHÔNG nằm trong repo (dữ liệu vận hành của khách, xem .gitignore).\n"
              "Lấy corpus trước — xem docs/10_CHUYEN_MAY.md §3.\n"
              f"Dùng corpus khác: python {os.path.basename(__file__)} <cases.json> <evidence.json>",
              file=sys.stderr)
        sys.exit(2)

    subj = {c["sourceReference"].split(":", 1)[1]: c["subject"] for c in cases}
    desc = {}
    for e in ev:
        if e["sourceReference"].endswith("#description"):
            k = e["sourceReference"].split(":", 1)[1].split("#")[0]
            desc[k] = e["content"]

    # Chỉ lấy case CÓ NHÃN — 62 case "không xác định được" không có nhóm nên không chấm được.
    mau = [k for k in nhom_cua if k in subj]
    print(f"Case có nhãn nhóm: {len(mau)} / {len(cases)}   (corpus: {f_case})")
    print(f"Nhóm: {len(ten_nhom)}\n")

    # ⚠ Taxonomy và corpus phải NÓI VỀ CÙNG MỘT TẬP CASE. Không giao nhau thì mọi phép
    # chia sau đây là chia cho 0, và bản trước chết bằng `ZeroDivisionError` ở tận cuối
    # — sau khi đã nạp xong cả kho vào Postgres. Kiểm ở đây, trước khi tốn công.
    if not mau:
        print(f"KHÔNG có case nào của corpus `{f_case}` mang nhãn trong taxonomy.\n"
              f"  taxonomy gán nhãn cho {len(nhom_cua)} case\n"
              f"  corpus có {len(cases)} case\n"
              f"  giao nhau: 0\n"
              "Taxonomy trong repo được sinh trên corpus `dry-run-*`. Muốn đo trên corpus\n"
              "khác thì phải có taxonomy của chính corpus đó — chạy lại workflow\n"
              "scripts/workflows/dem-nguyen-nhan-rk4.js trên nó.", file=sys.stderr)
        sys.exit(2)
    if len(mau) < len(nhom_cua) * 0.5:
        print(f"⚠ Chỉ {len(mau)}/{len(nhom_cua)} case có nhãn khớp corpus này — "
              f"con số đo được sẽ dựa trên chưa tới nửa tập nhãn.\n")

    with psycopg.connect(DSN, autocommit=True) as conn:
        cur = conn.cursor()
        cur.execute("SELECT extname FROM pg_extension WHERE extname='unaccent'")
        if not cur.fetchone():
            try:
                cur.execute("CREATE EXTENSION IF NOT EXISTS unaccent")
                print("  đã cài extension unaccent")
            except Exception as e:
                print(f"  ⚠ không cài được unaccent ({e}); chỉ đo cấu hình 'simple'")

        cur.execute("DROP TABLE IF EXISTS thu_kho")
        cur.execute("""CREATE TABLE thu_kho (
            k text primary key, nhom int, subject text, than text,
            tsv tsvector, tsv_ua tsvector)""")

        co_ua = True
        for k in mau:
            than = desc.get(k, "")
            try:
                cur.execute(
                    """INSERT INTO thu_kho (k, nhom, subject, than, tsv, tsv_ua) VALUES (%s,%s,%s,%s,
                       setweight(to_tsvector('simple', %s),'A') || setweight(to_tsvector('simple', %s),'B'),
                       setweight(to_tsvector('simple', unaccent(%s)),'A') || setweight(to_tsvector('simple', unaccent(%s)),'B'))""",
                    (k, nhom_cua[k], subj[k], than, subj[k], than, subj[k], than))
            except Exception:
                co_ua = False
                cur.execute(
                    """INSERT INTO thu_kho (k, nhom, subject, than, tsv) VALUES (%s,%s,%s,%s,
                       setweight(to_tsvector('simple', %s),'A') || setweight(to_tsvector('simple', %s),'B'))""",
                    (k, nhom_cua[k], subj[k], than, subj[k], than))

        # ⚠ TRUY VẤN PHẢI GHÉP BẰNG `|`, KHÔNG PHẢI websearch_to_tsquery.
        # Đo được 2026-09-05: `websearch_to_tsquery('simple', <đoạn văn 1000 ký tự>)` sinh
        # ra `t1 & t2 & ... & tN` — AND của hàng chục token — và **0/88 truy vấn trả về
        # dòng nào**. Đây là mặt thứ hai của `AR-h` #2: dấu `-` làm truy vấn ngắn trả 0
        # dòng, còn độ DÀI làm truy vấn dài trả 0 dòng. Cùng một nguyên nhân (mặc định
        # AND), cùng một kiểu thất bại im lặng — không lỗi, chỉ là kho rỗng.
        # Mọi search engine thật ghép OR rồi xếp hạng; đó mới là thứ cần đo.
        def tsquery_or(q: str, ua: bool) -> str:
            cur.execute(
                "SELECT string_agg(quote_literal(lexeme), ' | ') FROM unnest(to_tsvector('simple', %s))"
                if not ua else
                "SELECT string_agg(quote_literal(lexeme), ' | ') FROM unnest(to_tsvector('simple', unaccent(%s)))",
                (q,))
            return (cur.fetchone() or [None])[0] or ""

        ket = {}
        for cot, ten_cot, dung_ua in (("tsv", "simple", False),
                                      ("tsv_ua", "simple + unaccent", True)):
            if dung_ua and not co_ua:
                continue
            trung1 = trung3 = 0
            truot = []
            for k in mau:
                q = tin_nhan_dau(desc.get(k, "") or subj[k])
                q = re.sub(r"[^\w\s]", " ", q)[:1000]  # bỏ dấu câu: `-` đảo ngược truy vấn (AR-h #2)
                tq = tsquery_or(q, dung_ua)
                if not tq:
                    truot.append((k, ten_nhom[nhom_cua[k]], []))
                    continue
                cur.execute(
                    f"""SELECT nhom FROM thu_kho WHERE k <> %s AND {cot} @@ %s::tsquery
                        ORDER BY ts_rank_cd({cot}, %s::tsquery) DESC LIMIT 3""",
                    (k, tq, tq))
                top = [r[0] for r in cur.fetchall()]
                dung = nhom_cua[k]
                if top[:1] == [dung]:
                    trung1 += 1
                if dung in top:
                    trung3 += 1
                else:
                    truot.append((k, ten_nhom[dung], [ten_nhom[t] for t in top]))
            ket[ten_cot] = (trung1, trung3, truot)

        print("=" * 70)
        print("KẾT QUẢ — ngưỡng đặt TRƯỚC: <60% embedding đáng thử · >70% đóng câu hỏi")
        print("=" * 70)
        n = len(mau)
        for ten_cot, (t1, t3, truot) in ket.items():
            print(f"\n  {ten_cot}")
            print(f"      top-1 đúng nhóm: {t1}/{n} = {100*t1//n}%")
            print(f"      top-3 đúng nhóm: {t3}/{n} = {100*t3//n}%   <-- con số quyết định")
        best = max(ket.items(), key=lambda x: x[1][1])
        t3 = best[1][1]
        ty = 100 * t3 // n
        print(f"\n  Tốt nhất: {best[0]} — top-3 = {ty}%")
        print("  => " + ("FTS KHÔNG đủ, embedding đáng thử." if ty < 60 else
                         "VÙNG XÁM, phải nhìn case thất bại." if ty <= 70 else
                         "ĐÓNG câu hỏi: không dựng vector DB."))

        print("\n" + "=" * 70)
        print("MƯỜI CASE THẤT BẠI — đọc chúng quan trọng hơn đọc con số")
        print("=" * 70)
        for k, dung, top in best[1][2][:10]:
            print(f"\n  {k}  {subj.get(k,'')[:56]}")
            print(f"      đúng : {dung[:60]}")
            print(f"      trả  : {' | '.join(t[:34] for t in top) or '(không trả về gì)'}")

        cur.execute("DROP TABLE IF EXISTS thu_kho")


if __name__ == "__main__":
    main()
