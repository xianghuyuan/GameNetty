namespace ET.Server
{
    [EntitySystemOf(typeof(AccountSessionComponent))]
    [FriendOf(typeof(ET.Server.AccountSessionComponent))]
    public static partial class AccountSessionsComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ET.Server.AccountSessionComponent self)
        {
            
        }
        
        [EntitySystem]
        private static void Destroy(this ET.Server.AccountSessionComponent self)
        {
            self.AccountSessionDictionary.Clear();
        }

        public static Session Get(this AccountSessionComponent self, string accountName)
        {
            if (!self.AccountSessionDictionary.TryGetValue(accountName,out EntityRef<Session> session))
            {
                return null;
            }

            return session;
        }

        public static void Add(this AccountSessionComponent self, string accountName,EntityRef<Session> session)
        {
            if (self.AccountSessionDictionary.ContainsKey(accountName))
            {
                self.AccountSessionDictionary[accountName] = session;
                return;
            }
            self.AccountSessionDictionary.Add(accountName,session);
        }

        public static void Remove(this AccountSessionComponent self, string accountName)
        {
            if (self.AccountSessionDictionary.ContainsKey(accountName))
            {
                self.AccountSessionDictionary.Remove(accountName);
            }
        }
    } 
}

