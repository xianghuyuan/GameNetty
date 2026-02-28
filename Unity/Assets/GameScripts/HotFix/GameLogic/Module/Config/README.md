# Luban 配置表管理系统

## 概述

本项目使用 Luban 作为配置表工具，通过 ConfigComponent 和 ConfigHelper 实现配置的加载和管理。

## 架构设计

### 1. ConfigComponent（配置组件）
- 作为 Scene 的组件，管理 Tables 实例
- 负责配置的生命周期管理
- 在组件销毁时自动清理配置引用

### 2. ConfigHelper（配置访问辅助类）
- 提供便捷的静态访问方式
- 不污染 Luban 生成的 Generate 目录
- 支持快捷访问：`ConfigHelper.UnitConfig.Get(id)`

### 3. ConfigLoader（配置加载器）
- 支持多种加载模式：
  - `Resources`：从 Resources 目录加载（开发模式）
  - `StreamingAssets`：从 StreamingAssets 加载
  - `AssetBundle`：从 AssetBundle 加载（正式环境）
  - `PersistentData`：从持久化目录加载（支持热更新）

## 文件结构

```
📁 GameLogic/Module/Config/
├── ConfigComponent.cs          # 配置组件
├── ConfigHelper.cs             # 配置访问辅助类 ⭐
├── ConfigLoader.cs             # 配置加载器
└── EntryEvent3_LoadConfig.cs   # 配置加载事件

📁 GameProto/Generate/Config/
├── Tables.cs                   # Luban 生成的主表类
├── UnitConfigCategory.cs       # Luban 生成的配置类
├── AIConfigCategory.cs
└── ResourceConfigCategory.cs
```

## 使用方法

### 1. 基本使用（推荐）

```csharp
// 通过 ConfigHelper 快捷访问
UnitConfig config = ConfigHelper.UnitConfig.Get(1001);
AIConfig aiConfig = ConfigHelper.AIConfig.Get(100);
ResourceConfig resConfig = ConfigHelper.ResourceConfig.Get(200);
```

### 2. 通过 ConfigComponent 访问

```csharp
// 获取 ConfigComponent
ConfigComponent configComponent = scene.GetComponent<ConfigComponent>();

// 访问配置
UnitConfig config = configComponent.Tables.UnitConfigCategory.Get(1001);
```

### 3. 遍历所有配置

```csharp
// 遍历所有 Unit 配置
foreach (var config in ConfigHelper.UnitConfig.DataList)
{
    Log.Info($"Unit: {config.Id}, Name: {config.Name}");
}

// 使用字典访问
var dataMap = ConfigHelper.UnitConfig.DataMap;
```

## 配置加载流程

1. **启动时自动加载**
   - 在 `EntryEvent3` 事件中自动加载配置
   - 编辑器模式：从 StreamingAssets 加载
   - 正式环境：可配置加载模式

2. **手动加载**
   ```csharp
   ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
   ConfigLoader.CurrentMode = ConfigLoader.LoadMode.StreamingAssets;
   configComponent.Load();
   ```

3. **切换加载模式**
   ```csharp
   // 开发模式
   ConfigLoader.CurrentMode = ConfigLoader.LoadMode.Resources;
   
   // 正式环境
   ConfigLoader.CurrentMode = ConfigLoader.LoadMode.AssetBundle;
   
   // 热更新后
   ConfigLoader.CurrentMode = ConfigLoader.LoadMode.PersistentData;
   ```

## 配置文件路径

### StreamingAssets 模式
```
StreamingAssets/Config/
├── UnitConfigCategory.bytes
├── AIConfigCategory.bytes
└── ResourceConfigCategory.bytes
```

### Resources 模式
```
Resources/Config/
├── UnitConfigCategory.bytes
├── AIConfigCategory.bytes
└── ResourceConfigCategory.bytes
```

### PersistentData 模式（热更新）
```
PersistentDataPath/Config/
├── UnitConfigCategory.bytes
├── AIConfigCategory.bytes
└── ResourceConfigCategory.bytes
```

## 注意事项

1. **不要修改 Generate 目录**
   - Generate 目录由 Luban 自动生成
   - 所有扩展代码都应放在 Module/Config 目录
   - 使用 ConfigHelper 而不是在生成类中添加 Instance 属性

2. **配置生命周期**
   - ConfigHelper.Tables 在 ConfigComponent.Load() 时设置
   - 在 ConfigComponent.Destroy() 时清理
   - 确保在使用前已加载配置

3. **线程安全**
   - 配置表是只读的，可以安全地在多线程中访问
   - 不要在运行时修改配置数据

4. **热重载支持**
   - 如需支持配置热重载，调用 `configComponent.Load()` 重新加载
   - 会自动更新 ConfigHelper 引用

5. **扩展新配置表**
   - Luban 生成新的 ConfigCategory 后
   - 在 ConfigHelper 中添加对应的属性
   - 无需修改 Generate 目录中的任何文件

## 示例代码

### 在 UnitFactory 中使用
```csharp
public static void InitUnitNumeric(BattleUnit unit, int configId)
{
    var config = ConfigHelper.UnitConfig.Get(configId);
    var numeric = unit.GetComponent<NumericComponent>();
    
    numeric.Set(NumericType.MaxHp, config.MaxHp);
    numeric.Set(NumericType.Attack, config.Attack);
}
```

### 在 UnitSystem 中使用
```csharp
public static UnitConfig Config(this Unit self)
{
    return ConfigHelper.UnitConfig.Get(self.ConfigId);
}
```

## 性能优化建议

1. **缓存常用配置**
   ```csharp
   private UnitConfig cachedConfig;
   
   public void Init(int configId)
   {
       cachedConfig = ConfigHelper.UnitConfig.Get(configId);
   }
   ```

2. **批量查询**
   ```csharp
   var configs = ConfigHelper.UnitConfig.DataList
       .Where(c => c.Type == unitType)
       .ToList();
   ```

3. **使用 GetOrDefault 避免异常**
   ```csharp
   var config = ConfigHelper.UnitConfig.GetOrDefault(configId);
   if (config == null)
   {
       Log.Warning($"配置不存在: {configId}");
       return;
   }
   ```

## 添加新配置表

当 Luban 生成新的配置表时，只需在 ConfigHelper 中添加对应属性：

```csharp
public static class ConfigHelper
{
    // 现有配置...
    
    /// <summary>
    /// 新配置表快捷访问
    /// </summary>
    public static NewConfigCategory NewConfig => tables?.NewConfigCategory;
}
```

无需修改 Generate 目录中的任何文件！

