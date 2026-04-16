using System.Collections.Generic;

namespace ET.Server
{
    public static class DamageHelper
    {
        public static void Damage(Unit attacker, Unit target, Buff damageBuff, int value)
        {
            long targetId = target.Id;
            int targetUnitType = (int)target.UnitType;

            // 计算命中

            NumericComponent numericComponent = target.NumericComponent;
            long hp = numericComponent.Get(NumericType.HP);

            // spell mod
            SpellComponent spellComponent = attacker.GetComponent<SpellComponent>();
            int spellConfigId = damageBuff.GetSpellConfigId();

            // 攻击力 × 伤害系数；BTDamage.Value > 0 时使用固定值（向后兼容）
            int finalValue;
            if (value > 0)
            {
                finalValue = value;
            }
            else
            {
                long attack = attacker.NumericComponent?.GetAsLong(NumericType.Attack) ?? 0;
                SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);
                int damageMultiplier = spellConfig?.DamageMultiplier ?? 0;
                finalValue = (int)(attack * damageMultiplier / 100);
            }

            if (hp < finalValue)
            {
                finalValue = (int)hp;
            }
            long v = hp - finalValue;

            int damagePct = spellComponent.GetMod(spellConfigId, SpellModType.SPELLMOD_DAMAGE);
            v = (int)(v * (100 + damagePct) / 100f);

            // 计算护甲

            // 计算抗性

            // 计算暴击

            if (v < 0)
            {
                v = 0;
            }

            long actualDamage = hp - v;
            if (actualDamage < 0) actualDamage = 0;

            // 发布 BeforeSpellDamageApply，允许外部系统（如 DamageContext）拦截
            SpellDamageInterceptInfo interceptInfo = new SpellDamageInterceptInfo();
            EventSystem.Instance.Publish(target.Scene(), new BeforeSpellDamageApply
            {
                Attacker = attacker,
                Target = target,
                FinalDamage = actualDamage,
                SpellConfigId = spellConfigId,
                Info = interceptInfo,
            });

            if (!interceptInfo.Intercepted)
            {
                // 未被拦截，走原有逻辑
                numericComponent.Set(NumericType.HP, v);
            }

            // 加上仇恨（无论是否拦截都执行）
            ThreatComponent threatComponent = target.GetComponent<ThreatComponent>();
            if (threatComponent != null)
            {
                int threat = finalValue;
                // 计算仇恨Mod
                int threatPct = spellComponent.GetMod(spellConfigId, SpellModType.SPELLMOD_THREAT);
                threat = (int)(threat * (100 + threatPct) / 100f);
                threatComponent.AddThreat(attacker, threat);
            }

            // 触发被攻击Effect（无论是否拦截都执行）
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
                    using BTEnv env = BTEnv.Create(attacker.Scene(), target.Id);
                    env.AddEntity(effect.Attacker, attacker);
                    env.AddEntity(effect.Unit, target);
                    env.AddEntity(effect.Buff, buff);
                    BTDispatcher.Instance.Handle(effect, env);
                }
            }

            // 死亡（仅在未被拦截时由此处触发，拦截后由 DamageContext 管线处理）
            if (!interceptInfo.Intercepted && v <= 0)
            {
                EventSystem.Instance.Publish(target.Scene(), new UnitDie
                {
                    Unit = attacker,
                    Target = target,
                    TargetId = targetId,
                    TargetUnitType = targetUnitType,
                });
            }
        }
    }
}
