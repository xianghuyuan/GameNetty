using System.Diagnostics;

namespace ET.Server
{
    [Event(SceneType.Battle)]
    [FriendOf(typeof(WaveManagerComponent))]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(SlotManagerComponent))]
    [FriendOf(typeof(BattleUnitRegistryComponent))]
    public class BattleUnitDead_Event : AEvent<Scene, BattleUnitDead>
    {
        protected override async ETTask Run(Scene scene, BattleUnitDead args)
        {
            BattleUnit deadUnit = args.BattleUnit;
            if (deadUnit == null)
            {
                return;
            }
            
            BattleRoom battleRoom = deadUnit.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return;
            }
            
            // 通知所有以该单位为目标的角色重新决策
            battleRoom.ForEachUnit(unit =>
            {
                BattleActionDecisionComponent decision = unit.GetComponent<BattleActionDecisionComponent>();
                if (decision != null)
                {
                    BattleUnit currentTarget = decision.CurrentTarget;
                    if (currentTarget != null && currentTarget.Id == deadUnit.Id)
                    {
                        decision.MakeDecision();
                    }
                }
            });

            // 释放该单位占用的站位插槽
            SlotManagerComponent slotManager = battleRoom.GetComponent<SlotManagerComponent>();
            if (slotManager != null)
            {
                slotManager.ReleaseSlot(deadUnit.Id);
                slotManager.ReleaseAllSlotsForTarget(deadUnit.Id);
            }
            
            if (deadUnit.Camp == UnitCamp.Enemy)
            {
                WaveManagerComponent waveManager = battleRoom.GetComponent<WaveManagerComponent>();
                if (waveManager != null)
                {
                    await waveManager.OnMonsterDead(deadUnit.Id);
                }
            }
            else
            {
                bool allHeroesDead = true;
                battleRoom.ForEachUnit(unit =>
                {
                    if (unit.Camp == UnitCamp.Friend)
                    {
                        allHeroesDead = false;
                    }
                });
                
                if (allHeroesDead)
                {
                    await OnBattleFailed(battleRoom);
                }
            }
            
            await ETTask.CompletedTask;
        }
        
        private async ETTask OnBattleFailed(BattleRoom battleRoom)
        {
            battleRoom.State = BattleState.End;
            
            M2C_BattleEnd battleEndMsg = M2C_BattleEnd.Create();
            battleEndMsg.battleId = battleRoom.Id;
            battleEndMsg.success = false;
            battleEndMsg.duration = 0;
            
            BroadcastToBattleRoom(battleRoom, battleEndMsg);
            
            await battleRoom.Root().GetComponent<TimerComponent>().WaitAsync(3000);
            
            Scene mapScene = battleRoom.Scene();
            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager != null)
            {
                foreach (long playerId in battleRoom.PlayerIds)
                {
                    roomManager.RemoveUnitFromBattleRoom(playerId);
                }
                roomManager.RemoveBattleRoom(battleRoom.Id);
            }
            
            battleRoom.Dispose();
        }
        
        private void BroadcastToBattleRoom(BattleRoom battleRoom, IMessage message)
        {
            Scene mapScene = battleRoom.Root();
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            
            foreach (long playerId in battleRoom.PlayerIds)
            {
                Unit player = unitComponent.Get(playerId);
                if (player != null)
                {
                    MapMessageHelper.SendToClient(player, message);
                }
            }
        }
    }
}
