# BattleRoom 单位管理重构设计文档

> 引入 `BattleUnitRegistryComponent` 统一单位查询，替换 `battleRoom.Units` 直接访问

**版本**: v2.0  
**日期**: 2026-04-10  
**作者**: Droid  
**状态**: 设计阶段

---

## 一、背景与目标

### 1.1 当前架构痛点

当前 `BattleRoom` 的 `Units` 字典被 **20+ 处** 代码直接访问：

```
BattleRoom (IScene)
├── WaveManagerComponent
├── SkillTimelineComponent
├── BossSyncComponent
├── BattleSpatialGrid
└── Units: Dictionary<long, EntityRef<BattleUnit>>  ← 所有单位混在一起，直接暴露
```

**核心问题**：

1. **字段直接暴露**：`battleRoom.Units` 被各模块直接访问，无法在查询过程中插入统一逻辑（如过滤、日志、统计）
2. **GC 压力**：高频 tick 路径（20ms 碰撞检测）遍历 `battleRoom.Units.Values`，部分场景每次创建新 List
3. **职责不清**：WaveManagerComponent 既管波次编排，又直接操作 `battleRoom.Units` 注册/移除怪物，查询和业务逻辑耦合

### 1.2 目标

引入 `BattleUnitRegistryComponent`（战斗单位注册表），接管 `BattleRoom.Units` 的查询职责。**不改变实体层级，不新增 Wave Entity，保持扁平结构**。

**设计原则**：
- **BattleUnit 只属于 BattleRoom**（`[ChildOf(typeof(BattleRoom))]` 不变）
- **单波次**，当前不涉及多波并行
- **WaveManagerComponent 保持现有职责**，只改造其直接操作 `battleRoom.Units` 的部分

### 1.3 预期收益

| 维度 | 改造前 | 改造后 |
|------|--------|--------|
| 单位查询 | `battleRoom.Units[id]` 直接字典访问 | `battleRoom.GetUnit(id)` 封装方法 |
| 全遍历 | `foreach (var kv in battleRoom.Units)` | `battleRoom.ForEachUnit(action)` 零分配 |
| 接口签名 | 20+ 处直接访问字典 | **对外接口签名不变**，内部委托给 Registry |
| 高频 GC | 每次遍历可能创建新 List | `ForEachUnit` 回调遍历无分配 |

---

## 二、核心设计

### 2.1 实体层级图

改造前后实体层级**完全不变**：

```
BattleRoom (IScene, SceneType.Battle)
├── WaveManagerComponent        ← 波次编排（保持现有职责）
├── SkillTimelineComponent
├── BossSyncComponent
├── BattleSpatialGrid
├── SlotManagerComponent
├── BattleUnitRegistryComponent ← 新增：统一单位注册表
│
└── BattleUnit [Hero-A]         ← [ChildOf(BattleRoom)] 不变
├── BattleUnit [Hero-B]
├── BattleUnit [Minion-1]
└── ...
```

### 2.2 BattleUnitRegistryComponent

```csharp
// Server/Model/Demo/Battle/BattleUnitRegistryComponent.cs

namespace ET.Server
{
    /// <summary>
    /// 战斗单位注册表 - 挂在 BattleRoom 上，统一管理所有 BattleUnit 的注册与查询。
    /// 接管原 battleRoom.Units 字典的查询职责。
    /// </summary>
    [ComponentOf(typeof(BattleRoom))]
    public class BattleUnitRegistryComponent : Entity, IAwake, IDestroy
    {
        /// <summary>全局单位索引：unitId -> BattleUnit</summary>
        public Dictionary<long, EntityRef<BattleUnit>> Units { get; } = new();
    }
}
```

### 2.3 查询接口封装

在 `BattleRoomSystem` 中提供封装方法，对外接口签名不变，内部委托给 Registry：

