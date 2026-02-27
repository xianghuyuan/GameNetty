namespace ET.Server
{
	/// <summary>
	/// 这个就很关键了，这个挂载在Session上，用来记录这个Session对应的Player是谁，精确到玩家登陆到的是哪个账户了
	/// 只需要在Gate网关上挂载,因为所有消息都是由客户端发送到网关，再根据需要从网关上进行转发的
	/// </summary>
	[ComponentOf(typeof(Session))]
	public class SessionPlayerComponent : Entity, IAwake, IDestroy
	{
		private EntityRef<Player> player;

		public Player Player
		{
			get
			{
				return this.player;
			}
			set
			{
				this.player = value;
			}
		}
	}
}