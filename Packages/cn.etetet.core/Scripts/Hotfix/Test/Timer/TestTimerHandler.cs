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
}
