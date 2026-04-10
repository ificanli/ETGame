using System.Reflection;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Test_ClickUnit_Enemy_NoLock_Test : ATestHandler
    {
        private const long QuestNpcUnitId = 10024;

        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClickUnit_Enemy_NoLock_Test));
            Scene scene = scope.TestFiber.Root;

            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            Unit enemy = TestHelper.CreateServerUnit(scene, UnitType.Monster, campId: 2);
            Unit questNpc = CreateQuestNpcUnit(scene, QuestNpcUnitId, 1);
            Unit ecaUnit = CreateEcaUnit(scene, "click_unit_test_point", new float3(20f, 0f, 0f), 0.5f);
            if (player == null || enemy == null || questNpc == null || ecaUnit == null)
            {
                Log.Console(
                    $"server units invalid: playerNull={player == null}, enemyNull={enemy == null}, questNpcNull={questNpc == null}, ecaNull={ecaUnit == null}");
                return 1;
            }

            player.Position = float3.zero;
            player.AddComponent<QuestComponent>();
            EntityRef<Unit> playerRef = player;
            long enemyUnitId = enemy.Id;
            long questNpcUnitId = questNpc.Id;
            long ecaTargetUnitId = ecaUnit.Id;

            TargetComponent targetComponent = player.GetComponent<TargetComponent>() ?? player.AddComponent<TargetComponent>();
            EntityRef<TargetComponent> targetComponentRef = targetComponent;

            C2M_ClickUnitRequestHandler handler = new C2M_ClickUnitRequestHandler();
            MethodInfo runMethod = typeof(C2M_ClickUnitRequestHandler).GetMethod("Run", BindingFlags.Instance | BindingFlags.NonPublic);
            if (runMethod == null)
            {
                Log.Console("Run method not found on C2M_ClickUnitRequestHandler");
                return 2;
            }

            M2C_ClickUnitResponse enemyResponse = await InvokeClickAsync(player, handler, runMethod, enemyUnitId);
            if (enemyResponse == null)
            {
                Log.Console("enemy click response is null");
                return 3;
            }

            player = playerRef;
            targetComponent = targetComponentRef;
            if (player == null || targetComponent == null)
            {
                Log.Console("player or target component is null after enemy click");
                return 4;
            }

            if (targetComponent.Unit != null)
            {
                Log.Console($"enemy click should not set target component, actualTargetId={targetComponent.Unit.Id}");
                return 5;
            }

            if (enemyResponse.Error != ErrorCode.ERR_Success)
            {
                Log.Console($"enemy click should keep success response, actualError={enemyResponse.Error}");
                return 6;
            }

            if (enemyResponse.questInfo.Count != 0)
            {
                Log.Console($"enemy click should not return quest info, actualCount={enemyResponse.questInfo.Count}");
                return 7;
            }

            M2C_ClickUnitResponse questResponse = await InvokeClickAsync(player, handler, runMethod, questNpcUnitId);
            if (questResponse == null)
            {
                Log.Console("quest npc click response is null");
                return 8;
            }

            player = playerRef;
            targetComponent = targetComponentRef;
            if (player == null || targetComponent == null)
            {
                Log.Console("player or target component is null after quest npc click");
                return 9;
            }

            if (targetComponent.Unit != null)
            {
                Log.Console($"quest npc click should not set target component, actualTargetId={targetComponent.Unit.Id}");
                return 10;
            }

            if (questResponse.Error != ErrorCode.ERR_Success)
            {
                Log.Console($"quest npc click should keep success response, actualError={questResponse.Error}");
                return 11;
            }

            if (questResponse.questInfo.Count != 0)
            {
                Log.Console($"quest npc click should not return quest info, actualCount={questResponse.questInfo.Count}");
                return 12;
            }

            M2C_ClickUnitResponse ecaResponse = await InvokeClickAsync(player, handler, runMethod, ecaTargetUnitId);
            if (ecaResponse == null)
            {
                Log.Console("eca click response is null");
                return 13;
            }

            player = playerRef;
            targetComponent = targetComponentRef;
            if (player == null || targetComponent == null)
            {
                Log.Console("player or target component is null after eca click");
                return 14;
            }

            if (targetComponent.Unit != null)
            {
                Log.Console($"eca click should not set target component, actualTargetId={targetComponent.Unit.Id}");
                return 15;
            }

            if (ecaResponse.Error != ErrorCode.ERR_Success)
            {
                Log.Console($"eca click should be no-op and keep success response, actualError={ecaResponse.Error}");
                return 16;
            }

            if (ecaResponse.questInfo.Count != 0)
            {
                Log.Console($"eca click should not return quest info, actualCount={ecaResponse.questInfo.Count}");
                return 17;
            }

            Log.Console("Test_ClickUnit_Enemy_NoLock_Test PASSED");
            return ErrorCode.ERR_Success;
        }

        private static async ETTask<M2C_ClickUnitResponse> InvokeClickAsync(
            Unit player,
            C2M_ClickUnitRequestHandler handler,
            MethodInfo runMethod,
            long targetUnitId)
        {
            TargetComponent targetComponent = player.GetComponent<TargetComponent>();
            if (targetComponent != null)
            {
                targetComponent.Unit = null;
            }

            C2M_ClickUnitRequest request = C2M_ClickUnitRequest.Create();
            request.UnitId = targetUnitId;
            M2C_ClickUnitResponse response = M2C_ClickUnitResponse.Create();

            object invokeResult = runMethod.Invoke(handler, new object[] { player, request, response });
            if (invokeResult is not ETTask task)
            {
                Log.Console($"Run invoke result invalid: {invokeResult?.GetType().FullName ?? "null"}");
                return null;
            }

            await task;
            return response;
        }

        private static Unit CreateQuestNpcUnit(Scene scene, long unitId, int campId)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            UnitConfig unitConfig = TestHelper.FindUnitConfig(UnitType.Player);
            if (unitConfig == null)
            {
                return null;
            }

            Unit unit = unitComponent.AddChildWithId<Unit, int>(unitId, unitConfig.Id);
            unit.UnitType = UnitType.Player;

            NumericComponent numeric = unit.AddComponent<NumericComponent>();
            foreach ((int numericType, long numericValue) in unitConfig.KV)
            {
                numeric.SetNoEvent(numericType, numericValue);
            }

            if (campId > 0)
            {
                unit.AddComponent<CampComponent, int>(campId);
            }

            return unit;
        }

        private static Unit CreateEcaUnit(Scene scene, string pointId, float3 position, float interactRange)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            Unit unit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), 0);
            unit.Position = position;
            unit.AddComponent<ECAPointComponent, string, int, float>(pointId, ECAPointType.RangeTrigger, interactRange);
            return unit;
        }
    }
}
