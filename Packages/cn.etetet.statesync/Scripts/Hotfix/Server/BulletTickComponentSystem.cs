namespace ET.Server
{
    [EntitySystemOf(typeof(BulletTickComponent))]
    public static partial class BulletTickComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BulletTickComponent self)
        {
            Scene scene = self.GetParent<Scene>();
            HighFrequencySchedulerComponent scheduler = scene?.GetComponent<HighFrequencySchedulerComponent>();
            scheduler?.RegisterChannel(HighFrequencyChannelConfigFactory.CreateBullet33ms());
        }

        [EntitySystem]
        private static void Destroy(this BulletTickComponent self)
        {
        }
    }

    [Invoke(HighFrequencyInvokeType.Bullet33msTick)]
    public class BulletTickInvoker : AInvokeHandler<HighFrequencyTickCallback>
    {
        public override void Handle(HighFrequencyTickCallback args)
        {
            Entity entity = args.Entity;
            BulletComponent bulletComponent = entity as BulletComponent;
            if (bulletComponent == null || bulletComponent.IsDisposed)
            {
                return;
            }

            bulletComponent.TickFixedStep(args.DeltaTimeMs);
        }
    }
}
