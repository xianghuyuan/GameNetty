using System;

namespace GameLogic
{
    public sealed partial class BattleGMWidget : UIWidget
    {
        private Action _createEmitterHandler;
        private Action _deleteEmitterHandler;
        private Action _addBuffHandler;
        private Action _spawnEnemyHandler;

        private partial void OnClickCreateEmitterBtn()
        {
            _createEmitterHandler?.Invoke();
        }

        private partial void OnClickDeleteEmitterBtn()
        {
            _deleteEmitterHandler?.Invoke();
        }

        private partial void OnClickAddBuffBtn()
        {
            _addBuffHandler?.Invoke();
        }

        private partial void OnClickSpawnEnemyBtn()
        {
            _spawnEnemyHandler?.Invoke();
        }

        public void SetHandlers(
            Action createEmitterHandler,
            Action deleteEmitterHandler,
            Action addBuffHandler,
            Action spawnEnemyHandler)
        {
            _createEmitterHandler = createEmitterHandler;
            _deleteEmitterHandler = deleteEmitterHandler;
            _addBuffHandler = addBuffHandler;
            _spawnEnemyHandler = spawnEnemyHandler;
        }

        public void Refresh(bool canCreateEmitter, bool canDeleteEmitter, bool canAddBuff, bool canSpawnEnemy)
        {
            if (m_btnCreateEmitter != null) m_btnCreateEmitter.interactable = canCreateEmitter;
            if (m_btnDeleteEmitter != null) m_btnDeleteEmitter.interactable = canDeleteEmitter;
            if (m_btnAddBuff != null) m_btnAddBuff.interactable = canAddBuff;
            if (m_btnSpawnEnemy != null) m_btnSpawnEnemy.interactable = canSpawnEnemy;
        }
    }
}
