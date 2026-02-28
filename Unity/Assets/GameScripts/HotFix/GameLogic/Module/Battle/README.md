# BattleUnit 副本实体系统 - 使用指南

## 概述

BattleUnit 副本实体系统实现了战斗数据与主世界数据的隔离，确保战斗中的变化不会影响主世界的 Unit。

## 核心概念

### 数据流转

```
主世界 Unit (持久化)
    ↓ 进入战斗
BattleUnit (临时副本)
    ↓ 战斗结束
同步结果回 Unit
```

### 关键设计

1. **OwnerId 关联**: BattleUnit.OwnerId 记录对应的 Unit.Id
2. **数据隔离**: 战斗中只修改 BattleUnit，不影响 Unit
3. **结算同步**: 战斗结束后，只同步必要数据（经验、掉落）回 Unit

## 快速开始

### 1. 初始化 BattleComponent

在场景初始化时添加 BattleComponent：

```csharp
// 在场景初始化时
Scene scene = ...;
scene.AddComponent<BattleComponent>();
```

### 2. 进入战斗

```csharp
// 获取主世界 Unit
Unit unit = scene.GetComponent<UnitComponent>().Get(playerId);

// 创建战斗
BattleComponent battleComponent = scene.GetComponent<BattleComponent>();
Battle battle = battleComponent.CreateBattle(battleId, (int)BattleType.WaveBattle);

// 从 Unit 创建 BattleUnit（玩家英雄）
BattleUnit heroUnit = UnitFactory.CreateHero(battle, unit, new float3(0, 0, 0));

// 创建怪物
BattleUnit monster1 = UnitFactory.CreateMonster(battle, 2001, new float3(10, 0, 0));
BattleUnit monster2 = UnitFactory.CreateMonster(battle, 2001, new float3(15, 0, 0));

// 开始战斗
battle.Start();
```

### 3. 战斗中

```csharp
// 获取当前战斗
Battle battle = scene.GetComponent<BattleComponent>().GetCurrentBattle();

// 获取所有友方单位
List<BattleUnit> friends = battle.GetBattleUnitsByCamp(UnitCamp.Friend);

// 获取所有敌方单位
List<BattleUnit> enemies = battle.GetBattleUnitsByCamp(UnitCamp.Enemy);

// 造成伤害
BattleUnit target = enemies[0];
target.TakeDamage(100);

// 检查是否死亡
if (target.IsDead)
{
    Log.Info("敌人已死亡");
}

// 检查战斗是否结束
if (battle.CheckBattleEnd())
{
    bool success = battle.GetAliveBattleUnits(UnitCamp.Enemy).Count == 0;
    battle.End(success);
}
```

### 4. 战斗结束

```csharp
// 战斗结束时会触发 BattleEnd 事件
// 在事件处理中同步数据

// 获取战斗结果（从 BattleEnd 事件）
BattleResult result = ...;

// 查找对应的主世界 Unit
Unit unit = BattleUnitHelper.FindOwnerUnit(scene, heroUnit);

// 同步结果到 Unit
if (unit != null)
{
    BattleUnitHelper.SyncBattleResultToUnit(unit, result);
}

// 清理战斗
battleComponent.RemoveBattle(battle.BattleId);
```

## 完整示例

### 示例 1：简单战斗流程

