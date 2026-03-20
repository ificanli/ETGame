using System.Collections.Generic;
using System;
using System.Linq;

namespace ET.Server
{
    public static class RogueBuffConfigLoader
    {
        public static List<BuffConfig> BuildBuiltinBuffConfigs()
        {
            BuffConfigCategory category = new();
            RegisterGoldExpert(category);
            RegisterAllSearch(category);
            RegisterBossHunter(category);
            RegisterFirepower(category);
            RegisterEndurance(category);
            RegisterHunter(category);
            RegisterDiceManiac(category);

            return category.GetAll().Values.OrderBy(static config => config.Id).ToList();
        }

        public static void EnsureRegistered()
        {
            BuffConfigCategory category = BuffConfigCategory.Instance;
            if (category == null)
            {
                return;
            }

            if (!category.Contain(1001))
            {
                RegisterGoldExpert(category);
                RegisterAllSearch(category);
                RegisterBossHunter(category);
                RegisterFirepower(category);
                RegisterEndurance(category);
                RegisterHunter(category);
                RegisterDiceManiac(category);
            }

            RegisterCommonShowTagBuffs(category);
            RegisterMissingLegacyOptionBuffs(category);
        }

        private static void RegisterGoldExpert(BuffConfigCategory category)
        {
            Register(category, Buff(1001, "Rogue 1001", add: Add(new BTRogueSpawnMerchant { RewardGold = 30000 })));
            Register(category, Buff(1002, "Rogue 1002", add: Add(new BTRogueAddGold { Amount = 5000 })));
            Register(category, Buff(
                1003,
                "Rogue 1003",
                new EffectRogueKillGoldBonus { Gold = 300 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1004,
                "Rogue 1004",
                new EffectRogueFatalImmunity(),
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1005,
                "Rogue 1005",
                new EffectRogueContainerLowQualityGold { Gold = 300, HighQualityThreshold = 4 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1006,
                "Rogue 1006",
                new EffectRogueGoldDamageBonus { GoldPerOnePercent = 500 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1007,
                "Rogue 1007",
                new EffectRogueAreaDiscoveryGold { Gold = 800 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
        }

        private static void RegisterAllSearch(BuffConfigCategory category)
        {
            EffectRogueContainerRadar radarEffect = new EffectRogueContainerRadar
            {
                Radius = 30,
                IntervalMs = 20000,
            };
            Register(category, Buff(
                1011,
                "Rogue 1011",
                radarEffect,
                tickTime: radarEffect.IntervalMs > 0 ? radarEffect.IntervalMs : 20000,
                tick: Tick(new BTRogueRadarScan())));
            Register(category, Buff(
                1012,
                "Rogue 1012",
                new EffectRogueContainerResultProbabilityMultiplier { Permille = 2000 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1013,
                "Rogue 1013",
                new EffectRogueTemporaryKey { ItemConfigId = 10001, Count = 1 },
                add: Add(new BTRogueGrantItem { ItemConfigId = 10001, Count = 1 }),
                remove: Remove(new BTRogueCleanupTemporaryItems())));
            Register(category, Buff(
                1014,
                "Rogue 1014",
                new EffectRogueInteractRangeBonus { Distance = 3f },
                add: Add(new BTRogueApplyInteractRangeBonus()),
                remove: Remove(new BTRogueRemoveInteractRangeBonus())));
            Register(category, Buff(
                1015,
                "Rogue 1015",
                new EffectRogueSilentSearch(),
                add: Add(new BTRogueApplySilentSearch()),
                remove: Remove(new BTRogueRemoveSilentSearch())));
            Register(category, Buff(
                1016,
                "Rogue 1016",
                new EffectRogueBagSpaceHpBonus { MaxHpPermilleAtFull = 300 },
                tickTime: 1000,
                tick: Tick(new BTRogueUpdateBagSpaceHpBonus()),
                remove: Remove(new BTRogueClearBagSpaceHpBonus())));
        }

        private static void RegisterBossHunter(BuffConfigCategory category)
        {
            Register(category, Buff(
                1021,
                "Rogue 1021",
                new EffectRogueObjectiveRewardBuff { BuffConfigId = 102101 },
                add: Add(new BTRogueRegisterObjective { ObjectiveId = 0, GoalValue = 15 }),
                remove: Remove(new BTRogueUnregisterObjective())));
            Register(category, Buff(
                102101,
                "Rogue 1021 Reward",
                new EffectRogueMonsterDamageBonus { DamageBonusPermille = 500 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1022,
                "Rogue 1022",
                new EffectRogueDamageReduction { DamageReduction = 300, SourceFilter = 1 },
                new EffectRogueLifeSteal { LifeStealPermille = 300 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1023,
                "Rogue 1023",
                new EffectRogueOnKillHeal { HealLostHpPermille = 20 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1024,
                "Rogue 1024",
                new EffectRogueSearchMonsterProbabilityMultiplier { Permille = 2000 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1025,
                "Rogue 1025",
                new EffectRogueWeaponModifiers
                {
                    Entries = new List<RogueWeaponModifierEntry>
                    {
                        WeaponModifier(WeaponModType.Penetration, 100),
                        WeaponModifier(WeaponModType.BulletDamage, 150),
                    },
                },
                add: Add(new BTRogueAddWeaponModifiers()),
                remove: Remove(new BTRogueRemoveWeaponModifiers())));
            Register(category, Buff(
                1026,
                "Rogue 1026",
                new EffectRogueNearMonsterSpeedBonus { Radius = 30, SpeedPct = 30 },
                tickTime: 1000,
                tick: Tick(new BTRogueUpdateNearMonsterSpeed()),
                remove: Remove(new BTRogueClearNearMonsterSpeed())));
        }

        private static void RegisterFirepower(BuffConfigCategory category)
        {
            Register(category, Buff(
                1031,
                "Rogue 1031",
                new EffectRogueWeaponModifiers
                {
                    Entries = new List<RogueWeaponModifierEntry>
                    {
                        WeaponModifier(WeaponModType.AttackRange, 200),
                    },
                },
                add: Add(new BTRogueAddWeaponModifiers()),
                remove: Remove(new BTRogueRemoveWeaponModifiers())));
            Register(category, Buff(
                1032,
                "Rogue 1032",
                new EffectRogueSkillDisable(),
                new EffectRogueWeaponModifiers
                {
                    Entries = new List<RogueWeaponModifierEntry>
                    {
                        WeaponModifier(WeaponModType.AttackSpeed, 500),
                    },
                },
                add: Add(new BTRogueApplyPassiveEffects(), new BTRogueAddWeaponModifiers()),
                remove: Remove(new BTRogueRemovePassiveEffects(), new BTRogueRemoveWeaponModifiers())));
            Register(category, Buff(
                1033,
                "Rogue 1033",
                new EffectRogueHitHeroCritGrowth { CritPermillePerHit = 10 },
                add: Add(new BTRogueApplyHitHeroCritGrowth()),
                remove: Remove(new BTRogueRemoveHitHeroCritGrowth())));
            Register(category, Buff(
                1034,
                "Rogue 1034",
                new EffectRogueWeaponModifiers
                {
                    Entries = new List<RogueWeaponModifierEntry>
                    {
                        WeaponModifier(WeaponModType.MagazineCapacity, 500),
                        WeaponModifier(WeaponModType.ReloadSpeed, 300),
                    },
                },
                add: Add(new BTRogueAddWeaponModifiers()),
                remove: Remove(new BTRogueRemoveWeaponModifiers())));
            Register(category, Buff(
                1035,
                "Rogue 1035",
                new EffectRogueTrapMaster { IdleMs = 5000, BulletCount = 15 },
                add: Add(new BTRogueApplyTrapMaster()),
                remove: Remove(new BTRogueRemoveTrapMaster())));
            Register(category, Buff(
                1036,
                "Rogue 1036",
                new EffectRogueReloadFirstShotsBoost { DamageBonusPermille = 2000, ShotCount = 3, PenetrationCount = 1 },
                add: Add(new BTRogueApplyReloadFirstShotsBoost()),
                remove: Remove(new BTRogueRemoveReloadFirstShotsBoost())));
        }

        private static void RegisterEndurance(BuffConfigCategory category)
        {
            Register(category, Buff(
                1041,
                "Rogue 1041",
                new EffectRogueScaleModifier { ScalePermille = 200, MaxHpPermille = 300 },
                add: Add(new BTRogueApplyScaleModifier { MaxHpPermille = 300 }),
                remove: Remove(new BTRogueRevertScaleModifier())));
            Register(category, Buff(
                1042,
                "Rogue 1042",
                tickTime: 1000,
                tick: Tick(new BTRogueHealMaxHpPermille { HealPermille = 10 })));
            Register(category, Buff(
                1043,
                "Rogue 1043",
                new EffectRogueLowHpShield { TriggerHpPermille = 200, ShieldMaxHpPermille = 500 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1044,
                "Rogue 1044",
                new EffectRogueExtendGameTime { DurationMs = 300000 },
                add: Add(new BTRogueApplyPassiveEffects()),
                tickTime: 30000,
                tick: Tick(new BTRoguePeriodicHpScale { MaxHpPermille = 30 }),
                remove: Remove(new BTRogueRevertPeriodicHpScale(), new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1045,
                "Rogue 1045",
                new EffectRogueHealSpirit { HealPermille = 50, IntervalMs = 30000 },
                tickTime: 30000,
                add: Add(new BTRogueSummonHealSpirit()),
                tick: Tick(new BTRogueSummonHealSpirit()),
                remove: Remove(new BTRogueRemoveSummonedSpirit())));
        }

        private static void RegisterHunter(BuffConfigCategory category)
        {
            Register(category, Buff(
                1051,
                "Rogue 1051",
                new EffectRogueLowHpDamageBonus { HpThresholdPermille = 300, DamageBonusPermille = 300 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1052,
                "Rogue 1052",
                new EffectRogueOnKillStackAttack { BonusPermillePerStack = 300, MaxStacks = 3 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1053,
                "Rogue 1053",
                new EffectRogueOutOfCombatStealth(),
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects(), new BTRogueClearOutOfCombatStealth())));
            Register(category, Buff(
                1054,
                "Rogue 1054",
                new EffectRogueScaleModifier { ScalePermille = -200, MaxHpPermille = -200 },
                new EffectRogueSizeDifferenceDamageBonus { MaxDifferencePermille = 500, MaxDamageBonusPermille = 500 },
                add: Add(new BTRogueApplyScaleModifier { MaxHpPermille = -200 }, new BTRogueApplyPassiveEffects(), new BTRogueApplySpeedFinalPct { Value = 30 }),
                remove: Remove(new BTRogueRevertScaleModifier(), new BTRogueRemovePassiveEffects(), new BTRogueRemoveSpeedFinalPct())));
            Register(category, Buff(
                1055,
                "Rogue 1055",
                new EffectRogueWeaponModifiers
                {
                    Entries = new List<RogueWeaponModifierEntry>
                    {
                        WeaponModifier(WeaponModType.AttackRange, -800),
                        WeaponModifier(WeaponModType.BulletDamage, 4000),
                    },
                },
                add: Add(new BTRogueAddWeaponModifiers()),
                remove: Remove(new BTRogueRemoveWeaponModifiers())));
            Register(category, Buff(
                1056,
                "Rogue 1056",
                new EffectRogueAfterSkillSpeedBoost { SpeedPct = 10, DurationMs = 3000 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
        }

        private static void RegisterDiceManiac(BuffConfigCategory category)
        {
            Register(category, Buff(1061, "Rogue 1061", add: Add(new BTRogueGrantRandomCard { Quality = 2, Count = 1 })));
            Register(category, Buff(1062, "Rogue 1062", add: Add(new BTRogueGrantRandomCard { Quality = 3, Count = 2 })));
            Register(category, Buff(
                1063,
                "Rogue 1063",
                new EffectRogueInstantKillChance { ChancePermille = 10, GoldReward = 8888 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1064,
                "Rogue 1064",
                new EffectRogueProbabilityMultiplier { Permille = 1500 },
                add: Add(new BTRogueApplyPassiveEffects()),
                remove: Remove(new BTRogueRemovePassiveEffects())));
            Register(category, Buff(
                1065,
                "Rogue 1065",
                new EffectRogueReplaceAllCards(),
                add: Add(
                    new BTRogueReplaceAllCards(),
                    new BTRogueGrantRandomCard { Quality = 2, Count = 2 },
                    new BTRogueGrantRandomCard { Quality = 3, Count = 1 })));
        }

        private static void Register(BuffConfigCategory category, BuffConfig buffConfig)
        {
            if (category.Contain(buffConfig.Id))
            {
                return;
            }

            buffConfig.OnAfterDeserialize();
            category.Add(buffConfig);
        }

        private static void RegisterCommonShowTagBuffs(BuffConfigCategory category)
        {
            TagsConfigCategory tagsCategory = TagsConfigCategory.Instance;
            if (tagsCategory?.DataList == null)
            {
                return;
            }

            foreach (TagsConfig tagConfig in tagsCategory.DataList)
            {
                if (tagConfig?.ShowTagsBuffId == null || tagConfig.ShowTagsBuffId.Length == 0)
                {
                    continue;
                }

                foreach (int buffConfigId in tagConfig.ShowTagsBuffId)
                {
                    if (buffConfigId <= 0 || category.Contain(buffConfigId))
                    {
                        continue;
                    }

                    Register(category, Buff(buffConfigId, $"Rogue ShowTag {buffConfigId}"));
                }
            }
        }

        private static void RegisterMissingLegacyOptionBuffs(BuffConfigCategory category)
        {
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                return;
            }

            foreach (RogueOptionConfig optionConfig in configCategory.GetOptions().Values)
            {
                if (!TryGetExplicitLegacyBuffConfigId(optionConfig, out int buffConfigId) ||
                    buffConfigId <= 0 ||
                    category.Contain(buffConfigId))
                {
                    continue;
                }

                Register(category, CreatePlaceholderBuff(buffConfigId));
            }
        }

        private static bool TryGetExplicitLegacyBuffConfigId(RogueOptionConfig optionConfig, out int buffConfigId)
        {
            buffConfigId = 0;
            if (optionConfig == null)
            {
                return false;
            }

            if (optionConfig.BuffConfigId > 0)
            {
                buffConfigId = optionConfig.BuffConfigId;
                return true;
            }

            if (string.IsNullOrWhiteSpace(optionConfig.BTConfig))
            {
                return false;
            }

            string configText = optionConfig.BTConfig.Trim();
            const string buffPrefix = "buff:";
            if (configText.StartsWith(buffPrefix, StringComparison.OrdinalIgnoreCase))
            {
                configText = configText.Substring(buffPrefix.Length).Trim();
            }

            return int.TryParse(configText, out buffConfigId) && buffConfigId > 0;
        }

        private static BuffConfig CreatePlaceholderBuff(int buffConfigId)
        {
            return new BuffConfig
            {
                Id = buffConfigId,
                Desc = $"Rogue Legacy Placeholder {buffConfigId}",
                Duration = -1,
                TickTime = 1000000,
                MaxStack = 1,
                Stack = 1,
                OverLayRuleType = OverLayRuleType.None,
                NoticeType = NoticeType.NoNotice,
            };
        }

        private static BuffConfig Buff(int id, string desc, params EffectNode[] effects)
        {
            return Buff(id, desc, -1, null, null, null, effects);
        }

        private static BuffConfig Buff(int id, string desc, EffectNode effect, int tickTime = 0, EffectServerBuffAdd add = null, EffectServerBuffRemove remove = null, EffectServerBuffTick tick = null)
        {
            return Buff(id, desc, tickTime, add, remove, tick, effect);
        }

        private static BuffConfig Buff(int id, string desc, EffectNode effect1, EffectNode effect2, int tickTime = 0, EffectServerBuffAdd add = null, EffectServerBuffRemove remove = null, EffectServerBuffTick tick = null)
        {
            return Buff(id, desc, tickTime, add, remove, tick, effect1, effect2);
        }

        private static BuffConfig Buff(int id, string desc, int tickTime = 0, EffectServerBuffAdd add = null, EffectServerBuffRemove remove = null, EffectServerBuffTick tick = null, params EffectNode[] effects)
        {
            BuffConfig config = new BuffConfig
            {
                Id = id,
                Desc = desc,
                Duration = -1,
                TickTime = tickTime,
                MaxStack = 1,
                Stack = 1,
                OverLayRuleType = OverLayRuleType.None,
                NoticeType = NoticeType.NoNotice,
            };

            if (effects != null)
            {
                foreach (EffectNode effect in effects)
                {
                    if (effect != null)
                    {
                        config.Effects.Add(effect);
                    }
                }
            }

            if (add != null)
            {
                config.Effects.Add(add);
            }

            if (remove != null)
            {
                config.Effects.Add(remove);
            }

            if (tick != null)
            {
                config.Effects.Add(tick);
            }

            return config;
        }

        private static EffectServerBuffAdd Add(params BTNode[] children)
        {
            EffectServerBuffAdd effect = new EffectServerBuffAdd();
            effect.Children.AddRange(children);
            return effect;
        }

        private static EffectServerBuffRemove Remove(params BTNode[] children)
        {
            EffectServerBuffRemove effect = new EffectServerBuffRemove();
            effect.Children.AddRange(children);
            return effect;
        }

        private static EffectServerBuffTick Tick(params BTNode[] children)
        {
            EffectServerBuffTick effect = new EffectServerBuffTick();
            effect.Children.AddRange(children);
            return effect;
        }

        private static RogueWeaponModifierEntry WeaponModifier(int modType, int valuePermille)
        {
            return new RogueWeaponModifierEntry
            {
                ModType = modType,
                ValuePermille = valuePermille,
            };
        }
    }
}
