namespace ET.Server
{
    [ComponentOf(typeof(BuffData))]
    public class RogueScaleModifierBuffStateComponent : Entity, IAwake
    {
        public long AppliedMaxHpDelta;
    }

    [ComponentOf(typeof(BuffData))]
    public class RoguePeriodicHpScaleBuffStateComponent : Entity, IAwake
    {
        public long AppliedMaxHpDelta;
    }

    [ComponentOf(typeof(BuffData))]
    public class RogueOnKillStackBuffStateComponent : Entity, IAwake
    {
        public int CurrentStacks;
    }

    [ComponentOf(typeof(BuffData))]
    public class RogueInteractRangeBonusBuffStateComponent : Entity, IAwake
    {
        public float AppliedBonusDistance;
    }

    [ComponentOf(typeof(BuffData))]
    public class RogueLowHpShieldBuffStateComponent : Entity, IAwake
    {
        public long RemainingShieldValue;
        public bool Triggered;
    }

    [ComponentOf(typeof(BuffData))]
    public class RogueBagSpaceHpBonusBuffStateComponent : Entity, IAwake
    {
        public long AppliedMaxHpDelta;
    }

    [ComponentOf(typeof(BuffData))]
    public class RogueNearMonsterSpeedBuffStateComponent : Entity, IAwake
    {
        public int AppliedSpeedPct;
    }

    [ComponentOf(typeof(BuffData))]
    public class RogueSpeedFinalPctBuffStateComponent : Entity, IAwake
    {
        public int AppliedSpeedPct;
    }
}
