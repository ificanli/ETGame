using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 当前携带态与跑局结果回写相关的状态辅助方法。
    /// </summary>
    public static class LoadoutStateHelper
    {
        public static int ValidateWeaponSlot(int configId)
        {
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                return ErrorCode.ERR_Success;
            }

            EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            if (equipConfig == null)
            {
                return ErrorCode.ERR_LoadoutItemNotFound;
            }

            if (equipConfig.EquipSlot != (int)EquipmentSlotType.MainHand)
            {
                return ErrorCode.ERR_LoadoutSlotMismatch;
            }

            return ErrorCode.ERR_Success;
        }

        public static int ValidateEquipSlot(int configId, int expectedSlot)
        {
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                return ErrorCode.ERR_Success;
            }

            EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            if (equipConfig == null)
            {
                return ErrorCode.ERR_LoadoutItemNotFound;
            }

            if (equipConfig.EquipSlot != expectedSlot)
            {
                return ErrorCode.ERR_LoadoutSlotMismatch;
            }

            return ErrorCode.ERR_Success;
        }

        public static int ValidateItemExists(int configId)
        {
            if (configId <= 0)
            {
                return ErrorCode.ERR_Success;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                return ErrorCode.ERR_Success;
            }

            EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            return equipConfig != null ? ErrorCode.ERR_Success : ErrorCode.ERR_LoadoutItemNotFound;
        }

        public static int ValidateFixedSlotItem(int configId, LoadoutFixedSlotType slotType)
        {
            return slotType switch
            {
                LoadoutFixedSlotType.MainWeapon => ValidateWeaponSlot(configId),
                LoadoutFixedSlotType.SubWeapon => ValidateWeaponSlot(configId),
                LoadoutFixedSlotType.Armor => ValidateEquipSlot(configId, (int)EquipmentSlotType.Chest),
                LoadoutFixedSlotType.Backpack => ValidateBackpackSlot(configId),
                _ => ErrorCode.ERR_LoadoutAreaInvalid,
            };
        }

        public static int ValidateBackpackSlot(int configId)
        {
            if (configId <= 0)
            {
                return ErrorCode.ERR_Success;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig == null)
            {
                return ErrorCode.ERR_LoadoutItemNotFound;
            }

            return itemConfig.IsBackpack || (itemConfig.BackpackWidth > 0 && itemConfig.BackpackHeight > 0)
                ? ErrorCode.ERR_Success
                : ErrorCode.ERR_LoadoutSlotMismatch;
        }

        public static bool TryBuildValidatedGridItems(
            IList<LoadoutGridItemData> source,
            int containerWidth,
            int containerHeight,
            out List<LoadoutGridItemInfo> result)
        {
            result = new List<LoadoutGridItemInfo>();
            List<GridPlacementItemInfo> validationItems = new List<GridPlacementItemInfo>();

            if ((containerWidth == 0 && containerHeight != 0) || (containerWidth != 0 && containerHeight == 0))
            {
                return false;
            }

            if (source == null || source.Count == 0)
            {
                return containerWidth >= 0 && containerHeight >= 0;
            }

            if (containerWidth <= 0 || containerHeight <= 0)
            {
                return false;
            }

            for (int i = 0; i < source.Count; ++i)
            {
                LoadoutGridItemData item = source[i];
                if (item == null || item.ConfigId <= 0 || item.Count <= 0 || item.AnchorSlotIndex < 0)
                {
                    return false;
                }

                result.Add(new LoadoutGridItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.AnchorSlotIndex,
                    GridWidth = item.GridWidth,
                    GridHeight = item.GridHeight,
                });

                validationItems.Add(new GridPlacementItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.AnchorSlotIndex,
                    GridWidth = item.GridWidth,
                    GridHeight = item.GridHeight,
                });
            }

            return LoadoutGridPlacementHelper.ArePlacementsValid(validationItems, containerWidth, containerHeight);
        }

        public static void CopyGridItemsToMessage(IList<LoadoutGridItemInfo> source, IList<LoadoutGridItemData> target)
        {
            target.Clear();
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; ++i)
            {
                LoadoutGridItemInfo item = source[i];
                LoadoutGridItemData data = LoadoutGridItemData.Create();
                data.ConfigId = item.ConfigId;
                data.Count = item.Count;
                data.AnchorSlotIndex = item.AnchorSlotIndex;
                data.GridWidth = item.GridWidth;
                data.GridHeight = item.GridHeight;
                target.Add(data);
            }
        }

        public static void ApplyConfirmedSnapshot(
            LoadoutComponent loadout,
            C2G_ConfirmLoadout request,
            List<LoadoutGridItemInfo> finalBagItems,
            List<LoadoutGridItemInfo> finalSecureItems)
        {
            loadout.HeroConfigId = request.HeroConfigId;
            loadout.MainWeaponConfigId = request.MainWeaponConfigId;
            loadout.SubWeaponConfigId = request.SubWeaponConfigId;
            loadout.ArmorConfigId = request.ArmorConfigId;

            bool hasBagSnapshot =
                    request.BackpackConfigId > 0 ||
                    request.BagWidth > 0 ||
                    request.BagHeight > 0 ||
                    finalBagItems.Count > 0;
            if (hasBagSnapshot)
            {
                loadout.BackpackConfigId = request.BackpackConfigId;
                loadout.BagWidth = request.BagWidth;
                loadout.BagHeight = request.BagHeight;
                loadout.CarriedBagItems.Clear();
                loadout.CarriedBagItems.AddRange(finalBagItems);
            }

            bool hasSecureSnapshot =
                    request.SecureWidth > 0 ||
                    request.SecureHeight > 0 ||
                    finalSecureItems.Count > 0;
            if (hasSecureSnapshot)
            {
                loadout.SecureWidth = request.SecureWidth;
                loadout.SecureHeight = request.SecureHeight;
                loadout.CarriedSecureItems.Clear();
                loadout.CarriedSecureItems.AddRange(finalSecureItems);
            }

            loadout.ConsumableConfigIds.Clear();
            if (request.ConsumableConfigIds != null)
            {
                loadout.ConsumableConfigIds.AddRange(request.ConsumableConfigIds);
            }

            loadout.IsConfirmed = true;
            loadout.ConfirmedAt = TimeInfo.Instance.ServerNow();
        }

        public static void FillGetHeroListResponse(LoadoutComponent loadout, G2C_GetHeroList response)
        {
            response.CurrentHeroConfigId = loadout.HeroConfigId;
            response.CurrentMainWeaponConfigId = loadout.MainWeaponConfigId;
            response.CurrentSubWeaponConfigId = loadout.SubWeaponConfigId;
            response.CurrentArmorConfigId = loadout.ArmorConfigId;
            response.CurrentBackpackConfigId = loadout.BackpackConfigId;
            response.CurrentBagWidth = loadout.BagWidth;
            response.CurrentBagHeight = loadout.BagHeight;
            response.SecureWidth = loadout.SecureWidth;
            response.SecureHeight = loadout.SecureHeight;
            response.IsConfirmed = loadout.IsConfirmed;
            response.ConfirmedAt = loadout.ConfirmedAt;
            response.CurrentConsumableConfigIds.AddRange(loadout.ConsumableConfigIds);

            CopyGridItemsToMessage(loadout.CarriedBagItems, response.CurrentBagItems);
            CopyGridItemsToMessage(loadout.CarriedSecureItems, response.CurrentSecureItems);
        }

        public static void FillGetHeroListStorageResponse(PlayerStorageComponent storage, G2C_GetHeroList response)
        {
            if (storage == null || response == null)
            {
                return;
            }

            FillWarehouseSummary(storage.WarehouseItems, response.StorageConfigIds, response.StorageCounts);
            CopyWarehouseItemsToMessage(storage.WarehouseItems, response.CurrentWarehouseItems);
            response.TotalWealth = storage.TotalWealth;
        }

        public static void FillLoadoutStateChangedResponse(
            LoadoutComponent loadout,
            PlayerStorageComponent storage,
            G2C_LoadoutStateChanged response)
        {
            if (storage != null)
            {
                FillWarehouseSummary(storage.WarehouseItems, response.StorageConfigIds, response.StorageCounts);
                CopyWarehouseItemsToMessage(storage.WarehouseItems, response.CurrentWarehouseItems);
                response.TotalWealth = storage.TotalWealth;
            }

            response.CurrentHeroConfigId = loadout.HeroConfigId;
            response.CurrentMainWeaponConfigId = loadout.MainWeaponConfigId;
            response.CurrentSubWeaponConfigId = loadout.SubWeaponConfigId;
            response.CurrentArmorConfigId = loadout.ArmorConfigId;
            response.CurrentBackpackConfigId = loadout.BackpackConfigId;
            response.CurrentBagWidth = loadout.BagWidth;
            response.CurrentBagHeight = loadout.BagHeight;
            response.SecureWidth = loadout.SecureWidth;
            response.SecureHeight = loadout.SecureHeight;
            response.IsConfirmed = loadout.IsConfirmed;
            response.ConfirmedAt = loadout.ConfirmedAt;
            response.CurrentConsumableConfigIds.AddRange(loadout.ConsumableConfigIds);

            CopyGridItemsToMessage(loadout.CarriedBagItems, response.CurrentBagItems);
            CopyGridItemsToMessage(loadout.CarriedSecureItems, response.CurrentSecureItems);
        }

        public static void ApplyCarryResult(LoadoutComponent loadout, Map2G_LoadoutCarryResult message)
        {
            loadout.MainWeaponConfigId = message.MainWeaponConfigId;
            loadout.SubWeaponConfigId = message.SubWeaponConfigId;
            loadout.ArmorConfigId = message.ArmorConfigId;
            loadout.BackpackConfigId = message.BackpackConfigId;
            loadout.BagWidth = message.BagWidth;
            loadout.BagHeight = message.BagHeight;
            loadout.CarriedBagItems.Clear();

            if (message.FinalBagItems != null)
            {
                for (int i = 0; i < message.FinalBagItems.Count; ++i)
                {
                    LoadoutGridItemData item = message.FinalBagItems[i];
                    if (item == null)
                    {
                        continue;
                    }

                    loadout.CarriedBagItems.Add(new LoadoutGridItemInfo
                    {
                        ConfigId = item.ConfigId,
                        Count = item.Count,
                        AnchorSlotIndex = item.AnchorSlotIndex,
                        GridWidth = item.GridWidth,
                        GridHeight = item.GridHeight,
                    });
                }
            }

            loadout.IsConfirmed = false;
            loadout.ConfirmedAt = 0;
        }

        public static void ResetToSecureOnly(LoadoutComponent loadout)
        {
            loadout.MainWeaponConfigId = 0;
            loadout.SubWeaponConfigId = 0;
            loadout.ArmorConfigId = 0;
            loadout.BackpackConfigId = 0;
            loadout.BagWidth = 0;
            loadout.BagHeight = 0;
            loadout.CarriedBagItems.Clear();
            loadout.ConsumableConfigIds.Clear();
            loadout.IsConfirmed = false;
            loadout.ConfirmedAt = 0;
        }

        public static Dictionary<int, int> BuildEvacuationSummary(Map2G_LoadoutCarryResult message)
        {
            Dictionary<int, int> summary = new();
            AddSummaryItem(summary, message.MainWeaponConfigId, 1);
            AddSummaryItem(summary, message.SubWeaponConfigId, 1);
            AddSummaryItem(summary, message.ArmorConfigId, 1);
            AddSummaryItem(summary, message.BackpackConfigId, 1);

            if (message.FinalBagItems != null)
            {
                for (int i = 0; i < message.FinalBagItems.Count; ++i)
                {
                    LoadoutGridItemData item = message.FinalBagItems[i];
                    if (item == null)
                    {
                        continue;
                    }

                    AddSummaryItem(summary, item.ConfigId, item.Count);
                }
            }

            return summary;
        }

        private static void AddSummaryItem(Dictionary<int, int> summary, int configId, int count)
        {
            if (configId <= 0 || count <= 0)
            {
                return;
            }

            if (summary.TryGetValue(configId, out int existing))
            {
                summary[configId] = existing + count;
            }
            else
            {
                summary[configId] = count;
            }
        }

        private static void CopyWarehouseItemsToMessage(IList<LoadoutWarehouseItemInfo> source, IList<LoadoutWarehouseItemData> target)
        {
            target.Clear();
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = source[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                LoadoutWarehouseItemData data = LoadoutWarehouseItemData.Create();
                data.ItemUid = item.ItemUid;
                data.ConfigId = item.ConfigId;
                data.Count = item.Count;
                data.GridWidth = item.GridWidth;
                data.GridHeight = item.GridHeight;
                target.Add(data);
            }
        }

        private static void FillWarehouseSummary(IList<LoadoutWarehouseItemInfo> source, IList<int> configIds, IList<int> counts)
        {
            configIds.Clear();
            counts.Clear();
            if (source == null)
            {
                return;
            }

            Dictionary<int, int> summary = new();
            for (int i = 0; i < source.Count; ++i)
            {
                LoadoutWarehouseItemInfo item = source[i];
                if (item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                if (summary.TryGetValue(item.ConfigId, out int existed))
                {
                    summary[item.ConfigId] = existed + item.Count;
                }
                else
                {
                    summary[item.ConfigId] = item.Count;
                }
            }

            List<int> ids = new(summary.Keys);
            ids.Sort(static (a, b) => a.CompareTo(b));
            for (int i = 0; i < ids.Count; ++i)
            {
                int configId = ids[i];
                configIds.Add(configId);
                counts.Add(summary[configId]);
            }
        }
    }
}
