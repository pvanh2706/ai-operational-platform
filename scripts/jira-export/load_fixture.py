#!/usr/bin/env python3
"""Nạp fixture vào Kênh 1 của app đang chạy.

Tách khỏi export_jira_to_channel1.py có chủ đích: cái kia nói chuyện với Jira và cần
credential; cái này chỉ đọc hai file JSON đã có sẵn trong repo và gọi vào app local.
Không có bí mật nào đi qua đây, nên nó chạy được trên máy bất kỳ ai clone repo.

    python scripts/jira-export/make_fixture.py   # tạo file, chạy trước
    dotnet run --project src/KnowledgePlatform.Api --launch-profile http
    python scripts/jira-export/load_fixture.py

Chạy lại vô hại — cả hai endpoint idempotent theo (TenantId, SourceReference).
"""

from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
APP = os.environ.get("APP_BASE_URL", "http://localhost:5119").rstrip("/")
KEY = os.environ.get("APP_SIGNAL_KEY") or None
MAX_PER_BATCH = 500


def post(path: str, body: list) -> dict:
    headers = {"Content-Type": "application/json; charset=utf-8"}
    if KEY:
        headers["X-Signal-Key"] = KEY
    req = urllib.request.Request(
        f"{APP}{path}", method="POST", headers=headers,
        data=json.dumps(body, ensure_ascii=False).encode("utf-8"))
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            return json.loads(r.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        print(f"LỖI HTTP {e.code} tại {path}:\n{e.read().decode('utf-8', 'replace')}",
              file=sys.stderr)
        sys.exit(1)
    except urllib.error.URLError as e:
        print(f"Không gọi được {APP}{path}: {e.reason}\n"
              f"App đã chạy chưa? "
              f"dotnet run --project src/KnowledgePlatform.Api --launch-profile http",
              file=sys.stderr)
        sys.exit(1)


def load(ten: str) -> list:
    p = os.path.join(HERE, ten)
    if not os.path.exists(p):
        print(f"Chưa có {ten}. Chạy trước: python scripts/jira-export/make_fixture.py",
              file=sys.stderr)
        sys.exit(1)
    with open(p, encoding="utf-8") as f:
        return json.load(f)


def push(path: str, items: list, nhan: str) -> None:
    nhan_da, tao_moi = 0, 0
    for i in range(0, len(items), MAX_PER_BATCH):
        r = post(path, items[i:i + MAX_PER_BATCH])
        nhan_da += r.get("received", 0)
        tao_moi += r.get("created", 0)
    print(f"  {nhan}: server nhận {nhan_da}, tạo mới {tao_moi}"
          f"  (chênh lệch = đã có từ trước, không phải lỗi)")


def main() -> None:
    cases, evidence = load("fixture-cases.json"), load("fixture-evidence.json")
    print(f"Nạp vào {APP} — {len(cases)} case, {len(evidence)} evidence")
    # Thứ tự BẮT BUỘC: evidence trỏ tới case chưa tồn tại thì cả lô bị từ chối (AR-f).
    push("/signals/case-observed", cases, "case")
    push("/signals/case-evidence", evidence, "evidence")
    print("XONG.")


if __name__ == "__main__":
    main()
