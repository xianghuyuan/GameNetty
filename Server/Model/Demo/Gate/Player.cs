namespace ET.Server
{
    public enum PlayerState
    {
        Disconnect,
        Gate,
        Game,
    }
    [ChildOf(typeof(PlayerComponent))]
    public sealed class Player : Entity, IAwake<string>
    {
        public string Account { get; set; }
        public PlayerState PlayerState { get; set; }
        /// <summary>
        /// 从缓存服加载进来的，就是数据库中的唯一id
        /// </summary>
        public long UnitId{ get; set; }
        public long ChatInfoInstanceId { get; set; }
    }
}