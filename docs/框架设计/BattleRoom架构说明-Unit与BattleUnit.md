# BattleRoom 架构说明：Unit vs BattleUnit

## 🎯 核心概念

### 关键理解

**BattleRoom 不包含 UnitComponent！**

- `UnitComponent` 只存在于 **Map Scene** 上
- `BattleRoom` 只包含 **BattleUnit**（战斗单位）
- `Unit`（玩家实体）始终在 Map Scene 的 UnitComponent 中

---

## 🏗️ 正确的架构

```
Map Scene (SceneType.Map)
├── UnitComponent (全局玩家管理)
│   ├── Unit A (玩家A的主实体)
│   ├── Unit B (玩家B的主实体)
│   └── Unit C (玩家C的主实体)
│
├── BattleRoomManagerComponent
│
└── BattleRoom 1 (SceneType.Battle)
    ├── PlayerIds: [A, B]  ← 只存储玩家ID引用
    ├── State: Fighting
    └── Units: Dictionary<long, EntityRef<BattleUnit>>
        ├── BattleUnit 1 (玩家A的战斗化身)
        │   └── OwnerId = A  ← 指向 Unit A
        ├── BattleUnit 2 (玩家B的战斗化身)
        │   └── OwnerId = B  ← 指向 Unit B
        └── BattleUnit 3 (怪物)
            └── OwnerId = 0  ← 怪物没有主人
```

---

## 📊 Unit vs BattleUnit 对比

| 特性 | Unit | BattleUnit |
|------|------|-----------|
| **位置** | Map Scene → UnitComponent | BattleRoom → Units |
| **生命周期** | 持久存在（玩家在线期间） | 临时存在（战斗期间） |
| **数据** | 账号、等级、装备、背包等 | 战斗属性、位置、状态 |
| **作用** | 玩家在主世界的实体 | 玩家在战斗中的化身 |
| **销毁时机** | 玩家下线 | 战斗结束 |
| **组件** | NumericComponent, BagComponent, etc. | NumericComponent, BuffComponent, etc. |

---

## 🔄 数据流转

### 1. 进入战斗

```
Unit (Map Scene)
    ↓ 复制数据
BattleUnit (BattleRoom)
```

```csharp
// 从 Unit 创建 BattleUnit
BattleUnit battleUnit = UnitFactory.CreateHero(battleRoom, unit.Id, unit.ConfigId, position);
battleUnit.OwnerId = unit.Id;  // ⭐ 记录对应的 Unit ID

// 复制数值
BattleUnitHelper.CopyNumeric(unit, battleUnit);
```

### 2. 战斗中

```
BattleUnit 在 BattleRoom 中战斗
- 受到伤害
- 释放技能
- 获得 Buff
- ...
```

### 3. 战斗结束

```
BattleUnit (BattleRoom)
    ↓ 同步结果
Unit (Map Scene)
    ↓ 销毁 BattleUnit
BattleUnit.Dispose()
```

```csharp
// 战斗结束，同步结果到 Unit
BattleUnitHelper.SyncBattleResultToUnit(unit, battleResult);

// 销毁 BattleUnit
battleUnit.Dispose();

// Unit 继续存在于 Map Scene
```

---

## 💡 如何获取 Unit？

### ❌ 错误做法

```csharp
// ❌ 错误！BattleRoom 没有 UnitComponent
UnitComponent unitComponent = battleRoom.GetComponent<UnitComponent>();
```

### ✅ 正确做法

```csharp
// ✅ 正确！从 Map Scene 获取 UnitComponent
Scene mapScene = battleRoom.Scene();  // 获取父 Scene (Map Scene)
UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
Unit unit = unitComponent.Get(playerId);
```

---

## 📝 代码示例

### 示例 1: 广播消息给 BattleRoom 内的玩家

```csharp
private static void BroadcastToBattleRoom(this WaveManagerComponent self, IMessage message)
{
    BattleRoom battleRoom = self.GetParent<BattleRoom>();
    
    // ✅ 正确：从 Map Scene 获取 UnitComponent
    Scene mapScene = battleRoom.Scene();
    UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
    
    // 遍历 BattleRoom 的 PlayerIds
    foreach (long playerId in battleRoom.PlayerIds)
    {
        // 从 Map Scene 的 UnitComponent 获取 Unit
        Unit player = unitComponent.Get(playerId);
        if (player != null)
        {
            MapMessageHelper.SendToClient(player, message);
        }
    }
}
```

### 示例 2: 创建 BattleUnit

```csharp
private async ETTask InitWaveBattle(BattleRoom battleRoom, Unit unit, int totalWaves)
{
    // unit 参数来自 Map Scene 的 UnitComponent
    // 通过 MessageLocationHandler 自动定位
    
    // 创建 BattleUnit（战斗化身）
    BattleUnit battleUnit = UnitFactory.CreateHero(
        battleRoom,      // 父实体是 BattleRoom
        unit.Id,         // OwnerId 指向 Unit
        unit.ConfigId,   // 配置ID
        Vector3.zero     // 战斗位置
    );
    
    // 添加到 BattleRoom 的 Units 字典
    battleRoom.Units[battleUnit.Id] = battleUnit;
    
    // 复制 Unit 的数值到 BattleUnit
    BattleUnitHelper.CopyNumeric(unit, battleUnit);
}
```

### 示例 3: 战斗结束后同步数据

