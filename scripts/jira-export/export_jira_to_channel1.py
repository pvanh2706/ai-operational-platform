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
    APP_SIGNAL_KEY    tuỳ chọn   máy dev không cần (appsettings.Development.json
                                 đã thừa nhận endpoint không khoá); deploy thật thì cần
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
import sys
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from datetime import datetime

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
        signal_key=os.environ.get("APP_SIGNAL_KEY") or None,
        max_issues=int(os.environ.get("MAX_ISSUES", "0")),
    )


def http_json(url: str, headers: dict[str, str], body: object | None = None) -> dict:
    data = None
    if body is not None:
        data = json.dumps(body, ensure_ascii=False).encode("utf-8")
        headers = {**headers, "Content-Type": "application/json; charset=utf-8"}
    req = urllib.request.Request(url, data=data, headers=headers,
                                 method="POST" if body is not None else "GET")
    try:
        with urllib.request.urlopen(req, timeout=TIMEOUT_SECONDS) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        # Body lỗi của cả Jira lẫn app đều nói rõ sai ở đâu (app trả ValidationProblem
        # chỉ đích danh phần tử lỗi) — nuốt nó đi là bắt người chạy đoán mò.
        detail = e.read().decode("utf-8", errors="replace")
        die(f"HTTP {e.code} từ {url}\n{detail}")
    except urllib.error.URLError as e:
        die(f"không gọi được {url}: {e.reason}")


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


# ------------------------------------------------------------- dựng tín hiệu


def build_signals(cfg: Config, issues: list[dict]) -> tuple[list[dict], list[dict]]:
    cases: list[dict] = []
    evidence: list[dict] = []

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

        description, created = fetch_description(cfg, key)
        if description and description.strip():
            evidence.append({
                "caseSourceReference": case_ref,
                "sourceReference": f"jira:{key}#description",
                # v0.2 R5: giữ NGUYÊN VĂN nguồn — không chèn thêm tác giả/nhãn gì
                # vào content. Ai nói cái gì chưa có chỗ chứa; đó là giới hạn đã
                # biết của schema evidence hôm nay, không phải của script.
                "content": description,
                "observedAt": iso(created),
                # Bên gửi khai, hệ thống không suy (IM-19). Script này chỉ đẩy text
                # thuần từ REST API — nó BIẾT là text, nên khai High là khai thật.
                "machineReadability": "High",
            })

        for c in fetch_comments(cfg, key):
            body = c.get("body") or ""
            if not body.strip():
                continue  # evidence rỗng nghĩa là rác trong kho gom — endpoint cũng sẽ 400
            evidence.append({
                "caseSourceReference": case_ref,
                "sourceReference": f"jira:{key}#comment-{c['id']}",
                "content": body,
                "observedAt": iso(c.get("created")),
                "machineReadability": "High",
            })

        if i % 20 == 0 or i == len(issues):
            print(f"  đọc nội dung: {i}/{len(issues)} issue, {len(evidence)} mẩu evidence")

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
        headers["X-Signal-Key"] = cfg.signal_key
    received = created = 0
    for n, batch in enumerate(batches(items), 1):
        result = http_json(f"{cfg.app_base}{path}", headers, body=batch)
        received += result.get("received", 0)
        created += result.get("created", 0)
        print(f"  {label} lô {n}: gửi {len(batch)}, server nhận {result.get('received')}, "
              f"tạo mới {result.get('created')}")
    print(f"  {label}: TỔNG nhận {received}, tạo mới {created} "
          f"(chênh lệch = đã có từ trước, không phải lỗi — idempotent)")


def main() -> None:
    parser = argparse.ArgumentParser(description="Xuất Jira Server/DC vào Kênh 1 (Path A bước b).")
    parser.add_argument("--dry-run", action="store_true",
                        help="chỉ đọc Jira và ghi JSON ra cạnh script, KHÔNG gửi vào app")
    args = parser.parse_args()

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
        for name, data in (("dry-run-cases.json", cases), ("dry-run-evidence.json", evidence)):
            path = os.path.join(here, name)
            with open(path, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            print(f"  đã ghi {path}")
        print("DRY RUN — chưa gửi gì. Soi hai file trên rồi chạy lại không có --dry-run.")
        return

    print("Bước 3/3 — đẩy vào Kênh 1 (case TRƯỚC, evidence SAU — AR-f)…")
    push(cfg, "/signals/case-observed", cases, "case")
    push(cfg, "/signals/case-evidence", evidence, "evidence")
    print("XONG. Chạy lại script bất kỳ lúc nào đều vô hại — cả hai đường idempotent.")


if __name__ == "__main__":
    main()
