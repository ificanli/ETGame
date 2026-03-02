namespace ET
{
    /// <summary>
    /// 条件节点：检查目标是否在武器攻击范围内
    /// </summary>
    public class BTWeaponInRange : BTCondition
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;

        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;

        /// <summary>攻击范围（米）</summary>
        public float Range = 10f;
    }
}
