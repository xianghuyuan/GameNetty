---
name: proto-config
description: 处理 GameNetty 的 Proto 消息、Opcode、Luban 配置表、生成代码同步和双端协议一致性问题。用于新增或修改协议、配置表、生成代码、二进制配置数据，以及排查协议或配置导致的功能异常。
---

# 协议与配置 Skill

## 适用范围

用于：

- 新增或修改 `Config/Proto/` 下的客户端-服务端或服务端内部消息。
- 新增或修改 `Config/Excel/GameConfig/` 下的 Luban 配置表。
- 同步服务端和客户端的生成代码、Opcode、配置二进制数据。
- 排查协议字段、Opcode、配置字段、生成代码不同步导致的问题。

不用于：

- 不涉及协议和配置的纯业务逻辑改动。
- Unity Prefab 或 UI 结构调整；使用 `unity-ui-prefab`。
- 战斗架构规则判断；战斗相关时可结合 `battle-system`。

## 核心规则

- Proto 定义位于 `Config/Proto/`。
- `OuterMessage_C_10001.proto` 不会自动生成 C#。修改后必须手动同步双端生成代码。
- 服务端消息生成代码位于 `Server/Model/Generate/Message/OuterMessage_C_10001.cs`。
- 客户端消息生成代码位于 `Unity/Assets/GameScripts/HotFix/GameProto/Generate/Message/OuterMessage_C_10001.cs`。
- Opcode 顺序递增，从 `10001` 开始；新增消息使用下一个可用 Opcode。
- Excel 配置源位于 `Config/Excel/GameConfig/`。
- 修改 Excel 配置后运行 Luban 脚本生成双端代码和二进制数据。
- 不要只改一端协议或生成代码；协议、Opcode、字段顺序和字段类型必须双端一致。

## 工作流程

1. 明确改动类型：Proto、Excel 配置、生成代码、二进制配置数据，或它们的组合。
2. 先读取 `AGENTS.md`，再搜索现有相似协议、配置表、Handler、客户端调用点。
3. 修改 Proto 时，检查最后一个 Opcode 和消息类，分配下一个 Opcode。
4. 手动同步服务端和客户端生成消息代码，保持类名、字段、Opcode 完全一致。
5. 修改 Excel 时，确认表注册、字段名、字段类型和现有配置读取方式一致。
6. 修改完成后运行可用生成脚本和构建命令；无法运行时说明原因。

## 验证

- Proto 改动：检查 `Config/Proto/`、服务端生成代码、客户端生成代码三处一致。
- 配置改动：运行 `./Tools/Luban/GenConfig_Server.sh` 和 `./Tools/Luban/GenConfig_Client.sh`。
- 服务端受影响：运行 `dotnet build Server/Server.sln`。
- Share 工具或生成器受影响：先运行 `dotnet build Share/Share.sln`。

## 冲突处理

- 与流程型 skill 冲突时，流程由 `fix-bug` 或 `implement-feature` 决定，本 skill 负责协议和配置规则。
- 与领域 skill 冲突时，如果改动核心是消息、Opcode 或配置生成，以本 skill 的同步规则为准。
- 如果冲突会改变数据权威、持久化结构、协议兼容性或生成代码归属，先向用户确认。

## 最终汇报

汇报：

- 修改了哪些协议或配置。
- Opcode 和字段同步情况。
- 生成脚本和构建验证结果。
- 仍需手动处理的数据或风险。
