using System;

namespace ET.Client
{
    /// <summary>
    /// 客户端背包组件系统
    /// </summary>
    [EntitySystemOf(typeof(ItemComponent))]
    public static partial class ItemComponentSystem
    {
        #region 生命周期方法

        [EntitySystem]
        private static void Awake(this ItemComponent self)
        {
            self.SlotItems.Clear();
            self.SetCapacity(100); // 默认背包容量100
        }

        [EntitySystem]
        private static void Destroy(this ItemComponent self)
        {
            self.SlotItems.Clear();
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 更新背包物品信息
        /// </summary>
        public static void UpdateItem(this ItemComponent self, long itemId, int slotIndex, int configId, int count)
        {
            EnsureSlotIndex(self, slotIndex);

            Item item = self.GetItemById(itemId);
            
            if (count <= 0)
            {
                // Count=0表示该槽位的物品被移除或清空
                item?.Dispose();
            }
            else
            {
                // 查找是否已存在该ItemId的物品
                if (item != null)
                {
                    // 更新现有物品
                    // 如果槽位改变，需要更新槽位映射
                    if (item.SlotIndex != slotIndex)
                    {
                        self.SetSlotItem(slotIndex, item);
                    }
                    
                    item.ConfigId = configId;
                    item.Count = count;
                    item.SlotIndex = slotIndex;
                }
                else
                {
                    // 创建新物品，使用服务端传来的ItemId
                    item = self.AddChildWithId<Item>(itemId);
                    item.ConfigId = configId;
                    item.Count = count;
                    self.SetSlotItem(slotIndex, item);
                }
            }
        }

        /// <summary>
        /// 设置背包容量
        /// </summary>
        public static void SetCapacity(this ItemComponent self, int capacity)
        {
            if (capacity < 0)
            {
                Log.Error($"invalid capacity: {capacity}");
                return;
            }

            self.Capacity = capacity;
            EnsureSlotContainerSize(self, capacity);
        }

        /// <summary>
        /// 获取指定槽位的物品
        /// </summary>
        public static Item GetItemBySlot(this ItemComponent self, int slotIndex)
        {
            if ((uint)slotIndex < (uint)self.SlotItems.Count)
            {
                Item item = self.SlotItems[slotIndex];
                if (item != null)
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// 通过ItemId获取物品
        /// </summary>
        public static Item GetItemById(this ItemComponent self, long itemId)
        {
            return self.GetChild<Item>(itemId);
        }

        /// <summary>
        /// 获取指定物品的总数量
        /// </summary>
        public static int GetItemCount(this ItemComponent self, int configId)
        {
            int count = 0;
            foreach (EntityRef<Item> itemRef in self.SlotItems)
            {
                Item item = itemRef;
                if (item != null && item.ConfigId == configId)
                {
                    count += item.Count;
                }
            }
            return count;
        }

        /// <summary>
        /// 清空背包
        /// </summary>
        public static void Clear(this ItemComponent self)
        {
            for (int i = 0; i < self.SlotItems.Count; ++i)
            {
                Item item = self.SlotItems[i];
                item?.Dispose();
            }
        }

        /// <summary>
        /// 获取背包已使用槽位数量
        /// </summary>
        public static int GetUsedSlotCount(this ItemComponent self)
        {
            int count = 0;
            foreach (EntityRef<Item> itemRef in self.SlotItems)
            {
                Item item = itemRef;
                if (item != null)
                {
                    ++count;
                }
            }
            return count;
        }

        /// <summary>
        /// 检查背包是否已满
        /// </summary>
        public static bool IsFull(this ItemComponent self)
        {
            return self.GetUsedSlotCount() >= self.Capacity;
        }

        /// <summary>
        /// 设置指定槽位的物品
        /// </summary>
        public static void SetSlotItem(this ItemComponent self, int slotIndex, Item item)
        {
            EnsureSlotIndex(self, slotIndex);
            if (item != null)
            {
                item.SlotIndex = slotIndex;
            }

            self.SlotItems[slotIndex] = item;
        }

        /// <summary>
        /// 清空指定槽位的物品
        /// </summary>
        public static void ClearSlot(this ItemComponent self, int slotIndex)
        {
            if ((uint)slotIndex >= (uint)self.SlotItems.Count)
            {
                return;
            }

            self.SlotItems[slotIndex] = default;
        }

        #endregion

        #region 私有方法

        private static void EnsureSlotIndex(ItemComponent self, int slotIndex)
        {
            if (slotIndex < 0)
            {
                throw new Exception($"invalid slot index: {slotIndex}");
            }

            if (slotIndex >= self.Capacity)
            {
                throw new Exception($"slot index {slotIndex} exceeds capacity {self.Capacity}");
            }
        }

        private static void EnsureSlotContainerSize(ItemComponent self, int size)
        {
            if (size <= 0)
            {
                return;
            }

            if (self.SlotItems.Count >= size)
            {
                return;
            }

            int addCount = size - self.SlotItems.Count;
            for (int i = 0; i < addCount; ++i)
            {
                self.SlotItems.Add(default);
            }
        }

        #endregion
    }
}
