#!/usr/bin/env python3
from __future__ import annotations

import json
import subprocess
import sys
from datetime import datetime
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[2]
BOARD_DIR = ROOT / ".Codex" / "board"
PLANS_DIR = ROOT / ".Codex" / "plans"
ARTIFACTS_DIR = ROOT / ".Codex" / "artifacts"
STATUSES = {"todo", "doing", "review", "done", "blocked", "dropped"}


def read_json(request):
    length = int(request.headers.get("Content-Length", "0"))
    if length <= 0:
        return {}
    return json.loads(request.rfile.read(length).decode("utf-8"))


def write_json(request, status, payload):
    body = json.dumps(payload, ensure_ascii=False, indent=2).encode("utf-8")
    request.send_response(status)
    request.send_header("Content-Type", "application/json; charset=utf-8")
    request.send_header("Content-Length", str(len(body)))
    request.end_headers()
    request.wfile.write(body)


def plan_path(plan_id):
    safe = plan_id.replace("/", "").replace("\\", "")
    path = PLANS_DIR / f"{safe}.plan.json"
    if not path.exists():
        raise FileNotFoundError(plan_id)
    return path


def load_plan(plan_id):
    return json.loads(plan_path(plan_id).read_text(encoding="utf-8"))


def save_plan(plan):
    path = plan_path(plan["id"])
    path.write_text(json.dumps(plan, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def find_node(plan, node_id):
    for node in plan.get("nodes", []):
        if node.get("id") == node_id:
            return node
    raise KeyError(node_id)


def find_action(node, action_id):
    for action in node.get("actions", []):
        if action.get("id") == action_id:
            return action
    raise KeyError(action_id)


def list_plans():
    plans = []
    for path in sorted(PLANS_DIR.glob("*.plan.json")):
        plans.append(json.loads(path.read_text(encoding="utf-8")))
    return plans


def run_action(plan_id, node_id, action_id):
    plan = load_plan(plan_id)
    node = find_node(plan, node_id)
    action = find_action(node, action_id)
    command = action.get("command")
    if not isinstance(command, list) or not command or not all(isinstance(item, str) for item in command):
        raise ValueError("Action command must be a non-empty string array.")

    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    log_dir = ARTIFACTS_DIR / plan_id / "logs"
    log_dir.mkdir(parents=True, exist_ok=True)
    log_path = log_dir / f"{node_id}-{action_id}-{stamp}.log"

    started = datetime.now().isoformat(timespec="seconds")
    result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, timeout=600)
    finished = datetime.now().isoformat(timespec="seconds")

    log = {
        "plan_id": plan_id,
        "node_id": node_id,
        "action_id": action_id,
        "command": command,
        "started_at": started,
        "finished_at": finished,
        "returncode": result.returncode,
        "stdout": result.stdout,
        "stderr": result.stderr
    }
    log_path.write_text(json.dumps(log, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    node["last_run"] = {
        "action_id": action_id,
        "returncode": result.returncode,
        "log": str(log_path.relative_to(ROOT)),
        "finished_at": finished
    }
    if result.returncode == 0:
        node["status"] = action.get("on_success_status", node.get("status", "review"))
    else:
        node["status"] = "blocked"
    plan["current_node"] = node_id
    save_plan(plan)
    return {"ok": result.returncode == 0, "plan": plan, "log": node["last_run"]}


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(BOARD_DIR), **kwargs)

    def log_message(self, fmt, *args):
        sys.stderr.write("[board] " + fmt % args + "\n")

    def do_GET(self):
        if self.path == "/api/plans":
            write_json(self, 200, {"plans": list_plans()})
            return
        if self.path.startswith("/api/plans/"):
            plan_id = unquote(self.path.removeprefix("/api/plans/"))
            write_json(self, 200, {"plan": load_plan(plan_id)})
            return
        super().do_GET()

    def do_PATCH(self):
        if not self.path.startswith("/api/plans/") or "/nodes/" not in self.path:
            write_json(self, 404, {"error": "Unknown endpoint"})
            return
        left, node_id = self.path.removeprefix("/api/plans/").split("/nodes/", 1)
        plan_id = unquote(left)
        node_id = unquote(node_id)
        payload = read_json(self)
        plan = load_plan(plan_id)
        node = find_node(plan, node_id)

        if "status" in payload:
            if payload["status"] not in STATUSES:
                write_json(self, 400, {"error": "Invalid status"})
                return
            node["status"] = payload["status"]
            plan["current_node"] = node_id
        if "notes" in payload:
            node["notes"] = str(payload["notes"])

        save_plan(plan)
        write_json(self, 200, {"plan": plan})

    def do_POST(self):
        if self.path != "/api/actions/run":
            write_json(self, 404, {"error": "Unknown endpoint"})
            return
        payload = read_json(self)
        try:
            result = run_action(payload["plan_id"], payload["node_id"], payload["action_id"])
        except Exception as exc:
            write_json(self, 500, {"error": str(exc)})
            return
        write_json(self, 200, result)


def main():
    host = "127.0.0.1"
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8765
    server = ThreadingHTTPServer((host, port), Handler)
    print(f"Codex board: http://{host}:{port}")
    server.serve_forever()


if __name__ == "__main__":
    main()
