namespace ET
{
    /// <summary>
    /// 条件节点：检查Unit是否有有效目标
    /// 成功时将目标Unit写入env供后续节点使用
    /// </summary>
    public class BTWeaponHasTarget : BTCondition
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;

        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Target;
    }
}
