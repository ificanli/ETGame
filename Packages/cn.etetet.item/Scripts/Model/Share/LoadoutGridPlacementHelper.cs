using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 通用二维格子摆放校验工具。
    /// 当前优先服务于局外起装与跑局结算回写，不依赖具体玩法包。
    /// </summary>
    public static class LoadoutGridPlacementHelper
    {
        public const int DEFAULT_GRID_WIDTH = 1;
        public const int DEFAULT_GRID_HEIGHT = 1;

        public static int NormalizeGridWidth(int gridWidth)
        {
            return gridWidth > 0 ? gridWidth : DEFAULT_GRID_WIDTH;
        }

        public static int NormalizeGridHeight(int gridHeight)
        {
            return gridHeight > 0 ? gridHeight : DEFAULT_GRID_HEIGHT;
        }

        public static bool IsContainerSizeValid(int width, int height)
        {
            return width >= 0 && height >= 0;
        }

        public static bool ArePlacementsValid(IList<GridPlacementItemInfo> items, int containerWidth, int containerHeight)
        {
            if (!IsContainerSizeValid(containerWidth, containerHeight))
            {
                return false;
            }

            if (items == null || items.Count == 0)
            {
                return true;
            }

            bool[] occupied = new bool[containerWidth * containerHeight];
            for (int i = 0; i < items.Count; ++i)
            {
                GridPlacementItemInfo item = items[i];
                if (!TryMarkOccupied(occupied, containerWidth, containerHeight, item))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CanResize(IList<GridPlacementItemInfo> items, int newWidth, int newHeight)
        {
            return ArePlacementsValid(items, newWidth, newHeight);
        }

        public static bool CanPlaceAtAnchorSlot(
            IList<GridPlacementItemInfo> items,
            int containerWidth,
            int containerHeight,
            int anchorSlotIndex,
            int itemGridWidth,
            int itemGridHeight)
        {
            if (containerWidth <= 0 || containerHeight <= 0)
            {
                return false;
            }

            int normalizedGridWidth = NormalizeGridWidth(itemGridWidth);
            int normalizedGridHeight = NormalizeGridHeight(itemGridHeight);
            if (normalizedGridWidth > containerWidth || normalizedGridHeight > containerHeight)
            {
                return false;
            }

            int capacity = containerWidth * containerHeight;
            if (anchorSlotIndex < 0 || anchorSlotIndex >= capacity)
            {
                return false;
            }

            List<GridPlacementItemInfo> placements = new(items?.Count + 1 ?? 1);
            if (items != null)
            {
                for (int i = 0; i < items.Count; ++i)
                {
                    placements.Add(items[i]);
                }
            }

            placements.Add(new GridPlacementItemInfo
            {
                AnchorSlotIndex = anchorSlotIndex,
                GridWidth = normalizedGridWidth,
                GridHeight = normalizedGridHeight,
            });

            return ArePlacementsValid(placements, containerWidth, containerHeight);
        }

        public static bool TryFindFirstFitAnchorSlot(
            IList<GridPlacementItemInfo> items,
            int containerWidth,
            int containerHeight,
            int itemGridWidth,
            int itemGridHeight,
            out int anchorSlotIndex)
        {
            anchorSlotIndex = -1;
            if (containerWidth <= 0 || containerHeight <= 0)
            {
                return false;
            }

            int normalizedGridWidth = NormalizeGridWidth(itemGridWidth);
            int normalizedGridHeight = NormalizeGridHeight(itemGridHeight);
            if (normalizedGridWidth > containerWidth || normalizedGridHeight > containerHeight)
            {
                return false;
            }

            List<GridPlacementItemInfo> placements = new(items?.Count + 1 ?? 1);
            if (items != null)
            {
                for (int i = 0; i < items.Count; ++i)
                {
                    placements.Add(items[i]);
                }
            }

            int capacity = containerWidth * containerHeight;
            for (int i = 0; i < capacity; ++i)
            {
                placements.Add(new GridPlacementItemInfo
                {
                    AnchorSlotIndex = i,
                    GridWidth = normalizedGridWidth,
                    GridHeight = normalizedGridHeight,
                });

                if (ArePlacementsValid(placements, containerWidth, containerHeight))
                {
                    anchorSlotIndex = i;
                    return true;
                }

                placements.RemoveAt(placements.Count - 1);
            }

            return false;
        }

        private static bool TryMarkOccupied(bool[] occupied, int containerWidth, int containerHeight, GridPlacementItemInfo item)
        {
            if (containerWidth <= 0 || containerHeight <= 0)
            {
                return false;
            }

            int gridWidth = NormalizeGridWidth(item.GridWidth);
            int gridHeight = NormalizeGridHeight(item.GridHeight);
            if (item.AnchorSlotIndex < 0)
            {
                return false;
            }

            int anchorX = item.AnchorSlotIndex % containerWidth;
            int anchorY = item.AnchorSlotIndex / containerWidth;
            if (anchorX < 0 || anchorY < 0)
            {
                return false;
            }

            if (anchorX + gridWidth > containerWidth || anchorY + gridHeight > containerHeight)
            {
                return false;
            }

            for (int yy = anchorY; yy < anchorY + gridHeight; ++yy)
            {
                int rowOffset = yy * containerWidth;
                for (int xx = anchorX; xx < anchorX + gridWidth; ++xx)
                {
                    int slotIndex = rowOffset + xx;
                    if (slotIndex < 0 || slotIndex >= occupied.Length)
                    {
                        return false;
                    }

                    if (occupied[slotIndex])
                    {
                        return false;
                    }

                    occupied[slotIndex] = true;
                }
            }

            return true;
        }
    }
}
