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

            List<ECAConfig> configs = ECASceneHelper.CollectECAConfigs();
            if (configs.Count == 0)
            {
                Debug.LogWarning($"[ExportECAConfig] No ECAPointMarker found in scene '{sceneName}'");
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
    }
}
