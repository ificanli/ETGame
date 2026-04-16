using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 视野与索敌共用的直线可见性辅助。
    /// </summary>
    public static class VisibilityLineOfSightHelper
    {
        private const float DefaultMaxVerticalDelta = 4f;

        public static bool HasLineOfSight(Unit viewer, Unit target, float maxVerticalDelta = DefaultMaxVerticalDelta)
        {
            if (viewer == null || viewer.IsDisposed || target == null || target.IsDisposed)
            {
                return false;
            }

            return HasLineOfSight(viewer, target.Position, maxVerticalDelta);
        }

        public static bool HasLineOfSight(Unit viewer, float3 targetPosition, float maxVerticalDelta = DefaultMaxVerticalDelta)
        {
            if (viewer == null || viewer.IsDisposed)
            {
                return false;
            }

            PathfindingComponent pathfinding = viewer.GetComponent<PathfindingComponent>();
            float unitRadius = viewer.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            return pathfinding.HasLineOfSight(viewer.Position, targetPosition, unitRadius, maxVerticalDelta, out _);
        }

        public static float ResolveRawAoiWorldRadius(Unit unit, float fallbackRadius = 0f)
        {
            float safeFallbackRadius = math.max(0f, fallbackRadius);
            if (unit == null || unit.IsDisposed)
            {
                return safeFallbackRadius;
            }

            long rawAoi = unit.NumericComponent?.GetAsLong(NumericType.AOI) ?? 0L;
            return rawAoi > 0L ? rawAoi / 1000f : safeFallbackRadius;
        }
    }
}
