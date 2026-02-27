namespace ET.Server
{
    [ChildOf(typeof(MailComponent))]
    public class MailInfoEntity : Entity,IDestroy,IAwake,ISerializeToEntity
    {
        public string Title;
        public string Message;
    }
}