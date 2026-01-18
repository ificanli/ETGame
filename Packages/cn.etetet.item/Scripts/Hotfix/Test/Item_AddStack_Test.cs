using ET.Client;
using ET.Server;

namespace ET.Test
{
    public class Item_AddStack_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Item_AddStack_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, "Client");
            Scene clientScene = robot.Root;
            
            Unit unit = TestHelper.GetServerUnit(testFiber, robot);
            
            Server.ItemComponent serverItem = unit.GetComponent<Server.ItemComponent>();
            Client.ItemComponent clientItem = clientScene.GetComponent<Client.ItemComponent>();
            EntityRef<Server.ItemComponent> serverItemRef = serverItem;
            EntityRef<Client.ItemComponent> clientItemRef = clientItem;
            
            ItemHelper.AddItem(serverItem, 10001, 120, ItemChangeReason.QuestReward);
            
            if (serverItem.GetItemCount(10001) != 120)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            if (serverItem.GetUsedSlotCount() != 2)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            EntityRef<Scene> clientSceneRef = clientScene;
            
            // 预计收到两个M2C_UpdateItem的消息
            await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_UpdateItem>();
            clientScene = clientSceneRef;
            await clientScene.GetComponent<ObjectWait>().Wait<Wait_M2C_UpdateItem>();
            serverItem = serverItemRef;
            clientItem = clientItemRef;

            if (clientItem.GetItemCount(10001) != 120)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            if (clientItem.GetUsedSlotCount() != 2)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            int c1 = -1;
            int c2 = -1;
            int found = 0;
            foreach (var kv in clientItem.Children)
            {
                Client.Item it = kv.Value as Client.Item;
                if (it.ConfigId == 10001)
                {
                    if (found == 0)
                    {
                        c1 = it.Count;
                        found = 1;
                    }
                    else if (found == 1)
                    {
                        c2 = it.Count;
                        found = 2;
                    }
                }
            }

            if (found != 2)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            bool countsOk = (c1 == 99 && c2 == 21) || (c1 == 21 && c2 == 99);
            if (!countsOk)
            {
                return ErrorCode.ERR_ItemAddFailed;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
