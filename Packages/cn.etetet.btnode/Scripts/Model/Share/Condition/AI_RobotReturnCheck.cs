namespace ET
{
    public class AI_RobotReturnCheck: BTCondition
    {
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float MaxDistance = 35f;
    }
}
