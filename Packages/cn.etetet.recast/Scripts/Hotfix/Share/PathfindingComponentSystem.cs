using System;
using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Detour;
using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(PathfindingComponent))]
    public static partial class PathfindingComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PathfindingComponent self, string name)
        {
            self.Name = name;
            self.navXSign = -1;
            self.navXSignAdjusted = false;
            Scene scene = self.Scene();
            SceneNavmeshComponent sceneNavmeshComponent = scene?.GetComponent<SceneNavmeshComponent>();
            self.navMesh = sceneNavmeshComponent != null && sceneNavmeshComponent.Name == name
                ? sceneNavmeshComponent.NavMesh
                : NavmeshComponent.Instance.Get(name);
            
            if (self.navMesh == null)
            {
                throw new Exception($"nav load fail: {name}");
            }

            self.query = new DtNavMeshQuery(self.navMesh);
            self.filter = self.CreateQueryFilter(sceneNavmeshComponent);
            self.ApplySceneGuardConfig(sceneNavmeshComponent);
        }

        [EntitySystem]
        private static void Destroy(this PathfindingComponent self)
        {
            self.Name = string.Empty;
            self.navMesh = null;
        }

        private static IDtQueryFilter CreateQueryFilter(this PathfindingComponent self, SceneNavmeshComponent sceneNavmeshComponent)
        {
            if (sceneNavmeshComponent != null && sceneNavmeshComponent.UseAllPassQueryFilter)
            {
                Log.Warning($"[NavGuard] switch to all-pass query filter. scene={self.Scene().Name}, unitId={self.Parent?.Id ?? 0}, reason=all-polygons-filtered");
                return DtQueryNoOpFilter.Shared;
            }

            return new DtQueryDefaultFilter();
        }
        
        public static void Find(this PathfindingComponent self, float3 start, float3 target, List<float3> result, float unitRadius)
        {
            if (self.navMesh == null)
            {
                Log.Debug("寻路| Find 失败 pathfinding ptr is zero");
                throw new Exception($"pathfinding ptr is zero: {self.Scene().Name}");
            }

            bool hasStart = self.TryProjectNearestPoly(start, self.extents, out long startRef, out RcVec3f startPt, out _);
            bool hasEnd = self.TryProjectNearestPoly(target, self.extents, out long endRef, out RcVec3f endPt, out _);

            if (hasStart && hasEnd)
            {
                self.query.FindPath(startRef, endRef, startPt, endPt, self.filter, ref self.polys, new DtFindPathOption(0, float.MaxValue));

                if (self.polys.Count > 0)
                {
                    // In case of partial path, make sure the end point is clamped to the last polygon.
                    RcVec3f epos = RcVec3f.Of(endPt.x, endPt.y, endPt.z);
                    if (self.polys[^1] != endRef)
                    {
                        DtStatus dtStatus = self.query.ClosestPointOnPoly(self.polys[^1], endPt, out RcVec3f closest, out bool _);
                        if (dtStatus.Succeeded())
                        {
                            epos = closest;
                        }
                    }

                    self.query.FindStraightPath(startPt, epos, self.polys, ref self.straightPath, PathfindingComponent.MAX_POLYS, DtNavMeshQuery.DT_STRAIGHTPATH_ALL_CROSSINGS);
                    if (self.straightPath.Count > 0)
                    {
                        for (int i = 0; i < self.straightPath.Count; ++i)
                        {
                            RcVec3f pos = self.straightPath[i].pos;
                            result.Add(self.ToUnityPos(pos, self.navXSign));
                        }

                        if (result.Count >= 2)
                        {
                            return;
                        }
                    }
                }
            }

            // 禁用 reject 机制后的兜底：即使导航查询失败，也保证返回可移动的直线路径。
            result.Clear();
            result.Add(start);
            result.Add(target);
        }
        
        public static float3 FindRandomPointAroundCircle(this PathfindingComponent self, float3 center, float radius)
        {
            if (self.navMesh == null)
            {
                throw new Exception($"pathfinding ptr is zero: {self.Scene().Name}");
            }

            RcVec3f centerPos = self.ToNavPos(center, self.navXSign);
            
            self.query.FindNearestPoly(centerPos, self.extents, self.filter, out long startRef, out RcVec3f startPos, out _);
            
            DtStatus dtStatus = self.query.FindRandomPointAroundCircle(startRef, startPos, radius, self.filter, self.NavmeshRandom, out _, out RcVec3f endPt);
            if (!dtStatus.Succeeded())
            {
                //throw new Exception($"FindRandomPointAroundCircle error: {self.Scene().Name} {dtStatus.Value}");
                endPt = startPos;
            }

            return self.ToUnityPos(endPt, self.navXSign);
        }
        
        public static float3 FindRandomPointWithRaduis(this PathfindingComponent self, float3 pos, float minRadius, float maxRadius)
        {
            if (self.navMesh == null)
            {
                throw new Exception($"pathfinding ptr is zero: {self.Scene().Name}");
            }
            
            int degrees = RandomGenerator.RandomNumber(0, 360);
            float r = RandomGenerator.RandomNumber((int) (minRadius * 1000), (int) (maxRadius * 1000)) / 1000f;

            float x = r * math.cos(math.radians(degrees));
            float z = r * math.sin(math.radians(degrees));

            float3 findpos = new(pos.x + x, pos.y, pos.z + z);

            return self.RecastFindNearestPoint(findpos);
        }

        public static float3 RecastFindNearestPoint(this PathfindingComponent self, float3 pos)
        {
            if (self.navMesh == null)
            {
                throw new Exception($"pathfinding ptr is zero: {self.Scene().Name}");
            }

            if (!self.TryProjectNearestPoly(pos, self.extents, out _, out RcVec3f startPt, out _))
            {
                return pos;
            }

            return self.ToUnityPos(startPt, self.navXSign);
        }

        public static bool TryRecastFindNearestPoint(this PathfindingComponent self, float3 pos, float unitRadius, out float3 projectedPos, out float projectedDistance)
        {
            projectedPos = pos;
            projectedDistance = 0f;

            if (self.navMesh == null)
            {
                throw new Exception($"pathfinding ptr is zero: {self.Scene().Name}");
            }

            float safeRadius = self.GetSafeUnitRadius(unitRadius);
            if (!self.TryProjectNearestPoly(pos, self.extents, out _, out RcVec3f startPt, out projectedDistance))
            {
                return false;
            }

            float maxSnapDistance = self.GetFindNearestRejectDistance(safeRadius);
            if (projectedDistance > maxSnapDistance)
            {
                return false;
            }

            projectedPos = self.ToUnityPos(startPt, self.navXSign);
            return true;
        }

        public static bool TryRecastFindNearestPointForMovement(this PathfindingComponent self, float3 pos, float unitRadius, out float3 projectedPos, out float projectedDistance)
        {
            projectedPos = pos;
            projectedDistance = 0f;

            if (self.navMesh == null)
            {
                throw new Exception($"pathfinding ptr is zero: {self.Scene().Name}");
            }

            float safeRadius = self.GetSafeUnitRadius(unitRadius);
            RcVec3f movementExtents = self.GetMovementProjectExtents(safeRadius);
            if (!self.TryProjectNearestPoly(pos, movementExtents, out _, out RcVec3f startPt, out projectedDistance))
            {
                return false;
            }

            projectedPos = self.ToUnityPos(startPt, self.navXSign);
            return true;
        }

        public static bool TryRecastFindNearestPointForSpawn(this PathfindingComponent self, float3 pos, float unitRadius, out float3 projectedPos, out float projectedDistance)
        {
            projectedPos = pos;
            projectedDistance = 0f;

            if (self.navMesh == null)
            {
                throw new Exception($"pathfinding ptr is zero: {self.Scene().Name}");
            }

            float safeRadius = self.GetSafeUnitRadius(unitRadius);
            float minExtentXZ = math.max(self.extents.x, safeRadius * self.movementProjectExtentsByRadius);
            float extentY = math.max(self.extents.y, self.movementProjectExtents.y);
            float spawnExtentY = math.max(extentY, 512f);
            float maxExtentXZ = math.max(minExtentXZ, PathfindingComponent.FindRandomNavPosMaxRadius);

            float currentExtentXZ = minExtentXZ;
            while (currentExtentXZ < maxExtentXZ)
            {
                RcVec3f currentExtents = new RcVec3f(currentExtentXZ, spawnExtentY, currentExtentXZ);
                if (self.TryProjectNearestPoly(pos, currentExtents, out _, out RcVec3f currentProjectedPoint, out projectedDistance))
                {
                    projectedPos = self.ToUnityPos(currentProjectedPoint, self.navXSign);
                    return true;
                }

                currentExtentXZ *= 2f;
            }

            RcVec3f maxExtents = new RcVec3f(maxExtentXZ, spawnExtentY, maxExtentXZ);
            if (!self.TryProjectNearestPoly(pos, maxExtents, out _, out RcVec3f projectedPoint, out projectedDistance))
            {
                return false;
            }

            projectedPos = self.ToUnityPos(projectedPoint, self.navXSign);
            return true;
        }

        public static float GetMovementRejectDistance(this PathfindingComponent self, float unitRadius)
        {
            float safeRadius = self.GetSafeUnitRadius(unitRadius);
            return math.max(self.movementRejectDistance, safeRadius * self.movementRejectDistanceByRadius);
        }

        private static float GetFindNearestRejectDistance(this PathfindingComponent self, float unitRadius)
        {
            return math.max(self.findNearestRejectDistance, unitRadius * self.findNearestRejectDistanceByRadius);
        }

        private static float GetSafeUnitRadius(this PathfindingComponent self, float unitRadius)
        {
            return math.max(self.minUnitRadius, unitRadius);
        }

        private static RcVec3f GetMovementProjectExtents(this PathfindingComponent self, float safeRadius)
        {
            float xz = math.max(self.movementProjectExtents.x, safeRadius * self.movementProjectExtentsByRadius);
            return new RcVec3f(xz, self.movementProjectExtents.y, xz);
        }

        private static bool TryProjectNearestPoly(this PathfindingComponent self, float3 pos, RcVec3f extents, out long polyRef, out RcVec3f projectedPoint, out float projectedDistance)
        {
            if (self.TryProjectNearestPolyBySign(pos, extents, self.navXSign, out polyRef, out projectedPoint, out projectedDistance))
            {
                return true;
            }

            RcVec3f expandedExtents = self.GetExpandedProjectExtents(extents);
            bool hasExpandedExtents = expandedExtents.y > extents.y + 0.001f;
            if (hasExpandedExtents &&
                self.TryProjectNearestPolyBySign(pos, expandedExtents, self.navXSign, out polyRef, out projectedPoint, out projectedDistance))
            {
                return true;
            }

            int fallbackSign = -self.navXSign;
            if (self.TryProjectNearestPolyBySign(pos, extents, fallbackSign, out polyRef, out projectedPoint, out projectedDistance))
            {
                self.ApplyDetectedNavXSign(fallbackSign);
                return true;
            }

            if (hasExpandedExtents &&
                self.TryProjectNearestPolyBySign(pos, expandedExtents, fallbackSign, out polyRef, out projectedPoint, out projectedDistance))
            {
                self.ApplyDetectedNavXSign(fallbackSign);
                return true;
            }

            projectedDistance = float.MaxValue;
            polyRef = 0;
            projectedPoint = self.ToNavPos(pos, self.navXSign);
            return false;
        }

        private static bool TryProjectNearestPolyBySign(this PathfindingComponent self, float3 pos, RcVec3f extents, int navXSign, out long polyRef, out RcVec3f projectedPoint, out float projectedDistance)
        {
            RcVec3f navPos = self.ToNavPos(pos, navXSign);
            DtStatus status = self.query.FindNearestPoly(navPos, extents, self.filter, out polyRef, out projectedPoint, out _);
            if (status.Failed() || polyRef == 0)
            {
                projectedDistance = float.MaxValue;
                projectedPoint = navPos;
                return false;
            }

            float3 projected = self.ToUnityPos(projectedPoint, navXSign);
            projectedDistance = math.distance(new float2(pos.x, pos.z), new float2(projected.x, projected.z));
            return true;
        }

        private static RcVec3f GetExpandedProjectExtents(this PathfindingComponent self, RcVec3f extents)
        {
            float expandedY = math.max(extents.y, 256f);
            return expandedY > extents.y + 0.001f ? new RcVec3f(extents.x, expandedY, extents.z) : extents;
        }

        private static RcVec3f ToNavPos(this PathfindingComponent self, float3 unityPos, int navXSign)
        {
            return new RcVec3f(navXSign * unityPos.x, unityPos.y, unityPos.z);
        }

        private static float3 ToUnityPos(this PathfindingComponent self, RcVec3f navPos, int navXSign)
        {
            return new float3(navXSign * navPos.x, navPos.y, navPos.z);
        }

        private static void ApplyDetectedNavXSign(this PathfindingComponent self, int detectedSign)
        {
            if (self.navXSign == detectedSign)
            {
                return;
            }

            int oldSign = self.navXSign;
            self.navXSign = detectedSign;
            if (!self.navXSignAdjusted)
            {
                self.navXSignAdjusted = true;
                Log.Warning($"[NavGuard] auto switch nav x-sign: scene={self.Scene().Name}, unitId={self.Parent?.Id ?? 0}, old={oldSign}, new={detectedSign}");
            }
        }

        /// <summary>
        /// 沿 NavMesh 表面从 startPos 向 endPos 移动。
        /// 如果全程可达，返回 true，safePos = endPos。
        /// 如果碰到墙壁，返回 false，safePos = NavMesh 上最近可达点（已自动贴墙滑动）。
        /// 如果起点不在 NavMesh 上，返回 true 并放行（避免卡死）。
        /// </summary>
        public static bool TryMoveAlongSurface(this PathfindingComponent self, float3 startPos, float3 endPos, out float3 safePos)
        {
            safePos = endPos;

            if (self.navMesh == null)
            {
                return true;
            }

            // 找到起点所在的多边形（用移动专用的小范围 extents，避免跨墙抓到错误多边形）
            if (!self.TryProjectNearestPoly(startPos, self.movementProjectExtents, out long startRef, out RcVec3f startNavPt, out float startDist))
            {
                long now1 = TimeInfo.Instance.ServerNow();
                if (now1 - self.lastRaycastLogTime >= 500)
                {
                    self.lastRaycastLogTime = now1;
                    Log.Info($"[NavMove] start not on navmesh, pass through. unitId={self.Parent?.Id}, startPos={startPos}, navXSign={self.navXSign}");
                }
                return true;
            }

            // 起点离 NavMesh 太远，放行
            if (startDist > 2f)
            {
                long now2 = TimeInfo.Instance.ServerNow();
                if (now2 - self.lastRaycastLogTime >= 500)
                {
                    self.lastRaycastLogTime = now2;
                    Log.Info($"[NavMove] start too far from navmesh, pass through. unitId={self.Parent?.Id}, startPos={startPos}, startDist={startDist:F3}");
                }
                return true;
            }

            // 用 startPos 的 NavMesh 坐标作为起点，而非投影点（startNavPt）。
            // startNavPt 是 FindNearestPoly 的表面投影结果，当玩家贴着多边形边界时，
            // 投影点恰好在边界上，MoveAlongSurface 从边界出发会立即被截断，
            // 导致所有方向都走不了（空气墙）或产生误差（穿墙）。
            RcVec3f navStart = self.ToNavPos(startPos, self.navXSign);
            RcVec3f navEnd = self.ToNavPos(endPos, self.navXSign);

            List<long> visited = new List<long>(16);
            DtStatus status = self.query.MoveAlongSurface(startRef, navStart, navEnd, self.filter, out RcVec3f resultPos, ref visited);

            if (status.Failed())
            {
                long now3 = TimeInfo.Instance.ServerNow();
                if (now3 - self.lastRaycastLogTime >= 500)
                {
                    self.lastRaycastLogTime = now3;
                    Log.Info($"[NavMove] MoveAlongSurface failed, pass through. unitId={self.Parent?.Id}, status={status}");
                }
                return true;
            }

            float3 resultUnity = self.ToUnityPos(resultPos, self.navXSign);
            safePos = new float3(resultUnity.x, startPos.y, resultUnity.z);

            // 判断是否到达了目标点
            float reachedDist = math.distance(
                new float2(safePos.x, safePos.z),
                new float2(endPos.x, endPos.z));

            if (reachedDist < 0.001f)
            {
                return true;
            }

            // 没到达目标 = 碰到了墙
            long now4 = TimeInfo.Instance.ServerNow();
            if (now4 - self.lastRaycastLogTime >= 500)
            {
                self.lastRaycastLogTime = now4;
                Log.Info($"[NavMove] wall hit. unitId={self.Parent?.Id}, startPos={startPos}, endPos={endPos}, safePos={safePos}, reachedDist={reachedDist:F4}, visitedPolys={visited.Count}, navXSign={self.navXSign}");
            }

            return false;
        }

        /// <summary>
        /// 提取 NavMesh 所有可行走多边形的三角形顶点（Unity 坐标系），用于可视化调试。
        /// 返回三角形顶点列表（每 3 个顶点组成一个三角形）和对应的 flag。
        /// </summary>
        public static void ExtractNavMeshTriangles(this PathfindingComponent self, List<float3> vertices, List<int> flags)
        {
            vertices.Clear();
            flags.Clear();

            if (self.navMesh == null)
            {
                return;
            }

            int maxTiles = self.navMesh.GetMaxTiles();
            for (int tileIndex = 0; tileIndex < maxTiles; ++tileIndex)
            {
                DtMeshTile tile = self.navMesh.GetTile(tileIndex);
                if (tile?.data?.header == null)
                {
                    continue;
                }

                int polyCount = tile.data.header.polyCount;
                for (int polyIndex = 0; polyIndex < polyCount; ++polyIndex)
                {
                    DtPoly poly = tile.data.polys[polyIndex];
                    if (poly.GetPolyType() == DtPoly.DT_POLYTYPE_OFFMESH_CONNECTION)
                    {
                        continue;
                    }

                    // 凸多边形扇形三角化：以第一个顶点为中心
                    for (int j = 1; j < poly.vertCount - 1; ++j)
                    {
                        int v0 = poly.verts[0] * 3;
                        int v1 = poly.verts[j] * 3;
                        int v2 = poly.verts[j + 1] * 3;

                        float3 p0 = self.ToUnityPos(
                            new RcVec3f(tile.data.verts[v0], tile.data.verts[v0 + 1], tile.data.verts[v0 + 2]),
                            self.navXSign);
                        float3 p1 = self.ToUnityPos(
                            new RcVec3f(tile.data.verts[v1], tile.data.verts[v1 + 1], tile.data.verts[v1 + 2]),
                            self.navXSign);
                        float3 p2 = self.ToUnityPos(
                            new RcVec3f(tile.data.verts[v2], tile.data.verts[v2 + 1], tile.data.verts[v2 + 2]),
                            self.navXSign);

                        vertices.Add(p0);
                        vertices.Add(p1);
                        vertices.Add(p2);
                        flags.Add(poly.flags);
                    }
                }
            }

            Log.Info($"[NavMeshDebug] extracted {vertices.Count / 3} triangles from {maxTiles} tiles, navXSign={self.navXSign}");
        }

        private static void ApplySceneGuardConfig(this PathfindingComponent self, SceneNavmeshComponent sceneNavmeshComponent)
        {
            if (sceneNavmeshComponent == null)
            {
                self.minUnitRadius = NavmeshGuardRuntimeConfig.DefaultMinUnitRadius;
                self.movementProjectExtents = new RcVec3f(
                    NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentXZ,
                    NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentY,
                    NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentXZ);
                self.movementProjectExtentsByRadius = NavmeshGuardRuntimeConfig.DefaultMovementProjectHalfExtentByRadius;
                self.movementRejectDistance = NavmeshGuardRuntimeConfig.DefaultMovementRejectDistance;
                self.movementRejectDistanceByRadius = NavmeshGuardRuntimeConfig.DefaultMovementRejectDistanceByRadius;
                self.findNearestRejectDistance = NavmeshGuardRuntimeConfig.DefaultFindNearestRejectDistance;
                self.findNearestRejectDistanceByRadius = NavmeshGuardRuntimeConfig.DefaultFindNearestRejectDistanceByRadius;
                return;
            }

            self.minUnitRadius = sceneNavmeshComponent.MinUnitRadius;
            self.movementProjectExtents = new RcVec3f(
                sceneNavmeshComponent.MovementProjectHalfExtentXZ,
                sceneNavmeshComponent.MovementProjectHalfExtentY,
                sceneNavmeshComponent.MovementProjectHalfExtentXZ);
            self.movementProjectExtentsByRadius = sceneNavmeshComponent.MovementProjectHalfExtentByRadius;
            self.movementRejectDistance = sceneNavmeshComponent.MovementRejectDistance;
            self.movementRejectDistanceByRadius = sceneNavmeshComponent.MovementRejectDistanceByRadius;
            self.findNearestRejectDistance = sceneNavmeshComponent.FindNearestRejectDistance;
            self.findNearestRejectDistanceByRadius = sceneNavmeshComponent.FindNearestRejectDistanceByRadius;
        }
    }
}
