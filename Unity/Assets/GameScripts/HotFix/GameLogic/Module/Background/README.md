# 横版视差滚动背景系统

## 📖 简介

基于Material UV偏移的高性能视差滚动背景系统，支持多层背景以不同速度滚动产生景深效果。

## ✨ 特性

- ✅ **高性能**：使用Material UV偏移，无需移动GameObject
- ✅ **视差效果**：支持多层背景不同速度滚动
- ✅ **跟随玩家**：根据玩家移动速度动态调整
- ✅ **平滑过渡**：速度变化平滑自然
- ✅ **暂停控制**：支持游戏暂停时停止背景
- ✅ **可视化配置**：Inspector面板直观调整参数
- ✅ **无限循环**：自动无缝循环，无需手动处理

## 📁 文件结构

```
Assets/GameScripts/HotFix/GameLogic/Module/Background/
├── ParallaxLayer.cs          # 单层背景控制
├── ParallaxBackground.cs     # 多层管理器
└── README.md                 # 本文档
```

## 🚀 快速开始

### 1. 场景设置

#### 步骤1：创建背景管理器
1. 在Hierarchy中创建空GameObject，命名为 `ParallaxBackground`
2. 添加 `ParallaxBackground` 组件

#### 步骤2：创建背景层
为每一层背景创建GameObject（推荐使用Quad或Sprite）：

```
ParallaxBackground (根对象)
├── Layer_Far (远景 - 天空/云)
├── Layer_Middle (中景 - 山脉/建筑)
└── Layer_Near (近景 - 树木/地面)
```

#### 步骤3：添加ParallaxLayer组件
为每个背景层GameObject添加 `ParallaxLayer` 组件

#### 步骤4：配置材质
**重要**：背景材质必须正确设置才能无限循环

1. 选择背景贴图（Texture）
2. 在Inspector中设置：
   - **Wrap Mode**: `Repeat`（必须！）
   - **Filter Mode**: `Bilinear` 或 `Point`（根据美术风格）

3. 创建材质（Material）：
   - **Shader**: 选择 `Unlit/Texture` 或 `Sprites/Default`
   - **Tiling**: X轴设置为 2-4（根据需要调整）

4. 将材质赋给背景层的Renderer

### 2. 参数配置

#### ParallaxLayer（单层）参数

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| Target Renderer | 背景渲染器 | 自动获取 |
| Scroll Speed Multiplier | 滚动速度倍率 | 远景0.1，中景0.3，近景1.0 |
| Scroll Direction | 滚动方向 | (-1, 0) 向左 |
| Auto Get Renderer | 自动获取Renderer | ✓ |

#### ParallaxBackground（管理器）参数

| 参数 | 说明 | 推荐值 |
|------|------|--------|
| Layers | 所有背景层数组 | 自动查找或手动拖拽 |
| Global Speed Multiplier | 全局速度倍率 | 1.0 |
| Enable Smooth Transition | 启用速度平滑过渡 | ✓ |
| Transition Smoothness | 过渡平滑度 | 5.0 |
| Auto Find Layers | 自动查找子对象 | ✓ |

### 3. 代码集成

#### 基础用法

```csharp
using GameLogic;

public class PlayerController : MonoBehaviour
{
    private ParallaxBackground parallaxBg;
    
    void Start()
    {
        // 查找背景管理器
        parallaxBg = FindObjectOfType<ParallaxBackground>();
    }
    
    void Update()
    {
        // 获取玩家速度
        float playerSpeed = GetComponent<Rigidbody2D>().velocity.x;
        
        // 更新背景滚动
        parallaxBg.SetPlayerSpeed(playerSpeed);
    }
}
```

#### 暂停/恢复

```csharp
// 游戏暂停时
parallaxBg.PauseAll();

// 恢复游戏时
parallaxBg.ResumeAll();
```

#### 动态调整速度

```csharp
// 进入加速状态
parallaxBg.SetGlobalSpeedMultiplier(2.0f);

// 恢复正常速度
parallaxBg.SetGlobalSpeedMultiplier(1.0f);
```

#### 重置背景

```csharp
// 场景切换或重新开始时
parallaxBg.ResetAll();
```

## 🎮 典型配置示例

### 横版跑酷游戏

