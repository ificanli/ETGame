using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;

namespace ET.Server
{
    public static class ECALoader
    {
        /// <summary>
        /// 从导出的配置文件加载 ECA 点
        /// </summary>
        public static void LoadFromFile(Scene scene, string mapName)
        {
            string path = $"Packages/cn.etetet.map/Bundles/ECA/{mapName}.txt";
            if (!File.Exists(path))
            {
                return;
            }

            string json = File.ReadAllText(path);
            List<ECAConfig> configs = MongoHelper.FromJson<List<ECAConfig>>(json);
            if (configs == null || configs.Count == 0)
            {
                return;
            }

            Log.Info($"[ECALoader] Loading ECA config from file: {path}");
            LoadECAPoints(scene, configs);
        }

        public static void LoadECAPoints(Scene scene, List<ECAConfig> configs)
        {
            ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>();
            if (ecaManager == null)
            {
                ecaManager = scene.AddComponent<ECAManagerComponent>();
            }

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                Log.Error($"Scene {scene.Name} has no UnitComponent!");
                return;
            }

            Log.Info($"[ECALoader] Loading {configs.Count} ECA points");

            foreach (ECAConfig config in configs)
            {
                if (config == null) continue;

                Unit ecaUnit = unitComponent.AddChild<Unit, int>(0);
                ecaUnit.Position = new float3(config.PosX, config.PosY, config.PosZ);

                ecaUnit.AddComponent<ECAPointComponent, string, int, float>(
                    config.ConfigId,
                    config.PointType,
                    config.InteractRange
                );

                ecaManager.AddECAPoint(config.ConfigId, ecaUnit.Id);

                Log.Info($"[ECALoader] Loaded ECA point: {config.ConfigId}, Type: {config.PointType}, Pos: ({config.PosX}, {config.PosY}, {config.PosZ})");
            }

            Log.Info($"[ECALoader] Total loaded {ecaManager.ECAPoints.Count} ECA points");

            // 启动范围检测定时器（200ms 间隔）
            TimerComponent timerComponent = scene.TimerComponent;
            if (timerComponent != null && ecaManager.CheckRangeTimerId == 0)
            {
                ecaManager.CheckRangeTimerId = timerComponent.NewRepeatedTimer(200, TimerInvokeType.ECACheckRange, ecaManager);
            }
        }
    }
}
