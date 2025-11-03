using System.Collections.Generic;

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
        /// 更新背包物品信息
        /// </summary>
        public static void UpdateItem(this ItemComponent self, int slotIndex, int configId, int count)
        {
            if (count <= 0)
            {
                // 移除物品
                if (self.SlotItems.TryGetValue(slotIndex, out EntityRef<Item> itemRef))
                {
                    Item item = itemRef;
                    if (item != null && !item.IsDisposed)
                    {
                        item.Dispose();
                    }
                    self.SlotItems.Remove(slotIndex);
                }
            }
            else
            {
                // 更新或添加物品
                if (self.SlotItems.TryGetValue(slotIndex, out EntityRef<Item> itemRef))
                {
                    // 更新现有物品
                    Item item = itemRef;
                    if (item != null && !item.IsDisposed)
                    {
                        item.ConfigId = configId;
                        item.Count = count;
                        item.SlotIndex = slotIndex;
                    }
                    else
                    {
                        // Entity已失效，创建新的
                        Item newItem = self.AddChild<Item>();
                        newItem.ConfigId = configId;
                        newItem.Count = count;
                        newItem.SlotIndex = slotIndex;
                        self.SlotItems[slotIndex] = newItem;
                    }
                }
                else
                {
                    // 创建新物品
                    Item newItem = self.AddChild<Item>();
                    newItem.ConfigId = configId;
                    newItem.Count = count;
                    newItem.SlotIndex = slotIndex;
                    self.SlotItems[slotIndex] = newItem;
                }
            }
        }

        /// <summary>
        /// 获取指定槽位的物品
        /// </summary>
        public static Item GetItemBySlot(this ItemComponent self, int slotIndex)
        {
            if (self.SlotItems.TryGetValue(slotIndex, out EntityRef<Item> itemRef))
            {
                Item item = itemRef;
                if (item != null && !item.IsDisposed)
                {
                    return item;
                }
            }
            return null;
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

        #endregion
    }
}