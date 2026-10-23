"""Phiên TRẢI NGHIỆM của console — lưu lượt hỏi, kết quả, đánh giá của người thử (docs/DE_BAI_HOAN_THIEN_GIAO_DIEN_TRAI_NGHIEM.md §6).

    scripts/giao-dien/phien-trai-nghiem/<ngữ cảnh>/<id phiên>.json      — .gitignore chặn, ghi xong mới thay file

Vì sao Ở ĐÂY chứ không ở host hay trình duyệt:
  · host   — đây là công cụ ĐÁNH GIÁ nội bộ, không phải capability sản phẩm. Thêm bảng vào kp_* là thêm thứ sản phẩm hứa.
  · trình duyệt (localStorage) — xoá cache là mất nhận xét, và không ai ngoài trình duyệt đó đọc lại được để đối chiếu.
  · ui_server đã giữ đúng loại trạng thái này (cau-hinh-ai.json, gia-lap-io/), cùng luật loopback + .gitignore.

🛑 Ba luật:
  1. TÁCH THEO NGỮ CẢNH (cổng app + tenant). Mỗi ngữ cảnh một thư mục; đọc/ghi luôn phải nói ngữ cảnh nào — không có
     đường nào liệt kê chéo, nên đổi app không thể hiện lại lịch sử của app trước.
  2. KHÔNG GHI BÍ MẬT. Thân có khoá mang tên khoá/mật khẩu/token thì từ chối cả lượt — console không bao giờ gửi chúng,
     nên gặp là có lỗi ở nơi gọi. Nội dung nguồn trong lượt là bản host ĐÃ cho hiện (đã qua cổng che ở khâu gửi ra).
  3. LƯỢT KHÔNG ĐÈ LƯỢT. Ghi theo id lượt: id mới thì thêm, id cũ thì thay đúng lượt đó (cập nhật đánh giá). Hỏi lại cùng
     một câu là một id mới.

⚠ Đánh giá ở đây KHÔNG chạm host: không đổi mức kiểm chứng, trạng thái duyệt hay nấc USED của sản phẩm.
⚠ Thư viện CHUẨN, như phần còn lại của ui_server.
"""
from __future__ import annotations

import json
import os
import re
import subprocess
import threading
import uuid
from datetime import datetime, timezone

HERE = os.path.dirname(os.path.abspath(__file__))
GOC_REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
THU_MUC = os.path.join(HERE, "phien-trai-nghiem")
DINH_DANG = "kp-phien-trai-nghiem/1"

HINH_NGU_CANH = re.compile(r"^[a-z0-9][a-z0-9._-]{0,99}$")
HINH_ID = re.compile(r"^[A-Za-z0-9-]{8,64}$")

# Tên khoá (so không phân biệt hoa thường, bỏ - và _) mà một lượt không bao giờ được mang.
KHOA_CAM = {"pat", "matkhau", "password", "apikey", "xapikey", "secret", "bimat", "token", "authorization", "khoabimat"}

_KHOA = threading.Lock()


def bay_gio() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def tim_khoa_cam(x, duong: str = "") -> str | None:
    """Đường tới khoá cấm đầu tiên, hoặc None."""
    if isinstance(x, dict):
        for k, v in x.items():
            if re.sub(r"[-_]", "", str(k)).lower() in KHOA_CAM:
                return f"{duong}.{k}".lstrip(".")
            if (r := tim_khoa_cam(v, f"{duong}.{k}")) is not None:
                return r
    elif isinstance(x, list):
        for i, v in enumerate(x):
            if (r := tim_khoa_cam(v, f"{duong}[{i}]")) is not None:
                return r
    return None


def thu_muc_ngu_canh(ngu_canh: str, tao: bool = False) -> str:
    if not isinstance(ngu_canh, str) or not HINH_NGU_CANH.match(ngu_canh):
        raise ValueError(f"Ngữ cảnh không hợp lệ: {ngu_canh!r}")
    d = os.path.join(THU_MUC, ngu_canh)
    if tao:
        os.makedirs(d, exist_ok=True)
        if subprocess.run(["git", "check-ignore", "-q", d], cwd=GOC_REPO).returncode != 0:
            raise RuntimeError(f"{d} chưa được .gitignore chặn — không ghi. Thêm `scripts/giao-dien/phien-trai-nghiem/`.")
    return d


def duong_phien(ngu_canh: str, id_phien: str, tao: bool = False) -> str:
    if not isinstance(id_phien, str) or not HINH_ID.match(id_phien):
        raise ValueError(f"Id phiên không hợp lệ: {id_phien!r}")
    return os.path.join(thu_muc_ngu_canh(ngu_canh, tao), id_phien + ".json")


def doc(ngu_canh: str, id_phien: str) -> dict | None:
    p = duong_phien(ngu_canh, id_phien)
    if not os.path.isfile(p):
        return None
    with open(p, encoding="utf-8") as f:
        return json.load(f)


def ghi(ngu_canh: str, phien: dict) -> None:
    p = duong_phien(ngu_canh, phien["id"], tao=True)
    tam = p + ".tam"
    with open(tam, "w", encoding="utf-8") as f:
        json.dump(phien, f, ensure_ascii=False, indent=1)
    os.replace(tam, p)


