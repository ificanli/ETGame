using System.Collections.Generic;

namespace ET.Server
{
    public static class MonsterRuntimeProfileHelper
    {
        public const string BladeCatGroupId = "blade_cat";
        public const string ScoutMonkeyGroupId = "scout_monkey";
        public const string PhantomOwlGroupId = "phantom_owl";
        public const string HeavyGatorGroupId = "heavy_gator";
        public const string BalooGroupId = "baloo";

        private const int LightMonsterUnitConfigId = 1003;
        private const int HeavyMonsterUnitConfigId = 1004;

        private const int BladeCatAiBuffId = 330101;
        private const int ScoutMonkeyAiBuffId = 330102;
        private const int PhantomOwlAiBuffId = 330103;
        private const int HeavyGatorAiBuffId = 330104;
        private const int BalooAiBuffId = 330105;

        public static bool TryResolveUnitConfigId(string groupId, out int unitConfigId)
        {
            unitConfigId = 0;
            if (!TryNormalizeGroupId(groupId, out string normalizedGroupId))
            {
                return false;
            }

            if (!EnsureGroupRegistered(normalizedGroupId))
            {
                return false;
            }

            switch (normalizedGroupId)
            {
                case BladeCatGroupId:
                case ScoutMonkeyGroupId:
                case PhantomOwlGroupId:
                    unitConfigId = LightMonsterUnitConfigId;
                    return true;
                case HeavyGatorGroupId:
                case BalooGroupId:
                    unitConfigId = HeavyMonsterUnitConfigId;
                    return true;
                default:
                    return false;
            }
        }

        public static bool ApplyProfile(Unit monster, string groupId)
        {
            if (monster == null || monster.IsDisposed)
            {
                return false;
            }

            if (!TryNormalizeGroupId(groupId, out string normalizedGroupId) || !EnsureGroupRegistered(normalizedGroupId))
            {
                return false;
            }

            NumericComponent numeric = monster.NumericComponent;
            if (numeric == null)
            {
                return false;
            }

            switch (normalizedGroupId)
            {
                case BladeCatGroupId:
                    ApplyNumericProfile(numeric, 260, 2350, 430, 10000, BladeCatAiBuffId);
                    break;
                case ScoutMonkeyGroupId:
                    ApplyNumericProfile(numeric, 220, 2100, 420, 11000, ScoutMonkeyAiBuffId);
                    break;
                case PhantomOwlGroupId:
                    ApplyNumericProfile(numeric, 210, 2250, 410, 12000, PhantomOwlAiBuffId);
                    break;
                case HeavyGatorGroupId:
                    ApplyNumericProfile(numeric, 520, 1750, 520, 11000, HeavyGatorAiBuffId);
                    break;
                case BalooGroupId:
                    ApplyNumericProfile(numeric, 960, 1850, 600, 12000, BalooAiBuffId);
                    break;
                default:
                    return false;
            }

            TargetComponent targetComponent = monster.GetComponent<TargetComponent>();
            if (targetComponent != null)
            {
                targetComponent.Unit = null;
                targetComponent.Position = monster.Position;
            }

            return true;
        }

        public static bool IsMonster(Unit unit)
        {
            return unit != null && !unit.IsDisposed && unit.UnitType == UnitType.Monster;
        }

        public static bool IsElite(Unit unit)
        {
            return TryGetProfileGroupId(unit, out string groupId) &&
                    string.Equals(groupId, HeavyGatorGroupId, System.StringComparison.Ordinal);
        }

        public static bool IsBoss(Unit unit)
        {
            return TryGetProfileGroupId(unit, out string groupId) &&
                    string.Equals(groupId, BalooGroupId, System.StringComparison.Ordinal);
        }

        public static bool IsEliteOrBoss(Unit unit)
        {
            return IsElite(unit) || IsBoss(unit);
        }

        public static bool MatchesCombatFilter(Unit unit, int filter)
        {
            return filter switch
            {
                RogueUnitFilterType.None => true,
                RogueUnitFilterType.Monster => IsMonster(unit),
                RogueUnitFilterType.Boss => IsBoss(unit),
                RogueUnitFilterType.EliteOrBoss => IsEliteOrBoss(unit),
                RogueUnitFilterType.NonBossMonster => IsMonster(unit) && !IsBoss(unit),
                _ => true,
            };
        }

