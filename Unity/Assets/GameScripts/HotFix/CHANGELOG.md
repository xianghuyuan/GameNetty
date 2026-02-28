# 项目修改总结

## 修改日期
2024-02-27

## 修改内容

### 一、UnitFactory 客户端适配

#### 1. 创建 UnitFactory_Battle.cs
**位置**: `GameLogic/Module/Unit/UnitFactory_Battle.cs`

**功能**: 将 Battle 相关代码分离到独立文件
- `CreateHero()` - 创建玩家英雄
- `CreateMonster()` - 创建怪物
- `InitUnitNumeric()` - 初始化数值

#### 2. 修改 UnitFactory.cs
**位置**: `GameLogic/Module/Unit/UnitFactory.cs`

**新增功能**: 添加客户端专用的 `Create(Scene, UnitInfo)` 方法
- 从服务器消息 `UnitInfo` 创建完整的 Unit
- 自动初始化位置、朝向、类型
- 从 KV 字典加载所有数值属性
- 根据 UnitType 自动添加必要组件

**保留功能**: 原有的 `Create(Scene, long, UnitType)` 方法（服务端使用）

---

### 二、Luban 配置表管理系统（修正版）

#### 问题
之前错误地在 `Generate/Config/` 目录手动添加了 Singleton 文件，这个目录是 Luban 自动生成的，会被覆盖。

#### 解决方案
使用 **ConfigHelper** 模式，在业务逻辑目录提供配置访问。

#### 1. 创建 ConfigHelper.cs ⭐核心
**位置**: `GameLogic/Module/Config/ConfigHelper.cs`

**功能**: 配置访问辅助类
```csharp
// 使用方式
var config = ConfigHelper.UnitConfig.Get(1001);
var aiConfig = ConfigHelper.AIConfig.Get(100);
var resConfig = ConfigHelper.ResourceConfig.Get(200);
```

**优势**:
- ✅ 不污染 Generate 目录
- ✅ Luban 重新生成不受影响
- ✅ 提供便捷的静态访问
- ✅ 易于扩展新配置表

#### 2. 创建 ConfigComponent.cs
**位置**: `GameLogic/Module/Config/ConfigComponent.cs`

**功能**: 配置管理组件
- 管理 Tables 实例生命周期
- 加载配置时设置 ConfigHelper.Tables
- 销毁时清理配置引用

#### 3. 创建 ConfigLoader.cs
**位置**: `GameLogic/Module/Config/ConfigLoader.cs`

**功能**: 配置加载器，支持多种加载模式
- `Resources` - 从 Resources 加载（开发）
- `StreamingAssets` - 从 StreamingAssets 加载（默认）
- `AssetBundle` - 从 AssetBundle 加载（正式环境）
- `PersistentData` - 从持久化目录加载（热更新）

#### 4. 创建 EntryEvent3_LoadConfig.cs
**位置**: `GameLogic/Module/Config/EntryEvent3_LoadConfig.cs`

**功能**: 在启动时自动加载配置
- 监听 EntryEvent3 事件
- 根据环境自动选择加载模式
- 初始化 ConfigComponent

#### 5. 修改现有代码使用 ConfigHelper
- **UnitSystem.cs**: `UnitConfigCategory.Instance` → `ConfigHelper.UnitConfig`
- **UnitFactory_Battle.cs**: `UnitConfigCategory.Instance` → `ConfigHelper.UnitConfig`

---

## 文件清单

### 新增文件（7个）

```
📁 GameLogic/Module/Config/
├── ConfigComponent.cs          ✅ 配置组件
├── ConfigHelper.cs             ✅ 配置访问辅助类（核心）
├── ConfigLoader.cs             ✅ 配置加载器
├── EntryEvent3_LoadConfig.cs   ✅ 配置加载事件
└── README.md                   ✅ 使用文档

📁 GameLogic/Module/Unit/
└── UnitFactory_Battle.cs       ✅ Battle 相关代码
```

