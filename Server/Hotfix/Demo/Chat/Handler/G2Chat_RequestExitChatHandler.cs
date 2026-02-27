using System;

namespace ET
{
    [MessageHandler(SceneType.Chat)]
    [FriendOfAttribute(typeof(ET.ChatInfoUnit))]
    public class G2Chat_EnterChatHandler:MessageHandler<Unit, G2Chat_RequestExitChat,Chat2G_RequestExitChat>
    {
        protected override async ETTask Run(Unit unit, G2Chat_RequestExitChat request, Chat2G_RequestExitChat response)
        {

            ChatInfoUnitsComponent chatInfoUnitsComponent = unit.Scene().GetComponent<ChatInfoUnitsComponent>();
            chatInfoUnitsComponent.Remove(unit.Id);
            await ETTask.CompletedTask;
        }
    }
}