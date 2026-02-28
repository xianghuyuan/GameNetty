# ConfigLoader YooAsset 使用说明

## 概述

ConfigLoader 使用 YooAsset 加载配置文件，配置加载会自动等待 YooAsset 初始化完成。

---

## 🔧 配置加载流程

### 自动加载（推荐）

配置会在 `EntryEvent3` 事件中自动加载：

```csharp
[Event(SceneType.Main)]
public class EntryEvent3_LoadConfig : AEvent<Scene, EntryEvent3>
{
    protected override async ETTask Run(Scene root, EntryEvent3 args)
    {
        // 1. 等待 YooAsset 初始化完成
        var package = YooAssets.GetPackage(ConfigLoader.PackageName);
        while (package.InitializeStatus == EOperationStatus.Processing)
        {
            await TimerComponent.Instance.WaitAsync(100);
        }
        
        // 2. 加载配置
        ConfigComponent configComponent = root.AddComponent<ConfigComponent>();
        configComponent.Load();
    }
}
```

**流程**：
1. ✅ 检查 YooAsset 资源包是否存在
2. ✅ 等待 YooAsset 初始化完成
3. ✅ 加载所有配置表
4. ✅ 设置 ConfigHelper 单例

---

## 📋 前置条件

### 1. YooAsset 初始化

确保在 `EntryEvent3` 之前初始化 YooAsset：

```csharp
// 在 EntryEvent1 或 EntryEvent2 中初始化
[Event(SceneType.Main)]
public class EntryEvent1_InitYooAsset : AEvent<Scene, EntryEvent1>
{
    protected override async ETTask Run(Scene root, EntryEvent1 args)
    {
        // 创建资源包
        var package = YooAssets.CreatePackage("DefaultPackage");
        YooAssets.SetDefaultPackage(package);
        
        // 初始化参数（根据实际情况选择）
        InitializeParameters initParameters;
        
#if UNITY_EDITOR
        // 编辑器模式
        var editorSimulateModeParameters = new EditorSimulateModeParameters();
        editorSimulateModeParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
        initParameters = editorSimulateModeParameters;
#else
        // 正式环境
        var offlinePlayModeParameters = new OfflinePlayModeParameters();
        initParameters = offlinePlayModeParameters;
#endif
        
        // 初始化
        var initOperation = package.InitializeAsync(initParameters);
        await initOperation.Task;
        
        if (initOperation.Status == EOperationStatus.Succeed)
        {
            Log.Info("YooAsset 初始化成功");
        }
        else
        {
            Log.Error($"YooAsset 初始化失败: {initOperation.Error}");
        }
    }
}
```

### 2. 配置文件准备

#### 导出配置

```bash
cd Tools/Luban
./GenConfig_Client.sh  # macOS/Linux
# 或
GenConfig_Client.bat   # Windows
```

配置文件会导出到：
```
Unity/Assets/AssetRaw/Configs/
├── UnitConfigCategory.bytes
├── AIConfigCategory.bytes
└── ResourceConfigCategory.bytes
```

#### YooAsset 资源收集器配置

在 YooAsset 资源收集器中添加配置文件：

1. 打开 YooAsset 资源收集器窗口
2. 添加收集规则：
   - **收集路径**: `Assets/AssetRaw/Configs`
   - **收集器类型**: 收集所有资源
   - **可寻址规则**: 文件名（不含扩展名）
   - **打包规则**: 按文件夹打包

3. 确保配置文件可寻址名称为：
   - `UnitConfigCategory.bytes`
   - `AIConfigCategory.bytes`
   - `ResourceConfigCategory.bytes`

---

## 🚀 使用方式

### 访问配置

```csharp
// 通过 ConfigHelper 访问（推荐）
var unitConfig = ConfigHelper.UnitConfig.Get(1001);
var aiConfig = ConfigHelper.AIConfig.Get(100);

// 遍历配置
foreach (var config in ConfigHelper.UnitConfig.DataList)
{
    Log.Info($"Unit: {config.Id}, Name: {config.Name}");
}
```

### 手动加载配置

如果需要在其他时机加载配置：

```csharp
// 确保 YooAsset 已初始化
var package = YooAssets.GetPackage("DefaultPackage");
if (package.InitializeStatus == EOperationStatus.Succeed)
{
    ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
    configComponent.Load();
}
```

---

## ⚠️ 注意事项

### 1. 加载顺序

**正确的初始化顺序**：
```
EntryEvent1: 初始化 YooAsset
    ↓
EntryEvent2: 其他初始化
    ↓
EntryEvent3: 加载配置（自动等待 YooAsset）
```

### 2. 资源包名称

默认使用 `"DefaultPackage"`，如果使用其他名称：

```csharp
// 在加载配置前设置
ConfigLoader.PackageName = "YourPackageName";
```

### 3. 可寻址名称

配置文件的可寻址名称必须是 `文件名.bytes`：

```
正确: UnitConfigCategory.bytes
错误: Assets/AssetRaw/Configs/UnitConfigCategory.bytes
```

### 4. 编辑器模式

在编辑器中使用 YooAsset 的 `EditorSimulateMode`：

```csharp
var editorSimulateModeParameters = new EditorSimulateModeParameters();
editorSimulateModeParameters.SimulateManifestFilePath = 
    EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
```

这样可以在编辑器中直接加载资源，无需打包。

---

## 🔍 错误排查

### 错误 1: Package initialize not completed

**原因**: YooAsset 未初始化完成

**解决**: 
- 检查 YooAsset 是否在 EntryEvent1/2 中初始化
- 确认初始化成功
- EntryEvent3_LoadConfig 会自动等待初始化完成

### 错误 2: YooAsset 资源包不存在

**原因**: 资源包未创建或名称不匹配

**解决**:
```csharp
// 检查资源包名称
ConfigLoader.PackageName = "DefaultPackage"; // 确保与创建的包名一致
```

### 错误 3: 加载配置文件失败

**原因**: 配置文件未设置为可寻址或未打包

**解决**:
1. 检查 YooAsset 资源收集器配置
2. 确认配置文件在 `Assets/AssetRaw/Configs/`
3. 确认可寻址名称正确
4. 重新构建 AssetBundle（编辑器模式会自动处理）

---

## 📊 配置参数

```csharp
// 加载模式（固定为 AssetBundle）
ConfigLoader.CurrentMode = ConfigLoader.LoadMode.AssetBundle;

// 资源包名称（默认：DefaultPackage）
ConfigLoader.PackageName = "DefaultPackage";

// 配置目录（仅用于日志显示）
ConfigLoader.ConfigDirectory = "Configs";
```

---

## 📚 相关文档

- **配置系统**: `Module/Config/README.md`
- **ConfigHelper**: `Module/Config/ConfigHelper.cs`
- **Luban 导出**: `Tools/Luban/CONFIG_EXPORT_RULES.md`
- **YooAsset 官方文档**: https://www.yooasset.com/

---

## 🔄 更新记录

### 2024-02-27

**更新内容**：
- ✅ 移除 StreamingAssets 模式
- ✅ 统一使用 YooAsset 加载
- ✅ 自动等待 YooAsset 初始化完成
- ✅ 简化配置加载流程

**修改文件**：
- `EntryEvent3_LoadConfig.cs` - 添加 YooAsset 初始化等待逻辑
- `ConfigLoader.cs` - 增强错误检查和提示

---

**配置加载现在完全基于 YooAsset，确保在初始化完成后自动加载！** ✅
