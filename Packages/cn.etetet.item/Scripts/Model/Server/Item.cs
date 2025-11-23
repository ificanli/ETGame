namespace ET.Server
{
    /// <summary>
    /// 物品实体
    /// </summary>
    [ChildOf]
    public class Item: Entity, IAwake, IDestroy, ISerializeToEntity
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
        /// 物品所在背包槽位索引（-1表示未装入背包）
        /// </summary>
        public int SlotIndex;
    }
}