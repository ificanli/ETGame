namespace ET
{
    public class AI_BalooCombat: BTCoroutine
    {
        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int ThinkIntervalMs = 260;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int MainSpellId = 0;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int AltSpellId = 0;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int EnrageHpPermille = 450;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int EnrageThinkIntervalMs = 160;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public int EnrageSpeedPct = 135;
    }
}
