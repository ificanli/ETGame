using System;

namespace ET
{
    /// <summary>
    /// 导航吸附防穿墙运行时默认参数。
    /// </summary>
    [EnableClass]
    public class NavmeshGuardRuntimeConfig
    {
        public const float DefaultMovementProjectHalfExtentXZ = 0.55f;
        public const float DefaultMovementProjectHalfExtentY = 0.50f;
        public const float DefaultMovementProjectHalfExtentByRadius = 1.10f;
        public const float DefaultMovementRejectDistance = 0.35f;
        public const float DefaultMovementRejectDistanceByRadius = 0.80f;
        public const float DefaultFindNearestRejectDistance = 1.00f;
        public const float DefaultFindNearestRejectDistanceByRadius = 2.00f;
        public const float DefaultMinUnitRadius = 0.30f;

        /// <summary>
        /// 移动投影的基础半径（XZ）。
        /// </summary>
        public float MovementProjectHalfExtentXZ = DefaultMovementProjectHalfExtentXZ;

        /// <summary>
        /// 移动投影的Y轴半高。
        /// </summary>
        public float MovementProjectHalfExtentY = DefaultMovementProjectHalfExtentY;

        /// <summary>
        /// 按角色半径放大移动投影范围的系数（XZ）。
        /// </summary>
        public float MovementProjectHalfExtentByRadius = DefaultMovementProjectHalfExtentByRadius;

        /// <summary>
        /// 摇杆投影允许的基础偏差（XZ）。
        /// </summary>
        public float MovementRejectDistance = DefaultMovementRejectDistance;

        /// <summary>
        /// 按角色半径放大摇杆偏差阈值的系数。
        /// </summary>
        public float MovementRejectDistanceByRadius = DefaultMovementRejectDistanceByRadius;

        /// <summary>
        /// 点击寻路起终点投影允许的基础偏差（XZ）。
        /// </summary>
        public float FindNearestRejectDistance = DefaultFindNearestRejectDistance;

        /// <summary>
        /// 按角色半径放大点击寻路投影偏差阈值的系数。
        /// </summary>
        public float FindNearestRejectDistanceByRadius = DefaultFindNearestRejectDistanceByRadius;

        /// <summary>
        /// 参与半径放大的最小角色半径。
        /// </summary>
        public float MinUnitRadius = DefaultMinUnitRadius;

        public void Normalize()
        {
            this.MovementProjectHalfExtentXZ = Math.Max(0.05f, this.MovementProjectHalfExtentXZ);
            this.MovementProjectHalfExtentY = Math.Max(0.05f, this.MovementProjectHalfExtentY);
            this.MovementProjectHalfExtentByRadius = Math.Max(0f, this.MovementProjectHalfExtentByRadius);
            this.MovementRejectDistance = Math.Max(0.05f, this.MovementRejectDistance);
            this.MovementRejectDistanceByRadius = Math.Max(0f, this.MovementRejectDistanceByRadius);
            this.FindNearestRejectDistance = Math.Max(0.05f, this.FindNearestRejectDistance);
            this.FindNearestRejectDistanceByRadius = Math.Max(0f, this.FindNearestRejectDistanceByRadius);
            this.MinUnitRadius = Math.Max(0f, this.MinUnitRadius);
        }
    }
}
