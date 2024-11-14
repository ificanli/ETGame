namespace ET.Client
{
    [EffectHandler(SceneType.Current)]
    public class EffectAnimatorSetIntHandler: AEffectHandler<EffectAnimatorSetInt>
    {
        protected override void Run(Effect effect, EffectAnimatorSetInt effectConfig)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.ClientSpellAdd:
                {
                    Spell spell = effect.GetParent<Spell>();
                    Unit caster = spell.Caster;
                    caster.GetComponent<AnimatorComponent>().SetInt(effectConfig.MotionType.ToString(), effectConfig.Value);
                    break;
                }
                case EffectTimeType.ClientSpellHit:
                {
                    Spell spell = effect.GetParent<Spell>();
                    SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
                    foreach (Unit unit in spellTargetComponent.Units)
                    {
                        if (unit == null)
                        {
                            continue;
                        }
                        unit.GetComponent<AnimatorComponent>().SetTrigger(effectConfig.MotionType.ToString());
                    }
                    break;
                }
            }
        }
    }
}