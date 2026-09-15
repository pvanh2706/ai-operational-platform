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


# ⚠ Số đếm phải theo SỐ CÂY THẬT. Bản đầu nhận `so_cay` rồi KHÔNG DÙNG và viết cứng
# "hai" ở ba chỗ, nên sinh gói cho MỘT nhóm thì ngay dòng đầu đã nói sai với chính người
# được nhờ đọc. Đây là tài liệu GỬI RA NGOÀI dự án — sai số ở câu mở đầu làm người ta
# ngờ cả phần còn lại, mà thời gian của họ là thứ khan hiếm nhất ở đây.
SO_CHU = {1: "một", 2: "hai", 3: "ba", 4: "bốn", 5: "năm"}


def so_chu(n: int) -> str:
    return SO_CHU.get(n, str(n))


def doan_do_ghi_chep(cays: list) -> list:
    """Câu mô tả mức ghi chép của ĐÚNG những cây trong gói này.

    ⚠ Câu này từng là hằng số, và hằng số đó SAI kể từ cây thứ ba. Gói nhóm 1+2 mở đầu
    bằng "ticket hầu như không ghi cách anh/chị biết" — đúng với 4/10 và 8/10 của hai
    nhóm đầu theo cách đếm lúc đó, nhưng nhóm 3 ghi rõ 6/8. Gửi cho người làm nghề một
    câu sai về chính công việc của họ là cách nhanh nhất để họ thôi đọc phần còn lại.
    Nên câu này được TÍNH. ⚠ Ngưỡng là HAI PHẦN BA, không phải một nửa, và bản đầu đặt ở
    một nửa đã sai ngay lần chạy đầu: bản máy sinh của nhóm 1 khai 5/10 — đúng ranh giới —
    nên gói ĐANG CHỜ DUYỆT đổi lời sang "phần lớn chữ trong đó là chữ của chính anh/chị".
    Ở mức một nửa thì câu đó là nói quá, và nói quá với đúng người biết rõ sự thật thì mất
    nhiều hơn được. Ngưỡng cao còn giữ cho hai gói đã gửi (12, 16) sinh lại ra y nguyên.
    """
    ghi_ro = sum((c.get("phanBoBuocKiemDuocGhiLai", {}).get("ghi-ro-buoc-kiem") or {}).get("so", 0)
                 for c in cays)
    tong = sum(c["soCaseDungSau"] for c in cays)
    if not tong or ghi_ro * 3 < tong * 2:
        return [
            "**Chỗ chúng tôi chắc chắn còn sai: ticket ghi được *kết luận*, nhưng hầu như không ghi",
            "*cách anh/chị biết*.** Có ticket kết thúc bằng đúng tám chữ " + DOAN + " *\"Sai CCCD\"*. Những",
            "chỗ như vậy chúng tôi **tự đoán**, và đã đánh dấu " + TU_DOAN + " ngay tại chỗ đoán.",
            "Chỉ anh/chị nói được chỗ nào đoán sai.",
        ]
    return [
        "**Nhóm này khác: %d/%d ticket CÓ ghi lại cách chẩn đoán**, nên bản dưới đây dựng được"
        % (ghi_ro, tong),
        "chi tiết hơn " + DOAN + " phần lớn chữ trong đó là chữ của chính anh/chị, chúng tôi chỉ xếp lại.",
        "Những chỗ vẫn phải đoán thì được đánh dấu " + TU_DOAN + " ngay tại chỗ đoán.",
        "**Điều đó làm câu hỏi đổi:** không phải \"chúng tôi đoán sai chỗ nào\" mà *\"xếp như vậy có",
        "đúng thứ tự anh/chị làm không, và thiếu bước nào\"*.",
    ]


