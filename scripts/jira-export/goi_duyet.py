#!/usr/bin/env python3
"""Sinh GÓI DUYỆT cho người làm support từ các cây quyết định đã dựng.

Vì sao phải sinh bằng script chứ không viết tay: gói duyệt và cây quyết định nói CÙNG
một nội dung ở hai định dạng. Viết tay hai bản thì chúng lệch nhau, và lệch im lặng —
người duyệt sửa vào bản Markdown, còn `--kiem-cay` thì kiểm bản JSON. Sinh ra từ JSON
thì gói không bao giờ lệch khỏi thứ nó mô tả, và sau khi có kết quả duyệt thì sinh lại.

Hai việc script này làm mà một bản viết tay sẽ làm sai:

1. **Đánh dấu tường minh nhánh nào chúng tôi TỰ ĐOÁN.** Cây nhóm 1 có 6/13 nhánh không
   có ticket nào chống lưng. Không nói ra thì người duyệt đọc cả cây với cùng một mức
   tin cậy, và phần họ sửa sẽ lẫn giữa "chúng tôi ghi sai" với "chúng tôi đoán và đoán
   sai" — hai loại khác nhau, và `M2` cần phân biệt được.

2. **Bỏ tham chiếu NỘI BỘ khỏi văn bản gửi ra.** Các file JSON được viết cho người
   trong dự án nên chúng dẫn `docs/11 §4`, `AR-l`, `S8`, `G6/AP3`… Người làm support
   không biết những mã đó, và mọi câu họ không hiểu đều làm loãng câu trả lời mình cần.
   Xem `chi_cho_nguoi_duyet`.

    python scripts/jira-export/goi_duyet.py \
      docs/ket-qua-phan-tich/cay-quyet-dinh-phan-quyen-ky-hieu.json \
      docs/ket-qua-phan-tich/cay-quyet-dinh-ncc-tu-choi-payload.json \
      --ra docs/12_GOI_DUYET_SOP.md

⚠ Gói này GIỮ mã ticket (`ES-…`) để người duyệt tự tra Jira. Nó dành cho người ĐÃ có
  quyền đọc những ticket đó — tức support nội bộ. Muốn gửi ra ngoài ezCloud thì phải bỏ
  mã ticket và mọi thứ tra ngược được từ đó; script này KHÔNG làm việc che đó, và cố ý
  không làm: che dữ liệu khách là một quyết định của chủ dữ liệu, không phải một cờ CLI.
"""

from __future__ import annotations

import argparse
import io
import json
import os
import re
import sys

for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

DOAN = "—"
TU_DOAN = "⚠ **CHÚNG TÔI TỰ ĐOÁN**"
CO_NGUON = "✔ có ca thật"

# Mã hiệu chỉ có nghĩa với người trong dự án: docs/07, §5, AR-l, IM-24, R-K4, S8, M2,
# G12, AP3, Q-D. Câu nào chứa chúng thì không gửi cho người duyệt.
NOI_BO = re.compile(
    r"docs?/\d"
    r"|§\s*\d"
    r"|\bAR-[a-z]\b"
    r"|\bIM-\d"
    r"|\bR-K\d"
    r"|\bPR\d\b"
    r"|\bS\d\b"
    r"|\bM\d[a-z]?\b"
    r"|\bG\d+\b"
    r"|\bAP\d\b"
    r"|\bQ-[A-Z]\b"
)

# ⚠ `corpus` và `taxonomy` CỐ Ý không nằm trong danh sách trên, và đây là một chỗ đã sửa
# sau khi đọc phần in ra: để chúng ở đó thì hai câu HỮU ÍCH cho người duyệt bị bỏ oan
# ("xác nhận hầu như luôn do nhân viên nói…", "ES-343348 tự hỏi đúng câu này…") chỉ vì
# vướng một từ. Từ nghe lạ thì ĐỔI TỪ, đừng bỏ câu. Chính phần in ra số câu bị bỏ đã
# làm chỗ này lộ ra — nếu bộ lọc im lặng thì gói gửi đi đã thiếu hai câu mà không ai biết.
THAY_TU = [
    ("corpus", "lô ticket đã đọc"),
    ("Corpus", "Lô ticket đã đọc"),
    ("taxonomy", "bảng phân nhóm"),
]

_da_bo: list = []


