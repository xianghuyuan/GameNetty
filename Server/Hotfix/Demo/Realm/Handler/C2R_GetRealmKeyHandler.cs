using System;

namespace ET.Server
{
    [MessageSessionHandler(SceneType.Realm)]
    public class C2R_GetRealmKeyHandler : MessageSessionHandler<C2R_GetRealmKey,R2C_GetRealmKey>
    {
        protected override async ETTask Run(Session session, C2R_GetRealmKey request, R2C_GetRealmKey response)
        {
            if (session.GetComponent<SessionLockingComponent>() != null)
            {
                response.Error = ErrorCode.ERR_RequestRepeatedly;
                session.Disconnect().Coroutine();
                return;
            }

            string token = session.Root().GetComponent<TokenComponent>().Get(request.Account);

            if (token == null || token!=request.Token)
            {
                response.Error = ErrorCode.ERR_TokenError;
                session.Disconnect().Coroutine();
                return;
            }
            
            CoroutineLockComponent coroutineLockComponent = session.Root().GetComponent<CoroutineLockComponent>();

            using (await coroutineLockComponent.Wait(CoroutineLockType.LoginAccount,request.Account.GetLongHashCode()))
            {
                StartSceneConfig config = RealmGateAddressHelper.GetGate(request.ServerId, request.Account);
                Log.Debug($"gate address:{config}");
                
                R2G_GetLoginKey r2GGetLoginKey = R2G_GetLoginKey.Create();
                r2GGetLoginKey.Account = request.Account;
                G2R_GetLoginKey g2RGetLoginKey =
                        (G2R_GetLoginKey)await session.Fiber().Root.GetComponent<MessageSender>().Call(config.ActorId, r2GGetLoginKey);

                response.Address = config.InnerIPPort.ToString();
                response.Key = g2RGetLoginKey.Key;
                response.GateId = g2RGetLoginKey.GateId;
                //response.GateId = config.Id;
                //Realm断开链接
                session.Disconnect().Coroutine();
            }
        }
    }
}

