namespace ET.Server
{
    [EntitySystemOf(typeof(LoadoutComponent))]
    public static partial class LoadoutComponentSystem
    {
        [EntitySystem]
        private static void Awake(this LoadoutComponent self)
        {
            self.HeroConfigId = 0;
            self.MainWeaponConfigId = 0;
            self.SubWeaponConfigId = 0;
            self.ArmorConfigId = 0;
            self.ConsumableConfigIds.Clear();
            self.IsConfirmed = false;
        }

        [EntitySystem]
        private static void Destroy(this LoadoutComponent self)
        {
            self.ConsumableConfigIds.Clear();
        }
    }
}
