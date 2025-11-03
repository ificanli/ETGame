namespace ET.Client
{
    /// <summary>
    /// 客户端物品实体
    /// </summary>
    [ChildOf(typeof(ItemComponent))]
    public class Item: Entity, IAwake, IDestroy
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