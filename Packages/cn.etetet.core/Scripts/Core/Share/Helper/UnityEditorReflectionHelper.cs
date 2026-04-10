using System;
using System.Reflection;

namespace ET
{
    public static class UnityEditorReflectionHelper
    {
        private static Type GetUnityEditorType(string typeName)
        {
            return Type.GetType($"{typeName}, UnityEditor.CoreModule") ?? Type.GetType($"{typeName}, UnityEditor");
        }

        private static MethodInfo GetLoadAssetAtPathMethod()
        {
            Type assetDatabaseType = GetUnityEditorType("UnityEditor.AssetDatabase");
            if (assetDatabaseType == null)
            {
                return null;
            }

            foreach (MethodInfo methodInfo in assetDatabaseType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (methodInfo.Name != "LoadAssetAtPath" || !methodInfo.IsGenericMethodDefinition)
                {
                    continue;
                }

                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    return methodInfo;
                }
            }

            return null;
        }

        public static void SelectGameObject(object gameObject)
        {
#if UNITY_EDITOR
            Type selectionType = GetUnityEditorType("UnityEditor.Selection");
            PropertyInfo activeGameObjectProperty = selectionType?.GetProperty("activeGameObject", BindingFlags.Public | BindingFlags.Static);
            activeGameObjectProperty?.SetValue(null, gameObject);
#endif
        }

        public static string[] FindAssets(string filter)
        {
#if UNITY_EDITOR
            Type assetDatabaseType = GetUnityEditorType("UnityEditor.AssetDatabase");
            MethodInfo findAssetsMethod =
                    assetDatabaseType?.GetMethod("FindAssets", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(string[]) }, null);
            if (findAssetsMethod == null)
            {
                return Array.Empty<string>();
            }

            return findAssetsMethod.Invoke(null, new object[] { filter, null }) as string[] ?? Array.Empty<string>();
#else
            return Array.Empty<string>();
#endif
        }

        public static string GuidToAssetPath(string guid)
        {
#if UNITY_EDITOR
            Type assetDatabaseType = GetUnityEditorType("UnityEditor.AssetDatabase");
            MethodInfo guidToAssetPathMethod =
                    assetDatabaseType?.GetMethod("GUIDToAssetPath", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (guidToAssetPathMethod == null)
            {
                return string.Empty;
            }

            return guidToAssetPathMethod.Invoke(null, new object[] { guid }) as string ?? string.Empty;
#else
            return string.Empty;
#endif
        }

        public static object LoadAssetAtPath(string path)
        {
#if UNITY_EDITOR
            MethodInfo loadAssetAtPathMethod = GetLoadAssetAtPathMethod();
            if (loadAssetAtPathMethod == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            return loadAssetAtPathMethod.MakeGenericMethod(Type.GetType("UnityEngine.Object, UnityEngine")).Invoke(null, new object[] { path });
#else
            return null;
#endif
        }
    }
}
