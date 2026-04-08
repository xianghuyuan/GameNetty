# [项目名称] 割草类ARPG状态同步架构规范

> **适用场景**：类似《英雄没有闪》的高频攻击 + 同屏海量怪物 + 强调打击感的ARPG游戏。
> **核心指导思想**：服务端变成纯粹的"数学计算器与裁判"，客户端变成"狂野的演员"。通过"极度压榨性能的数学碰撞"与"双轨制同步"，在保证割草性能的同时，实现刀刀到肉的动作体验。

---

## 一、 底层架构重组：分离逻辑与表现

在项目初期，必须从底层限制服务端的权限，戒掉在服务端使用物理引擎的依赖。

### 1. 服务端禁用物理引擎
*   **绝对禁止**使用 `Rigidbody`、`Collider`、`CharacterController` 等物理组件。
*   **替代方案**：服务端所有实体仅保留基础数学属性：`float3 Position`、`float Speed`。
*   **碰撞检测**：服务端自己实现最基础的纯数学相交判断（如 `BattleSpatialGrid` 网格查询），这种运算在服务器上极快。

### 2. 引入空间划分算法（必备）
*   **痛点**：几百只怪与几十个技能的双重 `for` 循环碰撞检测，复杂度是灾难。
*   **落地做法**：在服务端实现 **网格地图**（`BattleSpatialGrid`）。Boss 释放技能时，仅取出技能覆盖网格内的目标进行碰撞计算，将计算量从 $O(N^2)$ 降至 $O(N)$。

### 3. 实体数据结构
*   杂兵在服务端仅保留轻量 `BattleUnit`（属性 + 空间网格注册），无移动组件、无 AI 组件。
*   Boss 在服务端拥有完整的 `BattleMoveComponent` + `BattleActionDecisionComponent`。

---

## 二、 确立"双轨制"同步协议

根据实体重要程度，网络协议严格分为两套逻辑，坚决避免"一刀切"。

### 轨道 A：杂兵系统（客户端权威 + 服务端验证）
*   **不同步个体位置**：服务端不发送杂兵移动指令，杂兵位置完全由客户端驱动。
*   **下发"群组意图"**：服务端下发波次指令（`M2C_SpawnWave`），客户端本地创建杂兵并运行 AI。

```
// 服务端 -> 客户端：杂兵波次指令
M2C_SpawnWave {
    battleId, waveId, monsterConfigId,
    count, centerX, centerY,
    spreadRange, startUnitId,
    moveDirX, moveDirY
}
```

*   **客户端驱动一切**：杂兵的移动、攻击判定、伤害计算全部在客户端完成。
*   **服务端验证**：客户端通过 `C2M_ClientBatchHit` 上报命中结果，服务端做基础合法性校验后承认伤害。
*   **双向命中上报**：`C2M_ClientBatchHit` 同时用于两个方向：
    *   **玩家打杂兵**：casterId = 玩家，hitUnitIds = 杂兵列表
    *   **杂兵打玩家**：casterId = 杂兵，hitUnitIds = 玩家列表

### 轨道 B：Boss/精英怪系统（服务端绝对权威）
*   **服务端控制一切**：Boss 的移动（`BattleMoveComponent` 100ms tick）、AI 决策（`BattleActionDecisionComponent` 100ms tick）、伤害计算全部在服务端完成。
*   **高频同步**：Boss 位置和状态以 **20Hz** 通过 `BossSyncComponent` 广播 `M2C_SyncBoss`。
*   **客户端只做表现**：收到 `M2C_BattleUnitMoveCommand` 直接 snap 位置，收到 `M2C_Damage` 更新血量。

### 玩家英雄
*   **移动**：客户端权威，通过 `Forward`（移动意图方向）驱动增量移动，`C2M_PlayerPositionSync` 同步位置给服务端供 Boss AI 追踪。
*   **施法**：`C2M_CastSkill` → 服务端执行技能选目标 + 伤害计算 → 广播 `M2C_Damage`。
*   **AI 自动战斗**：`ClientPlayerAIComponent`（100ms tick）负责自动选目标、判断射程、释放技能。射程判定与技能 CD 解耦，用最短技能射程停移。

---

## 三、 客户端移动系统：方向驱动模型

### 1. 核心字段（BattleUnit）
*   **`Forward`**（`float3`）：移动意图方向。非零时 View 层每帧执行 `speed * deltaTime` 增量移动；`float3.zero` 表示不移动。
*   **`FaceDirection`**（`float`）：视觉朝向（1f=右, -1f=左）。与 `Forward` 解耦：攻击时 `Forward=zero`（停移）但 `FaceDirection` 仍指向目标。

