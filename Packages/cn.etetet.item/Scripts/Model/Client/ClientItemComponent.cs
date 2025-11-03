using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端物品背包组件
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ClientItemComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 背包容量
        /// </summary>
        public int Capacity;
        
        /// <summary>
        /// 客户端背包物品数据（槽位索引 -> 物品信息）
        /// </summary>
        public Dictionary<int, ItemInfo> SlotItems = new();
    }
    
    /// <summary>
    /// 客户端物品信息
    /// </summary>
    public struct ItemInfo
    {
        /// <summary>
        /// 物品配置ID
        /// </summary>
        public int ConfigId;
        
        /// <summary>
        /// 物品数量
        /// </summary>
        public int Count;
        
        /// <summary>
        /// 槽位索引
        /// </summary>
        public int SlotIndex;
    }
}