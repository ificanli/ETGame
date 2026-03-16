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

        /// <summary>
        /// 物品占据的格子宽
        /// </summary>
        public int GridWidth;

        /// <summary>
        /// 物品占据的格子高
        /// </summary>
        public int GridHeight;
    }
}
