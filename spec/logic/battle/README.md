# 战斗域逻辑

本目录存放战斗相关功能逻辑规格。战斗域下继续按子系统拆分，避免 AI、表现层、HUD 状态逻辑混在同一层。

## 子目录

- [ai/](./ai/README.md)：战斗 AI、目标选择、自动战斗和高性能小怪逻辑。
- [view/](./view/README.md)：战斗单位表现层、逻辑状态到表现状态的映射。
- [hud/](./hud/README.md)：战斗内 HUD 状态、界面交互逻辑和客户端 Widget 绑定。
- [vehicle/](./vehicle/README.md)：战斗中的载具、载具 Buff、载具成长和战斗效果逻辑。
