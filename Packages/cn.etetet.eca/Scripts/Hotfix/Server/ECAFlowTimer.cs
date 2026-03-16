using ET;

namespace ET.Server
{
    [Invoke(TimerInvokeType.ECAFlowTimer)]
    public class ECAFlowTimer : ATimer<ECAFlowTimerComponent>
    {
        protected override void Run(ECAFlowTimerComponent self)
        {
            try
            {
                ECAFlowTimerHelper.HandleTimerElapsed(self);
            }
            catch (System.Exception e)
            {
                Log.Error($"[ECAFlowTimer] Error: {e}");
            }
        }
    }
}
