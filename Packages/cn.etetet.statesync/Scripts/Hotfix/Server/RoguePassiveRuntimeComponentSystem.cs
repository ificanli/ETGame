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
