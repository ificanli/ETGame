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

            // 主武器
            if (loadout.MainWeaponConfigId > 0)
            {
                EquipItemFromConfig(equipComp, loadout.MainWeaponConfigId, EquipmentSlotType.MainHand);
            }

            // 副武器
            if (loadout.SubWeaponConfigId > 0)
            {
                EquipItemFromConfig(equipComp, loadout.SubWeaponConfigId, EquipmentSlotType.OffHand);
            }

            // 护甲
            if (loadout.ArmorConfigId > 0)
            {
                EquipItemFromConfig(equipComp, loadout.ArmorConfigId, EquipmentSlotType.Chest);
            }

            // 消耗品
            for (int i = 0; i < loadout.ConsumableConfigIds.Count; i++)
            {
                int consumableConfigId = loadout.ConsumableConfigIds[i];
                if (consumableConfigId <= 0) continue;

                EquipmentSlotType slotType = i == 0 ? EquipmentSlotType.Consumable1 : EquipmentSlotType.Consumable2;
                EquipItemFromConfig(equipComp, consumableConfigId, slotType);
            }
        }

        private static void EquipItemFromConfig(EquipmentComponent equipComp, int configId, EquipmentSlotType slotType)
        {
            Item item = equipComp.AddChild<Item>();
            item.ConfigId = configId;
            item.Count = 1;
            equipComp.EquipItem(item, slotType);
        }
    }
}
