namespace ET.Server
{
    [EntitySystemOf(typeof(CombatStateComponent))]
    public static partial class CombatStateComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CombatStateComponent self)
        {
            self.LastDealDamageTime = 0;
            self.LastTakeDamageTime = 0;
            self.OutOfCombatDelayMs = 5000;
            self.InCombat = false;
            self.CheckTimerId = 0;
        }

        [EntitySystem]
        private static void Destroy(this CombatStateComponent self)
        {
            self.StopCheckTimer();
        }

        public static void OnDealDamage(this CombatStateComponent self)
        {
            self.LastDealDamageTime = TimeInfo.Instance.ServerNow();
            self.TryEnterCombat();
        }

        public static void OnTakeDamage(this CombatStateComponent self)
        {
            self.LastTakeDamageTime = TimeInfo.Instance.ServerNow();
            self.TryEnterCombat();
        }

        private static void TryEnterCombat(this CombatStateComponent self)
        {
            if (self.InCombat)
            {
                self.RestartCheckTimer();
                return;
            }

            self.InCombat = true;
            Unit unit = self.GetParent<Unit>();
            if (unit != null && !unit.IsDisposed)
            {
                EventSystem.Instance.Publish(unit.Scene(), new UnitEnterCombat { Unit = unit });
            }

            self.RestartCheckTimer();
        }

        private static void StopCheckTimer(this CombatStateComponent self)
        {
            if (self.CheckTimerId == 0)
            {
                return;
            }

            long timerId = self.CheckTimerId;
            self.CheckTimerId = 0;
            self.GetParent<Unit>()?.Root()?.TimerComponent?.Remove(ref timerId);
        }

        private static void RestartCheckTimer(this CombatStateComponent self)
        {
            self.StopCheckTimer();

            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            self.CheckTimerId = unit.Root().TimerComponent.NewOnceTimer(
                TimeInfo.Instance.ServerNow() + self.OutOfCombatDelayMs,
                TimerInvokeType.CombatStateCheck,
                self);
        }

        public static void CheckCombatTimeout(this CombatStateComponent self)
        {
            if (!self.InCombat)
            {
                return;
            }

            long now = TimeInfo.Instance.ServerNow();
            long lastCombatTime = self.LastDealDamageTime > self.LastTakeDamageTime
                ? self.LastDealDamageTime
                : self.LastTakeDamageTime;

            if (now - lastCombatTime >= self.OutOfCombatDelayMs)
            {
                self.InCombat = false;
                Unit unit = self.GetParent<Unit>();
                if (unit != null && !unit.IsDisposed)
                {
                    EventSystem.Instance.Publish(unit.Scene(), new UnitLeaveCombat { Unit = unit });
                }
            }
            else
            {
                self.RestartCheckTimer();
            }
        }
    }

    [Invoke(TimerInvokeType.CombatStateCheck)]
    public class CombatStateCheckTimer : ATimer<CombatStateComponent>
    {
        protected override void Run(CombatStateComponent self)
        {
            self.CheckCombatTimeout();
        }
    }

    [FriendOf(typeof(RunTimeLimitComponent))]
    [EntitySystemOf(typeof(RunTimeLimitComponent))]
    public static partial class RunTimeLimitComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RunTimeLimitComponent self, long durationMs, string mapName)
        {
            self.DurationMs = durationMs > 0 ? durationMs : RunTimeLimitConst.PlayerTimeoutMs;
            self.MapName = mapName ?? string.Empty;
            self.StartTime = TimeInfo.Instance.ServerNow();
            self.DeadlineTime = self.StartTime + self.DurationMs;
            self.TimerId = 0;
            self.RestartTimer();
        }

        [EntitySystem]
        private static void Destroy(this RunTimeLimitComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            RunTimeLimitMessageHelper.ClearState(unit);
            self.StopTimer();
        }

        public static void TriggerTimeoutDeath(this RunTimeLimitComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                self.Dispose();
                return;
            }

            NumericComponent numeric = unit.NumericComponent;
            if (numeric == null)
            {
                Log.Warning($"[RunTimeLimit] timeout skipped: NumericComponent missing, unitId={unit.Id}, map={self.MapName}");
                self.Dispose();
                return;
            }

            long currentHp = numeric.GetAsLong(NumericType.HP);
            if (currentHp <= 0)
            {
                self.Dispose();
                return;
            }

            string mapName = unit.Scene()?.Name.GetSceneConfigName() ?? self.MapName;
            Log.Info($"[RunTimeLimit] timeout reached, unitId={unit.Id}, map={mapName}, durationMs={self.DurationMs}, deadlineTime={self.DeadlineTime}");

            numeric.Set(NumericType.HP, 0);
            EventSystem.Instance.Publish(unit.Scene(), new UnitDie
            {
                Target = unit,
                TargetId = unit.Id,
                TargetUnitType = (int)unit.UnitType,
            });

            self.Dispose();
        }

        public static void AdjustDuration(this RunTimeLimitComponent self, long deltaMs)
        {
            if (self == null || self.IsDisposed || deltaMs == 0)
            {
                return;
            }

            self.DurationMs += deltaMs;
            if (self.DurationMs < 1)
            {
                self.DurationMs = 1;
            }

            self.DeadlineTime += deltaMs;
            long minDeadlineTime = TimeInfo.Instance.ServerNow() + 1;
            if (self.DeadlineTime < minDeadlineTime)
            {
                self.DeadlineTime = minDeadlineTime;
            }

            long effectiveDuration = self.DeadlineTime - self.StartTime;
            self.DurationMs = effectiveDuration > 0 ? effectiveDuration : 1;
            self.RestartTimer();
            RunTimeLimitMessageHelper.SyncState(self.GetParent<Unit>(), self);
        }

        private static void RestartTimer(this RunTimeLimitComponent self)
        {
            self.StopTimer();

            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            self.TimerId = unit.Root().TimerComponent.NewOnceTimer(
                self.DeadlineTime,
                TimerInvokeType.RunTimeLimitExpire,
                self);
        }

        private static void StopTimer(this RunTimeLimitComponent self)
        {
            if (self.TimerId == 0)
            {
                return;
            }

            long timerId = self.TimerId;
            self.TimerId = 0;
            self.GetParent<Unit>()?.Root()?.TimerComponent?.Remove(ref timerId);
        }
    }

    [Invoke(TimerInvokeType.RunTimeLimitExpire)]
    public class RunTimeLimitExpireTimer : ATimer<RunTimeLimitComponent>
    {
        protected override void Run(RunTimeLimitComponent self)
        {
            try
            {
                self.TriggerTimeoutDeath();
            }
            catch (System.Exception e)
            {
                Log.Error($"[RunTimeLimit] timer error: {e}");
            }
        }
    }

    public static class RogueRunTimeLimitHelper
    {
        public static long GetEffectiveDurationMs(Unit unit)
        {
            long durationMs = RunTimeLimitConst.PlayerTimeoutMs;
            long extendGameTimeMs = RogueEffectQueryHelper.GetExtendGameTimeMs(unit);
            if (extendGameTimeMs > 0)
            {
                durationMs += extendGameTimeMs;
            }

            return durationMs;
        }

        public static void RefreshDurationByExtendDelta(Unit unit, long previousExtendMs, long currentExtendMs)
        {
            RunTimeLimitComponent runTimeLimit = unit?.GetComponent<RunTimeLimitComponent>();
            if (runTimeLimit == null)
            {
                return;
            }

            long deltaMs = currentExtendMs - previousExtendMs;
            if (deltaMs == 0)
            {
                return;
            }

            runTimeLimit.AdjustDuration(deltaMs);
        }
    }
}
