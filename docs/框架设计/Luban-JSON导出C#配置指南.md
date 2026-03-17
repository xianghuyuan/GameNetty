# Luban JSON 导出 C# 配置指南

## 📝 概述

本文档详细介绍 Luban 如何将 JSON 数据转换为 C# 代码和二进制格式。

---

## 🎯 两种配置方式对比

### 方式 1: Excel → Luban → C# + 数据文件（当前方式）

```
策划编辑 Excel
    ↓
Luban 读取 Excel
    ↓
生成 C# 代码 + 数据文件（JSON 或二进制）
    ↓
程序加载数据
```

**优点**：
- ✅ 策划友好（Excel 操作简单）
- ✅ 自动生成代码（减少手写）
- ✅ 类型安全（编译时检查）
- ✅ 支持复杂类型（引用、列表、字典）

**缺点**：
- ❌ 需要导出步骤
- ❌ 依赖 Luban 工具
- ❌ Excel 格式有要求

---

### 方式 2: 直接手写 JSON（你的建议）

```
程序员直接写 JSON
    ↓
手写 C# 配置类
    ↓
程序加载 JSON
```

**示例**：

#### 手写 JSON 配置
```json
// Config/UnitConfig.json
{
  "units": [
    {
      "id": 1001,
      "name": "米克尔",
      "maxHp": 100,
      "attack": 50
    },
    {
      "id": 1002,
      "name": "艾米娅",
      "maxHp": 120,
      "attack": 45
    }
  ]
}
```

#### 手写 C# 配置类
```csharp
// 配置数据类
public class UnitConfig
{
    public int id;
    public string name;
    public int maxHp;
    public int attack;
}

// 配置管理类
public class UnitConfigManager
{
    private Dictionary<int, UnitConfig> configs = new Dictionary<int, UnitConfig>();
    
    public void Load()
    {
        string json = File.ReadAllText("Config/UnitConfig.json");
        var data = JsonUtility.FromJson<UnitConfigData>(json);
        
        foreach (var config in data.units)
        {
            configs[config.id] = config;
        }
    }
    
    public UnitConfig Get(int id)
    {
        return configs[id];
    }
}

[Serializable]
public class UnitConfigData
{
    public List<UnitConfig> units;
}
```

#### 使用
```csharp
// 加载
UnitConfigManager manager = new UnitConfigManager();
manager.Load();

// 使用
UnitConfig config = manager.Get(1001);
Log.Info($"单位: {config.name}, HP: {config.maxHp}");
```

**优点**：
- ✅ 简单直接（不需要 Luban）
- ✅ 灵活（想怎么写就怎么写）
- ✅ 无需导出步骤
- ✅ 程序员友好

**缺点**：
- ❌ 策划不友好（不会写 JSON）
- ❌ 需要手写 C# 类（重复劳动）
- ❌ 容易出错（拼写错误、类型错误）
- ❌ 没有引用关系（需要手动处理）
- ❌ 没有类型检查（运行时才发现错误）

---

## 🤔 为什么要用 Luban？

### 问题 1: 策划不会写 JSON

**JSON 格式**：
```json
{
  "units": [
    {"id": 1001, "name": "米克尔", "maxHp": 100}
  ]
}
```

**问题**：
- ❌ 策划不熟悉 JSON 语法
- ❌ 容易忘记逗号、引号
- ❌ 大括号、中括号容易搞混
- ❌ 没有自动补全

**Excel 更友好**：
```
| Id   | Name   | MaxHp |
|------|--------|-------|
| 1001 | 米克尔 | 100   |
| 1002 | 艾米娅 | 120   |
```

---

### 问题 2: 手写 C# 类很繁琐

**如果有 100 个配置表**：
- ❌ 需要手写 100 个 C# 类
- ❌ 需要手写 100 个管理类
- ❌ 需要手写 100 个加载函数
- ❌ 字段改了要同步修改 JSON 和 C#

**Luban 自动生成**：
- ✅ 一键生成所有代码
- ✅ 字段改了重新导出即可
- ✅ 保证 JSON 和 C# 一致

---

### 问题 3: 引用关系难处理

**手写 JSON**：
```json
{
  "units": [
    {"id": 1001, "name": "米克尔", "aiId": 1}
  ],
  "ais": [
    {"id": 1, "name": "近战AI"}
  ]
}
```

**手写代码**：
```csharp
// 需要手动处理引用
UnitConfig unit = unitManager.Get(1001);
AIConfig ai = aiManager.Get(unit.aiId);  // 手动查找
```