```
远景层（天空）：
- Speed Multiplier: 0.1
- 几乎不动，营造远距离感

中景层（山脉）：
- Speed Multiplier: 0.3
- 中等速度

近景层（地面）：
- Speed Multiplier: 1.0
- 与玩家速度同步

Global Speed Multiplier: 0.8
```

### 横版射击游戏

```
远景层（星空）：
- Speed Multiplier: 0.05
- 极慢速度

中景层（星云）：
- Speed Multiplier: 0.2

近景层（陨石带）：
- Speed Multiplier: 0.5

Global Speed Multiplier: 1.5
```

## 🔧 高级用法

### 单独控制某一层

```csharp
// 获取指定层
ParallaxLayer layer = parallaxBg.GetLayer(0);

// 修改该层速度
layer.SetSpeedMultiplier(0.5f);

// 修改该层方向
layer.SetScrollDirection(new Vector2(-1, 0.2f)); // 斜向滚动
```

### 动态添加背景层

```csharp
// 创建新的背景层GameObject
GameObject newLayer = new GameObject("DynamicLayer");
newLayer.transform.SetParent(parallaxBg.transform);

// 添加Renderer和ParallaxLayer
SpriteRenderer sr = newLayer.AddComponent<SpriteRenderer>();
sr.sprite = yourSprite;
sr.material.mainTexture.wrapMode = TextureWrapMode.Repeat;

ParallaxLayer layer = newLayer.AddComponent<ParallaxLayer>();
layer.Initialize();

// 重新初始化管理器以包含新层
parallaxBg.Initialize();
```

### 与事件系统集成

```csharp
// 在EventType.cs中添加事件
public const string PLAYER_SPEED_CHANGED = "PlayerSpeedChanged";
public const string GAME_PAUSED = "GamePaused";

// 在ParallaxBackground中监听事件
void Start()
{
    EventListener.AddListener(EventType.PLAYER_SPEED_CHANGED, OnPlayerSpeedChanged);
    EventListener.AddListener(EventType.GAME_PAUSED, OnGamePaused);
}

void OnPlayerSpeedChanged(object data)
{
    float speed = (float)data;
    SetPlayerSpeed(speed);
}

void OnGamePaused(object data)
{
    bool paused = (bool)data;
    if (paused) PauseAll();
    else ResumeAll();
}
```

## ⚠️ 注意事项

### 材质设置
- **必须**将贴图Wrap Mode设置为Repeat，否则无法循环
- 使用Unlit Shader可以提高性能
- 避免使用过大的贴图（推荐2048x512或更小）

### 性能优化
- UV偏移方式已经是最优性能
- 避免在Update中频繁修改Speed Multiplier
- 使用Sprite Atlas合并多个背景贴图

### 常见问题

**Q: 背景不滚动？**
- 检查Texture的Wrap Mode是否设置为Repeat
- 检查ParallaxLayer是否正确初始化
- 检查是否调用了SetPlayerSpeed

**Q: 背景有接缝？**
- 确保贴图左右边缘无缝衔接
- 调整Material的Tiling值

**Q: 滚动不平滑？**
- 启用Smooth Transition
- 增加Transition Smoothness值
- 检查帧率是否稳定

## 📊 性能指标

- **Draw Call**: 每层1个（使用Sprite Atlas可进一步优化）
- **CPU开销**: 极低（仅修改Material参数）
- **内存占用**: 取决于贴图大小
- **推荐层数**: 2-4层（更多层对性能影响不大）

## 🎨 美术资源要求

### 贴图规格
- **尺寸**: 宽度建议2048px，高度根据屏幕比例
- **格式**: PNG（支持透明）或JPG
- **无缝拼接**: 左右边缘必须无缝衔接

### 制作技巧
- 使用Photoshop的Offset滤镜检查无缝性
- 远景使用低对比度，近景使用高对比度
- 考虑视差速度差异设计细节密度

## 📝 更新日志

### v1.0.0 (2024-02-28)
- ✅ 初始版本发布
- ✅ 支持Material UV偏移滚动
- ✅ 支持多层视差效果
- ✅ 支持跟随玩家速度
- ✅ 支持暂停/恢复
- ✅ 支持平滑速度过渡

## 📧 技术支持

如有问题或建议，请联系开发团队。
