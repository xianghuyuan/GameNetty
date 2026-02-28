using UnityEngine;

namespace ET
{
	[MessageHandler(SceneType.Main)]
	public class M2C_StartSceneChangeHandler : MessageHandler<Scene, M2C_StartSceneChange>
	{
		protected override async ETTask Run(Scene root, M2C_StartSceneChange message)
		{
			Log.Debug("切换场景");
			await SceneChangeHelper.SceneChangeTo(root, message.SceneName, message.SceneInstanceId);
		}
	}
}
