using System;
using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Detour;

namespace ET.Server
{
    /// <summary>
    /// 将 ECA 点位状态映射到场景导航多边形阻挡。
    /// </summary>
    public static class ECAPointNavBlockHelper
    {
        public static void Rebuild(Scene scene)
        {
            if (scene == null || scene.IsDisposed)
            {
                return;
            }

            SceneNavmeshComponent sceneNavmeshComponent = scene.GetComponent<SceneNavmeshComponent>();
            ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>();
            if (sceneNavmeshComponent?.NavMesh == null || ecaManager == null)
            {
                return;
            }

            ECAPointNavBlockComponent navBlockComponent = scene.GetComponent<ECAPointNavBlockComponent>();
            if (navBlockComponent == null)
            {
                navBlockComponent = scene.AddComponent<ECAPointNavBlockComponent>();
            }

            navBlockComponent.PointPolyRefs.Clear();
            navBlockComponent.OriginalPolyFlags.Clear();
            navBlockComponent.PolyBlockRefCounts.Clear();
            navBlockComponent.AppliedBlockedPointIds.Clear();

            DtNavMeshQuery query = new(sceneNavmeshComponent.NavMesh);
            DtQueryDefaultFilter filter = new();
            foreach (ECAPointComponent point in ecaManager.GetAllECAPoints())
            {
                if (!ShouldManagePoint(point))
                {
                    continue;
                }

                if (!TryGetHalfExtents(point, out RcVec3f halfExtents))
                {
                    continue;
                }

                Unit pointUnit = point.GetParent<Unit>();
                if (pointUnit == null)
                {
                    continue;
                }

                RcVec3f center = new(-pointUnit.Position.x, pointUnit.Position.y, pointUnit.Position.z);
                PolyRefCollectQuery collector = new();
                DtStatus status = query.QueryPolygons(center, halfExtents, filter, collector);
                if (status.Failed() || collector.PolyRefs.Count == 0)
                {
                    Log.Warning($"[ECANavBlock] query poly refs failed or empty: point={point.PointId}, status={status.Value}, pos={pointUnit.Position}, extents={halfExtents}");
                    continue;
                }

                navBlockComponent.PointPolyRefs[point.PointId] = new List<long>(collector.PolyRefs);
            }

            foreach (ECAPointComponent point in ecaManager.GetAllECAPoints())
            {
                RefreshPointState(scene, point.PointId);
            }
        }

        public static void RefreshPointState(Scene scene, string pointId)
        {
            if (scene == null || scene.IsDisposed || string.IsNullOrWhiteSpace(pointId))
            {
                return;
            }

            ECAPointNavBlockComponent navBlockComponent = scene.GetComponent<ECAPointNavBlockComponent>();
            SceneNavmeshComponent sceneNavmeshComponent = scene.GetComponent<SceneNavmeshComponent>();
            ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>();
            if (navBlockComponent == null || sceneNavmeshComponent?.NavMesh == null || ecaManager == null)
            {
                return;
            }

            ECAPointComponent point = ecaManager.GetECAPoint(pointId);
            if (point == null || point.IsDisposed)
            {
                RemoveBlockedPoint(navBlockComponent, sceneNavmeshComponent.NavMesh, pointId);
                return;
            }

            if (ShouldBlockByCurrentState(point))
            {
                ApplyBlockedPoint(navBlockComponent, sceneNavmeshComponent.NavMesh, pointId);
                return;
            }

            RemoveBlockedPoint(navBlockComponent, sceneNavmeshComponent.NavMesh, pointId);
        }

        private static bool ShouldManagePoint(ECAPointComponent point)
        {
            return point != null && !point.IsDisposed && IsNavBlockEnabled(point);
        }

        private static bool IsNavBlockEnabled(ECAPointComponent point)
        {
            if (point == null)
            {
                return false;
            }

            if (FlowParamHelper.TryGetBoolParam(point.Params, ECAPointParamKey.NavBlockEnabled, out bool enabled))
            {
                return enabled;
            }

            return point.PointType == ECAPointType.Door || point.PointType == ECAPointType.KeyDoor;
        }

