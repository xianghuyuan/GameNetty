using System;

namespace GameLogic
{
    public sealed partial class VehicleWidget : UIWidget
    {
        private Action _clickHandler;

        protected override void OnCreate()
        {
            SetBuffIconCount(0);
        }

        private partial void OnClickvehicleBtn()
        {
            _clickHandler?.Invoke();
        }

        public void SetClickHandler(Action clickHandler)
        {
            _clickHandler = clickHandler;
        }

        public void SetPanelVisible(bool visible)
        {
            Visible = visible;
        }

        public void Refresh(string info, int buffIconCount)
        {
            m_tmpInfo.SetText(info ?? string.Empty);
            SetBuffIconCount(buffIconCount);
        }

        private void SetBuffIconCount(int count)
        {
            if (m_gobuff != null)
            {
                m_gobuff.SetActive(count > 0);
            }
        }

    }
}
