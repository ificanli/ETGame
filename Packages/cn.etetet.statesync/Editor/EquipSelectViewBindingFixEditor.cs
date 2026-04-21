using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Editor
{
    public static class EquipSelectViewBindingFixEditor
    {
        private const string EquipSelectViewPrefabPath =
            "Packages/cn.etetet.statesync/Assets/GameRes/YIUI/Lobby/Prefabs/EquipSelectView.prefab";

        private const string SelectImageBindName = "u_ComSelectImage";
        private const string LegacySelectImageBindName = "SelectImage";
        private const string PreviewIconNodeName = "icon";

        [MenuItem("ET/YIUI/Fix EquipSelectView SelectImage Binding")]
        public static void FixSelectImageBinding()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(EquipSelectViewPrefabPath);
            try
            {
                UIBindComponentTable componentTable = prefabRoot.GetComponent<UIBindComponentTable>();
                if (componentTable == null)
                {
                    throw new InvalidOperationException("EquipSelectView prefab missing UIBindComponentTable.");
                }

                Image previewIconImage = FindRequiredImage(prefabRoot.transform, PreviewIconNodeName);
                bool changed = ReplaceComponentBinding(componentTable, previewIconImage);
                componentTable.AutoCheck();

                EditorUtility.SetDirty(componentTable);
                EditorUtility.SetDirty(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, EquipSelectViewPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(changed
                    ? "[EquipSelectViewBindingFixEditor] Fixed SelectImage binding to icon Image."
                    : "[EquipSelectViewBindingFixEditor] SelectImage binding already points to icon Image.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[EquipSelectViewBindingFixEditor] Fix failed: {exception}");
                throw;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static bool ReplaceComponentBinding(UIBindComponentTable componentTable, Component targetComponent)
        {
            IList bindPairList = GetRequiredFieldValue(componentTable, "m_AllBindPair") as IList;
            if (bindPairList == null)
            {
                throw new InvalidOperationException("UIBindComponentTable missing m_AllBindPair.");
            }

            object matchedBindPair = null;
            FieldInfo matchedNameField = null;
            FieldInfo matchedComponentField = null;

            for (int i = 0; i < bindPairList.Count; ++i)
            {
                object bindPair = bindPairList[i];
                if (bindPair == null)
                {
                    continue;
                }

                FieldInfo nameField = FindRequiredField(bindPair.GetType(), "Name");
                FieldInfo componentField = FindRequiredField(bindPair.GetType(), "Component");
                string currentBindName = nameField.GetValue(bindPair) as string;
                if (!string.Equals(currentBindName, SelectImageBindName, StringComparison.Ordinal) &&
                    !string.Equals(currentBindName, LegacySelectImageBindName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (matchedBindPair != null)
                {
                    bindPairList.RemoveAt(i);
                    --i;
                    continue;
                }

                matchedBindPair = bindPair;
                matchedNameField = nameField;
                matchedComponentField = componentField;
            }

            if (matchedBindPair != null)
            {
                bool changed = false;
                string currentName = matchedNameField.GetValue(matchedBindPair) as string;
                if (!string.Equals(currentName, SelectImageBindName, StringComparison.Ordinal))
                {
                    matchedNameField.SetValue(matchedBindPair, SelectImageBindName);
                    changed = true;
                }

                if (!ReferenceEquals(matchedComponentField.GetValue(matchedBindPair), targetComponent))
                {
                    matchedComponentField.SetValue(matchedBindPair, targetComponent);
                    changed = true;
                }

                return changed;
            }

            Type bindPairType = bindPairList.GetType().GetGenericArguments()[0];
            object newBindPair = Activator.CreateInstance(bindPairType, true);
            FindRequiredField(bindPairType, "Name").SetValue(newBindPair, SelectImageBindName);
            FindRequiredField(bindPairType, "Component").SetValue(newBindPair, targetComponent);
            bindPairList.Add(newBindPair);
            return true;
        }

        private static Image FindRequiredImage(Transform root, string nodeName)
        {
            Transform target = FindDescendant(root, nodeName);
            if (target == null)
            {
                throw new InvalidOperationException($"EquipSelectView missing required node: {nodeName}");
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                throw new InvalidOperationException($"Node {nodeName} missing Image component.");
            }

            return image;
        }

        private static Transform FindDescendant(Transform root, string nodeName)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.name, nodeName, StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; ++i)
            {
                Transform child = FindDescendant(root.GetChild(i), nodeName);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static object GetRequiredFieldValue(object target, string fieldName)
        {
            FieldInfo fieldInfo = FindRequiredField(target.GetType(), fieldName);
            return fieldInfo.GetValue(target);
        }

        private static FieldInfo FindRequiredField(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (fieldInfo != null)
                {
                    return fieldInfo;
                }

                type = type.BaseType;
            }

            throw new InvalidOperationException($"Cannot find field: {fieldName}");
        }
    }
}
