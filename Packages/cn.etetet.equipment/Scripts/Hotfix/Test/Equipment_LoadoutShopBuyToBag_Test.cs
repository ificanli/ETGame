using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证起装商店物品可以直接购买到背包格子。
    /// </summary>
    public class Equipment_LoadoutShopBuyToBag_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_LoadoutShopBuyToBag_Test));
            Scene scene = scope.TestFiber.Root;

            PlayerComponent playerComponent = scene.GetComponent<PlayerComponent>() ?? scene.AddComponent<PlayerComponent>();
            Player player = playerComponent.AddChildWithId<Player, string>(IdGenerater.Instance.GenerateId(), nameof(Equipment_LoadoutShopBuyToBag_Test));
            playerComponent.Add(player);

            LoadoutComponent loadout = player.AddComponent<LoadoutComponent>();
            PlayerStorageComponent storage = player.AddComponent<PlayerStorageComponent>();
            LoadoutStateHelper.EnsureDefaultSecureSize(loadout);
            storage.EnsureWarehouseLayout(8);

            ItemConfig backpackConfig = FindBackpack();
            if (backpackConfig == null)
            {
                Log.Console("backpack config not found");
                return 1;
            }

            loadout.BackpackConfigId = backpackConfig.Id;
            loadout.BagWidth = backpackConfig.BackpackWidth;
            loadout.BagHeight = backpackConfig.BackpackHeight;

            ItemConfig shopItem = FindShopBagItem(backpackConfig.BackpackWidth, backpackConfig.BackpackHeight);
            if (shopItem == null)
            {
                Log.Console("shop bag item config not found");
                return 2;
            }

            storage.TotalWealth = shopItem.LoadoutBuyPrice + 500;
            long expectedWealth = storage.TotalWealth - shopItem.LoadoutBuyPrice;

            C2G_LoadoutBuyFromShop request = C2G_LoadoutBuyFromShop.Create();
            request.ConfigId = shopItem.Id;
            request.Count = 1;
            request.TargetAreaType = (int)LoadoutAreaType.Bag;
            request.TargetSlotType = 0;
            request.TargetAnchorSlotIndex = 0;

            int error = LoadoutOperationHelper.BuyFromShop(loadout, storage, request, out string message);
            if (error != ErrorCode.ERR_Success)
            {
                Log.Console($"buy from shop to bag failed: error={error}, message={message}");
                return 3;
            }

            if (loadout.CarriedBagItems.Count != 1)
            {
                Log.Console($"bag item count mismatch, expected=1, actual={loadout.CarriedBagItems.Count}");
                return 4;
            }

            LoadoutGridItemInfo item = loadout.CarriedBagItems[0];
            if (item.ConfigId != shopItem.Id || item.AnchorSlotIndex != 0)
            {
                Log.Console($"bag item mismatch, expectedConfig={shopItem.Id}, actualConfig={item.ConfigId}, actualAnchor={item.AnchorSlotIndex}");
                return 5;
            }

            if (storage.TotalWealth != expectedWealth)
            {
                Log.Console($"wealth mismatch, expected={expectedWealth}, actual={storage.TotalWealth}");
                return 6;
            }

            return ErrorCode.ERR_Success;
        }

        private static ItemConfig FindBackpack()
        {
            foreach (ItemConfig itemConfig in ItemConfigCategory.Instance.DataList)
            {
                if (itemConfig == null || itemConfig.Id <= 0)
                {
                    continue;
                }

                if (itemConfig.BackpackWidth <= 0 || itemConfig.BackpackHeight <= 0)
                {
                    continue;
                }

                return itemConfig;
            }

            return null;
        }

        private static ItemConfig FindShopBagItem(int bagWidth, int bagHeight)
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

                if (itemConfig.BackpackWidth > 0 || itemConfig.BackpackHeight > 0)
                {
                    continue;
                }

                int gridWidth = itemConfig.GridWidth > 0 ? itemConfig.GridWidth : 1;
                int gridHeight = itemConfig.GridHeight > 0 ? itemConfig.GridHeight : 1;
                if (gridWidth > bagWidth || gridHeight > bagHeight)
                {
                    continue;
                }

                return itemConfig;
            }

            return null;
        }
    }
}
