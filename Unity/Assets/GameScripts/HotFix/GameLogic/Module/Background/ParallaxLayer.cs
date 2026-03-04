using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 视差滚动单层背景控制
    /// 使用Material UV偏移实现高性能无限滚动
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("渲染器设置")]
        [Tooltip("背景渲染器（SpriteRenderer或MeshRenderer）")]
        [SerializeField] private Renderer targetRenderer;
        
        [Header("滚动参数")]
        [Tooltip("滚动速度倍率（相对于基础速度）")]
        [SerializeField] private float scrollSpeedMultiplier = 1f;
        
        [Tooltip("滚动方向（默认向左：-1,0）")]
        [SerializeField] private Vector2 scrollDirection = new Vector2(-1f, 0f);
        
        [Header("高级设置")]
        [Tooltip("是否在Start时自动获取Renderer")]
        [SerializeField] private bool autoGetRenderer = true;
        
        // 运行时状态
        private Material materialInstance;
        private Vector2 currentOffset;
        private bool isPaused;
        private bool isInitialized;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// 初始化背景层
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            // 自动获取Renderer
            if (autoGetRenderer && targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer == null)
            {
                Debug.LogError($"[ParallaxLayer] {gameObject.name} 未找到Renderer组件！", this);
                return;
            }

            // 创建材质实例，避免共享材质污染
            materialInstance = new Material(targetRenderer.sharedMaterial);
            targetRenderer.material = materialInstance;

            // 归一化滚动方向
            scrollDirection.Normalize();
            
            currentOffset = materialInstance.mainTextureOffset;
            isInitialized = true;
        }

        /// <summary>
        /// 更新滚动（由ParallaxBackground调用）
        /// </summary>
        /// <param name="baseSpeed">基础滚动速度</param>
        public void UpdateScroll(float baseSpeed)
        {
            if (!isInitialized || isPaused || materialInstance == null) return;

            // 计算实际滚动速度
            float actualSpeed = baseSpeed * scrollSpeedMultiplier;
            
            // 更新UV偏移
            currentOffset += scrollDirection * actualSpeed * Time.deltaTime;
            
            // 应用到材质（UV会自动循环，因为Wrap Mode设置为Repeat）
            materialInstance.mainTextureOffset = currentOffset;
        }

        /// <summary>
        /// 暂停滚动
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// 恢复滚动
        /// </summary>
        public void Resume()
        {
            isPaused = false;
        }

        /// <summary>
        /// 重置UV偏移
        /// </summary>
        public void ResetOffset()
        {
            currentOffset = Vector2.zero;
            if (materialInstance != null)
            {
                materialInstance.mainTextureOffset = currentOffset;
            }
        }

        /// <summary>
        /// 设置滚动速度倍率
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            scrollSpeedMultiplier = multiplier;
        }

        /// <summary>
        /// 设置滚动方向
        /// </summary>
        public void SetScrollDirection(Vector2 direction)
        {
            scrollDirection = direction.normalized;
        }

        private void OnDestroy()
        {
            // 清理材质实例
            if (materialInstance != null)
            {
                Destroy(materialInstance);
            }
        }

        private void OnValidate()
        {
            // 编辑器中自动归一化方向
            if (scrollDirection != Vector2.zero)
            {
                scrollDirection.Normalize();
            }
        }

        #region 属性访问器
        public bool IsPaused => isPaused;
        public float SpeedMultiplier => scrollSpeedMultiplier;
        public Vector2 ScrollDirection => scrollDirection;
        public Vector2 CurrentOffset => currentOffset;
        #endregion
    }
}
