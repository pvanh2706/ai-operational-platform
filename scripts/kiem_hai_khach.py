#!/usr/bin/env python3
"""PHÉP KIỂM NGHIỆM THU CỦA MỨC 3 — onboard được khách thứ HAI mà không sửa code.

Chủ dự án chốt `I2-1` (docs/18): mức 3 gọi là XONG khi **onboard được khách thứ hai mà
không sửa code**. Đó là một câu đo được, và file này là cái thước.

Vì sao cần một script riêng, ngoài `dotnet test` và `kiem_dau_cuoi.py`:

  dotnet test          bộ test TỰ dựng tenant bằng C#, trong cùng tiến trình. Nó chứng minh
                       LOGIC đúng, nhưng không chứng minh được rằng một NGƯỜI dựng được
                       khách hàng mới bằng những công cụ đang có.
  kiem_dau_cuoi.py     gọi thật, nhưng chỉ MỘT khách. Ranh giới giữa hai khách thì nó câm.
  file này             dựng HAI khách từ con số 0, CHỈ bằng lệnh CLI + HTTP công khai —
                       không một dòng code nào, không một câu SQL tay nào.

⚠ Nếu file này phải sửa code để chạy được thì mức 3 CHƯA xong, theo đúng định nghĩa.
  Chính lần chạy đầu đã bắt được một thứ như vậy: tạo khách hàng mới trước đó chỉ làm được
  bằng SQL tay. Lệnh `--tao-khach-hang` ra đời từ đó.

CHẠY:

    # 1. App phải chạy ở chế độ SHARED (nhiều khách trên một tiến trình).
    #    Chế độ này TỪ CHỐI cờ Auth:AcknowledgeUnauthenticatedApi -> khoá API là bắt buộc,
    #    và đó chính là thứ cần kiểm. ⚠ appsettings.Development.json BẬT cờ đó cho máy dev
    #    nên phải tắt tường minh, nếu không app TỪ CHỐI KHỞI ĐỘNG (đúng thiết kế, `IM-39`).
    ASPNETCORE_ENVIRONMENT=Development \\
    Tenancy__Mode=SharedMultiTenant \\
    Auth__AcknowledgeUnauthenticatedApi=false \\
    Link__PublicBaseUrl=http://localhost:5119 \\
    Webhook__BatVongQuet=false \\
    dotnet run --project src/KnowledgePlatform.Api --no-launch-profile

    # 2. Rồi chạy file này
    python scripts/kiem_hai_khach.py

Mã thoát: 0 = hai khách chạy cạnh nhau, không lẫn.  1 = có chỗ sai.  2 = không gọi được app.
"""

from __future__ import annotations

import io
import json
import os
import subprocess
import sys
import urllib.error
import urllib.request

# Quy ước của repo: SCRIPT TỰ ÉP UTF-8. Thiếu khối này thì một dòng kết quả có dấu làm script
# chết bằng UnicodeEncodeError trên Windows — ở chiều GHI, tức sau khi đã kiểm xong, nên
# thông điệp lỗi trỏ hoàn toàn sai chỗ.
for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

GOC = os.environ.get("APP_BASE_URL", "http://localhost:5119").rstrip("/")
DU_AN = os.environ.get("KP_API_PROJECT", "src/KnowledgePlatform.Api")

# Mã ticket có THẬT trong bảng định tuyến mặc định, và định tuyến ra một nhóm có thật.
# ⚠ Cố ý để HAI khách hỏi CÙNG một mã: nếu ranh giới hở thì khách A sẽ thấy SOP của khách B
#   cho đúng câu hỏi đó — đây là ca xấu nhất, không phải ca dễ.
TICKET_CHUNG = os.environ.get("KP_TICKET_CHUNG", "ES-343712")

_sai: list[str] = []
_da_chay = 0


def kiem(nhan: str, that, mong) -> bool:
    global _da_chay
    _da_chay += 1
    if that == mong:
        sys.stdout.write("  ok   %s\n" % nhan)
        return True
    _sai.append(nhan)
    sys.stdout.write("  SAI  %s: nhận %r, mong %r\n" % (nhan, that, mong))
    return False


def muc(ten: str) -> None:
    sys.stdout.write("\n── %s %s\n" % (ten, "─" * max(0, 60 - len(ten))))


def goi(method: str, duong: str, body=None, khoa: str | None = None) -> tuple[int, object]:
    data = json.dumps(body, ensure_ascii=False).encode("utf-8") if body is not None else None
    req = urllib.request.Request(GOC + duong, data=data, method=method)
    if data:
        req.add_header("Content-Type", "application/json")
    if khoa:
        req.add_header("Authorization", "Bearer " + khoa)
    try:
        with urllib.request.urlopen(req, timeout=30) as r:
            raw = r.read().decode("utf-8")
            return r.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8")
        try:
            return e.code, json.loads(raw)
        except ValueError:
            return e.code, raw
    except urllib.error.URLError as e:
        sys.stdout.write("\n🛑 KHÔNG GỌI ĐƯỢC %s — %s\n" % (GOC, e.reason))
        sys.stdout.write("   App chưa chạy, hoặc chưa chạy ở chế độ SharedMultiTenant.\n")
        sys.stdout.write("   Xem phần CHẠY ở đầu file này.\n")
        sys.exit(2)