### 2. 移动驱动
*   **AI 层**（100ms tick）：`ClientPlayerAIComponent` / `ClientMinionAIComponent` 负责决策，设置 `Forward` 和 `FaceDirection`。
*   **View 层**（每帧）：`BattleUnitViewSystem.Update` 读取 `Forward` 驱动增量移动 + 根据 `FaceDirection` 翻转精灵。

### 3. 停移逻辑（玩家）
*   遍历所有自动技能的 `SkillTargetingConfig.CastRange + EdgeDistance`。
*   **有技能 CD 就绪**：用 CD 就绪技能中的最短射程停移。
*   **全部技能 CD 中**：用所有技能中的最短射程停移（等 CD，不冲过头）。
*   停移后 `Forward = zero`，但 `FaceDirection` 保持指向目标。

### 4. 停移逻辑（杂兵）
*   从 `UnitCombatConfig.AutoSkillIds` / `NormalAttackSkillId` 推算最短射程作为 `AttackRange`。
*   进入 `AttackRange` 后 `Forward = zero`，停止移动。
*   CD 中时等待，CD 好后发送 `C2M_ClientBatchHit` 上报伤害。

---

## 四、 解决"高攻速"与"刀刀到肉"（核心难点）

### 1. 技能时间轴化
客户端按下攻击时，直接把"未来会发生什么"告诉服务端，包含攻击判定框的**有效时间窗口**。

### 2. 服务端异步插帧结算
*   服务端收到 `C2M_CastSkill` 后，将判定框注册到 `SkillTimelineComponent` 时间轴队列。
*   20ms tick 执行碰撞检测，100ms 批量打包下发伤害。

### 3. 客户端预表现
*   **砍杂兵**：发请求同时，本地立刻播放受击/死亡特效。服务端验证后通过 `M2C_Damage` 确认。
*   **砍 Boss**：仅播放挥砍特效和音效，不扣 Boss 血量。等待服务端下发 `M2C_Damage`，再飘字扣血。

---

## 五、 打击感的网络处理

### 1. 卡肉 —— 纯客户端视觉欺骗
*   客户端动画系统检测到命中，调用 `DOTween` 播放脉冲缩放（`PlayPulse`）模拟卡肉。
*   服务端的时间轴匀速流逝，不受影响。

### 2. 击退 —— 客户端拉扯 + 服务端静默
*   客户端：打中怪物，本地立刻给怪物施加位移。
*   服务端：收到命中消息，在服务端逻辑坐标上也加上击退距离，下发 `M2C_UnitKnockback`。
*   容错纠偏：服务端下发 `M2C_ForceCorrectPos`，客户端用极快速度"滑"过去，避免穿模拉扯感。

---

## 六、 客户端开发红线规范

1.  **死亡表现不等待**：怪物血量 `<=0`，客户端立刻播放死亡特效、移除实体。不用等服务端的"死亡确认包"。
2.  **网络状态机兜底**：如果连续发送多个请求都未收到服务端响应，必须停止本地预计算，并在 UI 提示"网络延迟"。
3.  **杂兵伤害走网络**：杂兵对玩家的伤害也必须通过 `C2M_ClientBatchHit` 上报服务端验证，不允许客户端直接扣减玩家血量。

---

## 附录：典型数据流向图景

```
[玩家自动战斗 AI Tick — 每100ms]
      │
      ├─ 找最近敌人
      ├─ 判断是否在技能射程内
      ├─ 在射程 → Forward=zero, FaceDirection→目标, 发 C2M_CastSkill
      └─ 超射程 → Forward→目标方向, FaceDirection→目标, 发 C2M_PlayerPositionSync

[每帧 View Update]
      │
      ├─ Forward != zero → 增量移动 (speed * deltaTime)
      └─ FaceDirection → 翻转精灵朝向

[杂兵 AI Tick — 每100ms]
      │
      ├─ 找最近友方
      ├─ 在攻击范围内 → Forward=zero, CD好了就发 C2M_ClientBatchHit
      └─ 超出范围 → Forward→玩家方向, 增量移动靠近

[Boss — 服务端 100ms tick]
      │
      ├─ DecisionComp: 选技能 → 在射程施法 / 超射程移动
      ├─ MoveComp: 增量移动 + 追击检测
      └─ BossSync: 50ms 广播 M2C_SyncBoss
```

**遵循此规范，可在百兆带宽和常规云服务器下，稳定支撑同屏上千怪物的高频动作交互。**
