# ConfigLoader TEngine 集成说明

## 概述

项目使用 TEngine 的 ResourceModule 来管理 YooAsset，配置加载依赖 TEngine 的初始化流程。

---

## 🔧 TEngine ResourceModule

### 自动初始化

TEngine 会在游戏启动时自动初始化 YooAsset：

1. **ResourceModuleDriver** 组件（挂在 GameEntry 上）
2. 自动创建 YooAsset 资源包
3. 根据配置选择运行模式（编辑器/离线/联机）

### 配置位置

在 Unity 编辑器中找到 `GameEntry` 预制体：
- 路径：通常在 `Assets/` 根目录
- 组件：`ResourceModuleDriver`
- 配置：
  - **Default Package Name**: `DefaultPackage`
  - **Play Mode**: 运行模式
  - **Host Server URL**: 资源服务器地址

---

## 📋 配置加载流程

### 当前实现

```csharp
[Event(SceneType.Main)]
public class EntryEvent3_LoadConfig : AEvent<Scene, EntryEvent3>
{
    protected override async ETTask Run(Scene root, EntryEvent3 args)
    {
        // 1. 尝试获取资源包（TEngine 已创建）
        var package = YooAssets.TryGetPackage(ConfigLoader.PackageName);
        
        if (package == null)
        {
            Log.Warning("YooAsset 资源包不存在，配置加载失败");
            return;
        }
        
        // 2. 等待初始化完成（最多 10 秒）
        int waitCount = 0;
        while (package.InitializeStatus == EOperationStatus.Processing && waitCount < 100)
        {
            await root.GetComponent<TimerComponent>().WaitAsync(100);
            waitCount++;
        }
        
        // 3. 检查初始化状态
        if (package.InitializeStatus != EOperationStatus.Succeed)
        {
            Log.Error($"YooAsset 初始化失败: {package.InitializeStatus}");
            return;
        }
        
        // 4. 加载配置
        ConfigComponent configComponent = root.AddComponent<ConfigComponent>();
        configComponent.Load();
    }
}
```

---

## ⚠️ 错误：YooAsset 初始化失败

### 错误信息

```
[ERROR] YooAsset 初始化失败: None，无法加载配置
```

### 原因分析

1. **TEngine ResourceModule 未启动**
   - GameEntry 预制体未加载
   - ResourceModuleDriver 组件未激活

2. **初始化时机问题**
   - EntryEvent3 触发时 TEngine 还未初始化完成
   - 需要调整初始化顺序

3. **资源包名称不匹配**
   - ConfigLoader.PackageName 与 TEngine 配置不一致

---

## ✅ 解决方案

### 方案 1：确保 TEngine 初始化顺序

检查游戏启动流程：

```
1. Unity Awake/Start
   ↓
2. TEngine ModuleSystem 初始化
   ↓
3. ResourceModule 初始化 YooAsset
   ↓
4. ET Framework 启动
   ↓
5. EntryEvent1/2/3
   ↓
6. 配置加载（EntryEvent3_LoadConfig）
```

**确保 TEngine 在 ET Framework 之前初始化！**

### 方案 2：检查 GameEntry 配置

1. 找到 `GameEntry` 预制体或场景对象
2. 检查 `ResourceModuleDriver` 组件：
   - ✅ 组件已启用
   - ✅ Default Package Name = `"DefaultPackage"`
   - ✅ Play Mode 已正确设置

### 方案 3：延迟配置加载

如果 TEngine 初始化较慢，可以延迟配置加载：

```csharp
// 在需要配置时再加载，而不是在 EntryEvent3
public static async ETTask EnsureConfigLoaded(Scene scene)
{
    var configComponent = scene.GetComponent<ConfigComponent>();
    if (configComponent != null)
    {
        return; // 已加载
    }
    
    // 等待 YooAsset 就绪
    var package = YooAssets.TryGetPackage("DefaultPackage");
    while (package == null || package.InitializeStatus != EOperationStatus.Succeed)
    {
        await TimerComponent.Instance.WaitAsync(100);
        package = YooAssets.TryGetPackage("DefaultPackage");
    }
    
    // 加载配置
    configComponent = scene.AddComponent<ConfigComponent>();
    configComponent.Load();
}
```

