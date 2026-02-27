namespace ET.Server
{
    [Invoke(TimerInvokeType.NumericSync)]
    public class NumericSyncTimerHandler: ATimer<NumericNoticeComponent>
    {
        protected override void Run(NumericNoticeComponent self)
        {
            self?.NoticeQueueMsgImmediately();
        }
    }
    
    [EntitySystemOf(typeof(NumericNoticeComponent))]
    [FriendOf(typeof(NumericNoticeComponent))]
    public static partial class NumericNoticeComponentSystem
    {

        [EntitySystem]
        private static void Awake(this NumericNoticeComponent self)
        {
            
        }
        [EntitySystem]
        private static void Destroy(this NumericNoticeComponent self)
        {
            self.Root().GetComponent<TimerComponent>().Remove(ref self.SyncTimerId);
            self.LastSendTime = 0;
            self.SyncTime = 0;

            for (int i = 0; i < self.QueueMessage.Count; i++)
            {
                M2C_NoticeNumericMsg queueMsg = (M2C_NoticeNumericMsg)self.QueueMessage.Dequeue();
                queueMsg?.Dispose();
            }

            foreach (var queueMsg in self.OutPutMessageDict.Values)
            {
                queueMsg?.Dispose();
            }
            
            self.OutPutMessageDict.Clear();
            self.QueueMessage.Clear();
            self.QueueMessage = default;
            self.OutPutMessageDict = default;
        }
        public static void Notice(this NumericNoticeComponent self,int numericType,long newValue)
        {
            if (self.LastSendTime >0 && TimeInfo.Instance.ServerNow() - self.LastSendTime<100)
            {
                self.AddQueueMessage(numericType, newValue);
                self.CheckSyncTimer();
            }
            else
            {
                self.NoticeImmdiately(numericType, newValue);
            }
        }
        
        public static void NoticeImmdiately(this NumericNoticeComponent self, int numericType, long newValue)
        {
            Unit unit = self.GetParent<Unit>();
            M2C_NoticeUnitNumeric singleNumericMessage = M2C_NoticeUnitNumeric.Create();
            singleNumericMessage.UnitId = unit.Id;
            singleNumericMessage.NumericType = numericType;
            singleNumericMessage.NewValue = newValue;
            self.LastSendTime = TimeInfo.Instance.ServerNow();
            
            MapMessageHelper.SendToClient(unit,singleNumericMessage);
        }

        public static void AddQueueMessage(this NumericNoticeComponent self,int numericType,long newValue)
        {
            if (self.OutPutMessageDict.TryGetValue(numericType,out M2C_NoticeNumericMsg msg))
            {
                msg.NewValue = newValue;
            }
            else
            {
                msg = M2C_NoticeNumericMsg.Create();
                msg.NumericType = numericType;
                msg.NewValue = newValue;
                self.OutPutMessageDict.Add(numericType,msg);
                self.QueueMessage.Enqueue(msg);
            }
        }

        public static void CheckSyncTimer(this NumericNoticeComponent self)
        {
            if (self.SyncTime<TimeInfo.Instance.ServerNow())
            {
                if (self.SyncTimerId != 0)
                {
                    self.Root().GetComponent<TimerComponent>().Remove(ref self.SyncTimerId);
                }

                self.SyncTime = TimeInfo.Instance.ServerNow() + 100;
                self.SyncTimerId = self.Root().GetComponent<TimerComponent>().NewOnceTimer(self.SyncTime, TimerInvokeType.NumericSync, self);
            }
        }

        public static void NoticeQueueMsgImmediately(this NumericNoticeComponent self)
        {
            int queueMsgNum = self.QueueMessage.Count;
            if (queueMsgNum<=0)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            self.OutPutMessageDict.Clear();
            
            M2C_NoticeUnitNumericList MultiNumericMessage = M2C_NoticeUnitNumericList.Create(true);
            MultiNumericMessage.UnitId = unit.Id;

            int messageCount = self.QueueMessage.Count;
            for (int i = 0; i < messageCount; i++)
            {
                M2C_NoticeNumericMsg queueMsg = (M2C_NoticeNumericMsg)self.QueueMessage.Dequeue();
                MultiNumericMessage.NumericTypeList.Add(queueMsg.NumericType);
                MultiNumericMessage.NewValueList.Add(queueMsg.NewValue);
                queueMsg?.Dispose();
            }

            self.LastSendTime = TimeInfo.Instance.ServerNow();
            MapMessageHelper.SendToClient(unit,MultiNumericMessage);
        }
    }
}