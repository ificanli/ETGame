namespace ET.Test
{
    /// <summary>
    /// 测试武器系统：弹药管理、射击间隔、换弹状态
    /// TDD: 验证 WeaponComponent 的核心逻辑
    /// </summary>
    public class Test_Weapon_ConsumeAmmo_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Weapon_ConsumeAmmo_Test));
            Fiber testFiber = scope.TestFiber;

            // 创建机器人
            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "WeaponRobot1");
            Scene scene1 = robot1.Root;
            EntityRef<Scene> scene1Ref = scene1;

            scene1 = scene1Ref;

            // 获取服务端Unit
            Unit unit1 = TestHelper.GetServerUnit(testFiber, robot1);

            if (unit1 == null)
            {
                Log.Console("unit1 is null");
                return 1;
            }

            // 添加武器组件（步枪ID=1001, 冲锋枪ID=1002）
            WeaponComponent weaponComp = unit1.AddComponent<WeaponComponent, int, int>(1001, 1002);

            if (weaponComp == null)
            {
                Log.Console("WeaponComponent AddComponent failed");
                return 2;
            }

            // 验证初始弹药
            int initialRifleAmmo = weaponComp.RifleAmmo;
            if (initialRifleAmmo != 30)
            {
                Log.Console($"Initial rifle ammo should be 30, but is {initialRifleAmmo}");
                return 3;
            }

            // 消耗1发弹药
            weaponComp.ConsumeAmmo(1001, 1);

            if (weaponComp.RifleAmmo != 29)
            {
                Log.Console($"After consuming 1 ammo, rifle ammo should be 29, but is {weaponComp.RifleAmmo}");
                return 4;
            }

            // 消耗到0
            weaponComp.ConsumeAmmo(1001, 29);
            if (weaponComp.RifleAmmo != 0)
            {
                Log.Console($"After consuming all ammo, rifle ammo should be 0, but is {weaponComp.RifleAmmo}");
                return 5;
            }

            // 尝试消耗负数（不应该小于0）
            weaponComp.ConsumeAmmo(1001, 1);
            if (weaponComp.RifleAmmo != 0)
            {
                Log.Console($"Ammo should not go below 0, but is {weaponComp.RifleAmmo}");
                return 6;
            }

            Log.Console("Test_Weapon_ConsumeAmmo_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
