using System;
using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Settings;
using UnityEditor;

namespace ET
{
    public static class HybridCLREditor
    {
        [MenuItem("ET/HybridCLR/CopyAotDlls")]
        public static void CopyAotDll()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string fromDir = Path.Combine(HybridCLRSettings.Instance.strippedAOTDllOutputRootDir, target.ToString());
            const string toDir = "Packages/cn.etetet.loader/Bundles/AotDlls";

            if (!Directory.Exists(fromDir))
            {
                throw new DirectoryNotFoundException($"未找到裁剪后的 AOT dll 目录: {fromDir}");
            }

            if (Directory.Exists(toDir))
            {
                Directory.Delete(toDir, true);
            }
            Directory.CreateDirectory(toDir);

            List<string> copiedDlls = new();
            List<string> missingDlls = new();

            foreach (string aotDll in HybridCLRSettings.Instance.patchAOTAssemblies ?? Array.Empty<string>())
            {
                string sourcePath = Path.Combine(fromDir, aotDll);
                if (!File.Exists(sourcePath))
                {
                    missingDlls.Add(aotDll);
                    continue;
                }

                File.Copy(sourcePath, Path.Combine(toDir, $"{aotDll}.bytes"), true);
                copiedDlls.Add(aotDll);
            }

            if (copiedDlls.Count == 0)
            {
                throw new FileNotFoundException($"未复制到任何 AOT dll，请检查裁剪产物目录: {fromDir}");
            }

            if (missingDlls.Count > 0)
            {
                UnityEngine.Debug.LogWarning($"[HybridCLREditor] 以下 AOT dll 在裁剪产物中不存在，已跳过: {string.Join(", ", missingDlls)}");
            }

            UnityEngine.Debug.Log($"[HybridCLREditor] CopyAotDll Finish! copied:{copiedDlls.Count} source:{fromDir}");
            
            AssetDatabase.Refresh();
        }
        
        [MenuItem("ET/HybridCLR/Init")]
        public static void Init()
        {
            const string fromFile = "Packages/cn.etetet.hybridclr/HybridCLR/AssemblyReferenceToLoader.asmref";
            
            const string toDir = "Assets/HybridCLR";
            if (Directory.Exists(toDir))
            {
                Directory.Delete(toDir, true);
            }
            Directory.CreateDirectory(toDir);
            
            File.Copy(fromFile, Path.Combine(toDir, "AssemblyReferenceToLoader.asmref"));
        }
    }
}
