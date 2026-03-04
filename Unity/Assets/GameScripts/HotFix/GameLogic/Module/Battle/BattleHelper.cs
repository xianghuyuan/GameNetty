namespace ET
{
    /// <summary>
    /// 战斗辅助类
    /// 封装战斗相关的网络请求和操作
    /// </summary>
    public static class BattleHelper
    {
        /// <summary>
        /// 开始单人战斗
        /// </summary>
        /// <param name="scene">场景</param>
        /// <param name="battleType">战斗类型: 0=WaveBattle, 1=Dungeon, 2=Boss</param>
        /// <param name="totalWaves">总波数（波次战斗用）</param>
        /// <returns>战斗ID（BattleRoomId），失败返回0</returns>
        public static async ETTask<long> StartBattle(Scene scene, int battleType = 0, int totalWaves = 1)
        {
            // 创建请求
            C2M_StartBattle request = C2M_StartBattle.Create();
            request.battleType = battleType;
            request.totalWaves = totalWaves;
            
            // 发送请求
            M2C_StartBattle response = await scene.GetComponent<ClientSenderComponent>().Call(request) as M2C_StartBattle;
            
            // 检查结果
            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"开始战斗失败: {response.Message}");
                return 0;
            }
            
            Log.Info($"开始战斗成功, BattleId: {response.battleId}");
            return response.battleId;
        }
        
        /// <summary>
        /// 队长开启组队战斗
        /// </summary>
        /// <param name="scene">场景</param>
        /// <param name="teamId">队伍ID</param>
        /// <param name="battleType">战斗类型</param>
        /// <param name="totalWaves">总波数</param>
        /// <returns>战斗ID，失败返回0</returns>
        public static async ETTask<long> StartTeamBattle(Scene scene, long teamId, int battleType = 0, int totalWaves = 5)
        {
            // 创建请求
            C2M_TeamStartBattle request = C2M_TeamStartBattle.Create();
            request.teamId = teamId;
            request.battleType = battleType;
            request.totalWaves = totalWaves;
            
            // 发送请求
            M2C_TeamStartBattle response = await scene.GetComponent<ClientSenderComponent>().Call(request) as M2C_TeamStartBattle;
            
            // 检查结果
            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"开启组队战斗失败: {response.Message}");
                return 0;
            }
            
            Log.Info($"开启组队战斗成功, BattleId: {response.battleId}, 成员数: {response.memberIds.Count}");
            return response.battleId;
        }
        
        /// <summary>
        /// 加入进行中的组队战斗
        /// </summary>
        /// <param name="scene">场景</param>
        /// <param name="battleId">战斗ID</param>
        /// <returns>是否成功</returns>
        public static async ETTask<bool> JoinTeamBattle(Scene scene, long battleId)
        {
            // 创建请求
            C2M_JoinTeamBattle request = C2M_JoinTeamBattle.Create();
            request.battleId = battleId;
            
            // 发送请求
            M2C_JoinTeamBattle response = await scene.GetComponent<ClientSenderComponent>().Call(request) as M2C_JoinTeamBattle;
            
            // 检查结果
            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"加入组队战斗失败: {response.Message}");
                return false;
            }
            
            Log.Info($"加入组队战斗成功, BattleId: {battleId}, 当前成员数: {response.memberIds.Count}");
            return true;
        }
        
        /// <summary>
        /// 退出战斗
        /// </summary>
        /// <param name="scene">场景</param>
        /// <param name="battleId">战斗ID</param>
        /// <returns>是否成功</returns>
        public static async ETTask<bool> ExitBattle(Scene scene, long battleId)
        {
            // 创建请求
            C2M_ExitBattle request = C2M_ExitBattle.Create();
            request.battleId = battleId;
            
            // 发送请求
            M2C_ExitBattle response = await scene.GetComponent<ClientSenderComponent>().Call(request) as M2C_ExitBattle;
            
            // 检查结果
            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"退出战斗失败: {response.Message}");
                return false;
            }
            
            Log.Info($"退出战斗成功, BattleId: {battleId}");
            return true;
        }
        
        /// <summary>
        /// 释放技能
        /// </summary>
        /// <param name="scene">场景</param>
        /// <param name="skillId">技能ID</param>
        /// <param name="targetId">目标ID（可选）</param>
        /// <returns>是否成功</returns>
        public static async ETTask<bool> CastSkill(Scene scene, int skillId, long targetId = 0)
        {
            // 创建请求
            C2M_CastSkill request = C2M_CastSkill.Create();
            request.skillId = skillId;
            request.targetId = targetId;
            
            // 发送请求
            M2C_CastSkill response = await scene.GetComponent<ClientSenderComponent>().Call(request) as M2C_CastSkill;
            
            // 检查结果
            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"释放技能失败: {response.Message}");
                return false;
            }
            
            Log.Debug($"释放技能成功, SkillId: {skillId}, CooldownEnd: {response.cooldownEnd}");
            return true;
        }
    }
}
