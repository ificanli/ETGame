using System.Collections.Generic;
using Unity.Mathematics;
using ET;
using ET.Server;

namespace ET.Test
{
    public class Ecanode_ContainerFlow_Timer_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_ContainerFlow_Timer_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();
            TimerComponent timerComponent = scene.TimerComponent ?? scene.AddComponent<TimerComponent>();

            FlowGraphData graph = new()
            {
                Nodes = new List<FlowNodeData>
                {
                    new FlowNodeData
                    {
                        Id = 1,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerInteract
                    },
                    new FlowNodeData
                    {
                        Id = 2,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.StartSearchTimer,
                        Params = new List<FlowParam>
                        {
                            new FlowParam { Key = "seconds", Value = "0.1" },
                            new FlowParam { Key = "timer_id", Value = "container_search" }
                        }
                    },
                    new FlowNodeData
                    {
                        Id = 3,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnTimerElapsed,
                        Params = new List<FlowParam>
                        {
                            new FlowParam { Key = "timer_id", Value = "container_search" }
                        }
                    },
                    new FlowNodeData
                    {
                        Id = 4,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.SetPointState,
                        Params = new List<FlowParam>
                        {
                            new FlowParam { Key = "state", Value = "1" }
                        }
                    }
                },
                Connections = new List<FlowConnectionData>
                {
                    new FlowConnectionData { FromNodeId = 1, ToNodeId = 2, Branch = "Out" },
                    new FlowConnectionData { FromNodeId = 3, ToNodeId = 4, Branch = "Out" }
                }
            };

            ECAConfig config = new()
            {
                ConfigId = "test_container_001",
                PointType = ECAPointType.Container,
                PosX = 0f,
                PosY = 0f,
                PosZ = 0f,
                InteractRange = 3f,
                FlowGraph = graph
            };

            ECALoader.LoadECAPoints(scene, new List<ECAConfig> { config });

            ECAManagerComponent manager = scene.GetComponent<ECAManagerComponent>();
            if (manager == null)
            {
                Log.Console("ECAManagerComponent not created");
                return 1;
            }

            ECAPointComponent point = manager.GetECAPoint("test_container_001");
            if (point == null)
            {
                Log.Console("ECAPointComponent not found");
                return 2;
            }
            EntityRef<ECAPointComponent> pointRef = point;

            Unit player = unitComponent.AddChild<Unit, int>(0);
            point.OnPlayerInteract(player);

            await timerComponent.WaitAsync(200);
            point = pointRef;
            if (point == null)
            {
                Log.Console("ECAPointComponent disposed after await");
                return 6;
            }

            if (point.CurrentState != 1)
            {
                Log.Console($"Expected point state 1, got {point.CurrentState}");
                return 3;
            }

            Unit pointUnit = point.GetParent<Unit>();
            ContainerComponent container = pointUnit?.GetComponent<ContainerComponent>();
            if (container == null)
            {
                Log.Console("ContainerComponent not created");
                return 4;
            }

            if (container.State != 1)
            {
                Log.Console($"Expected container state 1, got {container.State}");
                return 5;
            }

            Log.Console("Ecanode_ContainerFlow_Timer_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_SpawnPoint_Request_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_SpawnPoint_Request_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();
            TimerComponent timerComponent = scene.TimerComponent ?? scene.AddComponent<TimerComponent>();

            FlowGraphData graph = new()
            {
                Nodes = new List<FlowNodeData>
                {
                    new FlowNodeData
                    {
                        Id = 1,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerEnterRange
                    },
                    new FlowNodeData
                    {
                        Id = 2,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.SpawnMonsters,
                        Params = new List<FlowParam>
                        {
                            new FlowParam { Key = "group_id", Value = "group_001" },
                            new FlowParam { Key = "count", Value = "3" }
                        }
                    }
                },
                Connections = new List<FlowConnectionData>
                {
                    new FlowConnectionData { FromNodeId = 1, ToNodeId = 2, Branch = "Out" }
                }
            };

            ECAConfig config = new()
            {
                ConfigId = "test_spawn_001",
                PointType = ECAPointType.SpawnPoint,
                PosX = 10f,
                PosY = 0f,
                PosZ = 5f,
                InteractRange = 5f,
                FlowGraph = graph
            };

            ECALoader.LoadECAPoints(scene, new List<ECAConfig> { config });

            ECAManagerComponent manager = scene.GetComponent<ECAManagerComponent>();
            if (manager == null)
            {
                Log.Console("ECAManagerComponent not created");
                return 1;
            }

            ECAPointComponent point = manager.GetECAPoint("test_spawn_001");
            if (point == null)
            {
                Log.Console("ECAPointComponent not found");
                return 2;
            }
            EntityRef<ECAPointComponent> pointRef = point;

            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.Position = new float3(10f, 0f, 5f);

            ECAHelper.CheckPlayerInRange(player);
            await timerComponent.WaitAsync(100);
            point = pointRef;
            if (point == null)
            {
                Log.Console("ECAPointComponent disposed after await");
                return 6;
            }

            Unit pointUnit = point.GetParent<Unit>();
            SpawnPointComponent spawnPoint = pointUnit?.GetComponent<SpawnPointComponent>();
            if (spawnPoint == null)
            {
                Log.Console("SpawnPointComponent not created");
                return 3;
            }

            if (spawnPoint.LastGroupId != "group_001")
            {
                Log.Console($"Expected group_id group_001, got {spawnPoint.LastGroupId}");
                return 4;
            }

            if (spawnPoint.LastSpawnCount != 3)
            {
                Log.Console($"Expected spawn count 3, got {spawnPoint.LastSpawnCount}");
                return 5;
            }

            Log.Console("Ecanode_SpawnPoint_Request_Test passed");
            return ErrorCode.ERR_Success;
        }
    }
}
