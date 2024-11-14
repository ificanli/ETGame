namespace ET.Server
{
    [EffectHandler(SceneType.Map)]
    public class EffectNumericChangeHandler: AEffectHandler<EffectNumericChange>
    {
        protected override void Run(Effect effect, EffectNumericChange effectConfig)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.ServerSpellHit:
                {
                    Spell spell = effect.GetParent<Spell>();
                    SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
                    foreach (Unit unit in spellTargetComponent.Units)
                    {
                        NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
                        long value = numericComponent.GetAsLong(effectConfig.NumericType);
                        numericComponent.Set(effectConfig.NumericType, value + effectConfig.Value);
                    }
                    break;
                }
                case EffectTimeType.ServerBuffAdd:
                {
                    Buff buff = effect.GetParent<Buff>();
                    Unit unit = buff.Parent.GetParent<Unit>();
                    NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
                    long value = numericComponent.GetAsLong(effectConfig.NumericType);
                    numericComponent.Set(effectConfig.NumericType, value + effectConfig.Value);
                    // buff删除的时候会还原数值
                    BuffChangeNumericRecordComponent buffChangeNumericRecordComponent = 
                            buff.GetComponent<BuffChangeNumericRecordComponent>() ??
                            buff.AddComponent<BuffChangeNumericRecordComponent>();
                    buffChangeNumericRecordComponent.Add(effectConfig.NumericType, effectConfig.Value);
                    break;
                }
            }
        }
    }
}