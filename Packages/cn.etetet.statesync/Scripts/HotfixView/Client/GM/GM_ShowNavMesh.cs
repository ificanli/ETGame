using System.Collections.Generic;
using DotRecast.Core;
using DotRecast.Detour;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    [GM(EGMType.Test, 2, "显示NavMesh", "切换NavMesh可视化（绿色=可行走区域）")]
    public class GM_ShowNavMesh : IGMCommand
    {
        private const string DebugGoName = "NavMeshDebugView";

        public List<GMParamInfo> GetParams()
        {
            return new();
        }

        public async ETTask<bool> Run(Scene clientScene, ParamVo paramVo)
        {
            // 如果已存在则销毁（切换开关）
            GameObject existing = GameObject.Find(DebugGoName);
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing);
                Log.Info("[NavMeshDebug] visualization removed");
                await ETTask.CompletedTask;
                return false;
            }

            Scene currentScene = clientScene?.CurrentScene();
            if (currentScene == null)
            {
                Log.Warning("[NavMeshDebug] no current scene");
                await ETTask.CompletedTask;
                return false;
            }

            string configName = currentScene.Name.GetSceneConfigName();
            if (configName == "Home")
            {
                Log.Warning("[NavMeshDebug] Home scene has no navmesh");
                await ETTask.CompletedTask;
                return false;
            }

            DtNavMesh navMesh;
            try
            {
                navMesh = NavmeshComponent.Instance.Get(configName);
            }
            catch
            {
                Log.Warning($"[NavMeshDebug] navmesh not loaded: {configName}");
                await ETTask.CompletedTask;
                return false;
            }

            if (navMesh == null)
            {
                await ETTask.CompletedTask;
                return false;
            }

            // 获取玩家位置来检测 navXSign
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            Unity.Mathematics.float3 playerPos = myUnit?.Position ?? Unity.Mathematics.float3.zero;
            int navXSign = DetectNavXSign(navMesh, playerPos);

            // 提取所有多边形三角形
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Color> colors = new List<Color>();

            Color walkableColor = new Color(0f, 1f, 0f, 0.35f);
            Color unwalkableColor = new Color(1f, 0f, 0f, 0.5f);

            int maxTiles = navMesh.GetMaxTiles();
            for (int tileIndex = 0; tileIndex < maxTiles; ++tileIndex)
            {
                DtMeshTile tile = navMesh.GetTile(tileIndex);
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

                    bool walkable = poly.flags != 0;
                    Color color = walkable ? walkableColor : unwalkableColor;
                    float yOffset = 0.1f;

                    for (int j = 1; j < poly.vertCount - 1; ++j)
                    {
                        int v0Idx = poly.verts[0] * 3;
                        int v1Idx = poly.verts[j] * 3;
                        int v2Idx = poly.verts[j + 1] * 3;

                        Vector3 p0 = NavToUnity(tile.data.verts, v0Idx, navXSign, yOffset);
                        Vector3 p1 = NavToUnity(tile.data.verts, v1Idx, navXSign, yOffset);
                        Vector3 p2 = NavToUnity(tile.data.verts, v2Idx, navXSign, yOffset);

                        int baseIdx = verts.Count;
                        verts.Add(p0);
                        verts.Add(p1);
                        verts.Add(p2);
                        colors.Add(color);
                        colors.Add(color);
                        colors.Add(color);
                        // 正面+背面
                        tris.Add(baseIdx);
                        tris.Add(baseIdx + 1);
                        tris.Add(baseIdx + 2);
                        tris.Add(baseIdx + 2);
                        tris.Add(baseIdx + 1);
                        tris.Add(baseIdx);
                    }
                }
            }

            Log.Info($"[NavMeshDebug] scene={configName}, navXSign={navXSign}, triangles={verts.Count / 3}, playerPos={playerPos}");

            if (verts.Count < 3)
            {
                Log.Warning("[NavMeshDebug] no triangles extracted!");
                await ETTask.CompletedTask;
                return false;
            }

            // 创建 Mesh
            Mesh mesh = new Mesh();
            mesh.indexFormat = verts.Count > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.colors = colors.ToArray();
            mesh.RecalculateNormals();

            // 使用 Sprites/Default 支持顶点色+透明
            Material material = new Material(Shader.Find("Sprites/Default"));

            // 创建 GameObject
            GameObject debugGo = new GameObject(DebugGoName);
            MeshFilter meshFilter = debugGo.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer meshRenderer = debugGo.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            Log.Info($"[NavMeshDebug] visualization created: {verts.Count / 3} triangles, use GM again to hide");
            await ETTask.CompletedTask;
            return false;
        }

        private static Vector3 NavToUnity(float[] vertData, int idx, int navXSign, float yOffset)
        {
            return new Vector3(navXSign * vertData[idx], vertData[idx + 1] + yOffset, vertData[idx + 2]);
        }

        private static int DetectNavXSign(DtNavMesh navMesh, Unity.Mathematics.float3 playerPos)
        {
            DtNavMeshQuery query = new DtNavMeshQuery(navMesh);
            IDtQueryFilter filter = new DtQueryDefaultFilter();
            RcVec3f extents = new RcVec3f(15, 10, 15);

            RcVec3f navPos1 = new RcVec3f(-playerPos.x, playerPos.y, playerPos.z);
            query.FindNearestPoly(navPos1, extents, filter, out long ref1, out RcVec3f pt1, out _);
            float dist1 = ref1 != 0
                ? Vector3.Distance(new Vector3(playerPos.x, playerPos.y, playerPos.z), new Vector3(-pt1.x, pt1.y, pt1.z))
                : float.MaxValue;

            RcVec3f navPos2 = new RcVec3f(playerPos.x, playerPos.y, playerPos.z);
            query.FindNearestPoly(navPos2, extents, filter, out long ref2, out RcVec3f pt2, out _);
            float dist2 = ref2 != 0
                ? Vector3.Distance(new Vector3(playerPos.x, playerPos.y, playerPos.z), new Vector3(pt2.x, pt2.y, pt2.z))
                : float.MaxValue;

            int sign = dist1 <= dist2 ? -1 : 1;
            Log.Info($"[NavMeshDebug] DetectNavXSign: playerPos={playerPos}, dist(-1)={dist1:F3}, dist(1)={dist2:F3}, chosen={sign}");
            return sign;
        }
    }
}
