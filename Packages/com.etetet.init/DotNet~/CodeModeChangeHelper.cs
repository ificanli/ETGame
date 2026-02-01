using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ET
{
    public static class CodeModeChangeHelper
    {
        private static readonly string[] moduleDirs = { "Packages", "Library/PackageCache" };

        private static readonly string[] scriptDirs = { "Scripts", "CodeMode" };

        private static readonly string[] modelDirs = { "Model", "Hotfix", "ModelView", "HotfixView", "Core", "Loader" };

        private static readonly string[] serverDirs = { "Server", "Client", "Share", "ClientServer" };

        private static readonly HashSet<string> v = new()
        {
            "Client/Scripts/Model/Client",
            "Client/Scripts/Model/Share",
            "Client/CodeMode/Model/Client",
            "Client/Scripts/ModelView/Client",
            "Client/Scripts/ModelView/Share",
            "Client/CodeMode/ModelView/Client",
            "Client/Scripts/Hotfix/Client",
            "Client/Scripts/Hotfix/Share",
            "Client/CodeMode/Hotfix/Client",
            "Client/Scripts/HotfixView/Client",
            "Client/Scripts/HotfixView/Share",
            "Client/CodeMode/HotfixView/Client",
            "Client/Scripts/Core/Client",
            "Client/Scripts/Core/Share",
            "Client/CodeMode/Core/Client",
            "Client/Scripts/Loader/Client",
            "Client/Scripts/Loader/Share",
            "Client/CodeMode/Loader/Client",

            "Server/Scripts/Model/Server",
            "Server/Scripts/Model/Share",
            "Server/CodeMode/Model/Server",
            "Server/Scripts/Hotfix/Server",
            "Server/Scripts/Hotfix/Share",
            "Server/CodeMode/Hotfix/Server",
            "Server/Scripts/Core/Server",
            "Server/Scripts/Core/Share",
            "Server/CodeMode/Core/Server",
            "Server/Scripts/Loader/Server",
            "Server/Scripts/Loader/Share",
            "Server/CodeMode/Loader/Server",

            "ClientServer/Scripts/Model/Client",
            "ClientServer/Scripts/Model/Server",
            "ClientServer/Scripts/Model/Share",
            "ClientServer/CodeMode/Model/ClientServer",
            "ClientServer/Scripts/ModelView/Client",
            "ClientServer/Scripts/ModelView/Server",
            "ClientServer/Scripts/ModelView/Share",
            "ClientServer/CodeMode/ModelView/ClientServer",
            "ClientServer/Scripts/Hotfix/Client",
            "ClientServer/Scripts/Hotfix/Server",
            "ClientServer/Scripts/Hotfix/Share",
            "ClientServer/CodeMode/Hotfix/ClientServer",
            "ClientServer/Scripts/HotfixView/Client",
            "ClientServer/Scripts/HotfixView/Server",
            "ClientServer/Scripts/HotfixView/Share",
            "ClientServer/CodeMode/HotfixView/ClientServer",
            "ClientServer/Scripts/Core/Client",
            "ClientServer/Scripts/Core/Share",
            "ClientServer/Scripts/Core/Server",
            "ClientServer/CodeMode/Core/ClientServer",
            "ClientServer/Scripts/Loader/Client",
            "ClientServer/Scripts/Loader/Share",
            "ClientServer/Scripts/Loader/Server",
            "ClientServer/CodeMode/Loader/ClientServer",
        };

        public static void ChangeToCodeMode(string codeMode, string sceneName)
        {
            HashSet<string> targetPackages = CollectAllDependencies(sceneName);

            Console.WriteLine($"目标包列表: {string.Join(", ", targetPackages)}");

            foreach (string a in moduleDirs)
            {
                if (!Directory.Exists(a))
                {
                    continue;
                }

                foreach (string moduleDir in Directory.GetDirectories(a, "cn.etetet.*"))
                {
                    string packageName = Path.GetFileName(moduleDir);
                    bool isTargetPackage = targetPackages.Contains(packageName);

                    foreach (string scriptDir in scriptDirs)
                    {
                        string p = Path.Combine(moduleDir, scriptDir);
                        if (!Directory.Exists(p))
                        {
                            continue;
                        }

                        foreach (string modelDir in modelDirs)
                        {
                            string c = Path.Combine(p, modelDir);
                            if (!Directory.Exists(c))
                            {
                                continue;
                            }

                            foreach (string serverDir in serverDirs)
                            {
                                string e = Path.Combine(c, serverDir);
                                if (!Directory.Exists(e))
                                {
                                    continue;
                                }

                                // 对于目标包，根据 codeMode 处理；对于非目标包，删除所有 AssemblyReference
                                HandleAssemblyReferenceFile(codeMode, moduleDir, scriptDir, modelDir, serverDir, isTargetPackage);
                            }
                        }
                    }
                }
            }
        }

        private static HashSet<string> CollectAllDependencies(string sceneName)
        {
            var result = new HashSet<string>();
            var rootPackage = $"cn.etetet.{sceneName.ToLower()}";

            CollectDependenciesRecursive(rootPackage, result);

            return result;
        }

        private static void CollectDependenciesRecursive(string packageName, HashSet<string> collected)
        {
            if (collected.Contains(packageName))
            {
                return;
            }

            collected.Add(packageName);

            var dependencies = GetPackageDependencies(packageName);
            foreach (var dep in dependencies)
            {
                if (dep.StartsWith("cn.etetet."))
                {
                    CollectDependenciesRecursive(dep, collected);
                }
            }
        }

        private static List<string> GetPackageDependencies(string packageName)
        {
            var result = new List<string>();

            foreach (string moduleDir in moduleDirs)
            {
                string packagePath = Path.Combine(moduleDir, packageName, "package.json");
                if (File.Exists(packagePath))
                {
                    try
                    {
                        string json = File.ReadAllText(packagePath);
                        using JsonDocument doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("dependencies", out JsonElement deps))
                        {
                            foreach (JsonProperty prop in deps.EnumerateObject())
                            {
                                result.Add(prop.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"读取 {packagePath} 失败: {ex.Message}");
                    }

                    break;
                }
            }

            return result;
        }

        private static void HandleAssemblyReferenceFile(string codeMode, string moduleDir, string scriptDir, string modelDir, string serverDir, bool isTargetPackage)
        {
            string filePath = Path.Combine(moduleDir, scriptDir, modelDir, serverDir, "AssemblyReference.asmref");
            DeleteAssemblyReference(filePath);

            // 只有目标包才根据 codeMode 创建 AssemblyReference
            if (isTargetPackage)
            {
                string path = $"{codeMode}/{scriptDir}/{modelDir}/{serverDir}";
                if (v.Contains(path))
                {
                    CreateAssemblyReference(filePath, modelDir);
                }
            }
        }

        private static void DeleteAssemblyReference(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void CreateAssemblyReference(string path, string modelDir)
        {
            File.WriteAllText(path, $"{{ \"reference\": \"ET.{modelDir}\" }}");
        }
    }
}