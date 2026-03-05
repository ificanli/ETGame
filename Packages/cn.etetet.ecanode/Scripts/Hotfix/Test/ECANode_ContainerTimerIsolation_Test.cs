using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    public class Ecanode_Container_MultiPlayerTimerIsolation_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_Container_MultiPlayerTimerIsolation_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            scene.AddComponent<UnitComponent>();
            TimerComponent timerComponent = scene.TimerComponent ?? scene.AddComponent<TimerComponent>();

            FlowGraphData flowGraph = new()
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
                            new FlowParam { Key = "seconds", Value = "0.2" },
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
                    },
                    new FlowNodeData
                    {
                        Id = 5,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.OpenContainerUI
                    }
                },
                Connections = new List<FlowConnectionData>
                {
                    new FlowConnectionData { FromNodeId = 1, ToNodeId = 2, Branch = "Out" },
                    new FlowConnectionData { FromNodeId = 3, ToNodeId = 4, Branch = "Out" },
                    new FlowConnectionData { FromNodeId = 4, ToNodeId = 5, Branch = "Out" }
                }
            };

            ECAConfig config = new()
            {
                ConfigId = "test_container_timer_isolation",
                Type = ECAPointType.Container,
                PosX = 0f,
                PosY = 0f,
                PosZ = 0f,
                Params = new List<FlowParam>
                {
                    new FlowParam { Key = ECAPointParamKey.InteractRange, Value = "3" }
                },
                FlowGraph = flowGraph
            };

            ECALoader.LoadECAPoints(scene, new List<ECAConfig> { config });
            ECAManagerComponent manager = scene.GetComponent<ECAManagerComponent>();
            if (manager == null)
            {
                Log.Console("manager is null");
                return 1;
            }

            ECAPointComponent point = manager.GetECAPoint("test_container_timer_isolation");
            if (point == null)
            {
                Log.Console("point is null");
                return 2;
            }

            EntityRef<ECAPointComponent> pointRef = point;
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            Unit player1 = unitComponent.AddChild<Unit, int>(0);
            Unit player2 = unitComponent.AddChild<Unit, int>(0);
            EntityRef<Unit> player2Ref = player2;

            point.OnPlayerInteract(player1);
            point.OnPlayerInteract(player2);

            ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
            if (container == null)
            {
                Log.Console("container is null");
                return 3;
            }

            if (!container.IsSearching(player1.Id) || !container.IsSearching(player2.Id))
            {
                Log.Console($"expected both players searching, p1={container.IsSearching(player1.Id)} p2={container.IsSearching(player2.Id)}");
                return 4;
            }

            if (point.FlowTimers.Count != 2)
            {
                Log.Console($"expected 2 flow timers, got {point.FlowTimers.Count}");
                return 5;
            }

            ContainerRuntimeHelper.CancelSearch(point, player1, notify: false);
            if (container.IsSearching(player1.Id))
            {
                Log.Console("player1 search should be canceled");
                return 6;
            }

            if (!container.IsSearching(player2.Id))
            {
                Log.Console("player2 search should remain active");
                return 7;
            }

            if (point.FlowTimers.Count != 1)
            {
                Log.Console($"expected 1 flow timer after cancel player1, got {point.FlowTimers.Count}");
                return 8;
            }

            await timerComponent.WaitAsync(400);
            point = pointRef;
            player2 = player2Ref;
            if (point == null)
            {
                Log.Console("point disposed after await");
                return 9;
            }

            container = ContainerComponentSystem.GetOrAdd(point);
            if (container == null)
            {
                Log.Console("container null after await");
                return 10;
            }

            if (point.CurrentState != 1)
            {
                Log.Console($"expected point state 1 after player2 timer elapsed, got {point.CurrentState}");
                return 11;
            }

            if (container.IsSearching(player2.Id))
            {
                Log.Console("player2 search session should be cleared after open container");
                return 12;
            }

            if (point.FlowTimers.Count != 0)
            {
                Log.Console($"expected flow timers empty after elapsed, got {point.FlowTimers.Count}");
                return 13;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
