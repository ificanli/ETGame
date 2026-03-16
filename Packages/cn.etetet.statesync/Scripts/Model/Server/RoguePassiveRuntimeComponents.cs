using System.Collections.Generic;

namespace ET.Server
{
    public struct RogueLowHpDamageSourceData
    {
        public int HpThresholdPermille;
        public int DamageBonusPermille;
    }

    public struct RogueDamageReductionSourceData
    {
        public int DamageReduction;
        public int SourceFilter;
    }

    public struct RogueInstantKillSourceData
    {
        public int ChancePermille;
        public int GoldReward;
    }

    public struct RogueOnKillStackSourceData
    {
        public int BonusPermillePerStack;
        public int MaxStacks;
        public int CurrentStacks;
    }

    public struct RogueLowHpShieldSourceData
    {
        public int TriggerHpPermille;
        public int ShieldMaxHpPermille;
        public bool Triggered;
        public long RemainingShieldValue;
    }

    public struct RogueAfterSkillSpeedBoostSourceData
    {
        public int SpeedPct;
        public int DurationMs;
    }

    public struct RogueTrapMasterSourceData
    {
        public int IdleMs;
        public int BulletCount;
    }

    [ComponentOf(typeof(Unit))]
    public class RogueOutOfCombatStealthStateComponent : Entity, IAwake, IDestroy
    {
        public bool Concealed;
        public string ConcealmentSourceId;
    }

    [ComponentOf(typeof(Unit))]
    public class RogueAfterSkillSpeedBoostComponent : Entity, IAwake, IDestroy
    {
        public int AppliedSpeedPct;
        public long RemoveTimerId;
    }

    [ComponentOf(typeof(Unit))]
    public class RogueTrapMasterStateComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, RogueTrapMasterSourceData> Sources { get; set; } = new();
        public int EffectiveIdleMs;
        public int EffectiveBulletCount;
        public long LastMoveTime;
        public long MoveVersion;
        public long LastTriggeredMoveVersion;
    }

    [ComponentOf(typeof(Unit))]
    public class RogueBuffPassiveRuntimeComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, int> KillGoldBonusBySource { get; set; } = new();
        public Dictionary<long, int> GoldDamagePerOnePercentBySource { get; set; } = new();
        public Dictionary<long, int> AreaDiscoveryGoldBySource { get; set; } = new();
        public Dictionary<long, int> ProbabilityMultiplierBySource { get; set; } = new();
        public Dictionary<long, int> LifeStealPermilleBySource { get; set; } = new();
        public Dictionary<long, int> OnKillHealPermilleBySource { get; set; } = new();

        public Dictionary<long, RogueLowHpDamageSourceData> LowHpDamageBySource { get; set; } = new();
        public Dictionary<long, RogueDamageReductionSourceData> DamageReductionBySource { get; set; } = new();
        public Dictionary<long, RogueInstantKillSourceData> InstantKillBySource { get; set; } = new();
        public Dictionary<long, RogueOnKillStackSourceData> OnKillStackBySource { get; set; } = new();
        public Dictionary<long, RogueLowHpShieldSourceData> LowHpShieldBySource { get; set; } = new();
        public Dictionary<long, long> ExtendGameTimeBySourceMs { get; set; } = new();
        public Dictionary<long, RogueAfterSkillSpeedBoostSourceData> AfterSkillSpeedBoostBySource { get; set; } = new();

        public Dictionary<long, byte> FatalImmunitySources { get; set; } = new();
        public Dictionary<long, byte> SkillDisableSources { get; set; } = new();
        public Dictionary<long, byte> OutOfCombatStealthSources { get; set; } = new();
    }
}
