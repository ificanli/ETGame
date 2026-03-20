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
                        NodeId = 1,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerInteract
                    },
                    new FlowNodeData
                    {
                        NodeId = 2,
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
                        NodeId = 3,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnTimerElapsed,
                        Params = new List<FlowParam>
                        {
                            new FlowParam { Key = "timer_id", Value = "container_search" }
                        }
                    },
                    new FlowNodeData
                    {
                        NodeId = 4,
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
                Type = ECAPointType.Container,
                PosX = 0f,
                PosY = 0f,
                PosZ = 0f,
                Params = new List<FlowParam>
                {
                    new FlowParam { Key = ECAPointParamKey.InteractRange, Value = "3" }
                },
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
                        NodeId = 1,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerEnterRange
                    },
                    new FlowNodeData
                    {
                        NodeId = 2,
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
                Type = ECAPointType.SpawnPoint,
                PosX = 10f,
                PosY = 0f,
                PosZ = 5f,
                Params = new List<FlowParam>
                {
                    new FlowParam { Key = ECAPointParamKey.InteractRange, Value = "5" }
                },
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

    public class Ecanode_KeyDoor_Toggle_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_KeyDoor_Toggle_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();

            FlowGraphData graph = new()
            {
                Nodes = new List<FlowNodeData>
                {
                    new FlowNodeData
                    {
                        NodeId = 1,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerEnterRange
                    },
                    new FlowNodeData
                    {
                        NodeId = 2,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.RefreshDoorInteractHint
                    },
                    new FlowNodeData
                    {
                        NodeId = 3,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerLeaveRange
                    },
                    new FlowNodeData
                    {
                        NodeId = 4,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.HideInteractButton
                    },
                    new FlowNodeData
                    {
                        NodeId = 5,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerInteract
                    },
                    new FlowNodeData
                    {
                        NodeId = 6,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.ToggleDoor
                    }
                },
                Connections = new List<FlowConnectionData>
                {
                    new FlowConnectionData { FromNodeId = 1, ToNodeId = 2, Branch = "Out" },
                    new FlowConnectionData { FromNodeId = 3, ToNodeId = 4, Branch = "Out" },
                    new FlowConnectionData { FromNodeId = 5, ToNodeId = 6, Branch = "Out" }
                }
            };

            ECAConfig config = new()
            {
                ConfigId = "test_key_door_001",
                Type = ECAPointType.KeyDoor,
                PosX = 0f,
                PosY = 0f,
                PosZ = 0f,
                Params = new List<FlowParam>
                {
                    new FlowParam { Key = ECAPointParamKey.InteractRange, Value = "3" },
                    new FlowParam { Key = ECADoorParamKey.RequiredKeyItemId, Value = "10001" },
                    new FlowParam { Key = ECADoorParamKey.LockedButtonTextId, Value = "1" },
                    new FlowParam { Key = ECADoorParamKey.ClosedButtonTextId, Value = "2" },
                    new FlowParam { Key = ECADoorParamKey.OpenedButtonTextId, Value = "3" },
                    new FlowParam { Key = ECADoorParamKey.ConsumeKeyCount, Value = "1" }
                },
                FlowGraph = graph
            };

            ECALoader.LoadECAPoints(scene, new List<ECAConfig> { config });

            ECAManagerComponent manager = scene.GetComponent<ECAManagerComponent>();
            if (manager == null)
            {
                Log.Console("ECAManagerComponent not created");
                return 1;
            }

            ECAPointComponent point = manager.GetECAPoint("test_key_door_001");
            if (point == null)
            {
                Log.Console("ECAPointComponent not found");
                return 2;
            }

            Unit playerWithoutKey = unitComponent.AddChild<Unit, int>(0);
            point.OnPlayerInteract(playerWithoutKey);
            if (point.CurrentState != ECADoorState.Locked)
            {
                Log.Console($"expected locked state without key, got {point.CurrentState}");
                return 3;
            }

            Unit playerWithKey = unitComponent.AddChild<Unit, int>(0);
            ItemComponent itemComponent = playerWithKey.AddComponent<ItemComponent>();
            ItemHelper.AddItem(itemComponent, 10001, 1, ItemChangeReason.QuestReward);

            point.OnPlayerInteract(playerWithKey);
            if (point.CurrentState != ECADoorState.Opened)
            {
                Log.Console($"expected opened state after using key, got {point.CurrentState}");
                return 4;
            }

            if (itemComponent.GetItemCount(10001) != 0)
            {
                Log.Console($"expected key consumed, remain={itemComponent.GetItemCount(10001)}");
                return 5;
            }

            point.OnPlayerInteract(playerWithKey);
            if (point.CurrentState != ECADoorState.Closed)
            {
                Log.Console($"expected closed state after second interact, got {point.CurrentState}");
                return 6;
            }

            Log.Console("Ecanode_KeyDoor_Toggle_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_UnknownCondition_DefaultFalse_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_UnknownCondition_DefaultFalse_Test));
            ECAFlowConditionInvokeHandler handler = new();
            bool result = handler.Handle(new ECAFlowConditionInvoke
            {
                Key = "UnknownConditionKey"
            });

            if (result)
            {
                Log.Console("unknown condition key should return false");
                return 1;
            }

            Log.Console("Ecanode_UnknownCondition_DefaultFalse_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_PointDestroy_Cleanup_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_PointDestroy_Cleanup_Test));
            Scene scene = scope.TestFiber.Root;

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();
            TimerComponent timerComponent = scene.TimerComponent ?? scene.AddComponent<TimerComponent>();
            ECAManagerComponent manager = scene.AddComponent<ECAManagerComponent>();

            Unit pointUnit = unitComponent.AddChild<Unit, int>(0);
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>("destroy_cleanup_point", ECAPointType.Container, 3f);
            manager.AddECAPoint(point.PointId, pointUnit.Id);

            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.UnitType = UnitType.Player;

            if (!ECAFlowTimerHelper.StartTimer(point, player, "cleanup_timer", 1000))
            {
                Log.Console("failed to create flow timer");
                return 1;
            }

            if (point.FlowTimers.Count != 1)
            {
                Log.Console($"expected timer count 1, got {point.FlowTimers.Count}");
                return 2;
            }

            long timerEntityId = 0;
            foreach (long timerId in point.FlowTimers.Values)
            {
                timerEntityId = timerId;
                break;
            }

            pointUnit.Dispose();

            if (scene.GetChild<ECAFlowTimerComponent>(timerEntityId) != null)
            {
                Log.Console("flow timer should be disposed with point");
                return 3;
            }

            if (manager.ECAPoints.ContainsKey("destroy_cleanup_point"))
            {
                Log.Console("manager mapping should be removed on point destroy");
                return 4;
            }

            await timerComponent.WaitAsync(10);

            Log.Console("Ecanode_PointDestroy_Cleanup_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_RangeCleanupAndPlayerOnly_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_RangeCleanupAndPlayerOnly_Test));
            Scene scene = scope.TestFiber.Root;

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();
            ECAManagerComponent manager = scene.AddComponent<ECAManagerComponent>();

            Unit pointUnit = unitComponent.AddChild<Unit, int>(0);
            pointUnit.Position = float3.zero;
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>("range_cleanup_point", ECAPointType.Container, 3f);
            manager.AddECAPoint(point.PointId, pointUnit.Id);

            Unit validPlayer = unitComponent.AddChild<Unit, int>(0);
            validPlayer.UnitType = UnitType.Player;
            point.PlayersInRange.Add(validPlayer.Id);
            point.PlayersInRange.Add(999999);
            point.CleanupInvalidPlayersInRange(unitComponent);

            if (!point.PlayersInRange.Contains(validPlayer.Id))
            {
                Log.Console("valid player should stay in range set");
                return 1;
            }

            if (point.PlayersInRange.Contains(999999))
            {
                Log.Console("invalid player id should be removed from range set");
                return 2;
            }

            Unit monster = unitComponent.AddChild<Unit, int>(0);
            monster.UnitType = UnitType.Monster;
            monster.Position = float3.zero;
            ECAHelper.CheckPlayerInRange(monster);
            if (point.PlayersInRange.Contains(monster.Id))
            {
                Log.Console("non-player unit should not participate in range checks");
                return 3;
            }

            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.UnitType = UnitType.Player;
            player.Position = float3.zero;
            ECAHelper.CheckPlayerInRange(player);
            if (!point.PlayersInRange.Contains(player.Id))
            {
                Log.Console("player should enter range");
                return 4;
            }

            Log.Console("Ecanode_RangeCleanupAndPlayerOnly_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_CloseContainer_NoCreateContainer_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_CloseContainer_NoCreateContainer_Test));
            Scene scene = scope.TestFiber.Root;

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();
            Unit pointUnit = unitComponent.AddChild<Unit, int>(0);
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>("close_no_create_point", ECAPointType.RangeTrigger, 3f);
            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.UnitType = UnitType.Player;

            if (pointUnit.GetComponent<ContainerComponent>() != null)
            {
                Log.Console("container component should not exist before close/cancel");
                return 3;
            }

            bool canceled = ContainerRuntimeHelper.CancelSearch(point, player, notify: false);
            if (canceled)
            {
                Log.Console("cancel search should return false without existing container");
                return 1;
            }

            ContainerRuntimeHelper.CloseContainer(point, player);
            if (pointUnit.GetComponent<ContainerComponent>() != null)
            {
                Log.Console("close container should not create container component");
                return 2;
            }

            Log.Console("Ecanode_CloseContainer_NoCreateContainer_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_FlowGraph_RuntimeCache_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_FlowGraph_RuntimeCache_Test));

            FlowGraphData graph = new()
            {
                Nodes = new List<FlowNodeData>
                {
                    new()
                    {
                        NodeId = 1,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerInteract
                    },
                    new()
                    {
                        NodeId = 2,
                        NodeType = ECAFlowNodeType.State,
                        NodeKey = "State"
                    }
                },
                Connections = new List<FlowConnectionData>
                {
                    new()
                    {
                        FromNodeId = 1,
                        ToNodeId = 2,
                        Branch = "Out"
                    }
                }
            };

            await ECAFlowGraphRunner.TriggerEventAsync(graph, null, null, ECAFlowEventType.OnPlayerInteract);
            Dictionary<int, FlowNodeData> cachedNodeMap = graph.RuntimeNodeMap;
            Dictionary<int, Dictionary<string, List<int>>> cachedAdjacency = graph.RuntimeAdjacency;

            if (cachedNodeMap == null || cachedAdjacency == null)
            {
                Log.Console("graph runtime cache should be built after first trigger");
                return 1;
            }

            await ECAFlowGraphRunner.TriggerEventAsync(graph, null, null, ECAFlowEventType.OnPlayerInteract);
            if (!ReferenceEquals(cachedNodeMap, graph.RuntimeNodeMap) || !ReferenceEquals(cachedAdjacency, graph.RuntimeAdjacency))
            {
                Log.Console("graph runtime cache should be reused when graph shape is unchanged");
                return 2;
            }

            Log.Console("Ecanode_FlowGraph_RuntimeCache_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_FlowGraph_LegacyNodeId_Compat_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_FlowGraph_LegacyNodeId_Compat_Test));

            FlowGraphData graph = new()
            {
                Nodes = new List<FlowNodeData>
                {
                    new()
                    {
                        LegacyId = 7,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnMapLoaded
                    },
                    new()
                    {
                        LegacyId = 8,
                        NodeType = ECAFlowNodeType.State,
                        NodeKey = "LegacyState"
                    }
                },
                Connections = new List<FlowConnectionData>
                {
                    new()
                    {
                        FromNodeId = 7,
                        ToNodeId = 8,
                        Branch = "Out"
                    }
                }
            };

            await ECAFlowGraphRunner.TriggerEventAsync(graph, null, null, ECAFlowEventType.OnMapLoaded);
            if (graph.RuntimeNodeMap == null || graph.RuntimeAdjacency == null)
            {
                Log.Console("legacy node id graph should build runtime cache");
                return 1;
            }

            if (!graph.RuntimeNodeMap.ContainsKey(7) || !graph.RuntimeNodeMap.ContainsKey(8))
            {
                Log.Console("legacy node id should map to runtime node cache keys");
                return 2;
            }

            if (!graph.RuntimeAdjacency.TryGetValue(7, out Dictionary<string, List<int>> branchMap))
            {
                Log.Console("legacy node id should map to runtime adjacency key");
                return 3;
            }

            if (!branchMap.TryGetValue("Out", out List<int> nextList) || nextList.Count != 1 || nextList[0] != 8)
            {
                Log.Console("legacy node id adjacency should keep connection 7->8");
                return 4;
            }

            Log.Console("Ecanode_FlowGraph_LegacyNodeId_Compat_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_FlowGraph_ZeroNodeIdTitleCompat_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_FlowGraph_ZeroNodeIdTitleCompat_Test));
            Scene scene = scope.TestFiber.Root;

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();
            Unit pointUnit = unitComponent.AddChild<Unit, int>(0);
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>("legacy_zero_id_point", ECAPointType.RangeTrigger, 3f);
            point.IsActive = true;
            EntityRef<ECAPointComponent> pointRef = point;

            FlowGraphData graph = new()
            {
                Nodes = new List<FlowNodeData>
                {
                    new()
                    {
                        LegacyId = 0,
                        NodeId = 0,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnMapLoaded,
                        Title = "Event 7"
                    },
                    new()
                    {
                        LegacyId = 0,
                        NodeId = 0,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.SetPointActive,
                        Title = "Action 6",
                        Params = new List<FlowParam>
                        {
                            new() { Key = "active", Value = "0" }
                        }
                    }
                },
                Connections = new List<FlowConnectionData>
                {
                    new()
                    {
                        FromNodeId = 7,
                        ToNodeId = 6,
                        Branch = "Out"
                    }
                }
            };

            await ECAFlowGraphRunner.TriggerEventAsync(graph, point, null, ECAFlowEventType.OnMapLoaded);
            point = pointRef;
            if (point == null)
            {
                Log.Console("point should still exist after trigger");
                return 1;
            }

            if (point.IsActive)
            {
                Log.Console("zero node id graph should execute action via title fallback");
                return 2;
            }

            if (graph.RuntimeNodeMap == null || !graph.RuntimeNodeMap.ContainsKey(7) || !graph.RuntimeNodeMap.ContainsKey(6))
            {
                Log.Console("zero node id graph should repair runtime node map keys from title");
                return 3;
            }

            if (!graph.RuntimeAdjacency.TryGetValue(7, out Dictionary<string, List<int>> branchMap))
            {
                Log.Console("zero node id graph should keep adjacency key 7");
                return 4;
            }

            if (!branchMap.TryGetValue("Out", out List<int> nextList) || nextList.Count != 1 || nextList[0] != 6)
            {
                Log.Console("zero node id graph should keep connection 7->6");
                return 5;
            }

            Log.Console("Ecanode_FlowGraph_ZeroNodeIdTitleCompat_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_Evacuation_ConfigDriven_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_Evacuation_ConfigDriven_Test));
            Scene scene = scope.TestFiber.Root;

            scene.AddComponent<TimerComponent>();
            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();

            Unit pointUnit = unitComponent.AddChild<Unit, int>(0);
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>("evacuation_config_point", ECAPointType.EvacuationPoint, 5f);
            point.Params = new List<FlowParam>
            {
                new() { Key = ECAPointParamKey.EvacuationDurationMs, Value = "15000" },
                new() { Key = ECAPointParamKey.LobbyMapName, Value = "Lobby_Test_Map" }
            };

            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.UnitType = UnitType.Player;
            EntityRef<Unit> playerRef = player;

            await point.OnPlayerEnter(player);
            player = playerRef;
            PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation == null)
            {
                Log.Console("evacuation component should be created from fallback evacuation logic");
                return 1;
            }

            if (evacuation.RequiredTime != 15000)
            {
                Log.Console($"expected evacuation duration 15000, got {evacuation.RequiredTime}");
                return 2;
            }

            if (evacuation.LobbyMapName != "Lobby_Test_Map")
            {
                Log.Console($"expected lobby map Lobby_Test_Map, got {evacuation.LobbyMapName}");
                return 3;
            }

            Log.Console("Ecanode_Evacuation_ConfigDriven_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_Evacuation_FlowGraphFallback_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_Evacuation_FlowGraphFallback_Test));
            Scene scene = scope.TestFiber.Root;
            EntityRef<Scene> sceneRef = scene;

            scene.AddComponent<TimerComponent>();
            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();

            FlowGraphData graph = new()
            {
                Nodes = new List<FlowNodeData>
                {
                    new FlowNodeData
                    {
                        NodeId = 1,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerEnterRange
                    },
                    new FlowNodeData
                    {
                        NodeId = 2,
                        NodeType = ECAFlowNodeType.Condition,
                        NodeKey = ECAFlowConditionKey.PlayerInRange
                    },
                    new FlowNodeData
                    {
                        NodeId = 3,
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
                    new FlowConnectionData { FromNodeId = 2, ToNodeId = 3, Branch = "True" }
                }
            };

            ECAConfig config = new()
            {
                ConfigId = "evacuation_flowgraph_fallback_point",
                Type = ECAPointType.EvacuationPoint,
                PosX = 0f,
                PosY = 0f,
                PosZ = 0f,
                Params = new List<FlowParam>
                {
                    new FlowParam { Key = ECAPointParamKey.InteractRange, Value = "5" },
                    new FlowParam { Key = ECAPointParamKey.EvacuationDurationMs, Value = "15000" },
                    new FlowParam { Key = ECAPointParamKey.LobbyMapName, Value = "Home" }
                },
                FlowGraph = graph
            };

            ECALoader.LoadECAPoints(scene, new List<ECAConfig> { config });

            ECAManagerComponent manager = scene.GetComponent<ECAManagerComponent>();
            if (manager == null)
            {
                Log.Console("ECAManagerComponent not created");
                return 1;
            }

            ECAPointComponent point = manager.GetECAPoint("evacuation_flowgraph_fallback_point");
            if (point == null)
            {
                Log.Console("ECAPointComponent not found");
                return 2;
            }

            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.UnitType = UnitType.Player;

            EntityRef<ECAPointComponent> pointRef = point;
            EntityRef<Unit> playerRef = player;

            await point.OnPlayerEnter(player);

            scene = sceneRef;
            point = pointRef;
            player = playerRef;
            if (point == null || player == null)
            {
                Log.Console("point or player disposed after OnPlayerEnter");
                return 3;
            }

            if (point.CurrentState != 1)
            {
                Log.Console($"expected point state 1, got {point.CurrentState}");
                return 4;
            }

            PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation == null)
            {
                Log.Console("evacuation component should be created when flow graph lacks evacuation actions");
                return 5;
            }

            if (evacuation.RequiredTime != 15000)
            {
                Log.Console($"expected evacuation duration 15000, got {evacuation.RequiredTime}");
                return 6;
            }

            if (evacuation.LobbyMapName != "Home")
            {
                Log.Console($"expected lobby map Home, got {evacuation.LobbyMapName}");
                return 7;
            }

            point.OnPlayerLeave(player);
            await scene.TimerComponent.WaitAsync(100);
            scene = sceneRef;

            player = playerRef;
            if (player == null)
            {
                Log.Console("player disposed after OnPlayerLeaveAsync");
                return 8;
            }

            if (player.GetComponent<PlayerEvacuationComponent>() != null)
            {
                Log.Console("evacuation component should be removed after leaving range");
                return 9;
            }

            Log.Console("Ecanode_Evacuation_FlowGraphFallback_Test passed");
            return ErrorCode.ERR_Success;
        }
    }

    public class Ecanode_Evacuation_StartCountdownAction_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_Evacuation_StartCountdownAction_Test));
            Scene scene = scope.TestFiber.Root;
            EntityRef<Scene> sceneRef = scene;

            scene.AddComponent<TimerComponent>();
            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();

            FlowGraphData graph = new()
            {
                Nodes = new List<FlowNodeData>
                {
                    new FlowNodeData
                    {
                        NodeId = 1,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerEnterRange
                    },
                    new FlowNodeData
                    {
                        NodeId = 2,
                        NodeType = ECAFlowNodeType.Condition,
                        NodeKey = ECAFlowConditionKey.PlayerInRange
                    },
                    new FlowNodeData
                    {
                        NodeId = 3,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.StartEvacCountdown,
                        Params = new List<FlowParam>
                        {
                            new FlowParam { Key = "seconds", Value = "15" }
                        }
                    }
                },
                Connections = new List<FlowConnectionData>
                {
                    new FlowConnectionData { FromNodeId = 1, ToNodeId = 2, Branch = "Out" },
                    new FlowConnectionData { FromNodeId = 2, ToNodeId = 3, Branch = "True" }
                }
            };

            ECAConfig config = new()
            {
                ConfigId = "evacuation_start_countdown_action_point",
                Type = ECAPointType.EvacuationPoint,
                PosX = 0f,
                PosY = 0f,
                PosZ = 0f,
                Params = new List<FlowParam>
                {
                    new FlowParam { Key = ECAPointParamKey.InteractRange, Value = "5" },
                    new FlowParam { Key = ECAPointParamKey.LobbyMapName, Value = "Home" }
                },
                FlowGraph = graph
            };

            ECALoader.LoadECAPoints(scene, new List<ECAConfig> { config });

            ECAManagerComponent manager = scene.GetComponent<ECAManagerComponent>();
            if (manager == null)
            {
                Log.Console("ECAManagerComponent not created");
                return 1;
            }

            ECAPointComponent point = manager.GetECAPoint("evacuation_start_countdown_action_point");
            if (point == null)
            {
                Log.Console("ECAPointComponent not found");
                return 2;
            }

            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.UnitType = UnitType.Player;
            player.Position = new float3(0f, 0f, 0f);

            EntityRef<ECAPointComponent> pointRef = point;
            EntityRef<Unit> playerRef = player;
            await point.OnPlayerEnter(player);

            scene = sceneRef;
            point = pointRef;
            player = playerRef;
            if (player == null)
            {
                Log.Console("player disposed after OnPlayerEnter");
                return 3;
            }

            PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation == null)
            {
                Log.Console("evacuation component should be created by StartEvacCountdown");
                return 4;
            }

            if (evacuation.RequiredTime != 15000)
            {
                Log.Console($"expected evacuation duration 15000, got {evacuation.RequiredTime}");
                return 5;
            }

            if (evacuation.LobbyMapName != "Home")
            {
                Log.Console($"expected lobby map Home, got {evacuation.LobbyMapName}");
                return 6;
            }

            point = pointRef;
            point.OnPlayerLeave(player);
            await scene.TimerComponent.WaitAsync(100);
            scene = sceneRef;
            point = pointRef;

            player = playerRef;
            if (player == null)
            {
                Log.Console("player disposed after OnPlayerLeaveAsync");
                return 7;
            }

            if (player.GetComponent<PlayerEvacuationComponent>() != null)
            {
                Log.Console("evacuation component should be removed after leaving range");
                return 8;
            }

            player.Position = new float3(0f, 12f, 0f);
            ECAHelper.CheckPlayerInRange(player);
            await scene.TimerComponent.WaitAsync(300);

            scene = sceneRef;
            point = pointRef;
            player = playerRef;
            if (player == null)
            {
                Log.Console("player disposed after high-difference range check");
                return 9;
            }

            evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation == null)
            {
                Log.Console("evacuation component should be created when player matches point on XZ plane");
                return 10;
            }

            evacuation.Update();
            evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation == null)
            {
                Log.Console("evacuation component should remain active when only Y differs");
                return 11;
            }

            point.OnPlayerLeave(player);
            await scene.TimerComponent.WaitAsync(100);

            scene = sceneRef;
            point = pointRef;
            player = playerRef;
            if (player?.GetComponent<PlayerEvacuationComponent>() != null)
            {
                Log.Console("evacuation component should be removed after leaving range with height difference");
                return 12;
            }

            Log.Console("Ecanode_Evacuation_StartCountdownAction_Test passed");
            return ErrorCode.ERR_Success;
        }
    }
}
