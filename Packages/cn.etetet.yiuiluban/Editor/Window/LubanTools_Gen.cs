using System.Diagnostics;
using ET;
using UnityEditor;

namespace YIUI.Luban.Editor
{


    public partial class LubanTools
    {
        [MenuItem("ET/Excel/ExcelExporter")]
        public static void MenuLubanGen()
        {
            UnityEngine.Debug.Log("开始导出Excel配置...");
            Process process = ProcessHelper.DotNet("./Packages/cn.etetet.excel/DotNet~/Exe/ET.ExcelExporter.dll", "./", true);
            UnityEngine.Debug.Log(process.StandardOutput.ReadToEnd());
            if (process.ExitCode != 0)
            {
                UnityEngine.Debug.LogError(process.StandardError.ReadToEnd());
                UnityEngine.Debug.LogError("导出Excel配置失败...");
                return;
            }
            UnityEngine.Debug.Log("导出Excel配置完成...");
        }        
    }
}