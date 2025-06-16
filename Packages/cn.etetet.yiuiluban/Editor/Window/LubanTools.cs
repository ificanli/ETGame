using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YIUI.Luban.Editor
{
    public partial class LubanTools
    {
        public static Action CloseWindow;

        public static Action CloseWindowRefresh;

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
                MenuLubanGen();
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
                    CloseWindowRefresh?.Invoke();
                }
            }

            if (GUILayout.Button("同步覆盖 LoaderInvoker", GUILayout.Height(30)))
            {
                SyncInvoke();
            }
        }
    }
}