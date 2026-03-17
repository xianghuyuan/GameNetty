# Luban Excel 导出 C# 配置指南

## 📝 概述

本文档介绍如何使用 Luban 将 Excel 配置表导出为 C# 代码和二进制数据。

---

## 🚀 快速开始

### 一键导出

```bash
cd /Users/gxx/Documents/UGit/GameNetty/Tools/Luban

# 生成服务端配置
./GenConfig_Server.sh    # Mac/Linux
GenConfig_Server.bat     # Windows

# 生成客户端配置
./GenConfig_Client.sh    # Mac/Linux
GenConfig_Client.bat     # Windows
```

---

## 📂 项目结构

### Excel 配置文件位置

```
Config/Excel/GameConfig/
├── __luban__.conf          # Luban 配置文件
├── __tables__.xlsx         # 表定义文件
├── __beans__.xlsx          # Bean 定义文件
├── __enums__.xlsx          # 枚举定义文件
├── UnitConfig.xlsx         # 单位配置
├── AIConfig.xlsx           # AI 配置
└── ResourceConfig.xlsx     # 资源配置
```

### 输出目录

#### 服务端
```
Server/Model/Generate/Config/
├── Tables.cs               # 配置表管理类
├── UnitConfig.cs           # 单位配置类
├── UnitConfigCategory.cs   # 单位配置表管理
└── ...

Config/Generate/
├── UnitConfigCategory.bytes    # 单位配置二进制数据
├── AIConfigCategory.bytes      # AI 配置二进制数据
└── ...
```

#### 客户端
```
Unity/Assets/GameScripts/HotFix/GameProto/Generate/Config/
├── Tables.cs
├── UnitConfig.cs
└── ...

Unity/Assets/AssetRaw/Configs/
├── UnitConfigCategory.bytes
├── AIConfigCategory.bytes
└── ...
```

---

## 📊 Excel 表格结构

### 标准格式

以 `UnitConfig.xlsx` 为例：

| 行号 | 内容 | 说明 | 示例 |
|------|------|------|------|
| 1 | `##var` | 变量标记行（固定值） | `##var` |
| 2 | `##type` | 类型定义行（固定值） | `##type` |
| 3 | `##` | 分组标记行（可选） | `##` |
| 4 | 字段名 | 字段名称 | `Id`, `Type`, `Name`, `MaxHp` |
| 5 | 类型 | 数据类型 | `int`, `string`, `int` |
| 6 | 描述 | 字段说明 | `Id`, `类型`, `名字`, `最大血量` |
| 7+ | 数据 | 实际配置数据 | `1001`, `1`, `米克尔`, `100` |

### 完整示例

```
行1:  ##var
行2:  ##type
行3:  ##
行4:  Id      Type    Name      Position  Height  AI
行5:  int     int     string    int       int     int#ref=AIConfigCategory
行6:  Id      类型    名字      位置      身高    AI配置
行7:  1001    1       米克尔    100       180     1
行8:  1002    2       艾米娅    200       165     2
行9:  1003    1       德克萨斯  300       170     3
```

---

## 🏷️ 字段类型详解

### 基础类型

| Excel 类型 | C# 类型 | 说明 | 示例值 |
|-----------|---------|------|--------|
| `int` | `int` | 32位整数 | `1001` |
| `long` | `long` | 64位整数 | `10000000` |
| `float` | `float` | 单精度浮点 | `3.14` |
| `double` | `double` | 双精度浮点 | `3.14159` |
| `bool` | `bool` | 布尔值 | `true`, `false` |
| `string` | `string` | 字符串 | `"米克尔"` |

### 分组标记（客户端/服务端分离）

| Excel 类型 | 说明 | 导出到 |
|-----------|------|--------|
| `int` | 默认（客户端+服务端） | Client + Server |
| `int&group=c` | 只客户端 | Client |
| `int&group=s` | 只服务端 | Server |

**示例**：
```
字段名:    Id      Name    MaxHp       ServerData
类型:      int     string  int&group=c int&group=s
描述:      Id      名字    最大血量    服务端数据
客户端:    ✅      ✅      ✅          ❌
服务端:    ✅      ✅      ✅          ✅
```

**生成的客户端代码**：
```csharp
public sealed partial class UnitConfig
{
    public readonly int Id;
    public readonly string Name;
    public readonly int MaxHp;
    // ServerData 不会生成
}
```

**生成的服务端代码**：
```csharp
public sealed partial class UnitConfig
{
    public readonly int Id;
    public readonly string Name;
    public readonly int MaxHp;
    public readonly int ServerData;  // 只在服务端有
}
```

### 引用类型

| Excel 类型 | 说明 | 生成代码 |
|-----------|------|---------|
| `int#ref=AIConfigCategory` | 引用 AI 配置表 | `public readonly int AI;`<br>`public AIConfig AI_Ref;` |