def liet_ke(ngu_canh: str) -> list[dict]:
    d = thu_muc_ngu_canh(ngu_canh)
    if not os.path.isdir(d):
        return []
    ra = []
    for ten in os.listdir(d):
        if not ten.endswith(".json"):
            continue
        try:
            with open(os.path.join(d, ten), encoding="utf-8") as f:
                p = json.load(f)
        except (OSError, ValueError):
            continue  # file hỏng không được làm mất cả danh sách
        ra.append({"id": p.get("id"), "ten": p.get("ten"), "kichBan": p.get("kichBan"), "taoLuc": p.get("taoLuc"),
                   "capNhatLuc": p.get("capNhatLuc"), "soLuot": len(p.get("luot") or [])})
    return sorted(ra, key=lambda x: x.get("capNhatLuc") or "", reverse=True)


def tao(ngu_canh: str, t: dict) -> dict:
    luc = bay_gio()
    phien = {
        "dinhDang": DINH_DANG,
        "id": str(uuid.uuid4()),
        # Tên mặc định theo giờ MÁY (người đọc nghĩ theo giờ địa phương); mốc taoLuc vẫn UTC.
        "ten": (t.get("ten") or "").strip() or f"Phiên {datetime.now().strftime('%Y-%m-%d %H:%M')}",
        # Mô tả ngữ cảnh (app, cổng, tenant) để bản xuất tự đứng được; KHOÁ thư mục vẫn là `ngu_canh` đã kiểm.
        "nguCanh": {**(t.get("thongTinNguCanh") or {}), "khoa": ngu_canh},
        "kichBan": t.get("kichBan"),
        "nguoiThaoTac": (t.get("nguoiThaoTac") or "").strip() or None,
        "ghiChu": t.get("ghiChu") or "",
        "taoLuc": luc,
        "capNhatLuc": luc,
        "luot": [],
    }
    if (k := tim_khoa_cam(phien)) is not None:
        raise ValueError(f"Phiên mang khoá cấm `{k}` — không ghi.")
    ghi(ngu_canh, phien)
    return phien


def ghi_luot(ngu_canh: str, id_phien: str, luot: dict) -> dict:
    if not isinstance(luot, dict) or not isinstance(luot.get("id"), str) or not HINH_ID.match(luot["id"]):
        raise ValueError("Lượt phải có `id` hợp lệ.")
    if (k := tim_khoa_cam(luot)) is not None:
        raise ValueError(f"Lượt mang khoá cấm `{k}` — không ghi gì.")
    phien = doc(ngu_canh, id_phien)
    if phien is None:
        raise LookupError("Không thấy phiên trong ngữ cảnh này.")
    ds = phien.setdefault("luot", [])
    for i, x in enumerate(ds):
        if x.get("id") == luot["id"]:
            ds[i] = luot
            break
    else:
        ds.append(luot)
    phien["capNhatLuc"] = bay_gio()
    ghi(ngu_canh, phien)
    return phien


def sua(ngu_canh: str, id_phien: str, t: dict) -> dict:
    phien = doc(ngu_canh, id_phien)
    if phien is None:
        raise LookupError("Không thấy phiên trong ngữ cảnh này.")
    for k in ("ten", "ghiChu", "nguoiThaoTac"):
        if k in t:
            phien[k] = t[k]
    phien["capNhatLuc"] = bay_gio()
    ghi(ngu_canh, phien)
    return phien


def xoa(ngu_canh: str, id_phien: str) -> bool:
    p = duong_phien(ngu_canh, id_phien)
    if not os.path.isfile(p):
        return False
    os.remove(p)
    return True


class PhienTraiNghiemApi:
    """Trộn vào Handler của ui_server — `/api/phien-*`. Mọi lời gọi PHẢI mang `nguCanh`."""

    def _phien(self, viec, t: dict) -> None:
        try:
            with _KHOA:
                self._tra(200, {"ok": True, **viec(t)})
        except ValueError as ex:
            self._tra(400, {"ok": False, "loi": str(ex)})
        except LookupError as ex:
            self._tra(404, {"ok": False, "loi": str(ex)})

    def api_phien_ds(self, t: dict) -> None:
        self._phien(lambda t: {"phien": liet_ke(t.get("nguCanh"))}, t)

    def api_phien_doc(self, t: dict) -> None:
        def viec(t):
            p = doc(t.get("nguCanh"), t.get("id"))
            if p is None:
                raise LookupError("Không thấy phiên trong ngữ cảnh này.")
            return {"phien": p}
        self._phien(viec, t)

    def api_phien_tao(self, t: dict) -> None:
        self._phien(lambda t: {"phien": tao(t.get("nguCanh"), t)}, t)

    def api_phien_luot(self, t: dict) -> None:
        self._phien(lambda t: {"phien": ghi_luot(t.get("nguCanh"), t.get("id"), t.get("luot"))}, t)

    def api_phien_sua(self, t: dict) -> None:
        self._phien(lambda t: {"phien": sua(t.get("nguCanh"), t.get("id"), t)}, t)

    def api_phien_xoa(self, t: dict) -> None:
        self._phien(lambda t: {"daXoa": xoa(t.get("nguCanh"), t.get("id"))}, t)
