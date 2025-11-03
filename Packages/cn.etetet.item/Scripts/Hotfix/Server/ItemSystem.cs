namespace ET.Server
{
    /// <summary>
    /// 物品实体系统
    /// </summary>
    [EntitySystemOf(typeof(Item))]
    public static partial class ItemSystem
    {
        #region 生命周期方法

        [EntitySystem]
        private static void Awake(this Item self)
        {
            self.SlotIndex = -1;
        }

        [EntitySystem]
        private static void Destroy(this Item self)
        {
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 设置物品数量
        /// </summary>
        public static void SetCount(this Item self, int count)
        {
            self.Count = count;
        }

        /// <summary>
        /// 增加物品数量
        /// </summary>
        public static void AddCount(this Item self, int count)
        {
            self.Count += count;
        }

        /// <summary>
        /// 减少物品数量
        /// </summary>
        public static bool ReduceCount(this Item self, int count)
        {
            if (self.Count < count)
            {
                return false;
            }
            self.Count -= count;
            return true;
        }

        /// <summary>
        /// 设置槽位索引
        /// </summary>
        public static void SetSlotIndex(this Item self, int slotIndex)
        {
            self.SlotIndex = slotIndex;
        }

        #endregion
    }
}