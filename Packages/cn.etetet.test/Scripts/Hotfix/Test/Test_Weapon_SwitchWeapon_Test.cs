namespace ET.Test
{
    /// <summary>
    /// 测试武器系统：切换武器
    /// TDD: 验证武器切换功能
    /// </summary>
    public class Test_Weapon_SwitchWeapon_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Weapon_SwitchWeapon_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "WeaponRobot1");
            Unit unit1 = TestHelper.GetServerUnit(testFiber, robot1);

            if (unit1 == null)
            {
                Log.Console("unit1 is null");
                return 1;
            }

            WeaponComponent weaponComp = unit1.AddComponent<WeaponComponent, int, int>(1001, 1002);

            // 验证初始装备步枪
            if (weaponComp.CurrentWeapon != WeaponType.Rifle)
            {
                Log.Console($"Initial weapon should be Rifle, but is {weaponComp.CurrentWeapon}");
                return 2;
            }

            // 切换到冲锋枪
            weaponComp.SwitchWeapon(WeaponType.SMG);

            // 验证切换成功
            if (weaponComp.CurrentWeapon != WeaponType.SMG)
            {
                Log.Console($"After switch, weapon should be SMG, but is {weaponComp.CurrentWeapon}");
                return 3;
            }

            Log.Console("Test_Weapon_SwitchWeapon_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
