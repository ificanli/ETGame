using System.Collections.Generic;
using ET;
using ET.Server;

namespace ET.Test
{
    public class Ecanode_Container_RandomCountRange_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_Container_RandomCountRange_Test));
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
                        NodeKey = ECAFlowActionKey.GenerateContainerLoot,
                        Params = new List<FlowParam>
                        {
                            new FlowParam { Key = "output_mode", Value = ContainerOutputMode.ContainerPanel.ToString() },
                            new FlowParam { Key = "loot_table", Value = "10001*1" },
                            new FlowParam { Key = "count", Value = "2-4" },
                            new FlowParam { Key = "radius", Value = "0" }
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
                ConfigId = "test_container_random_count_range",
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

            ECAPointComponent point = manager.GetECAPoint("test_container_random_count_range");
            if (point == null)
            {
                Log.Console("ECAPointComponent not found");
                return 2;
            }

            EntityRef<ECAPointComponent> pointRef = point;
            Unit player = unitComponent.AddChild<Unit, int>(0);
            point.OnPlayerInteract(player);

            await timerComponent.WaitAsync(100);
            point = pointRef;
            if (point == null)
            {
                Log.Console("ECAPointComponent disposed after await");
                return 3;
            }

            Unit pointUnit = point.GetParent<Unit>();
            ContainerComponent container = pointUnit?.GetComponent<ContainerComponent>();
            if (container == null)
            {
                Log.Console("ContainerComponent not created");
                return 4;
            }

            if (!container.LootGenerated)
            {
                Log.Console("container loot should be generated");
                return 5;
            }

            if (container.LastDropCount < 2 || container.LastDropCount > 4)
            {
                Log.Console($"resolved count should be within range 2-4, actual={container.LastDropCount}");
                return 6;
            }

            if (container.ItemEntries.Count != 1)
            {
                Log.Console($"container item entry count mismatch: {container.ItemEntries.Count}");
                return 7;
            }

            if (!container.TryGetItem(0, out ContainerItemEntry item))
            {
                Log.Console("container item should exist in slot 0");
                return 8;
            }

            if (item.ConfigId != 10001)
            {
                Log.Console($"container item config mismatch: {item.ConfigId}");
                return 9;
            }

            if (item.Count != container.LastDropCount)
            {
                Log.Console($"container item count should match resolved count, item={item.Count}, resolved={container.LastDropCount}");
                return 10;
            }

            Log.Console("Ecanode_Container_RandomCountRange_Test passed");
            return ErrorCode.ERR_Success;
        }
    }
}
