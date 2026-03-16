namespace ET.Server
{
    [EntitySystemOf(typeof(TacticalVisionComponent))]
    public static partial class TacticalVisionComponentSystem
    {
        private const int TickIntervalMs = 200;

        [Invoke(TimerInvokeType.TacticalVisionTick)]
        public class TacticalVisionTickTimer : ATimer<TacticalVisionComponent>
        {
            protected override void Run(TacticalVisionComponent self)
            {
                TacticalVisionHelper.Refresh(self);
            }
        }

        [EntitySystem]
        private static void Awake(this TacticalVisionComponent self)
        {
            self.TickTimerId = self.Root().TimerComponent.NewRepeatedTimer(TickIntervalMs, TimerInvokeType.TacticalVisionTick, self);
        }

        [EntitySystem]
        private static void Destroy(this TacticalVisionComponent self)
        {
            if (self.TickTimerId == 0)
            {
                return;
            }

            self.Root()?.TimerComponent?.Remove(ref self.TickTimerId);
            self.TickTimerId = 0;
        }
    }
}
