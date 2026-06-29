using Cysharp.Threading.Tasks;
using ET;
namespace GameLogic
{
    [Window(UILayer.UI)]
    partial class LoginUI : UIWindow
    {
        private partial void OnClickLoginBtn()
        {
            LoginHelper.Login(global::Init.Root,m_inputAccount.text,m_inputPassword.text).Coroutine();
        }

        private partial void OnClickOfflineBtn()
        {
            BattleEntry.StartBattle(global::Init.Root, new BattleStartRequest
            {
                Mode = BattleStartMode.Offline,
            }).Coroutine();
        }
    }
}
