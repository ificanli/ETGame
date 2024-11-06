namespace ET
{
    [EntitySystemOf(typeof(Buff))]
    public static partial class BuffSystem
    {
        [EntitySystem]
        private static void Awake(this Buff self, int configId)
        {
            self.ConfigId = configId;
        }
    }
}