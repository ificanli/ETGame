namespace ET.Server
{
    [Invoke(TimerInvokeType.PlayerEvacuationTimer)]
    public class PlayerEvacuationTimer : ATimer<PlayerEvacuationComponent>
    {
        protected override void Run(PlayerEvacuationComponent self)
        {
            try
            {
                self.Update();
            }
            catch (System.Exception e)
            {
                Log.Error($"[PlayerEvacuationTimer] Error: {e}");
            }
        }
    }
}
