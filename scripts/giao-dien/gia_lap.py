"""Trang GIẢ LẬP hai luồng — `O1-1`/`O4-2`/`O6-1` (docs/18 nhóm O, 2026-10-19).

Lái app THỨ HAI (`run-gia-lap-app.bat`: kp_gialap, khách `gia-lap`, cổng 5120) qua cả hai luồng, với
một trong hai cách gán AI ở mỗi bước có AI:

    API   app tự gọi model bằng khoá trong appsettings.Local.json (O2-1)
    file  xuất prompt ra file → Claude/Codex đọc, ghi JSON → nạp lại (vòng không-credit, IM-45/46)

    luồng XUÔI   ticket bịa → Kênh 1 → định tuyến (AI) → chấm với đáp án bịa
    luồng NGƯỢC  case một nhóm → soạn nháp SOP (AI) → duyệt tri thức + quy trình
    đóng vòng    /hoi lại → bước hiện tại + hướng dẫn kèm dẫn chứng → xác nhận bước → phản hồi (ô J)

⚠ Mọi việc đi qua ĐÚNG đường mà sản phẩm có — HTTP của app, hoặc lệnh CLI của chính app. Trang này
  không ghi thẳng vào database, không tự dựng kết quả: một lối tắt ở đây là một thứ test không kiểm.
⚠ Dữ liệu ở đây BỊA (gia_lap_du_lieu.py), khác trang quét Jira — nhưng file trao đổi với AI vẫn nằm
  trong thư mục .gitignore chặn, vì cổng `KiemChoGhi` của lệnh xuất HỎI GIT, không hỏi nội dung.
"""
from __future__ import annotations

import json
import os
import subprocess
import urllib.error
import urllib.parse
import urllib.request
from collections import Counter

import gia_lap_du_lieu as D

HERE = os.path.dirname(os.path.abspath(__file__))
GOC_REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
DU_AN_APP = os.path.join(GOC_REPO, "src", "KnowledgePlatform.Api")
EXE_APP = os.path.join(DU_AN_APP, "bin", "Debug", "net10.0", "KnowledgePlatform.Api.exe")
THU_MUC_IO = os.path.join(HERE, "gia-lap-io")
TAXONOMY = os.path.join(GOC_REPO, "docs", "ket-qua-phan-tich", "taxonomy-19-nhom-hoa-don.json")

MA_KHACH = "gia-lap"
CHUOI_KET_NOI = "Host=localhost;Port=5432;Database=kp_gialap;Username=kp_app;Password=123456"

# Trạng thái phiên — RAM. Tắt giao diện là mất; lô trên database vẫn còn.
GL: dict = {
    "base": "http://localhost:5120",
    "lo": None,            # kết quả gia_lap_du_lieu.tao_lo
    "dinhTuyen": {},       # khoá nguồn -> {nhom, nguon, biet}
    "banNhap": None,       # SopDraftIngestResult cuối cùng
    "nhom": None,          # nhóm đang soạn nháp
}


# ----------------------------------------------------------------- hạ tầng


def goi_app(method: str, duong: str, body=None, timeout: int = 300) -> tuple[int, object]:
    data = json.dumps(body, ensure_ascii=False).encode("utf-8") if body is not None else None
    req = urllib.request.Request(GL["base"].rstrip("/") + duong, data=data, method=method)
    if data is not None:
        req.add_header("Content-Type", "application/json; charset=utf-8")
    try:
        with urllib.request.urlopen(req, timeout=timeout) as r:
            raw = r.read().decode("utf-8", errors="replace")
            return r.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8", errors="replace")
        try:
            return e.code, json.loads(raw)
        except ValueError:
            return e.code, raw
    except urllib.error.URLError as e:
        raise RuntimeError(f"Không gọi được app giả lập ở {GL['base']} — {e.reason}. "
                           f"Chạy scripts\\giao-dien\\run-gia-lap-app.bat trước.") from e


def moi_truong_cli() -> dict:
    """Cùng cấu hình với run-gia-lap-app.bat — lệnh CLI và app phải nhìn CÙNG một database."""
    return {
        **os.environ,
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ConnectionStrings__Default": CHUOI_KET_NOI,
        "Tenancy__Mode": "DedicatedSingleTenant",
        "Tenancy__TenantExternalKey": MA_KHACH,
        # Log từng câu SQL của EF làm báo cáo lệnh dài vài trăm dòng — người đọc mất đúng dòng cần đọc.
        "Logging__LogLevel__Default": "Warning",
    }


