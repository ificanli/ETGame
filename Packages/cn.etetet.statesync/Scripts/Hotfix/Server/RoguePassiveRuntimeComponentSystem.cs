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
            self.EffectiveBulletCount = 0;
            self.LastMoveTime = TimeInfo.Instance.ServerNow();
            self.MoveVersion = 0;
            self.LastTriggeredMoveVersion = -1;
        }

        [EntitySystem]
        private static void Destroy(this RogueTrapMasterStateComponent self)
        {
            self.Sources.Clear();
            self.EffectiveIdleMs = 0;
            self.EffectiveBulletCount = 0;
            self.LastMoveTime = 0;
            self.MoveVersion = 0;
            self.LastTriggeredMoveVersion = -1;
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

        public static void RegisterSource(this RogueTemporaryItemStateComponent self, long sourceId, int itemConfigId, int count)
        {
            if (self == null || self.IsDisposed || sourceId == 0 || itemConfigId <= 0 || count <= 0)
            {
                return;
            }

            self.Sources[sourceId] = new RogueTemporaryItemSourceData
            {
                ItemConfigId = itemConfigId,
                RemainingCount = count,
            };
        }

        public static int ConsumeItem(this RogueTemporaryItemStateComponent self, int itemConfigId, int count)
        {
            if (self == null || self.IsDisposed || itemConfigId <= 0 || count <= 0)
            {
                return 0;
            }

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
                    sourceData.ItemConfigId != itemConfigId ||
                    sourceData.RemainingCount <= 0)
                {
                    continue;
                }

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
            if (unit == null || unit.IsDisposed || sourceData.ItemConfigId <= 0 || sourceData.RemainingCount <= 0)
            {
                return;
            }

            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                return;
            }

            int currentCount = itemComponent.GetItemCount(sourceData.ItemConfigId);
            int removeCount = currentCount >= sourceData.RemainingCount ? sourceData.RemainingCount : currentCount;
            if (removeCount > 0)
            {
                ItemHelper.RemoveItem(itemComponent, sourceData.ItemConfigId, removeCount, ItemChangeReason.UseItem);
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

        public static void SetSource(this RogueSummonedSpiritStateComponent self, long sourceId, long spiritUnitId)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            if (!self.Sources.TryGetValue(sourceId, out RogueSummonedSpiritSourceData sourceData))
            {
                sourceData = new RogueSummonedSpiritSourceData();
            }

            sourceData.SpiritUnitId = spiritUnitId;
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

            self.Sources.Remove(sourceId);
            DisposeSpirit(owner, sourceData.SpiritUnitId);
        }

        public static void ReplaceSpirit(this RogueSummonedSpiritStateComponent self, Unit owner, long sourceId, long newSpiritUnitId)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            if (self.Sources.TryGetValue(sourceId, out RogueSummonedSpiritSourceData sourceData))
            {
                DisposeSpirit(owner, sourceData.SpiritUnitId);
            }

            self.SetSource(sourceId, newSpiritUnitId);
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

        public static void UpsertSource(this RogueTrapMasterStateComponent self, long sourceId, int idleMs, int bulletCount)
        {
            if (self == null || self.IsDisposed || sourceId == 0 || idleMs <= 0 || bulletCount <= 0)
            {
                return;
            }

            self.Sources[sourceId] = new RogueTrapMasterSourceData
            {
                IdleMs = idleMs,
                BulletCount = bulletCount,
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
            self.MoveVersion += 1;
        }

        public static bool CanTrigger(this RogueTrapMasterStateComponent self)
        {
            if (self == null || self.IsDisposed || self.EffectiveIdleMs <= 0 || self.EffectiveBulletCount <= 0)
            {
                return false;
            }

            if (self.LastTriggeredMoveVersion == self.MoveVersion)
            {
                return false;
            }

            long now = TimeInfo.Instance.ServerNow();
            return now - self.LastMoveTime >= self.EffectiveIdleMs;
        }

        public static void MarkTriggered(this RogueTrapMasterStateComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.LastTriggeredMoveVersion = self.MoveVersion;
        }

        private static void RefreshTrapMasterEffectiveData(this RogueTrapMasterStateComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            int idleMs = int.MaxValue;
            int bulletCount = 0;
            foreach (RogueTrapMasterSourceData source in self.Sources.Values)
            {
                if (source.IdleMs <= 0 || source.BulletCount <= 0)
                {
                    continue;
                }

                if (source.IdleMs < idleMs)
                {
                    idleMs = source.IdleMs;
                }

                bulletCount += source.BulletCount;
            }

            self.EffectiveIdleMs = idleMs == int.MaxValue ? 0 : idleMs;
            self.EffectiveBulletCount = bulletCount > 0 ? bulletCount : 0;
        }
    }
}
