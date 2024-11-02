using System;
using System.Collections.Generic;
using System.IO;
using DotRecast.Core;
using DotRecast.Detour;
using DotRecast.Detour.Io;
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
            self.navMesh = NavmeshComponent.Instance.Get(name);
            
            if (self.navMesh == null)
            {
                throw new Exception($"nav load fail: {name}");
            }
            
            self.filter = new DtQueryDefaultFilter();
            self.query = new DtNavMeshQuery(self.navMesh);
        }

        [EntitySystem]
        private static void Destroy(this PathfindingComponent self)
        {
            self.Name = string.Empty;
            self.navMesh = null;
        }
        
        public static void Find(this PathfindingComponent self, float3 start, float3 target, List<float3> result)
        {
            if (self.navMesh == null)
            {
                Log.Debug("寻路| Find 失败 pathfinding ptr is zero");
                throw new Exception($"pathfinding ptr is zero: {self.Scene().Name}");
            }

            RcVec3f startPos = new(-start.x, start.y, start.z);
            RcVec3f endPos = new(-target.x, target.y, target.z);

            long startRef;
            long endRef;
            RcVec3f startPt;
            RcVec3f endPt;
            
            self.query.FindNearestPoly(startPos, self.extents, self.filter, out startRef, out startPt, out _);
            self.query.FindNearestPoly(endPos, self.extents, self.filter, out endRef, out endPt, out _);
            
            self.query.FindPath(startRef, endRef, startPt, endPt, self.filter, ref self.polys, new DtFindPathOption(0, float.MaxValue));

            if (0 >= self.polys.Count)
            {
                return;
            }
            
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

            for (int i = 0; i < self.straightPath.Count; ++i)
            {
                RcVec3f pos = self.straightPath[i].pos;
                result.Add(new float3(-pos.x, pos.y, pos.z));
            }
        }
        
        public static float3 FindRandomPointAroundCircle(this PathfindingComponent self, float3 center, float radius)
        {
            if (self.navMesh == null)
            {
                throw new Exception($"pathfinding ptr is zero: {self.Scene().Name}");
            }

            RcVec3f centerPos = new(-center.x, center.y, center.z);
            
            self.query.FindNearestPoly(centerPos, self.extents, self.filter, out long startRef, out RcVec3f startPos, out _);
            
            DtStatus dtStatus = self.query.FindRandomPointAroundCircle(startRef, startPos, radius, self.filter, self.NavmeshRandom, out _, out RcVec3f endPt);
            if (!dtStatus.Succeeded())
            {
                //throw new Exception($"FindRandomPointAroundCircle error: {self.Scene().Name} {dtStatus.Value}");
                startPos = centerPos;
            }

            return new float3(-endPt.x, endPt.y, endPt.z);
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

            RcVec3f startPos = new(-pos.x, pos.y, pos.z);
            self.query.FindNearestPoly(startPos, self.extents, self.filter, out long startRef, out RcVec3f startPt, out _);
            
            return new float3(-startPt[0], startPt[1], startPt[2]);
        }
    }
}