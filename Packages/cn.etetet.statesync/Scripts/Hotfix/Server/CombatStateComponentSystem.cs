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
}
