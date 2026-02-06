using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Hibzz.DependencyResolver
{
    /// <summary>
    /// 主包选择器 - 右键菜单设置主包并收集依赖
    /// </summary>
    public static class MainPackageSelector
    {
        private const string OutputFileName = "MainPackage.txt";

        #region MenuItem

        /// <summary>
        /// 右键菜单：设置为主包
        /// </summary>
        [MenuItem("Assets/ET/Set As Main Package", false, 100)]
        private static void SetAsMainPackage()
        {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[MainPackageSelector] 未选中任何文件");
                return;
            }

            string packageName = GetPackageNameFromPath(assetPath);
            if (string.IsNullOrEmpty(packageName))
            {
                Debug.LogError($"[MainPackageSelector] 无法从路径获取包名: {assetPath}");
                return;
            }

            Debug.Log($"[MainPackageSelector] 设置主包: {packageName}");

            // 递归收集所有依赖
            HashSet<string> allDependencies = new HashSet<string>();
            CollectDependenciesRecursive(packageName, allDependencies);

            // 从依赖列表中移除主包自身
            allDependencies.Remove(packageName);

            Debug.Log($"[MainPackageSelector] 收集到 {allDependencies.Count} 个依赖包");

            // 写入文件
            WriteMainPackageFile(packageName, allDependencies);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 验证菜单项是否可用
        /// </summary>
        [MenuItem("Assets/ET/Set As Main Package", true)]
        private static bool ValidateSetAsMainPackage()
        {
            if (Selection.activeObject == null)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            return IsValidPackage(assetPath);
        }

        #endregion

        #region 验证

        /// <summary>
        /// 验证选中的是否为 cn.etetet.* 包目录或其 package.json
        /// </summary>
        private static bool IsValidPackage(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            // 统一使用正斜杠处理
            string normalizedPath = assetPath.Replace('\\', '/');

            // 支持两种格式:
            // 1. 包目录: Packages/cn.etetet.xxx
            // 2. package.json: Packages/cn.etetet.xxx/package.json
            if (Regex.IsMatch(normalizedPath, @"^Packages/cn\.etetet\.[^/]+$"))
                return true;

            if (Regex.IsMatch(normalizedPath, @"^Packages/cn\.etetet\.[^/]+/package\.json$"))
                return true;

            return false;
        }

        #endregion

        #region 包名获取

        /// <summary>
        /// 从路径获取包名（支持包目录或 package.json）
        /// </summary>
        private static string GetPackageNameFromPath(string assetPath)
        {
            string normalizedPath = assetPath.Replace('\\', '/');

            // 匹配包目录: Packages/cn.etetet.xxx
            Match match = Regex.Match(normalizedPath, @"^Packages/(cn\.etetet\.[^/]+)(?:/package\.json)?$");
            if (match.Success)
            {
                string packageDirName = match.Groups[1].Value;

                // 处理可能带版本号的情况
                int atIndex = packageDirName.IndexOf('@');
                if (atIndex > 0)
                {
                    return packageDirName.Substring(0, atIndex);
                }
                return packageDirName;
            }

            return null;
        }

        #endregion

        #region 依赖收集

        /// <summary>
        /// 递归收集所有依赖
        /// </summary>
        private static void CollectDependenciesRecursive(string packageName, HashSet<string> collected)
        {
            if (collected.Contains(packageName))
                return;

            collected.Add(packageName);

            List<string> dependencies = GetPackageDependencies(packageName);
            foreach (string dep in dependencies)
            {
                if (dep.StartsWith("cn.etetet."))
                {
                    CollectDependenciesRecursive(dep, collected);
                }
            }
        }

        /// <summary>
        /// 获取包的依赖列表
        /// </summary>
        private static List<string> GetPackageDependencies(string packageName)
        {
            List<string> result = new List<string>();

            string packagePath = Path.Combine("Packages", packageName, "package.json");
            if (!File.Exists(packagePath))
            {
                return result;
            }

            try
            {
                string json = File.ReadAllText(packagePath);
                result = ParseDependencies(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MainPackageSelector] 解析依赖失败 {packagePath}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 解析 package.json 中的 dependencies
        /// </summary>
        private static List<string> ParseDependencies(string json)
        {
            List<string> result = new List<string>();

            // 使用正则表达式匹配 dependencies 块中的包名
            // 匹配 "dependencies": { "包名": "版本", ... }
            Match dependenciesMatch = Regex.Match(json, @"""dependencies""\s*:\s*\{([^}]*)\}", RegexOptions.Singleline);
            if (!dependenciesMatch.Success)
            {
                return result;
            }

            string dependenciesContent = dependenciesMatch.Groups[1].Value;

            // 匹配每个依赖项的包名
            MatchCollection packageMatches = Regex.Matches(dependenciesContent, @"""([^""]+)""\s*:\s*""[^""]*""");
            foreach (Match match in packageMatches)
            {
                result.Add(match.Groups[1].Value);
            }

            return result;
        }

        #endregion

        #region 文件输出

        /// <summary>
        /// 写入 MainPackage.txt 文件
        /// </summary>
        private static void WriteMainPackageFile(string mainPackage, HashSet<string> dependencies)
        {
            List<string> lines = new List<string>();

            // 第一行是主包
            lines.Add(mainPackage);

            // 后续行是依赖包（按字母排序）
            lines.AddRange(dependencies.OrderBy(x => x));

            // 写入文件
            string outputPath = Path.Combine(Application.dataPath, "..", OutputFileName);
            outputPath = Path.GetFullPath(outputPath);

            File.WriteAllLines(outputPath, lines);

            Debug.Log($"[MainPackageSelector] 已保存到: {outputPath}");
        }

        #endregion
    }
}
