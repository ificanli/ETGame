using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;
using Debug = UnityEngine.Debug;
using Process = System.Diagnostics.Process;
using YooBuildResult = YooAsset.Editor.BuildResult;
using UnityBuildReport = UnityEditor.Build.Reporting.BuildReport;
using UnityBuildResult = UnityEditor.Build.Reporting.BuildResult;

namespace ET
{
    public static class ClientBuildAutomation
    {
        private const string DefaultOutputDir = "Release/WindowsClient";
        private const string BundleCacheFolderName = "_BundleBuildCache";
        private const string ExecutableName = "ET.exe";
        private const string DefaultScenePath = "Packages/cn.etetet.statesync/Scenes/Init.unity";
        private const string MainPackageFileName = "MainPackage.txt";
        private const string AssetBundleCollectorSettingPathFormat = "Packages/{0}/Settings/AssetBundleCollectorSetting.asset";

        [MenuItem("ET/Loader/Build Windows Client Bundles Only", false, 30)]
        public static void BuildWindowsBundlesOnlyFromMenu()
        {
            try
            {
                BuildRequest request = new BuildRequest();
                BuildWindowsBundlesOnly(request);

                string outputDir = ResolveOutputDirectory(GetProjectRoot(), request.OutputDir);
                string bundleOutputRoot = GetBundleOutputRoot(outputDir);
                string buildinRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
                EditorUtility.DisplayDialog(
                    "Windows客户端离线Bundle构建",
                    $"Bundle构建完成。\n缓存目录：{bundleOutputRoot}\n内置目录：{buildinRoot}\n\n可继续执行：ET/Loader/Validate Windows Client Offline Runtime",
                    "确定");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Windows客户端离线Bundle构建失败", $"请查看 Console 日志。\n\n{exception.Message}", "确定");
            }
        }

        [MenuItem("ET/Loader/Build Windows Client Offline", false, 31)]
        public static void BuildWindowsOfflineFromMenu()
        {
            try
            {
                BuildRequest request = new BuildRequest();
                BuildWindowsOffline(request);

                string outputDir = ResolveOutputDirectory(GetProjectRoot(), request.OutputDir);
                EditorUtility.DisplayDialog("Windows客户端离线打包", $"构建完成。\n输出目录：{outputDir}", "确定");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Windows客户端离线打包失败", $"请查看 Console 日志。\n\n{exception.Message}", "确定");
            }
        }

        [MenuItem("ET/Loader/Validate Windows Client Offline Runtime", false, 32)]
        public static void ValidateOfflineRuntimeFromMenu()
        {
            try
            {
                string summary = ValidateOfflineRuntime();
                EditorUtility.DisplayDialog("离线运行时校验", summary, "确定");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("离线运行时校验失败", $"请查看 Console 日志。\n\n{exception.Message}", "确定");
            }
        }

        [MenuItem("ET/Loader/Switch To Editor Runtime Config", false, 33)]
        public static void SwitchToEditorRuntimeConfigFromMenu()
        {
            try
            {
                string summary = ApplyRuntimeConfig(CodeMode.ClientServer, EPlayMode.EditorSimulateMode);
                EditorUtility.DisplayDialog("编辑器运行配置", summary, "确定");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("编辑器运行配置切换失败", $"请查看 Console 日志。\n\n{exception.Message}", "确定");
            }
        }

        [MenuItem("ET/Loader/Switch To Package Runtime Config", false, 34)]
        public static void SwitchToPackageRuntimeConfigFromMenu()
        {
            try
            {
                string summary = ApplyRuntimeConfig(CodeMode.Client, EPlayMode.OfflinePlayMode);
                EditorUtility.DisplayDialog("包体运行配置", summary, "确定");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("包体运行配置切换失败", $"请查看 Console 日志。\n\n{exception.Message}", "确定");
            }
        }

