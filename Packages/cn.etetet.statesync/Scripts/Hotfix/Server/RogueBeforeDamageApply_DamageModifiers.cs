namespace ET.Server
{
    /// <summary>
    /// 伤害修正：金币转伤害、低血量额外伤害、固定减伤、秒杀概率、吸血。
    /// 在 BeforeDamageApply 阶段修改 FinalDamage。
    /// </summary>
    [Event(SceneType.Map)]
    public class RogueBeforeDamageApply_DamageModifiers : AEvent<Scene, RogueBeforeDamageApply>
    {
        private const int DamageReductionSourceMonster = 1;
        private const int CriticalDamageBonusPermille = 1000;

        protected override async ETTask Run(Scene scene, RogueBeforeDamageApply a)
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

            Unit attacker = unitComponent.Get(ctx.SourceUnitId);
            Unit target = unitComponent.Get(ctx.TargetUnitId);

            // 攻击者增伤
            if (attacker != null && !attacker.IsDisposed && attacker.UnitType == UnitType.Player)
            {
                ApplyAttackerModifiers(attacker, target, ctx);
            }

            // 目标减伤
            if (target != null && !target.IsDisposed && target.UnitType == UnitType.Player)
            {
                ApplyTargetModifiers(target, attacker, ctx);
            }

            await ETTask.CompletedTask;
        }

        public static void ApplyAttackerModifiers(Unit attacker, Unit target, DamageContext ctx)
        {
            RogueBuffPassiveRuntimeComponent passiveRuntime = attacker.GetComponent<RogueBuffPassiveRuntimeComponent>();
            if (passiveRuntime == null)
            {
                return;
            }

            long bonusDamage = 0;

            RogueProgressComponent progress = attacker.GetComponent<RogueProgressComponent>();
            if (progress != null && progress.CurrentGold > 0)
            {
                foreach (int goldPerOnePercent in passiveRuntime.GoldDamagePerOnePercentBySource.Values)
                {
                    if (goldPerOnePercent <= 0)
                    {
                        continue;
                    }

                    int bonusPct = progress.CurrentGold / goldPerOnePercent;
                    if (bonusPct > 0)
                    {
                        bonusDamage += ctx.FinalDamage * bonusPct / 100;
                    }
                }
            }

            if (target != null && !target.IsDisposed)
            {
                NumericComponent targetNumeric = target.NumericComponent;
                long targetHp = targetNumeric?.GetAsLong(NumericType.HP) ?? 0;
                long targetMaxHp = targetNumeric?.GetAsLong(NumericType.MaxHP) ?? 0;
                if (targetMaxHp > 0)
                {
                    int hpPermille = (int)(targetHp * 1000 / targetMaxHp);
                    foreach (RogueLowHpDamageSourceData source in passiveRuntime.LowHpDamageBySource.Values)
                    {
                        if (source.HpThresholdPermille <= 0 || source.DamageBonusPermille <= 0)
                        {
                            continue;
                        }

                        if (hpPermille < source.HpThresholdPermille)
                        {
                            bonusDamage += ctx.FinalDamage * source.DamageBonusPermille / 1000;
                        }
                    }
                }
            }

            if (target != null && !target.IsDisposed && target.UnitType == UnitType.Monster)
            {
                int monsterDamageBonusPermille = RogueEffectQueryHelper.GetMonsterDamageBonusPermille(attacker);
                if (monsterDamageBonusPermille > 0)
                {
                    bonusDamage += ctx.FinalDamage * monsterDamageBonusPermille / 1000;
                }
            }

            if (target != null && !target.IsDisposed)
            {
                int maxSizeDifferenceBonusPermille = RogueEffectQueryHelper.GetSizeDifferenceDamageBonusPermille(attacker);
                if (maxSizeDifferenceBonusPermille > 0)
                {
                    int attackerScalePermille = RogueEffectQueryHelper.GetScaleModifierPermille(attacker);
                    int targetScalePermille = RogueEffectQueryHelper.GetScaleModifierPermille(target);
                    int sizeDifferencePermille = attackerScalePermille - targetScalePermille;
                    if (sizeDifferencePermille < 0)
                    {
                        sizeDifferencePermille = -sizeDifferencePermille;
                    }

                    int appliedBonusPermille = sizeDifferencePermille > maxSizeDifferenceBonusPermille
                        ? maxSizeDifferenceBonusPermille
                        : sizeDifferencePermille;
                    if (appliedBonusPermille > 0)
                    {
                        bonusDamage += ctx.FinalDamage * appliedBonusPermille / 1000;
                    }
                }
            }

            if (ctx.IsBullet)
            {
                int critChancePermille = RogueEffectQueryHelper.GetHitHeroCritPermille(attacker);
                if (critChancePermille > 1000)
                {
                    critChancePermille = 1000;
                }

                if (critChancePermille > 0 && RandomGenerator.RandomNumber(0, 1000) < critChancePermille)
                {
                    ctx.IsCritical = true;
                    bonusDamage += ctx.FinalDamage * CriticalDamageBonusPermille / 1000;
                }
            }

            if (target != null && !target.IsDisposed && target.UnitType == UnitType.Monster)
            {
                int probabilityMultiplier = RogueEffectQueryHelper.GetProbabilityMultiplierPermille(attacker);
                foreach (RogueInstantKillSourceData source in passiveRuntime.InstantKillBySource.Values)
                {
                    if (source.ChancePermille <= 0)
                    {
                        continue;
                    }

                    int chancePermille = source.ChancePermille;
                    if (probabilityMultiplier > 0 && probabilityMultiplier != 1000)
                    {
                        chancePermille = chancePermille * probabilityMultiplier / 1000;
                    }

                    if (chancePermille > 1000)
                    {
                        chancePermille = 1000;
                    }

                    int roll = RandomGenerator.RandomNumber(0, 1000);
                    if (roll >= chancePermille)
                    {
                        continue;
                    }

                    NumericComponent targetNumeric = target.NumericComponent;
                    if (targetNumeric == null)
                    {
                        continue;
                    }

                    ctx.FinalDamage = targetNumeric.GetAsLong(NumericType.HP);
                    if (source.GoldReward > 0)
                    {
                        if (progress != null)
                        {
                            RogueGoldHelper.AddGold(progress, source.GoldReward);
                            RogueProgressHelper.SyncProgress(attacker, progress);
                        }
                    }

                    Log.Info($"[RogueInstantKill] triggered, attackerId={attacker.Id}, targetId={target.Id}, gold={source.GoldReward}");
                }
            }

            int lifeStealPermille = passiveRuntime.GetLifeStealPermille();
            if (lifeStealPermille > 0)
            {
                ctx.LifeStealPermille += lifeStealPermille;
            }

            foreach (RogueOnKillStackSourceData stackState in passiveRuntime.OnKillStackBySource.Values)
            {
                if (stackState.BonusPermillePerStack <= 0 || stackState.CurrentStacks <= 0)
                {
                    continue;
                }

                bonusDamage += ctx.FinalDamage * stackState.CurrentStacks * stackState.BonusPermillePerStack / 1000;
            }

            if (bonusDamage > 0)
            {
                ctx.FinalDamage += bonusDamage;
            }
        }

        public static void ApplyTargetModifiers(Unit target, Unit attacker, DamageContext ctx)
        {
            RogueBuffPassiveRuntimeComponent passiveRuntime = target.GetComponent<RogueBuffPassiveRuntimeComponent>();
            if (passiveRuntime == null)
            {
                return;
            }

            long totalReduction = 0;
            foreach (RogueDamageReductionSourceData source in passiveRuntime.DamageReductionBySource.Values)
            {
                if (source.DamageReduction <= 0)
                {
                    continue;
                }

                if (!MatchesDamageReductionFilter(source.SourceFilter, attacker))
                {
                    continue;
                }

                if (source.DamageReduction <= 1000)
                {
                    // 1~1000 按千分比减伤
                    totalReduction += ctx.FinalDamage * source.DamageReduction / 1000;
                }
                else
                {
                    // >1000 按固定值减伤
                    totalReduction += source.DamageReduction;
                }
            }

            if (totalReduction > 0)
            {
                ctx.FinalDamage -= totalReduction;
                if (ctx.FinalDamage < 0)
                {
                    ctx.FinalDamage = 0;
                }
            }

            if (ctx.FinalDamage <= 0)
            {
                return;
            }

            NumericComponent numeric = target.NumericComponent;
            if (numeric == null)
            {
                return;
            }

            using ListComponent<long> shieldSourceIds = ListComponent<long>.Create();
            foreach (long sourceId in passiveRuntime.LowHpShieldBySource.Keys)
            {
                shieldSourceIds.Add(sourceId);
            }

            foreach (long sourceId in shieldSourceIds)
            {
                if (!passiveRuntime.LowHpShieldBySource.TryGetValue(sourceId, out RogueLowHpShieldSourceData shieldState))
                {
                    continue;
                }

                if (shieldState.TriggerHpPermille <= 0 || shieldState.ShieldMaxHpPermille <= 0)
                {
                    continue;
                }

                if (!shieldState.Triggered)
                {
                    long hp = numeric.GetAsLong(NumericType.HP);
                    long maxHp = numeric.GetAsLong(NumericType.MaxHP);
                    if (maxHp <= 0)
                    {
                        continue;
                    }

                    int hpPermille = (int)(hp * 1000 / maxHp);
                    if (hpPermille <= shieldState.TriggerHpPermille)
                    {
                        shieldState.Triggered = true;
                        shieldState.RemainingShieldValue = maxHp * shieldState.ShieldMaxHpPermille / 1000;
                    }
                }

                if (shieldState.RemainingShieldValue <= 0 || ctx.FinalDamage <= 0)
                {
                    passiveRuntime.LowHpShieldBySource[sourceId] = shieldState;
                    continue;
                }

                long absorb = ctx.FinalDamage <= shieldState.RemainingShieldValue ? ctx.FinalDamage : shieldState.RemainingShieldValue;
                ctx.FinalDamage -= absorb;
                shieldState.RemainingShieldValue -= absorb;
                passiveRuntime.LowHpShieldBySource[sourceId] = shieldState;
                if (ctx.FinalDamage <= 0)
                {
                    break;
                }
            }
        }

        private static bool MatchesDamageReductionFilter(int sourceFilter, Unit attacker)
        {
            if (sourceFilter <= 0)
            {
                return true;
            }

            return sourceFilter switch
            {
                DamageReductionSourceMonster => attacker != null && !attacker.IsDisposed && attacker.UnitType == UnitType.Monster,
                _ => true,
            };
        }
    }
}
