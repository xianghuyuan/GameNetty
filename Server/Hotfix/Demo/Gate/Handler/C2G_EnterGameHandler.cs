using System;

namespace ET.Server
{
    [FriendOf(typeof(RoleInfo))]
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_EnterGameHandler:MessageSessionHandler<C2G_EnterGame,G2C_EnterGame>
    {
        protected override async ETTask Run(Session session, C2G_EnterGame request, G2C_EnterGame response)
        {
            if (session.GetComponent<SessionLockingComponent>()!=null)
            {
                response.Error = ErrorCode.ERR_RequestRepeatedly;
                return;
            }

            SessionPlayerComponent sessionPlayerComponent = session.GetComponent<SessionPlayerComponent>();
            if (null == sessionPlayerComponent)
            {
                response.Error = ErrorCode.ERR_SessionPlayerError;
                return;
            }

            Player player = sessionPlayerComponent.Player;
            
            if (player == null ||player.IsDisposed)
            {
                response.Error = ErrorCode.ERR_NonePlayerError;
                return;
            }
            
            CoroutineLockComponent coroutineLockComponent = session.Root().GetComponent<CoroutineLockComponent>();
            long instanceID = session.InstanceId;
            using (session.AddComponent<SessionLockingComponent>())
            {
                using (await coroutineLockComponent.Wait(CoroutineLockType.LoginGate,player.Account.GetLongHashCode()))
                {
                    if (instanceID != session.InstanceId || player.IsDisposed)
                    {
                        response.Error = ErrorCode.ERR_PlayerSessionError;
                        return;
                    }

                    if (player.PlayerState == PlayerState.Game)//从gate网关到服务器了
                    {
                        try
                        {
                            G2M_SecondLogin g2MSecondLogin = G2M_SecondLogin.Create();
                            M2G_SecondLogin reqEnter = (M2G_SecondLogin)await session.Root().GetComponent<MessageLocationSenderComponent>()
                                    .Get(LocationType.Unit).Call(player.UnitId, g2MSecondLogin);
                            
                            if (reqEnter.Error == ErrorCode.ERR_Success)
                            {
                                response.MyUnitId = player.UnitId;
                                return;
                            }
                            response.Error = ErrorCode.ERR_ReEnterGameError;
                            await DisconnectHelper.KickPlayerNoLock(player);
                            session.Disconnect().Coroutine();
                        }
                        catch (Exception e)
                        {
                           Log.Error($"二次登陆失败:{e}");
                           response.Error = ErrorCode.ERR_ReEnterGameError2;
                           await DisconnectHelper.KickPlayerNoLock(player);
                           session.Disconnect().Coroutine();
                           throw;
                        }
                        return;
                    }

                    try
                    {
                        (bool isNewPlayer, Unit unit) = await UnitLoadHelper.LoadUnit(player);
                        //登录邮箱服
                        await LoginMailServer(player, unit);
                        await LoginChatServer(player, unit);
                        long unitId = unit.Id;
                        
                        StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(session.Zone(), "Game");
                        //将这个数组发送一份到Map
                        TransferHelper.TransferAtFrameFinish(unit,startSceneConfig.ActorId,startSceneConfig.Name).Coroutine();
                        player.UnitId = unitId;
                        response.MyUnitId = unitId;
                        player.PlayerState = PlayerState.Game;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"角色进入游戏逻辑服失败 账号Id:{player.Account} 角色Id{player.Id} 异常信息：{e}");
                        response.Error = ErrorCode.ERR_EnterGameError;
                        await DisconnectHelper.KickPlayerNoLock(player);
                            session.Disconnect().Coroutine();
                    }
                }
            }
        }
        /// <summary>
        /// 登陆聊天服
        /// </summary>
        /// <param name="player"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static async ETTask<int> LoginMailServer(Player player, Unit unit)
        {
            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(player.Zone(), "Mail");
            G2Mail_LoginMailServer request = G2Mail_LoginMailServer.Create();
            request.UnitId = unit.Id;
            
            Mail2G_LoginMailServer response = (Mail2G_LoginMailServer)await player.Root().GetComponent<MessageSender>().Call(startSceneConfig.ActorId, request);
            return response.Error;
        }

        public static async ETTask<int> LoginChatServer(Player player,Unit unit)
        {
            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(player.Zone(), "Chat");
            G2Chat_EnterChat request = G2Chat_EnterChat.Create();
            request.UnitId = unit.Id;
            Chat2G_EnterChat response = (Chat2G_EnterChat)await player.Root().GetComponent<MessageSender>().Call(startSceneConfig.ActorId, request);
            return response.Error;
        }
    }
}

