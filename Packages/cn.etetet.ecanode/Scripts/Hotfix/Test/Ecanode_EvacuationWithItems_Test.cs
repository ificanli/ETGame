using ET.Client;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证撤离时正确收集物品并发送结算消息
    /// TDD: 此测试在步骤8（修改 PlayerEvacuationComponentSystem）之前会失败
    /// </summary>
    public class Ecanode_EvacuationWithItems_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_EvacuationWithItems_Test));
            Fiber testFiber = scope.TestFiber;

            // 创建机器人并进入地图
            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Ecanode_EvacuationWithItems_Test));
            Scene clientScene = robot.Root;
            EntityRef<Scene> clientSceneRef = clientScene;

            Unit unit = TestHelper.GetServerUnit(testFiber, robot);
            if (unit == null)
            {
                Log.Console($"server unit is null");
                return 1;
            }
            EntityRef<Unit> unitRef = unit;

            // 服务端为 Unit 背包添加测试物品（ConfigId=10001, Count=5）
            Server.ItemComponent serverItem = unit.GetComponent<Server.ItemComponent>();
            if (serverItem == null)
            {
                Log.Console($"ItemComponent is null on unit");
                return 2;
            }

            ItemHelper.AddItem(serverItem, 10001, 5, ItemChangeReason.QuestReward);

            if (serverItem.GetItemCount(10001) != 5)
            {
                Log.Console($"expected 5 items, got {serverItem.GetItemCount(10001)}");
                return 3;
            }

            // 等待 M2C_UpdateItem 通知完成
            clientScene = clientSceneRef;
            await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_UpdateItem>();
            clientScene = clientSceneRef;

            // 直接调用撤离结算（绕过ECA计时器以加速测试）
            unit = unitRef;
            await EvacuationSettlementHelper.Settle(unit);

            // 等待撤离结算消息
            clientScene = clientSceneRef;
            Wait_M2C_EvacuationSettlement waitResult = await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_EvacuationSettlement>();
            clientScene = clientSceneRef;

            M2C_EvacuationSettlement settlement = waitResult.M2C_EvacuationSettlement;
            if (settlement == null)
            {
                Log.Console($"M2C_EvacuationSettlement is null");
                return 4;
            }

            if (!settlement.Success)
            {
                Log.Console($"settlement.Success is false");
                return 5;
            }

            if (settlement.Items == null || settlement.Items.Count == 0)
            {
                Log.Console($"settlement.Items is empty, expected at least 1 item");
                return 6;
            }

            // 验证结算消息中包含正确的物品
            bool found10001 = false;
            foreach (ItemData itemData in settlement.Items)
            {
                if (itemData.ConfigId == 10001 && itemData.Count == 5)
                {
                    found10001 = true;
                    break;
                }
            }

            if (!found10001)
            {
                Log.Console($"settlement does not contain ConfigId=10001 Count=5");
                return 7;
            }

            if (settlement.TotalWealth <= 0)
            {
                Log.Console($"TotalWealth should be > 0, got {settlement.TotalWealth}");
                return 8;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
