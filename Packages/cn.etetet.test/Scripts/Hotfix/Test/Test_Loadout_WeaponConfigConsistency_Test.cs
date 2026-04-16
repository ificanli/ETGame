using System.Collections.Generic;

namespace ET.Test
{
    public class Test_Loadout_WeaponConfigConsistency_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Loadout_WeaponConfigConsistency_Test));

            WeaponConfigCategory weaponCategory = WeaponConfigCategory.Instance;
            ItemConfigCategory itemCategory = ItemConfigCategory.Instance;
            if (weaponCategory == null)
            {
                Log.Console("weapon config category is null");
                return 1;
            }

            if (itemCategory == null)
            {
                Log.Console("item config category is null");
                return 2;
            }

            HashSet<int> weaponIds = new();
            int weaponCount = 0;
            foreach (WeaponConfig weaponConfig in weaponCategory.DataList)
            {
                if (weaponConfig == null || weaponConfig.Id <= 0)
                {
                    continue;
                }

                ++weaponCount;
                if (!weaponIds.Add(weaponConfig.Id))
                {
                    Log.Console($"duplicate weapon config id: {weaponConfig.Id}");
                    return 3;
                }

                ItemConfig itemConfig = itemCategory.GetOrDefault(weaponConfig.Id);
                if (itemConfig == null)
                {
                    Log.Console($"weapon item config missing: {weaponConfig.Id}");
                    return 4;
                }

                if (!itemConfig.LoadoutShopVisible)
                {
                    Log.Console($"weapon item not visible in loadout shop: {weaponConfig.Id}");
                    return 5;
                }

                if (!itemConfig.CanEquipMainWeapon && !itemConfig.CanEquipSubWeapon)
                {
                    Log.Console($"weapon item not marked as weapon slot item: {weaponConfig.Id}");
                    return 6;
                }
            }

            if (weaponCount <= 0)
            {
                Log.Console("weapon config list is empty");
                return 7;
            }

            foreach (ItemConfig itemConfig in itemCategory.DataList)
            {
                if (itemConfig == null || itemConfig.Id <= 0 || !itemConfig.LoadoutShopVisible)
                {
                    continue;
                }

                if (!itemConfig.CanEquipMainWeapon && !itemConfig.CanEquipSubWeapon)
                {
                    continue;
                }

                if (weaponCategory.GetOrDefault(itemConfig.Id) == null)
                {
                    Log.Console($"loadout shop weapon missing weapon config: {itemConfig.Id}");
                    return 8;
                }
            }

            return ErrorCode.ERR_Success;
        }
    }
}
