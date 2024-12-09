namespace ET.Server
{
    public static class DamageHelper
    {
        public static void Damage(Unit attacker, Unit target, Buff damageBuff, int value)
        {
            NumericComponent numericComponent = target.GetComponent<NumericComponent>();
            long hp = numericComponent.Get(NumericType.HP);
            if (hp < value)
            {
                value = (int) hp;
            }
            long v = hp - value;
            numericComponent.Set(NumericType.HP, v);

            // 加上仇恨
            ThreatComponent threatComponent = target.GetComponent<ThreatComponent>();
            if (threatComponent != null)
            {
                threatComponent.AddThreat(attacker.Id, value);
            }

            var buffs = target.GetComponent<BuffComponent>().GetByEffectType<EffectServerBuffHitted>();
            if (buffs != null)
            {
                using BTEnv env = BTEnv.Create(attacker.Scene());
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
}