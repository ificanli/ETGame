namespace ET.Server
{
    [EnableClass]
    public class EvacuationDurationAdjustContext
    {
        public EntityRef<Unit> Player;
        public long DurationMs;
    }

    public struct EvacuationDurationAdjustEvent
    {
        public EvacuationDurationAdjustContext Context;
    }
}
