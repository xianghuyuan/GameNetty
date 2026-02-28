namespace ET
{
    /// <summary>
    /// 配置管理组件，管理所有Luban配置表
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ConfigComponent : Entity, IAwake, IDestroy
    {
        public Tables Tables { get; set; }
    }
    
    [EntitySystemOf(typeof(ConfigComponent))]
    [FriendOf(typeof(ConfigComponent))]
    public static partial class ConfigComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ConfigComponent self)
        {
        }
        
        [EntitySystem]
        private static void Destroy(this ConfigComponent self)
        {
            // 清理配置引用
            ConfigHelper.Clear();
        }
        
        /// <summary>
        /// 加载所有配置表
        /// </summary>
        public static void Load(this ConfigComponent self)
        {
            self.Tables = new Tables(LoadByteBuf);
            
            // 设置到 ConfigHelper，方便快捷访问
            ConfigHelper.Tables = self.Tables;
        }
        
        /// <summary>
        /// 加载配置文件的字节数据
        /// </summary>
        private static Luban.ByteBuf LoadByteBuf(string fileName)
        {
            return ConfigLoader.LoadByteBuf(fileName);
        }
    }
}
