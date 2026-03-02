namespace ET.Test
{
    /// <summary>
    /// 测试武器系统：补充弹药（换弹）
    /// TDD: 验证 RefillAmmo 功能
    /// </summary>
    public class Test_Weapon_RefillAmmo_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Weapon_RefillAmmo_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "WeaponRobot1");
            Unit unit1 = TestHelper.GetServerUnit(testFiber, robot1);

            if (unit1 == null)
            {
                Log.Console("unit1 is null");
                return 1;
            }

            WeaponComponent weaponComp = unit1.AddComponent<WeaponComponent, int, int>(1001, 1002);

            // 消耗部分弹药
            weaponComp.ConsumeAmmo(1001, 20);
            if (weaponComp.RifleAmmo != 10)
            {
                Log.Console($"After consuming 20 ammo, rifle ammo should be 10, but is {weaponComp.RifleAmmo}");
                return 2;
            }

            // 补充弹药
            weaponComp.RefillAmmo(1001);

            if (weaponComp.RifleAmmo != 30)
            {
                Log.Console($"After refill, rifle ammo should be 30, but is {weaponComp.RifleAmmo}");
                return 3;
            }

            Log.Console("Test_Weapon_RefillAmmo_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
