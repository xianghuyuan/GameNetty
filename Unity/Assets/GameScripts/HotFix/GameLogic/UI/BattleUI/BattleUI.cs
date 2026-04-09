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
            await BattleHelper.StartBattle(global::Init.Root);
            BattleHelper.BattleReady(global::Init.Root).Coroutine();
        }

        private async partial void OnClickExitBtn()
        {
            if (!_battleActive) return;

            var battleComponent = global::Init.Root.GetComponent<BattleComponent>();
            if (battleComponent == null) return;

            Battle battle = battleComponent.GetCurrentBattle();
            if (battle == null) return;

            bool success = await BattleHelper.ExitBattle(global::Init.Root, battle.BattleId);
            if (success)
            {
                battle.End(false);
                battleComponent.RemoveBattle(battle.BattleId);
                SetBattleActive(false);
                BattleUIHelper.ClearAll();
            }
        }

        public void SetBattleActive(bool active)
        {
            _battleActive = active;
            if (m_btnBegin != null) m_btnBegin.gameObject.SetActive(!active);
            if (m_btnExit != null) m_btnExit.gameObject.SetActive(active);

            if (m_tmpBegin != null) m_tmpBegin.text = active ? "战斗中..." : "开始战斗";
            if (m_tmpExit != null) m_tmpExit.text = "离开战斗";
        }
    }
}