        private static bool TryGetHalfExtents(ECAPointComponent point, out RcVec3f halfExtents)
        {
            halfExtents = RcVec3f.Zero;
            if (!IsNavBlockEnabled(point))
            {
                return false;
            }

            float halfX = ECAConfig.DefaultNavBlockHalfExtentsX;
            float halfY = ECAConfig.DefaultNavBlockHalfExtentsY;
            float halfZ = ECAConfig.DefaultNavBlockHalfExtentsZ;

            if (FlowParamHelper.TryGetFloatParam(point.Params, ECAPointParamKey.NavBlockHalfExtentsX, out float configuredHalfX) && configuredHalfX > 0f)
            {
                halfX = configuredHalfX;
            }

            if (FlowParamHelper.TryGetFloatParam(point.Params, ECAPointParamKey.NavBlockHalfExtentsY, out float configuredHalfY) && configuredHalfY > 0f)
            {
                halfY = configuredHalfY;
            }

            if (FlowParamHelper.TryGetFloatParam(point.Params, ECAPointParamKey.NavBlockHalfExtentsZ, out float configuredHalfZ) && configuredHalfZ > 0f)
            {
                halfZ = configuredHalfZ;
            }

            halfExtents = new RcVec3f(halfX, halfY, halfZ);
            return true;
        }

        private static bool ShouldBlockByCurrentState(ECAPointComponent point)
        {
            if (!IsNavBlockEnabled(point))
            {
                return false;
            }

            if (TryParseBlockedStates(point, out HashSet<int> blockedStates))
            {
                return blockedStates.Contains(point.CurrentState);
            }

            return point.PointType switch
            {
                ECAPointType.Door => point.CurrentState == ECADoorState.Closed,
                ECAPointType.KeyDoor => point.CurrentState == ECADoorState.Locked || point.CurrentState == ECADoorState.Closed,
                _ => false
            };
        }

        private static bool TryParseBlockedStates(ECAPointComponent point, out HashSet<int> blockedStates)
        {
            blockedStates = null;
            string raw = FlowParamHelper.GetParamValue(point?.Params, ECAPointParamKey.NavBlockStates);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            blockedStates = new HashSet<int>();
            string[] tokens = raw.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                if (int.TryParse(token.Trim(), out int state))
                {
                    blockedStates.Add(state);
                }
            }

            return blockedStates.Count > 0;
        }

        private static void ApplyBlockedPoint(ECAPointNavBlockComponent navBlockComponent, DtNavMesh navMesh, string pointId)
        {
            if (navBlockComponent.AppliedBlockedPointIds.Contains(pointId))
            {
                return;
            }

            if (!navBlockComponent.PointPolyRefs.TryGetValue(pointId, out List<long> polyRefs) || polyRefs.Count == 0)
            {
                Log.Warning($"[ECANavBlock] point has no mapped poly refs: point={pointId}");
                return;
            }

            foreach (long polyRef in polyRefs)
            {
                if (!navBlockComponent.OriginalPolyFlags.TryGetValue(polyRef, out int originalFlags))
                {
                    DtStatus getStatus = navMesh.GetPolyFlags(polyRef, out originalFlags);
                    if (getStatus.Failed())
                    {
                        Log.Warning($"[ECANavBlock] get poly flags failed: point={pointId}, polyRef={polyRef}, status={getStatus.Value}");
                        continue;
                    }

                    navBlockComponent.OriginalPolyFlags[polyRef] = originalFlags;
                }

                int refCount = 0;
                navBlockComponent.PolyBlockRefCounts.TryGetValue(polyRef, out refCount);
                refCount++;
                navBlockComponent.PolyBlockRefCounts[polyRef] = refCount;

                if (refCount == 1)
                {
                    navMesh.SetPolyFlags(polyRef, originalFlags | NavmeshRuntimeFlag.SceneBlocked);
                }
            }

            navBlockComponent.AppliedBlockedPointIds.Add(pointId);
        }

        private static void RemoveBlockedPoint(ECAPointNavBlockComponent navBlockComponent, DtNavMesh navMesh, string pointId)
        {
            if (!navBlockComponent.AppliedBlockedPointIds.Contains(pointId))
            {
                return;
            }

            if (!navBlockComponent.PointPolyRefs.TryGetValue(pointId, out List<long> polyRefs) || polyRefs.Count == 0)
            {
                navBlockComponent.AppliedBlockedPointIds.Remove(pointId);
                return;
            }

            foreach (long polyRef in polyRefs)
            {
                if (!navBlockComponent.PolyBlockRefCounts.TryGetValue(polyRef, out int refCount))
                {
                    continue;
                }

                refCount--;
                if (refCount > 0)
                {
                    navBlockComponent.PolyBlockRefCounts[polyRef] = refCount;
                    continue;
                }

                navBlockComponent.PolyBlockRefCounts.Remove(polyRef);
                if (navBlockComponent.OriginalPolyFlags.TryGetValue(polyRef, out int originalFlags))
                {
                    navMesh.SetPolyFlags(polyRef, originalFlags);
                }
            }

            navBlockComponent.AppliedBlockedPointIds.Remove(pointId);
        }
    }
}
