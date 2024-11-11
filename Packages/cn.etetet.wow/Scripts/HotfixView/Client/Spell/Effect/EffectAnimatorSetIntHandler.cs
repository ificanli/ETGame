namespace ET.Client
{
    [EffectHandler(SceneType.Current)]
    public class EffectAnimatorSetIntHandler: AEffectHandler<EffectAnimatorSetInt>
    {
        protected override void Run(Effect effect, EffectAnimatorSetInt effectConfig)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.ServerSpellAdd:
                {
                    Spell spell = effect.Source as Spell;
                    Unit caster = spell.Caster;
                    caster.GetComponent<AnimatorComponent>().SetInt(effectConfig.MotionType.ToString(), effectConfig.Value);
                    break;
                }
            }
        }
    }
}