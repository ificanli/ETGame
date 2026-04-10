using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试多条目 EffectGroup 的移除：选择一个含多个 Entry 的 Group 后，
    /// RemoveSelectedOption 应移除所有关联的 Runtime。
    /// </summary>
    public class Test_Rogue_MultiEntryGroupRemoval_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_MultiEntryGroupRemoval_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            if (scene.CoroutineLockComponent == null)
            {
                scene.AddComponent<CoroutineLockComponent>();
            }

            UnitConfig playerConfig = null;
            foreach (UnitConfig config in UnitConfigCategory.Instance.DataList)
            {
                if (config.UnitType == UnitType.Player)
                {
                    playerConfig = config;
                    break;
                }
            }

            if (playerConfig == null)
            {
                Log.Console("player unit config is null");
                return 1;
            }

            Unit player = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            player.UnitType = UnitType.Player;
            player.AddComponent<BuffComponent>();
            NumericComponent numeric = player.AddComponent<NumericComponent>();
            numeric.SetNoEvent(NumericType.HP, 10000);
            numeric.SetNoEvent(NumericType.MaxHP, 10000);

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Console("rogue config category is null");
                return 2;
            }

            if (!TestHelper.TryFindExecutableRogueOption(configCategory, null, out int optionId, out RogueOptionConfig optionConfig))
            {
                Log.Console("failed to find executable rogue option");
                return 3;
            }

            string oldBtConfig = optionConfig.BTConfig;
            int oldEffectGroupId = optionConfig.EffectGroupId;
            const int testGroupId = 88802;

            try
            {
                optionConfig.BTConfig = $"group:{testGroupId}";
                optionConfig.EffectGroupId = 0;

                // 注册含 3 个 Entry 的 EffectGroup
                configCategory.RegisterEffectGroup(new RogueEffectGroupConfig
                {
                    Id = testGroupId,
                    Entries =
                    {
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.AddKillGold,
                            Value1 = 100,
                        },
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.OnKillHeal,
                            Value1 = 50,
                        },
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.LifeSteal,
                            Value1 = 200,
                        },
                    },
                });

                RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(player, false);
                if (progress == null)
                {
                    Log.Console("rogue progress is null");
                    return 4;
                }

                progress.ChoicePending = true;
                progress.ChoiceSerial = 1;
                progress.PendingOptionIds.Clear();
                progress.PendingOptionIds.Add(optionId);

                EntityRef<Unit> playerRef = player;
                EntityRef<RogueProgressComponent> progressRef = progress;
                int chooseError = await RogueProgressHelper.ChooseOption(player, progress.ChoiceSerial, optionId, null);
                player = playerRef;
                progress = progressRef;

                if (chooseError != ErrorCode.ERR_Success)
                {
                    Log.Console($"choose option failed, error={chooseError}");
                    return 5;
                }

                // 验证创建了 3 个 Runtime
                RogueEffectRuntimeComponent runtimeComponent = player.GetComponent<RogueEffectRuntimeComponent>();
                if (runtimeComponent == null)
                {
                    Log.Console("runtime component is null after apply");
                    return 6;
                }

                int runtimeCount = runtimeComponent.ChildrenCount();
                if (runtimeCount != 3)
                {
                    Log.Console($"expected 3 runtimes, actual={runtimeCount}");
                    return 7;
                }

                // 移除选项 — 应移除所有 3 个 Runtime
                int removedCount = RogueEffectHelper.RemoveSelectedOption(player, progress, optionId);

                runtimeComponent = player.GetComponent<RogueEffectRuntimeComponent>();
                int remainingCount = runtimeComponent?.ChildrenCount() ?? 0;
                if (remainingCount != 0)
                {
                    Log.Console($"expected 0 remaining runtimes after removal, actual={remainingCount}");
                    return 8;
                }

                return ErrorCode.ERR_Success;
            }
            finally
            {
                optionConfig.BTConfig = oldBtConfig;
                optionConfig.EffectGroupId = oldEffectGroupId;
                configCategory.RemoveEffectGroup(testGroupId);
            }
        }
    }
}
