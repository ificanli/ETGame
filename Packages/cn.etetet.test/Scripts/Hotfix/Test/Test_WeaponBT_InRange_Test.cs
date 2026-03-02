using Unity.Mathematics;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试BT条件节点：BTWeaponInRange
    /// TDD: 验证范围判断逻辑 - 目标在范围内返回0，范围外返回1
    /// </summary>
    public class Test_WeaponBT_InRange_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_WeaponBT_InRange_Test));
            Fiber testFiber = scope.TestFiber;

            // 创建两个机器人
            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "InRangeRobot1");
            Scene scene1 = robot1.Root;
            EntityRef<Scene> scene1Ref = scene1;

            Fiber robot2 = await TestHelper.CreateRobot(testFiber, "InRangeRobot2");
            scene1 = scene1Ref;

            Unit caster = TestHelper.GetServerUnit(testFiber, robot1);
            Unit target = TestHelper.GetServerUnit(testFiber, robot2);

            if (caster == null || target == null)
            {
                Log.Console("units are null");
                return 1;
            }

            // 创建 BTEnv 并设置 caster 和 target
            Scene serverScene = caster.Scene();
            BTEnv env = BTEnv.Create(serverScene, caster.Id);
            env.AddEntity("Caster", caster);
            env.AddEntity("Target", target);

            // 创建 BTWeaponInRange 节点
            BTWeaponInRange node = new BTWeaponInRange
            {
                Caster = "Caster",
                Target = "Target",
                Range = 10f
            };

            // 创建 Handler 并直接测试
            BTWeaponInRangeHandler handler = new BTWeaponInRangeHandler();

            // 验证1: caster和target在同一位置（距离=0），应该在范围内
            caster.Position = new float3(0, 0, 0);
            target.Position = new float3(0, 0, 0);
            int result = handler.Handle(node, env);
            if (result != 0)
            {
                Log.Console("Should be in range when at same position");
                env.Dispose();
                return 2;
            }

            // 验证2: target在范围边缘（距离=9.9），应该在范围内
            target.Position = new float3(9.9f, 0, 0);
            result = handler.Handle(node, env);
            if (result != 0)
            {
                Log.Console("Should be in range at 9.9m with 10m range");
                env.Dispose();
                return 3;
            }

            // 验证3: target超出范围（距离=10.1），应该不在范围内
            target.Position = new float3(10.1f, 0, 0);
            result = handler.Handle(node, env);
            if (result != 1)
            {
                Log.Console("Should NOT be in range at 10.1m with 10m range");
                env.Dispose();
                return 4;
            }

            // 验证4: 缩小范围为5，距离7在范围外
            node.Range = 5f;
            target.Position = new float3(7, 0, 0);
            result = handler.Handle(node, env);
            if (result != 1)
            {
                Log.Console("Should NOT be in 5m range when target is at 7m");
                env.Dispose();
                return 5;
            }

            env.Dispose();

            Log.Console("Test_WeaponBT_InRange_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
