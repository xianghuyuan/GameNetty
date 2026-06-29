# UI 美术规格模板

## 基本信息

```text
ResourceKey:
UI 模块:
用途:
```

## 风格要求

```text
界面类型: 战斗 HUD / 背包 / 结算 / 图标 / 按钮 / 面板
适配方向: 横屏
风格关键词:
识别要求:
```

## 路径契约

```text
临时产出路径:
Unity/Assets/AssetRaw/UIRaw/_Incoming/{模块名}/{ResourceKey}/

正式 UI 大图路径:
Unity/Assets/AssetRaw/UIRaw/Raw/{模块名}/

正式 UI 小图/图标路径:
Unity/Assets/AssetRaw/UIRaw/Atlas/{模块名}/
```

## 资产清单

| 资源名 | 文件名 | 尺寸 | 说明 |
| --- | --- | --- | --- |
| Icon_{ResourceKey} | Icon_{ResourceKey}.png | 128x128 | 图标，放 `Atlas` |
| Btn_{ResourceKey}_Normal | Btn_{ResourceKey}_Normal.png | 视需求 | 按钮默认态，放 `Atlas` |
| Btn_{ResourceKey}_Pressed | Btn_{ResourceKey}_Pressed.png | 视需求 | 按钮按下态，放 `Atlas` |
| Panel_{ResourceKey}_Bg | Panel_{ResourceKey}_Bg.png | 视需求 | 面板背景，小尺寸放 `Atlas`，大尺寸放 `Raw` |
| Raw_{ResourceKey}_Bg | Raw_{ResourceKey}_Bg.png | 视需求 | 整屏背景或大图，放 `Raw` |

## 程序绑定字段

```text
IconPath:
PrefabPath:
AtlasPath:
RawPath:
```

## AI 生图 Prompt

```text

```

## 验收标准

- 图标在目标尺寸下主体清楚。
- 同一模块内颜色、描边、光影风格一致。
- 按钮状态差异明确。
- UI 资源切片和九宫格规则清楚。
- 大图放 `Assets/AssetRaw/UIRaw/Raw`，小图和图标放 `Assets/AssetRaw/UIRaw/Atlas`。
