using ET.Server;

namespace ET.Test
{
    public class Ecanode_ContainerMoveToBagPlacement_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Ecanode_ContainerMoveToBagPlacement_Test));
            Scene scene = scope.TestFiber.Root;
            scene.AddComponent<CoroutineLockComponent>();

            UnitComponent unitComponent = scene.AddComponent<UnitComponent>();

            Unit pointUnit = unitComponent.AddChild<Unit, int>(0);
            ECAPointComponent point = pointUnit.AddComponent<ECAPointComponent, string, int, float>(
                "test_container_move_to_bag_placement",
                ECAPointType.Container,
                3f);
            ContainerComponent container = ContainerComponentSystem.GetOrAdd(point);
            container.State = ContainerState.Opened;

            Unit player = unitComponent.AddChild<Unit, int>(0);
            player.UnitType = UnitType.Player;
            ItemComponent itemComponent = player.AddComponent<ItemComponent>();
            itemComponent.SetSize(3, 3);
            EntityRef<Unit> playerRef = player;
            EntityRef<ECAPointComponent> pointRef = point;
            EntityRef<ContainerComponent> containerRef = container;
            EntityRef<ItemComponent> itemComponentRef = itemComponent;

            container.SetItem(0, 10012, 1);
            int invalidMoveError = await ContainerRuntimeHelper.MoveItem(
                player,
                point,
                (int)ContainerItemAreaType.Container,
                0,
                0,
                (int)ContainerItemAreaType.Bag,
                8);
            player = playerRef;
            point = pointRef;
            container = containerRef;
            itemComponent = itemComponentRef;
            if (player == null || point == null || container == null || itemComponent == null)
            {
                Log.Console("entities disposed after invalid container->bag move");
                return 1;
            }

            if (invalidMoveError != ErrorCode.ERR_ECAContainerBagFull)
            {
                Log.Console($"expected invalid container->bag anchor error, actual={invalidMoveError}");
                return 2;
            }

            if (!container.TryGetItem(0, out ContainerItemEntry remainedSourceItem) ||
                remainedSourceItem.ConfigId != 10012 ||
                remainedSourceItem.Count != 1)
            {
                Log.Console("invalid container->bag move should keep source item inside container");
                return 3;
            }

            if (FindItemByConfig(itemComponent, 22012) != null || itemComponent.GetItemBySlot(8) != null)
            {
                Log.Console("invalid container->bag move should not create bag item");
                return 4;
            }

            itemComponent.Clear();
            itemComponent.SetSize(3, 3);
            container.ClearItems();
            container.State = ContainerState.Opened;
            container.SetItem(0, 10012, 1);

            int validMoveError = await ContainerRuntimeHelper.MoveItem(
                player,
                point,
                (int)ContainerItemAreaType.Container,
                0,
                0,
                (int)ContainerItemAreaType.Bag,
                0);
            player = playerRef;
            point = pointRef;
            container = containerRef;
            itemComponent = itemComponentRef;
            if (player == null || point == null || container == null || itemComponent == null)
            {
                Log.Console("entities disposed after valid container->bag move");
                return 5;
            }

            if (validMoveError != ErrorCode.ERR_Success)
            {
                Log.Console($"expected valid container->bag move success, actual={validMoveError}");
                return 6;
            }

            if (container.HasAnyItem())
            {
                Log.Console("valid container->bag move should empty the container");
                return 7;
            }

            Item movedBagItem = itemComponent.GetItemBySlot(0);
            if (movedBagItem == null ||
                movedBagItem.ConfigId != 22012 ||
                movedBagItem.Count != 1 ||
                movedBagItem.GridWidth != 2 ||
                movedBagItem.GridHeight != 3)
            {
                Log.Console("valid container->bag move should normalize config id and write real footprint");
                return 8;
            }

            itemComponent.Clear();
            itemComponent.SetSize(3, 3);
            container.ClearItems();
            container.State = ContainerState.Opened;
            Item sourceBagItem = AddBagAnchorItem(itemComponent, 8, 22001, 1, 1, 1);
            EntityRef<Item> sourceBagItemRef = sourceBagItem;
            container.SetItem(0, 10012, 1);

            int invalidSwapError = await ContainerRuntimeHelper.MoveItem(
                player,
                point,
                (int)ContainerItemAreaType.Bag,
                8,
                sourceBagItem.Id,
                (int)ContainerItemAreaType.Container,
                0);
            player = playerRef;
            point = pointRef;
            container = containerRef;
            itemComponent = itemComponentRef;
            sourceBagItem = sourceBagItemRef;
            if (player == null || point == null || container == null || itemComponent == null || sourceBagItem == null)
            {
                Log.Console("entities disposed after invalid bag->container swap");
                return 9;
            }

            if (invalidSwapError != ErrorCode.ERR_ECAContainerBagFull)
            {
                Log.Console($"expected invalid bag->container swap error, actual={invalidSwapError}");
                return 10;
            }

            if (sourceBagItem.IsDisposed || sourceBagItem.SlotIndex != 8 || itemComponent.GetItemBySlot(8) != sourceBagItem)
            {
                Log.Console("invalid bag->container swap should keep source bag item in place");
                return 11;
            }

            if (!container.TryGetItem(0, out ContainerItemEntry remainedTargetItem) ||
                remainedTargetItem.ConfigId != 10012 ||
                remainedTargetItem.Count != 1)
            {
                Log.Console("invalid bag->container swap should keep target container item unchanged");
                return 12;
            }

            Log.Console("Ecanode_ContainerMoveToBagPlacement_Test passed");
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

        private static Item FindItemByConfig(ItemComponent itemComponent, int configId)
        {
            foreach (var kv in itemComponent.Children)
            {
                if (kv.Value is Item item && item.ConfigId == configId)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