**示例**：
```
字段名: AI
类型:   int#ref=AIConfigCategory
数据:   1
```

**生成的代码**：
```csharp
public sealed partial class UnitConfig
{
    public readonly int AI;           // 存储 AI 配置 ID
    public AIConfig AI_Ref;           // 自动生成的引用字段
}
```

**使用方式**：
```csharp
UnitConfig unit = tables.UnitConfigCategory.Get(1001);
AIConfig ai = unit.AI_Ref;  // 直接访问引用的 AI 配置
Log.Info($"AI 名称: {ai.Name}");
```

### 集合类型

| Excel 类型 | C# 类型 | 示例数据 |
|-----------|---------|---------|
| `list,int` | `List<int>` | `[1,2,3]` |
| `list,string` | `List<string>` | `["a","b","c"]` |
| `map,int,string` | `Dictionary<int,string>` | `{1:"a", 2:"b"}` |
| `list,int#ref=SkillConfigCategory` | `List<int>` + 引用 | `[1,2,3]` |

**示例**：
```
字段名: Skills
类型:   list,int#ref=SkillConfigCategory
数据:   [1,2,3]
```

**生成的代码**：
```csharp
public readonly List<int> Skills;
public List<SkillConfig> Skills_Ref;  // 自动生成的引用列表
```

---

## 📝 配置文件详解

### __luban__.conf

**位置**: `Config/Excel/GameConfig/__luban__.conf`

```json
{
  "groups": [
    {"names":["c"], "default":true},  // c = client（客户端）
    {"names":["s"], "default":true}   // s = server（服务端）
  ],
  "schemaFiles": [
    {"fileName":"Defines", "type":""},              // 自定义类型定义
    {"fileName":"__tables__.xlsx", "type":"table"}, // 表定义
    {"fileName":"__beans__.xlsx", "type":"bean"},   // Bean定义
    {"fileName":"__enums__.xlsx", "type":"enum"}    // 枚举定义
  ],
  "dataDir": "./",  // 数据文件目录
  "targets": [
    {"name":"Client", "manager":"Tables", "groups":["c"], "topModule":"ET"},
    {"name":"Server", "manager":"Tables", "groups":["s"], "topModule":"ET"},
    {"name":"All", "manager":"Tables", "groups":["c","s"], "topModule":"ET"}
  ]
}
```

**关键配置说明**：

| 配置项 | 说明 | 示例 |
|--------|------|------|
| `groups` | 定义分组标识 | `c`=客户端, `s`=服务端 |
| `schemaFiles` | 定义配置文件列表 | 表、Bean、枚举定义 |
| `dataDir` | 数据文件所在目录 | `./` 表示当前目录 |
| `targets` | 导出目标配置 | Client/Server/All |

**targets 详解**：

```json
{
  "name": "Client",           // 目标名称（对应 -t 参数）
  "manager": "Tables",        // 生成的管理类名
  "groups": ["c"],            // 包含的分组（只导出 group=c 的字段）
  "topModule": "ET"           // 顶层命名空间
}
```

---

## 🔧 导出命令详解

### 服务端导出

```bash
dotnet Luban.dll \
    -t All \                    # 导出所有表
    -c cs-bin \                 # C# 代码 + 二进制数据
    -d bin \                    # 数据格式：二进制
    --conf Config/Excel/GameConfig/__luban__.conf \
    -x outputCodeDir=Server/Model/Generate/Config \
    -x bin.outputDataDir=Config/Generate/
```

### 客户端导出

```bash
dotnet Luban.dll \
    -t Client \                 # 只导出客户端表
    -c cs-bin \                 # C# 代码 + 二进制数据
    -d bin \                    # 数据格式：二进制
    --conf Config/Excel/GameConfig/__luban__.conf \
    -x outputCodeDir=Unity/Assets/GameScripts/HotFix/GameProto/Generate/Config \
    -x bin.outputDataDir=Unity/Assets/AssetRaw/Configs
```

### 参数说明

| 参数 | 值 | 说明 |
|------|-----|------|
| `-t` | `Client` | 只导出客户端字段（group=c） |
| | `Server` | 只导出服务端字段（group=s） |
| | `All` | 导出所有字段 |
| `-c` | `cs-bin` | C# 代码 + 二进制数据（推荐） |
| | `cs-json` | C# 代码 + JSON 数据（调试用） |
| `-d` | `bin` | 二进制格式（体积小，速度快） |
| | `json` | JSON 格式（可读，调试方便） |
| `--conf` | 配置文件路径 | `__luban__.conf` 位置 |
| `-x outputCodeDir` | C# 代码输出目录 | 生成的 .cs 文件位置 |
| `-x bin.outputDataDir` | 二进制数据输出目录 | 生成的 .bytes 文件位置 |

---

## 🎮 在代码中使用

### 服务端加载配置

