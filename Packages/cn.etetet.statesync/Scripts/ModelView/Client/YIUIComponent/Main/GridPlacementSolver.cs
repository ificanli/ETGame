using System;
using System.Collections.Generic;

namespace ET.Client
{
    public enum GridDropResultType
    {
        Failed = 0,
        Placed = 1,
        Swapped = 2
    }

    public struct GridItemFootprint
    {
        public long ItemId;
        public int X;
        public int Y;
        public int Width;
        public int Height;
    }

    public struct GridDropResult
    {
        public GridDropResultType ResultType;
        public int PlaceX;
        public int PlaceY;
        public long SwapItemId;
        public int SwapItemX;
        public int SwapItemY;
    }

    /// <summary>
    /// 背包/容器网格占位与放置求解器。
    /// 规则：可放就放；单阻挡且互换可行则交换；其余失败。
    /// </summary>
    [EnableClass]
    public sealed class GridPlacementSolver
    {
        private readonly int cols;
        private readonly int rows;
        private readonly long[] cells;
        private readonly Dictionary<long, GridItemFootprint> itemMap = new();

        public int Cols => this.cols;
        public int Rows => this.rows;

        public GridPlacementSolver(int cols, int rows)
        {
            this.cols = cols;
            this.rows = rows;
            this.cells = new long[cols * rows];
        }

        public void Clear()
        {
            this.itemMap.Clear();
            Array.Clear(this.cells, 0, this.cells.Length);
        }

        public bool TryGetItem(long itemId, out GridItemFootprint footprint)
        {
            return this.itemMap.TryGetValue(itemId, out footprint);
        }

        public bool TryPlaceOrMove(GridItemFootprint footprint)
        {
            if (footprint.ItemId == 0 || footprint.Width <= 0 || footprint.Height <= 0)
            {
                return false;
            }

            if (!this.CanPlace(footprint.X, footprint.Y, footprint.Width, footprint.Height, footprint.ItemId))
            {
                return false;
            }

            if (this.itemMap.TryGetValue(footprint.ItemId, out GridItemFootprint old))
            {
                this.FillCells(old, 0);
            }

            this.itemMap[footprint.ItemId] = footprint;
            this.FillCells(footprint, footprint.ItemId);
            return true;
        }

        public bool Remove(long itemId)
        {
            if (!this.itemMap.TryGetValue(itemId, out GridItemFootprint old))
            {
                return false;
            }

            this.itemMap.Remove(itemId);
            this.FillCells(old, 0);
            return true;
        }

        public bool CanPlace(int x, int y, int width, int height, long ignoreItemId = 0, long ignoreItemId2 = 0)
        {
            if (x < 0 || y < 0 || width <= 0 || height <= 0 || x + width > this.cols || y + height > this.rows)
            {
                return false;
            }

            int maxY = y + height;
            int maxX = x + width;
            for (int yy = y; yy < maxY; ++yy)
            {
                int rowOffset = yy * this.cols;
                for (int xx = x; xx < maxX; ++xx)
                {
                    long occupied = this.cells[rowOffset + xx];
                    if (occupied == 0 || occupied == ignoreItemId || occupied == ignoreItemId2)
                    {
                        continue;
                    }

                    return false;
                }
            }

            return true;
        }

        public GridDropResult ResolveDrop(long movingItemId, int targetX, int targetY, bool allowSwap = true)
        {
            GridDropResult fail = new GridDropResult
            {
                ResultType = GridDropResultType.Failed,
                PlaceX = targetX,
                PlaceY = targetY
            };

            if (!this.itemMap.TryGetValue(movingItemId, out GridItemFootprint moving))
            {
                return fail;
            }

            if (this.CanPlace(targetX, targetY, moving.Width, moving.Height, movingItemId))
            {
                return new GridDropResult
                {
                    ResultType = GridDropResultType.Placed,
                    PlaceX = targetX,
                    PlaceY = targetY
                };
            }

            if (!allowSwap)
            {
                return fail;
            }

            HashSet<long> blockers = new();
            this.CollectBlockers(targetX, targetY, moving.Width, moving.Height, movingItemId, blockers);
            if (blockers.Count != 1)
            {
                return fail;
            }

            long blockerId = 0;
            foreach (long id in blockers)
            {
                blockerId = id;
                break;
            }

            if (!this.itemMap.TryGetValue(blockerId, out GridItemFootprint blocker))
            {
                return fail;
            }

            bool movingCanPlace = this.CanPlace(targetX, targetY, moving.Width, moving.Height, movingItemId, blockerId);
            if (!movingCanPlace)
            {
                return fail;
            }

            bool blockerCanPlace = this.CanPlace(moving.X, moving.Y, blocker.Width, blocker.Height, movingItemId, blockerId);
            if (!blockerCanPlace)
            {
                return fail;
            }

            return new GridDropResult
            {
                ResultType = GridDropResultType.Swapped,
                PlaceX = targetX,
                PlaceY = targetY,
                SwapItemId = blockerId,
                SwapItemX = moving.X,
                SwapItemY = moving.Y
            };
        }