def lenh(*args: str) -> str:
    """Chạy một lệnh vận hành của sản phẩm. Đây là công cụ mà NGƯỜI thật sẽ dùng."""
    r = subprocess.run(
        ["dotnet", "run", "--project", DU_AN, "--no-build", "--"] + list(args),
        capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        sys.stdout.write("\n🛑 LỆNH HỎNG: %s\n%s\n%s\n" % (" ".join(args), r.stdout, r.stderr))
        sys.exit(2)
    return r.stdout or ""


def nhap_sop(nhom: str) -> dict:
    """Bản nháp tối thiểu — đủ để sinh MỘT record duyệt được."""
    return {
        "nhom": nhom,
        "soCaseDungSau": 2,
        "trieuChungVao": [{"moTa": "Không phát hành được hoá đơn", "case": ["ES-9001"]}],
        "buocKiem": [{
            "ma": "K1",
            "cauHoi": "Ô ký hiệu có giá trị không?",
            "noiXem": "Màn phát hành",
            "nhanh": [{"quanSat": "Ô ký hiệu trống nhưng chọn được", "diToi": "S1",
                       "case": ["ES-9001"], "chungCu": "evidence-noi-ro"}],
        }],
        "buocSua": [{"ma": "S1", "viec": "Chọn ký hiệu rồi phát hành lại", "case": ["ES-9001"]}],
        # ⚠ Tên năm ô là dạng KEBAB, không phải camelCase — xem SopDistribution.
        "phanBoBuocKiemDuocGhiLai": {
            "ghi-ro-buoc-kiem": {"so": 2, "case": ["ES-9001", "ES-9002"]},
            "suy-ra-duoc-nhung-khong-ghi": {"so": 0, "case": []},
            "chi-ghi-ket-luan": {"so": 0, "case": []},
            "buoc-kiem-ngoai-ticket": {"so": 0, "case": []},
            "khong-phai-chan-doan": {"so": 0, "case": []},
        },
    }


class Khach:
    def __init__(self, ten: str, hau_to: str) -> None:
        self.ten = ten
        self.external_key = "nghiem-thu-%s-%s" % (ten, hau_to)
        self.nhom = "nhom-%s-%s" % (ten, hau_to)
        self.dich = "http://127.0.0.1:5199/khong-ai-nghe-%s" % ten
        self.khoa = ""
        self.tenant_id = ""
        self.record_id = ""
        self.quy_trinh_id = ""


def dung_khach(k: Khach) -> None:
    """Onboard một khách từ con số 0 — CHỈ bằng lệnh CLI và HTTP công khai."""
    lenh("--tao-khach-hang", k.external_key, "Khách %s (nghiệm thu)" % k.ten)

    ra = lenh("--tao-khoa", k.external_key, "connector nghiệm thu")
    for tu in ra.replace("\n", " ").split():
        if tu.startswith("kp_"):
            k.khoa = tu
            break

    kiem("[%s] tạo được khoá API bằng lệnh" % k.ten, bool(k.khoa), True)


def nap_tri_thuc(k: Khach) -> None:
    s, b = goi("PUT", "/internal/webhook", {"url": k.dich}, khoa=k.khoa)
    kiem("[%s] khai báo đích gửi kết quả -> 200" % k.ten, s, 200)

    s, b = goi("POST", "/knowledge/sop-draft", nhap_sop(k.nhom), khoa=k.khoa)
    if not kiem("[%s] nạp bản nháp -> 200" % k.ten, s, 200):
        return

    k.record_id = b["nguyenNhan"][0]["id"]

    # P4: bản nháp sinh MỘT quy trình DRAFT. Lấy id NGAY ở đây — bản đầu của đoạn này lấy nó SAU lượt duyệt tri
    # thức, lúc `b` đã bị gán lại, nên id rỗng: POST /processes//approve ra 404 và GET /processes/ khớp nhầm route
    # liệt kê ra 200. Không phải lỗi sản phẩm — nhưng đúng hình dạng "phép kiểm hỏng mà trông như sản phẩm hỏng".
    k.quy_trinh_id = ((b.get("quyTrinh") or {}).get("id") or "")
    kiem("[%s] bản nháp sinh một quy trình DRAFT" % k.ten, bool(k.quy_trinh_id), True)

    # 🛑 Ở chế độ SHARED, duyệt bị TỪ CHỐI bằng 409 — cổng `AR-l` (`IM-31`), CỐ Ý.
    #
    # Duyệt LÀ xuất bản, và đường rò của `AR-l` xảy ra ở khâu xuất bản chứ không ở khâu truy
    # vấn hàng, nên RLS không cứu được. Cổng này phải ở nguyên cho tới khi `AR-l` được GIẢI
    # (cần `G1-1`: thêm field mã khách sạn vào Jira — việc của tổ chức, không phải của code).
    #
    # Phép kiểm vì thế khẳng định 409, và khẳng định luôn LÝ DO: một 409 vì lý do khác sẽ
    # trượt qua nếu chỉ so mã số.
    s, b = goi("POST", "/knowledge/%s/approve" % k.record_id,
               {"nguoiDuyet": "nghiem-thu", "phamViXem": "noi-bo",
                "xacNhanDuLieuThuongMai": True}, khoa=k.khoa)
    kiem("[%s] duyệt ở chế độ shared -> 409 (cổng AR-l)" % k.ten, s, 409)
    kiem("[%s]   ... và nói rõ AR-l là lý do" % k.ten, "AR-l" in json.dumps(b, ensure_ascii=False), True)

    # P4 (2026-10-10): bản nháp vừa nạp sinh một quy trình DRAFT. Duyệt nó cũng là XUẤT BẢN, nên
    # cổng AR-l chặn y như duyệt tri thức — và phải nói rõ AR-l là lý do.
    s, b = goi("POST", "/processes/%s/approve" % k.quy_trinh_id,
               {"nguoiDuyet": "nghiem-thu", "phamViXem": "noi-bo",
                "xacNhanDuLieuThuongMai": True}, khoa=k.khoa)
    kiem("[%s] duyệt QUY TRÌNH ở chế độ shared -> 409 (cổng AR-l)" % k.ten, s, 409)
    kiem("[%s]   ... và nói rõ AR-l là lý do" % k.ten, "AR-l" in json.dumps(b, ensure_ascii=False), True)

    s, b = goi("GET", "/internal/tenant-boundary", khoa=k.khoa)
    if kiem("[%s] đọc được ranh giới của chính mình -> 200" % k.ten, s, 200):
        k.tenant_id = (b or {}).get("tenantId", "")


def cach_ly(a: Khach, b: Khach) -> None:
    muc("C · Ranh giới giữa hai khách")

    kiem("hai khách là hai tenant khác nhau", a.tenant_id != b.tenant_id and bool(a.tenant_id), True)

    s, _ = goi("GET", "/knowledge/%s" % b.record_id, khoa=a.khoa)
    kiem("khoá A KHÔNG đọc được tri thức của B -> 404", s, 404)

    s, _ = goi("GET", "/processes/%s" % b.quy_trinh_id, khoa=a.khoa)
    kiem("khoá A KHÔNG đọc được QUY TRÌNH của B -> 404", s, 404)

    s, body = goi("GET", "/internal/webhook", khoa=a.khoa)
    kiem("khoá A đọc cấu hình đích -> thấy của CHÍNH MÌNH", (body or {}).get("url"), a.dich)

    s, _ = goi("POST", "/knowledge/%s/link" % b.record_id,
               {"nguoiPhat": "khach-a"}, khoa=a.khoa)
    kiem("khoá A KHÔNG phát được link cho tri thức của B -> 404", s, 404)

    # ⚠ Ở chế độ shared, cổng `AR-l` chặn TRƯỚC khi tới được phép kiểm quyền sở hữu, nên câu
    #   trả lời là 409 chứ không phải 404. Ghi đúng như nó là: khẳng định 404 ở đây sẽ thành
    #   một phép kiểm đỏ mãi mãi vì lý do không ai sửa được bằng code.
    s, _ = goi("POST", "/knowledge/%s/approve" % b.record_id,
               {"nguoiDuyet": "khach-a", "phamViXem": "noi-bo",
                "xacNhanDuLieuThuongMai": True}, khoa=a.khoa)
    kiem("khoá A duyệt tri thức của B -> bị chặn (409 cổng AR-l)", s, 409)

    s, _ = goi("GET", "/internal/tenant-boundary", khoa=a.khoa + "sai")
    kiem("khoá bị sửa một ký tự -> 401", s, 401)


def cung_mot_cau_hoi(a: Khach, b: Khach) -> None:
    """
    🛑 Ca XẤU NHẤT, không phải ca dễ: hai khách hỏi CÙNG một mã ticket.

    Nếu ranh giới hở ở khâu trả lời thì khách A sẽ nhận được SOP của khách B cho đúng câu
    hỏi đó — và cả hai bên đều thấy "hệ thống trả lời được", không ai báo lỗi gì.
    """
    muc("D · Hai khách hỏi CÙNG một câu")

    cau = {"maTicket": TICKET_CHUNG, "tieuDe": "Không phát hành được hoá đơn", "moTa": None}

    s, ra_a = goi("POST", "/hoi", cau, khoa=a.khoa)
    kiem("[a] hỏi -> 200", s, 200)
    s, ra_b = goi("POST", "/hoi", cau, khoa=b.khoa)
    kiem("[b] hỏi -> 200", s, 200)

    ten_a = [h.get("tenNguyenNhan", "") for h in (ra_a or {}).get("huongDan") or []]
    ten_b = [h.get("tenNguyenNhan", "") for h in (ra_b or {}).get("huongDan") or []]

    # Hai khách nạp hai NHÓM khác nhau, nên cùng một mã ticket vẫn chỉ ra tri thức của chính
    # họ — hoặc KHÔNG BIẾT. Điều KHÔNG được phép xảy ra là A thấy tri thức của B.
    kiem("khách A không thấy tri thức mang nhóm của B",
         any(b.nhom in t for t in ten_a), False)
    kiem("khách B không thấy tri thức mang nhóm của A",
         any(a.nhom in t for t in ten_b), False)


def main() -> int:
    hau_to = os.urandom(4).hex()
    a, b = Khach("a", hau_to), Khach("b", hau_to)

    sys.stdout.write("=" * 66 + "\n")
    sys.stdout.write("NGHIỆM THU MỨC 3  ·  %s  ·  lượt %s\n" % (GOC, hau_to))
    sys.stdout.write("I2-1: onboard được khách thứ HAI mà không sửa code\n")
    sys.stdout.write("=" * 66 + "\n")

    s, _ = goi("GET", "/health")
    if not kiem("app đang chạy", s, 200):
        return 1

    muc("A · Dựng hai khách hàng từ con số 0")
    dung_khach(a)

    # 🛑 HỎI THẲNG CHẾ ĐỘ, KHÔNG SUY TỪ MÃ 401.
    #
    # Bản đầu của file này kiểm chế độ shared bằng cách gọi không khoá rồi mong 401 — và nó
    # XANH VÌ LÝ DO SAI: một bản DEDICATED có đòi khoá cũng trả 401 y hệt. Lần chạy đầu vì
    # thế đã đo nhầm một bản dedicated còn sót trên cổng, rồi báo ba chỗ sai vô nghĩa.
    # Một phép kiểm xanh vì lý do sai nguy hiểm hơn một phép kiểm không có.
    s, mode_body = goi("GET", "/internal/tenant-boundary", khoa=a.khoa)
    if not kiem("app đang chạy chế độ SharedMultiTenant",
                (mode_body or {}).get("mode"), "SharedMultiTenant"):
        print("     -> Phep kiem nay chi co nghia o che do shared. Xem dau file.")
        return 1

    dung_khach(b)

    muc("B · Mỗi khách tự nạp tri thức của mình")
    nap_tri_thuc(a)
    nap_tri_thuc(b)

    cach_ly(a, b)
    cung_mot_cau_hoi(a, b)

    sys.stdout.write("\n" + "=" * 66 + "\n")
    sys.stdout.write("%d phép kiểm  ·  %d sai\n" % (_da_chay, len(_sai)))

    if _sai:
        sys.stdout.write("\n🛑 CHỖ SAI:\n")
        for x in _sai:
            sys.stdout.write("   · %s\n" % x)
        sys.stdout.write("\n⚠ Mức 3 CHƯA đạt theo định nghĩa I2-1.\n")
        return 1
    print("")
    print("✅ Hai khách hàng dựng được từ con số 0, chạy cạnh nhau, không lẫn.")
    print("   Không sửa một dòng code nào, không chạy một câu SQL tay nào.")
    print("")
    print("🛑 NHƯNG ĐỌC TIẾP — mức 3 CHƯA dùng thật được, và chỗ chặn KHÔNG phải code:")
    print("")
    print("   Ở chế độ shared, DUYỆT bị từ chối bằng 409 — cổng AR-l (IM-31), cố ý.")
    print("   Duyệt LÀ xuất bản, và đường rò của AR-l xảy ra ở khâu xuất bản.")
    print("   Nghĩa là hôm nay hai khách onboard được, hỏi được, nhưng kho của họ")
    print("   KHÔNG có tri thức nào duyệt được -> /hoi trả KHÔNG BIẾT cho mọi câu.")
    print("")
    print("   Mở khoá nó cần G1-1: thêm field mã khách sạn vào Jira. Việc của tổ chức.")
    print("")
    print("⚠ Và phép kiểm này KHÔNG đo chất lượng câu trả lời. I2-1 đo ONBOARD ĐƯỢC,")
    print("  không đo TRẢ LỜI CÓ ÍCH — thước đó là M1/M2 ở docs/02.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
