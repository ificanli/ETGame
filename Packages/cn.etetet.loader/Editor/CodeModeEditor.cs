using System.Diagnostics;
using UnityEditor;

namespace ET
{
    public static class CodeModeEditor
    {
        [MenuItem("ET/Loader/RefreshAssemblyReference")]
        public static void Init()
        {
            var globalConfig = UnityEngine.Resources.Load<GlobalConfig>("GlobalConfig");
            Process process = ProcessHelper.DotNet($"Bin/ET.CodeMode.dll --CodeMode={globalConfig.CodeMode.ToString()}", ".", true);
            process.WaitForExit();
            AssetDatabase.Refresh();
        }
    }
}