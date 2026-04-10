namespace ET
{
    public class BTDamageSpellTargetCircle: BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;

        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;

        public UnitType UnitType;

        public float Radius;

        public int Value;
    }
}
