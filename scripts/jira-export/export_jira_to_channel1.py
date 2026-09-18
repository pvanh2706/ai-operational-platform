#!/usr/bin/env python3
"""Xuất issue + comment từ Jira Server/Data Center rồi đẩy vào Kênh 1.

Đây là bước (b) của Path A (docs/00_CURRENT_STATE.md §"Việc tiếp theo"): trước khi
build FTS, kho phải có case THẬT kèm nội dung thật — "FTS tune trên corpus thật,
không phải case bịa". Script này là connector một chiều, chạy tay, chạy lại vô hại:

    1. POST /signals/case-observed   ← issue     (idempotent, gửi lại trả created=0)
    2. POST /signals/case-evidence   ← description + comment, trỏ về issue

Thứ tự đó là bắt buộc: endpoint evidence TỪ CHỐI CẢ LÔ nếu nhắc tới một case chưa
tồn tại (AR-f). Nhưng vì case-observed idempotent, script cứ gửi case trước mỗi lần
— không cần nhớ lần trước đã gửi gì (IM-15).

Cấu hình qua biến môi trường — KHÔNG có credential nào nằm trong file này:

    JIRA_BASE_URL     bắt buộc   vd: https://jira.congty.vn
    JIRA_JQL          bắt buộc   vd: project = OTA AND resolution IS NOT EMPTY
    JIRA_PAT          một trong  Personal Access Token (Jira 8.14+, header Bearer)
    JIRA_USER/_PASS   hai cách   Basic auth cho bản cũ hơn
    APP_BASE_URL      tuỳ chọn   mặc định http://localhost:5119 (launchSettings)
    KP_API_KEY        tuỳ chọn   khoá API của khách (`kp_<id>_<secret>`), gửi bằng
                                 Authorization: Bearer. Máy dev không cần khi
                                 Auth:AcknowledgeUnauthenticatedApi đang bật; deploy
                                 thật thì BẮT BUỘC. Tạo bằng:
                                     dotnet run --project src/KnowledgePlatform.Api --                                        --tao-khoa <mã-khách> "connector Jira"
                                 (APP_SIGNAL_KEY vẫn đọc được — tên cũ, sẽ bỏ.)
    MAX_ISSUES        tuỳ chọn   trần số issue kéo về, 0 = không trần. §8.2 nói
                                 n = 50-200 là đủ cho phép đếm.

Cách chạy (PowerShell):

    $env:JIRA_BASE_URL = "https://jira.congty.vn"
    $env:JIRA_PAT      = "..."
    $env:JIRA_JQL      = "project = OTA AND resolution IS NOT EMPTY ORDER BY created ASC"
    python scripts/jira-export/export_jira_to_channel1.py --dry-run   # xem trước, không gửi
    python scripts/jira-export/export_jira_to_channel1.py             # gửi thật

--dry-run ghi hai file JSON cạnh script (dry-run-cases.json, dry-run-evidence.json)
để soi bằng mắt TRƯỚC khi nạp — dữ liệu vào kho gom là dữ liệu model sẽ học, rác
vào là nháp SOP sai ngay từ nguồn.

Chỉ dùng thư viện chuẩn — không phải pip install gì.
"""

from __future__ import annotations

import argparse
import base64
import json
import os
import re
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from datetime import datetime

# Windows: khi output bi chuyen huong (pipe, ghi file, hoac chay tu trinh khac), Python
# dat stdout ve codepage ANSI cua he thong - cp1252 tren may nay - va moi chu tieng Viet
# lam no nem UnicodeEncodeError. Da vap that 2026-09-04: script chay dung khi in ra
# console, chet ngay khi `| head`. Ep UTF-8 o day, khong bat nguoi chay phai nho dat
# PYTHONIOENCODING - thu chi hong luc bi pipe la thu se hong dung luc dang go loi.
for _luong in (sys.stdout, sys.stderr):
    if (getattr(_luong, "encoding", "") or "").lower().replace("-", "") != "utf8":
        try:
            _luong.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

