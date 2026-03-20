namespace ET.Server
{
    /// <summary>
    /// 武器换弹完成定时调度。
    /// 让换弹在脱离目标或行为树不再轮询时也能按时结束。
    /// </summary>
    public static class WeaponReloadSchedulerHelper
    {
        public static void RefreshTimer(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent == null)
            {
                return;
            }

            StopTimer(weaponComponent);

            long nextFinishTime = GetNextFinishTime(weaponComponent);
            if (nextFinishTime <= 0)
            {
                return;
            }

            weaponComponent.ReloadTimerId = unit.Root().TimerComponent.NewOnceTimer(
                nextFinishTime,
                TimerInvokeType.WeaponReloadComplete,
                weaponComponent);
        }

        public static void StopTimer(WeaponComponent weaponComponent)
        {
            if (weaponComponent == null || weaponComponent.IsDisposed || weaponComponent.ReloadTimerId == 0)
            {
                return;
            }

            long timerId = weaponComponent.ReloadTimerId;
            weaponComponent.ReloadTimerId = 0;
            weaponComponent.Root()?.TimerComponent?.Remove(ref timerId);
        }

        public static void OnTimer(WeaponComponent weaponComponent)
        {
            if (weaponComponent == null || weaponComponent.IsDisposed)
            {
                return;
            }

            weaponComponent.ReloadTimerId = 0;

            Unit unit = weaponComponent.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            WeaponReloadHelper.TryCompleteReload(unit, 1);
            WeaponReloadHelper.TryCompleteReload(unit, 2);
            RefreshTimer(unit);
        }

        private static long GetNextFinishTime(WeaponComponent weaponComponent)
        {
            long nextFinishTime = 0;
            CollectNextFinishTime(weaponComponent, 1, ref nextFinishTime);
            CollectNextFinishTime(weaponComponent, 2, ref nextFinishTime);
            return nextFinishTime;
        }

        private static void CollectNextFinishTime(WeaponComponent weaponComponent, int slotIndex, ref long nextFinishTime)
        {
            if (weaponComponent == null || !weaponComponent.IsReloading(slotIndex))
            {
                return;
            }

            long finishTime = weaponComponent.GetReloadFinishTime(slotIndex);
            if (finishTime <= 0)
            {
                return;
            }

            if (nextFinishTime == 0 || finishTime < nextFinishTime)
            {
                nextFinishTime = finishTime;
            }
        }
    }

    [Invoke(TimerInvokeType.WeaponReloadComplete)]
    public class WeaponReloadCompleteTimer : ATimer<WeaponComponent>
    {
        protected override void Run(WeaponComponent self)
        {
            WeaponReloadSchedulerHelper.OnTimer(self);
        }
    }
}