        public GridDropResult ResolveDropNearest(long movingItemId, int anchorX, int anchorY, int maxRadius, bool allowSwap = true)
        {
            GridDropResult firstTry = this.ResolveDrop(movingItemId, anchorX, anchorY, allowSwap);
            if (firstTry.ResultType != GridDropResultType.Failed || maxRadius <= 0)
            {
                return firstTry;
            }

            for (int radius = 1; radius <= maxRadius; ++radius)
            {
                int left = anchorX - radius;
                int right = anchorX + radius;
                int top = anchorY - radius;
                int bottom = anchorY + radius;

                for (int x = left; x <= right; ++x)
                {
                    GridDropResult topResult = this.ResolveDrop(movingItemId, x, top, allowSwap);
                    if (topResult.ResultType != GridDropResultType.Failed)
                    {
                        return topResult;
                    }

                    GridDropResult bottomResult = this.ResolveDrop(movingItemId, x, bottom, allowSwap);
                    if (bottomResult.ResultType != GridDropResultType.Failed)
                    {
                        return bottomResult;
                    }
                }

                for (int y = top + 1; y <= bottom - 1; ++y)
                {
                    GridDropResult leftResult = this.ResolveDrop(movingItemId, left, y, allowSwap);
                    if (leftResult.ResultType != GridDropResultType.Failed)
                    {
                        return leftResult;
                    }

                    GridDropResult rightResult = this.ResolveDrop(movingItemId, right, y, allowSwap);
                    if (rightResult.ResultType != GridDropResultType.Failed)
                    {
                        return rightResult;
                    }
                }
            }

            return firstTry;
        }

        public bool ApplyDropResult(long movingItemId, GridDropResult result)
        {
            if (result.ResultType == GridDropResultType.Failed)
            {
                return false;
            }

            if (!this.itemMap.TryGetValue(movingItemId, out GridItemFootprint moving))
            {
                return false;
            }

            if (result.ResultType == GridDropResultType.Swapped)
            {
                if (!this.itemMap.TryGetValue(result.SwapItemId, out GridItemFootprint blocker))
                {
                    return false;
                }

                blocker.X = result.SwapItemX;
                blocker.Y = result.SwapItemY;
                if (!this.TryPlaceOrMove(blocker))
                {
                    return false;
                }
            }

            moving.X = result.PlaceX;
            moving.Y = result.PlaceY;
            return this.TryPlaceOrMove(moving);
        }

        private void CollectBlockers(int x, int y, int width, int height, long ignoreItemId, HashSet<long> output)
        {
            output.Clear();

            int minX = Math.Max(x, 0);
            int minY = Math.Max(y, 0);
            int maxX = Math.Min(x + width, this.cols);
            int maxY = Math.Min(y + height, this.rows);
            if (minX >= maxX || minY >= maxY)
            {
                return;
            }

            for (int yy = minY; yy < maxY; ++yy)
            {
                int rowOffset = yy * this.cols;
                for (int xx = minX; xx < maxX; ++xx)
                {
                    long occupied = this.cells[rowOffset + xx];
                    if (occupied == 0 || occupied == ignoreItemId)
                    {
                        continue;
                    }

                    output.Add(occupied);
                }
            }
        }

        private void FillCells(GridItemFootprint footprint, long itemId)
        {
            int maxY = footprint.Y + footprint.Height;
            int maxX = footprint.X + footprint.Width;
            for (int yy = footprint.Y; yy < maxY; ++yy)
            {
                int rowOffset = yy * this.cols;
                for (int xx = footprint.X; xx < maxX; ++xx)
                {
                    this.cells[rowOffset + xx] = itemId;
                }
            }
        }
    }
}
