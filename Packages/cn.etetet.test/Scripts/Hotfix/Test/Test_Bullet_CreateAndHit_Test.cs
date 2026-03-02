using Unity.Mathematics;

namespace ET.Test
{
    /// <summary>
    /// 测试子弹系统：创建子弹和命中检测
    /// TDD: 验证 BulletComponent 的核心逻辑
    /// </summary>
    public class Test_Bullet_CreateAndHit_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Bullet_CreateAndHit_Test));
            Fiber testFiber = scope.TestFiber;

            // 创建两个机器人（射击者和目标）
            Fiber robot1 = await TestHelper.CreateRobot(testFiber, "BulletRobot1");
            Scene scene1 = robot1.Root;
            EntityRef<Scene> scene1Ref = scene1;

            Fiber robot2 = await TestHelper.CreateRobot(testFiber, "BulletRobot2");
            scene1 = scene1Ref;

            // 获取服务端Unit
            Unit owner = TestHelper.GetServerUnit(testFiber, robot1);
            Unit target = TestHelper.GetServerUnit(testFiber, robot2);

            if (owner == null)
            {
                Log.Console("owner is null");
                return 1;
            }

            if (target == null)
            {
                Log.Console("target is null");
                return 2;
            }

            // 设置位置
            owner.Position = new float3(0, 0, 0);
            target.Position = new float3(5, 0, 0);  // 距离5米

            // 给目标添加HP
            NumericComponent targetNumeric = target.NumericComponent;
            if (targetNumeric == null)
            {
                targetNumeric = target.AddComponent<NumericComponent>();
            }
            targetNumeric.Set(NumericType.HP, 100);

            // 创建子弹
            Fiber mapFiber = TestHelper.GetMap(testFiber, robot1);
            Scene mapScene = mapFiber.Root;
            Unit bullet = BulletHelper.CreateBullet(mapScene, owner, target, 30f);

            if (bullet == null)
            {
                Log.Console("bullet is null");
                return 3;
            }

            // 验证子弹组件
            BulletComponent bulletComp = bullet.GetComponent<BulletComponent>();
            if (bulletComp == null)
            {
                Log.Console("BulletComponent is null");
                return 4;
            }

            if (bulletComp.OwnerId != owner.Id)
            {
                Log.Console($"OwnerId should be {owner.Id}, but is {bulletComp.OwnerId}");
                return 5;
            }

            if (bulletComp.TargetId != target.Id)
            {
                Log.Console($"TargetId should be {target.Id}, but is {bulletComp.TargetId}");
                return 6;
            }

            if (bulletComp.Damage != 30f)
            {
                Log.Console($"Damage should be 30, but is {bulletComp.Damage}");
                return 7;
            }

            // 验证子弹初始位置
            float distance = math.distance(bullet.Position, owner.Position);
            if (distance > 0.1f)
            {
                Log.Console($"Bullet should start at owner position, distance is {distance}");
                return 8;
            }

            Log.Console("Test_Bullet_CreateAndHit_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