def chi_cho_nguoi_duyet(van_ban):
    """Bỏ những CÂU mang tham chiếu nội bộ ra khỏi văn bản gửi người duyệt.

    Cắt theo CÂU, không theo từ — bỏ nửa câu thì còn tệ hơn giữ nguyên. Không im lặng:
    số câu bị bỏ được in ra cuối mỗi lần chạy, để còn đọc lại xem có bỏ oan mất thông
    tin nào khó thay hay không.
    """
    if not van_ban:
        return van_ban
    van_ban = str(van_ban)
    for a, b in THAY_TU:
        van_ban = van_ban.replace(a, b)
    cau = re.split(r"(?<=[.!?])\s+", van_ban)
    giu = [c for c in cau if not NOI_BO.search(c)]
    _da_bo.extend(c for c in cau if NOI_BO.search(c))
    return " ".join(giu).strip()


def doc(p):
    with io.open(p, encoding="utf-8") as f:
        return json.load(f)


def nhan_chung_cu(gt):
    return CO_NGUON if gt == "evidence-noi-ro" else TU_DOAN


def o_ghi(so_dong=2):
    """Khối trống để người duyệt gõ vào."""
    return ["```"] + [""] * so_dong + ["```", ""]


def dau_trang(so_cay: int, so_case: int) -> list:
    return [
        "# 12 " + DOAN + " GÓI DUYỆT: hai bản nháp quy trình xử lý, cần người làm support sửa",
        "",
        "> **File này được SINH TỰ ĐỘNG** từ `scripts/jira-export/goi_duyet.py` và từ hai file",
        "> cây quyết định. Nếu bạn bảo trì repo thì đừng sửa trực tiếp vào đây " + DOAN + " sửa file",
        "> JSON rồi sinh lại. Người DUYỆT thì cứ ghi thoải mái vào bản của mình.",
        "",
        "---",
        "",
        "## Gửi anh/chị làm support",
        "",
        "Chúng tôi đang thử dựng lại **quy trình xử lý** cho hai loại sự cố hay gặp về hoá đơn",
        "điện tử, bằng cách đọc lại " + str(so_case) + " ticket đã đóng trên Jira. Kết quả ở dưới.",
        "",
        "**Chỗ chúng tôi chắc chắn còn sai: ticket ghi được *kết luận*, nhưng hầu như không ghi",
        "*cách anh/chị biết*.** Có ticket kết thúc bằng đúng tám chữ " + DOAN + " *\"Sai CCCD\"*. Những",
        "chỗ như vậy chúng tôi **tự đoán**, và đã đánh dấu " + TU_DOAN + " ngay tại chỗ đoán.",
        "Chỉ anh/chị nói được chỗ nào đoán sai.",
        "",
        "**Mất bao lâu:** khoảng 20" + DOAN + "30 phút. **Không cần đọc lại ticket** " + DOAN + " mã ticket để",
        "ở đó cho anh/chị tra khi muốn, không phải để anh/chị đi kiểm tra chúng tôi.",
        "",
        "**Trả lời thế nào:** ghi thẳng vào cột **“Sửa lại”**, hoặc trả lời theo số câu ở phần",
        "cuối. Đúng thì để trống " + DOAN + " **ô trống nghĩa là “đúng”**, không phải “chưa xem”.",
        "",
        "\U0001f6d1 **Chỗ hữu ích nhất lại là chỗ không có ô trống nào sẵn:** *bước kiểm nào chúng",
        "tôi KHÔNG CÓ trong danh sách* " + DOAN + " thứ anh/chị vẫn làm mà không ai ghi vào ticket.",
        "Xin ghi vào mục **“Còn thiếu”** ở cuối mỗi phần. Kể cả *\"gọi điện hỏi khách\"* hay",
        "*\"remote vào xem\"* " + DOAN + " đó vẫn là bước kiểm, và đúng là loại không bao giờ được ghi lại.",
        "",
    ]


