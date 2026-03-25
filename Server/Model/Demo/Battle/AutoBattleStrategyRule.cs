namespace ET.Server
{
    public enum AutoBattleSkillSelectRule
    {
        ShortestMoveThenPriority = 1,
        HighestPriorityThenMove = 2,
    }

    public enum AutoBattleTargetSelectRule
    {
        NearestEnemy = 1,
        KeepCurrentThenNearest = 2,
        LowestHp = 3,
    }
}
