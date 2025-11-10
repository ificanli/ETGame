namespace ET.Server
{
    /// <summary>
    /// 装备辅助类 - 处理涉及消息通知的装备逻辑
    /// </summary>
    public static class EquipmentHelper
    {
        /// <summary>
        /// 装备装备（从背包装备到装备槽位）
        /// </summary>
        /// <param name="unit">单位</param>
        /// <param name="itemId">要装备的物品ID</param>
        /// <param name="slotType">装备槽位类型</param>
        /// <returns>错误码，0表示成功</returns>
        public static int EquipItem(Unit unit, long itemId, EquipmentSlotType slotType)
        {
            // 获取装备组件
            EquipmentComponent equipmentComponent = unit.GetComponent<EquipmentComponent>();

            // 获取背包组件
            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();

            // 获取要装备的物品
            Item item = itemComponent.GetItemById(itemId);
            if (item == null)
            {
                Log.Error($"item not found in bag: {itemId}");
                return ErrorCode.ERR_EquipmentItemNotInBag;
            }

            // 确保物品有装备组件（标记为装备）
            item.AddComponent<EquipmentItemComponent>();

            Log.Debug($"equip item: itemId={itemId}, slotType={slotType}");

            // 检查槽位是否已有装备
            Item oldItem = equipmentComponent.GetEquippedItem(slotType);
            if (oldItem != null)
            {
                // 先卸下旧装备到背包
                int unequipResult = UnEquipItemInternal(unit, slotType, false);
                if (unequipResult != ErrorCode.ERR_Success)
                {
                    Log.Error($"failed to unequip old item: {unequipResult}");
                    return unequipResult;
                }
            }

            // 从背包中移除（记录槽位索引用于可能的回滚）
            ItemHelper.RemoveItemByIdNoDispose(itemComponent, itemId, ItemChangeReason.Equip);

            // 装备到装备槽位
            equipmentComponent.EquipItem(item, slotType);

            // 通知客户端装备变化
            NotifyEquipmentChange(unit, slotType, item);

            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 卸载装备（从装备槽位回到背包）
        /// </summary>
        /// <param name="unit">单位</param>
        /// <param name="slotType">装备槽位类型</param>
        /// <returns>错误码，0表示成功</returns>
        public static int UnequipItem(Unit unit, EquipmentSlotType slotType)
        {
            return UnEquipItemInternal(unit, slotType, true);
        }

        /// <summary>
        /// 卸载装备内部实现
        /// </summary>
        /// <param name="unit">单位</param>
        /// <param name="slotType">装备槽位类型</param>
        /// <param name="notifyClient">是否通知客户端</param>
        /// <returns>错误码，0表示成功</returns>
        private static int UnEquipItemInternal(Unit unit, EquipmentSlotType slotType, bool notifyClient)
        {
            // 获取装备组件
            EquipmentComponent equipmentComponent = unit.GetComponent<EquipmentComponent>();

            // 获取装备槽位的物品
            Item item = equipmentComponent.GetEquippedItem(slotType);
            if (item == null)
            {
                Log.Error($"no equipment in slot: {slotType}");
                return ErrorCode.ERR_EquipmentSlotEmpty;
            }

            // 获取背包组件
            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();

            // 检查背包是否有空槽位
            int emptySlot = itemComponent.FindEmptySlot();
            if (emptySlot < 0)
            {
                Log.Error("bag is full");
                return ErrorCode.ERR_EquipmentBagFull;
            }

            Log.Debug($"unequip item: itemId={item.Id}, slotType={slotType}");

            // 从装备槽位卸下
            equipmentComponent.UnEquipItem(slotType);

            // 放回背包
            itemComponent.SetSlotItem(emptySlot, item);

            if (notifyClient)
            {
                // 通知客户端装备变化（itemId=0表示卸载）
                NotifyEquipmentRemove(unit, slotType);

                // 通知客户端背包物品添加
                ItemHelper.NotifyItemUpdate(itemComponent, item);
            }

            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 通知客户端装备变化
        /// </summary>
        private static void NotifyEquipmentChange(Unit unit, EquipmentSlotType slotType, Item item)
        {
            // 使用M2C_UpdateItem消息通知装备变化
            // SlotIndex使用负数表示装备槽位：-(slotType+1)
            M2C_UpdateItem message = M2C_UpdateItem.Create();
            message.ItemId = item.Id;
            message.SlotIndex = -((int)slotType + 1); // 负数表示装备槽位
            message.ConfigId = item.ConfigId;
            message.Count = 1; // 装备数量总是1

            MapMessageHelper.NoticeClient(unit, message, NoticeType.Self);
            
            Log.Debug($"equipment changed: unitId={unit.Id}, slotType={slotType}, itemId={item.Id}, configId={item.ConfigId}");
        }

        /// <summary>
        /// 通知客户端装备移除
        /// </summary>
        private static void NotifyEquipmentRemove(Unit unit, EquipmentSlotType slotType)
        {
            // 使用M2C_UpdateItem消息通知装备移除（Count=0表示移除）
            M2C_UpdateItem message = M2C_UpdateItem.Create();
            message.ItemId = 0;
            message.SlotIndex = -((int)slotType + 1); // 负数表示装备槽位
            message.ConfigId = 0;
            message.Count = 0; // Count=0表示移除

            MapMessageHelper.NoticeClient(unit, message, NoticeType.Self);
            
            Log.Debug($"equipment removed: unitId={unit.Id}, slotType={slotType}");
        }
    }
}