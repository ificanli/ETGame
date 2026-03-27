namespace ET.Test
{
    [ChildOf]
    public class TestTimerEntity : Entity, IAwake
    {
        public int TriggerCount;
    }

    [ChildOf]
    public class TestHighFrequencyEntity : Entity, IAwake
    {
        public int TickCount;
        public int RemovedCount;
        public bool RemoveOnFirstTick;
        public bool DeferredRemoveObserved;
    }
}
