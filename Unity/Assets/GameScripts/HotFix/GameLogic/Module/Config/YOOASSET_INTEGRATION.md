# ConfigLoader YooAsset 集成说明

## 概述

ConfigLoader 已集成 YooAsset，支持从 AssetBundle 加载配置文件。

---

## 🔧 实现内容

### LoadFromAssetBundle 方法

使用 YooAsset 同步加载配置文件（可寻址模式）：

```csharp
private static byte[] LoadFromAssetBundle(string fileName)
{
    // 1. 获取资源包
    var package = YooAssets.GetPackage(PackageName);
    
    // 2. 使用可寻址名称加载（不需要完整路径）
    string assetPath = $"{fileName}.bytes";
    
    // 3. 同步加载
    var handle = package.LoadAssetSync<TextAsset>(assetPath);
    
    // 4. 获取字节数据
    byte[] bytes = textAsset.bytes;
    
    // 5. 释放句柄
    handle.Release();
    
    return bytes;
}
```

**注意**：使用可寻址名称，不需要完整的 `Assets/AssetRaw/...` 路径。

---

## 📂 配置参数

### 可配置属性

```csharp
// 当前加载模式（默认：AssetBundle）
ConfigLoader.CurrentMode = ConfigLoader.LoadMode.AssetBundle;

// 配置文件目录名（默认：Configs）
ConfigLoader.ConfigDirectory = "Configs";

// YooAsset 资源包名称（默认：DefaultPackage）
ConfigLoader.PackageName = "DefaultPackage";
```

---

## 🚀 使用方式

### 1. 默认使用（推荐）

```csharp
// 使用默认配置（AssetBundle 模式）
ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
configComponent.Load();

// 配置文件会从以下路径加载：
// Assets/AssetRaw/Configs/UnitConfigCategory.bytes
// Assets/AssetRaw/Configs/AIConfigCategory.bytes
// Assets/AssetRaw/Configs/ResourceConfigCategory.bytes
```

### 2. 自定义资源包

```csharp
// 如果使用自定义资源包名称
ConfigLoader.PackageName = "MyCustomPackage";

ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
configComponent.Load();
```

### 3. 切换加载模式

```csharp
// 开发模式：从 StreamingAssets 加载
#if UNITY_EDITOR
ConfigLoader.CurrentMode = ConfigLoader.LoadMode.StreamingAssets;
#else
// 正式环境：从 AssetBundle 加载
ConfigLoader.CurrentMode = ConfigLoader.LoadMode.AssetBundle;
#endif

ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
configComponent.Load();
```

---

## 📋 加载模式对比

| 模式 | 说明 | 路径 | 使用场景 |
|------|------|------|----------|
| **AssetBundle** | YooAsset 加载 | `Assets/AssetRaw/Configs/*.bytes` | 正式环境（默认） |
| **StreamingAssets** | 文件系统加载 | `StreamingAssets/Configs/*.bytes` | 开发/测试 |
| **Resources** | Resources 加载 | `Resources/Configs/*` | 快速测试 |
| **PersistentData** | 持久化目录 | `PersistentDataPath/Configs/*.bytes` | 热更新配置 |

---

## 🔄 资源路径规则

### YooAsset 可寻址模式

配置文件使用可寻址名称加载，不需要完整路径：

```csharp
// fileName: "UnitConfigCategory"
// 加载路径: "UnitConfigCategory.bytes"

string assetPath = $"{fileName}.bytes";
var handle = package.LoadAssetSync<TextAsset>(assetPath);
```

### 资源配置

在 YooAsset 的资源收集器中，确保配置文件被正确标记为可寻址：

```
Assets/AssetRaw/Configs/
├── UnitConfigCategory.bytes    → 可寻址名称: "UnitConfigCategory.bytes"
├── AIConfigCategory.bytes      → 可寻址名称: "AIConfigCategory.bytes"
└── ResourceConfigCategory.bytes → 可寻址名称: "ResourceConfigCategory.bytes"
```

**优势**：
- ✅ 路径简洁，不需要完整路径
- ✅ 支持资源重定向和热更新
- ✅ 符合 YooAsset 最佳实践

