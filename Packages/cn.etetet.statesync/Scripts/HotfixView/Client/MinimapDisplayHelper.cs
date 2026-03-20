using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 小地图显示辅助方法，统一底图有效 UV 与 UI 节点拉伸规则。
    /// </summary>
    public static class MinimapDisplayHelper
    {
        public static void StretchToFillParent(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        public static Rect GetExpandedBaseUvRect(MinimapRuntimeComponent runtime, Texture texture)
        {
            return GetBaseTextureWorldUvRect(runtime, texture);
        }

        public static Rect GetExpandedFogUvRect()
        {
            return new Rect(0f, 0f, 1f, 1f);
        }

        public static Rect GetCompactBaseUvRect(MinimapRuntimeComponent runtime, Texture texture, float3 centerPosition)
        {
            Rect worldUvRect = GetBaseTextureWorldUvRect(runtime, texture);
            if (!TryGetWorldSpan(runtime, out float width, out float height))
            {
                return worldUvRect;
            }

            float2 center = runtime.WorldToNormalizedPosition(centerPosition);
            float uvWidth = Mathf.Clamp01((runtime.CompactRange * 2f) / width) * worldUvRect.width;
            float uvHeight = Mathf.Clamp01((runtime.CompactRange * 2f) / height) * worldUvRect.height;
            float x = center.x * worldUvRect.width + worldUvRect.x - uvWidth * 0.5f;
            float y = center.y * worldUvRect.height + worldUvRect.y - uvHeight * 0.5f;
            x = Mathf.Clamp(x, worldUvRect.x, worldUvRect.x + worldUvRect.width - uvWidth);
            y = Mathf.Clamp(y, worldUvRect.y, worldUvRect.y + worldUvRect.height - uvHeight);
            return new Rect(x, y, uvWidth, uvHeight);
        }

        public static Rect GetCompactFogUvRect(MinimapRuntimeComponent runtime, float3 centerPosition)
        {
            if (!TryGetWorldSpan(runtime, out float width, out float height))
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float2 center = runtime.WorldToNormalizedPosition(centerPosition);
            float uvWidth = Mathf.Clamp01((runtime.CompactRange * 2f) / width);
            float uvHeight = Mathf.Clamp01((runtime.CompactRange * 2f) / height);
            float x = Mathf.Clamp(center.x - uvWidth * 0.5f, 0f, 1f - uvWidth);
            float y = Mathf.Clamp(center.y - uvHeight * 0.5f, 0f, 1f - uvHeight);
            return new Rect(x, y, uvWidth, uvHeight);
        }

        private static Rect GetBaseTextureWorldUvRect(MinimapRuntimeComponent runtime, Texture texture)
        {
            if (!TryGetWorldSpan(runtime, out float width, out float height) || texture == null || texture.height <= 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float worldAspect = width / height;
            float textureAspect = texture.width / (float)texture.height;
            if (worldAspect <= 0f || textureAspect <= 0f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (Mathf.Abs(textureAspect - worldAspect) <= 0.0001f)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (textureAspect > worldAspect)
            {
                float activeWidth = worldAspect / textureAspect;
                float paddingX = (1f - activeWidth) * 0.5f;
                return new Rect(paddingX, 0f, activeWidth, 1f);
            }

            float activeHeight = textureAspect / worldAspect;
            float paddingY = (1f - activeHeight) * 0.5f;
            return new Rect(0f, paddingY, 1f, activeHeight);
        }

        private static bool TryGetWorldSpan(MinimapRuntimeComponent runtime, out float width, out float height)
        {
            width = 0f;
            height = 0f;
            if (runtime == null)
            {
                return false;
            }

            width = runtime.WorldMaxX - runtime.WorldMinX;
            height = runtime.WorldMaxZ - runtime.WorldMinZ;
            return width > 0f && height > 0f;
        }
    }
}
