#!/usr/bin/env python3
"""Giao diện web chạy LOCAL cho vòng quét Jira → AI phân tích → nạp lại.

    run-ui.bat            mở http://127.0.0.1:8791 trên trình duyệt

VÌ SAO LÀ MỘT SERVER RIÊNG CHỨ KHÔNG PHẢI ENDPOINT CỦA APP:
`LenhDinhTuyen` đã chốt rằng xuất/nạp vòng không-credit là LỆNH CLI chứ không phải
endpoint, vì file xuất ra chứa dữ liệu khách thật và một endpoint trả nó qua HTTP là
thêm một đường ra phải canh. Quyết định đó vẫn đúng, nên công cụ này KHÔNG mở thêm
đường nào trên app: nó là một tiến trình riêng, **chỉ nghe trên 127.0.0.1**, tắt là hết.

🛑 TRANG NÀY HIỂN THỊ DỮ LIỆU KHÁCH THẬT. Đó là đánh đổi có chủ đích để demo được —
  và vì thế nó không bao giờ được bind ra 0.0.0.0. Xem DIA_CHI.

⚠ PAT chỉ nằm trong RAM của tiến trình này. Không ghi ra đĩa, không trả ngược về
  trình duyệt, không vào log. Tắt server là mất — đúng như mong muốn.

⚠ Thư viện CHUẨN, không thêm phụ thuộc. Cùng lý do bảy script trong scripts/jira-export:
  đây là đường nạp dữ liệu, phải chạy được trên một máy vừa cài Python.
"""
from __future__ import annotations

import base64
import http.server
import io
import json
import os
import subprocess
import sys
import threading
import traceback
import urllib.error
import urllib.parse
import urllib.request
import webbrowser

HERE = os.path.dirname(os.path.abspath(__file__))
GOC = os.path.abspath(os.path.join(HERE, ".."))
JIRA_DIR = os.path.join(GOC, "jira-export")
sys.path.insert(0, JIRA_DIR)

import export_jira_to_channel1 as X  # noqa: E402
import gia_lap  # noqa: E402 — trang giả lập hai luồng (O4-2), cùng server, đường /gia-lap
import console_web  # noqa: E402 — console Vue (P3-1): /console tĩnh + /app/<cổng>/... chuyển tiếp sang host

# 🛑 CHỈ loopback. Đổi thành "" hay "0.0.0.0" là phơi dữ liệu khách ra cả LAN.
DIA_CHI = "127.0.0.1"
CONG = 8791

# Trạng thái phiên — RAM, không bền. `auth` là chuỗi bí mật: không bao giờ đi ra ngoài.
# `N1-2` (docs/18, 2026-10-10): chủ dự án chọn TẮT TẠM mọi cổng che bằng MỘT cờ. Phía server
# là `CongChe:TatTamVaChapNhanRuiRo`; phía giao diện này là biến môi trường dưới đây. Bật thì cổng
# credential của check_corpus vẫn CHẠY và vẫn BÁO — chỉ thôi CHẶN nút gửi vào kho.
# ⚠ Hai cờ là hai chỗ đặt riêng, cố ý: giao diện không đọc được cấu hình của server. Bật cờ ở đây
#   mà server vẫn che thì mẩu vào kho VẪN được che bên thứ ba; ngược lại thì không.
CONG_CHE_TAT_TAM = os.environ.get("KP_CONG_CHE_TAT_TAM") == "1"

PHIEN: dict = {
    "base": None, "auth": None, "nhan": None,
    "cases": [], "evidence": [], "jql": None,
    "corpus_chan": None,
    # Kết quả AI đã qua `kiem_ket_qua`, giữ nguyên để bước 6 dựng lại bảng nhóm.
    "phan_tich": None,
}

# Thư mục project của app — `--nap-dinh-tuyen` phải chạy với cwd ở đây, nếu không
# content root lệch và app KHÔNG đọc được appsettings.Development.json (đã vấp: nó báo
# "THIẾU chuỗi kết nối" trong khi file nằm ngay đó).
DU_AN_APP = os.path.join(GOC, "..", "src", "KnowledgePlatform.Api")
EXE_APP = os.path.join(DU_AN_APP, "bin", "Debug", "net10.0", "KnowledgePlatform.Api.exe")

# Nhật ký tiến độ để trình duyệt hỏi lại — quét mất vài phút, một trang đứng im
# không phân biệt được với một trang đã chết.
TIEN_DO: list[str] = []
DANG_CHAY = {"viec": None, "xong": False, "loi": None}


# ----------------------------------------------------------------- tiện ích


class GhiTienDo(io.TextIOBase):
    """Bắt `print` của module export và đẩy vào TIEN_DO, vẫn in ra console."""

    def __init__(self, goc):
        self._goc = goc
        self._dem = ""

    def write(self, s: str) -> int:
        self._goc.write(s)
        self._dem += s
        while "\n" in self._dem:
            dong, self._dem = self._dem.split("\n", 1)
            if dong.strip():
                TIEN_DO.append(dong.rstrip())
        return len(s)

    def flush(self):
        self._goc.flush()


