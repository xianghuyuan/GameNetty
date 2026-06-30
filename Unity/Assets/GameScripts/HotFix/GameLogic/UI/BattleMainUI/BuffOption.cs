using System;
using ET;

namespace GameLogic
{
    public sealed partial class BuffOption : UIWidget
    {
        private Action<int> _addHandler;
        private int _effectPackId;
        private bool _canAdd;

        protected override void OnCreate()
        {
        }

        private partial void OnClickAddBtn()
        {
            if (!_canAdd || _effectPackId == 0)
            {
                return;
            }

            _addHandler?.Invoke(_effectPackId);
        }

        public void SetAddHandler(Action<int> addHandler)
        {
            _addHandler = addHandler;
        }

        public void Refresh(EmitterEffectPackConfig config, bool canAdd)
        {
            Visible = config != null && canAdd;
            if (!Visible)
            {
                _effectPackId = 0;
                _canAdd = false;
                return;
            }

            _effectPackId = config.Id;
            _canAdd = canAdd;
            m_tmpName.SetText(config.Name);
            m_tmpDesc.SetText(config.Desc);
            m_btnAdd.SetInteractable(canAdd);
        }

    }
}
