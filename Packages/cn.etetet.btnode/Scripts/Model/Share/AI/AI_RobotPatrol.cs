namespace ET
{
    public class AI_RobotPatrol: BTCoroutine
    {
        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float MinRadius = 2f;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float MaxRadius = 8f;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int IdleMinMs = 1000;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int IdleMaxMs = 3000;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int ExitCombatBuffConfigId = 200111;
    }
}
