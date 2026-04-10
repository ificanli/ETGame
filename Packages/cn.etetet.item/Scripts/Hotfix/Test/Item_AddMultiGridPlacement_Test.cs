using ET.Client;
using ET.Server;

namespace ET.Test
{
    public class Item_AddMultiGridPlacement_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Item_AddMultiGridPlacement_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, "Client");
            Scene clientScene = robot.Root;

            Unit unit = TestHelper.GetServerUnit(testFiber, robot);

            Server.ItemComponent serverItem = unit.GetComponent<Server.ItemComponent>();
            Client.ItemComponent clientItem = clientScene.GetComponent<Client.ItemComponent>();
            EntityRef<Server.ItemComponent> serverItemRef = serverItem;
            EntityRef<Client.ItemComponent> clientItemRef = clientItem;

            serverItem.Clear();
            clientItem.Clear();
            serverItem.SetSize(3, 3);
            clientItem.SetSize(3, 3);

            AddBagAnchorItem(serverItem, 0, 10001, 1, 1, 1);
            AddBagAnchorItem(serverItem, 1, 10001, 1, 1, 1);

            ItemHelper.AddItem(serverItem, 22005, 1, ItemChangeReason.QuestReward);

            serverItem = serverItemRef;
            if (serverItem == null)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            Server.Item serverAddedItem = FindItemByConfig(serverItem, 22005);
            if (serverAddedItem == null || serverAddedItem.SlotIndex != 3)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            if (serverItem.GetItemBySlot(2) != null)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            EntityRef<Scene> clientSceneRef = clientScene;
            await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_UpdateItem>();

            clientScene = clientSceneRef;
            clientItem = clientItemRef;
            if (clientScene == null || clientItem == null)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            Client.Item clientAddedItem = FindItemByConfig(clientItem, 22005);
            if (clientAddedItem == null || clientAddedItem.SlotIndex != 3)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            if (clientItem.GetItemBySlot(2) != null)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            return ErrorCode.ERR_Success;
        }

        private static void AddBagAnchorItem(Server.ItemComponent itemComponent, int slotIndex, int configId, int count, int gridWidth, int gridHeight)
        {
            Server.Item item = itemComponent.AddChild<Server.Item>();
            item.ConfigId = configId;
            item.Count = count;
            item.GridWidth = gridWidth;
            item.GridHeight = gridHeight;
            itemComponent.SetSlotItem(slotIndex, item);
        }

        private static Server.Item FindItemByConfig(Server.ItemComponent itemComponent, int configId)
        {
            foreach (var kv in itemComponent.Children)
            {
                if (kv.Value is Server.Item item && item.ConfigId == configId)
                {
                    return item;
                }
            }

            return null;
        }

        private static Client.Item FindItemByConfig(Client.ItemComponent itemComponent, int configId)
        {
            foreach (var kv in itemComponent.Children)
            {
                if (kv.Value is Client.Item item && item.ConfigId == configId)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
