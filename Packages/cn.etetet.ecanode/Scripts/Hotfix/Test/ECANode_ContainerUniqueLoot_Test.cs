using System.Collections.Generic;
using ET;
using ET.Server;

namespace ET.Test
{
    public class Ecanode_Container_UniqueLootNoRepeat_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_Container_UniqueLootNoRepeat_Test));
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
                            new FlowParam { Key = "loot_table", Value = "10001*1|10002*1|10003*1|10004*1|10005*1|10006*1" },
                            new FlowParam { Key = "count", Value = "4-5" },
                            new FlowParam { Key = "radius", Value = "0" },
                            new FlowParam { Key = "allow_repeat", Value = "0" }
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
                ConfigId = "test_container_unique_loot_no_repeat",
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

            ECAPointComponent point = manager.GetECAPoint("test_container_unique_loot_no_repeat");
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

            if (container.LastDropCount < 4 || container.LastDropCount > 5)
            {
                Log.Console($"resolved count should be within range 4-5, actual={container.LastDropCount}");
                return 6;
            }

            if (container.ItemEntries.Count != container.LastDropCount)
            {
                Log.Console($"unique loot should not repeat, entries={container.ItemEntries.Count}, resolved={container.LastDropCount}");
                return 7;
            }

            HashSet<int> configIds = new HashSet<int>();
            foreach (ContainerItemEntry entry in container.ItemEntries.Values)
            {
                if (!configIds.Add(entry.ConfigId))
                {
                    Log.Console($"duplicate config id found: {entry.ConfigId}");
                    return 8;
                }

                if (entry.Count != 1)
                {
                    Log.Console($"unique loot entry count should stay 1, configId={entry.ConfigId}, count={entry.Count}");
                    return 9;
                }
            }

            Log.Console("Ecanode_Container_UniqueLootNoRepeat_Test passed");
            return ErrorCode.ERR_Success;
        }
    }
}
