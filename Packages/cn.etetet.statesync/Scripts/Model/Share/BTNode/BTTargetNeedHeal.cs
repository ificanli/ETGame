namespace ET
{
    public class BTTargetNeedHeal : BTCondition
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
    }
}
