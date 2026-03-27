namespace ET.Test
{
    [Invoke(TimerInvokeType.TestOnceTimer)]
    public class TestOnceTimerHandler : ATimer<TestTimerEntity>
    {
        protected override void Run(TestTimerEntity self)
        {
            self.TriggerCount++;
        }
    }

    [Invoke(TimerInvokeType.TestRepeatedTimer)]
    public class TestRepeatedTimerHandler : ATimer<TestTimerEntity>
    {
        protected override void Run(TestTimerEntity self)
        {
            self.TriggerCount++;
        }
    }

    [Invoke(TimerInvokeType.TestHighFrequencyTick)]
    public class TestHighFrequencyTickHandler : AInvokeHandler<HighFrequencyTickCallback>
    {
        private const int TestHighFrequencyChannelId = 9001;

        public override void Handle(HighFrequencyTickCallback args)
        {
            Entity entity = args.Entity;
            TestHighFrequencyEntity self = entity as TestHighFrequencyEntity;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            ++self.TickCount;
            if (!self.RemoveOnFirstTick || self.TickCount != 1)
            {
                return;
            }

            HighFrequencySchedulerComponent scheduler = self.Scene()?.GetComponent<HighFrequencySchedulerComponent>();
            if (scheduler == null)
            {
                return;
            }

            self.DeferredRemoveObserved = scheduler.RequestRemoveEntity(TestHighFrequencyChannelId, self, out bool deferred) && deferred;
        }
    }

    [Invoke(TimerInvokeType.TestHighFrequencyRemoved)]
    public class TestHighFrequencyRemovedHandler : AInvokeHandler<HighFrequencyEntityRemovedCallback>
    {
        public override void Handle(HighFrequencyEntityRemovedCallback args)
        {
            Entity entity = args.Entity;
            TestHighFrequencyEntity self = entity as TestHighFrequencyEntity;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            ++self.RemovedCount;
            self.DeferredRemoveObserved = self.DeferredRemoveObserved || args.IsDeferredCommit;
        }
    }
}
