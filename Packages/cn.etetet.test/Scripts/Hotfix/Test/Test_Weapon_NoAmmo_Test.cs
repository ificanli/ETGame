namespace ET.Test
{
    /// <summary>
    /// 测试武器系统：弹药为0时不能射击
    /// TDD: 验证弹药检查逻辑
    /// </summary>
    public class Test_Weapon_NoAmmo_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Weapon_NoAmmo_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "WeaponRobot1");
            Unit unit1 = TestHelper.GetServerUnit(testFiber, robot1);

            if (unit1 == null)
            {
                Log.Console("unit1 is null");
                return 1;
            }

            WeaponComponent weaponComp = unit1.AddComponent<WeaponComponent, int, int>(1001, 1002);

            // 消耗所有弹药
            weaponComp.ConsumeAmmo(1001, 30);

            // 验证弹药为0
            if (weaponComp.GetAmmo(1001) != 0)
            {
                Log.Console($"Ammo should be 0, but is {weaponComp.GetAmmo(1001)}");
                return 2;
            }

            // 验证不能射击
            if (weaponComp.CanFire(1001))
            {
                Log.Console("Cannot fire without ammo");
                return 3;
            }

            Log.Console("Test_Weapon_NoAmmo_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