        public static bool EnsureGroupRegistered(string groupId)
        {
            if (!TryNormalizeGroupId(groupId, out string normalizedGroupId))
            {
                return false;
            }

            SpellConfigCategory spellCategory = SpellConfigCategory.Instance;
            BuffConfigCategory buffCategory = BuffConfigCategory.Instance;
            if (spellCategory == null || buffCategory == null)
            {
                return false;
            }

            switch (normalizedGroupId)
            {
                case BladeCatGroupId:
                    RegisterBladeCat(spellCategory, buffCategory);
                    return true;
                case ScoutMonkeyGroupId:
                    RegisterScoutMonkey(spellCategory, buffCategory);
                    return true;
                case PhantomOwlGroupId:
                    RegisterPhantomOwl(spellCategory, buffCategory);
                    return true;
                case HeavyGatorGroupId:
                    RegisterHeavyGator(spellCategory, buffCategory);
                    return true;
                case BalooGroupId:
                    RegisterBaloo(spellCategory, buffCategory);
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryNormalizeGroupId(string groupId, out string normalizedGroupId)
        {
            normalizedGroupId = null;
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return false;
            }

            switch (groupId.Trim().ToLowerInvariant())
            {
                case BladeCatGroupId:
                case "影刃猫":
                    normalizedGroupId = BladeCatGroupId;
                    return true;
                case ScoutMonkeyGroupId:
                case "侦察猴":
                case "捣蛋侦察猴":
                    normalizedGroupId = ScoutMonkeyGroupId;
                    return true;
                case PhantomOwlGroupId:
                case "幻影猫头鹰":
                    normalizedGroupId = PhantomOwlGroupId;
                    return true;
                case HeavyGatorGroupId:
                case "重装鳄鱼":
                    normalizedGroupId = HeavyGatorGroupId;
                    return true;
                case BalooGroupId:
                case "巴鲁":
                case "机械巨熊巴鲁":
                case "机械巨熊·巴鲁":
                    normalizedGroupId = BalooGroupId;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetProfileGroupId(Unit unit, out string groupId)
        {
            groupId = null;
            if (!IsMonster(unit))
            {
                return false;
            }

            int aiBuffId = unit.NumericComponent?.GetAsInt(NumericType.AI) ?? 0;
            switch (aiBuffId)
            {
                case BladeCatAiBuffId:
                    groupId = BladeCatGroupId;
                    return true;
                case ScoutMonkeyAiBuffId:
                    groupId = ScoutMonkeyGroupId;
                    return true;
                case PhantomOwlAiBuffId:
                    groupId = PhantomOwlGroupId;
                    return true;
                case HeavyGatorAiBuffId:
                    groupId = HeavyGatorGroupId;
                    return true;
                case BalooAiBuffId:
                    groupId = BalooGroupId;
                    return true;
            }

            UnitConfig config = UnitConfigCategory.Instance.GetOrDefault(unit.ConfigId);
            return config != null && TryNormalizeGroupId(config.Name, out groupId);
        }

        private static void ApplyNumericProfile(NumericComponent numeric, int maxHp, int speedBase, int radiusBase, int aoiBase, int aiBuffConfigId)
        {
            SetNumeric(numeric, NumericType.MaxHPBase, maxHp);
            SetNumeric(numeric, NumericType.MaxHP, maxHp);
            SetNumeric(numeric, NumericType.HP, maxHp);
            SetNumeric(numeric, NumericType.SpeedBase, speedBase);
            SetNumeric(numeric, NumericType.Speed, speedBase);
            SetNumeric(numeric, NumericType.RadiusBase, radiusBase);
            SetNumeric(numeric, NumericType.Radius, radiusBase);
            SetNumeric(numeric, NumericType.AOIBase, aoiBase);
            SetNumeric(numeric, NumericType.AOI, aoiBase);
            SetNumeric(numeric, NumericType.AI, aiBuffConfigId);
        }

        private static void RegisterBladeCat(SpellConfigCategory spellCategory, BuffConfigCategory buffCategory)
        {
            SpellConfig strike = CreateSingleTargetDamageSpell(130102, 230102, "影刃猫-突刺命中", 0, 2400, 0);
            SpellConfig windup = CreateSingleTargetWindupSpell(130101, 230101, "影刃猫-突刺前摇", 900, 2800, 0);

            RegisterSpell(spellCategory, windup);
            RegisterSpell(spellCategory, strike);
            RegisterBuff(buffCategory, CreateSingleTargetWindupBuff(230101, "影刃猫-突刺前摇", 360, strike.Id));
            RegisterBuff(buffCategory, CreateSingleTargetDamageBuff(230102, "影刃猫-突刺命中", 18));
            RegisterBuff(
                buffCategory,
                CreateStandardAiBuff(
                    BladeCatAiBuffId,
                    "影刃猫AI",
                    new AI_BladeCatCombat
                    {
                        ThinkIntervalMs = 160,
                        MainSpellId = windup.Id,
                    },
                    patrolMinRadius: 1.2f,
                    patrolMaxRadius: 4.5f,
                    aggroRange: 8.5f,
                    returnDistance: 16f,
                    idleMinMs: 500,
                    idleMaxMs: 1200));
        }

        private static void RegisterScoutMonkey(SpellConfigCategory spellCategory, BuffConfigCategory buffCategory)
        {
            SpellConfig bomb = new SpellConfig
            {
                Id = 130111,
                BuffId = 230111,
                Desc = "侦察猴-延迟爆炸",
                CD = 1500,
                TargetSelector = new TargetSelectorPosition
                {
                    MaxDistance = 8,
                },
            };

            RegisterSpell(spellCategory, bomb);
            RegisterBuff(buffCategory, CreateTargetPointExplosionBuff(230111, "侦察猴-延迟爆炸", 650, 2.2f, 16));
            RegisterBuff(
                buffCategory,
                CreateStandardAiBuff(
                    ScoutMonkeyAiBuffId,
                    "侦察猴AI",
                    new AI_ScoutMonkeyCombat
                    {
                        ThinkIntervalMs = 180,
                        MainSpellId = bomb.Id,
                    },
                    patrolMinRadius: 1.5f,
                    patrolMaxRadius: 5.5f,
                    aggroRange: 10f,
                    returnDistance: 18f,
                    idleMinMs: 700,
                    idleMaxMs: 1400));
        }

        private static void RegisterPhantomOwl(SpellConfigCategory spellCategory, BuffConfigCategory buffCategory)
        {
            SpellConfig outbound = CreateSingleTargetDamageSpell(130122, 230122, "幻影猫头鹰-去程命中", 0, 6500, 2000);
            SpellConfig inbound = CreateSingleTargetDamageSpell(130123, 230123, "幻影猫头鹰-回程命中", 0, 6500, 2000);
            SpellConfig dualHit = new SpellConfig
            {
                Id = 130121,
                BuffId = 230121,
                Desc = "幻影猫头鹰-双段回旋",
                CD = 1600,
                TargetSelector = new TargetSelectorSingle
                {
                    UnitType = UnitType.Player,
                    MaxDistance = 6500,
                    MinDistance = 2200,
                },
            };

            RegisterSpell(spellCategory, dualHit);
            RegisterSpell(spellCategory, outbound);
            RegisterSpell(spellCategory, inbound);
            RegisterBuff(buffCategory, CreateDoubleStageSingleTargetBuff(230121, "幻影猫头鹰-双段回旋", 320, outbound.Id, inbound.Id));
            RegisterBuff(buffCategory, CreateSingleTargetDamageBuff(230122, "幻影猫头鹰-去程命中", 10));
            RegisterBuff(buffCategory, CreateSingleTargetDamageBuff(230123, "幻影猫头鹰-回程命中", 10));
            RegisterBuff(
                buffCategory,
                CreateStandardAiBuff(
                    PhantomOwlAiBuffId,
                    "幻影猫头鹰AI",
                    new AI_PhantomOwlCombat
                    {
                        ThinkIntervalMs = 140,
                        MainSpellId = dualHit.Id,
                    },
                    patrolMinRadius: 1.5f,
                    patrolMaxRadius: 6f,
                    aggroRange: 11f,
                    returnDistance: 20f,
                    idleMinMs: 650,
                    idleMaxMs: 1300));
        }

        private static void RegisterHeavyGator(SpellConfigCategory spellCategory, BuffConfigCategory buffCategory)
        {
            SpellConfig spray = new SpellConfig
            {
                Id = 130131,
                BuffId = 230131,
                Desc = "重装鳄鱼-前方扇形喷射",
                CD = 2200,
                TargetSelector = new TargetSelectorSector
                {
                    UnitType = UnitType.Player,
                    MaxDistance = 4200,
                    Radius = 4200,
                    Angle = 90,
                },
            };

            RegisterSpell(spellCategory, spray);
            RegisterBuff(buffCategory, CreateMultiTargetDamageBuff(230131, "重装鳄鱼-前方扇形喷射", 20));
            RegisterBuff(
                buffCategory,
                CreateStandardAiBuff(
                    HeavyGatorAiBuffId,
                    "重装鳄鱼AI",
                    new AI_HeavyGatorCombat
                    {
                        ThinkIntervalMs = 260,
                        MainSpellId = spray.Id,
                    },
                    patrolMinRadius: 0.6f,
                    patrolMaxRadius: 3.5f,
                    aggroRange: 9f,
                    returnDistance: 14f,
                    idleMinMs: 900,
                    idleMaxMs: 1600));
        }

        private static void RegisterBaloo(SpellConfigCategory spellCategory, BuffConfigCategory buffCategory)
        {
            SpellConfig punchHit = CreateSingleTargetDamageSpell(130142, 230142, "巴鲁-弹簧拳命中", 0, 3400, 0);
            SpellConfig punch = CreateSingleTargetWindupSpell(130141, 230141, "巴鲁-弹簧拳前摇", 1500, 3600, 0);
            SpellConfig bombard = new SpellConfig
            {
                Id = 130143,
                BuffId = 230143,
                Desc = "巴鲁-积木雨轰炸",
                CD = 2200,
                TargetSelector = new TargetSelectorPosition
                {
                    MaxDistance = 10,
                },
            };

            RegisterSpell(spellCategory, punch);
            RegisterSpell(spellCategory, punchHit);
            RegisterSpell(spellCategory, bombard);
            RegisterBuff(buffCategory, CreateSingleTargetWindupBuff(230141, "巴鲁-弹簧拳前摇", 480, punchHit.Id));
            RegisterBuff(buffCategory, CreateSingleTargetDamageBuff(230142, "巴鲁-弹簧拳命中", 26));
            RegisterBuff(buffCategory, CreateTargetPointExplosionBuff(230143, "巴鲁-积木雨轰炸", 900, 3.1f, 22));
            RegisterBuff(
                buffCategory,
                CreateStandardAiBuff(
                    BalooAiBuffId,
                    "巴鲁AI",
                    new AI_BalooCombat
                    {
                        ThinkIntervalMs = 260,
                        MainSpellId = punch.Id,
                        AltSpellId = bombard.Id,
                        EnrageHpPermille = 450,
                        EnrageThinkIntervalMs = 160,
                        EnrageSpeedPct = 135,
                    },
                    patrolMinRadius: 0.3f,
                    patrolMaxRadius: 2.2f,
                    aggroRange: 10f,
                    returnDistance: 12f,
                    idleMinMs: 1000,
                    idleMaxMs: 1800));
        }

        private static void RegisterSpell(SpellConfigCategory spellCategory, SpellConfig spellConfig)
        {
            if (spellCategory == null || spellConfig == null || spellCategory.Contain(spellConfig.Id))
            {
                return;
            }

            if (spellConfig.TargetSelector != null)
            {
                BTNodeIdHelper.EnsureIds(spellConfig.TargetSelector);
            }

            spellCategory.Add(spellConfig);
        }

        private static void RegisterBuff(BuffConfigCategory buffCategory, BuffConfig buffConfig)
        {
            if (buffCategory == null || buffConfig == null || buffCategory.Contain(buffConfig.Id))
            {
                return;
            }

            buffConfig.OnAfterDeserialize();
            buffCategory.Add(buffConfig);
        }

        private static void SetNumeric(NumericComponent numeric, int numericType, int value)
        {
            numeric.SetNoEvent(numericType, value);
        }

        private static SpellConfig CreateSingleTargetWindupSpell(int spellId, int buffId, string desc, int cd, int maxDistance, int minDistance)
        {
            return new SpellConfig
            {
                Id = spellId,
                BuffId = buffId,
                Desc = desc,
                CD = cd,
                TargetSelector = new TargetSelectorSingle
                {
                    UnitType = UnitType.Player,
                    MaxDistance = maxDistance,
                    MinDistance = minDistance,
                },
            };
        }

        private static SpellConfig CreateSingleTargetDamageSpell(int spellId, int buffId, string desc, int cd, int maxDistance, int minDistance)
        {
            return new SpellConfig
            {
                Id = spellId,
                BuffId = buffId,
                Desc = desc,
                CD = cd,
                TargetSelector = new TargetSelectorSingle
                {
                    UnitType = UnitType.Player,
                    MaxDistance = maxDistance,
                    MinDistance = minDistance,
                },
            };
        }

        private static BuffConfig CreateSingleTargetWindupBuff(int buffId, string desc, int durationMs, int followUpSpellId)
        {
            return new BuffConfig
            {
                Id = buffId,
                Desc = desc,
                Duration = durationMs,
                Effects = new List<EffectNode>
                {
                    new EffectServerBuffRemove
                    {
                        Children =
                        {
                            new BTCreateSpell
                            {
                                Unit = "Caster",
                                Buff = "Buff",
                                SpellConfigId = followUpSpellId,
                            },
                        },
                    },
                },
            };
        }

        private static BuffConfig CreateSingleTargetDamageBuff(int buffId, string desc, int damage)
        {
            return new BuffConfig
            {
                Id = buffId,
                Desc = desc,
                Duration = 0,
                Effects = new List<EffectNode>
                {
                    new EffectServerBuffAdd
                    {
                        Children =
                        {
                            new BTGetSpellTargetUnit
                            {
                                Buff = "Buff",
                                Unit = "Target",
                            },
                            new BTDamage
                            {
                                Caster = "Caster",
                                Target = "Target",
                                Buff = "Buff",
                                Value = damage,
                            },
                        },
                    },
                },
            };
        }

        private static BuffConfig CreateDoubleStageSingleTargetBuff(int buffId, string desc, int durationMs, int addSpellId, int removeSpellId)
        {
            return new BuffConfig
            {
                Id = buffId,
                Desc = desc,
                Duration = durationMs,
                Effects = new List<EffectNode>
                {
                    new EffectServerBuffAdd
                    {
                        Children =
                        {
                            new BTCreateSpell
                            {
                                Unit = "Caster",
                                Buff = "Buff",
                                SpellConfigId = addSpellId,
                            },
                        },
                    },
                    new EffectServerBuffRemove
                    {
                        Children =
                        {
                            new BTCreateSpell
                            {
                                Unit = "Caster",
                                Buff = "Buff",
                                SpellConfigId = removeSpellId,
                            },
                        },
                    },
                },
            };
        }

        private static BuffConfig CreateTargetPointExplosionBuff(int buffId, string desc, int durationMs, float radius, int damage)
        {
            return new BuffConfig
            {
                Id = buffId,
                Desc = desc,
                Duration = durationMs,
                Effects = new List<EffectNode>
                {
                    new EffectServerBuffRemove
                    {
                        Children =
                        {
                            new BTDamageSpellTargetCircle
                            {
                                Caster = "Caster",
                                Buff = "Buff",
                                UnitType = UnitType.Player,
                                Radius = radius,
                                Value = damage,
                            },
                        },
                    },
                },
            };
        }

        private static BuffConfig CreateMultiTargetDamageBuff(int buffId, string desc, int damage)
        {
            return new BuffConfig
            {
                Id = buffId,
                Desc = desc,
                Duration = 0,
                Effects = new List<EffectNode>
                {
                    new EffectServerBuffAdd
                    {
                        Children =
                        {
                            new BTGetSpellTargetUnits
                            {
                                Buff = "Buff",
                                Units = "Targets",
                            },
                            new BTForeachUnit
                            {
                                Units = "Targets",
                                Unit = "Target",
                                Children =
                                {
                                    new BTDamage
                                    {
                                        Caster = "Caster",
                                        Target = "Target",
                                        Buff = "Buff",
                                        Value = damage,
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        private static BuffConfig CreateStandardAiBuff(
            int buffId,
            string desc,
            BTCoroutine combatNode,
            float patrolMinRadius,
            float patrolMaxRadius,
            float aggroRange,
            float returnDistance,
            int idleMinMs,
            int idleMaxMs)
        {
            return new BuffConfig
            {
                Id = buffId,
                Desc = desc,
                Duration = -1,
                TickTime = 400,
                Effects = new List<EffectNode>
                {
                    new EffectServerBuffTick
                    {
                        Override = true,
                        Children =
                        {
                            new BTGetBuffOwner
                            {
                                Buff = "Buff",
                                Unit = "Unit",
                            },
                            new BTSelector
                            {
                                Children =
                                {
                                    new BTSequence
                                    {
                                        Children =
                                        {
                                            new AI_MonsterReturnCheck
                                            {
                                                Buff = "Buff",
                                                ReturnDistance = returnDistance,
                                            },
                                            new AI_MonsterReturn
                                            {
                                                Buff = "Buff",
                                                WaitIntervalMs = 800,
                                            },
                                        },
                                    },
                                    new BTSequence
                                    {
                                        Children =
                                        {
                                            new BTHasThreat
                                            {
                                                Unit = "Unit",
                                            },
                                            combatNode,
                                        },
                                    },
                                    new BTSequence
                                    {
                                        Children =
                                        {
                                            new BTNot
                                            {
                                                Children =
                                                {
                                                    new BTHasThreat
                                                    {
                                                        Unit = "Unit",
                                                    },
                                                },
                                            },
                                            new AI_MonsterXunLuo
                                            {
                                                Buff = "Buff",
                                                PatrolMinRadius = patrolMinRadius,
                                                PatrolMaxRadius = patrolMaxRadius,
                                                AggroRange = aggroRange,
                                                IdleMinMs = idleMinMs,
                                                IdleMaxMs = idleMaxMs,
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }
    }
}