def mot_cay(cay: dict, chi_so: int) -> list:
    bang = (cay.get("bangTraMaLoi") or {}).get("dong") or []
    muc = iter(range(1, 20))

    def so():
        return "%d.%d" % (chi_so, next(muc))

    r = ["---", "", "# Phần %d. %s" % (chi_so, cay["nhom"]), ""]
    hd = cay.get("hinhDang") or {}
    dang = hd.get("nhom%d" % chi_so) if hd else None
    if dang:
        r += ["*Dạng quy trình chúng tôi thấy: %s*" % chi_cho_nguoi_duyet(dang), ""]
    r += ["**Số ticket đứng sau phần này:** %d" % cay["soCaseDungSau"], ""]

    r += ["## %s Khách báo vào bằng những câu nào" % so(), ""]
    r += ["| Khách nói / màn hình báo | Ticket | Sửa lại |", "|---|---|---|"]
    for t in cay["trieuChungVao"]:
        r.append("| %s | %s | |" % (chi_cho_nguoi_duyet(t["moTa"]), ", ".join(t["case"])))
    r.append("")
    cua = chi_cho_nguoi_duyet(cay.get("_vaoCungMotCua"))
    if cua:
        r += ["> ⚠ %s **Đúng không, hay khách vẫn nói khác nhau ở chỗ nào?**" % cua, ""]

    r += ["## %s Các bước kiểm, theo thứ tự chúng tôi đoán" % so(), ""]
    r += ["> \U0001f6d1 **Thứ tự này chúng tôi TỰ ĐẶT.** Không ticket nào ghi thứ tự kiểm, nên",
          "> đây là chỗ sai nhiều nhất. Nếu thực tế anh/chị kiểm theo thứ tự khác, xin ghi ra.", ""]
    for b in cay["buocKiem"]:
        r += ["### %s. %s" % (b["ma"], chi_cho_nguoi_duyet(b["cauHoi"])), ""]
        r += ["- **Xem ở:** %s" % chi_cho_nguoi_duyet(b["noiXem"])]
        if b.get("quyenCan"):
            r.append("- **Cần quyền:** %s" % chi_cho_nguoi_duyet(b["quyenCan"]))
        if b.get("viSaoTruoc"):
            r.append("- **Vì sao chúng tôi đặt ở bước này:** %s"
                     % chi_cho_nguoi_duyet(b["viSaoTruoc"]))
        if b.get("nguyenVan"):
            r.append("- **Lấy từ ticket:** %s" % chi_cho_nguoi_duyet(b["nguyenVan"]))
        if b.get("khoangTrong"):
            r.append("- ⚠ **Chỗ chúng tôi không biết:** %s"
                     % chi_cho_nguoi_duyet(b["khoangTrong"]))
        r.append("")
        r += ["| Thấy gì | Thì làm gì tiếp | Nguồn | Ticket | Sửa lại |",
              "|---|---|---|---|---|"]
        for n in b["nhanh"]:
            r.append("| %s | `%s` | %s | %s | |"
                     % (chi_cho_nguoi_duyet(n["quanSat"]), n["diToi"],
                        nhan_chung_cu(n.get("chungCu")), ", ".join(n["case"]) or DOAN))
        r.append("")
        bay = chi_cho_nguoi_duyet(b.get("bayDaVapThat"))
        if bay:
            r += ["> **Chỗ chúng tôi thấy dễ vấp " + DOAN + " có đúng vậy không?** %s" % bay, ""]

    if bang:
        r += ["## %s Bảng tra mã lỗi" % so(), ""]
        r += ["> \U0001f6d1 **Nếu cả file này chỉ sửa được một thứ, xin sửa bảng này.** Chính khách",
              "> đã hỏi “có bảng liệt kê mã lỗi không” và được trả lời là chưa có.", ""]
        r += ["| Nhà cung cấp | Mã lỗi | Trường bị từ chối | Sửa ở đâu | Nguồn | Ticket | Sửa lại |",
              "|---|---|---|---|---|---|---|"]
        for d in bang:
            r.append("| %s | `%s` | %s | `%s` | %s | %s | |"
                     % (d["ncc"], d["maLoi"], chi_cho_nguoi_duyet(d["truongBiTuChoi"]),
                        d["diToi"], nhan_chung_cu(d.get("chungCu")),
                        ", ".join(d.get("case") or []) or DOAN))
        r.append("")
        r += ["**Còn mã lỗi nào hay gặp mà bảng chưa có? Mã đó trỏ tới trường nào?**", ""]
        r += o_ghi(3)

    r += ["## %s Cách sửa" % so(), ""]
    r += ["| Mã | Việc | Ticket | Sửa lại |", "|---|---|---|---|"]
    for sb in cay["buocSua"]:
        r.append("| `%s` | %s | %s | |"
                 % (sb["ma"], chi_cho_nguoi_duyet(sb["viec"]),
                    ", ".join(sb.get("case") or []) or DOAN))
    r.append("")

    xn = cay.get("buocXacNhan") or {}
    if xn:
        r += ["## %s Khi nào coi là xong" % so(), ""]
        r += ["**%s**" % chi_cho_nguoi_duyet(xn["viec"]), ""]
        kt = chi_cho_nguoi_duyet(xn.get("khoangTrong"))
        if kt:
            r += ["> ⚠ %s" % kt, ""]
        r += ["Điều kiện này đủ chưa? Nếu chưa thì còn thiếu gì:", ""]
        r += o_ghi()

    doan = [(b["ma"], chi_cho_nguoi_duyet(n["quanSat"]))
            for b in cay["buocKiem"] for n in b["nhanh"]
            if n.get("chungCu") != "evidence-noi-ro"]
    doan += [("bảng", "%s / %s" % (d["ncc"], d["maLoi"])) for d in bang
             if d.get("chungCu") != "evidence-noi-ro"]
    if doan:
        r += ["## %s %d chỗ chúng tôi TỰ ĐOÁN " % (so(), len(doan)) + DOAN
              + " xin trả lời từng chỗ", ""]
        r += ["Mỗi chỗ chỉ cần một trong ba: **đúng** / **sai, phải là …** / "
              "**không bao giờ xảy ra**.", ""]
        r += ["| # | Ở bước | Chúng tôi đoán rằng | Đúng / Sai / Không có |",
              "|---|---|---|---|"]
        for i, (ma, qs) in enumerate(doan, 1):
            r.append("| %d | `%s` | %s | |" % (i, ma, qs))
        r.append("")

    r += ["## %s Còn thiếu " % so() + DOAN
          + " bước kiểm anh/chị vẫn làm mà không có ở trên", ""]
    r += o_ghi(4)
    return r


