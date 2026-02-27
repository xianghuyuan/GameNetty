namespace ET.Server
{
    [MessageHandler(SceneType.Rank)]
    public class C2Rank_GetRanksInfoHandler:MessageHandler<Scene, C2Rank_GetRanksInfo,Rank2C_GetRanksInfo>
    {
        protected override async ETTask Run(Scene unit, C2Rank_GetRanksInfo request, Rank2C_GetRanksInfo response)
        {
            RankComponent rankComponent = unit.GetComponent<RankComponent>();
            response.RankInfoProtoList = rankComponent.GetRankInfos();

            await ETTask.CompletedTask;
        }
    }
}