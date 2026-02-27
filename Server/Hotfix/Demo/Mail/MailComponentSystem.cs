namespace ET.Server
{
    [EntitySystemOf(typeof(MailComponent))]
    [FriendOfAttribute(typeof(MailInfoEntity))]
    [FriendOfAttribute(typeof(MailComponent))]
    public static partial class MailComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MailComponent self)
        {
            MailInfoEntity mailInfoEntity1 = self.AddChild<MailInfoEntity>();
            mailInfoEntity1.Title = "第一封邮件";
            mailInfoEntity1.Message = "这是第一封邮件";
            self.MailInfosList.Add(mailInfoEntity1);
            
            MailInfoEntity mailInfoEntity2 = self.AddChild<MailInfoEntity>();
            mailInfoEntity2.Title = "第二封邮件";
            mailInfoEntity2.Message = "这是第二封邮件";
            self.MailInfosList.Add(mailInfoEntity2);
        }

        [EntitySystem]
        private static void Destroy(this MailComponent slef)
        {


        }

        [EntitySystem]
        private static void Deserialize(this MailComponent self)
        {
            foreach (Entity entity in self.Children.Values)
            {
                if (entity is MailInfoEntity mailInfoEntity)
                {
                    self.MailInfosList.Add(mailInfoEntity);
                }
            }
        }
    }
}