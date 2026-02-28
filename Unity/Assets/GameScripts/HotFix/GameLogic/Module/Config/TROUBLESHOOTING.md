# ConfigLoader 错误修复指南

## 问题：Package initialize not completed

### 错误信息
```
YooAsset 加载配置文件异常: AIConfigCategory, Error: System.Exception: Package initialize not completed !
```

### 原因
配置加载时 YooAsset 资源包还未初始化完成。

---

## ✅ 解决方案

### 方案 1：使用 StreamingAssets 模式（推荐用于开发）

已自动配置：编辑器模式下自动使用 StreamingAssets 模式。

```csharp
// EntryEvent3_LoadConfig.cs 已自动处理
#if UNITY_EDITOR
    ConfigLoader.CurrentMode = ConfigLoader.LoadMode.StreamingAssets;
#else
    ConfigLoader.CurrentMode = ConfigLoader.LoadMode.AssetBundle;
#endif
```

**优点**：
- ✅ 不依赖 YooAsset 初始化
- ✅ 开发调试方便
- ✅ 配置修改后立即生效

**配置文件位置**：
```
StreamingAssets/Configs/
├── UnitConfigCategory.bytes
├── AIConfigCategory.bytes
└── ResourceConfigCategory.bytes
```

---

### 方案 2：等待 YooAsset 初始化完成（正式环境）

如果需要在正式环境使用 AssetBundle 模式：

```csharp
// 1. 初始化 YooAsset
var package = YooAssets.CreatePackage("DefaultPackage");
var initParameters = new OfflinePlayModeParameters();
var initOperation = package.InitializeAsync(initParameters);

// 2. 等待初始化完成
await initOperation.Task;

// 3. 加载配置
ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
ConfigLoader.CurrentMode = ConfigLoader.LoadMode.AssetBundle;
configComponent.Load();
```

---

## 🔧 已修复内容

### 1. ConfigLoader.cs

添加了 YooAsset 初始化状态检查：

```csharp
// 检查包是否初始化完成
if (package.InitializeStatus != EOperationStatus.Succeed)
{
    Log.Error($"YooAsset 资源包未初始化完成: {PackageName}, Status: {package.InitializeStatus}");
    Log.Error("请确保在加载配置前完成 YooAsset 初始化，或使用 StreamingAssets 模式");
    return null;
}
```

### 2. EntryEvent3_LoadConfig.cs

自动根据环境选择加载模式：

```csharp
#if UNITY_EDITOR
    // 编辑器：StreamingAssets 模式
    ConfigLoader.CurrentMode = ConfigLoader.LoadMode.StreamingAssets;
#else
    // 正式环境：AssetBundle 模式
    ConfigLoader.CurrentMode = ConfigLoader.LoadMode.AssetBundle;
#endif
```

---

## 📋 配置文件准备

### 导出配置到 StreamingAssets

执行 Luban 导出脚本（已修改输出路径）：

```bash
cd Tools/Luban
./GenConfig_Client.sh  # macOS/Linux
# 或
GenConfig_Client.bat   # Windows
```

配置文件会自动导出到：
```
Unity/Assets/AssetRaw/Configs/
├── UnitConfigCategory.bytes
├── AIConfigCategory.bytes
└── ResourceConfigCategory.bytes
```

### 复制到 StreamingAssets（开发模式）

如果需要在编辑器使用 StreamingAssets 模式：

```bash
# 创建目录
mkdir -p Unity/Assets/StreamingAssets/Configs

# 复制配置文件
cp Unity/Assets/AssetRaw/Configs/*.bytes Unity/Assets/StreamingAssets/Configs/
```

---

## 🚀 使用建议

### 开发阶段

```csharp
// 使用 StreamingAssets 模式（默认）
ConfigLoader.CurrentMode = ConfigLoader.LoadMode.StreamingAssets;
```

**优点**：
- 不需要 YooAsset 初始化
- 配置修改后重新导出即可
- 调试方便

### 正式环境

```csharp
// 使用 AssetBundle 模式
ConfigLoader.CurrentMode = ConfigLoader.LoadMode.AssetBundle;
```

**前提**：
- YooAsset 已初始化完成
- 配置文件已打包到 AssetBundle
- 配置文件设置为可寻址

---

## ⚠️ 常见问题

### Q1: 编辑器模式下仍然报错？

**检查**：
1. 配置文件是否存在于 `StreamingAssets/Configs/`
2. 是否执行了 Luban 导出脚本
3. 是否复制了 .bytes 文件到 StreamingAssets

**解决**：
```bash
# 重新导出配置
cd Tools/Luban
./GenConfig_Client.sh

# 复制到 StreamingAssets
cp Unity/Assets/AssetRaw/Configs/*.bytes Unity/Assets/StreamingAssets/Configs/
```

### Q2: 正式环境下如何使用 AssetBundle 模式？

**步骤**：
1. 确保 YooAsset 在配置加载前初始化完成
2. 配置文件设置为可寻址资源
3. 打包 AssetBundle
4. 设置 `ConfigLoader.CurrentMode = LoadMode.AssetBundle`

### Q3: 如何手动切换加载模式？

```csharp
// 在加载配置前设置
ConfigLoader.CurrentMode = ConfigLoader.LoadMode.StreamingAssets; // 或 AssetBundle

ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
configComponent.Load();
```

---

## 📊 加载模式对比

| 模式 | 依赖 | 适用场景 | 配置路径 |
|------|------|----------|----------|
| **StreamingAssets** | 无 | 开发/测试 | `StreamingAssets/Configs/` |
| **AssetBundle** | YooAsset | 正式环境 | YooAsset 可寻址 |
| **Resources** | 无 | 快速测试 | `Resources/Configs/` |
| **PersistentData** | 无 | 热更新 | `PersistentDataPath/Configs/` |

---

## 🔄 更新记录

### 2024-02-27

**修复内容**：
- ✅ 添加 YooAsset 初始化状态检查
- ✅ 编辑器模式自动使用 StreamingAssets
- ✅ 改善错误提示信息
- ✅ 添加详细的解决建议

**修改文件**：
- `ConfigLoader.cs` - 添加初始化检查
- `EntryEvent3_LoadConfig.cs` - 自动选择加载模式

---

## 📚 相关文档

- **YooAsset 集成**: `Module/Config/YOOASSET_INTEGRATION.md`
- **配置系统**: `Module/Config/README.md`
- **Luban 导出**: `Tools/Luban/CONFIG_EXPORT_RULES.md`

---

**问题已修复！编辑器模式下会自动使用 StreamingAssets 模式，不再依赖 YooAsset 初始化。** ✅
