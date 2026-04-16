using System;

namespace ET.Server
{
    [MessageHandler(SceneType.Gate)]
    public class Map2G_HomeStorageSummaryRequestHandler : MessageLocationHandler<Player, Map2G_HomeStorageSummaryRequest, G2Map_HomeStorageSummaryResponse>
    {
        protected override async ETTask Run(Player player, Map2G_HomeStorageSummaryRequest request, G2Map_HomeStorageSummaryResponse response)
        {
            EntityRef<Player> playerRef = player;
            using (await player.Root().CoroutineLockComponent.Wait(CoroutineLockType.Loadout, player.Id))
            {
                player = playerRef;
                if (player == null)
                {
                    return;
                }

                PlayerStorageComponent storage = player.GetComponent<PlayerStorageComponent>() ?? player.AddComponent<PlayerStorageComponent>();
                FillSummary(storage, response);
            }
        }

        private static void FillSummary(PlayerStorageComponent storage, G2Map_HomeStorageSummaryResponse response)
        {
            if (storage == null || response == null)
            {
                return;
            }

            response.TotalWealth = storage.TotalWealth;
            response.WarehouseItemCount = storage.WarehouseItems.Count;
            response.WarehouseOccupiedCellCount = GetWarehouseOccupiedCellCount(storage);
        }

        private static int GetWarehouseOccupiedCellCount(PlayerStorageComponent storage)
        {
            int occupiedCellCount = 0;
            if (storage == null)
            {
                return occupiedCellCount;
            }

            for (int i = 0; i < storage.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = storage.WarehouseItems[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                occupiedCellCount += Math.Max(1, item.GridWidth) * Math.Max(1, item.GridHeight);
            }

            return occupiedCellCount;
        }
    }
}
