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
            self.BackpackConfigId = 0;
            self.BagWidth = 0;
            self.BagHeight = 0;
            self.SecureWidth = 0;
            self.SecureHeight = 0;
            self.CarriedBagItems.Clear();
            self.CarriedSecureItems.Clear();
            self.ConsumableConfigIds.Clear();
            self.IsConfirmed = false;
            self.ConfirmedAt = 0;
        }

        [EntitySystem]
        private static void Destroy(this LoadoutComponent self)
        {
            self.CarriedBagItems.Clear();
            self.CarriedSecureItems.Clear();
            self.ConsumableConfigIds.Clear();
        }
    }
}
