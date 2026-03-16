namespace ET.Server
{
    [EntitySystemOf(typeof(WardComponent))]
    public static partial class WardComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WardComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this WardComponent self)
        {
        }
    }
}