# Trần của server (IngestOptions): vượt là bị từ chối CẢ LÔ, nên chia lô ở đây.
MAX_PER_BATCH = 500
# Comment Jira có thể rất dài; Kestrel mặc định chặn body ~30MB. Chia lô thêm theo
# tổng kích thước để một lô 500 comment dài không thành một request bị chặn.
MAX_BATCH_BYTES = 20 * 1024 * 1024
# Trần subject của /signals/case-observed (Program.cs Validate): dài hơn là 400.
MAX_SUBJECT = 1024
JIRA_PAGE_SIZE = 100
TIMEOUT_SECONDS = 60


def die(msg: str) -> "NoReturn":  # noqa: F821 - chỉ để đọc, không import typing cho một dòng
    print(f"LỖI: {msg}", file=sys.stderr)
    sys.exit(1)


@dataclass
class Config:
    jira_base: str
    jira_auth_header: str
    jql: str
    app_base: str
    signal_key: str | None
    max_issues: int


def read_config() -> Config:
    jira_base = os.environ.get("JIRA_BASE_URL", "").rstrip("/")
    if not jira_base:
        die("thiếu JIRA_BASE_URL.")

    jql = os.environ.get("JIRA_JQL", "").strip()
    if not jql:
        die("thiếu JIRA_JQL — script cố ý không có JQL mặc định, để 'lấy những case nào' "
            "là một quyết định nhìn thấy được chứ không phải mặc định chôn trong code.")

    pat = os.environ.get("JIRA_PAT", "").strip()
    user = os.environ.get("JIRA_USER", "").strip()
    password = os.environ.get("JIRA_PASS", "") or os.environ.get("JIRA_PASSWORD", "")
    if pat:
        auth = f"Bearer {pat}"
    elif user and password:
        raw = base64.b64encode(f"{user}:{password}".encode()).decode()
        auth = f"Basic {raw}"
    else:
        die("thiếu xác thực: đặt JIRA_PAT (Jira 8.14+), hoặc JIRA_USER + JIRA_PASS.")

    return Config(
        jira_base=jira_base,
        jira_auth_header=auth,
        jql=jql,
        app_base=os.environ.get("APP_BASE_URL", "http://localhost:5119").rstrip("/"),
        # `AR-e` 2026-09-16: khoá API theo TỪNG khách, gửi bằng Authorization: Bearer.
        # Giữ APP_SIGNAL_KEY làm đường lùi vì hai máy đang đặt sẵn biến đó — nhưng tên
        # mới là KP_API_KEY, giống hai script còn lại. Một dự án có hai tên cho cùng một
        # thứ là một dự án sẽ có người đặt nhầm cái còn lại.
        signal_key=os.environ.get("KP_API_KEY") or os.environ.get("APP_SIGNAL_KEY") or None,
        max_issues=int(os.environ.get("MAX_ISSUES", "0")),
    )


class LoiThoangQua(Exception):
    """Lỗi có thể hết sau khi thử lại: timeout, mất kết nối, 429, 5xx."""


