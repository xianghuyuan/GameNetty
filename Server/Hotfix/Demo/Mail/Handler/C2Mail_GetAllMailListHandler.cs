namespace ET.Server
{
    [MessageHandler(SceneType.Mail)]
    [FriendOfAttribute(typeof(MailComponent))]
    [FriendOfAttribute(typeof(MailInfoEntity))]
    public class C2Mail_GetAllMailListHandler : MessageHandler<MailUnit, C2Mail_GetAllMailList, Mail2C_GetAllMailList>
    {
        protected override async ETTask Run(MailUnit mailUnit, C2Mail_GetAllMailList request, Mail2C_GetAllMailList response)
        {
            MailComponent mailComponent = mailUnit.GetComponent<MailComponent>();
            foreach (MailInfoEntity mailInfoEntity in mailComponent.MailInfosList)
            {
                MailInfoProto mailInfoProto = MailInfoProto.Create();
                mailInfoProto.MailId = mailInfoEntity.Id;
                mailInfoProto.Title = mailInfoEntity.Title;
                mailInfoProto.Message = mailInfoEntity.Message;
                response.MailInfoList.Add(mailInfoProto);
            }

            await ETTask.CompletedTask;
        }
    }
}