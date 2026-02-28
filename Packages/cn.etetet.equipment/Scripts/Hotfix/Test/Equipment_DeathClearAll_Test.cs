using ET.Client;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证玩家死亡后背包和装备槽全部清空，并收到死亡结算消息
    /// TDD: 此测试在步骤9（UnitDieEvent_ClearLoadout）实现之前会失败
    /// </summary>
    public class Equipment_DeathClearAll_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_DeathClearAll_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Equipment_DeathClearAll_Test));
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
            EntityRef<Server.ItemComponent> serverItemRef = serverItem;
            if (serverItem == null)
            {
                Log.Console($"ItemComponent is null");
                return 2;
            }

            ItemHelper.AddItem(serverItem, 10001, 3, ItemChangeReason.QuestReward);

            // 等待 M2C_UpdateItem 通知
            clientScene = clientSceneRef;
            await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_UpdateItem>();
            clientScene = clientSceneRef;
            unit = unitRef;
            serverItem = serverItemRef;

            // 给 Unit 装备槽添加装备
            Server.EquipmentComponent equipComp = unit.GetComponent<Server.EquipmentComponent>();
            EntityRef<Server.EquipmentComponent> equipCompRef = equipComp;
            if (equipComp != null)
            {
                Server.Item weapon = equipComp.AddChild<Server.Item>();
                weapon.ConfigId = 10001;
                weapon.Count = 1;
                equipComp.EquipItem(weapon, EquipmentSlotType.MainHand);
            }

            // 触发死亡事件（模拟 Unit 死亡）
            Scene mapScene = unit.Scene();
            EventSystem.Instance.Publish(mapScene, new UnitDie { Unit = unit, Target = unit });

            // 等待死亡结算消息
            clientScene = clientSceneRef;
            Wait_M2C_DeathSettlement waitResult = await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_DeathSettlement>();
            clientScene = clientSceneRef;
            unit = unitRef;
            serverItem = serverItemRef;
            equipComp = equipCompRef;

            if (waitResult.M2C_DeathSettlement == null)
            {
                Log.Console($"M2C_DeathSettlement is null");
                return 3;
            }

            // 验证背包已清空
            if (serverItem.GetItemCount(10001) != 0)
            {
                Log.Console($"bag should be empty after death, but has {serverItem.GetItemCount(10001)} items");
                return 4;
            }

            // 验证装备槽已清空
            if (equipComp != null && equipComp.GetEquippedItemCount() != 0)
            {
                Log.Console($"equipment slots should be empty after death, but has {equipComp.GetEquippedItemCount()} items");
                return 5;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
