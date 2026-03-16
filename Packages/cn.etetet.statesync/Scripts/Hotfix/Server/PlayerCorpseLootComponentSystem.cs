namespace ET.Server
{
    [EntitySystemOf(typeof(PlayerCorpseLootComponent))]
    public static partial class PlayerCorpseLootComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerCorpseLootComponent self)
        {
            self.PointId = string.Empty;
            self.CreatedTime = 0;
        }
    }
}
