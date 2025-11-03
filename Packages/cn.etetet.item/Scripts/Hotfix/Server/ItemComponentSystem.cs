using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 背包组件系统
    /// </summary>
    [EntitySystemOf(typeof(ItemComponent))]
    [FriendOf(typeof(ItemComponent))]
    [FriendOf(typeof(Item))]
    public static partial class ItemComponentSystem
    {
        #region 生命周期方法

        [EntitySystem]
        private static void Awake(this ItemComponent self)
        {
            self.Capacity = 100; // 默认背包容量100
        }

        [EntitySystem]
        private static void Destroy(this ItemComponent self)
        {
            self.SlotItems.Clear();
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 设置背包容量
        /// </summary>
        public static void SetCapacity(this ItemComponent self, int capacity)
        {
            self.Capacity = capacity;
        }

        /// <summary>
        /// 添加物品到背包
        /// </summary>
        /// <param name="self">物品组件</param>
        /// <param name="configId">物品配置ID</param>
        /// <param name="count">物品数量</param>
        /// <returns>是否添加成功</returns>
        public static bool AddItem(this ItemComponent self, int configId, int count)
        {
            if (count <= 0)
            {
                Log.Error("invalid item count");
                return false;
            }

            ItemConfigCategory itemConfigCategory = ItemConfigCategory.Instance;
            ItemConfig itemConfig = itemConfigCategory.Get(configId);
            if (itemConfig == null)
            {
                Log.Error($"item config not found: {configId}");
                return false;
            }

            int maxStack = itemConfig.MaxStack;
            int remainCount = count;

            // 如果物品可堆叠，先尝试叠加到已有物品
            if (maxStack > 1)
            {
                foreach (var kv in self.SlotItems)
                {
                    Item item = kv.Value;
                    if (item == null || item.IsDisposed)
                    {
                        continue;
                    }

                    if (item.ConfigId == configId && item.Count < maxStack)
                    {
                        int addCount = System.Math.Min(remainCount, maxStack - item.Count);
                        item.AddCount(addCount);
                        remainCount -= addCount;

                        if (remainCount <= 0)
                        {
                            return true;
                        }
                    }
                }
            }

            // 需要创建新物品
            while (remainCount > 0)
            {
                int slotIndex = self.FindEmptySlot();
                if (slotIndex < 0)
                {
                    Log.Error("bag is full");
                    return false;
                }

                int addCount = System.Math.Min(remainCount, maxStack);
                Item newItem = self.AddChild<Item>();
                newItem.ConfigId = configId;
                newItem.Count = addCount;
                newItem.SlotIndex = slotIndex;

                self.SlotItems[slotIndex] = newItem;
                remainCount -= addCount;
            }

            return true;
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        /// <param name="self">物品组件</param>
        /// <param name="configId">物品配置ID</param>
        /// <param name="count">移除数量</param>
        /// <returns>是否移除成功</returns>
        public static bool RemoveItem(this ItemComponent self, int configId, int count)
        {
            if (count <= 0)
            {
                return false;
            }

            // 先检查是否有足够的物品
            int totalCount = self.GetItemCount(configId);
            if (totalCount < count)
            {
                return false;
            }

            int remainCount = count;
            List<int> emptySlots = new();

            foreach (var kv in self.SlotItems)
            {
                if (remainCount <= 0)
                {
                    break;
                }

                Item item = kv.Value;
                if (item == null || item.IsDisposed)
                {
                    continue;
                }

                if (item.ConfigId == configId)
                {
                    if (item.Count <= remainCount)
                    {
                        remainCount -= item.Count;
                        emptySlots.Add(kv.Key);
                        item.Dispose();
                    }
                    else
                    {
                        item.ReduceCount(remainCount);
                        remainCount = 0;
                    }
                }
            }

            // 清理空槽位
            foreach (int slotIndex in emptySlots)
            {
                self.SlotItems.Remove(slotIndex);
            }

            return true;
        }

        /// <summary>
        /// 获取指定物品的总数量
        /// </summary>
        public static int GetItemCount(this ItemComponent self, int configId)
        {
            int count = 0;
            foreach (var kv in self.SlotItems)
            {
                Item item = kv.Value;
                if (item != null && !item.IsDisposed && item.ConfigId == configId)
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
                if (!self.SlotItems.ContainsKey(i))
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
            return self.SlotItems.Count;
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
            self.SlotItems.TryGetValue(slotIndex, out EntityRef<Item> itemRef);
            return itemRef;
        }

        /// <summary>
        /// 清空背包
        /// </summary>
        public static void Clear(this ItemComponent self)
        {
            foreach (var kv in self.SlotItems)
            {
                Item item = kv.Value;
                if (item != null && !item.IsDisposed)
                {
                    item.Dispose();
                }
            }
            self.SlotItems.Clear();
        }

        #endregion
    }
}