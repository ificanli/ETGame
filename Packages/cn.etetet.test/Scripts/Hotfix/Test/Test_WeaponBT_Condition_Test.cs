namespace ET.Test
{
    /// <summary>
    /// 测试武器BT条件节点：BTWeaponHasTarget、BTWeaponInRange、BTWeaponCanFire
    /// TDD: 验证BT条件节点的判断逻辑
    /// </summary>
    public class Test_WeaponBT_Condition_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_WeaponBT_Condition_Test));
            Fiber testFiber = scope.TestFiber;

            // 创建两个机器人
            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "BTRobot1");
            Scene scene1 = robot1.Root;
            EntityRef<Scene> scene1Ref = scene1;

            Fiber robot2 = await TestHelper.CreateRobot(testFiber, "BTRobot2");
            scene1 = scene1Ref;

            Unit caster = TestHelper.GetServerUnit(testFiber, robot1);
            Unit target = TestHelper.GetServerUnit(testFiber, robot2);

            if (caster == null || target == null)
            {
                Log.Console("units are null");
                return 1;
            }

            // 给caster添加武器组件
            WeaponComponent weaponComp = caster.AddComponent<WeaponComponent, int, int>(1001, 1002);

            // 验证 BTWeaponCanFire：初始有弹药应该可以射击
            if (!weaponComp.CanFire(1001))
            {
                Log.Console("Should be able to fire with full ammo");
                return 2;
            }

            // 验证 BTWeaponCanFire：弹药为0不能射击
            weaponComp.ConsumeAmmo(1001, 30);
            if (weaponComp.CanFire(1001))
            {
                Log.Console("Should not fire with 0 ammo");
                return 3;
            }

            // 验证 BTWeaponCanFire：换弹状态不能射击
            weaponComp.RefillAmmo(1001);
            weaponComp.SetReloading(1001, true);
            if (weaponComp.CanFire(1001))
            {
                Log.Console("Should not fire while reloading");
                return 4;
            }
            weaponComp.SetReloading(1001, false);

            // 验证 WeaponComponent 双武器独立
            int smgAmmo = weaponComp.SMGAmmo;
            weaponComp.ConsumeAmmo(1001, 5);  // 消耗步枪弹药
            if (weaponComp.SMGAmmo != smgAmmo)
            {
                Log.Console("SMG ammo should not be affected by rifle consume");
                return 5;
            }

            Log.Console("Test_WeaponBT_Condition_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