### 修改文件（3个）

```
📁 GameLogic/Module/Unit/
├── UnitFactory.cs              🔧 添加客户端 Create 方法
├── UnitSystem.cs               🔧 使用 ConfigHelper
└── UnitFactory_Battle.cs       🔧 使用 ConfigHelper
```

### 删除文件（4个）❌

```
📁 GameProto/Generate/Config/
├── Tables_Singleton.cs         ❌ 已删除（不应在 Generate 目录）
├── UnitConfigCategory_Singleton.cs  ❌ 已删除
├── AIConfigCategory_Singleton.cs    ❌ 已删除
└── ResourceConfigCategory_Singleton.cs  ❌ 已删除
```

---

## 使用示例

### 1. 客户端创建 Unit
```csharp
// SceneChangeHelper 中自动使用新方法
M2C_CreateMyUnit m2CCreateMyUnit = waitCreateMyUnit.Message;
Unit unit = UnitFactory.Create(currentScene, m2CCreateMyUnit.Unit);
```

### 2. 访问配置表
```csharp
// 方式1：通过 ConfigHelper（推荐）
var unitConfig = ConfigHelper.UnitConfig.Get(1001);
var aiConfig = ConfigHelper.AIConfig.Get(100);

// 方式2：通过 ConfigComponent
var config = scene.GetComponent<ConfigComponent>().Tables.UnitConfigCategory.Get(1001);

// 方式3：遍历配置
foreach (var cfg in ConfigHelper.UnitConfig.DataList)
{
    Log.Info($"Unit: {cfg.Id}, Name: {cfg.Name}");
}
```

### 3. 添加新配置表
当 Luban 生成新配置表时，只需在 ConfigHelper 添加属性：

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

---

## 架构优势

### UnitFactory 改进
1. ✅ **代码分离**: Battle 和客户端逻辑分离，职责清晰
2. ✅ **完整支持**: 从服务器消息创建完整 Unit
3. ✅ **向后兼容**: 保留原有方法，不影响现有代码
4. ✅ **自动适配**: SceneChangeHelper 自动使用新方法

### 配置管理改进
1. ✅ **不污染生成代码**: 所有扩展在业务逻辑目录
2. ✅ **Luban 友好**: 重新生成配置不受影响
3. ✅ **便捷访问**: `ConfigHelper.UnitConfig.Get(id)` 简洁明了
4. ✅ **易于扩展**: 新增配置表只需添加一个属性
5. ✅ **符合 ET 框架**: 使用 ConfigComponent 管理生命周期
6. ✅ **多种加载模式**: 支持开发、正式、热更新等场景

---

## 注意事项

### 重要提醒
1. **不要修改 Generate 目录**: 该目录由 Luban 自动生成，会被覆盖
2. **使用 ConfigHelper**: 所有配置访问都通过 ConfigHelper
3. **配置文件路径**: 确保配置文件放在正确的目录（默认 StreamingAssets/Config/）
4. **启动顺序**: ConfigComponent 在 EntryEvent3 时自动加载

### 扩展指南
- 添加新配置表：在 ConfigHelper 添加属性
- 修改加载模式：设置 `ConfigLoader.CurrentMode`
- 热重载配置：调用 `configComponent.Load()`

---

## 测试建议

1. **测试客户端创建 Unit**
   - 验证从服务器消息创建 Unit
   - 检查位置、朝向、数值是否正确
   - 确认组件是否正确添加

2. **测试配置加载**
   - 验证配置在启动时正确加载
   - 测试 ConfigHelper 访问配置
   - 检查配置热重载功能

3. **测试 Battle 功能**
   - 验证 CreateHero 和 CreateMonster
   - 确认 Battle 相关代码正常工作

---

## 完成状态

✅ 所有修改已完成
✅ 代码已验证
✅ 文档已更新
✅ 架构清晰合理

项目现在拥有一个健壮、易维护的配置管理系统和完善的 Unit 创建机制！
