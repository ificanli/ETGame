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
    /// 主包选择器 - 按包名设置主包并收集依赖
    /// </summary>
    public static class MainPackageSelector
    {
        private const string OutputFileName = "MainPackage.txt";

        /// <summary>
        /// 按包名设置主包并收集依赖
        /// </summary>
        public static bool SetAsMainPackage(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                Debug.LogError("[MainPackageSelector] 包名为空");
                return false;
            }

            if (!packageName.StartsWith("cn.etetet."))
            {
                Debug.LogError($"[MainPackageSelector] 非法包名: {packageName}");
                return false;
            }

            if (!TryResolvePackageName(packageName, out string resolvedPackageName, out string packageJsonPath))
            {
                Debug.LogError($"[MainPackageSelector] 未找到包配置: {packageJsonPath}");
                return false;
            }

            Debug.Log($"[MainPackageSelector] 设置主包: {resolvedPackageName}");

            // 收集主包直接依赖（不递归）
            HashSet<string> allDependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string dep in GetPackageDependencies(resolvedPackageName))
            {
                if (dep.StartsWith("cn.etetet."))
                {
                    allDependencies.Add(dep);
                }
            }

            // 从依赖列表中移除主包自身
            allDependencies.Remove(resolvedPackageName);

            Debug.Log($"[MainPackageSelector] 收集到 {allDependencies.Count} 个依赖包");

            // 写入文件
            WriteMainPackageFile(resolvedPackageName, allDependencies);

            AssetDatabase.Refresh();

            return true;
        }

        #region 依赖收集

        private static bool TryResolvePackageName(string packageName, out string resolvedPackageName, out string packageJsonPath)
        {
            resolvedPackageName = null;
            packageJsonPath = Path.Combine("Packages", packageName, "package.json");

            if (File.Exists(packageJsonPath))
            {
                resolvedPackageName = TryReadPackageNameFromPackageJson(packageJsonPath) ?? packageName;
                return true;
            }

            if (!Directory.Exists("Packages"))
            {
                return false;
            }

            string matchedPackageDirectory = Directory
                    .GetDirectories("Packages", "cn.etetet.*")
                    .Select(Path.GetFileName)
                    .FirstOrDefault(name => string.Equals(name, packageName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(matchedPackageDirectory))
            {
                return false;
            }

            packageJsonPath = Path.Combine("Packages", matchedPackageDirectory, "package.json");
            if (!File.Exists(packageJsonPath))
            {
                return false;
            }

            resolvedPackageName = TryReadPackageNameFromPackageJson(packageJsonPath) ?? matchedPackageDirectory;
            return true;
        }

        private static string TryReadPackageNameFromPackageJson(string packageJsonPath)
        {
            try
            {
                string json = File.ReadAllText(packageJsonPath);
                Match nameMatch = Regex.Match(json, @"""name""\s*:\s*""([^""]+)""");
                if (nameMatch.Success)
                {
                    return nameMatch.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MainPackageSelector] 读取包名失败 {packageJsonPath}: {ex.Message}");
            }

            return null;
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
