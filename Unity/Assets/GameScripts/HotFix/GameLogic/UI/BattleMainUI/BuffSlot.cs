using System;
using ET;

namespace GameLogic
{
    public sealed partial class BuffSlot : UIWidget
    {
        private Action<int> _removeHandler;
        private int _slotIndex;
        private bool _hasBuff;

        protected override void OnCreate()
        {
        }

        private partial void OnClickRemoveBtn()
        {
            if (!_hasBuff)
            {
                return;
            }

            _removeHandler?.Invoke(_slotIndex);
        }

        public void SetRemoveHandler(Action<int> removeHandler)
        {
            _removeHandler = removeHandler;
        }

        public void Refresh(int slotIndex, EmitterEffectPackConfig config)
        {
            _slotIndex = slotIndex;
            _hasBuff = config != null;
            Visible = true;
            m_tmpName.SetText(_hasBuff ? config.Name : "空槽");
            m_tmpDesc.SetText(_hasBuff ? config.Desc : "未装配");
            m_btnRemove.SetInteractable(_hasBuff);
        }

    }
}
