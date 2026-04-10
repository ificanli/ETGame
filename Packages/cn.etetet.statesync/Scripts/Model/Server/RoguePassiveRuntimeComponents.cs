using System.Collections.Generic;
using Unity.Mathematics;

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

    public struct RogueLifeStealSourceData
    {
        public int LifeStealPermille;
        public int TargetFilter;
    }

    public struct RogueContainerLowQualityGoldSourceData
    {
        public int Gold;
        public int HighQualityThreshold;
    }

    public struct RogueHitHeroCritSourceData
    {
        public int CritPermillePerHit;
        public int AccumulatedCritPermille;
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

    public struct RogueReloadFirstShotsSourceData
    {
        public int DamageBonusPermille;
        public int ShotCount;
        public int RemainingShots;
        public int PenetrationCount;
    }

    public struct RogueTemporaryItemSourceData
    {
        public int ItemConfigId;
        public int RemainingCount;
    }

    public struct RogueSummonedSpiritSourceData
    {
        public long SpiritUnitId;
        public long DisposeTimerId;
        public int HealPermille;
        public int LifetimeMs;
        public int DamageBonusPermille;
        public float AuraRadius;
        public float PickupRadius;
        public bool AuraActive;
    }

    public struct RogueTrapMasterSourceData
    {
        public int IdleMs;
        public int BulletCount;
        public int TrapDamagePermille;
        public float TrapRadius;
        public int TrapLifetimeMs;
        public int TrapTickIntervalMs;
    }

    public struct RogueOnKillWeaponEnchantSourceData
    {
        public int SlotIndex;
        public int DamageBonusPermille;
        public int PenetrationCount;
        public int TargetFilter;
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
        public int EffectiveRequiredShotCount;
        public int EffectiveTrapDamagePermille;
        public float EffectiveTrapRadius;
        public int EffectiveTrapLifetimeMs;
        public int EffectiveTrapTickIntervalMs;
        public int AccumulatedShots;
        public long LastMoveTime;
        public long MoveVersion;
        public long LastTriggeredMoveVersion;
    }

    [ChildOf(typeof(Scene))]
    public class RogueTrapEntity : Entity, IAwake, IDestroy
    {
        public long OwnerId;
        public float3 Position;
        public long Damage;
        public int WeaponId;
        public float Radius;
        public int TickIntervalMs;
        public long ExpireTime;
        public long TimerId;
    }

    [ComponentOf(typeof(Unit))]
    public class RogueHitHeroCritStateComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, RogueHitHeroCritSourceData> Sources { get; set; } = new();
    }

    [ComponentOf(typeof(Unit))]
    public class RogueReloadFirstShotsStateComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, RogueReloadFirstShotsSourceData> Sources { get; set; } = new();
    }

    [ComponentOf(typeof(Unit))]
    public class RogueOnKillWeaponEnchantStateComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, RogueOnKillWeaponEnchantSourceData> Sources { get; set; } = new();
    }

    [ComponentOf(typeof(Unit))]
    public class RogueTemporaryItemStateComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, RogueTemporaryItemSourceData> Sources { get; set; } = new();
    }

    [ComponentOf(typeof(Unit))]
    public class RogueSummonedSpiritStateComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, RogueSummonedSpiritSourceData> Sources { get; set; } = new();
    }

    [ComponentOf(typeof(Unit))]
    public class RogueBuffPassiveRuntimeComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, int> KillGoldBonusBySource { get; set; } = new();
        public Dictionary<long, RogueContainerLowQualityGoldSourceData> ContainerLowQualityGoldBySource { get; set; } = new();
        public Dictionary<long, int> GoldDamagePerOnePercentBySource { get; set; } = new();
        public Dictionary<long, int> MonsterDamageBonusPermilleBySource { get; set; } = new();
        public Dictionary<long, int> AreaDiscoveryGoldBySource { get; set; } = new();
        public Dictionary<long, int> ProbabilityMultiplierBySource { get; set; } = new();
        public Dictionary<long, int> ContainerResultProbabilityMultiplierBySource { get; set; } = new();
        public Dictionary<long, int> SearchMonsterProbabilityMultiplierBySource { get; set; } = new();
        public Dictionary<long, RogueLifeStealSourceData> LifeStealPermilleBySource { get; set; } = new();
        public Dictionary<long, int> SizeDifferenceDamageBonusBySource { get; set; } = new();
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
