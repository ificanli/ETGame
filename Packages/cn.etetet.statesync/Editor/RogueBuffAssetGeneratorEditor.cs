#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

            List<BuffConfig> configs = BuildBuiltinBuffConfigs();
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

        private static List<BuffConfig> BuildBuiltinBuffConfigs()
        {
            const string fullTypeName = "ET.Server.RogueBuffConfigLoader";
            const string methodName = "BuildBuiltinBuffConfigs";

            Type loaderType = null;
            foreach (Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                loaderType = assembly.GetType(fullTypeName, false);
                if (loaderType != null)
                {
                    break;
                }
            }

            if (loaderType == null)
            {
                throw new System.Exception("未找到 ET.Server.RogueBuffConfigLoader。请先确保当前 CodeMode 包含 Server 代码后再执行该菜单。");
            }

            MethodInfo methodInfo = loaderType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (methodInfo == null)
            {
                throw new System.Exception($"未找到方法: {fullTypeName}.{methodName}");
            }

            if (methodInfo.Invoke(null, null) is not List<BuffConfig> configs)
            {
                throw new System.Exception($"{fullTypeName}.{methodName} 返回值不是 List<BuffConfig>");
            }

            return configs;
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