```csharp
public class BattleExample
{
    public static async ETTask StartSimpleBattle(Scene scene, long playerId)
    {
        // 1. 获取主世界 Unit
        Unit unit = scene.GetComponent<UnitComponent>().Get(playerId);
        if (unit == null)
        {
            Log.Error("找不到玩家 Unit");
            return;
        }
        
        // 2. 创建战斗
        BattleComponent battleComponent = scene.GetComponent<BattleComponent>();
        long battleId = IdGenerater.Instance.GenerateId();
        Battle battle = battleComponent.CreateBattle(battleId, (int)BattleType.WaveBattle);
        
        // 3. 创建玩家英雄
        BattleUnit hero = UnitFactory.CreateHero(battle, unit, new float3(0, 0, 0));
        
        // 4. 创建怪物
        BattleUnit monster = UnitFactory.CreateMonster(battle, 2001, new float3(10, 0, 0));
        
        // 5. 开始战斗
        battle.Start();
        
        // 6. 模拟战斗（实际应该由战斗系统驱动）
        while (!battle.CheckBattleEnd())
        {
            // 玩家攻击怪物
            monster.TakeDamage(50);
            
            await TimerComponent.Instance.WaitAsync(1000);
            
            if (monster.IsDead)
            {
                break;
            }
            
            // 怪物反击
            hero.TakeDamage(10);
            
            if (hero.IsDead)
            {
                break;
            }
        }
        
        // 7. 战斗结束
        bool success = !hero.IsDead;
        battle.End(success);
        
        // 8. 同步结果
        if (success)
        {
            BattleResult result = new BattleResult
            {
                Success = true,
                Duration = 10,
                Exp = 100,
                Drops = new List<ItemDrop>
                {
                    new ItemDrop { ConfigId = 1001, Count = 1 }
                },
                PlayerDamage = new Dictionary<long, int>()
            };
            
            BattleUnitHelper.SyncBattleResultToUnit(unit, result);
        }
        
        // 9. 清理战斗
        battleComponent.RemoveBattle(battleId);
    }
}
```

### 示例 2：监听战斗事件

```csharp
// 战斗开始事件
[Event(SceneType.Current)]
public class BattleStartEvent : AEvent<Scene, BattleStart>
{
    protected override async ETTask Run(Scene scene, BattleStart args)
    {
        Log.Info($"战斗开始: BattleId={args.Battle.BattleId}");
        
        // 显示战斗 UI
        // await UIHelper.ShowBattleUI(scene);
        
        await ETTask.CompletedTask;
    }
}

// 战斗结束事件
[Event(SceneType.Current)]
public class BattleEndEvent : AEvent<Scene, BattleEnd>
{
    protected override async ETTask Run(Scene scene, BattleEnd args)
    {
        Log.Info($"战斗结束: Success={args.Result.Success}");
        
        // 显示结算界面
        // await UIHelper.ShowBattleResult(scene, args.Result);
        
        await ETTask.CompletedTask;
    }
}

// 单位死亡事件
[Event(SceneType.Current)]
public class BattleUnitDeadEvent : AEvent<Scene, BattleUnitDead>
{
    protected override async ETTask Run(Scene scene, BattleUnitDead args)
    {
        Log.Info($"单位死亡: ConfigId={args.BattleUnit.ConfigId}, Camp={args.BattleUnit.Camp}");
        
        // 播放死亡动画
        // await PlayDeathAnimation(args.BattleUnit);
        
        await ETTask.CompletedTask;
    }
}
```

## API 参考

### BattleComponent

```csharp
// 创建战斗
Battle CreateBattle(long battleId, int battleType)

// 获取战斗
Battle GetBattle(long battleId)

// 获取当前战斗
Battle GetCurrentBattle()

// 移除战斗
void RemoveBattle(long battleId)
```

### Battle

```csharp
// 开始战斗
void Start()

// 暂停战斗
void Pause()

// 恢复战斗
void Resume()

// 结束战斗
void End(bool success)

// 获取所有战斗单位
List<BattleUnit> GetAllBattleUnits()

// 根据阵营获取战斗单位
List<BattleUnit> GetBattleUnitsByCamp(UnitCamp camp)

// 根据 OwnerId 获取战斗单位
BattleUnit GetBattleUnitByOwner(long ownerId)

// 获取存活的战斗单位
List<BattleUnit> GetAliveBattleUnits(UnitCamp camp)

// 检查战斗是否结束
bool CheckBattleEnd()
```

### BattleUnit

```csharp
// 获取配置
UnitConfig GetConfig()

// 判断是否死亡
bool CheckIsDead()

// 受到伤害
void TakeDamage(int damage)

// 治疗
void Heal(int healAmount)
```

### UnitFactory

```csharp
// 创建玩家英雄
BattleUnit CreateHero(Battle battle, Unit unit, float3 position)

// 创建怪物
BattleUnit CreateMonster(Battle battle, int configId, float3 position)
```

