using System;

namespace ET.Server
{
    [MessageHandler(SceneType.Chat)]
    [FriendOfAttribute(typeof(ET.ChatInfoUnitsComponent))]
    [FriendOfAttribute(typeof(ET.ChatInfoUnit))]
    public class C2Chat_SendChatInfoHandler : MessageHandler<ChatInfoUnit, C2Chat_SendChatInfo, Chat2C_SendChatInfo>
    {
        protected override async ETTask Run(ChatInfoUnit chatInfoUnit, C2Chat_SendChatInfo request, Chat2C_SendChatInfo response)
        {
            if (string.IsNullOrEmpty(request.ChatMessage))
            {
                response.Error = ErrorCode.ERR_ChatMessageEmpty;
                return;
            }
            
            ChatInfoUnitsComponent chatInfoUnitsComponent = chatInfoUnit.Scene().GetComponent<ChatInfoUnitsComponent>();
            foreach (EntityRef<ChatInfoUnit> otherUnit in chatInfoUnitsComponent.ChatInfoUnitsDict.Values)
            {
                ChatInfoUnit targetUnit = otherUnit;
                if (targetUnit == null) continue;
                Chat2C_NoticeChatInfo chat2CNoticeChatInfo = Chat2C_NoticeChatInfo.Create();
                chat2CNoticeChatInfo.Name = chatInfoUnit.Name;
                chat2CNoticeChatInfo.ChatMessage = request.ChatMessage;
                // 使用 Location 服务发送，通过 UnitId 自动路由
                chatInfoUnit.Root().GetComponent<MessageLocationSenderComponent>()
                        .Get(LocationType.GateSession)
                        .Send(targetUnit.Id, chat2CNoticeChatInfo);
            }
            await ETTask.CompletedTask;
        }
    }
}