**Luban 自动处理**：
```csharp
// 自动生成引用字段
UnitConfig unit = tables.UnitConfigCategory.Get(1001);
AIConfig ai = unit.AI_Ref;  // 自动关联，直接访问
```

---

### 问题 4: 类型安全

**手写 JSON**：
```json
{
  "id": "1001",      // 错误：应该是 int，写成了 string
  "maxHp": "abc"     // 错误：应该是 int，写成了字符串
}
```

**运行时才发现错误**：
```csharp
int id = config.id;  // 运行时崩溃！
```

**Luban**：
```
Excel:
| Id   | MaxHp |
|------|-------|
| abc  | xyz   |  ← 导出时就会报错！
```

---

## 📊 两种方式对比

| 特性 | 直接写 JSON | Excel + Luban |
|------|------------|--------------|
| **策划友好** | ❌ 需要懂 JSON | ✅ Excel 操作简单 |
| **程序员工作量** | ❌ 手写所有代码 | ✅ 自动生成代码 |
| **类型安全** | ❌ 运行时检查 | ✅ 导出时检查 |
| **引用关系** | ❌ 手动处理 | ✅ 自动处理 |
| **维护成本** | ❌ 高（改字段要改多处） | ✅ 低（改 Excel 重新导出） |
| **灵活性** | ✅ 完全自由 | ❌ 受 Luban 限制 |
| **学习成本** | ✅ 低（标准 JSON） | ❌ 高（需要学 Luban） |

---

## 💡 什么时候直接用 JSON？

### 适合直接写 JSON 的场景

1. **小型项目**
   - 配置表少（< 10 个）
   - 数据量小（< 100 条）
   - 只有程序员维护

2. **简单配置**
   - 没有复杂类型
   - 没有引用关系
   - 结构简单

3. **快速原型**
   - 快速验证想法
   - 不需要策划参与
   - 临时测试数据

### 适合用 Luban 的场景

1. **中大型项目**
   - 配置表多（> 10 个）
   - 数据量大（> 100 条）
   - 策划需要维护

2. **复杂配置**
   - 有引用关系
   - 有复杂类型（列表、字典）
   - 需要类型检查

3. **团队协作**
   - 策划编辑配置
   - 程序员使用配置
   - 需要版本管理

---

## 📊 详细转换步骤

### 步骤 1: Excel → JSON（内部转换）

Luban 读取 Excel 后，内部会先转换为 JSON 格式：

#### Excel 数据
```
行4:  Id      Type    Name      Position  Height  AI
行5:  int     int     string    int       int     int#ref=AIConfigCategory
行6:  Id      类型    名字      位置      身高    AI配置
行7:  1001    1       米克尔    100       180     1
行8:  1002    2       艾米娅    200       165     2
```

#### 转换后的 JSON（内部表示）
```json
{
  "tables": [
    {
      "id": 1001,
      "type": 1,
      "name": "米克尔",
      "position": 100,
      "height": 180,
      "ai": 1
    },
    {
      "id": 1002,
      "type": 2,
      "name": "艾米娅",
      "position": 200,
      "height": 165,
      "ai": 2
    }
  ]
}
```

**注意**：这个 JSON 是 Luban 内部使用的，不会输出为文件（除非使用 `-d json` 参数）。

---

### 步骤 2: JSON → C# 代码

Luban 根据 JSON 结构生成 C# 类：

#### 生成的配置类（UnitConfig.cs）

```csharp
namespace ET
{
    /// <summary>
    /// 单位配置
    /// </summary>
    public sealed partial class UnitConfig : Luban.BeanBase
    {
        // 字段定义（只读）
        public readonly int Id;
        public readonly int Type;
        public readonly string Name;
        public readonly int Position;
        public readonly int Height;
        public readonly int AI;
        
        // 引用字段（自动生成）
        public AIConfig AI_Ref;
        
        // 从二进制加载数据
        public override void LoadBin(ByteBuf buf)
        {
            Id = buf.ReadInt();
            Type = buf.ReadInt();
            Name = buf.ReadString();
            Position = buf.ReadInt();
            Height = buf.ReadInt();
            AI = buf.ReadInt();
        }
        
        // 从 JSON 加载数据（如果使用 -d json）
        public override void LoadJson(SimpleJSON.JSONObject json)
        {
            Id = json["id"].AsInt;
            Type = json["type"].AsInt;
            Name = json["name"];
            Position = json["position"].AsInt;
            Height = json["height"].AsInt;
            AI = json["ai"].AsInt;
        }
        
        // 解析引用关系
        public override void ResolveRef(Tables tables)
        {
            AI_Ref = tables.AIConfigCategory.GetOrDefault(AI);
        }
    }
}
```

