namespace ET.Server
{
    [EntitySystemOf(typeof(DetectorComponent))]
    public static partial class DetectorComponentSystem
    {
        [EntitySystem]
        private static void Awake(this DetectorComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this DetectorComponent self)
        {
        }
    }
}
