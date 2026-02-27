using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ET.Server
{
    /// <summary>
    /// Realm公共服
    /// </summary>
    [MessageSessionHandler(SceneType.Realm)]
    [FriendOfAttribute(typeof(Account))]
    public class C2R_LoginAccountHandler : MessageSessionHandler<C2R_LoginAccount,R2C_LoginAccount>
    {
        protected override async ETTask Run(Session session, C2R_LoginAccount request, R2C_LoginAccount response)
        {
            session.RemoveComponent<SessionAcceptTimeoutComponent>();

            if (session.GetComponent<SessionLockingComponent>() != null)
            {
                response.Error = ErrorCode.ERR_RequestRepeatedly;//重复登陆
                session.Disconnect().Coroutine();
                return;
            }

            if (string.IsNullOrEmpty(request.AccountName) || string.IsNullOrEmpty(request.Password))
            {
                response.Error = ErrorCode.ERR_LoginInfoIsNull;
                session.Disconnect().Coroutine();
                return;
            }

            if (!Regex.IsMatch(request.AccountName.Trim(),"^(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$"))
            {
                response.Error = ErrorCode.ERR_AccountNameFormError;
                session.Disconnect().Coroutine();
                return;
            }
            
            if (!Regex.IsMatch(request.AccountName.Trim(),"^[A-Za-z0-9]+$"))
            {
                response.Error = ErrorCode.ERR_PasswordFormError;
                session.Disconnect().Coroutine();
                return;
            }

            CoroutineLockComponent coroutineLockComponent = session.Root().GetComponent<CoroutineLockComponent>();
            using (session.AddComponent<SessionLockingComponent>())//用作重复登陆判断
            {
                using (await coroutineLockComponent.Wait(CoroutineLockType.LoginAccount, request.AccountName.GetLongHashCode()))
                {
                    DBComponent dbComponent = session.Root().GetComponent<DBManagerComponent>().GetZoneDB(session.Zone());//1000
                    List<Account> accountInfoList = null;
                    try 
                    {
                        // 尝试执行数据库操作
                        accountInfoList = await dbComponent.Query<Account>(d => d.AccountName.Equals(request.AccountName));
                        // 如果成功执行，说明数据库连接正常
                    }
                    catch (Exception e)
                    {
                        // 如果抛出异常，说明数据库连接可能有问题
                        Log.Error($"数据库操作失败: {e}");
                        // 可以在这里处理连接失败的情况
                    }
                    //var accountInfoList = await dbComponent.Query<Account>(d => d.AccountName.Equals(request.AccountName));
                    Account account = null;
                    if (accountInfoList!=null && accountInfoList.Count >0 )
                    {
                        account = accountInfoList[0];
                        session.AddChild(account);
                        if (account.AccountType == (int)AccountType.BlackList)//黑名单
                        {
                            response.Error = ErrorCode.ERR_AccountInBlackListError;
                            session.Disconnect().Coroutine();
                            account?.Dispose();
                            return;
                        }
                        if (!account.Password.Equals(request.Password))//密码错误
                        {
                            response.Error = ErrorCode.ERR_PasswordFormError;
                            session.Disconnect().Coroutine();
                            account?.Dispose();
                            return;
                        }
                    }
                    else
                    {
                        account = session.AddChild<Account>();
                        account.AccountName = request.AccountName.Trim();
                        account.Password = request.Password;
                        account.CreateTime = TimeInfo.Instance.ServerNow();
                        account.AccountType = (int)AccountType.General;
                        await dbComponent.Save<Account>(account);
                    }

                    R2L_LoginAccountRequest r2LLoginAccountRequest = R2L_LoginAccountRequest.Create();
                    r2LLoginAccountRequest.AccountName = request.AccountName;

                    StartSceneConfig loginCenterConfig = StartSceneConfigCategory.Instance.LoginCenterConfig;
                    L2R_LoginAccountRequest loginAccountResponse = await session.Fiber().Root.GetComponent<MessageSender>()
                            .Call(loginCenterConfig.ActorId, r2LLoginAccountRequest) as L2R_LoginAccountRequest;

                    if (loginAccountResponse.Error != ErrorCode.ERR_Success)//登陆中心服登陆失败
                    {
                        response.Error = loginAccountResponse.Error;
                        session?.Disconnect().Coroutine();
                        account?.Dispose();
                        return;
                    }

                    //顶号处理
                    Session otherSession = session.Root().GetComponent<AccountSessionComponent>().Get(request.AccountName);
                    //otherSession?.Send();
                    otherSession?.Disconnect().Coroutine();
                    //
                    string Token = TimeInfo.Instance.ServerNow().ToString() + RandomGenerator.RandomNumber(int.MinValue, int.MaxValue).ToString();
                    response.Token = Token;
                    session.Root().GetComponent<AccountSessionComponent>().Add(request.AccountName,session);
                    session.AddComponent<AccountCheckOutTimeComponent, string>(request.AccountName);

                    
                    session.Root().GetComponent<TokenComponent>().Remove(request.AccountName);
                    session.Root().GetComponent<TokenComponent>().Add(request.AccountName,Token);
                    account?.Dispose();        
                }
            }
        }
    }
}

