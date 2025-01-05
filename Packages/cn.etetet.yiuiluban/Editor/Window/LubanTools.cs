using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YIUI.Luban.Editor
{
    public partial class LubanTools : EditorWindow
    {
        [MenuItem("ET/Luban 配置工具")]
        private static void OpenWindow()
        {
            var window = GetWindow<LubanTools>("Luban 配置工具");
            if (window != null)
                window.Show();
        }

        public static void CloseWindow()
        {
            GetWindow<LubanTools>()?.Close();
        }

        public static void CloseWindowRefresh()
        {
            CloseWindow();
            AssetDatabase.SaveAssets();
            if (!EditorApplication.ExecuteMenuItem("ET/Loader/ReGenerateProjectAssemblyReference"))
            {
                ReGenerateProjectAssemblyReference();
            }

            EditorApplication.ExecuteMenuItem("Assets/Refresh");
        }

        private void OnGUI()
        {
            if (!GenPackageExists())
            {
                if (GUILayout.Button("创建 LubanGen包", GUILayout.Height(50)))
                {
                    InitGen();
                }
            }
            else
            {
                OnGUITools();
            }
        }

        private void OnGUITools()
        {
            if (GUILayout.Button("Luban 官方文档"))
            {
                Application.OpenURL("https://luban.doc.code-philosophy.com/docs/intro");
            }

            if (GUILayout.Button("Luban YIUI文档"))
            {
                Application.OpenURL("https://lib9kmxvq7k.feishu.cn/wiki/W1ylwC9xDip1YQk4eijcxgO9nh0");
            }

            GUILayout.Space(10);

            if (GUILayout.Button("导出", GUILayout.ExpandWidth(true), GUILayout.Height(50)))
            {
                LubanGen();
            }

            if (GUILayout.Button("仅生成Conf", GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            {
                var result = CreateLubanConf();
                UnityTipsHelper.Show($"Luban生成Conf {(result ? "成功" : "失败")}");
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("选择目标ET包文件夹 创建配置模版");

            if (GUILayout.Button("创建模版", GUILayout.Height(50)))
            {
                var folderPath = EditorUtility.OpenFolderPanel("选择 ET包", "", "");
                if (!string.IsNullOrEmpty(folderPath))
                {
                    CreateToPackage(folderPath);
                }
            }

            GUILayout.Space(20);

            if (DemoPackageExists())
            {
                if (GUILayout.Button("删除 LubanDemo包", GUILayout.Height(50)))
                {
                    DeleteLubanDemoPackage();
                }
            }
            else
            {
                if (GUILayout.Button("创建 LubanDemo包", GUILayout.Height(50)))
                {
                    CreateLubanDemoPackage();
                }
            }

            if (GUILayout.Button("替换 ET.Excel To Luban", GUILayout.Height(30)))
            {
                if (ReplaceAll(true))
                {
                    CloseWindowRefresh();
                }
            }

            if (GUILayout.Button("同步覆盖 LoaderInvoker", GUILayout.Height(30)))
            {
                SyncInvoke();
            }
        }
    }
}