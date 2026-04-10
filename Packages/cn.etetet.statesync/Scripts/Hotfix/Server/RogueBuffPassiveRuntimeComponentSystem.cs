namespace ET.Server
{
    [EntitySystemOf(typeof(RogueBuffPassiveRuntimeComponent))]
    public static partial class RogueBuffPassiveRuntimeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueBuffPassiveRuntimeComponent self)
        {
            self.Reset();
        }

        [EntitySystem]
        private static void Destroy(this RogueBuffPassiveRuntimeComponent self)
        {
            self.Reset();
        }

        private static void Reset(this RogueBuffPassiveRuntimeComponent self)
        {
            self.KillGoldBonusBySource.Clear();
            self.ContainerLowQualityGoldBySource.Clear();
            self.GoldDamagePerOnePercentBySource.Clear();
            self.MonsterDamageBonusPermilleBySource.Clear();
            self.AreaDiscoveryGoldBySource.Clear();
            self.ProbabilityMultiplierBySource.Clear();
            self.ContainerResultProbabilityMultiplierBySource.Clear();
            self.SearchMonsterProbabilityMultiplierBySource.Clear();
            self.LifeStealPermilleBySource.Clear();
            self.SizeDifferenceDamageBonusBySource.Clear();
            self.OnKillHealPermilleBySource.Clear();

            self.LowHpDamageBySource.Clear();
            self.DamageReductionBySource.Clear();
            self.InstantKillBySource.Clear();
            self.OnKillStackBySource.Clear();
            self.LowHpShieldBySource.Clear();
            self.ExtendGameTimeBySourceMs.Clear();
            self.AfterSkillSpeedBoostBySource.Clear();

            self.FatalImmunitySources.Clear();
            self.SkillDisableSources.Clear();
            self.OutOfCombatStealthSources.Clear();
        }
    }

    public static class RogueBuffPassiveRuntimeHelper
    {
        public static void ApplyFromBuff(this RogueBuffPassiveRuntimeComponent self, Buff buff)
        {
            if (self == null || self.IsDisposed || buff == null || buff.IsDisposed)
            {
                return;
            }

            BuffConfig config = buff.GetConfig();
            if (config == null)
            {
                return;
            }

            long sourceId = buff.Id;

            EffectRogueKillGoldBonus killGold = config.GetEffect<EffectRogueKillGoldBonus>();
            if (killGold != null && killGold.Gold > 0)
            {
                self.KillGoldBonusBySource[sourceId] = killGold.Gold;
            }
            else
            {
                self.KillGoldBonusBySource.Remove(sourceId);
            }

            EffectRogueContainerLowQualityGold containerLowQualityGold = config.GetEffect<EffectRogueContainerLowQualityGold>();
            if (containerLowQualityGold != null && containerLowQualityGold.Gold > 0)
            {
                self.ContainerLowQualityGoldBySource[sourceId] = new RogueContainerLowQualityGoldSourceData
                {
                    Gold = containerLowQualityGold.Gold,
                    HighQualityThreshold = containerLowQualityGold.HighQualityThreshold > 0 ? containerLowQualityGold.HighQualityThreshold : 4,
                };
            }
            else
            {
                self.ContainerLowQualityGoldBySource.Remove(sourceId);
            }

            if (config.GetEffect<EffectRogueFatalImmunity>() != null)
            {
                self.FatalImmunitySources[sourceId] = 1;
            }
            else
            {
                self.FatalImmunitySources.Remove(sourceId);
            }

            EffectRogueGoldDamageBonus goldDamageBonus = config.GetEffect<EffectRogueGoldDamageBonus>();
            if (goldDamageBonus != null && goldDamageBonus.GoldPerOnePercent > 0)
            {
                self.GoldDamagePerOnePercentBySource[sourceId] = goldDamageBonus.GoldPerOnePercent;
            }
            else
            {
                self.GoldDamagePerOnePercentBySource.Remove(sourceId);
            }

            EffectRogueMonsterDamageBonus monsterDamageBonus = config.GetEffect<EffectRogueMonsterDamageBonus>();
            if (monsterDamageBonus != null && monsterDamageBonus.DamageBonusPermille > 0)
            {
                self.MonsterDamageBonusPermilleBySource[sourceId] = monsterDamageBonus.DamageBonusPermille;
            }
            else
            {
                self.MonsterDamageBonusPermilleBySource.Remove(sourceId);
            }

            EffectRogueAreaDiscoveryGold areaDiscoveryGold = config.GetEffect<EffectRogueAreaDiscoveryGold>();
            if (areaDiscoveryGold != null && areaDiscoveryGold.Gold > 0)
            {
                self.AreaDiscoveryGoldBySource[sourceId] = areaDiscoveryGold.Gold;
            }
            else
            {
                self.AreaDiscoveryGoldBySource.Remove(sourceId);
            }

            EffectRogueProbabilityMultiplier probabilityMultiplier = config.GetEffect<EffectRogueProbabilityMultiplier>();
            if (probabilityMultiplier != null && probabilityMultiplier.Permille > 0)
            {
                self.ProbabilityMultiplierBySource[sourceId] = probabilityMultiplier.Permille;
            }
            else
            {
                self.ProbabilityMultiplierBySource.Remove(sourceId);
            }

            EffectRogueContainerResultProbabilityMultiplier containerResultProbability = config.GetEffect<EffectRogueContainerResultProbabilityMultiplier>();
            if (containerResultProbability != null && containerResultProbability.Permille > 0)
            {
                self.ContainerResultProbabilityMultiplierBySource[sourceId] = containerResultProbability.Permille;
            }
            else
            {
                self.ContainerResultProbabilityMultiplierBySource.Remove(sourceId);
            }

            EffectRogueSearchMonsterProbabilityMultiplier searchMonsterProbability = config.GetEffect<EffectRogueSearchMonsterProbabilityMultiplier>();
            if (searchMonsterProbability != null && searchMonsterProbability.Permille > 0)
            {
                self.SearchMonsterProbabilityMultiplierBySource[sourceId] = searchMonsterProbability.Permille;
            }
            else
            {
                self.SearchMonsterProbabilityMultiplierBySource.Remove(sourceId);
            }

            EffectRogueLowHpDamageBonus lowHpDamage = config.GetEffect<EffectRogueLowHpDamageBonus>();
            if (lowHpDamage != null && lowHpDamage.HpThresholdPermille > 0 && lowHpDamage.DamageBonusPermille > 0)
            {
                self.LowHpDamageBySource[sourceId] = new RogueLowHpDamageSourceData
                {
                    HpThresholdPermille = lowHpDamage.HpThresholdPermille,
                    DamageBonusPermille = lowHpDamage.DamageBonusPermille,
                };
            }
            else
            {
                self.LowHpDamageBySource.Remove(sourceId);
            }

            EffectRogueDamageReduction damageReduction = config.GetEffect<EffectRogueDamageReduction>();
            if (damageReduction != null && damageReduction.DamageReduction > 0)
            {
                self.DamageReductionBySource[sourceId] = new RogueDamageReductionSourceData
                {
                    DamageReduction = damageReduction.DamageReduction,
                    SourceFilter = damageReduction.SourceFilter,
                };
            }
            else
            {
                self.DamageReductionBySource.Remove(sourceId);
            }

            EffectRogueLifeSteal lifeSteal = config.GetEffect<EffectRogueLifeSteal>();
            if (lifeSteal != null && lifeSteal.LifeStealPermille > 0)
            {
                self.LifeStealPermilleBySource[sourceId] = new RogueLifeStealSourceData
                {
                    LifeStealPermille = lifeSteal.LifeStealPermille,
                    TargetFilter = lifeSteal.TargetFilter,
                };
            }
            else
            {
                self.LifeStealPermilleBySource.Remove(sourceId);
            }

            EffectRogueSizeDifferenceDamageBonus sizeDifferenceDamageBonus = config.GetEffect<EffectRogueSizeDifferenceDamageBonus>();
            if (sizeDifferenceDamageBonus != null && sizeDifferenceDamageBonus.MaxDamageBonusPermille > 0)
            {
                self.SizeDifferenceDamageBonusBySource[sourceId] = sizeDifferenceDamageBonus.MaxDamageBonusPermille;
            }
            else
            {
                self.SizeDifferenceDamageBonusBySource.Remove(sourceId);
            }

            EffectRogueInstantKillChance instantKill = config.GetEffect<EffectRogueInstantKillChance>();
            if (instantKill != null && instantKill.ChancePermille > 0)
            {
                self.InstantKillBySource[sourceId] = new RogueInstantKillSourceData
                {
                    ChancePermille = instantKill.ChancePermille,
                    GoldReward = instantKill.GoldReward,
                };
            }
            else
            {
                self.InstantKillBySource.Remove(sourceId);
            }

            EffectRogueOnKillHeal onKillHeal = config.GetEffect<EffectRogueOnKillHeal>();
            if (onKillHeal != null && onKillHeal.HealLostHpPermille > 0)
            {
                self.OnKillHealPermilleBySource[sourceId] = onKillHeal.HealLostHpPermille;
            }
            else
            {
                self.OnKillHealPermilleBySource.Remove(sourceId);
            }

            EffectRogueOnKillStackAttack onKillStack = config.GetEffect<EffectRogueOnKillStackAttack>();
            if (onKillStack != null && onKillStack.BonusPermillePerStack > 0)
            {
                int maxStacks = onKillStack.MaxStacks > 0 ? onKillStack.MaxStacks : 1;
                if (!self.OnKillStackBySource.TryGetValue(sourceId, out RogueOnKillStackSourceData stackState))
                {
                    stackState = new RogueOnKillStackSourceData();
                }

                if (stackState.CurrentStacks > maxStacks)
                {
                    stackState.CurrentStacks = maxStacks;
                }

                stackState.BonusPermillePerStack = onKillStack.BonusPermillePerStack;
                stackState.MaxStacks = maxStacks;
                self.OnKillStackBySource[sourceId] = stackState;
            }
            else
            {
                self.OnKillStackBySource.Remove(sourceId);
            }

            EffectRogueLowHpShield lowHpShield = config.GetEffect<EffectRogueLowHpShield>();
            if (lowHpShield != null && lowHpShield.TriggerHpPermille > 0 && lowHpShield.ShieldMaxHpPermille > 0)
            {
                if (!self.LowHpShieldBySource.TryGetValue(sourceId, out RogueLowHpShieldSourceData shieldState))
                {
                    shieldState = new RogueLowHpShieldSourceData();
                }

                shieldState.TriggerHpPermille = lowHpShield.TriggerHpPermille;
                shieldState.ShieldMaxHpPermille = lowHpShield.ShieldMaxHpPermille;
                self.LowHpShieldBySource[sourceId] = shieldState;
            }
            else
            {
                self.LowHpShieldBySource.Remove(sourceId);
            }

            EffectRogueExtendGameTime extendGameTime = config.GetEffect<EffectRogueExtendGameTime>();
            if (extendGameTime != null && extendGameTime.DurationMs > 0)
            {
                self.ExtendGameTimeBySourceMs[sourceId] = extendGameTime.DurationMs;
            }
            else
            {
                self.ExtendGameTimeBySourceMs.Remove(sourceId);
            }

            EffectRogueAfterSkillSpeedBoost afterSkillSpeedBoost = config.GetEffect<EffectRogueAfterSkillSpeedBoost>();
            if (afterSkillSpeedBoost != null && afterSkillSpeedBoost.SpeedPct > 0 && afterSkillSpeedBoost.DurationMs > 0)
            {
                self.AfterSkillSpeedBoostBySource[sourceId] = new RogueAfterSkillSpeedBoostSourceData
                {
                    SpeedPct = afterSkillSpeedBoost.SpeedPct,
                    DurationMs = afterSkillSpeedBoost.DurationMs,
                };
            }
            else
            {
                self.AfterSkillSpeedBoostBySource.Remove(sourceId);
            }

            if (config.GetEffect<EffectRogueSkillDisable>() != null)
            {
                self.SkillDisableSources[sourceId] = 1;
            }
            else
            {
                self.SkillDisableSources.Remove(sourceId);
            }

            if (config.GetEffect<EffectRogueOutOfCombatStealth>() != null)
            {
                self.OutOfCombatStealthSources[sourceId] = 1;
            }
            else
            {
                self.OutOfCombatStealthSources.Remove(sourceId);
            }
        }

        public static void RemoveBySource(this RogueBuffPassiveRuntimeComponent self, long sourceId)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            self.KillGoldBonusBySource.Remove(sourceId);
            self.ContainerLowQualityGoldBySource.Remove(sourceId);
            self.GoldDamagePerOnePercentBySource.Remove(sourceId);
            self.MonsterDamageBonusPermilleBySource.Remove(sourceId);
            self.AreaDiscoveryGoldBySource.Remove(sourceId);
            self.ProbabilityMultiplierBySource.Remove(sourceId);
            self.ContainerResultProbabilityMultiplierBySource.Remove(sourceId);
            self.SearchMonsterProbabilityMultiplierBySource.Remove(sourceId);
            self.LifeStealPermilleBySource.Remove(sourceId);
            self.SizeDifferenceDamageBonusBySource.Remove(sourceId);
            self.OnKillHealPermilleBySource.Remove(sourceId);
            self.LowHpDamageBySource.Remove(sourceId);
            self.DamageReductionBySource.Remove(sourceId);
            self.InstantKillBySource.Remove(sourceId);
            self.OnKillStackBySource.Remove(sourceId);
            self.LowHpShieldBySource.Remove(sourceId);
            self.ExtendGameTimeBySourceMs.Remove(sourceId);
            self.AfterSkillSpeedBoostBySource.Remove(sourceId);
            self.FatalImmunitySources.Remove(sourceId);
            self.SkillDisableSources.Remove(sourceId);
            self.OutOfCombatStealthSources.Remove(sourceId);
        }

        public static int GetKillGoldBonus(this RogueBuffPassiveRuntimeComponent self, int targetUnitType)
        {
            if (self == null || self.IsDisposed || targetUnitType != (int)UnitType.Monster)
            {
                return 0;
            }

            int total = 0;
            foreach (int value in self.KillGoldBonusBySource.Values)
            {
                if (value > 0)
                {
                    total += value;
                }
            }

            return total;
        }

        public static void GetContainerLowQualityGold(this RogueBuffPassiveRuntimeComponent self, out int totalGold, out int highQualityThreshold)
        {
            totalGold = 0;
            highQualityThreshold = 4;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            foreach (RogueContainerLowQualityGoldSourceData value in self.ContainerLowQualityGoldBySource.Values)
            {
                if (value.Gold > 0)
                {
                    totalGold += value.Gold;
                }

                if (value.HighQualityThreshold > highQualityThreshold)
                {
                    highQualityThreshold = value.HighQualityThreshold;
                }
            }
        }

        public static int GetAreaDiscoveryGold(this RogueBuffPassiveRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return 0;
            }

            int total = 0;
            foreach (int value in self.AreaDiscoveryGoldBySource.Values)
            {
                if (value > 0)
                {
                    total += value;
                }
            }

            return total;
        }

        public static int GetProbabilityMultiplierPermille(this RogueBuffPassiveRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return 1000;
            }

            int total = 0;
            foreach (int value in self.ProbabilityMultiplierBySource.Values)
            {
                if (value > 0)
                {
                    total += value;
                }
            }

            return total > 0 ? total : 1000;
        }

        public static int GetContainerResultProbabilityMultiplierPermille(this RogueBuffPassiveRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return 1000;
            }

            int total = 0;
            foreach (int value in self.ContainerResultProbabilityMultiplierBySource.Values)
            {
                if (value > 0)
                {
                    total += value;
                }
            }

            return total > 0 ? total : 1000;
        }

        public static int GetSearchMonsterProbabilityMultiplierPermille(this RogueBuffPassiveRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return 1000;
            }

            int total = 0;
            foreach (int value in self.SearchMonsterProbabilityMultiplierBySource.Values)
            {
                if (value > 0)
                {
                    total += value;
                }
            }

            return total > 0 ? total : 1000;
        }

        public static bool HasFatalImmunity(this RogueBuffPassiveRuntimeComponent self)
        {
            return self != null && !self.IsDisposed && self.FatalImmunitySources.Count > 0;
        }

        public static bool HasSkillDisable(this RogueBuffPassiveRuntimeComponent self)
        {
            return self != null && !self.IsDisposed && self.SkillDisableSources.Count > 0;
        }

        public static bool HasOutOfCombatStealth(this RogueBuffPassiveRuntimeComponent self)
        {
            return self != null && !self.IsDisposed && self.OutOfCombatStealthSources.Count > 0;
        }

        public static long GetExtendGameTimeMs(this RogueBuffPassiveRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return 0;
            }

            long total = 0;
            foreach (long value in self.ExtendGameTimeBySourceMs.Values)
            {
                if (value > 0)
                {
                    total += value;
                }
            }

            return total;
        }

        public static int GetMonsterDamageBonusPermille(this RogueBuffPassiveRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return 0;
            }

            int total = 0;
            foreach (int value in self.MonsterDamageBonusPermilleBySource.Values)
            {
                if (value > 0)
                {
                    total += value;
                }
            }

            return total;
        }

        public static int GetLifeStealPermille(this RogueBuffPassiveRuntimeComponent self, Unit target)
        {
            if (self == null || self.IsDisposed)
            {
                return 0;
            }

            int total = 0;
            foreach (RogueLifeStealSourceData source in self.LifeStealPermilleBySource.Values)
            {
                if (source.LifeStealPermille <= 0)
                {
                    continue;
                }

                if (!MonsterRuntimeProfileHelper.MatchesCombatFilter(target, source.TargetFilter))
                {
                    continue;
                }

                total += source.LifeStealPermille;
            }

            return total;
        }

        public static int GetLifeStealPermille(this RogueBuffPassiveRuntimeComponent self)
        {
            return self.GetLifeStealPermille(null);
        }

        public static int GetOnKillHealPermille(this RogueBuffPassiveRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return 0;
            }

            int total = 0;
            foreach (int value in self.OnKillHealPermilleBySource.Values)
            {
                if (value > 0)
                {
                    total += value;
                }
            }

            return total;
        }

        public static int GetSizeDifferenceDamageBonusPermille(this RogueBuffPassiveRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return 0;
            }

            int total = 0;
            foreach (int value in self.SizeDifferenceDamageBonusBySource.Values)
            {
                if (value > 0)
                {
                    total += value;
                }
            }

            return total;
        }

        public static void GetAfterSkillSpeedBoostData(this RogueBuffPassiveRuntimeComponent self, out int totalSpeedPct, out int maxDurationMs)
        {
            totalSpeedPct = 0;
            maxDurationMs = 0;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            foreach (RogueAfterSkillSpeedBoostSourceData value in self.AfterSkillSpeedBoostBySource.Values)
            {
                if (value.SpeedPct <= 0 || value.DurationMs <= 0)
                {
                    continue;
                }

                totalSpeedPct += value.SpeedPct;
                if (value.DurationMs > maxDurationMs)
                {
                    maxDurationMs = value.DurationMs;
                }
            }
        }

        public static bool IsEmpty(this RogueBuffPassiveRuntimeComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return true;
            }

            return self.KillGoldBonusBySource.Count == 0 &&
                    self.ContainerLowQualityGoldBySource.Count == 0 &&
                    self.GoldDamagePerOnePercentBySource.Count == 0 &&
                    self.MonsterDamageBonusPermilleBySource.Count == 0 &&
                    self.AreaDiscoveryGoldBySource.Count == 0 &&
                    self.ProbabilityMultiplierBySource.Count == 0 &&
                    self.ContainerResultProbabilityMultiplierBySource.Count == 0 &&
                    self.SearchMonsterProbabilityMultiplierBySource.Count == 0 &&
                    self.LifeStealPermilleBySource.Count == 0 &&
                    self.SizeDifferenceDamageBonusBySource.Count == 0 &&
                    self.OnKillHealPermilleBySource.Count == 0 &&
                    self.LowHpDamageBySource.Count == 0 &&
                    self.DamageReductionBySource.Count == 0 &&
                    self.InstantKillBySource.Count == 0 &&
                    self.OnKillStackBySource.Count == 0 &&
                    self.LowHpShieldBySource.Count == 0 &&
                    self.ExtendGameTimeBySourceMs.Count == 0 &&
                    self.AfterSkillSpeedBoostBySource.Count == 0 &&
                    self.FatalImmunitySources.Count == 0 &&
                    self.SkillDisableSources.Count == 0 &&
                    self.OutOfCombatStealthSources.Count == 0;
        }
    }
}
