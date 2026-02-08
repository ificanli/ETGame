using System.IO;
using System.Diagnostics;
using Hibzz.DependencyResolver;
using UnityEditor;
using UnityEngine;

namespace ET
{
    [CustomEditor(typeof(GlobalConfig))]
    public class GlobalConfigEditor : Editor
    {
        private CodeMode codeMode;
        
        private void OnEnable()
        {
            GlobalConfig globalConfig = (GlobalConfig)this.target;
            this.codeMode = globalConfig.CodeMode;
            //globalConfig.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
            
            EditorUtility.SetDirty(globalConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            GlobalConfig globalConfig = (GlobalConfig)this.target;

            SerializedProperty iterator = this.serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    continue;
                }

                if (iterator.name == nameof(GlobalConfig.SceneName))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Set Main Package"))
                    {
                        SetMainPackageBySceneName(globalConfig);
                    }
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            this.serializedObject.ApplyModifiedProperties();

            if (globalConfig.CodeMode != this.codeMode)
            {
                this.codeMode = globalConfig.CodeMode;
                Process process = ProcessHelper.DotNet($"Bin/ET.CodeMode.dll --CodeMode={globalConfig.CodeMode} --SceneName={globalConfig.SceneName}", ".", true);
                process.WaitForExit();
                AssetDatabase.Refresh();
            }

            EditorUtility.SetDirty(globalConfig);
        }

        private static void SetMainPackageBySceneName(GlobalConfig globalConfig)
        {
            string sceneName = globalConfig.SceneName?.Trim();
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                UnityEngine.Debug.LogError("[GlobalConfigEditor] SceneName 为空，无法设置主包");
                return;
            }

            string packageName = $"cn.etetet.{sceneName}";
            string packageDirectory = Path.Combine("Packages", packageName);
            if (!Directory.Exists(packageDirectory))
            {
                UnityEngine.Debug.LogError($"[GlobalConfigEditor] 包不存在: {packageName}");
                return;
            }

            bool result = MainPackageSelector.SetAsMainPackage(packageName);
            if (!result)
            {
                UnityEngine.Debug.LogError($"[GlobalConfigEditor] 设置主包失败: {packageName}");
            }
        }
    }
}
