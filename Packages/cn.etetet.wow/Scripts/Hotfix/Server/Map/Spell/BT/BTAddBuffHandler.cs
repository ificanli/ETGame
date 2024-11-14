namespace ET.Server
{
    public class BTAddBuffHandler: ABTHandler<BTAddBuff>
    {
        protected override bool Run(Effect effect, BTAddBuff btConfig)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.ServerSpellAdd:
                {
                    Spell spell = effect.GetParent<Spell>();
                    Unit caster = spell.Caster;
                    caster.GetComponent<BuffComponent>().CreateBuff(btConfig.BuffConfig);
                    break;
                }
                case EffectTimeType.ServerSpellHit:
                {
                    Spell spell = effect.GetParent<Spell>();
                    SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
                    foreach (Unit unit in spellTargetComponent.Units)
                    {
                        unit.GetComponent<BuffComponent>().CreateBuff(btConfig.BuffConfig);
                    }
                    break;
                }
            }
            return true;
        }
    }
}