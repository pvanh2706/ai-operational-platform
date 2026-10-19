"""Ticket BỊA cho trang giả lập hai luồng — `O6-1` (docs/18 nhóm O).

Mỗi ticket mang `nhomDung` — đáp án do người viết file này đặt — để trang giả lập CHẤM được AI định
tuyến đúng bao nhiêu. Đó là thứ dữ liệu thật không cho: corpus Jira không có nhãn đúng nào cả.

🛑 Ba luật khi sửa file này:
  1. GIÁ TRỊ BỊA HOÀN TOÀN. Không chép tên khách sạn, MST, mã đặt phòng, nội dung ticket thật — kể cả
     "chỉ một câu". `scripts/quet_credential_repo.py` đối chiếu repo với corpus trước mỗi commit.
  2. KHÔNG có hình dạng credential (mật khẩu, ID+pass điều khiển từ xa, token). Cổng che sẽ che chúng và
     lượt thử sẽ đo cổng che thay vì đo AI.
  3. `nhomDung` phải CHÉP NGUYÊN tên trong `docs/ket-qua-phan-tich/taxonomy-19-nhom-hoa-don.json` — host
     chỉ đọc taxonomy đó. `kiem_nhom_dung()` bên dưới kiểm điều này lúc chạy.

Mẩu ĐẦU TIÊN của mỗi ticket là thứ có mặt LÚC TICKET ĐẾN — định tuyến chỉ đọc tiêu đề + mẩu này
(`docs/13` §1). Các mẩu sau là chẩn đoán + cách sửa: luồng ngược đọc chúng để soạn SOP.
"""
from __future__ import annotations

import json
import os
from datetime import datetime, timedelta, timezone

KHONG_BIET = "KHONG-BIET"

NHOM_KY_HIEU = "Phân quyền & ký hiệu hoá đơn chưa sẵn sàng"
NHOM_NCC = "NCC từ chối payload — tra mã lỗi, sửa dữ liệu nguồn, phát hành lại"
NHOM_NGAY = "Ngày làm việc/kiểm toán quyết định ngày trên hoá đơn"
NHOM_DOAN = "Hoá đơn đoàn lớn/nhiều giao dịch — timeout, treo Đang xử lý"

# (tiêu đề, nhóm đúng, [mẩu lúc đến, chẩn đoán, cách sửa ...])
MAU: list[tuple[str, str, list[str]]] = [
    ("Không chọn được ký hiệu khi phát hành hoá đơn", NHOM_KY_HIEU, [
        "Lễ tân bấm Phát hành hoá đơn nhưng ô Ký hiệu trống trơn, danh sách thả xuống không có giá trị nào để chọn.",
        "Kiểm trên ezInvoice.Admin: ký hiệu 1C26TGL đã tạo nhưng chưa gán cho tài khoản lễ tân.",
        "Gán ký hiệu cho nhóm quyền Lễ tân, cho đăng nhập lại -> ô Ký hiệu có giá trị, phát hành được.",
    ]),
    ("Báo đỏ Validation form error khi xuất hoá đơn", NHOM_KY_HIEU, [
        "Màn xuất hoá đơn báo đỏ 'Validation form error', không có mã lỗi nào khác. Mới đổi sang năm 2026.",
        "Ký hiệu năm cũ đã hết hiệu lực, ký hiệu năm mới chưa được kích hoạt trên ezInvoice.Admin.",
        "Kích hoạt ký hiệu năm mới, tải lại trang -> hết báo đỏ.",
    ]),
    ("Tài khoản thu ngân không thấy nút phát hành", NHOM_KY_HIEU, [
        "Thu ngân ca đêm không thấy nút Phát hành hoá đơn trên folio, tài khoản quản lý thì vẫn thấy.",
        "Nhóm quyền Thu ngân thiếu quyền 'Phát hành hoá đơn điện tử'.",
        "Bổ sung quyền cho nhóm Thu ngân, đăng xuất đăng nhập lại -> thấy nút.",
    ]),
    ("Hoá đơn bị nhà cung cấp trả về lỗi E0042", NHOM_NCC, [
        "Phát hành hoá đơn thì nhận thông báo từ nhà cung cấp: 'Mã lỗi E0042 - Mã số thuế người mua không hợp lệ'. Hoá đơn nằm ở nháp.",
        "MST người mua nhập thừa một dấu cách ở cuối (0100000099 ). NCC kiểm định dạng nên từ chối.",
        "Sửa MST trên hồ sơ khách công ty trong PMS, phát hành lại -> NCC nhận.",
    ]),
    ("Không phát hành được, NCC báo sai định dạng CCCD", NHOM_NCC, [
        "NCC trả về 'Số CCCD người mua không đúng định dạng', hoá đơn dừng ở trạng thái nháp.",
        "Trường CCCD đang lưu số hộ chiếu của khách nước ngoài (có chữ cái).",
        "Chuyển số hộ chiếu sang đúng trường Hộ chiếu, để trống CCCD, phát hành lại -> thành công.",
    ]),
    ("Lỗi tên đơn vị mua quá dài khi phát hành", NHOM_NCC, [
        "Thông báo từ nhà cung cấp: 'Tên người mua vượt quá độ dài cho phép'. Không phát hành được hoá đơn công ty.",
        "Tên công ty trong PMS nhập kèm cả địa chỉ, dài hơn giới hạn của NCC.",
        "Tách địa chỉ sang trường Địa chỉ, rút gọn tên, phát hành lại -> NCC chấp nhận.",
    ]),
    ("Ngày trên hoá đơn bị lùi một ngày", NHOM_NGAY, [
        "Khách trả phòng sáng 15 nhưng hoá đơn in ngày 14. Kế toán hỏi vì sao lệch ngày.",
        "Khách sạn chưa chạy kiểm toán đêm (Night Audit) ngày 14, nên ngày làm việc trên hệ thống vẫn là 14.",
        "Chạy kiểm toán đêm cho ngày 14, các hoá đơn sau đó lấy đúng ngày 15. Hướng dẫn chạy kiểm toán đúng giờ.",
    ]),
    ("Hoá đơn nhà hàng mang ngày hôm trước", NHOM_NGAY, [
        "Hoá đơn của outlet nhà hàng xuất lúc 0h30 mang ngày hôm trước, trong khi lễ tân thì đúng ngày.",
        "Outlet nhà hàng có ngày làm việc riêng, chưa đóng ca/chuyển ngày cho khu kinh doanh đó.",
        "Đóng ca và chuyển ngày cho outlet nhà hàng -> hoá đơn mới đúng ngày.",
    ]),
    ("Hoá đơn đoàn 120 phòng treo Đang xử lý", NHOM_DOAN, [
        "Xuất hoá đơn gộp cho đoàn 120 phòng thì màn hình quay mãi ở 'Đang xử lý', sau vài phút báo hết thời gian.",
        "Folio đoàn có hơn 900 giao dịch, request tạo hoá đơn vượt thời gian chờ của luồng phát hành.",
        "Tách hoá đơn theo từng ngày lưu trú (mỗi hoá đơn dưới 200 dòng) rồi phát hành từng phần -> thành công.",
    ]),
    ("Timeout khi phát hành hoá đơn hội nghị nhiều dịch vụ", NHOM_DOAN, [
        "Phát hành hoá đơn cho booking hội nghị (phòng + hội trường + ăn uống 3 ngày) bị timeout, hoá đơn không lên.",
        "Booking có rất nhiều dòng dịch vụ nhỏ, khối lượng vượt sức chịu của một lần phát hành.",
        "Gộp dịch vụ cùng loại thành một dòng trước khi phát hành, hoặc tách thành nhiều hoá đơn.",
    ]),
    ("Khách báo hoá đơn có vấn đề", KHONG_BIET, [
        "Khách gọi lên nói hoá đơn có vấn đề, nhờ kiểm tra giúp. Chưa rõ là hoá đơn nào.",
        "Đã gọi lại khách nhưng không liên lạc được.",
    ]),
]

