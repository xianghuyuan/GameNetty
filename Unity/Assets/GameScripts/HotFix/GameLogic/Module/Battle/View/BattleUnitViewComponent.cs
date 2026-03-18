using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 战斗单位表现管理组件
    /// 挂载在 Battle 上，管理所有单位的 2D 表现
    /// </summary>
    [ComponentOf(typeof(Battle))]
    public class BattleUnitViewComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// UnitId -> BattleUnitView 映射
        /// </summary>
        public Dictionary<long, BattleUnitView> Views { get; } = new();
    }
}
