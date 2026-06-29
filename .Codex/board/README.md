# Codex Board

本地流程看板，用来读取 `.Codex/plans/*.plan.json`，编辑节点状态，并执行 plan 中声明过的白名单动作。

## 启动

```bash
python3 .Codex/board/server.py
```

默认地址：

```text
http://127.0.0.1:8765
```

也可以指定端口：

```bash
python3 .Codex/board/server.py 8777
```

## 文件分工

- `.Codex/plans/*.plan.json`：流程实例和节点状态。
- `.Codex/board/server.py`：本地服务，负责读写 plan、执行白名单 action。
- `.Codex/board/index.html`：看板页面。
- `.Codex/board/board.js`：前端交互。
- `.Codex/artifacts/<plan-id>/logs/`：action 执行日志。

## Action 规则

面板只能执行 plan JSON 中声明过的 `actions`。`command` 必须是字符串数组，不经过 shell 展开。

```json
{
  "id": "dry_run_assets",
  "label": "素材 Dry Run",
  "command": [
    "rg",
    "-n",
    "BattleMainWindow",
    "Unity/Assets/GameScripts",
    "--dry-run"
  ],
  "on_success_status": "review"
}
```
