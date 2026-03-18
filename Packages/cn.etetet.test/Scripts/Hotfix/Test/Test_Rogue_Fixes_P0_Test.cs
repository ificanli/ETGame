using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_Fixes_P0_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_Fixes_P0_Test));
            Scene scene = scope.TestFiber.Root;

            if (scene.CoroutineLockComponent == null)
            {
                scene.AddComponent<CoroutineLockComponent>();
            }

            RogueBuffConfigLoader.EnsureRegistered();
            RogueEffectGroupLoader.RegisterAll();

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Console("rogue config category is null");
                return 1;
            }

            int[] buffBackedOptionIds = { 1005, 1012, 1013, 1021, 1024, 1033, 1036, 1044, 1054, 1064 };
            for (int i = 0; i < buffBackedOptionIds.Length; ++i)
            {
                int optionId = buffBackedOptionIds[i];
                if (!configCategory.TryGetEffectGroup(optionId, out RogueEffectGroupConfig groupConfig) || groupConfig == null)
                {
                    Log.Console($"effect group missing: {optionId}");
                    return 10 + i;
                }

                if (groupConfig.Entries == null || groupConfig.Entries.Count != 1)
                {
                    Log.Console($"effect group entry count mismatch: optionId={optionId}, count={groupConfig.Entries?.Count ?? 0}");
                    return 30 + i;
                }

                RogueEffectEntryConfig entry = groupConfig.Entries[0];
                if (entry == null || entry.ExecuteType != RogueEffectExecuteType.AddBuff || entry.RefId != optionId)
                {
                    Log.Console($"effect group entry mismatch: optionId={optionId}, executeType={entry?.ExecuteType ?? 0}, refId={entry?.RefId ?? 0}");
                    return 50 + i;
                }

                if (!groupConfig.TryGetPreviewBuffConfigId(out int previewBuffConfigId) || previewBuffConfigId != optionId)
                {
                    Log.Console($"effect group preview buff mismatch: optionId={optionId}, buffConfigId={previewBuffConfigId}");
                    return 70 + i;
                }
            }

            Unit probabilityPlayer = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (probabilityPlayer == null)
            {
                Log.Console("probability player is null");
                return 100;
            }

            if (ApplyOption(probabilityPlayer, 1012) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1012 failed");
                return 101;
            }

            if (ApplyOption(probabilityPlayer, 1024) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1024 failed");
                return 102;
            }

            if (ApplyOption(probabilityPlayer, 1064) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1064 failed");
                return 103;
            }

            ContainerLootBuildContext lootContext = new ContainerLootBuildContext
            {
                Player = probabilityPlayer,
                LootTable = "dummy",
                Count = 1,
                AllowRepeat = true,
                ResultRollMultiplierPermille = 1000,
            };
            RogueEcaRuntimeHookHelper.ApplyContainerResultProbability(lootContext);
            if (lootContext.ResultRollMultiplierPermille != 2000)
            {
                Log.Console($"container multiplier mismatch: {lootContext.ResultRollMultiplierPermille}");
                return 104;
            }

            SearchMonsterSpawnContext spawnContext = new SearchMonsterSpawnContext
            {
                Player = probabilityPlayer,
                GroupId = "g1",
                SpawnCount = 1,
                ChancePermille = 500,
            };
            RogueEcaRuntimeHookHelper.ApplySearchMonsterProbability(spawnContext);
            if (spawnContext.ChancePermille != 1000)
            {
                Log.Console($"search monster chance mismatch: {spawnContext.ChancePermille}");
                return 105;
            }

            int probabilityMultiplier = RogueEffectQueryHelper.GetProbabilityMultiplierPermille(probabilityPlayer);
            if (probabilityMultiplier != 1500)
            {
                Log.Console($"general probability multiplier mismatch: {probabilityMultiplier}");
                return 106;
            }

            Unit temporaryKeyPlayer = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, addItemComponent: true, campId: 1);
            if (temporaryKeyPlayer == null)
            {
                Log.Console("temporary key player is null");
                return 110;
            }

            EffectRogueTemporaryKey temporaryKeyEffect = BuffConfigCategory.Instance.Get(1013)?.GetEffect<EffectRogueTemporaryKey>();
            if (temporaryKeyEffect == null || temporaryKeyEffect.ItemConfigId <= 0 || temporaryKeyEffect.Count <= 0)
            {
                Log.Console("rogue buff 1013 temporary key effect invalid");
                return 111;
            }

            if (ApplyOption(temporaryKeyPlayer, 1013) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1013 failed");
                return 112;
            }

            ItemComponent keyItemComponent = temporaryKeyPlayer.GetComponent<ItemComponent>();
            if (keyItemComponent == null || keyItemComponent.GetItemCount(temporaryKeyEffect.ItemConfigId) != temporaryKeyEffect.Count)
            {
                Log.Console($"temporary key item count mismatch after apply: {keyItemComponent?.GetItemCount(temporaryKeyEffect.ItemConfigId) ?? 0}");
                return 113;
            }

            RogueTemporaryItemStateComponent temporaryItemState = temporaryKeyPlayer.GetComponent<RogueTemporaryItemStateComponent>();
            if (temporaryItemState == null || temporaryItemState.Sources.Count != 1)
            {
                Log.Console($"temporary item state mismatch after apply: {temporaryItemState?.Sources.Count ?? 0}");
                return 114;
            }

            RogueEffectHelper.RemoveSelectedOption(temporaryKeyPlayer, temporaryKeyPlayer.GetComponent<RogueProgressComponent>(), 1013);
            if (keyItemComponent.GetItemCount(temporaryKeyEffect.ItemConfigId) != 0)
            {
                Log.Console($"temporary key item should be cleared on remove: {keyItemComponent.GetItemCount(temporaryKeyEffect.ItemConfigId)}");
                return 115;
            }

            if (temporaryKeyPlayer.GetComponent<RogueTemporaryItemStateComponent>() != null)
            {
                Log.Console("temporary item state should be removed on option remove");
                return 116;
            }

            if (ApplyOption(temporaryKeyPlayer, 1013) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1013 second time failed");
                return 117;
            }

            if (!ItemHelper.RemoveItem(keyItemComponent, temporaryKeyEffect.ItemConfigId, temporaryKeyEffect.Count, ItemChangeReason.UseItem))
            {
                Log.Console("remove temporary key item for consume failed");
                return 118;
            }

            RogueEcaRuntimeHookHelper.ConsumeTemporaryDoorKey(temporaryKeyPlayer, temporaryKeyEffect.ItemConfigId, temporaryKeyEffect.Count);
            if (keyItemComponent.GetItemCount(temporaryKeyEffect.ItemConfigId) != 0)
            {
                Log.Console($"temporary key item count mismatch after consume: {keyItemComponent.GetItemCount(temporaryKeyEffect.ItemConfigId)}");
                return 119;
            }

            if (temporaryKeyPlayer.GetComponent<RogueTemporaryItemStateComponent>() != null)
            {
                Log.Console("temporary item state should be removed after consume");
                return 120;
            }

            Unit objectivePlayer = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (objectivePlayer == null)
            {
                Log.Console("objective player is null");
                return 130;
            }

            if (ApplyOption(objectivePlayer, 1021) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1021 failed");
                return 131;
            }

            if (RogueEffectQueryHelper.GetMonsterDamageBonusPermille(objectivePlayer) != 0)
            {
                Log.Console("monster damage bonus should be 0 before objective complete");
                return 132;
            }

            RogueObjectiveComponent objectiveComponent = objectivePlayer.GetComponent<RogueObjectiveComponent>();
            RogueObjective objective = GetOnlyChild<RogueObjective>(objectiveComponent);
            if (objective == null || objective.GoalValue != 15 || objective.RewardBuffConfigId != 102101)
            {
                Log.Console($"objective data mismatch: goal={objective?.GoalValue ?? 0}, rewardBuffConfigId={objective?.RewardBuffConfigId ?? 0}");
                return 133;
            }

            for (int i = 0; i < objective.GoalValue; ++i)
            {
                RogueObjectiveHelper.OnKill(objectivePlayer, objectiveComponent, (int)UnitType.Monster);
            }

            if (!objective.Completed || !objective.RewardClaimed || objective.RewardBuffId <= 0)
            {
                Log.Console($"objective completion mismatch: completed={objective?.Completed}, rewardClaimed={objective?.RewardClaimed}, rewardBuffId={objective?.RewardBuffId ?? 0}");
                return 134;
            }

            if (RogueEffectQueryHelper.GetMonsterDamageBonusPermille(objectivePlayer) != 500)
            {
                Log.Console($"monster damage bonus mismatch after objective complete: {RogueEffectQueryHelper.GetMonsterDamageBonusPermille(objectivePlayer)}");
                return 135;
            }

            RogueProgressComponent objectiveProgress = objectivePlayer.GetComponent<RogueProgressComponent>();
            if (!objectiveProgress.AppliedBuffIds.Contains(objective.RewardBuffId))
            {
                Log.Console("reward buff id missing from applied buff ids");
                return 136;
            }

            RogueEffectHelper.RemoveSelectedOption(objectivePlayer, objectiveProgress, 1021);
            if (RogueEffectQueryHelper.GetMonsterDamageBonusPermille(objectivePlayer) != 0)
            {
                Log.Console("monster damage bonus should be cleared after remove");
                return 137;
            }

            if (objectivePlayer.GetComponent<RogueObjectiveComponent>() != null)
            {
                Log.Console("objective component should be removed after option remove");
                return 138;
            }

            Unit lowQualityPlayer = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (lowQualityPlayer == null)
            {
                Log.Console("low quality player is null");
                return 140;
            }

            if (ApplyOption(lowQualityPlayer, 1005) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1005 failed");
                return 141;
            }

            RogueEffectQueryHelper.GetContainerLowQualityGold(lowQualityPlayer, out int totalGold, out int highQualityThreshold);
            if (totalGold <= 0 || highQualityThreshold <= 0)
            {
                Log.Console($"container low quality gold effect invalid: gold={totalGold}, threshold={highQualityThreshold}");
                return 142;
            }

            if (!TryFindItemConfigIdByQuality(false, highQualityThreshold, out int lowQualityItemConfigId) ||
                !TryFindItemConfigIdByQuality(true, highQualityThreshold, out int highQualityItemConfigId))
            {
                Log.Console("not found test item config by quality");
                return 143;
            }

            ECAPointComponent lowQualityPoint = CreateContainerPoint(scene, "rogue_low_quality_1");
            ContainerComponent lowQualityContainer = lowQualityPoint.GetParent<Unit>().GetComponent<ContainerComponent>();
            lowQualityContainer.SetItem(0, lowQualityItemConfigId, 1);

            RogueProgressComponent lowQualityProgress = lowQualityPlayer.GetComponent<RogueProgressComponent>();
            int grantedGold = RogueEcaRuntimeHookHelper.TryGrantContainerLowQualityGold(lowQualityPoint, lowQualityPlayer);
            if (grantedGold != totalGold || lowQualityProgress.CurrentGold != totalGold)
            {
                Log.Console($"container low quality gold mismatch: granted={grantedGold}, currentGold={lowQualityProgress.CurrentGold}, expected={totalGold}");
                return 144;
            }

            ECAPointComponent highQualityPoint = CreateContainerPoint(scene, "rogue_high_quality_1");
            ContainerComponent highQualityContainer = highQualityPoint.GetParent<Unit>().GetComponent<ContainerComponent>();
            highQualityContainer.SetItem(0, highQualityItemConfigId, 1);

            int highQualityGrantedGold = RogueEcaRuntimeHookHelper.TryGrantContainerLowQualityGold(highQualityPoint, lowQualityPlayer);
            if (highQualityGrantedGold != 0 || lowQualityProgress.CurrentGold != totalGold)
            {
                Log.Console($"high quality container should not grant gold: granted={highQualityGrantedGold}, currentGold={lowQualityProgress.CurrentGold}");
                return 145;
            }

            Unit critPlayer = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            Unit enemyHero = TestHelper.CreateServerUnit(scene, UnitType.Player, campId: 2);
            Unit enemyMonster = TestHelper.CreateServerUnit(scene, UnitType.Monster, campId: 2);
            if (critPlayer == null || enemyHero == null || enemyMonster == null)
            {
                Log.Console("crit growth test units are null");
                return 150;
            }

            if (ApplyOption(critPlayer, 1033) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1033 failed");
                return 151;
            }

            DamageContext hitMonsterContext = new DamageContext
            {
                IsBullet = true,
                SourceUnitId = critPlayer.Id,
                TargetUnitId = enemyMonster.Id,
            };
            if (RogueHitHeroCritGrowthHelper.TryApply(hitMonsterContext, scene.GetComponent<UnitComponent>()))
            {
                Log.Console("hit monster should not trigger crit growth");
                return 152;
            }

            if (RogueEffectQueryHelper.GetHitHeroCritPermille(critPlayer) != 0)
            {
                Log.Console($"crit growth should remain 0 after hitting monster: {RogueEffectQueryHelper.GetHitHeroCritPermille(critPlayer)}");
                return 153;
            }

            DamageContext hitHeroContext = new DamageContext
            {
                IsBullet = true,
                SourceUnitId = critPlayer.Id,
                TargetUnitId = enemyHero.Id,
            };
            if (!RogueHitHeroCritGrowthHelper.TryApply(hitHeroContext, scene.GetComponent<UnitComponent>()))
            {
                Log.Console("hit enemy hero should trigger crit growth");
                return 154;
            }

            if (RogueEffectQueryHelper.GetHitHeroCritPermille(critPlayer) != 10)
            {
                Log.Console($"crit growth mismatch after hitting hero: {RogueEffectQueryHelper.GetHitHeroCritPermille(critPlayer)}");
                return 155;
            }

            Unit reloadPlayer = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (reloadPlayer == null)
            {
                Log.Console("reload player is null");
                return 160;
            }

            if (ApplyOption(reloadPlayer, 1036) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1036 failed");
                return 161;
            }

            RogueReloadFirstShotsStateComponent reloadState = reloadPlayer.GetComponent<RogueReloadFirstShotsStateComponent>();
            if (reloadState == null || reloadState.Sources.Count != 1)
            {
                Log.Console($"reload state source mismatch: {reloadState?.Sources.Count ?? 0}");
                return 162;
            }

            if (!RogueReloadFirstShotsEventHelper.TryActivate(reloadPlayer))
            {
                Log.Console("reload activation failed");
                return 163;
            }

            reloadState.ConsumeShot(out int damageBonus1, out int penetration1);
            reloadState.ConsumeShot(out int damageBonus2, out int penetration2);
            reloadState.ConsumeShot(out int damageBonus3, out int penetration3);
            reloadState.ConsumeShot(out int damageBonus4, out int penetration4);
            if (damageBonus1 != 2000 || penetration1 != 1 ||
                damageBonus2 != 2000 || penetration2 != 1 ||
                damageBonus3 != 2000 || penetration3 != 1 ||
                damageBonus4 != 0 || penetration4 != 0)
            {
                Log.Console($"reload shot data mismatch: [{damageBonus1},{penetration1}] [{damageBonus2},{penetration2}] [{damageBonus3},{penetration3}] [{damageBonus4},{penetration4}]");
                return 164;
            }

            Unit extendTimePlayer = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (extendTimePlayer == null)
            {
                Log.Console("extend time player is null");
                return 170;
            }

            if (ApplyOption(extendTimePlayer, 1044) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1044 failed");
                return 171;
            }

            if (RogueEffectQueryHelper.GetExtendGameTimeMs(extendTimePlayer) != 300000)
            {
                Log.Console($"extend game time mismatch: {RogueEffectQueryHelper.GetExtendGameTimeMs(extendTimePlayer)}");
                return 172;
            }

            EvacuationDurationAdjustContext evacuationContext = new EvacuationDurationAdjustContext
            {
                Player = extendTimePlayer,
                DurationMs = 10000,
            };
            RogueEcaRuntimeHookHelper.ApplyExtendGameTime(evacuationContext);
            if (evacuationContext.DurationMs != 310000)
            {
                Log.Console($"evacuation duration mismatch: {evacuationContext.DurationMs}");
                return 173;
            }

            Unit sizeAttacker = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            Unit sizeTarget = TestHelper.CreateServerUnit(scene, UnitType.Monster, campId: 2);
            if (sizeAttacker == null || sizeTarget == null)
            {
                Log.Console("size difference test units are null");
                return 180;
            }

            int baseSpeedFinalPct = sizeAttacker.NumericComponent?.GetAsInt(NumericType.SpeedFinalPct) ?? 0;
            if (ApplyOption(sizeAttacker, 1054) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1054 failed");
                return 181;
            }

            int currentSpeedFinalPct = sizeAttacker.NumericComponent?.GetAsInt(NumericType.SpeedFinalPct) ?? 0;
            if (currentSpeedFinalPct != baseSpeedFinalPct + 30)
            {
                Log.Console($"speed final pct mismatch after 1054 apply: before={baseSpeedFinalPct}, after={currentSpeedFinalPct}");
                return 182;
            }

            if (RogueEffectQueryHelper.GetScaleModifierPermille(sizeAttacker) != -200)
            {
                Log.Console($"scale modifier mismatch after 1054 apply: {RogueEffectQueryHelper.GetScaleModifierPermille(sizeAttacker)}");
                return 183;
            }

            DamageContext sizeDamageContext = new DamageContext
            {
                BaseDamage = 100,
                FinalDamage = 100,
                SourceUnitId = sizeAttacker.Id,
                TargetUnitId = sizeTarget.Id,
            };
            RogueBeforeDamageApply_DamageModifiers.ApplyAttackerModifiers(sizeAttacker, sizeTarget, sizeDamageContext);
            if (sizeDamageContext.FinalDamage != 120)
            {
                Log.Console($"size difference damage mismatch: {sizeDamageContext.FinalDamage}");
                return 184;
            }

            RogueEffectHelper.RemoveSelectedOption(sizeAttacker, sizeAttacker.GetComponent<RogueProgressComponent>(), 1054);
            int revertedSpeedFinalPct = sizeAttacker.NumericComponent?.GetAsInt(NumericType.SpeedFinalPct) ?? 0;
            if (revertedSpeedFinalPct != baseSpeedFinalPct)
            {
                Log.Console($"speed final pct should revert after 1054 remove: before={baseSpeedFinalPct}, after={revertedSpeedFinalPct}");
                return 185;
            }

            return ErrorCode.ERR_Success;
        }

        private static int ApplyOption(Unit unit, int optionId)
        {
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            RogueProgressComponent progress = unit?.GetComponent<RogueProgressComponent>() ?? RogueProgressHelper.EnsureProgress(unit, false);
            if (unit == null || unit.IsDisposed || progress == null || configCategory == null)
            {
                return ErrorCode.ERR_Cancel;
            }

            if (!configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) || optionConfig == null)
            {
                return ErrorCode.ERR_RogueChoiceOptionInvalid;
            }

            return RogueEffectHelper.ApplySelectedOption(unit, progress, optionConfig, optionId, null);
        }

        private static T GetOnlyChild<T>(Entity entity) where T : Entity
        {
            if (entity == null || entity.Children == null)
            {
                return null;
            }

            foreach (Entity child in entity.Children.Values)
            {
                if (child is T result)
                {
                    return result;
                }
            }

            return null;
        }

        private static bool TryFindItemConfigIdByQuality(bool highQuality, int threshold, out int configId)
        {
            configId = 0;
            foreach (ItemConfig itemConfig in ItemConfigCategory.Instance.DataList)
            {
                if (itemConfig == null || itemConfig.Id <= 0)
                {
                    continue;
                }

                if (highQuality)
                {
                    if (itemConfig.Quality >= threshold)
                    {
                        configId = itemConfig.Id;
                        return true;
                    }

                    continue;
                }

                if (itemConfig.Quality > 0 && itemConfig.Quality < threshold)
                {
                    configId = itemConfig.Id;
                    return true;
                }
            }

            return false;
        }

        private static ECAPointComponent CreateContainerPoint(Scene scene, string pointId)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            Unit pointUnit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), 0);
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>(pointId, ECAPointType.Container, 3f);
            point.Params = new List<FlowParam>();
            pointUnit.AddComponent<ContainerComponent, string>(pointId);
            return point;
        }
    }
}
