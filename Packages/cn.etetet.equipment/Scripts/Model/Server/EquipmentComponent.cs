using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    /// <summary>
    /// 装备管理组件（挂在Unit上，管理穿戴的装备Item）
    /// ITransfer: 保证 Gate->Map 传送时装备数据不丢失
    /// IDeserialize: 传送后重建 EquippedItems 字典
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class EquipmentComponent: Entity, IAwake, IDestroy, ITransfer, IDeserialize
    {
        /// <summary>
        /// 装备槽位字典（槽位类型 -> 穿戴的Item的EntityRef）
        /// 该字段在 Deserialize 中从子 Item 重建，无需序列化
        /// </summary>
        [BsonIgnore]
        public Dictionary<EquipmentSlotType, EntityRef<Item>> EquippedItems = new();
    }
}