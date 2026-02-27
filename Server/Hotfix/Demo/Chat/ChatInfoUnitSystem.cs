namespace ET
{
    [FriendOf(typeof(ChatInfoUnit))]
    public class ChatInfoUnitDestroySystem : DestroySystem<ChatInfoUnit>
    {
        protected override void Destroy(ChatInfoUnit self)
        {
            self.Name = null;
            self.GateSessionActorId = 0;
        }
    }
}