def goi_jira(base: str, auth: str, duong: str, timeout: int = 30) -> dict:
    req = urllib.request.Request(base.rstrip("/") + duong,
                                 headers={"Authorization": auth, "Accept": "application/json"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return json.loads(r.read().decode("utf-8"))


def tao_auth(pat: str, user: str, mat_khau: str) -> str:
    if pat.strip():
        return "Bearer " + pat.strip()
    if user.strip() and mat_khau:
        return "Basic " + base64.b64encode(f"{user.strip()}:{mat_khau}".encode()).decode()
    raise ValueError("Cần Personal Access Token, hoặc tên đăng nhập + mật khẩu.")


def duong_corpus(nhan: str, duoi: str) -> str:
    """Mọi file corpus đều nằm ở scripts/jira-export với tiền tố dry-run-.

    ⚠ KHÔNG phải quy ước cho đẹp: .gitignore chặn theo mẫu `dry-run-*.json`, nên tên
      đúng mẫu thì tự nằm trong vùng đã chặn. `chan_neu_git_khong_bo_qua` vẫn hỏi lại
      git trước khi ghi — quy ước là lớp một, hỏi git là lớp hai.
    """
    ten = f"dry-run-{nhan}-{duoi}.json" if nhan else f"dry-run-{duoi}.json"
    return os.path.join(JIRA_DIR, ten)


def bao_dam_git_bo_qua(duong: str) -> None:
    """Hỏi git TRƯỚC, và nói lý do bằng tiếng người.

    ⚠ Không gọi thẳng `X.chan_neu_git_khong_bo_qua` rồi bắt SystemExit: `sys.exit(1)`
      mang theo MÃ THOÁT chứ không mang theo câu giải thích, nên chỗ từ chối sẽ hiện
      lên giao diện thành chữ "1". Kiểm ở đây để câu từ chối đọc được, rồi vẫn gọi
      guard của connector làm lớp hai.
    """
    if subprocess.run(["git", "check-ignore", "-q", duong], cwd=GOC).returncode != 0:
        raise RuntimeError(
            f"{os.path.basename(duong)} chưa được .gitignore chặn, mà nội dung là ticket "
            f"thật của khách. Không ghi gì. Thêm mẫu tương ứng vào .gitignore rồi thử lại.")
    X.chan_neu_git_khong_bo_qua(duong)


# ------------------------------------------------- chốt nhóm -> định tuyến

# AI trả `doTinCay`; đường nạp của app đòi `nguon` với đúng ba giá trị. Ánh xạ ở đây
# chứ không bắt người dùng tự dịch — nhưng KHÔNG bịa: "thấp" không được lên
# `noi-ro-trong-mo-ta`, vì đó là nhãn nói rằng hồ sơ CÓ ghi nguyên nhân.
NGUON_THEO_TIN_CAY = {
    "cao": "noi-ro-trong-mo-ta",
    "vua": "suy-tu-trieu-chung",
    "thap": "suy-tu-trieu-chung",
}


def dung_taxonomy(phan_tich: dict, giu: dict[str, str]) -> dict:
    """Taxonomy theo hình dạng `LenhDinhTuyen.DocTaxonomy` đọc: chỉ `nhom[].ten` là bắt buộc.

    ⚠ KHÔNG kèm `caseKeys`. File taxonomy-19 có trường đó, nhưng nó là danh sách mã
      ticket thật; ở đây danh sách nhóm còn CHƯA được ai rà ngoài lượt bấm vừa rồi, nên
      không nhân bản thêm mã ticket ra một file nữa. Đường nạp không cần nó.
    """
    # 🛑 GỘP theo tên chốt, không phải mỗi nhóm gốc một mục. Hai nhóm gốc được đặt trùng
    #   tên là người vận hành CỐ Ý gộp; nếu cứ thế đẩy ra thì taxonomy có hai mục cùng
    #   `ten` mà `moTa` chỏi nhau. `DocTaxonomy` dựng HashSet nên không vỡ — nhưng cả khối
    #   taxonomy được dán nguyên vào prompt ở `DungGoi`, và hai định nghĩa mâu thuẫn dưới
    #   một cái tên là đúng thứ làm model phân nhóm tệ đi. Hỏng im lặng, ở tận vòng sau.
    theo_ten: dict[str, dict] = {}
    for n in phan_tich.get("nhom", []):
        ten_goc = str(n.get("ten", "")).strip()
        if ten_goc not in giu:
            continue

        ten = giu[ten_goc]
        mo_ta = str(n.get("moTa", "")).strip()
        dau_hieu = [d for d in (n.get("dauHieu") or []) if str(d).strip()]

        if ten not in theo_ten:
            theo_ten[ten] = {"ten": ten, "moTa": mo_ta, "dauHieu": list(dau_hieu)}
            continue

        cu = theo_ten[ten]
        if mo_ta and mo_ta not in cu["moTa"]:
            cu["moTa"] = (cu["moTa"] + " / " + mo_ta).strip(" /")
        for d in dau_hieu:
            if d not in cu["dauHieu"]:
                cu["dauHieu"].append(d)

    ra = list(theo_ten.values())

    return {
        "nhom": ra,
        "soNhom": len(ra),
        "_ghiChu": [
            "SINH TỰ ĐỘNG từ giao diện, sau khi người vận hành rà và chốt danh sách nhóm.",
            "Nguồn: một lượt AI phân nhóm trên corpus quét từ Jira.",
            "",
            "⚠ Đây CHƯA phải taxonomy đã đo như `taxonomy-19-nhom-hoa-don.json` (84,1%, docs/13).",
            "  Nó mới đủ để đường nạp chạy. Muốn tin con số nào thì phải chấm lại bằng",
            "  scripts/jira-export/cham_routing.py.",
        ],
    }


def chuyen_gan_nhan(phan_tich: dict, giu: dict[str, str]) -> tuple[list[dict], int]:
    """Đổi `ganNhan` của AI sang hình dạng `--nap-dinh-tuyen` đọc được.

    Hai hình dạng lệch nhau ở bốn tên trường, và đó là lý do file phân tích không tự nạp
    được: `maCase/nhom/doTinCay/lyDo` so với `khoaNguon/nhom/nguon/giaiThich`.

    🛑 Nhóm bị người vận hành BỎ thì case của nó thành `KHONG-BIET`, KHÔNG phải bị xoá
      khỏi lô. Bỏ hẳn dòng đi thì case đó trông như chưa bao giờ được xét, và lần xuất
      sau nó sẽ quay lại như mới — che mất việc đã có người nhìn và quyết định loại nó.
    """
    ra: list[dict] = []
    thanh_khong_biet = 0

    for g in phan_tich.get("ganNhan", []):
        ma = str(g.get("maCase", "")).strip()
        if not ma:
            continue

        ten_goc = str(g.get("nhom", "")).strip()
        ten_moi = giu.get(ten_goc)

        if ten_moi is None:
            if ten_goc != "KHONG-BIET":
                thanh_khong_biet += 1
            ra.append({
                "khoaNguon": ma,
                "nhom": "KHONG-BIET",
                "nguon": "khong-du-thong-tin",
                "giaiThich": (g.get("lyDo") or "")[:400] or "nhóm bị loại khi chốt danh sách",
            })
            continue

        tin_cay = str(g.get("doTinCay", "")).strip().lower()
        ra.append({
            "khoaNguon": ma,
            "nhom": ten_moi,
            "nguon": NGUON_THEO_TIN_CAY.get(tin_cay, "suy-tu-trieu-chung"),
            "giaiThich": (g.get("lyDo") or "")[:400] or "(AI không ghi lý do)",
        })

    return ra, thanh_khong_biet


def chay_nap_dinh_tuyen(file_kq: str, file_tax: str, ma_khach: str, nguoi_nap: str) -> tuple[int, str]:
    """Gọi `--nap-dinh-tuyen` bằng EXE ĐÃ BUILD, không qua `dotnet run`.

    🛑 Cố ý không dùng `dotnet run`: nó build lại, mà backend đang chạy đang giữ chính
      mấy file DLL đó -> build đỏ với MSB3027 "file locked". Người dùng sẽ thấy một lỗi
      về file khoá trong khi việc họ bấm là "nạp định tuyến". Exe đã build thì chạy được
      ngay và không đụng tới backend.
    """
    if not os.path.exists(EXE_APP):
        return 9, ("Chưa có bản build của app.\n"
                   "Chạy trước:  dotnet build src/KnowledgePlatform.Api\n"
                   f"(đang tìm ở {EXE_APP})")

    moi_truong = {
        **os.environ,
        "ASPNETCORE_ENVIRONMENT": "Development",
        # Trỏ app sang taxonomy vừa chốt. Không sửa file cấu hình nào của app.
        "DinhTuyen__DuongDanTaxonomy": os.path.abspath(file_tax),
    }

    kq = subprocess.run(
        [EXE_APP, "--nap-dinh-tuyen", ma_khach, os.path.abspath(file_kq), nguoi_nap],
        cwd=os.path.abspath(DU_AN_APP), capture_output=True, text=True,
        encoding="utf-8", errors="replace", env=moi_truong)

    return kq.returncode, (kq.stdout or "") + (kq.stderr or "")


# -------------------------------------------------------------- việc nền


def viec_quet(jql: str, max_issues: int, nhan: str) -> None:
    cfg = X.Config(
        jira_base=PHIEN["base"].rstrip("/"),
        jira_auth_header=PHIEN["auth"],
        jql=jql,
        app_base="http://localhost:5119",
        signal_key=None,
        max_issues=max_issues,
    )

    goc_out = sys.stdout
    sys.stdout = GhiTienDo(goc_out)
    try:
        TIEN_DO.append("Kéo issue từ Jira…")
        issues = X.fetch_issues(cfg)
        if not issues:
            raise RuntimeError("JQL không khớp issue nào.")

        TIEN_DO.append("Kéo description + comment…")
        cases, evidence = X.build_signals(cfg, issues)

        for duoi, data in (("cases", cases), ("evidence", evidence)):
            duong = duong_corpus(nhan, duoi)
            bao_dam_git_bo_qua(duong)
            with open(duong, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            TIEN_DO.append(f"đã ghi {os.path.basename(duong)}")

        PHIEN.update(cases=cases, evidence=evidence, nhan=nhan, jql=jql)
    finally:
        sys.stdout = goc_out

    TIEN_DO.append("Chạy cổng credential (check_corpus.py)…")
    kq = subprocess.run(
        [sys.executable, os.path.join(JIRA_DIR, "check_corpus.py"),
         duong_corpus(nhan, "cases"), duong_corpus(nhan, "evidence")],
        cwd=JIRA_DIR, capture_output=True, text=True, encoding="utf-8",
        env={**os.environ, "PYTHONIOENCODING": "utf-8"})

    bao_cao = (kq.stdout or "") + (kq.stderr or "")
    # `check_corpus` tự kết luận có chặn hay không — đọc kết luận của nó, đừng tự đoán lại.
    PHIEN["corpus_chan"] = "phát hiện CHẶN" in bao_cao
    DANG_CHAY["bao_cao"] = bao_cao


def viec_gui_kho(app_base: str, api_key: str) -> None:
    cfg = X.Config(
        jira_base=PHIEN["base"], jira_auth_header=PHIEN["auth"], jql=PHIEN["jql"] or "",
        app_base=app_base.rstrip("/"), signal_key=api_key or None, max_issues=0)

    goc_out = sys.stdout
    sys.stdout = GhiTienDo(goc_out)
    try:
        X.push(cfg, "/signals/case-observed", PHIEN["cases"], "case")
        X.push(cfg, "/signals/case-evidence", PHIEN["evidence"], "evidence")
    finally:
        sys.stdout = goc_out


def chay_nen(ten: str, ham, *a) -> None:
    TIEN_DO.clear()
    DANG_CHAY.update(viec=ten, xong=False, loi=None, bao_cao=None)

    def bao_boc():
        try:
            ham(*a)
        except (Exception, SystemExit) as ex:  # noqa: BLE001 — mọi lỗi phải hiện lên giao diện
            DANG_CHAY["loi"] = f"{type(ex).__name__}: {ex}"
            TIEN_DO.append("LỖI: " + str(ex))
            traceback.print_exc()
        finally:
            DANG_CHAY["xong"] = True

    threading.Thread(target=bao_boc, daemon=True).start()


# ------------------------------------------------------------- sinh prompt


def dung_prompt(cases: list, evidence: list) -> str:
    """Gói corpus + yêu cầu phân tích thành MỘT khối dán được cho AI.

    🛑 Prompt BẮT BUỘC có đường thoát KHONG-BIET. Đã đo trên chính nguồn này: khi
      không có đường thoát, 40,3% số case được gán nhãn tự tin trong khi hồ sơ CHƯA
      BAO GIỜ ghi ra nguyên nhân — tự tin không kiểm được, với sản phẩm thì nguy hiểm
      ngang gán sai. Xem docs/13 §3.
    """
    theo_case: dict[str, list] = {}
    for m in evidence:
        theo_case.setdefault(m["caseSourceReference"], []).append(m)

    khoi = []
    for c in cases:
        # ⚠ `.get` chứ không `[]`: một case thiếu trường không được phép làm hỏng CẢ
        #   prompt. Mất một tiêu đề thì AI vẫn đọc được phần còn lại; ném KeyError ở
        #   giữa vòng lặp thì người dùng mất trắng lô vừa quét mất 6 phút.
        mau = theo_case.get(c.get("sourceReference", ""), [])
        noi_dung = "\n".join(f"  - {(m.get('content') or '').strip()[:1500]}" for m in mau[:8])
        khoi.append(f"### {c.get('sourceReference', '(thiếu mã)')}\n"
                    f"Tiêu đề: {c.get('subject') or '(không có tiêu đề)'}\n"
                    f"{noi_dung or '  (không có nội dung)'}")

    return f"""Bạn là kỹ sư vận hành đang đọc {len(cases)} ticket hỗ trợ khách hàng thật.

NHIỆM VỤ
1. Gom các ticket CÙNG MỘT NGUYÊN NHÂN GỐC thành nhóm. Trùng lặp nghĩa là cùng nguyên
   nhân, không phải cùng từ ngữ.
2. Đặt tên mỗi nhóm theo NGUYÊN NHÂN, không theo triệu chứng.
3. Gán mỗi ticket vào đúng một nhóm.

LUẬT BẮT BUỘC
- Nếu hồ sơ KHÔNG nói ra nguyên nhân, gán nhóm "KHONG-BIET". Đừng suy đoán.
  Đây là luật quan trọng nhất: một nhãn tự tin mà không kiểm được thì có hại ngang
  một nhãn sai.
- Chỉ dùng thông tin có trong ticket. Không thêm kiến thức ngoài.
- `lyDo` phải trích đoạn văn bản trong ticket đã khiến bạn kết luận như vậy.

TRẢ VỀ ĐÚNG MỘT KHỐI JSON, không kèm lời dẫn:

```json
{{
  "nhom": [
    {{"ten": "Tên nhóm theo nguyên nhân", "moTa": "nguyên nhân gốc là gì",
      "dauHieu": ["dấu hiệu nhận ra nhóm này"]}}
  ],
  "ganNhan": [
    {{"maCase": "jira:ES-123", "nhom": "Tên nhóm", "doTinCay": "cao|vua|thap",
      "lyDo": "trích đoạn trong ticket"}}
  ]
}}
```

DỮ LIỆU
{chr(10).join(khoi)}
"""


def kiem_ket_qua(du_lieu: dict, cases: list) -> list[str]:
    """Soát JSON của AI. Trả danh sách lỗi — rỗng là hợp lệ.

    ⚠ Soát THẬT, không tin. Một mô hình trả JSON đúng cú pháp mà bịa mã case là ca
      thường gặp, và nó im lặng: nhãn vẫn nạp được, chỉ là gắn vào cái không tồn tại.
    """
    loi: list[str] = []
    if not isinstance(du_lieu, dict):
        return ["Kết quả không phải một đối tượng JSON."]

    nhom = du_lieu.get("nhom")
    gan = du_lieu.get("ganNhan")
    if not isinstance(nhom, list) or not nhom:
        loi.append("Thiếu mảng `nhom` (hoặc rỗng).")
    if not isinstance(gan, list) or not gan:
        loi.append("Thiếu mảng `ganNhan` (hoặc rỗng).")
    if loi:
        return loi

    ten_nhom = set()
    for i, n in enumerate(nhom):
        if not isinstance(n, dict) or not str(n.get("ten", "")).strip():
            loi.append(f"nhom[{i}] thiếu `ten`.")
        else:
            ten_nhom.add(n["ten"].strip())

    ma_that = {c["sourceReference"] for c in cases}
    for i, g in enumerate(gan):
        if not isinstance(g, dict):
            loi.append(f"ganNhan[{i}] không phải đối tượng.")
            continue
        ma = str(g.get("maCase", "")).strip()
        tn = str(g.get("nhom", "")).strip()
        if ma not in ma_that:
            loi.append(f"ganNhan[{i}]: `{ma}` KHÔNG có trong lô vừa quét — AI bịa mã case.")
        if tn != "KHONG-BIET" and tn not in ten_nhom:
            loi.append(f"ganNhan[{i}]: nhóm `{tn}` không nằm trong mảng `nhom`.")

    return loi[:25]


# ------------------------------------------------------------------ HTTP


class Handler(gia_lap.GiaLapApi, http.server.BaseHTTPRequestHandler):
    server_version = "KP-UI"

    def log_message(self, fmt, *args):
        pass  # im lặng; tiến độ đã có đường riêng

    # -- hạ tầng nhỏ --

    def _tra(self, ma: int, du_lieu: dict) -> None:
        raw = json.dumps(du_lieu, ensure_ascii=False).encode("utf-8")
        self.send_response(ma)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(raw)))
        self.end_headers()
        self.wfile.write(raw)

    def _doc(self) -> dict:
        n = int(self.headers.get("Content-Length") or 0)
        return json.loads(self.rfile.read(n).decode("utf-8")) if n else {}

    # -- GET --

    def do_GET(self):  # noqa: N802
        duong = urllib.parse.urlparse(self.path).path

        if console_web.la_duong_console(duong):
            console_web.phuc_vu_console(self, duong)
            return
        if console_web.la_duong_app(duong):
            console_web.chuyen_tiep(self, "GET")
            return

        trang = {"/": "index.html", "/index.html": "index.html", "/gia-lap": "gia-lap.html"}.get(duong)
        if trang:
            with open(os.path.join(HERE, trang), "rb") as f:
                raw = f.read()
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Content-Length", str(len(raw)))
            self.end_headers()
            self.wfile.write(raw)
            return

        if duong == "/api/tien-do":
            self._tra(200, {
                "dong": TIEN_DO[-200:],
                "xong": DANG_CHAY["xong"],
                "loi": DANG_CHAY["loi"],
                "viec": DANG_CHAY["viec"],
                "baoCao": DANG_CHAY.get("bao_cao"),
                "corpusChan": PHIEN["corpus_chan"],
                "congCheTatTam": CONG_CHE_TAT_TAM,
                "soCase": len(PHIEN["cases"]),
                "soEvidence": len(PHIEN["evidence"]),
            })
            return

        self.send_error(404)

    # -- POST --

    def do_PUT(self):  # noqa: N802
        if console_web.la_duong_app(urllib.parse.urlparse(self.path).path):
            console_web.chuyen_tiep(self, "PUT")
            return
        self.send_error(404)

    def do_POST(self):  # noqa: N802
        duong = urllib.parse.urlparse(self.path).path
        # Trước khi đọc thân: thân của đường chuyển tiếp đi nguyên byte sang host, không qua json.loads ở đây.
        if console_web.la_duong_app(duong):
            console_web.chuyen_tiep(self, "POST")
            return
        try:
            than = self._doc()
        except Exception as ex:  # noqa: BLE001
            self._tra(400, {"loi": f"Thân yêu cầu không phải JSON: {ex}"})
            return

        try:
            xu_ly = getattr(self, "api_" + duong.removeprefix("/api/").replace("-", "_"), None)
            if xu_ly is None:
                self.send_error(404)
                return
            xu_ly(than)
        # 🛑 BẮT CẢ SystemExit, không chỉ Exception. `X.die()` dùng sys.exit(), và
        #   SystemExit KHÔNG phải Exception — nên trước 2026-09-18 một lần `die()`
        #   trong handler làm kết nối đóng mà KHÔNG trả gì: trình duyệt chỉ thấy trang
        #   treo, đúng chỗ mà lý do từ chối là thứ người dùng cần đọc nhất.
        except (Exception, SystemExit) as ex:  # noqa: BLE001
            traceback.print_exc()
            ly_do = str(ex) or "cổng an toàn đã từ chối — xem cửa sổ lệnh"
            self._tra(500, {"loi": ly_do})

    # -- các API --

    def api_ket_noi(self, t: dict) -> None:
        base = (t.get("baseUrl") or "").strip().rstrip("/")
        if not base:
            self._tra(400, {"loi": "Thiếu địa chỉ Jira."})
            return

        auth = tao_auth(t.get("pat") or "", t.get("user") or "", t.get("matKhau") or "")

        try:
            toi = goi_jira(base, auth, "/rest/api/2/myself")
        except urllib.error.HTTPError as ex:
            self._tra(200, {"ok": False,
                            "loi": f"HTTP {ex.code} — {'sai thông tin đăng nhập' if ex.code in (401, 403) else ex.reason}"})
            return
        except Exception as ex:  # noqa: BLE001
            self._tra(200, {"ok": False, "loi": f"Không nối được: {type(ex).__name__}: {ex}"})
            return

        PHIEN["base"], PHIEN["auth"] = base, auth

        du_an = goi_jira(base, auth, "/rest/api/2/project")
        resolution = goi_jira(base, auth, "/rest/api/2/resolution")

        self._tra(200, {
            "ok": True,
            "ten": toi.get("displayName") or toi.get("name"),
            "duAn": sorted(({"key": d.get("key"), "ten": d.get("name")} for d in du_an),
                           key=lambda d: d["key"]),
            # 🛑 Lấy tên resolution TỪ JIRA, không gõ tay. Đã vấp: file mẫu ghi
            #   `Incomplete` và JQL trả HTTP 400 — Jira này chỉ có tên dài
            #   "Incomplete - nếu thông tin trong issue là chưa đủ".
            "resolution": [r.get("name") for r in resolution],
        })

    def api_dem(self, t: dict) -> None:
        if not PHIEN["auth"]:
            self._tra(400, {"loi": "Chưa kết nối Jira."})
            return
        q = urllib.parse.urlencode({"jql": t.get("jql") or "", "maxResults": 0, "fields": "key"})
        try:
            kq = goi_jira(PHIEN["base"], PHIEN["auth"], "/rest/api/2/search?" + q)
        except urllib.error.HTTPError as ex:
            chi_tiet = []
            try:
                chi_tiet = json.loads(ex.read().decode("utf-8")).get("errorMessages", [])
            except Exception:  # noqa: BLE001
                pass
            self._tra(200, {"ok": False, "loi": "; ".join(chi_tiet) or f"HTTP {ex.code}"})
            return
        self._tra(200, {"ok": True, "tong": kq.get("total", 0)})

    def api_quet(self, t: dict) -> None:
        if not PHIEN["auth"]:
            self._tra(400, {"loi": "Chưa kết nối Jira."})
            return
        if DANG_CHAY["viec"] and not DANG_CHAY["xong"]:
            self._tra(409, {"loi": "Đang chạy một việc khác."})
            return

        nhan = (t.get("nhan") or "ui").strip().lower()
        chay_nen("quet", viec_quet, t.get("jql") or "", int(t.get("maxIssues") or 150), nhan)
        self._tra(200, {"ok": True})

    def api_prompt(self, t: dict) -> None:
        if not PHIEN["cases"]:
            self._tra(400, {"loi": "Chưa quét được lô nào."})
            return

        prompt = dung_prompt(PHIEN["cases"], PHIEN["evidence"])
        duong = duong_corpus(PHIEN["nhan"], "prompt").removesuffix(".json") + ".txt"

        bao_dam_git_bo_qua(duong)
        with open(duong, "w", encoding="utf-8") as f:
            f.write(prompt)

        self._tra(200, {"ok": True, "prompt": prompt, "duong": duong, "soKyTu": len(prompt)})

    def api_nap(self, t: dict) -> None:
        if not PHIEN["cases"]:
            self._tra(400, {"loi": "Chưa quét được lô nào — không có gì để đối chiếu."})
            return

        tho = (t.get("ketQua") or "").strip()
        # AI hay bọc JSON trong ```json … ``` — gỡ hàng rào thay vì bắt người dùng tự sửa.
        if tho.startswith("```"):
            tho = tho.split("\n", 1)[-1]
            tho = tho.rsplit("```", 1)[0]

        try:
            du_lieu = json.loads(tho)
        except json.JSONDecodeError as ex:
            self._tra(200, {"ok": False, "loi": [f"Không đọc được thành JSON: {ex}"]})
            return

        loi = kiem_ket_qua(du_lieu, PHIEN["cases"])
        if loi:
            self._tra(200, {"ok": False, "loi": loi})
            return

        duong = duong_corpus(PHIEN["nhan"], "phan-tich")
        bao_dam_git_bo_qua(duong)
        with open(duong, "w", encoding="utf-8") as f:
            json.dump(du_lieu, f, ensure_ascii=False, indent=2)

        PHIEN["phan_tich"] = du_lieu

        gan = du_lieu["ganNhan"]
        biet = [g for g in gan if str(g.get("nhom", "")).strip() != "KHONG-BIET"]
        self._tra(200, {
            "ok": True, "duong": duong,
            "soNhom": len(du_lieu["nhom"]),
            "soGan": len(gan),
            "soBiet": len(biet),
            "soKhongBiet": len(gan) - len(biet),
            "nhom": [{"ten": n.get("ten"), "moTa": n.get("moTa", ""),
                      "dauHieu": n.get("dauHieu", []),
                      "soCase": sum(1 for g in gan if g.get("nhom") == n.get("ten"))}
                     for n in du_lieu["nhom"]],
        })

    def api_lo_da_co(self, t: dict) -> None:
        """Các lô đã phân tích còn nằm trên đĩa.

        ⚠ Có đường này vì trạng thái phiên nằm trong RAM: tắt giao diện là mất sạch
          PAT, corpus và kết quả AI. Không có nó thì muốn chốt lại danh sách nhóm của
          một lô cũ phải quét lại Jira từ đầu — mất vài phút cho một việc mà dữ liệu
          đã có sẵn.
        """
        ra = []
        for ten in sorted(os.listdir(JIRA_DIR)):
            if ten.startswith("dry-run-") and ten.endswith("-phan-tich.json"):
                nhan = ten[len("dry-run-"):-len("-phan-tich.json")]
                ra.append({"nhan": nhan, "ten": ten})
        self._tra(200, {"ok": True, "lo": ra})

    def api_mo_lo(self, t: dict) -> None:
        """Đọc lại một lô đã phân tích từ đĩa vào phiên."""
        nhan = (t.get("nhan") or "").strip()
        if not nhan:
            self._tra(400, {"loi": "Thiếu nhãn lô."})
            return

        duong = duong_corpus(nhan, "phan-tich")
        if not os.path.exists(duong):
            self._tra(200, {"ok": False, "loi": f"Không thấy {os.path.basename(duong)}."})
            return

        with open(duong, encoding="utf-8") as f:
            du_lieu = json.load(f)

        # Soát lại dù file do chính công cụ này ghi ra: giữa hai lần chạy có thể có người
        # sửa tay, và một file hỏng đọc vào đây sẽ nổ ở tận lúc dựng taxonomy.
        if not isinstance(du_lieu, dict) or not du_lieu.get("nhom") or not du_lieu.get("ganNhan"):
            self._tra(200, {"ok": False, "loi": "File không có đủ `nhom` và `ganNhan`."})
            return

        PHIEN["phan_tich"] = du_lieu
        PHIEN["nhan"] = nhan

        gan = du_lieu["ganNhan"]
        self._tra(200, {
            "ok": True,
            "nhan": nhan,
            "soGan": len(gan),
            "nhom": [{"ten": n.get("ten"), "moTa": n.get("moTa", ""),
                      "dauHieu": n.get("dauHieu", []),
                      "soCase": sum(1 for g in gan if g.get("nhom") == n.get("ten"))}
                     for n in du_lieu["nhom"]],
        })

    def api_chot_nhom(self, t: dict) -> None:
        """Bước 6: người vận hành đã rà xong danh sách nhóm -> sinh taxonomy + nạp định tuyến.

        🛑 Đây là chỗ DUY NHẤT quyết định tên nhóm nào được coi là hợp lệ. Sau bước này,
          mọi tên nằm ngoài danh sách sẽ bị `--nap-dinh-tuyen` từ chối — cố ý, vì một
          tên nhóm bịa ra sẽ đi thẳng tới người làm thật qua `/hoi` mà không có gì báo.
        """
        phan_tich = PHIEN.get("phan_tich")
        if not phan_tich:
            self._tra(400, {"loi": "Chưa nạp kết quả AI ở bước 5."})
            return

        ma_khach = (t.get("maKhach") or "").strip()
        nguoi_nap = (t.get("nguoiNap") or "").strip()
        if not ma_khach or not nguoi_nap:
            self._tra(400, {"loi": "Cần mã khách hàng và tên người nạp."})
            return

        # {tên gốc -> tên chốt}. Nhóm không có trong đây = người vận hành đã BỎ.
        giu: dict[str, str] = {}
        loi: list[str] = []
        da_dung: dict[str, str] = {}

        for d in t.get("nhom") or []:
            if not d.get("dung"):
                continue
            goc = str(d.get("tenGoc", "")).strip()
            moi = str(d.get("ten", "")).strip()
            if not goc:
                continue
            if not moi:
                loi.append(f"Nhóm “{goc}” để tên trống.")
                continue
            if moi == "KHONG-BIET":
                loi.append("Không được đặt tên nhóm là KHONG-BIET — đó là giá trị dành riêng.")
                continue
            # Gộp hai nhóm bằng cách đặt TRÙNG TÊN là hợp lệ và có chủ đích.
            da_dung.setdefault(moi, goc)
            giu[goc] = moi

        if not giu:
            loi.append("Không còn nhóm nào được giữ — cả lô sẽ thành KHÔNG BIẾT.")

        if loi:
            self._tra(200, {"ok": False, "loi": loi})
            return

        nhan = PHIEN["nhan"] or "ui"

        # Taxonomy ĐỂ CẠNH các file dry-run, không để trong docs/: nó là danh sách sinh từ
        # dữ liệu khách chưa ai rà kỹ, và `docs/ket-qua-phan-tich/` đang được git theo dõi.
        # Muốn đưa vào repo thì đó là một quyết định riêng, do người bấm chọn, không phải
        # hệ quả phụ của việc bấm nút ở đây.
        duong_tax = duong_corpus(nhan, "taxonomy")
        duong_kq = duong_corpus(nhan, "dinh-tuyen")

        for d in (duong_tax, duong_kq):
            bao_dam_git_bo_qua(d)

        taxonomy = dung_taxonomy(phan_tich, giu)
        dong, thanh_kb = chuyen_gan_nhan(phan_tich, giu)

        with open(duong_tax, "w", encoding="utf-8") as f:
            json.dump(taxonomy, f, ensure_ascii=False, indent=2)
        with open(duong_kq, "w", encoding="utf-8") as f:
            json.dump({"ketQua": dong}, f, ensure_ascii=False, indent=2)

        ma_thoat, bao_cao = chay_nap_dinh_tuyen(duong_kq, duong_tax, ma_khach, nguoi_nap)

        self._tra(200, {
            "ok": ma_thoat == 0,
            "maThoat": ma_thoat,
            "baoCao": bao_cao.strip(),
            "soNhomChot": len(taxonomy["nhom"]),
            "soNhomGop": len(giu) - len(set(giu.values())),
            "soDong": len(dong),
            "soThanhKhongBiet": thanh_kb,
            "duongTaxonomy": duong_tax,
            "duongKetQua": duong_kq,
        })

    def api_gui_kho(self, t: dict) -> None:
        if not PHIEN["cases"]:
            self._tra(400, {"loi": "Chưa quét được lô nào."})
            return
        if PHIEN["corpus_chan"] and not CONG_CHE_TAT_TAM:
            # 🛑 Không cho đi vòng. AR-j: đường nạp KHÔNG che credential, ThirdPartyRedactor
            #   chỉ che dữ liệu bên thứ ba. Cổng này là thứ duy nhất đứng giữa 61 chỗ nghi
            #   là bí mật và database.
            #   ⚠ Ngoại lệ DUY NHẤT: cờ `N1-2` (KP_CONG_CHE_TAT_TAM=1) — quyết định có ghi lại
            #     của chủ dự án, không phải một đường vòng.
            self._tra(409, {"loi": "check_corpus báo CHẶN — còn chỗ nghi là credential. "
                                   "Không gửi vào kho."})
            return
        if DANG_CHAY["viec"] and not DANG_CHAY["xong"]:
            self._tra(409, {"loi": "Đang chạy một việc khác."})
            return

        chay_nen("gui-kho", viec_gui_kho,
                 (t.get("appBase") or "http://localhost:5119"), (t.get("apiKey") or ""))
        self._tra(200, {"ok": True})


def main() -> int:
    srv = http.server.ThreadingHTTPServer((DIA_CHI, CONG), Handler)
    dia = f"http://{DIA_CHI}:{CONG}"
    print(f"Giao diện chạy ở {dia}  ·  console Vue: {dia}/console")
    print("⚠ Trang này hiện dữ liệu khách thật. Chỉ nghe trên loopback; Ctrl+C để tắt.")
    if CONG_CHE_TAT_TAM:
        print("🛑 KP_CONG_CHE_TAT_TAM=1 — cổng credential CHỈ BÁO, KHÔNG CHẶN (N1-2). "
              "Lô có mật khẩu/token vẫn gửi được vào kho.")
    try:
        webbrowser.open(dia)
    except Exception:  # noqa: BLE001 — không mở được trình duyệt thì vẫn chạy
        pass
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        print("\nĐã tắt.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