        public static void BuildWindowsOfflineFromCommandLine()
        {
            try
            {
                BuildRequest request = ParseBuildRequest(Environment.GetCommandLineArgs());
                BuildWindowsOffline(request);
                ExitBatchMode(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ExitBatchMode(1);
                throw;
            }
        }

        private static void BuildWindowsOffline(BuildRequest request)
        {
            string projectRoot = GetProjectRoot();
            string outputDir = ResolveOutputDirectory(projectRoot, request.OutputDir);
            string bundleOutputRoot = GetBundleOutputRoot(outputDir);
            string executablePath = Path.Combine(outputDir, ExecutableName);
            string buildVersion = string.IsNullOrWhiteSpace(request.BuildVersion) ? GetDefaultBuildVersion() : request.BuildVersion;

            ValidateScenePath(projectRoot);
            ValidateOutputDirectory(projectRoot, outputDir);

            string globalConfigPath = FindSingleAssetPath("GlobalConfig");
            string yooConfigPath = FindSingleAssetPath("YooConfig");
            GlobalConfig globalConfig = LoadAssetAtPath<GlobalConfig>(globalConfigPath, "GlobalConfig");
            YooConfig yooConfig = LoadAssetAtPath<YooConfig>(yooConfigPath, "YooConfig");
            string mainPackageName = ReadMainPackageName(projectRoot);

            CodeMode originalCodeMode = globalConfig.CodeMode;
            EPlayMode originalPlayMode = yooConfig.EPlayMode;
            bool codeModeChanged = false;
            bool playModeChanged = false;
            bool buildSucceeded = false;

            try
            {
                PrepareOutputDirectory(outputDir);
                SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

                codeModeChanged = EnsureCodeMode(globalConfigPath, CodeMode.Client);
                playModeChanged = EnsurePlayMode(yooConfigPath, EPlayMode.OfflinePlayMode);
                RefreshHotUpdateDlls();
                GenerateHybridClrArtifacts();
                CopyHybridClrAotDlls();

                AssetBundleCollectorSetting collectorSetting = LoadCollectorSetting(mainPackageName);
                BuildOfflineBundles(collectorSetting, buildVersion, bundleOutputRoot);

                UnityBuildReport report = BuildHelper.Build(
                    PlatformType.Windows,
                    BuildOptions.None,
                    executablePath,
                    BuildHelper.GetDefaultLevels(),
                    false);

                if (report.summary.result != UnityBuildResult.Succeeded)
                {
                    throw new Exception($"Windows 客户端构建失败: {report.summary.result}");
                }

                Debug.Log("[ClientBuildAutomation] BuildPlayer 完成，使用本次 IL2CPP 裁剪产物重建 AOT Bundle");
                CopyHybridClrAotDlls();

                string postBuildVersion = buildVersion + "-post";
                BuildOfflineBundles(collectorSetting, postBuildVersion, bundleOutputRoot);
                CopyBuiltinBundlesToPlayer(outputDir);

                buildSucceeded = true;
                DeleteDirectoryIfExists(bundleOutputRoot);
                Debug.Log($"[ClientBuildAutomation] Windows 离线包构建完成: {outputDir}");
            }
            finally
            {
                RestoreEnvironment(globalConfigPath, yooConfigPath, originalCodeMode, originalPlayMode, codeModeChanged, playModeChanged);

                if (!buildSucceeded)
                {
                    Debug.LogWarning($"[ClientBuildAutomation] 构建失败，保留 Bundle 构建缓存以便排查: {bundleOutputRoot}");
                }
            }
        }

        private static void BuildWindowsBundlesOnly(BuildRequest request)
        {
            string projectRoot = GetProjectRoot();
            string outputDir = ResolveOutputDirectory(projectRoot, request.OutputDir);
            string bundleOutputRoot = GetBundleOutputRoot(outputDir);
            string buildVersion = string.IsNullOrWhiteSpace(request.BuildVersion) ? GetDefaultBuildVersion() : request.BuildVersion;

            ValidateOutputDirectory(projectRoot, outputDir);

            string globalConfigPath = FindSingleAssetPath("GlobalConfig");
            string yooConfigPath = FindSingleAssetPath("YooConfig");
            GlobalConfig globalConfig = LoadAssetAtPath<GlobalConfig>(globalConfigPath, "GlobalConfig");
            YooConfig yooConfig = LoadAssetAtPath<YooConfig>(yooConfigPath, "YooConfig");
            string mainPackageName = ReadMainPackageName(projectRoot);

            CodeMode originalCodeMode = globalConfig.CodeMode;
            EPlayMode originalPlayMode = yooConfig.EPlayMode;
            bool codeModeChanged = false;
            bool playModeChanged = false;
            bool buildSucceeded = false;

            try
            {
                PrepareBundleOutputDirectory(outputDir, bundleOutputRoot);
                SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

                codeModeChanged = EnsureCodeMode(globalConfigPath, CodeMode.Client);
                playModeChanged = EnsurePlayMode(yooConfigPath, EPlayMode.OfflinePlayMode);
                RefreshHotUpdateDlls();
                GenerateHybridClrArtifacts();
                CopyHybridClrAotDlls();

                AssetBundleCollectorSetting collectorSetting = LoadCollectorSetting(mainPackageName);
                BuildOfflineBundles(collectorSetting, buildVersion, bundleOutputRoot);

                buildSucceeded = true;
                Debug.Log($"[ClientBuildAutomation] Windows 离线Bundle构建完成: {bundleOutputRoot}");
            }
            finally
            {
                RestoreEnvironment(globalConfigPath, yooConfigPath, originalCodeMode, originalPlayMode, codeModeChanged, playModeChanged);

                if (!buildSucceeded)
                {
                    Debug.LogWarning($"[ClientBuildAutomation] Bundle构建失败，保留构建缓存以便排查: {bundleOutputRoot}");
                }
            }
        }

        private static void BuildOfflineBundles(AssetBundleCollectorSetting collectorSetting, string buildVersion, string buildOutputRoot)
        {
            if (collectorSetting == null)
            {
                throw new ArgumentNullException(nameof(collectorSetting));
            }

            if (collectorSetting.Packages == null || collectorSetting.Packages.Count == 0)
            {
                throw new Exception("AssetBundleCollectorSetting 中没有可构建的资源包。");
            }

            Directory.CreateDirectory(buildOutputRoot);

            foreach (AssetBundleCollectorPackage package in collectorSetting.Packages)
            {
                ClearBuiltinPackageOutput(package.PackageName);
                string pipelineName = ResolveOfflineBuildPipeline(package.PackageName);
                Debug.Log($"[ClientBuildAutomation] 开始构建 YooAsset 包: {package.PackageName}, Pipeline: {pipelineName}, Version: {buildVersion}");

                YooBuildResult buildResult = pipelineName switch
                {
                    nameof(EBuildPipeline.BuiltinBuildPipeline) => BuildBuiltinPackage(package.PackageName, buildVersion, buildOutputRoot),
                    _ => BuildScriptablePackage(package.PackageName, buildVersion, buildOutputRoot, collectorSetting.UniqueBundleName),
                };

                if (!buildResult.Success)
                {
                    throw new Exception($"YooAsset 包构建失败: {package.PackageName}, FailedTask: {buildResult.FailedTask}, Error: {buildResult.ErrorInfo}");
                }

                string builtinPackageRoot = GetBuiltinPackageRoot(package.PackageName);
                string versionFilePath = Path.Combine(builtinPackageRoot, YooAssetSettingsData.GetPackageVersionFileName(package.PackageName));
                Debug.Log($"[ClientBuildAutomation] YooAsset 包构建完成: {package.PackageName}, BuiltinRoot: {builtinPackageRoot}, VersionFile: {versionFilePath}");
            }

            AssetDatabase.Refresh();
        }

        private static YooBuildResult BuildScriptablePackage(string packageName, string buildVersion, string buildOutputRoot, bool uniqueBundleName)
        {
            string pipelineName = nameof(EBuildPipeline.ScriptableBuildPipeline);
            ScriptableBuildParameters buildParameters = new ScriptableBuildParameters
            {
                BuildOutputRoot = buildOutputRoot,
                BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(),
                BuildPipeline = pipelineName,
                BuildBundleType = (int)EBuildBundleType.AssetBundle,
                BuildTarget = BuildTarget.StandaloneWindows64,
                PackageName = packageName,
                PackageVersion = buildVersion,
                EnableSharePackRule = true,
                VerifyBuildingResult = true,
                FileNameStyle = AssetBundleBuilderSetting.GetPackageFileNameStyle(packageName, pipelineName),
                BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll,
                BuildinFileCopyParams = string.Empty,
                CompressOption = AssetBundleBuilderSetting.GetPackageCompressOption(packageName, pipelineName),
                ClearBuildCacheFiles = AssetBundleBuilderSetting.GetPackageClearBuildCache(packageName, pipelineName),
                UseAssetDependencyDB = AssetBundleBuilderSetting.GetPackageUseAssetDependencyDB(packageName, pipelineName),
                EncryptionServices = CreateServiceInstance<IEncryptionServices>(AssetBundleBuilderSetting.GetPackageEncyptionServicesClassName(packageName, pipelineName)),
                ManifestProcessServices = CreateServiceInstance<IManifestProcessServices>(AssetBundleBuilderSetting.GetPackageManifestProcessServicesClassName(packageName, pipelineName)),
                ManifestRestoreServices = CreateServiceInstance<IManifestRestoreServices>(AssetBundleBuilderSetting.GetPackageManifestRestoreServicesClassName(packageName, pipelineName)),
                BuiltinShadersBundleName = GetBuiltinShaderBundleName(packageName, uniqueBundleName),
                MonoScriptsBundleName = GetMonoScriptsBundleName(packageName, uniqueBundleName),
            };

            return new ScriptableBuildPipeline().Run(buildParameters, true);
        }

        private static YooBuildResult BuildBuiltinPackage(string packageName, string buildVersion, string buildOutputRoot)
        {
            string pipelineName = nameof(EBuildPipeline.BuiltinBuildPipeline);
            BuiltinBuildParameters buildParameters = new BuiltinBuildParameters
            {
                BuildOutputRoot = buildOutputRoot,
                BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(),
                BuildPipeline = pipelineName,
                BuildBundleType = (int)EBuildBundleType.AssetBundle,
                BuildTarget = BuildTarget.StandaloneWindows64,
                PackageName = packageName,
                PackageVersion = buildVersion,
                EnableSharePackRule = true,
                VerifyBuildingResult = true,
                FileNameStyle = AssetBundleBuilderSetting.GetPackageFileNameStyle(packageName, pipelineName),
                BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll,
                BuildinFileCopyParams = string.Empty,
                CompressOption = AssetBundleBuilderSetting.GetPackageCompressOption(packageName, pipelineName),
                ClearBuildCacheFiles = AssetBundleBuilderSetting.GetPackageClearBuildCache(packageName, pipelineName),
                UseAssetDependencyDB = AssetBundleBuilderSetting.GetPackageUseAssetDependencyDB(packageName, pipelineName),
                EncryptionServices = CreateServiceInstance<IEncryptionServices>(AssetBundleBuilderSetting.GetPackageEncyptionServicesClassName(packageName, pipelineName)),
                ManifestProcessServices = CreateServiceInstance<IManifestProcessServices>(AssetBundleBuilderSetting.GetPackageManifestProcessServicesClassName(packageName, pipelineName)),
                ManifestRestoreServices = CreateServiceInstance<IManifestRestoreServices>(AssetBundleBuilderSetting.GetPackageManifestRestoreServicesClassName(packageName, pipelineName)),
            };

            return new BuiltinBuildPipeline().Run(buildParameters, true);
        }

        private static void RestoreEnvironment(
            string globalConfigPath,
            string yooConfigPath,
            CodeMode originalCodeMode,
            EPlayMode originalPlayMode,
            bool codeModeChanged,
            bool playModeChanged)
        {
            List<Exception> restoreExceptions = new List<Exception>();

            try
            {
                if (codeModeChanged)
                {
                    GlobalConfig globalConfig = LoadAssetAtPath<GlobalConfig>(globalConfigPath, "GlobalConfig");
                    if (globalConfig.CodeMode != originalCodeMode)
                    {
                        Debug.Log($"[ClientBuildAutomation] 恢复 CodeMode: {originalCodeMode}");
                        globalConfig.CodeMode = originalCodeMode;
                        EditorUtility.SetDirty(globalConfig);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        RunCodeModeTool(originalCodeMode);
                    }
                }
            }
            catch (Exception exception)
            {
                restoreExceptions.Add(exception);
            }

            try
            {
                if (playModeChanged)
                {
                    YooConfig yooConfig = LoadAssetAtPath<YooConfig>(yooConfigPath, "YooConfig");
                    if (yooConfig.EPlayMode != originalPlayMode)
                    {
                        Debug.Log($"[ClientBuildAutomation] 恢复 PlayMode: {originalPlayMode}");
                        yooConfig.EPlayMode = originalPlayMode;
                        EditorUtility.SetDirty(yooConfig);
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception exception)
            {
                restoreExceptions.Add(exception);
            }

            if (restoreExceptions.Count > 0)
            {
                throw new AggregateException("构建后恢复本地环境失败。", restoreExceptions);
            }
        }

        private static bool EnsureCodeMode(string globalConfigPath, CodeMode targetCodeMode)
        {
            GlobalConfig globalConfig = LoadAssetAtPath<GlobalConfig>(globalConfigPath, "GlobalConfig");
            if (globalConfig.CodeMode == targetCodeMode)
            {
                Debug.Log($"[ClientBuildAutomation] CodeMode 已是目标值: {targetCodeMode}");
                return false;
            }

            Debug.Log($"[ClientBuildAutomation] 切换 CodeMode: {globalConfig.CodeMode} -> {targetCodeMode}");
            globalConfig.CodeMode = targetCodeMode;
            EditorUtility.SetDirty(globalConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RunCodeModeTool(targetCodeMode);
            return true;
        }

        private static bool EnsurePlayMode(string yooConfigPath, EPlayMode targetPlayMode)
        {
            YooConfig yooConfig = LoadAssetAtPath<YooConfig>(yooConfigPath, "YooConfig");
            if (yooConfig.EPlayMode == targetPlayMode)
            {
                Debug.Log($"[ClientBuildAutomation] PlayMode 已是目标值: {targetPlayMode}");
                return false;
            }

            Debug.Log($"[ClientBuildAutomation] 切换 PlayMode: {yooConfig.EPlayMode} -> {targetPlayMode}");
            yooConfig.EPlayMode = targetPlayMode;
            EditorUtility.SetDirty(yooConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        private static void RunCodeModeTool(CodeMode codeMode)
        {
            Process process = ProcessHelper.DotNet($"Bin/ET.CodeMode.dll --CodeMode={codeMode}", ".", true);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"执行 ET.CodeMode.dll 失败，CodeMode={codeMode}，退出码: {process.ExitCode}");
            }
        }

        private static void PrepareOutputDirectory(string outputDir)
        {
            DeleteDirectoryIfExists(outputDir);
            Directory.CreateDirectory(outputDir);
        }

        private static void PrepareBundleOutputDirectory(string outputDir, string bundleOutputRoot)
        {
            Directory.CreateDirectory(outputDir);
            DeleteDirectoryIfExists(bundleOutputRoot);
            Directory.CreateDirectory(bundleOutputRoot);
        }

        private static void DeleteDirectoryIfExists(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }

        private static void SwitchActiveBuildTarget(BuildTargetGroup targetGroup, BuildTarget target)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target)
            {
                return;
            }

            Debug.Log($"[ClientBuildAutomation] 切换 BuildTarget: {EditorUserBuildSettings.activeBuildTarget} -> {target}");
            bool switchSucceeded = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);
            if (!switchSucceeded)
            {
                throw new Exception($"切换 BuildTarget 失败: {target}");
            }
        }

        private static AssetBundleCollectorSetting LoadCollectorSetting(string mainPackageName)
        {
            string settingPath = string.Format(AssetBundleCollectorSettingPathFormat, mainPackageName).Replace('\\', '/');
            AssetDatabase.ImportAsset(settingPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetBundleCollectorSetting collectorSetting = AssetDatabase.LoadAssetAtPath<AssetBundleCollectorSetting>(settingPath);
            if (collectorSetting == null)
            {
                throw new Exception($"未找到 AssetBundleCollectorSetting: {settingPath}");
            }

            return collectorSetting;
        }

        private static string GetBuiltinShaderBundleName(string packageName, bool uniqueBundleName)
        {
            PackRuleResult packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
            return packRuleResult.GetBundleName(packageName, uniqueBundleName);
        }

        private static string GetMonoScriptsBundleName(string packageName, bool uniqueBundleName)
        {
            PackRuleResult packRuleResult = DefaultPackRule.CreateMonosPackRuleResult();
            return packRuleResult.GetBundleName(packageName, uniqueBundleName);
        }

        private static T CreateServiceInstance<T>(string className) where T : class
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                return null;
            }

            List<Type> classTypes = EditorTools.GetAssignableTypes(typeof(T));
            Type classType = classTypes.Find(type => type.FullName == className);
            if (classType == null)
            {
                Debug.LogWarning($"[ClientBuildAutomation] 未找到服务类: {className}");
                return null;
            }

            return Activator.CreateInstance(classType) as T;
        }

