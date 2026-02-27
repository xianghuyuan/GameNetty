namespace ET.Server
{
    [Invoke((long)SceneType.UnitCache)]
    public class AddToBytesInvoke_Cache:AInvokeHandler<AddToBytes>
    {
        public override void Handle(AddToBytes args)
        {
            Unit unit = args.Unit;
            unit?.GetComponent<UnitDBSaveComponent>().AddToBytes(args.Type,args.Bytes);
        }
    }
}