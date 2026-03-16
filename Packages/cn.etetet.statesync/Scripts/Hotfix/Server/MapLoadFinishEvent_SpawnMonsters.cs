using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 地图加载完成后刷怪的事件处理器
    /// 遍历所有刷怪点（MonsterSpawnPoint），触发OnMapLoaded事件执行Flow Graph
    /// </summary>
    [Event(SceneType.Map)]
    public class MapLoadFinishEvent_SpawnMonsters : AEvent<Scene, MapLoadFinishEvent>
    {
        protected override ETTask Run(Scene scene, MapLoadFinishEvent args)
        {
            Scene mapScene = args.Scene;
            Log.Info($"[MapLoadFinish] Event triggered: scene={mapScene?.Name}");

            if (mapScene == null || mapScene.IsDisposed)
            {
                Log.Warning($"[MapLoadFinish] Scene is null or disposed");
                return ETTask.CompletedTask;
            }

            string mapName = mapScene.Name.GetSceneConfigName();
            Log.Info($"[MapLoadFinish] Processing map: {mapName}");

            // 获取 ECA 管理器
            ECAManagerComponent ecaManager = mapScene.GetComponent<ECAManagerComponent>();
            if (ecaManager == null)
            {
                Log.Warning($"[MapLoadFinish] No ECAManagerComponent found: scene={mapScene.Name}, map={mapName}");
                return ETTask.CompletedTask;
            }

            // 获取所有 ECA 点
            List<ECAPointComponent> allPoints = ecaManager.GetAllECAPoints();
            Log.Info($"[MapLoadFinish] Total ECA points: {allPoints?.Count ?? 0}");

            int spawnPointCount = 0;
            int flowTriggeredCount = 0;

            // 遍历所有刷怪点
            foreach (ECAPointComponent point in allPoints)
            {
                if (point == null || point.IsDisposed)
                {
                    continue;
                }

                // 只处理刷怪点类型
                if (point.PointType != ECAPointType.MonsterSpawnPoint)
                {
                    continue;
                }

                spawnPointCount++;
                Log.Info($"[MapLoadFinish] Found MonsterSpawnPoint: pointId={point.PointId}, hasFlowGraph={point.FlowGraph != null}");

                // 触发 OnMapLoaded 事件，执行 Flow Graph
                if (point.FlowGraph != null)
                {
                    Log.Info($"[MapLoadFinish] Triggering OnMapLoaded event for point: {point.PointId}");
                    ECAFlowGraphHelper.TriggerEvent(point, null, ECAFlowEventType.OnMapLoaded);
                    flowTriggeredCount++;
                    Log.Info($"[MapLoadFinish] OnMapLoaded event triggered successfully: pointId={point.PointId}");
                }
                else
                {
                    Log.Warning($"[MapLoadFinish] Point {point.PointId} has no FlowGraph configured");
                }
            }

            Log.Info($"[MapLoadFinish] Map spawn completed: scene={mapScene.Name}, map={mapName}, spawnPoints={spawnPointCount}, flowTriggered={flowTriggeredCount}");
            return ETTask.CompletedTask;
        }
    }
}
