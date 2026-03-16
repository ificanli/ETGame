using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试 RogueObjective 击杀进度推进。
    /// </summary>
    public class Test_Rogue_ObjectiveProgress_OnKill_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_ObjectiveProgress_OnKill_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();

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

            Unit killer = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            killer.UnitType = UnitType.Player;
            killer.AddComponent<NumericComponent>().SetNoEvent(NumericType.HP, 1000);

            // 添加任务组件，注册一个击杀任意怪物的任务（ObjectiveId=0 表示任意）
            RogueObjectiveComponent objectiveComponent = killer.AddComponent<RogueObjectiveComponent>();
            RogueObjective objective = objectiveComponent.AddChild<RogueObjective>();
            objective.OptionId = 100;
            objective.EffectGroupId = 1;
            objective.ObjectiveId = 0; // 任意怪物
            objective.Progress = 0;
            objective.Completed = false;
            objective.RewardClaimed = false;

            // 同时添加对应的 RogueEffectRuntime 来提供 GoalValue
            RogueEffectRuntimeComponent runtimeComponent = killer.AddComponent<RogueEffectRuntimeComponent>();
            RogueEffectRuntime runtime = runtimeComponent.AddChild<RogueEffectRuntime>();
            runtime.OptionId = 100;
            runtime.EffectGroupId = 1;
            runtime.ExecuteType = RogueEffectExecuteType.RegisterObjective;
            runtime.RefId = 0; // 对应 ObjectiveId
            runtime.Value1 = 3; // GoalValue = 3

            // 第一次击杀
            RogueObjectiveHelper.OnKill(killer, objectiveComponent, (int)UnitType.Monster);

            if (objective.Progress != 1)
            {
                Log.Console($"progress after first kill mismatch, expected=1, actual={objective.Progress}");
                return 2;
            }

            if (objective.Completed)
            {
                Log.Console("should not be completed after first kill");
                return 3;
            }

            // 第二次击杀
            RogueObjectiveHelper.OnKill(killer, objectiveComponent, (int)UnitType.Monster);

            if (objective.Progress != 2)
            {
                Log.Console($"progress after second kill mismatch, expected=2, actual={objective.Progress}");
                return 4;
            }

            // 第三次击杀 → 完成
            RogueObjectiveHelper.OnKill(killer, objectiveComponent, (int)UnitType.Monster);

            if (objective.Progress != 3)
            {
                Log.Console($"progress after third kill mismatch, expected=3, actual={objective.Progress}");
                return 5;
            }

            if (!objective.Completed)
            {
                Log.Console("should be completed after third kill");
                return 6;
            }

            if (!objective.RewardClaimed)
            {
                Log.Console("reward should be claimed after completion");
                return 7;
            }

            // 完成后再击杀不应增加进度
            RogueObjectiveHelper.OnKill(killer, objectiveComponent, (int)UnitType.Monster);

            if (objective.Progress != 3)
            {
                Log.Console($"progress should not increase after completion, actual={objective.Progress}");
                return 8;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
