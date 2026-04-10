using System;
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
            self.BagConfigId = 0;
            self.SlotItems.Clear();
            self.SetCapacity(100); // 默认背包容量100
        }

        [EntitySystem]
        private static void Destroy(this ItemComponent self)
        {
            self.SlotItems.Clear();
        }
        
        [EntitySystem]
        private static void Deserialize(this ItemComponent self)
        {
            EnsureSlotContainerSize(self, self.Capacity);
            foreach (var kv in self.Children)
            {
                if (kv.Value is Item item)
                {
                    int resolvedConfigId = LegacyItemConfigIdHelper.NormalizeConfigId(item.ConfigId);
                    if (resolvedConfigId != item.ConfigId)
                    {
                        item.ConfigId = resolvedConfigId;
                    }

                    self.SlotItems[item.SlotIndex] = item;
                }
            }
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
                if (item != null && LegacyItemConfigIdHelper.MatchesConfigId(item.ConfigId, configId))
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
        /// 按当前二维背包布局查找首个可用锚点。
        /// </summary>
        public static bool TryFindFirstFitAnchorSlot(this ItemComponent self, int gridWidth, int gridHeight, out int anchorSlotIndex)
        {
            int width = self.Width > 0 ? self.Width : self.Capacity;
            int height = self.Height > 0 ? self.Height : 1;
            if (width <= 0 || height <= 0)
            {
                anchorSlotIndex = -1;
                return false;
            }

            List<GridPlacementItemInfo> placements = new();
            CollectPlacementInfos(self, placements);

            return LoadoutGridPlacementHelper.TryFindFirstFitAnchorSlot(
                placements,
                width,
                height,
                gridWidth,
                gridHeight,
                out anchorSlotIndex);
        }

        /// <summary>
        /// 检查指定锚点在当前二维背包布局下是否合法。
        /// </summary>
        public static bool CanPlaceAtAnchorSlot(
            this ItemComponent self,
            int anchorSlotIndex,
            int gridWidth,
            int gridHeight,
            long ignoreItemId = 0,
            long ignoreItemId2 = 0)
        {
            int width = self.Width > 0 ? self.Width : self.Capacity;
            int height = self.Height > 0 ? self.Height : 1;
            if (width <= 0 || height <= 0)
            {
                return false;
            }

            List<GridPlacementItemInfo> placements = new();
            CollectPlacementInfos(self, placements, ignoreItemId, ignoreItemId2);

            return LoadoutGridPlacementHelper.CanPlaceAtAnchorSlot(
                placements,
                width,
                height,
                anchorSlotIndex,
                gridWidth,
                gridHeight);
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

            self.SetSize(capacity, 1);
        }

        /// <summary>
        /// 设置背包二维尺寸
        /// </summary>
        public static void SetSize(this ItemComponent self, int width, int height)
        {
            if (width < 0 || height < 0)
            {
                throw new Exception($"invalid bag size: {width}x{height}");
            }

            self.Width = width;
            self.Height = height;
            self.Capacity = width * height;
            EnsureSlotContainerSize(self, self.Capacity);
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

        private static void CollectPlacementInfos(
            ItemComponent self,
            List<GridPlacementItemInfo> placements,
            long ignoreItemId = 0,
            long ignoreItemId2 = 0)
        {
            placements.Clear();

            foreach (EntityRef<Item> itemRef in self.SlotItems)
            {
                Item item = itemRef;
                if (item == null || item.IsDisposed || item.Id == ignoreItemId || item.Id == ignoreItemId2)
                {
                    continue;
                }

                placements.Add(new GridPlacementItemInfo
                {
                    ConfigId = item.ConfigId,
                    Count = item.Count,
                    AnchorSlotIndex = item.SlotIndex,
                    GridWidth = item.GridWidth,
                    GridHeight = item.GridHeight,
                });
            }
        }

        #endregion
    }
}
