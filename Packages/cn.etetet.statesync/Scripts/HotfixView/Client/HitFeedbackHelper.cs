using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 命中反馈：受击闪白 + 短暂缩放抖动。
    /// 仅对敌方/怪物生效（我方被打中不做本期反馈）。
    /// </summary>
    public static class HitFeedbackHelper
    {
        public const float HitFlashFadeOutRatio = 0.2f;// 控制“前面顶满多久、后面渐隐多久”。值越大：渐隐越长，拖尾更明显。  值越小：更像“啪一下”短促闪白。                                    
                                               
       
        public const float HitFlashBaseColorMultiplier = 10.35f;//控制模型本体颜色被提亮多少。  
        public const float HitFlashEmissionBoost = 10f; // 控制额外发光强度
        public const float HitScalePunch = 1.02f;
        public const int HitScaleDurationMs = 100;//  控制总总时长

        private const string EmissionKeyword = "_EMISSION";

        /// <summary>
        /// 对目标 Unit 播放闪白效果。
        /// </summary>
        public static async ETTask PlayHitFlash(Scene root, Unit target, int durationMs)
        {
            GameObjectComponent goComponent = target.GetComponent<GameObjectComponent>();
            if (goComponent == null)
            {
                return;
            }

            Renderer[] renderers = goComponent.CachedRenderers;
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            Material[][] rendererMaterials = new Material[renderers.Length][];
            bool[][] hasBaseColors = new bool[renderers.Length][];
            Color[][] originalBaseColors = new Color[renderers.Length][];
            bool[][] hasColors = new bool[renderers.Length][];
            Color[][] originalColors = new Color[renderers.Length][];
            bool[][] hasEmissionColors = new bool[renderers.Length][];
            Color[][] originalEmissionColors = new Color[renderers.Length][];
            bool[][] originalEmissionKeywords = new bool[renderers.Length][];

            bool hasAnyUsableProperty = CaptureFlashMaterials(
                renderers,
                rendererMaterials,
                hasBaseColors,
                originalBaseColors,
                hasColors,
                originalColors,
                hasEmissionColors,
                originalEmissionColors,
                originalEmissionKeywords);
            if (!hasAnyUsableProperty)
            {
                return;
            }

            ApplyFlash(
                rendererMaterials,
                hasBaseColors,
                originalBaseColors,
                hasColors,
                originalColors,
                hasEmissionColors,
                originalEmissionColors,
                1f);

            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> targetRef = target;
            TimerComponent timerComponent = root.TimerComponent;
            if (timerComponent == null)
            {
                RestoreFlash(
                    rendererMaterials,
                    hasBaseColors,
                    originalBaseColors,
                    hasColors,
                    originalColors,
                    hasEmissionColors,
                    originalEmissionColors,
                    originalEmissionKeywords);
                return;
            }

            float totalMs = durationMs > 0 ? durationMs : 80f;
            float holdMs = totalMs * (1f - HitFlashFadeOutRatio);
            float fadeMs = totalMs * HitFlashFadeOutRatio;

            if (holdMs > 0)
            {
                await timerComponent.WaitAsync((long)holdMs);
                root = rootRef;
                target = targetRef;
                if (root == null || target == null || target.IsDisposed)
                {
                    return;
                }
            }

            float fadeElapsed = 0f;
            while (fadeElapsed < fadeMs)
            {
                await timerComponent.WaitAsync(16);
                root = rootRef;
                target = targetRef;
                if (root == null || target == null || target.IsDisposed)
                {
                    return;
                }

                fadeElapsed += 16f;
                float t = Mathf.Clamp01(fadeElapsed / fadeMs);
                ApplyFlash(
                    rendererMaterials,
                    hasBaseColors,
                    originalBaseColors,
                    hasColors,
                    originalColors,
                    hasEmissionColors,
                    originalEmissionColors,
                    Mathf.Lerp(1f, 0f, t));
            }

            RestoreFlash(
                rendererMaterials,
                hasBaseColors,
                originalBaseColors,
                hasColors,
                originalColors,
                hasEmissionColors,
                originalEmissionColors,
                originalEmissionKeywords);
        }

        /// <summary>
        /// 对目标 Unit 播放短暂缩放抖动。
        /// </summary>
        public static async ETTask PlayHitScale(Scene root, Unit target)
        {
            GameObjectComponent goComponent = target.GetComponent<GameObjectComponent>();
            Transform transform = goComponent?.GameObject?.transform;
            if (transform == null) return;

            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> targetRef = target;
            TimerComponent timerComponent = root.TimerComponent;
            if (timerComponent == null) return;

            Vector3 originalScale = transform.localScale;
            transform.localScale = originalScale * HitScalePunch;

            float elapsed = 0f;
            while (elapsed < HitScaleDurationMs)
            {
                await timerComponent.WaitAsync(16);
                root = rootRef;
                target = targetRef;
                if (root == null || target == null || target.IsDisposed) return;

                transform = target.GetComponent<GameObjectComponent>()?.GameObject?.transform;
                if (transform == null) return;

                elapsed += 16f;
                float t = Mathf.Clamp01(elapsed / HitScaleDurationMs);
                transform.localScale = Vector3.Lerp(originalScale * HitScalePunch, originalScale, t);
            }

            if (transform != null)
            {
                transform.localScale = originalScale;
            }
        }

        private static bool CaptureFlashMaterials(
            Renderer[] renderers,
            Material[][] rendererMaterials,
            bool[][] hasBaseColors,
            Color[][] originalBaseColors,
            bool[][] hasColors,
            Color[][] originalColors,
            bool[][] hasEmissionColors,
            Color[][] originalEmissionColors,
            bool[][] originalEmissionKeywords)
        {
            bool hasAnyUsableProperty = false;
            for (int i = 0; i < renderers.Length; ++i)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer)))
                {
                    continue;
                }

                Material[] materials = renderer.materials;
                if (materials == null || materials.Length == 0)
                {
                    continue;
                }

                rendererMaterials[i] = materials;
                hasBaseColors[i] = new bool[materials.Length];
                originalBaseColors[i] = new Color[materials.Length];
                hasColors[i] = new bool[materials.Length];
                originalColors[i] = new Color[materials.Length];
                hasEmissionColors[i] = new bool[materials.Length];
                originalEmissionColors[i] = new Color[materials.Length];
                originalEmissionKeywords[i] = new bool[materials.Length];

                for (int j = 0; j < materials.Length; ++j)
                {
                    Material material = materials[j];
                    if (material == null)
                    {
                        continue;
                    }

                    if (material.HasProperty("_BaseColor"))
                    {
                        hasBaseColors[i][j] = true;
                        originalBaseColors[i][j] = material.GetColor("_BaseColor");
                        hasAnyUsableProperty = true;
                    }

                    if (material.HasProperty("_Color"))
                    {
                        hasColors[i][j] = true;
                        originalColors[i][j] = material.GetColor("_Color");
                        hasAnyUsableProperty = true;
                    }

                    if (material.HasProperty("_EmissionColor"))
                    {
                        hasEmissionColors[i][j] = true;
                        originalEmissionColors[i][j] = material.GetColor("_EmissionColor");
                        originalEmissionKeywords[i][j] = material.IsKeywordEnabled(EmissionKeyword);
                        hasAnyUsableProperty = true;
                    }
                }
            }

            return hasAnyUsableProperty;
        }

        private static void ApplyFlash(
            Material[][] rendererMaterials,
            bool[][] hasBaseColors,
            Color[][] originalBaseColors,
            bool[][] hasColors,
            Color[][] originalColors,
            bool[][] hasEmissionColors,
            Color[][] originalEmissionColors,
            float amount)
        {
            float clampedAmount = Mathf.Clamp01(amount);
            float intensityMultiplier = Mathf.Lerp(1f, HitFlashBaseColorMultiplier, clampedAmount);
            Color emissionBoost = Color.white * (clampedAmount * HitFlashEmissionBoost);

            for (int i = 0; i < rendererMaterials.Length; ++i)
            {
                Material[] materials = rendererMaterials[i];
                if (materials == null)
                {
                    continue;
                }

                for (int j = 0; j < materials.Length; ++j)
                {
                    Material material = materials[j];
                    if (material == null)
                    {
                        continue;
                    }

                    if (hasBaseColors[i] != null && hasBaseColors[i][j])
                    {
                        material.SetColor("_BaseColor", MultiplyColor(originalBaseColors[i][j], intensityMultiplier));
                    }

                    if (hasColors[i] != null && hasColors[i][j])
                    {
                        material.SetColor("_Color", MultiplyColor(originalColors[i][j], intensityMultiplier));
                    }

                    if (hasEmissionColors[i] != null && hasEmissionColors[i][j])
                    {
                        material.EnableKeyword(EmissionKeyword);
                        material.SetColor("_EmissionColor", originalEmissionColors[i][j] + emissionBoost);
                    }
                }
            }
        }

        private static void RestoreFlash(
            Material[][] rendererMaterials,
            bool[][] hasBaseColors,
            Color[][] originalBaseColors,
            bool[][] hasColors,
            Color[][] originalColors,
            bool[][] hasEmissionColors,
            Color[][] originalEmissionColors,
            bool[][] originalEmissionKeywords)
        {
            for (int i = 0; i < rendererMaterials.Length; ++i)
            {
                Material[] materials = rendererMaterials[i];
                if (materials == null)
                {
                    continue;
                }

                for (int j = 0; j < materials.Length; ++j)
                {
                    Material material = materials[j];
                    if (material == null)
                    {
                        continue;
                    }

                    if (hasBaseColors[i] != null && hasBaseColors[i][j])
                    {
                        material.SetColor("_BaseColor", originalBaseColors[i][j]);
                    }

                    if (hasColors[i] != null && hasColors[i][j])
                    {
                        material.SetColor("_Color", originalColors[i][j]);
                    }

                    if (hasEmissionColors[i] != null && hasEmissionColors[i][j])
                    {
                        material.SetColor("_EmissionColor", originalEmissionColors[i][j]);
                        if (originalEmissionKeywords[i] != null && originalEmissionKeywords[i][j])
                        {
                            material.EnableKeyword(EmissionKeyword);
                        }
                        else
                        {
                            material.DisableKeyword(EmissionKeyword);
                        }
                    }
                }
            }
        }

        private static Color MultiplyColor(Color source, float multiplier)
        {
            return new Color(source.r * multiplier, source.g * multiplier, source.b * multiplier, source.a);
        }
    }
}
