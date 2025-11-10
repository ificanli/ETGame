namespace ET.Client
{
    /// <summary>
    /// 客户端装备组件系统
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

        #endregion

        #region 业务方法

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
        /// 更新装备信息
        /// </summary>
        public static void UpdateEquipment(this EquipmentComponent self, EquipmentSlotType slotType, Item item)
        {
            if (item != null)
            {
                item.SlotIndex = (int)slotType;
                self.EquippedItems[slotType] = item;
            }
            else
            {
                self.EquippedItems.Remove(slotType);
            }
        }

        #endregion
    }
}