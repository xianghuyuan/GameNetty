# 混合配置方案：Excel + JSON

## 📂 目录结构规划

```
Config/
├── Excel/                          # Excel 配置（策划维护）
│   ├── GameConfig/
│   │   ├── __luban__.conf
│   │   ├── __tables__.xlsx
│   │   ├── UnitConfig.xlsx
│   │   ├── SkillConfig.xlsx
│   │   └── ...
│   └── StartConfig/
│       └── ...
│
├── Json/                           # JSON 配置（程序员维护）✨ 新增
│   ├── __luban__.conf              # JSON 专用配置
│   ├── DebugConfig.json            # 调试配置
│   ├── FeatureToggle.json          # 功能开关
│   ├── ServerList.json             # 服务器列表
│   └── LocalizationOverride.json   # 本地化覆盖
│
└── Generate/                       # 生成的配置文件
    ├── Excel/                      # Excel 生成的
    │   ├── UnitConfigCategory.bytes
    │   └── ...
    └── Json/                       # JSON 生成的
        ├── DebugConfig.bytes
        └── ...
```

---

## 🎯 配置分类

### Excel 配置（策划维护）

**适用场景**：
- ✅ 数值配置（单位、技能、物品）
- ✅ 大量数据（> 50 条）
- ✅ 需要策划频繁修改
- ✅ 有复杂引用关系

**示例**：
- UnitConfig.xlsx - 单位配置
- SkillConfig.xlsx - 技能配置
- ItemConfig.xlsx - 物品配置
- AIConfig.xlsx - AI 配置

---

### JSON 配置（程序员维护）

**适用场景**：
- ✅ 系统配置（调试、开关）
- ✅ 少量数据（< 20 条）
- ✅ 程序员维护
- ✅ 需要灵活修改

**示例**：
- DebugConfig.json - 调试配置
- FeatureToggle.json - 功能开关
- ServerList.json - 服务器列表
- LocalizationOverride.json - 本地化覆盖

---

## 📝 JSON 配置示例

### 1. DebugConfig.json（调试配置）

```json
{
  "configs": [
    {
      "id": 1,
      "name": "ShowFPS",
      "enabled": true,
      "description": "显示 FPS"
    },
    {
      "id": 2,
      "name": "GodMode",
      "enabled": false,
      "description": "无敌模式"
    },
    {
      "id": 3,
      "name": "ShowCollider",
      "enabled": false,
      "description": "显示碰撞体"
    }
  ]
}
```

### 2. FeatureToggle.json（功能开关）

```json
{
  "features": [
    {
      "id": 1,
      "name": "NewBattleSystem",
      "enabled": true,
      "minVersion": "1.0.0",
      "description": "新战斗系统"
    },
    {
      "id": 2,
      "name": "SocialSystem",
      "enabled": false,
      "minVersion": "1.1.0",
      "description": "社交系统"
    }
  ]
}
```

### 3. ServerList.json（服务器列表）

```json
{
  "servers": [
    {
      "id": 1,
      "name": "本地测试",
      "host": "127.0.0.1",
      "port": 10002,
      "enabled": true
    },
    {
      "id": 2,
      "name": "开发服",
      "host": "dev.example.com",
      "port": 10002,
      "enabled": true
    },
    {
      "id": 3,
      "name": "正式服",
      "host": "prod.example.com",
      "port": 10002,
      "enabled": false
    }
  ]
}
```

---

## ⚙️ Luban 配置

### Config/Json/__luban__.conf

```json
{
  "groups": [
    {"names":["c"], "default":true},
    {"names":["s"], "default":true}
  ],
  "schemaFiles": [
    {"fileName":"__tables__.xlsx", "type":"table"}
  ],
  "dataDir": "./",
  "inputDataDir": "./",
  "targets": [
    {"name":"Client", "manager":"JsonTables", "groups":["c"], "topModule":"ET"},
    {"name":"Server", "manager":"JsonTables", "groups":["s"], "topModule":"ET"},
    {"name":"All", "manager":"JsonTables", "groups":["c","s"], "topModule":"ET"}
  ]
}
```

