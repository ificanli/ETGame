namespace ET.Test
{
    /// <summary>
    /// TDD: 验证双击快速转移的默认目标路由。
    /// </summary>
    public class Statesync_QuickTransferRoute_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.CreateOneFiber(
                context.Fiber, SceneType.TestEmpty, nameof(Statesync_QuickTransferRoute_Test));

            if (!QuickTransferRouteHelper.TryResolveSearchPanelTarget(
                    SearchPanelOpenMode.ContainerSearch,
                    ContainerItemAreaType.Container,
                    out ContainerItemAreaType searchTarget) ||
                searchTarget != ContainerItemAreaType.Bag)
            {
                Log.Console($"container search route failed: target={searchTarget}");
                return 1;
            }

            if (!QuickTransferRouteHelper.TryResolveSearchPanelTarget(
                    SearchPanelOpenMode.CorpseLoot,
                    ContainerItemAreaType.Bag,
                    out searchTarget) ||
                searchTarget != ContainerItemAreaType.Container)
            {
                Log.Console($"corpse loot route failed: target={searchTarget}");
                return 2;
            }

            if (QuickTransferRouteHelper.TryResolveSearchPanelTarget(
                    SearchPanelOpenMode.BackpackInspect,
                    ContainerItemAreaType.Bag,
                    out _))
            {
                Log.Console("backpack inspect should not resolve search quick transfer");
                return 3;
            }

            if (QuickTransferRouteHelper.TryResolveSearchPanelTarget(
                    SearchPanelOpenMode.ContainerSearch,
                    ContainerItemAreaType.Secure,
                    out _))
            {
                Log.Console("secure area should not resolve search quick transfer");
                return 4;
            }

            if (!QuickTransferRouteHelper.TryResolveLoadoutQuickTransfer(
                    true,
                    LoadoutAreaType.None,
                    LoadoutFixedSlotType.None,
                    out bool targetIsWarehouse,
                    out LoadoutAreaType loadoutTargetArea) ||
                targetIsWarehouse ||
                loadoutTargetArea != LoadoutAreaType.Bag)
            {
                Log.Console($"warehouse route failed: targetIsWarehouse={targetIsWarehouse}, area={loadoutTargetArea}");
                return 5;
            }

            if (!QuickTransferRouteHelper.TryResolveLoadoutQuickTransfer(
                    false,
                    LoadoutAreaType.FixedSlot,
                    LoadoutFixedSlotType.MainWeapon,
                    out targetIsWarehouse,
                    out loadoutTargetArea) ||
                !targetIsWarehouse ||
                loadoutTargetArea != LoadoutAreaType.None)
            {
                Log.Console($"fixed slot route failed: targetIsWarehouse={targetIsWarehouse}, area={loadoutTargetArea}");
                return 6;
            }

            if (!QuickTransferRouteHelper.TryResolveLoadoutQuickTransfer(
                    false,
                    LoadoutAreaType.Bag,
                    LoadoutFixedSlotType.None,
                    out targetIsWarehouse,
                    out loadoutTargetArea) ||
                !targetIsWarehouse)
            {
                Log.Console($"bag route failed: targetIsWarehouse={targetIsWarehouse}, area={loadoutTargetArea}");
                return 7;
            }

            if (!QuickTransferRouteHelper.TryResolveLoadoutQuickTransfer(
                    false,
                    LoadoutAreaType.Secure,
                    LoadoutFixedSlotType.None,
                    out targetIsWarehouse,
                    out loadoutTargetArea) ||
                !targetIsWarehouse)
            {
                Log.Console($"secure route failed: targetIsWarehouse={targetIsWarehouse}, area={loadoutTargetArea}");
                return 8;
            }

            if (QuickTransferRouteHelper.TryResolveLoadoutQuickTransfer(
                    false,
                    LoadoutAreaType.None,
                    LoadoutFixedSlotType.None,
                    out _,
                    out _))
            {
                Log.Console("none route should not resolve");
                return 9;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
