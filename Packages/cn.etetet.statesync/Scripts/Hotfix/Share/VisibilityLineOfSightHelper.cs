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

    /// <summary>
    /// 统一解析当前武器是否驱动特殊视野半径。
    /// </summary>
    public static class WeaponVisionRangeHelper
    {
        public static bool TryResolveCurrentSniperVisionRange(Unit unit, out float visionRange)
        {
            visionRange = 0f;
            if (unit == null || unit.IsDisposed)
            {
                return false;
            }

            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent == null || weaponComponent.CurrentSlot <= 0 || weaponComponent.CurrentWeaponId <= 0)
            {
                return false;
            }

            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(weaponComponent.CurrentWeaponId);
            if (weaponConfig == null || weaponConfig.WeaponTypeId != (int)WeaponType.SniperRifle)
            {
                return false;
            }

            visionRange = math.max(0f, weaponComponent.GetEffectiveAttackRange(weaponComponent.CurrentSlot));
            return visionRange > 0f;
        }
    }
}
