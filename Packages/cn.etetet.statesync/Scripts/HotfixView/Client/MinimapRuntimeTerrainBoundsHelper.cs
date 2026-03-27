using UnityEngine;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace ET.Client
{
    /// <summary>
    /// 从当前 Unity 场景中的 Terrain 自动解析小地图世界边界。
    /// </summary>
    public static class MinimapRuntimeTerrainBoundsHelper
    {
        public static void RefreshWorldBoundsFromTerrain(this MinimapRuntimeComponent runtime)
        {
            if (runtime == null || runtime.IsDisposed)
            {
                return;
            }

            ++runtime.WorldBoundsAutoResolveRetryCount;
            runtime.WorldBoundsAutoResolveAttempted = true;

            if (TryGetCurrentSceneTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ))
            {
                runtime.UpdateWorldBounds(minX, maxX, minZ, maxZ, true);
                Log.Info(
                    $"[MinimapRuntime] map={runtime.MapName}, world bounds resolved from terrain, min=({minX:F2},{minZ:F2}), max=({maxX:F2},{maxZ:F2})");
                return;
            }

            runtime.ApplyConfiguredWorldBounds();
        }

        private static bool TryGetCurrentSceneTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = 0f;
            maxX = 0f;
            minZ = 0f;
            maxZ = 0f;

            UnityScene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return false;
            }

            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains == null || terrains.Length == 0)
            {
                return false;
            }

            bool found = false;
            foreach (Terrain terrain in terrains)
            {
                if (terrain == null || terrain.terrainData == null || !terrain.isActiveAndEnabled)
                {
                    continue;
                }

                if (terrain.gameObject == null || terrain.gameObject.scene.handle != activeScene.handle)
                {
                    continue;
                }

                Vector3 terrainOrigin = terrain.GetPosition();
                Vector3 terrainSize = terrain.terrainData.size;
                if (terrainSize.x <= 0f || terrainSize.z <= 0f)
                {
                    continue;
                }

                float terrainMinX = terrainOrigin.x;
                float terrainMaxX = terrainOrigin.x + terrainSize.x;
                float terrainMinZ = terrainOrigin.z;
                float terrainMaxZ = terrainOrigin.z + terrainSize.z;

                if (!found)
                {
                    minX = terrainMinX;
                    maxX = terrainMaxX;
                    minZ = terrainMinZ;
                    maxZ = terrainMaxZ;
                    found = true;
                    continue;
                }

                minX = Mathf.Min(minX, terrainMinX);
                maxX = Mathf.Max(maxX, terrainMaxX);
                minZ = Mathf.Min(minZ, terrainMinZ);
                maxZ = Mathf.Max(maxZ, terrainMaxZ);
            }

            return found;
        }
    }
}