```csharp
// BattleRoomSystem 扩展方法

/// <summary>获取指定 ID 的单位（原有接口，内部改为委托给 Registry）</summary>
public static BattleUnit GetUnit(this BattleRoom self, long unitId)
{
    BattleUnitRegistryComponent registry = self.GetComponent<BattleUnitRegistryComponent>();
    if (registry != null) return registry.GetUnit(unitId);
    
    // 兼容降级：如果 Registry 未初始化，走原字典
    if (self.Units.TryGetValue(unitId, out EntityRef<BattleUnit> unitRef))
        return unitRef;
    return null;
}

/// <summary>获取所有存活单位</summary>
public static List<BattleUnit> GetAllUnits(this BattleRoom self)
{
    BattleUnitRegistryComponent registry = self.GetComponent<BattleUnitRegistryComponent>();
    if (registry != null) return registry.GetAllUnits();
    
    // 兼容降级
    // ... 原逻辑
}

/// <summary>遍历所有存活单位（零分配，适用于高频 tick 路径）</summary>
public static void ForEachUnit(this BattleRoom self, Action<BattleUnit> action)
{
    BattleUnitRegistryComponent registry = self.GetComponent<BattleUnitRegistryComponent>();
    if (registry != null) { registry.ForEachUnit(action); return; }
    
    // 兼容降级
    // ... 原逻辑
}

/// <summary>按阵营获取单位</summary>
public static List<BattleUnit> GetUnitsByCamp(this BattleRoom self, int camp) { ... }
```

### 2.4 Registry System

```csharp
// Server/Hotfix/Demo/Battle/BattleUnitRegistryComponentSystem.cs

namespace ET.Server
{
    [EntitySystemOf(typeof(BattleUnitRegistryComponent))]
    [FriendOf(typeof(BattleUnitRegistryComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleUnitRegistryComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleUnitRegistryComponent self) { }

        [EntitySystem]
        private static void Destroy(this BattleUnitRegistryComponent self)
        {
            self.Units.Clear();
        }

        /// <summary>注册单位</summary>
        public static void Register(this BattleUnitRegistryComponent self, BattleUnit unit)
        {
            if (unit != null && !unit.IsDisposed)
            {
                self.Units[unit.Id] = unit;
            }
        }

        /// <summary>移除单位</summary>
        public static void Unregister(this BattleUnitRegistryComponent self, long unitId)
        {
            self.Units.Remove(unitId);
        }

        /// <summary>获取单位</summary>
        public static BattleUnit GetUnit(this BattleUnitRegistryComponent self, long unitId)
        {
            return self.Units.TryGetValue(unitId, out EntityRef<BattleUnit> unitRef) ? unitRef : null;
        }

        /// <summary>获取所有存活单位</summary>
        public static List<BattleUnit> GetAllUnits(this BattleUnitRegistryComponent self)
        {
            List<BattleUnit> result = new();
            foreach (var kv in self.Units)
            {
                BattleUnit unit = kv.Value;
                if (unit != null && !unit.IsDead) result.Add(unit);
            }
            return result;
        }

        /// <summary>遍历所有存活单位（零分配）</summary>
        public static void ForEachUnit(this BattleUnitRegistryComponent self, Action<BattleUnit> action)
        {
            foreach (var kv in self.Units)
            {
                BattleUnit unit = kv.Value;
                if (unit != null && !unit.IsDead)
                {
                    action(unit);
                }
            }
        }
    }
}
```

---

## 三、迁移方案

### 3.1 需要新增的文件

| 文件 | 路径 | 说明 |
|------|------|------|
| `BattleUnitRegistryComponent.cs` | `Server/Model/Demo/Battle/` | 注册表 Model 定义 |
| `BattleUnitRegistryComponentSystem.cs` | `Server/Hotfix/Demo/Battle/` | 注册表业务逻辑 |

### 3.2 需要修改的文件

#### 核心改造

| 文件 | 改动点 | 规模 |
|------|--------|------|
| `BattleRoomSystem.cs` | Awake 中初始化 Registry；GetUnit/GetAllUnits/GetUnitsByCamp 委托给 Registry | 中 |
| `WaveManagerComponentSystem.cs` | 创建怪物时同时注册到 Registry；怪物死亡时从 Registry 移除 | 小 |

