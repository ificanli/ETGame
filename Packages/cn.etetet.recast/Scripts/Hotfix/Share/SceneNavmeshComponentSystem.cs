namespace ET
{
    [EntitySystemOf(typeof(SceneNavmeshComponent))]
    [FriendOf(typeof(SceneNavmeshComponent))]
    public static partial class SceneNavmeshComponentSystem
    {
        private const int DefaultWalkablePolyFlag = 1;

        [EntitySystem]
        private static void Awake(this SceneNavmeshComponent self, string name, DotRecast.Detour.DtNavMesh navMesh)
        {
            self.Name = name;
            self.NavMesh = navMesh;
            NormalizeZeroPolyFlags(self, self.NavMesh, name);

            NavmeshGuardRuntimeConfig config = LoadNavmeshGuardConfig(name);
            self.MovementProjectHalfExtentXZ = config.MovementProjectHalfExtentXZ;
            self.MovementProjectHalfExtentY = config.MovementProjectHalfExtentY;
            self.MovementProjectHalfExtentByRadius = config.MovementProjectHalfExtentByRadius;
            self.MovementRejectDistance = config.MovementRejectDistance;
            self.MovementRejectDistanceByRadius = config.MovementRejectDistanceByRadius;
            self.FindNearestRejectDistance = config.FindNearestRejectDistance;
            self.FindNearestRejectDistanceByRadius = config.FindNearestRejectDistanceByRadius;
            self.MinUnitRadius = config.MinUnitRadius;

            Log.Info($"[Navmesh] scene init: scene={name}, polyCount={self.NavPolyCount}, zeroFlags={self.NavZeroFlagPolyCount}, fixedZeroFlags={self.NavFixedZeroFlagPolyCount}, defaultFilterPassPolys={self.NavDefaultFilterPassPolyCount}, useAllPassFilter={self.UseAllPassQueryFilter}");
        }

        [EntitySystem]
        private static void Destroy(this SceneNavmeshComponent self)
        {
            self.Name = string.Empty;
            self.NavMesh = null;
            self.NavPolyCount = 0;
            self.NavZeroFlagPolyCount = 0;
            self.NavFixedZeroFlagPolyCount = 0;
            self.NavDefaultFilterPassPolyCount = 0;
            self.UseAllPassQueryFilter = false;
        }

        private static NavmeshGuardRuntimeConfig LoadNavmeshGuardConfig(string sceneName)
        {
            NavmeshGuardRuntimeConfig config = new NavmeshGuardRuntimeConfig();
            NavmeshGuardConfigCategory category = NavmeshGuardConfigCategory.Instance;
            NavmeshGuardConfig tableConfig = category?.Data;
            if (tableConfig == null)
            {
                config.Normalize();
                Log.Warning($"[NavGuard] table config not found, use defaults. scene={sceneName}");
                return config;
            }
            
            config.MovementProjectHalfExtentXZ = tableConfig.MovementProjectHalfExtentXZ;
            config.MovementProjectHalfExtentY = tableConfig.MovementProjectHalfExtentY;
            config.MovementProjectHalfExtentByRadius = tableConfig.MovementProjectHalfExtentByRadius;
            config.MovementRejectDistance = tableConfig.MovementRejectDistance;
            config.MovementRejectDistanceByRadius = tableConfig.MovementRejectDistanceByRadius;
            config.FindNearestRejectDistance = tableConfig.FindNearestRejectDistance;
            config.FindNearestRejectDistanceByRadius = tableConfig.FindNearestRejectDistanceByRadius;
            config.MinUnitRadius = tableConfig.MinUnitRadius;

            config.Normalize();
            return config;
        }

        private static void NormalizeZeroPolyFlags(SceneNavmeshComponent self, DotRecast.Detour.DtNavMesh navMesh, string sceneName)
        {
            if (navMesh == null)
            {
                self.NavPolyCount = 0;
                self.NavZeroFlagPolyCount = 0;
                self.NavFixedZeroFlagPolyCount = 0;
                self.NavDefaultFilterPassPolyCount = 0;
                self.UseAllPassQueryFilter = false;
                return;
            }

            int checkedPolys = 0;
            int zeroFlagPolys = 0;
            int fixedPolys = 0;
            int failedPolys = 0;
            int nonZeroPolys = 0;
            int defaultFilterPassPolys = 0;

            int maxTiles = navMesh.GetMaxTiles();
            for (int tileIndex = 0; tileIndex < maxTiles; ++tileIndex)
            {
                DotRecast.Detour.DtMeshTile tile = navMesh.GetTile(tileIndex);
                if (tile?.data?.header == null)
                {
                    continue;
                }

                long polyRefBase = navMesh.GetPolyRefBase(tile);
                int polyCount = tile.data.header.polyCount;
                for (int polyIndex = 0; polyIndex < polyCount; ++polyIndex)
                {
                    long polyRef = polyRefBase + polyIndex;
                    checkedPolys++;

                    DotRecast.Detour.DtStatus getStatus = navMesh.GetPolyFlags(polyRef, out int flags);
                    if (getStatus.Failed())
                    {
                        failedPolys++;
                        continue;
                    }

                    if (flags == 0)
                    {
                        zeroFlagPolys++;
                        DotRecast.Detour.DtStatus setStatus = navMesh.SetPolyFlags(polyRef, DefaultWalkablePolyFlag);
                        if (setStatus.Failed())
                        {
                            failedPolys++;
                            continue;
                        }

                        fixedPolys++;
                        flags = DefaultWalkablePolyFlag;
                    }

                    if (flags != 0)
                    {
                        nonZeroPolys++;
                        if ((flags & 0xffff) != 0)
                        {
                            defaultFilterPassPolys++;
                        }
                    }
                }
            }

            self.NavPolyCount = checkedPolys;
            self.NavZeroFlagPolyCount = zeroFlagPolys;
            self.NavFixedZeroFlagPolyCount = fixedPolys;
            self.NavDefaultFilterPassPolyCount = defaultFilterPassPolys;
            self.UseAllPassQueryFilter = checkedPolys > 0 && (nonZeroPolys == 0 || defaultFilterPassPolys == 0);

            if (fixedPolys > 0 || failedPolys > 0)
            {
                Log.Warning($"[Navmesh] normalize poly flags: scene={sceneName}, checked={checkedPolys}, fixedZeroFlags={fixedPolys}, failed={failedPolys}");
            }

            if (self.UseAllPassQueryFilter)
            {
                Log.Warning($"[Navmesh] all polygons are filtered by default query filter, fallback to all-pass query filter. scene={sceneName}, checked={checkedPolys}, zeroFlags={zeroFlagPolys}, defaultFilterPassPolys={defaultFilterPassPolys}, failed={failedPolys}");
            }
        }
    }
}
