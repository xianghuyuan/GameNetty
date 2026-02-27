namespace ET.Server
{
    [MessageHandler(SceneType.Chat)]
    [FriendOfAttribute(typeof(ET.ChatInfoUnit))]
    public class G2Chat_EnterChatHandler:MessageHandler<Scene,G2Chat_EnterChat, Chat2G_EnterChat>
    {
        protected override async ETTask Run(Scene scene, G2Chat_EnterChat request, Chat2G_EnterChat response)
        {
            ChatInfoUnitsComponent chatInfoUnitsComponent = scene.GetComponent<ChatInfoUnitsComponent>();

            ChatInfoUnit chatInfoUnit = chatInfoUnitsComponent.Get(request.UnitId);

            if ( chatInfoUnit != null)
            {
                return;
            }
            
            chatInfoUnit      = chatInfoUnitsComponent.AddChildWithId<ChatInfoUnit>(request.UnitId);
            chatInfoUnit.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.UnOrderedMessage);
            chatInfoUnitsComponent.Add(chatInfoUnit);
            await chatInfoUnit.AddLocation(LocationType.Chat);
            Log.Info("已加入聊天场景");
            await ETTask.CompletedTask;
        }
    }
}