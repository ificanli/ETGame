namespace ET
{
    public class AI_RobotReturn: BTCoroutine
    {
        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int WaitIntervalMs = 1000;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int ExitCombatBuffConfigId = 200111;
    }
}