---

## ⚠️ 注意事项

### 1. YooAsset 初始化

确保在加载配置前，YooAsset 已经初始化：

```csharp
// 初始化 YooAsset
var package = YooAssets.CreatePackage("DefaultPackage");
var initParameters = new OfflinePlayModeParameters();
await package.InitializeAsync(initParameters);

// 然后加载配置
ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
configComponent.Load();
```

### 2. 资源包名称

默认使用 `"DefaultPackage"`，如果你的项目使用不同的包名，需要设置：

```csharp
ConfigLoader.PackageName = "YourPackageName";
```

### 3. 同步加载

当前实现使用同步加载 `LoadAssetSync`，适合配置文件这种小文件。

如果需要异步加载，可以修改 `ConfigComponent.Load()` 为异步方法。

### 4. 资源释放

每次加载后会立即释放句柄 `handle.Release()`，因为配置数据已经复制到内存。

---

## 🔍 错误处理

### 常见错误

#### 1. 资源包不存在

```
错误: YooAsset 资源包不存在: DefaultPackage
解决: 确保 YooAsset 已初始化，或设置正确的 PackageName
```

#### 2. 资源加载失败

```
错误: YooAsset 加载配置文件失败: UnitConfigCategory.bytes
解决: 
- 检查文件是否存在于 Assets/AssetRaw/Configs/
- 检查文件是否被打包到 AssetBundle
- 检查 YooAsset 资源收集器配置
- 确认可寻址名称是否正确（应为 "文件名.bytes"）
```

#### 3. 资源类型错误

```
错误: YooAsset 加载的资源不是 TextAsset
解决: 确保 .bytes 文件被识别为 TextAsset 类型
```

---

## 📊 性能优化

### 1. 配置文件大小

配置文件使用二进制格式（.bytes），体积小，加载快：

```
UnitConfigCategory.bytes: ~10KB
AIConfigCategory.bytes: ~5KB
ResourceConfigCategory.bytes: ~8KB
```

### 2. 加载时机

建议在游戏启动时加载配置，避免运行时加载：

```csharp
// 在 EntryEvent3 中自动加载
[Event(SceneType.Main)]
public class EntryEvent3_LoadConfig : AEvent<Scene, EntryEvent3>
{
    protected override async ETTask Run(Scene scene, EntryEvent3 args)
    {
        ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
        configComponent.Load();
        await ETTask.CompletedTask;
    }
}
```

### 3. 内存占用

配置数据加载后常驻内存，总内存占用通常 < 1MB。

---

## 🧪 测试建议

### 1. 编辑器测试

```csharp
// 使用 StreamingAssets 模式测试
ConfigLoader.CurrentMode = ConfigLoader.LoadMode.StreamingAssets;
```

### 2. 真机测试

```csharp
// 使用 AssetBundle 模式测试
ConfigLoader.CurrentMode = ConfigLoader.LoadMode.AssetBundle;
```

### 3. 验证加载

```csharp
// 加载配置
ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
configComponent.Load();

// 验证数据
var config = ConfigHelper.UnitConfig.Get(1001);
if (config != null)
{
    Log.Info($"配置加载成功: {config.Name}");
}
else
{
    Log.Error("配置加载失败");
}
```

---

## 📚 相关文档

- **ConfigLoader 源码**: `Module/Config/ConfigLoader.cs`
- **ConfigComponent**: `Module/Config/ConfigComponent.cs`
- **ConfigHelper**: `Module/Config/ConfigHelper.cs`
- **YooAsset 文档**: https://www.yooasset.com/

---

## 🔄 更新记录

### 2024-02-27

**新增功能**:
- ✅ 实现 LoadFromAssetBundle 方法
- ✅ 集成 YooAsset 同步加载
- ✅ 添加 PackageName 配置参数
- ✅ 完善错误处理和日志

**配置变更**:
- ConfigDirectory 默认值改为 `"Configs"`（匹配实际目录）
- CurrentMode 默认值改为 `LoadMode.AssetBundle`（正式环境）

---

**ConfigLoader YooAsset 集成完成！** 🎉
