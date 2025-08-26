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
            ProcessHelper.DotNet("./Bin/ET.ExcelExporter.dll", "./", true).WaitForExit();
        }        
    }
}