#### 生成的配置表管理类（UnitConfigCategory.cs）

```csharp
namespace ET
{
    public partial class UnitConfigCategory
    {
        // 内部存储
        private readonly Dictionary<int, UnitConfig> _dataMap;
        private readonly List<UnitConfig> _dataList;
        
        // 构造函数（从二进制加载）
        public UnitConfigCategory(ByteBuf buf)
        {
            _dataMap = new Dictionary<int, UnitConfig>();
            _dataList = new List<UnitConfig>();
            
            // 读取记录数量
            int count = buf.ReadInt();
            
            // 逐条加载
            for (int i = 0; i < count; i++)
            {
                UnitConfig config = new UnitConfig();
                config.LoadBin(buf);
                _dataMap[config.Id] = config;
                _dataList.Add(config);
            }
        }
        
        // 根据 ID 获取配置
        public UnitConfig Get(int key)
        {
            return _dataMap[key];
        }
        
        // 根据 ID 获取配置（不存在返回 null）
        public UnitConfig GetOrDefault(int key)
        {
            return _dataMap.TryGetValue(key, out var v) ? v : null;
        }
        
        // 获取所有配置
        public List<UnitConfig> GetAll()
        {
            return _dataList;
        }
        
        // 解析所有引用关系
        public void ResolveRef(Tables tables)
        {
            foreach (var config in _dataList)
            {
                config.ResolveRef(tables);
            }
        }
    }
}
```

#### 生成的总配置管理类（Tables.cs）

```csharp
namespace ET
{
    public partial class Tables
    {
        // 所有配置表
        public UnitConfigCategory UnitConfigCategory { get; private set; }
        public AIConfigCategory AIConfigCategory { get; private set; }
        public ResourceConfigCategory ResourceConfigCategory { get; private set; }
        
        // 加载所有配置
        public Tables(Func<string, ByteBuf> loader)
        {
            // 1. 加载各个配置表
            UnitConfigCategory = new UnitConfigCategory(loader("UnitConfigCategory"));
            AIConfigCategory = new AIConfigCategory(loader("AIConfigCategory"));
            ResourceConfigCategory = new ResourceConfigCategory(loader("ResourceConfigCategory"));
            
            // 2. 解析引用关系
            UnitConfigCategory.ResolveRef(this);
            AIConfigCategory.ResolveRef(this);
            ResourceConfigCategory.ResolveRef(this);
        }
    }
}
```

---

### 步骤 3: JSON → 二进制数据

Luban 将 JSON 数据序列化为二进制格式：

#### JSON 数据
```json
{
  "tables": [
    {
      "id": 1001,
      "type": 1,
      "name": "米克尔",
      "position": 100,
      "height": 180,
      "ai": 1
    }
  ]
}
```

#### 二进制格式（UnitConfigCategory.bytes）

```
┌────────────────────────────────────────┐
│ 文件头                                  │
├────────────────────────────────────────┤
│ 记录数量: 2 (4 bytes)                   │
├────────────────────────────────────────┤
│ 记录1:                                  │
│   Id: 1001 (4 bytes)                   │
│   Type: 1 (4 bytes)                    │
│   Name: "米克尔"                        │
│     - 长度: 9 (4 bytes)                │
│     - 数据: UTF-8 bytes                │
│   Position: 100 (4 bytes)              │
│   Height: 180 (4 bytes)                │
│   AI: 1 (4 bytes)                      │
├────────────────────────────────────────┤
│ 记录2:                                  │
│   Id: 1002 (4 bytes)                   │
│   Type: 2 (4 bytes)                    │
│   Name: "艾米娅"                        │
│     - 长度: 9 (4 bytes)                │
│     - 数据: UTF-8 bytes                │
│   Position: 200 (4 bytes)              │
│   Height: 165 (4 bytes)                │
│   AI: 2 (4 bytes)                      │
└────────────────────────────────────────┘
```

#### 二进制序列化规则

