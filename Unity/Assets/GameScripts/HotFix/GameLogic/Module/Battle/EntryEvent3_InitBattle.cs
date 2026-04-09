namespace ET
{
    [Event(SceneType.Main)]
    public class EntryEvent3_InitBattle : AEvent<Scene, EntryEvent3>
    {
        protected override async ETTask Run(Scene root, EntryEvent3 args)
        {
            // 添加 BattleComponent 到 Main 场景
            root.AddComponent<BattleComponent>();
            // 挂载 ET↔TE 事件桥接器
            root.AddComponent<EventBridgeComponent>();
            
            Log.Info("BattleComponent & EventBridgeComponent 初始化完成");
            
            await ETTask.CompletedTask;
        }
    }
}
