namespace ET.Server
{
    [EntitySystemOf(typeof(UnitCache))]
    [FriendOfAttribute(typeof(ET.Server.UnitCache))]
    public static partial class UnitCacheSystem
    {
        [EntitySystem]
        private static void Awake(this ET.Server.UnitCache self)
        {
        }
        [EntitySystem]
        private static void Destroy(this ET.Server.UnitCache self)
        {
            foreach (var entityRef in self.CacheComponentsDic.Values)
            {
                Entity entity = entityRef;
                entity?.Dispose();
            }
            self.CacheComponentsDic.Clear();
            self.key = null;
        }

        public static async ETTask<Entity> Get(this UnitCache self, long unitId)
        {
            Entity entity = null;
            
            if (!self.CacheComponentsDic.TryGetValue(unitId, out EntityRef<Entity> entityRef))
            {
                entity = await self.Root().GetComponent<DBManagerComponent>().GetZoneDB(self.Zone()).Query<Entity>(unitId, self.key);
                if (entity!=null)
                {
                    self.AddOrUpdate(entity);
                }
            }
            else
            {
                entity = entityRef;
            }

            return entity;
        }

        public static void Delete(this UnitCache self,long id)
        {
            if (self.CacheComponentsDic.TryGetValue(id,out EntityRef<Entity> entityRef))
            {
                self.CacheComponentsDic.Remove(id);
                Entity entity = entityRef;
                entity?.Dispose();
            }
        }
        
        public static void AddOrUpdate(this UnitCache self,Entity entity)
        {
            if (entity == null)
            {
                return;
            }

            if (self.CacheComponentsDic.TryGetValue(entity.Id,out EntityRef<Entity> oldEntityRef))
            {
                Entity oldEntity = oldEntityRef;
                if (entity != oldEntity)
                {
                    oldEntity?.Dispose();
                }

                self.CacheComponentsDic.Remove(entity.Id);
            }
            self.CacheComponentsDic.Add(entity.Id,entity);
        }
    }
}