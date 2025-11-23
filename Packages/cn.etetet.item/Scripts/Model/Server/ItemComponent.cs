using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    /// <summary>
    /// 物品背包组件
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class ItemComponent: Entity, IAwake, IDestroy, IDeserialize, ITransfer
    {
        /// <summary>
        /// 背包容量
        /// </summary>
        public int Capacity;
        
        /// <summary>
        /// 背包槽位列表（索引对应槽位号，值为物品EntityRef）
        /// </summary>
        [BsonIgnore]
        public List<EntityRef<Item>> SlotItems = new();
    }
}
