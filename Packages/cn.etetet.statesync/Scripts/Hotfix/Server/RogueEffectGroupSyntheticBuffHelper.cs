using System.Collections.Generic;

namespace ET.Server
{
    public static class RogueEffectGroupSyntheticBuffHelper
    {
        private const int SyntheticBuffIdBase = 5000000;

        public static bool TryEnsureSyntheticBuffConfig(int effectGroupId, RogueEffectGroupConfig groupConfig, out int buffConfigId)
        {
            buffConfigId = 0;
            if (effectGroupId <= 0 || groupConfig?.Entries == null || groupConfig.Entries.Count == 0)
            {
                return false;
            }

            BuffConfigCategory category = BuffConfigCategory.Instance;
            if (category == null || effectGroupId > int.MaxValue - SyntheticBuffIdBase)
            {
                return false;
            }

            buffConfigId = SyntheticBuffIdBase + effectGroupId;
            if (category.Contain(buffConfigId))
            {
                return true;
            }

            BuffConfig config = BuildSyntheticBuffConfig(buffConfigId, effectGroupId, groupConfig);
            if (config == null)
            {
                return false;
            }

            config.OnAfterDeserialize();
            category.Add(config);
            return true;
        }

        private static BuffConfig BuildSyntheticBuffConfig(int buffConfigId, int effectGroupId, RogueEffectGroupConfig groupConfig)
        {
            BuffConfig config = new BuffConfig
            {
                Id = buffConfigId,
                Desc = $"Rogue Synthetic Group {effectGroupId}",
                Duration = -1,
                TickTime = 0,
                MaxStack = 1,
                Stack = 1,
                OverLayRuleType = OverLayRuleType.None,
                NoticeType = NoticeType.NoNotice,
            };

            List<BTNode> addNodes = new();
            List<BTNode> removeNodes = new();
            List<BTNode> tickNodes = new();
            List<RogueWeaponModifierEntry> weaponEntries = null;
            bool needPassiveRuntimeHooks = false;
            bool hasExecutableEntry = false;
            int tickTime = 0;

            foreach (RogueEffectEntryConfig entry in groupConfig.Entries)
            {
                if (entry == null)
                {
                    continue;
                }

                switch (entry.ExecuteType)
                {
                    case RogueEffectExecuteType.AddGold:
                    {
                        hasExecutableEntry = true;
                        addNodes.Add(new BTRogueAddGold { Amount = entry.Value1 });
                        break;
                    }
                    case RogueEffectExecuteType.RegisterObjective:
                    {
                        hasExecutableEntry = true;
                        addNodes.Add(new BTRogueRegisterObjective
                        {
                            ObjectiveId = entry.RefId,
                            GoalValue = entry.Value1 > 0 ? entry.Value1 : 1,
                        });
                        removeNodes.Add(new BTRogueUnregisterObjective());
                        break;
                    }
                    case RogueEffectExecuteType.GrantRandomCard:
                    {
                        hasExecutableEntry = true;
                        addNodes.Add(new BTRogueGrantRandomCard
                        {
                            Quality = entry.Value1,
                            Count = entry.Value2 > 0 ? entry.Value2 : 1,
                            FixedOptionId = entry.RefId > 0 ? entry.RefId : 0,
                        });
                        break;
                    }
                    case RogueEffectExecuteType.ReplaceAllCards:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueReplaceAllCards());
                        addNodes.Add(new BTRogueReplaceAllCards());
                        break;
                    }
                    case RogueEffectExecuteType.AddKillGold:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueKillGoldBonus { Gold = entry.Value1 });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.OnKillHeal:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueOnKillHeal { HealLostHpPermille = entry.Value1 });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.OnKillStackAttack:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueOnKillStackAttack
                        {
                            BonusPermillePerStack = entry.Value1,
                            MaxStacks = entry.Value2 > 0 ? entry.Value2 : 1,
                        });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.GoldDamageBonus:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueGoldDamageBonus
                        {
                            GoldPerOnePercent = entry.Value1 > 0 ? entry.Value1 : 1,
                        });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.LowHpDamageBonus:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueLowHpDamageBonus
                        {
                            HpThresholdPermille = entry.Value1,
                            DamageBonusPermille = entry.Value2,
                        });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.DamageReduction:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueDamageReduction
                        {
                            DamageReduction = entry.Value1,
                            SourceFilter = entry.Value2,
                        });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.LifeSteal:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueLifeSteal { LifeStealPermille = entry.Value1 });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.InstantKillChance:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueInstantKillChance
                        {
                            ChancePermille = entry.Value1,
                            GoldReward = entry.Value2,
                        });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.ExtendGameTime:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueExtendGameTime { DurationMs = entry.Value1 });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.PeriodicHpScale:
                    {
                        hasExecutableEntry = true;
                        if (entry.Value1 > 0 && entry.Value2 != 0)
                        {
                            tickTime = tickTime > 0 ? System.Math.Min(tickTime, entry.Value1) : entry.Value1;
                            tickNodes.Add(new BTRoguePeriodicHpScale { MaxHpPermille = entry.Value2 });
                            removeNodes.Add(new BTRogueRevertPeriodicHpScale());
                        }

                        break;
                    }
                    case RogueEffectExecuteType.ProbabilityMultiplier:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueProbabilityMultiplier { Permille = entry.Value1 });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.SkillDisable:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueSkillDisable());
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.AreaDiscoveryGold:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueAreaDiscoveryGold { Gold = entry.Value1 });
                        needPassiveRuntimeHooks = true;
                        break;
                    }
                    case RogueEffectExecuteType.ScaleModifier:
                    {
                        hasExecutableEntry = true;
                        config.Effects.Add(new EffectRogueScaleModifier
                        {
                            ScalePermille = entry.Value1,
                            MaxHpPermille = entry.Value2,
                        });
                        if (entry.Value2 != 0)
                        {
                            addNodes.Add(new BTRogueApplyScaleModifier { MaxHpPermille = entry.Value2 });
                            removeNodes.Add(new BTRogueRevertScaleModifier());
                        }

                        break;
                    }
                    case RogueEffectExecuteType.WeaponModifier:
                    {
                        hasExecutableEntry = true;
                        if (entry.RefId > 0 && entry.Value1 != 0)
                        {
                            weaponEntries ??= new List<RogueWeaponModifierEntry>();
                            weaponEntries.Add(new RogueWeaponModifierEntry
                            {
                                ModType = entry.RefId,
                                ValuePermille = entry.Value1,
                            });
                        }

                        break;
                    }
                    case RogueEffectExecuteType.AddBuff:
                    default:
                    {
                        break;
                    }
                }
            }

            if (!hasExecutableEntry)
            {
                return null;
            }

            if (weaponEntries != null && weaponEntries.Count > 0)
            {
                config.Effects.Add(new EffectRogueWeaponModifiers
                {
                    Entries = weaponEntries,
                });
                addNodes.Add(new BTRogueAddWeaponModifiers());
                removeNodes.Add(new BTRogueRemoveWeaponModifiers());
            }

            if (needPassiveRuntimeHooks)
            {
                addNodes.Add(new BTRogueApplyPassiveEffects());
                removeNodes.Add(new BTRogueRemovePassiveEffects());
            }

            if (addNodes.Count > 0)
            {
                EffectServerBuffAdd addEffect = new EffectServerBuffAdd();
                addEffect.Children.AddRange(addNodes);
                config.Effects.Add(addEffect);
            }

            if (removeNodes.Count > 0)
            {
                EffectServerBuffRemove removeEffect = new EffectServerBuffRemove();
                removeEffect.Children.AddRange(removeNodes);
                config.Effects.Add(removeEffect);
            }

            if (tickNodes.Count > 0)
            {
                config.TickTime = tickTime > 0 ? tickTime : 1000;
                EffectServerBuffTick tickEffect = new EffectServerBuffTick();
                tickEffect.Children.AddRange(tickNodes);
                config.Effects.Add(tickEffect);
            }

            return config;
        }
    }
}
