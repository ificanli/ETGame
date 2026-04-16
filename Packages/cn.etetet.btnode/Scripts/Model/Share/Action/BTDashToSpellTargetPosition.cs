namespace ET
{
    public class BTDashToSpellTargetPosition: BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;

        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;

        public float MaxDistance = 3f;

        public float MinStopDistance = 0.35f;

        public int DurationMs = 400;

        public int TurnTimeMs = 80;
    }
}
