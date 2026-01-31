using System.Collections.Generic;

namespace ET.Server
{
    public static partial class AOIHelper
    {
        public static long CreateCellId(int x, int y)
        {
            return (long) ((ulong) x << 32) | (uint) y;
        }

        public static void CalcEnterAndLeaveCell(AOIEntity aoiEntity, int cellX, int cellY, HashSet<long> enterCell, HashSet<long> leaveCell)
        {
            enterCell.Clear();
            leaveCell.Clear();
            int viewDistance = aoiEntity.NumericComponent.Get(NumericType.AOI);
            if (viewDistance <= 0)
            {
                viewDistance = 1;
            }
            int r = (viewDistance - 1) / AOIManagerComponent.CellSize + 1;
            int leaveR = r;
            if (aoiEntity.Unit.UnitType == UnitType.Player)
            {
                leaveR += 1;
            }

            for (int i = cellX - leaveR; i <= cellX + leaveR; ++i)
            {
                for (int j = cellY - leaveR; j <= cellY + leaveR; ++j)
                {
                    long cellId = CreateCellId(i, j);
                    leaveCell.Add(cellId);

                    if (i > cellX + r || i < cellX - r || j > cellY + r || j < cellY - r)
                    {
                        continue;
                    }

                    enterCell.Add(cellId);
                }
            }
        }
    }
}