def http_json(url: str, headers: dict[str, str], body: object | None = None,
              so_lan_thu: int = 3) -> dict:
    """Gọi HTTP, THỬ LẠI với lỗi thoáng qua.

    ⚠ Sinh ra từ một lần vấp thật 2026-09-04. Kéo 150 issue là ~380 request liên tiếp;
    ở request thứ ~380 Jira không trả lời (`WinError 10060`) và bản trước của hàm này
    gọi `die()` ngay — **mất trắng 6 phút gọi mạng và 328 mẩu evidence đã đọc xong**.
    Một script gọi hàng trăm request qua mạng phải chịu được MỘT request lỗi; không thì
    xác suất chạy trọn một lô giảm theo số request, và nó giảm nhanh.

    Lỗi 4xx (trừ 429) thì KHÔNG thử lại — sai JQL hay sai khoá thì thử mười lần cũng
    vẫn sai, và thử lại chỉ làm thông báo lỗi đến muộn hơn.
    """
    data = None
    if body is not None:
        data = json.dumps(body, ensure_ascii=False).encode("utf-8")
        headers = {**headers, "Content-Type": "application/json; charset=utf-8"}
    req = urllib.request.Request(url, data=data, headers=headers,
                                 method="POST" if body is not None else "GET")

    for lan in range(1, so_lan_thu + 1):
        try:
            with urllib.request.urlopen(req, timeout=TIMEOUT_SECONDS) as resp:
                return json.loads(resp.read().decode("utf-8"))
        except urllib.error.HTTPError as ex:
            # Body lỗi của cả Jira lẫn app đều nói rõ sai ở đâu (app trả ValidationProblem
            # chỉ đích danh phần tử lỗi) — nuốt nó đi là bắt người chạy đoán mò.
            detail = ex.read().decode("utf-8", errors="replace")
            if ex.code == 429 or 500 <= ex.code < 600:
                if lan < so_lan_thu:
                    cho = 2 ** lan
                    print(f"    HTTP {ex.code}, thử lại sau {cho}s ({lan}/{so_lan_thu - 1})")
                    time.sleep(cho)
                    continue
                raise LoiThoangQua(f"HTTP {ex.code} sau {so_lan_thu} lần thử: {url}")
            die(f"HTTP {ex.code} từ {url}" + chr(10) + detail)
        except (urllib.error.URLError, TimeoutError, OSError) as ex:
            ly_do = getattr(ex, "reason", ex)
            if lan < so_lan_thu:
                cho = 2 ** lan
                print(f"    mạng lỗi ({ly_do}), thử lại sau {cho}s ({lan}/{so_lan_thu - 1})")
                time.sleep(cho)
                continue
            raise LoiThoangQua(f"không gọi được sau {so_lan_thu} lần thử: {url}: {ly_do}")

    raise LoiThoangQua(f"không gọi được: {url}")


# ---------------------------------------------------------------- đọc từ Jira


def fetch_issues(cfg: Config) -> list[dict]:
    """Kéo issue theo JQL, phân trang. Chỉ lấy field cần cho tín hiệu case."""
    issues: list[dict] = []
    start = 0
    while True:
        params = urllib.parse.urlencode({
            "jql": cfg.jql,
            "startAt": start,
            "maxResults": JIRA_PAGE_SIZE,
            "fields": "summary,created,resolutiondate",
        })
        page = http_json(f"{cfg.jira_base}/rest/api/2/search?{params}",
                         {"Authorization": cfg.jira_auth_header})
        batch = page.get("issues", [])
        issues.extend(batch)
        total = page.get("total", len(issues))
        print(f"  Jira search: {len(issues)}/{total} issue")
        if cfg.max_issues and len(issues) >= cfg.max_issues:
            issues = issues[: cfg.max_issues]
            print(f"  dừng ở trần MAX_ISSUES={cfg.max_issues}")
            break
        if start + JIRA_PAGE_SIZE >= total or not batch:
            break
        start += JIRA_PAGE_SIZE
    return issues


def fetch_comments(cfg: Config, key: str) -> list[dict]:
    comments: list[dict] = []
    start = 0
    while True:
        params = urllib.parse.urlencode({"startAt": start, "maxResults": JIRA_PAGE_SIZE})
        page = http_json(f"{cfg.jira_base}/rest/api/2/issue/{key}/comment?{params}",
                         {"Authorization": cfg.jira_auth_header})
        batch = page.get("comments", [])
        comments.extend(batch)
        total = page.get("total", len(comments))
        if start + JIRA_PAGE_SIZE >= total or not batch:
            break
        start += JIRA_PAGE_SIZE
    return comments


