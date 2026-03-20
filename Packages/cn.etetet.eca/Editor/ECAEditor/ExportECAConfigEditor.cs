using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ET.Client
{
    public static class ExportECAConfigEditor
    {
        private const string ExportPath = "Packages/cn.etetet.map/Bundles/ECA";

        [MenuItem("ET/ECA/Export ECA Config")]
        public static void ExportECAConfig()
        {
            MongoRegister.Init();

            string sceneName = EditorSceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[ExportECAConfig] No active scene!");
                return;
            }

            ECAPointMarker[] markers = GameObject.FindObjectsByType<ECAPointMarker>(FindObjectsSortMode.None);
            if (!ECAFlowGraphAssetMigrationUtility.TryPrepareReferencedFlowGraphs(markers, out string graphErrorMessage))
            {
                Debug.LogError($"[ExportECAConfig] FlowGraph validation failed in scene '{sceneName}':\n{graphErrorMessage}");
                EditorUtility.DisplayDialog("Export ECA Config", $"场景 {sceneName} 的 FlowGraph 存在无效节点，请先修复后再导出：\n\n{graphErrorMessage}", "知道了");
                return;
            }

            List<ECAConfig> configs = ECASceneHelper.CollectECAConfigs();
            if (configs.Count == 0)
            {
                Debug.LogWarning($"[ExportECAConfig] No ECAPointMarker found in scene '{sceneName}'");
                return;
            }

            if (TryGetDuplicateConfigIds(configs, out List<string> duplicateConfigIds))
            {
                string duplicateList = string.Join("\n", duplicateConfigIds);
                string message = $"场景 {sceneName} 存在重复的 ConfigId：\n{duplicateList}\n\n请先修改为唯一值后再导出。";

                Debug.LogError($"[ExportECAConfig] Duplicate ConfigId found in scene '{sceneName}': {string.Join(", ", duplicateConfigIds)}");
                EditorUtility.DisplayDialog("Export ECA Config", message, "知道了");
                return;
            }

            if (!Directory.Exists(ExportPath))
            {
                Directory.CreateDirectory(ExportPath);
            }

            string json = MongoHelper.ToJson(configs, MongoHelper.ConfigSettings);
            string filePath = Path.Combine(ExportPath, $"{sceneName}.txt");
            File.WriteAllText(filePath, json);

            Debug.Log($"[ExportECAConfig] Exported {configs.Count} ECA points for scene '{sceneName}' to {filePath}");
        }

        private static bool TryGetDuplicateConfigIds(List<ECAConfig> configs, out List<string> duplicateConfigIds)
        {
            duplicateConfigIds = new List<string>();
            if (configs == null || configs.Count == 0)
            {
                return false;
            }

            HashSet<string> seenConfigIds = new(System.StringComparer.Ordinal);
            HashSet<string> duplicateSet = new(System.StringComparer.Ordinal);

            foreach (ECAConfig config in configs)
            {
                if (config == null || string.IsNullOrWhiteSpace(config.ConfigId))
                {
                    continue;
                }

                string configId = config.ConfigId.Trim();
                if (!seenConfigIds.Add(configId))
                {
                    duplicateSet.Add(configId);
                }
            }

            if (duplicateSet.Count == 0)
            {
                return false;
            }

            duplicateConfigIds.AddRange(duplicateSet);
            duplicateConfigIds.Sort(System.StringComparer.Ordinal);
            return true;
        }
    }
}
