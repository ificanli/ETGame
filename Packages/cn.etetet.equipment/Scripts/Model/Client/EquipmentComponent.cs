using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端装备组件
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class EquipmentComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 装备槽位字典（槽位类型 -> 装备EntityRef）
        /// </summary>
        public Dictionary<EquipmentSlotType, EntityRef<Item>> EquippedItems = new();
    }
}