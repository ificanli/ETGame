namespace ET.Server
{
    [Invoke(TimerInvokeType.ECACheckRange)]
    public class ECACheckRangeTimer : ATimer<ECAManagerComponent>
    {
        protected override void Run(ECAManagerComponent self)
        {
            try
            {
                UnitComponent unitComponent = self.Scene().GetComponent<UnitComponent>();
                if (unitComponent == null) return;

                int scannedUnitCount = 0;
                foreach (Entity entity in unitComponent.Children.Values)
                {
                    if (entity is not Unit unit) continue;
                    if (unit.GetComponent<ECAPointComponent>() != null) continue; // 跳过 ECA 点本身
                    if (unit.UnitType != UnitType.Player) continue;
                    ++scannedUnitCount;
                    ECAHelper.CheckPlayerInRange(unit);
                }

                long now = TimeInfo.Instance.ServerNow();
                if (now - self.RangeDebugLastLogTime >= 5000)
                {
                    self.RangeDebugLastLogTime = now;
                    int pointCount = self.ECAPoints?.Count ?? 0;
                    Log.Info($"[ECADebug][RangeTick] scene={self.Scene().Name}, ecaPoints={pointCount}, scannedUnits={scannedUnitCount}");
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[ECACheckRangeTimer] Error: {e}");
            }
        }
    }
}
