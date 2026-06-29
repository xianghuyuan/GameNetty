using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 战斗单位注册表 - 挂在 BattleRoom 上，统一管理所有 BattleUnit 的注册与查询。
    /// 接管原 BattleRoom.Units 字典的查询职责。
    /// </summary>
    [ComponentOf(typeof(BattleRoom))]
    public class BattleUnitRegistryComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, EntityRef<BattleUnit>> Units { get; } = new();
    }
}
