using UnityEngine;

namespace YIUIFramework
{
    public static class UnityObjectExtensions
    {
        public static void SafeDestroySelf(this Object obj)
        {
            if (obj == null) return;

            #if UNITY_EDITOR
            // 在编辑器中，如果对象是资源的一部分（不是场景实例），使用DestroyImmediate
            if (UnityEditor.AssetDatabase.Contains(obj))
            {
                Object.DestroyImmediate(obj, true);
            }
            else if (!Application.isPlaying)
            {
                Object.DestroyImmediate(obj);
            }
            else
            {
                Object.Destroy(obj);
            }
            #else
            Object.Destroy(obj);
            #endif
        }
    }
}
