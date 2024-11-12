using System;
using System.Linq;
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
            EditorResHelper.SaveAssets(globalConfig);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            GlobalConfig globalConfig = (GlobalConfig)this.target;
            if (globalConfig.CodeMode != this.codeMode)
            {
                this.codeMode = globalConfig.CodeMode;
                CodeModeChangeHelper.ChangeToCodeMode(codeMode.ToString());
                ReGenerateProjectFilesHelper.Run();
                AssetDatabase.Refresh();
            }
            /*
            string sceneName = EditorGUILayout.TextField($"SceneName", globalConfig.SceneName);
            if (sceneName != globalConfig.SceneName)
            {
                globalConfig.SceneName = sceneName;
                EditorResHelper.SaveAssets(globalConfig);
                AssetDatabase.Refresh();
            }
            
            bool enbaleDll = EditorGUILayout.Toggle($"EnableDll", globalConfig.EnableDll);
            if (enbaleDll != globalConfig.EnableDll)
            {
                globalConfig.EnableDll = enbaleDll;
                EditorResHelper.SaveAssets(globalConfig);
                AssetDatabase.Refresh();
            }
            
            string address = EditorGUILayout.TextField($"Address", globalConfig.Address);
            if (address != globalConfig.Address)
            {
                globalConfig.Address = address;
                EditorResHelper.SaveAssets(globalConfig);
                AssetDatabase.Refresh();
            }
            */
        }
    }
}