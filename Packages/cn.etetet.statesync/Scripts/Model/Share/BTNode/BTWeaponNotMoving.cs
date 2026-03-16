namespace ET
{
    /// <summary>
    /// 条件节点：检查单位是否静止（某些武器需要静止才能射击）
    /// </summary>
    public class BTWeaponNotMoving : BTCondition
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
    }
}