def fetch_description(cfg: Config, key: str) -> tuple[str | None, str | None]:
    """Trả (description, created). Search không trả description để payload nhẹ,
    nên lấy riêng — mỗi issue thêm một lần gọi, chấp nhận được ở cỡ n=50-200."""
    page = http_json(f"{cfg.jira_base}/rest/api/2/issue/{key}?fields=description,created",
                     {"Authorization": cfg.jira_auth_header})
    fields = page.get("fields", {})
    return fields.get("description"), fields.get("created")


def iso(ts: str | None) -> str | None:
    """Chuẩn hoá timestamp của Jira Server về ISO 8601 mà .NET đọc được.

    Jira Server/DC trả "2026-08-01T10:00:00.000+0700" — offset KHÔNG có dấu hai
    chấm. System.Text.Json (DateTimeOffset) chỉ nhận "+07:00". Gửi nguyên dạng
    của Jira là cả lô bị 400 — lỗi này chỉ lộ khi gọi thật, nên chuẩn hoá ở đây.
    """
    if not ts:
        return None
    try:
        return datetime.strptime(ts, "%Y-%m-%dT%H:%M:%S.%f%z").isoformat()
    except ValueError:
        return ts  # dạng khác (đã có :, hoặc Z) — gửi nguyên, sai thì server nói rõ


# --------------------------------------------- AR-l: mã khách sạn (subTenantCode)

# Từ 2026-09-16 đường nạp BẮT BUỘC khai mã khách sạn, và connector này khai
# "khong-xac-dinh" — một lời khai THẬT, không phải một chỗ trống được lấp cho qua.
#
# 🛑 VÌ SAO CONNECTOR JIRA KHÔNG KHAI ĐƯỢC MÃ THẬT, và đây là chỗ đã ĐO chứ không phải
#    chưa làm tới:
#      Mã khách sạn   [customfield_12710]  ->  -1.0 ở 32/32 case. Trường TỒN TẠI, phủ
#                     100%, và là HẰNG SỐ — sentinel "chưa xác định" được thiết kế có ý
#                     định rồi không bao giờ được điền.
#      Tên khách sạn  [customfield_13326]  ->  25/32 rỗng; 7 case còn lại ghi TÊN TEAM
#                     ("ezCloud - Customer Support"), không phải tên khách sạn.
#    Và suy từ TIÊU ĐỀ thì bị cấm, cũng vì đã đo: chỉ 8/32 case có mã trong tiêu đề, và
#    ngay trong phần đó tên tiêu đề còn lệch với thân bài (ES-346622 ghi "Mariha" trong
#    khi email là mirahhotel.sales@). Sub-tenant là ranh giới BẢO MẬT — suy sai không
#    phải một nhãn sai, nó là dữ liệu khách sạn A nằm trong SOP của khách sạn B.
#
# ⚠ HỆ QUẢ, ghi ra để không ai đọc nhầm một lượt export thành công là AR-l đã mở lại:
#   lô export từ nguồn này có ĐÚNG 0 giá trị phân biệt. Server sẽ nói thẳng điều đó trong
#   `subTenantWarning`, và chế độ shared vẫn TỪ CHỐI DUYỆT (409).
#   Mở lại được khi ezCloud điền customfield_12710 cho thật, hoặc khi có một bảng ánh xạ
#   mã khách sạn đi kèm — lúc đó sửa ĐÚNG hằng số này, không sửa chỗ nào khác.
MA_KHACH_SAN = "khong-xac-dinh"


# ------------------------------------------------- AR-k: gán machineReadability

