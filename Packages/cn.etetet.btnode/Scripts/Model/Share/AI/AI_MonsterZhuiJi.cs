namespace ET
{
    public class AI_MonsterZhuiJi: BTCoroutine
    {
        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int ThinkIntervalMs = 200;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int PreCastSpellId = 100110;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int MainSpellId = 100100;
    }
}
