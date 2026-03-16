namespace ET.Server
{
    [EntitySystemOf(typeof(ECAPointNavBlockComponent))]
    [FriendOf(typeof(ECAPointNavBlockComponent))]
    public static partial class ECAPointNavBlockComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAPointNavBlockComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ECAPointNavBlockComponent self)
        {
            self.PointPolyRefs.Clear();
            self.OriginalPolyFlags.Clear();
            self.PolyBlockRefCounts.Clear();
            self.AppliedBlockedPointIds.Clear();
        }
    }
}
