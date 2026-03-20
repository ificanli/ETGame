using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证起装商店购买在财富不足时会被拒绝。
    /// </summary>
    public class Equipment_LoadoutShopBuyWealthNotEnough_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_LoadoutShopBuyWealthNotEnough_Test));
            Scene scene = scope.TestFiber.Root;

            PlayerComponent playerComponent = scene.GetComponent<PlayerComponent>() ?? scene.AddComponent<PlayerComponent>();
            Player player = playerComponent.AddChildWithId<Player, string>(IdGenerater.Instance.GenerateId(), nameof(Equipment_LoadoutShopBuyWealthNotEnough_Test));
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

            storage.TotalWealth = shopWeapon.LoadoutBuyPrice - 1;

            C2G_LoadoutBuyFromShop request = C2G_LoadoutBuyFromShop.Create();
            request.ConfigId = shopWeapon.Id;
            request.Count = 1;
            request.TargetAreaType = (int)LoadoutAreaType.FixedSlot;
            request.TargetSlotType = (int)LoadoutFixedSlotType.MainWeapon;

            int error = LoadoutOperationHelper.BuyFromShop(loadout, storage, request, out string message);
            if (error != ErrorCode.ERR_LoadoutWealthNotEnough)
            {
                Log.Console($"expected wealth not enough, actualError={error}, message={message}");
                return 2;
            }

            if (loadout.MainWeaponConfigId != 0)
            {
                Log.Console($"main weapon should remain empty, actual={loadout.MainWeaponConfigId}");
                return 3;
            }

            if (storage.TotalWealth != shopWeapon.LoadoutBuyPrice - 1)
            {
                Log.Console($"wealth should remain unchanged, actual={storage.TotalWealth}");
                return 4;
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

                if (!itemConfig.LoadoutShopVisible || itemConfig.LoadoutBuyPrice <= 1)
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
