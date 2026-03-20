using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证起装商店物品可以直接购买到固定槽位。
    /// </summary>
    public class Equipment_LoadoutShopBuyToFixedSlot_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_LoadoutShopBuyToFixedSlot_Test));
            Scene scene = scope.TestFiber.Root;

            PlayerComponent playerComponent = scene.GetComponent<PlayerComponent>() ?? scene.AddComponent<PlayerComponent>();
            Player player = playerComponent.AddChildWithId<Player, string>(IdGenerater.Instance.GenerateId(), nameof(Equipment_LoadoutShopBuyToFixedSlot_Test));
            playerComponent.Add(player);

            LoadoutComponent loadout = player.AddComponent<LoadoutComponent>();
            PlayerStorageComponent storage = player.AddComponent<PlayerStorageComponent>();
            LoadoutStateHelper.EnsureDefaultSecureSize(loadout);
            storage.EnsureWarehouseLayout(8);

            ItemConfig shopWeapon = FindShopWeapon();
            if (shopWeapon == null)
            {
                Log.Console("shop weapon config not found");
                return 1;
            }

            storage.TotalWealth = shopWeapon.LoadoutBuyPrice + 500;
            long expectedWealth = storage.TotalWealth - shopWeapon.LoadoutBuyPrice;
            int initialWarehouseCount = storage.GetWarehouseCount(shopWeapon.Id);

            C2G_LoadoutBuyFromShop request = C2G_LoadoutBuyFromShop.Create();
            request.ConfigId = shopWeapon.Id;
            request.Count = 1;
            request.TargetAreaType = (int)LoadoutAreaType.FixedSlot;
            request.TargetSlotType = (int)LoadoutFixedSlotType.MainWeapon;

            int error = LoadoutOperationHelper.BuyFromShop(loadout, storage, request, out string message);
            if (error != ErrorCode.ERR_Success)
            {
                Log.Console($"buy from shop to fixed slot failed: error={error}, message={message}");
                return 2;
            }

            if (loadout.MainWeaponConfigId != shopWeapon.Id)
            {
                Log.Console($"main weapon mismatch, expected={shopWeapon.Id}, actual={loadout.MainWeaponConfigId}");
                return 3;
            }

            if (storage.TotalWealth != expectedWealth)
            {
                Log.Console($"wealth mismatch, expected={expectedWealth}, actual={storage.TotalWealth}");
                return 4;
            }

            if (storage.GetWarehouseCount(shopWeapon.Id) != initialWarehouseCount)
            {
                Log.Console($"shop bought weapon should not change warehouse count, configId={shopWeapon.Id}, before={initialWarehouseCount}, after={storage.GetWarehouseCount(shopWeapon.Id)}");
                return 5;
            }

            return ErrorCode.ERR_Success;
        }

        private static ItemConfig FindShopWeapon()
        {
            foreach (ItemConfig itemConfig in ItemConfigCategory.Instance.DataList)
            {
                if (itemConfig == null || itemConfig.Id <= 0)
                {
                    continue;
                }

                if (!itemConfig.LoadoutShopVisible || itemConfig.LoadoutBuyPrice <= 0)
                {
                    continue;
                }

                if (!itemConfig.CanEquipMainWeapon && !itemConfig.CanEquipSubWeapon)
                {
                    continue;
                }

                return itemConfig;
            }

            return null;
        }
    }
}