def chay_cli(*doi_so: str) -> tuple[int, str]:
    """Gọi EXE đã build — không `dotnet run` (app đang chạy giữ DLL, build lại sẽ đỏ MSB3027)."""
    if not os.path.exists(EXE_APP):
        return 9, f"Chưa có bản build của app ({EXE_APP}). Chạy: dotnet build src/KnowledgePlatform.Api"
    kq = subprocess.run([EXE_APP, *doi_so], cwd=DU_AN_APP, capture_output=True, text=True,
                        encoding="utf-8", errors="replace", env=moi_truong_cli())
    return kq.returncode, ((kq.stdout or "") + (kq.stderr or "")).strip()


def thu_muc_lo() -> str:
    lo = can_lo()
    duong = os.path.join(THU_MUC_IO, lo["lo"])
    os.makedirs(duong, exist_ok=True)
    if subprocess.run(["git", "check-ignore", "-q", duong], cwd=GOC_REPO).returncode != 0:
        raise RuntimeError(f"{duong} chưa được .gitignore chặn. Không ghi gì.")
    return duong


def can_lo() -> dict:
    if not GL["lo"]:
        GL["lo"] = mo_lo_moi_nhat()
    if not GL["lo"]:
        raise RuntimeError("Chưa sinh lô dữ liệu giả (bước 1).")
    return GL["lo"]


def mo_lo_moi_nhat() -> dict | None:
    """Lô mới nhất trên đĩa. Có vì phiên nằm trong RAM: tắt giao diện là mất lô, trong khi case của
    lô vẫn nằm trong kp_gialap — sinh lô mới chỉ để đi tiếp là để rác lại trong database."""
    if not os.path.isdir(THU_MUC_IO):
        return None
    for ten in sorted(os.listdir(THU_MUC_IO), reverse=True):
        p = os.path.join(THU_MUC_IO, ten, "lo.json")
        if os.path.exists(p):
            with open(p, encoding="utf-8") as f:
                return json.load(f)
    return None


def ghi_ban_nhap(bn: dict) -> None:
    """Bản nháp vừa nạp — RAM + đĩa (cạnh lo.json). Tải lại trang hay khởi động lại ui_server vẫn mở được màn
    Bản nháp SOP cho đúng các id đó, thay vì phải soạn lại (tốn một lượt model ở chế độ API)."""
    GL["banNhap"] = bn
    with open(os.path.join(thu_muc_lo(), "ban-nhap.json"), "w", encoding="utf-8") as f:
        json.dump(bn, f, ensure_ascii=False, indent=2)


def doc_ban_nhap() -> dict | None:
    if GL.get("banNhap") or not GL.get("lo"):
        return GL.get("banNhap")
    p = os.path.join(THU_MUC_IO, GL["lo"]["lo"], "ban-nhap.json")
    if os.path.exists(p):
        with open(p, encoding="utf-8") as f:
            GL["banNhap"] = json.load(f)
    return GL.get("banNhap")


def doc_json_ai(tho: str):
    """JSON AI trả về — gỡ hàng rào ``` và lời dẫn trước/sau khối JSON thay vì bắt người dùng tự sửa."""
    tho = (tho or "").strip().lstrip("﻿")
    if "```" in tho:
        phan = tho.split("```")
        # khối nằm giữa cặp hàng rào đầu tiên; bỏ nhãn ngôn ngữ ở dòng đầu
        if len(phan) >= 3:
            tho = phan[1].split("\n", 1)[1] if "\n" in phan[1] else phan[1]
    dau = min([i for i in (tho.find("{"), tho.find("[")) if i >= 0], default=-1)
    if dau > 0:
        tho = tho[dau:]
    return json.loads(tho)


