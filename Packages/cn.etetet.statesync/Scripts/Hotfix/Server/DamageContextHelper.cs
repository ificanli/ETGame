using System;

namespace ET.Server
{
    /// <summary>
    /// 统一伤害处理管线。所有伤害路径最终都应调用此 Helper。
    /// Pipeline: BaseDamage → BeforeDamageApply → 致命判定 → 扣血 → AfterDamageApply
    /// </summary>
    public static class DamageContextHelper
    {
        /// <summary>
        /// 通用伤害入口。子弹和 Spell 都走这里。
        /// </summary>
        public static void ApplyDamage(
            Scene scene,
            Unit attacker,
            Unit target,
            long baseDamage,
            int weaponId = 0,
            bool isBullet = false,
            bool isCritical = false)
        {
            if (target == null || target.IsDisposed || scene == null || scene.IsDisposed)
            {
                return;
            }

            NumericComponent targetNumeric = target.NumericComponent;
            if (targetNumeric == null)
            {
                return;
            }

            // 目标已死亡，不再处理伤害
            long preHp = targetNumeric.GetAsLong(NumericType.HP);
            if (preHp <= 0)
            {
                return;
            }

            DamageContext ctx = new DamageContext();
            ctx.SourceUnitId = attacker?.Id ?? 0;
            ctx.TargetUnitId = target.Id;
            ctx.WeaponId = weaponId;
            ctx.IsBullet = isBullet;
            ctx.IsBoss = MonsterRuntimeProfileHelper.IsBoss(target);
            ctx.IsCritical = isCritical;
            ctx.BaseDamage = baseDamage;
            ctx.FinalDamage = baseDamage;
            ctx.CanDie = true;

            // Step 1-3: 发布 BeforeDamageApply，订阅者可修改 FinalDamage / CanDie
            EventSystem.Instance.Publish(scene, new RogueBeforeDamageApply { Context = ctx });

            // 确保 FinalDamage 不为负
            if (ctx.FinalDamage < 0)
            {
                ctx.FinalDamage = 0;
            }

            // Step 4: 致命判定 + 实际扣血
            // 先快照目标信息，避免扣血后目标被移除导致类型丢失（影响击杀经验/任务结算）。
            long targetId = target.Id;
            int targetUnitType = (int)target.UnitType;

            long currentHp = targetNumeric.GetAsLong(NumericType.HP);
            long newHp = currentHp - ctx.FinalDamage;

            if (newHp <= 0 && !ctx.CanDie)
            {
                newHp = 1;
            }

            if (newHp < 0)
            {
                newHp = 0;
            }

            ctx.ActualDamage = currentHp - newHp;
            ctx.TargetKilled = newHp <= 0;

            // Step 5: 写入 HP
            targetNumeric.Set(NumericType.HP, newHp);

            // Step 6: 发布 AfterDamageApply（吸血、击杀奖励等）
            EventSystem.Instance.Publish(scene, new RogueAfterDamageApply { Context = ctx });

            // Step 7: 死亡事件
            if (ctx.TargetKilled)
            {
                UnitDie unitDie = new UnitDie
                {
                    TargetId = targetId,
                    TargetUnitType = targetUnitType,
                };

                if (attacker == null || attacker.IsDisposed || attacker.InstanceId == 0)
                {
                    return;
                }

                try
                {
                    unitDie.Unit = attacker;
                }
                catch (Exception e)
                {
                    Log.Warning($"[Damage] skip UnitDie publish: invalid attacker ref, attackerId={attacker.Id}, targetId={targetId}, error={e.Message}");
                    return;
                }

                // NumericType.HP 写入后目标可能被同步移除，避免对已销毁实体创建 EntityRef。
                if (target != null && !target.IsDisposed && target.InstanceId != 0)
                {
                    try
                    {
                        unitDie.Target = target;
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"[Damage] ignore UnitDie.Target ref: targetId={targetId}, error={e.Message}");
                    }
                }

                EventSystem.Instance.Publish(scene, unitDie);
            }
        }
    }
}
