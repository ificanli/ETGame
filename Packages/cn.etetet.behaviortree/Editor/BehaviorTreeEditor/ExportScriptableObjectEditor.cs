using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
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

            // 1.5 第一遍遍历：确保TreeId非0且全局唯一（发现重复则自动修复并保存）
            EnsureAllTreeIdUniqueAndSave(allScriptableObjects);

            // 2. 第二遍遍历：导出数据
            SpellConfigCategory spellConfigCategory = new();
            BuffConfigCategory buffConfigCategory = new();
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

            JsonWriterSettings jsonWriterSettings = new JsonWriterSettings() 
            { 
                Indent = true, 
                IndentChars = "\t", 
                NewLineChars = "\n", 
                OutputMode = JsonOutputMode.Shell 
            };
            File.WriteAllText(Path.Combine(ExportPath, "SpellConfigCategory.txt"), ((object)spellConfigCategory).ToJson(jsonWriterSettings));
            File.WriteAllText(Path.Combine(ExportPath, "BuffConfigCategory.txt"), ((object)buffConfigCategory).ToJson(jsonWriterSettings));
            
            Debug.Log("Export ScriptableObject OK!");
        }
        
        
        [MenuItem("ET/Map/ReloadScriptableObject _F4")]
        public static void ReloadScriptableObject()
        {
            Reload().Coroutine();
            
            async ETTask Reload()
            {
                await World.Instance.AddSingleton<ConfigLoader>().LoadAsync();
                Log.Debug($"reload config finish!");
            }
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

        private sealed class TreeIdOwnerInfo
        {
            public string AssetPath;
            public string AssetName;
            public string FieldPath;
            public string NodeType;
        }

        private static void EnsureAllTreeIdUniqueAndSave(string[] allScriptableObjects)
        {
            Dictionary<long, TreeIdOwnerInfo> firstOwnerByTreeId = new();
            HashSet<long> usedTreeIds = new();
            HashSet<ScriptableObject> dirtyAssets = new();

            int totalNodes = 0;
            int fixedZeroCount = 0;
            int fixedDuplicateCount = 0;

            foreach (string guid in allScriptableObjects)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset == null)
                {
                    continue;
                }

                try
                {
                    if (asset is SpellScriptableObject spellScriptableObject)
                    {
                        ProcessTreeId(asset, assetPath, "SpellConfig.Cost", spellScriptableObject.SpellConfig.Cost, usedTreeIds, firstOwnerByTreeId, dirtyAssets, ref totalNodes, ref fixedZeroCount, ref fixedDuplicateCount);
                        ProcessTreeId(asset, assetPath, "SpellConfig.TargetSelector", spellScriptableObject.SpellConfig.TargetSelector, usedTreeIds, firstOwnerByTreeId, dirtyAssets, ref totalNodes, ref fixedZeroCount, ref fixedDuplicateCount);
                        continue;
                    }

                    if (asset is BuffScriptableObject buffScriptableObject)
                    {
                        if (buffScriptableObject.BuffConfig.Effects != null)
                        {
                            for (int i = 0; i < buffScriptableObject.BuffConfig.Effects.Count; i++)
                            {
                                EffectNode effect = buffScriptableObject.BuffConfig.Effects[i];
                                ProcessTreeId(asset, assetPath, $"BuffConfig.Effects[{i}]", effect, usedTreeIds, firstOwnerByTreeId, dirtyAssets, ref totalNodes, ref fixedZeroCount, ref fixedDuplicateCount);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error checking TreeId for {assetPath}: {e}");
                }
            }

            if (dirtyAssets.Count > 0)
            {
                foreach (ScriptableObject dirtyAsset in dirtyAssets)
                {
                    EditorUtility.SetDirty(dirtyAsset);
                }

                AssetDatabase.SaveAssets();
            }

            if (fixedZeroCount > 0 || fixedDuplicateCount > 0)
            {
                Debug.Log($"TreeId check finished. TotalNodes={totalNodes}, FixedZero={fixedZeroCount}, FixedDuplicate={fixedDuplicateCount}, SavedAssets={dirtyAssets.Count}");
            }
        }

        private static void ProcessTreeId(
            ScriptableObject asset,
            string assetPath,
            string fieldPath,
            BTRoot root,
            HashSet<long> usedTreeIds,
            Dictionary<long, TreeIdOwnerInfo> firstOwnerByTreeId,
            HashSet<ScriptableObject> dirtyAssets,
            ref int totalNodes,
            ref int fixedZeroCount,
            ref int fixedDuplicateCount)
        {
            if (root == null)
            {
                return;
            }

            totalNodes++;

            TreeIdOwnerInfo ownerInfo = new()
            {
                AssetPath = assetPath,
                AssetName = asset.name,
                FieldPath = fieldPath,
                NodeType = root.GetType().Name,
            };

            long oldTreeId = root.TreeId;

            if (oldTreeId != 0 && usedTreeIds.Add(oldTreeId))
            {
                firstOwnerByTreeId[oldTreeId] = ownerInfo;
                return;
            }

            long newTreeId = GenerateUniqueTreeId(usedTreeIds);
            root.TreeId = newTreeId;
            dirtyAssets.Add(asset);
            firstOwnerByTreeId[newTreeId] = ownerInfo;

            if (oldTreeId == 0)
            {
                fixedZeroCount++;
                Debug.Log($"Set TreeId for {ownerInfo.NodeType}: {newTreeId} ({ownerInfo.AssetName} @ {ownerInfo.FieldPath})");
                return;
            }

            fixedDuplicateCount++;
            if (firstOwnerByTreeId.TryGetValue(oldTreeId, out TreeIdOwnerInfo firstOwner))
            {
                Debug.LogWarning($"Duplicate TreeId detected: {oldTreeId}\nFirst: {firstOwner.AssetPath} ({firstOwner.AssetName} @ {firstOwner.FieldPath}, {firstOwner.NodeType})\nNow:   {ownerInfo.AssetPath} ({ownerInfo.AssetName} @ {ownerInfo.FieldPath}, {ownerInfo.NodeType})\nReset: {oldTreeId} -> {newTreeId}");
            }
            else
            {
                Debug.LogWarning($"Duplicate TreeId detected: {oldTreeId} @ {ownerInfo.AssetPath} ({ownerInfo.AssetName} @ {ownerInfo.FieldPath}, {ownerInfo.NodeType}) -> Reset to {newTreeId}");
            }
        }

        private static long GenerateUniqueTreeId(HashSet<long> usedTreeIds)
        {
            long treeId;
            do
            {
                treeId = RandomGenerator.RandInt64();
            } while (treeId == 0 || usedTreeIds.Contains(treeId));

            usedTreeIds.Add(treeId);
            return treeId;
        }
    }
}