        private static void GenerateHybridClrArtifacts()
        {
            Debug.Log("[ClientBuildAutomation] 开始执行 HybridCLR/Generate/All");
            HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll();
        }

        private static void CopyHybridClrAotDlls()
        {
            Debug.Log("[ClientBuildAutomation] 开始执行 ET/HybridCLR/CopyAotDlls");
            HybridCLREditor.CopyAotDll();
        }

        private static string ApplyRuntimeConfig(CodeMode targetCodeMode, EPlayMode targetPlayMode)
        {
            string globalConfigPath = FindSingleAssetPath("GlobalConfig");
            string yooConfigPath = FindSingleAssetPath("YooConfig");

            bool codeModeChanged = EnsureCodeMode(globalConfigPath, targetCodeMode);
            bool playModeChanged = EnsurePlayMode(yooConfigPath, targetPlayMode);

            string summary = $"切换完成。\nCodeMode = {targetCodeMode}\nPlayMode = {targetPlayMode}";
            if (!codeModeChanged && !playModeChanged)
            {
                summary += "\n当前已经是目标配置。";
            }

            Debug.Log($"[ClientBuildAutomation] {summary.Replace('\n', ' ')}");
            return summary;
        }

        private static string ValidateOfflineRuntime()
        {
            string projectRoot = GetProjectRoot();
            string mainPackageName = ReadMainPackageName(projectRoot);
            AssetBundleCollectorSetting collectorSetting = LoadCollectorSetting(mainPackageName);
            List<string> issues = new List<string>();
            List<string> summaries = new List<string>();

            foreach (AssetBundleCollectorPackage package in collectorSetting.Packages)
            {
                summaries.Add(ValidateOfflineRuntimePackage(package.PackageName, issues));
            }

            if (issues.Count > 0)
            {
                throw new Exception($"离线运行时校验失败，共 {issues.Count} 项异常：\n{string.Join("\n", issues)}");
            }

            string summary = $"离线运行时校验通过。\n{string.Join("\n", summaries)}";
            Debug.Log($"[ClientBuildAutomation] {summary}");
            return summary;
        }

