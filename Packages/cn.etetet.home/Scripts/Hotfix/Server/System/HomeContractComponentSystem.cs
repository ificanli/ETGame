namespace ET.Server
{
    [EntitySystemOf(typeof(HomeContractComponent))]
    public static partial class HomeContractComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HomeContractComponent self)
        {
            self.AvailableContracts ??= new();
            self.ActiveContracts ??= new();
        }

        [EntitySystem]
        private static void Destroy(this HomeContractComponent self)
        {
            self.AvailableContracts.Clear();
            self.ActiveContracts.Clear();
        }

        [EntitySystem]
        private static void Deserialize(this HomeContractComponent self)
        {
            self.AvailableContracts ??= new();
            self.ActiveContracts ??= new();
        }
    }
}
