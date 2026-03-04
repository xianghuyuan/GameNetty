using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 视差滚动背景管理器
    /// 管理多层背景，根据玩家速度动态调整滚动效果
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [Header("背景层设置")]
        [Tooltip("所有背景层（从远到近排列）")]
        [SerializeField] private ParallaxLayer[] layers;
        
        [Header("速度控制")]
        [Tooltip("全局速度倍率")]
        [SerializeField] private float globalSpeedMultiplier = 1f;
        
        [Tooltip("当前玩家速度")]
        [SerializeField] private float currentPlayerSpeed = 0f;
        
        [Header("平滑过渡")]
        [Tooltip("是否启用速度平滑过渡")]
        [SerializeField] private bool enableSmoothTransition = true;
        
        [Tooltip("速度过渡平滑度（值越大越平滑）")]
        [SerializeField] private float transitionSmoothness = 5f;
        
        [Header("自动设置")]
        [Tooltip("是否在Start时自动查找所有ParallaxLayer子对象")]
        [SerializeField] private bool autoFindLayers = true;
        
        // 运行时状态
        private bool isPaused;
        private float targetPlayerSpeed;
        private bool isInitialized;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// 初始化背景系统
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            // 自动查找所有子对象中的ParallaxLayer
            if (autoFindLayers)
            {
                layers = GetComponentsInChildren<ParallaxLayer>();
            }

            if (layers == null || layers.Length == 0)
            {
                Debug.LogWarning($"[ParallaxBackground] {gameObject.name} 未找到任何ParallaxLayer！", this);
                return;
            }

            // 初始化所有层
            foreach (var layer in layers)
            {
                if (layer != null)
                {
                    layer.Initialize();
                }
            }

            Debug.Log($"[ParallaxBackground] 初始化完成，共 {layers.Length} 层背景");
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || isPaused) return;

            // 平滑过渡速度
            if (enableSmoothTransition)
            {
                currentPlayerSpeed = Mathf.Lerp(
                    currentPlayerSpeed, 
                    targetPlayerSpeed, 
                    Time.deltaTime * transitionSmoothness
                );
            }
            else
            {
                currentPlayerSpeed = targetPlayerSpeed;
            }

            // 计算基础滚动速度
            float baseSpeed = currentPlayerSpeed * globalSpeedMultiplier;

            // 更新所有背景层
            foreach (var layer in layers)
            {
                if (layer != null)
                {
                    layer.UpdateScroll(baseSpeed);
                }
            }
        }

        #region 公共API

        /// <summary>
        /// 设置玩家速度（通常从玩家移动脚本调用）
        /// </summary>
        /// <param name="speed">玩家移动速度</param>
        public void SetPlayerSpeed(float speed)
        {
            targetPlayerSpeed = speed;
        }

        /// <summary>
        /// 暂停所有背景层
        /// </summary>
        public void PauseAll()
        {
            isPaused = true;
            foreach (var layer in layers)
            {
                if (layer != null)
                {
                    layer.Pause();
                }
            }
        }

        /// <summary>
        /// 恢复所有背景层
        /// </summary>
        public void ResumeAll()
        {
            isPaused = false;
            foreach (var layer in layers)
            {
                if (layer != null)
                {
                    layer.Resume();
                }
            }
        }

        /// <summary>
        /// 重置所有背景层的UV偏移
        /// </summary>
        public void ResetAll()
        {
            foreach (var layer in layers)
            {
                if (layer != null)
                {
                    layer.ResetOffset();
                }
            }
            currentPlayerSpeed = 0f;
            targetPlayerSpeed = 0f;
        }

        /// <summary>
        /// 设置全局速度倍率
        /// </summary>
        /// <param name="multiplier">速度倍率</param>
        public void SetGlobalSpeedMultiplier(float multiplier)
        {
            globalSpeedMultiplier = multiplier;
        }

        /// <summary>
        /// 获取指定索引的背景层
        /// </summary>
        public ParallaxLayer GetLayer(int index)
        {
            if (index >= 0 && index < layers.Length)
            {
                return layers[index];
            }
            return null;
        }

        /// <summary>
        /// 启用/禁用速度平滑过渡
        /// </summary>
        public void SetSmoothTransition(bool enable)
        {
            enableSmoothTransition = enable;
        }

        #endregion

        #region 属性访问器
        public bool IsPaused => isPaused;
        public float CurrentPlayerSpeed => currentPlayerSpeed;
        public float GlobalSpeedMultiplier => globalSpeedMultiplier;
        public int LayerCount => layers?.Length ?? 0;
        #endregion

        #region 编辑器辅助
        private void OnValidate()
        {
            // 限制参数范围
            globalSpeedMultiplier = Mathf.Max(0f, globalSpeedMultiplier);
            transitionSmoothness = Mathf.Max(0.1f, transitionSmoothness);
        }

        // 在Scene视图中绘制调试信息
        private void OnDrawGizmosSelected()
        {
            if (layers == null || layers.Length == 0) return;

            Gizmos.color = Color.cyan;
            Vector3 position = transform.position;
            
            // 绘制背景层标识
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] != null)
                {
                    Vector3 layerPos = layers[i].transform.position;
                    Gizmos.DrawLine(position, layerPos);
                    Gizmos.DrawWireSphere(layerPos, 0.5f);
                }
            }
        }
        #endregion
    }
}
