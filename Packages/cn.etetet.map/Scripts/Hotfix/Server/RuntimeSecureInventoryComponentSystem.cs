namespace ET.Server
{
    [EntitySystemOf(typeof(RuntimeSecureInventoryComponent))]
    public static partial class RuntimeSecureInventoryComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RuntimeSecureInventoryComponent self)
        {
            self.Width = 0;
            self.Height = 0;
            self.Items.Clear();
        }

        [EntitySystem]
        private static void Destroy(this RuntimeSecureInventoryComponent self)
        {
            self.Items.Clear();
        }
    }
}
