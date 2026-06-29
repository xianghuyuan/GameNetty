using ET;
using TMPro;
using UnityEngine;

namespace GameLogic
{
    public partial class BattleUI
    {
        private bool _battleActive;

        private async partial void OnClickBeginBtn()
        {
            if (_battleActive) return;
            BattleStartResult result = await BattleEntry.StartBattle(global::Init.Root, new BattleStartRequest
            {
                Mode = BattleStartMode.Online,
                AutoReady = true,
            });
            if (!result.IsSuccess)
            {
                return;
            }
        }

        private async partial void OnClickExitBtn()
        {
            if (!_battleActive) return;

            var battleComponent = global::Init.Root.GetComponent<BattleComponent>();
            if (battleComponent == null) return;

            Battle battle = battleComponent.GetCurrentBattle();
            if (battle == null) return;

            bool success = await BattleEntry.ExitCurrentBattle(global::Init.Root);
            if (success)
            {
                SetBattleActive(false);
            }
        }

        public void SetBattleActive(bool active)
        {
            _battleActive = active;
            if (m_btnBegin != null) m_btnBegin.gameObject.SetActive(!active);
            if (m_btnExit != null) m_btnExit.gameObject.SetActive(active);

            if (m_tmpBegin != null) m_tmpBegin.SetText(active ? "战斗中..." : "开始战斗");
            if (m_tmpExit != null) m_tmpExit.SetText("离开战斗");
        }
    }
}
