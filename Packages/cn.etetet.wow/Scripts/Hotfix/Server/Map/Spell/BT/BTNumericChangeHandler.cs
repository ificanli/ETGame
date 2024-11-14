namespace ET.Server
{
    public class BTNumericChangeHandler: ABTHandler<BTNumericChange>
    {
        protected override bool Run(Effect effect, BTNumericChange btConfig)
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
                        long value = numericComponent.GetAsLong(btConfig.NumericType);
                        numericComponent.Set(btConfig.NumericType, value + btConfig.Value);
                    }
                    break;
                }
                case EffectTimeType.ServerBuffAdd:
                {
                    Buff buff = effect.GetParent<Buff>();
                    Unit unit = buff.Parent.GetParent<Unit>();
                    NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
                    long value = numericComponent.GetAsLong(btConfig.NumericType);
                    numericComponent.Set(btConfig.NumericType, value + btConfig.Value);
                    // buff删除的时候会还原数值
                    BuffChangeNumericRecordComponent buffChangeNumericRecordComponent = 
                            buff.GetComponent<BuffChangeNumericRecordComponent>() ??
                            buff.AddComponent<BuffChangeNumericRecordComponent>();
                    buffChangeNumericRecordComponent.Add(btConfig.NumericType, btConfig.Value);
                    break;
                }
            }
            return true;
        }
    }
}