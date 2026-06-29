#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace GameNetty.EditorTools
{
    [CreateAssetMenu(fileName = "BattleEmitterPreset", menuName = "GameNetty/战斗/发射器预设")]
    public sealed class BattleEmitterPresetAsset : ScriptableObject
    {
        public List<BattleEmitterPresetEntry> Emitters = new()
        {
            new BattleEmitterPresetEntry(),
        };
    }

    [System.Serializable]
    public sealed class BattleEmitterPresetEntry
    {
        public string Name = "Emitter";
        public int CooldownMs = 1000;
        public float Range = 3f;
        public int BuffSlotCount = 4;
        public bool CanMoveCast;
        // 槽位里存放 BuffGroupConfig.Id
        public List<int> BuffGroupIds = new();
    }
}
#endif
