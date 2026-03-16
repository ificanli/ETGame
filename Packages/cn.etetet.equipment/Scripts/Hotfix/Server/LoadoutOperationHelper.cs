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

            if (request.ConfigId <= 0)
            {
                message = "config id invalid";
                return ErrorCode.ERR_LoadoutItemNotFound;
            }

            if (request.Count <= 0)
            {
                message = "count invalid";
                return ErrorCode.ERR_LoadoutCountInvalid;
            }

            if (storage.GetWarehouseCount(request.ConfigId) < request.Count)
            {
                message = "warehouse item not enough";
                return ErrorCode.ERR_LoadoutWarehouseNotEnough;
            }

            int error = AddOwnedItem(
                loadout,
                request.TargetAreaType,
                request.TargetSlotType,
                request.TargetAnchorSlotIndex,
                request.TargetBagWidth,
                request.TargetBagHeight,
                request.ConfigId,
                request.Count,
                out message);
            if (error != ErrorCode.ERR_Success)
            {
                return error;
            }

            if (!storage.TryConsumeWarehouseItem(request.ConfigId, request.Count))
            {
                message = "warehouse item not enough";
                return ErrorCode.ERR_LoadoutWarehouseNotEnough;
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

                int error = PlaceIntoFixedSlot(loadout, targetSlotType, configId, targetBagWidth, targetBagHeight, out message);
                if (error != ErrorCode.ERR_Success)
                {
                    return error;
                }

                ClearFixedSlot(loadout, sourceSlotType);
                InvalidateConfirmedState(loadout);
                return ErrorCode.ERR_Success;
            }

            if (!TryBuildGridItem(configId, 1, targetAnchorSlotIndex, out LoadoutGridItemInfo gridItem, out message))
            {
                return ErrorCode.ERR_LoadoutGridInvalid;
            }

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

            int placeError = PlaceIntoGridArea(loadout, targetArea, gridItem, out message);
            if (placeError != ErrorCode.ERR_Success)
            {
                return placeError;
            }

            ClearFixedSlot(loadout, sourceSlotType);
            InvalidateConfirmedState(loadout);
            return ErrorCode.ERR_Success;
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

                int error = PlaceIntoFixedSlot(loadout, targetSlotType, sourceItem.ConfigId, targetBagWidth, targetBagHeight, out message);
                if (error != ErrorCode.ERR_Success)
                {
                    return error;
                }

                GetGridContainer(loadout, sourceArea).RemoveAt(sourceIndex);
                InvalidateConfirmedState(loadout);
                return ErrorCode.ERR_Success;
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
