#if ENABLE_VIEW && UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ET
{
    [InitializeOnLoad]
    internal static class ComponentViewRepaintUpdater
    {
        private const double RepaintIntervalSeconds = 0.3d;
        private static double nextRepaintTime;

        static ComponentViewRepaintUpdater()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < nextRepaintTime)
            {
                return;
            }

            GameObject activeObject = Selection.activeGameObject;
            if (activeObject == null)
            {
                return;
            }

            ComponentView view = activeObject.GetComponent<ComponentView>();
            if (view == null || !view.isActiveAndEnabled)
            {
                return;
            }

            nextRepaintTime = EditorApplication.timeSinceStartup + RepaintIntervalSeconds;
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
#endif