# Dấu hiệu nội dung do MÁY sinh ra và máy đọc lại được: payload, log, stack trace.
# Đây là những gì enum gọi là High — "kết quả query Kibana, response API" (§8.1-KQ).
#
# ⚠ ĐÃ ĐO TRÊN 873 MẨU THẬT (dry-run 345 + spread 528), 2026-09-15. Kết quả phải đọc kỹ:
#     '{' 196 · '}' 195 · '</' 16 · '/>' 1 · 'Exception' 1
#     11/16 dấu hiệu còn lại KHÔNG KHỚP MẨU NÀO — xml, Traceback, stack frame java/.NET,
#     HTTP/1., statusCode, SQL, curl.
#   Tức trên nguồn NÀY, nhãn High thực chất do MỘT cặp dấu quyết định: `{` và `}`.
#
# 🛑 GIỮ LẠI 11 dấu hiệu chưa khớp là CÓ CHỦ ĐÍCH, không phải quên dọn: `G12` nói đặc
#   điểm dữ liệu của MỘT khách là THAM SỐ, không phải hằng số thiết kế — một helpdesk
#   khác rất có thể dán stack trace. Nhưng ĐỪNG đọc danh sách này thành "đã phủ stack
#   trace": trên nguồn hiện có, phần đó CHƯA TỪNG được kiểm chứng bởi một mẩu nào.
_DAU_HIEU_CAU_TRUC = (
    "{", "}", "<?xml", "</", "/>",           # JSON / XML
    "Exception", "Traceback", "at java.", "at System.",
    "HTTP/1.", "status code", "statusCode",
    "SELECT ", "select ", "WHERE ", "curl ",
)

# Dấu hiệu nội dung máy KHÔNG đọc được: enum gọi là Low — "ảnh chưa OCR, ghi âm".
# Đo trên cùng 873 mẩu: '.png' 374 · '!image' 240 · '!screenshot' 39 · '.jpg' 35 ·
# '[^' 25 · '.mp4' 5 · '.jpeg' 3 · '.gif' 2. Chỉ '.mp3' và 'attachment' chưa khớp.
_DAU_HIEU_KHONG_DOC_DUOC = (
    "!image", "[^", ".png", ".jpg", ".jpeg", ".gif", ".mp4", ".mp3",
    "!screenshot", "attachment",
)

# Dưới ngưỡng này thì mẩu không mang nổi một quan sát nào. Đo trên corpus thật: mẩu
# 5 ký tự chỉ chứa "80771", mẩu 9 ký tự chỉ chứa chính mã case, mẩu 35 ký tự chỉ
# chứa tên file — cả ba đều từng được khai `High`.
_NGUONG_QUA_NGAN = 40


def doan_machine_readability(content: str) -> str:
    """BÊN GỬI khai máy đọc được mẩu này đến đâu — `AR-k`, chốt 2026-09-15.

    ⚠ TRƯỚC 2026-09-15 HÀM NÀY KHÔNG TỒN TẠI: script khai cứng `"High"` cho MỌI mẩu,
    và đo ra 128/128 đều `High` — kể cả mẩu 5 ký tự. Lập luận cũ ghi ngay tại chỗ là
    *"script chỉ đẩy text thuần nên nó BIẾT là text, khai High là khai thật"*. Lập luận
    đó sai ở chỗ đánh tráo hai câu hỏi: trường này KHÔNG hỏi "có phải text không", nó hỏi
    "MÁY ĐỌC ĐƯỢC ĐẾN ĐÂU". Một câu `"80771"` là text, và máy không đọc được gì từ nó.

    🛑 Một trường phủ 100% với MỘT giá trị duy nhất **tệ hơn trường rỗng**: trường rỗng
    thì ai cũng thấy là thiếu, còn trường phủ-100%-một-giá-trị thì TRÔNG NHƯ đã có dữ
    liệu — mọi phép kiểm "trường này có được điền không?" đều trả lời CÓ. Vì thế luật
    dưới đây phải sinh ra NHIỀU GIÁ TRỊ PHÂN BIỆT, và đó là thứ phải ĐO chứ không phải
    thứ để tin. Đo bằng: `python scripts/jira-export/check_corpus.py`.

    ⚠ Đây là phép đoán theo HÌNH DẠNG, và nó sẽ sai ở một số mẩu. Điều đó chấp nhận được
    và khác hẳn cái cũ: một nhãn sai lẻ tẻ thì lọc ra vẫn còn dữ liệu, còn một nhãn hằng
    số thì lọc xong mất sạch.
    """
    van_ban = (content or "").strip()

    if not van_ban:
        return "Unknown"

    # Xét hình dạng KHÔNG đọc được TRƯỚC: một mẩu chỉ có "!image-1.png!" vẫn dài hơn
    # ngưỡng ngắn, nhưng máy không rút được gì từ nó.
    thap = van_ban.lower()
    if any(d in thap for d in _DAU_HIEU_KHONG_DOC_DUOC):
        # Trừ khi quanh cái ảnh còn có văn bản thật đáng kể.
        con_lai = len(van_ban)
        if con_lai < 200:
            return "Low"

    if any(d in van_ban for d in _DAU_HIEU_CAU_TRUC):
        return "High"

    if len(van_ban) < _NGUONG_QUA_NGAN:
        return "Low"

    # Văn xuôi của người: đọc được, nhưng không phải dữ liệu có cấu trúc.
    return "Medium"


