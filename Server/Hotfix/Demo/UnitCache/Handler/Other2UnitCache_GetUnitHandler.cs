using System;
using System.Collections.Generic;

namespace ET.Server
{
    [MessageHandler(SceneType.UnitCache)]
    [FriendOfAttribute(typeof(ET.Server.UnitCacheComponent))]
    public class Other2UnitCache_GetUnitHandler : MessageHandler<Scene, Other2UnitCache_GetUnit, UnitCache2Other_GetUnit>
    {
        protected override async ETTask Run(Scene scene, Other2UnitCache_GetUnit request, UnitCache2Other_GetUnit response)
        {
            UnitCacheComponent unitCacheComponent = scene.GetComponent<UnitCacheComponent>();
            Dictionary<string,Entity> dict = ObjectPool.Instance.Fetch(typeof(Dictionary<string,Entity>)) as Dictionary<string,Entity>;
            try
            {
                if (request.componentNameList.Count == 0)
                {
                    dict.Add("ET.Unit", null);
                    foreach (var s in unitCacheComponent.UnitCacheKeyList)
                    {
                        if (s == "ET.Unit")
                        {
                            continue;
                        }

                        dict.Add(s, null);
                    }
                }
                else
                {
                    foreach (var s in request.componentNameList)
                    {
                        dict.Add(s, null);
                    }
                }

                long unitId = request.unitId;
                CoroutineLockComponent coroutineLockComponent = scene.GetComponent<CoroutineLockComponent>();
                using (await coroutineLockComponent.Wait(CoroutineLockType.UnitCacheGet,unitId))
                {
                    unitCacheComponent.CallCache(unitId);
                    using (ListComponent<string> keyList = new ListComponent<string>())
                    {
                        foreach (var key in dict.Keys)
                        {
                           keyList.Add(key); 
                        }

                        foreach (var key in keyList)
                        {
                            Entity entity = await unitCacheComponent.Get(unitId, key);
                            dict[key] = entity;
                        }
                    }
                }

                foreach (var info in dict)
                {
                    response.ComponentNameList.Add(info.Key);

                    response.EntityList.Add(info.Value?.ToBson() ?? null); 
                }
            }
            
            finally
            {
                dict.Clear();
                ObjectPool.Instance.Recycle(dict);
            }
            
        }
    }
}