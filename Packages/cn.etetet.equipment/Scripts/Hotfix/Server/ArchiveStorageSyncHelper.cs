using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// PlayerStorageComponent 与 Archive 服务之间的快照同步帮助类。
    /// </summary>
    public static class ArchiveStorageSyncHelper
    {
        public static async ETTask LoadOrCreate(Player player, PlayerStorageComponent storage)
        {
            if (player == null || storage == null)
            {
                return;
            }

            string account = player.Account;
            if (string.IsNullOrWhiteSpace(account))
            {
                return;
            }

            EntityRef<PlayerStorageComponent> storageRef = storage;
            Archive2G_GetOrCreatePlayerArchiveResponse response =
                await ArchiveMessageHelper.GetOrCreatePlayerArchive(
                    player.Root(),
                    account,
                    storage.TotalWealth,
                    storage.LastEvacuationWealth,
                    storage.WarehouseColumnCount,
                    CreateWarehouseSnapshot(storage),
                    CreateLastEvacuationSnapshot(storage));

            storage = storageRef;
            if (storage == null)
            {
                return;
            }

            if (response == null)
            {
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[ArchiveStorage] load failed: account={account}, error={response.Error}, message={response.Message}");
                return;
            }

            ApplySnapshot(storage, response);
        }

        public static async ETTask Sync(Player player, PlayerStorageComponent storage)
        {
            if (player == null || storage == null)
            {
                return;
            }

            string account = player.Account;
            if (string.IsNullOrWhiteSpace(account))
            {
                return;
            }

            Archive2G_SavePlayerArchiveResponse response =
                await ArchiveMessageHelper.SavePlayerArchive(
                    player.Root(),
                    account,
                    storage.TotalWealth,
                    storage.LastEvacuationWealth,
                    storage.WarehouseColumnCount,
                    CreateWarehouseSnapshot(storage),
                    CreateLastEvacuationSnapshot(storage));

            if (response == null)
            {
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[ArchiveStorage] sync failed: account={account}, error={response.Error}, message={response.Message}");
            }
        }

        public static async ETTask RecordBattleResult(Player player, int resultType, bool isSuccess, long totalWealth, int killNum)
        {
            if (player == null || string.IsNullOrWhiteSpace(player.Account))
            {
                return;
            }

            await ArchiveMessageHelper.RecordBattleResult(
                player.Root(),
                player.Account,
                player.Id,
                resultType,
                isSuccess,
                totalWealth,
                killNum);
        }

        private static List<global::ET.ArchiveWarehouseItemProto> CreateWarehouseSnapshot(PlayerStorageComponent storage)
        {
            List<global::ET.ArchiveWarehouseItemProto> result = new();
            if (storage?.WarehouseItems == null)
            {
                return result;
            }

            for (int i = 0; i < storage.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = storage.WarehouseItems[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                global::ET.ArchiveWarehouseItemProto data = global::ET.ArchiveWarehouseItemProto.Create();
                data.ItemUid = item.ItemUid;
                data.ConfigId = item.ConfigId;
                data.Count = item.Count;
                data.GridWidth = item.GridWidth;
                data.GridHeight = item.GridHeight;
                data.AnchorSlotIndex = item.AnchorSlotIndex;
                result.Add(data);
            }

            return result;
        }

        private static List<global::ET.ArchiveItemCountProto> CreateLastEvacuationSnapshot(PlayerStorageComponent storage)
        {
            List<global::ET.ArchiveItemCountProto> result = new();
            if (storage?.LastEvacuationItems == null)
            {
                return result;
            }

            foreach ((int configId, int count) in storage.LastEvacuationItems)
            {
                if (configId <= 0 || count <= 0)
                {
                    continue;
                }

                global::ET.ArchiveItemCountProto data = global::ET.ArchiveItemCountProto.Create();
                data.ConfigId = configId;
                data.Count = count;
                result.Add(data);
            }

            return result;
        }

        private static void ApplySnapshot(PlayerStorageComponent storage, global::ET.Archive2G_GetOrCreatePlayerArchiveResponse response)
        {
            storage.TotalWealth = response.TotalWealth;
            storage.LastEvacuationWealth = response.LastEvacuationWealth;
            storage.WarehouseColumnCount = response.WarehouseColumnCount;
            storage.InitialItemsGranted = true;

            storage.WarehouseItems.Clear();
            if (response.WarehouseItems != null)
            {
                for (int i = 0; i < response.WarehouseItems.Count; ++i)
                {
                    global::ET.ArchiveWarehouseItemProto item = response.WarehouseItems[i];
                    if (item == null || item.ConfigId <= 0 || item.Count <= 0)
                    {
                        continue;
                    }

                    storage.WarehouseItems.Add(new LoadoutWarehouseItemInfo
                    {
                        ItemUid = item.ItemUid,
                        ConfigId = item.ConfigId,
                        Count = item.Count,
                        GridWidth = item.GridWidth,
                        GridHeight = item.GridHeight,
                        AnchorSlotIndex = item.AnchorSlotIndex,
                    });
                }
            }

            storage.LastEvacuationItems.Clear();
            if (response.LastEvacuationItems != null)
            {
                for (int i = 0; i < response.LastEvacuationItems.Count; ++i)
                {
                    global::ET.ArchiveItemCountProto item = response.LastEvacuationItems[i];
                    if (item == null || item.ConfigId <= 0 || item.Count <= 0)
                    {
                        continue;
                    }

                    storage.LastEvacuationItems[item.ConfigId] = item.Count;
                }
            }
        }
    }
}
