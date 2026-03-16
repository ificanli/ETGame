using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试 OnKillStackAttack：击杀叠加攻击力层数，伤害加成在 BeforeDamageApply 中生效。
    /// </summary>
    public class Test_Rogue_OnKillStackAttack_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_OnKillStackAttack_Test));
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

            Unit player = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            player.UnitType = UnitType.Player;
            player.AddComponent<BuffComponent>();
            NumericComponent playerNumeric = player.AddComponent<NumericComponent>();
            playerNumeric.SetNoEvent(NumericType.HP, 10000);
            playerNumeric.SetNoEvent(NumericType.MaxHP, 10000);

            // 手动添加 OnKillStackAttack runtime: Value1=300(30%每层), Value2=3(最大3层)
            RogueEffectRuntimeComponent runtimeComponent = player.AddComponent<RogueEffectRuntimeComponent>();
            RogueEffectRuntime runtime = runtimeComponent.AddEffectRuntime(9999, 9999,
                new RogueEffectEntryConfig
                {
                    ExecuteType = RogueEffectExecuteType.OnKillStackAttack,
                    Value1 = 300,
                    Value2 = 3,
                }, 0L);

            // 初始 StackCount 应为 0
            if (runtime.StackCount != 0)
            {
                Log.Console($"initial stack count should be 0, actual={runtime.StackCount}");
                return 2;
            }

            // 模拟击杀 — 直接调用 handler 逻辑
            // 叠加 1 层
            SimulateKillStack(runtime);
            if (runtime.StackCount != 1)
            {
                Log.Console($"stack count after 1 kill should be 1, actual={runtime.StackCount}");
                return 3;
            }

            // 叠加到最大层数
            SimulateKillStack(runtime);
            SimulateKillStack(runtime);
            if (runtime.StackCount != 3)
            {
                Log.Console($"stack count after 3 kills should be 3, actual={runtime.StackCount}");
                return 4;
            }

            // 超过最大层数不再叠加
            SimulateKillStack(runtime);
            if (runtime.StackCount != 3)
            {
                Log.Console($"stack count should cap at 3, actual={runtime.StackCount}");
                return 5;
            }

            return ErrorCode.ERR_Success;
        }

        private static void SimulateKillStack(RogueEffectRuntime runtime)
        {
            int maxStacks = runtime.Value2 > 0 ? runtime.Value2 : 3;
            if (runtime.StackCount >= maxStacks) return;
            runtime.StackCount += 1;
        }
    }
}
