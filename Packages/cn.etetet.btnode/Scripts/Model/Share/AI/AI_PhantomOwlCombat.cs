namespace ET
{
    public class AI_PhantomOwlCombat: BTCoroutine
    {
        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int ThinkIntervalMs = 180;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int MainSpellId = 0;
    }
}