def lenh_goi_y(duong_vao: str, duong_ra: str, viec: str) -> dict:
    """Câu giao việc cho một agent đọc file. Chỉ là GỢI Ý — dán vào khung chat cũng được.

    ⚠ KHÔNG có dấu backtick hay nháy kép trong câu: nó nằm trong "…" của một lệnh shell, và PowerShell
      coi backtick là ký tự thoát — `h thành ký tự lạ, câu tới tay AI đã hỏng mà không ai thấy.
    """
    cau = (f"Đọc file {duong_vao}. {viec} Ghi DUY NHẤT khối JSON kết quả (không lời dẫn, không hàng rào code) "
           f"vào file {duong_ra}.")
    return {
        "cau": cau,
        "claude": f'claude -p "{cau}" --allowedTools "Read,Write"',
        "codex": f'codex exec --full-auto "{cau}"',
    }


def hai_muc_cau_hinh(ra: str) -> str:
    """Chỉ hai mục của `--kiem-cau-hinh` liên quan tới AI — cả bản in dài gần trăm dòng."""
    giu, dang_giu = [], False
    for dong in ra.splitlines():
        if dong.startswith("──"):
            dang_giu = "Khoá Anthropic" in dong or "Định tuyến ticket" in dong
        if dang_giu:
            giu.append(dong)
    return "\n".join(giu)


def ma_khach_san(khoa: str) -> str:
    for m in can_lo()["evidence"]:
        if m["caseSourceReference"] == khoa:
            return m["subTenantCode"]
    return "khong-xac-dinh"


# ------------------------------------------------------------------ chấm


def hoi_ca_lo(chi_khoa: list[str] | None = None) -> list[dict]:
    """POST /hoi cho từng ticket. Ở chế độ API đây là chỗ app GỌI MODEL định tuyến (nếu bật GoiModel)."""
    lo = can_lo()
    dong = []
    for khoa, da in lo["dapAn"].items():
        if chi_khoa is not None and khoa not in chi_khoa:
            continue
        s, b = goi_app("POST", "/hoi", {"maTicket": khoa})
        b = b if isinstance(b, dict) else {}
        nhom = b.get("nhom") if b.get("nhom") else None
        nguon = b.get("nguonDinhTuyen") or f"HTTP {s}"
        GL["dinhTuyen"][khoa] = {"nhom": nhom, "nguon": nguon}

        qt = b.get("quyTrinh") or {}
        buoc = qt.get("buocHienTai") or {}
        dong.append({
            "khoa": khoa,
            "tieuDe": da["tieuDe"],
            "nhomDung": da["nhomDung"],
            "nhom": nhom or D.KHONG_BIET,
            "nguon": nguon,
            "giaiThich": b.get("giaiThichDinhTuyen"),
            "ketQua": cham_mot(nhom, da["nhomDung"]),
            # HTTP khác 200 = LỖI VẬN HÀNH (app chết, 4xx/5xx) — khác hẳn "không biết", vốn là 200 hợp lệ.
            "http": s,
            "loiVanHanh": None if s == 200 else (json.dumps(b, ensure_ascii=False)[:300] if b else f"HTTP {s}"),
            "bietTraLoi": b.get("bietTraLoi"),
            "lyDoKhongBiet": b.get("lyDoKhongBiet"),
            "soHuongDan": len(b.get("huongDan") or []),
            "huongDan": b.get("huongDan") or [],
            "buocHienTai": buoc.get("ma"),
            "noiDungBuoc": buoc.get("noiDung"),
            "noiXemBuoc": buoc.get("noiXem"),
            "quyTrinh": ({"id": qt.get("id"), "ten": qt.get("ten"), "phienBan": qt.get("phienBan"),
                          "traTheo": qt.get("traTheo")} if qt else None),
            "daXongHet": qt.get("daXongHet"),
            "lyDoKhongCoQuyTrinh": b.get("lyDoKhongCoQuyTrinh"),
            "maLuotGoiY": b.get("maLuotGoiY"),
        })
    return dong


def cham_mot(nhom: str | None, dung: str) -> str:
    if not nhom or nhom == D.KHONG_BIET:
        return "dung" if dung == D.KHONG_BIET else "khong-biet"
    return "dung" if nhom == dung else "sai"


def tong_cham(dong: list[dict]) -> dict:
    c = Counter(d["ketQua"] for d in dong)
    return {"dung": c["dung"], "sai": c["sai"], "khongBiet": c["khong-biet"], "tong": len(dong)}


