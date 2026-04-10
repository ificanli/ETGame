using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    [EntitySystemOf(typeof(RogueOutOfCombatStealthStateComponent))]
    public static partial class RogueOutOfCombatStealthStateComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueOutOfCombatStealthStateComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            self.Concealed = false;
            self.ConcealmentSourceId = $"rogue_stealth_{unit?.Id ?? 0}";
        }

        [EntitySystem]
        private static void Destroy(this RogueOutOfCombatStealthStateComponent self)
        {
            self.ClearConcealment();
        }
    }

    [EntitySystemOf(typeof(RogueAfterSkillSpeedBoostComponent))]
    public static partial class RogueAfterSkillSpeedBoostComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueAfterSkillSpeedBoostComponent self)
        {
            self.AppliedSpeedPct = 0;
            self.RemoveTimerId = 0;
        }

        [EntitySystem]
        private static void Destroy(this RogueAfterSkillSpeedBoostComponent self)
        {
            self.ClearSpeedBoost();
        }
    }

    [EntitySystemOf(typeof(RogueTrapMasterStateComponent))]
    public static partial class RogueTrapMasterStateComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueTrapMasterStateComponent self)
        {
            self.Sources.Clear();
            self.EffectiveIdleMs = 0;
            self.EffectiveRequiredShotCount = 0;
            self.EffectiveTrapDamagePermille = 0;
            self.EffectiveTrapRadius = 0f;
            self.EffectiveTrapLifetimeMs = 0;
            self.EffectiveTrapTickIntervalMs = 0;
            self.AccumulatedShots = 0;
            self.LastMoveTime = TimeInfo.Instance.ServerNow();
            self.MoveVersion = 0;
            self.LastTriggeredMoveVersion = -1;
        }

        [EntitySystem]
        private static void Destroy(this RogueTrapMasterStateComponent self)
        {
            self.Sources.Clear();
            self.EffectiveIdleMs = 0;
            self.EffectiveRequiredShotCount = 0;
            self.EffectiveTrapDamagePermille = 0;
            self.EffectiveTrapRadius = 0f;
            self.EffectiveTrapLifetimeMs = 0;
            self.EffectiveTrapTickIntervalMs = 0;
            self.AccumulatedShots = 0;
            self.LastMoveTime = 0;
            self.MoveVersion = 0;
            self.LastTriggeredMoveVersion = -1;
        }
    }

    [EntitySystemOf(typeof(RogueTrapEntity))]
    public static partial class RogueTrapEntitySystem
    {
        [EntitySystem]
        private static void Awake(this RogueTrapEntity self)
        {
            self.OwnerId = 0;
            self.Position = float3.zero;
            self.Damage = 0;
            self.WeaponId = 0;
            self.Radius = 0f;
            self.TickIntervalMs = 0;
            self.ExpireTime = 0;
            self.TimerId = 0;
        }

        [EntitySystem]
        private static void Destroy(this RogueTrapEntity self)
        {
            self.StopTimer();
            self.OwnerId = 0;
            self.Position = float3.zero;
            self.Damage = 0;
            self.WeaponId = 0;
            self.Radius = 0f;
            self.TickIntervalMs = 0;
            self.ExpireTime = 0;
        }
    }

    [EntitySystemOf(typeof(RogueHitHeroCritStateComponent))]
    public static partial class RogueHitHeroCritStateComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueHitHeroCritStateComponent self)
        {
            self.Sources.Clear();
        }

        [EntitySystem]
        private static void Destroy(this RogueHitHeroCritStateComponent self)
        {
            self.Sources.Clear();
        }
    }

    [EntitySystemOf(typeof(RogueReloadFirstShotsStateComponent))]
    public static partial class RogueReloadFirstShotsStateComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueReloadFirstShotsStateComponent self)
        {
            self.Sources.Clear();
        }

        [EntitySystem]
        private static void Destroy(this RogueReloadFirstShotsStateComponent self)
        {
            self.Sources.Clear();
        }
    }

    [EntitySystemOf(typeof(RogueOnKillWeaponEnchantStateComponent))]
    public static partial class RogueOnKillWeaponEnchantStateComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueOnKillWeaponEnchantStateComponent self)
        {
            self.Sources.Clear();
        }

        [EntitySystem]
        private static void Destroy(this RogueOnKillWeaponEnchantStateComponent self)
        {
            self.Sources.Clear();
        }
    }

    [EntitySystemOf(typeof(RogueTemporaryItemStateComponent))]
    public static partial class RogueTemporaryItemStateComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueTemporaryItemStateComponent self)
        {
            self.Sources.Clear();
        }

        [EntitySystem]
        private static void Destroy(this RogueTemporaryItemStateComponent self)
        {
            self.Sources.Clear();
        }
    }

    [EntitySystemOf(typeof(RogueSummonedSpiritStateComponent))]
    public static partial class RogueSummonedSpiritStateComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueSummonedSpiritStateComponent self)
        {
            self.Sources.Clear();
        }

        [EntitySystem]
        private static void Destroy(this RogueSummonedSpiritStateComponent self)
        {
            Unit owner = self.GetParent<Unit>();
            self.ClearAll(owner);
            self.Sources.Clear();
        }
    }

    [Invoke(TimerInvokeType.RogueAfterSkillSpeedBoostExpire)]
    public class RogueAfterSkillSpeedBoostExpireTimer : ATimer<RogueAfterSkillSpeedBoostComponent>
    {
        protected override void Run(RogueAfterSkillSpeedBoostComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            self.ClearSpeedBoost();
            unit?.RemoveComponent<RogueAfterSkillSpeedBoostComponent>();
        }
    }

    public static class RoguePassiveRuntimeHelper
    {
        public static void AddSource(this RogueHitHeroCritStateComponent self, long sourceId, int critPermillePerHit)
        {
            if (self == null || self.IsDisposed || sourceId == 0 || critPermillePerHit <= 0)
            {
                return;
            }

            if (!self.Sources.TryGetValue(sourceId, out RogueHitHeroCritSourceData sourceData))
            {
                sourceData = new RogueHitHeroCritSourceData();
            }

            sourceData.CritPermillePerHit = critPermillePerHit;
            self.Sources[sourceId] = sourceData;
        }

        public static void RemoveSource(this RogueHitHeroCritStateComponent self, long sourceId)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            self.Sources.Remove(sourceId);
        }

        public static void OnHitHero(this RogueHitHeroCritStateComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            using ListComponent<long> sourceIds = ListComponent<long>.Create();
            foreach (long sourceId in self.Sources.Keys)
            {
                sourceIds.Add(sourceId);
            }

            foreach (long sourceId in sourceIds)
            {
                if (!self.Sources.TryGetValue(sourceId, out RogueHitHeroCritSourceData sourceData) || sourceData.CritPermillePerHit <= 0)
                {
                    continue;
                }

                sourceData.AccumulatedCritPermille += sourceData.CritPermillePerHit;
                self.Sources[sourceId] = sourceData;
            }
        }

        public static int GetTotalCritPermille(this RogueHitHeroCritStateComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return 0;
            }

            int total = 0;
            foreach (RogueHitHeroCritSourceData sourceData in self.Sources.Values)
            {
                if (sourceData.AccumulatedCritPermille > 0)
                {
                    total += sourceData.AccumulatedCritPermille;
                }
            }

            return total;
        }

        public static bool IsEmpty(this RogueHitHeroCritStateComponent self)
        {
            return self == null || self.IsDisposed || self.Sources.Count == 0;
        }

        public static void AddSource(
            this RogueReloadFirstShotsStateComponent self,
            long sourceId,
            int damageBonusPermille,
            int shotCount,
            int penetrationCount)
        {
            if (self == null || self.IsDisposed || sourceId == 0 || damageBonusPermille <= 0 || shotCount <= 0)
            {
                return;
            }

            self.Sources[sourceId] = new RogueReloadFirstShotsSourceData
            {
                DamageBonusPermille = damageBonusPermille,
                ShotCount = shotCount,
                RemainingShots = 0,
                PenetrationCount = penetrationCount > 0 ? penetrationCount : 0,
            };
        }

        public static void RemoveSource(this RogueReloadFirstShotsStateComponent self, long sourceId)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            self.Sources.Remove(sourceId);
        }

        public static void ActivateOnReload(this RogueReloadFirstShotsStateComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            using ListComponent<long> sourceIds = ListComponent<long>.Create();
            foreach (long sourceId in self.Sources.Keys)
            {
                sourceIds.Add(sourceId);
            }

            foreach (long sourceId in sourceIds)
            {
                if (!self.Sources.TryGetValue(sourceId, out RogueReloadFirstShotsSourceData sourceData) || sourceData.ShotCount <= 0)
                {
                    continue;
                }

                sourceData.RemainingShots = sourceData.ShotCount;
                self.Sources[sourceId] = sourceData;
            }
        }

        public static void ConsumeShot(this RogueReloadFirstShotsStateComponent self, out int totalDamageBonusPermille, out int maxPenetrationCount)
        {
            totalDamageBonusPermille = 0;
            maxPenetrationCount = 0;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            using ListComponent<long> sourceIds = ListComponent<long>.Create();
            foreach (long sourceId in self.Sources.Keys)
            {
                sourceIds.Add(sourceId);
            }

            foreach (long sourceId in sourceIds)
            {
                if (!self.Sources.TryGetValue(sourceId, out RogueReloadFirstShotsSourceData sourceData) || sourceData.RemainingShots <= 0)
                {
                    continue;
                }

                totalDamageBonusPermille += sourceData.DamageBonusPermille;
                if (sourceData.PenetrationCount > maxPenetrationCount)
                {
                    maxPenetrationCount = sourceData.PenetrationCount;
                }

                sourceData.RemainingShots -= 1;
                self.Sources[sourceId] = sourceData;
            }
        }

        public static bool IsEmpty(this RogueReloadFirstShotsStateComponent self)
        {
            return self == null || self.IsDisposed || self.Sources.Count == 0;
        }

        public static void SetSource(this RogueOnKillWeaponEnchantStateComponent self, long sourceId, EffectRogueOnKillWeaponEnchant effect)
        {
            if (self == null || self.IsDisposed || sourceId == 0 || effect == null || effect.SlotIndex <= 0)
            {
                return;
            }

            self.Sources[sourceId] = new RogueOnKillWeaponEnchantSourceData
            {
                SlotIndex = effect.SlotIndex,
                DamageBonusPermille = effect.DamageBonusPermille,
                PenetrationCount = effect.PenetrationCount,
                TargetFilter = effect.TargetFilter,
            };
        }

        public static void RemoveSource(this RogueOnKillWeaponEnchantStateComponent self, long sourceId)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            self.Sources.Remove(sourceId);
        }

        public static bool IsEmpty(this RogueOnKillWeaponEnchantStateComponent self)
        {
            return self == null || self.IsDisposed || self.Sources.Count == 0;
        }

        public static void RegisterSource(this RogueTemporaryItemStateComponent self, long sourceId, int itemConfigId, int count)
        {
            if (self == null || self.IsDisposed || sourceId == 0 || itemConfigId <= 0 || count <= 0)
            {
                return;
            }

            int resolvedConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(itemConfigId);
            self.Sources[sourceId] = new RogueTemporaryItemSourceData
            {
                ItemConfigId = resolvedConfigId,
                RemainingCount = count,
            };
        }

        public static int ConsumeItem(this RogueTemporaryItemStateComponent self, int itemConfigId, int count)
        {
            if (self == null || self.IsDisposed || itemConfigId <= 0 || count <= 0)
            {
                return 0;
            }

            int resolvedConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(itemConfigId);
            int remaining = count;
            using ListComponent<long> sourceIds = ListComponent<long>.Create();
            foreach (long sourceId in self.Sources.Keys)
            {
                sourceIds.Add(sourceId);
            }

            foreach (long sourceId in sourceIds)
            {
                if (remaining <= 0)
                {
                    break;
                }

                if (!self.Sources.TryGetValue(sourceId, out RogueTemporaryItemSourceData sourceData) ||
                    !LegacyItemConfigIdHelper.MatchesConfigId(sourceData.ItemConfigId, resolvedConfigId) ||
                    sourceData.RemainingCount <= 0)
                {
                    continue;
                }

                sourceData.ItemConfigId = resolvedConfigId;

                int consume = sourceData.RemainingCount >= remaining ? remaining : sourceData.RemainingCount;
                sourceData.RemainingCount -= consume;
                remaining -= consume;

                if (sourceData.RemainingCount > 0)
                {
                    self.Sources[sourceId] = sourceData;
                }
                else
                {
                    self.Sources.Remove(sourceId);
                }
            }

            return count - remaining;
        }

        public static void CleanupSource(this RogueTemporaryItemStateComponent self, Unit unit, long sourceId)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            if (!self.Sources.TryGetValue(sourceId, out RogueTemporaryItemSourceData sourceData))
            {
                return;
            }

            self.Sources.Remove(sourceId);
            int resolvedConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(sourceData.ItemConfigId);
            if (unit == null || unit.IsDisposed || resolvedConfigId <= 0 || sourceData.RemainingCount <= 0)
            {
                return;
            }

            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                return;
            }

            int currentCount = itemComponent.GetItemCount(resolvedConfigId);
            int removeCount = currentCount >= sourceData.RemainingCount ? sourceData.RemainingCount : currentCount;
            if (removeCount > 0)
            {
                ItemHelper.RemoveItem(itemComponent, resolvedConfigId, removeCount, ItemChangeReason.UseItem);
            }
        }

        public static void ClearAll(this RogueTemporaryItemStateComponent self, Unit unit)
        {
            if (self == null || self.IsDisposed || self.Sources.Count == 0)
            {
                return;
            }

            using ListComponent<long> sourceIds = ListComponent<long>.Create();
            foreach (long sourceId in self.Sources.Keys)
            {
                sourceIds.Add(sourceId);
            }

            foreach (long sourceId in sourceIds)
            {
                self.CleanupSource(unit, sourceId);
            }
        }

        public static bool IsEmpty(this RogueTemporaryItemStateComponent self)
        {
            return self == null || self.IsDisposed || self.Sources.Count == 0;
        }

        public static void SetSource(this RogueSummonedSpiritStateComponent self, long sourceId, long spiritUnitId, EffectRogueHealSpirit effect)
        {
            if (self == null || self.IsDisposed || sourceId == 0 || effect == null)
            {
                return;
            }

            if (!self.Sources.TryGetValue(sourceId, out RogueSummonedSpiritSourceData sourceData))
            {
                sourceData = new RogueSummonedSpiritSourceData();
            }

            sourceData.SpiritUnitId = spiritUnitId;
            sourceData.HealPermille = effect.HealPermille;
            sourceData.LifetimeMs = effect.IntervalMs;
            sourceData.DamageBonusPermille = effect.DamageBonusPermille;
            sourceData.AuraRadius = effect.AuraRadius;
            sourceData.PickupRadius = effect.PickupRadius;
            self.Sources[sourceId] = sourceData;
        }

        public static void ClearSource(this RogueSummonedSpiritStateComponent self, Unit owner, long sourceId)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            if (!self.Sources.TryGetValue(sourceId, out RogueSummonedSpiritSourceData sourceData))
            {
                return;
            }

            self.ApplySpiritAura(owner, sourceId, 0, false);
            self.Sources.Remove(sourceId);
            DisposeSpirit(owner, sourceData.SpiritUnitId);
        }

        public static void ReplaceSpirit(this RogueSummonedSpiritStateComponent self, Unit owner, long sourceId, long newSpiritUnitId, EffectRogueHealSpirit effect)
        {
            if (self == null || self.IsDisposed || sourceId == 0 || effect == null)
            {
                return;
            }

            if (self.Sources.TryGetValue(sourceId, out RogueSummonedSpiritSourceData sourceData))
            {
                self.ApplySpiritAura(owner, sourceId, 0, false);
                DisposeSpirit(owner, sourceData.SpiritUnitId);
            }

            self.SetSource(sourceId, newSpiritUnitId, effect);
        }

        public static void ClearAll(this RogueSummonedSpiritStateComponent self, Unit owner)
        {
            if (self == null || self.IsDisposed || self.Sources.Count == 0)
            {
                return;
            }

            using ListComponent<long> sourceIds = ListComponent<long>.Create();
            foreach (long sourceId in self.Sources.Keys)
            {
                sourceIds.Add(sourceId);
            }

            foreach (long sourceId in sourceIds)
            {
                self.ClearSource(owner, sourceId);
            }
        }

        public static bool IsEmpty(this RogueSummonedSpiritStateComponent self)
        {
            return self == null || self.IsDisposed || self.Sources.Count == 0;
        }

        public static void RefreshSource(this RogueSummonedSpiritStateComponent self, Unit owner, long sourceId)
        {
            if (self == null || self.IsDisposed || owner == null || owner.IsDisposed || sourceId == 0)
            {
                return;
            }

            if (!self.Sources.TryGetValue(sourceId, out RogueSummonedSpiritSourceData sourceData))
            {
                return;
            }

            UnitComponent unitComponent = owner.Scene()?.GetComponent<UnitComponent>();
            Unit spirit = unitComponent?.Get(sourceData.SpiritUnitId);
            if (spirit == null || spirit.IsDisposed)
            {
                self.ApplySpiritAura(owner, sourceId, 0, false);
                self.Sources.Remove(sourceId);
                return;
            }

            float distanceSqr = math.lengthsq(spirit.Position.xz - owner.Position.xz);
            float pickupRadius = sourceData.PickupRadius > 0f ? sourceData.PickupRadius : 0f;
            if (pickupRadius > 0f && distanceSqr <= pickupRadius * pickupRadius)
            {
                RogueBuffActionInternalHelper.HealByMaxHpPermille(owner, sourceData.HealPermille);
                self.ClearSource(owner, sourceId);
                return;
            }

            float auraRadius = sourceData.AuraRadius > 0f ? sourceData.AuraRadius : 0f;
            bool shouldAura = sourceData.DamageBonusPermille > 0 &&
                    auraRadius > 0f &&
                    distanceSqr <= auraRadius * auraRadius;
            bool auraModifierApplied = self.HasSpiritAuraModifier(owner, sourceId, sourceData.DamageBonusPermille);
            if (shouldAura != sourceData.AuraActive || (shouldAura && !auraModifierApplied))
            {
                self.ApplySpiritAura(owner, sourceId, sourceData.DamageBonusPermille, shouldAura);
                sourceData.AuraActive = shouldAura;
                self.Sources[sourceId] = sourceData;
            }
        }

        public static void RefreshAll(this RogueSummonedSpiritStateComponent self, Unit owner)
        {
            if (self == null || self.IsDisposed || owner == null || owner.IsDisposed || self.Sources.Count == 0)
            {
                return;
            }

            using ListComponent<long> sourceIds = ListComponent<long>.Create();
            foreach (long sourceId in self.Sources.Keys)
            {
                sourceIds.Add(sourceId);
            }

            foreach (long sourceId in sourceIds)
            {
                self.RefreshSource(owner, sourceId);
            }
        }

        private static void DisposeSpirit(Unit owner, long spiritUnitId)
        {
            if (owner == null || owner.IsDisposed || spiritUnitId == 0)
            {
                return;
            }

            UnitComponent unitComponent = owner.Scene()?.GetComponent<UnitComponent>();
            Unit spirit = unitComponent?.Get(spiritUnitId);
            if (spirit != null && !spirit.IsDisposed)
            {
                spirit.Dispose();
            }
        }

        private static void ApplySpiritAura(this RogueSummonedSpiritStateComponent self, Unit owner, long sourceId, int damageBonusPermille, bool active)
        {
            long modifierSourceId = ResolveSpiritAuraModifierSourceId(owner, sourceId);
            if (owner == null || owner.IsDisposed || modifierSourceId == 0)
            {
                return;
            }

            RogueWeaponModifierComponent modifierComponent = owner.GetComponent<RogueWeaponModifierComponent>();
            if (!active)
            {
                if (modifierComponent == null)
                {
                    return;
                }

                modifierComponent.RemoveSource(modifierSourceId);
                if (modifierComponent.IsEmpty())
                {
                    owner.RemoveComponent<RogueWeaponModifierComponent>();
                }

                WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(owner);

                return;
            }

            if (damageBonusPermille <= 0)
            {
                return;
            }

            modifierComponent ??= owner.AddComponent<RogueWeaponModifierComponent>();
            modifierComponent.SetSource(modifierSourceId, 0, new List<RogueWeaponModifierEntry>
            {
                new RogueWeaponModifierEntry
                {
                    ModType = WeaponModType.BulletDamage,
                    ValuePermille = damageBonusPermille,
                },
            });
            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(owner);
        }

        private static bool HasSpiritAuraModifier(this RogueSummonedSpiritStateComponent self, Unit owner, long sourceId, int damageBonusPermille)
        {
            long modifierSourceId = ResolveSpiritAuraModifierSourceId(owner, sourceId);
            RogueWeaponModifierComponent modifierComponent = owner?.GetComponent<RogueWeaponModifierComponent>();
            if (modifierSourceId == 0 ||
                modifierComponent == null ||
                modifierComponent.IsDisposed ||
                !modifierComponent.Sources.TryGetValue(modifierSourceId, out RogueWeaponModifierSourceData sourceData) ||
                sourceData == null ||
                sourceData.Modifiers == null)
            {
                return false;
            }

            return sourceData.Modifiers.TryGetValue(WeaponModType.BulletDamage, out int currentDamageBonus) &&
                    currentDamageBonus == damageBonusPermille;
        }

        private static long ResolveSpiritAuraModifierSourceId(Unit owner, long sourceId)
        {
            if (sourceId != 0)
            {
                return sourceId;
            }

            if (owner == null || owner.IsDisposed || owner.Id == 0)
            {
                return 0;
            }

            return -owner.Id;
        }

        public static void ApplyConcealment(this RogueOutOfCombatStealthStateComponent self)
        {
            if (self == null || self.IsDisposed || self.Concealed)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            Scene scene = unit?.Scene();
            ExtraUnitVisibilityComponent visibility = scene?.GetComponent<ExtraUnitVisibilityComponent>();
            if (unit == null || unit.IsDisposed || visibility == null)
            {
                return;
            }

            visibility.SetPlayerConcealmentState(self.ConcealmentSourceId, unit, true);
            self.Concealed = true;
        }

        public static void ClearConcealment(this RogueOutOfCombatStealthStateComponent self)
        {
            if (self == null || self.IsDisposed || !self.Concealed)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            Scene scene = unit?.Scene();
            ExtraUnitVisibilityComponent visibility = scene?.GetComponent<ExtraUnitVisibilityComponent>();
            if (unit != null && !unit.IsDisposed && visibility != null)
            {
                visibility.SetPlayerConcealmentState(self.ConcealmentSourceId, unit, false);
            }

            self.Concealed = false;
        }

        public static void RefreshSpeedBoost(this RogueAfterSkillSpeedBoostComponent self, int totalSpeedPct, int durationMs)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            NumericComponent numeric = unit?.NumericComponent;
            if (unit == null || unit.IsDisposed || numeric == null || totalSpeedPct <= 0 || durationMs <= 0)
            {
                self.ClearSpeedBoost();
                return;
            }

            if (self.AppliedSpeedPct != 0)
            {
                int oldFinalPct = numeric.GetAsInt(NumericType.SpeedFinalPct);
                numeric.Set(NumericType.SpeedFinalPct, oldFinalPct - self.AppliedSpeedPct);
            }

            self.AppliedSpeedPct = totalSpeedPct;
            int finalPct = numeric.GetAsInt(NumericType.SpeedFinalPct);
            numeric.Set(NumericType.SpeedFinalPct, finalPct + totalSpeedPct);

            if (self.RemoveTimerId != 0)
            {
                long removeTimerId = self.RemoveTimerId;
                unit.Root().TimerComponent.Remove(ref removeTimerId);
                self.RemoveTimerId = 0;
            }

            self.RemoveTimerId = unit.Root().TimerComponent.NewOnceTimer(
                TimeInfo.Instance.ServerNow() + durationMs,
                TimerInvokeType.RogueAfterSkillSpeedBoostExpire,
                self);
        }

        public static void ClearSpeedBoost(this RogueAfterSkillSpeedBoostComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            NumericComponent numeric = unit?.NumericComponent;
            if (numeric != null && self.AppliedSpeedPct != 0)
            {
                int finalPct = numeric.GetAsInt(NumericType.SpeedFinalPct);
                numeric.Set(NumericType.SpeedFinalPct, finalPct - self.AppliedSpeedPct);
            }

            self.AppliedSpeedPct = 0;

            if (self.RemoveTimerId != 0)
            {
                long timerId = self.RemoveTimerId;
                unit?.Root()?.TimerComponent?.Remove(ref timerId);
                self.RemoveTimerId = 0;
            }
        }

        public static void UpsertSource(this RogueTrapMasterStateComponent self, long sourceId, EffectRogueTrapMaster effect)
        {
            if (self == null ||
                self.IsDisposed ||
                sourceId == 0 ||
                effect == null ||
                effect.IdleMs <= 0 ||
                effect.BulletCount <= 0)
            {
                return;
            }

            self.Sources[sourceId] = new RogueTrapMasterSourceData
            {
                IdleMs = effect.IdleMs,
                BulletCount = effect.BulletCount,
                TrapDamagePermille = effect.TrapDamagePermille,
                TrapRadius = effect.TrapRadius,
                TrapLifetimeMs = effect.TrapLifetimeMs,
                TrapTickIntervalMs = effect.TrapTickIntervalMs,
            };
            self.RefreshTrapMasterEffectiveData();
        }

        public static void RemoveSource(this RogueTrapMasterStateComponent self, long sourceId)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            if (!self.Sources.Remove(sourceId))
            {
                return;
            }

            self.RefreshTrapMasterEffectiveData();
        }

        public static void OnMoved(this RogueTrapMasterStateComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.LastMoveTime = TimeInfo.Instance.ServerNow();
            self.AccumulatedShots = 0;
            self.MoveVersion += 1;
        }

        public static bool CanAccumulateTrapShots(this RogueTrapMasterStateComponent self)
        {
            if (self == null ||
                self.IsDisposed ||
                self.EffectiveIdleMs <= 0 ||
                self.EffectiveRequiredShotCount <= 0 ||
                self.EffectiveTrapDamagePermille <= 0 ||
                self.EffectiveTrapRadius <= 0f ||
                self.EffectiveTrapLifetimeMs <= 0 ||
                self.EffectiveTrapTickIntervalMs <= 0)
            {
                return false;
            }

            long now = TimeInfo.Instance.ServerNow();
            return now - self.LastMoveTime >= self.EffectiveIdleMs;
        }

        public static bool RegisterShotAndTryTriggerTrap(this RogueTrapMasterStateComponent self)
        {
            if (!self.CanAccumulateTrapShots())
            {
                return false;
            }

            self.AccumulatedShots += 1;
            if (self.AccumulatedShots < self.EffectiveRequiredShotCount)
            {
                return false;
            }

            self.AccumulatedShots = 0;
            self.LastTriggeredMoveVersion = self.MoveVersion;
            return true;
        }

        private static void RefreshTrapMasterEffectiveData(this RogueTrapMasterStateComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            int idleMs = int.MaxValue;
            int requiredShotCount = int.MaxValue;
            int trapDamagePermille = 0;
            float trapRadius = 0f;
            int trapLifetimeMs = 0;
            int trapTickIntervalMs = int.MaxValue;
            foreach (RogueTrapMasterSourceData source in self.Sources.Values)
            {
                if (source.IdleMs <= 0 ||
                    source.BulletCount <= 0 ||
                    source.TrapDamagePermille <= 0 ||
                    source.TrapRadius <= 0f ||
                    source.TrapLifetimeMs <= 0 ||
                    source.TrapTickIntervalMs <= 0)
                {
                    continue;
                }

                if (source.IdleMs < idleMs)
                {
                    idleMs = source.IdleMs;
                }

                if (source.BulletCount < requiredShotCount)
                {
                    requiredShotCount = source.BulletCount;
                }

                if (source.TrapDamagePermille > trapDamagePermille)
                {
                    trapDamagePermille = source.TrapDamagePermille;
                }

                if (source.TrapRadius > trapRadius)
                {
                    trapRadius = source.TrapRadius;
                }

                if (source.TrapLifetimeMs > trapLifetimeMs)
                {
                    trapLifetimeMs = source.TrapLifetimeMs;
                }

                if (source.TrapTickIntervalMs < trapTickIntervalMs)
                {
                    trapTickIntervalMs = source.TrapTickIntervalMs;
                }
            }

            self.EffectiveIdleMs = idleMs == int.MaxValue ? 0 : idleMs;
            self.EffectiveRequiredShotCount = requiredShotCount == int.MaxValue ? 0 : requiredShotCount;
            self.EffectiveTrapDamagePermille = trapDamagePermille;
            self.EffectiveTrapRadius = trapRadius > 0f ? trapRadius : 0f;
            self.EffectiveTrapLifetimeMs = trapLifetimeMs > 0 ? trapLifetimeMs : 0;
            self.EffectiveTrapTickIntervalMs = trapTickIntervalMs == int.MaxValue ? 0 : trapTickIntervalMs;
            if (self.AccumulatedShots >= self.EffectiveRequiredShotCount && self.EffectiveRequiredShotCount > 0)
            {
                self.AccumulatedShots = 0;
            }
        }

        public static void Initialize(this RogueTrapEntity self, Unit owner, float3 position, long damage, int weaponId, float radius, int tickIntervalMs, int lifetimeMs)
        {
            if (self == null || self.IsDisposed || owner == null || owner.IsDisposed || damage <= 0 || radius <= 0f || tickIntervalMs <= 0 || lifetimeMs <= 0)
            {
                self?.Dispose();
                return;
            }

            self.StopTimer();
            self.OwnerId = owner.Id;
            self.Position = position;
            self.Damage = damage;
            self.WeaponId = weaponId;
            self.Radius = radius;
            self.TickIntervalMs = tickIntervalMs;
            self.ExpireTime = TimeInfo.Instance.ServerNow() + lifetimeMs;
            self.TimerId = owner.Root().TimerComponent.NewRepeatedTimer(tickIntervalMs, TimerInvokeType.RogueTrapTick, self);
        }

        public static void Tick(this RogueTrapEntity self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            long now = TimeInfo.Instance.ServerNow();
            if (now >= self.ExpireTime)
            {
                self.Dispose();
                return;
            }

            Scene scene = self.GetParent<Scene>();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            Unit owner = unitComponent?.Get(self.OwnerId);
            if (scene == null || scene.IsDisposed || unitComponent == null || owner == null || owner.IsDisposed)
            {
                self.Dispose();
                return;
            }

            float radiusSqr = self.Radius * self.Radius;
            using ListComponent<Unit> targets = ListComponent<Unit>.Create();
            foreach (Unit target in unitComponent.Children.Values)
            {
                if (!MonsterRuntimeProfileHelper.IsMonster(target))
                {
                    continue;
                }

                float2 offset = target.Position.xz - self.Position.xz;
                if (math.lengthsq(offset) <= radiusSqr)
                {
                    targets.Add(target);
                }
            }

            if (targets.Count == 0)
            {
                return;
            }

            foreach (Unit target in targets)
            {
                DamageContextHelper.ApplyDamage(scene, owner, target, self.Damage, self.WeaponId, false);
            }

            self.Dispose();
        }

        public static void StopTimer(this RogueTrapEntity self)
        {
            if (self == null || self.IsDisposed || self.TimerId == 0)
            {
                return;
            }

            long timerId = self.TimerId;
            self.TimerId = 0;
            self.Root()?.TimerComponent?.Remove(ref timerId);
        }
    }

    [Invoke(TimerInvokeType.RogueTrapTick)]
    public class RogueTrapTickTimer : ATimer<RogueTrapEntity>
    {
        protected override void Run(RogueTrapEntity self)
        {
            self.Tick();
        }
    }
}
