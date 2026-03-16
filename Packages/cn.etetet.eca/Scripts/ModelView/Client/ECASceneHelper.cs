using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// ECA 场景辅助类（客户端）
    /// 用于从 Unity 场景收集 ECA 配置
    /// </summary>
    public static class ECASceneHelper
    {
        /// <summary>
        /// 收集场景中所有 ECA 配置
        /// </summary>
        public static List<ECAConfig> CollectECAConfigs()
        {
            List<ECAConfig> configs = new();

            ECAPointMarker[] markers = GameObject.FindObjectsByType<ECAPointMarker>(FindObjectsSortMode.None);
            Log.Info($"[ECASceneHelper] Found {markers.Length} ECA point markers");

            foreach (ECAPointMarker marker in markers)
            {
                ECAConfig config = marker.GetConfig();
                if (config != null)
                {
                    configs.Add(config);
                }
            }

            return configs;
        }
    }
}
