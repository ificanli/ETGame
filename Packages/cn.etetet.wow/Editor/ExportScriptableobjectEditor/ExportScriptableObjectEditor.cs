using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET.Client
{
    public static class ExportScriptableObjectEditor
    {
        private const string ExportPath = "Packages/cn.etetet.wow/Bundles/Bson";
        
        [MenuItem("ET/WOW/ExportScriptableObject")]
        public static void ExportScriptableObject()
        {
            // 1.获取所有的ScriptableObject
            string[] allScriptableObjects = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject");

            SpellConfigCategory spellConfigCategory = new();
            BuffConfigCategory buffConfigCategory = new();
            AIConfigCategory aiConfigCategory = new();
            
            foreach (var guid in allScriptableObjects)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (scriptableObject == null)
                {
                    continue;
                }

                try
                {
                    if (scriptableObject is SpellScriptableObject spellScriptableObject)
                    {
                        if (scriptableObject.name != spellScriptableObject.SpellConfig.Id.ToString())
                        {
                            Log.Error($"File name not match Id: {spellScriptableObject.name}");
                        }
                        spellConfigCategory.Add(spellScriptableObject.SpellConfig);
                        continue;
                    }
                
                    if (scriptableObject is BuffScriptableObject buffScriptableObject)
                    {
                        if (scriptableObject.name != buffScriptableObject.BuffConfig.Id.ToString())
                        {
                            Log.Error($"File name not match Id: {buffScriptableObject.name}");
                        }
                        buffConfigCategory.Add(buffScriptableObject.BuffConfig);
                        continue;
                    }
                
                    if (scriptableObject is AIScriptableObject aiScriptableObject)
                    {
                        if (scriptableObject.name != aiScriptableObject.AIConfig.Id.ToString())
                        {
                            Log.Error($"File name not match Id: {aiScriptableObject.name}");
                        }
                        aiConfigCategory.Add(aiScriptableObject.AIConfig);
                        continue;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"error: {path}", e);
                }
            }
            
            File.WriteAllBytes(Path.Combine(ExportPath, "SpellConfigCategory.bytes"), spellConfigCategory.ToBson());
            File.WriteAllBytes(Path.Combine(ExportPath, "BuffConfigCategory.bytes"), buffConfigCategory.ToBson());
            File.WriteAllBytes(Path.Combine(ExportPath, "AIConfigCategory.bytes"), aiConfigCategory.ToBson());
            
            Debug.Log("Export ScriptableObject OK!");
        }
    }
}