using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(BattleUnitViewComponent))]
    [FriendOf(typeof(BattleUnitViewComponent))]
    [FriendOf(typeof(BattleUnitView))]
    public static partial class BattleUnitViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleUnitViewComponent self)
        {
        }
        
        [EntitySystem]
        private static void Destroy(this BattleUnitViewComponent self)
        {
            foreach (var view in self.Views.Values)
            {
                view.Dispose();
            }
            self.Views.Clear();
        }
        
        /// <summary>
        /// 创建单位表现（异步加载 Prefab）
        /// </summary>
        public static async UniTask<BattleUnitView> CreateViewAsync(this BattleUnitViewComponent self, BattleUnit unit)
        {
            if (self.Views.ContainsKey(unit.Id))
            {
                Log.Warning($"单位表现已存在: UnitId={unit.Id}");
                return self.Views[unit.Id];
            }
            
            BattleUnitView view = self.AddChildWithId<BattleUnitView,UnitCamp, float3>(unit.Id, unit.Camp, unit.Position);
            
            // 通过 TEngine 加载 Prefab
            view.GameObject = await GameModule.Resource.LoadGameObjectAsync(BattleAreaConfig.BattleUnitViewPrefabPath);
            
            if (view.GameObject == null)
            {
                Log.Error($"加载战斗单位 Prefab 失败: {BattleAreaConfig.BattleUnitViewPrefabPath}");
                view.Dispose();
                return null;
            }
            
            view.GameObject.name = $"Unit_{unit.Id}";
            
            // 使用服务端直传的战斗世界坐标，客户端仅做 XZ -> XY 映射
            Vector3 worldPos = BattleAreaConfig.GetWorldPosition(view.Camp, view.Position);
            view.GameObject.transform.position = worldPos;
            
            // 设置阵营
            view.GameObject.transform.localScale = new Vector3(view.Camp == UnitCamp.Friend ? 1f : -1f, 1f, 1f);
            view.InitPresentation();
            
            self.Views[view.UnitId] = view;
            
            Log.Info($"创建单位 UnitId={view.UnitId}, Camp={view.Camp}, Pos={worldPos}");
            
            return view;
        }
        
        /// <summary>
        /// 移除单位表现
        /// </summary>
        public static void RemoveView(this BattleUnitViewComponent self, long unitId)
        {
            if (self.Views.TryGetValue(unitId, out BattleUnitView view))
            {
                view.Dispose();
                self.Views.Remove(unitId);
                Log.Info($"移除单位表现: UnitId={unitId}");
            }
        }
        
        /// <summary>
        /// 更新单位位置，timer为0表示瞬移
        /// </summary>
        public static void UpdateViewPosition(this BattleUnitViewComponent self, long unitId, float3 position,float timer = 0)
        {
            Debug.Log("更新位置");
            if (self.Views.TryGetValue(unitId, out BattleUnitView view))
            {
                view.UpdatePosition(position,timer);
            }
        }

        public static void PlayAttackFeedback(this BattleUnitViewComponent self, Scene root, long unitId)
        {
            if (self.Views.TryGetValue(unitId, out BattleUnitView view))
            {
                view.PlayAttackFeedback();
            }
        }

        public static void PlayHitFeedback(this BattleUnitViewComponent self, Scene root, long unitId, int damage)
        {
            if (self.Views.TryGetValue(unitId, out BattleUnitView view))
            {
                view.PlayHitFeedback(damage);
            }
        }
        
        /// <summary>
        /// 获取所有单位表现
        /// </summary>
        public static List<BattleUnitView> GetAllViews(this BattleUnitViewComponent self)
        {
            return new List<BattleUnitView>(self.Views.Values);
        }
    }
}
