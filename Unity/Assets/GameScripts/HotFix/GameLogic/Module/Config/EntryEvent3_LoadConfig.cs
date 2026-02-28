using Cysharp.Threading.Tasks;
using YooAsset;
using TEngine;
using TEngine.TEngine;

namespace ET
{
    /// <summary>
    /// 配置加载事件 - 在 EntryEvent3 时加载配置
    /// 注意：依赖 TEngine ResourceModule 初始化 YooAsset
    /// </summary>
    [Event(SceneType.Main)]
    public class EntryEvent3_LoadConfig : AEvent<Scene, EntryEvent3>
    {
        protected override async ETTask Run(Scene root, EntryEvent3 args)
        {
            await UniTask.WaitUntil(()=>ResourceHelper.IsDefaultPackageReady);
            
            Log.Info("[ConfigLoader] ✅ DefaultPackage 已就绪，开始加载配置");
            
            // 添加配置组件并加载
            ConfigComponent configComponent = root.AddComponent<ConfigComponent>();
            ConfigLoader.CurrentMode = ConfigLoader.LoadMode.AssetBundle;
            configComponent.Load();
            
            Log.Info("[ConfigLoader] ✅ 配置表加载完成");
            
            await ETTask.CompletedTask;
        }
    }
}