# Mã khách sạn BỊA. `AR-l` cần ít nhất HAI giá trị phân biệt mới coi là có ranh giới.
MA_KHACH_SAN = ["GL-KS-01", "GL-KS-02", "GL-KS-03"]


def tao_lo(so_luong: int | None = None, bay_gio: datetime | None = None) -> dict:
    """Một lô ticket mới, mã duy nhất theo thời điểm. `so_luong` cắt bớt từ đầu danh sách."""
    bay_gio = bay_gio or datetime.now(timezone.utc)
    lo = bay_gio.strftime("%Y%m%d-%H%M%S")
    # ⚠ Mã TRẦN (phần sau dấu `:` cuối) phải duy nhất giữa các lô: model và người hay viết mã trần, và
    #   `TraCaseTheoMa` khớp đuôi `:mã` — hai lô cùng có `GL-001` thì mã đó MƠ HỒ và dẫn chứng không nối.
    tien_to = f"gialap:{lo}-"

    mau = MAU if not so_luong else MAU[:max(1, min(so_luong, len(MAU)))]
    cases, evidence, dap_an = [], [], {}

    for i, (tieu_de, nhom, cac_mau) in enumerate(mau, start=1):
        khoa = f"{tien_to}{i:03d}"
        luc_den = bay_gio - timedelta(days=len(mau) - i, hours=2)
        cases.append({
            "sourceReference": khoa,
            "subject": tieu_de,
            "sourceCreatedAt": luc_den.isoformat(),
            "sourceResolvedAt": (luc_den + timedelta(hours=3)).isoformat(),
        })
        for j, noi_dung in enumerate(cac_mau, start=1):
            evidence.append({
                "caseSourceReference": khoa,
                "sourceReference": f"{khoa}#m{j}",
                "content": noi_dung,
                "observedAt": (luc_den + timedelta(minutes=40 * (j - 1))).isoformat(),
                "machineReadability": "Medium",
                "subTenantCode": MA_KHACH_SAN[i % len(MA_KHACH_SAN)],
            })
        dap_an[khoa] = {"tieuDe": tieu_de, "nhomDung": nhom}

    return {"lo": lo, "tienTo": tien_to, "cases": cases, "evidence": evidence, "dapAn": dap_an}


def kiem_nhom_dung(duong_taxonomy: str) -> list[str]:
    """Tên `nhomDung` nào KHÔNG có trong taxonomy — rỗng là khớp. Gọi lúc chạy, không tin trí nhớ."""
    with open(duong_taxonomy, encoding="utf-8") as f:
        ten = {n["ten"] for n in json.load(f)["nhom"]}
    return sorted({nhom for _, nhom, _ in MAU if nhom != KHONG_BIET and nhom not in ten})


if __name__ == "__main__":
    goc = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    sai = kiem_nhom_dung(os.path.join(goc, "docs", "ket-qua-phan-tich", "taxonomy-19-nhom-hoa-don.json"))
    print("khop taxonomy" if not sai else f"SAI: {sai}")
