using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 集成测试：武器组件 + 阵营系统 + BT节点协同工作
    /// TDD: 验证多系统集成后行为正确
    /// </summary>
    public class Test_Integration_CombatFlow_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Integration_CombatFlow_Test));
            Fiber testFiber = scope.TestFiber;

            // 创建两个机器人（阵营不同）
            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "IntegRobot1");
            Scene scene1 = robot1.Root;
            EntityRef<Scene> scene1Ref = scene1;

            Fiber robot2 = await TestHelper.CreateRobot(testFiber, "IntegRobot2");
            scene1 = scene1Ref;

            Unit caster = TestHelper.GetServerUnit(testFiber, robot1);
            Unit target = TestHelper.GetServerUnit(testFiber, robot2);

            if (caster == null || target == null)
            {
                Log.Console("units are null");
                return 1;
            }

            // ===== 设置阵营 =====
            CampComponent casterCamp = caster.AddComponent<CampComponent, int>(1);
            CampComponent targetCamp = target.AddComponent<CampComponent, int>(2);

            // 验证1: 不同阵营互为敌人
            if (!CampHelper.IsEnemy(caster, target))
            {
                Log.Console("Camp1 and Camp2 should be enemies");
                return 2;
            }

            // 验证2: 同阵营不是敌人
            if (CampHelper.IsEnemy(caster, caster))
            {
                Log.Console("Same camp should not be enemies");
                return 3;
            }

            // ===== 设置武器 =====
            WeaponComponent weaponComp = caster.AddComponent<WeaponComponent, int, int>(1001, 1002);

            int startRifleAmmo = weaponComp.RifleAmmo;
            int startSmgAmmo = weaponComp.SMGAmmo;

            // ===== 通过BT节点射击多次 =====
            Scene serverScene = caster.Scene();
            BTEnv env = BTEnv.Create(serverScene, caster.Id);
            env.AddEntity("Caster", caster);
            env.AddEntity("Target", target);

            BTWeaponFire fireNode = new BTWeaponFire
            {
                Caster = "Caster",
                Target = "Target",
                WeaponType = WeaponType.Rifle
            };
            BTWeaponFireHandler fireHandler = new BTWeaponFireHandler();

            // 射击5次（注意每次射击有时间间隔限制500ms）
            // 第一次射击应该成功
            int fireResult = fireHandler.Handle(fireNode, env);
            if (fireResult != 0)
            {
                Log.Console("First shot should succeed");
                env.Dispose();
                return 4;
            }

            // 弹药消耗1次
            if (weaponComp.RifleAmmo != startRifleAmmo - 1)
            {
                Log.Console($"Rifle ammo should be {startRifleAmmo - 1} after one shot");
                env.Dispose();
                return 5;
            }

            // ===== 验证 BTWeaponCanFire 在射击间隔内返回失败 =====
            BTWeaponCanFire canFireNode = new BTWeaponCanFire
            {
                Caster = "Caster",
                WeaponType = WeaponType.Rifle
            };
            BTWeaponCanFireHandler canFireHandler = new BTWeaponCanFireHandler();

            // 刚射击后因为时间间隔，不能立即再次射击
            int canFireResult = canFireHandler.Handle(canFireNode, env);
            if (canFireResult != 1)
            {
                Log.Console("Should NOT be able to fire immediately after a shot (fire interval)");
                env.Dispose();
                return 6;
            }

            // ===== 验证 BTWeaponInRange 范围判断 =====
            BTWeaponInRange inRangeNode = new BTWeaponInRange
            {
                Caster = "Caster",
                Target = "Target",
                Range = 10f
            };
            BTWeaponInRangeHandler inRangeHandler = new BTWeaponInRangeHandler();

            // 设置caster和target位置，距离5米
            caster.Position = new Unity.Mathematics.float3(0, 0, 0);
            target.Position = new Unity.Mathematics.float3(5, 0, 0);
            int inRangeResult = inRangeHandler.Handle(inRangeNode, env);
            if (inRangeResult != 0)
            {
                Log.Console("Target at 5m should be in 10m range");
                env.Dispose();
                return 7;
            }

            // target移到15m处，超出范围
            target.Position = new Unity.Mathematics.float3(15, 0, 0);
            inRangeResult = inRangeHandler.Handle(inRangeNode, env);
            if (inRangeResult != 1)
            {
                Log.Console("Target at 15m should NOT be in 10m range");
                env.Dispose();
                return 8;
            }

            // ===== 验证换弹后可以射击 =====
            // 先消耗所有弹药
            weaponComp.ConsumeAmmo(1001, 30);
            if (weaponComp.CanFire(1001))
            {
                Log.Console("Should not fire with 0 ammo");
                env.Dispose();
                return 9;
            }

            BTWeaponReload reloadNode = new BTWeaponReload
            {
                Caster = "Caster",
                WeaponType = WeaponType.Rifle
            };
            BTWeaponReloadHandler reloadHandler = new BTWeaponReloadHandler();
            reloadHandler.Handle(reloadNode, env);

            // 换弹后弹药恢复
            if (weaponComp.RifleAmmo != 30)
            {
                Log.Console($"After reload rifle should have 30 ammo, got {weaponComp.RifleAmmo}");
                env.Dispose();
                return 10;
            }

            // SMG弹药不受影响
            if (weaponComp.SMGAmmo != startSmgAmmo)
            {
                Log.Console("SMG ammo should not be affected by rifle reload");
                env.Dispose();
                return 11;
            }

            env.Dispose();

            Log.Console("Test_Integration_CombatFlow_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
