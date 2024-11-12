namespace ET.Client
{
    [EffectHandler(SceneType.Current)]
    public class EffectAnimatorSetTriggerHandler: AEffectHandler<EffectAnimatorSetTrigger>
    {
        protected override void Run(Effect effect, EffectAnimatorSetTrigger effectConfig)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.ClientSpellAdd:
                {
                    Spell spell = effect.Source as Spell;
                    Unit caster = spell.Caster;
                    caster.GetComponent<AnimatorComponent>().SetTrigger(effectConfig.MotionType.ToString());
                    break;
                }
            }
        }
    }
}