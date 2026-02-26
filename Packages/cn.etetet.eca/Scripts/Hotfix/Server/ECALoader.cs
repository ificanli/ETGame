using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public static class ECALoader
    {
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
        }
    }
}
