using System;
using System.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using UnityEditor;
using UnityEngine;

namespace ET.Client
{
    public static class ExportScriptableObjectEditor
    {
        private const string ExportPath = "Packages/cn.etetet.wow/Bundles/Json";
        
        [MenuItem("ET/WOW/ExportScriptableObject")]
        public static void ExportScriptableObject()
        {
            MongoRegister.Init();
            
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

                        try
                        {
                            spellScriptableObject.SpellConfig.ToJson();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"error: {scriptableObject.name}, {e}");
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
                        
                        try
                        {
                            buffScriptableObject.BuffConfig.ToJson();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"error: {scriptableObject.name}, {e}");
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
                        
                        try
                        {
                            aiScriptableObject.AIConfig.ToJson();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"error: {scriptableObject.name}, {e}");
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

            if (!Directory.Exists(ExportPath))
            {
                Directory.CreateDirectory(ExportPath);
            }
            
            File.WriteAllText(Path.Combine(ExportPath, "SpellConfigCategory.txt"), spellConfigCategory.ToJson());
            File.WriteAllText(Path.Combine(ExportPath, "BuffConfigCategory.txt"), buffConfigCategory.ToJson());
            File.WriteAllText(Path.Combine(ExportPath, "AIConfigCategory.txt"), aiConfigCategory.ToJson());
            
            Debug.Log("Export ScriptableObject OK!");
        }
    }
}