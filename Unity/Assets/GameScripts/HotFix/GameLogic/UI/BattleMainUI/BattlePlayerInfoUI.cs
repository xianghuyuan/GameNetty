namespace GameLogic
{
    public partial class BattlePlayerInfoUI : UIWindow
    {
        private long _battleUnitId;
        
        /// <summary>
        /// 设置战斗单位数据
        /// </summary>
        public void SetBattleUnit(long battleUnitId)
        {
            _battleUnitId = battleUnitId;
            RefreshData();
        }
        
        /// <summary>
        /// 直接设置数据显示
        /// </summary>
        public void SetData(long hp, long maxHp, long attack, float range = 1.5f)
        {
            m_tmpHP.text = $"HP: {hp}/{maxHp}";
            m_slider.value = maxHp > 0 ? (float)hp / maxHp : 0;
            m_tmpAk.text = $"ATK: {attack}";
            m_tmpAkDistance.text = $"Range: {range}";
        }
        
        /// <summary>
        /// 刷新数据显示（需要在ET上下文中调用）
        /// </summary>
        public void RefreshData()
        {
            // 此方法需要在ET上下文中调用，由外部传入数据
        }
        
        private partial void OnSliderChange(float value)
        {
            // 血条变化不需要额外处理
        }
    }
}