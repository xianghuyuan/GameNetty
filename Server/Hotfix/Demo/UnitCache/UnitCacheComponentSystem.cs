using System;
using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(UnitCacheComponent))]
    [FriendOfAttribute(typeof(ET.Server.UnitCache))]
    [FriendOfAttribute(typeof(ET.Server.UnitCacheComponent))]
    public static partial class UnitCacheComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ET.Server.UnitCacheComponent self)
        {
            self.UnitCacheKeyList.Clear();
            foreach (Type type in CodeTypes.Instance.GetTypes().Values)
            {
                if (type != typeof(IUnitCache) && typeof(IUnitCache).IsAssignableFrom(type))
                {
                    self.UnitCacheKeyList.Add(type.FullName);
                }
            }

            foreach (string key in self.UnitCacheKeyList)
            {
                UnitCache unitCache = self.AddChild<UnitCache>();
                unitCache.key = key;
                self.UnitCaches.Add(key, unitCache);
            }

            self.AddComponent<LRUCache>();
        }
        
        [EntitySystem]
        private static void Destroy(this ET.Server.UnitCacheComponent self)
        {
            foreach (var unitCachesRef in self.UnitCaches.Values)
            {
                UnitCache unitCache = unitCachesRef;
                unitCache?.Dispose();
            }
            self.UnitCaches.Clear();
        }

        public static void CallCache(this UnitCacheComponent self, long id)
        {
            self.GetComponent<LRUCache>().Call(id);
        }

        public static async ETTask<Entity> Get(this UnitCacheComponent self, long unitId,string key )
        {
            UnitCache unitCache = default;
            if (self.UnitCaches.TryGetValue(key, out EntityRef<UnitCache> unitCacheRef))
            {
                unitCache = unitCacheRef;
            }
            else
            {
                unitCache = self.AddChild<UnitCache>();
                unitCache.key = key;
                self.UnitCaches.Add(key, unitCache);
            }
            return await unitCache.Get(unitId);
        }

        public static async ETTask Delete(this UnitCacheComponent self, long unitId)
        {
            using(await self.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.UnitCacheGet, unitId))
            {
                foreach (UnitCache cache in self.UnitCaches.Values)
                {
                    cache.Delete(unitId);
                }
            }
        }
        
        public static async ETTask AddOrUpdate(this UnitCacheComponent self, long unitId, List<Entity> entityList)
        {
            using (ListComponent<Entity> list = ListComponent<Entity>.Create())
            {
                self.CallCache(unitId);
                foreach (var entity in entityList)
                {
                    string key = entity.GetType().FullName;
                    UnitCache unitCache = default;
                    if (!self.UnitCaches.TryGetValue(key,out EntityRef<UnitCache> unitCacheRef))
                    {
                        unitCache = self.AddChild<UnitCache>();
                        unitCache.key = key;
                        self.UnitCaches.Add(key, unitCache);
                    }
                    else
                    {
                        unitCache = unitCacheRef;
                    }
                    
                    unitCache.AddOrUpdate(entity);
                    list.Add(entity);
                }

                if (list.Count > 0)
                {
                    await self.Root().GetComponent<DBManagerComponent>().GetZoneDB(self.Zone()).Save(unitId, list);
                }
            }
            await ETTask.CompletedTask;
        }
    }
}