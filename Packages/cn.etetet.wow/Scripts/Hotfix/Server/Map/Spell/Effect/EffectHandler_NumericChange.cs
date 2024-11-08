namespace ET.Server
{
    [Invoke(EffectType.NumericChange)]
    public class EffectHandler_NumericChange: AInvokeHandler<Effect>
    {
        public override void Handle(Effect effect)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.ServerSpellHit:
                {
                    Spell spell = effect.Owner as Spell;
                    int damage = effect.Config.Params[0];
                    
                    break;
                }
                    
                case EffectTimeType.ServerBuffAdd:
                    break;
                case EffectTimeType.ServerBuffRemove:
                    break;
                case EffectTimeType.ServerBuffTick:
                    break;
            }
        }
    }
}