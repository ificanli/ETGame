using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 起装辅助类：将 LoadoutComponent 中的装备配置应用到 Unit 的 EquipmentComponent
    /// 在 C2G_EnterMapHandler 中创建 Unit 后调用
    /// </summary>
    public static class LoadoutHelper
    {
        /// <summary>
        /// 将起装配置应用到 Unit（创建 Item 并装入对应槽位）
        /// </summary>
        public static void ApplyLoadout(Unit unit, LoadoutComponent loadout)
        {
            if (loadout == null || !loadout.IsConfirmed)
            {
                return;
            }

            EquipmentComponent equipComp = unit.GetComponent<EquipmentComponent>();
            if (equipComp == null)
            {
                Log.Error($"LoadoutHelper.ApplyLoadout: unit {unit.Id} has no EquipmentComponent");
                return;
            }

            ApplyFixedLoadout(
                equipComp,
                loadout.MainWeaponConfigId,
                loadout.SubWeaponConfigId,
                loadout.ArmorConfigId,
                loadout.ConsumableConfigIds);

            ApplyBagItems(unit, loadout);
        }

        /// <summary>
        /// 直接用配置ID应用起装（不需要 LoadoutComponent）
        /// </summary>
        public static void ApplyLoadout(Unit unit, int mainWeaponConfigId, int subWeaponConfigId, int armorConfigId)
        {
            EquipmentComponent equipComp = unit.GetComponent<EquipmentComponent>();
            if (equipComp == null)
            {
                Log.Error($"LoadoutHelper.ApplyLoadout: unit {unit.Id} has no EquipmentComponent");
                return;
            }

            ApplyFixedLoadout(equipComp, mainWeaponConfigId, subWeaponConfigId, armorConfigId, null);
        }

        /// <summary>
        /// 直接用完整运行时快照应用起装（固定装备 + 消耗品 + 背包）。
        /// </summary>
        public static void ApplyLoadout(
            Unit unit,
            int mainWeaponConfigId,
            int subWeaponConfigId,
            int armorConfigId,
            IList<int> consumableConfigIds,
            int backpackConfigId,
            int bagWidth,
            int bagHeight,
            IList<LoadoutGridItemInfo> bagItems)
        {
            EquipmentComponent equipComp = unit.GetComponent<EquipmentComponent>();
            if (equipComp == null)
            {
                Log.Error($"LoadoutHelper.ApplyLoadout: unit {unit.Id} has no EquipmentComponent");
                return;
            }

            ApplyFixedLoadout(equipComp, mainWeaponConfigId, subWeaponConfigId, armorConfigId, consumableConfigIds);
            ApplyBagItems(unit, backpackConfigId, bagWidth, bagHeight, bagItems);
        }

        private static void ApplyFixedLoadout(
            EquipmentComponent equipComp,
            int mainWeaponConfigId,
            int subWeaponConfigId,
            int armorConfigId,
            IList<int> consumableConfigIds)
        {
            ClearRuntimeEquipment(equipComp);

            if (mainWeaponConfigId > 0)
            {
                EquipItemFromConfig(equipComp, mainWeaponConfigId, EquipmentSlotType.MainHand);
            }

            if (subWeaponConfigId > 0)
            {
                EquipItemFromConfig(equipComp, subWeaponConfigId, EquipmentSlotType.OffHand);
            }

            if (armorConfigId > 0)
            {
                EquipItemFromConfig(equipComp, armorConfigId, EquipmentSlotType.Chest);
            }

            if (consumableConfigIds == null)
            {
                return;
            }

            for (int i = 0; i < consumableConfigIds.Count; ++i)
            {
                int consumableConfigId = consumableConfigIds[i];
                if (consumableConfigId <= 0)
                {
                    continue;
                }

                EquipmentSlotType slotType = i == 0 ? EquipmentSlotType.Consumable1 : EquipmentSlotType.Consumable2;
                EquipItemFromConfig(equipComp, consumableConfigId, slotType);
            }
        }

        private static void ClearRuntimeEquipment(EquipmentComponent equipComp)
        {
            if (equipComp == null || equipComp.EquippedItems.Count == 0)
            {
                return;
            }

            List<EquipmentSlotType> occupiedSlots = new(equipComp.EquippedItems.Keys);
            for (int i = 0; i < occupiedSlots.Count; ++i)
            {
                EquipmentSlotType slotType = occupiedSlots[i];
                if (!equipComp.HasEquippedItem(slotType))
                {
                    continue;
                }

                equipComp.UnEquipItem(slotType);
            }
        }

        private static void ApplyBagItems(Unit unit, LoadoutComponent loadout)
        {
            ApplyBagItems(unit, loadout.BackpackConfigId, loadout.BagWidth, loadout.BagHeight, loadout.CarriedBagItems);
        }

        private static void ApplyBagItems(
            Unit unit,
            int backpackConfigId,
            int bagWidth,
            int bagHeight,
            IList<LoadoutGridItemInfo> bagItems)
        {
            ItemComponent itemComp = unit.GetComponent<ItemComponent>();
            if (itemComp == null)
            {
                return;
            }

            itemComp.Clear();
            itemComp.BagConfigId = backpackConfigId;

            if (backpackConfigId <= 0 || bagWidth <= 0 || bagHeight <= 0)
            {
                itemComp.BagConfigId = 0;
                itemComp.SetSize(0, 0);
                return;
            }

            List<GridPlacementItemInfo> validationItems = new();
            int bagItemCount = bagItems?.Count ?? 0;
            for (int i = 0; i < bagItemCount; ++i)
            {
                LoadoutGridItemInfo bagItem = bagItems[i];
                validationItems.Add(new GridPlacementItemInfo
                {
                    ConfigId = bagItem.ConfigId,
                    Count = bagItem.Count,
                    AnchorSlotIndex = bagItem.AnchorSlotIndex,
                    GridWidth = bagItem.GridWidth,
                    GridHeight = bagItem.GridHeight,
                });
            }

            if (!LoadoutGridPlacementHelper.ArePlacementsValid(validationItems, bagWidth, bagHeight))
            {
                Log.Error($"LoadoutHelper.ApplyBagItems: invalid carried bag layout, player={unit.Id}");
                itemComp.BagConfigId = 0;
                itemComp.SetSize(0, 0);
                return;
            }

            itemComp.SetSize(bagWidth, bagHeight);

            for (int i = 0; i < bagItemCount; ++i)
            {
                LoadoutGridItemInfo bagItem = bagItems[i];
                Item item = itemComp.AddChild<Item>();
                item.ConfigId = bagItem.ConfigId;
                item.Count = bagItem.Count;
                item.GridWidth = bagItem.GridWidth;
                item.GridHeight = bagItem.GridHeight;
                itemComp.SetSlotItem(bagItem.AnchorSlotIndex, item);
            }
        }

        private static void EquipItemFromConfig(EquipmentComponent equipComp, int configId, EquipmentSlotType slotType)
        {
            if (equipComp.HasEquippedItem(slotType))
            {
                equipComp.UnEquipItem(slotType);
            }

            // AddChild sets the parent; EquipItem also calls AddChild → would throw "重复设置了Parent".
            // So we do the work inline: create item (parent already = equipComp), then register the slot.
            Item item = equipComp.AddChild<Item>();
            item.ConfigId = configId;
            item.Count = 1;
            item.AddComponent<EquipmentItemComponent>();
            item.SlotIndex = (int)slotType;
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            item.GridWidth = itemConfig?.GridWidth > 0 ? itemConfig.GridWidth : LoadoutGridPlacementHelper.DEFAULT_GRID_WIDTH;
            item.GridHeight = itemConfig?.GridHeight > 0 ? itemConfig.GridHeight : LoadoutGridPlacementHelper.DEFAULT_GRID_HEIGHT;
            equipComp.EquippedItems[slotType] = item;
        }
    }
}
