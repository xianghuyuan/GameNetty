using System.Collections.Generic;

namespace ET
{
    public class ChatInfoUnitsComponentDestroy : DestroySystem<ChatInfoUnitsComponent>
    {
        protected override void Destroy(ChatInfoUnitsComponent self)
        {
        }
    }

    [FriendOfAttribute(typeof(ET.ChatInfoUnitsComponent))]
    public static class ChatInfoUnitsComponentSystem
    {
        public static void Add(this ChatInfoUnitsComponent self, ChatInfoUnit chatInfoUnit)
        {
            if (self.ChatInfoUnitsDict.ContainsKey(chatInfoUnit.Id))
            {
                Log.Error($"chatInfoUnit is exist! ： {chatInfoUnit.Id}");
                return;
            }
            self.ChatInfoUnitsDict.Add(chatInfoUnit.Id, chatInfoUnit);
        }


        public static ChatInfoUnit Get(this ChatInfoUnitsComponent self, long id)
        {
            self.ChatInfoUnitsDict.TryGetValue(id, out EntityRef<ChatInfoUnit> chatInfoUnit);
            return chatInfoUnit;
        }


        public static void Remove(this ChatInfoUnitsComponent self, long id)
        {
            if (self.ChatInfoUnitsDict.TryGetValue(id, out EntityRef<ChatInfoUnit> chatInfoUnit))
            {
                self.ChatInfoUnitsDict.Remove(id);
            }
        }
    }
}