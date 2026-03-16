namespace ET
{
    public class AI_RobotCombatCheck: BTCondition
    {
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float MaxRange = 18f;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int SelectIntervalMs = 300;
    }
}
