using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YooAsset.Editor
{
    public class SettingLoader
    {
        private const string MainPackageFileName = "MainPackage.txt";
        private const string AssetBundleCollectorSettingTypeName = "AssetBundleCollectorSetting";

        public static string GetPreferredSettingPath<TSetting>() where TSetting : ScriptableObject
        {
            return GetPreferredSettingPath(typeof(TSetting));
        }

        /// <summary>
        /// 加载相关的配置文件
        /// </summary>
        public static TSetting LoadSettingData<TSetting>() where TSetting : ScriptableObject
        {
            var settingType = typeof(TSetting);
            var guids = AssetDatabase.FindAssets($"t:{settingType.Name}");

            string preferredPath = GetPreferredSettingPath(settingType);
            if (!string.IsNullOrEmpty(preferredPath))
            {
                var preferredSetting = AssetDatabase.LoadAssetAtPath<TSetting>(preferredPath);
                if (preferredSetting != null)
                {
                    return preferredSetting;
                }
            }

            if (guids.Length == 0)
            {
                Debug.LogWarning($"Create new {settingType.Name}.asset");
                var setting = ScriptableObject.CreateInstance<TSetting>();
                string filePath = string.IsNullOrEmpty(preferredPath)
                    ? $"Assets/{settingType.Name}.asset"
                    : preferredPath;
                EnsureAssetFolder(filePath);
                AssetDatabase.CreateAsset(setting, filePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return setting;
            }

            if (!string.IsNullOrEmpty(preferredPath) && guids.Length > 1)
            {
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.Equals(path, preferredPath, StringComparison.OrdinalIgnoreCase))
                    {
                        var setting = AssetDatabase.LoadAssetAtPath<TSetting>(path);
                        if (setting != null)
                        {
                            return setting;
                        }

                        break;
                    }
                }

                Debug.LogWarning($"Can not find preferred setting in main package path : {preferredPath}");
            }

            if (guids.Length != 1)
            {
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Debug.LogWarning($"Found multiple file : {path}");
                }

                throw new Exception($"Found multiple {settingType.Name} files !");
            }

            string filePathFallback = AssetDatabase.GUIDToAssetPath(guids[0]);
            var fallbackSetting = AssetDatabase.LoadAssetAtPath<TSetting>(filePathFallback);
            return fallbackSetting;
        }

        private static string GetPreferredSettingPath(Type settingType)
        {
            if (settingType.Name != AssetBundleCollectorSettingTypeName)
            {
                return null;
            }

            string packageName = GetMainPackageName();
            if (string.IsNullOrEmpty(packageName))
            {
                return null;
            }

            return $"Packages/{packageName}/Settings/{settingType.Name}.asset";
        }

        private static string GetMainPackageName()
        {
            string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string mainPackagePath = Path.Combine(projectPath, MainPackageFileName);
            if (!File.Exists(mainPackagePath))
            {
                Debug.LogWarning($"Can not find {MainPackageFileName} : {mainPackagePath}");
                return null;
            }

            foreach (string line in File.ReadLines(mainPackagePath))
            {
                string packageName = line?.Trim();
                if (string.IsNullOrEmpty(packageName))
                {
                    continue;
                }

                return packageName.TrimStart('\uFEFF');
            }

            Debug.LogWarning($"{MainPackageFileName} is empty : {mainPackagePath}");
            return null;
        }

        private static void EnsureAssetFolder(string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            if (parts.Length == 0)
            {
                return;
            }

            string currentPath = parts[0];
            if (!AssetDatabase.IsValidFolder(currentPath))
            {
                return;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}
