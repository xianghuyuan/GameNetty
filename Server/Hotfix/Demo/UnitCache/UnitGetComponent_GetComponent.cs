using System;

namespace ET.Server
{
    [Event(SceneType.All)]
    public class UnitGetComponent_GetComponent:AEvent<Scene,UnitGetComponent>
    {
        protected override async ETTask Run(Scene scene, UnitGetComponent args)
        {
            Unit unit = args.Unit;
            Type type = args.Type;
            unit.GetComponent<UnitDBSaveComponent>()?.AddChange(type);
            
            //判定Unit身上是否存在需要获得的组件
            if (unit.Components.ContainsKey(type.TypeHandle.Value.ToInt64()))
            {
                return;
            }
            UnitDBSaveComponent unitDBSaveComponent = unit.GetComponent<UnitDBSaveComponent>();
            if (unitDBSaveComponent==null)
            {
                return;
            }

            if (!unit.GetComponent<UnitDBSaveComponent>().Bytes.TryGetValue(type,out byte[] bs))
            {
                return;
            }
            
            Entity t = MongoHelper.Deserialize(type,bs) as Entity;
            unit.AddComponent(t);
            await ETTask.CompletedTask;
        }
    }
}