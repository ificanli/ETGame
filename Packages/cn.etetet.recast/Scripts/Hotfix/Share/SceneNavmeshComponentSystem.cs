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
            RebuildMissingPolyNeighbors(self.NavMesh, name);
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

        /// <summary>
        /// 检测并重建缺失的多边形邻居数据（poly.neis）。
        /// 烘焙工具可能未生成邻居信息，导致所有多边形成为孤岛。
        /// 通过检测共享边（两个相同顶点索引）自动重建。
        /// </summary>
        private static void RebuildMissingPolyNeighbors(DotRecast.Detour.DtNavMesh navMesh, string sceneName)
        {
            if (navMesh == null)
            {
                return;
            }

            int totalFixed = 0;
            int totalEdgesChecked = 0;
            int maxTiles = navMesh.GetMaxTiles();

            for (int tileIndex = 0; tileIndex < maxTiles; ++tileIndex)
            {
                DotRecast.Detour.DtMeshTile tile = navMesh.GetTile(tileIndex);
                if (tile?.data?.header == null)
                {
                    continue;
                }

                int polyCount = tile.data.header.polyCount;

                // 先检查是否有缺失的邻居
                bool hasMissingNeis = false;
                for (int i = 0; i < polyCount && !hasMissingNeis; ++i)
                {
                    DotRecast.Detour.DtPoly poly = tile.data.polys[i];
                    if (poly.GetPolyType() == DotRecast.Detour.DtPoly.DT_POLYTYPE_OFFMESH_CONNECTION)
                    {
                        continue;
                    }

                    for (int e = 0; e < poly.vertCount; ++e)
                    {
                        // 只关注 neis==0 的边（无邻居标记）
                        if (poly.neis[e] == 0)
                        {
                            hasMissingNeis = true;
                            break;
                        }
                    }
                }

                if (!hasMissingNeis)
                {
                    continue;
                }

                // 遍历每对多边形，查找共享边
                int fixedInTile = 0;
                for (int i = 0; i < polyCount; ++i)
                {
                    DotRecast.Detour.DtPoly polyA = tile.data.polys[i];
                    if (polyA.GetPolyType() == DotRecast.Detour.DtPoly.DT_POLYTYPE_OFFMESH_CONNECTION)
                    {
                        continue;
                    }

                    for (int j = i + 1; j < polyCount; ++j)
                    {
                        DotRecast.Detour.DtPoly polyB = tile.data.polys[j];
                        if (polyB.GetPolyType() == DotRecast.Detour.DtPoly.DT_POLYTYPE_OFFMESH_CONNECTION)
                        {
                            continue;
                        }

                        totalEdgesChecked++;

                        // 找共享边：A 的某条边 (va0, va1) 和 B 的某条边 (vb0, vb1) 共享两个顶点
                        for (int ea = 0; ea < polyA.vertCount; ++ea)
                        {
                            if (polyA.neis[ea] != 0)
                            {
                                continue; // 已有邻居，跳过
                            }

                            int va0 = polyA.verts[ea];
                            int va1 = polyA.verts[(ea + 1) % polyA.vertCount];

                            for (int eb = 0; eb < polyB.vertCount; ++eb)
                            {
                                if (polyB.neis[eb] != 0)
                                {
                                    continue; // 已有邻居，跳过
                                }

                                int vb0 = polyB.verts[eb];
                                int vb1 = polyB.verts[(eb + 1) % polyB.vertCount];

                                // 共享边：顶点索引匹配（方向相反）
                                if ((va0 == vb1 && va1 == vb0) || (va0 == vb0 && va1 == vb1))
                                {
                                    // neis 值 = 邻居多边形索引 + 1（DotRecast 约定）
                                    polyA.neis[ea] = j + 1;
                                    polyB.neis[eb] = i + 1;
                                    fixedInTile += 2;
                                }
                            }
                        }
                    }
                }

                totalFixed += fixedInTile;
            }

            if (totalFixed > 0)
            {
                Log.Info($"[Navmesh] rebuilt {totalFixed} missing poly neighbors. scene={sceneName}");

                // 重建 polyLinks（ConnectIntLinks 从 poly.neis 生成链接链表）
                for (int tileIndex = 0; tileIndex < maxTiles; ++tileIndex)
                {
                    DotRecast.Detour.DtMeshTile tile = navMesh.GetTile(tileIndex);
                    if (tile?.data?.header == null)
                    {
                        continue;
                    }

                    navMesh.ConnectIntLinks(tile);
                }

                Log.Info($"[Navmesh] rebuilt internal links after neighbor fix. scene={sceneName}");
            }
        }
    }
}
