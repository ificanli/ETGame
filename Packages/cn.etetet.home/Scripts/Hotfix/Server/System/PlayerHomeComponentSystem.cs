namespace ET.Server
{
    [EntitySystemOf(typeof(PlayerHomeComponent))]
    public static partial class PlayerHomeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerHomeComponent self)
        {
            self.UnlockedBuildingConfigIds ??= new();
        }

        [EntitySystem]
        private static void Destroy(this PlayerHomeComponent self)
        {
        }

        [EntitySystem]
        private static void Deserialize(this PlayerHomeComponent self)
        {
            self.UnlockedBuildingConfigIds ??= new();
        }
    }
}
