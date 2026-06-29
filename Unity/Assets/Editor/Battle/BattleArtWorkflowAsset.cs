#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameNetty.EditorTools
{
    [CreateAssetMenu(fileName = "ArtWorkflow", menuName = "GameNetty/美术/美术工作流")]
    public sealed class BattleArtWorkflowAsset : ScriptableObject
    {
        public string WorkflowName = "美术工作流";
        public string Model = "gpt-image-2";
        public string Size = "2048x1152";
        public string Quality = "high";
        public string OutputFormat = "png";
        public string OutputDir = "Unity/Assets/AssetRaw/UIRaw/_Incoming/Battle/battle_main";
        public List<BattleArtWorkflowNode> Roots = new()
        {
            BattleArtWorkflowNode.Create("Global"),
        };
    }

    [Serializable]
    public sealed class BattleArtWorkflowNode
    {
        public string NodeId = Guid.NewGuid().ToString("N");
        public string Name = "节点";
        public string SizeOverride = string.Empty;
        public string OutputDirOverride = string.Empty;
        public bool OverrideParentPrompt;
        public bool OverrideParentNegativePrompt;
        [TextArea(3, 8)]
        public string Prompt = string.Empty;
        [TextArea(2, 5)]
        public string NegativePrompt = string.Empty;
        public List<BattleArtWorkflowNode> Children = new();

        public static BattleArtWorkflowNode Create(string name)
        {
            return new BattleArtWorkflowNode
            {
                Name = name,
            };
        }
    }
}
#endif
