#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using ET.Server;
using UnityEditor;
using UnityEngine;

namespace ET.Client
{
    public static class RogueBuffAssetGeneratorEditor
    {
        private const string OutputFolder = "Packages/cn.etetet.statesync/Assets/BT/RogueBuffs";

        [MenuItem("ET/Rogue/Generate Missing Buff BT Assets")]
        private static void GenerateMissingAssets()
        {
            GenerateAssets();
        }

        private static void GenerateAssets()
        {
            MongoRegister.Init();
            EnsureOutputFolder();

            List<BuffConfig> configs = RogueBuffConfigLoader.BuildBuiltinBuffConfigs();
            int createdCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;

            foreach (BuffConfig config in configs)
            {
                string assetPath = $"{OutputFolder}/{config.Id}.asset";
                BuffScriptableObject asset = AssetDatabase.LoadAssetAtPath<BuffScriptableObject>(assetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<BuffScriptableObject>();
                    asset.name = config.Id.ToString();
                    asset.BuffConfig = config;
                    AssetDatabase.CreateAsset(asset, assetPath);
                    createdCount++;
                    continue;
                }

                skippedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ExportScriptableObjectEditor.ExportScriptableObject();

            Debug.Log($"Rogue buff BT assets generated. created={createdCount}, updated={updatedCount}, skipped={skippedCount}, folder={OutputFolder}");
        }

        private static void EnsureOutputFolder()
        {
            if (Directory.Exists(OutputFolder))
            {
                return;
            }

            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();
        }
    }
}

#endif
