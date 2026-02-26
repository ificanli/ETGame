using System.Collections.Generic;
using Unity.Mathematics;
using ET.Server;

namespace ET.Test
{
    public class Ecanode_LoadECAPoints_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_LoadECAPoints_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            // 添加必要的组件
            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();

            // 创建测试配置
            List<ECAConfig> configs = new()
            {
                new ECAConfig
                {
                    ConfigId = "test_evac_001",
                    PointType = 1, // EvacuationPoint
                    PosX = 100f,
                    PosY = 0f,
                    PosZ = 50f,
                    InteractRange = 5f
                }
            };

            // 加载 ECA 点
            ECALoader.LoadECAPoints(scene, configs);

            // 验证
            ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>();
            if (ecaManager == null)
            {
                Log.Console("ECAManagerComponent not created");
                return 1;
            }

            if (ecaManager.ECAPoints.Count != 1)
            {
                Log.Console($"Expected 1 ECA point, but got {ecaManager.ECAPoints.Count}");
                return 2;
            }

            Log.Console("Test_LoadECAPoints passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_PlayerEnterEvacuation_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_PlayerEnterEvacuation_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            // 添加必要的组件
            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();

            // 创建 ECA 点
            List<ECAConfig> configs = new()
            {
                new ECAConfig
                {
                    ConfigId = "test_evac_002",
                    PointType = 1,
                    PosX = 100f,
                    PosY = 0f,
                    PosZ = 50f,
                    InteractRange = 5f
                }
            };

            ECALoader.LoadECAPoints(scene, configs);

            // 创建测试玩家
            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.Position = new float3(100f, 0f, 50f); // 在撤离点范围内

            // 触发范围检测
            ECAHelper.CheckPlayerInRange(player);

            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> playerRef = player;
            await scene.TimerComponent.WaitAsync(100); // 等待异步操作完成
            scene = sceneRef;
            player = playerRef;

            // 验证玩家是否开始撤离
            PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation == null)
            {
                Log.Console("PlayerEvacuationComponent not created");
                return 1;
            }

            if (evacuation.Status != 1)
            {
                Log.Console($"Expected status 1 (InProgress), but got {evacuation.Status}");
                return 2;
            }

            if (evacuation.RequiredTime != 10000)
            {
                Log.Console($"Expected required time 10000ms, but got {evacuation.RequiredTime}");
                return 3;
            }

            Log.Console("Test_PlayerEnterEvacuation passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_PlayerLeaveEvacuation_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_PlayerLeaveEvacuation_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            // 添加必要的组件
            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();

            // 创建 ECA 点
            List<ECAConfig> configs = new()
            {
                new ECAConfig
                {
                    ConfigId = "test_evac_003",
                    PointType = 1,
                    PosX = 100f,
                    PosY = 0f,
                    PosZ = 50f,
                    InteractRange = 5f
                }
            };

            ECALoader.LoadECAPoints(scene, configs);

            // 创建测试玩家
            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.Position = new float3(100f, 0f, 50f); // 在范围内

            // 触发进入
            ECAHelper.CheckPlayerInRange(player);

            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> playerRef = player;
            await scene.TimerComponent.WaitAsync(100);
            scene = sceneRef;
            player = playerRef;

            // 验证开始撤离
            PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation == null)
            {
                Log.Console("PlayerEvacuationComponent not created after entering");
                return 1;
            }

            // 玩家离开范围
            player.Position = new float3(200f, 0f, 50f); // 远离撤离点

            // 触发离开
            ECAHelper.CheckPlayerInRange(player);
            await scene.TimerComponent.WaitAsync(100);
            scene = sceneRef;
            player = playerRef;

            // 验证撤离被取消
            evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation != null)
            {
                Log.Console("PlayerEvacuationComponent should be removed after leaving");
                return 2;
            }

            Log.Console("Test_PlayerLeaveEvacuation passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_EvacuationComplete_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_EvacuationComplete_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            // 添加必要的组件
            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();

            // 创建 ECA 点
            List<ECAConfig> configs = new()
            {
                new ECAConfig
                {
                    ConfigId = "test_evac_004",
                    PointType = 1,
                    PosX = 100f,
                    PosY = 0f,
                    PosZ = 50f,
                    InteractRange = 5f
                }
            };

            ECALoader.LoadECAPoints(scene, configs);

            // 创建测试玩家
            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.Position = new float3(100f, 0f, 50f);

            // 触发进入
            ECAHelper.CheckPlayerInRange(player);

            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> playerRef = player;
            await scene.TimerComponent.WaitAsync(100);
            scene = sceneRef;
            player = playerRef;

            PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation == null)
            {
                Log.Console("PlayerEvacuationComponent not created");
                return 1;
            }

            Log.Console("Waiting for evacuation to complete (10 seconds)...");

            // 等待撤离完成（10秒 + 缓冲）
            await scene.TimerComponent.WaitAsync(11000);
            scene = sceneRef;
            player = playerRef;

            // 验证撤离完成后组件被移除
            evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation != null)
            {
                Log.Console("PlayerEvacuationComponent should be removed after completion");
                return 2;
            }

            Log.Console("Test_EvacuationComplete passed");
            return ErrorCode.ERR_Success;
        }
    }
}
