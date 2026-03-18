using Cysharp.Threading.Tasks;

namespace ET
{
    /// <summary>
    /// 战斗单位创建事件 - 创建 2D 表现
    /// </summary>
    [Event(SceneType.Main)]
    public class BattleUnitCreated_View : AEvent<Scene, BattleUnitCreated>
    {
        protected override async ETTask Run(Scene scene, BattleUnitCreated args)
        {
            Battle battle = args.Battle;
            BattleUnit unit = args.Unit;
            
            BattleUnitViewComponent viewComponent = battle.GetComponent<BattleUnitViewComponent>();
            if (viewComponent == null)
            {
                viewComponent = battle.AddComponent<BattleUnitViewComponent>();
            }
            
            await viewComponent.CreateViewAsync(unit);
            
            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// 战斗单位死亡事件 - 移除表现
    /// </summary>
    [Event(SceneType.Main)]
    public class BattleUnitDead_View : AEvent<Scene, BattleUnitDead>
    {
        protected override async ETTask Run(Scene scene, BattleUnitDead args)
        {
            BattleUnit unit = args.BattleUnit;
            
            Battle battle = unit.GetParent<Battle>();
            if (battle == null)
            {
                return;
            }
            
            BattleUnitViewComponent viewComponent = battle.GetComponent<BattleUnitViewComponent>();
            if (viewComponent != null)
            {
                viewComponent.RemoveView(unit.Id);
            }
            
            await ETTask.CompletedTask;
        }
    }
    
    /// <summary>
    /// 战斗结束事件 - 清理所有表现
    /// </summary>
    [Event(SceneType.Main)]
    public class BattleEnd_View : AEvent<Scene, BattleEnd>
    {
        protected override async ETTask Run(Scene scene, BattleEnd args)
        {
            Battle battle = args.Battle;
            
            BattleUnitViewComponent viewComponent = battle.GetComponent<BattleUnitViewComponent>();
            if (viewComponent != null)
            {
                viewComponent.Dispose();
            }
            
            Log.Info($"战斗结束，清理所有单位表现: BattleId={battle.BattleId}");
            
            await ETTask.CompletedTask;
        }
    }
}