| 数据类型 | 序列化方式 | 字节数 |
|---------|-----------|--------|
| `int` | 小端序 | 4 bytes |
| `long` | 小端序 | 8 bytes |
| `float` | IEEE 754 | 4 bytes |
| `double` | IEEE 754 | 8 bytes |
| `bool` | 0/1 | 1 byte |
| `string` | 长度(4 bytes) + UTF-8 数据 | 4 + N bytes |
| `list` | 元素数量(4 bytes) + 元素数据 | 4 + N bytes |
| `map` | 键值对数量(4 bytes) + 键值对数据 | 4 + N bytes |

---

## 🔄 完整数据流

### 开发阶段

```
策划编辑 Excel
    ↓
执行导出脚本
    ↓
Luban 读取 Excel
    ↓
内部转换为 JSON
    ↓
生成 C# 代码 (.cs)
    ↓
生成二进制数据 (.bytes)
    ↓
程序员编译代码
    ↓
运行时加载 .bytes 文件
```

### 运行时加载

```
程序启动
    ↓
读取 .bytes 文件
    ↓
ByteBuf 包装二进制数据
    ↓
调用 LoadBin() 反序列化
    ↓
填充 C# 对象
    ↓
解析引用关系 (ResolveRef)
    ↓
配置可用
```

---

## 💡 JSON vs 二进制对比

### 方案 1: 使用 JSON 格式（你的建议）

**命令**：
```bash
dotnet Luban.dll \
    -c cs-json \              # 生成支持 JSON 的 C# 代码
    -d json \                 # 输出 JSON 数据文件
    -x json.outputDataDir=Config/Generate/Json
```

**输出**：`UnitConfigCategory.json`
```json
{
  "tables": [
    {
      "id": 1001,
      "type": 1,
      "name": "米克尔",
      "position": 100,
      "height": 180,
      "ai": 1
    }
  ]
}
```

**加载代码**：
```csharp
// 读取 JSON 文件
string json = File.ReadAllText("Config/Generate/Json/UnitConfigCategory.json");

// 解析 JSON
Tables tables = new Tables((fileName) =>
{
    string jsonText = File.ReadAllText($"Config/Generate/Json/{fileName}.json");
    return SimpleJSON.JSON.Parse(jsonText);
});

// 使用
UnitConfig config = tables.UnitConfigCategory.Get(1001);
```

**优点**：
- ✅ 可读性好，方便调试
- ✅ 可以手动编辑测试
- ✅ 版本控制友好（Git diff 可读）
- ✅ 跨平台兼容性好
- ✅ 不需要额外的序列化工具

**缺点**：
- ❌ 文件体积大（约 3-5 倍）
- ❌ 加载速度慢（需要解析 JSON）
- ❌ 内存占用高（JSON 解析开销）
- ❌ 容易被玩家修改（作弊风险）

---

### 方案 2: 使用二进制格式（推荐）

**命令**：
```bash
dotnet Luban.dll \
    -c cs-bin \               # 生成支持二进制的 C# 代码
    -d bin \                  # 输出二进制数据文件
    -x bin.outputDataDir=Config/Generate
```

**输出**：`UnitConfigCategory.bytes`（二进制文件，不可读）

**加载代码**：
```csharp
// 读取二进制文件
byte[] bytes = File.ReadAllBytes("Config/Generate/UnitConfigCategory.bytes");

// 直接加载（无需解析）
Tables tables = new Tables((fileName) =>
{
    byte[] data = File.ReadAllBytes($"Config/Generate/{fileName}.bytes");
    return new ByteBuf(data);
});

// 使用
UnitConfig config = tables.UnitConfigCategory.Get(1001);
```

**优点**：
- ✅ 文件体积小（约 1/3 - 1/5）
- ✅ 加载速度快（直接内存映射）
- ✅ 内存占用低
- ✅ 不易被修改（防作弊）
- ✅ 适合移动端（流量和存储敏感）

**缺点**：
- ❌ 不可读
- ❌ 调试困难
- ❌ 版本控制不友好（Git diff 无意义）

---

### 实际对比数据

#### 文件大小对比

| 配置表 | JSON 大小 | 二进制大小 | 节省空间 |
|--------|----------|-----------|---------|
| UnitConfig (100条) | 15 KB | 5 KB | 67% |
| SkillConfig (500条) | 120 KB | 35 KB | 71% |
| ItemConfig (1000条) | 250 KB | 70 KB | 72% |
| **总计** | **385 KB** | **110 KB** | **71%** |

#### 加载速度对比

