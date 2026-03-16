namespace ET.Server
{
    /// <summary>
    /// 装备管理组件系统
    /// </summary>
    [EntitySystemOf(typeof(EquipmentComponent))]
    public static partial class EquipmentComponentSystem
    {
        #region 生命周期方法

        [EntitySystem]
        private static void Awake(this EquipmentComponent self)
        {
            self.EquippedItems.Clear();
        }

        [EntitySystem]
        private static void Destroy(this EquipmentComponent self)
        {
            self.EquippedItems.Clear();
        }

        /// <summary>
        /// 传送反序列化：从子 Item 重建 EquippedItems 字典
        /// 在 Gate->Map 传送完成后由框架自动调用
        /// </summary>
        [EntitySystem]
        private static void Deserialize(this EquipmentComponent self)
        {
            self.EquippedItems.Clear();
            foreach (var kv in self.Children)
            {
                if (kv.Value is Item item && item.SlotIndex > 0)
                {
                    self.EquippedItems[(EquipmentSlotType)item.SlotIndex] = item;
                }
            }
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 穿戴装备（将Item从背包移到装备槽位）
        /// </summary>
        public static void EquipItem(this EquipmentComponent self, Item item, EquipmentSlotType slotType)
        {
            if (item == null)
            {
                throw new System.Exception("Item cannot be null");
            }

            self.AddChild(item);

            // 检查Item是否有装备组件
            item.AddComponent<EquipmentItemComponent>();


            // 如果该槽位已有装备，先卸下
            if (self.EquippedItems.ContainsKey(slotType))
            {
                Item oldItem = self.EquippedItems[slotType];
                if (oldItem != null)
                {
                    // 卸下旧装备，设置SlotIndex为-1表示未装备
                    oldItem.SlotIndex = -1;
                }
            }

            // 穿戴新装备，使用Item的SlotIndex记录槽位类型
            item.SlotIndex = (int)slotType;
            self.EquippedItems[slotType] = item;
        }

        /// <summary>
        /// 卸下装备
        /// </summary>
        public static void UnEquipItem(this EquipmentComponent self, EquipmentSlotType slotType)
        {
            if (self.EquippedItems.ContainsKey(slotType))
            {
                Item item = self.EquippedItems[slotType];
                if (item != null)
                {
                    // 设置SlotIndex为-1表示未装备
                    item.SlotIndex = -1;
                }
                item.RemoveComponent<EquipmentItemComponent>();
                self.RemoveChild(item.Id, false);
                self.EquippedItems.Remove(slotType);
            }
        }

        /// <summary>
        /// 获取穿戴的装备Item
        /// </summary>
        public static Item GetEquippedItem(this EquipmentComponent self, EquipmentSlotType slotType)
        {
            if (self.EquippedItems.TryGetValue(slotType, out EntityRef<Item> itemRef))
            {
                return itemRef;
            }
            return null;
        }

        /// <summary>
        /// 检查槽位是否有装备
        /// </summary>
        public static bool HasEquippedItem(this EquipmentComponent self, EquipmentSlotType slotType)
        {
            if (self.EquippedItems.TryGetValue(slotType, out EntityRef<Item> itemRef))
            {
                Item item = itemRef;
                return item != null;
            }
            return false;
        }

        /// <summary>
        /// 获取所有穿戴的装备数量
        /// </summary>
        public static int GetEquippedItemCount(this EquipmentComponent self)
        {
            int count = 0;
            foreach (var kvp in self.EquippedItems)
            {
                Item item = kvp.Value;
                if (item != null)
                {
                    ++count;
                }
            }
            return count;
        }

        /// <summary>
        /// 清空所有装备
        /// </summary>
        public static void ClearAllEquipment(this EquipmentComponent self)
        {
            foreach (var kvp in self.EquippedItems)
            {
                Item item = kvp.Value;
                if (item != null)
                {
                    // 设置SlotIndex为-1表示未装备
                    item.SlotIndex = -1;
                }
            }
            self.EquippedItems.Clear();
        }

        #endregion
    }
}