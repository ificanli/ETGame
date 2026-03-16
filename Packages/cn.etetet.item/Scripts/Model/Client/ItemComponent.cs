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
        /// 当前背包本体配置ID
        /// </summary>
        public int BagConfigId;

        /// <summary>
        /// 背包格子宽
        /// </summary>
        public int Width;

        /// <summary>
        /// 背包格子高
        /// </summary>
        public int Height;

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
