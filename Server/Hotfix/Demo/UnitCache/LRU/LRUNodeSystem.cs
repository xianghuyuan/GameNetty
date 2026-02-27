namespace ET.Server
{
    [EntitySystemOf(typeof(LRUNode))]
    [FriendOfAttribute(typeof(ET.Server.LRUNode))]
    public static partial class LRUNodeSystem
    {
        [EntitySystem]
        private static void Awake(this ET.Server.LRUNode self, long key)
        {
            self.Key = key;
        }
        [EntitySystem]
        private static void Destroy(this ET.Server.LRUNode self)
        {
            self.Key = 0;
            self.Frequency = 0;
        }
    }
}