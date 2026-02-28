# Luban 配置表客户端导出规则

## 📋 概述

本项目使用 Luban 作为配置表工具，支持客户端和服务端分离导出。

---

## 🔧 导出脚本

### 客户端导出

**脚本位置**：
- macOS/Linux: `Tools/Luban/GenConfig_Client.sh`
- Windows: `Tools/Luban/GenConfig_Client.bat`

**执行方式**：
```bash
# macOS/Linux
cd Tools/Luban
./GenConfig_Client.sh

# Windows
cd Tools\Luban
GenConfig_Client.bat
```

### 导出参数说明

```bash
dotnet Luban.dll \
    --customTemplateDir CustomTemplate \  # 自定义模板目录
    -t Client \                           # 导出目标：Client（客户端）
    -c cs-bin \                           # 代码格式：C# + 二进制数据
    -d bin \                              # 数据格式：二进制
    --conf __luban__.conf \               # 配置文件
    -x outputCodeDir=<代码输出目录> \      # C# 代码输出路径
    -x bin.outputDataDir=<数据输出目录> \  # .bytes 数据输出路径
    -x lineEnding=LF                      # 换行符（LF/CRLF）
```

---

## 📂 输出目录

### 修改后的正确路径

#### 代码输出（C# 类）
```
Unity/Assets/GameScripts/HotFix/GameProto/Generate/Config/
├── Tables.cs
├── UnitConfig.cs
├── UnitConfigCategory.cs
├── AIConfig.cs
├── AIConfigCategory.cs
└── ResourceConfig.cs
```

#### 数据输出（.bytes 文件）✅ 已修改
```
Unity/Assets/AssetRaw/Configs/
├── UnitConfigCategory.bytes
├── AIConfigCategory.bytes
└── ResourceConfigCategory.bytes
```

**修改内容**：
- ❌ 旧路径: `Config/Generate/GameConfig/c/`
- ✅ 新路径: `Unity/Assets/AssetRaw/Configs/`

---

## 📝 配置文件结构

### __luban__.conf

位置：`Config/Excel/GameConfig/__luban__.conf`

```json
{
  "groups": [
    {"names":["c"], "default":true},  // c = client（客户端）
    {"names":["s"], "default":true}   // s = server（服务端）
  ],
  "schemaFiles": [
    {"fileName":"Defines", "type":""},
    {"fileName":"__tables__.xlsx", "type":"table"},
    {"fileName":"__beans__.xlsx", "type":"bean"},
    {"fileName":"__enums__.xlsx", "type":"enum"}
  ],
  "dataDir": "./",
  "targets": [
    {"name":"Client", "manager":"Tables", "groups":["c"], "topModule":"ET"},
    {"name":"Server", "manager":"Tables", "groups":["s"], "topModule":"ET"},
    {"name":"All", "manager":"Tables", "groups":["c","s"], "topModule":"ET"}
  ]
}
```

**关键配置**：
- `groups`: 定义分组（c=客户端, s=服务端）
- `targets`: 定义导出目标
  - `Client`: 只导出 `group=c` 的字段
  - `Server`: 只导出 `group=s` 的字段
  - `All`: 导出所有字段

---

## 📊 Excel 表格结构

### 标准格式

以 `UnitConfig.xlsx` 为例：

```
行1: ##var        | 变量标记行
行2: ##type       | 类型定义行
行3: ##           | 分组标记行（可选）
行4: 字段名       | Id | Type | Name | Position | Height | AI
行5: 类型         | int | int | string | int | int | int#ref=AIConfigCategory
行6: 描述         | Id | 类型 | 名字 | 位置 | 身高 | AI配置
行7+: 数据行      | 1001 | 1 | 米克尔 | 100 | 180 | 1
```

### 行说明

| 行号 | 标记 | 说明 | 示例 |
|------|------|------|------|
| 1 | `##var` | 变量标记 | 固定值 |
| 2 | `##type` | 类型定义 | 固定值 |
| 3 | `##` | 分组标记 | 可选，用于标记客户端/服务端 |
| 4 | 字段名 | 字段名称 | Id, Type, Name |
| 5 | 类型 | 数据类型 | int, string, float |
| 6 | 描述 | 字段说明 | 用于生成注释 |
| 7+ | 数据 | 实际数据 | 配置数据 |

---

## 🏷️ 类型标记

### 基础类型

| 类型 | 说明 | 示例 |
|------|------|------|
| `int` | 整数 | 1001 |
| `long` | 长整数 | 10000000 |
| `float` | 浮点数 | 3.14 |
| `bool` | 布尔值 | true/false |
| `string` | 字符串 | "米克尔" |

### 分组标记

| 标记 | 说明 | 导出到 |
|------|------|--------|
| `int` | 默认（客户端+服务端） | 客户端 + 服务端 |
| `int&group=c` | 只客户端 | 客户端 |
| `int&group=s` | 只服务端 | 服务端 |

**示例**：
```
字段名:    Hp          MaxHp       InternalData
类型:      int         int&group=c int&group=s
说明:      当前血量    最大血量    内部数据（客户端不可见）
```

### 引用类型

| 标记 | 说明 | 示例 |
|------|------|------|
| `int#ref=XXXCategory` | 引用其他配置表 | `int#ref=AIConfigCategory` |
| `list,int` | 整数列表 | `[1,2,3]` |
| `map,int,string` | 字典 | `{1:"a", 2:"b"}` |

**引用示例**：
```
字段名: AI
类型:   int#ref=AIConfigCategory
说明:   AI配置ID，会自动生成 AI_Ref 引用字段
```

生成的代码：
```csharp
public readonly int AI;
public AIConfig AI_Ref;  // 自动生成的引用字段
```

---

## 🎯 客户端导出规则