        private static string ValidateOfflineRuntimePackage(string packageName, List<string> issues)
        {
            object manifest = LoadBuiltinManifest(packageName);
            int codeCount = ValidateExactLocations(packageName, manifest, "Code", GetCodeBundleLocations(), issues);
            ValidateCodeBundleFreshness(packageName, issues);
            int aotCount = ValidateExactLocations(packageName, manifest, "AOT", GetAotDllLocations(), issues);
            int uiCount = ValidateExactLocations(packageName, manifest, "UI", GetCriticalUiLocations(), issues);
            int configCount = ValidateConfigLocations(packageName, manifest, issues);
            string summary = $"包 {packageName}: Code {codeCount} 项, AOT {aotCount} 项, UI {uiCount} 项, Config {configCount} 项";
            Debug.Log($"[ClientBuildAutomation] {summary}");
            return summary;
        }

        private static int ValidateExactLocations(string packageName, object manifest, string groupName, IEnumerable<string> locations, List<string> issues)
        {
            int count = 0;
            foreach (string location in locations.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
            {
                ++count;
                string assetPath = TryMapLocationToAssetPath(manifest, location);
                if (string.IsNullOrEmpty(assetPath))
                {
                    issues.Add($"[{packageName}/{groupName}] 缺少 location: {location}");
                }
            }

            return count;
        }

        private static void ValidateCodeBundleFreshness(string packageName, List<string> issues)
        {
            foreach (string dllName in AssemblyTool.DllNames)
            {
                string builtDllPath = Path.Combine(GetProjectRoot(), Define.BuildOutputDir, $"{dllName}.dll");
                string bundledDllPath = Path.Combine(GetProjectRoot(), Define.CodeDir, $"{dllName}.dll.bytes");

                if (!File.Exists(builtDllPath))
                {
                    issues.Add($"[{packageName}/Code] 缺少最新编译产物: {builtDllPath}");
                    continue;
                }

                if (!File.Exists(bundledDllPath))
                {
                    issues.Add($"[{packageName}/Code] 缺少代码包产物: {bundledDllPath}");
                    continue;
                }

                string builtHash = ComputeFileSha256(builtDllPath);
                string bundledHash = ComputeFileSha256(bundledDllPath);
                if (!string.Equals(builtHash, bundledHash, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(
                        $"[{packageName}/Code] 代码包已过期: {dllName}. Bundled={bundledDllPath}, BuildOutput={builtDllPath}");
                }
            }
        }

        private static int ValidateConfigLocations(string packageName, object manifest, List<string> issues)
        {
            int count = 0;
            foreach (Type configType in GetClientConfigTypes())
            {
                ++count;
                ConfigProcessAttribute processAttribute =
                    configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false).FirstOrDefault() as ConfigProcessAttribute;
                if (processAttribute == null)
                {
                    issues.Add($"[{packageName}/Config] 配置类型缺少 ConfigProcessAttribute: {configType.FullName}");
                    continue;
                }

                if (!TryResolveConfigAssetPath(manifest, configType, out string location, out string assetPath))
                {
                    string directLocation = configType.Name;
                    string fallbackLocation = $"et_{configType.Name.ToLowerInvariant()}";
                    issues.Add($"[{packageName}/Config] 缺少配置location: {configType.FullName}，已尝试 {directLocation} / {fallbackLocation}");
                    continue;
                }

                if (!IsConfigAssetPathValid(processAttribute.ConfigType, assetPath))
                {
                    issues.Add(
                        $"[{packageName}/Config] 配置格式不匹配: {configType.FullName}, ConfigType={processAttribute.ConfigType}, Location={location}, AssetPath={assetPath}");
                }
            }

            return count;
        }

        private static bool TryResolveConfigAssetPath(object manifest, Type configType, out string location, out string assetPath)
        {
            foreach (string candidateLocation in GetConfigLocations(configType.Name))
            {
                string mappedAssetPath = TryMapLocationToAssetPath(manifest, candidateLocation);
                if (string.IsNullOrEmpty(mappedAssetPath))
                {
                    continue;
                }

                location = candidateLocation;
                assetPath = mappedAssetPath;
                return true;
            }

            location = null;
            assetPath = null;
            return false;
        }

        private static IEnumerable<string> GetConfigLocations(string configName)
        {
            yield return configName;

            string fallbackLocation = $"et_{configName.ToLowerInvariant()}";
            if (!string.Equals(configName, fallbackLocation, StringComparison.Ordinal))
            {
                yield return fallbackLocation;
            }
        }

        private static bool IsConfigAssetPathValid(int configType, string assetPath)
        {
            string normalizedAssetPath = assetPath.Replace('\\', '/');
            switch (configType)
            {
                case ConfigType.Luban:
                    return normalizedAssetPath.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase) &&
                            normalizedAssetPath.Contains("/Binary/");
                case ConfigType.Json:
                    return normalizedAssetPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                            normalizedAssetPath.Contains("/Json/");
                case ConfigType.Bson:
                    return normalizedAssetPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);
                default:
                    return true;
            }
        }

