namespace ET.Server
{
    /// <summary>
    /// 伤害后处理：更新战斗状态 + 吸血效果。
    /// </summary>
    [Event(SceneType.Map)]
    public class RogueAfterDamageApply_UpdateCombatState : AEvent<Scene, RogueAfterDamageApply>
    {
        protected override async ETTask Run(Scene scene, RogueAfterDamageApply a)
        {
            DamageContext ctx = a.Context;
            if (ctx == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            // 攻击方进入战斗
            Unit attacker = null;
            if (ctx.SourceUnitId > 0)
            {
                attacker = unitComponent.Get(ctx.SourceUnitId);
                CombatStateComponent attackerCombat = GetOrAddCombatState(attacker);
                attackerCombat?.OnDealDamage();
            }

            // 防御方进入战斗
            if (ctx.TargetUnitId > 0)
            {
                Unit target = unitComponent.Get(ctx.TargetUnitId);
                CombatStateComponent targetCombat = GetOrAddCombatState(target);
                targetCombat?.OnTakeDamage();
            }

            // 吸血
            if (ctx.LifeStealPermille > 0 && ctx.ActualDamage > 0 && attacker != null && !attacker.IsDisposed)
            {
                NumericComponent attackerNumeric = attacker.NumericComponent;
                if (attackerNumeric != null)
                {
                    long healAmount = ctx.ActualDamage * ctx.LifeStealPermille / 1000;
                    if (healAmount > 0)
                    {
                        long currentHp = attackerNumeric.GetAsLong(NumericType.HP);
                        long maxHp = attackerNumeric.GetAsLong(NumericType.MaxHP);
                        long newHp = currentHp + healAmount;
                        if (newHp > maxHp) newHp = maxHp;
                        attackerNumeric.Set(NumericType.HP, newHp);
                    }
                }
            }

            await ETTask.CompletedTask;
        }

        private static CombatStateComponent GetOrAddCombatState(Unit unit)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                return null;
            }

            CombatStateComponent combat = unit.GetComponent<CombatStateComponent>();
            if (combat == null)
            {
                combat = unit.AddComponent<CombatStateComponent>();
            }

            return combat;
        }
    }
}