```csharp
private async ETTask OnAllWavesCompleted(this WaveManagerComponent self)
{
    BattleRoom battleRoom = self.GetParent<BattleRoom>();
    
    // ✅ 正确：从 Map Scene 获取 UnitComponent
    Scene mapScene = battleRoom.Scene();
    UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
    
    // 遍历所有玩家
    foreach (long playerId in battleRoom.PlayerIds)
    {
        // 获取 Unit（主实体）
        Unit unit = unitComponent.Get(playerId);
        if (unit == null) continue;
        
        // 同步战斗结果到 Unit
        BattleUnitHelper.SyncBattleResultToUnit(unit, battleResult);
    }
    
    // 清理 BattleRoom（包括所有 BattleUnit）
    battleRoom.Dispose();
}
```

---

## 🎮 完整流程示例

### 玩家开始战斗

```csharp
[MessageLocationHandler(SceneType.Map)]
public class C2M_StartBattleHandler : MessageLocationHandler<Unit, C2M_StartBattle, M2C_StartBattle>
{
    protected override async ETTask Run(Unit unit, C2M_StartBattle request, M2C_StartBattle response)
    {
        // unit 来自 Map Scene 的 UnitComponent
        Scene mapScene = unit.Scene();
        
        // 创建 BattleRoom
        BattleRoom battleRoom = mapScene.AddChild<BattleRoom>();
        battleRoom.PlayerIds.Add(unit.Id);  // ⭐ 只存储 ID
        
        // 创建 BattleUnit（战斗化身）
        BattleUnit battleUnit = UnitFactory.CreateHero(battleRoom, unit.Id, unit.ConfigId, Vector3.zero);
        battleRoom.Units[battleUnit.Id] = battleUnit;
        
        // Unit 依然在 Map Scene 的 UnitComponent 中
        // BattleUnit 在 BattleRoom 的 Units 中
    }
}
```

### 怪物死亡处理

```csharp
public static async ETTask OnMonsterDead(this WaveManagerComponent self, long monsterId)
{
    BattleRoom battleRoom = self.GetParent<BattleRoom>();
    
    // 从 BattleRoom.Units 移除怪物
    if (battleRoom.Units.TryGetValue(monsterId, out EntityRef<BattleUnit> monsterRef))
    {
        BattleUnit monster = monsterRef;
        monster?.Dispose();
        battleRoom.Units.Remove(monsterId);
    }
    
    // 检查波次是否完成
    self.CurrentWaveMonsterIds.Remove(monsterId);
    if (self.CurrentWaveMonsterIds.Count == 0)
    {
        await self.OnWaveCompleted();
    }
}
```

---

## ⚠️ 常见错误

### 错误 1: 在 BattleRoom 上查找 UnitComponent

```csharp
// ❌ 错误
UnitComponent unitComponent = battleRoom.GetComponent<UnitComponent>();
// 结果：null，因为 BattleRoom 没有 UnitComponent
```

**正确做法**：
```csharp
// ✅ 正确
Scene mapScene = battleRoom.Scene();
UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
```

### 错误 2: 混淆 Unit 和 BattleUnit

```csharp
// ❌ 错误：把 Unit 添加到 BattleRoom
battleRoom.Units[unit.Id] = unit;  // 类型错误！

// ✅ 正确：创建 BattleUnit 并添加
BattleUnit battleUnit = UnitFactory.CreateHero(...);
battleRoom.Units[battleUnit.Id] = battleUnit;
```

### 错误 3: 战斗结束后没有同步数据

```csharp
// ❌ 错误：直接销毁 BattleRoom，丢失战斗结果
battleRoom.Dispose();

// ✅ 正确：先同步数据到 Unit，再销毁
foreach (long playerId in battleRoom.PlayerIds)
{
    Unit unit = mapScene.GetComponent<UnitComponent>().Get(playerId);
    BattleUnitHelper.SyncBattleResultToUnit(unit, battleResult);
}
battleRoom.Dispose();
```

---

## 🎯 设计原则

### 为什么这样设计？

1. **数据隔离**
   - Unit 包含持久数据（等级、装备、背包）
   - BattleUnit 只包含战斗临时数据
   - 战斗结束后，临时数据销毁，持久数据保留

2. **性能优化**
   - BattleRoom 是临时场景，可以快速创建和销毁
   - 不需要在 BattleRoom 中维护完整的玩家数据

3. **逻辑清晰**
   - Unit 负责主世界逻辑
   - BattleUnit 负责战斗逻辑
   - 职责分明，易于维护

4. **支持多战斗实例**
   - 同一个 Unit 可以对应多个 BattleUnit（不同战斗）
   - 通过 OwnerId 关联

---

## 📖 总结

### 记住这些关键点：

1. ✅ **UnitComponent 只在 Map Scene 上**
2. ✅ **BattleRoom 只包含 BattleUnit**
3. ✅ **通过 `battleRoom.Scene()` 获取 Map Scene**
4. ✅ **通过 `OwnerId` 关联 Unit 和 BattleUnit**
5. ✅ **战斗结束后同步数据到 Unit**

### 架构图

```
Map Scene (持久层)
    ↓ 引用
BattleRoom (临时层)
    ↓ 战斗结束
Map Scene (持久层)
```

---

**创建日期**: 2026-03-04
**作者**: Droid
**版本**: v1.0
