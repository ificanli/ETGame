namespace ET.Server
{
    [EntitySystemOf(typeof(HomeProductionComponent))]
    public static partial class HomeProductionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HomeProductionComponent self)
        {
            self.ProductionOrders ??= new();
        }

        [EntitySystem]
        private static void Destroy(this HomeProductionComponent self)
        {
            self.ProductionOrders.Clear();
        }

        [EntitySystem]
        private static void Deserialize(this HomeProductionComponent self)
        {
            self.ProductionOrders ??= new();
        }
    }
}
