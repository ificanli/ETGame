namespace ET
{
    /// <summary>
    /// 双击快速转移的纯路由规则。
    /// 只负责决定默认目标方向，不负责找位与实际请求。
    /// </summary>
    public static class QuickTransferRouteHelper
    {
        public static bool TryResolveSearchPanelTarget(
            SearchPanelOpenMode openMode,
            ContainerItemAreaType sourceAreaType,
            out ContainerItemAreaType targetAreaType)
        {
            targetAreaType = ContainerItemAreaType.None;
            if (openMode != SearchPanelOpenMode.ContainerSearch &&
                openMode != SearchPanelOpenMode.CorpseLoot)
            {
                return false;
            }

            targetAreaType = sourceAreaType switch
            {
                ContainerItemAreaType.Container => ContainerItemAreaType.Bag,
                ContainerItemAreaType.Bag => ContainerItemAreaType.Container,
                _ => ContainerItemAreaType.None,
            };
            return targetAreaType != ContainerItemAreaType.None;
        }

        public static bool TryResolveLoadoutQuickTransfer(
            bool isWarehouse,
            LoadoutAreaType sourceAreaType,
            LoadoutFixedSlotType fixedSlotType,
            out bool targetIsWarehouse,
            out LoadoutAreaType targetAreaType)
        {
            targetIsWarehouse = false;
            targetAreaType = LoadoutAreaType.None;

            if (isWarehouse)
            {
                targetAreaType = LoadoutAreaType.Bag;
                return true;
            }

            switch (sourceAreaType)
            {
                case LoadoutAreaType.FixedSlot when fixedSlotType != LoadoutFixedSlotType.None:
                    targetIsWarehouse = true;
                    return true;
                case LoadoutAreaType.Bag:
                case LoadoutAreaType.Secure:
                    targetIsWarehouse = true;
                    return true;
                default:
                    return false;
            }
        }
    }
}