#### 查询迁移（`battleRoom.Units` → 封装方法）

| 文件 | 访问模式 | 迁移方式 |
|------|----------|----------|
| `SkillTimelineComponentSystem.cs` | 全遍历 `battleRoom.Units` + `battleRoom.GetUnit()` | 主路径已是 `GetUnit()`，降级路径改为 `ForEachUnit()` |
| `BossSyncComponentSystem.cs` | `battleRoom.Units.TryGetValue(bossId)` | 改为 `battleRoom.GetUnit(bossId)` |
| `ProjectileComponentSystem.cs` | 全遍历 `battleRoom.Units.Values` + `TryGetValue` | 改为 `ForEachUnit()` / `GetUnit()` |
| `BattleMoveComponentSystem.cs` | `battleRoom.Units.TryGetValue(chaseTargetId)` | 改为 `GetUnit(targetId)` |
| `BattleSkillHelper.cs` | 全遍历 `battleRoom.Units.Values`（3处）+ `TryGetValue`（1处） | 改为 `GetAllUnits()` / `GetUnit()` |
| `BattleUnitDead_Event.cs` | 全遍历 `battleRoom.Units`（2处） | 改为 `GetAllUnits()` / `ForEachUnit()` |
| `C2M_BattleReadyHandler.cs` | 全遍历 `battleRoom.Units` | 改为 `ForEachUnit()` |
| `ProjectileEvent_Handlers.cs` | `battleRoom.Units.TryGetValue()` | 改为 `GetUnit()` |
| `WaveManagerComponentSystem.cs` | `battleRoom.Units[monster.Id]` 赋值 + `TryGetValue` | 改为 Registry 注册/查找 |

**已使用封装方法，无需改动**：`C2M_ClientBatchHitHandler.cs`、`C2M_PlayerPositionSyncHandler.cs`、`BattleUnitHelper.cs`

### 3.3 分阶段迁移

#### 阶段 0：新增 Registry（无破坏性改动）

- [ ] 新增 `BattleUnitRegistryComponent`（Model + Hotfix）
- [ ] 新增 `BattleRoomSystem` 中的 `GetUnit/GetAllUnits/ForEachUnit/GetUnitsByCamp` 封装方法（内部兼容降级）
- [ ] **不修改任何现有代码**

#### 阶段 1：Registry 接管

- [ ] `BattleRoomSystem.Awake()` 中添加 Registry 初始化
- [ ] 所有创建 BattleUnit 的地方同时注册到 Registry
- [ ] `battleRoom.Units` 标记 `[Obsolete]`，逐模块迁移直接访问改为封装方法
- [ ] **验证**：运行服务端，确认战斗流程正常

#### 阶段 2：清理

- [ ] 移除 `battleRoom.Units` 字典（确认无直接访问后）
- [ ] 移除所有兼容降级代码
- [ ] **验证**：完整回归测试

### 3.4 回滚点

| 阶段 | 回滚方式 | 风险 |
|------|----------|------|
| 阶段 0 | 删除新增文件 | 无风险 |
| 阶段 1 | 恢复 `GetUnit()` 原始实现，移除 Registry | 低风险（接口未变） |
| 阶段 2 | 恢复 `BattleRoom.Units` 字典 | 低风险 |

---

## 四、高频查询性能分析

### 4.1 SkillTimelineComponentSystem（碰撞检测，20ms tick）

```csharp
// 改造前
foreach (var kv in battleRoom.Units)
{
    BattleUnit unit = kv.Value;
    if (unit != null && !unit.IsDead && unit.Id != entry.CasterId && unit.Camp != caster.Camp)
        candidateIds.Add(unit.Id);
}

// 改造后（零分配）
battleRoom.ForEachUnit(unit =>
{
    if (unit.Id != entry.CasterId && unit.Camp != caster.Camp)
        candidateIds.Add(unit.Id);
});
```

**性能**：与改造前等价，内部都是字典遍历，无额外开销。

### 4.2 BossSyncComponentSystem（Boss 同步，50ms tick）

