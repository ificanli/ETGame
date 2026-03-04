namespace ET
{
    /// <summary>
    /// 动作节点：执行武器射击，消耗弹药并创建子弹
    /// </summary>
    public class BTWeaponFire : BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;

        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;

        /// <summary>武器槽位索引（1=Slot1, 2=Slot2）</summary>
        public int SlotIndex = 1;
    }
}
