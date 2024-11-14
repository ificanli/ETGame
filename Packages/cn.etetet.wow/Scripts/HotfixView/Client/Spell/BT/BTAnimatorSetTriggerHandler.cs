namespace ET.Client
{
    public class BTAnimatorSetTriggerHandler: ABTHandler<BTAnimatorSetTrigger>
    {
        protected override bool Run(Effect effect, BTAnimatorSetTrigger node)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.ClientSpellAdd:
                {
                    Spell spell = effect.GetParent<Spell>();
                    Unit caster = spell.Caster;
                    caster.GetComponent<AnimatorComponent>().SetTrigger(node.MotionType.ToString());
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