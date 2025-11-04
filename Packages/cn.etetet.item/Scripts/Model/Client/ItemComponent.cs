using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端物品背包组件
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ItemComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 背包容量
        /// </summary>
        public int Capacity;
        
        /// <summary>
        /// 背包槽位列表（索引对应槽位号，值为物品EntityRef）
        /// </summary>
        public List<EntityRef<Item>> SlotItems = new();
    }
}