### BattleUnitHelper

```csharp
// 从 Unit 创建 BattleUnit
BattleUnit CreateFromUnit(Battle battle, Unit unit, float3 position)

// 复制数值
void CopyNumeric(Unit unit, BattleUnit battleUnit)

// 同步战斗结果到 Unit
void SyncBattleResultToUnit(Unit unit, BattleResult result)

// 查找对应的 Unit
Unit FindOwnerUnit(Scene scene, BattleUnit battleUnit)

// 从配置表创建怪物
BattleUnit CreateMonsterFromConfig(Battle battle, int configId, float3 position)
```

## 注意事项

### 1. OwnerId 的重要性

```csharp
// ✅ 正确：创建时设置 OwnerId
battleUnit.OwnerId = unit.Id;

// ❌ 错误：忘记设置 OwnerId
// 会导致战斗结束后无法找到对应的 Unit
```

### 2. 数据隔离

```csharp
// ❌ 错误：直接修改主世界 Unit
unit.GetComponent<NumericComponent>().Set(NumericType.Hp, newHp);

// ✅ 正确：只修改 BattleUnit
battleUnit.GetComponent<NumericComponent>().Set(NumericType.Hp, newHp);
```

### 3. 战斗结束后清理

```csharp
// ✅ 正确：战斗结束后立即清理
battleComponent.RemoveBattle(battleId);

// ❌ 错误：忘记清理，导致内存泄漏
```

### 4. 失败不同步

```csharp
// ✅ 正确：只在胜利时同步数据
if (result.Success)
{
    BattleUnitHelper.SyncBattleResultToUnit(unit, result);
}

// ❌ 错误：失败也同步，可能导致数据异常
```

## 扩展功能

### TODO 列表

以下功能在 MVP 版本中未实现，可以后续扩展：

1. **Buff 系统**: BuffComponent
2. **技能系统**: SkillComponent
3. **战斗状态**: BattleStateComponent（眩晕、沉默等）
4. **波次管理**: WaveManager
5. **AI 系统**: 怪物 AI
6. **战斗回放**: 记录战斗过程
7. **伤害统计**: 详细的伤害统计
8. **战斗日志**: 完整的战斗日志

## 常见问题

### Q: 为什么要创建 BattleUnit 副本？

A: 数据隔离，防止战斗异常影响主世界数据，支持战斗失败回滚。

### Q: BattleUnit 和 Unit 有什么区别？

A: 
- Unit: 主世界角色，持久化，有背包、任务等业务组件
- BattleUnit: 战斗副本，临时，只有战斗相关组件

### Q: 怎么在战斗中访问主世界 Unit？

A: 通过 `BattleUnitHelper.FindOwnerUnit(scene, battleUnit)` 查找。

### Q: 战斗失败后数据会丢失吗？

A: 不会，战斗失败不同步数据，主世界 Unit 保持原样。

### Q: 可以同时进行多个战斗吗？

A: MVP 版本只支持单个战斗（CurrentBattle），如需多战斗需要扩展 BattleComponent。

## 文件结构

```
Module/Battle/
├── BattleEnum.cs              # 枚举定义
├── Battle.cs                  # Battle 实体
├── BattleSystem.cs            # Battle 系统
├── BattleUnit.cs              # BattleUnit 实体
├── BattleUnitSystem.cs        # BattleUnit 系统
├── BattleComponent.cs         # 战斗管理组件
├── BattleResult.cs            # 战斗结果
├── BattleUnitHelper.cs        # 数据复制辅助类
└── BattleEventType.cs         # 战斗事件

Module/Unit/
└── UnitFactory_Battle.cs      # 战斗单位工厂

Module/Numeric/
└── NumericNoticeComponent.cs  # 数值通知组件
```

## 总结

BattleUnit 副本实体系统提供了：

✅ 数据安全：战斗异常不影响主世界  
✅ 业务隔离：战斗系统独立，易于扩展  
✅ 灵活性：支持多种战斗模式  
✅ 可维护性：代码结构清晰，职责明确  

现在你可以开始使用这个系统构建你的战斗功能了！
