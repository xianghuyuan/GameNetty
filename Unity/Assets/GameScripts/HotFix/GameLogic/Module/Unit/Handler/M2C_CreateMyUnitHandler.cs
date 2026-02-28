using GameLogic;

namespace ET
{
	[MessageHandler(SceneType.Main)]
	public class M2C_CreateMyUnitHandler: MessageHandler<Scene, M2C_CreateMyUnit>
	{
		protected override async ETTask Run(Scene root, M2C_CreateMyUnit message)
		{
			// 打印玩家数值
			Log.Info($"========== 玩家数值 ==========");
			Log.Info($"UnitId: {message.Unit.UnitId}");
			Log.Info($"ConfigId: {message.Unit.ConfigId}");
			foreach (var kv in message.Unit.KV)
			{
				string numericName = NumericHelper.GetNumericName(kv.Key);
				Log.Info($"{numericName}({kv.Key}): {kv.Value}");
			}
			Log.Info($"==============================");
			
			// 通知场景切换协程继续往下走
			root.GetComponent<ObjectWait>().Notify(new Wait_CreateMyUnit() {Message = message});
			await ETTask.CompletedTask;
		}
	}
}
