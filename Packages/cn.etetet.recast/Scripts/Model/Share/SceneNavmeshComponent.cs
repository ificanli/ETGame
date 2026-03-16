using DotRecast.Detour;

namespace ET
{
    /// <summary>
    /// 场景级导航网格实例，避免同名地图的运行时改动互相污染。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class SceneNavmeshComponent : Entity, IAwake<string, DtNavMesh>, IDestroy
    {
        public string Name;
        public DtNavMesh NavMesh;

        public int NavPolyCount;
        public int NavZeroFlagPolyCount;
        public int NavFixedZeroFlagPolyCount;
        public int NavDefaultFilterPassPolyCount;
        public bool UseAllPassQueryFilter;

        public float MovementProjectHalfExtentXZ = NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentXZ;
        public float MovementProjectHalfExtentY = NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentY;
        public float MovementProjectHalfExtentByRadius = NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentByRadius;

        public float MovementRejectDistance = NavmeshGuardRuntimeConfig.DefaultMovementRejectDistance;
        public float MovementRejectDistanceByRadius = NavmeshGuardRuntimeConfig.DefaultMovementRejectDistanceByRadius;

        public float FindNearestRejectDistance = NavmeshGuardRuntimeConfig.DefaultFindNearestRejectDistance;
        public float FindNearestRejectDistanceByRadius = NavmeshGuardRuntimeConfig.DefaultFindNearestRejectDistanceByRadius;

        public float MinUnitRadius = NavmeshGuardRuntimeConfig.DefaultMinUnitRadius;
    }
}