```csharp
// 改造前
if (!battleRoom.Units.TryGetValue(bossId, out EntityRef<BattleUnit> bossRef))

// 改造后
BattleUnit boss = battleRoom.GetUnit(bossId);
```

**性能**：`GetUnit()` 内部是一次字典查找，与直接 `TryGetValue` 等价。

### 4.3 ProjectileComponentSystem（投射物碰撞，50ms tick）

```csharp
// 改造后（零分配）
battleRoom.ForEachUnit(target =>
{
    if (target.Id == projectileUnit.Id || target.Id == self.CasterId) return;
    if (target.IsDead) return;
    if (!IsEnemyCamp(self.Camp, target.Camp)) return;
    // ... 碰撞检测逻辑
});
```

### 4.4 BattleSkillHelper（技能目标选择，100ms tick）

```csharp
// 遍历频率不高，GetAllUnits() 的 GC 开销可接受
List<BattleUnit> allUnits = battleRoom.GetAllUnits();
foreach (BattleUnit unit in allUnits) { ... }
```

---

## 五、客户端影响

**无影响。** 本次改造是服务端内部重构，不改变实体层级、不新增消息协议、不改变客户端逻辑。

---

## 六、风险与注意事项

| 风险 | 说明 | 缓解措施 |
|------|------|----------|
| Registry 与 Units 不同步 | 迁移期间两套索引共存，可能不一致 | 阶段 1 中保持同步，阶段 2 再移除旧字典 |
| 忘记从 Registry 移除死亡单位 | 导致"幽灵单位" | 在 `BattleUnitDead_Event` 中统一处理 |
| ForEachUnit 过滤条件不一致 | `GetAllUnits` 过滤 `IsDead`，`ForEachUnit` 也必须一致 | 两个方法使用相同的过滤逻辑 |
| 空间网格与 Registry 同步 | 单位创建/移动/销毁需同时更新 | 现有逻辑不变，只是注册入口统一 |

---

## 附录 A：完整文件变更清单

### 新增文件

| 序号 | 文件路径 | 说明 |
|------|----------|------|
| 1 | `Server/Model/Demo/Battle/BattleUnitRegistryComponent.cs` | 注册表 Model |
| 2 | `Server/Hotfix/Demo/Battle/BattleUnitRegistryComponentSystem.cs` | 注册表逻辑 |

### 修改文件

| 序号 | 文件路径 | 改动摘要 |
|------|----------|----------|
| 1 | `Server/Hotfix/Demo/Battle/BattleRoomSystem.cs` | Registry 初始化；GetUnit/GetAllUnits/ForEachUnit 委托给 Registry |
| 2 | `Server/Hotfix/Demo/Battle/WaveManagerComponentSystem.cs` | 创建怪物时注册到 Registry；死亡时从 Registry 移除 |
| 3 | `Server/Hotfix/Demo/Battle/SkillTimelineComponentSystem.cs` | `battleRoom.Units` → `ForEachUnit()` |
| 4 | `Server/Hotfix/Demo/Battle/BossSyncComponentSystem.cs` | `TryGetValue` → `GetUnit()` |
| 5 | `Server/Hotfix/Demo/Battle/ProjectileComponentSystem.cs` | `Units.Values` → `ForEachUnit()` / `GetUnit()` |
| 6 | `Server/Hotfix/Demo/Battle/BattleMoveComponentSystem.cs` | `TryGetValue` → `GetUnit()` |
| 7 | `Server/Hotfix/Demo/Battle/BattleSkillHelper.cs` | `Units.Values` → `GetAllUnits()` / `GetUnit()` |
| 8 | `Server/Hotfix/Demo/Battle/Event/BattleUnitDead_Event.cs` | `battleRoom.Units` → `GetAllUnits()` / `ForEachUnit()` |
| 9 | `Server/Hotfix/Demo/Battle/Handler/C2M_BattleReadyHandler.cs` | `battleRoom.Units` → `ForEachUnit()` |
| 10 | `Server/Hotfix/Demo/Battle/Event/ProjectileEvent_Handlers.cs` | `TryGetValue` → `GetUnit()` |

---

**文档结束**
