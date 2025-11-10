namespace ET.Server
{
    /// <summary>
    /// 装备物品组件（挂在Item上，标记该Item为装备）
    /// Item.SlotIndex用于表示装备槽位类型
    /// 穿戴状态通过EquipmentComponent是否包含该Item来判断
    /// </summary>
    [ComponentOf(typeof(Item))]
    public class EquipmentItemComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 装备强化等级
        /// </summary>
        public int EnhanceLevel;
    }
}