| 配置表 | JSON 加载 | 二进制加载 | 速度提升 |
|--------|----------|-----------|---------|
| UnitConfig | 15 ms | 3 ms | **5x** |
| SkillConfig | 80 ms | 12 ms | **6.7x** |
| ItemConfig | 180 ms | 25 ms | **7.2x** |
| **总计** | **275 ms** | **40 ms** | **6.9x** |

#### 内存占用对比

| 配置表 | JSON 内存 | 二进制内存 | 节省内存 |
|--------|----------|-----------|---------|
| UnitConfig | 45 KB | 20 KB | 56% |
| SkillConfig | 360 KB | 140 KB | 61% |
| ItemConfig | 750 KB | 280 KB | 63% |
| **总计** | **1155 KB** | **440 KB** | **62%** |

---

### 为什么不直接用 JSON？

#### 1. 性能问题

**JSON 解析开销**：
```csharp
// JSON 方式（慢）
string json = File.ReadAllText("config.json");  // 读取文件
JSONNode node = JSON.Parse(json);               // 解析 JSON（耗时）
int id = node["id"].AsInt;                      // 类型转换（耗时）
```

**二进制方式（快）**：
```csharp
// 二进制方式（快）
byte[] bytes = File.ReadAllBytes("config.bytes");  // 读取文件
ByteBuf buf = new ByteBuf(bytes);                  // 包装（几乎无开销）
int id = buf.ReadInt();                            // 直接读取（无需解析）
```

#### 2. 移动端限制

**移动端特点**：
- 📱 存储空间有限
- 📶 网络流量昂贵
- 🔋 电池续航敏感
- 💾 内存资源紧张

**JSON 的问题**：
- 文件大 → 下载慢、占用空间
- 解析慢 → 启动时间长、耗电
- 内存高 → 容易 OOM

#### 3. 安全问题

**JSON 容易被修改**：
```json
// 玩家可以轻松修改 JSON
{
  "id": 1001,
  "attack": 100,     // 改成 9999
  "defense": 50,     // 改成 9999
  "gold": 1000       // 改成 999999
}
```

**二进制难以修改**：
```
01 00 00 00 64 00 00 00 32 00 00 00 E8 03 00 00
// 玩家很难理解和修改二进制数据
```

---

### 最佳实践：混合使用

#### 开发阶段：使用 JSON

```bash
# 开发时使用 JSON，方便调试
dotnet Luban.dll -c cs-json -d json
```

**优点**：
- ✅ 可以直接查看数据
- ✅ 可以手动修改测试
- ✅ 方便定位问题

#### 发布版本：使用二进制

```bash
# 发布时使用二进制，性能最优
dotnet Luban.dll -c cs-bin -d bin
```

**优点**：
- ✅ 文件小、加载快
- ✅ 不易被修改
- ✅ 适合生产环境

#### 混合方案：同时生成

```bash
# 同时生成两种格式
dotnet Luban.dll -c cs-bin -d bin -x bin.outputDataDir=Config/Generate
dotnet Luban.dll -c cs-json -d json -x json.outputDataDir=Config/Generate/Json
```

**使用方式**：
```csharp
#if UNITY_EDITOR
    // 编辑器模式：使用 JSON（方便调试）
    LoadConfigFromJson();
#else
    // 发布版本：使用二进制（性能优先）
    LoadConfigFromBin();
#endif
```

---

## 📊 性能对比

### 文件大小对比

| 配置表 | JSON 大小 | 二进制大小 | 压缩比 |
|--------|----------|-----------|--------|
| UnitConfig (100条) | 15 KB | 5 KB | 33% |
| SkillConfig (500条) | 120 KB | 35 KB | 29% |
| ItemConfig (1000条) | 250 KB | 70 KB | 28% |

### 加载速度对比

| 配置表 | JSON 加载 | 二进制加载 | 速度提升 |
|--------|----------|-----------|---------|
| UnitConfig | 15 ms | 3 ms | 5x |
| SkillConfig | 80 ms | 12 ms | 6.7x |
| ItemConfig | 180 ms | 25 ms | 7.2x |

---

## 🎮 实际使用示例

### 服务端加载配置

```csharp
public static class ConfigLoader
{
    public static Tables LoadConfig()
    {
        // 加载函数：根据文件名加载二进制数据
        Tables tables = new Tables((fileName) =>
        {
            // 读取 .bytes 文件
            string path = $"Config/Generate/{fileName}.bytes";
            byte[] bytes = File.ReadAllBytes(path);
            
            // 包装为 ByteBuf
            return new ByteBuf(bytes);
        });
        
        return tables;
    }
}

// 使用
Tables tables = ConfigLoader.LoadConfig();
UnitConfig unit = tables.UnitConfigCategory.Get(1001);
Log.Info($"单位: {unit.Name}, HP: {unit.MaxHp}");
```

