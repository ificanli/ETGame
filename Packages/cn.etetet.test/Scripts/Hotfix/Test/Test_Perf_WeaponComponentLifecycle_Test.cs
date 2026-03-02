using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 性能测试：验证武器组件和BT节点在大量重复操作中的稳定性
    /// TDD: 简化版性能验证，检查基本系统无异常崩溃
    /// </summary>
    public class Test_Perf_WeaponComponentLifecycle_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Perf_WeaponComponentLifecycle_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "PerfRobot1");
            Scene scene1 = robot1.Root;
            EntityRef<Scene> scene1Ref = scene1;

            Fiber robot2 = await TestHelper.CreateRobot(testFiber, "PerfRobot2");
            scene1 = scene1Ref;

            Unit caster = TestHelper.GetServerUnit(testFiber, robot1);
            Unit target = TestHelper.GetServerUnit(testFiber, robot2);

            if (caster == null || target == null)
            {
                Log.Console("units are null");
                return 1;
            }

            // ===== 测试武器组件重复添加/删除的稳定性 =====
            int iterations = 100;
            for (int i = 0; i < iterations; i++)
            {
                WeaponComponent wc = caster.AddComponent<WeaponComponent, int, int>(1001, 1002);

                // 基本操作
                wc.ConsumeAmmo(1001, 5);
                wc.RefillAmmo(1001);
                wc.SwitchWeapon(WeaponType.SMG);
                wc.SwitchWeapon(WeaponType.Rifle);

                caster.RemoveComponent<WeaponComponent>();
            }

            // 验证：100次后组件已经移除
            if (caster.GetComponent<WeaponComponent>() != null)
            {
                Log.Console("WeaponComponent should be removed after RemoveComponent");
                return 2;
            }

            // ===== 测试BT节点多次执行稳定性 =====
            WeaponComponent weaponComp = caster.AddComponent<WeaponComponent, int, int>(1001, 1002);
            Scene serverScene = caster.Scene();
            BTEnv env = BTEnv.Create(serverScene, caster.Id);
            env.AddEntity("Caster", caster);
            env.AddEntity("Target", target);

            BTWeaponCanFire canFireNode = new BTWeaponCanFire
            {
                Caster = "Caster",
                WeaponType = WeaponType.Rifle
            };
            BTWeaponCanFireHandler canFireHandler = new BTWeaponCanFireHandler();

            BTWeaponInRange inRangeNode = new BTWeaponInRange
            {
                Caster = "Caster",
                Target = "Target",
                Range = 10f
            };
            BTWeaponInRangeHandler inRangeHandler = new BTWeaponInRangeHandler();

            // 连续执行BT节点100次，验证不崩溃
            int canFireCount = 0;
            int inRangeCount = 0;
            for (int i = 0; i < 100; i++)
            {
                int canFire = canFireHandler.Handle(canFireNode, env);
                int inRange = inRangeHandler.Handle(inRangeNode, env);

                if (canFire == 0) canFireCount++;
                if (inRange == 0) inRangeCount++;
            }

            // 第一次应该能射击（但之后因时间间隔不行），成功至少1次
            if (canFireCount < 1)
            {
                Log.Console("Should be able to fire at least once in 100 attempts");
                env.Dispose();
                return 3;
            }

            // 距离为0，所有100次都应该在范围内
            if (inRangeCount != 100)
            {
                Log.Console($"All 100 checks at same position should be in range, got {inRangeCount}");
                env.Dispose();
                return 4;
            }

            env.Dispose();

            Log.Console($"Test_Perf_WeaponComponentLifecycle_Test PASSED (canFire: {canFireCount}/100, inRange: {inRangeCount}/100)");
            return ErrorCode.ERR_Success;
        }
    }
}
