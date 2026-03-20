# 攻击目标功能文档

## 功能概述

玩家通过点击怪物发起攻击，服务器验证攻击合法性后，计算伤害并扣除怪物生命值，怪物死亡时触发死亡事件。

## 需求描述

1. 玩家点击场景中的怪物发起攻击
2. 服务器验证攻击的合法性（距离、技能冷却、目标状态等）
3. 计算伤害值并扣除怪物生命值
4. 当怪物生命值低于等于0时，触发怪物死亡事件

## 接口说明

### C2M_AttackTargetHandler

- **功能**: 处理客户端发起的攻击请求
- **输入**: 目标单位ID、攻击技能ID
- **输出**: 攻击结果（命中/未命中）、伤害值

### BattleUnitDead_Event

- **功能**: 战斗单位死亡事件
- **触发条件**: 单位生命值 <= 0
- **处理**: 通知客户端、掉落奖励、移除单位

## 数据结构

```csharp
// 攻击请求
public class C2M_AttackTargetRequest : IRequest
{
    public long TargetId { get; set; }
    public int SkillId { get; set; }
}

// 攻击响应
public class M2C_AttackResultResponse : IResponse
{
    public long TargetId { get; set; }
    public int Damage { get; set; }
    public bool IsDead { get; set; }
}
```

## 流程描述

```
玩家点击怪物
    ↓
客户端发送攻击请求 (C2M_AttackTarget)
    ↓
服务器验证攻击合法性
    ├─ 检查目标是否有效
    ├─ 检查攻击距离
    └─ 检查技能冷却
    ↓
计算伤害值
    ↓
扣除怪物生命值
    ↓
判断是否死亡
    ├─ 未死亡 → 返回攻击结果
    └─ 死亡 → 触发死亡事件
```

## 注意事项

1. 攻击请求需要带时间戳，防止重放攻击
2. 服务器端必须做完整的攻击验证，不能信任客户端数据
3. 伤害计算需要考虑多种因素（属性加成、装备加成、Buff等）
4. 死亡事件需要原子性处理，避免重复触发
