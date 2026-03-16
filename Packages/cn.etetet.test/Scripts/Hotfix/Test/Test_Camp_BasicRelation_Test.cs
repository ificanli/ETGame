namespace ET.Test
{
    /// <summary>
    /// 测试阵营系统基础功能：阵营关系判断（敌对/友方/中立）
    /// TDD: 验证 CampComponent 和 CampHelper 的核心逻辑
    /// </summary>
    public class Test_Camp_BasicRelation_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Camp_BasicRelation_Test));
            Fiber testFiber = scope.TestFiber;

            // 创建两个机器人
            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "CampRobot1");
            Scene scene1 = robot1.Root;
            EntityRef<Scene> scene1Ref = scene1;

            Fiber robot2 = await TestHelper.CreateRobot(testFiber, "CampRobot2");
            scene1 = scene1Ref;
            Scene scene2 = robot2.Root;

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

            // 验证初始状态下单位没有阵营组件
            CampComponent initCamp = unit1.GetComponent<CampComponent>();
            if (initCamp != null)
            {
                Log.Console("unit1 should not have CampComponent initially");
                return 3;
            }

            // 给 unit1 添加阵营1，unit2 添加阵营2
            unit1.AddComponent<CampComponent, int>(1);
            unit2.AddComponent<CampComponent, int>(2);

            CampComponent camp1 = unit1.GetComponent<CampComponent>();
            CampComponent camp2 = unit2.GetComponent<CampComponent>();

            if (camp1 == null)
            {
                Log.Console("camp1 AddComponent failed");
                return 4;
            }

            if (camp2 == null)
            {
                Log.Console("camp2 AddComponent failed");
                return 5;
            }

            // 验证阵营ID
            if (camp1.CampId != 1)
            {
                Log.Console($"camp1.CampId should be 1, but is {camp1.CampId}");
                return 6;
            }

            if (camp2.CampId != 2)
            {
                Log.Console($"camp2.CampId should be 2, but is {camp2.CampId}");
                return 7;
            }

            // 验证不同阵营互为敌对
            if (!CampHelper.IsEnemy(unit1, unit2))
            {
                Log.Console("unit1 and unit2 should be enemies (different camps)");
                return 8;
            }

            // 保存引用，准备再次await
            EntityRef<Unit> unit1Ref = unit1;

            // 创建第三个robot，阵营同unit1
            Fiber robot3 = await TestHelper.CreateRobot(testFiber, "CampRobot3");

            // await 后重新获取
            unit1 = unit1Ref;

            Unit unit3 = TestHelper.GetServerUnit(testFiber, robot3);
            unit3.AddComponent<CampComponent, int>(1); // 同阵营1

            // 验证相同阵营不是敌对
            if (CampHelper.IsEnemy(unit1, unit3))
            {
                Log.Console("unit1 and unit3 (same camp 1) should NOT be enemies");
                return 9;
            }

            // 验证相同阵营是友方
            if (!CampHelper.IsFriendly(unit1, unit3))
            {
                Log.Console("unit1 and unit3 (same camp 1) should be friendly");
                return 10;
            }

            Log.Console("Test_Camp_BasicRelation_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
