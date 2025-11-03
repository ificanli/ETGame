using System.Collections.Generic;

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
            List<long> updatedItemIds = new();

            // 如果物品可堆叠，先尝试叠加到已有物品
            if (maxStack > 1)
            {
                foreach (var kv in self.SlotItems)
                {
                    Item item = kv.Value;
                    if (item == null)
                    {
                        continue;
                    }

                    if (item.ConfigId == configId && item.Count < maxStack)
                    {
                        int addCount = System.Math.Min(remainCount, maxStack - item.Count);
                        item.AddCount(addCount);
                        remainCount -= addCount;
                        updatedItemIds.Add(item.Id);

                        if (remainCount <= 0)
                        {
                            break;
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
                updatedItemIds.Add(newItem.Id);
                remainCount -= addCount;
            }

            // 通知客户端物品更新
            foreach (long itemId in updatedItemIds)
            {
                Item item = self.GetItemById(itemId);
                if (item != null)
                {
                    self.NotifyItemUpdate(item);
                }
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
            List<long> updatedItemIds = new();

            foreach (var kv in self.SlotItems)
            {
                if (remainCount <= 0)
                {
                    break;
                }

                Item item = kv.Value;
                if (item == null)
                {
                    continue;
                }

                if (item.ConfigId == configId)
                {
                    if (item.Count <= remainCount)
                    {
                        remainCount -= item.Count;
                        emptySlots.Add(kv.Key);
                        // 通知客户端移除该槽位的物品（Count=0表示移除）
                        self.NotifyItemRemove(item);
                        item.Dispose();
                    }
                    else
                    {
                        item.ReduceCount(remainCount);
                        remainCount = 0;
                        updatedItemIds.Add(item.Id);
                    }
                }
            }

            // 清理空槽位
            foreach (int slotIndex in emptySlots)
            {
                self.SlotItems.Remove(slotIndex);
            }

            // 通知客户端物品数量更新
            foreach (long itemId in updatedItemIds)
            {
                Item item = self.GetItemById(itemId);
                if (item != null)
                {
                    self.NotifyItemUpdate(item);
                }
            }

            return true;
        }
        /// <summary>
        /// 通过ItemId移除物品
        /// </summary>
        /// <param name="self">物品组件</param>
        /// <param name="itemId">物品ID</param>
        /// <returns>是否移除成功</returns>
        public static bool RemoveItemById(this ItemComponent self, long itemId)
        {
            Item item = self.GetItemById(itemId);
            if (item == null)
            {
                Log.Error($"item not found: {itemId}");
                return false;
            }

            int slotIndex = item.SlotIndex;
            
            // 通知客户端移除该物品
            self.NotifyItemRemove(item);
            
            // 从字典移除
            self.SlotItems.Remove(slotIndex);
            
            // 销毁物品实体
            item.Dispose();

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
            foreach (var kv in self.SlotItems)
            {
                Item item = kv.Value;
                if (item != null)
                {
                    item.Dispose();
                }
            }
            self.SlotItems.Clear();
        }

        /// <summary>
        /// 通知客户端物品变化
        /// </summary>
        public static async ETTask NotifyItemChanges(this ItemComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            
            // 遍历所有物品，通知客户端更新
            foreach (var kv in self.SlotItems)
            {
                Item item = kv.Value;
                if (item == null)
                {
                    continue;
                }

                self.NotifyItemUpdate(item);
            }

            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 通知客户端单个物品更新
        /// </summary>
        public static void NotifyItemUpdate(this ItemComponent self, Item item)
        {
            Unit unit = self.GetParent<Unit>();
            
            M2C_UpdateItem message = M2C_UpdateItem.Create();
            message.ItemId = item.Id;
            message.SlotIndex = item.SlotIndex;
            message.ConfigId = item.ConfigId;
            message.Count = item.Count;
            
            MapMessageHelper.NoticeClient(unit, message, NoticeType.Self);
        }
        
        /// <summary>
        /// 通知客户端移除物品（Count=0表示移除）
        /// </summary>
        public static void NotifyItemRemove(this ItemComponent self, Item item)
        {
            Unit unit = self.GetParent<Unit>();
            
            M2C_UpdateItem message = M2C_UpdateItem.Create();
            message.ItemId = item.Id;
            message.SlotIndex = item.SlotIndex;
            message.ConfigId = item.ConfigId;
            message.Count = 0; // Count=0表示移除
            
            MapMessageHelper.NoticeClient(unit, message, NoticeType.Self);
        }

        /// <summary>
        /// 通知客户端背包容量变化
        /// </summary>
        public static void NotifyCapacityChange(this ItemComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            
            M2C_UpdateBagCapacity message = M2C_UpdateBagCapacity.Create();
            message.Capacity = self.Capacity;
            
            MapMessageHelper.NoticeClient(unit, message, NoticeType.Self);
        }

        /// <summary>
        /// 移动或堆叠物品
        /// </summary>
        /// <param name="self">物品组件</param>
        /// <param name="itemId">要移动的物品ID</param>
        /// <param name="toSlot">目标槽位</param>
        /// <returns>错误码，0表示成功</returns>
        public static int MoveItem(this ItemComponent self, long itemId, int toSlot)
        {
            // 验证槽位索引
            if (toSlot < 0 || toSlot >= self.Capacity)
            {
                return ErrorCode.ERR_ItemSlotInvalid;
            }

            // 获取要移动的物品
            Item fromItem = self.GetItemById(itemId);
            if (fromItem == null)
            {
                return ErrorCode.ERR_ItemNotFound;
            }
            
            int fromSlot = fromItem.SlotIndex;

            // 不能移动到同一个槽位
            if (fromSlot == toSlot)
            {
                return ErrorCode.ERR_ItemMoveToSameSlot;
            }

            // 获取目标槽位物品
            Item toItem = self.GetItemBySlot(toSlot);

            // 情况1：目标槽位为空，直接移动
            if (toItem == null)
            {
                // 先通知源槽位清空（在修改之前，保存物品Id和源槽位）
                long movedItemId = fromItem.Id;
                int fromSlotIndex = fromSlot;
                
                // 更新物品槽位
                fromItem.SlotIndex = toSlot;
                self.SlotItems.Remove(fromSlot);
                self.SlotItems[toSlot] = fromItem;

                // 通知客户端更新
                Unit unit = self.GetParent<Unit>();
                
                // 源槽位清空：使用原物品Id + 源槽位 + Count=0
                M2C_UpdateItem clearMessage = M2C_UpdateItem.Create();
                clearMessage.ItemId = movedItemId;
                clearMessage.SlotIndex = fromSlotIndex;
                clearMessage.ConfigId = 0;
                clearMessage.Count = 0;
                MapMessageHelper.NoticeClient(unit, clearMessage, NoticeType.Self);
                
                self.NotifyItemUpdate(fromItem); // 目标槽位更新
                return ErrorCode.ERR_Success;
            }

            // 情况2：目标槽位有物品
            // 如果ConfigId相同且可以堆叠，尝试堆叠
            if (fromItem.ConfigId == toItem.ConfigId)
            {
                ItemConfigCategory itemConfigCategory = ItemConfigCategory.Instance;
                ItemConfig itemConfig = itemConfigCategory.Get(fromItem.ConfigId);
                if (itemConfig == null)
                {
                    return ErrorCode.ERR_ItemNotFound;
                }

                int maxStack = itemConfig.MaxStack;

                // 只有可堆叠物品才能堆叠
                if (maxStack > 1)
                {
                    // 计算可以堆叠的数量
                    int canStackCount = maxStack - toItem.Count;
                    if (canStackCount > 0)
                    {
                        // 堆叠数量取最小值
                        int stackCount = System.Math.Min(canStackCount, fromItem.Count);
                        
                        // 更新目标槽位数量
                        toItem.AddCount(stackCount);
                        
                        // 更新源槽位数量
                        fromItem.ReduceCount(stackCount);

                        if (fromItem.Count <= 0)
                        {
                            // 源槽位物品完全堆叠到目标槽位
                            // 先保存信息，再移除
                            long fromItemId = fromItem.Id;
                            int fromSlotIndex = fromItem.SlotIndex;
                            
                            self.SlotItems.Remove(fromSlot);
                            fromItem.Dispose();
                            
                            // 源槽位清空：使用原物品Id + 源槽位 + Count=0
                            Unit unit = self.GetParent<Unit>();
                            M2C_UpdateItem clearMessage = M2C_UpdateItem.Create();
                            clearMessage.ItemId = fromItemId;
                            clearMessage.SlotIndex = fromSlotIndex;
                            clearMessage.ConfigId = 0;
                            clearMessage.Count = 0;
                            MapMessageHelper.NoticeClient(unit, clearMessage, NoticeType.Self);
                        }
                        else
                        {
                            // 源槽位还有剩余
                            self.NotifyItemUpdate(fromItem); // 源槽位更新数量
                        }

                        // 通知目标槽位更新
                        self.NotifyItemUpdate(toItem);
                        return ErrorCode.ERR_Success;
                    }
                    else
                    {
                        // 目标槽位已满，无法堆叠
                        return ErrorCode.ERR_ItemCannotStack;
                    }
                }
                else
                {
                    // 不可堆叠物品，执行交换
                    self.SwapItems(fromSlot, toSlot, fromItem, toItem);
                    return ErrorCode.ERR_Success;
                }
            }
            else
            {
                // 情况3：ConfigId不同，交换位置
                self.SwapItems(fromSlot, toSlot, fromItem, toItem);
                return ErrorCode.ERR_Success;
            }
        }

        /// <summary>
        /// 交换两个槽位的物品
        /// </summary>
        private static void SwapItems(this ItemComponent self, int fromSlot, int toSlot, Item fromItem, Item toItem)
        {
            // 交换槽位索引
            fromItem.SlotIndex = toSlot;
            toItem.SlotIndex = fromSlot;

            // 更新字典映射
            self.SlotItems[fromSlot] = toItem;
            self.SlotItems[toSlot] = fromItem;

            // 通知客户端更新两个槽位
            self.NotifyItemUpdate(toItem);
            self.NotifyItemUpdate(fromItem);
        }

        #endregion
    }
}