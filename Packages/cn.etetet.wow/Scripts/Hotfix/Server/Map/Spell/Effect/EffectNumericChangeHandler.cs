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
                    Spell spell = effect.Source as Spell;
                    SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
                    foreach (Unit unit in spellTargetComponent.Units)
                    {
                        NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
                        long value = numericComponent.GetAsLong(effectConfig.NumericType);
                        numericComponent.Set(effectConfig.NumericType, value + effectConfig.Value);
                    }
                    break;
                }
            }
        }
    }
}