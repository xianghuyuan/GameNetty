# UI 美术规格文档

本目录用于管理 UI 生图需求、资源规格、命名规则和验收标准。这里的文档是程序线和 UI 美术线之间的契约，不是 Unity 运行时资源。

## 目录职责

```text
docs/art/
  README.md                 UI 美术规格总说明
  _templates/               可复用模板
  ui/                       UI 图标、界面、大图规格
```

UI 原始资源遵守项目现有目录：

```text
Unity/Assets/AssetRaw/UIRaw/Raw/       大图、整屏背景、未入图集的大尺寸 UI 图
Unity/Assets/AssetRaw/UIRaw/Atlas/     小图、图标、按钮、血条、可入图集 UI 图
```

`Atlas` 下按一级模块目录生成独立图集，例如 `Atlas/Battle/` 和 `Atlas/Common/`。不要再建立 `SingleAtlas` 目录。

AI 生图、外包回稿、未验收资源建议先放入：

```text
Unity/Assets/AssetRaw/UIRaw/_Incoming/
```

## 使用流程

1. 新需求先在 `docs/art/` 下创建对应规格文档。
2. 先确定 `ResourceKey`、正式资源路径、命名规则、尺寸、切图方式和验收标准。
3. 程序按文档里的 `ResourceKey` 和路径接入占位资源。
4. UI 美术按文档输出大图、图标、按钮、面板、状态图等资源。
5. 未确认资源进入 `_Incoming/`，确认后再移动到正式资源目录。
6. Unity 集成时检查 Sprite 导入设置、图集归类、九宫格切片、Prefab 引用、配置路径是否和规格文档一致。

## 文档命名

推荐使用稳定资源名：

```text
UI_BattleHud_ArtSpec.md
UI_MainMenu_ArtSpec.md
UI_Settlement_ArtSpec.md
UI_SkillIcons_ArtSpec.md
```

## 核心原则

- 规格文档描述“要什么”和“怎么验收”。
- `AssetRaw/UIRaw` 保存 UI 原始资源。
- 程序不要依赖未确认图片，只依赖 `ResourceKey`、Prefab 路径、Icon 路径等稳定契约。
- 美术资源先进入 `_Incoming/`，验收后再进入正式目录。
- UI 大图进入 `Assets/AssetRaw/UIRaw/Raw`，UI 小图和图标进入 `Assets/AssetRaw/UIRaw/Atlas`。
