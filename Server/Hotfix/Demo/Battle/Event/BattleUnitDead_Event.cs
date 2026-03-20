using System.Diagnostics;

namespace ET.Server
{
    [Event(SceneType.Battle)]
    [FriendOf(typeof(WaveManagerComponent))]
    [FriendOf(typeof(BattleRoom))]
    public class BattleUnitDead_Event : AEvent<Scene, BattleUnitDead>
    {
        protected override async ETTask Run(Scene scene, BattleUnitDead args)
        {
            Log.Debug("攻击伤害计算");
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
            
            Log.Info($"战斗单位死亡: UnitId={deadUnit.Id}, Camp={deadUnit.Camp}, ConfigId={deadUnit.ConfigId}");
            
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
                foreach (var kv in battleRoom.Units)
                {
                    BattleUnit unit = kv.Value;
                    if (unit != null && unit.Camp == UnitCamp.Friend && !unit.IsDead)
                    {
                        allHeroesDead = false;
                        break;
                    }
                }
                
                if (allHeroesDead)
                {
                    await OnBattleFailed(battleRoom);
                }
            }
            
            await ETTask.CompletedTask;
        }
        
        private async ETTask OnBattleFailed(BattleRoom battleRoom)
        {
            Log.Info($"战斗失败: BattleRoomId={battleRoom.Id}");
            
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
