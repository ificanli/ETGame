using System;
using System.Text;
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
        private const float HorizontalAnimationDistanceSqr = 0.00015625f;
        private const float MinVisualAnimationSpeed = 0.35f;
        private const float SuspiciousWalkVisualDistanceSqr = 0.0004f;
        private const float MinSnapDistance = 2f;
        private const float SnapDistanceBySpeedSeconds = 0.35f;
        private const float PredictionCorrectionSpeedRatio = 0.3f;
        private const float MaxPredictionCorrectionDistance = 5f;
        private const float PredictionDirectionChangeDot = 0.75f;
        private const float MinAuthoritativePredictionDistance = 0.001f;
        private const float ConstrainedPredictionDirectionDot = 0.98f;
        private const float MaxAuthoritativeSyncDeltaTime = 0.1f;
        private const float MaxMovingPredictionLeadSeconds = 0.12f;
        private const float MaxReplayTargetShiftSeconds = 0.05f;
        private const float MaxStationaryPredictionLeadSeconds = 0.12f;

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
            self.PositionPredictionDirection = Vector3.zero;
            self.PositionPredictionSpeed = 0f;
            self.PositionPredictionBlocked = false;
            self.HoldLocalPredictionOnStationarySync = false;
            self.LastAuthoritativeSyncTime = 0;
            self.LastPredictionTraceLogTime = 0;
            self.LastVisualMoveTraceLogTime = 0;
            self.LastAnimatorStateTraceLogTime = 0;
            self.LastFrameTraceLogTime = 0;
            self.WallBlockedCount = 0;
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
                self.TraceFrameState(unit, transform.position, transform.position);
                return;
            }

            if (self.PredictionEnabled)
            {
                Vector3 oldPredictedViewPosition = transform.position;
                self.AdvancePrediction(unit);
                self.UpdatePredictionTarget(unit);
                self.MoveVisualTowardsTarget(unit, transform, oldPredictedViewPosition);
                self.RefreshVisualState(unit, oldPredictedViewPosition, transform.position);
                self.TraceFrameState(unit, oldPredictedViewPosition, transform.position);
                return;
            }

            Vector3 oldViewPosition = transform.position;
            self.MoveVisualTowardsTarget(unit, transform, oldViewPosition);
            self.RefreshVisualState(unit, oldViewPosition, transform.position);
            self.TraceFrameState(unit, oldViewPosition, transform.position);
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
                self.PositionPredictionDirection = Vector3.zero;
                self.PositionPredictionSpeed = 0f;
                self.PositionPredictionBlocked = false;
                self.HoldLocalPredictionOnStationarySync = false;
                self.LastAuthoritativeSyncTime = 0;
                self.WallBlockedCount = 0;
            }
        }

        public static void ApplyAuthoritativePosition(this UnitViewInterpolationComponent self, float3 authoritativePosition)
        {
            Unit unit = self.GetParent<Unit>();
            Vector3 serverPosition = authoritativePosition;
            if (!self.PredictionEnabled)
            {
                self.SetTargetPosition(serverPosition);
                return;
            }

            Vector3 previousVisualTarget = self.BuildPredictionTarget();
            bool locallyStopped = unit.IsMyUnit() &&
                self.PredictedSpeed <= 0.01f &&
                self.PositionPredictionSpeed <= 0.01f;
            if (locallyStopped)
            {
                // 松手后直接以权威点为纠偏基准，避免旧视觉目标在服务端尾包阶段持续放大 VisualCorrection。
                previousVisualTarget = serverPosition;
            }
            else if (self.PositionPredictionBlocked)
            {
                previousVisualTarget = self.ClampStationaryPredictionLead(serverPosition, previousVisualTarget);
            }
            else
            {
                previousVisualTarget = self.ClampPredictionTargetToNavmesh(unit, previousVisualTarget, serverPosition);
                if (self.HoldLocalPredictionOnStationarySync)
                {
                    previousVisualTarget = self.ClampStationaryPredictionLead(serverPosition, previousVisualTarget);
                }
            }

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
            Vector3 normalizedDirection = ((Vector3)predictedDirection).sqrMagnitude > 0.000001f
                ? ((Vector3)predictedDirection).normalized
                : Vector3.zero;
            bool directionChanged = normalizedDirection.sqrMagnitude > 0.000001f &&
                (self.PredictedDirection.sqrMagnitude <= 0.000001f ||
                    Vector3.Dot(self.PredictedDirection.normalized, normalizedDirection) < PredictionDirectionChangeDot);

            self.PredictedDirection = normalizedDirection;
            self.PredictedSpeed = predictedSpeed;

            if (predictedSpeed < 0.01f || normalizedDirection.sqrMagnitude <= 0.000001f)
            {
                self.PositionPredictionDirection = Vector3.zero;
                self.PositionPredictionSpeed = 0f;
                self.PositionPredictionBlocked = false;
                self.HoldLocalPredictionOnStationarySync = false;
                self.WallBlockedCount = 0;
                return;
            }

            if (directionChanged)
            {
                self.PositionPredictionDirection = normalizedDirection;
                self.PositionPredictionSpeed = predictedSpeed;
                self.PositionPredictionBlocked = false;
                return;
            }

            if (!self.PositionPredictionBlocked && self.PositionPredictionSpeed < 0.01f)
            {
                self.PositionPredictionDirection = normalizedDirection;
                self.PositionPredictionSpeed = predictedSpeed;
            }
        }

        public static void FreezePredictionOnLocalStop(this UnitViewInterpolationComponent self, float3 currentViewPosition)
        {
            if (!self.PredictionEnabled)
            {
                self.SetPredictionMotion(float3.zero, 0f);
                self.SetConstrainedPredictionMotion(float3.zero, 0f, false);
                return;
            }

            Vector3 viewPosition = currentViewPosition;
            self.PredictedDirection = Vector3.zero;
            self.PredictedSpeed = 0f;
            self.PositionPredictionDirection = Vector3.zero;
            self.PositionPredictionSpeed = 0f;
            self.PositionPredictionBlocked = false;
            self.HoldLocalPredictionOnStationarySync = false;
            self.WallBlockedCount = 0;

            self.PredictedDelta = viewPosition - self.AuthoritativePosition - self.VisualCorrection;
            self.TargetPosition = self.BuildPredictionTarget();
        }

        public static void ClearLocalMotionOnAuthoritativeStop(this UnitViewInterpolationComponent self)
        {
            self.PredictedDirection = Vector3.zero;
            self.PredictedSpeed = 0f;
            self.PositionPredictionDirection = Vector3.zero;
            self.PositionPredictionSpeed = 0f;
            self.PositionPredictionBlocked = false;
            self.HoldLocalPredictionOnStationarySync = false;
            self.WallBlockedCount = 0;
        }

        public static void ApplyAuthoritativeReplay(this UnitViewInterpolationComponent self, Vector3 currentViewPosition, float3 authoritativePosition,
            float3 replayedPredictedDelta, float3 predictedDirection, float predictedSpeed, float3 constrainedDirection,
            float constrainedSpeed, bool blocked)
        {
            Unit unit = self.GetParent<Unit>();
            Vector3 previousTargetPosition = self.TargetPosition;
            Vector3 normalizedPredictedDirection = ((Vector3)predictedDirection).sqrMagnitude > 0.000001f
                ? ((Vector3)predictedDirection).normalized
                : Vector3.zero;
            Vector3 normalizedConstrainedDirection = ((Vector3)constrainedDirection).sqrMagnitude > 0.000001f
                ? ((Vector3)constrainedDirection).normalized
                : Vector3.zero;
            Vector3 finalPredictedDelta = replayedPredictedDelta;
            if (!blocked && normalizedConstrainedDirection.sqrMagnitude > 0.000001f && constrainedSpeed > 0.01f)
            {
                finalPredictedDelta = self.PreserveActiveInputLead(unit, currentViewPosition, previousTargetPosition,
                    authoritativePosition, finalPredictedDelta, normalizedConstrainedDirection, constrainedSpeed);
            }

            float replayLeadSpeed = math.max(constrainedSpeed, predictedSpeed);
            if (replayLeadSpeed > 0.01f)
            {
                finalPredictedDelta = self.ClampPredictionLeadDelta(finalPredictedDelta, replayLeadSpeed, MaxMovingPredictionLeadSeconds);
            }

            Vector3 replayTargetPosition = (Vector3)authoritativePosition + finalPredictedDelta;
            Vector3 smoothedTargetPosition = self.SoftenAbruptReplayTarget(unit, currentViewPosition, previousTargetPosition,
                (Vector3)authoritativePosition, replayTargetPosition, normalizedConstrainedDirection, normalizedPredictedDirection,
                constrainedSpeed, predictedSpeed);
            Vector3 visualCorrection = smoothedTargetPosition - replayTargetPosition;

            self.AuthoritativePosition = authoritativePosition;
            self.PredictedDelta = finalPredictedDelta;
            self.VisualCorrection = visualCorrection;
            self.PredictedDirection = normalizedPredictedDirection;
            self.PredictedSpeed = normalizedPredictedDirection.sqrMagnitude > 0.000001f && predictedSpeed > 0.01f
                ? predictedSpeed
                : 0f;
            self.PositionPredictionDirection = normalizedConstrainedDirection;
            self.PositionPredictionSpeed = normalizedConstrainedDirection.sqrMagnitude > 0.000001f && constrainedSpeed > 0.01f
                ? constrainedSpeed
                : 0f;
            self.PositionPredictionBlocked = blocked;
            self.HoldLocalPredictionOnStationarySync = false;
            self.LastAuthoritativeSyncTime = TimeInfo.Instance.ClientNow();
            self.WallBlockedCount = blocked ? 1 : 0;
            self.TargetPosition = self.BuildPredictionTarget();
        }

        public static void SetConstrainedPredictionMotion(this UnitViewInterpolationComponent self, float3 constrainedDirection, float constrainedSpeed, bool blocked)
        {
            Vector3 normalizedDirection = ((Vector3)constrainedDirection).sqrMagnitude > 0.000001f
                ? ((Vector3)constrainedDirection).normalized
                : Vector3.zero;

            self.PositionPredictionDirection = normalizedDirection;
            self.PositionPredictionSpeed = normalizedDirection.sqrMagnitude > 0.000001f && constrainedSpeed > 0.01f
                ? constrainedSpeed
                : 0f;
            self.PositionPredictionBlocked = blocked;
            self.HoldLocalPredictionOnStationarySync = false;

            if (blocked && self.PredictionEnabled)
            {
                self.VisualCorrection += self.PredictedDelta;
                self.PredictedDelta = Vector3.zero;
                if (self.VisualCorrection.sqrMagnitude > MaxPredictionCorrectionDistance * MaxPredictionCorrectionDistance)
                {
                    self.VisualCorrection = self.VisualCorrection.normalized * MaxPredictionCorrectionDistance;
                }
            }
            else
            {
                self.WallBlockedCount = 0;
            }
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
            self.PositionPredictionDirection = Vector3.zero;
            self.PositionPredictionSpeed = 0f;
            self.PositionPredictionBlocked = false;
            self.HoldLocalPredictionOnStationarySync = false;
            self.LastAuthoritativeSyncTime = 0;
            self.WallBlockedCount = 0;
            self.SkipNextChangePositionSync = skipNextChangePositionSync;
        }

        public static Vector3 ClampReplayPredictionPosition(this UnitViewInterpolationComponent self, Unit unit,
            Vector3 authoritativePosition, Vector3 replayPosition, float leadSpeed)
        {
            if (unit == null || unit.IsDisposed || leadSpeed <= 0.01f)
            {
                return replayPosition;
            }

            Vector3 replayDelta = replayPosition - authoritativePosition;
            Vector3 clampedReplayDelta = self.ClampPredictionLeadDelta(replayDelta, leadSpeed, MaxMovingPredictionLeadSeconds);
            if ((clampedReplayDelta - replayDelta).sqrMagnitude <= 0.000001f)
            {
                return replayPosition;
            }

            Vector3 fallbackPosition = authoritativePosition + replayDelta;
            Vector3 clampedReplayPosition = authoritativePosition + clampedReplayDelta;
            return self.ClampPredictionTargetToNavmesh(unit, clampedReplayPosition, fallbackPosition);
        }

        public static void ResolveLocalPositionPrediction(this UnitViewInterpolationComponent self, Unit unit, float3 authoritativePosition)
        {
            Vector3 serverPosition = authoritativePosition;
            Vector3 previousAuthoritativePosition = self.AuthoritativePosition;
            Vector3 inputDirection = self.PredictedDirection;
            float inputSpeed = self.PredictedSpeed;
            long now = TimeInfo.Instance.ClientNow();

            if (inputSpeed < 0.01f || inputDirection.sqrMagnitude <= 0.000001f)
            {
                self.PositionPredictionDirection = Vector3.zero;
                self.PositionPredictionSpeed = 0f;
                self.PositionPredictionBlocked = false;
                self.HoldLocalPredictionOnStationarySync = false;
                self.LastAuthoritativeSyncTime = now;
                self.WallBlockedCount = 0;
                return;
            }

            float syncDeltaTime = self.LastAuthoritativeSyncTime > 0
                ? math.clamp((now - self.LastAuthoritativeSyncTime) / 1000f, 0.001f, MaxAuthoritativeSyncDeltaTime)
                : math.max(Time.deltaTime, 0.016f);
            self.LastAuthoritativeSyncTime = now;

            Vector3 authoritativeDelta = serverPosition - previousAuthoritativePosition;
            float authoritativeMoveDistance = authoritativeDelta.magnitude;
            float expectedMoveDistance = inputSpeed * syncDeltaTime;

            if (authoritativeMoveDistance > MinAuthoritativePredictionDistance)
            {
                self.PositionPredictionBlocked = false;
                self.HoldLocalPredictionOnStationarySync = false;
                self.WallBlockedCount = 0;

                // 本地已经先做过同一套 NavMesh 约束，服务端同步到来时优先保留本地预测方向，
                // 否则旧输入阶段的服务端位移会把当前输入方向拉回去，造成瞬移/拉扯感。
                if (self.PositionPredictionDirection.sqrMagnitude <= 0.000001f ||
                    self.PositionPredictionSpeed < 0.01f)
                {
                    Vector3 authoritativeDirection = authoritativeDelta.normalized;
                    float directionDot = Vector3.Dot(authoritativeDirection, inputDirection);
                    if (directionDot >= ConstrainedPredictionDirectionDot)
                    {
                        self.PositionPredictionDirection = authoritativeDirection;
                        self.PositionPredictionSpeed = math.min(inputSpeed, authoritativeMoveDistance / syncDeltaTime);
                    }
                }

                return;
            }

            bool hasLocalConstrainedMotion = !self.PositionPredictionBlocked &&
                self.PositionPredictionDirection.sqrMagnitude > 0.000001f &&
                self.PositionPredictionSpeed > 0.01f;
            if (hasLocalConstrainedMotion)
            {
                self.HoldLocalPredictionOnStationarySync = true;
                self.WallBlockedCount = 0;
                float localDirectionDot = Vector3.Dot(self.PositionPredictionDirection.normalized, inputDirection);
                self.TracePredictionResolve(unit, "hold-local", authoritativeMoveDistance, expectedMoveDistance, syncDeltaTime, localDirectionDot);
                return;
            }

            self.PositionPredictionDirection = Vector3.zero;
            self.PositionPredictionSpeed = 0f;
            if (expectedMoveDistance > MinAuthoritativePredictionDistance)
            {
                ++self.WallBlockedCount;
                self.PositionPredictionBlocked = self.WallBlockedCount >= 2;
            }
            else
            {
                self.WallBlockedCount = 0;
                self.PositionPredictionBlocked = false;
            }

            self.HoldLocalPredictionOnStationarySync = false;
            if (self.PositionPredictionBlocked)
            {
                self.TracePredictionResolve(unit, "blocked", authoritativeMoveDistance, expectedMoveDistance, syncDeltaTime, 0f);
            }
        }

        private static void AdvancePrediction(this UnitViewInterpolationComponent self, Unit unit)
        {
            if (self.PredictedDirection.sqrMagnitude > 0.000001f)
            {
                TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
                if (turnComponent == null || !turnComponent.IsTurning())
                {
                    unit.Rotation = quaternion.LookRotation((float3)self.PredictedDirection, math.up());
                }
            }

            if (self.PositionPredictionSpeed < 0.01f || Time.deltaTime <= 0f || self.PositionPredictionDirection.sqrMagnitude < 0.000001f)
            {
                return;
            }

            Vector3 predictionStep = self.PositionPredictionDirection * (self.PositionPredictionSpeed * Time.deltaTime);
            self.PredictedDelta += predictionStep;
            self.PredictedDelta = self.ClampPredictionLeadDelta(self.PredictedDelta,
                math.max(self.PositionPredictionSpeed, self.PredictedSpeed), MaxMovingPredictionLeadSeconds);
        }

        private static void UpdatePredictionTarget(this UnitViewInterpolationComponent self, Unit unit)
        {
            float correctionBaseSpeed = math.max(self.PredictedSpeed, self.PositionPredictionSpeed);
            if (correctionBaseSpeed < 0.01f && self.VisualCorrection.sqrMagnitude > 0.000001f)
            {
                // 松手后仍要继续回收残余纠偏，避免 VisualCorrection 因输入速度归零而冻结。
                float capabilityMoveSpeed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
                correctionBaseSpeed = capabilityMoveSpeed > 0.01f ? capabilityMoveSpeed : MaxPredictionCorrectionDistance;
            }

            float correctionSpeed = correctionBaseSpeed * PredictionCorrectionSpeedRatio;
            self.VisualCorrection = Vector3.MoveTowards(self.VisualCorrection, Vector3.zero, correctionSpeed * Time.deltaTime);
            self.TargetPosition = self.BuildPredictionTarget();
        }

        private static Vector3 BuildPredictionTarget(this UnitViewInterpolationComponent self)
        {
            return self.AuthoritativePosition + self.PredictedDelta + self.VisualCorrection;
        }

        private static Vector3 ClampPredictionLeadDelta(this UnitViewInterpolationComponent self, Vector3 predictedDelta,
            float leadSpeed, float maxLeadSeconds)
        {
            float maxLeadDistance = leadSpeed * maxLeadSeconds;
            if (maxLeadDistance <= MinAuthoritativePredictionDistance)
            {
                return Vector3.zero;
            }

            Vector3 horizontalDelta = predictedDelta;
            horizontalDelta.y = 0f;
            float horizontalDistance = horizontalDelta.magnitude;
            if (horizontalDistance <= maxLeadDistance)
            {
                return predictedDelta;
            }

            Vector3 clampedHorizontalDelta = horizontalDelta.normalized * maxLeadDistance;
            return new Vector3(clampedHorizontalDelta.x, predictedDelta.y, clampedHorizontalDelta.z);
        }

        private static Vector3 PreserveActiveInputLead(this UnitViewInterpolationComponent self, Unit unit, Vector3 currentViewPosition,
            Vector3 previousTargetPosition, Vector3 authoritativePosition, Vector3 replayedPredictedDelta, Vector3 constrainedDirection,
            float constrainedSpeed)
        {
            float replayForwardDistance = Vector3.Dot(replayedPredictedDelta, constrainedDirection);
            if (replayForwardDistance < 0f)
            {
                replayForwardDistance = 0f;
            }

            float currentViewForwardDistance = Vector3.Dot(currentViewPosition - authoritativePosition, constrainedDirection);
            if (currentViewForwardDistance < 0f)
            {
                currentViewForwardDistance = 0f;
            }

            float previousTargetForwardDistance = Vector3.Dot(previousTargetPosition - authoritativePosition, constrainedDirection);
            if (previousTargetForwardDistance < 0f)
            {
                previousTargetForwardDistance = 0f;
            }

            float maxLeadDistance = constrainedSpeed * MaxAuthoritativeSyncDeltaTime;
            if (maxLeadDistance <= MinAuthoritativePredictionDistance)
            {
                return replayedPredictedDelta;
            }

            float preservedForwardDistance = math.clamp(
                math.max(replayForwardDistance, math.max(currentViewForwardDistance, previousTargetForwardDistance)),
                0f,
                maxLeadDistance);
            if (preservedForwardDistance <= replayForwardDistance + MinAuthoritativePredictionDistance)
            {
                return replayedPredictedDelta;
            }

            Vector3 candidateDelta = constrainedDirection * preservedForwardDistance;
            Vector3 fallbackTarget = authoritativePosition + replayedPredictedDelta;
            Vector3 candidateTarget = authoritativePosition + candidateDelta;
            candidateTarget = self.ClampPredictionTargetToNavmesh(unit, candidateTarget, fallbackTarget);

            Vector3 clampedDelta = candidateTarget - authoritativePosition;
            clampedDelta.y = replayedPredictedDelta.y;
            float clampedForwardDistance = Vector3.Dot(clampedDelta, constrainedDirection);
            return clampedForwardDistance > replayForwardDistance + MinAuthoritativePredictionDistance
                ? clampedDelta
                : replayedPredictedDelta;
        }

        private static Vector3 SoftenAbruptReplayTarget(this UnitViewInterpolationComponent self, Unit unit, Vector3 currentViewPosition,
            Vector3 previousTargetPosition, Vector3 authoritativePosition, Vector3 replayTargetPosition, Vector3 constrainedDirection, Vector3 predictedDirection,
            float constrainedSpeed, float predictedSpeed)
        {
            float capabilityMoveSpeed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            float smoothingSpeed = math.max(math.max(constrainedSpeed, predictedSpeed), capabilityMoveSpeed);
            if (smoothingSpeed <= 0.01f)
            {
                return replayTargetPosition;
            }

            float maxTargetShiftDistance = smoothingSpeed * MaxReplayTargetShiftSeconds;
            if (maxTargetShiftDistance <= MinAuthoritativePredictionDistance ||
                !self.ShouldSmoothReplayTarget(currentViewPosition, previousTargetPosition, authoritativePosition, replayTargetPosition,
                    constrainedDirection, predictedDirection, maxTargetShiftDistance))
            {
                return replayTargetPosition;
            }

            Vector3 softenedTargetPosition = Vector3.MoveTowards(currentViewPosition, replayTargetPosition, maxTargetShiftDistance);
            return self.ClampPredictionTargetToNavmesh(unit, softenedTargetPosition, replayTargetPosition);
        }

        private static bool ShouldSmoothReplayTarget(this UnitViewInterpolationComponent self, Vector3 currentViewPosition,
            Vector3 previousTargetPosition, Vector3 authoritativePosition, Vector3 replayTargetPosition, Vector3 constrainedDirection,
            Vector3 predictedDirection, float maxTargetShiftDistance)
        {
            Vector3 replayDeltaFromView = replayTargetPosition - currentViewPosition;
            replayDeltaFromView.y = 0f;
            if (replayDeltaFromView.sqrMagnitude <= MinAuthoritativePredictionDistance * MinAuthoritativePredictionDistance)
            {
                return false;
            }

            Vector3 activeDirection = constrainedDirection.sqrMagnitude > 0.000001f
                ? constrainedDirection
                : predictedDirection;
            bool retreating = false;
            if (activeDirection.sqrMagnitude > 0.000001f)
            {
                retreating = Vector3.Dot(replayDeltaFromView, activeDirection.normalized) < -MinAuthoritativePredictionDistance;
            }
            else
            {
                Vector3 currentLead = currentViewPosition - authoritativePosition;
                currentLead.y = 0f;
                if (currentLead.sqrMagnitude > MinAuthoritativePredictionDistance * MinAuthoritativePredictionDistance)
                {
                    retreating = Vector3.Dot(replayDeltaFromView, currentLead.normalized) < -MinAuthoritativePredictionDistance;
                }
            }

            if (retreating)
            {
                return true;
            }

            Vector3 replayShift = replayTargetPosition - previousTargetPosition;
            replayShift.y = 0f;
            if (replayShift.sqrMagnitude <= maxTargetShiftDistance * maxTargetShiftDistance)
            {
                return false;
            }

            Vector3 previousDeltaFromView = previousTargetPosition - currentViewPosition;
            previousDeltaFromView.y = 0f;
            if (previousDeltaFromView.sqrMagnitude <= MinAuthoritativePredictionDistance * MinAuthoritativePredictionDistance)
            {
                return true;
            }

            float replayRedirectDot = Vector3.Dot(previousDeltaFromView.normalized, replayDeltaFromView.normalized);
            return replayRedirectDot < 0.98f || replayDeltaFromView.sqrMagnitude > maxTargetShiftDistance * maxTargetShiftDistance;
        }

        private static Vector3 ClampPredictionTargetToNavmesh(this UnitViewInterpolationComponent self, Unit unit, Vector3 targetPosition, Vector3 fallbackPosition)
        {
            PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                return targetPosition;
            }

            float unitRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            if (!pathfinding.TryRecastFindNearestPointForMovement(targetPosition, unitRadius, out float3 projectedPos, out float projectedDistance))
            {
                return fallbackPosition;
            }

            return projectedDistance <= 0.0005f ? targetPosition : (Vector3)projectedPos;
        }

        private static Vector3 ClampStationaryPredictionLead(this UnitViewInterpolationComponent self, Vector3 authoritativePosition, Vector3 targetPosition)
        {
            float leadDistance = math.max(self.PredictedSpeed, self.PositionPredictionSpeed) * MaxStationaryPredictionLeadSeconds;
            if (leadDistance <= MinAuthoritativePredictionDistance)
            {
                return authoritativePosition;
            }

            Vector3 horizontalDelta = targetPosition - authoritativePosition;
            horizontalDelta.y = 0f;
            float horizontalDistance = horizontalDelta.magnitude;
            if (horizontalDistance <= leadDistance)
            {
                return targetPosition;
            }

            Vector3 clampedTarget = authoritativePosition + horizontalDelta.normalized * leadDistance;
            clampedTarget.y = targetPosition.y;
            return clampedTarget;
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

        private static void MoveVisualTowardsTarget(this UnitViewInterpolationComponent self, Unit unit, Transform transform, Vector3 oldViewPosition)
        {
            Vector3 delta = self.TargetPosition - oldViewPosition;
            if (self.ShouldSnap(unit, delta))
            {
                transform.position = self.TargetPosition;
                return;
            }

            if (delta.sqrMagnitude <= ArriveDistanceSqr)
            {
                if ((transform.position - self.TargetPosition).sqrMagnitude > 0f)
                {
                    transform.position = self.TargetPosition;
                }
                return;
            }

            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
            float maxStep = speed * MaxVisualSpeedMultiplier * Time.deltaTime;
            transform.position = Vector3.MoveTowards(oldViewPosition, self.TargetPosition, maxStep);

            if ((self.TargetPosition - transform.position).sqrMagnitude <= ArriveDistanceSqr)
            {
                transform.position = self.TargetPosition;
            }
        }

        private static void RefreshVisualState(this UnitViewInterpolationComponent self, Unit unit, Vector3 oldViewPosition, Vector3 newViewPosition)
        {
            Vector3 visualDelta = newViewPosition - oldViewPosition;
            Vector3 horizontalVisualDelta = visualDelta;
            horizontalVisualDelta.y = 0f;
            Vector3 horizontalTargetDelta = self.TargetPosition - oldViewPosition;
            horizontalTargetDelta.y = 0f;
            AnimatorComponent animator = unit.GetComponent<AnimatorComponent>();
            if (animator != null)
            {
                float moveSpeed = 0f;
                string moveSpeedSource = "idle";
                float horizontalMoveDistance = math.max(horizontalVisualDelta.magnitude, horizontalTargetDelta.magnitude);
                JoystickMoveAuthorityStateComponent authorityState = unit.GetComponent<JoystickMoveAuthorityStateComponent>();
                bool authoritativeStopped = authorityState != null && authorityState.LastSpeed <= 0.01f;
                bool locallyStopped = self.PredictedSpeed <= 0.01f && self.PositionPredictionSpeed <= 0.01f;
                bool forceIdleByAuthority = authoritativeStopped && locallyStopped;
                if (forceIdleByAuthority)
                {
                    moveSpeedSource = "idle-authoritative-stop";
                }
                else if (horizontalMoveDistance * horizontalMoveDistance > HorizontalAnimationDistanceSqr)
                {
                    float capabilityMoveSpeed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
                    float runtimeMoveSpeed = horizontalMoveDistance / math.max(Time.deltaTime, 0.0001f);
                    moveSpeed = capabilityMoveSpeed > 0.01f ? math.min(runtimeMoveSpeed, capabilityMoveSpeed) : runtimeMoveSpeed;
                    if (moveSpeed < MinVisualAnimationSpeed &&
                        self.PredictedSpeed <= 0.01f &&
                        self.PositionPredictionSpeed <= 0.01f)
                    {
                        moveSpeed = 0f;
                        moveSpeedSource = "idle-small-correction";
                    }
                    else
                    {
                        moveSpeedSource = "visual-horizontal";
                    }
                }

                animator.SetFloat(nameof(MotionType.MoveSpeed), moveSpeed);
                self.TraceSuspiciousWalkState(unit, moveSpeed, moveSpeedSource, visualDelta, horizontalVisualDelta, horizontalTargetDelta, oldViewPosition, newViewPosition);
                self.TraceAnimatorShouldIdle(unit, animator, moveSpeed, moveSpeedSource, authoritativeStopped, locallyStopped);
            }

            CinemachineComponent cinemachineComponent = unit.GetComponent<CinemachineComponent>();
            if (cinemachineComponent != null && !cinemachineComponent.IsDisposed)
            {
                cinemachineComponent.ApplyFollowTransform();
            }

            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            gameObjectComponent?.RefreshLocalConcealmentVisual(unit, newViewPosition);
        }

        private static void TraceSuspiciousWalkState(this UnitViewInterpolationComponent self, Unit unit, float moveSpeed, string moveSpeedSource,
            Vector3 visualDelta, Vector3 horizontalVisualDelta, Vector3 horizontalTargetDelta, Vector3 oldViewPosition, Vector3 newViewPosition)
        {
            if (!unit.IsMyUnit() || moveSpeed <= 0.01f)
            {
                return;
            }

            bool suspiciousWalk = horizontalVisualDelta.sqrMagnitude <= SuspiciousWalkVisualDistanceSqr &&
                horizontalTargetDelta.sqrMagnitude <= SuspiciousWalkVisualDistanceSqr;
            if (!suspiciousWalk)
            {
                return;
            }

            long now = TimeInfo.Instance.ClientNow();
            if (now - self.LastVisualMoveTraceLogTime < 250)
            {
                return;
            }

            self.LastVisualMoveTraceLogTime = now;
            Vector3 targetDelta = self.TargetPosition - newViewPosition;
            Vector3 horizontalTargetDeltaAfterMove = targetDelta;
            horizontalTargetDeltaAfterMove.y = 0f;
            Log.Info(
                $"[NavMove][VisualAnim] unitId={unit.Id}, source={moveSpeedSource}, moveSpeed={moveSpeed:F3}, visualDelta={visualDelta}, visualDeltaSqr={visualDelta.sqrMagnitude:F6}, horizontalVisualDelta={horizontalVisualDelta}, horizontalVisualDeltaSqr={horizontalVisualDelta.sqrMagnitude:F6}, oldView={oldViewPosition}, newView={newViewPosition}, targetPos={self.TargetPosition}, horizontalTargetDeltaBeforeMove={horizontalTargetDelta}, horizontalTargetDeltaBeforeMoveSqr={horizontalTargetDelta.sqrMagnitude:F6}, targetDelta={targetDelta}, horizontalTargetDeltaAfterMove={horizontalTargetDeltaAfterMove}, predictedSpeed={self.PredictedSpeed:F3}, predictedDir={self.PredictedDirection}, constrainedSpeed={self.PositionPredictionSpeed:F3}, constrainedDir={self.PositionPredictionDirection}, blocked={self.PositionPredictionBlocked}, hold={self.HoldLocalPredictionOnStationarySync}, predictedDelta={self.PredictedDelta}, visualCorrection={self.VisualCorrection}");
        }

        private static void TraceAnimatorShouldIdle(this UnitViewInterpolationComponent self, Unit unit, AnimatorComponent animator, float moveSpeed,
            string moveSpeedSource, bool authoritativeStopped, bool locallyStopped)
        {
            if (!unit.IsMyUnit() || moveSpeed > 0.01f || animator?.Animator == null)
            {
                return;
            }

            Animator unityAnimator = animator.Animator;
            float runtimeMoveSpeed = animator.HasParameter(nameof(MotionType.MoveSpeed))
                ? unityAnimator.GetFloat(nameof(MotionType.MoveSpeed))
                : 0f;
            string currentClips = GetClipNames(unityAnimator.GetCurrentAnimatorClipInfo(0));
            string nextClips = GetClipNames(unityAnimator.GetNextAnimatorClipInfo(0));
            bool idleLike = currentClips.Contains("Idle", StringComparison.OrdinalIgnoreCase) ||
                nextClips.Contains("Idle", StringComparison.OrdinalIgnoreCase);
            if (idleLike && runtimeMoveSpeed <= 0.01f)
            {
                return;
            }

            long now = TimeInfo.Instance.ClientNow();
            if (now - self.LastAnimatorStateTraceLogTime < 250)
            {
                return;
            }

            self.LastAnimatorStateTraceLogTime = now;
            Log.Info(
                $"[NavMove][AnimatorIdle] unitId={unit.Id}, source={moveSpeedSource}, authoritativeStopped={authoritativeStopped}, locallyStopped={locallyStopped}, runtimeMoveSpeed={runtimeMoveSpeed:F3}, currentStateHash={unityAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash}, nextStateHash={unityAnimator.GetNextAnimatorStateInfo(0).shortNameHash}, currentClips={currentClips}, nextClips={nextClips}, predictedSpeed={self.PredictedSpeed:F3}, constrainedSpeed={self.PositionPredictionSpeed:F3}, visualCorrection={self.VisualCorrection}");
        }

        private static void TracePredictionResolve(this UnitViewInterpolationComponent self, Unit unit, string state, float authoritativeMoveDistance, float expectedMoveDistance, float syncDeltaTime, float directionDot)
        {
            long now = TimeInfo.Instance.ClientNow();
            if (now - self.LastPredictionTraceLogTime < 250)
            {
                return;
            }

            self.LastPredictionTraceLogTime = now;
            Log.Info(
                $"[NavMove][ClientPrediction] state={state}, unitId={unit.Id}, inputDir={self.PredictedDirection}, inputSpeed={self.PredictedSpeed:F3}, predictedDir={self.PositionPredictionDirection}, predictedSpeed={self.PositionPredictionSpeed:F3}, authPos={self.AuthoritativePosition}, authMove={authoritativeMoveDistance:F4}, expectedMove={expectedMoveDistance:F4}, syncDt={syncDeltaTime:F4}, dirDot={directionDot:F3}, visualCorrection={self.VisualCorrection}");
        }

        private static void TraceFrameState(this UnitViewInterpolationComponent self, Unit unit, Vector3 oldViewPosition, Vector3 newViewPosition)
        {
            if (!unit.IsMyUnit())
            {
                return;
            }

            long now = TimeInfo.Instance.ClientNow();
            Vector3 viewDelta = newViewPosition - oldViewPosition;
            bool active = viewDelta.sqrMagnitude > 0.000001f ||
                self.PredictedDelta.sqrMagnitude > 0.000001f ||
                self.VisualCorrection.sqrMagnitude > 0.000001f ||
                self.PredictedSpeed > 0.01f ||
                self.PositionPredictionSpeed > 0.01f;
            if (!active || now - self.LastFrameTraceLogTime < 80)
            {
                return;
            }

            self.LastFrameTraceLogTime = now;
            Log.Info(
                $"[NavMove][Frame] unitId={unit.Id}, oldView={oldViewPosition}, newView={newViewPosition}, viewDelta={viewDelta}, targetPos={self.TargetPosition}, authPos={self.AuthoritativePosition}, predictedDelta={self.PredictedDelta}, visualCorrection={self.VisualCorrection}, predictedDir={self.PredictedDirection}, predictedSpeed={self.PredictedSpeed:F3}, constrainedDir={self.PositionPredictionDirection}, constrainedSpeed={self.PositionPredictionSpeed:F3}, blocked={self.PositionPredictionBlocked}, hold={self.HoldLocalPredictionOnStationarySync}");
        }

        private static string GetClipNames(AnimatorClipInfo[] clipInfos)
        {
            if (clipInfos == null || clipInfos.Length == 0)
            {
                return "[]";
            }

            StringBuilder stringBuilder = new StringBuilder("[");
            for (int i = 0; i < clipInfos.Length; ++i)
            {
                if (i > 0)
                {
                    stringBuilder.Append(", ");
                }

                stringBuilder.Append(clipInfos[i].clip != null ? clipInfos[i].clip.name : "null");
                stringBuilder.Append('@');
                stringBuilder.Append(clipInfos[i].weight.ToString("F3"));
            }

            stringBuilder.Append(']');
            return stringBuilder.ToString();
        }
    }
}
