namespace ET.Server
{
    [EntitySystemOf(typeof(BulletTickComponent))]
    public static partial class BulletTickComponentSystem
    {
        [Invoke(TimerInvokeType.BulletTick)]
        public class BulletTickTimer : ATimer<BulletTickComponent>
        {
            protected override void Run(BulletTickComponent self)
            {
                self.Tick();
            }
        }

        [EntitySystem]
        private static void Awake(this BulletTickComponent self)
        {
            self.TickTimerId = self.Root().TimerComponent.NewRepeatedTimer(33, TimerInvokeType.BulletTick, self);
        }

        [EntitySystem]
        private static void Destroy(this BulletTickComponent self)
        {
            if (self.TickTimerId != 0)
            {
                self.Root()?.TimerComponent?.Remove(ref self.TickTimerId);
                self.TickTimerId = 0;
            }
        }

        private static void Tick(this BulletTickComponent self)
        {
            Scene scene = self.GetParent<Scene>();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            using ListComponent<BulletComponent> bullets = ListComponent<BulletComponent>.Create();

            foreach (Unit unit in unitComponent.Children.Values)
            {
                BulletComponent bulletComp = unit.GetComponent<BulletComponent>();
                if (bulletComp == null || bulletComp.IsDisposed)
                {
                    continue;
                }

                bullets.Add(bulletComp);
            }

            foreach (BulletComponent bulletComp in bullets)
            {
                if (bulletComp == null || bulletComp.IsDisposed)
                {
                    continue;
                }

                bulletComp.Update();
            }
        }
    }
}
