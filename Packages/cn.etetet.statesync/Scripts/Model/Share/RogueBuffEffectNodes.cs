using System.Collections.Generic;

namespace ET
{
    public class EffectRogueFatalImmunity : EffectNode
    {
    }

    public class EffectRogueKillGoldBonus : EffectNode
    {
        public int Gold;
    }

    public class EffectRogueContainerLowQualityGold : EffectNode
    {
        public int Gold;
        public int HighQualityThreshold;
    }

    public class EffectRogueGoldDamageBonus : EffectNode
    {
        public int GoldPerOnePercent;
    }

    public class EffectRogueMonsterDamageBonus : EffectNode
    {
        public int DamageBonusPermille;
    }

    public class EffectRogueLowHpDamageBonus : EffectNode
    {
        public int HpThresholdPermille;
        public int DamageBonusPermille;
    }

    public class EffectRogueDamageReduction : EffectNode
    {
        public int DamageReduction;
        public int SourceFilter;
    }

    public class EffectRogueLifeSteal : EffectNode
    {
        public int LifeStealPermille;
    }

    public class EffectRogueInstantKillChance : EffectNode
    {
        public int ChancePermille;
        public int GoldReward;
    }

    public class EffectRogueOnKillHeal : EffectNode
    {
        public int HealLostHpPermille;
    }

    public class EffectRogueOnKillStackAttack : EffectNode
    {
        public int BonusPermillePerStack;
        public int MaxStacks;
    }

    public class EffectRogueAreaDiscoveryGold : EffectNode
    {
        public int Gold;
    }

    public class EffectRogueInteractRangeBonus : EffectNode
    {
        public float Distance;
    }

    public class EffectRogueContainerRadar : EffectNode
    {
        public int Radius;
        public int IntervalMs;
    }

    public class EffectRogueTemporaryKey : EffectNode
    {
        public int ItemConfigId;
        public int Count;
    }

    public class EffectRogueObjectiveRewardBuff : EffectNode
    {
        public int BuffConfigId;
    }

    public class EffectRogueSilentSearch : EffectNode
    {
    }

    public class EffectRogueBagSpaceHpBonus : EffectNode
    {
        public int MaxHpPermilleAtFull;
    }

    public class EffectRogueNearMonsterSpeedBonus : EffectNode
    {
        public int Radius;
        public int SpeedPct;
    }

    public class EffectRogueTrapMaster : EffectNode
    {
        public int IdleMs;
        public int BulletCount;
    }

    public class EffectRogueLowHpShield : EffectNode
    {
        public int TriggerHpPermille;
        public int ShieldMaxHpPermille;
    }

    public class EffectRogueHealSpirit : EffectNode
    {
        public int HealPermille;
        public int IntervalMs;
    }

    public class EffectRogueOutOfCombatStealth : EffectNode
    {
    }

    public class EffectRogueAfterSkillSpeedBoost : EffectNode
    {
        public int SpeedPct;
        public int DurationMs;
    }

    public class EffectRogueProbabilityMultiplier : EffectNode
    {
        public int Permille;
    }

    public class EffectRogueContainerResultProbabilityMultiplier : EffectNode
    {
        public int Permille;
    }

    public class EffectRogueSearchMonsterProbabilityMultiplier : EffectNode
    {
        public int Permille;
    }

    public class EffectRogueSkillDisable : EffectNode
    {
    }

    public class EffectRogueExtendGameTime : EffectNode
    {
        public long DurationMs;
    }

    public class EffectRogueScaleModifier : EffectNode
    {
        public int ScalePermille;
        public int MaxHpPermille;
    }

    public class EffectRogueSizeDifferenceDamageBonus : EffectNode
    {
        public int MaxDifferencePermille;
        public int MaxDamageBonusPermille;
    }

    public class EffectRogueHitHeroCritGrowth : EffectNode
    {
        public int CritPermillePerHit;
    }

    public class EffectRogueReloadFirstShotsBoost : EffectNode
    {
        public int DamageBonusPermille;
        public int ShotCount;
        public int PenetrationCount;
    }

    public class EffectRogueReplaceAllCards : EffectNode
    {
    }

    [EnableClass]
    public class RogueWeaponModifierEntry
    {
        public int ModType;
        public int ValuePermille;
    }

    public class EffectRogueWeaponModifiers : EffectNode
    {
        public List<RogueWeaponModifierEntry> Entries = new();
    }
}
