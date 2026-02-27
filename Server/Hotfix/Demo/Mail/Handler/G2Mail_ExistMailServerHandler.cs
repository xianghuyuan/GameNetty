namespace ET.Server
{
    [MessageHandler(SceneType.Mail)]
    public class G2Mail_ExistMailServerHandler:MessageHandler<MailUnit,G2Mail_ExistMailServer,Mail2G_ExistMailServer>
    {
        protected override async ETTask Run(MailUnit unit, G2Mail_ExistMailServer request, Mail2G_ExistMailServer response)
        {
            unit.GetComponent<MailComponent>().BeginInit();
            await unit.Root().GetComponent<DBManagerComponent>().GetZoneDB(unit.Zone()).Save(unit.GetComponent<MailComponent>());
            MailUnitExist(unit).Coroutine();
            await ETTask.CompletedTask;
        }

        private async ETTask MailUnitExist(MailUnit mailUnit)
        {
            await mailUnit.Fiber().WaitFrameFinish();
            await mailUnit.RemoveLocation(LocationType.Mail);
            mailUnit.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.GateSession).Remove(mailUnit.Id);
            mailUnit?.Dispose();
        }
    }
}