        private static object LoadBuiltinManifest(string packageName)
        {
            string packageRoot = GetBuiltinPackageRoot(packageName);
            string versionFilePath = Path.Combine(packageRoot, YooAssetSettingsData.GetPackageVersionFileName(packageName));
            if (!File.Exists(versionFilePath))
            {
                throw new Exception($"未找到离线版本文件，请先执行 Bundle 构建: {versionFilePath}");
            }

            string packageVersion = File.ReadAllText(versionFilePath).Trim().TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                throw new Exception($"离线版本文件为空: {versionFilePath}");
            }

            string manifestFileName = YooAssetSettingsData.GetManifestBinaryFileName(packageName, packageVersion);
            string manifestFilePath = Path.Combine(packageRoot, manifestFileName);
            if (!File.Exists(manifestFilePath))
            {
                throw new Exception($"未找到离线Manifest文件: {manifestFilePath}");
            }

            string pipelineName = ResolveOfflineBuildPipeline(packageName);
            IManifestRestoreServices restoreServices =
                CreateServiceInstance<IManifestRestoreServices>(AssetBundleBuilderSetting.GetPackageManifestRestoreServicesClassName(packageName, pipelineName));
            byte[] manifestData = File.ReadAllBytes(manifestFilePath);

            Type manifestToolsType = typeof(YooAssetSettingsData).Assembly.GetType("YooAsset.ManifestTools");
            if (manifestToolsType == null)
            {
                throw new Exception("未找到 YooAsset.ManifestTools 类型，无法反序列化离线Manifest。");
            }