# ------------------------------------------------------------- dựng tín hiệu


def build_signals(cfg: Config, issues: list[dict]) -> tuple[list[dict], list[dict]]:
    cases: list[dict] = []
    evidence: list[dict] = []
    bo_qua: list[tuple[str, str]] = []

    for i, issue in enumerate(issues, 1):
        key = issue["key"]
        fields = issue.get("fields", {})
        case_ref = f"jira:{key}"

        subject = (fields.get("summary") or "").strip()
        if not subject:
            print(f"  ⚠ {key}: summary rỗng — bỏ qua cả issue (endpoint sẽ 400).")
            continue
        if len(subject) > MAX_SUBJECT:
            # Trần là của endpoint, không phải của Jira. Cắt và NÓI RA — cắt im lặng
            # là một thất bại im lặng nữa.
            print(f"  ⚠ {key}: subject dài {len(subject)}, cắt còn {MAX_SUBJECT}.")
            subject = subject[:MAX_SUBJECT]

        cases.append({
            "sourceReference": case_ref,
            "subject": subject,
            "sourceCreatedAt": iso(fields.get("created")),
            "sourceResolvedAt": iso(fields.get("resolutiondate")),
        })

        # ⚠ Bọc riêng phần gọi mạng của MỘT issue. Trước 2026-09-04 một timeout ở
        # issue thứ 141 làm mất trắng 140 issue đã đọc xong. Bỏ qua issue lỗi thì
        # corpus thiếu vài case — chấp nhận được cho phép đếm; mất cả lô thì không.
        # ⚠ Bỏ qua phải NÓI RA và ĐẾM: một corpus thiếu case mà người dùng tưởng đủ
        # sẽ làm mọi tỉ lệ tính trên nó sai, và sai im lặng.
        try:
            description, created = fetch_description(cfg, key)
            comments = fetch_comments(cfg, key)
        except LoiThoangQua as ex:
            bo_qua.append((key, str(ex)))
            print(f"  ⚠ BỎ QUA {key}: {ex}")
            cases.pop()  # đã thêm ở trên; bỏ luôn case để không có case rỗng nội dung
            continue

        if description and description.strip():
            evidence.append({
                "caseSourceReference": case_ref,
                "sourceReference": f"jira:{key}#description",
                # v0.2 R5: giữ NGUYÊN VĂN nguồn — không chèn thêm tác giả/nhãn gì
                # vào content. Ai nói cái gì chưa có chỗ chứa; đó là giới hạn đã
                # biết của schema evidence hôm nay, không phải của script.
                "content": description,
                "observedAt": iso(created),
                # Bên gửi khai, hệ thống không suy (IM-19) — và từ 2026-09-15 thì khai
                # THEO HÌNH DẠNG chứ không khai hằng số. Xem doan_machine_readability.
                "machineReadability": doan_machine_readability(description),
                # AR-l — bên gửi PHẢI khai. Xem MA_KHACH_SAN ở trên: nguồn Jira không có
                # trường nào dùng được, nên lời khai thật là "chưa xác định".
                "subTenantCode": MA_KHACH_SAN,
            })

        for c in comments:
            body = c.get("body") or ""
            if not body.strip():
                continue  # evidence rỗng nghĩa là rác trong kho gom — endpoint cũng sẽ 400
            evidence.append({
                "caseSourceReference": case_ref,
                "sourceReference": f"jira:{key}#comment-{c['id']}",
                "content": body,
                "observedAt": iso(c.get("created")),
                "machineReadability": doan_machine_readability(body),
                "subTenantCode": MA_KHACH_SAN,
            })

        if i % 20 == 0 or i == len(issues):
            print(f"  đọc nội dung: {i}/{len(issues)} issue, {len(evidence)} mẩu evidence")

    if bo_qua:
        print(f"{chr(10)}  ⚠ ĐÃ BỎ QUA {len(bo_qua)}/{len(issues)} issue vì lỗi mạng:")
        for key, ly_do in bo_qua:
            print(f"      {key}")
        print("    Corpus THIẾU những case này. Chạy lại script để lấy nốt — cả hai")
        print("    endpoint idempotent nên gửi lại vô hại.")

    return cases, evidence