def nhom_da_dinh_tuyen() -> list[dict]:
    c = Counter(v["nhom"] for v in GL["dinhTuyen"].values() if v.get("nhom") and v["nhom"] != D.KHONG_BIET)
    return [{"ten": t, "soCase": n} for t, n in c.most_common()]


# ------------------------------------------------------------------- API


class GiaLapApi:
    """Trộn vào Handler của ui_server — mỗi `api_gl_*` là một nút trên trang giả lập."""

    def api_gl_trang_thai(self, t: dict) -> None:
        if t.get("base"):
            GL["base"] = t["base"].strip().rstrip("/")

        try:
            s, _ = goi_app("GET", "/health/ready", timeout=10)
            song = s == 200
        except RuntimeError as ex:
            song, s = False, str(ex)

        # `--kiem-cau-hinh` đọc ĐÚNG cấu hình mà app giả lập đọc (cùng biến môi trường, cùng file Local).
        ma, ra = chay_cli("--kiem-cau-hinh")
        co_khoa = "CÓ khoá" in ra
        goi_model = "BẬT — mọi ticket" in ra

        sai_tax = D.kiem_nhom_dung(TAXONOMY)
        if not GL["lo"]:
            GL["lo"] = mo_lo_moi_nhat()

        self._tra(200, {
            "ok": True, "base": GL["base"], "appSong": song, "chiTietApp": s,
            "coKhoa": co_khoa, "goiModel": goi_model,
            "cauHinh": hai_muc_cau_hinh(ra),
            "nhomDungLech": sai_tax,
            "lo": GL["lo"]["lo"] if GL["lo"] else None,
            # Console (P12-4) dựng lại bảng lô sau khi tải lại trang mà không phải sinh lô mới.
            "tienTo": GL["lo"]["tienTo"] if GL["lo"] else None,
            "ticket": [{"khoa": k, **v} for k, v in GL["lo"]["dapAn"].items()] if GL["lo"] else [],
            "banNhap": doc_ban_nhap(),
        })

    def api_gl_sinh(self, t: dict) -> None:
        lo = D.tao_lo(int(t.get("soLuong") or 0) or None)
        s1, b1 = goi_app("POST", "/signals/case-observed", lo["cases"])
        if s1 != 200:
            self._tra(200, {"ok": False, "loi": f"case-observed HTTP {s1}: {json.dumps(b1, ensure_ascii=False)[:400]}"})
            return
        s2, b2 = goi_app("POST", "/signals/case-evidence", lo["evidence"])
        if s2 != 200:
            self._tra(200, {"ok": False, "loi": f"case-evidence HTTP {s2}: {json.dumps(b2, ensure_ascii=False)[:400]}"})
            return

        GL.update(lo=lo, dinhTuyen={}, banNhap=None, nhom=None)
        with open(os.path.join(thu_muc_lo(), "lo.json"), "w", encoding="utf-8") as f:
            json.dump(lo, f, ensure_ascii=False, indent=2)
        self._tra(200, {
            "ok": True, "lo": lo["lo"], "tienTo": lo["tienTo"],
            "soCase": b1.get("created"), "soEvidence": b2.get("created"),
            "ticket": [{"khoa": k, **v} for k, v in lo["dapAn"].items()],
        })

    # -------- luồng xuôi: định tuyến

    def api_gl_dinh_tuyen_api(self, t: dict) -> None:
        dong = hoi_ca_lo()
        self._tra(200, {"ok": True, "dong": dong, "cham": tong_cham(dong), "nhom": nhom_da_dinh_tuyen()})

    def api_gl_xuat_dinh_tuyen(self, t: dict) -> None:
        lo = can_lo()
        thu_muc = thu_muc_lo()
        vao = os.path.join(thu_muc, "dinh-tuyen.prompt.json")
        ra = os.path.join(thu_muc, "dinh-tuyen.ketqua.json")

        ma, bao_cao = chay_cli("--xuat-dinh-tuyen", MA_KHACH, vao, str(len(lo["cases"])), lo["tienTo"])
        if ma != 0 or not os.path.exists(vao):
            self._tra(200, {"ok": False, "loi": f"Lệnh xuất trả mã {ma}.", "baoCao": bao_cao})
            return

        with open(vao, encoding="utf-8") as f:
            noi_dung = f.read()
        self._tra(200, {
            "ok": True, "duongVao": vao, "duongRa": ra, "noiDung": noi_dung, "baoCao": bao_cao,
            "lenh": lenh_goi_y(vao, ra, "Làm đúng trường huongDan trong đó cho từng ticket, theo trường schemaDauRa."),
        })

    def api_gl_nap_dinh_tuyen(self, t: dict) -> None:
        thu_muc = thu_muc_lo()
        ra = os.path.join(thu_muc, "dinh-tuyen.ketqua.json")
        nguoi = (t.get("nguoi") or "").strip() or "trang-gia-lap"

        tho = t.get("ketQua")
        if not (tho or "").strip():
            if not os.path.exists(ra):
                self._tra(200, {"ok": False, "loi": f"Chưa dán kết quả và chưa có file {ra}."})
                return
            with open(ra, encoding="utf-8-sig") as f:
                tho = f.read()

        try:
            du_lieu = doc_json_ai(tho)
        except ValueError as ex:
            self._tra(200, {"ok": False, "loi": f"Không đọc được thành JSON: {ex}"})
            return

        sach = os.path.join(thu_muc, "dinh-tuyen.ketqua.sach.json")
        with open(sach, "w", encoding="utf-8") as f:
            json.dump(du_lieu, f, ensure_ascii=False, indent=2)

        ma, bao_cao = chay_cli("--nap-dinh-tuyen", MA_KHACH, sach, nguoi)
        dong = hoi_ca_lo()
        self._tra(200, {"ok": ma == 0, "maThoat": ma, "baoCao": bao_cao,
                        "dong": dong, "cham": tong_cham(dong), "nhom": nhom_da_dinh_tuyen()})

    # -------- luồng ngược: soạn nháp + duyệt

    def api_gl_soan_api(self, t: dict) -> None:
        nhom = (t.get("nhom") or "").strip()
        GL["nhom"] = nhom
        # Q6-1: nhà cung cấp do màn Cấu hình chọn; bỏ trống = mặc định của host (SoanNhap:NhaCungCap).
        s, b = goi_app("POST", "/knowledge/soan-nhap",
                       {"nhom": nhom, "nguoiYeuCau": (t.get("nguoi") or "trang-gia-lap").strip(),
                        "nhaCungCap": t.get("nhaCungCap") or None})
        b = b if isinstance(b, dict) else {"raw": b}
        if s == 200 and b.get("daNap"):
            ghi_ban_nhap(b["daNap"])
        self._tra(200, {"ok": s == 200, "http": s, "ketQua": b})

    def api_gl_xuat_soan(self, t: dict) -> None:
        nhom = (t.get("nhom") or "").strip()
        GL["nhom"] = nhom
        thu_muc = thu_muc_lo()
        vao = os.path.join(thu_muc, "soan-nhap.prompt.json")
        ra = os.path.join(thu_muc, "soan-nhap.ketqua.json")

        ma, bao_cao = chay_cli("--xuat-soan-nhap", MA_KHACH, nhom, vao)
        if ma != 0 or not os.path.exists(vao):
            self._tra(200, {"ok": False, "loi": f"Lệnh xuất trả mã {ma}.", "baoCao": bao_cao})
            return

        with open(vao, encoding="utf-8") as f:
            noi_dung = f.read()
        self._tra(200, {
            "ok": True, "duongVao": vao, "duongRa": ra, "noiDung": noi_dung, "baoCao": bao_cao,
            "lenh": lenh_goi_y(vao, ra, "Coi trường system là chỉ dẫn hệ thống và trường user là yêu cầu; "
                                        "trả bản nháp SOP đúng trường schemaDauRa."),
        })

    def api_gl_nap_soan(self, t: dict) -> None:
        thu_muc = thu_muc_lo()
        ra = os.path.join(thu_muc, "soan-nhap.ketqua.json")
        tho = t.get("ketQua")
        if not (tho or "").strip():
            if not os.path.exists(ra):
                self._tra(200, {"ok": False, "loi": f"Chưa dán kết quả và chưa có file {ra}."})
                return
            with open(ra, encoding="utf-8-sig") as f:
                tho = f.read()
        try:
            du_lieu = doc_json_ai(tho)
        except ValueError as ex:
            self._tra(200, {"ok": False, "loi": f"Không đọc được thành JSON: {ex}"})
            return

        s, b = goi_app("POST", "/knowledge/sop-draft", du_lieu)
        if s == 200 and isinstance(b, dict):
            ghi_ban_nhap(b)
        self._tra(200, {"ok": s == 200, "http": s, "ketQua": b})

    def api_gl_duyet(self, t: dict) -> None:
        # ⚠ DUYỆT NHANH — chỉ cho dữ liệu BỊA của giả lập: tự khai `xacNhanDuLieuThuongMai: true` hộ người bấm. Dữ liệu
        #   thật đi màn Bản nháp SOP của console, nơi người duyệt đọc nội dung rồi TỰ tích lời khai đó.
        bn = doc_ban_nhap()
        if not bn:
            self._tra(400, {"loi": "Chưa có bản nháp nào được nạp ở bước soạn nháp."})
            return

        nguoi = (t.get("nguoi") or "").strip() or "trang-gia-lap"
        than = {"nguoiDuyet": nguoi, "phamViXem": "noi-bo", "xacNhanDuLieuThuongMai": True}
        dong = []
        for r in bn.get("nguyenNhan") or []:
            s, b = goi_app("POST", f"/knowledge/{r['id']}/approve", than)
            dong.append({"loai": "tri thức", "ten": r.get("tenNguyenNhan"), "http": s,
                         "trangThai": (b or {}).get("trangThaiSau") if isinstance(b, dict) else None,
                         "loi": None if s == 200 else json.dumps(b, ensure_ascii=False)[:300]})

        qt = bn.get("quyTrinh") or {}
        if qt.get("id"):
            s, b = goi_app("POST", f"/processes/{qt['id']}/approve", than)
            dong.append({"loai": "quy trình", "ten": f"phiên bản {qt.get('phienBan')} · {qt.get('soBuoc')} bước",
                         "http": s, "trangThai": (b or {}).get("trangThai") if isinstance(b, dict) else None,
                         "loi": None if s == 200 else json.dumps(b, ensure_ascii=False)[:300]})

        self._tra(200, {"ok": all(d["http"] == 200 for d in dong), "dong": dong})

    # -------- đóng vòng: hỏi lại, xác nhận bước, phản hồi

    def api_gl_hoi_lai(self, t: dict) -> None:
        # Console gửi nhóm + khoá TƯỜNG MINH (nó đọc nhóm hiệu lực từ host); trang HTML cũ dựa vào RAM của phiên —
        # khởi động lại server là mất, và "hỏi lại" lặng lẽ hỏi cả lô thay vì một nhóm.
        nhom = (t.get("nhom") or "").strip() or GL.get("nhom")
        khoa = t.get("khoa") or ([k for k, v in GL["dinhTuyen"].items() if v.get("nhom") == nhom] if nhom else None)
        dong = hoi_ca_lo(khoa)
        self._tra(200, {"ok": True, "nhom": nhom, "dong": dong})

    def api_gl_xac_nhan_buoc(self, t: dict) -> None:
        khoa, ma_buoc = (t.get("khoa") or "").strip(), (t.get("maBuoc") or "").strip()
        s, b = goi_app("POST", "/quy-trinh/xac-nhan-buoc", {
            "khoaNguon": khoa, "maBuoc": ma_buoc,
            "nguoiXacNhan": (t.get("nguoi") or "trang-gia-lap").strip(),
            "subTenantCode": ma_khach_san(khoa), "ghiChu": "xác nhận từ trang giả lập",
        })
        dong = hoi_ca_lo([khoa])
        self._tra(200, {"ok": s == 200, "http": s, "ketQua": b, "dong": dong})

    def api_gl_phan_hoi(self, t: dict) -> None:
        ma = (t.get("maLuotGoiY") or "").strip()
        s, b = goi_app("POST", f"/goi-y/{urllib.parse.quote(ma)}/phan-hoi", {
            "nac": t.get("nac") or "used", "datDuoc": bool(t.get("datDuoc", True)),
            "nguoiKhai": (t.get("nguoi") or "trang-gia-lap").strip(), "ghiChu": "phản hồi từ trang giả lập",
        })
        s2, tk = goi_app("GET", "/internal/goi-y/thong-ke")
        self._tra(200, {"ok": s == 200, "http": s, "ketQua": b, "thongKe": tk if s2 == 200 else None})
