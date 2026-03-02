namespace ET.Test
{
    /// <summary>
    /// 测试武器系统：换弹期间不能射击
    /// TDD: 验证换弹状态管理
    /// </summary>
    public class Test_Weapon_ReloadingState_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Weapon_ReloadingState_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "WeaponRobot1");
            Unit unit1 = TestHelper.GetServerUnit(testFiber, robot1);

            if (unit1 == null)
            {
                Log.Console("unit1 is null");
                return 1;
            }

            WeaponComponent weaponComp = unit1.AddComponent<WeaponComponent, int, int>(1001, 1002);

            // 开始换弹
            weaponComp.SetReloading(1001, true);

            // 验证换弹状态
            if (!weaponComp.IsReloading(1001))
            {
                Log.Console("Weapon should be reloading");
                return 2;
            }

            // 验证换弹期间不能射击
            if (weaponComp.CanFire(1001))
            {
                Log.Console("Cannot fire while reloading");
                return 3;
            }

            // 换弹完成
            weaponComp.SetReloading(1001, false);

            // 验证可以射击了
            if (!weaponComp.CanFire(1001))
            {
                Log.Console("Should be able to fire after reloading");
                return 4;
            }

            Log.Console("Test_Weapon_ReloadingState_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
