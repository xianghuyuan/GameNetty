using ET;

namespace GameLogic
{
    public partial class BattleUI
    {
        private partial void OnClickBeginBtn()
        {
            BattleHelper.StartBattle(global::Init.Root).Coroutine();
        }
    }
}