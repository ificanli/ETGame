namespace ET
{
    public class AI_MonsterXunLuo: BTCoroutine
    {
        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float PatrolMinRadius = 0f;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float PatrolMaxRadius = 0f;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float AggroRange = 0f;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int IdleMinMs = 1000;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int IdleMaxMs = 4000;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int ExitCombatBuffConfigId = 200111;
    }
}
