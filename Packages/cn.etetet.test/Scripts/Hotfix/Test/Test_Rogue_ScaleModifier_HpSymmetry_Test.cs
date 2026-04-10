using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试 ScaleModifier HP 对称性：应用和移除后 MaxHPFinalAdd 应回到原值。
    /// </summary>
    public class Test_Rogue_ScaleModifier_HpSymmetry_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_ScaleModifier_HpSymmetry_Test));
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
            numeric.SetNoEvent(NumericType.MaxHP, 1000);
            numeric.SetNoEvent(NumericType.MaxHPFinalAdd, 0);
            numeric.SetNoEvent(NumericType.HP, 1000);

            long originalMaxHpAdd = numeric.GetAsLong(NumericType.MaxHPFinalAdd);

            // 注册临时 EffectGroup: ScaleModifier +30% HP
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Console("rogue config category is null");
                return 2;
            }

            const int testGroupId = 88801;

            if (!TestHelper.TryFindExecutableRogueOption(configCategory, null, out int optionId, out RogueOptionConfig optionConfig))
            {
                Log.Console("failed to find executable rogue option");
                return 3;
            }

            string oldBtConfig = optionConfig.BTConfig;
            int oldEffectGroupId = optionConfig.EffectGroupId;

            try
            {
                optionConfig.BTConfig = $"group:{testGroupId}";
                optionConfig.EffectGroupId = 0;

                configCategory.RegisterEffectGroup(new RogueEffectGroupConfig
                {
                    Id = testGroupId,
                    Entries =
                    {
                        new RogueEffectEntryConfig
                        {
                            ExecuteType = RogueEffectExecuteType.ScaleModifier,
                            Value1 = 200,
                            Value2 = 300, // +30% MaxHP
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

                // await 后重新获取 numeric
                NumericComponent numericAfter = player.NumericComponent;
                if (numericAfter == null)
                {
                    Log.Console("numeric component is null after await");
                    return 5;
                }

                // 验证 MaxHPFinalAdd 增加了
                long afterApplyMaxHpAdd = numericAfter.GetAsLong(NumericType.MaxHPFinalAdd);
                if (afterApplyMaxHpAdd <= originalMaxHpAdd)
                {
                    Log.Console($"MaxHPFinalAdd should increase after apply, before={originalMaxHpAdd}, after={afterApplyMaxHpAdd}");
                    return 6;
                }

                long appliedDelta = afterApplyMaxHpAdd - originalMaxHpAdd;

                // 移除选项
                int removedCount = RogueEffectHelper.RemoveSelectedOption(player, progress, optionId);
                if (removedCount <= 0)
                {
                    Log.Console("remove selected option returned 0");
                    return 7;
                }

                // 验证 MaxHPFinalAdd 回到原值
                long afterRemoveMaxHpAdd = numericAfter.GetAsLong(NumericType.MaxHPFinalAdd);
                if (afterRemoveMaxHpAdd != originalMaxHpAdd)
                {
                    Log.Console($"MaxHPFinalAdd should return to original after remove, original={originalMaxHpAdd}, actual={afterRemoveMaxHpAdd}, delta={appliedDelta}");
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
