using System;

namespace ET.Server
{
    /// <summary>
    /// 背包组件系统
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
        /// 查找空槽位
        /// </summary>
        /// <returns>空槽位索引，-1表示没有空槽位</returns>
        public static int FindEmptySlot(this ItemComponent self)
        {
            for (int i = 0; i < self.Capacity; i++)
            {
                Item item = self.SlotItems[i];
                if (item == null)
                {
                    return i;
                }
            }
            return -1;
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
        /// 获取指定槽位的物品
        /// </summary>
        public static Item GetItemBySlot(this ItemComponent self, int slotIndex)
        {
            if ((uint)slotIndex < (uint)self.SlotItems.Count)
            {
                return self.SlotItems[slotIndex];
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
        /// 清空背包
        /// </summary>
        public static void Clear(this ItemComponent self)
        {
            for (int i = 0; i < self.SlotItems.Count; ++i)
            {
                Item item = self.SlotItems[i];
                if (item != null)
                {
                    item.Dispose();
                }
                self.SlotItems[i] = default;
            }
        }

        #endregion

        #region 业务辅助方法

        /// <summary>
        /// 设置背包容量
        /// </summary>
        public static void SetCapacity(this ItemComponent self, int capacity)
        {
            if (capacity < 0)
            {
                throw new Exception($"invalid capacity: {capacity}");
            }

            self.Capacity = capacity;
            EnsureSlotContainerSize(self, capacity);
        }

        /// <summary>
        /// 清空指定槽位
        /// </summary>
        public static void ClearSlot(this ItemComponent self, int slotIndex)
        {
            if ((uint)slotIndex >= (uint)self.SlotItems.Count)
            {
                return;
            }

            self.SlotItems[slotIndex] = default;
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
        /// 尝试获取指定槽位的物品
        /// </summary>
        public static Item TryGetSlotItem(this ItemComponent self, int slotIndex)
        {
            if ((uint)slotIndex < (uint)self.SlotItems.Count)
            {
                return self.SlotItems[slotIndex];
            }

            return null;
        }

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
