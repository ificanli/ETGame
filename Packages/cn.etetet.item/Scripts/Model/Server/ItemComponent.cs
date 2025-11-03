using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 物品背包组件
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class ItemComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 背包容量
        /// </summary>
        public int Capacity;
        
        /// <summary>
        /// 背包槽位映射表（槽位索引 -> 物品EntityRef）
        /// </summary>
        public Dictionary<int, EntityRef<Item>> SlotItems = new();
    }
}