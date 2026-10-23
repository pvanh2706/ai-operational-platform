"""Kiểm chỗ LƯU phiên trải nghiệm (phien_trai_nghiem.py) — không cần app, không cần ui_server đang chạy:

    python scripts/giao-dien/kiem_phien_trai_nghiem.py

Rủi ro được kiểm: hai ngữ cảnh (app/tenant) không thấy phiên của nhau · lượt mới không đè lượt cũ, kể cả cùng câu hỏi ·
cập nhật đánh giá thay ĐÚNG lượt đó · khoá mang tên bí mật bị từ chối mà không ghi gì · tên ngữ cảnh/id không thoát được
ra ngoài thư mục · file phiên nằm trong vùng .gitignore chặn.

Ghi vào một thư mục con tạm BÊN TRONG phien-trai-nghiem/ (đã bị git bỏ qua) rồi xoá — không đụng phiên thật.
"""
from __future__ import annotations

import os
import shutil
import sys
import uuid

sys.stdout.reconfigure(encoding="utf-8")  # Windows mặc định cp1252 — in tiếng Việt chết sau khi việc đã làm
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import phien_trai_nghiem as P  # noqa: E402

GOC = P.THU_MUC
P.THU_MUC = os.path.join(GOC, f"_kiem-{uuid.uuid4().hex}")

chay = sai = 0


def kiem(nhan: str, that, mong) -> None:
    global chay, sai
    chay += 1
    ok = that == mong
    sai += 0 if ok else 1
    print(f"  {'ok ' if ok else 'SAI'}  {nhan}" + ("" if ok else f": nhận {that!r}, mong {mong!r}"))


def nem(ham, loai) -> str | None:
    try:
        ham()
    except loai as ex:
        return type(ex).__name__
    return None


def luot(id_: str, cau: str = "Khách báo lỗi gì?", **them) -> dict:
    return {"id": id_, "loai": "hoi-dap", "cauHoi": cau, "ketCuc": "tra-loi", "danhGia": {"doDung": "chua"}, **them}


try:
    A, B = "cong-5120.aaaa-1111", "cong-5119.bbbb-2222"

    print("\n── Tách theo ngữ cảnh")
    pa = P.tao(A, {"ten": "phiên A", "thongTinNguCanh": {"cong": 5120, "tenantId": "aaaa-1111"}})
    pb = P.tao(B, {"ten": "phiên B"})
    kiem("A chỉ thấy phiên của A", [x["id"] for x in P.liet_ke(A)], [pa["id"]])
    kiem("B chỉ thấy phiên của B", [x["id"] for x in P.liet_ke(B)], [pb["id"]])
    kiem("đọc phiên của A bằng ngữ cảnh B → không thấy", P.doc(B, pa["id"]), None)
    kiem("ghi lượt vào phiên A qua ngữ cảnh B → LookupError", nem(lambda: P.ghi_luot(B, pa["id"], luot("luot-0001")), LookupError), "LookupError")
    kiem("ngữ cảnh lưu kèm khoá đã kiểm, không lấy khoá do thân gửi", pa["nguCanh"]["khoa"], A)
    kiem("ngữ cảnh chưa có phiên → danh sách rỗng, không lỗi", P.liet_ke("cong-9999.chua-co"), [])

    print("\n── Lượt không đè lượt")
    P.ghi_luot(A, pa["id"], luot("luot-0001"))
    P.ghi_luot(A, pa["id"], luot("luot-0002"))  # cùng câu hỏi, id khác
    p = P.doc(A, pa["id"])
    kiem("hai lượt cùng câu hỏi → hai lượt", [x["id"] for x in p["luot"]], ["luot-0001", "luot-0002"])
    P.ghi_luot(A, pa["id"], luot("luot-0001", danhGia={"doDung": "sai"}))
    p = P.doc(A, pa["id"])
    kiem("cập nhật đánh giá thay đúng lượt 0001, giữ thứ tự", [(x["id"], x["danhGia"]["doDung"]) for x in p["luot"]],
         [("luot-0001", "sai"), ("luot-0002", "chua")])
    kiem("liệt kê đếm đúng số lượt", P.liet_ke(A)[0]["soLuot"], 2)

    print("\n── Không ghi bí mật")
    for khoa in ("apiKey", "pat", "matKhau", "x-api-key", "Authorization", "secret"):
        kiem(f"lượt mang `{khoa}` → từ chối", nem(lambda: P.ghi_luot(A, pa["id"], luot("luot-0003", loi={khoa: "abc"})), ValueError), "ValueError")
    kiem("… và không lượt nào được ghi thêm", len(P.doc(A, pa["id"])["luot"]), 2)
    kiem("trường `khoa` / `khoaNguon` (khoá NGUỒN, không phải bí mật) vẫn ghi được",
         nem(lambda: P.ghi_luot(A, pa["id"], luot("luot-0004", nguon={"khoa": "gialap:x", "khoaNguon": "gialap:x"})), ValueError), None)

    print("\n── Không thoát ra ngoài thư mục")
    for nc in ("../ra-ngoai", "cong-5120/../../x", "CONG-HOA", "", "a" * 101):
        kiem(f"ngữ cảnh {nc[:20]!r} → từ chối", nem(lambda: P.liet_ke(nc), ValueError), "ValueError")
    for id_ in ("../../appsettings", "ngan", "a/b-cdefgh"):
        kiem(f"id {id_!r} → từ chối", nem(lambda: P.doc(A, id_), ValueError), "ValueError")
    kiem("id lượt không hợp lệ → từ chối", nem(lambda: P.ghi_luot(A, pa["id"], {"id": "../x"}), ValueError), "ValueError")

    print("\n── Xoá")
    kiem("xoá phiên A", P.xoa(A, pa["id"]), True)
    kiem("xoá lần hai → không có gì để xoá", P.xoa(A, pa["id"]), False)
    kiem("B không bị ảnh hưởng", [x["id"] for x in P.liet_ke(B)], [pb["id"]])

    print("\n── .gitignore")
    import subprocess
    kiem("thư mục phiên bị git bỏ qua",
         subprocess.run(["git", "check-ignore", "-q", os.path.join(GOC, "x", "y.json")], cwd=P.GOC_REPO).returncode, 0)
finally:
    shutil.rmtree(P.THU_MUC, ignore_errors=True)

print(f"\n{'✗ %d/%d phép kiểm SAI' % (sai, chay) if sai else '✓ %d/%d phép kiểm đúng' % (chay, chay)}")
sys.exit(1 if sai else 0)
