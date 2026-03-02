using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试BT行动节点：BTWeaponFire、BTWeaponReload
    /// TDD: 验证射击和换弹逻辑
    /// </summary>
    public class Test_WeaponBT_Fire_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_WeaponBT_Fire_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "FireRobot1");
            Scene scene1 = robot1.Root;
            EntityRef<Scene> scene1Ref = scene1;

            Fiber robot2 = await TestHelper.CreateRobot(testFiber, "FireRobot2");
            scene1 = scene1Ref;

            Unit caster = TestHelper.GetServerUnit(testFiber, robot1);
            Unit target = TestHelper.GetServerUnit(testFiber, robot2);

            if (caster == null || target == null)
            {
                Log.Console("units are null");
                return 1;
            }

            // 给caster添加武器组件（RifleId=1001, SMGId=1002）
            WeaponComponent weaponComp = caster.AddComponent<WeaponComponent, int, int>(1001, 1002);

            // 创建BTEnv
            Scene serverScene = caster.Scene();
            BTEnv env = BTEnv.Create(serverScene, caster.Id);
            env.AddEntity("Caster", caster);
            env.AddEntity("Target", target);

            // ===== 测试 BTWeaponFire =====
            BTWeaponFire fireNode = new BTWeaponFire
            {
                Caster = "Caster",
                Target = "Target",
                WeaponType = WeaponType.Rifle
            };
            BTWeaponFireHandler fireHandler = new BTWeaponFireHandler();

            // 验证1: 有弹药时射击成功（返回0）
            int rifleAmmo = weaponComp.RifleAmmo;
            int result = fireHandler.Handle(fireNode, env);
            if (result != 0)
            {
                Log.Console("BTWeaponFire should succeed with ammo");
                env.Dispose();
                return 2;
            }

            // 验证2: 射击后弹药减少1
            if (weaponComp.RifleAmmo != rifleAmmo - 1)
            {
                Log.Console($"Ammo should decrease by 1, expected {rifleAmmo - 1} got {weaponComp.RifleAmmo}");
                env.Dispose();
                return 3;
            }

            // ===== 测试 BTWeaponReload =====
            BTWeaponReload reloadNode = new BTWeaponReload
            {
                Caster = "Caster",
                WeaponType = WeaponType.Rifle
            };
            BTWeaponReloadHandler reloadHandler = new BTWeaponReloadHandler();

            // 验证3: 弹药未满时换弹成功（返回0）
            int result3 = reloadHandler.Handle(reloadNode, env);
            if (result3 != 0)
            {
                Log.Console("BTWeaponReload should succeed when ammo < max");
                env.Dispose();
                return 4;
            }

            // 验证4: 换弹后弹药恢复满
            if (weaponComp.RifleAmmo != 30)
            {
                Log.Console($"After reload rifle ammo should be 30, got {weaponComp.RifleAmmo}");
                env.Dispose();
                return 5;
            }

            // 验证5: 弹药已满时不需要换弹（返回1）
            int result5 = reloadHandler.Handle(reloadNode, env);
            if (result5 != 1)
            {
                Log.Console("BTWeaponReload should fail (return 1) when ammo is full");
                env.Dispose();
                return 6;
            }

            // ===== 测试 SMG 武器 =====
            BTWeaponFire smgFireNode = new BTWeaponFire
            {
                Caster = "Caster",
                Target = "Target",
                WeaponType = WeaponType.SMG
            };

            int smgAmmo = weaponComp.SMGAmmo;
            int smgResult = fireHandler.Handle(smgFireNode, env);
            if (smgResult != 0)
            {
                Log.Console("BTWeaponFire SMG should succeed with ammo");
                env.Dispose();
                return 7;
            }

            // 验证SMG弹药减少1，步枪弹药不变
            if (weaponComp.SMGAmmo != smgAmmo - 1)
            {
                Log.Console($"SMG ammo should decrease by 1");
                env.Dispose();
                return 8;
            }

            if (weaponComp.RifleAmmo != 30)
            {
                Log.Console("Rifle ammo should not be affected by SMG fire");
                env.Dispose();
                return 9;
            }

            env.Dispose();

            Log.Console("Test_WeaponBT_Fire_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
