using UnityEngine;

namespace ET.Client
{
    public static class ECAPointViewHelper
    {
        public static void RebuildBindings(Scene scene)
        {
            Scene viewScene = ResolveViewScene(scene);
            if (viewScene == null || viewScene.IsDisposed)
            {
                return;
            }

            ECAPointViewRuntimeComponent runtime = GetOrAddRuntime(viewScene);
            if (runtime == null)
            {
                return;
            }

            runtime.PointViewMarkers.Clear();
            runtime.BindingsBuilt = true;

            ECAPointViewMarker[] markers = GameObject.FindObjectsByType<ECAPointViewMarker>(FindObjectsSortMode.None);
            foreach (ECAPointViewMarker marker in markers)
            {
                if (marker == null || string.IsNullOrWhiteSpace(marker.PointId) || marker.TargetAnimator == null)
                {
                    continue;
                }

                if (!runtime.PointViewMarkers.TryAdd(marker.PointId, marker))
                {
                    Log.Warning($"[ECAPointView] duplicate point view marker: pointId={marker.PointId}, object={marker.name}");
                }
            }

            SyncCachedStates(scene, viewScene, runtime);
        }

        public static void ApplyPointState(Scene scene, string pointId, int state)
        {
            if (string.IsNullOrWhiteSpace(pointId))
            {
                return;
            }

            Scene viewScene = ResolveViewScene(scene);
            if (viewScene == null || viewScene.IsDisposed)
            {
                return;
            }

            ECAPointViewRuntimeComponent runtime = GetOrAddRuntime(viewScene);
            if (runtime == null)
            {
                return;
            }

            if (!runtime.BindingsBuilt || !TryGetMarker(runtime, pointId, out ECAPointViewMarker marker))
            {
                RebuildBindings(scene);
                runtime = GetOrAddRuntime(viewScene);
                if (runtime == null || !TryGetMarker(runtime, pointId, out marker))
                {
                    return;
                }
            }

            bool hasPreviousState = runtime.AppliedStates.TryGetValue(pointId, out int previousState);
            if (hasPreviousState && previousState == state)
            {
                return;
            }

            ApplyState(marker, state, instant: !hasPreviousState);
            runtime.AppliedStates[pointId] = state;
        }

        private static void SyncCachedStates(Scene sourceScene, Scene viewScene, ECAPointViewRuntimeComponent runtime)
        {
            if (runtime == null)
            {
                return;
            }

            ECAInteractClientComponent interactRuntime = sourceScene?.GetComponent<ECAInteractClientComponent>() ??
                                                        viewScene?.GetComponent<ECAInteractClientComponent>();
            if (interactRuntime == null || interactRuntime.PointStates.Count == 0)
            {
                return;
            }

            foreach ((string pointId, int state) in interactRuntime.PointStates)
            {
                if (!TryGetMarker(runtime, pointId, out ECAPointViewMarker marker))
                {
                    continue;
                }

                bool hasPreviousState = runtime.AppliedStates.ContainsKey(pointId);
                ApplyState(marker, state, instant: !hasPreviousState);
                runtime.AppliedStates[pointId] = state;
            }
        }

        private static void ApplyState(ECAPointViewMarker marker, int state, bool instant)
        {
            if (marker == null || marker.TargetAnimator == null)
            {
                return;
            }

            bool isOpen = IsOpenState(marker, state);
            Animator animator = marker.TargetAnimator;

            if (marker.UseStateInt && HasParameter(animator, marker.StateIntParam, AnimatorControllerParameterType.Int))
            {
                animator.SetInteger(marker.StateIntParam, state);
            }

            if (marker.UseOpenBool && HasParameter(animator, marker.OpenBoolParam, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(marker.OpenBoolParam, isOpen);
            }

            if (instant || !marker.UseOpenCloseTrigger)
            {
                return;
            }

            string triggerName = isOpen ? marker.OpenTriggerParam : marker.CloseTriggerParam;
            if (HasParameter(animator, triggerName, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(triggerName);
            }
        }

        private static bool IsOpenState(ECAPointViewMarker marker, int state)
        {
            if (marker?.OpenStates == null || marker.OpenStates.Count == 0)
            {
                return false;
            }

            foreach (int openState in marker.OpenStates)
            {
                if (openState == state)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetMarker(ECAPointViewRuntimeComponent runtime, string pointId, out ECAPointViewMarker marker)
        {
            marker = null;
            if (runtime == null || string.IsNullOrWhiteSpace(pointId))
            {
                return false;
            }

            if (!runtime.PointViewMarkers.TryGetValue(pointId, out marker))
            {
                return false;
            }

            if (marker != null && marker.TargetAnimator != null)
            {
                return true;
            }

            runtime.PointViewMarkers.Remove(pointId);
            return false;
        }

        private static ECAPointViewRuntimeComponent GetOrAddRuntime(Scene scene)
        {
            if (scene == null || scene.IsDisposed)
            {
                return null;
            }

            ECAPointViewRuntimeComponent runtime = scene.GetComponent<ECAPointViewRuntimeComponent>();
            if (runtime == null)
            {
                runtime = scene.AddComponent<ECAPointViewRuntimeComponent>();
            }

            return runtime;
        }

        private static Scene ResolveViewScene(Scene scene)
        {
            if (scene == null || scene.IsDisposed)
            {
                return null;
            }

            CurrentScenesComponent currentScenes = scene.GetComponent<CurrentScenesComponent>();
            return currentScenes?.Scene ?? scene;
        }

        private static bool HasParameter(Animator animator, string parameterName, AnimatorControllerParameterType parameterType)
        {
            if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == parameterName && parameter.type == parameterType)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
