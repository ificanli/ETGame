using System.Collections.Generic;

namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public static class DamageHelper
    {
        public static void Damage(Unit attacker, Unit target, Buff damageBuff, int value)
        {
            // 计算命中
            
            NumericComponent numericComponent = target.GetComponent<NumericComponent>();
            long hp = numericComponent.Get(NumericType.HP);
            if (hp < value)
            {
                value = (int) hp;
            }
            long v = hp - value;
            
            // spell mod
            SpellComponent spellComponent = attacker.GetComponent<SpellComponent>();
            int spellConfigId = damageBuff.GetSpellConfigId();
            int damagePct = spellComponent.GetMod(spellConfigId, SpellModType.SPELLMOD_DAMAGE);
            v = (int)(v * (100 + damagePct) / 100f);
            
            // 计算护甲
            
            // 计算抗性
            
            // 计算暴击
            
            numericComponent.Set(NumericType.HP, v);

            // 加上仇恨
            ThreatComponent threatComponent = target.GetComponent<ThreatComponent>();
            if (threatComponent != null)
            {
                int threat = value;
                // 计算仇恨Mod
                int threatPct = spellComponent.GetMod(spellConfigId, SpellModType.SPELLMOD_THREAT);
                threat = (int)(threat * (100 + threatPct) / 100f);
                threatComponent.AddThreat(attacker, threat);
            }

            // 触发被攻击Effect
            using ListComponent<Buff> hittedBuffs = ListComponent<Buff>.Create();
            target.GetComponent<BuffComponent>().GetByEffectType<EffectServerBuffHitted>(hittedBuffs);

            foreach (Buff buff in hittedBuffs)
            {
                if (buff == null)
                {
                    Log.Error("hitted buff is null");
                    continue;
                }

                EffectServerBuffHitted effect = buff.GetConfig().GetEffect<EffectServerBuffHitted>();
                if (effect != null)
                {
                    using BTEnv env = BTEnv.Create(attacker.Scene());
                    env.AddEntity(effect.Attacker, attacker);
                    env.AddEntity(effect.Unit, target);
                    env.AddEntity(effect.Buff, buff);
                    BTDispatcher.Instance.Handle(effect, env);
                }
            }
        }
    }
}