### 方案 4：使用 TEngine 的资源加载接口

如果 YooAsset 初始化问题持续，可以考虑使用 TEngine 的资源加载接口：

```csharp
// 通过 TEngine ResourceModule 加载
var resourceModule = ModuleSystem.GetModule<IResourceModule>();
var handle = resourceModule.LoadAssetSync<TextAsset>(assetPath);
```

---

## 🔍 调试步骤

### 1. 检查 TEngine 是否初始化

在 EntryEvent3_LoadConfig 开始处添加日志：

```csharp
Log.Info("=== 开始加载配置 ===");
Log.Info($"YooAssets 是否初始化: {YooAssets.Initialized}");

var package = YooAssets.TryGetPackage("DefaultPackage");
if (package != null)
{
    Log.Info($"资源包状态: {package.InitializeStatus}");
}
else
{
    Log.Error("资源包不存在！");
}
```

### 2. 检查初始化顺序

在 TEngine ResourceModule 初始化完成后添加日志：

```csharp
// ResourceModule.cs 或 ResourceModuleDriver.cs
Log.Info("TEngine ResourceModule 初始化完成");
Log.Info($"Package: {DefaultPackageName}, Status: {package.InitializeStatus}");
```

### 3. 检查资源包名称

确认名称一致：

```csharp
// ConfigLoader.cs
public static string PackageName { get; set; } = "DefaultPackage";

// TEngine ResourceModuleDriver
Default Package Name = "DefaultPackage"
```

---

## 📊 TEngine 运行模式

### EditorSimulateMode（编辑器模拟）

- 不需要打包 AssetBundle
- 直接从 Assets 目录加载
- 适合开发调试

### OfflinePlayMode（离线模式）

- 从 StreamingAssets 加载
- 适合单机游戏或首包资源

### HostPlayMode（联机模式）

- 从远程服务器下载
- 支持热更新
- 适合线上运行

---

## 📝 配置文件准备

### 1. 导出配置

```bash
cd Tools/Luban
./GenConfig_Client.sh
```

配置文件输出到：
```
Unity/Assets/AssetRaw/Configs/
├── UnitConfigCategory.bytes
├── AIConfigCategory.bytes
└── ResourceConfigCategory.bytes
```

### 2. YooAsset 资源收集器

确保配置文件被收集：
- 路径：`Assets/AssetRaw/Configs/`
- 可寻址：是
- 打包规则：按需配置

### 3. 构建 AssetBundle（正式环境）

使用 TEngine 的构建工具或 YooAsset 构建窗口构建资源。

---

## 🚀 推荐配置

### 开发阶段

```
TEngine ResourceModuleDriver:
- Play Mode: EditorSimulateMode
- Default Package Name: DefaultPackage

优点：
- 无需打包
- 修改配置后重新导出即可
- 调试方便
```

### 正式环境

```
TEngine ResourceModuleDriver:
- Play Mode: OfflinePlayMode 或 HostPlayMode
- Default Package Name: DefaultPackage
- Host Server URL: 你的CDN地址

优点：
- 支持热更新
- 资源加密
- 版本管理
```

---

## 📚 相关文档

- **TEngine 文档**: `Assets/TEngine/README.md`
- **YooAsset 文档**: https://www.yooasset.com/
- **ConfigLoader**: `Module/Config/ConfigLoader.cs`
- **Luban 导出**: `Tools/Luban/CONFIG_EXPORT_RULES.md`

---

## 🔄 更新记录

### 2024-02-27

**修改内容**：
- ✅ 使用 `YooAssets.TryGetPackage` 兼容 TEngine
- ✅ 添加初始化超时检测（10秒）
- ✅ 改善错误提示信息
- ✅ 添加 TEngine 集成说明

**修改文件**：
- `EntryEvent3_LoadConfig.cs` - 兼容 TEngine 初始化

---

**配置加载现在兼容 TEngine ResourceModule，请确保 TEngine 在 ET Framework 之前初始化！** ✅
