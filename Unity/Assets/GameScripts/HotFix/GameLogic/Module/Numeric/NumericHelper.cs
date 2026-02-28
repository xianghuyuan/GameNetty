namespace ET
{
    public static class NumericHelper
    {
        public static async ETTask Test(Scene rootScene)
        {
            C2M_TestNumericValue request = C2M_TestNumericValue.Create();
            M2C_TestNumericValue response = (M2C_TestNumericValue)await rootScene.GetComponent<ClientSenderComponent>().Call(request);
            if (response.Error!=ErrorCode.ERR_Success)
            {
                
            }
            await ETTask.CompletedTask;
        }
        public static string GetNumericName(int numericType)
        {
            return numericType switch
            {
                NumericType.Speed => "移动速度",
                NumericType.Hp => "当前生命",
                NumericType.MaxHp => "最大生命",
                NumericType.Attack => "攻击力",
                NumericType.Defense => "防御力",
                NumericType.Level => "等级",
                NumericType.AOI => "范围",
                _ => $"属性{numericType}"
            };
        }
    }
}