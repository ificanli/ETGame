using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace ExtenderToolBar
{
    [InitializeOnLoad]
    public static class LubanExcelToolBar
    {
        static LubanExcelToolBar()
        {
            //左右位置,间隔,可随意切换
            //ToolbarExtender.RightToolbarGUI.Add(OnLubanExcelToolbarGUI);
            ToolbarExtender.LeftToolbarGUI.Add(OnLubanExcelToolbarGUI);
        }

        private static void OnLubanExcelToolbarGUI()
        {
            GUILayout.Space(20);
            var iconContent = EditorGUIUtility.IconContent("d_Profiler.UI");
            iconContent.image = AssetDatabase.LoadAssetAtPath<Texture>("Packages/cn.etetet.yiuiluban/Editor/YIUIWindow/Icon/luban_excel_open.png");
            iconContent.tooltip = "查看配置表";
            if (GUILayout.Button(iconContent))
            {
                EditorApplication.ExecuteMenuItem("ET/YIUI Luban 配置工具");
            }

            GUILayout.Space(5);

            iconContent.image = AssetDatabase.LoadAssetAtPath<Texture>("Packages/cn.etetet.yiuiluban/Editor/YIUIWindow/Icon/luban_excel_export.png");
            iconContent.tooltip = "配置生成";
            if (GUILayout.Button(iconContent))
            {
                EditorApplication.ExecuteMenuItem("ET/Excel/ExcelExporter");
            }

            GUILayout.Space(20);
        }
    }
}