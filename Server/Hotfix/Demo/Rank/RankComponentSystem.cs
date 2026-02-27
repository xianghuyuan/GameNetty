using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(RankComponent))]
    [FriendOf(typeof(RankEntity))]
    [FriendOf(typeof(RankComponent))]
    public static partial class RankComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RankComponent self)
        {
            
        }
        
        [EntitySystem]
        private static void Destroy(this RankComponent self)
        {
            
        }

        public static async ETTask LoadRankInfos(this RankComponent self)
        {
            RankEntity rankEntity1 = self.AddChild<RankEntity>();
            rankEntity1.Name = "张三";
            rankEntity1.UnitId = 100;
            self.RankInfos.Add(rankEntity1);
            
            RankEntity rankEntity2 = self.AddChild<RankEntity>();
            rankEntity2.Name = "李四";
            rankEntity2.UnitId = 101;
            self.RankInfos.Add(rankEntity2);
            
            await ETTask.CompletedTask;
        }

        public static List<RankInfoProto> GetRankInfos(this RankComponent self)
        {
            List<RankInfoProto> list = new List<RankInfoProto>();
            foreach (RankEntity rankEntity in self.RankInfos)
            {
                RankInfoProto rankInfoProto = RankInfoProto.Create();
                rankInfoProto.Id = rankEntity.Id;
                rankInfoProto.UnitId = rankEntity.UnitId;
                rankInfoProto.Name = rankEntity.Name;
                list.Add(rankInfoProto);
            }
            return list;
        }
    }
}