namespace ET
{
    public class AI_BladeCatCombat: BTCoroutine
    {
        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int ThinkIntervalMs = 200;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int MainSpellId = 0;
    }
}
