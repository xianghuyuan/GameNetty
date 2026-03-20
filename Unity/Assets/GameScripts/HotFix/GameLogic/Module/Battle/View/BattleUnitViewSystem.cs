using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(BattleUnitView))]
    [FriendOf(typeof(BattleUnitView))]
    public static partial class BattleUnitViewSystem
    {
        [EntitySystem]
        private static void Awake(this BattleUnitView self, UnitCamp camp, float3 position)
        {
            self.UnitId = self.Id;
            self.Camp = camp;
            self.Position = position;
        }
        
        [EntitySystem]
        private static void Destroy(this BattleUnitView self)
        {
            if (self.GameObject != null)
            {
                UnityEngine.Object.Destroy(self.GameObject);
                self.GameObject = null;
                self.SpriteRenderer = null;
            }
        }
        
        /// <summary>
        /// 更新位置
        /// </summary>
        public static void UpdatePosition(this BattleUnitView self, float3 position)
        {
            self.Position = position;
            
            if (self.GameObject != null)
            {
                Vector3 worldPos = BattleAreaConfig.GetWorldPosition(self.Camp, position);
                self.GameObject.transform.position = worldPos;
            }
        }
        
        /// <summary>
        /// 设置颜色
        /// </summary>
        public static void SetColor(this BattleUnitView self, Color color)
        {
            if (self.SpriteRenderer != null)
            {
                self.SpriteRenderer.color = color;
            }
        }
        
        /// <summary>
        /// 设置可见性
        /// </summary>
        public static void SetVisible(this BattleUnitView self, bool visible)
        {
            if (self.GameObject != null)
            {
                self.GameObject.SetActive(visible);
            }
        }
    }
}
