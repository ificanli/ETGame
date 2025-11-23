using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 物品辅助类 - 处理涉及消息通知的物品逻辑
    /// </summary>
    public static class ItemHelper
    {
        /// <summary>
        /// 设置背包容量并通知客户端
        /// </summary>
        public static void SetCapacity(ItemComponent self, int capacity)
        {
            self.SetCapacity(capacity);
            NotifyCapacityChange(self);
        }

        /// <summary>
        /// 添加物品到背包
        /// </summary>
        /// <param name="self">物品组件</param>
        /// <param name="configId">物品配置ID</param>
        /// <param name="count">物品数量</param>
        /// <param name="reason">道具变化原因</param>
        /// <returns>是否添加成功</returns>
        public static void AddItem(ItemComponent self, int configId, int count, ItemChangeReason reason)
        {
            if (count <= 0)
            {
                throw new Exception("invalid item count");
            }

            ItemConfigCategory itemConfigCategory = ItemConfigCategory.Instance;
            ItemConfig itemConfig = itemConfigCategory.Get(configId);
            if (itemConfig == null)
            {
                throw new Exception($"item config not found: {configId}");
            }

            Log.Debug($"add item: configId={configId}, count={count}, reason={reason}");

            int maxStack = itemConfig.MaxStack;
            int remainCount = count;
            List<long> updatedItemIds = new();

            // 如果物品可堆叠，先尝试叠加到已有物品
            if (maxStack > 1)
            {
                for (int i = 0; i < self.SlotItems.Count; ++i)
                {
                    Item item = self.SlotItems[i];
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
                    throw new Exception("bag is full");
                }

                int addCount = System.Math.Min(remainCount, maxStack);
                Item newItem = self.AddChild<Item>();
                newItem.ConfigId = configId;
                newItem.Count = addCount;

                self.SetSlotItem(slotIndex, newItem);
                updatedItemIds.Add(newItem.Id);
                remainCount -= addCount;
            }

            // 通知客户端物品更新
            foreach (long itemId in updatedItemIds)
            {
                Item item = self.GetItemById(itemId);
                if (item != null)
                {
                    NotifyItemUpdate(self, item);
                }
            }
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        /// <param name="self">物品组件</param>
        /// <param name="configId">物品配置ID</param>
        /// <param name="count">移除数量</param>
        /// <param name="reason">道具变化原因</param>
        /// <returns>是否移除成功</returns>
        public static bool RemoveItem(ItemComponent self, int configId, int count, ItemChangeReason reason)
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

            Log.Debug($"remove item: configId={configId}, count={count}, reason={reason}");

            int remainCount = count;
            List<long> updatedItemIds = new();

            for (int i = 0; i < self.SlotItems.Count; ++i)
            {
                if (remainCount <= 0)
                {
                    break;
                }

                Item item = self.SlotItems[i];
                if (item == null)
                {
                    continue;
                }

                if (item.ConfigId == configId)
                {
                    if (item.Count <= remainCount)
                    {
                        remainCount -= item.Count;
                        // 通知客户端移除该槽位的物品（Count=0表示移除）
                        NotifyItemRemove(self, item);
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

            // 通知客户端物品数量更新
            foreach (long itemId in updatedItemIds)
            {
                Item item = self.GetItemById(itemId);
                if (item != null)
                {
                    NotifyItemUpdate(self, item);
                }
            }

            return true;
        }

        /// <summary>
        /// 通过ItemId移除物品
        /// </summary>
        /// <param name="self">物品组件</param>
        /// <param name="itemId">物品ID</param>
        /// <param name="reason">道具变化原因</param>
        /// <returns>是否移除成功</returns>
        public static bool RemoveItemById(ItemComponent self, long itemId, ItemChangeReason reason)
        {
            Item item = self.GetItemById(itemId);
            if (item == null)
            {
                Log.Error($"item not found: {itemId}");
                return false;
            }

            Log.Debug($"remove item by id: itemId={itemId}, configId={item.ConfigId}, count={item.Count}, reason={reason}");

            // 通知客户端移除该物品
            NotifyItemRemove(self, item);

            // 销毁物品实体
            item.Dispose();

            return true;
        }

                /// <summary>
        /// 通过ItemId移除物品
        /// </summary>
        /// <param name="self">物品组件</param>
        /// <param name="itemId">物品ID</param>
        /// <param name="reason">道具变化原因</param>
        /// <returns>是否移除成功</returns>
        public static Item RemoveItemByIdNoDispose(ItemComponent self, long itemId, ItemChangeReason reason)
        {
            Item item = self.GetItemById(itemId);
            if (item == null)
            {
                Log.Error($"item not found: {itemId}");
                return null;
            }

            Log.Debug($"remove item by id: itemId={itemId}, configId={item.ConfigId}, count={item.Count}, reason={reason}");

            // 通知客户端移除该物品
            NotifyItemRemove(self, item);

            return item;
        }

        /// <summary>
        /// 通知客户端物品变化
        /// </summary>
        public static async ETTask NotifyItemChanges(ItemComponent self)
        {
            // 遍历所有物品，通知客户端更新
            for (int i = 0; i < self.SlotItems.Count; ++i)
            {
                Item item = self.SlotItems[i];
                if (item == null)
                {
                    continue;
                }

                NotifyItemUpdate(self, item);
            }

            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 通知客户端单个物品更新
        /// </summary>
        public static void NotifyItemUpdate(ItemComponent self, Item item)
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
        public static void NotifyItemRemove(ItemComponent self, Item item)
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
        public static void NotifyCapacityChange(ItemComponent self)
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
        public static int MoveItem(ItemComponent self, long itemId, int toSlot)
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

            Log.Debug($"move item: itemId={itemId}, fromSlot={fromItem.SlotIndex}, toSlot={toSlot}, reason={ItemChangeReason.MoveItem}");

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
                // 更新物品槽位
                self.ClearSlot(fromSlot);
                self.SetSlotItem(toSlot, fromItem);

                // 通知客户端更新（客户端会自动处理槽位变化，移除旧槽位映射）
                NotifyItemUpdate(self, fromItem);
                return ErrorCode.ERR_Success;
            }

            // 情况2：目标槽位有物品
            // 如果ConfigId相同且可以堆叠，尝试堆叠
            if (fromItem.ConfigId != toItem.ConfigId)
            {
                SwapItems(self, fromSlot, toSlot, fromItem, toItem);
                return ErrorCode.ERR_Success;
            }

            // 情况3：ConfigId不同，交换位置
            ItemConfigCategory itemConfigCategory = ItemConfigCategory.Instance;
            ItemConfig itemConfig = itemConfigCategory.Get(fromItem.ConfigId);
            if (itemConfig == null)
            {
                return ErrorCode.ERR_ItemNotFound;
            }

            int maxStack = itemConfig.MaxStack;

            // 只有可堆叠物品才能堆叠
            if (maxStack <= 1)
            {
                SwapItems(self, fromSlot, toSlot, fromItem, toItem);
                return ErrorCode.ERR_Success;
            }

            int canStackCount = maxStack - toItem.Count;

            // 目标堆已满，执行交换
            if (canStackCount <= 0)
            {
                SwapItems(self, fromSlot, toSlot, fromItem, toItem);
                return ErrorCode.ERR_Success;
            }

            // 堆叠数量取最小值
            int stackCount = System.Math.Min(canStackCount, fromItem.Count);

            // 更新目标槽位数量
            toItem.AddCount(stackCount);

            // 更新源槽位数量
            fromItem.ReduceCount(stackCount);

            if (fromItem.Count <= 0)
            {
                // 通知客户端移除源槽位物品
                NotifyItemRemove(self, fromItem);

                // 源槽位物品完全堆叠到目标槽位，销毁物品实体
                fromItem.Dispose();
            }
            else
            {
                // 源槽位还有剩余
                NotifyItemUpdate(self, fromItem); // 源槽位更新数量
            }

            // 通知目标槽位更新
            NotifyItemUpdate(self, toItem);
            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 交换两个槽位的物品
        /// </summary>
        private static void SwapItems(ItemComponent self, int fromSlot, int toSlot, Item fromItem, Item toItem)
        {
            // 更新字典映射
            self.SetSlotItem(fromSlot, toItem);
            self.SetSlotItem(toSlot, fromItem);

            // 通知客户端更新两个槽位
            NotifyItemUpdate(self, toItem);
            NotifyItemUpdate(self, fromItem);
        }

        /// <summary>
        /// 整理背包 - 相同ConfigId的物品尽量堆叠在一起
        /// </summary>
        /// <param name="self">物品组件</param>
        /// <returns>错误码，0表示成功</returns>
        public static int SortBag(ItemComponent self)
        {
            Log.Debug($"sort bag: reason={ItemChangeReason.SortBag}");

            ItemConfigCategory itemConfigCategory = ItemConfigCategory.Instance;
            Unit unit = self.GetParent<Unit>();

            // 第一步：收集所有物品信息，按ConfigId分组统计数量
            Dictionary<int, int> configIdToCount = new();
            List<(long itemId, int slotIndex)> oldItemInfos = new();

            for (int i = 0; i < self.SlotItems.Count; ++i)
            {
                Item item = self.SlotItems[i];
                if (item == null)
                {
                    continue;
                }

                configIdToCount.TryAdd(item.ConfigId, 0);

                configIdToCount[item.ConfigId] += item.Count;
                oldItemInfos.Add((item.Id, item.SlotIndex));
            }

            // 第二步：先通知客户端清空所有旧槽位
            foreach ((long itemId, int slotIndex) in oldItemInfos)
            {
                M2C_UpdateItem clearMessage = M2C_UpdateItem.Create();
                clearMessage.ItemId = itemId;
                clearMessage.SlotIndex = slotIndex;
                clearMessage.ConfigId = 0;
                clearMessage.Count = 0;
                MapMessageHelper.NoticeClient(unit, clearMessage, NoticeType.Self);
            }

            // 第三步：清空所有槽位，并销毁旧物品
            for (int i = 0; i < self.SlotItems.Count; ++i)
            {
                Item item = self.SlotItems[i];
                item?.Dispose();
            }

            // 第四步：按ConfigId排序，重新创建物品堆, 相同ConfigId的物品尽量堆叠在一起, 不排序的话可能每次整理结果都不一样
            List<int> sortedConfigIds = new(configIdToCount.Keys);
            sortedConfigIds.Sort();

            int currentSlot = 0;
            foreach (int configId in sortedConfigIds)
            {
                ItemConfig itemConfig = itemConfigCategory.Get(configId);
                if (itemConfig == null)
                {
                    Log.Error($"item config not found: {configId}");
                    continue;
                }

                int maxStack = itemConfig.MaxStack;
                int remainCount = configIdToCount[configId];

                // 创建物品堆，每堆最多maxStack个
                while (remainCount > 0)
                {
                    if (currentSlot >= self.Capacity)
                    {
                        Log.Error("bag is full during sorting, this should not happen");
                        break;
                    }

                    int stackCount = System.Math.Min(remainCount, maxStack);

                    Item newItem = self.AddChild<Item>();
                    newItem.ConfigId = configId;
                    newItem.Count = stackCount;

                    self.SetSlotItem(currentSlot, newItem);

                    remainCount -= stackCount;
                    currentSlot++;
                }
            }

            // 第五步：通知客户端新物品
            for (int i = 0; i < self.SlotItems.Count; ++i)
            {
                Item item = self.SlotItems[i];
                if (item != null)
                {
                    NotifyItemUpdate(self, item);
                }
            }

            return ErrorCode.ERR_Success;
        }
    }
}
