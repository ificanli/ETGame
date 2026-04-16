using System;

namespace ET.Server
{
    [MessageHandler(SceneType.Gate)]
    public class Map2G_HomeStorageOperateRequestHandler : MessageLocationHandler<Player, Map2G_HomeStorageOperateRequest, G2Map_HomeStorageOperateResponse>
    {
        protected override async ETTask Run(Player player, Map2G_HomeStorageOperateRequest request, G2Map_HomeStorageOperateResponse response)
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
                bool changed = false;
                switch (request.OperationType)
                {
                    case HomeStorageOperationType.Unknown:
                    case HomeStorageOperationType.SpendWealth:
                    case HomeStorageOperationType.AddWealth:
                    {
                        if (request.WealthDelta < 0)
                        {
                            long cost = -request.WealthDelta;
                            if (!storage.TrySpendWealth(cost))
                            {
                                response.Error = ErrorCode.ERR_HomeResourceNotEnough;
                                response.Message = "金币不足。";
                                FillSummary(storage, response);
                                return;
                            }

                            changed = true;
                            break;
                        }

                        if (request.WealthDelta > 0)
                        {
                            storage.TotalWealth += request.WealthDelta;
                            changed = true;
                        }

                        break;
                    }
                    case HomeStorageOperationType.ConsumeWarehouseItem:
                    {
                        if (request.ItemConfigId <= 0 || request.ItemCount <= 0)
                        {
                            response.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                            response.Message = "回收材料参数无效。";
                            FillSummary(storage, response);
                            return;
                        }

                        if (!storage.TryConsumeWarehouseItem(request.ItemConfigId, request.ItemCount))
                        {
                            response.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                            response.Message = "仓库中没有足够的可回收物品。";
                            FillSummary(storage, response);
                            return;
                        }

                        changed = true;
                        break;
                    }
                    case HomeStorageOperationType.AddWarehouseItem:
                    {
                        if (request.ItemConfigId <= 0 || request.ItemCount <= 0)
                        {
                            response.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                            response.Message = "回收入库参数无效。";
                            FillSummary(storage, response);
                            return;
                        }

                        storage.AddWarehouseItem(request.ItemConfigId, request.ItemCount);
                        changed = true;
                        break;
                    }
                    case HomeStorageOperationType.TakeWarehouseItemByUid:
                    {
                        if (request.ItemUid <= 0 || request.ItemCount <= 0)
                        {
                            response.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                            response.Message = "展示取物参数无效。";
                            FillSummary(storage, response);
                            return;
                        }

                        if (!storage.TryTakeWarehouseItem(request.ItemUid, request.ItemCount, out LoadoutWarehouseItemInfo item, out string message))
                        {
                            response.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                            response.Message = string.IsNullOrWhiteSpace(message) ? "仓库中没有找到要展示的物品。" : message;
                            FillSummary(storage, response);
                            return;
                        }

                        response.ResultItemConfigId = item.ConfigId;
                        changed = true;
                        break;
                    }
                    default:
                    {
                        response.Error = ErrorCode.ERR_HomePrerequisiteNotMet;
                        response.Message = $"不支持的仓储操作类型：{request.OperationType}";
                        FillSummary(storage, response);
                        return;
                    }
                }

                FillSummary(storage, response);
                if (changed)
                {
                    ArchiveStorageSyncHelper.Sync(player, storage).Coroutine();
                }
            }
        }

        private static void FillSummary(PlayerStorageComponent storage, G2Map_HomeStorageOperateResponse response)
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
