using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(ET.Server.LRUCache))]
    [FriendOfAttribute(typeof(ET.Server.LRUCache))]
    [FriendOfAttribute(typeof(ET.Server.LRUNode))]
    public static partial class LRUCacheSystem
    {
        [EntitySystem]
        private static void Awake(this ET.Server.LRUCache self)
        {
            self.MinFrequency = 0;
            self.FrequencyDic.Add(0,new LinkedList<EntityRef<LRUNode>>());
        }
        [EntitySystem]
        private static void Destroy(this ET.Server.LRUCache self)
        {
            self.LRUNodeDict.Clear();
            self.FrequencyDic.Clear();
            self.MinFrequency = 0;
        }

        public static void Call(this LRUCache self, long key)
        {
            EntityRef<LRUNode> nodeRef;
            LRUNode n;
            if (self.LRUNodeDict.TryGetValue(key,out nodeRef))
            {
                n = nodeRef;
                self.FrequencyDic[n.Frequency].Remove(n);
                n.Frequency++;
                if (!self.FrequencyDic.ContainsKey(n.Frequency))
                {
                    self.FrequencyDic.Add(n.Frequency,new LinkedList<EntityRef<LRUNode>>());
                }

                self.FrequencyDic[n.Frequency].AddLast(n);
                if (self.FrequencyDic[self.MinFrequency].Count == 0)
                {
                    self.MinFrequency = n.Frequency;
                }
                return;
            }

            n = self.AddChild<LRUNode, long>(key);
            n.Frequency = 0;

            self.FrequencyDic[0].AddLast(n);
            self.MinFrequency = 0;
            self.LRUNodeDict[key] = n;

            if (self.LRUNodeDict.Count >= 3000)
            {
                LRUNode fn = self.FrequencyDic[self.MinFrequency].First.Value;
                long unitId = fn.Key;
                self.FrequencyDic[self.MinFrequency].RemoveFirst();
                self.LRUNodeDict.Remove(unitId);
                fn.Dispose();

                EventSystem.Instance.Invoke((long)SceneType.UnitCache, new LRUUnitCacheDelete() { LruCache = self, key = unitId });
            }
        }
    }
}