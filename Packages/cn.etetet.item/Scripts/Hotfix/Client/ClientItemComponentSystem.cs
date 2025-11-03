using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端背包组件系统
    /// </summary>
    [EntitySystemOf(typeof(ClientItemComponent))]
    [FriendOf(typeof(ClientItemComponent))]
    public static partial class ClientItemComponentSystem
    {
        #region 生命周期方法

        [EntitySystem]
        private static void Awake(this ClientItemComponent self)
        {
            self.Capacity = 100; // 默认背包容量100
        }

        [EntitySystem]
        private static void Destroy(this ClientItemComponent self)
        {
            self.SlotItems.Clear();
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 更新背包物品信息
        /// </summary>
        public static void UpdateItem(this ClientItemComponent self, int slotIndex, int configId, int count)
        {
            if (count <= 0)
            {
                // 移除物品
                self.SlotItems.Remove(slotIndex);
            }
            else
            {
                // 更新或添加物品
                self.SlotItems[slotIndex] = new ItemInfo
                {
                    ConfigId = configId,
                    Count = count,
                    SlotIndex = slotIndex
                };
            }
        }

        /// <summary>
        /// 获取指定槽位的物品
        /// </summary>
        public static ItemInfo? GetItemBySlot(this ClientItemComponent self, int slotIndex)
        {
            if (self.SlotItems.TryGetValue(slotIndex, out ItemInfo itemInfo))
            {
                return itemInfo;
            }
            return null;
        }

        /// <summary>
        /// 获取指定物品的总数量
        /// </summary>
        public static int GetItemCount(this ClientItemComponent self, int configId)
        {
            int count = 0;
            foreach (var kv in self.SlotItems)
            {
                if (kv.Value.ConfigId == configId)
                {
                    count += kv.Value.Count;
                }
            }
            return count;
        }

        /// <summary>
        /// 清空背包
        /// </summary>
        public static void Clear(this ClientItemComponent self)
        {
            self.SlotItems.Clear();
        }

        /// <summary>
        /// 获取背包已使用槽位数量
        /// </summary>
        public static int GetUsedSlotCount(this ClientItemComponent self)
        {
            return self.SlotItems.Count;
        }

        /// <summary>
        /// 检查背包是否已满
        /// </summary>
        public static bool IsFull(this ClientItemComponent self)
        {
            return self.GetUsedSlotCount() >= self.Capacity;
        }

        #endregion
    }
}