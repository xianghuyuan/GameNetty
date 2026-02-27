using System.Collections.Generic;

namespace ET
{
    //unit和battle unit
    [ComponentOf()]
    public class NumericNoticeComponent : Entity,IAwake,IDestroy
    {
        public Dictionary<int, M2C_NoticeNumericMsg> OutPutMessageDict = new Dictionary<int, M2C_NoticeNumericMsg>();

        public Queue<IMessage> QueueMessage = new Queue<IMessage>();

        public long SyncTimerId = 0;
        public long SyncTime = 0;
        public long LastSendTime = 0;
    }
}