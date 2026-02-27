using System;
namespace ET.Server
{
    
    [Invoke(TimerInvokeType.SaveChangeDBData)]
    public class UnitDBSaveComponentTimer:ATimer<UnitDBSaveComponent>
    {
        protected override void Run(UnitDBSaveComponent self)
        {
            try
            {
                self?.SaveChange();
            }catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }
    }

    [EntitySystemOf(typeof(UnitDBSaveComponent))]
    [FriendOfAttribute(typeof(UnitDBSaveComponent))]
    //每隔一段时间，执行SaveChange
    public static partial class UnitDBSaveComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ET.Server.UnitDBSaveComponent self)
        {
            self.Timer = self.Root().GetComponent<TimerComponent>().NewRepeatedTimer(4 * 1000, TimerInvokeType.SaveChangeDBData, self);
        }
        [EntitySystem]
        private static void Destroy(this ET.Server.UnitDBSaveComponent self)
        {
            self.Root().GetComponent<TimerComponent>().Remove(ref self.Timer);
        }

        //执行SaveChangeNoWait的时候
        public static void AddToBytes(this ET.Server.UnitDBSaveComponent self,Type type,byte[] bytes)
        {
            self.Bytes[type] = bytes;
        }

        /// <summary>
        /// 通过GetComponent来记录哪些组件被修改过
        /// </summary>
        /// <param name="self"></param>
        /// <param name="type"></param>
        public static void AddChange(this ET.Server.UnitDBSaveComponent self,Type type)
        {
            self.EntityChangeTypes.Add(type);
        }

        public static async ETTask SaveChange(this UnitDBSaveComponent self)
        {
            CoroutineLockComponent coroutineLockComponent = self.Root().GetComponent<CoroutineLockComponent>();
            using (await coroutineLockComponent.Wait(CoroutineLockType.Mailbox,self.GetParent<Unit>().InstanceId))
            {
                self.SaveChangeNoWait();
            }
        }
        
        public static void SaveChangeNoWait(this UnitDBSaveComponent self)
        {
            if (self.IsDisposed || self.Parent == null)
            {
                return;
            }

            if (self.Root() == null)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();

            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            if (self.EntityChangeTypes.Count <= 0)
            {
                return;
            }
            
            Other2UnitCache_AddOrUpdateUnit message = Other2UnitCache_AddOrUpdateUnit.Create();
            message.UnitId = unit.Id;
            message.EntityTypes.Add(unit.GetType().FullName);
            message.EntityBytes.Add(unit.ToBson());
            foreach (var type in self.EntityChangeTypes)
            {
                Entity entity = unit.GetComponent(type);
                if (entity == null)
                {
                    continue;
                }
                Log.Debug("开始保存变化部分Entity数据"+type.FullName);
                byte[] bytes = entity.ToBson();
                message.EntityTypes.Add(type.FullName);
                message.EntityBytes.Add(bytes);
                self.AddToBytes(type,bytes);
            }
            self.EntityChangeTypes.Clear();

            StartSceneConfig unitCacheConfig = StartSceneConfigCategory.Instance.UnitCache;
            self.Root().GetComponent<MessageSender>().Call(unitCacheConfig.ActorId, message).Coroutine();
        }
    }
}