### Config/Json/__tables__.xlsx

| 表名 | 命名空间 | 类型 | 分组 | 说明 |
|------|---------|------|------|------|
| DebugConfig | cfg | table | c | 调试配置 |
| FeatureToggle | cfg | table | c,s | 功能开关 |
| ServerList | cfg | table | c | 服务器列表 |

---

## 🔧 导出脚本

### Tools/Luban/GenConfig_Json.sh

```bash
#!/bin/bash

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

WORKSPACE=../..
LUBAN_DLL=$WORKSPACE/Tools/Luban/LubanRelease/Luban.dll
CONF_ROOT=$WORKSPACE/Config/Json

# 生成客户端 JSON 配置
dotnet $LUBAN_DLL \
    -t Client \
    -c cs-bin \
    -d bin \
    --conf $CONF_ROOT/__luban__.conf \
    -x outputCodeDir=$WORKSPACE/Unity/Assets/GameScripts/HotFix/GameProto/Generate/JsonConfig \
    -x bin.outputDataDir=$WORKSPACE/Unity/Assets/AssetRaw/Configs/Json \
    -x lineEnding=LF

echo "==================== JSON 配置生成完成 ===================="
```

### Tools/Luban/GenConfig_Json.bat

```bat
@echo off
cd /d %~dp0
echo %CD%

set WORKSPACE=..\..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\LubanRelease\Luban.dll
set CONF_ROOT=%WORKSPACE%\Config\Json

:: 生成客户端 JSON 配置
dotnet %LUBAN_DLL% ^
    -t Client ^
    -c cs-bin ^
    -d bin ^
    --conf %CONF_ROOT%\__luban__.conf ^
    -x outputCodeDir=%WORKSPACE%\Unity\Assets\GameScripts\HotFix\GameProto\Generate\JsonConfig ^
    -x bin.outputDataDir=%WORKSPACE%\Unity\Assets\AssetRaw\Configs\Json ^
    -x lineEnding=CRLF

echo ==================== JSON 配置生成完成 ====================
pause
```

---

## 💻 代码集成

### 配置加载器

```csharp
// Unity/Assets/GameScripts/HotFix/GameLogic/Config/ConfigLoader.cs

namespace ET
{
    public static class ConfigLoader
    {
        /// <summary>
        /// 加载所有配置
        /// </summary>
        public static async ETTask LoadAllConfigs(Scene scene)
        {
            // 1. 加载 Excel 配置（游戏数值）
            Tables excelTables = await LoadExcelConfigs();
            
            // 2. 加载 JSON 配置（系统配置）
            JsonTables jsonTables = await LoadJsonConfigs();
            
            // 3. 保存到全局
            ConfigComponent configComponent = scene.AddComponent<ConfigComponent>();
            configComponent.ExcelTables = excelTables;
            configComponent.JsonTables = jsonTables;
            
            Log.Info("所有配置加载完成");
        }
        
        /// <summary>
        /// 加载 Excel 配置
        /// </summary>
        private static async ETTask<Tables> LoadExcelConfigs()
        {
            Tables tables = new Tables((fileName) =>
            {
                TextAsset asset = Resources.Load<TextAsset>($"Configs/{fileName}");
                return new ByteBuf(asset.bytes);
            });
            
            Log.Info("Excel 配置加载完成");
            return tables;
        }
        
        /// <summary>
        /// 加载 JSON 配置
        /// </summary>
        private static async ETTask<JsonTables> LoadJsonConfigs()
        {
            JsonTables tables = new JsonTables((fileName) =>
            {
                TextAsset asset = Resources.Load<TextAsset>($"Configs/Json/{fileName}");
                return new ByteBuf(asset.bytes);
            });
            
            Log.Info("JSON 配置加载完成");
            return tables;
        }
    }
}
```

