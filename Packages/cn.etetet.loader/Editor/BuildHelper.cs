using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ET
{
    public static class BuildHelper
    {
        private const string relativeDirPrefix = "./Release";
        private static readonly string[] defaultLevels = { "Packages/cn.etetet.statesync/Scenes/Init.unity" };

        public static string BuildFolder = "./Release/{0}/StreamingAssets/";

#if ENABLE_VIEW
        [MenuItem("ET/Loader/Remove ENABLE_VIEW", false)]
        public static void RemoveEnableView()
        {
            EnableDefineSymbols("ENABLE_VIEW", false);
        }
#else
        [MenuItem("ET/Loader/Add ENABLE_VIEW", false)]
        public static void AddEnableView()
        {
            EnableDefineSymbols("ENABLE_VIEW", true);
        }
#endif
        public static void EnableDefineSymbols(string symbols, bool enable)
        {
            Debug.Log($"EnableDefineSymbols {symbols} {enable}");
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            var ss = defines.Split(';').ToList();
            if (enable)
            {
                if (ss.Contains(symbols))
                {
                    return;
                }

                ss.Add(symbols);
            }
            else
            {
                if (!ss.Contains(symbols))
                {
                    return;
                }

                ss.Remove(symbols);
            }

            Debug.Log($"EnableDefineSymbols {symbols} {enable}");
            defines = string.Join(";", ss);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup), defines);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void Build(PlatformType type, BuildOptions buildOptions)
        {
            string outputPath = GetDefaultOutputPath(type);
            Build(type, buildOptions, outputPath, null, true);
        }

        public static BuildReport Build(PlatformType type, BuildOptions buildOptions, string outputPath, string[] levels = null, bool revealOutput = false)
        {
            BuildTarget buildTarget = GetBuildTarget(type, out _);
            string[] buildLevels = levels ?? defaultLevels;
            string finalOutputPath = string.IsNullOrWhiteSpace(outputPath) ? GetDefaultOutputPath(type) : outputPath;
            string outputDirectory = Path.GetDirectoryName(finalOutputPath);

            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            AssetDatabase.Refresh();

            Debug.Log($"start build: {finalOutputPath}");

            BuildReport report = BuildPipeline.BuildPlayer(buildLevels, finalOutputPath, buildTarget, buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.Log($"BuildResult:{report.summary.result}");
                return report;
            }

            Debug.Log("finish build");
            if (revealOutput && !Application.isBatchMode)
            {
                EditorUtility.OpenWithDefaultApp(outputDirectory ?? relativeDirPrefix);
            }

            return report;
        }

        public static string[] GetDefaultLevels()
        {
            return defaultLevels.ToArray();
        }

        public static string GetDefaultOutputPath(PlatformType type)
        {
            GetBuildTarget(type, out string executableName);
            return $"{relativeDirPrefix}/{executableName}";
        }

        private static BuildTarget GetBuildTarget(PlatformType type, out string executableName)
        {
            executableName = "ET";
            switch (type)
            {
                case PlatformType.Windows:
                    executableName += ".exe";
                    return BuildTarget.StandaloneWindows64;
                case PlatformType.Android:
                    executableName += ".apk";
                    return BuildTarget.Android;
                case PlatformType.IOS:
                    return BuildTarget.iOS;
                case PlatformType.MacOS:
                    return BuildTarget.StandaloneOSX;
                case PlatformType.Linux:
                    return BuildTarget.StandaloneLinux64;
                case PlatformType.WebGL:
                    return BuildTarget.WebGL;
                default:
                    return BuildTarget.StandaloneWindows64;
            }
        }
    }
}
