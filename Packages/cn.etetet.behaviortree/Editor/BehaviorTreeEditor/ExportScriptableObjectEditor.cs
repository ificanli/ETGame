using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using UnityEditor;
using UnityEngine;

namespace ET.Client
{
    public static class ExportScriptableObjectEditor
    {
        private const string ExportPath = "Packages/cn.etetet.map/Bundles/Json";
        
        [MenuItem("ET/Map/ExportScriptableObject _F3")]
        public static void ExportScriptableObject()
        {
            MongoRegister.Init();

            // 1.获取所有的ScriptableObject
            string[] allScriptableObjects = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject");

            // 1.5 第一遍遍历：检查并设置TreeId
            foreach (var guid in allScriptableObjects)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (scriptableObject == null)
                {
                    continue;
                }

                bool needsSave = false;

                try
                {
                    if (scriptableObject is SpellScriptableObject spellScriptableObject)
                    {
                        if (CheckAndSetTreeId(spellScriptableObject.SpellConfig.Cost))
                        {
                            needsSave = true;
                        }
                        if (CheckAndSetTreeId(spellScriptableObject.SpellConfig.TargetSelector))
                        {
                            needsSave = true;
                        }
                    }
                    else if (scriptableObject is BuffScriptableObject buffScriptableObject)
                    {
                        if (buffScriptableObject.BuffConfig.Effects != null)
                        {
                            foreach (EffectNode effect in buffScriptableObject.BuffConfig.Effects)
                            {
                                if (CheckAndSetTreeId(effect))
                                {
                                    needsSave = true;
                                }
                            }
                        }
                    }
                    else if (scriptableObject is AIScriptableObject aiScriptableObject)
                    {
                        if (CheckAndSetTreeId(aiScriptableObject.AIConfig.Root))
                        {
                            needsSave = true;
                        }
                    }

                    // 如果有修改，保存资源
                    if (needsSave)
                    {
                        EditorUtility.SetDirty(scriptableObject);
                        Debug.Log($"Updated TreeId for: {scriptableObject.name}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error checking TreeId for {path}: {e}");
                }
            }

            // 保存所有修改的资源
            AssetDatabase.SaveAssets();

            // 2. 第二遍遍历：导出数据
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

                        // 校验CostNode的BTInput合法性
                        if (spellScriptableObject.SpellConfig.Cost != null)
                        {
                            if (!ValidateBTRoot(spellScriptableObject.SpellConfig.Cost, out List<string> costErrors))
                            {
                                string errorMessage = $"Validation failed for SpellConfig {spellScriptableObject.name} Cost:\n" + string.Join("\n", costErrors);
                                Debug.LogError(errorMessage);
                                throw new Exception(errorMessage);
                            }
                        }

                        // 校验TargetSelector的BTInput合法性
                        if (spellScriptableObject.SpellConfig.TargetSelector != null)
                        {
                            if (!ValidateBTRoot(spellScriptableObject.SpellConfig.TargetSelector, out List<string> targetErrors))
                            {
                                string errorMessage = $"Validation failed for SpellConfig {spellScriptableObject.name} TargetSelector:\n" + string.Join("\n", targetErrors);
                                Debug.LogError(errorMessage);
                                throw new Exception(errorMessage);
                            }
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

                        // 校验Effects中每个EffectNode的BTInput合法性
                        if (buffScriptableObject.BuffConfig.Effects != null)
                        {
                            for (int i = 0; i < buffScriptableObject.BuffConfig.Effects.Count; i++)
                            {
                                EffectNode effect = buffScriptableObject.BuffConfig.Effects[i];
                                if (effect != null)
                                {
                                    if (!ValidateBTRoot(effect, out List<string> effectErrors))
                                    {
                                        string errorMessage = $"Validation failed for BuffConfig {buffScriptableObject.name} Effects[{i}] ({effect.GetType().Name}):\n" + string.Join("\n", effectErrors);
                                        Debug.LogError(errorMessage);
                                        throw new Exception(errorMessage);
                                    }
                                }
                            }
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

                        // 校验BTInput的合法性
                        if (!ValidateBTRoot(aiScriptableObject.AIConfig.Root, out List<string> errors))
                        {
                            string errorMessage = $"Validation failed for AIConfig {aiScriptableObject.name}:\n" + string.Join("\n", errors);
                            Debug.LogError(errorMessage);
                            throw new Exception(errorMessage);
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
        
        
        [MenuItem("ET/Map/ReloadScriptableObject _F4")]
        public static void ReloadScriptableObject()
        {
            World.Instance.AddSingleton<ConfigLoader>().LoadAsync().NoContext();
        }

        /// <summary>
        /// 校验BTRoot的所有BTInput字段是否合法
        /// </summary>
        /// <param name="root">BTRoot节点</param>
        /// <param name="errors">输出错误信息列表</param>
        /// <returns>是否所有BTInput都合法</returns>
        private static bool ValidateBTRoot(BTRoot root, out List<string> errors)
        {
            errors = new List<string>();

            if (root == null)
            {
                return true;
            }

            // 遍历所有节点进行校验
            BTNodeValidator.ValidateNodeRecursive(root, new Dictionary<string, Type>(), errors);

            return errors.Count == 0;
        }

        /// <summary>
        /// 检查并设置BTRoot的TreeId
        /// </summary>
        /// <param name="root">BTRoot节点</param>
        /// <returns>是否TreeId被修改</returns>
        private static bool CheckAndSetTreeId(BTRoot root)
        {
            if (root == null)
            {
                return false;
            }

            // 检查根节点的TreeId
            if (root.TreeId == 0)
            {
                root.TreeId = RandomGenerator.RandInt64();
                Debug.Log($"Set TreeId for {root.GetType().Name}: {root.TreeId}");
                return true;
            }

            return false;
        }
    }
}