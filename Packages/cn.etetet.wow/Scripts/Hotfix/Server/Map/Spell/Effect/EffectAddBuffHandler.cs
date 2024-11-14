namespace ET.Server
{
    [EffectHandler(SceneType.Map)]
    public class EffectAddBuffHandler: AEffectHandler<EffectAddBuff>
    {
        protected override void Run(Effect effect, EffectAddBuff effectConfig)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.ServerSpellAdd:
                {
                    Spell spell = effect.GetParent<Spell>();
                    Unit caster = spell.Caster;
                    caster.GetComponent<BuffComponent>().CreateBuff(effectConfig.BuffConfig);
                    break;
                }
                case EffectTimeType.ServerSpellHit:
                {
                    Spell spell = effect.GetParent<Spell>();
                    SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
                    foreach (Unit unit in spellTargetComponent.Units)
                    {
                        unit.GetComponent<BuffComponent>().CreateBuff(effectConfig.BuffConfig);
                    }
                    break;
                }
            }
        }
    }
}