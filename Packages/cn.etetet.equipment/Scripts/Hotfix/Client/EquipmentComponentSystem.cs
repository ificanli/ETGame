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
        /// 更新装备信息（通过消息）
        /// </summary>
        /// <param name="self">装备组件</param>
        /// <param name="slotType">槽位类型（-1表示卸下装备）</param>
        /// <param name="itemId">物品ID</param>
        /// <param name="configId">物品配置ID</param>
        public static void UpdateEquipment(this EquipmentComponent self, int slotType, long itemId, int configId)
        {
            Scene scene = self.GetParent<Scene>();
            ItemComponent itemComponent = scene.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                Log.Error("ItemComponent not found in scene");
                return;
            }
            
            // slotType=-1表示卸下装备
            if (slotType == -1)
            {
                // 根据ItemId找到对应的装备并移除
                Item item = itemComponent.GetItemById(itemId);
                if (item != null && item.SlotIndex >= 0)
                {
                    EquipmentSlotType slot = (EquipmentSlotType)item.SlotIndex;
                    self.EquippedItems.Remove(slot);
                }
                return;
            }

            // 装备穿戴
            EquipmentSlotType equipSlot = (EquipmentSlotType)slotType;
            Item equipItem = itemComponent.GetItemById(itemId);
            
            if (equipItem != null)
            {
                // 更新现有装备引用
                if (equipItem.SlotIndex != slotType)
                {
                    // 槽位改变，需要更新映射
                    if (equipItem.SlotIndex >= 0)
                    {
                        EquipmentSlotType oldSlot = (EquipmentSlotType)equipItem.SlotIndex;
                        self.EquippedItems.Remove(oldSlot);
                    }
                }
                
                equipItem.ConfigId = configId;
                equipItem.SlotIndex = slotType;
                equipItem.Count = 1; // 装备数量总是1
                self.EquippedItems[equipSlot] = equipItem;
            }
            else
            {
                // 物品不存在，需要创建（通过ItemComponent）
                Item newItem = itemComponent.AddChildWithId<Item>(itemId);
                newItem.ConfigId = configId;
                newItem.SlotIndex = slotType;
                newItem.Count = 1; // 装备数量总是1
                
                // 将装备引用添加到装备组件
                self.EquippedItems[equipSlot] = newItem;
            }
        }

        #endregion
    }
}