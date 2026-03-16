namespace ET
{
    /// <summary>
    /// 条件节点：检查指定武器是否可以射击（有弹药、不在换弹、间隔足够）
    /// </summary>
    public class BTWeaponCanFire : BTCondition
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;

        /// <summary>武器槽位索引（1=Slot1, 2=Slot2）</summary>
        public int SlotIndex = 1;
    }
}
