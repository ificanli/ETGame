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
            ProcessHelper.DotNet("./Packages/cn.etetet.excel/DotNet~/Exe/ET.ExcelExporter.dll", "./", true).WaitForExit();
        }        
    }
}