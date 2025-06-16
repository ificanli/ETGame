using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;
using YIUIFramework;
using YIUIFramework.Editor;

namespace YIUILuban.Editor
{
    public class LubanToolRoot
    {
        [HideInInspector]
        public OdinMenuEditorWindow AutoTool { get; set; }

        [HideInInspector]
        public OdinMenuTree Tree { get; set; }

        internal const string m_LubanName = "Luban";

        private const string m_AllETPkgPath = "Packages/";

        internal readonly Dictionary<string, List<string>> m_AllExcelData = new();

        private static void FindAllCommonParentDirectories(List<string> results, string rootPath, int targetDepth)
        {
            if (!Directory.Exists(rootPath))
            {
                return;
            }

            var queue = new Queue<(string Path, int Depth)>();
            queue.Enqueue((rootPath, 0));

            while (queue.Count > 0)
            {
                var (currentPath, currentDepth) = queue.Dequeue();

                if (currentDepth == targetDepth)
                {
                    var basePath = Path.Combine(currentPath, "Base");
                    var datasPath = Path.Combine(currentPath, "Datas");

                    if (Directory.Exists(basePath) && Directory.Exists(datasPath))
                    {
                        results.Add(currentPath);
                    }
                }
                else if (currentDepth < targetDepth)
                {
                    try
                    {
                        foreach (string subDir in Directory.GetDirectories(currentPath))
                        {
                            queue.Enqueue((subDir, currentDepth + 1));
                        }
                    }
                    catch (Exception)
                    {
                        // ignored  
                    }
                }
            }
        }

        public void RefreshAllLuban(OdinMenuEditorWindow autoTool, OdinMenuTree tree)
        {
            this.AutoTool = autoTool;
            this.Tree = tree;
            this.m_AllExcelData.Clear();

            try
            {
                var result = new List<string>();
                foreach (var packagePath in Directory.GetDirectories(EditorHelper.GetProjPath(m_AllETPkgPath)))
                {
                    result.Clear();
                    FindAllCommonParentDirectories(result, packagePath, 2);
                    foreach (var path in result)
                    {
                        var etPackagesName = UIOperationHelper.GetETPackagesName(path, false);
                        if (string.IsNullOrEmpty(etPackagesName))
                        {
                            Debug.LogError($"错误这里没有找到ET包名 请检查 {path}");
                            continue;
                        }

                        if (path.Contains(".Template"))
                        {
                            continue;
                        }

                        if (!m_AllExcelData.ContainsKey(etPackagesName))
                        {
                            m_AllExcelData.Add(etPackagesName, new());
                        }

                        m_AllExcelData[etPackagesName].Add(path);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"获取所有模块错误 请检查 err={e.Message}{e.StackTrace}");
                return;
            }

            if (!YIUILubanTool.LubanEditorData.RootShowAllConfig)
            {
                var allDatas = new TreeMenuItem<LubanAllDatasModule>(this.AutoTool, this.Tree, $"{m_LubanName}/所有配置", EditorIcons.Folder);
                allDatas.UserData = new LubanAllConfigModuleData
                {
                    AllExcelData = m_AllExcelData
                };
            }

            var fileNameHash = new HashSet<string>();

            foreach ((var packagesName, var configPathList) in this.m_AllExcelData)
            {
                fileNameHash.Clear();
                var showChildName = configPathList.Count > 1;
                foreach (var configPath in configPathList)
                {
                    var folderName = "";
                    if (showChildName)
                    {
                        folderName = $"[{Path.GetFileName(configPath)}]";
                        if (!fileNameHash.Add(folderName))
                        {
                            Debug.LogError($"文件名重复{folderName} 请检查 {configPath}");
                            continue;
                        }
                    }

                    var data = YIUILubanTool.LubanEditorData.GetLubanEditorPackageSettings(packagesName);
                    var showName = packagesName;
                    var modulePath = $"{m_LubanName}/{packagesName}{folderName}";
                    if (data is { Alias: not null } && !string.IsNullOrEmpty(data.Alias))
                    {
                        var alias = data.Alias;
                        showName = $"{alias}[{packagesName}]{folderName}";
                        modulePath = $"{m_LubanName}/{showName}";
                    }

                    var pkgMenu = new TreeMenuItem<LubanConfigModule>(this.AutoTool, this.Tree, modulePath, EditorIcons.Folder);
                    pkgMenu.UserData = new LubanConfigModuleData
                    {
                        ModulePath = modulePath,
                        PackagesName = packagesName,
                        ConfigPath = configPath,
                        FolderName = folderName,
                    };
                }
            }
        }
    }
}