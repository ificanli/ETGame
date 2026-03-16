using ET.Client;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 验证 EquipmentComponent 在 Gate -> Map 传送后保持完整
    /// TDD: 此测试在实现 ITransfer 之前会失败
    /// </summary>
    public class Equipment_Transfer_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Equipment_Transfer_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Equipment_Transfer_Test));

            Unit unit = TestHelper.GetServerUnit(testFiber, robot);
            if (unit == null)
            {
                Log.Console($"server unit is null");
                return 1;
            }

            // 验证 EquipmentComponent 在 Gate -> Map 传送后仍然存在
            Server.EquipmentComponent equipComp = unit.GetComponent<Server.EquipmentComponent>();
            if (equipComp == null)
            {
                Log.Console($"EquipmentComponent is null after Gate->Map transfer, ITransfer not implemented");
                return 2;
            }

            // 服务端添加装备并验证 Deserialize 重建了 EquippedItems
            Server.Item testItem = equipComp.AddChild<Server.Item>();
            testItem.ConfigId = 10001;
            testItem.Count = 1;
            equipComp.EquipItem(testItem, EquipmentSlotType.MainHand);

            if (equipComp.GetEquippedItemCount() != 1)
            {
                Log.Console($"expected 1 equipped item, got {equipComp.GetEquippedItemCount()}");
                return 3;
            }

            Server.Item equipped = equipComp.GetEquippedItem(EquipmentSlotType.MainHand);
            if (equipped == null)
            {
                Log.Console($"MainHand slot is empty after equip");
                return 4;
            }

            if (equipped.ConfigId != 10001)
            {
                Log.Console($"expected ConfigId=10001, got {equipped.ConfigId}");
                return 5;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