### ConfigComponent

```csharp
// Server/Model/Demo/Config/ConfigComponent.cs

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class ConfigComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// Excel 配置表（游戏数值）
        /// </summary>
        public Tables ExcelTables { get; set; }
        
        /// <summary>
        /// JSON 配置表（系统配置）
        /// </summary>
        public JsonTables JsonTables { get; set; }
    }
}
```

### 使用示例

```csharp
// 使用 Excel 配置（游戏数值）
UnitConfig unitConfig = ConfigComponent.Instance.ExcelTables.UnitConfigCategory.Get(1001);
Log.Info($"单位: {unitConfig.Name}");

// 使用 JSON 配置（系统配置）
DebugConfig debugConfig = ConfigComponent.Instance.JsonTables.DebugConfigCategory.Get(1);
if (debugConfig.Enabled)
{
    ShowFPS();
}

// 使用功能开关
FeatureToggle feature = ConfigComponent.Instance.JsonTables.FeatureToggleCategory.Get(1);
if (feature.Enabled)
{
    EnableNewBattleSystem();
}
```

---

## 📋 工作流程

### Excel 配置流程（策划）

```
1. 策划编辑 Excel
   ↓
2. 执行 GenConfig_Client.sh
   ↓
3. 生成到 Unity/Assets/AssetRaw/Configs/
   ↓
4. 程序员使用
```

### JSON 配置流程（程序员）

```
1. 程序员编辑 JSON
   ↓
2. 执行 GenConfig_Json.sh
   ↓
3. 生成到 Unity/Assets/AssetRaw/Configs/Json/
   ↓
4. 程序员使用
```

---

## 🎯 配置选择指南

### 使用 Excel 的场景

- ✅ 单位、技能、物品、AI 等游戏数值
- ✅ 数据量大（> 50 条）
- ✅ 策划需要频繁修改
- ✅ 有复杂引用关系

### 使用 JSON 的场景

- ✅ 调试开关、功能开关
- ✅ 服务器列表、环境配置
- ✅ 数据量小（< 20 条）
- ✅ 程序员维护
- ✅ 需要快速修改测试

---

## 🚀 快速开始

### 1. 创建目录结构

```bash
mkdir -p Config/Json
mkdir -p Unity/Assets/AssetRaw/Configs/Json
mkdir -p Unity/Assets/GameScripts/HotFix/GameProto/Generate/JsonConfig
```

### 2. 创建 JSON 配置文件

```bash
# 创建 DebugConfig.json
cat > Config/Json/DebugConfig.json << 'EOF'
{
  "configs": [
    {"id": 1, "name": "ShowFPS", "enabled": true, "description": "显示FPS"}
  ]
}
EOF
```

### 3. 创建 Luban 配置

```bash
# 创建 __luban__.conf
# 创建 __tables__.xlsx
```

### 4. 执行导出

```bash
cd Tools/Luban
./GenConfig_Json.sh
```

### 5. 在代码中使用

```csharp
await ConfigLoader.LoadAllConfigs(scene);
DebugConfig config = ConfigComponent.Instance.JsonTables.DebugConfigCategory.Get(1);
```

---

## ⚠️ 注意事项

### 1. 命名规范

- Excel 配置：`UnitConfig.xlsx` → `UnitConfigCategory`
- JSON 配置：`DebugConfig.json` → `DebugConfigCategory`

### 2. 避免重名

Excel 和 JSON 的配置表名不要重复：
- ❌ Excel: `ServerConfig.xlsx` + JSON: `ServerConfig.json`
- ✅ Excel: `GameServerConfig.xlsx` + JSON: `LoginServerConfig.json`

### 3. 分离管理类

```csharp
public class ConfigComponent
{
    public Tables ExcelTables;      // Excel 配置
    public JsonTables JsonTables;   // JSON 配置
}
```

---

**创建日期**: 2026-03-06
**作者**: Droid
**版本**: v1.0