            MethodInfo deserializeMethod = manifestToolsType.GetMethod(
                "DeserializeFromBinary",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (deserializeMethod == null)
            {
                throw new Exception("未找到 YooAsset.ManifestTools.DeserializeFromBinary 方法。");
            }

            object manifest = deserializeMethod.Invoke(null, new object[] { manifestData, restoreServices });
            if (manifest == null)
            {
                throw new Exception($"离线Manifest反序列化失败: {manifestFilePath}");
            }

            return manifest;
        }

        private static string TryMapLocationToAssetPath(object manifest, string location)
        {
            MethodInfo tryMappingMethod = manifest.GetType().GetMethod(
                "TryMappingToAssetPath",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (tryMappingMethod == null)
            {
                throw new Exception("未找到 PackageManifest.TryMappingToAssetPath 方法。");
            }

            return tryMappingMethod.Invoke(manifest, new object[] { location }) as string;
        }

        private static IEnumerable<string> GetCodeBundleLocations()
        {
            return EnumerateBundleLocations("Packages/cn.etetet.loader/Bundles/Code", "*.dll.bytes");
        }

        private static IEnumerable<string> GetAotDllLocations()
        {
            return EnumerateBundleLocations("Packages/cn.etetet.loader/Bundles/AotDlls", "*.dll.bytes");
        }

        private static IEnumerable<string> GetCriticalUiLocations()
        {
            yield return ResolvePublicConstString("ET.YIUIConstHelper", "YIUIConstAssetName", "YIUIConstAsset");
            yield return ResolvePublicConstString("ET.YIUIConstAsset", "AtlasDataName", "YIUIAtlasData");
        }

        private static IEnumerable<string> EnumerateBundleLocations(string relativeDirectory, string searchPattern)
        {
            string directoryPath = Path.Combine(GetProjectRoot(), relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(directoryPath))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
                .OrderBy(fileName => fileName, StringComparer.Ordinal)
                .ToArray();
        }

        private static IEnumerable<Type> GetClientConfigTypes()
        {
            return TypeCache.GetTypesWithAttribute<ConfigProcessAttribute>()
                .Where(type => type != null)
                .Where(type => !type.IsAbstract)
                .Where(type => type.FullName != null)
                .Where(type => !type.FullName.StartsWith("ET.Server.", StringComparison.Ordinal))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        private static string ResolvePublicConstString(string typeFullName, string fieldName, string fallbackValue)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeFullName, false);
                if (type == null)
                {
                    continue;
                }

                FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (fieldInfo == null || fieldInfo.FieldType != typeof(string))
                {
                    continue;
                }

                string value = fieldInfo.GetValue(null) as string;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return fallbackValue;
        }

