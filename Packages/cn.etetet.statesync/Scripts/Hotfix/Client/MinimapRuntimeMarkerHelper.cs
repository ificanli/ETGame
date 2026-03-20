using System;
using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// 小地图 marker 运行时辅助逻辑。
    /// 负责把当前可见单位同步到小地图缓存中。
    /// </summary>
    public static class MinimapRuntimeMarkerHelper
    {
        public static void SyncUnit(MinimapRuntimeComponent runtime, Unit unit)
        {
            if (!ShouldTrackUnit(runtime, unit))
            {
                RemoveMarker(runtime, unit?.Id ?? 0);
                return;
            }

            long now = TimeInfo.Instance.ClientNow();
            runtime.Markers[unit.Id] = new MinimapMarkerRuntime()
            {
                UnitId = unit.Id,
                ConfigId = unit.ConfigId,
                UnitType = unit.UnitType,
                Position = unit.Position,
                Forward = unit.Forward,
                IsVisible = true,
                LastSeenTime = now,
            };
        }

        public static void SyncPosition(MinimapRuntimeComponent runtime, Unit unit)
        {
            if (!ShouldTrackUnit(runtime, unit))
            {
                RemoveMarker(runtime, unit?.Id ?? 0);
                return;
            }

            if (!TryGetMarker(runtime, unit, out MinimapMarkerRuntime marker))
            {
                return;
            }

            marker.Position = unit.Position;
            marker.IsVisible = true;
            marker.LastSeenTime = TimeInfo.Instance.ClientNow();
            runtime.Markers[unit.Id] = marker;
        }

        public static void SyncForward(MinimapRuntimeComponent runtime, Unit unit)
        {
            if (!ShouldTrackUnit(runtime, unit))
            {
                RemoveMarker(runtime, unit?.Id ?? 0);
                return;
            }

            if (!TryGetMarker(runtime, unit, out MinimapMarkerRuntime marker))
            {
                return;
            }

            marker.Forward = unit.Forward;
            marker.IsVisible = true;
            marker.LastSeenTime = TimeInfo.Instance.ClientNow();
            runtime.Markers[unit.Id] = marker;
        }

        public static void RemoveMarker(MinimapRuntimeComponent runtime, long unitId)
        {
            if (runtime == null || unitId == 0)
            {
                return;
            }

            runtime.Markers.Remove(unitId);
        }

        public static float2 WorldToCompactLocalPosition(MinimapRuntimeComponent runtime, float3 centerPosition, float3 worldPosition)
        {
            float range = math.max(runtime?.CompactRange ?? 0f, 0.01f);
            float offsetX = math.clamp((worldPosition.x - centerPosition.x) / range, -1f, 1f);
            float offsetY = math.clamp((worldPosition.z - centerPosition.z) / range, -1f, 1f);
            return new float2(offsetX, offsetY);
        }

        public static bool TryWorldToCompactLocalPosition(
            MinimapRuntimeComponent runtime,
            float3 centerPosition,
            float3 worldPosition,
            out float2 localPosition)
        {
            localPosition = float2.zero;
            float range = runtime?.CompactRange ?? 0f;
            if (range <= 0f)
            {
                return false;
            }

            float offsetX = (worldPosition.x - centerPosition.x) / range;
            float offsetY = (worldPosition.z - centerPosition.z) / range;
            if (math.abs(offsetX) > 1f || math.abs(offsetY) > 1f)
            {
                return false;
            }

            localPosition = new float2(offsetX, offsetY);
            return true;
        }

        public static bool ShouldDisplayMarker(MinimapRuntimeComponent runtime, MinimapMarkerRuntime marker)
        {
            if (runtime == null || !marker.IsVisible)
            {
                return false;
            }

            return !ShouldHideRemotePlayer(runtime, marker.UnitId, marker.UnitType);
        }

        private static bool TryGetMarker(MinimapRuntimeComponent runtime, Unit unit, out MinimapMarkerRuntime marker)
        {
            marker = default;
            if (!ShouldTrackUnit(runtime, unit))
            {
                return false;
            }

            if (runtime.Markers.TryGetValue(unit.Id, out marker))
            {
                return true;
            }

            SyncUnit(runtime, unit);
            return runtime.Markers.TryGetValue(unit.Id, out marker);
        }

        private static bool ShouldTrackUnit(MinimapRuntimeComponent runtime, Unit unit)
        {
            if (runtime == null || unit == null || unit.IsDisposed)
            {
                return false;
            }

            return !ShouldHideRemotePlayer(runtime, unit.Id, unit.UnitType);
        }

        private static bool ShouldHideRemotePlayer(MinimapRuntimeComponent runtime, long unitId, UnitType unitType)
        {
            if (runtime == null ||
                unitType != UnitType.Player ||
                unitId == 0 ||
                unitId == runtime.MyUnitId)
            {
                return false;
            }

            return string.Equals(runtime.MapName, "SDCMap", StringComparison.OrdinalIgnoreCase);
        }
    }
}
