using System;
using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Detour;

namespace ET
{
    [EnableClass]
    public class RecastRandom: IRcRand
    {
        public float Next()
        {
            return RandomGenerator.RandFloat();
        }
    }
    
    /// <summary>
    /// 同一块地图可能有多种寻路数据，玩家可以随时切换，怪物也可能跟玩家的寻路不一样，寻路组件应该挂在Unit上
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class PathfindingComponent: Entity, IAwake<string>, IDestroy
    {
        public const int MAX_POLYS = 256;
        
        public const int FindRandomNavPosMaxRadius = 15000;  // 随机找寻路点的最大半径
        
        public RcVec3f extents = new(15, 10, 15);

        /// <summary>
        /// 移动贴地时使用的基础投影范围（按角色半径动态放大）。
        /// </summary>
        public RcVec3f movementProjectExtents = new(
            NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentXZ,
            NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentY,
            NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentXZ);

        /// <summary>
        /// 按角色半径放大移动投影范围的系数（XZ）。
        /// </summary>
        public float movementProjectExtentsByRadius = NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentByRadius;

        /// <summary>
        /// 摇杆移动投影允许的基础偏差（XZ）。
        /// </summary>
        public float movementRejectDistance = NavmeshGuardRuntimeConfig.DefaultMovementRejectDistance;

        /// <summary>
        /// 按角色半径放大摇杆偏差阈值的系数。
        /// </summary>
        public float movementRejectDistanceByRadius = NavmeshGuardRuntimeConfig.DefaultMovementRejectDistanceByRadius;

        /// <summary>
        /// 点击寻路起终点投影允许的基础偏差（XZ）。
        /// </summary>
        public float findNearestRejectDistance = NavmeshGuardRuntimeConfig.DefaultFindNearestRejectDistance;

        /// <summary>
        /// 按角色半径放大点击寻路偏差阈值的系数。
        /// </summary>
        public float findNearestRejectDistanceByRadius = NavmeshGuardRuntimeConfig.DefaultFindNearestRejectDistanceByRadius;

        /// <summary>
        /// 参与半径放大的最小半径。
        /// </summary>
        public float minUnitRadius = NavmeshGuardRuntimeConfig.DefaultMinUnitRadius;
        
        public string Name;
        
        public DtNavMesh navMesh;
        
        public List<long> polys = new(MAX_POLYS);

        public IDtQueryFilter filter;
        
        public List<StraightPathItem> straightPath = new();

        public DtNavMeshQuery query;

        public RecastRandom NavmeshRandom { get; } = new();

        /// <summary>
        /// Unity->NavMesh 的 X 轴变换符号，默认沿用历史行为（-1）。
        /// 当运行时检测到当前场景坐标系不匹配时会自动切换为 1。
        /// </summary>
        public int navXSign = -1;

        /// <summary>
        /// 避免坐标系切换日志刷屏。
        /// </summary>
        public bool navXSignAdjusted;
    }
}