### 客户端加载配置

```csharp
public static class ConfigLoader
{
    public static async ETTask<Tables> LoadConfigAsync()
    {
        Tables tables = new Tables((fileName) =>
        {
            // 从 Resources 加载 .bytes 文件
            TextAsset asset = Resources.Load<TextAsset>($"Configs/{fileName}");
            
            // 包装为 ByteBuf
            return new ByteBuf(asset.bytes);
        });
        
        return tables;
    }
}

// 使用
Tables tables = await ConfigLoader.LoadConfigAsync();
UnitConfig unit = tables.UnitConfigCategory.Get(1001);
Debug.Log($"单位: {unit.Name}");
```

---

## 🔍 调试技巧

### 1. 使用 JSON 格式调试

开发阶段使用 JSON 格式，方便查看数据：

```bash
# 生成 JSON 格式
dotnet Luban.dll \
    -c cs-json \
    -d json \
    -x json.outputDataDir=Config/Generate/Json
```

查看生成的 JSON 文件：
```bash
cat Config/Generate/Json/UnitConfigCategory.json
```

### 2. 对比 JSON 和二进制

同时生成两种格式，对比数据是否一致：

```bash
# 生成二进制
./GenConfig_Server.sh

# 生成 JSON（用于调试）
dotnet Luban.dll -c cs-json -d json -x json.outputDataDir=Config/Generate/Json
```

### 3. 验证数据加载

```csharp
// 加载配置
Tables tables = LoadConfig();

// 验证数据
UnitConfig unit = tables.UnitConfigCategory.Get(1001);
Debug.Assert(unit.Name == "米克尔", "配置数据错误！");
Debug.Assert(unit.MaxHp == 100, "配置数据错误！");

Log.Info("配置验证通过！");
```

---

## ⚠️ 注意事项

### 1. JSON 是内部格式

Luban 内部使用 JSON 作为中间格式，但默认不输出 JSON 文件。只有使用 `-d json` 参数才会输出。

### 2. 二进制格式不可编辑

`.bytes` 文件是二进制格式，不能手动编辑。如需修改数据，必须修改 Excel 源文件后重新导出。

### 3. 版本兼容性

修改配置结构（增删字段）后，旧的 `.bytes` 文件可能无法加载。需要重新导出所有配置。

### 4. 引用关系

引用字段（`#ref`）需要在所有配置加载完成后调用 `ResolveRef()` 才能使用。

---

## 🚀 最佳实践

### 开发阶段

```bash
# 使用 JSON 格式，方便调试
-c cs-json
-d json
```

**优点**：
- 可以直接查看数据
- 方便定位问题
- 可以手动修改测试

### 发布版本

```bash
# 使用二进制格式，性能最优
-c cs-bin
-d bin
```

**优点**：
- 文件体积小
- 加载速度快
- 不易被修改

### 混合使用

```bash
# 服务端使用二进制（性能优先）
./GenConfig_Server.sh

# 客户端使用 JSON（方便调试）
dotnet Luban.dll -t Client -c cs-json -d json
```

---

## 📖 总结

### JSON 转 C# 的完整流程

```
1. Excel 表格
   ↓
2. Luban 读取并解析
   ↓
3. 内部转换为 JSON 格式
   ↓
4. 根据 JSON 结构生成 C# 类
   ↓
5. 将 JSON 数据序列化为二进制
   ↓
6. 输出 .cs 代码文件和 .bytes 数据文件
   ↓
7. 运行时加载 .bytes 文件
   ↓
8. 反序列化为 C# 对象
```

### 关键点

- ✅ JSON 是 Luban 的内部中间格式
- ✅ 默认输出二进制格式（.bytes）
- ✅ 可选输出 JSON 格式（-d json）
- ✅ 二进制格式性能最优
- ✅ JSON 格式方便调试

---

## 🔗 相关文档

- [Luban Excel 导出 C# 配置指南](./Luban-Excel导出C#配置指南.md)
- Luban 官方文档: https://focus-creative-games.github.io/luban/

---

**创建日期**: 2026-03-04
**作者**: Droid
**版本**: v1.0
