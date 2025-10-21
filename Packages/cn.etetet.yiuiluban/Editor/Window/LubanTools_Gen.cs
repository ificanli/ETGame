using System.Diagnostics;
using ET;
using UnityEditor;
using UnityEngine;

namespace YIUI.Luban.Editor
{


    public partial class LubanTools
    {
        [MenuItem("ET/Excel/ExcelExporter")]
        public static void MenuLubanGen()
        {
            GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");

            string configType = globalConfig.EditorScripts ? "Json" : "Luban";
            
            ProcessHelper.DotNet($"./Bin/ET.ExcelExporter.dll {configType}", "./", true).WaitForExit();
        }        
    }
}