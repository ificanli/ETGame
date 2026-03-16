namespace ET.Test
{
    /// <summary>
    /// 测试目标选择系统：TargetSelectorComponent + TargetSelectorHelper
    /// TDD: 验证自动索敌的核心逻辑（手动目标优先、无目标时返回null）
    /// 注意：AOI范围目标查找需要服务端AOI，这里主要测试组件的状态管理逻辑
    /// </summary>
    public class Test_TargetSelector_Basic_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_TargetSelector_Basic_Test));
            Fiber testFiber = scope.TestFiber;

            // 创建两个机器人
            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "TargetRobot1");
            Scene scene1 = robot1.Root;
            EntityRef<Scene> scene1Ref = scene1;

            Fiber robot2 = await TestHelper.CreateRobot(testFiber, "TargetRobot2");
            scene1 = scene1Ref;

            // 获取服务端Unit
            Unit unit1 = TestHelper.GetServerUnit(testFiber, robot1);
            Unit unit2 = TestHelper.GetServerUnit(testFiber, robot2);

            if (unit1 == null)
            {
                Log.Console("unit1 is null");
                return 1;
            }

            if (unit2 == null)
            {
                Log.Console("unit2 is null");
                return 2;
            }

            // 验证初始状态下单位没有 TargetSelectorComponent
            TargetSelectorComponent initSelector = unit1.GetComponent<TargetSelectorComponent>();
            if (initSelector != null)
            {
                Log.Console("unit1 should not have TargetSelectorComponent initially");
                return 3;
            }

            // 给 unit1 添加 TargetSelectorComponent
            unit1.AddComponent<TargetSelectorComponent>();
            TargetSelectorComponent selector = unit1.GetComponent<TargetSelectorComponent>();

            if (selector == null)
            {
                Log.Console("TargetSelectorComponent AddComponent failed");
                return 4;
            }

            // 验证初始值
            if (selector.CurrentTargetId != 0)
            {
                Log.Console($"CurrentTargetId should be 0, but is {selector.CurrentTargetId}");
                return 5;
            }

            if (selector.SelectIntervalMs != 1000)
            {
                Log.Console($"SelectIntervalMs should be 1000, but is {selector.SelectIntervalMs}");
                return 6;
            }

            if (selector.MaxRange != 10f)
            {
                Log.Console($"MaxRange should be 10f, but is {selector.MaxRange}");
                return 7;
            }

            // 验证 GetCurrentTarget 在无目标时返回 null
            Unit currentTarget = selector.GetCurrentTarget();
            if (currentTarget != null)
            {
                Log.Console("GetCurrentTarget should return null when no target set");
                return 8;
            }

            // 保存引用，准备设置手动目标
            EntityRef<Unit> unit1Ref = unit1;
            EntityRef<Unit> unit2Ref = unit2;

            // 设置手动目标（unit2）
            unit1 = unit1Ref;
            unit2 = unit2Ref;
            selector = unit1.GetComponent<TargetSelectorComponent>();

            selector.SetManualTarget(unit2.Id);

            if (selector.ManualTargetId != unit2.Id)
            {
                Log.Console($"ManualTargetId should be {unit2.Id}, but is {selector.ManualTargetId}");
                return 9;
            }

            // 验证 SetManualTarget 重置了 LastSelectTime
            if (selector.LastSelectTime != 0)
            {
                Log.Console($"LastSelectTime should be reset to 0 after SetManualTarget, but is {selector.LastSelectTime}");
                return 10;
            }

            Log.Console("Test_TargetSelector_BasicTest PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