```csharp
// 加载配置
public static async ETTask LoadConfig(Scene scene)
{
    Tables tables = new Tables((file) =>
    {
        byte[] bytes = File.ReadAllBytes($"Config/Generate/{file}.bytes");
        return new ByteBuf(bytes);
    });
    
    // 保存到全局
    scene.AddComponent<ConfigComponent>().Tables = tables;
}

// 使用配置
UnitConfig config = ConfigComponent.Instance.Tables.UnitConfigCategory.Get(1001);
Log.Info($"单位名称: {config.Name}, 血量: {config.MaxHp}");

// 使用引用
AIConfig ai = config.AI_Ref;
Log.Info($"AI 名称: {ai.Name}");
```

### 客户端加载配置

```csharp
// 加载配置
public static async ETTask LoadConfig()
{
    Tables tables = new Tables((file) =>
    {
        TextAsset asset = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>($"Configs/{file}");
        return new ByteBuf(asset.bytes);
    });
    
    ConfigHelper.Tables = tables;
}

// 使用配置
UnitConfig config = ConfigHelper.Tables.UnitConfigCategory.Get(1001);
Debug.Log($"单位名称: {config.Name}");

// 使用引用
AIConfig ai = config.AI_Ref;
Debug.Log($"AI 名称: {ai.Name}");
```

---

## 📋 完整工作流程

### 1. 编辑 Excel 配置

在 `Config/Excel/GameConfig/` 目录下编辑 Excel 文件：

```
UnitConfig.xlsx:
行4:  Id      Name      MaxHp
行5:  int     string    int
行6:  Id      名字      最大血量
行7:  1001    米克尔    100
行8:  1002    艾米娅    120
```

### 2. 执行导出脚本

```bash
cd Tools/Luban
./GenConfig_Server.sh  # 生成服务端配置
./GenConfig_Client.sh  # 生成客户端配置
```

### 3. 验证输出

**检查代码**：
```bash
ls Server/Model/Generate/Config/
# 应该看到：Tables.cs, UnitConfig.cs, UnitConfigCategory.cs
```

**检查数据**：
```bash
ls Config/Generate/
# 应该看到：UnitConfigCategory.bytes
```

### 4. 在代码中使用

```csharp
// 加载
Tables tables = new Tables(LoadByteBuf);

// 使用
UnitConfig config = tables.UnitConfigCategory.Get(1001);
Log.Info($"单位: {config.Name}, HP: {config.MaxHp}");
```

---

## ⚠️ 注意事项

### 1. Excel 格式要求

- ✅ 前3行必须是 `##var`, `##type`, `##`
- ✅ 第4行是字段名（PascalCase）
- ✅ 第5行是字段类型
- ✅ 第6行是字段描述
- ✅ 第7行开始是数据

### 2. 字段命名规范

- ✅ 使用 PascalCase：`MaxHp`, `AttackPower`
- ❌ 避免使用 C# 关键字：`class`, `int`, `string`
- ✅ 引用字段会自动生成 `_Ref` 后缀

### 3. 分组使用

```
int              → 客户端和服务端都有
int&group=c      → 只在客户端
int&group=s      → 只在服务端（客户端看不到）
```

### 4. 不要手动修改生成的代码

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
- ✅ 使用 `partial class` 在其他文件中扩展
- ❌ 不要直接修改生成的文件

---

## 🔍 常见问题

### Q1: 如何添加新的配置表？

1. 在 `Config/Excel/GameConfig/` 创建新的 Excel 文件（如 `SkillConfig.xlsx`）
2. 按照标准格式填写表头和数据
3. 在 `__tables__.xlsx` 中注册新表
4. 执行导出脚本

### Q2: 如何添加只在服务端可见的字段？

在类型列添加 `&group=s` 标记：
```
字段名: ServerOnlyData
类型:   int&group=s
```

### Q3: 如何添加引用字段？

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

### Q4: 如何调试配置数据？

使用 JSON 格式导出：
```bash
dotnet Luban.dll \
    -c cs-json \              # 改为 JSON 格式
    -d json \                 # 数据也用 JSON
    -x json.outputDataDir=Config/Generate/Json
```

### Q5: 生成失败怎么办？

1. 检查 Excel 格式是否正确（前3行标记）
2. 检查字段类型是否正确
3. 检查 `__luban__.conf` 配置是否正确
4. 查看错误日志

---

## 📊 性能对比

| 格式 | 文件大小 | 加载速度 | 内存占用 | 可读性 |
|------|---------|---------|---------|--------|
| **bin** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ❌ |
| **json** | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ✅ |

**推荐**：
- **开发阶段**：使用 JSON 格式，方便调试
- **发布版本**：使用 bin 格式，性能最优

---

## 🔗 相关文档

- [Luban JSON 导出 C# 配置指南](./Luban-JSON导出C#配置指南.md)
- Luban 官方文档: https://focus-creative-games.github.io/luban/

---

**创建日期**: 2026-03-04
**作者**: Droid
**版本**: v1.0
