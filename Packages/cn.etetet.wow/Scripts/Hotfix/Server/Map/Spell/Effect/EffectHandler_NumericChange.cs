namespace ET.Server
{
    [Invoke(EffectType.NumericChange)]
    public class EffectHandler_NumericChange: AInvokeHandler<Effect>
    {
        public override void Handle(Effect effect)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.SpellHit:
                {
                    Spell spell = effect.Owner as Spell;
                    int damage = effect.Config.Params[0];
                    
                    break;
                }
                    
                case EffectTimeType.BuffAdd:
                    break;
                case EffectTimeType.BuffRemove:
                    break;
                case EffectTimeType.BuffTick:
                    break;
            }
        }
    }
}