def dau_trang(so_cay: int, so_case: int, so_tai_lieu: str = "12", cays: list = None) -> list:
    chu = so_chu(so_cay)
    return [
        "# " + so_tai_lieu + " " + DOAN + " GÓI DUYỆT: " + chu + " bản nháp quy trình xử lý, cần người làm support sửa",
        "",
        "> **File này được SINH TỰ ĐỘNG** từ `scripts/jira-export/goi_duyet.py` và từ " + chu + " file",
        "> cây quyết định. Nếu bạn bảo trì repo thì đừng sửa trực tiếp vào đây " + DOAN + " sửa file",
        "> JSON rồi sinh lại. Người DUYỆT thì cứ ghi thoải mái vào bản của mình.",
        "",
        "---",
        "",
        "## Gửi anh/chị làm support",
        "",
        "Chúng tôi đang thử dựng lại **quy trình xử lý** cho " + chu + " loại sự cố hay gặp về hoá đơn",
        "điện tử, bằng cách đọc lại " + str(so_case) + " ticket đã đóng trên Jira. Kết quả ở dưới.",
        "",
    ] + doan_do_ghi_chep(cays or []) + [
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


# Mỗi loại bảng tự khai KHOÁ của nó và tiêu đề cột cho người duyệt. Cứng hoá một loại
# bảng là sai: ba nhóm dựng tới nay ra ba topology, và nhóm thứ ba tra bằng cặp (cột tiền,
# nơi khai sai) — một khoá mà không màn hình nào đọc ra được, nó phải được dựng bằng cách
# so cùng một hoá đơn ở ba nơi. Xem `hinhDang` của cây nhóm 3.
LOAI_BANG = {
    "bangTraMaLoi": {
        "tieuDe": "Bảng tra mã lỗi",
        "moDau": ["> \U0001f6d1 **Nếu cả file này chỉ sửa được một thứ, xin sửa bảng này.** Chính khách",
                  "> đã hỏi “có bảng liệt kê mã lỗi không” và được trả lời là chưa có.", ""],
        "cot": [("Nhà cung cấp", "ncc", False), ("Mã lỗi", "maLoi", True),
                ("Trường bị từ chối", "truongBiTuChoi", False)],
        "khoa": ("ncc", "maLoi"),
        "hoiThem": "**Còn mã lỗi nào hay gặp mà bảng chưa có? Mã đó trỏ tới trường nào?**",
    },
    "bangDoiChieuCot": {
        "tieuDe": "Bảng đối chiếu: cột nào lệch thì khai sai ở đâu",
        "moDau": ["> \U0001f6d1 **Đây là phần chúng tôi cần anh/chị sửa nhất.** Cùng một câu “hoá đơn",
                  "> sai tiền” ra tám nguyên nhân khác nhau ở bảng dưới, và chỗ khai sai thì nằm",
                  "> rải ở ba nơi: PMS, trang quản trị ezInvoice, và bên nhà cung cấp.", ""],
        "cot": [("Cột bị lệch", "cot", False), ("Khai sai ở đâu", "noiKhaiSai", False),
                ("Lưu ý", "ghiChu", False)],
        "khoa": ("cot", "noiKhaiSai"),
        "hoiThem": "**Còn kiểu lệch tiền nào hay gặp mà bảng chưa có? Nó khai sai ở đâu?**",
    },
}


def mot_cay(cay: dict, chi_so: int) -> list:
    cac_bang = [(ten, LOAI_BANG[ten], (cay.get(ten) or {}).get("dong") or [])
                for ten in LOAI_BANG if cay.get(ten)]
    bang = [(LOAI_BANG[ten], d) for ten, _, dong in cac_bang for d in dong]
    muc = iter(range(1, 20))

    def so():
        return "%d.%d" % (chi_so, next(muc))

    r = ["---", "", "# Phần %d. %s" % (chi_so, cay["nhom"]), ""]
    hd = cay.get("hinhDang") or {}
    # ⚠ Không suy dạng quy trình theo VỊ TRÍ trong gói. `hinhDang` của một cây ghi cả ba
    # nhóm để đối chiếu, nên `nhom%d` theo thứ tự phần chỉ đúng khi gói gồm đủ các nhóm
    # xếp đúng thứ tự — sinh gói riêng cho một nhóm là ra dạng của nhóm khác, im lặng.
    # Cây khai `cuaNhomNay` thì đọc theo khai báo.
    dang = hd.get(hd.get("cuaNhomNay") or ("nhom%d" % chi_so)) if hd else None
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

    for ten_bang, luat, dong in cac_bang:
        r += ["## %s %s" % (so(), luat["tieuDe"]), ""]
        r += list(luat["moDau"])
        tieu_de = [nhan for nhan, _, _ in luat["cot"]]
        r += ["| %s | Sửa ở đâu | Nguồn | Ticket | Sửa lại |" % " | ".join(tieu_de),
              "|" + "---|" * (len(tieu_de) + 4)]
        for d in dong:
            o = []
            for _, truong, ma_nguon in luat["cot"]:
                gt = chi_cho_nguoi_duyet(d.get(truong)) or DOAN
                o.append("`%s`" % gt if ma_nguon else gt)
            r.append("| %s | `%s` | %s | %s | |"
                     % (" | ".join(o), d["diToi"], nhan_chung_cu(d.get("chungCu")),
                        ", ".join(d.get("case") or []) or DOAN))
        r.append("")
        r += [luat["hoiThem"], ""]
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
    doan += [("bảng", " / ".join(str(d.get(k)) for k in luat["khoa"]))
             for luat, d in bang if d.get("chungCu") != "evidence-noi-ro"]
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
    ap.add_argument("--so", default="12",
                    help="số hiệu tài liệu ở tiêu đề (mặc định 12). Gói sinh từ bản MÁY SINH "
                         "là một tài liệu KHÁC với gói sinh từ bản người dựng, đừng để chúng mang cùng số.")
    args = ap.parse_args()

    for p in args.cay:
        if not os.path.exists(p):
            print("Không thấy %s" % p, file=sys.stderr)
            return 1

    cays = [doc(p) for p in args.cay]
    so_case = sum(c["soCaseDungSau"] for c in cays)
    dong = dau_trang(len(cays), so_case, args.so, cays)
    for i, c in enumerate(cays, 1):
        dong += mot_cay(c, i)
    dong += cuoi_trang()

    with io.open(args.ra, "w", encoding="utf-8") as f:
        f.write("\n".join(dong))

    o_trong = sum(l.count("| |") for l in dong)
    tu_doan = sum(1 for c in cays for b in c["buocKiem"] for n in b["nhanh"]
                  if n.get("chungCu") != "evidence-noi-ro")
    tu_doan += sum(1 for c in cays for ten in LOAI_BANG
                   for d in (c.get(ten) or {}).get("dong") or []
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
