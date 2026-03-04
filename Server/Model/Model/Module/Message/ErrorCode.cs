namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_Success = 0;

        // 1-11004 是SocketError请看SocketError定义
        //-----------------------------------
        // 100000-109999是Core层的错误
        
        // 110000以下的错误请看ErrorCore.cs
        
        // 这里配置逻辑层的错误码
        // 110000 - 200000是抛异常的错误
        // 200001以上不抛异常
        public const int ERR_RequestRepeatedly = 200001;
        public const int ERR_LoginInfoEmpty = 200002;
        public const int ERR_LoginPassword = 200003;
        public const int ERR_LoginInfoIsNull = 200004;
        public const int ERR_AccountNameFormError = 200005;
        public const int ERR_PasswordFormError = 200006;
        public const int ERR_AccountInBlackListError = 200007;
        public const int ERR_TokenError = 200008;
        public const int ERR_RoleNameIsNull = 200009;
        public const int ERR_RoleNameSame = 200010;
        public const int ERR_RoleNotExist = 200011;
        public const int ERR_ConnectGateKeyError = 200012;
        public const int ERR_LoginGameGateError = 200013;
        public const int ERR_OtherAccountLogin = 200014;
        public const int ERR_SessionPlayerError = 200015;
        public const int ERR_NonePlayerError = 200016;
        public const int ERR_PlayerSessionError = 200017;
        public const int ERR_EnterGameError = 200018;
        public const int ERR_ReEnterGameError = 200019;
        public const int ERR_ReEnterGameError2 = 200020;
        public const int ERR_BuyError = 200021;
        public const int ERR_MoneyEnough = 200021;
        public const int ERR_ChatMessageEmpty = 200022;
        public const int ERR_NetWorkError = 200023;
        public const int ERR_MakeConfigNotExist = 200043;
        public const int ERR_NoMakeFreeQueue  = 200044;
        public const int ERR_MakeConsumeError = 200045;
        public const int ERR_NoMakeQueueOver  = 200046;
        public const int ERR_NoTaskExist  = 200047;
        public const int ERR_BeforeTaskNoOver  = 200048;
        public const int ERR_NoTaskInfoExist  = 200049;
        public const int ERR_TaskNoCompleted  = 200050;
        public const int ERR_TaskRewarded  = 200051;
        
        // Misc error codes (200150-200199)
        public const int ERR_UnitNotFound = 200150;               // Unit not found
        public const int ERR_InvalidTarget = 200151;              // Invalid target
        public const int ERR_PlayerNotExist = 200152;             // Player does not exist
        
        // Combat error codes (200200-200299)
        public const int ERR_UnitIsDead = 200201;                 // Unit already dead
        public const int ERR_TargetIsDead = 200202;               // Target already dead
        public const int ERR_AttackCooldown = 200203;             // Attack still cooling down
        public const int ERR_OutOfRange = 200204;                 // Target out of range
        public const int ERR_AlreadyInBattle = 200205;            // Already in combat
        public const int ERR_NotInBattle = 200206;                // Not currently in combat
        public const int ERR_CombatNotStarted = 200207;           // Combat has not started
        public const int ERR_InvalidCombatTarget = 200208;        // Invalid combat target
        public const int ERR_CombatEnded = 200209;                // Combat has ended
        public const int ERR_CannotAttackSelf = 200210;           // Cannot attack self
        public const int ERR_InsufficientMana = 200211;           // Insufficient mana
        public const int ERR_SkillCooldown = 200212;              // Skill still cooling down
        public const int ERR_SkillNotFound = 200213;              // Skill definition missing
        public const int ERR_InvalidSkillTarget = 200214;         // Invalid skill target
        public const int ERR_BattleNotFound = 200215;             // Battle not found
        public const int ERR_BattleEnded = 200216;                // Battle has ended
        public const int ERR_BattleNotActive = 200217;            // Battle not active
        public const int ERR_TeamMemberInBattle = 200218;         // Team member already in battle
        public const int ERR_RoomNotFound = 200219;               // Room not found
        public const int ERR_RoomFull = 200220;                   // Room is full
        public const int ERR_RoomNotActive = 200221;              // Room not active
        public const int ERR_InvalidRoomState = 200222;           // Invalid room state
        public const int ERR_RoomPlayerLimit = 200223;            // Room player limit reached
        public const int ERR_CreateRoomFailed = 200224;           // Failed to create room
        public const int ERR_RoomExpired = 200225;                // Room has expired

        // Team error codes (200300-200399)
        public const int ERR_AlreadyInTeam = 200301;              // Already in a team
        public const int ERR_NotInTeam = 200302;                  // Not in any team
        public const int ERR_NotTeamLeader = 200303;              // Not the team leader
        public const int ERR_TeamFull = 200304;                   // Team is full
        public const int ERR_TeamNotFound = 200305;               // Team does not exist
        public const int ERR_TeamMemberNotEnough = 200306;        // Not enough team members
        public const int ERR_CannotLeaveTeam = 200307;            // Cannot leave team in current state
        public const int ERR_TeamInBattle = 200308;               // Team is in battle
        public const int ERR_TeamInDungeon = 200309;              // Team is in dungeon
        public const int ERR_InvalidTeamState = 200310;           // Invalid team state

        // Dungeon error codes (200400-200499)
        public const int ERR_DungeonNotFound = 200401;            // Dungeon config not found
        public const int ERR_DungeonAlreadyStarted = 200402;      // Dungeon already started
        public const int ERR_DungeonNotStarted = 200403;          // Dungeon has not started
        public const int ERR_DungeonEnded = 200404;               // Dungeon has ended
        public const int ERR_CannotEnterDungeon = 200405;         // Cannot enter dungeon
        public const int ERR_DungeonInstanceNotFound = 200406;    // Dungeon instance not found
        public const int ERR_NotInDungeon = 200407;               // Not in any dungeon
        public const int ERR_DungeonLevelNotEnough = 200408;      // Level not enough for dungeon
        public const int ERR_DungeonConfigInvalid = 200409;       // Invalid dungeon configuration
        
        // Control Mode error codes (200500-200599)
        public const int ERR_OperationTooFrequent = 200501;       // Operation too frequent
        public const int ERR_OperationDenied = 200502;            // Operation denied (e.g., auto mode)
        //无效战斗
        public const int ERR_InvalidBattleType = 200503;
    }
}