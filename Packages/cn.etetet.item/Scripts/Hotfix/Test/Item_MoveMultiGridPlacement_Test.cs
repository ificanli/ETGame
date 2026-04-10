using ET.Server;

namespace ET.Test
{
    public class Item_MoveMultiGridPlacement_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Item_MoveMultiGridPlacement_Test));
            Scene scene = scope.TestFiber.Root;

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();
            Unit unit = unitComponent.AddChild<Unit, int>(0);
            ItemComponent itemComponent = unit.AddComponent<ItemComponent>();

            itemComponent.SetSize(3, 3);
            Item largeItem = AddBagAnchorItem(itemComponent, 0, 22012, 1, 2, 3);
            int invalidMoveError = ItemHelper.MoveItem(itemComponent, largeItem.Id, 8);
            if (invalidMoveError != ErrorCode.ERR_ItemSlotInvalid)
            {
                Log.Console($"expected invalid anchor move error, actual={invalidMoveError}");
                return 1;
            }

            if (largeItem.SlotIndex != 0 || itemComponent.GetItemBySlot(0) != largeItem || itemComponent.GetItemBySlot(8) != null)
            {
                Log.Console("invalid anchor move should keep multi-grid item at original slot");
                return 2;
            }

            itemComponent.Clear();
            itemComponent.SetSize(4, 4);
            Item largeSwapItem = AddBagAnchorItem(itemComponent, 0, 22005, 1, 2, 2);
            Item smallSwapItem = AddBagAnchorItem(itemComponent, 14, 22001, 1, 1, 1);
            int invalidSwapError = ItemHelper.MoveItem(itemComponent, smallSwapItem.Id, 0);
            if (invalidSwapError != ErrorCode.ERR_ItemSlotInvalid)
            {
                Log.Console($"expected invalid swap error, actual={invalidSwapError}");
                return 3;
            }

            if (largeSwapItem.SlotIndex != 0 ||
                smallSwapItem.SlotIndex != 14 ||
                itemComponent.GetItemBySlot(0) != largeSwapItem ||
                itemComponent.GetItemBySlot(14) != smallSwapItem)
            {
                Log.Console("invalid swap should keep both items at original anchors");
                return 4;
            }

            Log.Console("Item_MoveMultiGridPlacement_Test passed");
            return ErrorCode.ERR_Success;
        }

        private static Item AddBagAnchorItem(ItemComponent itemComponent, int slotIndex, int configId, int count, int gridWidth, int gridHeight)
        {
            Item item = itemComponent.AddChild<Item>();
            item.ConfigId = configId;
            item.Count = count;
            item.GridWidth = gridWidth;
            item.GridHeight = gridHeight;
            itemComponent.SetSlotItem(slotIndex, item);
            return item;
        }
    }
}
