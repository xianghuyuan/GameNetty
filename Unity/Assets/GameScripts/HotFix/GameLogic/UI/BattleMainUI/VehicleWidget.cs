using System;

namespace GameLogic
{
    public sealed partial class VehicleWidget : UIWidget
    {
        private Action<long> _clickHandler;
        private long _vehicleId;

        protected override void OnCreate()
        {
        }

        private partial void OnClickvehicleBtn()
        {
            _clickHandler?.Invoke(_vehicleId);
        }

        public void SetClickHandler(Action<long> clickHandler)
        {
            _clickHandler = clickHandler;
        }

        public void SetPanelVisible(bool visible)
        {
            Visible = visible;
        }

        public void Refresh(long vehicleId, string info, int buffIconCount)
        {
            _vehicleId = vehicleId;
            m_tmpInfo.SetText(info ?? string.Empty);
            SetBuffIconCount(buffIconCount);
        }

        private void SetBuffIconCount(int count)
        {
            int childCount = m_tfbuff.childCount;
            for (int i = childCount; i < count; i++)
            {
                var obj = UnityEngine.Object.Instantiate(m_gobuff, m_tfbuff);
                obj.SetActive(true);
            }

            for (int i = 0; i < m_tfbuff.childCount; i++)
            {
                m_tfbuff.GetChild(i).gameObject.SetActive(i < count);
            }
        }
    }
}
