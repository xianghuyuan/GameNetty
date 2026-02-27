using System;
using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class ChatInfoUnitsComponent : Entity,IAwake,IDestroy
    {
        public Dictionary<long,EntityRef<ChatInfoUnit>> ChatInfoUnitsDict = new();
    }
}