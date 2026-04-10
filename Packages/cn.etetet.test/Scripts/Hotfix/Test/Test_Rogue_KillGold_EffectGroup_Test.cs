using ET.Server;

namespace ET.Test
{
    public class Test_Rogue_KillGold_EffectGroup_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_KillGold_EffectGroup_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            if (scene.CoroutineLockComponent == null)
            {
                scene.AddComponent<CoroutineLockComponent>();
            }

            UnitConfig playerConfig = null;
            UnitConfig monsterConfig = null;
            foreach (UnitConfig config in UnitConfigCategory.Instance.DataList)
            {
                if (playerConfig == null && config.UnitType == UnitType.Player)
                {
                    playerConfig = config;
                }

                if (monsterConfig == null && config.UnitType == UnitType.Monster)
                {
                    monsterConfig = config;
                }

                if (playerConfig != null && monsterConfig != null)
                {
                    break;
                }
            }

            if (playerConfig == null)
            {
                Log.Console("player unit config is null");
                return 1;
            }

            if (monsterConfig == null)
            {
                Log.Console("monster unit config is null");
                return 2;
            }

            Unit player = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            player.UnitType = UnitType.Player;
            player.AddComponent<BuffComponent>();

            NumericComponent playerNumeric = player.AddComponent<NumericComponent>();
            foreach ((int numericType, long numericValue) in playerConfig.KV)
            {
                playerNumeric.SetNoEvent(numericType, numericValue);
            }

            Unit monster = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), monsterConfig.Id);
            monster.UnitType = UnitType.Monster;
            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> monsterRef = monster;

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Console("rogue config category is null");
                return 3;
            }

            if (!TestHelper.TryFindExecutableRogueOption(configCategory, null, out int optionId, out RogueOptionConfig optionConfig))
            {
                Log.Console("failed to find executable rogue option");
                return 4;
            }

            string oldBtConfig = optionConfig.BTConfig;
            int oldEffectGroupId = optionConfig.EffectGroupId;
            const int effectGroupId = 7203;
            const int killGold = 300;

            try
            {
                optionConfig.BTConfig = $"group:{effectGroupId}";
                optionConfig.EffectGroupId = 0;

                configCategory.RegisterEffectGroup(new RogueEffectGroupConfig
                {
                    Id = effectGroupId,
                    Entries =
                    {
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.AddKillGold,
                            Value1 = killGold,
                        },
                    },
                });

                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(player, false);
                if (progress == null)
                {
                    Log.Console("rogue progress is null");
                    return 5;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 1;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(optionId);

                EntityRef<Unit> playerRef = player;
                EntityRef<RogueProgressComponent> progressRef = progress;
                int chooseError = await RogueProgressHelper.ChooseOption(player, progress.ChoiceSerial, optionId, null);
                scene = sceneRef;
                player = playerRef;
                monster = monsterRef;
                progress = progressRef;
                if (scene == null || player == null || monster == null || progress == null)
                {
                    Log.Console("scene player monster or progress disposed after choose");
                    return 6;
                }

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"choose option failed, error={chooseError}");
                    return 7;
                }

                RogueEffectRuntimeComponent runtimeComponent = player.GetComponent<RogueEffectRuntimeComponent>();
                if (runtimeComponent == null || runtimeComponent.GetTotalValue1ByExecuteType(RogueEffectExecuteType.AddKillGold) != killGold)
                {
                    Log.Console("kill gold runtime not registered");
                    return 8;
                }

                int targetUnitType = (int)monster.UnitType;
                int directKillBonus = RogueGoldHelper.GetKillGoldBonus(runtimeComponent, targetUnitType);
                if (directKillBonus != killGold)
                {
                    Log.Console($"direct kill bonus mismatch, bonus={directKillBonus}, targetType={targetUnitType}, monsterType={(int)UnitType.Monster}");
                    return 9;
                }

                int firstKillGain = RogueKillRewardHelper.TryGrantKillGold(player, targetUnitType);

                if (firstKillGain != killGold || progress.CurrentGold != killGold)
                {
                    Log.Console($"gold after monster kill mismatch, gold={progress.CurrentGold}");
                    return 10;
                }

                int secondKillGain = RogueKillRewardHelper.TryGrantKillGold(player, (int)player.UnitType);

                if (secondKillGain != 0 || progress.CurrentGold != killGold)
                {
                    Log.Console($"gold should not change after player target die event, gold={progress.CurrentGold}");
                    return 11;
                }

                return ErrorCode.ERR_Success;
            }
            finally
            {
                optionConfig.BTConfig = oldBtConfig;
                optionConfig.EffectGroupId = oldEffectGroupId;
                configCategory.RemoveEffectGroup(effectGroupId);
            }
        }
    }
}
