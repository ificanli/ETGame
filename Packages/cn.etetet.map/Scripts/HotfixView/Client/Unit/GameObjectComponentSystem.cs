using System;
using System.Collections.Generic;
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
            self.ClearOutlineVisual();
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

            Renderer[] allRenderers = self.GameObject.GetComponentsInChildren<Renderer>(true);
            if (allRenderers == null || allRenderers.Length == 0)
            {
                self.CachedRenderers = null;
                self.OriginalMaterials = null;
                self.TransparentMaterials = null;
                return;
            }

            List<Renderer> filteredRenderers = new(allRenderers.Length);
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer == null || renderer.GetComponent<UnitOutlineMarker>() != null)
                {
                    continue;
                }

                filteredRenderers.Add(renderer);
            }

            self.CachedRenderers = filteredRenderers.ToArray();
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

        public static bool ApplyOutlineVisual(this GameObjectComponent self, Color color, float width)
        {
            if (self == null || self.GameObject == null)
            {
                return false;
            }

            if (width <= 0f || color.a <= 0.001f)
            {
                self.ClearOutlineVisual();
                return true;
            }

            Shader shader = Shader.Find("ET/Monster/Outline");
            if (shader == null)
            {
                Log.Warning("[MonsterOutline] shader not found: ET/Monster/Outline");
                return false;
            }

            if (self.CachedRenderers == null || self.CachedRenderers.Length == 0 || self.OriginalMaterials == null)
            {
                self.CacheRenderers();
            }

            if (self.CachedRenderers == null || self.CachedRenderers.Length == 0)
            {
                return false;
            }

            if (self.OutlineMaterial == null || self.OutlineMaterial.shader != shader)
            {
                if (self.OutlineMaterial != null)
                {
                    UnityEngine.Object.Destroy(self.OutlineMaterial);
                }

                self.OutlineMaterial = new Material(shader)
                {
                    name = "RuntimeMonsterOutline"
                };
            }

            self.OutlineMaterial.SetColor("_OutlineColor", color);
            self.OutlineMaterial.SetFloat("_OutlineWidth", width);
            self.OutlineMaterial.SetFloat("_DepthOffset", 0f);
            self.OutlineColor = color;
            self.OutlineWidth = width;

            if (self.OutlineObjects != null && self.OutlineObjects.Length > 0)
            {
                return true;
            }

            List<GameObject> outlineObjects = new(self.CachedRenderers.Length);
            foreach (Renderer renderer in self.CachedRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                GameObject outlineObject = renderer switch
                {
                    SkinnedMeshRenderer skinnedMeshRenderer => CreateOutlineObject(skinnedMeshRenderer, self.OutlineMaterial),
                    MeshRenderer meshRenderer => CreateOutlineObject(meshRenderer, self.OutlineMaterial),
                    _ => null,
                };

                if (outlineObject == null)
                {
                    continue;
                }

                outlineObjects.Add(outlineObject);
            }

            self.OutlineObjects = outlineObjects.Count > 0 ? outlineObjects.ToArray() : null;
            return self.OutlineObjects != null && self.OutlineObjects.Length > 0;
        }

        public static void ClearOutlineVisual(this GameObjectComponent self)
        {
            if (self == null)
            {
                return;
            }

            if (self.OutlineObjects != null)
            {
                for (int i = 0; i < self.OutlineObjects.Length; ++i)
                {
                    if (self.OutlineObjects[i] != null)
                    {
                        UnityEngine.Object.Destroy(self.OutlineObjects[i]);
                    }
                }

                self.OutlineObjects = null;
            }

            if (self.OutlineMaterial != null)
            {
                UnityEngine.Object.Destroy(self.OutlineMaterial);
                self.OutlineMaterial = null;
            }

            self.OutlineColor = Color.clear;
            self.OutlineWidth = 0f;
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

        private static GameObject CreateOutlineObject(SkinnedMeshRenderer sourceRenderer, Material outlineMaterial)
        {
            if (sourceRenderer == null || sourceRenderer.sharedMesh == null || outlineMaterial == null)
            {
                return null;
            }

            GameObject outlineObject = new($"{sourceRenderer.name}_Outline");
            outlineObject.transform.SetParent(sourceRenderer.transform, false);
            outlineObject.AddComponent<UnitOutlineMarker>();

            SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
            outlineRenderer.sharedMesh = sourceRenderer.sharedMesh;
            outlineRenderer.rootBone = sourceRenderer.rootBone;
            outlineRenderer.bones = sourceRenderer.bones;
            outlineRenderer.localBounds = sourceRenderer.localBounds;
            outlineRenderer.updateWhenOffscreen = sourceRenderer.updateWhenOffscreen;
            outlineRenderer.quality = sourceRenderer.quality;
            outlineRenderer.skinnedMotionVectors = false;
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            outlineRenderer.allowOcclusionWhenDynamic = false;
            outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
            outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            outlineRenderer.sharedMaterials = CreateOutlineMaterialArray(outlineMaterial, sourceRenderer.sharedMaterials?.Length ?? 0);
            outlineRenderer.enabled = sourceRenderer.enabled;
            return outlineObject;
        }

        private static GameObject CreateOutlineObject(MeshRenderer sourceRenderer, Material outlineMaterial)
        {
            if (sourceRenderer == null || outlineMaterial == null)
            {
                return null;
            }

            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                return null;
            }

            GameObject outlineObject = new($"{sourceRenderer.name}_Outline");
            outlineObject.transform.SetParent(sourceRenderer.transform, false);
            outlineObject.AddComponent<UnitOutlineMarker>();

            MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            outlineRenderer.allowOcclusionWhenDynamic = false;
            outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
            outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            outlineRenderer.sharedMaterials = CreateOutlineMaterialArray(outlineMaterial, sourceRenderer.sharedMaterials?.Length ?? 0);
            outlineRenderer.enabled = sourceRenderer.enabled;
            return outlineObject;
        }

        private static Material[] CreateOutlineMaterialArray(Material outlineMaterial, int count)
        {
            int materialCount = count > 0 ? count : 1;
            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materialCount; ++i)
            {
                materials[i] = outlineMaterial;
            }

            return materials;
        }
    }

    [EntitySystemOf(typeof(UnitViewInterpolationComponent))]
    public static partial class UnitViewInterpolationComponentSystem
    {
        // --- 权威融合常量 ---
        private const float SnapThreshold = 2.0f;
        private const float CorrectionRate = 8.0f;
        private const float CorrectionDeadzone = 0.005f;
        private const float MaxPredictionLeadSeconds = 0.2f;

        // --- 视觉常量 ---
        private const float MaxVisualSpeedMultiplier = 1.2f;
        private const float ArriveDistanceSqr = 0.0001f;
        private const float HorizontalAnimationDistanceSqr = 0.00015625f;
        private const float MinVisualAnimationSpeed = 0.35f;
        private const float MinSnapDistance = 2f;
        private const float SnapDistanceBySpeedSeconds = 0.35f;

        // --- 诊断常量 ---
        private const int DiagnosticLogIntervalMs = 200;
        private const float DiagnosticGapWarnDistance = 0.5f;

        [EntitySystem]
        private static void Awake(this UnitViewInterpolationComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            Vector3 startPosition = gameObjectComponent?.Transform != null ? gameObjectComponent.Transform.position : unit.Position;
            self.TargetPosition = startPosition;
            self.Initialized = false;
            self.PredictionEnabled = unit.IsMyUnit();
            self.AuthoritativePosition = startPosition;
            self.LastAuthoritativeSyncTime = 0;
            self.LocalPredictedPosition = startPosition;
            self.LocalMoveDirection = Vector3.zero;
            self.LocalMoveSpeed = 0f;
            self.LastDiagnosticLogTime = 0;
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

            Vector3 oldViewPosition = transform.position;

            if (self.PredictionEnabled)
            {
                float dt = Time.deltaTime;
                PathfindingComponent pathfinding = unit.GetComponent<PathfindingComponent>();

                // 第一步：本地输入运动（NavMesh 约束）
                if (self.LocalMoveSpeed > 0.01f && self.LocalMoveDirection.sqrMagnitude > 0.000001f && dt > 0f)
                {
                    Vector3 desired = self.LocalPredictedPosition
                        + self.LocalMoveDirection * self.LocalMoveSpeed * dt;
                    desired.y = self.LocalPredictedPosition.y;

                    if (pathfinding != null)
                    {
                        try
                        {
                            pathfinding.TryMoveAlongSurface(self.LocalPredictedPosition, desired, out float3 constrained);
                            self.LocalPredictedPosition = (Vector3)constrained;
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"[Move] local NavMesh exception: {e.Message}");
                            self.LocalPredictedPosition = desired;
                        }
                    }
                    else
                    {
                        self.LocalPredictedPosition = desired;
                    }

                    // 转向
                    TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
                    if (turnComponent == null || !turnComponent.IsTurning())
                    {
                        unit.Rotation = quaternion.LookRotation((float3)self.LocalMoveDirection, math.up());
                    }
                }

                // 第二步：权威融合（也走 NavMesh）
                ApplyAuthorityCorrection(self, pathfinding, dt);

                // 第三步：更新视觉目标（单向写入，不反写）
                self.TargetPosition = self.LocalPredictedPosition;
            }

            self.MoveVisualTowardsTarget(unit, transform, oldViewPosition);
            self.RefreshVisualState(unit, oldViewPosition, transform.position);
        }

        // ========== 权威融合 ==========

        private static void ApplyAuthorityCorrection(UnitViewInterpolationComponent self, PathfindingComponent pathfinding, float dt)
        {
            Vector3 diff = self.AuthoritativePosition - self.LocalPredictedPosition;
            diff.y = 0f;
            float gap = diff.magnitude;

            if (gap > SnapThreshold)
            {
                self.LocalPredictedPosition = self.AuthoritativePosition;
                return;
            }

            if (gap <= CorrectionDeadzone)
            {
                return;
            }

            // 零输入方向守卫：本地无输入时不追 authority，防止松手后被尾包带着"平滑续走"
            if (self.LocalMoveSpeed < 0.01f)
            {
                return;
            }

            // 预测领先守卫：本地预测在权威前方（同向移动的正常态）时
            // 只有超出预算的部分才修正，预算内完全不动
            Vector3 localDir = self.LocalMoveDirection;
            localDir.y = 0f;
            if (localDir.sqrMagnitude > 0.000001f && diff.sqrMagnitude > 0.000001f)
            {
                float dot = Vector3.Dot(diff.normalized, localDir.normalized);
                if (dot < 0f)
                {
                    // 本地预测领先权威 — 计算允许的最大前置量
                    float maxLead = self.LocalMoveSpeed * MaxPredictionLeadSeconds;
                    if (gap <= maxLead)
                    {
                        // 在预算内，完全不修正
                        return;
                    }

                    // 超出预算，只修正超出部分
                    gap = gap - maxLead;
                }
            }

            // 有输入时：比例修正，gap 指数衰减趋零
            float correction = gap * CorrectionRate * dt;
            Vector3 correctedPos = Vector3.MoveTowards(
                self.LocalPredictedPosition, self.AuthoritativePosition, correction);
            correctedPos.y = self.LocalPredictedPosition.y;

            // 修正也走 NavMesh 约束，避免切墙
            if (pathfinding != null)
            {
                try
                {
                    pathfinding.TryMoveAlongSurface(self.LocalPredictedPosition, correctedPos, out float3 constrained);
                    self.LocalPredictedPosition = (Vector3)constrained;
                }
                catch (Exception e)
                {
                    Log.Warning($"[Move] correction NavMesh exception: {e.Message}");
                    self.LocalPredictedPosition = correctedPos;
                }
            }
            else
            {
                self.LocalPredictedPosition = correctedPos;
            }
        }

        // ========== 公共接口 ==========

        public static void SetTargetPosition(this UnitViewInterpolationComponent self, float3 targetPosition)
        {
            self.TargetPosition = targetPosition;
            if (!self.PredictionEnabled)
            {
                self.AuthoritativePosition = targetPosition;
                self.LocalPredictedPosition = targetPosition;
                self.LocalMoveDirection = Vector3.zero;
                self.LocalMoveSpeed = 0f;
                self.LastAuthoritativeSyncTime = 0;
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

            self.AuthoritativePosition = serverPosition;
            self.LastAuthoritativeSyncTime = TimeInfo.Instance.ClientNow();
            self.TryLogAuthorityGap();
        }

        public static void SetLocalMoveInput(this UnitViewInterpolationComponent self, float3 direction, float speed)
        {
            Vector3 dir = direction;
            self.LocalMoveDirection = dir.sqrMagnitude > 0.000001f ? dir.normalized : Vector3.zero;
            self.LocalMoveSpeed = speed;
        }

        public static void ResetPrediction(this UnitViewInterpolationComponent self, float3 position)
        {
            Vector3 resetPosition = position;
            self.TargetPosition = resetPosition;
            self.AuthoritativePosition = resetPosition;
            self.LocalPredictedPosition = resetPosition;
            self.LocalMoveDirection = Vector3.zero;
            self.LocalMoveSpeed = 0f;
            self.LastAuthoritativeSyncTime = 0;
            self.LastDiagnosticLogTime = 0;
        }

        // ========== 视觉 ==========

        private static bool ShouldSnap(this UnitViewInterpolationComponent self, Unit unit, Vector3 delta)
        {
            if (self.PredictionEnabled)
            {
                return delta.sqrMagnitude >= SnapThreshold * SnapThreshold;
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
                JoystickMoveAuthorityStateComponent authorityState = unit.GetComponent<JoystickMoveAuthorityStateComponent>();
                float capabilityMoveSpeed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
                float moveSpeed = 0f;
                bool authoritativeStopped = authorityState != null && authorityState.LastSpeed <= 0.01f;
                bool locallyStopped = self.LocalMoveSpeed <= 0.01f;
                bool forceIdleByAuthority = authoritativeStopped && locallyStopped;
                if (!forceIdleByAuthority)
                {
                    float horizontalMoveDistance = math.max(horizontalVisualDelta.magnitude, horizontalTargetDelta.magnitude);
                    if (horizontalMoveDistance * horizontalMoveDistance > HorizontalAnimationDistanceSqr)
                    {
                        float runtimeMoveSpeed = horizontalMoveDistance / math.max(Time.deltaTime, 0.0001f);
                        moveSpeed = capabilityMoveSpeed > 0.01f ? math.min(runtimeMoveSpeed, capabilityMoveSpeed) : runtimeMoveSpeed;
                        if (moveSpeed < MinVisualAnimationSpeed && locallyStopped)
                        {
                            moveSpeed = 0f;
                        }
                    }
                }

                animator.SetFloat(nameof(MotionType.MoveSpeed), moveSpeed);
            }

            CinemachineComponent cinemachineComponent = unit.GetComponent<CinemachineComponent>();
            if (cinemachineComponent != null && !cinemachineComponent.IsDisposed)
            {
                cinemachineComponent.ApplyFollowTransform();
            }

            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            gameObjectComponent?.RefreshLocalConcealmentVisual(unit, newViewPosition);
        }

        // ========== 诊断（默认静默） ==========

        [System.Diagnostics.Conditional("ET_ENABLE_MOVE_DIAGNOSTIC")]
        private static void TryLogAuthorityGap(this UnitViewInterpolationComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed || !unit.IsMyUnit())
            {
                return;
            }

            Vector3 diff = self.AuthoritativePosition - self.LocalPredictedPosition;
            diff.y = 0f;
            float gap = diff.magnitude;
            if (gap <= DiagnosticGapWarnDistance)
            {
                return;
            }

            long now = TimeInfo.Instance.ClientNow();
            if (now - self.LastDiagnosticLogTime < DiagnosticLogIntervalMs)
            {
                return;
            }

            self.LastDiagnosticLogTime = now;
            Log.Warning(
                $"[Move][DiagGap] unitId={unit.Id}, gap={gap:F3}, local={self.LocalPredictedPosition}, auth={self.AuthoritativePosition}, " +
                $"moveDir={self.LocalMoveDirection}, moveSpeed={self.LocalMoveSpeed:F3}");
        }

        private static float GetHorizontalDistance(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
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
