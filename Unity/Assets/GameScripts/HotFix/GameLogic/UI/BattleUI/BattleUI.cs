using ET;

namespace GameLogic
{
    public partial class BattleUI
    {
        private async partial void OnClickBeginBtn()
        {
            await BattleHelper.StartBattle(global::Init.Root);
            BattleHelper.BattleReady(global::Init.Root).Coroutine();
        }
    }
}