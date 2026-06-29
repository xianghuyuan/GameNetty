#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace GameNetty.EditorTools
{
    [CreateAssetMenu(fileName = "BattleLevelPreset", menuName = "GameNetty/战斗/关卡预设")]
    public sealed class BattleLevelPresetAsset : ScriptableObject
    {
        public string LevelName = "Level 001";
        public float PlayerSpawnX = -5f;
        public int PlayerHp = 1000;
        public float PlayerSpeed = 3f;
        public BattleEmitterPresetAsset PlayerEmitterPreset;
        public List<BattleLevelWaveEntry> Waves = new()
        {
            new BattleLevelWaveEntry(),
        };
    }

    [System.Serializable]
    public sealed class BattleLevelWaveEntry
    {
        public string Name = "Wave 1";
        public int DelayMs;
        public List<BattleLevelSpawnEntry> Spawns = new()
        {
            new BattleLevelSpawnEntry(),
        };
    }

    [System.Serializable]
    public sealed class BattleLevelSpawnEntry
    {
        public int DelayMs;
        public int Hp = 6;
        public int Attack = 8;
        public int Defense = 1;
        public float Speed = 1f;
        public int Count = 6;
        public float OffsetFromCamera = 2.5f;
        public float SpreadRange = 4f;
        public BattleEmitterPresetAsset EmitterPreset;
    }
}
#endif
