namespace ET
{
    public class AI_RobotWeaponCombat : BTCoroutine
    {
        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int ThinkIntervalMs = 200;

        // 保留旧字段，兼容从施法机器人BT复制出来的序列化数据。
        [Sirenix.OdinInspector.BoxGroup("兼容字段")]
        public int PreCastSpellId = 0;

        [Sirenix.OdinInspector.BoxGroup("兼容字段")]
        public int MainSpellId = 0;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float PreferredMinDistance = 5f;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float PreferredMaxDistance = 8f;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float StopMoveTolerance = 1f;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float RetreatDistance = 3f;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float RetreatStepDistance = 4f;
    }
}
