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

                foreach (Entity entity in unitComponent.Children.Values)
                {
                    if (entity is not Unit unit) continue;
                    if (unit.GetComponent<ECAPointComponent>() != null) continue; // 跳过 ECA 点本身
                    ECAHelper.CheckPlayerInRange(unit);
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[ECACheckRangeTimer] Error: {e}");
            }
        }
    }
}
