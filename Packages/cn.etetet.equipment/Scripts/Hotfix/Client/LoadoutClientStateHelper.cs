using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端局外起装正式状态同步辅助方法。
    /// </summary>
    public static class LoadoutClientStateHelper
    {
        public static void ApplyGetHeroList(LoadoutComponent loadout, G2C_GetHeroList message)
        {
            if (message.IsConfirmed || loadout.SelectedHeroConfigId <= 0)
            {
                loadout.SelectedHeroConfigId = message.CurrentHeroConfigId;
            }

            loadout.MainWeaponConfigId = message.CurrentMainWeaponConfigId;
            loadout.SubWeaponConfigId = message.CurrentSubWeaponConfigId;
            loadout.ArmorConfigId = message.CurrentArmorConfigId;
            loadout.BackpackConfigId = message.CurrentBackpackConfigId;
            loadout.BagWidth = message.CurrentBagWidth;
            loadout.BagHeight = message.CurrentBagHeight;
            loadout.SecureWidth = message.SecureWidth;
            loadout.SecureHeight = message.SecureHeight;
            loadout.WarehouseColumnCount = message.CurrentWarehouseColumnCount;
            loadout.TotalWealth = message.TotalWealth;
            loadout.IsConfirmed = message.IsConfirmed;
            loadout.ConfirmedAt = message.ConfirmedAt;

            loadout.StorageItemCounts.Clear();
            int storagePairCount = message.StorageConfigIds.Count < message.StorageCounts.Count
                ? message.StorageConfigIds.Count
                : message.StorageCounts.Count;
            for (int i = 0; i < storagePairCount; ++i)
            {
                int count = message.StorageCounts[i];
                if (count <= 0)
                {
                    continue;
                }

                loadout.StorageItemCounts[message.StorageConfigIds[i]] = count;
            }

            loadout.ConsumableConfigIds.Clear();
            loadout.ConsumableConfigIds.AddRange(message.CurrentConsumableConfigIds);

            CopyGridItems(message.CurrentBagItems, loadout.CarriedBagItems);
            CopyGridItems(message.CurrentSecureItems, loadout.CarriedSecureItems);
            CopyWarehouseItems(message.CurrentWarehouseItems, loadout.WarehouseItems);
        }

        public static void ApplyStateChanged(LoadoutComponent loadout, G2C_LoadoutStateChanged message)
        {
            if (message.IsConfirmed || loadout.SelectedHeroConfigId <= 0)
            {
                loadout.SelectedHeroConfigId = message.CurrentHeroConfigId;
            }

            loadout.MainWeaponConfigId = message.CurrentMainWeaponConfigId;
            loadout.SubWeaponConfigId = message.CurrentSubWeaponConfigId;
            loadout.ArmorConfigId = message.CurrentArmorConfigId;
            loadout.BackpackConfigId = message.CurrentBackpackConfigId;
            loadout.BagWidth = message.CurrentBagWidth;
            loadout.BagHeight = message.CurrentBagHeight;
            loadout.SecureWidth = message.SecureWidth;
            loadout.SecureHeight = message.SecureHeight;
            loadout.WarehouseColumnCount = message.CurrentWarehouseColumnCount;
            loadout.TotalWealth = message.TotalWealth;
            loadout.IsConfirmed = message.IsConfirmed;
            loadout.ConfirmedAt = message.ConfirmedAt;

            loadout.StorageItemCounts.Clear();
            int storagePairCount = message.StorageConfigIds.Count < message.StorageCounts.Count
                ? message.StorageConfigIds.Count
                : message.StorageCounts.Count;
            for (int i = 0; i < storagePairCount; ++i)
            {
                int count = message.StorageCounts[i];
                if (count <= 0)
                {
                    continue;
                }

                loadout.StorageItemCounts[message.StorageConfigIds[i]] = count;
            }

            loadout.ConsumableConfigIds.Clear();
            loadout.ConsumableConfigIds.AddRange(message.CurrentConsumableConfigIds);

            CopyGridItems(message.CurrentBagItems, loadout.CarriedBagItems);
            CopyGridItems(message.CurrentSecureItems, loadout.CarriedSecureItems);
            CopyWarehouseItems(message.CurrentWarehouseItems, loadout.WarehouseItems);
        }

        private static void CopyGridItems(IList<LoadoutGridItemData> source, List<LoadoutGridItemInfo> target)
        {
            target.Clear();
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; ++i)
            {
                LoadoutGridItemData item = source[i];
                if (item == null)
                {
                    continue;
                }

                target.Add(new LoadoutGridItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.AnchorSlotIndex,
                    GridWidth = item.GridWidth,
                    GridHeight = item.GridHeight,
                });
            }
        }

        private static void CopyWarehouseItems(IList<LoadoutWarehouseItemData> source, List<LoadoutWarehouseItemInfo> target)
        {
            target.Clear();
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; ++i)
            {
                LoadoutWarehouseItemData item = source[i];
                if (item == null || item.ConfigId <= 0 || item.Count <= 0)
                {
                    continue;
                }

                target.Add(new LoadoutWarehouseItemInfo
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
    }
}