# ----------------------------------------------------------------- đẩy vào app


def batches(items: list[dict]) -> list[list[dict]]:
    """Chia theo CẢ số lượng lẫn kích thước — trần 500 là của server (IM-16),
    trần bytes là để không đụng giới hạn body của Kestrel với comment dài."""
    out: list[list[dict]] = []
    cur: list[dict] = []
    cur_bytes = 2
    for item in items:
        size = len(json.dumps(item, ensure_ascii=False).encode("utf-8")) + 1
        if cur and (len(cur) >= MAX_PER_BATCH or cur_bytes + size > MAX_BATCH_BYTES):
            out.append(cur)
            cur, cur_bytes = [], 2
        cur.append(item)
        cur_bytes += size
    if cur:
        out.append(cur)
    return out


def push(cfg: Config, path: str, items: list[dict], label: str) -> None:
    headers = {}
    if cfg.signal_key:
        headers["Authorization"] = "Bearer " + cfg.signal_key  # AR-e: khoá theo từng khách
    received = created = 0
    for n, batch in enumerate(batches(items), 1):
        result = http_json(f"{cfg.app_base}{path}", headers, body=batch)
        received += result.get("received", 0)
        created += result.get("created", 0)
        print(f"  {label} lô {n}: gửi {len(batch)}, server nhận {result.get('received')}, "
              f"tạo mới {result.get('created')}")
    print(f"  {label}: TỔNG nhận {received}, tạo mới {created} "
          f"(chênh lệch = đã có từ trước, không phải lỗi — idempotent)")


def chan_neu_git_khong_bo_qua(duong_dan: str) -> None:
    """DỪNG nếu file sắp ghi không được `.gitignore` chặn.

    🛑 Nội dung sắp ghi là hội thoại THẬT với khách. `git add -A` quét hết, và một lần
      lọt vào lịch sử git là thứ không xoá lại được. Nên hỏi THẬT `git check-ignore`
      chứ không so tên thư mục — cùng lý do `nhom_sop.py` đã làm vậy.
    """
    goc = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
    tuyet_doi = os.path.abspath(duong_dan)
    try:
        trong_repo = os.path.commonpath([tuyet_doi, goc]) == goc
    except ValueError:
        # Khác ổ đĩa (C: vs D:) thì commonpath NÉM chứ không trả False.
        return
    if not trong_repo:
        return

    try:
        ma = subprocess.run(["git", "check-ignore", "-q", tuyet_doi], cwd=goc).returncode
    except OSError:
        die(f"không chạy được `git check-ignore` để kiểm {tuyet_doi}. Không ghi gì.")

    if ma != 0:
        die(f"{tuyet_doi} KHÔNG được .gitignore chặn, mà nội dung là hội thoại thật với "
            f"khách. Không ghi. `.gitignore` chặn theo mẫu `dry-run-*.json`, nên --nhan "
            f"phải chỉ gồm chữ thường/số/gạch ngang.")


