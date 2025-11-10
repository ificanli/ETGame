using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 装备管理组件（挂在Unit上，管理穿戴的装备Item）
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class EquipmentComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 装备槽位字典（槽位类型 -> 穿戴的Item的EntityRef）
        /// </summary>
        public Dictionary<EquipmentSlotType, EntityRef<Item>> EquippedItems = new();
    }
}