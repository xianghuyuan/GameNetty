namespace GameLogic
{
    public partial class BattleMonsterinfoUI : UIWindow
    {
        private long _battleUnitId;
        
        /// <summary>
        /// 设置战斗单位数据
        /// </summary>
        public void SetBattleUnit(long battleUnitId)
        {
            _battleUnitId = battleUnitId;
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
        
        private partial void OnSliderChange(float value)
        {
            // 血条变化不需要额外处理
        }
    }
}