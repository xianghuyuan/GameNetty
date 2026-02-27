using System;
using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class UnitDBSaveComponent :Entity,IAwake,IDestroy
    {
        public long Timer;
        //被修改过的组件
        public HashSet<Type> EntityChangeTypes { get; } = new HashSet<Type>();
        //组件对应的字节流
        public Dictionary<Type, byte[]> Bytes { get; } = new Dictionary<Type, byte[]>();
    }
}