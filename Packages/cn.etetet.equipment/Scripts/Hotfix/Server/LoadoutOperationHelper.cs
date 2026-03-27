using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 局外起装正式状态的即时操作辅助方法。
    /// </summary>
    public static class LoadoutOperationHelper
    {
        public static int TakeFromWarehouse(
            LoadoutComponent loadout,
            PlayerStorageComponent storage,
            C2G_LoadoutTakeFromWarehouse request,
            out string message)
        {
            message = string.Empty;

            if (request.Count <= 0)
            {
                message = "count invalid";
                return ErrorCode.ERR_LoadoutCountInvalid;
            }

            if (!TryResolveWarehouseRequestItem(storage, request, out LoadoutWarehouseItemInfo warehouseItem, out message))
            {
                return ErrorCode.ERR_LoadoutItemNotFound;
            }

            if ((LoadoutAreaType)request.TargetAreaType == LoadoutAreaType.FixedSlot)
            {
                if (request.Count != 1)
                {
                    message = "fixed slot only accepts count 1";
                    return ErrorCode.ERR_LoadoutCountInvalid;
                }

                return TakeFromWarehouseToFixedSlot(
                    loadout,
                    storage,
                    warehouseItem.ConfigId,
                    (LoadoutFixedSlotType)request.TargetSlotType,
                    request.ItemUid,
                    out message);
            }

            int error = AddOwnedItem(
                loadout,
                request.TargetAreaType,
                request.TargetSlotType,
                request.TargetAnchorSlotIndex,
                request.TargetBagWidth,
                request.TargetBagHeight,
                warehouseItem.ConfigId,
                request.Count,
                out message);
            if (error != ErrorCode.ERR_Success)
            {
                return error;
            }

            bool consumed = request.ItemUid > 0
                ? storage.TryTakeWarehouseItem(request.ItemUid, request.Count, out _, out message)
                : storage.TryConsumeWarehouseItem(warehouseItem.ConfigId, request.Count);
            if (!consumed)
            {
                if (string.IsNullOrEmpty(message))
                {
                    message = "warehouse item not enough";
                }

                return ErrorCode.ERR_LoadoutWarehouseNotEnough;
            }

            InvalidateConfirmedState(loadout);
            return ErrorCode.ERR_Success;
        }

        public static int BuyFromShop(
            LoadoutComponent loadout,
            PlayerStorageComponent storage,
            C2G_LoadoutBuyFromShop request,
            out string message)
        {
            message = string.Empty;

            if (request.Count <= 0)
            {
                message = "count invalid";
                return ErrorCode.ERR_LoadoutCountInvalid;
            }

            if (!TryResolveShopPurchaseItem(request.ConfigId, request.Count, out ItemConfig shopItem, out long totalCost, out message))
            {
                return ErrorCode.ERR_LoadoutShopItemUnavailable;
            }

            if (!storage.CanAfford(totalCost))
            {
                message = "wealth not enough";
                return ErrorCode.ERR_LoadoutWealthNotEnough;
            }

            object[] snapshot = CaptureSnapshot(loadout);
            int error = AddOwnedItem(
                loadout,
                request.TargetAreaType,
                request.TargetSlotType,
                request.TargetAnchorSlotIndex,
                request.TargetBagWidth,
                request.TargetBagHeight,
                shopItem.Id,
                request.Count,
                out message);
            if (error != ErrorCode.ERR_Success)
            {
                RestoreSnapshot(loadout, snapshot);
                return error;
            }

            if (!storage.TrySpendWealth(totalCost))
            {
                RestoreSnapshot(loadout, snapshot);
                message = "wealth not enough";
                return ErrorCode.ERR_LoadoutWealthNotEnough;
            }

            InvalidateConfirmedState(loadout);
            return ErrorCode.ERR_Success;
        }

        public static int PutToWarehouse(
            LoadoutComponent loadout,
            PlayerStorageComponent storage,
            C2G_LoadoutPutToWarehouse request,
            out string message)
        {
            message = string.Empty;
            if (!storage.EnsureWarehouseLayout(request.WarehouseColumnCount))
            {
                message = "warehouse column count invalid";
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            LoadoutAreaType sourceArea = (LoadoutAreaType)request.SourceAreaType;

            if (sourceArea == LoadoutAreaType.FixedSlot)
            {
                LoadoutFixedSlotType slotType = (LoadoutFixedSlotType)request.SourceSlotType;
                int configId = GetFixedSlotConfigId(loadout, slotType);
                if (configId <= 0)
                {
                    message = "source slot empty";
                    return ErrorCode.ERR_LoadoutSourceEmpty;
                }

                if (slotType == LoadoutFixedSlotType.Backpack && loadout.CarriedBagItems.Count > 0)
                {
                    message = "cannot unload backpack while bag contains items";
                    return ErrorCode.ERR_LoadoutBagNotEmpty;
                }

                storage.AddWarehouseItem(configId, 1);
                ClearFixedSlot(loadout, slotType);
                InvalidateConfirmedState(loadout);
                return ErrorCode.ERR_Success;
            }

            if (!TryTakeOwnedGridItem(loadout, sourceArea, request.SourceAnchorSlotIndex, out int sourceIndex, out LoadoutGridItemInfo sourceItem, out message))
            {
                return ErrorCode.ERR_LoadoutSourceEmpty;
            }

            if (request.Count <= 0 || request.Count > sourceItem.Count)
            {
                message = "count invalid";
                return ErrorCode.ERR_LoadoutCountInvalid;
            }

            storage.AddWarehouseItem(sourceItem.ConfigId, request.Count);

            List<LoadoutGridItemInfo> container = GetGridContainer(loadout, sourceArea);
            if (request.Count == sourceItem.Count)
            {
                container.RemoveAt(sourceIndex);
            }
            else
            {
                LoadoutGridItemInfo remain = sourceItem;
                remain.Count -= request.Count;
                container[sourceIndex] = remain;
            }

            InvalidateConfirmedState(loadout);
            return ErrorCode.ERR_Success;
        }

        public static int MoveOwnedItem(
            LoadoutComponent loadout,
            C2G_LoadoutMoveOwnedItem request,
            out string message)
        {
            message = string.Empty;
            LoadoutAreaType sourceArea = (LoadoutAreaType)request.SourceAreaType;
            LoadoutAreaType targetArea = (LoadoutAreaType)request.TargetAreaType;

            if (sourceArea == LoadoutAreaType.None || targetArea == LoadoutAreaType.None)
            {
                message = "area invalid";
                return ErrorCode.ERR_LoadoutAreaInvalid;
            }

            if (sourceArea == LoadoutAreaType.FixedSlot)
            {
                return MoveFromFixedSlot(
                    loadout,
                    (LoadoutFixedSlotType)request.SourceSlotType,
                    targetArea,
                    (LoadoutFixedSlotType)request.TargetSlotType,
                    request.TargetAnchorSlotIndex,
                    request.TargetBagWidth,
                    request.TargetBagHeight,
                    out message);
            }

            if (!TryTakeOwnedGridItem(loadout, sourceArea, request.SourceAnchorSlotIndex, out int sourceIndex, out LoadoutGridItemInfo sourceItem, out message))
            {
                return ErrorCode.ERR_LoadoutSourceEmpty;
            }

            return MoveFromGridArea(
                loadout,
                sourceArea,
                sourceIndex,
                sourceItem,
                targetArea,
                (LoadoutFixedSlotType)request.TargetSlotType,
                request.TargetAnchorSlotIndex,
                request.TargetBagWidth,
                request.TargetBagHeight,
                out message);
        }

        public static int MoveWarehouseItem(PlayerStorageComponent storage, C2G_LoadoutMoveWarehouseItem request, out string message)
        {
            message = string.Empty;
            if (storage == null)
            {
                message = "storage missing";
                return ErrorCode.ERR_LoadoutStateConflict;
            }

            if (!storage.EnsureWarehouseLayout(request.WarehouseColumnCount))
            {
                message = "warehouse column count invalid";
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            if (request.ItemUid <= 0)
            {
                message = "item uid invalid";
                return ErrorCode.ERR_LoadoutItemNotFound;
            }

            if (request.TargetAnchorSlotIndex < 0)
            {
                message = "anchor slot invalid";
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            if (!storage.TryGetWarehouseItem(request.ItemUid, out int sourceIndex, out LoadoutWarehouseItemInfo sourceItem))
            {
                message = "warehouse item not found";
                return ErrorCode.ERR_LoadoutItemNotFound;
            }

            if (sourceItem.AnchorSlotIndex == request.TargetAnchorSlotIndex)
            {
                return ErrorCode.ERR_Success;
            }

            LoadoutWarehouseItemInfo moved = sourceItem;
            moved.AnchorSlotIndex = request.TargetAnchorSlotIndex;
            List<int> blockers = GetBlockingWarehouseIndices(storage, storage.WarehouseItems, moved, sourceIndex);
            if (blockers == null)
            {
                message = "target placement invalid";
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            if (blockers.Count == 0)
            {
                storage.WarehouseItems[sourceIndex] = moved;
                return ErrorCode.ERR_Success;
            }

            if (blockers.Count != 1)
            {
                message = "target placement invalid";
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            int blockerIndex = blockers[0];
            LoadoutWarehouseItemInfo blocker = storage.WarehouseItems[blockerIndex];
            LoadoutWarehouseItemInfo swapped = blocker;
            swapped.AnchorSlotIndex = sourceItem.AnchorSlotIndex;

            if (!CanPlaceWarehouseItem(storage, storage.WarehouseItems, moved, sourceIndex, blockerIndex) ||
                !CanPlaceWarehouseItem(storage, storage.WarehouseItems, swapped, blockerIndex, sourceIndex))
            {
                message = "target placement invalid";
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            storage.WarehouseItems[sourceIndex] = moved;
            storage.WarehouseItems[blockerIndex] = swapped;
            return ErrorCode.ERR_Success;
        }

        public static void OneKeyUnload(LoadoutComponent loadout, PlayerStorageComponent storage)
        {
            AddFixedSlotToWarehouse(storage, loadout.MainWeaponConfigId);
            AddFixedSlotToWarehouse(storage, loadout.SubWeaponConfigId);
            AddFixedSlotToWarehouse(storage, loadout.ArmorConfigId);
            AddFixedSlotToWarehouse(storage, loadout.BackpackConfigId);

            for (int i = 0; i < loadout.CarriedBagItems.Count; ++i)
            {
                LoadoutGridItemInfo item = loadout.CarriedBagItems[i];
                storage.AddWarehouseItem(item.ConfigId, item.Count);
            }

            for (int i = 0; i < loadout.CarriedSecureItems.Count; ++i)
            {
                LoadoutGridItemInfo item = loadout.CarriedSecureItems[i];
                storage.AddWarehouseItem(item.ConfigId, item.Count);
            }

            loadout.MainWeaponConfigId = 0;
            loadout.SubWeaponConfigId = 0;
            loadout.ArmorConfigId = 0;
            loadout.BackpackConfigId = 0;
            loadout.BagWidth = 0;
            loadout.BagHeight = 0;
            loadout.CarriedBagItems.Clear();
            loadout.CarriedSecureItems.Clear();
            loadout.ConsumableConfigIds.Clear();

            InvalidateConfirmedState(loadout);
        }

        public static void PushStateChanged(Player player, LoadoutComponent loadout, PlayerStorageComponent storage)
        {
            if (player == null || loadout == null)
            {
                return;
            }

            Session session = player.GetComponent<PlayerSessionComponent>()?.Session;
            if (session == null)
            {
                return;
            }

            G2C_LoadoutStateChanged message = G2C_LoadoutStateChanged.Create();
            LoadoutStateHelper.FillLoadoutStateChangedResponse(loadout, storage, message);
            session.Send(message);

            if (storage != null)
            {
                ArchiveStorageSyncHelper.Sync(player, storage).Coroutine();
            }
        }

        private static int MoveFromFixedSlot(
            LoadoutComponent loadout,
            LoadoutFixedSlotType sourceSlotType,
            LoadoutAreaType targetArea,
            LoadoutFixedSlotType targetSlotType,
            int targetAnchorSlotIndex,
            int targetBagWidth,
            int targetBagHeight,
            out string message)
        {
            message = string.Empty;

            int configId = GetFixedSlotConfigId(loadout, sourceSlotType);
            if (configId <= 0)
            {
                message = "source slot empty";
                return ErrorCode.ERR_LoadoutSourceEmpty;
            }

            if (targetArea == LoadoutAreaType.FixedSlot)
            {
                if (sourceSlotType == targetSlotType)
                {
                    message = "move to same slot";
                    return ErrorCode.ERR_LoadoutStateConflict;
                }

                return MoveFixedSlotToFixedSlot(loadout, sourceSlotType, targetSlotType, out message);
            }

            if (!TryBuildGridItem(configId, 1, targetAnchorSlotIndex, out LoadoutGridItemInfo gridItem, out message))
            {
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            return MoveFixedSlotToGridArea(loadout, sourceSlotType, targetArea, gridItem, out message);
        }

        private static int MoveFromGridArea(
            LoadoutComponent loadout,
            LoadoutAreaType sourceArea,
            int sourceIndex,
            LoadoutGridItemInfo sourceItem,
            LoadoutAreaType targetArea,
            LoadoutFixedSlotType targetSlotType,
            int targetAnchorSlotIndex,
            int targetBagWidth,
            int targetBagHeight,
            out string message)
        {
            message = string.Empty;

            if (targetArea == LoadoutAreaType.FixedSlot)
            {
                if (sourceItem.Count != 1)
                {
                    message = "stack item cannot move into fixed slot";
                    return ErrorCode.ERR_LoadoutCountInvalid;
                }

                return MoveGridAreaToFixedSlot(
                    loadout,
                    sourceArea,
                    sourceIndex,
                    sourceItem,
                    targetSlotType,
                    out message);
            }

            if (sourceArea == targetArea && sourceItem.AnchorSlotIndex == targetAnchorSlotIndex)
            {
                message = "move to same slot";
                return ErrorCode.ERR_LoadoutStateConflict;
            }

            List<LoadoutGridItemInfo> sourceContainer = GetGridContainer(loadout, sourceArea);
            List<LoadoutGridItemInfo> targetContainer = GetGridContainer(loadout, targetArea);
            if (targetContainer == null)
            {
                message = "target area invalid";
                return ErrorCode.ERR_LoadoutAreaInvalid;
            }

            LoadoutGridItemInfo moved = sourceItem;
            moved.AnchorSlotIndex = targetAnchorSlotIndex;

            int targetWidth = GetGridContainerWidth(loadout, targetArea);
            int targetHeight = GetGridContainerHeight(loadout, targetArea);

            bool canPlace = sourceArea == targetArea
                ? CanPlaceGridItem(targetContainer, targetWidth, targetHeight, moved, sourceIndex)
                : CanPlaceGridItem(targetContainer, targetWidth, targetHeight, moved, -1);

            if (!canPlace)
            {
                message = "target placement invalid";
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            if (sourceArea == targetArea)
            {
                targetContainer[sourceIndex] = moved;
            }
            else
            {
                sourceContainer.RemoveAt(sourceIndex);
                targetContainer.Add(moved);
            }

            InvalidateConfirmedState(loadout);
            return ErrorCode.ERR_Success;
        }

        private static int AddOwnedItem(
            LoadoutComponent loadout,
            int targetAreaType,
            int targetSlotType,
            int targetAnchorSlotIndex,
            int targetBagWidth,
            int targetBagHeight,
            int configId,
            int count,
            out string message)
        {
            message = string.Empty;

            if ((LoadoutAreaType)targetAreaType == LoadoutAreaType.FixedSlot)
            {
                if (count != 1)
                {
                    message = "fixed slot only accepts count 1";
                    return ErrorCode.ERR_LoadoutCountInvalid;
                }

                return PlaceIntoFixedSlot(loadout, (LoadoutFixedSlotType)targetSlotType, configId, targetBagWidth, targetBagHeight, out message);
            }

            if (!TryBuildGridItem(configId, count, targetAnchorSlotIndex, out LoadoutGridItemInfo gridItem, out message))
            {
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            return PlaceIntoGridArea(loadout, (LoadoutAreaType)targetAreaType, gridItem, out message);
        }

        private static int TakeFromWarehouseToFixedSlot(
            LoadoutComponent loadout,
            PlayerStorageComponent storage,
            int configId,
            LoadoutFixedSlotType targetSlotType,
            long itemUid,
            out string message)
        {
            message = string.Empty;
            object[] snapshot = CaptureSnapshot(loadout);
            int replacedConfigId = GetFixedSlotConfigId(loadout, targetSlotType);

            int applyError = ApplyFixedSlotConfig(loadout, targetSlotType, configId, clearBagItemsWhenRemovingBackpack: false, out message);
            if (applyError != ErrorCode.ERR_Success)
            {
                RestoreSnapshot(loadout, snapshot);
                return applyError;
            }

            if (!ValidateCurrentState(loadout, out message))
            {
                RestoreSnapshot(loadout, snapshot);
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            bool consumed = itemUid > 0
                ? storage.TryTakeWarehouseItem(itemUid, 1, out _, out message)
                : storage.TryConsumeWarehouseItem(configId, 1);
            if (!consumed)
            {
                RestoreSnapshot(loadout, snapshot);
                if (string.IsNullOrEmpty(message))
                {
                    message = "warehouse item not enough";
                }

                return ErrorCode.ERR_LoadoutWarehouseNotEnough;
            }

            if (replacedConfigId > 0)
            {
                storage.AddWarehouseItem(replacedConfigId, 1);
            }

            InvalidateConfirmedState(loadout);
            return ErrorCode.ERR_Success;
        }

        private static int MoveFixedSlotToFixedSlot(
            LoadoutComponent loadout,
            LoadoutFixedSlotType sourceSlotType,
            LoadoutFixedSlotType targetSlotType,
            out string message)
        {
            message = string.Empty;
            object[] snapshot = CaptureSnapshot(loadout);
            int sourceConfigId = GetFixedSlotConfigId(loadout, sourceSlotType);
            int targetConfigId = GetFixedSlotConfigId(loadout, targetSlotType);

            int applyTargetError = ApplyFixedSlotConfig(loadout, targetSlotType, sourceConfigId, clearBagItemsWhenRemovingBackpack: false, out message);
            if (applyTargetError != ErrorCode.ERR_Success)
            {
                RestoreSnapshot(loadout, snapshot);
                return applyTargetError;
            }

            int applySourceError = targetConfigId > 0
                ? ApplyFixedSlotConfig(loadout, sourceSlotType, targetConfigId, clearBagItemsWhenRemovingBackpack: false, out message)
                : ApplyFixedSlotConfig(loadout, sourceSlotType, 0, clearBagItemsWhenRemovingBackpack: true, out message);
            if (applySourceError != ErrorCode.ERR_Success)
            {
                RestoreSnapshot(loadout, snapshot);
                return applySourceError;
            }

            if (!ValidateCurrentState(loadout, out message))
            {
                RestoreSnapshot(loadout, snapshot);
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            InvalidateConfirmedState(loadout);
            return ErrorCode.ERR_Success;
        }

        private static int MoveFixedSlotToGridArea(
            LoadoutComponent loadout,
            LoadoutFixedSlotType sourceSlotType,
            LoadoutAreaType targetArea,
            LoadoutGridItemInfo gridItem,
            out string message)
        {
            message = string.Empty;
            List<LoadoutGridItemInfo> targetContainer = GetGridContainer(loadout, targetArea);
            if (targetContainer == null)
            {
                message = "target area invalid";
                return ErrorCode.ERR_LoadoutAreaInvalid;
            }

            object[] snapshot = CaptureSnapshot(loadout);
            List<int> blockers = GetBlockingGridIndices(loadout, targetArea, targetContainer, gridItem, -1);
            if (blockers == null)
            {
                RestoreSnapshot(loadout, snapshot);
                message = "target placement invalid";
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            if (blockers.Count == 0)
            {
                if (sourceSlotType == LoadoutFixedSlotType.Backpack)
                {
                    if (targetArea == LoadoutAreaType.Bag)
                    {
                        message = "cannot move equipped backpack into carried bag";
                        return ErrorCode.ERR_LoadoutStateConflict;
                    }

                    if (loadout.CarriedBagItems.Count > 0)
                    {
                        message = "cannot move backpack while bag contains items";
                        return ErrorCode.ERR_LoadoutBagNotEmpty;
                    }
                }

                int clearError = ApplyFixedSlotConfig(loadout, sourceSlotType, 0, clearBagItemsWhenRemovingBackpack: true, out message);
                if (clearError != ErrorCode.ERR_Success)
                {
                    RestoreSnapshot(loadout, snapshot);
                    return clearError;
                }

                targetContainer.Add(gridItem);
            }
            else if (blockers.Count == 1)
            {
                int blockerIndex = blockers[0];
                LoadoutGridItemInfo blocker = targetContainer[blockerIndex];
                if (blocker.Count != 1)
                {
                    RestoreSnapshot(loadout, snapshot);
                    message = "stack item cannot move into fixed slot";
                    return ErrorCode.ERR_LoadoutCountInvalid;
                }

                int applyError = ApplyFixedSlotConfig(loadout, sourceSlotType, blocker.ConfigId, clearBagItemsWhenRemovingBackpack: false, out message);
                if (applyError != ErrorCode.ERR_Success)
                {
                    RestoreSnapshot(loadout, snapshot);
                    return applyError;
                }

                int containerWidth = GetGridContainerWidth(loadout, targetArea);
                int containerHeight = GetGridContainerHeight(loadout, targetArea);
                if (!CanPlaceGridItem(targetContainer, containerWidth, containerHeight, gridItem, blockerIndex))
                {
                    RestoreSnapshot(loadout, snapshot);
                    message = "target placement invalid";
                    return ErrorCode.ERR_LoadoutGridInvalid;
                }

                targetContainer.RemoveAt(blockerIndex);
                targetContainer.Add(gridItem);
            }
            else
            {
                RestoreSnapshot(loadout, snapshot);
                message = "target placement invalid";
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            if (!ValidateCurrentState(loadout, out message))
            {
                RestoreSnapshot(loadout, snapshot);
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            InvalidateConfirmedState(loadout);
            return ErrorCode.ERR_Success;
        }

        private static int MoveGridAreaToFixedSlot(
            LoadoutComponent loadout,
            LoadoutAreaType sourceArea,
            int sourceIndex,
            LoadoutGridItemInfo sourceItem,
            LoadoutFixedSlotType targetSlotType,
            out string message)
        {
            message = string.Empty;
            List<LoadoutGridItemInfo> sourceContainer = GetGridContainer(loadout, sourceArea);
            if (sourceContainer == null)
            {
                message = "source area invalid";
                return ErrorCode.ERR_LoadoutAreaInvalid;
            }

            object[] snapshot = CaptureSnapshot(loadout);
            int targetConfigId = GetFixedSlotConfigId(loadout, targetSlotType);

            int applyError = ApplyFixedSlotConfig(loadout, targetSlotType, sourceItem.ConfigId, clearBagItemsWhenRemovingBackpack: false, out message);
            if (applyError != ErrorCode.ERR_Success)
            {
                RestoreSnapshot(loadout, snapshot);
                return applyError;
            }

            if (targetConfigId > 0)
            {
                if (!TryBuildGridItem(targetConfigId, 1, sourceItem.AnchorSlotIndex, out LoadoutGridItemInfo swappedItem, out message))
                {
                    RestoreSnapshot(loadout, snapshot);
                    return ErrorCode.ERR_LoadoutGridInvalid;
                }

                int sourceWidth = GetGridContainerWidth(loadout, sourceArea);
                int sourceHeight = GetGridContainerHeight(loadout, sourceArea);
                if (!CanPlaceGridItem(sourceContainer, sourceWidth, sourceHeight, swappedItem, sourceIndex))
                {
                    RestoreSnapshot(loadout, snapshot);
                    message = "target placement invalid";
                    return ErrorCode.ERR_LoadoutGridInvalid;
                }

                sourceContainer[sourceIndex] = swappedItem;
            }
            else
            {
                sourceContainer.RemoveAt(sourceIndex);
            }

            if (!ValidateCurrentState(loadout, out message))
            {
                RestoreSnapshot(loadout, snapshot);
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            InvalidateConfirmedState(loadout);
            return ErrorCode.ERR_Success;
        }

        private static int PlaceIntoFixedSlot(
            LoadoutComponent loadout,
            LoadoutFixedSlotType slotType,
            int configId,
            int targetBagWidth,
            int targetBagHeight,
            out string message)
        {
            message = string.Empty;

            if (slotType == LoadoutFixedSlotType.None)
            {
                message = "slot type invalid";
                return ErrorCode.ERR_LoadoutAreaInvalid;
            }

            if (GetFixedSlotConfigId(loadout, slotType) > 0)
            {
                message = "target fixed slot occupied";
                return ErrorCode.ERR_LoadoutTargetOccupied;
            }

            int validationError = LoadoutStateHelper.ValidateFixedSlotItem(configId, slotType);
            if (validationError != ErrorCode.ERR_Success)
            {
                message = $"config {configId} cannot place into fixed slot {slotType}";
                return validationError;
            }

            if (slotType == LoadoutFixedSlotType.Backpack)
            {
                if (TryResolveBackpackSize(configId, out int configuredBagWidth, out int configuredBagHeight))
                {
                    targetBagWidth = configuredBagWidth;
                    targetBagHeight = configuredBagHeight;
                }

                if (targetBagWidth <= 0 || targetBagHeight <= 0)
                {
                    message = "backpack size missing";
                    return ErrorCode.ERR_LoadoutGridInvalid;
                }

                loadout.BackpackConfigId = configId;
                loadout.BagWidth = targetBagWidth;
                loadout.BagHeight = targetBagHeight;
                return ErrorCode.ERR_Success;
            }

            SetFixedSlotConfigId(loadout, slotType, configId);
            return ErrorCode.ERR_Success;
        }

        private static int PlaceIntoGridArea(
            LoadoutComponent loadout,
            LoadoutAreaType areaType,
            LoadoutGridItemInfo item,
            out string message)
        {
            message = string.Empty;

            List<LoadoutGridItemInfo> container = GetGridContainer(loadout, areaType);
            if (container == null)
            {
                message = "target area invalid";
                return ErrorCode.ERR_LoadoutAreaInvalid;
            }

            int containerWidth = GetGridContainerWidth(loadout, areaType);
            int containerHeight = GetGridContainerHeight(loadout, areaType);
            if (!CanPlaceGridItem(container, containerWidth, containerHeight, item, -1))
            {
                message = "target placement invalid";
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

            container.Add(item);
            return ErrorCode.ERR_Success;
        }

        private static bool TryResolveWarehouseRequestItem(
            PlayerStorageComponent storage,
            C2G_LoadoutTakeFromWarehouse request,
            out LoadoutWarehouseItemInfo warehouseItem,
            out string message)
        {
            warehouseItem = default;
            message = string.Empty;

            if (request.ItemUid > 0)
            {
                if (!storage.TryGetWarehouseItem(request.ItemUid, out _, out warehouseItem))
                {
                    message = "warehouse item not found";
                    return false;
                }

                if (request.ConfigId > 0 && warehouseItem.ConfigId != request.ConfigId)
                {
                    message = "warehouse item config mismatch";
                    return false;
                }

                if (request.Count > warehouseItem.Count)
                {
                    message = "warehouse item not enough";
                    return false;
                }

                return true;
            }

            if (request.ConfigId <= 0)
            {
                message = "config id invalid";
                return false;
            }

            if (storage.GetWarehouseCount(request.ConfigId) < request.Count)
            {
                message = "warehouse item not enough";
                return false;
            }

            for (int i = 0; i < storage.WarehouseItems.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = storage.WarehouseItems[i];
                if (item.ConfigId != request.ConfigId || item.Count < request.Count)
                {
                    continue;
                }

                warehouseItem = item;
                return true;
            }

            message = "warehouse item not found";
            return false;
        }

        private static bool TryResolveShopPurchaseItem(
            int configId,
            int count,
            out ItemConfig shopItem,
            out long totalCost,
            out string message)
        {
            shopItem = null;
            totalCost = 0;
            message = string.Empty;

            if (configId <= 0)
            {
                message = "config id invalid";
                return false;
            }

            if (count <= 0)
            {
                message = "count invalid";
                return false;
            }

            shopItem = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (shopItem == null)
            {
                message = "shop item config not found";
                return false;
            }

            if (!shopItem.LoadoutShopVisible)
            {
                message = "shop item hidden";
                return false;
            }

            if (shopItem.LoadoutBuyPrice < 0)
            {
                message = "shop item price invalid";
                return false;
            }

            totalCost = (long)shopItem.LoadoutBuyPrice * count;
            return true;
        }

        private static List<int> GetBlockingWarehouseIndices(
            PlayerStorageComponent storage,
            List<LoadoutWarehouseItemInfo> container,
            LoadoutWarehouseItemInfo targetItem,
            int ignoreIndex)
        {
            int columnCount = storage?.WarehouseColumnCount ?? 0;
            if (!IsWarehouseItemInBounds(targetItem, columnCount))
            {
                return null;
            }

            List<int> blockers = new();
            for (int i = 0; i < container.Count; ++i)
            {
                if (i == ignoreIndex)
                {
                    continue;
                }

                LoadoutWarehouseItemInfo item = container[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                if (!IsWarehouseItemInBounds(item, columnCount))
                {
                    return null;
                }

                if (IsWarehouseItemOverlapping(targetItem, item, columnCount))
                {
                    blockers.Add(i);
                }
            }

            return blockers;
        }

        private static bool CanPlaceWarehouseItem(
            PlayerStorageComponent storage,
            List<LoadoutWarehouseItemInfo> container,
            LoadoutWarehouseItemInfo targetItem,
            int ignoreIndexA,
            int ignoreIndexB)
        {
            int columnCount = storage?.WarehouseColumnCount ?? 0;
            if (!IsWarehouseItemInBounds(targetItem, columnCount))
            {
                return false;
            }

            int candidateRows = targetItem.AnchorSlotIndex / columnCount + targetItem.GridHeight;
            List<GridPlacementItemInfo> validationItems = new(container.Count);
            for (int i = 0; i < container.Count; ++i)
            {
                if (i == ignoreIndexA || i == ignoreIndexB)
                {
                    continue;
                }

                LoadoutWarehouseItemInfo item = container[i];
                if (item.ConfigId <= 0 || item.Count <= 0 || !IsWarehouseItemInBounds(item, columnCount))
                {
                    return false;
                }

                candidateRows = System.Math.Max(candidateRows, item.AnchorSlotIndex / columnCount + item.GridHeight);
                validationItems.Add(new GridPlacementItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.AnchorSlotIndex,
                    GridWidth = item.GridWidth,
                    GridHeight = item.GridHeight,
                });
            }

            validationItems.Add(new GridPlacementItemInfo
            {
                ConfigId = targetItem.ConfigId,
                Count = targetItem.Count,
                AnchorSlotIndex = targetItem.AnchorSlotIndex,
                GridWidth = targetItem.GridWidth,
                GridHeight = targetItem.GridHeight,
            });
            return LoadoutGridPlacementHelper.ArePlacementsValid(validationItems, columnCount, System.Math.Max(1, candidateRows));
        }

        private static bool IsWarehouseItemInBounds(LoadoutWarehouseItemInfo item, int columnCount)
        {
            if (columnCount <= 0 || item.AnchorSlotIndex < 0)
            {
                return false;
            }

            int x = item.AnchorSlotIndex % columnCount;
            return item.GridWidth > 0 &&
                   item.GridHeight > 0 &&
                   x >= 0 &&
                   x + item.GridWidth <= columnCount;
        }

        private static bool IsWarehouseItemOverlapping(LoadoutWarehouseItemInfo a, LoadoutWarehouseItemInfo b, int columnCount)
        {
            int ax = a.AnchorSlotIndex % columnCount;
            int ay = a.AnchorSlotIndex / columnCount;
            int bx = b.AnchorSlotIndex % columnCount;
            int by = b.AnchorSlotIndex / columnCount;
            return ax < bx + b.GridWidth &&
                   ax + a.GridWidth > bx &&
                   ay < by + b.GridHeight &&
                   ay + a.GridHeight > by;
        }

        private static bool TryTakeOwnedGridItem(
            LoadoutComponent loadout,
            LoadoutAreaType areaType,
            int anchorSlotIndex,
            out int containerIndex,
            out LoadoutGridItemInfo sourceItem,
            out string message)
        {
            containerIndex = -1;
            sourceItem = default;
            message = string.Empty;

            List<LoadoutGridItemInfo> container = GetGridContainer(loadout, areaType);
            if (container == null)
            {
                message = "source area invalid";
                return false;
            }

            for (int i = 0; i < container.Count; ++i)
            {
                if (container[i].AnchorSlotIndex != anchorSlotIndex)
                {
                    continue;
                }

                containerIndex = i;
                sourceItem = container[i];
                return true;
            }

            message = "source item not found";
            return false;
        }

        private static bool TryBuildGridItem(
            int configId,
            int count,
            int anchorSlotIndex,
            out LoadoutGridItemInfo item,
            out string message)
        {
            item = default;
            message = string.Empty;

            if (anchorSlotIndex < 0)
            {
                message = "anchor slot invalid";
                return false;
            }

            if (!TryResolveItemMetrics(configId, out int maxStack, out int gridWidth, out int gridHeight))
            {
                message = $"config not found: {configId}";
                return false;
            }

            if (count <= 0 || count > maxStack)
            {
                message = $"count invalid for config {configId}";
                return false;
            }

            item.ConfigId = configId;
            item.Count = count;
            item.AnchorSlotIndex = anchorSlotIndex;
            item.GridWidth = gridWidth;
            item.GridHeight = gridHeight;
            return true;
        }

        private static bool TryResolveItemMetrics(int configId, out int maxStack, out int gridWidth, out int gridHeight)
        {
            maxStack = 1;
            gridWidth = LoadoutGridPlacementHelper.DEFAULT_GRID_WIDTH;
            gridHeight = LoadoutGridPlacementHelper.DEFAULT_GRID_HEIGHT;

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                maxStack = itemConfig.MaxStack > 0 ? itemConfig.MaxStack : 1;
                gridWidth = itemConfig.GridWidth > 0 ? itemConfig.GridWidth : LoadoutGridPlacementHelper.DEFAULT_GRID_WIDTH;
                gridHeight = itemConfig.GridHeight > 0 ? itemConfig.GridHeight : LoadoutGridPlacementHelper.DEFAULT_GRID_HEIGHT;
                return true;
            }

            EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            return equipConfig != null;
        }

        private static bool TryResolveBackpackSize(int configId, out int bagWidth, out int bagHeight)
        {
            bagWidth = 0;
            bagHeight = 0;

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig == null)
            {
                return false;
            }

            if (itemConfig.BackpackWidth <= 0 || itemConfig.BackpackHeight <= 0)
            {
                return false;
            }

            bagWidth = itemConfig.BackpackWidth;
            bagHeight = itemConfig.BackpackHeight;
            return true;
        }

        private static bool CanPlaceGridItem(
            List<LoadoutGridItemInfo> container,
            int containerWidth,
            int containerHeight,
            LoadoutGridItemInfo targetItem,
            int ignoreIndex)
        {
            if (container == null || containerWidth <= 0 || containerHeight <= 0)
            {
                return false;
            }

            List<GridPlacementItemInfo> validationItems = new(container.Count + 1);
            for (int i = 0; i < container.Count; ++i)
            {
                if (i == ignoreIndex)
                {
                    continue;
                }

                LoadoutGridItemInfo item = container[i];
                validationItems.Add(new GridPlacementItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.AnchorSlotIndex,
                    GridWidth = item.GridWidth,
                    GridHeight = item.GridHeight,
                });
            }

            validationItems.Add(new GridPlacementItemInfo
            {
                ConfigId = targetItem.ConfigId,
                Count = targetItem.Count,
                AnchorSlotIndex = targetItem.AnchorSlotIndex,
                GridWidth = targetItem.GridWidth,
                GridHeight = targetItem.GridHeight,
            });

            return LoadoutGridPlacementHelper.ArePlacementsValid(validationItems, containerWidth, containerHeight);
        }

        private static int ApplyFixedSlotConfig(
            LoadoutComponent loadout,
            LoadoutFixedSlotType slotType,
            int configId,
            bool clearBagItemsWhenRemovingBackpack,
            out string message)
        {
            message = string.Empty;
            if (slotType == LoadoutFixedSlotType.None)
            {
                message = "slot type invalid";
                return ErrorCode.ERR_LoadoutAreaInvalid;
            }

            if (configId <= 0)
            {
                if (slotType == LoadoutFixedSlotType.Backpack)
                {
                    loadout.BackpackConfigId = 0;
                    loadout.BagWidth = 0;
                    loadout.BagHeight = 0;
                    if (clearBagItemsWhenRemovingBackpack)
                    {
                        loadout.CarriedBagItems.Clear();
                    }

                    return ErrorCode.ERR_Success;
                }

                SetFixedSlotConfigId(loadout, slotType, 0);
                return ErrorCode.ERR_Success;
            }

            int validationError = LoadoutStateHelper.ValidateFixedSlotItem(configId, slotType);
            if (validationError != ErrorCode.ERR_Success)
            {
                message = $"config {configId} cannot place into fixed slot {slotType}";
                return validationError;
            }

            if (slotType == LoadoutFixedSlotType.Backpack)
            {
                if (!TryResolveBackpackSize(configId, out int bagWidth, out int bagHeight))
                {
                    message = "backpack size missing";
                    return ErrorCode.ERR_LoadoutGridInvalid;
                }

                loadout.BackpackConfigId = configId;
                loadout.BagWidth = bagWidth;
                loadout.BagHeight = bagHeight;
                return ErrorCode.ERR_Success;
            }

            SetFixedSlotConfigId(loadout, slotType, configId);
            return ErrorCode.ERR_Success;
        }

        private static bool ValidateCurrentState(LoadoutComponent loadout, out string message)
        {
            message = string.Empty;
            if (loadout == null)
            {
                message = "loadout null";
                return false;
            }

            if (loadout.MainWeaponConfigId > 0 &&
                LoadoutStateHelper.ValidateFixedSlotItem(loadout.MainWeaponConfigId, LoadoutFixedSlotType.MainWeapon) != ErrorCode.ERR_Success)
            {
                message = "main weapon invalid";
                return false;
            }

            if (loadout.SubWeaponConfigId > 0 &&
                LoadoutStateHelper.ValidateFixedSlotItem(loadout.SubWeaponConfigId, LoadoutFixedSlotType.SubWeapon) != ErrorCode.ERR_Success)
            {
                message = "sub weapon invalid";
                return false;
            }

            if (loadout.ArmorConfigId > 0 &&
                LoadoutStateHelper.ValidateFixedSlotItem(loadout.ArmorConfigId, LoadoutFixedSlotType.Armor) != ErrorCode.ERR_Success)
            {
                message = "armor invalid";
                return false;
            }

            if (loadout.BackpackConfigId > 0)
            {
                if (LoadoutStateHelper.ValidateFixedSlotItem(loadout.BackpackConfigId, LoadoutFixedSlotType.Backpack) != ErrorCode.ERR_Success)
                {
                    message = "backpack invalid";
                    return false;
                }

                if (!TryResolveBackpackSize(loadout.BackpackConfigId, out int bagWidth, out int bagHeight))
                {
                    message = "backpack size missing";
                    return false;
                }

                loadout.BagWidth = bagWidth;
                loadout.BagHeight = bagHeight;
                if (!ValidateGridContainer(loadout.CarriedBagItems, bagWidth, bagHeight))
                {
                    message = "bag placement invalid";
                    return false;
                }
            }
            else
            {
                if (loadout.CarriedBagItems.Count > 0)
                {
                    message = "bag items exist without backpack";
                    return false;
                }

                loadout.BagWidth = 0;
                loadout.BagHeight = 0;
            }

            if (!ValidateGridContainer(loadout.CarriedSecureItems, loadout.SecureWidth, loadout.SecureHeight))
            {
                message = "secure placement invalid";
                return false;
            }

            return true;
        }

        private static bool ValidateGridContainer(List<LoadoutGridItemInfo> container, int containerWidth, int containerHeight)
        {
            if (container == null || container.Count == 0)
            {
                return containerWidth >= 0 && containerHeight >= 0;
            }

            if (containerWidth <= 0 || containerHeight <= 0)
            {
                return false;
            }

            List<GridPlacementItemInfo> validationItems = BuildValidationItems(container, -1);
            return LoadoutGridPlacementHelper.ArePlacementsValid(validationItems, containerWidth, containerHeight);
        }

        private static List<int> GetBlockingGridIndices(
            LoadoutComponent loadout,
            LoadoutAreaType areaType,
            List<LoadoutGridItemInfo> container,
            LoadoutGridItemInfo targetItem,
            int ignoreIndex)
        {
            int containerWidth = GetGridContainerWidth(loadout, areaType);
            int containerHeight = GetGridContainerHeight(loadout, areaType);
            if (!IsGridItemInBounds(targetItem, containerWidth, containerHeight))
            {
                return null;
            }

            List<int> blockers = new();
            for (int i = 0; i < container.Count; ++i)
            {
                if (i == ignoreIndex)
                {
                    continue;
                }

                if (IsGridItemOverlapping(targetItem, container[i], containerWidth))
                {
                    blockers.Add(i);
                }
            }

            return blockers;
        }

        private static bool IsGridItemInBounds(LoadoutGridItemInfo item, int containerWidth, int containerHeight)
        {
            if (containerWidth <= 0 || containerHeight <= 0 || item.AnchorSlotIndex < 0)
            {
                return false;
            }

            int x = item.AnchorSlotIndex % containerWidth;
            int y = item.AnchorSlotIndex / containerWidth;
            return x >= 0 &&
                   y >= 0 &&
                   item.GridWidth > 0 &&
                   item.GridHeight > 0 &&
                   x + item.GridWidth <= containerWidth &&
                   y + item.GridHeight <= containerHeight;
        }

        private static bool IsGridItemOverlapping(LoadoutGridItemInfo a, LoadoutGridItemInfo b, int containerWidth)
        {
            int ax = a.AnchorSlotIndex % containerWidth;
            int ay = a.AnchorSlotIndex / containerWidth;
            int bx = b.AnchorSlotIndex % containerWidth;
            int by = b.AnchorSlotIndex / containerWidth;
            return ax < bx + b.GridWidth &&
                   ax + a.GridWidth > bx &&
                   ay < by + b.GridHeight &&
                   ay + a.GridHeight > by;
        }

        private static List<GridPlacementItemInfo> BuildValidationItems(List<LoadoutGridItemInfo> container, int ignoreIndex)
        {
            List<GridPlacementItemInfo> validationItems = new(container.Count > 0 ? container.Count : 1);
            for (int i = 0; i < container.Count; ++i)
            {
                if (i == ignoreIndex)
                {
                    continue;
                }

                LoadoutGridItemInfo item = container[i];
                validationItems.Add(new GridPlacementItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.AnchorSlotIndex,
                    GridWidth = item.GridWidth,
                    GridHeight = item.GridHeight,
                });
            }

            return validationItems;
        }

        private static object[] CaptureSnapshot(LoadoutComponent loadout)
        {
            return new object[]
            {
                loadout.HeroConfigId,
                loadout.MainWeaponConfigId,
                loadout.SubWeaponConfigId,
                loadout.ArmorConfigId,
                loadout.BackpackConfigId,
                loadout.BagWidth,
                loadout.BagHeight,
                loadout.SecureWidth,
                loadout.SecureHeight,
                new List<LoadoutGridItemInfo>(loadout.CarriedBagItems),
                new List<LoadoutGridItemInfo>(loadout.CarriedSecureItems),
                new List<int>(loadout.ConsumableConfigIds),
                loadout.IsConfirmed,
                loadout.ConfirmedAt,
            };
        }

        private static void RestoreSnapshot(LoadoutComponent loadout, object[] snapshot)
        {
            loadout.HeroConfigId = (int)snapshot[0];
            loadout.MainWeaponConfigId = (int)snapshot[1];
            loadout.SubWeaponConfigId = (int)snapshot[2];
            loadout.ArmorConfigId = (int)snapshot[3];
            loadout.BackpackConfigId = (int)snapshot[4];
            loadout.BagWidth = (int)snapshot[5];
            loadout.BagHeight = (int)snapshot[6];
            loadout.SecureWidth = (int)snapshot[7];
            loadout.SecureHeight = (int)snapshot[8];
            loadout.IsConfirmed = (bool)snapshot[12];
            loadout.ConfirmedAt = (long)snapshot[13];

            loadout.CarriedBagItems.Clear();
            loadout.CarriedBagItems.AddRange((List<LoadoutGridItemInfo>)snapshot[9]);
            loadout.CarriedSecureItems.Clear();
            loadout.CarriedSecureItems.AddRange((List<LoadoutGridItemInfo>)snapshot[10]);
            loadout.ConsumableConfigIds.Clear();
            loadout.ConsumableConfigIds.AddRange((List<int>)snapshot[11]);
        }

        private static List<LoadoutGridItemInfo> GetGridContainer(LoadoutComponent loadout, LoadoutAreaType areaType)
        {
            return areaType switch
            {
                LoadoutAreaType.Bag when loadout.BackpackConfigId > 0 && loadout.BagWidth > 0 && loadout.BagHeight > 0 => loadout.CarriedBagItems,
                LoadoutAreaType.Secure when loadout.SecureWidth > 0 && loadout.SecureHeight > 0 => loadout.CarriedSecureItems,
                _ => null,
            };
        }

        private static int GetGridContainerWidth(LoadoutComponent loadout, LoadoutAreaType areaType)
        {
            return areaType switch
            {
                LoadoutAreaType.Bag => loadout.BagWidth,
                LoadoutAreaType.Secure => loadout.SecureWidth,
                _ => 0,
            };
        }

        private static int GetGridContainerHeight(LoadoutComponent loadout, LoadoutAreaType areaType)
        {
            return areaType switch
            {
                LoadoutAreaType.Bag => loadout.BagHeight,
                LoadoutAreaType.Secure => loadout.SecureHeight,
                _ => 0,
            };
        }

        private static int GetFixedSlotConfigId(LoadoutComponent loadout, LoadoutFixedSlotType slotType)
        {
            return slotType switch
            {
                LoadoutFixedSlotType.MainWeapon => loadout.MainWeaponConfigId,
                LoadoutFixedSlotType.SubWeapon => loadout.SubWeaponConfigId,
                LoadoutFixedSlotType.Armor => loadout.ArmorConfigId,
                LoadoutFixedSlotType.Backpack => loadout.BackpackConfigId,
                _ => 0,
            };
        }

        private static void SetFixedSlotConfigId(LoadoutComponent loadout, LoadoutFixedSlotType slotType, int configId)
        {
            switch (slotType)
            {
                case LoadoutFixedSlotType.MainWeapon:
                    loadout.MainWeaponConfigId = configId;
                    break;
                case LoadoutFixedSlotType.SubWeapon:
                    loadout.SubWeaponConfigId = configId;
                    break;
                case LoadoutFixedSlotType.Armor:
                    loadout.ArmorConfigId = configId;
                    break;
                case LoadoutFixedSlotType.Backpack:
                    loadout.BackpackConfigId = configId;
                    break;
            }
        }

        private static void ClearFixedSlot(LoadoutComponent loadout, LoadoutFixedSlotType slotType)
        {
            switch (slotType)
            {
                case LoadoutFixedSlotType.MainWeapon:
                    loadout.MainWeaponConfigId = 0;
                    break;
                case LoadoutFixedSlotType.SubWeapon:
                    loadout.SubWeaponConfigId = 0;
                    break;
                case LoadoutFixedSlotType.Armor:
                    loadout.ArmorConfigId = 0;
                    break;
                case LoadoutFixedSlotType.Backpack:
                    loadout.BackpackConfigId = 0;
                    loadout.BagWidth = 0;
                    loadout.BagHeight = 0;
                    loadout.CarriedBagItems.Clear();
                    break;
            }
        }

        private static void AddFixedSlotToWarehouse(PlayerStorageComponent storage, int configId)
        {
            if (configId > 0)
            {
                storage.AddWarehouseItem(configId, 1);
            }
        }

        private static void InvalidateConfirmedState(LoadoutComponent loadout)
        {
            loadout.IsConfirmed = false;
            loadout.ConfirmedAt = 0;
        }
    }
}
