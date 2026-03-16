using System.Collections.Generic;
using Unity.Mathematics;
using ET;
using ET.Server;

namespace ET.Test
{
    public class Ecanode_ConcealmentVisibility_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_ConcealmentVisibility_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            ExtraUnitVisibilityComponent visibility = scene.GetComponent<ExtraUnitVisibilityComponent>() ?? scene.AddComponent<ExtraUnitVisibilityComponent>();

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
                        NodeKey = ECAFlowActionKey.ApplyStealth,
                        Params = new List<FlowParam>
                        {
                            new FlowParam { Key = "self_alpha", Value = "0.5" }
                        }
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
                        NodeKey = ECAFlowActionKey.RemoveStealth
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
                ConfigId = "test_bush_001",
                Type = ECAPointType.RangeTrigger,
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

            ECAPointComponent point = manager.GetECAPoint("test_bush_001");
            if (point == null)
            {
                Log.Console("ECAPointComponent not found");
                return 2;
            }

            if (visibility == null)
            {
                Log.Console("ExtraUnitVisibilityComponent not created");
                return 3;
            }

            Unit player1 = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), 0);
            Unit player2 = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), 0);
            player1.UnitType = UnitType.Player;
            player2.UnitType = UnitType.Player;
            player1.Position = new float3(0f, 0f, 0f);
            player2.Position = new float3(6f, 0f, 0f);

            EntityRef<ECAPointComponent> pointRef = point;
            EntityRef<ExtraUnitVisibilityComponent> visibilityRef = visibility;
            EntityRef<Unit> player1Ref = player1;
            EntityRef<Unit> player2Ref = player2;

            await point.OnPlayerEnter(player1);

            point = pointRef;
            visibility = visibilityRef;
            player1 = player1Ref;
            player2 = player2Ref;
            if (point == null || visibility == null || player1 == null || player2 == null)
            {
                Log.Console("entity disposed after player1 enter");
                return 4;
            }

            if (!visibility.ShouldConcealTargetFromViewer(player2, player1))
            {
                Log.Console("player outside concealment should not see player inside concealment");
                return 5;
            }

            if (!visibility.IsTargetConcealed(player2.Id, player1.Id))
            {
                Log.Console("concealed relation missing after player1 enters concealment");
                return 6;
            }

            if (visibility.ShouldConcealTargetFromViewer(player1, player2))
            {
                Log.Console("player inside concealment should still see player outside concealment");
                return 7;
            }

            await point.OnPlayerEnter(player2);

            point = pointRef;
            visibility = visibilityRef;
            player1 = player1Ref;
            player2 = player2Ref;
            if (point == null || visibility == null || player1 == null || player2 == null)
            {
                Log.Console("entity disposed after player2 enter");
                return 8;
            }

            if (visibility.ShouldConcealTargetFromViewer(player1, player2) || visibility.ShouldConcealTargetFromViewer(player2, player1))
            {
                Log.Console("players inside same concealment source should see each other");
                return 9;
            }

            if (visibility.IsTargetConcealed(player1.Id, player2.Id) || visibility.IsTargetConcealed(player2.Id, player1.Id))
            {
                Log.Console("concealed relation should be cleared when both players share source");
                return 10;
            }

            point.OnPlayerLeave(player1);

            point = pointRef;
            visibility = visibilityRef;
            player1 = player1Ref;
            player2 = player2Ref;
            if (point == null || visibility == null || player1 == null || player2 == null)
            {
                Log.Console("entity disposed after player1 leave");
                return 11;
            }

            if (!visibility.ShouldConcealTargetFromViewer(player1, player2))
            {
                Log.Console("player outside concealment should not see player remaining inside concealment");
                return 12;
            }

            if (!visibility.IsTargetConcealed(player1.Id, player2.Id))
            {
                Log.Console("concealed relation missing after player1 leaves concealment");
                return 13;
            }

            if (visibility.ShouldConcealTargetFromViewer(player2, player1))
            {
                Log.Console("player inside concealment should still see player outside after leave");
                return 14;
            }

            if (visibility.IsTargetConcealed(player2.Id, player1.Id))
            {
                Log.Console("reverse concealed relation should not exist after player1 leaves source");
                return 15;
            }

            point.OnPlayerLeave(player2);

            if (visibility.IsTargetConcealed(player1.Id, player2.Id) || visibility.IsTargetConcealed(player2.Id, player1.Id))
            {
                Log.Console("concealed relation should be cleared after both players leave");
                return 16;
            }

            Log.Console("Ecanode_ConcealmentVisibility_Test passed");
            return ErrorCode.ERR_Success;
        }
    }
}