def cuoi_trang() -> list:
    cau = [
        "Thứ tự các bước kiểm ở trên có đúng thứ tự anh/chị THỰC SỰ làm không? "
        "Nếu không, thứ tự đúng là gì?",
        "Có bước nào ở trên mà thực tế **không ai làm** vì không đáng làm " + DOAN
        + " tức chúng tôi đã thêm việc vô ích?",
        "Bước kiểm nào hay phải làm mà **không tự làm được** " + DOAN
        + " thiếu quyền, thiếu màn hình, hay phải nhờ người khác?",
        "Nếu đưa hai quy trình này cho một bạn **mới vào làm**, chỗ nào bạn đó sẽ hiểu sai "
        "hoặc làm sai thứ tự?",
        "Có loại sự cố hoá đơn nào **hay gặp hơn** hai loại này mà chúng tôi bỏ qua không?",
    ]
    r = ["---", "", "# Phần cuối. Năm câu xin trả lời bằng chữ", ""]
    for i, c in enumerate(cau, 1):
        r += ["**Câu %d.** %s" % (i, c), ""] + o_ghi()
    r += ["---", "",
          "Cảm ơn anh/chị. Phần anh/chị sửa được dùng để đo xem bản nháp do máy dựng còn lệch",
          "bao nhiêu so với người làm thật " + DOAN + " nên **chỗ nào anh/chị sửa nhiều nhất là chỗ",
          "có giá trị nhất**, không phải chỗ làm chúng tôi mất mặt.", ""]
    return r


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("cay", nargs="+", help="một hoặc nhiều file cây quyết định (.json)")
    ap.add_argument("--ra", required=True, help="file Markdown đầu ra")
    args = ap.parse_args()

    for p in args.cay:
        if not os.path.exists(p):
            print("Không thấy %s" % p, file=sys.stderr)
            return 1

    cays = [doc(p) for p in args.cay]
    so_case = sum(c["soCaseDungSau"] for c in cays)
    dong = dau_trang(len(cays), so_case)
    for i, c in enumerate(cays, 1):
        dong += mot_cay(c, i)
    dong += cuoi_trang()

    with io.open(args.ra, "w", encoding="utf-8") as f:
        f.write("\n".join(dong))

    o_trong = sum(l.count("| |") for l in dong)
    tu_doan = sum(1 for c in cays for b in c["buocKiem"] for n in b["nhanh"]
                  if n.get("chungCu") != "evidence-noi-ro")
    tu_doan += sum(1 for c in cays
                   for d in (c.get("bangTraMaLoi") or {}).get("dong") or []
                   if d.get("chungCu") != "evidence-noi-ro")
    print("Đã sinh %s" % args.ra)
    print("  %d cây · %d ticket · %d dòng" % (len(cays), so_case, len(dong)))
    print("  %d ô trống để người duyệt ghi · %d chỗ đánh dấu TỰ ĐOÁN" % (o_trong, tu_doan))
    if _da_bo:
        print("  ⚠ đã bỏ %d câu mang tham chiếu nội bộ khỏi văn bản gửi ra "
              "(xem chi_cho_nguoi_duyet)" % len(_da_bo))
        for c in _da_bo:
            print("      · " + (c[:96] + ("…" if len(c) > 96 else "")))
    return 0


if __name__ == "__main__":
    sys.exit(main())
