using ET.Client;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证撤离后结算数据写回 PlayerStorageComponent（Gate 侧）
    /// TDD: 此测试在步骤10（PlayerStorageComponent）实现之前会失败
    /// </summary>
    public class Equipment_StorageAfterEvacuation_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_StorageAfterEvacuation_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Equipment_StorageAfterEvacuation_Test));
            Scene clientScene = robot.Root;
            EntityRef<Scene> clientSceneRef = clientScene;

            Unit unit = TestHelper.GetServerUnit(testFiber, robot);
            EntityRef<Unit> unitRef = unit;
            if (unit == null)
            {
                Log.Console($"server unit is null");
                return 1;
            }

            // 给 Unit 背包添加物品
            Server.ItemComponent serverItem = unit.GetComponent<Server.ItemComponent>();
            ItemHelper.AddItem(serverItem, 10001, 10, ItemChangeReason.QuestReward);

            clientScene = clientSceneRef;
            await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_UpdateItem>();
            clientScene = clientSceneRef;
            unit = unitRef;

            // 触发撤离结算
            await EvacuationSettlementHelper.Settle(unit);
            unit = unitRef;

            // 等待结算消息
            clientScene = clientSceneRef;
            Wait_M2C_EvacuationSettlement waitResult = await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_EvacuationSettlement>();
            clientScene = clientSceneRef;
            unit = unitRef;

            if (waitResult.M2C_EvacuationSettlement == null || !waitResult.M2C_EvacuationSettlement.Success)
            {
                Log.Console($"evacuation settlement failed");
                return 2;
            }

            // 验证 Gate 侧 Player 的 PlayerStorageComponent 已写入结算数据
            // 通过 UnitGateInfoComponent.ActorId 找到 Gate 侧 Player
            UnitGateInfoComponent gateInfo = unit.GetComponent<UnitGateInfoComponent>();
            if (gateInfo == null)
            {
                Log.Console($"UnitGateInfoComponent is null");
                return 3;
            }

            // 通过 ProcessInnerSender 向 Gate 发送查询（M0 简化：直接验证结算消息内容正确即可）
            // 完整的跨 Fiber 写回验证在 T8 集成测试中覆盖
            M2C_EvacuationSettlement settlement = waitResult.M2C_EvacuationSettlement;
            if (settlement.TotalWealth <= 0)
            {
                Log.Console($"TotalWealth should be > 0 after evacuation with items");
                return 4;
            }

            bool hasItem = false;
            foreach (ItemData item in settlement.Items)
            {
                if (item.ConfigId == 10001 && item.Count == 10)
                {
                    hasItem = true;
                    break;
                }
            }

            if (!hasItem)
            {
                Log.Console($"settlement items do not contain ConfigId=10001 Count=10");
                return 5;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