### 1. 字段过滤

**规则**：
- ✅ 无标记字段：导出到客户端和服务端
- ✅ `group=c` 字段：只导出到客户端
- ❌ `group=s` 字段：不导出到客户端

**示例**：
```
字段名:    Id    Name    Hp      ServerData
类型:      int   string  int     int&group=s
客户端:    ✅    ✅      ✅      ❌
服务端:    ✅    ✅      ✅      ✅
```

### 2. 数据格式

**客户端使用二进制格式**：
- 文件格式：`.bytes`
- 优点：体积小、加载快、不易被修改
- 缺点：不可读

**生成的文件**：
```
UnitConfigCategory.bytes      # Unit 配置数据
AIConfigCategory.bytes        # AI 配置数据
ResourceConfigCategory.bytes  # 资源配置数据
```

### 3. 代码生成

**生成的 C# 类**：

```csharp
// UnitConfig.cs
public sealed partial class UnitConfig : Luban.BeanBase
{
    public readonly int Id;
    public readonly int Type;
    public readonly string Name;
    public readonly int Position;
    public readonly int Height;
    public readonly int AI;
    public AIConfig AI_Ref;  // 引用字段
}

// UnitConfigCategory.cs
public partial class UnitConfigCategory
{
    private readonly Dictionary<int, UnitConfig> _dataMap;
    private readonly List<UnitConfig> _dataList;
    
    public UnitConfig Get(int key) => _dataMap[key];
    public UnitConfig GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : null;
}

// Tables.cs
public partial class Tables
{
    public UnitConfigCategory UnitConfigCategory {get; }
    public AIConfigCategory AIConfigCategory {get; }
    public ResourceConfigCategory ResourceConfigCategory {get; }
}
```

---

## 🚀 使用流程

### 1. 编辑配置表

在 `Config/Excel/GameConfig/` 目录下编辑 Excel 文件：
- `UnitConfig.xlsx` - 单位配置
- `AIConfig.xlsx` - AI 配置
- `ResourceConfig.xlsx` - 资源配置

### 2. 执行导出脚本

```bash
cd Tools/Luban
./GenConfig_Client.sh  # macOS/Linux
# 或
GenConfig_Client.bat   # Windows
```

### 3. 验证输出

**检查代码**：
```bash
ls Unity/Assets/GameScripts/HotFix/GameProto/Generate/Config/
# 应该看到：Tables.cs, UnitConfig.cs, UnitConfigCategory.cs 等
```

**检查数据**：
```bash
ls Unity/Assets/AssetRaw/Configs/
# 应该看到：UnitConfigCategory.bytes, AIConfigCategory.bytes 等
```

### 4. 在代码中使用

```csharp
// 加载配置（在 ConfigComponent 中）
ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
configComponent.Load();

// 访问配置
UnitConfig config = ConfigHelper.UnitConfig.Get(1001);
Log.Info($"Unit Name: {config.Name}");

// 使用引用
AIConfig aiConfig = config.AI_Ref;
Log.Info($"AI Name: {aiConfig.Name}");
```

---

## ⚠️ 注意事项

### 1. 不要手动修改生成的代码

生成的代码文件头部有警告：
```csharp
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
```

**如需扩展**：
- ✅ 使用 partial class 在其他文件中扩展
- ❌ 不要直接修改生成的文件

### 2. 配置文件路径

**数据文件必须在正确的位置**：
- ✅ `Unity/Assets/AssetRaw/Configs/` - 正确
- ❌ `Config/Generate/GameConfig/c/` - 错误（旧路径）

### 3. 字段命名规范

- 字段名使用 PascalCase：`MaxHp`, `AttackPower`
- 避免使用 C# 关键字：`class`, `int`, `string` 等
- 引用字段会自动生成 `_Ref` 后缀

### 4. 数据验证

导出后检查：
- 代码是否编译通过
- 数据文件是否生成
- 引用关系是否正确

---

## 📚 常见问题

### Q: 如何添加新的配置表？

1. 在 `Config/Excel/GameConfig/` 创建新的 Excel 文件
2. 在 `__tables__.xlsx` 中注册新表
3. 执行导出脚本
4. 在 `ConfigHelper.cs` 中添加快捷访问属性

### Q: 如何添加只在服务端可见的字段？

在类型列添加 `&group=s` 标记：
```
字段名: ServerOnlyData
类型:   int&group=s
```

### Q: 如何添加引用字段？

使用 `#ref=` 标记：
```
字段名: SkillId
类型:   int#ref=SkillConfigCategory
```

会自动生成：
```csharp
public readonly int SkillId;
public SkillConfig SkillId_Ref;
```

### Q: 配置数据在哪里加载？

在 `ConfigComponent.Load()` 方法中：
```csharp
self.Tables = new Tables(LoadByteBuf);
```

`LoadByteBuf` 函数从 `Unity/Assets/AssetRaw/Configs/` 加载 `.bytes` 文件。

---

## 🔄 修改记录

### 2024-02-27

**修改内容**：
- ✅ 修改 `GenConfig_Client.sh` 数据输出路径
- ✅ 修改 `GenConfig_Client.bat` 数据输出路径
- ✅ 从 `Config/Generate/GameConfig/c/` 改为 `Unity/Assets/AssetRaw/Configs/`

**原因**：
- 配置数据需要在 Unity 项目中才能被加载
- AssetRaw 目录是资源原始文件目录
- 符合项目资源管理规范

---

## 📖 参考资料

- Luban 官方文档: https://luban.doc.code-philosophy.com/
- 项目配置文件: `Config/Excel/GameConfig/__luban__.conf`
- 导出脚本: `Tools/Luban/GenConfig_Client.sh`

---

**配置表导出规则文档完成！** 🎉
