using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

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

            if (scene.TimerComponent == null)
            {
                scene.AddComponent<TimerComponent>();
            }

            EntityRef<Scene> sceneRef = scene;
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

            int areaDiscoveryError = await VerifyAreaDiscoveryFix(scene);
            if (areaDiscoveryError != ErrorCode.ERR_Success)
            {
                return areaDiscoveryError;
            }

            scene = sceneRef;
            int bossHunterError = await VerifyBossHunterFix(scene);
            if (bossHunterError != ErrorCode.ERR_Success)
            {
                return bossHunterError;
            }

            scene = sceneRef;
            int weaponEnchantError = await VerifyWeaponEnchantFix(scene);
            if (weaponEnchantError != ErrorCode.ERR_Success)
            {
                return weaponEnchantError;
            }

            scene = sceneRef;
            int trapMasterError = await VerifyTrapMasterFix(scene);
            if (trapMasterError != ErrorCode.ERR_Success)
            {
                return trapMasterError;
            }

            scene = sceneRef;
            int runTimeLimitError = await VerifyRunTimeLimitFix(scene);
            if (runTimeLimitError != ErrorCode.ERR_Success)
            {
                return runTimeLimitError;
            }

            scene = sceneRef;
            int instantKillError = await VerifyInstantKillBossFilterFix(scene);
            if (instantKillError != ErrorCode.ERR_Success)
            {
                return instantKillError;
            }

            return ErrorCode.ERR_Success;
        }

        private static async ETTask<int> VerifyAreaDiscoveryFix(Scene scene)
        {
            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (player == null)
            {
                Log.Console("area discovery player is null");
                return 190;
            }

            player.Position = new float3(1000f, 0f, 1000f);

            if (ApplyOption(player, 1007) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1007 failed");
                return 191;
            }

            RogueProgressComponent progress = player.GetComponent<RogueProgressComponent>();
            if (progress == null)
            {
                Log.Console("area discovery progress is null");
                return 192;
            }

            ECAPointComponent point = CreateRangeTriggerPoint(scene, "rogue_area_fix_alpha", player.Position);
            if (point == null)
            {
                Log.Console("area discovery point is null");
                return 193;
            }

            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> playerRef = player;
            EntityRef<ECAPointComponent> pointRef = point;
            ECAPointPlayerEnterEvent_RogueAreaDiscovery handler = new ECAPointPlayerEnterEvent_RogueAreaDiscovery();
            await handler.Handle(scene, new ECAPointPlayerEnterEvent
            {
                Point = point,
                Player = player,
            });

            scene = sceneRef;
            player = playerRef;
            point = pointRef;

            RogueProgressComponent firstEnterProgress = player.GetComponent<RogueProgressComponent>();
            if (firstEnterProgress == null || firstEnterProgress.CurrentGold != 800)
            {
                Log.Console($"area discovery first reward mismatch: {firstEnterProgress?.CurrentGold ?? 0}");
                return 194;
            }

            await handler.Handle(scene, new ECAPointPlayerEnterEvent
            {
                Point = point,
                Player = player,
            });

            scene = sceneRef;
            player = playerRef;
            RogueAreaDiscoveryComponent areaDiscovery = player.GetComponent<RogueAreaDiscoveryComponent>();
            RogueProgressComponent secondEnterProgress = player.GetComponent<RogueProgressComponent>();
            if (secondEnterProgress == null ||
                secondEnterProgress.CurrentGold != 800 ||
                areaDiscovery == null ||
                areaDiscovery.VisitedAreaIds.Count != 1)
            {
                Log.Console($"area discovery dedupe mismatch: gold={secondEnterProgress?.CurrentGold ?? 0}, visited={areaDiscovery?.VisitedAreaIds.Count ?? 0}");
                return 195;
            }

            return ErrorCode.ERR_Success;
        }

        private static async ETTask<int> VerifyBossHunterFix(Scene scene)
        {
            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (player == null)
            {
                Log.Console("boss hunter player is null");
                return 200;
            }

            player.Position = new float3(1100f, 0f, 1100f);

            if (ApplyOption(player, 1022) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1022 failed");
                return 201;
            }

            Unit boss = CreateProfiledMonster(scene, MonsterRuntimeProfileHelper.BalooGroupId, player.Position + new float3(3f, 0f, 0f));
            Unit normalMonster = CreateProfiledMonster(scene, MonsterRuntimeProfileHelper.BladeCatGroupId, player.Position + new float3(6f, 0f, 0f));
            if (boss == null || normalMonster == null)
            {
                Log.Console("boss hunter test monsters are null");
                return 202;
            }

            EntityRef<Scene> sceneRef = scene;
            long playerId = player.Id;
            long bossId = boss.Id;
            long normalMonsterId = normalMonster.Id;
            RogueBeforeDamageApply_DamageModifiers handler = new RogueBeforeDamageApply_DamageModifiers();

            DamageContext bossAttackContext = new DamageContext
            {
                SourceUnitId = bossId,
                TargetUnitId = playerId,
                BaseDamage = 1000,
                FinalDamage = 1000,
            };
            await handler.Handle(scene, new RogueBeforeDamageApply { Context = bossAttackContext });
            if (bossAttackContext.FinalDamage != 700)
            {
                Log.Console($"boss reduction mismatch: {bossAttackContext.FinalDamage}");
                return 203;
            }

            scene = sceneRef;
            DamageContext normalAttackContext = new DamageContext
            {
                SourceUnitId = normalMonsterId,
                TargetUnitId = playerId,
                BaseDamage = 1000,
                FinalDamage = 1000,
            };
            await handler.Handle(scene, new RogueBeforeDamageApply { Context = normalAttackContext });
            if (normalAttackContext.FinalDamage != 1000)
            {
                Log.Console($"non-boss reduction should not apply: {normalAttackContext.FinalDamage}");
                return 204;
            }

            scene = sceneRef;
            DamageContext attackBossContext = new DamageContext
            {
                SourceUnitId = playerId,
                TargetUnitId = bossId,
                BaseDamage = 100,
                FinalDamage = 100,
                IsBullet = true,
            };
            await handler.Handle(scene, new RogueBeforeDamageApply { Context = attackBossContext });
            if (attackBossContext.LifeStealPermille != 300)
            {
                Log.Console($"boss lifesteal mismatch: {attackBossContext.LifeStealPermille}");
                return 205;
            }

            scene = sceneRef;
            DamageContext attackNormalContext = new DamageContext
            {
                SourceUnitId = playerId,
                TargetUnitId = normalMonsterId,
                BaseDamage = 100,
                FinalDamage = 100,
                IsBullet = true,
            };
            await handler.Handle(scene, new RogueBeforeDamageApply { Context = attackNormalContext });
            if (attackNormalContext.LifeStealPermille != 0)
            {
                Log.Console($"non-boss lifesteal should not apply: {attackNormalContext.LifeStealPermille}");
                return 206;
            }

            return ErrorCode.ERR_Success;
        }

        private static async ETTask<int> VerifyWeaponEnchantFix(Scene scene)
        {
            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (player == null)
            {
                Log.Console("weapon enchant player is null");
                return 210;
            }

            player.Position = new float3(1200f, 0f, 1200f);

            if (ApplyOption(player, 1025) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1025 failed");
                return 211;
            }

            if (!TryGetTwoWeaponConfigIds(out int mainWeaponId, out int subWeaponId))
            {
                Log.Console("weapon enchant test weapon configs missing");
                return 212;
            }

            InitializeWeapons(player, mainWeaponId, subWeaponId);
            WeaponComponent weaponComponent = player.GetComponent<WeaponComponent>();
            if (weaponComponent == null || weaponComponent.CurrentSlot != 1)
            {
                Log.Console("weapon enchant weapon component invalid");
                return 213;
            }

            float baseMainDamage = weaponComponent.GetEffectiveDamage(1);
            float baseSubDamage = weaponComponent.GetEffectiveDamage(2);
            if (baseMainDamage <= 0f || baseSubDamage <= 0f)
            {
                Log.Console($"weapon enchant base damage invalid: main={baseMainDamage}, sub={baseSubDamage}");
                return 214;
            }

            UnitDieEvent_RogueOnKillEffects handler = new UnitDieEvent_RogueOnKillEffects();
            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> playerRef = player;
            long playerId = player.Id;

            Unit normalMonster = CreateProfiledMonster(scene, MonsterRuntimeProfileHelper.BladeCatGroupId, player.Position + new float3(3f, 0f, 0f));
            if (normalMonster == null)
            {
                Log.Console("weapon enchant normal monster is null");
                return 215;
            }

            long normalMonsterId = normalMonster.Id;
            int normalMonsterUnitType = (int)normalMonster.UnitType;

            Unit eliteMonster = CreateProfiledMonster(scene, MonsterRuntimeProfileHelper.HeavyGatorGroupId, player.Position + new float3(6f, 0f, 0f));
            if (eliteMonster == null)
            {
                Log.Console("weapon enchant elite monster is null");
                return 217;
            }

            long eliteMonsterId = eliteMonster.Id;
            int eliteMonsterUnitType = (int)eliteMonster.UnitType;
            UnitDie normalKillArgs = new UnitDie
            {
                Unit = player,
                Target = normalMonster,
                TargetId = normalMonsterId,
                TargetUnitType = normalMonsterUnitType,
            };
            UnitDie eliteKillArgs = new UnitDie
            {
                Unit = player,
                Target = eliteMonster,
                TargetId = eliteMonsterId,
                TargetUnitType = eliteMonsterUnitType,
            };

            await handler.Handle(scene, normalKillArgs);

            scene = sceneRef;
            player = playerRef;
            RogueWeaponModifierComponent modifierComponent = player.GetComponent<RogueWeaponModifierComponent>();
            if (modifierComponent != null &&
                (modifierComponent.GetModifier(1, WeaponModType.BulletDamage) != 0 ||
                 modifierComponent.GetModifier(1, WeaponModType.Penetration) != 0 ||
                 modifierComponent.GetModifier(2, WeaponModType.BulletDamage) != 0 ||
                 modifierComponent.GetModifier(2, WeaponModType.Penetration) != 0))
            {
                Log.Console("normal monster kill should not grant weapon enchant");
                return 216;
            }

            await handler.Handle(scene, eliteKillArgs);

            scene = sceneRef;
            player = playerRef;
            modifierComponent = player.GetComponent<RogueWeaponModifierComponent>();
            if (modifierComponent == null)
            {
                Log.Console("weapon enchant modifier component is null after elite kill");
                return 218;
            }

            int slot1DamageModifier = modifierComponent.GetModifier(1, WeaponModType.BulletDamage);
            int slot1PenetrationModifier = modifierComponent.GetModifier(1, WeaponModType.Penetration);
            if (slot1DamageModifier <= 0 && slot1PenetrationModifier <= 0)
            {
                Log.Console($"weapon enchant modifier missing on slot1: damage={slot1DamageModifier}, penetration={slot1PenetrationModifier}");
                return 219;
            }

            if (modifierComponent.GetModifier(2, WeaponModType.BulletDamage) != 0 ||
                modifierComponent.GetModifier(2, WeaponModType.Penetration) != 0)
            {
                Log.Console("weapon enchant should not apply to slot2");
                return 220;
            }

            weaponComponent = player.GetComponent<WeaponComponent>();
            if (slot1DamageModifier > 0 && weaponComponent.GetEffectiveDamage(1) <= baseMainDamage)
            {
                Log.Console($"slot1 damage should increase after enchant: before={baseMainDamage}, after={weaponComponent.GetEffectiveDamage(1)}");
                return 221;
            }

            if (slot1PenetrationModifier > 0 && weaponComponent.GetEffectivePenetrationCount(1) < slot1PenetrationModifier)
            {
                Log.Console($"slot1 penetration should increase after enchant: actual={weaponComponent.GetEffectivePenetrationCount(1)}, expected>={slot1PenetrationModifier}");
                return 222;
            }

            if (weaponComponent.GetEffectiveDamage(2) != baseSubDamage || weaponComponent.GetEffectivePenetrationCount(2) != 0)
            {
                Log.Console($"slot2 runtime stats should remain unchanged: damage={weaponComponent.GetEffectiveDamage(2)}, penetration={weaponComponent.GetEffectivePenetrationCount(2)}");
                return 223;
            }

            return ErrorCode.ERR_Success;
        }

        private static async ETTask<int> VerifyTrapMasterFix(Scene scene)
        {
            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (player == null)
            {
                Log.Console("trap master player is null");
                return 230;
            }

            player.Position = new float3(1300f, 0f, 1300f);

            if (!TryGetTwoWeaponConfigIds(out int mainWeaponId, out int subWeaponId))
            {
                Log.Console("trap master test weapon configs missing");
                return 231;
            }

            InitializeWeapons(player, mainWeaponId, subWeaponId);
            WeaponComponent weaponComponent = player.GetComponent<WeaponComponent>();
            float weaponDamage = weaponComponent?.GetEffectiveDamage(1) ?? 0f;
            if (weaponComponent == null || weaponDamage <= 0f)
            {
                Log.Console($"trap master weapon runtime invalid: damage={weaponDamage}");
                return 232;
            }

            if (ApplyOption(player, 1035) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1035 failed");
                return 233;
            }

            RogueTrapMasterStateComponent trapState = player.GetComponent<RogueTrapMasterStateComponent>();
            if (trapState == null || trapState.EffectiveRequiredShotCount != 15 || trapState.EffectiveTrapDamagePermille != 3000)
            {
                Log.Console($"trap state mismatch: shots={trapState?.EffectiveRequiredShotCount ?? 0}, damage={trapState?.EffectiveTrapDamagePermille ?? 0}");
                return 234;
            }

            Unit monster = CreateProfiledMonster(scene, MonsterRuntimeProfileHelper.BladeCatGroupId, player.Position + new float3(1f, 0f, 0f));
            if (monster == null)
            {
                Log.Console("trap master target monster is null");
                return 235;
            }

            EntityRef<Scene> sceneRef = scene;
            long playerId = player.Id;
            long monsterId = monster.Id;
            long beforeHp = monster.NumericComponent?.GetAsLong(NumericType.HP) ?? 0;
            trapState.LastMoveTime = TimeInfo.Instance.ServerNow() - trapState.EffectiveIdleMs;
            int requiredShotCount = trapState.EffectiveRequiredShotCount;
            UnitWeaponFired fireArgs = new UnitWeaponFired
            {
                Caster = player,
                SlotIndex = 1,
                WeaponId = mainWeaponId,
                Damage = weaponDamage,
            };

            UnitWeaponFired_RogueTrapMaster handler = new UnitWeaponFired_RogueTrapMaster();
            for (int i = 0; i < requiredShotCount - 1; ++i)
            {
                await handler.Handle(scene, fireArgs);
                scene = sceneRef;
            }

            if (FindTrapEntity(scene, playerId) != null)
            {
                Log.Console("trap should not be created before reaching shot threshold");
                return 236;
            }

            await handler.Handle(scene, fireArgs);

            scene = sceneRef;
            RogueTrapEntity trapEntity = FindTrapEntity(scene, playerId);
            if (trapEntity == null || trapEntity.Damage <= 0)
            {
                Log.Console($"trap entity invalid after trigger: damage={trapEntity?.Damage ?? 0}");
                return 237;
            }

            trapEntity.Tick();

            Unit monsterAfterTrigger = scene.GetComponent<UnitComponent>()?.Get(monsterId);
            long afterHp = monsterAfterTrigger == null || monsterAfterTrigger.IsDisposed
                ? 0
                : (monsterAfterTrigger.NumericComponent?.GetAsLong(NumericType.HP) ?? 0);
            if (afterHp >= beforeHp)
            {
                Log.Console($"trap damage should reduce monster hp: before={beforeHp}, after={afterHp}");
                return 238;
            }

            if (!trapEntity.IsDisposed)
            {
                Log.Console("trap entity should dispose after triggering");
                return 239;
            }

            return ErrorCode.ERR_Success;
        }

        private static async ETTask<int> VerifyRunTimeLimitFix(Scene scene)
        {
            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (player == null)
            {
                Log.Console("run time limit player is null");
                return 250;
            }

            if (ApplyOption(player, 1044) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1044 for run time limit failed");
                return 251;
            }

            long expectedExtendedDuration = RunTimeLimitConst.PlayerTimeoutMs + 300000;
            if (RogueRunTimeLimitHelper.GetEffectiveDurationMs(player) != expectedExtendedDuration)
            {
                Log.Console($"effective run time duration mismatch before enter map: {RogueRunTimeLimitHelper.GetEffectiveDurationMs(player)}");
                return 252;
            }

            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> playerRef = player;
            PlayerEnterMap_RunTimeLimit handler = new PlayerEnterMap_RunTimeLimit();
            await handler.Handle(scene, new PlayerEnterMap
            {
                Unit = player,
                MapName = "RogueFixMap",
            });

            scene = sceneRef;
            player = playerRef;
            RunTimeLimitComponent runTimeLimit = player.GetComponent<RunTimeLimitComponent>();
            if (runTimeLimit == null || runTimeLimit.DurationMs != expectedExtendedDuration)
            {
                Log.Console($"run time limit duration mismatch after map enter: {runTimeLimit?.DurationMs ?? 0}");
                return 253;
            }

            if (runTimeLimit.DeadlineTime - runTimeLimit.StartTime != expectedExtendedDuration)
            {
                Log.Console($"run time limit deadline mismatch: start={runTimeLimit.StartTime}, deadline={runTimeLimit.DeadlineTime}, duration={runTimeLimit.DurationMs}");
                return 254;
            }

            RogueEffectHelper.RemoveSelectedOption(player, player.GetComponent<RogueProgressComponent>(), 1044);
            if (RogueEffectQueryHelper.GetExtendGameTimeMs(player) != 0)
            {
                Log.Console($"extend game time should be cleared after remove: {RogueEffectQueryHelper.GetExtendGameTimeMs(player)}");
                return 255;
            }

            runTimeLimit = player.GetComponent<RunTimeLimitComponent>();
            if (runTimeLimit == null || runTimeLimit.DurationMs != RunTimeLimitConst.PlayerTimeoutMs)
            {
                Log.Console($"run time limit should revert to base duration: {runTimeLimit?.DurationMs ?? 0}");
                return 256;
            }

            return ErrorCode.ERR_Success;
        }

        private static async ETTask<int> VerifyInstantKillBossFilterFix(Scene scene)
        {
            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (player == null)
            {
                Log.Console("instant kill player is null");
                return 260;
            }

            player.Position = new float3(1400f, 0f, 1400f);

            if (ApplyOption(player, 1063) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1063 failed");
                return 261;
            }

            RogueBuffPassiveRuntimeComponent passiveRuntime = player.GetComponent<RogueBuffPassiveRuntimeComponent>();
            RogueProgressComponent progress = player.GetComponent<RogueProgressComponent>();
            if (passiveRuntime == null || progress == null || passiveRuntime.InstantKillBySource.Count == 0)
            {
                Log.Console("instant kill passive runtime is invalid");
                return 262;
            }

            foreach (long sourceId in new List<long>(passiveRuntime.InstantKillBySource.Keys))
            {
                RogueInstantKillSourceData sourceData = passiveRuntime.InstantKillBySource[sourceId];
                sourceData.ChancePermille = 1000;
                passiveRuntime.InstantKillBySource[sourceId] = sourceData;
            }

            Unit normalMonster = CreateProfiledMonster(scene, MonsterRuntimeProfileHelper.BladeCatGroupId, player.Position + new float3(3f, 0f, 0f));
            Unit boss = CreateProfiledMonster(scene, MonsterRuntimeProfileHelper.BalooGroupId, player.Position + new float3(6f, 0f, 0f));
            if (normalMonster == null || boss == null)
            {
                Log.Console("instant kill test monsters are null");
                return 263;
            }

            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> playerRef = player;
            long playerId = player.Id;
            long normalMonsterId = normalMonster.Id;
            long bossId = boss.Id;
            RogueBeforeDamageApply_DamageModifiers handler = new RogueBeforeDamageApply_DamageModifiers();
            long normalHp = normalMonster.NumericComponent?.GetAsLong(NumericType.HP) ?? 0;
            DamageContext normalContext = new DamageContext
            {
                SourceUnitId = playerId,
                TargetUnitId = normalMonsterId,
                BaseDamage = 1,
                FinalDamage = 1,
                IsBullet = true,
            };
            await handler.Handle(scene, new RogueBeforeDamageApply { Context = normalContext });
            if (normalContext.FinalDamage != normalHp)
            {
                Log.Console($"instant kill should execute normal monster: damage={normalContext.FinalDamage}, hp={normalHp}");
                return 264;
            }

            scene = sceneRef;
            player = playerRef;
            RogueProgressComponent progressAfterNormalKill = player.GetComponent<RogueProgressComponent>();
            if (progressAfterNormalKill == null || progressAfterNormalKill.CurrentGold != 8888)
            {
                Log.Console($"instant kill gold reward mismatch: {progressAfterNormalKill?.CurrentGold ?? 0}");
                return 265;
            }

            DamageContext bossContext = new DamageContext
            {
                SourceUnitId = playerId,
                TargetUnitId = bossId,
                BaseDamage = 1,
                FinalDamage = 1,
                IsBullet = true,
            };
            await handler.Handle(scene, new RogueBeforeDamageApply { Context = bossContext });
            if (bossContext.FinalDamage != 1)
            {
                Log.Console($"boss should not be instant killed: {bossContext.FinalDamage}");
                return 266;
            }

            player = playerRef;
            RogueProgressComponent progressAfterBossHit = player.GetComponent<RogueProgressComponent>();
            if (progressAfterBossHit == null || progressAfterBossHit.CurrentGold != 8888)
            {
                Log.Console($"boss hit should not grant extra gold: {progressAfterBossHit?.CurrentGold ?? 0}");
                return 267;
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

        private static bool TryGetTwoWeaponConfigIds(out int mainWeaponId, out int subWeaponId)
        {
            mainWeaponId = 0;
            subWeaponId = 0;

            foreach (WeaponConfig weaponConfig in WeaponConfigCategory.Instance.DataList)
            {
                if (weaponConfig == null)
                {
                    continue;
                }

                if (mainWeaponId == 0)
                {
                    mainWeaponId = weaponConfig.Id;
                    continue;
                }

                if (weaponConfig.Id != mainWeaponId)
                {
                    subWeaponId = weaponConfig.Id;
                    break;
                }
            }

            return mainWeaponId > 0 && subWeaponId > 0;
        }

        private static void InitializeWeapons(Unit unit, int mainWeaponId, int subWeaponId)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            EquipmentComponent equipmentComponent = unit.GetComponent<EquipmentComponent>() ?? unit.AddComponent<EquipmentComponent>();
            if (equipmentComponent == null)
            {
                return;
            }

            LoadoutHelper.ApplyLoadout(unit, mainWeaponId, subWeaponId, 0);
            WeaponInitHelper.InitializeWeaponsFromUnit(unit);
        }

        private static Unit CreateProfiledMonster(Scene scene, string groupId, float3 position)
        {
            Unit monster = TestHelper.CreateServerUnit(scene, UnitType.Monster, campId: 2);
            if (monster == null)
            {
                return null;
            }

            monster.Position = position;
            return MonsterRuntimeProfileHelper.ApplyProfile(monster, groupId) ? monster : null;
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

        private static ECAPointComponent CreateRangeTriggerPoint(Scene scene, string pointId, float3 position)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            Unit pointUnit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), 0);
            pointUnit.Position = position;

            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>(pointId, ECAPointType.RangeTrigger, 3f);
            point.Params = new List<FlowParam>();
            return point;
        }

        private static RogueTrapEntity FindTrapEntity(Scene scene, long ownerId)
        {
            if (scene?.Children == null)
            {
                return null;
            }

            foreach (Entity child in scene.Children.Values)
            {
                if (child is RogueTrapEntity trapEntity &&
                    !trapEntity.IsDisposed &&
                    trapEntity.OwnerId == ownerId)
                {
                    return trapEntity;
                }
            }

            return null;
        }
    }
}