        private static string FindSingleAssetPath(string typeName)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeName}");
            if (guids.Length == 0)
            {
                throw new Exception($"未找到 {typeName} 资产。");
            }

            if (guids.Length > 1)
            {
                string assetList = string.Join(", ", guids.Select(AssetDatabase.GUIDToAssetPath));
                throw new Exception($"找到多个 {typeName} 资产，无法确定目标: {assetList}");
            }

            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        private static T LoadAssetAtPath<T>(string assetPath, string typeName) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new Exception($"加载 {typeName} 资产失败: {assetPath}");
            }

            return asset;
        }

        private static string ReadMainPackageName(string projectRoot)
        {
            string mainPackagePath = Path.Combine(projectRoot, MainPackageFileName);
            if (!File.Exists(mainPackagePath))
            {
                throw new Exception($"未找到 {MainPackageFileName}: {mainPackagePath}");
            }

            foreach (string line in File.ReadLines(mainPackagePath))
            {
                string packageName = line?.Trim();
                if (!string.IsNullOrWhiteSpace(packageName))
                {
                    return packageName.TrimStart('\uFEFF');
                }
            }

            throw new Exception($"{MainPackageFileName} 为空: {mainPackagePath}");
        }

        private static void ValidateScenePath(string projectRoot)
        {
            string sceneFullPath = Path.Combine(projectRoot, DefaultScenePath);
            if (!File.Exists(sceneFullPath))
            {
                throw new Exception($"未找到客户端入口场景: {DefaultScenePath}");
            }
        }

        private static void ValidateOutputDirectory(string projectRoot, string outputDir)
        {
            string rootPath = Path.GetPathRoot(outputDir);
            if (string.Equals(outputDir, projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("输出目录不能是项目根目录。");
            }

            if (string.Equals(outputDir, rootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("输出目录不能是磁盘根目录。");
            }
        }

        private static string ResolveOutputDirectory(string projectRoot, string outputDir)
        {
            string candidate = string.IsNullOrWhiteSpace(outputDir) ? DefaultOutputDir : outputDir;
            string fullPath = Path.IsPathRooted(candidate) ? candidate : Path.Combine(projectRoot, candidate);
            return Path.GetFullPath(fullPath);
        }

        private static string GetBundleOutputRoot(string outputDir)
        {
            return Path.Combine(outputDir, BundleCacheFolderName);
        }

        private static string GetBuiltinPackageRoot(string packageName)
        {
            return Path.Combine(AssetBundleBuilderHelper.GetStreamingAssetsRoot(), packageName);
        }

        private static string ResolveOfflineBuildPipeline(string packageName)
        {
            string configuredPipeline = AssetBundleBuilderSetting.GetPackageBuildPipeline(packageName);
            if (configuredPipeline == nameof(EBuildPipeline.BuiltinBuildPipeline) ||
                configuredPipeline == nameof(EBuildPipeline.ScriptableBuildPipeline))
            {
                return configuredPipeline;
            }

            Debug.LogWarning($"[ClientBuildAutomation] 包 {packageName} 当前构建管线 {configuredPipeline} 不适用于离线包，自动回退到 {nameof(EBuildPipeline.ScriptableBuildPipeline)}");
            return nameof(EBuildPipeline.ScriptableBuildPipeline);
        }

        private static void CopyBuiltinBundlesToPlayer(string outputDir)
        {
            string builtinRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            string playerStreamingAssets = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(ExecutableName)}_Data", "StreamingAssets", "Bundles");

            if (!Directory.Exists(builtinRoot))
            {
                throw new Exception($"内置 Bundle 目录不存在: {builtinRoot}");
            }

            if (!Directory.Exists(playerStreamingAssets))
            {
                throw new Exception($"包体 StreamingAssets 目录不存在: {playerStreamingAssets}");
            }

            Debug.Log($"[ClientBuildAutomation] 覆盖包体 Bundle: {builtinRoot} -> {playerStreamingAssets}");
            DeleteDirectoryIfExists(playerStreamingAssets);
            CopyDirectoryRecursive(builtinRoot, playerStreamingAssets);
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Copy(filePath, Path.Combine(destinationDir, fileName), true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                CopyDirectoryRecursive(subDir, Path.Combine(destinationDir, dirName));
            }
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static void RefreshHotUpdateDlls()
        {
            Debug.Log("[ClientBuildAutomation] 开始刷新热更 DLL 到 Bundles/Code");
            AssemblyTool.DoCompile();
        }

        private static string ComputeFileSha256(string filePath)
        {
            using SHA256 sha256 = SHA256.Create();
            using FileStream stream = File.OpenRead(filePath);
            byte[] hashBytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        private static string GetDefaultBuildVersion()
        {
            return $"{DateTime.Now:yyyy-MM-dd-HHmmss}";
        }

        private static void ClearBuiltinPackageOutput(string packageName)
        {
            string builtinPackageRoot = GetBuiltinPackageRoot(packageName);
            DeleteDirectoryIfExists(builtinPackageRoot);
            DeleteFileIfExists($"{builtinPackageRoot}.meta");
        }

        private static void DeleteFileIfExists(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private static BuildRequest ParseBuildRequest(string[] args)
        {
            BuildRequest request = new BuildRequest();
            for (int index = 0; index < args.Length; ++index)
            {
                string argument = args[index];
                if (argument == "-outputDir")
                {
                    if (index + 1 < args.Length)
                    {
                        request.OutputDir = args[++index];
                    }

                    continue;
                }

                if (argument == "-buildVersion")
                {
                    if (index + 1 < args.Length)
                    {
                        request.BuildVersion = args[++index];
                    }

                    continue;
                }

                if (argument.StartsWith("--outputDir=", StringComparison.Ordinal))
                {
                    request.OutputDir = argument.Substring("--outputDir=".Length);
                }
                else if (argument.StartsWith("--buildVersion=", StringComparison.Ordinal))
                {
                    request.BuildVersion = argument.Substring("--buildVersion=".Length);
                }
            }

            return request;
        }

        private static void ExitBatchMode(int exitCode)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(exitCode);
            }
        }

        private sealed class BuildRequest
        {
            public string OutputDir { get; set; }
            public string BuildVersion { get; set; }
        }
    }
}
