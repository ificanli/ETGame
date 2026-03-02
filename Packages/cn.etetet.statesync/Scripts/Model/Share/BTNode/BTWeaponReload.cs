namespace ET
{
    /// <summary>
    /// 动作节点：执行换弹，设置换弹状态并补充弹药
    /// </summary>
    public class BTWeaponReload : BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;

        /// <summary>武器类型</summary>
        public WeaponType WeaponType = WeaponType.Rifle;
    }
}
