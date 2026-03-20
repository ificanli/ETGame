using System.Collections.Generic;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    [GM(EGMType.Test, 1, "场景迷雾半径", "运行时调试当前地图的场景迷雾半径")]
    public class GM_SceneFogRadius : IGMCommand
    {
        public List<GMParamInfo> GetParams()
        {
            return new()
            {
                new GMParamInfo(EGMParamType.Float, "半径(世界单位)", "8"),
            };
        }

        public async ETTask<bool> Run(Scene clientScene, ParamVo paramVo)
        {
            float radius = Mathf.Max(0f, paramVo.Get<float>(0));
            Scene currentScene = clientScene?.CurrentScene();
            MinimapRuntimeComponent runtime = currentScene?.GetComponent<MinimapRuntimeComponent>();
            if (runtime == null)
            {
                Log.Warning("[SceneFogGM] MinimapRuntimeComponent missing");
                await ETTask.CompletedTask;
                return false;
            }

            runtime.FogVisionRadius = radius;
            runtime.RefreshLocalFog();

            SceneFogComponent sceneFog = currentScene.GetComponent<SceneFogComponent>();
            if (sceneFog != null)
            {
                sceneFog.LastRefreshTime = float.MinValue;
                sceneFog.LastDiagnosticLogTime = 0;
                sceneFog.LastDiagnosticSignature = string.Empty;
            }

            Log.Info(
                $"[SceneFogGM] map={runtime.MapName}, set fog radius to {runtime.FogVisionRadius:F1}, visibleCells={runtime.CurrentVisibleCells.Count}, exploredCells={runtime.ExploredCells.Count}");

            await ETTask.CompletedTask;
            return false;
        }
    }
}
