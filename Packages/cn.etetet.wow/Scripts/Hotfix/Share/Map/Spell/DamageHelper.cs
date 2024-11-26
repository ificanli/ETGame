namespace ET
{
    public static class DamageHelper
    {
        public static void Damage(Unit attacker, Unit target, int value)
        {
            NumericComponent numericComponent = target.GetComponent<NumericComponent>();
            long hp = numericComponent.Get(NumericType.HP);
            if (hp < value)
            {
                value = (int) hp;
            }
            numericComponent.Set(NumericType.HP, value);

            var buffs = target.GetComponent<BuffComponent>().GetByEffectType<EffectServerBuffHitted>();
            using BTEnv env = BTEnv.Create();
            env.AddEntity(BTEvnKey.Attacker, attacker);
            env.AddEntity(BTEvnKey.Target, target);
            foreach (Buff buff in buffs)
            {
                if (buff == null)
                {
                    continue;
                }
                
                env.AddEntity(BTEvnKey.Buff, buff);

                EffectServerBuffHitted effect = buff.GetConfig().GetEffect<EffectServerBuffHitted>();
                BTDispatcher.Instance.Handle(effect, env);
            }
        }
    }
}