def main() -> None:
    parser = argparse.ArgumentParser(description="Xuất Jira Server/DC vào Kênh 1 (Path A bước b).")
    parser.add_argument("--dry-run", action="store_true",
                        help="chỉ đọc Jira và ghi JSON ra cạnh script, KHÔNG gửi vào app")
    # ⚠ Corpus hoá đơn (150 case / 345 mẩu) là vật đối chiếu của check_corpus.py,
    #   nhom_sop.py và thu_retrieval.py. Chạy một chủ đề khác mà ghi đè lên nó là mất
    #   nền so sánh của mọi số đo đã công bố. Nhãn giữ hai corpus cùng tồn tại.
    parser.add_argument("--nhan", default="", metavar="NHÃN",
                        help="ghi ra dry-run-<NHÃN>-cases.json thay vì dry-run-cases.json, "
                             "để corpus chủ đề mới không đè lên corpus đã đo")
    args = parser.parse_args()

    if args.nhan and not re.fullmatch(r"[a-z0-9-]{1,40}", args.nhan):
        die("--nhan chỉ nhận chữ thường, số và gạch ngang (tối đa 40 ký tự) — để tên "
            "file luôn khớp mẫu `dry-run-*.json` mà .gitignore đang chặn.")

    cfg = read_config()
    print(f"Jira:  {cfg.jira_base}")
    print(f"JQL:   {cfg.jql}")
    print(f"App:   {cfg.app_base}  (khoá tín hiệu: {'có' if cfg.signal_key else 'không — chỉ đúng cho máy dev'})")

    print("Bước 1/3 — kéo issue từ Jira…")
    issues = fetch_issues(cfg)
    if not issues:
        die("JQL không khớp issue nào — kiểm tra lại JIRA_JQL.")

    print("Bước 2/3 — kéo description + comment…")
    cases, evidence = build_signals(cfg, issues)
    print(f"  dựng xong: {len(cases)} tín hiệu case, {len(evidence)} mẩu evidence")

    if args.dry_run:
        here = os.path.dirname(os.path.abspath(__file__))
        tien_to = f"dry-run-{args.nhan}" if args.nhan else "dry-run"
        for name, data in ((f"{tien_to}-cases.json", cases), (f"{tien_to}-evidence.json", evidence)):
            path = os.path.join(here, name)
            chan_neu_git_khong_bo_qua(path)
            with open(path, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            print(f"  đã ghi {path}")
        print("DRY RUN — chưa gửi gì. Soi hai file trên rồi chạy lại không có --dry-run.")
        return

    print("Bước 3/3 — đẩy vào Kênh 1 (case TRƯỚC, evidence SAU — AR-f)…")
    push(cfg, "/signals/case-observed", cases, "case")
    push(cfg, "/signals/case-evidence", evidence, "evidence")
    print("XONG. Chạy lại script bất kỳ lúc nào đều vô hại — cả hai đường idempotent.")
    print()
    print(f"⚠ AR-l: lô này khai subTenantCode = \"{MA_KHACH_SAN}\" cho TẤT CẢ mẩu, vì nguồn")
    print("  Jira không có trường nào dùng được (đã đo: customfield_12710 = -1.0 ở 32/32).")
    print("  Tức 0 giá trị PHÂN BIỆT — ranh giới khách sạn CHƯA mở lại được, và chế độ")
    print("  shared vẫn từ chối duyệt. Hỏi trạng thái: GET /internal/sub-tenant-coverage")
    print()
    print("⚠ Mẩu ĐÃ CÓ trong kho thì mã khách sạn mới KHÔNG ghi đè (K-B3). Gửi chồng lên")
    print("  sẽ trả 200 mà không đổi gì — xem subTenantNotApplied trong response.")


if __name__ == "__main__":
    main()
