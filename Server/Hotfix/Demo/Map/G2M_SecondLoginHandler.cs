namespace ET.Server
{
    [MessageLocationHandler(SceneType.Map)]
    public class G2M_SecondLoginHandler:MessageLocationHandler<Unit,G2M_SecondLogin,M2G_SecondLogin>
    {
        protected override async ETTask Run(Unit unit, G2M_SecondLogin request, M2G_SecondLogin response)
        {
            Scene scene = unit.Root();
            
            M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
            m2CStartSceneChange.SceneInstanceId = scene.InstanceId;
            m2CStartSceneChange.SceneName = scene.Name;
            MapMessageHelper.SendToClient(unit,m2CStartSceneChange);
            
            M2C_CreateMyUnit m2CCreateMyUnit = M2C_CreateMyUnit.Create();
            m2CCreateMyUnit.Unit = UnitHelper.CreateUnitInfo(unit);
            MapMessageHelper.SendToClient(unit,m2CCreateMyUnit);
            
            await ETTask.CompletedTask;
        }
    }
}

