using Cysharp.Threading.Tasks;
using ET;
namespace GameLogic
{
    [Window(UILayer.UI)]
    partial class LoginUI : UIWindow
    {
        private partial UniTaskVoid OnClickLoginBtn()
        {
            LoginHelper.Login(global::Init.Root,m_inputAccount.text,m_inputPassword.text).Coroutine();
            return default;
        }
    }
}

