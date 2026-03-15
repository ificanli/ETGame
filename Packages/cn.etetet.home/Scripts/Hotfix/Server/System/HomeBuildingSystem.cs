namespace ET.Server
{
    [EntitySystemOf(typeof(HomeBuilding))]
    public static partial class HomeBuildingSystem
    {
        [EntitySystem]
        private static void Awake(this HomeBuilding self)
        {
        }

        [EntitySystem]
        private static void Destroy(this HomeBuilding self)
        {
        }
    }
}
