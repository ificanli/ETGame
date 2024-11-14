namespace ET.Client
{
    public class IbtAnimatorSetIntHandler: ABTHandler<BTAnimatorSetInt>
    {
        protected override bool Run(Effect effect, BTAnimatorSetInt node)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.ClientSpellAdd:
                {
                    Spell spell = effect.GetParent<Spell>();
                    Unit caster = spell.Caster;
                    caster.GetComponent<AnimatorComponent>().SetInt(node.MotionType.ToString(), node.Value);
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
                        unit.GetComponent<AnimatorComponent>().SetTrigger(node.MotionType.ToString());
                    }
                    break;
                }
            }
            return true;
        }
    }
}