using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameObjectComponent))]
    public static partial class GameObjectComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this GameObjectComponent self)
        {
            self.ClearConcealmentTransparencyCache();
            UnityEngine.Object.Destroy(self.GameObject);
        }

        [EntitySystem]
        private static void Awake(this GameObjectComponent self)
        {
        }

        public static void CacheRenderers(this GameObjectComponent self)
        {
            if (self == null || self.GameObject == null)
            {
                return;
            }

            self.CachedRenderers = self.GameObject.GetComponentsInChildren<Renderer>(true);
            if (self.CachedRenderers == null || self.CachedRenderers.Length == 0)
            {
                self.OriginalMaterials = null;
                self.TransparentMaterials = null;
                return;
            }

            self.OriginalMaterials = new Material[self.CachedRenderers.Length][];
            self.TransparentMaterials = new Material[self.CachedRenderers.Length][];
            for (int i = 0; i < self.CachedRenderers.Length; ++i)
            {
                self.OriginalMaterials[i] = self.CachedRenderers[i] != null ? self.CachedRenderers[i].sharedMaterials : null;
            }
        }

        public static void RefreshLocalConcealmentVisual(this GameObjectComponent self, Unit unit, Vector3 position)
        {
            if (self == null || unit == null || unit.IsDisposed)
            {
                return;
            }

            Scene scene = unit.Scene();
            if (scene == null || scene.IsDisposed)
            {
                return;
            }

            long myId = scene.Root().GetComponent<PlayerComponent>()?.MyId ?? 0;
            if (myId != unit.Id)
            {
                if (self.ConcealmentTransparencyApplied)
                {
                    self.ApplyConcealmentTransparency(1f, false);
                }
                return;
            }

            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(scene);
            runtime?.RefreshLocalConcealmentState(position);

            float alpha = runtime != null && runtime.SelfInConcealmentArea ? runtime.SelfConcealmentAlpha : 1f;
            bool transparent = runtime != null && runtime.SelfInConcealmentArea && alpha < 0.999f;
            self.ApplyConcealmentTransparency(alpha, transparent);
        }

        private static void ApplyConcealmentTransparency(this GameObjectComponent self, float alpha, bool transparent)
        {
            if (self.GameObject == null)
            {
                return;
            }

            if (self.CachedRenderers == null || self.CachedRenderers.Length == 0 || self.OriginalMaterials == null)
            {
                self.CacheRenderers();
            }

            if (!transparent)
            {
                if (!self.ConcealmentTransparencyApplied)
                {
                    return;
                }

                for (int i = 0; i < self.CachedRenderers.Length; ++i)
                {
                    Renderer renderer = self.CachedRenderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.sharedMaterials = self.OriginalMaterials[i];
                }

                self.DestroyTransparentMaterials();
                self.ConcealmentTransparencyApplied = false;
                self.ConcealmentTransparencyAlpha = 1f;
                return;
            }

            if (self.ConcealmentTransparencyApplied && Mathf.Abs(self.ConcealmentTransparencyAlpha - alpha) < 0.001f)
            {
                return;
            }

            self.DestroyTransparentMaterials();

            if (self.CachedRenderers == null)
            {
                return;
            }

            for (int i = 0; i < self.CachedRenderers.Length; ++i)
            {
                Renderer renderer = self.CachedRenderers[i];
                Material[] originalMaterials = self.OriginalMaterials[i];
                if (renderer == null || originalMaterials == null || originalMaterials.Length == 0)
                {
                    continue;
                }

                Material[] clonedMaterials = new Material[originalMaterials.Length];
                for (int j = 0; j < originalMaterials.Length; ++j)
                {
                    Material sourceMaterial = originalMaterials[j];
                    if (sourceMaterial == null)
                    {
                        continue;
                    }

                    Material material = new Material(sourceMaterial);
                    ConfigureTransparentMaterial(material, alpha);
                    clonedMaterials[j] = material;
                }

                self.TransparentMaterials[i] = clonedMaterials;
                renderer.materials = clonedMaterials;
            }

            self.ConcealmentTransparencyApplied = true;
            self.ConcealmentTransparencyAlpha = alpha;
        }

        private static void ConfigureTransparentMaterial(Material material, float alpha)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }
            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }
            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }
            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;

            if (material.HasProperty("_BaseColor"))
            {
                Color color = material.GetColor("_BaseColor");
                color.a = alpha;
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color");
                color.a = alpha;
                material.SetColor("_Color", color);
            }
        }

        private static void ClearConcealmentTransparencyCache(this GameObjectComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.DestroyTransparentMaterials();
            self.CachedRenderers = null;
            self.OriginalMaterials = null;
            self.TransparentMaterials = null;
            self.ConcealmentTransparencyApplied = false;
            self.ConcealmentTransparencyAlpha = 1f;
        }

        private static void DestroyTransparentMaterials(this GameObjectComponent self)
        {
            if (self.TransparentMaterials == null)
            {
                return;
            }

            for (int i = 0; i < self.TransparentMaterials.Length; ++i)
            {
                Material[] materials = self.TransparentMaterials[i];
                if (materials == null)
                {
                    continue;
                }

                for (int j = 0; j < materials.Length; ++j)
                {
                    if (materials[j] != null)
                    {
                        UnityEngine.Object.Destroy(materials[j]);
                    }
                }

                self.TransparentMaterials[i] = null;
            }
        }
    }

    [EntitySystemOf(typeof(UnitViewInterpolationComponent))]
    public static partial class UnitViewInterpolationComponentSystem
    {
        private const float MaxVisualSpeedMultiplier = 1.2f;
        private const float ArriveDistanceSqr = 0.0001f;
        private const float VisualMoveDistanceSqr = 0.000001f;
        private const float MinSnapDistance = 2f;
        private const float SnapDistanceBySpeedSeconds = 0.35f;
        private const float MinPredictionCorrectionMoveSpeed = 3f;
        private const float PredictionCorrectionSpeedRatio = 0.5f;
        private const float MaxPredictionCorrectionDistance = 5f;

        [EntitySystem]
        private static void Awake(this UnitViewInterpolationComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            Vector3 startPosition = gameObjectComponent?.Transform != null ? gameObjectComponent.Transform.position : unit.Position;
            self.TargetPosition = startPosition;
            self.Initialized = false;
            self.PredictionEnabled = unit.IsMyUnit();
            self.SkipNextChangePositionSync = false;
            self.AuthoritativePosition = startPosition;
            self.PredictedDelta = Vector3.zero;
            self.VisualCorrection = Vector3.zero;
            self.PredictedDirection = Vector3.zero;
            self.PredictedSpeed = 0f;
        }

        [EntitySystem]
        private static void Destroy(this UnitViewInterpolationComponent self)
        {
        }

        [EntitySystem]
        private static void Update(this UnitViewInterpolationComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            Transform transform = gameObjectComponent?.Transform;
            if (transform == null)
            {
                return;
            }

            if (!self.Initialized)
            {
                transform.position = self.TargetPosition;
                self.Initialized = true;
                self.RefreshVisualState(unit, transform.position, transform.position);
                return;
            }

            if (self.PredictionEnabled)
            {
                Vector3 oldPredictedViewPosition = transform.position;
                self.AdvancePrediction(unit);
                self.UpdatePredictionTarget();
                transform.position = self.TargetPosition;
                self.RefreshVisualState(unit, oldPredictedViewPosition, transform.position);
                return;
            }

            Vector3 oldViewPosition = transform.position;
            Vector3 delta = self.TargetPosition - oldViewPosition;
            if (self.ShouldSnap(unit, delta))
            {
                transform.position = self.TargetPosition;
                self.RefreshVisualState(unit, oldViewPosition, transform.position);
                return;
            }

            if (delta.sqrMagnitude <= ArriveDistanceSqr)
            {
                if ((transform.position - self.TargetPosition).sqrMagnitude > 0f)
                {
                    transform.position = self.TargetPosition;
                }

                self.RefreshVisualState(unit, oldViewPosition, transform.position);
                return;
            }

            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            float maxStep = speed * MaxVisualSpeedMultiplier * Time.deltaTime;
            transform.position = Vector3.MoveTowards(oldViewPosition, self.TargetPosition, maxStep);

            if ((self.TargetPosition - transform.position).sqrMagnitude <= ArriveDistanceSqr)
            {
                transform.position = self.TargetPosition;
            }

            self.RefreshVisualState(unit, oldViewPosition, transform.position);
        }

        public static void SetTargetPosition(this UnitViewInterpolationComponent self, float3 targetPosition)
        {
            // 服务端移动已做权威贴地，视图层直接使用同步过来的目标位置，
            // 避免客户端再次射线贴地造成Y轴抖动和相机跟随抖动。
            self.TargetPosition = targetPosition;
            if (!self.PredictionEnabled)
            {
                self.AuthoritativePosition = targetPosition;
                self.PredictedDelta = Vector3.zero;
                self.VisualCorrection = Vector3.zero;
            }
        }

        public static void ApplyAuthoritativePosition(this UnitViewInterpolationComponent self, float3 authoritativePosition)
        {
            Vector3 serverPosition = authoritativePosition;
            if (!self.PredictionEnabled)
            {
                self.SetTargetPosition(serverPosition);
                return;
            }

            Vector3 previousVisualTarget = self.BuildPredictionTarget();
            self.AuthoritativePosition = serverPosition;
            self.PredictedDelta = Vector3.zero;
            self.VisualCorrection = previousVisualTarget - serverPosition;
            if (self.VisualCorrection.sqrMagnitude > MaxPredictionCorrectionDistance * MaxPredictionCorrectionDistance)
            {
                self.VisualCorrection = self.VisualCorrection.normalized * MaxPredictionCorrectionDistance;
            }

            self.TargetPosition = self.BuildPredictionTarget();
        }

        public static void SetPredictionMotion(this UnitViewInterpolationComponent self, float3 predictedDirection, float predictedSpeed)
        {
            self.PredictedDirection = predictedDirection;
            self.PredictedSpeed = predictedSpeed;
        }

        public static void ResetPrediction(this UnitViewInterpolationComponent self, float3 position, bool skipNextChangePositionSync = false)
        {
            Vector3 resetPosition = position;
            self.TargetPosition = resetPosition;
            self.AuthoritativePosition = resetPosition;
            self.PredictedDelta = Vector3.zero;
            self.VisualCorrection = Vector3.zero;
            self.PredictedDirection = Vector3.zero;
            self.PredictedSpeed = 0f;
            self.SkipNextChangePositionSync = skipNextChangePositionSync;
        }

        private static void AdvancePrediction(this UnitViewInterpolationComponent self, Unit unit)
        {
            if (self.PredictedSpeed < 0.01f || Time.deltaTime <= 0f || self.PredictedDirection.sqrMagnitude < 0.000001f)
            {
                return;
            }

            Vector3 predictionStep = self.PredictedDirection * (self.PredictedSpeed * Time.deltaTime);
            self.PredictedDelta += predictionStep;

            TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
            if (turnComponent == null || !turnComponent.IsTurning())
            {
                unit.Rotation = quaternion.LookRotation((float3)self.PredictedDirection, math.up());
            }
        }

        private static void UpdatePredictionTarget(this UnitViewInterpolationComponent self)
        {
            float correctionSpeed = Mathf.Max(self.PredictedSpeed * PredictionCorrectionSpeedRatio, MinPredictionCorrectionMoveSpeed);
            self.VisualCorrection = Vector3.MoveTowards(self.VisualCorrection, Vector3.zero, correctionSpeed * Time.deltaTime);
            self.TargetPosition = self.BuildPredictionTarget();
        }

        private static Vector3 BuildPredictionTarget(this UnitViewInterpolationComponent self)
        {
            return self.AuthoritativePosition + self.PredictedDelta + self.VisualCorrection;
        }

        private static bool ShouldSnap(this UnitViewInterpolationComponent self, Unit unit, Vector3 delta)
        {
            if (self.PredictionEnabled)
            {
                return delta.sqrMagnitude >= MaxPredictionCorrectionDistance * MaxPredictionCorrectionDistance;
            }

            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            float snapDistance = Mathf.Max(MinSnapDistance, speed * SnapDistanceBySpeedSeconds);
            return delta.sqrMagnitude >= snapDistance * snapDistance;
        }

        private static void RefreshVisualState(this UnitViewInterpolationComponent self, Unit unit, Vector3 oldViewPosition, Vector3 newViewPosition)
        {
            Vector3 visualDelta = newViewPosition - oldViewPosition;
            AnimatorComponent animator = unit.GetComponent<AnimatorComponent>();
            if (animator != null)
            {
                float moveSpeed = visualDelta.sqrMagnitude > VisualMoveDistanceSqr
                    ? unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f
                    : 0f;
                animator.SetFloat(nameof(MotionType.MoveSpeed), moveSpeed);
            }

            CinemachineComponent cinemachineComponent = unit.GetComponent<CinemachineComponent>();
            if (cinemachineComponent?.Follow != null && cinemachineComponent.Head != null)
            {
                cinemachineComponent.Follow.position = cinemachineComponent.Head.position;
            }

            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            gameObjectComponent?.RefreshLocalConcealmentVisual(unit, newViewPosition);
        }
    }
}
