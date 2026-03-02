namespace ET.Test
{
    /// <summary>
    /// 测试武器系统：双武器独立弹药管理
    /// TDD: 验证步枪和冲锋枪的弹药互不影响
    /// </summary>
    public class Test_Weapon_DualWeaponAmmo_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Weapon_DualWeaponAmmo_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "WeaponRobot1");
            Unit unit1 = TestHelper.GetServerUnit(testFiber, robot1);

            if (unit1 == null)
            {
                Log.Console("unit1 is null");
                return 1;
            }

            WeaponComponent weaponComp = unit1.AddComponent<WeaponComponent, int, int>(1001, 1002);

            int initialRifleAmmo = weaponComp.RifleAmmo;
            int initialSMGAmmo = weaponComp.SMGAmmo;

            // 消耗步枪弹药
            weaponComp.ConsumeAmmo(1001, 10);

            // 验证步枪弹药减少
            if (weaponComp.RifleAmmo != initialRifleAmmo - 10)
            {
                Log.Console($"Rifle ammo should be {initialRifleAmmo - 10}, but is {weaponComp.RifleAmmo}");
                return 2;
            }

            // 验证冲锋枪弹药不受影响
            if (weaponComp.SMGAmmo != initialSMGAmmo)
            {
                Log.Console($"SMG ammo should remain {initialSMGAmmo}, but is {weaponComp.SMGAmmo}");
                return 3;
            }

            Log.Console("Test_Weapon_DualWeaponAmmo_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
